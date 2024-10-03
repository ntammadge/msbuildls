using msbuildls.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Server;

namespace msbuildls.LanguageServer.Extensions;

public static class LanguageServerOptionsExtensions
{
    public static LanguageServerOptions AddTextDocumentHandlers(this LanguageServerOptions options)
    {
        return options
            .WithHandler<TextDocumentHandler>()
            .WithHandler<TextDocumentSymbolsHandler>();
    }

    public static LanguageServerOptions AddCommandHandlers(this LanguageServerOptions options)
    {
        return options
            .WithHandler<GoToDefinitionHandler>();
    }
}