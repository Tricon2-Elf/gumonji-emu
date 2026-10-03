using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using gumonji.Network;
using gumonji.Network.Crypto;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gumonji.Common.Tests;

public sealed class ZoneTransportLifecycleTests
{
    [Fact]
    public async Task ShutdownWaitsForAsynchronousSessionCleanup()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var stopping = new CancellationTokenSource();
        var attached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleaning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var endpoint = Endpoint();
        var listener = new VceListener(NullLogger.Instance, "test", ServerKind.Zone, endpoint,
            _ => { attached.SetResult(); return new object(); }, (_, _, _, _) => Task.CompletedTask,
            onDisconnectAsync: async _ => { cleaning.SetResult(); await release.Task; });
        var running = listener.RunAsync(stopping.Token);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(endpoint, timeout.Token);
            using var cipher = await Handshake(client.GetStream(), timeout.Token);
            await attached.Task.WaitAsync(timeout.Token);
            stopping.Cancel();
            await cleaning.Task.WaitAsync(timeout.Token);
            Assert.False(running.IsCompleted);
        }
        finally
        {
            stopping.Cancel(); release.TrySetResult();
            await running.WaitAsync(timeout.Token);
        }
    }

    [Fact]
    public async Task ClosingConnectionStopsLaterPacketsInTheSameRecord()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var stopping = new CancellationTokenSource();
        var cleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var packets = new List<PacketType>();
        var endpoint = Endpoint();
        var listener = new VceListener(NullLogger.Instance, "test", ServerKind.Zone, endpoint, _ => new object(),
            async (connection, opcode, _, _) => { packets.Add(opcode); await connection.RequestCloseAsync(); },
            onDisconnectAsync: _ => { cleanup.SetResult(); return Task.CompletedTask; });
        var running = listener.RunAsync(stopping.Token);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(endpoint, timeout.Token);
            using var cipher = await Handshake(client.GetStream(), timeout.Token);
            var writer = new PacketWriter();
            writer.Write(4u); writer.Write((uint)PacketType.LogoutRequest);
            writer.Write(4u); writer.Write((uint)PacketType.TakeoffRequest);
            await VceRecords.WriteAsync(client.GetStream(), cipher, writer.ToBytes(), true, timeout.Token);
            await cleanup.Task.WaitAsync(timeout.Token);
            Assert.Equal(new[] { PacketType.LogoutRequest }, packets);
        }
        finally { stopping.Cancel(); await running.WaitAsync(timeout.Token); }
    }

    private static IPEndPoint Endpoint()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var endpoint = (IPEndPoint)probe.LocalEndpoint;
        probe.Stop();
        return endpoint;
    }

    private static async Task<Aes128Ecb> Handshake(Stream stream, CancellationToken ct)
    {
        var hello = new PacketWriter(); hello.Write(1u); hello.Write(16u);
        await stream.WriteAsync(hello.ToBytes(), ct);
        var status = await VceRecords.ReadExactAsync(stream, 4, ct);
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32BigEndian(status));
        var generator = LegacyDh.ParseUnsignedHex(await Field(stream, ct));
        var prime = LegacyDh.ParseUnsignedHex(await Field(stream, ct));
        var serverKey = LegacyDh.ParseUnsignedHex(await Field(stream, ct));
        var privateKey = LegacyDh.RandomPrivate();
        var publicKey = BigInteger.ModPow(generator, privateKey, prime);
        var encoded = Encoding.ASCII.GetBytes(LegacyDh.BnHex(publicKey));
        var writer = new PacketWriter(); writer.Write((uint)encoded.Length); writer.Write(encoded);
        await stream.WriteAsync(writer.ToBytes(), ct);
        return new(LegacyDh.DeriveAesKey(BigInteger.ModPow(serverKey, privateKey, prime)));
    }
    private static async Task<string> Field(Stream stream, CancellationToken ct)
    {
        var size = BinaryPrimitives.ReadInt32BigEndian(await VceRecords.ReadExactAsync(stream, 4, ct));
        return Encoding.ASCII.GetString(await VceRecords.ReadExactAsync(stream, size, ct));
    }
}
