using msbuildls.LanguageServer.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace msbuildls.Tests.Symbols;

public class SymbolProviderTests
{
    [Fact]
    public void CanGetSymbolsForFile()
    {
        var symbolProvider = new SymbolProvider();
        var docName = "test.targets";
        var expectedSymbol = new DocumentSymbol()
        {
            Name = "TestSymbol"
        };

        symbolProvider.AddOrUpdateDocumentSymbols(docName, expectedSymbol);
        var docSymbols = symbolProvider.GetSymbolsForDocument(docName);

        Assert.NotNull(docSymbols);
        var docSymbol = Assert.Single(docSymbols);
        Assert.Equal(expectedSymbol.Name, docSymbol?.DocumentSymbol?.Name);
    }

    [Fact]
    public void CanGetUpdatedSymbolsForFile()
    {
        var symbolProvider = new SymbolProvider();
        var docName = "test.targets";
        var firstSymbol = new DocumentSymbol()
        {
            Name = "TestSymbol"
        };
        var expectedSymbol = new DocumentSymbol()
        {
            Name = "TestSymbol2"
        };

        symbolProvider.AddOrUpdateDocumentSymbols(docName, firstSymbol);
        symbolProvider.AddOrUpdateDocumentSymbols(docName, expectedSymbol);

        var docSymbols = symbolProvider.GetSymbolsForDocument(docName);

        Assert.NotNull(docSymbols);
        var docSymbol = Assert.Single(docSymbols);
        Assert.Equal(expectedSymbol.Name, docSymbol?.DocumentSymbol?.Name);
    }

    [Fact]
    public void NullSymbolForFileNotFound()
    {
        var symbolProvider = new SymbolProvider();

        var docSymbols = symbolProvider.GetSymbolsForDocument("DoesNotExist.targets");

        Assert.Null(docSymbols);
    }
}