using System;
using archie.Backends;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace units;

[Collection("BackendFactoryTests")]
public class BackendFactoryTests
{
    [Fact]
    public void GetByShemeThrowsIfGarbageSupplied()
    {
        var spMoq = new Mock<IServiceProvider>();
        var loggerMoq = new Mock<ILogger<BackendFactory>>();
        var factory = new BackendFactory(loggerMoq.Object, spMoq.Object);
        Assert.Throws<NotSupportedException>(() =>
        {
            factory.GetByScheme("asdasdasdadsasd");
        });
    }

    [Theory]
    [InlineData("fs")]
    public void GetByShemeReturnsProperImplementationForRegistered(string scheme)
    {
        var loggerMoq = new Mock<ILogger<BackendFactory>>();
        var loggerFsBackMoq = new Mock<ILogger<FsBackend>>();
        var spMoq = new Mock<IServiceProvider>();
        spMoq.Setup(_ => _.GetService(It.IsAny<Type>())).Returns(loggerFsBackMoq.Object);

        var factory = new BackendFactory(loggerMoq.Object, spMoq.Object);
        var backend = factory.GetByScheme(scheme);
        Assert.Equal(scheme, backend.Scheme);

    }
}