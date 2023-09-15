using ENet;
using System;
using System.Runtime.InteropServices;

public class Courier
{
    static LogicScript localLogicScript;

    public Courier(LogicScript logicScript)
    {
        localLogicScript = logicScript;
    }

    public static void SendVideoPacket(PacketConfig packetConfig, VideoPacket videoPacket)
    {
        IntPtr configIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(packetConfig));
        Marshal.StructureToPtr(packetConfig, configIntPtr, false);

        VideoPacketContainer videoPacketContainer = new VideoPacketContainer
        {
            packetConfig = configIntPtr,
            videoPacket = videoPacket,
        };

        localLogicScript.logicSendVideoPacketBuffer.Enqueue(videoPacketContainer);
    }
    public static void SendMemoryPacket(PacketConfig packetConfig, MemoryPacket memoryPacket)
    {
        IntPtr configIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(packetConfig));
        Marshal.StructureToPtr(packetConfig, configIntPtr, false);

        MemoryPacketContainer memoryPacketContainer = new MemoryPacketContainer
        {
            packetConfig = configIntPtr,
            memoryPacket = memoryPacket,
        };

        localLogicScript.logicSendMemoryPacketBuffer.Enqueue(memoryPacketContainer);
    }

}