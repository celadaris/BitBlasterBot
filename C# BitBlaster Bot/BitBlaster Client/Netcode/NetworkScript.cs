using ENet;
using DisruptorUnity3d;
using System.Runtime.InteropServices;

public class NetworkScript
{
    public static RingBuffer<IntPtr> globalEvents { get; private set; }
    Task networkTask;
    public static CancellationTokenSource tokenSource;
    CancellationToken ct;

    Host client;
    ENet.Event netEvent;
    public static RingBuffer<IntPtr[]> sendLogicToNet { get; set; }

    public NetworkScript()
    {
        tokenSource = new CancellationTokenSource();
        ct = tokenSource.Token;
        client = new Host();
        sendLogicToNet = new RingBuffer<IntPtr[]>(100);
        globalEvents = new RingBuffer<IntPtr>(100);

        StartNetwork();

        ct.Register(() => 
        {
            client.Flush();
            ENet.Library.Deinitialize();
        });
    }

    void StartNetwork()
    {
        networkTask = Task.Run(() =>
         {
             ENet.Library.Initialize();

             Address address = new Address();

             address.SetHost("192.168.0.162");
             address.Port = 23002;
             client.Create();

             Peer peer = client.Connect(address);

             while (!ct.IsCancellationRequested)
             {
                 bool polled = false;

                 while (sendLogicToNet.Count > 0)
                 {
                     //create sending packet on heap
                     Packet packet = default(Packet);

                     //get completePacket pointers
                     IntPtr[] objects = sendLogicToNet.Dequeue();

                     //get packetConfig
                     PacketConfig packetConfig = (PacketConfig)Marshal.PtrToStructure(objects[0], typeof(PacketConfig));

                     //get buffer from complete package
                     byte[] bufferToSend = new byte[packetConfig.packetSize];
                     Marshal.Copy(objects[1], bufferToSend, 0, bufferToSend.Length);

                     //free memory
                     Marshal.FreeHGlobal(objects[0]);
                     Marshal.FreeHGlobal(objects[1]);

                     //create with appropriate flag
                     packet.Create(bufferToSend, packetConfig.packetFlag);

                     peer.Send(0, ref packet);
                 }

                 while (!polled)
                 {
                     if (client.CheckEvents(out netEvent) <= 0)
                     {
                         if (client.Service(15, out netEvent) <= 0)
                             break;

                         polled = true;
                     }

                     switch (netEvent.Type)
                     {
                         case ENet.EventType.None:
                             break;

                         case ENet.EventType.Connect:
                             //convert struct to IntPtr
                             IntPtr connectEventPtr = Marshal.AllocHGlobal(Marshal.SizeOf(netEvent));
                             Marshal.StructureToPtr(netEvent, connectEventPtr, false);
                             globalEvents.Enqueue(connectEventPtr);
                             break;

                         case ENet.EventType.Disconnect:
                             //convert struct to IntPtr
                             IntPtr disconnectEventPtr = Marshal.AllocHGlobal(Marshal.SizeOf(netEvent));
                             Marshal.StructureToPtr(netEvent, disconnectEventPtr, false);

                             globalEvents.Enqueue(disconnectEventPtr);
                             break;

                         case ENet.EventType.Timeout:
                             //convert struct to IntPtr
                             IntPtr timeoutEventPtr = Marshal.AllocHGlobal(Marshal.SizeOf(netEvent));
                             Marshal.StructureToPtr(netEvent, timeoutEventPtr, false);

                             globalEvents.Enqueue(timeoutEventPtr);
                             break;

                         case ENet.EventType.Receive:
                             //convert struct to IntPtr
                             IntPtr recieveEventPtr = Marshal.AllocHGlobal(Marshal.SizeOf(netEvent));
                             Marshal.StructureToPtr(netEvent, recieveEventPtr, false);

                             //log contents and send over to logic thread
                             //Console.WriteLine("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                             LogicScript.recvNetToLogic.Enqueue(recieveEventPtr);
                             break;
                     }
                 }
             }
         }, ct);

        networkTask.ContinueWith(t => { Console.WriteLine(t.Exception); },
    TaskContinuationOptions.OnlyOnFaulted);
    }
}