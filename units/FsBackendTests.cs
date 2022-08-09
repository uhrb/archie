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

public class FsBackendTests
{
    readonly string _testDataDirectory;
    readonly string _testWriteDirectory;

    public FsBackendTests()
    {
        var fi = new FileInfo(this.GetType().Assembly.Location).DirectoryName;
        _testDataDirectory = Path.Combine(fi!, "test-data/read");
        _testWriteDirectory = Path.Combine(fi!, "test-data");
    }

    [Fact]
    public async Task OpenWriteFailsIfOpenedNonExistingFile() {
        var backend = GetBackend();
        await Assert.ThrowsAnyAsync<DirectoryNotFoundException>(async() => {
            using(await backend.OpenWrite(new Uri("fs:///some/non/existing/file.txt"), FileMode.Open))
            {
            }
        });
        
    }

    [Fact]
    public async Task GetFileDescriptionThrowsForNonExistingFile()
    {
        var backend = GetBackend();
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await backend.GetFileDescription(new Uri("fs:///non/exising/file.txt"));
        });
    }

    [Theory]
    [InlineData("1.txt", "8C7DD922AD47494FC02C388E12C00EAC")]
    public async Task GetFileDescriptionReturnsCorrectFileDescription(string fileName, string hash) {
        var backend = GetBackend();
        var fileUri = new Uri($"fs://{Path.Combine(_testDataDirectory, fileName)}"); 
        var desc = await backend.GetFileDescription(fileUri);
        Assert.Equal(fileUri, desc.FullName);
        Assert.Equal(hash, desc.MD5);
    }

    [Theory]
    [InlineData("fs:///home/foo", "fs:///home/foo/bar", new[] { "bar" })]
    public void GetRelativeFragmentsReturnsCorrectValues(string basePath, string fullPath, string[] fragments)
    {
        var backend = GetBackend();
        var actualFragments = backend.GetRelativeFragments(new Uri(basePath), new Uri(fullPath));
        Assert.Equal(fragments, actualFragments);
    }

    [Theory]
    [InlineData("fs:///home", new[] { "some", "dir" }, "fs:///home/some/dir")]
    public void ComputePathReturnsCorrectResult(string basePath, string[] fragments, string expected)
    {
        var backend = GetBackend();
        var path = backend.ComputePath(new Uri(basePath), fragments);
        Assert.Equal(new Uri(expected), path);
    }



    [Theory]
    [InlineData("1.txt", "file")]
    [InlineData("2.txt", "some")]
    public async Task OpenReadReturnsCorrectStream(string name, string content)
    {
        var backend = GetBackend();
        using (var f = await backend.OpenRead(new Uri($"fs://{Path.Combine(_testDataDirectory, name)}")))
        using (TextReader tr = new StreamReader(f))
        {
            var actualContent = await tr.ReadToEndAsync();
            Assert.Equal(content, actualContent);
        }
    }

    [Fact]
    public void OpenReadThrowsIfSchemeIsNotValid()
    {
        var backend = GetBackend();
        Assert.Throws<NotSupportedException>(() =>
        {
            using (var f = backend.OpenRead(new Uri("https://example.com")))
            {

            }
        });
    }

    [Fact]
    public void SchemeIsCorrect()
    {
        var backend = GetBackend();
        Assert.Equal("fs", backend.Scheme);
    }

    [Theory]
    [InlineData(new string[] { "1.txt", "2.txt" }, new string[] { "8C7DD922AD47494FC02C388E12C00EAC", "03D59E663C1AF9AC33A9949D1193505A" })]
    public async Task ListReturnsCorrectValues(string[] names, string[] hashes)
    {
        var backend = GetBackend();
        var items = await backend.List(new Uri($"fs://{_testDataDirectory}"));

        var combined = new List<FileDescription>();
        for (var i = 0; i < names.Length; i++)
        {
            combined.Add(new FileDescription
            {
                FullName = new Uri($"fs://{Path.Combine(_testDataDirectory, names[i])}"),
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

    private IBackend GetBackend()
    {
        return new FsBackend(GetLogger());
    }

    private ILogger<FsBackend> GetLogger()
    {
        return new Mock<ILogger<FsBackend>>().Object;
    }
}