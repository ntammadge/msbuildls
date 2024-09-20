using msbuildls.LanguageServer.Symbols;
using msbuildls.LanguageServer.Symbols.MSBuild;
using Xunit;

namespace msbuildls.Tests.Symbols;

public class SymbolProviderTests
{
    [Fact]
    public void CanGetSymbolsForFile()
    {
        var symbolProvider = new SymbolProvider();
        var docName = "test.targets";
        var factorySymbols = new Project();

        symbolProvider.AddOrUpdateSymbols(docName, factorySymbols);
        var providedFileSymbols = symbolProvider.GetFileSymbols(docName);

        Assert.NotNull(providedFileSymbols);
        Assert.Equal(factorySymbols, providedFileSymbols);
    }

    [Fact]
    public void CanGetUpdatedSymbolsForFile()
    {
        var symbolProvider = new SymbolProvider();
        var docName = "test.targets";
        var firstFileSymbols = new Project();
        var secondFileSymbols = new Project()
        {
            PropertyGroups = []
        };

        symbolProvider.AddOrUpdateSymbols(docName, firstFileSymbols);
        symbolProvider.AddOrUpdateSymbols(docName, secondFileSymbols);

        var providedFileSymbols = symbolProvider.GetFileSymbols(docName);

        Assert.NotNull(providedFileSymbols);
        Assert.Equal(secondFileSymbols, providedFileSymbols);
        Assert.NotNull(providedFileSymbols.PropertyGroups);
        Assert.Equal(secondFileSymbols.PropertyGroups.Length, providedFileSymbols.PropertyGroups.Length);
    }

    [Fact]
    public void NullSymbolForFileNotFound()
    {
        var symbolProvider = new SymbolProvider();

        var fileSymbols = symbolProvider.GetFileSymbols("DoesNotExist.targets");

        Assert.Null(fileSymbols);
    }
}