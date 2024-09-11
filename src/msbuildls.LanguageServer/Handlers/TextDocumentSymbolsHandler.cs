using System.Threading;
using System.Threading.Tasks;
using msbuildls.LanguageServer.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Handlers;

internal class TextDocumentSymbolsHandler : DocumentSymbolHandlerBase
{
    private readonly ISymbolProvider _symbolProvider;

    public TextDocumentSymbolsHandler(ISymbolProvider symbolProvider)
    {
        _symbolProvider = symbolProvider;
    }
    public override Task<SymbolInformationOrDocumentSymbolContainer?> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_symbolProvider.GetSymbolsForDocument(request.TextDocument.Uri.Path));
    }

    protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentSymbolRegistrationOptions()
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
