using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using archie.Backends;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace units;

public class FsBackendTests
{
    /*
    public Task<IEnumerable<FileDescription>> List(Uri path);
    public Task<FileDescription> GetFileDescription(Uri path);

    Stream OpenRead(Uri entry);
    Stream OpenWrite(Uri uri);
    Uri ComputePath(Uri basePath, string[] fragments);
    string[] GetRelativeFragments(Uri basePath, Uri fullName);
    */

    [Fact]
    public void SchemeIsCorrect()
    {
        var backend = new FsBackend(GetLogger());
        Assert.Equal("fs", backend.Scheme);
    }

    [Fact]
    public async Task ListReturnsCorrectValues() {
        var backend = new FsBackend(GetLogger());
        var path = new Uri($"fs://{Path.Combine(this.GetType().Assembly.Location, "test-data/")}");
        var items = await backend.List(path);
        Assert.Equal(2, items.Count());
    }

    private ILogger<FsBackend> GetLogger()
    {
        return new Mock<ILogger<FsBackend>>().Object;
    }
}