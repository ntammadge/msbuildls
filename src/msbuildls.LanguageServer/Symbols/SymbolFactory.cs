using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Extensions.Logging;
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

    private const string BeginningDeserializationMessage = "Beginning deserialization of {filePath}";

    public SymbolFactory(ILogger<ISymbolFactory> logger)
    {
        _logger = logger;
    }

    public Project? ParseDocument(TextDocumentItem textDocumentItem)
    {
        var filePath = textDocumentItem.Uri.ToUri().LocalPath;
        _logger.LogInformation(BeginningDeserializationMessage, filePath);

        var project = DeserializeText(textDocumentItem.Text);

        if (project != null)
        {
            project.FullPath = filePath;
        }

        return project;
    }

    public Project? ParseFile(string filePath)
    {
        _logger.LogInformation(BeginningDeserializationMessage, filePath);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var text = File.ReadAllText(filePath);
        var project = DeserializeText(text);

        if (project != null)
        {
            project.FullPath = filePath;
        }

        return project;
    }

    private Project? DeserializeText(string text)
    {
        var readerSettings = new XmlReaderSettings()
        {
            IgnoreComments = false,
            IgnoreWhitespace = false
        };
        using var reader = XmlReader.Create(new StringReader(text), readerSettings);

        Project? project = null;

        try
        {
            project = Project.FromXmlReader(reader, new ProjectOptions());
            _logger.LogInformation("Deserialization completed successfully");
        }
        catch (Exception e)
        {
            _logger.LogError("Deserialization failed. Encountered an error: {exception}", e.Message);
        }

        return project;
    }

    public SymbolInformationOrDocumentSymbolContainer SymbolsForFile(Project file)
    {
        var documentSymbols = new List<DocumentSymbol>();

        documentSymbols.AddRange(GetPropertySymbols(file.Properties));
        documentSymbols.AddRange(GetTargetSymbols(file.Targets.Values));

        var symbols = documentSymbols.Select(symbol => SymbolInformationOrDocumentSymbol.Create(symbol));
        var symbolContainer = SymbolInformationOrDocumentSymbolContainer.From(symbols);
        return symbolContainer;
    }

    private IEnumerable<DocumentSymbol> GetPropertySymbols(IEnumerable<ProjectProperty> properties)
    {
        return properties
            .Select(property =>
            {
                return new DocumentSymbol()
                {
                    Name = property.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Property,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property.Xml.Location.Line, property.Xml.Location.Column, property.Xml.Location.Line, property.Xml.Location.Column),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property.Xml.Location.Line, property.Xml.Location.Column, property.Xml.Location.Line, property.Xml.Location.Column)
                };
            });
    }

    private IEnumerable<DocumentSymbol> GetTargetSymbols(IEnumerable<ProjectTargetInstance> targets)
    {
        return targets
            .Select(target =>
            {
                return new DocumentSymbol()
                {
                    Name = target.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Target,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.Location.Line, target.Location.Column, target.Location.Line, target.Location.Column),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.Location.Line, target.Location.Column, target.Location.Line, target.Location.Column),
                };
            });
    }
}