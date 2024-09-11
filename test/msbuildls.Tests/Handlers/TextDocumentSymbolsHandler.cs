using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using msbuildls.LanguageServer.Handlers;
using msbuildls.LanguageServer.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace msbuildls.Tests.Handlers;

public class TextDocumentSymbolsHandlerTests
{
    [Fact]
    public async Task SymbolsFromSymbolProviderAsync()
    {
        var docSymbolParams = new DocumentSymbolParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\test.targets")
            }
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetSymbolsForDocument(It.IsAny<string>()))
            .Returns(default(SymbolInformationOrDocumentSymbolContainer))
            .Callback<string>(docPath => Assert.Equal(docSymbolParams.TextDocument.Uri.Path, docPath));

        var handler = new TextDocumentSymbolsHandler(mockSymbolProvider.Object);

        await handler.Handle(docSymbolParams, new CancellationToken());

        mockSymbolProvider.Verify(provider => provider.GetSymbolsForDocument(It.IsAny<string>()), Times.Once());
    }
}