using System;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace msbuildls.LanguageServer;

internal class CapabilitiesManager : IInitializeManager<InitializeParams, InitializeResult>
{
    private InitializeParams? _initializeParams;

    public void SetInitializeParams(InitializeParams request)
    {
        _initializeParams = request;
    }

    public InitializeResult GetInitializeResult()
    {
        var serverCapabilities = new ServerCapabilities()
        {
            SemanticTokensOptions = new SemanticTokensOptions
            {
                Range = true,
            },
        };

        var initializeResult = new InitializeResult
        {
            Capabilities = serverCapabilities,
        };

        return initializeResult;
    }

    public InitializeParams GetInitializeParams()
    {
        if (_initializeParams is null)
        {
            throw new ArgumentNullException(nameof(_initializeParams));
        }

        return _initializeParams;
    }
}