using archie.Backends;
using archie.io;
using Microsoft.Extensions.Logging;
using Moq;
using units;
using Xunit;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Xunit.Abstractions;
using System;

namespace archie.Commands;

public class ComputeCommandTests
{
    private readonly ITestOutputHelper _helper;

    public ComputeCommandTests(ITestOutputHelper helper)
    {
        _helper = helper;
    }

    [Fact]
    public void ComputeCommandCreatesSuccess()
    {
        Assert.NotNull(GetComputeCommand());
    }

    [Theory]
    [InlineData("vfs", "dbfs")]
    public async Task HandleComputeWorksAsExpected(string firstScheme, string secondScheme)
    {
        var bfMoq = new Mock<IBackendFactory>();
        var objFormatterMoq = new Mock<IObjectFormatter>();
        objFormatterMoq.Setup(_ => _.FormatObject(It.IsAny<object>())).Returns((object o) => JsonConvert.SerializeObject(o));
        var streamsMoq = new Mock<IStreams>();
        var streamsHelper = new InputOutputWrapper(_helper);
        streamsMoq.Setup(_ => _.Stderror).Returns(streamsHelper.Stderror);
        streamsMoq.Setup(_ => _.Stdout).Returns(streamsHelper.Stdout);
        var firstBackend = new VirtualBackend(firstScheme);
        var secondBackend = new VirtualBackend(secondScheme);
        bfMoq.Setup(_ => _.GetByScheme(It.Is<string>(__ => __ == firstScheme))).Returns(firstBackend);
        bfMoq.Setup(_ => _.GetByScheme(It.Is<string>(__ => __ == secondScheme))).Returns(secondBackend);
        var cmd = new ComputeCommand(new VirtualLogger<ComputeCommand>(streamsHelper), bfMoq.Object, objFormatterMoq.Object, streamsMoq.Object);
        var firstRoot = firstBackend.GenerateRandomUri();
        var secondRoot = secondBackend.GenerateRandomUri();
        var result = await cmd.HandleCompute(firstRoot, secondRoot, null);
        Assert.True(streamsHelper.VirtualStdout.OutBuffer.Count > 0);
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("vfs", "dbfs", new[] { "1.txt", "2.txt", "3.txt" }, new[] { "2.txt", "3.txt" }, new[] { "1.txt" })]
    [InlineData("vfs", "dbfs", new[] { "1.txt", "2.txt" }, new[] { "1.txt", "2.txt" }, new string[] { })]
    [InlineData("vfs", "dbfs", new[] { "1.txt" }, new[] { "1.txt", "2.txt", "3.txt" }, new string[] { })]
    public async Task GetOnlyInFirstListReturnsCorrectList(
        string firstScheme,
        string secondScheme,
        string[] firstNames,
        string[] secondNames,
        string[] expected)
    {
        var bfMoq = new Mock<IBackendFactory>();
        var objFormatterMoq = new Mock<IObjectFormatter>();
        var streamsMoq = new Mock<IStreams>();
        var cmd = new ComputeCommand(new VirtualLogger<ComputeCommand>(streamsMoq.Object), bfMoq.Object, objFormatterMoq.Object, streamsMoq.Object);
        var firstBackend = new VirtualBackend(firstScheme);
        var secondBackend = new VirtualBackend(secondScheme);
        var firstRoot = firstBackend.GenerateRandomUri();
        var secondRoot = secondBackend.GenerateRandomUri();
        var firstList = await firstBackend.GenerateEntities(firstRoot, firstNames);
        var secondList = await secondBackend.GenerateEntities(secondRoot, secondNames);
        var expectedListFirst = await firstBackend.GenerateEntities(firstRoot, expected);
        var expectedListSecond = await secondBackend.GenerateEntities(secondRoot, expected);

        var result = cmd.GetOnlyInFirstList(firstList, secondList, firstBackend, firstRoot, secondBackend, secondRoot).ToList();
        Assert.Equal(expected.Length, result.Count());
        for (var i = 0; i < expected.Length; i++)
        {
            var parts = firstBackend.GetRelativeFragments(firstRoot, result[i].FullName);
            Assert.Equal(expected[i], parts[0]);
        }
    }

    [Theory]
    [InlineData(
        "vfs",
        "dbfs",
        new[] { "1.txt", "2.txt", "3.txt" },
        new[] { "a", "b", "c" },
        new[] { "2.txt", "3.txt" },
        new[] { "e", "d" },
        new[] { "2.txt", "3.txt" })]
    [InlineData(
        "vfs",
        "dbfs",
        new[] { "1.txt", "2.txt", "3.txt" },
        new[] { "a", "b", "c" },
        new[] { "1.txt", "2.txt", "3.txt" },
        new[] { "a", "b", "d" },
        new[] { "3.txt" })]
    public void GetBothListReturnsCorrectList(
        string firstScheme,
        string secondScheme,
        string[] firstNames,
        string[] firstHashes,
        string[] secondNames,
        string[] secondHashes,
        string[] expectedBoth)
    {
        var bfMoq = new Mock<IBackendFactory>();
        var objFormatterMoq = new Mock<IObjectFormatter>();
        var streamsMoq = new Mock<IStreams>();
        var cmd = new ComputeCommand(new VirtualLogger<ComputeCommand>(streamsMoq.Object), bfMoq.Object, objFormatterMoq.Object, streamsMoq.Object);
        var firstBackend = new VirtualBackend(firstScheme);
        var secondBackend = new VirtualBackend(secondScheme);
        var firstRoot = firstBackend.GenerateRandomUri();
        var secondRoot = secondBackend.GenerateRandomUri();
        var firstList = firstBackend.FromPairList(firstRoot, firstNames, firstHashes);
        var secondList = secondBackend.FromPairList(secondRoot, secondNames, secondHashes);
        var result = cmd.GetOnBothLists(firstList, secondList, firstBackend, firstRoot, secondBackend, secondRoot).ToList();
        Assert.Equal(expectedBoth.Length, result.Count());
        for (var i = 0; i < expectedBoth.Length; i++)
        {
            var relativePartsFirst = firstBackend.GetRelativeFragments(firstRoot, result[i].First.FullName);
            var relativePartsSecond = secondBackend.GetRelativeFragments(secondRoot, result[i].Second.FullName);
            Assert.Equal(relativePartsFirst, relativePartsSecond);
            Assert.NotEqual(result[i].First.MD5, result[i].Second.MD5);
            Assert.Equal(expectedBoth[i], relativePartsFirst[0]);
            Assert.Equal(expectedBoth[i], relativePartsSecond[0]);
        }
    }

    private ComputeCommand GetComputeCommand()
    {
        var bfMoq = new Mock<IBackendFactory>();
        var objFormatterMoq = new Mock<IObjectFormatter>();
        var streamsMoq = new Mock<IStreams>();
        var cmd = new ComputeCommand(new VirtualLogger<ComputeCommand>(streamsMoq.Object), bfMoq.Object, objFormatterMoq.Object, streamsMoq.Object);
        return cmd;
    }

}