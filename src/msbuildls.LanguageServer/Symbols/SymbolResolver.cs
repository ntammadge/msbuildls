using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using msbuildls.LanguageServer.Extensions;
using msbuildls.LanguageServer.Symbols.MSBuild;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

internal class SymbolResolver : ISymbolResolver
{
    private readonly ILogger<ISymbolResolver> _logger;
    private readonly ISymbolProvider _symbolProvider;

    public SymbolResolver(ILogger<ISymbolResolver> logger, ISymbolProvider symbolProvider)
    {
        _logger = logger;
        _symbolProvider = symbolProvider;
    }

    public Location? ResolveDefinitionForSymbol(IElementWithRange deserializedSymbol, string fileScope)
    {
        // Only properties are supported at the moment
        if (deserializedSymbol is not Property)
        {
            return null;
        }

        var fileSymbols = _symbolProvider.GetFileSymbols(fileScope);
        if (fileSymbols == null)
        {
            _logger.LogInformation("No symbols available in {fileScope} to resolve definition", fileScope);
            return null;
        }

        // Imports and properties are evaluated in order by appearance in the file
        // If a property appears before an import which contains an assignment to the same property, the position before the import is the definition.
        // If a property appears after an import which contains an assignment to the same property, the position in the imported file is the definition
        var definitionSources = new List<IElementWithRange>();
        definitionSources.AddRange(fileSymbols.PropertyGroups?.SelectMany(propGroup => propGroup.Properties ?? []) ?? []);
        definitionSources.AddRange(fileSymbols.Imports ?? []);
        definitionSources.AddRange(fileSymbols.ImportGroups?.SelectMany(importGroup => importGroup.Imports ?? []) ?? []);
        var fileScopeDirectory = new FileInfo(fileScope).Directory.FullName;

        foreach (var element in definitionSources.OrderBy(element => element.Range.Start))
        {
            // If the current element is a property, check if it has the same name
            // TODO: consider a where filter when getting properties
            if (element is Property potentialProperty && deserializedSymbol is Property deserializedProperty && deserializedProperty.Name == potentialProperty.Name)
            {
                return new Location()
                {
                    Uri = new Uri(fileScope),
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                        potentialProperty.Range.Start.Line,
                        potentialProperty.Range.Start.Character,
                        potentialProperty.Range.End.Line,
                        potentialProperty.Range.End.Character)
                };
            }
            // Check contents of import to see if a definition exists
            else if (element is Import import)
            {
                var importFileScope = import.Project;
                if (!Path.IsPathFullyQualified(importFileScope))
                {
                    importFileScope = Path.GetFullPath(Path.Combine(fileScopeDirectory, importFileScope));
                }
                // File exists check is unnecessary because we're pulling symbols from the symbol store rather than directly from the file

                var definitionFromImport = ResolveDefinitionForSymbol(deserializedSymbol, importFileScope);
                if (definitionFromImport != null)
                {
                    return definitionFromImport;
                }
            }
        }

        var targetDefinitionSources = (fileSymbols.Targets?.SelectMany(target => target.PropertyGroups?.SelectMany(propGroup => propGroup.Properties ?? []) ?? []) ?? []).OrderBy(property => property.Range.Start);
        foreach (var element in targetDefinitionSources)
        {
            if (element is Property potentialProperty && deserializedSymbol is Property deserializedProperty && deserializedProperty.Name == potentialProperty.Name)
            {
                return new Location()
                {
                    Uri = new Uri(fileScope),
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                        potentialProperty.Range.Start.Line,
                        potentialProperty.Range.Start.Character,
                        potentialProperty.Range.End.Line,
                        potentialProperty.Range.End.Character)
                };
            }
        }

        // No symbol definitions were found
        // Should never get here in the top-level scope because a reference/definition had to exist to check for definitions in the first place
        return null;
    }

    public IElementWithRange? ResolveSymbolAtLocation(string filePath, Position position)
    {
        _logger.LogInformation("Beginning resolution of symbol at location");

        var fileSymbols = _symbolProvider.GetFileSymbols(filePath);

        if (fileSymbols is null)
        {
            _logger.LogInformation("No symbols available to resolve for file");
            return null;
        }

        var resolvableSymbols = new List<IElementWithRange>();
        resolvableSymbols.AddRange(fileSymbols.PropertyGroups?.SelectMany(propGroup => propGroup?.Properties ?? []) ?? []);
        resolvableSymbols.AddRange(fileSymbols.Targets?.SelectMany(target => target.PropertyGroups?.SelectMany(propGroup => propGroup.Properties ?? []) ?? []) ?? []);

        var resolvedSymbol = resolvableSymbols.FirstOrDefault(element => position.IsIn(element.Range));
        if (resolvedSymbol == null)
        {
            _logger.LogInformation("No identifiable elements at the current position");
        }
        else
        {
            _logger.LogInformation("Resolved symbol at cursor");
        }
        return resolvedSymbol;
    }
}
