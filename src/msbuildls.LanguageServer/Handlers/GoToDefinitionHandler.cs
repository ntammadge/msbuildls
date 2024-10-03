using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Handlers;

public class GoToDefinitionHandler : DefinitionHandlerBase
{
    private readonly ILogger<GoToDefinitionHandler> _logger;

    public GoToDefinitionHandler(ILogger<GoToDefinitionHandler> logger)
    {
        _logger = logger;
    }

    public override Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing 'go to definition' in file {file} at line {line} character {character}", request.TextDocument.Uri.Path, request.Position.Line, request.Position.Character);
        throw new System.NotImplementedException();
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
