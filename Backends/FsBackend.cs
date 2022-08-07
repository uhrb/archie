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

    public FileEntryInfo GetEntryInfo(FileEntry target)
    {
        var fullName = Path.Combine(target.BasePath, target.RelativeName);
        var fi = new FileInfo(fullName);
        if(!fi.Exists) {
            throw new FileNotFoundException();
        }
        return new FileEntryInfo {
            RelativeName = target.RelativeName,
            BasePath = target.BasePath,
            CreatedAt = fi.CreationTimeUtc,
            ModifiedAt = fi.LastWriteTimeUtc,
            Tier = FileEntryTiers.NotSupported,
            Size = fi.Length
        };
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