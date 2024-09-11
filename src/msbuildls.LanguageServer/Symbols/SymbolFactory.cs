using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

internal static class KnownMsBuildNodes
{
    public const string PropertyGroup = "PropertyGroup";
    public const string ItemGroup = "ItemGroup";
    public const string Project = "Project";
    public const string Target = "Target";
}

/// <summary>
/// Provides a mapping from MsBuild elements to symbol types
/// </summary>
internal enum MsBuildSymbolKind
{
    Property = SymbolKind.Property,
    Project = SymbolKind.Namespace
}

internal class SymbolFactory : ISymbolFactory
{
    public DocumentSymbol MakeDocumentSymbols(XElement rootNode)
    {
        if (rootNode.Name.LocalName != KnownMsBuildNodes.Project)
        {
            // Log an error indicating we received an incorrect XML hierarchy
            // Consider return options
            return null;
        }

        var childNodes = rootNode.Elements();
        var childSymbols = new List<DocumentSymbol>();

        foreach (var node in childNodes)
        {
            if (node.Name.LocalName == KnownMsBuildNodes.PropertyGroup)
            {
                var propertyNodes = node.Elements();
                childSymbols.AddRange(propertyNodes.Select(propertyNode => MakePropertyFromNode(propertyNode)));
            }
        }

        var range = MakeSymbolRange(rootNode);
        return new DocumentSymbol()
        {
            Name = rootNode.Name.LocalName,
            Children = new Container<DocumentSymbol>(childSymbols),
            Kind = (SymbolKind)MsBuildSymbolKind.Project,
            Range = range,
            SelectionRange = range
        };
    }

    public DocumentSymbol MakePropertyFromNode(XElement propertyNode)
    {
        var range = MakeSymbolRange(propertyNode);
        return new DocumentSymbol()
        {
            Name = propertyNode.Name.LocalName,
            Range = range,
            SelectionRange = range,
            Kind =  (SymbolKind)MsBuildSymbolKind.Property
        };
    }

    private Range MakeSymbolRange(XElement node)
    {
        var lineInfo = (IXmlLineInfo)node;
        var offset = -1; // VSCode LSP client 0-indexes position values. If multiple client support is added, this will have to update if those clients have 1-indexed positions

        return new Range(
            new Position(lineInfo.LineNumber + offset, lineInfo.LinePosition + offset),
            new Position(lineInfo.LineNumber + offset, lineInfo.LinePosition + node.Name.LocalName.Length + offset)
        );
    }
}