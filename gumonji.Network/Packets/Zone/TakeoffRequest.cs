namespace gumonji.Network.Packets.Zone;

public sealed class TakeoffRequest : IIncomingPacket<TakeoffRequest>
{
    public static TakeoffRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (!data.IsEmpty) throw new InvalidDataException("invalid TAKEOFF size");
        return new();
    }
}
