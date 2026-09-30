using gumonji.Server.Handlers.Backd;
using Microsoft.Extensions.DependencyInjection;

namespace gumonji.Server;

public static class BackdServiceCollectionExtensions
{
    public static IServiceCollection AddBackdProtocol(this IServiceCollection services)
    {
        services.AddSingleton<BackdState>();
        foreach (var type in typeof(IBackdHandler).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(IBackdHandler).IsAssignableFrom(type))
                continue;
            services.AddSingleton(typeof(IBackdHandler), type);
        }
        services.AddSingleton<BackdProtocol>();
        return services;
    }
}
