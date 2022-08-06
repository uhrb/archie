using System.CommandLine;
using archie.Backends;
using Microsoft.Extensions.Logging;

namespace archie.Commands;

public class ComputeCommand : ICommand
{
    private readonly ILogger<ComputeCommand> _logger;
    private readonly IBackendFactory _backends;

    public ComputeCommand(ILogger<ComputeCommand> logger, IBackendFactory backends)
    {
        _logger = logger;
        _backends = backends;
    }

    public void Register(RootCommand rootCommand)
    {
        var computeCommand = new Command("compute", "Compute difference between fs points");
        var optionSource = new Option<string>("--source", "Source directory");
        var optionTarget = new Option<string>("--target", "Target directory");
        computeCommand.AddOption(optionSource);
        computeCommand.AddOption(optionTarget);
        computeCommand.SetHandler(
            (string source, string target) => HandleCompute(source, target),
            optionSource, optionTarget);
        rootCommand.AddCommand(computeCommand);
        _logger.LogDebug("Register");
    }

    private async Task HandleCompute(string source, string target)
    {
        _logger.LogDebug($"HandleCompute (Source={source}, Target={target})");
        var sourceBackend = _backends.GetBySchema(source);
        var targetBackend = _backends.GetBySchema(target);
        var sourceList = await sourceBackend.List(source);
        var targetList = await targetBackend.List(target);
    }
}