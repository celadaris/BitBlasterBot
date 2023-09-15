using DisruptorUnity3d;
using NetStack.Serialization;
using NetStack.Quantization;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

public class LogicScript : MonoBehaviour
{
    Task logicTask;
    CancellationTokenSource tokenSource;
    CancellationToken ct;


    public RingBuffer<GeneralControlsPacketContainer> logicSendGeneralControlsPacketBuffer { get; set; }
    public RingBuffer<EnterPacketContainer> logicSendEnterPacketBuffer { get; set; }
    public RingBuffer<IntPtr> recvNetToLogic { get; set; }

    void Awake()
    {
        tokenSource = new CancellationTokenSource();
        ct = tokenSource.Token;

        logicSendGeneralControlsPacketBuffer = new RingBuffer<GeneralControlsPacketContainer>(100);

        logicSendEnterPacketBuffer = new RingBuffer<EnterPacketContainer>(100);

        recvNetToLogic = new RingBuffer<IntPtr>(100);
        LogicThread();
    }

    void LogicThread()
    {
        logicTask = Task.Run(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                while (recvNetToLogic.Count > 0)
                {
                    //recv packet and dequeue to another IntPtr
                    byte[] buffer = new byte[50000];
                    IntPtr recvEventPtr = recvNetToLogic.Dequeue();

                    //convert IntPtr to struct
                    ENet.Event recvEvent;
                    recvEvent = (ENet.Event)Marshal.PtrToStructure(recvEventPtr, typeof(ENet.Event));

                    //free the IntPtr memory
                    Marshal.FreeHGlobal(recvEventPtr);

                    //copy packet contents to byte array, dispose of it from Event struct
                    recvEvent.Packet.CopyTo(buffer);
                    recvEvent.Packet.Dispose();

                    //convert Event struct back to IntPtr
                    IntPtr recvEventRePackage = Marshal.AllocHGlobal(Marshal.SizeOf(recvEvent));
                    Marshal.StructureToPtr(recvEvent, recvEventRePackage, false);

                    //convert packet to a bitbuffer
                    BitBuffer data = new BitBuffer(50000);
                    data.FromArray(buffer, buffer.Length);

                    //find what type of packet it is
                    ushort packetID = data.ReadUShort();
                    switch (packetID)
                    {
                        case 3:
                            Task.Run(() =>
                            {
                                //deserialize bitbuffer to a packet struct
                                VideoPacket videoPacket = new VideoPacket();
                                videoPacket.packetID = packetID;
                                videoPacket.arraySize = data.ReadUShort();

                                List<byte> videoFeedList = new List<byte>();
                                for (ushort i = 0; i < videoPacket.arraySize; i++)
                                {
                                    videoFeedList.Add(data.ReadByte());
                                }

                                //add array to struct
                                videoPacket.videoFeed = videoFeedList.ToArray();


                                //clear bitbuffer
                                data.Clear();

                                VideoPacketContainer videoPacketContainer = new VideoPacketContainer()
                                {
                                    packetConfig = recvEventRePackage,
                                    videoPacket = videoPacket
                                };

                                //send to main thread
                                MainThreadNetcode.recvVideoPacketToGame.Enqueue(videoPacketContainer);
                            }).ConfigureAwait(false);
                            break;

                        case 4:
                            //deserialize bitbuffer to a packet struct
                            MemoryPacket memoryPacket = new MemoryPacket();
                            memoryPacket.packetID = packetID;
                            memoryPacket.life = data.ReadByte();
                            memoryPacket.score = data.ReadInt();

                            //clear bitbuffer
                            data.Clear();

                            MemoryPacketContainer memoryPacketContainer = new MemoryPacketContainer()
                            {
                                packetConfig = recvEventRePackage,
                                memoryPacket = memoryPacket
                            };

                            //send to main thread
                            MainThreadNetcode.recvMemoryPacketToGame.Enqueue(memoryPacketContainer);
                            break;

                    }
                }

                while (logicSendGeneralControlsPacketBuffer.Count > 0)
                {
                    //get contents from main thread
                    GeneralControlsPacketContainer generalControlsPacketContainer = logicSendGeneralControlsPacketBuffer.Dequeue();

                    //convert IntPtr's to structs
                    PacketConfig packetConfig = (PacketConfig)Marshal.PtrToStructure(generalControlsPacketContainer.packetConfig, typeof(PacketConfig));

                    //free the IntPtr memory
                    Marshal.FreeHGlobal(generalControlsPacketContainer.packetConfig);

                    //compress generalControlsPacket
                    BitBuffer generalControlsPacketBitBuffer = new BitBuffer();

                    generalControlsPacketBitBuffer.AddUShort(generalControlsPacketContainer.generalControlsPacket.packetID);
                    generalControlsPacketBitBuffer.AddUShort(generalControlsPacketContainer.generalControlsPacket.arraySize);

                    for (ushort i = 0; i < generalControlsPacketContainer.generalControlsPacket.arraySize; i++)
                    {
                        generalControlsPacketBitBuffer.AddBool(generalControlsPacketContainer.generalControlsPacket.turn[i]);
                    }

                    for (ushort i = 0; i < generalControlsPacketContainer.generalControlsPacket.arraySize; i++)
                    {
                        generalControlsPacketBitBuffer.AddBool(generalControlsPacketContainer.generalControlsPacket.speed[i]);
                    }

                    for (ushort i = 0; i < generalControlsPacketContainer.generalControlsPacket.arraySize; i++)
                    {
                        generalControlsPacketBitBuffer.AddBool(generalControlsPacketContainer.generalControlsPacket.isFiring[i]);
                    }
                    //convert to byte array
                    byte[] generalControlsPacketBufferToSend = new byte[generalControlsPacketBitBuffer.Length];
                    generalControlsPacketBitBuffer.ToArray(generalControlsPacketBufferToSend);

                    //get size of our byte array
                    packetConfig.packetSize = generalControlsPacketBufferToSend.Length;

                    //convert packetConfig to IntPtr
                    IntPtr configIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(packetConfig));
                    Marshal.StructureToPtr(packetConfig, configIntPtr, false);

                    //convert byte array to IntPtr
                    IntPtr generalControlsPacketBufferIntPtr = Marshal.AllocHGlobal(generalControlsPacketBufferToSend.Length);
                    Marshal.Copy(generalControlsPacketBufferToSend, 0, generalControlsPacketBufferIntPtr, generalControlsPacketBufferToSend.Length);

                    //clear generalControlsPacketBitBuffer
                    generalControlsPacketBitBuffer.Clear();

                    IntPtr[] completePackage = new IntPtr[2]
                    {
                        configIntPtr,
                        generalControlsPacketBufferIntPtr
                    };

                    //add byte[] and packeConfig to ringbuffer
                    NetworkScript.sendLogicToNet.Enqueue(completePackage);
                }

                while (logicSendEnterPacketBuffer.Count > 0)
                {
                    //get contents from main thread
                    EnterPacketContainer enterPacketContainer = logicSendEnterPacketBuffer.Dequeue();

                    //convert IntPtr's to structs
                    PacketConfig packetConfig = (PacketConfig)Marshal.PtrToStructure(enterPacketContainer.packetConfig, typeof(PacketConfig));

                    //free the IntPtr memory
                    Marshal.FreeHGlobal(enterPacketContainer.packetConfig);

                    //compress enterPacket
                    BitBuffer enterPacketBitBuffer = new BitBuffer();

                    enterPacketBitBuffer.AddUShort(enterPacketContainer.enterPacket.packetID);
                    enterPacketBitBuffer.AddBool(enterPacketContainer.enterPacket.enterKey);
                    //convert to byte array
                    byte[] enterPacketBufferToSend = new byte[enterPacketBitBuffer.Length];
                    enterPacketBitBuffer.ToArray(enterPacketBufferToSend);

                    //get size of our byte array
                    packetConfig.packetSize = enterPacketBufferToSend.Length;

                    //convert packetConfig to IntPtr
                    IntPtr configIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(packetConfig));
                    Marshal.StructureToPtr(packetConfig, configIntPtr, false);

                    //convert byte array to IntPtr
                    IntPtr enterPacketBufferIntPtr = Marshal.AllocHGlobal(enterPacketBufferToSend.Length);
                    Marshal.Copy(enterPacketBufferToSend, 0, enterPacketBufferIntPtr, enterPacketBufferToSend.Length);

                    //clear enterPacketBitBuffer
                    enterPacketBitBuffer.Clear();

                    IntPtr[] completePackage = new IntPtr[2]
                    {
                        configIntPtr,
                        enterPacketBufferIntPtr
                    };

                    //add byte[] and packeConfig to ringbuffer
                    NetworkScript.sendLogicToNet.Enqueue(completePackage);
                }

            }
        }, ct);
        logicTask.ContinueWith(t => { Debug.Log(t.Exception); },
    TaskContinuationOptions.OnlyOnFaulted);
    }

    private void OnApplicationQuit()
    {
        tokenSource.Cancel();
    }
}
