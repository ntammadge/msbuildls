using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using msbuildls.LanguageServer.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Handlers;

internal class TextDocumentSymbolsHandler : DocumentSymbolHandlerBase
{
    private readonly ILogger<TextDocumentSymbolsHandler> _logger;
    private readonly ISymbolFactory _symbolFactory;
    private readonly ISymbolProvider _symbolProvider;

    public TextDocumentSymbolsHandler(
        ILogger<TextDocumentSymbolsHandler> logger,
        ISymbolFactory symbolFactory,
        ISymbolProvider symbolProvider
    )
    {
        _logger = logger;
        _symbolFactory = symbolFactory;
        _symbolProvider = symbolProvider;
    }
    public override Task<SymbolInformationOrDocumentSymbolContainer?> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling request for symbols from {documentPath}", request.TextDocument.Uri.Path);

        var projectSymbols = _symbolProvider.GetFileSymbols(request.TextDocument.Uri.Path);
        SymbolInformationOrDocumentSymbolContainer? symbols = null;

        if (projectSymbols != null)
        {
            _logger.LogInformation("Found symbol data in file");
            symbols = _symbolFactory.SymbolsForFile(projectSymbols);
            symbols = FilterDuplicateSymbols(symbols);
        }
        else
        {
            _logger.LogInformation("No symbols found in file");
        }

        return Task.FromResult(symbols);
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

    private SymbolInformationOrDocumentSymbolContainer? FilterDuplicateSymbols(SymbolInformationOrDocumentSymbolContainer? rawSymbols)
    {
        if (rawSymbols is null)
        {
            return null;
        }
        // This works for now with project-scoped properties. Will need updates for targets.
        // Property "original definition" is the first use of the property.
        // Target "definition" for execution is the last definition of the target
        var hashedSymbols = rawSymbols.DistinctBy(symbol => symbol.DocumentSymbol?.Name);
        return SymbolInformationOrDocumentSymbolContainer.From(hashedSymbols);
    }
}
