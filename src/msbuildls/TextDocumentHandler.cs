using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace msbuildls;

internal class TextDocumentHandler : TextDocumentSyncHandlerBase
{
    private readonly TextDocumentSelector _textDocumentSelector = new TextDocumentSelector(
        new TextDocumentFilter()
        {
            Pattern = "{**/*.props,**/*.targets,**/*.*proj}"
        }
    );
    private readonly ILogger<TextDocumentHandler> _logger;

    public TextDocumentHandler(ILogger<TextDocumentHandler> logger)
    {
        _logger = logger;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new TextDocumentAttributes(uri, "xml");
    }

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Opened file: {filePath}", request.TextDocument.Uri.Path);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Closed file: {filePath}", request.TextDocument.Uri.Path);
        return Unit.Task;
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSyncRegistrationOptions()
        {
            DocumentSelector = _textDocumentSelector,
            Change = TextDocumentSyncKind.Incremental,
            Save = new SaveOptions()
            {
                IncludeText = false
            }
        };
    }
}