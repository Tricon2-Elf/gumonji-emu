namespace gumonji.Network.Packets.Game;

public sealed class ClockRequest : IIncomingPacket<ClockRequest>
{
    public static ClockRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("invalid CLOCK_REQUEST size");
        return new ClockRequest();
    }
}
