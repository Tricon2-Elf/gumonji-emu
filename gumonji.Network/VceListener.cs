using System.Buffers.Binary;
using System.Net.Sockets;
using gumonji.Network.Crypto;
using Microsoft.Extensions.Logging;

namespace gumonji.Network;

public sealed class ClientConnection : IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly Aes128Ecb _cipher;
    private readonly bool _compression;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public ClientConnection(
        Guid id,
        string label,
        ServerKind kind,
        Stream stream,
        Aes128Ecb cipher,
        ILogger logger)
    {
        Id = id;
        Label = label;
        Kind = kind;
        _stream = stream;
        _cipher = cipher;
        _compression = kind == ServerKind.Game;
        _logger = logger;
    }

    public Guid Id { get; }
    public string Label { get; }
    public ServerKind Kind { get; }
    public object? Session { get; set; }

    public async Task SendAsync(PacketType type, ReadOnlyMemory<byte> body, CancellationToken ct = default)
    {
        var width = PacketTypeInfo.OpcodeWidth(Kind);
        var packet = new byte[width + body.Length];
        if (width == 2)
            BinaryPrimitives.WriteUInt16BigEndian(packet, (ushort)type);
        else
            BinaryPrimitives.WriteUInt32BigEndian(packet, (uint)type);
        body.Span.CopyTo(packet.AsSpan(width));

        var framed = new byte[4 + packet.Length];
        BinaryPrimitives.WriteInt32BigEndian(framed, packet.Length);
        packet.CopyTo(framed.AsSpan(4));

        await _sendLock.WaitAsync(ct);
        try
        {
            await VceRecords.WriteAsync(_stream, _cipher, framed, _compression, ct);
        }
        finally
        {
            _sendLock.Release();
        }

        _logger.LogInformation(
            "{Label} TX {Name} opcode=0x{Opcode:X} bytes={Bytes}",
            Label,
            type,
            (uint)type,
            packet.Length);
    }

    public async ValueTask DisposeAsync()
    {
        _cipher.Dispose();
        await _stream.DisposeAsync();
        _sendLock.Dispose();
    }
}

public sealed class VceListener
{
    private readonly ILogger _logger;
    private readonly string _name;
    private readonly ServerKind _kind;
    private readonly System.Net.IPEndPoint _endPoint;
    private readonly Func<ClientConnection, object> _attachSession;
    private readonly Func<ClientConnection, PacketType, ReadOnlyMemory<byte>, CancellationToken, Task> _onPacket;

    public VceListener(
        ILogger logger,
        string name,
        ServerKind kind,
        System.Net.IPEndPoint endPoint,
        Func<ClientConnection, object> attachSession,
        Func<ClientConnection, PacketType, ReadOnlyMemory<byte>, CancellationToken, Task> onPacket)
    {
        _logger = logger;
        _name = name;
        _kind = kind;
        _endPoint = endPoint;
        _attachSession = attachSession;
        _onPacket = onPacket;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var listener = new TcpListener(_endPoint);
        listener.Start();
        _logger.LogInformation(
            "listening for {Name} on {Address}:{Port} (legacy VCE DH)",
            _name,
            _endPoint.Address,
            _endPoint.Port);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcp = await listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => RunClientAsync(tcp, ct), ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task RunClientAsync(TcpClient tcp, CancellationToken ct)
    {
        var peer = tcp.Client.RemoteEndPoint;
        var label = $"{_name} peer={peer}";
        var stage = "8-byte client hello / DH exchange";
        _logger.LogInformation("{Label} connection from {Peer}", label, peer);
        ClientConnection? connection = null;
        try
        {
            var stream = tcp.GetStream();
            var cipher = await DhHandshake.AcceptAsync(stream, ct);
            _logger.LogInformation(
                "{Label} DH exchange complete; transport compression={Compression}",
                label,
                _kind == ServerKind.Game ? "vce-gzip-envelope" : "none");
            connection = new ClientConnection(Guid.NewGuid(), label, _kind, stream, cipher, _logger);
            connection.Session = _attachSession(connection);
            stage = "encrypted application records";
            var frames = new InnerFrames();
            var width = PacketTypeInfo.OpcodeWidth(_kind);
            while (!ct.IsCancellationRequested)
            {
                var chunk = await VceRecords.ReadAsync(stream, cipher, connection.Kind == ServerKind.Game, ct);
                foreach (var frame in frames.Feed(chunk))
                {
                    if (frame.Length < width)
                        throw new InvalidDataException("missing opcode");
                    var opcode = width == 2
                        ? BinaryPrimitives.ReadUInt16BigEndian(frame)
                        : BinaryPrimitives.ReadUInt32BigEndian(frame);
                    var type = (PacketType)opcode;
                    var body = frame.AsMemory(width);
                    await _onPacket(connection, type, body, ct);
                }
            }
        }
        catch (EndOfStreamException ex)
        {
            _logger.LogInformation("{Label} {Peer} disconnected during {Stage}: {Message}", label, peer, stage, ex.Message);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or TimeoutException)
        {
            _logger.LogError("{Label} {Peer} during {Stage}: {Message}", label, peer, stage, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Label} {Peer} unexpected failure during {Stage}", label, peer, stage);
        }
        finally
        {
            if (connection is not null)
                await connection.DisposeAsync();
            tcp.Dispose();
        }
    }
}
