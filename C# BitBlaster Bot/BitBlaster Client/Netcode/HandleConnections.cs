using ENet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public class HandleConnections
{
    public List<Peer> connectedPeers { get; set; }
    public List<int> allPlayerIDs { get; set; }
    public int myPlayerID { get; set; }

    // Start is called before the first frame update
    public HandleConnections()
    {
        connectedPeers = new List<Peer>();
        allPlayerIDs = new List<int>();
        MainThreadNetcode.timer.Elapsed += Timer_Elapsed;
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        while (NetworkScript.globalEvents.Count > 0)
        {
            IntPtr netEventPtr = NetworkScript.globalEvents.Dequeue();
            
            //convert IntPtr to struct
            ENet.Event netEvent;
            netEvent = (ENet.Event)Marshal.PtrToStructure(netEventPtr, typeof(ENet.Event));

            //free the IntPtr memory
            Marshal.FreeHGlobal(netEventPtr);

            switch (netEvent.Type)
            {
                case ENet.EventType.Connect:
                    Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Port: " + netEvent.Peer.Port);
                    break;

                case ENet.EventType.Disconnect:
                    Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                    break;

                case ENet.EventType.Timeout:
                    Console.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                    break;
            }
        }
    }

    ~HandleConnections()
    {
        MainThreadNetcode.timer.Elapsed -= Timer_Elapsed;
    }
}
