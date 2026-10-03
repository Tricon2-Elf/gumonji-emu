namespace gumonji.Network.Packets.Zone;

public sealed class LogoutRequest : IIncomingPacket<LogoutRequest>
{
    public static LogoutRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (!data.IsEmpty) throw new InvalidDataException("invalid LOGOUT size");
        return new();
    }
}
