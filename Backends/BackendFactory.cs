using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace archie.Backends;

public class BackendFactory : IBackendFactory
{
    private readonly ILogger<BackendFactory> _logger;
    private readonly IServiceProvider _provider;

    public BackendFactory(ILogger<BackendFactory> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }
    public IBackend GetBySchema(string schema)
    {
        _logger.LogDebug($"GetBySchema {schema}");
        switch (schema)
        {
            default:
                return (IBackend)ActivatorUtilities.CreateInstance(_provider, typeof(FsBackend));
        }
    }
}