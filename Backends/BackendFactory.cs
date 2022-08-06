using Microsoft.Extensions.Logging;

namespace archie.Backends;

public class BackendFactory : IBackendFactory
{
    private readonly ILogger<BackendFactory> _logger;

    public BackendFactory(ILogger<BackendFactory> logger)
    {
        _logger = logger;
    }
    public IBackend GetBySchema(string schema)
    {
        _logger.LogDebug($"GetBackend {schema}");
        switch (schema)
        {
            default:
                return new FsBackend();
        }
    }
}