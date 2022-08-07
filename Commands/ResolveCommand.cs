using System.CommandLine;
using archie.Backends;
using archie.io;
using archie.Models;
using Microsoft.Extensions.Logging;

namespace archie.Commands;

public class ResolveCommand : ICommand
{
    private readonly ILogger<ResolveCommand> _logger;
    private readonly IObjectFormatter _fmt;
    private readonly IStreams _s;
    private readonly IBackendFactory _bf;

    public ResolveCommand(ILogger<ResolveCommand> logger, IObjectFormatter fmt, IBackendFactory bf, IStreams streams)
    {
        _logger = logger;
        _fmt = fmt;
        _bf = bf;
        _s = streams;
    }
    public void Register(RootCommand command)
    {
        var cmd = new Command("resolve", "Resolve conflicts and prepare");
        var option = new Option<Uri>("--patch", "Patch file stream")
        {
            IsRequired = true
        };
        cmd.AddOption(option);
        cmd.SetHandler((Uri patch) => HandleResolve(patch), option);
        command.AddCommand(cmd);
    }

    private async Task<int> HandleResolve(Uri patch)
    {
        using (_logger.BeginScope($"{Guid.NewGuid()} HandleResolve"))
        {
            try
            {
                var ops = new List<FileOperation>();
                var patchBackend = _bf.GetBySchema(patch.Scheme);
                string? line;
                using (var file = patchBackend.OpenRead(patch))
                using (TextReader reader = new StreamReader(file))
                {
                    while (null != (line = await reader.ReadLineAsync()))
                    {
                        var item = _fmt.UnformatObject<FileOperation>(line);
                        ops.Add(item);
                    }
                }
                var nonConflict = ops.Where(_ => _.Operation != OperationType.Conflict);
                var conflicts = ops.Where(_ => _.Operation == OperationType.Conflict);
                _logger.LogDebug($"Operations={ops.Count}; NonConflict={nonConflict.Count()}; Conflict={conflicts.Count()}");
                var resolutions = new List<FileOperation>();
                foreach (var conflict in conflicts)
                {
                    using (_logger.BeginScope($"{Guid.NewGuid()} Conflict Resolution"))
                    {
                        _logger.LogDebug($"Conflict {_fmt.FormatObject(conflict)}");
                        await _s.Stdout.WriteLineAsync("CONFLICT");
                        await _s.Stdout.WriteLineAsync($"Files exists in both target and source. Please see information above and select proper file version");
                        var sourceBackend = _bf.GetBySchema(conflict.Source.BasePath);
                        var targetBackend = _bf.GetBySchema(conflict.Target.BasePath);
                        var sourceInfo = sourceBackend.GetEntryInfo(conflict.Source);
                        var targetInfo = targetBackend.GetEntryInfo(conflict.Target);
                        await printFileInfo("Source", sourceInfo, conflict.SourceHash);
                        await printFileInfo("Target", targetInfo, conflict.TargetHash);
                        await _s.Stdout.WriteAsync(
                            "Select conflict resolution option(1 - Source->Target, 2 - Target->Source, 3 - Remove, 4 - Exit):");
                        var flag = true;
                        while (flag)
                        {
                            var input = await _s.Stdin.ReadLineAsync();
                            var choice = input?.Length >= 1 ? input[0] : '0';
                            switch (choice)
                            {
                                case '1':
                                    var op1 = new FileOperation
                                    {
                                        Source = conflict.Source,
                                        Target = conflict.Target,
                                        Operation = OperationType.CopySourceToTarget
                                    };
                                    resolutions.Add(op1);
                                    _logger.LogInformation($"Resolved Source->Target {_fmt.FormatObject(op1)}");
                                    await _s.Stdout.WriteLineAsync($"RESOLVED Source->Target. File {conflict.Target.RelativeName} will be overwrited on Target.");
                                    flag = false;
                                    break;
                                case '2':
                                    var op2 = new FileOperation
                                    {
                                        Source = conflict.Source,
                                        Target = conflict.Target,
                                        Operation = OperationType.CopyTargetToSource
                                    };
                                    resolutions.Add(op2);
                                    _logger.LogInformation($"Resolved Target->Source {_fmt.FormatObject(op2)}");
                                    await _s.Stdout.WriteLineAsync($"RESOLVED Target->Source. File {conflict.Source.RelativeName} will be overwrited on Source.");
                                    flag = false;
                                    break;
                                case '3':
                                    _logger.LogInformation($"Resolved Skipped");
                                    await _s.Stdout.WriteLineAsync($"RESOLVED Skipped. Conflict removed.");
                                    flag = false;
                                    break;
                                case '4':
                                    return 0;
                                default:
                                    await _s.Stdout.WriteLineAsync("Option not recognized. Please try again.");
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                await _s.Stderror.WriteLineAsync($"{e.Message} - {e.StackTrace}");
                return 200;
            }
        }

        return 0;
    }

    private async Task printFileInfo(string label, FileEntryInfo i, string hash)
    {
        var tags = i.Tags != null ? string.Join(',', i.Tags) : "";
        await _s.Stdout.WriteLineAsync(
        @$"----------------- {label} -----------------
        Base:       {i.BasePath}
        Relative:   {i.RelativeName}
        Hash:       {hash}
        Created:    {i.CreatedAt.ToString("yyyy-mm-ddTHH:MM:ss.fffzzz")}
        Modified:   {i.ModifiedAt.ToString("yyyy-mm-DDTHH:MM:ss.fffzzz")}
        Size:       {i.Size}
        Tags:       {tags}
        Tier:       {i.Tier}
        ");
    }
}