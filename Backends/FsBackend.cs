using System.Security.Cryptography;
using archie.Models;

namespace archie.Backends;

public class FsBackend : IBackend
{
    public async Task<string> GetHash(FileEntry entry)
    {
        if (false == File.Exists(entry.Name))
        {
            throw new FileNotFoundException();
        }
        using (var reader = File.OpenRead(entry.Name))
        using (var md = MD5.Create())
        {
            var bytes = await md.ComputeHashAsync(reader);
            return Convert.ToHexString(bytes);
        }
    }

    public Task<string[]> List(string path)
    {
        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        return Task.FromResult(files);
    }
}