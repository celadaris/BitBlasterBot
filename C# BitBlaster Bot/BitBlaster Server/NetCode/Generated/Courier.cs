using ENet;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class Courier : MonoBehaviour
{
    [SerializeField] LogicScript logicScript;

    public void SendGeneralControlsPacket(PacketConfig packetConfig, GeneralControlsPacket generalControlsPacket, [Optional] Peer[] excludedPeers)
    {
        PacketConfig addedExcludedPeers;

        if (excludedPeers != null)
        {
            addedExcludedPeers = PeersArrayConverter.ConvertPeersArrayToPtr(packetConfig, excludedPeers);
        }
        else
        {
            addedExcludedPeers = PeersArrayConverter.ConvertPeersArrayToPtr(packetConfig);
        }

        IntPtr configIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(addedExcludedPeers));
        Marshal.StructureToPtr(addedExcludedPeers, configIntPtr, false);

        GeneralControlsPacketContainer generalControlsPacketContainer = new GeneralControlsPacketContainer
        {
                packetConfig = configIntPtr,
                generalControlsPacket = generalControlsPacket,
        };

        logicScript.logicSendGeneralControlsPacketBuffer.Enqueue(generalControlsPacketContainer);
    }

    public void SendEnterPacket(PacketConfig packetConfig, EnterPacket enterPacket, [Optional] Peer[] excludedPeers)
    {
        PacketConfig addedExcludedPeers;

        if (excludedPeers != null)
        {
            addedExcludedPeers = PeersArrayConverter.ConvertPeersArrayToPtr(packetConfig, excludedPeers);
        }
        else
        {
            addedExcludedPeers = PeersArrayConverter.ConvertPeersArrayToPtr(packetConfig);
        }

        IntPtr configIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(addedExcludedPeers));
        Marshal.StructureToPtr(addedExcludedPeers, configIntPtr, false);

        EnterPacketContainer enterPacketContainer = new EnterPacketContainer
        {
                packetConfig = configIntPtr,
                enterPacket = enterPacket,
        };

        logicScript.logicSendEnterPacketBuffer.Enqueue(enterPacketContainer);
    }

}