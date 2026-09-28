namespace gumonji.Network.Packets.Femsg;

/// <summary>Frontend's notification that the player chose their existing profile.</summary>
public sealed class ProfileRequest(uint userId) : IIncomingPacket<ProfileRequest>
{
    public uint UserId { get; } = userId;

    public static ProfileRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 4)
            throw new InvalidDataException("invalid PROFILE_REQUEST size");
        return new ProfileRequest(new PacketReader(data).ReadUInt32());
    }
}
