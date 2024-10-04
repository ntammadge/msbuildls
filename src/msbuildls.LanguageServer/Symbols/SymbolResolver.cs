using System;
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

    public Location? ResolveDefinitionForSymbol(IdentifiableElement deserializedSymbol, string fileScope)
    {
        _logger.LogInformation("Resolving the definition for symbol");
        var prop = deserializedSymbol as Property;

        var fileSymbols = _symbolProvider.GetFileSymbols(fileScope);
        if (fileSymbols == null)
        {
            _logger.LogInformation("No symbols available in {fileScope} to resolve definition", fileScope);
            return null;
        }

        var propertyReferences = fileSymbols.PropertyGroups?.SelectMany(propGroup => propGroup.Properties?.Where(property => property.Name == prop.Name) ?? []);
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

    public IdentifiableElement? ResolveSymbolAtLocation(string filePath, Position position)
    {
        _logger.LogInformation("Beginning resolution of symbol at location");

        var fileSymbols = _symbolProvider.GetFileSymbols(filePath);

        if (fileSymbols is null)
        {
            _logger.LogInformation("No symbols available to resolve for file");
            return null;
        }

        var properties = fileSymbols.PropertyGroups?.SelectMany(propGroup => propGroup?.Properties ?? []);
        var resolvedProperty = properties?.FirstOrDefault(property => position.IsIn(property.Range));
        _logger.LogInformation("Resolved symbol at cursor");
        return resolvedProperty;
    }
}
