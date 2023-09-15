using ENet;
using System;

public struct PacketConfig
{
    public PacketFlags packetFlag { get; set; }
    public int packetSize { get; set; }

    public int numberOfExcludedPeers { get; set; }
    public IntPtr excludedPeersPtr { get; set; }
    public long excludedPeersLongPtr { get; set; }
}