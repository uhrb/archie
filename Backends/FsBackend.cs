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
    public async Task<string> GetHash(FileEntry entry)
    {
        var path = Path.Combine(entry.BasePath, entry.RelativeName);
        using (_logger.BeginScope($"{Guid.NewGuid()} GetHash"))
        {
            if (false == File.Exists(path))
            {
                throw new FileNotFoundException();
            }
            using (var reader = File.OpenRead(path))
            using (var md = MD5.Create())
            {
                var bytes = await md.ComputeHashAsync(reader);
                var hash = Convert.ToHexString(bytes);
                _logger.LogTrace($"{hash} - {entry.BasePath}:{entry.RelativeName}");
                return hash;
            }
        }
    }

    public Task<IEnumerable<FileEntry>> List(string path)
    {
        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        return Task.FromResult(files.Select(_ => new FileEntry
        {
            RelativeName = Path.GetRelativePath(path, _),
            BasePath = path
        }));
    }
}