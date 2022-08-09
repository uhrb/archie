using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using archie.Backends;
using archie.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace units;

public class BackendsTests
{


    [Theory]
    [InlineData("fs", "fs:///some/non/existing/file.txt")]
    public async Task OpenWriteFailsIfOpenedNonExistingFile(string scheme, string nonExistingFile)
    {
        var backend = GetBackend(scheme);
        await Assert.ThrowsAnyAsync<DirectoryNotFoundException>(async () =>
        {
            using (await backend.Backend.OpenWrite(new Uri(nonExistingFile), FileMode.Open))
            {
            }
        });

    }

    [Theory]
    [InlineData("fs", "fs:///some/non/existing/file.txt")]
    public async Task GetFileDescriptionThrowsForNonExistingFile(string scheme, string nonExistingFile)
    {
        var backend = GetBackend(scheme);
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await backend.Backend.GetFileDescription(new Uri(nonExistingFile));
        });
    }

    [Theory]
    [InlineData("fs", "1.txt", "8C7DD922AD47494FC02C388E12C00EAC")]
    public async Task GetFileDescriptionReturnsCorrectFileDescription(string scheme, string fileName, string hash)
    {
        var backend = GetBackend(scheme);
        var fileUri = backend.Backend.ComputePath(backend.BasePath, new [] { fileName });
        var desc = await backend.Backend.GetFileDescription(fileUri);
        Assert.Equal(fileUri, desc.FullName);
        Assert.Equal(hash, desc.MD5);
    }

    [Theory]
    [InlineData("fs", "fs:///home/foo", "fs:///home/foo/bar", new[] { "bar" })]
    public void GetRelativeFragmentsReturnsCorrectValues(string scheme, string basePath, string fullPath, string[] fragments)
    {
        var backend = GetBackend(scheme);
        var actualFragments = backend.Backend.GetRelativeFragments(new Uri(basePath), new Uri(fullPath));
        Assert.Equal(fragments, actualFragments);
    }

    [Theory]
    [InlineData("fs", "fs:///home", new[] { "some", "dir" }, "fs:///home/some/dir")]
    public void ComputePathReturnsCorrectResult(string scheme, string basePath, string[] fragments, string expected)
    {
        var backend = GetBackend(scheme);
        var path = backend.Backend.ComputePath(new Uri(basePath), fragments);
        Assert.Equal(new Uri(expected), path);
    }


    [Theory]
    [InlineData("fs", "1.txt", "file")]
    [InlineData("fs", "2.txt", "some")]
    public async Task OpenReadReturnsCorrectStream(string scheme, string fileName, string content)
    {
        var backend = GetBackend(scheme);
        var fileUri = backend.Backend.ComputePath(backend.BasePath, new [] { fileName });
        using (var f = await backend.Backend.OpenRead(fileUri))
        using (TextReader tr = new StreamReader(f))
        {
            var actualContent = await tr.ReadToEndAsync();
            Assert.Equal(content, actualContent);
        }
    }

    [Theory]
    [InlineData("fs", "virtualEnv://example.com")]
    public void OpenReadThrowsIfSchemeIsNotValid(string scheme, string nonValidPath)
    {
        var backend = GetBackend(scheme);
        Assert.Throws<NotSupportedException>(() =>
        {
            using (var f = backend.Backend.OpenRead(new Uri(nonValidPath)))
            {

            }
        });
    }

    [Theory]
    [InlineData("fs")]
    public void SchemeIsCorrect(string scheme)
    {
        var backend = GetBackend(scheme);
        Assert.Equal(scheme, backend.Backend.Scheme);
    }

    [Theory]
    [InlineData("fs", new string[] { "1.txt", "2.txt" }, new string[] { "8C7DD922AD47494FC02C388E12C00EAC", "03D59E663C1AF9AC33A9949D1193505A" })]
    public async Task ListReturnsCorrectValues(string scheme, string[] names, string[] hashes)
    {
        var backend = GetBackend(scheme);
        var items = await backend.Backend.List(backend.BasePath);

        var combined = new List<FileDescription>();
        for (var i = 0; i < names.Length; i++)
        {
            combined.Add(new FileDescription
            {
                FullName = backend.Backend.ComputePath(backend.BasePath, new [] { names[i] }),
                MD5 = hashes[i]
            });
        }

        Assert.Equal(combined.Count, items.Count());
        Assert.Equal(combined, items, new FileDescriptionEqualityComparer());
    }

    private class FileDescriptionEqualityComparer : IEqualityComparer<FileDescription>
    {
        public bool Equals(FileDescription? x, FileDescription? y)
        {
            return (x!.FullName == y!.FullName) && (x!.MD5 == y!.MD5);
        }

        public int GetHashCode([DisallowNull] FileDescription obj)
        {
            return obj.GetHashCode();
        }
    }

    private class BackendConfig
    {
        public IBackend Backend { get; set; }
        public Uri BasePath { get; set; }
    }

    private BackendConfig GetBackend(string scheme)
    {

        switch (scheme)
        {
            case "fs":
                return GetFsBackend();
            default:
                throw new Exception();
        }
    }

    private BackendConfig GetFsBackend()
    {
        var fi = new FileInfo(this.GetType().Assembly.Location).DirectoryName;
        var test = Path.Combine(fi!, "test-data/read");
        return new BackendConfig()
        {
            Backend = new FsBackend(GetLogger<FsBackend>()),
            BasePath = new Uri($"fs://{test}")
        };
    }

    private ILogger<T> GetLogger<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }
}