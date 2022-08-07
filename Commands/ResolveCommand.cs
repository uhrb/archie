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
    private readonly TextWriter _wr;
    private readonly IBackendFactory _bf;
    private readonly TextReader _rd;

    public ResolveCommand(ILogger<ResolveCommand> logger, IObjectFormatter fmt, TextWriter wr, IBackendFactory bf, TextReader rd)
    {
        _logger = logger;
        _fmt = fmt;
        _wr = wr;
        _bf = bf;
        _rd = rd;
    }
    public void Register(RootCommand command)
    {
        var cmd = new Command("resolve", "Resolve conflicts and prepare");
        var option = new Option<FileInfo>("--patch", "Patch file stream")
        {
            IsRequired = true
        };
        cmd.AddOption(option);
        cmd.SetHandler((FileInfo patch) => HandleResolve(patch), option);
        command.AddCommand(cmd);
    }

    private async Task<int> HandleResolve(FileInfo patch)
    {
        using (_logger.BeginScope($"{Guid.NewGuid()} HandleResolve"))
        {
            try
            {
                var ops = new List<FileOperation>();
                using (var file = File.OpenText(patch.FullName))
                    while (!file.EndOfStream)
                    {
                        var item = _fmt.UnformatObject<FileOperation>(await file.ReadLineAsync());
                        ops.Add(item);
                    }
                var nonConflict = ops.Where(_ => _.Operation != OperationType.Conflict);
                var conflicts = ops.Where(_ => _.Operation == OperationType.Conflict);
                _logger.LogDebug($"Operations={ops.Count}; NonConflict={nonConflict.Count()}; Conflict={conflicts.Count()}");
                var resolutions = new List<FileOperation>();
                foreach (var conflict in conflicts)
                {
                    await _wr.WriteLineAsync("CONFLICT");
                    await _wr.WriteLineAsync($"Files exists in both target and source. Please see information above and select proper file version");
                    var sourceBackend = _bf.GetBySchema(conflict.Source.BasePath);
                    var targetBackend = _bf.GetBySchema(conflict.Target.BasePath);
                    var sourceInfo = sourceBackend.GetEntryInfo(conflict.Source);
                    var targetInfo = targetBackend.GetEntryInfo(conflict.Target);
                    await printFileInfo("Source", sourceInfo, conflict.SourceHash);
                    await printFileInfo("Target", targetInfo, conflict.TargetHash);
                    await _wr.WriteAsync(
                        "Select conflict resolution option(1 - Source->Target, 2 - Target->Source, 3 - Remove, 4 - Exit):");
                    var flag = true;
                    while (flag)
                    {
                        var input = await _rd.ReadLineAsync();
                        var choice = input?.Length >=1 ? input[0] : '0'; 
                        switch (choice)
                        {
                            case '1':
                                resolutions.Add(new FileOperation
                                {
                                    Source = conflict.Source,
                                    Target = conflict.Target,
                                    Operation = OperationType.CopySourceToTarget
                                });
                                await _wr.WriteLineAsync($"RESOLVED Source->Target. File {conflict.Target.RelativeName} will be overwrited on Target.");
                                flag = false;
                                break;
                            case '2':
                                resolutions.Add(new FileOperation
                                {
                                    Source = conflict.Source,
                                    Target = conflict.Target,
                                    Operation = OperationType.CopyTargetToSource
                                });
                                await _wr.WriteLineAsync($"RESOLVED Target->Source. File {conflict.Source.RelativeName} will be overwrited on Source.");
                                flag = false;
                                break;
                            case '3':
                                await _wr.WriteLineAsync($"RESOLVED Skipped. Conflict removed.");
                                flag = false;
                                break;
                            case '4':
                                return 0;
                            default:
                                await _wr.WriteLineAsync("Option not recognized. Please try again.");
                                break;
                        }
                    }
                }
            }
            catch (Exception e)

            {
                _logger.LogError(e, e.Message);
                return 200;
            }
        }

        return 0;
    }

    private async Task printFileInfo(string label, FileEntryInfo i, string hash)
    {
        var tags = i.Tags != null ? string.Join(',', i.Tags) : "";
        await _wr.WriteLineAsync(
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