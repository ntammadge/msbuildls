using msbuildls.LanguageServer.Symbols.MSBuild;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

public interface ISymbolFactory
{
    SymbolInformationOrDocumentSymbolContainer SymbolsForFile(Project fileSymbols);
    /// <summary>
    /// Parses a file from a request
    /// </summary>
    /// <param name="textDocument">The request's file representation</param>
    /// <returns>The deserialized file contents</returns>
    Project? ParseDocument(TextDocumentItem textDocument);
    /// <summary>
    /// Parses a file on disk from a given path
    /// </summary>
    /// <param name="filePath">A fully qualified path to the file to parse</param>
    /// <returns>The deserialized file contents</returns>
    Project? ParseFile(string filePath);
}