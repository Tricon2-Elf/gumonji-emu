using gumonji.Common.Handlers.Zone;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace gumonji.Common;

public static class PacketServiceCollectionExtensions
{
    public static IServiceCollection AddPacketHandlers(this IServiceCollection services)
    {
        services.TryAddSingleton<BackdState>();
        foreach (var type in typeof(IPacketHandler).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(IPacketHandler).IsAssignableFrom(type) ||
                type == typeof(ChunkSubscribeHandler)) continue;
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IPacketHandler), type));
        }

        foreach (var opcode in new[]
        {
            PacketType.ChunkSubscribe0Bcc,
            PacketType.ChunkSubscribe1Fa4,
            PacketType.ChunkSubscribe200C,
            PacketType.ChunkSubscribe203A,
            PacketType.ChunkSubscribe206C,
        })
            services.AddSingleton<IPacketHandler>(new ChunkSubscribeHandler(opcode));

        services.TryAddSingleton<PacketDispatcher>();
        return services;
    }
}
