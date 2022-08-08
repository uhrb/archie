using System.CommandLine;
using archie.Backends;
using archie.io;
using archie.Models;
using Microsoft.Extensions.Logging;

namespace archie.Commands;

public class ComputeCommand : ICommand
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

    public void Register(RootCommand rootCommand)
    {
        var computeCommand = new Command("diff", "Compute difference between fs points");
        var optionSource = new Option<Uri>("--source", "Source directory")
        {
            IsRequired = true
        };
        var optionTarget = new Option<Uri>("--target", "Target directory")
        {
            IsRequired = true
        };

        var optionOutput = new Option<Uri>("--output", "Output file");
        computeCommand.AddOption(optionOutput);
        computeCommand.AddOption(optionSource);
        computeCommand.AddOption(optionTarget);
        computeCommand.SetHandler(
            (Uri source, Uri target, Uri? output) => HandleCompute(source, target, output),
            optionSource, optionTarget, optionOutput);
        rootCommand.AddCommand(computeCommand);
        _logger.LogDebug("Command Registered");
    }

    private async Task<int> HandleCompute(Uri source, Uri target, Uri? output)
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

                var onlyOnTarget = targetList.Where(_ =>
                    !sourceList.Any(__ => _.GetRelativeName(target) == __.GetRelativeName(source)));

                var onlyOnSource = sourceList.Where(_ =>
                    !targetList.Any(__ => _.GetRelativeName(source) == __.GetRelativeName(target)));

                var both = sourceList
                    .Where(_ => targetList.Any(__ => __.GetRelativeName(target) == _.GetRelativeName(source)))
                    .Select(_ => new
                    {
                        Source = _,
                        Target = targetList.First(__ => __.GetRelativeName(target) == _.GetRelativeName(source))
                    });

                _logger.LogDebug($"OnlyOnTarget={onlyOnTarget.Count()}; OnlyOnSource={onlyOnSource.Count()}; Intersect={both.Count()}");

                var diffDescription = new DiffDescription
                {
                    Created = DateTime.UtcNow.ToString("yyyy-mm-ddTHH:MM:ss.sssZ"),
                    Source = source,
                    Target = target,
                    OnlyOnSource = onlyOnSource.ToList(),
                    OnlyOnTarget = onlyOnTarget.ToList(),
                    Both = both.Select(_ => new[] { _.Source, _.Target }).ToList()
                };

                var outputString = _fmt.FormatObject(diffDescription);

                if (output == default(Uri?))
                {
                    await _wr.Stdout.WriteLineAsync(outputString);
                }
                else
                {
                    var outputBackend = _backends.GetByScheme(output!.Scheme);
                    using (var sr = outputBackend.OpenWrite(output!))
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
}