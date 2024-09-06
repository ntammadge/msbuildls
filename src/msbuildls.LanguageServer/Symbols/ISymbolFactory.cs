using System.Xml.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

public interface ISymbolFactory
{
    /// <summary>
    /// Creates symbols for the document based on the provided root XML node (the project node).
    /// </summary>
    /// <param name="rootNode">The project node from parsing the file</param>
    /// <returns>The symbol hierarchy for the document, including referenced symbols</returns>
    DocumentSymbol MakeDocumentSymbols(XElement rootNode);
    /// <summary>
    /// Creates a property symbol from the provided XML property node
    /// </summary>
    /// <param name="propertyNode">The property node from the document</param>
    /// <returns>The property symbol</returns>
    DocumentSymbol MakePropertyFromNode(XElement propertyNode);
}