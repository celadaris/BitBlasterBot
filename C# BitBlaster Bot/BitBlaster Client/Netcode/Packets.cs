
[RecvPacket(packetID = 1)]
public struct GeneralControlsPacket
{
    public ushort packetID { get; set; }
    public ushort arraySize { get; set; }
    public bool[] turn { get; set; }
    public bool[] speed { get; set; }
    public bool[] isFiring { get; set; }
}

[RecvPacket(packetID = 2)]
public struct EnterPacket
{
    public ushort packetID { get; set; }
    public bool enterKey { get; set; }
}

[SendPacket(packetID = 3)]
public struct VideoPacket
{
    public ushort packetID { get; set; }
    public ushort arraySize { get; set; }
    public byte[] videoFeed { get; set; }
}

[SendPacket(packetID = 4)]
public struct MemoryPacket
{
    public ushort packetID { get; set; }
    public byte life { get; set; }
    public int score { get; set; }
}