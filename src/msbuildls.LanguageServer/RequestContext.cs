using System;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace msbuildls.LanguageServer;

internal class RequestContext
{
    public ILspServices LspServices;
    public ILspLogger Logger;

    public RequestContext(ILspServices lspServices, ILspLogger logger)
    {
        LspServices = lspServices;
        Logger = logger;
    }

    public T GetRequiredService<T>() where T : class
    {
        return LspServices.GetRequiredService<T>();
    }
}