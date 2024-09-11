using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

public interface ISymbolProvider
{
    SymbolInformationOrDocumentSymbolContainer? GetSymbolsForDocument(string fileName);
    void AddOrUpdateDocumentSymbols(string fileName, DocumentSymbol document);
}