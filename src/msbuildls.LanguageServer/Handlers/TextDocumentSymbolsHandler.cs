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

        var knownSymbols = rawSymbols
            .Where(symbol => symbol.DocumentSymbol!.Kind == (SymbolKind)MSBuildSymbolKind.Property)
            .GroupBy(symbol => new SymbolKey(symbol.DocumentSymbol!.Name, symbol.DocumentSymbol.Kind))
            .ToDictionary(group => group.Key, group => group.OrderBy(symbol => symbol.DocumentSymbol!.Range.Start).First());

        // Flatten targets and nested symbols
        foreach (var target in rawSymbols.Where(symbol => symbol.DocumentSymbol!.Kind == (SymbolKind)MSBuildSymbolKind.Target))
        {
            var targetKey = new SymbolKey(target.DocumentSymbol!.Name, target.DocumentSymbol.Kind);
            // Don't bother with redefinitions of targets for now.
            // Technically the last definition of a target after evaluation is the one that gets executed if the target is called, which complicates the decision making about which nested symbols are populated.
            if (knownSymbols.ContainsKey(targetKey))
            {
                continue;
            }

            // Don't need to add nested symbols if they don't exist
            if (target.DocumentSymbol.Children is null)
            {
                knownSymbols.Add(targetKey, target);
                continue;
            }

            // Have to remake the symbol because the properties are init-only sets and we need to flatten the symbol hierarchy (no child symbols)
            var newTarget = new DocumentSymbol()
            {
                Name = target.DocumentSymbol.Name,
                Kind = target.DocumentSymbol.Kind,
                Range = target.DocumentSymbol.Range,
                SelectionRange = target.DocumentSymbol.SelectionRange
            };
            knownSymbols.Add(targetKey, newTarget);

            var nestedSymbols = target.DocumentSymbol.Children
                .Where(nestedSymbol => nestedSymbol.Kind == (SymbolKind)MSBuildSymbolKind.Property)
                .GroupBy(nestedSymbol => new SymbolKey(nestedSymbol.Name, nestedSymbol.Kind));

            // Add the nested symbol to the known symbols collection if it's unknown
            foreach (var symbolGroup in nestedSymbols)
            {
                if (knownSymbols.ContainsKey(symbolGroup.Key))
                {
                    continue;
                }

                var nestedSymbol = symbolGroup.OrderBy(symbol => symbol.Range.Start).First();

                knownSymbols.Add(symbolGroup.Key, nestedSymbol);
            }
        }

        return SymbolInformationOrDocumentSymbolContainer.From(knownSymbols.Values);
    }

    private record SymbolKey(string Name, SymbolKind Kind);
}
