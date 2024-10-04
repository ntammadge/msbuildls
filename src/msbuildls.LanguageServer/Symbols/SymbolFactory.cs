using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using msbuildls.LanguageServer.Symbols.MSBuild;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

/// <summary>
/// Provides a mapping from MSBuild elements to symbol types
/// </summary>
internal enum MSBuildSymbolKind
{
    Property = SymbolKind.Property,
    Target = SymbolKind.Function
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
        documentSymbols.AddRange(GetPropertySymbols(file.PropertyGroups));

        var targetSymbols = GetTargetSymbols(file.Targets);
        documentSymbols.AddRange(GetTargetSymbols(file.Targets));

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
                    return new DocumentSymbol()
                    {
                        Name = property.Name,
                        Kind = (SymbolKind)MSBuildSymbolKind.Property,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property.Range.Start.Line, property.Range.Start.Character, property.Range.End.Line, property.Range.End.Character),
                        SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property.Range.Start.Line, property.Range.Start.Character, property.Range.Start.Line, property.Range.Start.Character + property.Name.Length)
                    };
                }) ?? []) ?? [];
    }

    private IEnumerable<DocumentSymbol> GetTargetSymbols(IEnumerable<Target>? targets)
    {
        return targets
            ?.Select(target =>
            {
                var startPos = new Position(target.StartPosition.Line + _symbolOffset, target.StartPosition.Character + _symbolOffset);
                var endPos = new Position(target.EndPosition.Line + _symbolOffset, target.EndPosition.Character + _symbolOffset);

                var nestedSymbols = new List<DocumentSymbol>();

                var propertySymbols = GetPropertySymbols(target.PropertyGroups);
                nestedSymbols.AddRange(propertySymbols);

                return new DocumentSymbol()
                {
                    Name = target.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Target,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(startPos, endPos),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(startPos, startPos), // Set selection range to the start of the element because the name attribute may not be the first attribute
                    Children = nestedSymbols.Any() ? nestedSymbols : null
                };
            }) ?? [];
    }
}