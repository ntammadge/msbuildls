using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using msbuildls.LanguageServer.Symbols;
using msbuildls.LanguageServer.Symbols.MSBuild;
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
        var filePath = request.TextDocument.Uri.ToUri().LocalPath;
        _logger.LogInformation("Opened file: {filePath}", filePath);

        var docSymbols = _symbolFactory.ParseDocument(request.TextDocument);
        if (docSymbols == null)
        {
            return Unit.Task;
        }
        _symbolProvider.AddOrUpdateSymbols(filePath, docSymbols);

        var parsedFiles = new List<string>() { filePath };
        HandleImports(filePath, parsedFiles, docSymbols);

        return Unit.Task;
    }

    private void HandleImports(string importingFile, List<string> parsedFiles, Project fileSymbols)
    {
        var imports = new List<Import>();
        imports.AddRange(fileSymbols.Imports ?? []);
        imports.AddRange(fileSymbols.ImportGroups?.SelectMany(importGroup => importGroup.Imports ?? []) ?? []);

        if (imports.Count == 0)
        {
            _logger.LogInformation("Not imports found in file {filePath}", importingFile);
            return;
        }

        _logger.BeginScope("Imports found in {filePath}", importingFile);
        var importingFileDirectory = new FileInfo(importingFile).Directory.FullName; // TODO: figure out when this could be null

        foreach (var import in imports) // TODO: Order by appearance in the file
        {
            var importPath = import.Project;
            if (!Path.IsPathFullyQualified(importPath))
            {
                importPath = Path.GetFullPath(Path.Combine(importingFileDirectory, importPath));
            }

            if (!Path.Exists(importPath))
            {
                _logger.LogInformation("Unable to parse file data for {filePath}. The file doesn't exist", importPath);
                continue;
            }
            if (parsedFiles.Contains(importPath))
            {
                _logger.LogInformation("Skipped parsing of {filePath} because it has already been parsed", importPath);
                continue;
            }

            var importSymbols = _symbolFactory.ParseFile(importPath);
            parsedFiles.Add(importPath);
            if (importSymbols != null)
            {
                _symbolProvider.AddOrUpdateSymbols(importPath, importSymbols);
                HandleImports(importPath, parsedFiles, importSymbols);
            }
        }
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
        _logger.LogInformation("Closed file: {filePath}", request.TextDocument.Uri.ToUri().LocalPath);
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
                IncludeText = false,
            }
        };
    }
}