using System;
using System.Collections.Generic;
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
        var fileSymbols = _symbolProvider.GetFileSymbols(fileScope);
        if (fileSymbols == null)
        {
            _logger.LogInformation("No symbols available in {fileScope} to resolve definition", fileScope);
            return null;
        }

        // Imports and properties are evaluated in order by appearance in the file
        // If a property appears before an import which contains an assignment to the same property, the position before the import is the definition.
        // If a property appears after an import which contains an assignment to the same property, the position in the imported file is the definition
        // TODO: Add range data to imports in order to order their appearances with properties accordingly

        // Prioritize project-level references over target-level references
        var propertyReferences = fileSymbols.PropertyGroups?.SelectMany(propGroup => propGroup.Properties?.Where(property => property.Name == (deserializedSymbol as Property).Name) ?? []) ?? [];
        if (!propertyReferences.Any())
        {
            propertyReferences = fileSymbols.Targets?.SelectMany(target => target.PropertyGroups?.SelectMany(propGroup => propGroup.Properties?.Where(property => property.Name == (deserializedSymbol as Property).Name) ?? []) ?? []);
        }
        var propDefinition = propertyReferences?.OrderBy(property => property.Range.Start.Line).First();

        return new Location()
        {
            Uri = new Uri(fileScope),
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                propDefinition.Range.Start.Line,
                propDefinition.Range.Start.Character,
                propDefinition.Range.End.Line,
                propDefinition.Range.End.Character)
        };
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
