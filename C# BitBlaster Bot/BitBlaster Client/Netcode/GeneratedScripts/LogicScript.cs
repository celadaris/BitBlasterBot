using DisruptorUnity3d;
using NetStack.Serialization;
using System.Runtime.InteropServices;

public class LogicScript
{
    Task logicTask;
    public static CancellationTokenSource tokenSource;
    CancellationToken ct;


    public RingBuffer<VideoPacketContainer> logicSendVideoPacketBuffer { get; set; }
    public RingBuffer<MemoryPacketContainer> logicSendMemoryPacketBuffer { get; set; }
    public static RingBuffer<IntPtr> recvNetToLogic { get; set; }

    public LogicScript()
    {
        tokenSource = new CancellationTokenSource();
        ct = tokenSource.Token;

        logicSendVideoPacketBuffer = new RingBuffer<VideoPacketContainer>(100);

        logicSendMemoryPacketBuffer = new RingBuffer<MemoryPacketContainer>(100);

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
                    byte[] buffer = new byte[1024];
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
                    BitBuffer data = new BitBuffer(1024);
                    data.FromArray(buffer, buffer.Length);

                    //find what type of packet it is
                    ushort packetID = data.ReadUShort();
                    switch (packetID)
                    {
                        case 1:
                            //deserialize bitbuffer to a packet struct
                            GeneralControlsPacket generalControlsPacket = new GeneralControlsPacket();
                            generalControlsPacket.packetID = packetID;
                            generalControlsPacket.arraySize = data.ReadUShort();

                            List<bool> turnList = new List<bool>();
                            for (ushort i = 0; i < generalControlsPacket.arraySize; i++)
                            {
                                turnList.Add(data.ReadBool());
                            }

                            //add array to struct
                            generalControlsPacket.turn = turnList.ToArray();


                            List<bool> speedList = new List<bool>();
                            for (ushort i = 0; i < generalControlsPacket.arraySize; i++)
                            {
                                speedList.Add(data.ReadBool());
                            }

                            //add array to struct
                            generalControlsPacket.speed = speedList.ToArray();


                            List<bool> isFiringList = new List<bool>();
                            for (ushort i = 0; i < generalControlsPacket.arraySize; i++)
                            {
                                isFiringList.Add(data.ReadBool());
                            }

                            //add array to struct
                            generalControlsPacket.isFiring = isFiringList.ToArray();


                            //clear bitbuffer
                            data.Clear();

                            GeneralControlsPacketContainer generalControlsPacketContainer = new GeneralControlsPacketContainer()
                            {
                                packetConfig = recvEventRePackage,
                                generalControlsPacket = generalControlsPacket
                            };

                            //send to main thread
                            MainThreadNetcode.recvGeneralControlsPacketToGame.Enqueue(generalControlsPacketContainer);
                            break;

                        case 2:
                            //deserialize bitbuffer to a packet struct
                            EnterPacket enterPacket = new EnterPacket();
                            enterPacket.packetID = packetID;
                            enterPacket.enterKey = data.ReadBool();

                            //clear bitbuffer
                            data.Clear();

                            EnterPacketContainer enterPacketContainer = new EnterPacketContainer()
                            {
                                packetConfig = recvEventRePackage,
                                enterPacket = enterPacket
                            };

                            //send to main thread
                            MainThreadNetcode.recvEnterPacketToGame.Enqueue(enterPacketContainer);
                            break;

                    }
                }

                while (logicSendVideoPacketBuffer.Count > 0)
                {
                    //get contents from main thread
                    VideoPacketContainer videoPacketContainer = logicSendVideoPacketBuffer.Dequeue();

                    //convert IntPtr's to structs
                    PacketConfig packetConfig = (PacketConfig)Marshal.PtrToStructure(videoPacketContainer.packetConfig, typeof(PacketConfig));

                    //free the IntPtr memory
                    Marshal.FreeHGlobal(videoPacketContainer.packetConfig);

                    //compress videoPacket
                    BitBuffer videoPacketBitBuffer = new BitBuffer();

                    videoPacketBitBuffer.AddUShort(videoPacketContainer.videoPacket.packetID);
                    videoPacketBitBuffer.AddUShort(videoPacketContainer.videoPacket.arraySize);

                    for (ushort i = 0; i < videoPacketContainer.videoPacket.arraySize; i++)
                    {
                        videoPacketBitBuffer.AddByte(videoPacketContainer.videoPacket.videoFeed[i]);
                    }
                    //convert to byte array
                    byte[] videoPacketBufferToSend = new byte[videoPacketBitBuffer.Length];
                    videoPacketBitBuffer.ToArray(videoPacketBufferToSend);

                    //get size of our byte array
                    packetConfig.packetSize = videoPacketBufferToSend.Length;

                    //convert packetConfig to IntPtr
                    IntPtr configIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(packetConfig));
                    Marshal.StructureToPtr(packetConfig, configIntPtr, false);

                    //convert byte array to IntPtr
                    IntPtr videoPacketBufferIntPtr = Marshal.AllocHGlobal(videoPacketBufferToSend.Length);
                    Marshal.Copy(videoPacketBufferToSend, 0, videoPacketBufferIntPtr, videoPacketBufferToSend.Length);

                    //clear videoPacketBitBuffer
                    videoPacketBitBuffer.Clear();

                    IntPtr[] completePackage = new IntPtr[2]
                    {
                        configIntPtr,
                        videoPacketBufferIntPtr
                    };

                    //add byte[] and packeConfig to ringbuffer
                    NetworkScript.sendLogicToNet.Enqueue(completePackage);
                }

                while (logicSendMemoryPacketBuffer.Count > 0)
                {
                    //get contents from main thread
                    MemoryPacketContainer memoryPacketContainer = logicSendMemoryPacketBuffer.Dequeue();

                    //convert IntPtr's to structs
                    PacketConfig packetConfig = (PacketConfig)Marshal.PtrToStructure(memoryPacketContainer.packetConfig, typeof(PacketConfig));

                    //free the IntPtr memory
                    Marshal.FreeHGlobal(memoryPacketContainer.packetConfig);

                    //compress memoryPacket
                    BitBuffer memoryPacketBitBuffer = new BitBuffer();

                    memoryPacketBitBuffer.AddUShort(memoryPacketContainer.memoryPacket.packetID);
                    memoryPacketBitBuffer.AddByte(memoryPacketContainer.memoryPacket.life);
                    memoryPacketBitBuffer.AddInt(memoryPacketContainer.memoryPacket.score);
                    //convert to byte array
                    byte[] memoryPacketBufferToSend = new byte[memoryPacketBitBuffer.Length];
                    memoryPacketBitBuffer.ToArray(memoryPacketBufferToSend);

                    //get size of our byte array
                    packetConfig.packetSize = memoryPacketBufferToSend.Length;

                    //convert packetConfig to IntPtr
                    IntPtr configIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(packetConfig));
                    Marshal.StructureToPtr(packetConfig, configIntPtr, false);

                    //convert byte array to IntPtr
                    IntPtr memoryPacketBufferIntPtr = Marshal.AllocHGlobal(memoryPacketBufferToSend.Length);
                    Marshal.Copy(memoryPacketBufferToSend, 0, memoryPacketBufferIntPtr, memoryPacketBufferToSend.Length);

                    //clear memoryPacketBitBuffer
                    memoryPacketBitBuffer.Clear();

                    IntPtr[] completePackage = new IntPtr[2]
                    {
                        configIntPtr,
                        memoryPacketBufferIntPtr
                    };

                    //add byte[] and packeConfig to ringbuffer
                    NetworkScript.sendLogicToNet.Enqueue(completePackage);
                }

            }
        }, ct);
        logicTask.ContinueWith(t => { Console.WriteLine(t.Exception); },
    TaskContinuationOptions.OnlyOnFaulted);
    }
}
