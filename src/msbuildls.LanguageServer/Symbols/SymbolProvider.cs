using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

internal class SymbolProvider : ISymbolProvider
{
    private readonly ConcurrentDictionary<string, DocumentSymbol> _documentSymbolStore;

    public SymbolProvider()
    {
        _documentSymbolStore = new ConcurrentDictionary<string, DocumentSymbol>();
    }

    public void AddOrUpdateDocumentSymbols(string fileName, DocumentSymbol document)
    {
        _documentSymbolStore.AddOrUpdate(fileName, document, (name, oldDocument) => document);
    }

    public SymbolInformationOrDocumentSymbolContainer? GetSymbolsForDocument(string fileName)
    {
        if (_documentSymbolStore.TryGetValue(fileName, out var documentSymbol))
        {
            return new SymbolInformationOrDocumentSymbolContainer(documentSymbol);
        }
        return null;
    }
}
