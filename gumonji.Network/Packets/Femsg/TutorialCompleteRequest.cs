namespace gumonji.Network.Packets.Femsg;

public sealed class TutorialCompleteRequest(uint userId, uint tutorialId)
    : IIncomingPacket<TutorialCompleteRequest>
{
    public uint UserId { get; } = userId;
    public uint TutorialId { get; } = tutorialId;

    public static TutorialCompleteRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 8)
            throw new InvalidDataException("invalid TUTORIAL_COMPLETE size");
        var reader = new PacketReader(data);
        return new TutorialCompleteRequest(reader.ReadUInt32(), reader.ReadUInt32());
    }
}
