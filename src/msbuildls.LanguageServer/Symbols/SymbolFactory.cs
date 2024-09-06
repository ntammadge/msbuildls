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

        var lineInfo = (IXmlLineInfo)rootNode;
        return new DocumentSymbol()
        {
            Name = rootNode.Name.LocalName,
            Children = new Container<DocumentSymbol>(childSymbols),
            Kind = (SymbolKind)MsBuildSymbolKind.Project,
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(lineInfo.LineNumber, lineInfo.LinePosition),
                new Position(lineInfo.LineNumber, lineInfo.LinePosition + rootNode.Name.LocalName.Length))
        };
    }

    public DocumentSymbol MakePropertyFromNode(XElement propertyNode)
    {
        var lineInfo = (IXmlLineInfo)propertyNode;
        return new DocumentSymbol()
        {
            Name = propertyNode.Name.LocalName,
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(new Position(lineInfo.LineNumber, lineInfo.LinePosition), new Position(lineInfo.LineNumber, lineInfo.LinePosition + propertyNode.Name.LocalName.Length)),
            Kind =  (SymbolKind)MsBuildSymbolKind.Property
        };
    }
}