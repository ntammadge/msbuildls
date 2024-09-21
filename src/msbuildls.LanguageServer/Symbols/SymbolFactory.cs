using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using msbuildls.LanguageServer.Symbols.MSBuild;
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
    private readonly int _symbolOffset = -1;
    private readonly ILogger<ISymbolFactory> _logger;

    public SymbolFactory(ILogger<ISymbolFactory> logger)
    {
        _logger = logger;
    }

    public Project? ParseDocument(TextDocumentItem textDocumentItem)
    {
        var documentPath = textDocumentItem.Uri.Path;
        _logger.LogInformation("Beginning deserialization of {documentPath}", documentPath);

        var serializer = new XmlSerializer(typeof(Project));

        using var reader = new StringReader(textDocumentItem.Text);
        Project? project = null;

        try
        {
            project = (Project?)serializer.Deserialize(reader);
            _logger.LogInformation("Completed deserialization of {documentPath}", documentPath);
        }
        catch(Exception e)
        {
            // Should the inner exception be used?
            _logger.LogError("Unable to deserialize {documentPath}. Exception encountered: {exception}", documentPath, e.Message);
        }

        return project;
    }

    public SymbolInformationOrDocumentSymbolContainer SymbolsForFile(Project file)
    {
        var documentSymbols = new List<DocumentSymbol>();

        var propertySymbols = GetPropertySymbols(file.PropertyGroups);
        documentSymbols.AddRange(propertySymbols);

        var symbols = documentSymbols.Select(symbol => SymbolInformationOrDocumentSymbol.Create(symbol));
        var symbolContainer = SymbolInformationOrDocumentSymbolContainer.From(symbols);
        return symbolContainer;
    }

    private IEnumerable<DocumentSymbol> GetPropertySymbols(IEnumerable<PropertyGroup>? propertyGroups)
    {
        return propertyGroups
            ?.SelectMany(propGroup => propGroup.Properties
                ?.Select(property =>
                {
                    var startPos = new Position(property.StartLine + _symbolOffset, property.StartChar + _symbolOffset);
                    var endPos = new Position(property.EndLine + _symbolOffset, property.EndChar + _symbolOffset);

                    return new DocumentSymbol()
                    {
                        Name = property.Name,
                        Kind = (SymbolKind)MsBuildSymbolKind.Property,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(startPos, endPos),
                        SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(startPos.Line, startPos.Character, startPos.Line, startPos.Character + property.Name.Length)
                    };
                }) ?? []) ?? [];
    }
}