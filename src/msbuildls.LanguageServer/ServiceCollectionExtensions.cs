using Microsoft.Extensions.DependencyInjection;
using msbuildls.LanguageServer.Symbols;

namespace msbuildls.LanguageServer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSymbolResolutionServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<ISymbolFactory, SymbolFactory>();
    }
}