using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using System.Timers;
using System.Text;
using UnityEngine;

public class Client : MonoBehaviour
{
    public string username = "user";

    public sbyte id = -1;
    public string ip = "127.0.0.1";
    public int port = 12345;
    public bool connected = false;
    public int ping = 0;

    public Timer heart;
    public float heartbeatInterval = 1000; 

    public static Client instance = null;
    public UdpComms udpComms;

    public delegate void PacketHandler(Packet _packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    public void Awake()
    {
        // Singleton implementation
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Debug.LogWarning($"Two client objects have been created. Destroying this ({name})!");
            Destroy(gameObject);
        }
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        InitializeClientData();
        udpComms = new UdpComms(HandleData);
    }

    public void Connect(string _ip)
    {
        ip = _ip;

        Debug.Log("Connecting....");
        udpComms.Start(new IPEndPoint(IPAddress.Parse(_ip), port));

        ClientSend.RequestJoin();
    }

    public void HandleData(Packet packet, ReceiveInfo receiveInfo)
    {
        byte packetId = packet.ReadByte();
        Debug.Log(packetId);
        packetHandlers[packetId](packet);
    }

    public void Disconnect()
    {
        udpComms.reliableMessages.Clear();
        ClientSend.Disconnect();
        connected = false;
        udpComms.socket.Close();
        id = -1;
    }

	private void LateUpdate()
	{
        udpComms.Update();
	}

	private void OnApplicationQuit()
	{
        Disconnect();
    }

	private void InitializeClientData()
    {
        // Fills out the dictionary with handler functions corresponding to their packet ids
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.ping, ClientHandle.Ping },
            { (int)ServerPackets.requestReceived, ClientHandle.RequestReceived },
            { (int)ServerPackets.position, ClientHandle.Position },
            { (int)ServerPackets.playerJoined, ClientHandle.PlayerJoined },
            { (int)ServerPackets.playerLeft, ClientHandle.PlayerLeft },
        };

        heart = new Timer
        {
            Interval = heartbeatInterval,
            AutoReset = true
		};
        heart.Elapsed += (s, e) => ClientSend.HeartBeat();

        Debug.Log("Packets initialized.");
    }
}
