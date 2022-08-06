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
    private readonly ICommandIO _io;

    public ComputeCommand(ILogger<ComputeCommand> logger, IBackendFactory backends, ICommandIO io)
    {
        _logger = logger;
        _backends = backends;
        _io = io;
    }

    public void Register(RootCommand rootCommand)
    {
        var computeCommand = new Command("compute", "Compute difference between fs points");
        var optionSource = new Option<string>("--source", "Source directory")
        {
            IsRequired = true
        };
        var optionTarget = new Option<string>("--target", "Target directory")
        {
            IsRequired = true
        };
        computeCommand.AddOption(optionSource);
        computeCommand.AddOption(optionTarget);
        computeCommand.SetHandler(
            (string source, string target) => HandleCompute(source, target),
            optionSource, optionTarget);
        rootCommand.AddCommand(computeCommand);
        _logger.LogDebug("Command Registered");
    }

    private async Task HandleCompute(string source, string target)
    {
        using (var scope = _logger.BeginScope($"{Guid.NewGuid()} HandleCompute"))
        {
            _logger.LogDebug($"Source={source}; Target={target}");
            var sourceBackend = _backends.GetBySchema(source);
            var targetBackend = _backends.GetBySchema(target);
            var sourceList = await sourceBackend.List(source);
            var targetList = await targetBackend.List(target);
            _logger.LogDebug($"SourceCount={sourceList.Count()}; TargetCount={targetList.Count()}");
            var onlyOnTarget = targetList.Where(_ => !sourceList.Any(__ => __.RelativeName == _.RelativeName));
            var onlyOnSource = sourceList.Where(_ => !targetList.Any(__ => __.RelativeName == _.RelativeName));
            var same = sourceList
                .Where(_ => targetList.Any(__ => __.RelativeName == _.RelativeName))
                .Select(_ => new
                {
                    Source = _,
                    Target = targetList.First(__ => __.RelativeName == _.RelativeName)
                });

            _logger.LogDebug($"OnlyOnTarget={onlyOnTarget.Count()}; OnlyOnSource={onlyOnSource.Count()}; Intersect={same.Count()}");
            foreach (var targetFile in onlyOnTarget)
            {
                _logger.LogTrace($"OnlyOnTarget={targetFile.BasePath}:{targetFile.RelativeName}");
                await _io.WriteLineAsync(new FileOperation
                {
                    Operation = OperationType.CopyTargetToSource,
                    Target = targetFile,
                    TargetHash = await targetBackend.GetHash(targetFile)
                });
            }
            foreach (var sourceFile in onlyOnSource)
            {
                _logger.LogTrace($"OnlyOnSource={sourceFile.BasePath}:{sourceFile.RelativeName}");
                await _io.WriteLineAsync(new FileOperation
                {
                    Operation = OperationType.CopySourceToTarget,
                    Source = sourceFile,
                    SourceHash = await sourceBackend.GetHash(sourceFile)
                });
            }

            foreach (var intersected in same)
            {
                using (_logger.BeginScope($"{Guid.NewGuid()} Intersected"))
                {
                    var hashSource = await sourceBackend.GetHash(intersected.Source);
                    var hashTarget = await targetBackend.GetHash(intersected.Target);
                    _logger.LogTrace($"Both Source={hashSource}-{intersected.Source.BasePath}:{intersected.Source.RelativeName}; Target={hashTarget}-{intersected.Target.BasePath}:{intersected.Target.RelativeName}");
                    if (hashSource != hashTarget)
                    {
                        _logger.LogDebug($"Conflict detected");
                        await _io.WriteLineAsync(new FileOperation
                        {
                            Source = intersected.Source,
                            Target = intersected.Target,
                            SourceHash = hashSource,
                            TargetHash = hashTarget,
                            Operation = OperationType.Conflict
                        });
                    }
                }
            }
        }
    }
}