namespace gumonji.Network.Packets.Game;

/// <summary>Client action emote (jump, sit, etc.): u32 action id, then u8 sequence.</summary>
public sealed class ActionEmoteRequest(uint actionId, byte sequence)
    : IIncomingPacket<ActionEmoteRequest>
{
    public uint ActionId { get; } = actionId;
    public byte Sequence { get; } = sequence;

    public static ActionEmoteRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var actionId = reader.ReadUInt32();
        var sequence = reader.ReadByte();
        reader.ExpectEnd();
        return new ActionEmoteRequest(actionId, sequence);
    }
}
