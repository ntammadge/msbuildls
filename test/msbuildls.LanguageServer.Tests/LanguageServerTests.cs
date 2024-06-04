using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using StreamJsonRpc;

namespace msbuildls.LanguageServer.Tests;

public class LanguageServerTests
{
    [Fact]
    public void AllHandlersRegistered()
    {
        var jsonRpc = new JsonRpc(Console.OpenStandardOutput(), Console.OpenStandardInput());
        var server = new MSBuildLanguageServer(jsonRpc, new JsonSerializer(), new LspLogger());

        var registeredHandlers = server.GetLspServices().GetRequiredServices<IMethodHandler>().Select(s => s.GetType()).ToHashSet();
        var allHandlers = typeof(MSBuildLanguageServer).Assembly.GetTypes().Where(s => s is IMethodHandler && !s.IsAbstract && !s.IsInterface);

        Assert.All(allHandlers, (handler) =>
        {
            if (!registeredHandlers.Contains(handler))
            {
                Assert.Fail($"Handler {handler} is not registered");
            }
        });
    }

    [Fact]
    public async Task CanInitializeAsync()
    {
        var jsonRpc = new JsonRpc(Console.OpenStandardOutput(), Console.OpenStandardInput());
        var server = new MSBuildLanguageServer(jsonRpc, new JsonSerializer(), new LspLogger());

        var initializeParams = new InitializeParams
        {
            Capabilities = new()
        };

        // Simply completing the request validates we can
        await server.GetTestAccessor().ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, LanguageServerConstants.DefaultLanguageName, initializeParams, new CancellationToken());
    }
}
