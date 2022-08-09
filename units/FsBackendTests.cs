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
    /*
    public Task<FileDescription> GetFileDescription(Uri path);

    Stream OpenRead(Uri entry);
    Stream OpenWrite(Uri uri);
    Uri ComputePath(Uri basePath, string[] fragments);
    string[] GetRelativeFragments(Uri basePath, Uri fullName);
    */

    readonly string _testDataDirectory;
    readonly string _testWriteDirectory;

    public FsBackendTests()
    {
        var fi = new FileInfo(this.GetType().Assembly.Location).DirectoryName;
        _testDataDirectory = Path.Combine(fi!, "test-data/read");
        _testWriteDirectory = Path.Combine(fi!, "test-data");
    }



    [Theory]
    [InlineData("1.txt", "file")]
    [InlineData("2.txt", "some")]
    public async Task OpenReadReturnsCorrectStream(string name, string content)
    {
        var backend = new FsBackend(GetLogger());
        using(var f = backend.OpenRead(new Uri($"fs://{Path.Combine(_testDataDirectory, name)}")))
        using(TextReader tr = new StreamReader(f)) {
            var actualContent = await tr.ReadToEndAsync();
            Assert.Equal(content, actualContent);
        }
    }

    [Fact]
    public void SchemeIsCorrect()
    {
        var backend = new FsBackend(GetLogger());
        Assert.Equal("fs", backend.Scheme);
    }

    [Theory]
    [InlineData(new string[] { "1.txt", "2.txt" }, new string[] { "8C7DD922AD47494FC02C388E12C00EAC", "03D59E663C1AF9AC33A9949D1193505A" })]
    public async Task ListReturnsCorrectValues(string[] names, string[] hashes)
    {
        var backend = new FsBackend(GetLogger());
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

    private ILogger<FsBackend> GetLogger()
    {
        return new Mock<ILogger<FsBackend>>().Object;
    }
}