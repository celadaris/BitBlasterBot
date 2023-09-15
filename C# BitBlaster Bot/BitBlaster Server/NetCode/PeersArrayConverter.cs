using ENet;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;


//https://social.msdn.microsoft.com/Forums/vstudio/en-US/db279cfb-7817-4d75-98ab-dc883ae347fa/struct-array-to-intptr-and-intptr-to-struct-array-c?forum=csharpgeneral
public class PeersArrayConverter
{
    public static PacketConfig ConvertPeersArrayToPtr(PacketConfig packetConfig, [Optional] Peer[] peerArray)
    {
        if (peerArray != null)
        {
            int numberOfExcludedPeers = peerArray.Length;

            IntPtr peerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Peer)) * numberOfExcludedPeers);
            long LongPtr = peerPtr.ToInt64(); // Must work both on x86 and x64
            for (int i = 0; i < peerArray.Length; i++)
            {
                IntPtr tempPeerPtr = new IntPtr(LongPtr);
                Marshal.StructureToPtr(peerArray[i], tempPeerPtr, false);
                LongPtr += Marshal.SizeOf(typeof(Peer));
            }

            PacketConfig packetConfigCopy = new PacketConfig
            {
                packetFlag = packetConfig.packetFlag,
                numberOfExcludedPeers = numberOfExcludedPeers,
                excludedPeersPtr = peerPtr,
                excludedPeersLongPtr = LongPtr,
            };

            return packetConfigCopy;
        }
        else
        {
            return packetConfig;
        }

    }

    public static Peer[] ConvertPtrToPeersArray(PacketConfig packetConfig)
    {
        if (packetConfig.numberOfExcludedPeers > 0)
        {
            List<Peer> excludedPeersList = new List<Peer>();

            IntPtr ptr = packetConfig.excludedPeersPtr;
            long LongPtr = packetConfig.excludedPeersLongPtr;
            int arraySize = packetConfig.numberOfExcludedPeers;

            for (int i = 0; i < arraySize; i++)
            {
                LongPtr -= Marshal.SizeOf(typeof(Peer));

                IntPtr RectPtr = new IntPtr(LongPtr);

                //convert the ptr to struct of point
                Peer str = new Peer();
                str = (Peer)Marshal.PtrToStructure(RectPtr, str.GetType());
                excludedPeersList.Add(str);
            }
            Marshal.FreeHGlobal(ptr);

            return excludedPeersList.ToArray();
        }
        else
        {
            Peer[] peerArray = new Peer[0];
            return peerArray;
        }
    }
}