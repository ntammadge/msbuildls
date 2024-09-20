using msbuildls.LanguageServer.Symbols.MSBuild;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

public interface ISymbolFactory
{
    SymbolInformationOrDocumentSymbolContainer SymbolsForFile(Project fileSymbols);
    Project? ParseDocument(TextDocumentItem textDocument);
}