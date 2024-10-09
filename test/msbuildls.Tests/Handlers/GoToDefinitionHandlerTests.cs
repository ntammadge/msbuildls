using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using msbuildls.LanguageServer.Handlers;
using msbuildls.LanguageServer.Symbols;
using msbuildls.LanguageServer.Symbols.MSBuild;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace msbuildls.Tests.Handlers;

public class GoToDefinitionHandlerTests
{
    [Fact]
    public async Task NoLocationIfNoSymbolAtLocationAsync()
    {
        var expectedFilePath = @"C:\Does\Not\Exist.targets";
        var expectedPosition = new Position();
        var mockSymbolResolver = new Mock<ISymbolResolver>();
        mockSymbolResolver
            .Setup(resolver => resolver.ResolveSymbolAtLocation(It.IsAny<string>(), It.IsAny<Position>()))
            .Callback<string, Position>((filePath, position) =>
            {
                Assert.Equal(expectedFilePath, filePath);
                Assert.Equal(expectedPosition, position);
            })
            .Returns(null as IElementWithRange);

        var handler = new GoToDefinitionHandler(NullLogger<GoToDefinitionHandler>.Instance, mockSymbolResolver.Object);

        var request = new DefinitionParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                Uri = new Uri(expectedFilePath),
            },
            Position = expectedPosition
        };
        var result = await handler.Handle(request, new CancellationToken());

        mockSymbolResolver
            .Verify(
                resolver => resolver.ResolveSymbolAtLocation(It.IsAny<string>(), It.IsAny<Position>()),
                Times.Once);
        Assert.Null(result);
    }

    [Fact]
    public async Task ReturnsLocationForSymbolDefinedInSameFileAsync()
    {
        var expectedFilePath = @"C:\Does\Not\Exist.targets";
        var expectedPosition = new Position();
        var expectedSymbol = new Property();
        var expectedLocation = new Location()
        {
            Uri = new Uri(expectedFilePath),
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(3, 10, 3, 50)
        };

        var mockSymbolResolver = new Mock<ISymbolResolver>();
        mockSymbolResolver
            .Setup(resolver => resolver.ResolveSymbolAtLocation(It.IsAny<string>(), It.IsAny<Position>()))
            .Callback<string, Position>((filePath, position) =>
            {
                Assert.Equal(expectedFilePath, filePath);
                Assert.Equal(expectedPosition, position);
            })
            .Returns(expectedSymbol);
        mockSymbolResolver
            .Setup(resolver => resolver.ResolveDefinitionForSymbol(It.IsAny<IElementWithRange>(), It.IsAny<string>()))
            .Callback<IElementWithRange, string>((symbol, filePath) =>
            {
                Assert.Equal(expectedSymbol, symbol);
                Assert.Equal(expectedFilePath, filePath);
            })
            .Returns(expectedLocation);

        var handler = new GoToDefinitionHandler(NullLogger<GoToDefinitionHandler>.Instance, mockSymbolResolver.Object);

        var request = new DefinitionParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                Uri = new Uri(expectedFilePath),
            },
            Position = expectedPosition
        };
        var result = await handler.Handle(request, new CancellationToken());

        mockSymbolResolver
            .Verify(
                resolver => resolver.ResolveSymbolAtLocation(It.IsAny<string>(), It.IsAny<Position>()),
                Times.Once);
        mockSymbolResolver
            .Verify(
                resolver => resolver.ResolveDefinitionForSymbol(It.IsAny<IElementWithRange>(), It.IsAny<string>()),
                Times.Once);
        Assert.NotNull(result);
        var resultLocation = Assert.Single(result);
        Assert.NotNull(resultLocation.Location);
        Assert.Equal(expectedLocation, resultLocation.Location);
    }
}