using System.Security.Cryptography;
using archie.Models;
using Microsoft.Extensions.Logging;

namespace archie.Backends;

public class FsBackend : IBackend
{
    const string _scheme = "fs";

    private ILogger<FsBackend> _logger;

    public FsBackend(ILogger<FsBackend> logger)
    {
        _logger = logger;
    }

    public FileEntryInfo GetEntryInfo(FileEntry target)
    {
        var fullName = Path.Combine(target.BasePath, target.RelativeName);
        var fi = new FileInfo(fullName);
        if (!fi.Exists)
        {
            throw new FileNotFoundException();
        }
        return new FileEntryInfo
        {
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
        var path = entry.FullName.AbsolutePath;
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
                _logger.LogTrace($"{hash} - {entry.BasePath}:{entry.RelativeName} - {entry.FullName}");
                return hash;
            }
        }
    }

    public Task<IEnumerable<FileEntry>> List(Uri path)
    {
        if (path.Scheme != _scheme)
        {
            throw new NotSupportedException();
        }

        _logger.LogDebug($"List {path} {path.AbsolutePath}");

        var files = Directory.GetFiles(path.AbsolutePath, "*", SearchOption.AllDirectories);
        return Task.FromResult(files.Select(_ => new FileEntry
        {
            RelativeName = Path.GetRelativePath(path.AbsolutePath, _),
            BasePath = path.AbsolutePath,
            FullName = new Uri($"fs://{_}")
        }));
    }

    public Stream OpenRead(FileEntry entry)
    {
        if (entry.FullName.Scheme != _scheme)
        {
            throw new NotSupportedException();
        }
        return File.OpenRead(entry.FullName.AbsolutePath);
    }

    public Stream OpenRead(Uri uri)
    {
        if (uri.Scheme != _scheme)
        {
            throw new NotSupportedException();
        }
        return File.OpenRead(uri.AbsolutePath);
    }
}