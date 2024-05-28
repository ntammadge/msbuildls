using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace msbuildls.LanguageServer;

[LanguageServerEndpoint("initialize", LanguageServerConstants.DefaultLanguageName)]
internal class InitializeEndpoint : IRequestHandler<InitializeParams, InitializeResult, RequestContext>
{
    public bool MutatesSolutionState => throw new System.NotImplementedException();

    public Task<InitializeResult> HandleRequestAsync(InitializeParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var capabilitiesManager = requestContext.GetRequiredService<IInitializeManager<InitializeParams, InitializeResult>>();

        capabilitiesManager.SetInitializeParams(request);
        var serverCapabilities = capabilitiesManager.GetInitializeResult();

        return Task.FromResult(serverCapabilities);
    }
}