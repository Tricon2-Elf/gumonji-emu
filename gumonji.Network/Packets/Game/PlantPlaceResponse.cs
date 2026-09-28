namespace gumonji.Network.Packets.Game;

public sealed class PlantPlaceResponse(
    uint objectId,
    byte subtype,
    byte color,
    uint fertility,
    ushort x,
    ushort y) : IOutgoingPacket
{
    public PacketType Type => PacketType.PlantPlaceResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objectId);
        writer.Write((byte)3);
        writer.Write(subtype);
        writer.Write(color);
        writer.Write((byte)0);
        writer.Write(fertility);
        writer.Write(fertility);
        writer.Write(1u);
        writer.Write(0u);
        writer.Write((byte)4);
        writer.Write((byte)0);
        writer.Write(x);
        writer.Write(y);
        writer.Write((byte)0);
        writer.Write(0u);
        writer.WriteCompactBytes([]);
        return writer.ToBytes();
    }
}
