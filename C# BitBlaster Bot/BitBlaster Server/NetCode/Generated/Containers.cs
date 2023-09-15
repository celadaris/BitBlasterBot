
using System;

public struct VideoPacketContainer
{
    public IntPtr packetConfig { get; set; }
    public VideoPacket videoPacket { get; set; }
}
public struct MemoryPacketContainer
{
    public IntPtr packetConfig { get; set; }
    public MemoryPacket memoryPacket { get; set; }
}
public struct GeneralControlsPacketContainer
{
    public IntPtr packetConfig { get; set; }
    public GeneralControlsPacket generalControlsPacket { get; set; }
}
public struct EnterPacketContainer
{
    public IntPtr packetConfig { get; set; }
    public EnterPacket enterPacket { get; set; }
}