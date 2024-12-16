using Microsoft.Build.Evaluation;

namespace msbuildls.LanguageServer.Symbols;

public interface ISymbolProvider
{
    void AddOrUpdateSymbols(string fileName, Project file);
    Project? GetFileSymbols(string fileName);
}