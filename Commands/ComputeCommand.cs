using System.CommandLine;
using archie.Backends;
using archie.io;
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
            _logger.LogDebug($"SourceCount={source.Length}; TargetCount={target.Length}");
            var onlyOnTarget = targetList.Except(sourceList);
            var onlyOnSource = sourceList.Except(targetList);
            var same = targetList.Intersect(sourceList);
            foreach(var targetFile in onlyOnTarget) {
                await _io.WriteLineAsync($"copy source << target {targetFile}");
            }
            foreach(var sourceFile in onlyOnSource) {
                await _io.WriteLineAsync($"copy source >> target {sourceFile}");
            }
            foreach(var intersected in same) {
                var hashSource = sourceBackend.GetHash(intersected);
                var hashTarget = targetBackend.GetHash(intersected);
                if(hashSource != hashTarget) {
                    await _io.WriteLineAsync($"conflict {intersected} shash={hashSource}/thash={hashTarget}");
                }
            }
        }
    }
}