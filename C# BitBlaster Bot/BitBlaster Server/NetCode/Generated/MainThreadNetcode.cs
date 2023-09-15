using DisruptorUnity3d;
using System.Runtime.InteropServices;
using UnityEngine;

public class MainThreadNetcode : MonoBehaviour
{
    [SerializeField] RLScript rLScript;
    [SerializeField] ImageRender imageRender;

    public static RingBuffer<VideoPacketContainer> recvVideoPacketToGame { get; set; }
    public static RingBuffer<MemoryPacketContainer> recvMemoryPacketToGame { get; set; }

    void Awake()
    {
        recvVideoPacketToGame = new RingBuffer<VideoPacketContainer>(100);
        recvMemoryPacketToGame = new RingBuffer<MemoryPacketContainer>(100);
    }

    // Update is called once per frame
    void Update()
    {
        while (recvVideoPacketToGame.Count > 0)
        {
            VideoPacketContainer objects = recvVideoPacketToGame.Dequeue();

            //convert IntPtr to struct
            ENet.Event recvEvent;
            recvEvent = (ENet.Event)Marshal.PtrToStructure(objects.packetConfig, typeof(ENet.Event));

            //free the IntPtr memory
            Marshal.FreeHGlobal(objects.packetConfig);

            imageRender.VideoPacketRecieved(recvEvent, objects.videoPacket);
        }
        while (recvMemoryPacketToGame.Count > 0)
        {
            MemoryPacketContainer objects = recvMemoryPacketToGame.Dequeue();

            //convert IntPtr to struct
            ENet.Event recvEvent;
            recvEvent = (ENet.Event)Marshal.PtrToStructure(objects.packetConfig, typeof(ENet.Event));

            //free the IntPtr memory
            Marshal.FreeHGlobal(objects.packetConfig);

            rLScript.MemoryPacketRecieved(recvEvent, objects.memoryPacket);
        }
    }
}