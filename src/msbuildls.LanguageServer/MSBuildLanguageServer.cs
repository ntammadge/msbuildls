using StreamJsonRpc;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.CommonLanguageServerProtocol.Framework.Handlers;

namespace msbuildls.LanguageServer;

internal class MSBuildLanguageServer : NewtonsoftLanguageServer<RequestContext>
{
    public MSBuildLanguageServer(
        JsonRpc jsonRpc,
        JsonSerializer jsonSerializer,
        ILspLogger logger)
        : base(jsonRpc, jsonSerializer, logger)
    {
        Initialize();
    }

    protected override ILspServices ConstructLspServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILspLogger>(Logger);
        services.AddSingleton<AbstractRequestContextFactory<RequestContext>, RequestContextFactory>();
        services.AddSingleton<AbstractHandlerProvider>(s => HandlerProvider);
        services.AddSingleton<IInitializeManager<InitializeParams, InitializeResult>, CapabilitiesManager>();
        services.AddSingleton<IMethodHandler, InitializeHandler<InitializeParams, InitializeResult, RequestContext>>();
        services.AddSingleton(this);

        var lspServices = new LspServices(services);
        return lspServices;
    }
}