using archie.Backends;
using archie.io;
using archie.Models;
using Microsoft.Extensions.Logging;

namespace archie.Commands;

[Command(Name = "diff", Description = "Compute difference on source and target")]
public sealed class ComputeCommand
{
    private readonly ILogger<ComputeCommand> _logger;
    private readonly IBackendFactory _backends;
    private readonly IObjectFormatter _fmt;
    private readonly IStreams _wr;

    public ComputeCommand(ILogger<ComputeCommand> logger, IBackendFactory backends, IObjectFormatter fmt, IStreams stream)
    {
        _logger = logger;
        _backends = backends;
        _fmt = fmt;
        _wr = stream;
    }


    [Handler]
    public async Task<int> HandleCompute(
        [Option<Uri>(
            Aliases = new string[] { "--source", "-s"},
            IsRequired = true,
            Description = "Source directory"
        )]Uri source, 
        [Option<Uri>(
            Aliases = new string[] { "--target", "-t"},
            IsRequired = true,
            Description = "Target directory"
        )]Uri target, 
        [Option<Uri>(
            Aliases = new string[] { "--output", "-o" },
            Description = "Output file"
        )]Uri? output)
    {
        using (var scope = _logger.BeginScope($"{Guid.NewGuid()} HandleCompute"))
        {
            _logger.LogDebug($"Source={source}, Target={target}");
            try
            {
                var sourceBackend = _backends.GetByScheme(source.Scheme);
                var targetBackend = _backends.GetByScheme(target.Scheme);
                var sourceList = await sourceBackend.List(source);
                var targetList = await targetBackend.List(target);
                _logger.LogDebug($"SourceCount={sourceList.Count()}; TargetCount={targetList.Count()}");

                var onlyOnTarget = GetOnlyInFirstList(targetList, sourceList, targetBackend, target, sourceBackend, source);
                var onlyOnSource = GetOnlyInFirstList(sourceList, targetList, sourceBackend, source, targetBackend, target);
                var both = GetOnBothLists(sourceList, targetList, sourceBackend, source, targetBackend, target);

                _logger.LogDebug($"OnlyOnTarget={onlyOnTarget.Count()}; OnlyOnSource={onlyOnSource.Count()}; Intersect={both.Count()}");

                var diffDescription = new DiffDescription
                {
                    Created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.sssZ"),
                    Source = source,
                    Target = target,
                    OnlyOnSource = onlyOnSource.ToList(),
                    OnlyOnTarget = onlyOnTarget.ToList(),
                    Both = both.Select(_ => new BothPoco
                    {
                        Source = _.First,
                        Target = _.Second
                    }).ToList()
                };

                var outputString = _fmt.FormatObject(diffDescription);

                if (output == default(Uri?))
                {
                    await _wr.Stdout.WriteLineAsync(outputString);
                }
                else
                {
                    var outputBackend = _backends.GetByScheme(output!.Scheme);
                    using (var sr = await outputBackend.OpenWrite(output!, FileMode.OpenOrCreate))
                    using (var wr = (TextWriter)new StreamWriter(sr))
                    {
                        await wr.WriteLineAsync(outputString);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                await _wr.Stderror.WriteLineAsync($"{e.Message} - {e.StackTrace}");
                return 100;
            }
            return 0;
        }
    }

    public class TupleOps
    {
        public FileDescription First { get; set; }
        public FileDescription Second { get; set; }
    }

    public IEnumerable<TupleOps> GetOnBothLists(
        IEnumerable<FileDescription> first,
        IEnumerable<FileDescription> second,
        IBackend firstB,
        Uri firstBase,
        IBackend secondB,
        Uri secondBase)
    {
        var firstDict = first.ToDictionary(_ => string.Join("/", firstB.GetRelativeFragments(firstBase, _.FullName)));
        var secondDict = second.ToDictionary(_ => string.Join("/", secondB.GetRelativeFragments(secondBase, _.FullName)));
        var both = firstDict.Keys.Intersect(secondDict.Keys);
        foreach (var key in both)
        {
            var f = firstDict[key];
            var s = secondDict[key];
            if (f.MD5 != s.MD5)
            {
                yield return new TupleOps
                {
                    First = f,
                    Second = s
                };
            }
        }
    }

    public IEnumerable<FileDescription> GetOnlyInFirstList(
        IEnumerable<FileDescription> first,
        IEnumerable<FileDescription> second,
        IBackend firstB,
        Uri firstBase,
        IBackend secondB,
        Uri secondBase)
    {
        var firstDict = first.ToDictionary(_ => string.Join("/", firstB.GetRelativeFragments(firstBase, _.FullName)), _ => _);
        var secondDict = second.ToDictionary(_ => string.Join("/", secondB.GetRelativeFragments(secondBase, _.FullName)), _ => _);
        var except = firstDict.Keys.Except(secondDict.Keys);
        foreach (var key in except)
        {
            yield return firstDict[key];
        }
    }
}