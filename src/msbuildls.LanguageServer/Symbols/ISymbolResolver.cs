using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

public interface ISymbolResolver
{
    /// <summary>
    /// Finds the deserialized symbol at the current location
    /// </summary>
    /// <param name="filePath">The path of the file to search</param>
    /// <param name="position">The position in the file to find the symbol at</param>
    /// <returns>The found symbol or null if not found</returns>
    IElementWithRange? ResolveSymbolAtLocation(string filePath, Position position);
    /// <summary>
    /// Resolves the definition of a known symbol
    /// </summary>
    /// <param name="deserializedSymbol">The known symbol</param>
    /// <param name="fileScope">The scope of symbols to search</param>
    /// <returns>The found symbol definition location</returns>
    Location? ResolveDefinitionForSymbol(IElementWithRange deserializedSymbol, string fileScope);
}