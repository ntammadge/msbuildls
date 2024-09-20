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

public class TextDocumentHandlerTests
{
    [Fact]
    public async Task OpenDocumentAddsSymbolsToProviderAsync()
    {
        var request = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\SomeFile.targets")
            }
        };
        var factorySymbols = new Project();

        var nullLogger = NullLogger<TextDocumentHandler>.Instance;

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()))
            .Returns(factorySymbols);

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()))
            .Callback<string, Project>((fileName, fileSymbols) =>
            {
                Assert.Equal(request.TextDocument.Uri, fileName);
                Assert.Equal(factorySymbols, fileSymbols);
            });

        var handler = new TextDocumentHandler(
            nullLogger,
            mockSymbolFactory.Object,
            mockSymbolProvider.Object
            );



        await handler.Handle(request, new CancellationToken());

        mockSymbolFactory.Verify(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()), Times.Once);
        mockSymbolProvider.Verify(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()), Times.Once);
    }

    [Fact]
    public async Task OpenDocumentDoesNotAddNullEntryIfParsingErrorAsync()
    {
        var nullLogger = NullLogger<TextDocumentHandler>.Instance;
        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()))
            .Returns((Project?)null);
        var mockSymbolProvider = new Mock<ISymbolProvider>();

        var handler = new TextDocumentHandler(
            nullLogger,
            mockSymbolFactory.Object,
            mockSymbolProvider.Object
            );

        var request = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\DoesNotExist.targets")
            }
        };

        await handler.Handle(request, new CancellationToken());

        mockSymbolFactory.Verify(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()), Times.Once);
        mockSymbolProvider.Verify(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()), Times.Never);
    }
}