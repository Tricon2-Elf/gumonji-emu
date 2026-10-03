namespace gumonji.Network.Packets.Zone;

public sealed class RenderTaskRequest : IIncomingPacket<RenderTaskRequest>
{
    public static RenderTaskRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (!data.IsEmpty) throw new InvalidDataException("invalid GET_RENDER_TASK size");
        return new();
    }
}
