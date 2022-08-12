using archie.Backends;
using archie.io;
using Microsoft.Extensions.Logging;
using Moq;
using units;
using Xunit;
using System.Threading.Tasks;
using System.Linq;

namespace archie.Commands;

public class ComputeCommandTests
{

    [Fact]
    public void ComputeCommandCreatesSuccess()
    {

        Assert.NotNull(GetComputeCommand());
    }

    [Theory]
    [InlineData(new[] { "1.txt", "2.txt", "3.txt" }, new[] { "2.txt", "3.txt" }, new[] { "1.txt" })]
    [InlineData(new[] { "1.txt", "2.txt" }, new[] { "1.txt", "2.txt" }, new string[] { })]
    [InlineData(new[] { "1.txt" }, new[] { "1.txt", "2.txt", "3.txt" }, new string[] { })]
    public async Task GetOnlyInFirstListReturnsCorrectList(string[] firstNames, string[] secondNames, string[] expected)
    {
        var loggerMoq = new Mock<ILogger<ComputeCommand>>();
        var bfMoq = new Mock<IBackendFactory>();
        var objFormatterMoq = new Mock<IObjectFormatter>();
        var streamsMoq = new Mock<IStreams>();
        var cmd = new ComputeCommand(loggerMoq.Object, bfMoq.Object, objFormatterMoq.Object, streamsMoq.Object);
        var backend = new VirtualBackend();
        var firstRoot = backend.GenerateRandomUri();
        var secondRoot = backend.GenerateRandomUri();
        var firstList = await backend.GenerateEntities(firstRoot, firstNames);
        var expectedList = await backend.GenerateEntities(firstRoot, expected);
        var secondList = await backend.GenerateEntities(secondRoot, secondNames);
        var result = cmd.GetOnlyInFirstList(firstList, secondList, backend, firstRoot, backend, secondRoot);
        Assert.Equal(expectedList, result, new FileDescriptionEqualityComparer());
    }

    [Theory]
    [InlineData(
        new[] { "1.txt", "2.txt", "3.txt" },
        new[] { "a", "b", "c" },
        new[] { "2.txt", "3.txt" },
        new[] { "e", "d" },
        new[] { "2.txt", "3.txt" })]
    [InlineData(
        new[] { "1.txt", "2.txt", "3.txt" },
        new[] { "a", "b", "c" },
        new[] { "1.txt", "2.txt", "3.txt" },
        new[] { "a", "b", "d" },
        new[] { "3.txt" })]
    public void GetBothListReturnsCorrectList(
        string[] firstNames,
        string[] firstHashes,
        string[] secondNames,
        string[] secondHashes,
        string[] expectedBoth)
    {
        var loggerMoq = new Mock<ILogger<ComputeCommand>>();
        var bfMoq = new Mock<IBackendFactory>();
        var objFormatterMoq = new Mock<IObjectFormatter>();
        var streamsMoq = new Mock<IStreams>();
        var cmd = new ComputeCommand(loggerMoq.Object, bfMoq.Object, objFormatterMoq.Object, streamsMoq.Object);
        var backend = new VirtualBackend();
        var firstRoot = backend.GenerateRandomUri();
        var secondRoot = backend.GenerateRandomUri();
        var firstList = backend.FromPairList(firstRoot, firstNames, firstHashes);
        var secondList = backend.FromPairList(secondRoot, secondNames, secondHashes);
        var result = cmd.GetOnBothLists(firstList, secondList, backend, firstRoot, backend, secondRoot).ToList();
        Assert.Equal(expectedBoth.Length, result.Count());
        for (var i = 0; i < expectedBoth.Length; i++)
        {
            var relativePartsFirst = backend.GetRelativeFragments(firstRoot, result[i].First.FullName);
            var relativePartsSecond = backend.GetRelativeFragments(secondRoot, result[i].Second.FullName);
            Assert.Equal(relativePartsFirst, relativePartsSecond);
            Assert.NotEqual(result[i].First.MD5, result[i].Second.MD5);
            Assert.Equal(expectedBoth[i], relativePartsFirst[0]);
            Assert.Equal(expectedBoth[i], relativePartsSecond[0]);
        }
    }

    private ComputeCommand GetComputeCommand()
    {
        var loggerMoq = new Mock<ILogger<ComputeCommand>>();
        var bfMoq = new Mock<IBackendFactory>();
        var objFormatterMoq = new Mock<IObjectFormatter>();
        var streamsMoq = new Mock<IStreams>();
        var cmd = new ComputeCommand(loggerMoq.Object, bfMoq.Object, objFormatterMoq.Object, streamsMoq.Object);
        return cmd;
    }
}