using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using archie.Backends;
using archie.Models;

namespace units;

public sealed class VirtualBackend : IBackend
{
    public string Scheme { get; private set; }

    public List<FileDescription> FilesToList { get; private set; }

    public Dictionary<Uri, MemoryStream> OpenedStreams { get; private set; }

    public VirtualBackend(string scheme)
    {
        Scheme = scheme;
        FilesToList = new List<FileDescription>();
        OpenedStreams = new Dictionary<Uri, MemoryStream>();
    }

    public Uri GenerateRandomUri()
    {
        return new Uri($"{Scheme}://{Guid.NewGuid()}/");
    }

    public Uri ComputePath(Uri basePath, string[] fragments)
    {
        CheckScheme(basePath);
        var toJoin = new List<string>();
        toJoin.Add(basePath.AbsolutePath);
        toJoin.AddRange(fragments);
        return new Uri($"{Scheme}://{string.Join("/", toJoin)}");
    }

    public Task<FileDescription> GetFileDescription(Uri path)
    {
        var hashingString = $"{path}";
        return Task.FromResult(new FileDescription
        {
            FullName = path,
            MD5 = Convert.ToHexString(MD5.Create().ComputeHash(Encoding.Default.GetBytes(hashingString)))
        });
    }

    public async Task<IList<FileDescription>> GenerateEntities(Uri rootPath, string[] names)
    {
        CheckScheme(rootPath);
        var lst = new List<FileDescription>();
        foreach (var name in names)
        {
            lst.Add(await GetFileDescription(ComputePath(rootPath, new[] { name })));
        }
        return lst;
    }

    public string[] GetRelativeFragments(Uri basePath, Uri fullName)
    {
        CheckScheme(basePath);
        CheckScheme(fullName);
        var baseFrags = basePath.AbsolutePath.Split("/");
        var fullFrags = fullName.AbsolutePath.Split("/");
        return fullFrags.Skip(baseFrags.Length).ToArray();
    }

    public Task<IEnumerable<FileDescription>> List(Uri path)
    {
        CheckScheme(path);
        return Task.FromResult((IEnumerable<FileDescription>)FilesToList);
    }

    public Task<Stream> OpenRead(Uri uri)
    {
        var stream = new MemoryStream();
        OpenedStreams.Add(uri, stream);
        return Task.FromResult((Stream)stream);
    }

    public Task<Stream> OpenWrite(Uri uri, FileMode mode)
    {
        var stream = new MemoryStream();
        OpenedStreams.Add(uri, stream);
        return Task.FromResult((Stream)stream);
    }

    public void CheckScheme(Uri input)
    {
        if (Scheme != input.Scheme)
        {
            throw new NotSupportedException();
        }
    }

    internal IEnumerable<FileDescription> FromPairList(Uri root, string[] names, string[] hashes)
    {
        for (var i = 0; i < names.Length; i++)
        {
            yield return new FileDescription
            {
                FullName = ComputePath(root, new[] { names[i] }),
                MD5 = hashes[i]
            };
        }
    }
}