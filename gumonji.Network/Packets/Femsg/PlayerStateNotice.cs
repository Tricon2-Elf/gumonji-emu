namespace gumonji.Network.Packets.Femsg;

public sealed class PlayerStateNotice(uint mode) : IIncomingPacket<PlayerStateNotice>
{
    public uint Mode { get; } = mode;

    public static PlayerStateNotice FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 4)
            throw new InvalidDataException("invalid PLAYER_STATE size");
        return new PlayerStateNotice(new PacketReader(data).ReadUInt32());
    }
}
