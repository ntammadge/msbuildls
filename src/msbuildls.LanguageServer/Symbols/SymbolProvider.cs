using System.Collections.Concurrent;
using msbuildls.LanguageServer.Symbols.MSBuild;

namespace msbuildls.LanguageServer.Symbols;

internal class SymbolProvider : ISymbolProvider
{
    private readonly ConcurrentDictionary<string, Project> _fileSymbolStore;

    public SymbolProvider()
    {
        _fileSymbolStore = new ConcurrentDictionary<string, Project>();
    }

    public void AddOrUpdateSymbols(string fileName, Project fileSymbols)
    {
        _fileSymbolStore.AddOrUpdate(fileName, fileSymbols, (name, oldFileSymbols) => fileSymbols);
    }

    public Project? GetFileSymbols(string fileName)
    {
        if (_fileSymbolStore.TryGetValue(fileName, out var fileSymbols))
        {
            return fileSymbols;
        }
        return null;
    }
}
