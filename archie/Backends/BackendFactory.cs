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
    public IBackend GetByScheme(string schema)
    {
        _logger.LogTrace($"GetBySchema {schema}");

        switch (schema)
        {
            case "fs":
                return (IBackend)ActivatorUtilities.CreateInstance(_provider, typeof(FsBackend));
            default:
                throw new NotSupportedException();
        }
    }
}