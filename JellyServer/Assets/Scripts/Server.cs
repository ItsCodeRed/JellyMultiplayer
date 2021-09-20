using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

public class Server : MonoBehaviour
{
	public int port = 12345;
	public int maxPlayers = 8;

	public static Server instance = null;
	public UdpComms udpComms;

	public List<Client> clients = new List<Client>();
	public List<Client> connectedClients = new List<Client>();

	public delegate void PacketHandler(Packet _packet, sbyte _clientId);
	public static Dictionary<int, PacketHandler> packetHandlers;

	public void Awake()
	{
		// Singleton implementation
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Debug.LogWarning($"Two server objects have been created. Destroying this ({name})!");
			Destroy(gameObject);
		}

		Debug.Log("Starting server....");
		InitializeSeverData();

		Application.targetFrameRate = 60;
		QualitySettings.vSyncCount = 0;
		udpComms = new UdpComms(new UdpComms.DataHandler(HandleData));
		udpComms.Start(port);

		Debug.Log($"Server has started on port {port}.");
	}

	private void LateUpdate()
	{
		if (udpComms != null)
		{
			udpComms.Update();
		}
	}

	public void HandleData(Packet packet, ReceiveInfo receiveInfo)
	{
		sbyte clientId = packet.ReadSByte();

		if (clientId == -1) // an id of -1 indicates a client without an id. ie a new client
		{
			HandleJoin(packet, receiveInfo);
		}
		else if (clients[clientId].connected)
		{
			byte packetId = packet.ReadByte();
			packetHandlers[packetId](packet, clientId); // handles the packet using the packet handler method specified in the message
		}
		else
		{
			Debug.LogWarning($"Packet sent by disconnected client (client-{clientId}). Weird!");
		}
	}

	private void HandleJoin(Packet packet, ReceiveInfo receiveInfo)
	{
		try
		{
			sbyte clientId = AddNewClient(receiveInfo.SenderIp);

			if (clientId >= 0)
			{
				byte packetId = packet.ReadByte();
				packetHandlers[packetId](packet, clientId); // handles the packet using the welcome packet handler
			}
		}
		catch (Exception _ex)
		{
			Debug.LogWarning($"User at {receiveInfo.SenderIp.Address} failed to connect. Exception: {_ex}");
		}
	}

	private sbyte AddNewClient(IPEndPoint _ip)
	{
		// Goes through the client list, and attempts to find an empty client instance to connect to
		for (sbyte i = 0; i < maxPlayers; i++)
		{
			if (!clients[i].connected)
			{
				clients[i].Connect(_ip);
				connectedClients.Add(clients[i]);
				ServerSend.RequestReceived(i);
				return i;
			}
		}

		Debug.Log($"{_ip.Address} failed to connect: Server is full!");
		return -1;
	}

	private void OnApplicationQuit()
	{
		if (udpComms != null)
		{
			udpComms.socket.Close();
		}
	}

	private void InitializeSeverData()
	{
		// Fills out the client list with empty client instances
		for (sbyte i = 0; i < maxPlayers; i++)
		{
			clients.Add(new Client(i));
		}

		// Fills out the dictionary with handler functions corresponding to their packet ids
		packetHandlers = new Dictionary<int, PacketHandler>()
		{
			{ (int)ClientPackets.heartbeat, ServerHandle.Heatbeat },
			{ (int)ClientPackets.joinRequest, ServerHandle.JoinRequest },
			{ (int)ClientPackets.disconnect, ServerHandle.Disconnect },
			{ (int)ClientPackets.input, ServerHandle.Input },
		};

		Debug.Log("Server initialized.");
	}
}
