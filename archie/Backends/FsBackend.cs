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

    public async Task<FileDescription> GetFileDescription(Uri path)
    {
        CheckScheme(path);
        var entry = await ComputeEntry(path.AbsolutePath);
        entry.FullName = path;
        return entry;
    }

    private async Task<FileDescription> ComputeEntry(string path)
    {
        var fi = new FileInfo(path);
        if (!fi.Exists)
        {
            throw new FileNotFoundException($"File {path} not found");
        }
        return new FileDescription
        {
            MD5 = await GetHash(path),
        };
    }
    public async Task<string> GetHash(string path)
    {
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
                _logger.LogTrace($"{hash} - {path}");
                return hash;
            }
        }
    }
    public Stream OpenRead(Uri uri)
    {
        CheckScheme(uri);
        return File.OpenRead(uri.AbsolutePath);
    }

    public Stream OpenWrite(Uri uri)
    {
        CheckScheme(uri);
        return File.OpenWrite(uri.AbsolutePath);
    }

    public async Task<IEnumerable<FileDescription>> List(Uri path)
    {
        CheckScheme(path);

        _logger.LogDebug($"List {path} {path.AbsolutePath}");

        var files = Directory.GetFiles(path.AbsolutePath, "*", SearchOption.AllDirectories);
        var tasks = files.Select(async _ =>
        {
            var entry = await ComputeEntry(_);
            entry.FullName = new Uri(path, _);
            return entry;
        });
        await Task.WhenAll(tasks);

        return tasks.Select(_ => _.Result);
    }

    private void CheckScheme(Uri uri)
    {
        if (uri.Scheme != _scheme)
        {
            throw new NotSupportedException($"Scheme {uri.Scheme} not supported by FsBackend Param={uri}");
        }
    }

    public Uri ComputePath(Uri basePath, string[] fragments)
    {
        _logger.LogTrace($"ComputePath {basePath} {string.Join(",", fragments)}");
        var path = Path.Combine(basePath.AbsolutePath, string.Join(Path.DirectorySeparatorChar, fragments));
        return new Uri($"{_scheme}://{path}");
    }

    public string[] GetRelativeFragments(Uri basePath, Uri fullName)
    {
        CheckScheme(basePath);
        CheckScheme(fullName);
        var result = Path.GetRelativePath(basePath.AbsolutePath, fullName.AbsolutePath).Split(Path.DirectorySeparatorChar);
        _logger.LogTrace($"GetRelativeFragments {basePath} {fullName} {string.Join(",", result)}");
        return result;

    }
}