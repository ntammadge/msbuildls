using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace msbuildls.LanguageServer;

internal class RequestContextFactory : AbstractRequestContextFactory<RequestContext>
{
    private readonly ILspServices _lspServices;

    public RequestContextFactory(ILspServices lspServices)
    {
        _lspServices = lspServices;
    }

    public override Task<RequestContext> CreateRequestContextAsync<TRequestParam>(IQueueItem<RequestContext> queueItem, IMethodHandler methodHandler, TRequestParam requestParam, CancellationToken cancellationToken)
    {
        var logger = _lspServices.GetRequiredService<ILspLogger>();

        var requestContext = new RequestContext(_lspServices, logger);

        return Task.FromResult(requestContext);
    }
}