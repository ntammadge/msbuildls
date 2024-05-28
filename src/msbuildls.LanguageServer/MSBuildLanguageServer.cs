using StreamJsonRpc;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<IInitializeManager<InitializeParams, InitializeResult>, CapabilitiesManager>();
        services.AddSingleton<IMethodHandler, InitializeEndpoint>();
        services.AddSingleton(this);

        var lspServices = new LspServices(services);
        return lspServices;
    }
}