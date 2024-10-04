using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using msbuildls.LanguageServer.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Handlers;

public class GoToDefinitionHandler : DefinitionHandlerBase
{
    private readonly ILogger<GoToDefinitionHandler> _logger;
    private readonly ISymbolResolver _symbolResolver;

    public GoToDefinitionHandler(ILogger<GoToDefinitionHandler> logger, ISymbolResolver symbolResolver)
    {
        _logger = logger;
        _symbolResolver = symbolResolver;
    }

    public override Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        var filePath = request.TextDocument.Uri.ToUri().LocalPath;
        _logger.LogInformation("Executing 'go to definition' in file {filePath} at line {line} character {character}", filePath, request.Position.Line, request.Position.Character);

        var symbolAtLocation = _symbolResolver.ResolveSymbolAtLocation(filePath, request.Position);
        if (symbolAtLocation is null)
        {
            return Task.FromResult<LocationOrLocationLinks?>(null);
        }

        var symbolDefinition = _symbolResolver.ResolveDefinitionForSymbol(symbolAtLocation, filePath);

        var locationLink = LocationOrLocationLinks.From(new []
        {
            new LocationOrLocationLink(new Location()
            {
                Uri = request.TextDocument.Uri,
                Range = new Range(
                    symbolDefinition.Range.Start.Line,
                    symbolDefinition.Range.Start.Character,
                    symbolDefinition.Range.End.Line,
                    symbolDefinition.Range.End.Character)
            })
        });
        return Task.FromResult<LocationOrLocationLinks?>(locationLink);
    }

    protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DefinitionRegistrationOptions()
        {
            DocumentSelector = new TextDocumentSelector(
                new TextDocumentFilter()
                {
                    Pattern = "{**/*.props,**/*.targets,**/*.*proj}"
                }
            )
        };
    }
}
