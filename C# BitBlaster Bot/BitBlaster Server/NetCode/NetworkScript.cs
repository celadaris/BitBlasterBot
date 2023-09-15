using UnityEngine;
using System.Threading.Tasks;
using ENet;
using DisruptorUnity3d;
using System.Threading;
using System.Runtime.InteropServices;
using System;

public class NetworkScript : MonoBehaviour
{
    [SerializeField] LogicScript logicScript;
	[SerializeField] int peerLimit;
	public static RingBuffer<IntPtr> globalEvents { get; private set; }
    Task networkTask;
	CancellationTokenSource tokenSource;
	CancellationToken ct;

	Host server;
	
	//multiple threads can read but !!do not write outside of network thread!!
	ENet.Event netEvent;
	public static RingBuffer<IntPtr[]> sendLogicToNet { get; set; }

    void Awake()
    {
        tokenSource = new CancellationTokenSource();
        ct = tokenSource.Token;

        server = new Host();
        sendLogicToNet = new RingBuffer<IntPtr[]>(100);
        globalEvents = new RingBuffer<IntPtr>(100);

        StartNetwork();
    }

	void StartNetwork()
    {
		networkTask = Task.Run(() =>
		 {
			 ENet.Library.Initialize();

			 Address address = new Address();

			 address.Port = 23002;
			 server.Create(address, peerLimit);

			 while (!ct.IsCancellationRequested)
			 {
				 bool polled = false;

				 while (sendLogicToNet.Count > 0)
				 {
					 //send packet
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

                     //create packet with appropriate flag
                     packet.Create(bufferToSend, packetConfig.packetFlag);

                     //check if there are excluded peers
                     if (packetConfig.numberOfExcludedPeers > 0)
					 {
						 Peer[] excludedPeers = PeersArrayConverter.ConvertPtrToPeersArray(packetConfig);
                         server.Broadcast(0, ref packet, excludedPeers);
                     }
					 else
					 {
                         server.Broadcast(0, ref packet);
                     }
				 }

				 while (!polled)
				 {
					 if (server.CheckEvents(out netEvent) <= 0)
					 {
						 if (server.Service(15, out netEvent) <= 0)
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

							 //debug.log contents and send over to logic thread
                             //Debug.Log("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                             logicScript.recvNetToLogic.Enqueue(recieveEventPtr);
                             break;
					 }
				 }
			 }
		 }, ct);

		networkTask.ContinueWith(t => { Debug.Log(t.Exception); },
	TaskContinuationOptions.OnlyOnFaulted);
	}

    private void OnApplicationQuit()
    {
        server.Flush();
        ENet.Library.Deinitialize();
		tokenSource.Cancel();
    }
}
