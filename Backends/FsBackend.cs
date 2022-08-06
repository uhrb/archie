using System.Security.Cryptography;
using archie.Models;
using Microsoft.Extensions.Logging;

namespace archie.Backends;

public class FsBackend : IBackend
{
    private ILogger<FsBackend> _logger;

    public FsBackend(ILogger<FsBackend> logger)
    {
        _logger = logger;
    }
    public async Task<string> GetHash(string entry)
    {
        using (_logger.BeginScope($"{Guid.NewGuid()} GetHash"))
        {
            if (false == File.Exists(entry))
            {
                throw new FileNotFoundException();
            }
            using (var reader = File.OpenRead(entry))
            using (var md = MD5.Create())
            {
                var bytes = await md.ComputeHashAsync(reader);
                var hash = Convert.ToHexString(bytes);
                _logger.LogTrace($"{hash} - {entry}");
                return hash;
            }
        }
    }

    public Task<string[]> List(string path)
    {
        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        return Task.FromResult(files);
    }
}