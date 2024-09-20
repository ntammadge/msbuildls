using msbuildls.LanguageServer.Symbols.MSBuild;

namespace msbuildls.LanguageServer.Symbols;

public interface ISymbolProvider
{
    void AddOrUpdateSymbols(string fileName, Project file);
    Project? GetFileSymbols(string fileName);
}