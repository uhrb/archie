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
        var patchOption = new Option<Uri>("--patch", "Patch file stream")
        {
            IsRequired = true
        };
        var outOption = new Option<Uri>("--output", "Output for patch");
        var s_oos = new Option<OperationType>("--strategy-oos", () => OperationType.Unknown, "Only on source resolution strategy");
        var s_oot = new Option<OperationType>("--strategy-oot", () => OperationType.Unknown, "Only on source resolution strategy");
        var s_both = new Option<OperationType>("--strategy-both", () => OperationType.Unknown, "Conflict resolution strategy");
        cmd.AddOption(patchOption);
        cmd.AddOption(outOption);

        cmd.AddOption(s_oos);
        cmd.AddOption(s_oot);
        cmd.AddOption(s_both);

        cmd.SetHandler((Uri patch, Uri? output, OperationType s_oos, OperationType s_oot, OperationType s_both)
                => HandleResolve(patch, output, s_oos, s_oot, s_both), patchOption, outOption, s_oos, s_oot, s_both);
        command.AddCommand(cmd);
        _logger.LogDebug("Command registered");
    }

    private async Task<int> HandleResolve(Uri patch, Uri? output, OperationType s_oos, OperationType s_oot, OperationType s_both)
    {
        using (_logger.BeginScope($"{Guid.NewGuid()} HandleResolve"))
        {
            _logger.LogDebug($"Input patch={patch}, output={output}, s_oos={s_oos}, s_oot={s_oot}, both={s_both}");
            try
            {
                var patchBackend = _bf.GetByScheme(patch.Scheme);
                DiffDescription desc;
                using (var file = await patchBackend.OpenRead(patch))
                using (TextReader reader = new StreamReader(file))
                {
                    var jsono = await reader.ReadToEndAsync();
                    desc = _fmt.UnformatObject<DiffDescription>(jsono);
                }
                _logger.LogDebug($"OnlyOnSource={desc.OnlyOnSource.Count}; OnlyOnTarget={desc.OnlyOnTarget.Count}; Both={desc.Both.Count}");
                var resolutions = new List<FileOperation>();
                if (desc.OnlyOnSource.Count > 0)
                {
                    switch (s_oos)
                    {
                        case OperationType.Unknown:
                            break;
                        case OperationType.CopyToTarget:
                        case OperationType.DeleteAtSource:
                            resolutions.AddRange(ComputeOnlyOnSourceAuto(desc.OnlyOnSource, desc.Source, desc.Target, s_oos));
                            break;
                        default:
                            throw new ArgumentException($"Strategy for OnlyOnSource cannot have value {s_oos}");
                    }
                }

                switch (s_oot)
                {
                    case OperationType.Unknown:
                        break;
                    case OperationType.CopyToSource:
                    case OperationType.DeleteAtTarget:
                        resolutions.AddRange(ComputeOnlyOnTargetAuto(desc.OnlyOnTarget, desc.Source, desc.Target, s_oot));
                        break;
                    default:
                        throw new ArgumentException($"Strategy for OnlyOnTarget cannot have valye {s_oot}");
                }

                switch (s_both)
                {
                    case OperationType.Unknown:
                        break;
                    case OperationType.DeleteAtSource:
                    case OperationType.DeleteAtTarget:
                    case OperationType.OverwriteAtSource:
                    case OperationType.OverwriteAtTarget:
                    case OperationType.AppendAtSource:
                    case OperationType.AppendAtTarget:
                        resolutions.AddRange(ComputeBothAuto(desc.Both, desc.Source, desc.Target, s_both));
                        break;
                    default:
                        throw new ArgumentException($"Strategy for Both cannot have value {s_both}");
                }

                _logger.LogDebug($"All resolved - {resolutions.Count}");
                var resolve = new ResolveDescription
                {
                    Source = desc.Source,
                    Target = desc.Target,
                    Created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    Operations = resolutions
                };
                var json = _fmt.FormatObject(resolve);
                if (output != default(Uri?))
                {
                    var outputBackend = _bf.GetByScheme(output.Scheme);
                    using (var f = await outputBackend.OpenWrite(output, FileMode.OpenOrCreate))
                    using (TextWriter wrr = new StreamWriter(f))
                    {
                        await wrr.WriteLineAsync(json);
                    }
                }
                else
                {
                    await _s.Stdout.WriteLineAsync(json);
                }
                _logger.LogDebug("Done");
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

    private IEnumerable<FileOperation> ComputeBothAuto(List<BothPoco> both, Uri source, Uri target, OperationType s_both)
    {
        return both.Select(_ => new FileOperation
        {
            Source = _.Source,
            Target = _.Target,
            Operation = s_both
        });
    }

    private IEnumerable<FileOperation> ComputeOnlyOnTargetAuto(List<FileDescription> onlyOnTarget, Uri source, Uri target, OperationType s_oot)
    {
        return onlyOnTarget.Select(_ =>
        {
            var op = new FileOperation
            {
                Target = _,
                Operation = s_oot,
            };
            switch (s_oot)
            {
                case OperationType.CopyToSource:
                    var targetBackend = _bf.GetByScheme(target.Scheme);
                    var sourceBackend = _bf.GetByScheme(source.Scheme);
                    var fragments = targetBackend.GetRelativeFragments(target, _.FullName);
                    var sourceName = sourceBackend.ComputePath(source, fragments);
                    op.Source = new FileDescription
                    {
                        FullName = sourceName,
                        MD5 = string.Empty
                    };
                    break;
            }

            return op;
        });
    }

    private IEnumerable<FileOperation> ComputeOnlyOnSourceAuto(List<FileDescription> onlyOnSource, Uri source, Uri target, OperationType s_oos)
    {
        return onlyOnSource.Select(_ =>
        {
            var op = new FileOperation
            {
                Source = _,
                Operation = s_oos,
            };
            switch (s_oos)
            {
                case OperationType.CopyToTarget:
                    var targetBackend = _bf.GetByScheme(target.Scheme);
                    var sourceBackend = _bf.GetByScheme(source.Scheme);
                    var fragments = sourceBackend.GetRelativeFragments(source, _.FullName);
                    var targetName = targetBackend.ComputePath(target, fragments);
                    op.Target = new FileDescription
                    {
                        FullName = targetName,
                        MD5 = string.Empty
                    };
                    break;
            }

            return op;
        });
    }

    /*
    private async Task printFileInfo(string label, FileDescription i)
    {
        var tags = i.Tags != null ? string.Join(',', i.Tags) : "";
        await _s.Stdout.WriteLineAsync(
        @$"----------------- {label} -----------------
        Base:       {i.FullName}
        Hash:       {i.MD5}
        Created:    {i.CreatedAt.ToString("yyyy-mm-ddTHH:MM:ss.fffzzz")}
        Modified:   {i.ModifiedAt.ToString("yyyy-mm-DDTHH:MM:ss.fffzzz")}
        Size:       {i.Size}
        Tags:       {tags}
        Tier:       {i.Tier}
        ");
    }
    */
}