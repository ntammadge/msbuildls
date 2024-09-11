using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using msbuildls.LanguageServer.Handlers;
using msbuildls.LanguageServer.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace msbuildls.Tests.Handlers;

public class TextDocumentHandlerTests
{
    [Fact]
    public async Task OpenDocumentAddsSymbolsToProviderAsync()
    {
        var symbolName = "Project";
        var openDocParams = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Text = "<Project></Project>",
                Uri = new Uri(@"C:\test.targets")
            }
        };
        var expectedXmlNode = XElement.Parse(openDocParams.TextDocument.Text, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.MakeDocumentSymbols(It.IsAny<XElement>()))
            .Returns(new DocumentSymbol() { Name = symbolName })
            .Callback<XElement>((inputNode) =>
            {
                Assert.NotNull(inputNode);
                Assert.Equal(expectedXmlNode.Name.LocalName, inputNode.Name.LocalName);
            });
        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.AddOrUpdateDocumentSymbols(It.IsAny<string>(), It.IsAny<DocumentSymbol>()))
            .Callback<string, DocumentSymbol>((docName, docSymbol) =>
            {
                Assert.NotNull(docSymbol);
                Assert.Equal(openDocParams.TextDocument.Uri.Path, docName);
                Assert.Equal(symbolName, docSymbol.Name);
            });

        var textDocumentHandler = new TextDocumentHandler(
            NullLogger<TextDocumentHandler>.Instance,
            mockSymbolFactory.Object,
            mockSymbolProvider.Object
        );

        await textDocumentHandler.Handle(openDocParams, new CancellationToken());

        mockSymbolFactory.Verify(factory => factory.MakeDocumentSymbols(It.IsAny<XElement>()), Times.Once());
        mockSymbolProvider.Verify(provider => provider.AddOrUpdateDocumentSymbols(It.IsAny<string>(), It.IsAny<DocumentSymbol>()), Times.Once());
    }
}