using ENet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class HandleConnections : MonoBehaviour
{
    public List<Peer> connectedPeers { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        connectedPeers = new List<Peer>();
    }

    // Update is called once per frame
    void Update()
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
                    Debug.Log("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Port: " + netEvent.Peer.Port);
                    connectedPeers.Add(netEvent.Peer);
                    break;

                case ENet.EventType.Disconnect:
                    Debug.Log("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                    Peer disconnectedPeer = connectedPeers.Where(x => x.ID == netEvent.Peer.ID).FirstOrDefault();
                    connectedPeers.Remove(disconnectedPeer);
                    break;

                case ENet.EventType.Timeout:
                    Debug.Log("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                    Peer timeoutPeer = connectedPeers.Where(x => x.ID == netEvent.Peer.ID).FirstOrDefault();
                    connectedPeers.Remove(timeoutPeer);
                    break;
            }
        }
    }
}
