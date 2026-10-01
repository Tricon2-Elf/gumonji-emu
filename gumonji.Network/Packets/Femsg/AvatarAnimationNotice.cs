namespace gumonji.Network.Packets.Femsg;

/// <summary>One-way frontend notice from client sender sub_40D930 (opcode 420).</summary>
public sealed record AvatarAnimationNotice(uint AnimationId) : IIncomingPacket<AvatarAnimationNotice>
{
    public static AvatarAnimationNotice FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var id = reader.ReadUInt32();
        reader.ExpectEnd();
        return new(id);
    }
}
