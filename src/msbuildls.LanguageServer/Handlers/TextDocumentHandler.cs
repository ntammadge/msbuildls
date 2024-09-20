using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using msbuildls.LanguageServer.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace msbuildls.LanguageServer.Handlers;

internal class TextDocumentHandler : TextDocumentSyncHandlerBase
{
    private readonly TextDocumentSelector _textDocumentSelector = new TextDocumentSelector(
        new TextDocumentFilter()
        {
            Pattern = "{**/*.props,**/*.targets,**/*.*proj}"
        }
    );
    private readonly ILogger<TextDocumentHandler> _logger;
    private readonly ISymbolFactory _symbolFactory;
    private readonly ISymbolProvider _symbolProvider;

    public TextDocumentHandler(
        ILogger<TextDocumentHandler> logger,
        ISymbolFactory symbolFactory,
        ISymbolProvider symbolProvider
        )
    {
        _logger = logger;
        _symbolFactory = symbolFactory;
        _symbolProvider = symbolProvider;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new TextDocumentAttributes(uri, "xml");
    }

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Opened file: {filePath}", request.TextDocument.Uri.Path);

        var docSymbols = _symbolFactory.ParseDocument(request.TextDocument);
        if (docSymbols != null)
        {
            _symbolProvider.AddOrUpdateSymbols(request.TextDocument.Uri.Path, docSymbols);
        }

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
            Save = new OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities.SaveOptions()
            {
                IncludeText = false,
            }
        };
    }
}