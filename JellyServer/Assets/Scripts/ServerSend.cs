using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class ServerSend
{
	private static void SendData(Packet packet, sbyte id)
	{
		Server.instance.udpComms.Send(packet, Server.instance.clients[id].ip);
	}

	private static void SendDataToAll(Packet packet)
	{
		for (int i = 0; i < Server.instance.connectedClients.Count; i++)
		{
			using Packet newPacket = new Packet(packet.ToArray());
			Server.instance.udpComms.Send(newPacket, Server.instance.connectedClients[i].ip);
		}
	}

	private static void SendDataToAll(Packet packet, sbyte exceptId)
	{
		foreach (Client client in Server.instance.connectedClients)
		{
			using Packet newPacket = new Packet(packet.ToArray());
			if (client.id == exceptId) continue;
			Server.instance.udpComms.Send(newPacket, client.ip);
		}
	}

	public static void Ping(int ping, sbyte id)
	{
		using Packet packet = new Packet(PacketType.reliable, ServerPackets.ping);

		packet.Write(ping);

		SendData(packet, id);
	}

	public static void RequestReceived(sbyte id)
	{
		Debug.Log($"Connection request received from {Server.instance.clients[id].ip.Address}.");

		using Packet packet = new Packet(PacketType.reliable, ServerPackets.requestReceived);

		packet.Write($"Welcome client-{id}!");
		packet.Write(id);

		SendData(packet, id);
	}

	public static void PlayerJoined(Client client, sbyte id)
	{
		using Packet packet = new Packet(PacketType.reliable, ServerPackets.playerJoined);

		packet.Write(client.id);
		packet.Write(client.username);

		SendData(packet, id);
	}

	public static void PlayerLeft(sbyte id)
	{
		using Packet packet = new Packet(PacketType.reliable, ServerPackets.playerLeft);

		packet.Write(id);

		SendDataToAll(packet, id);
	}

	public static void Position(Vector2 position, sbyte id)
	{
		using Packet packet = new Packet(PacketType.unreliable, ServerPackets.position);

		packet.Write(id);
		packet.Write(position[0]);
		packet.Write(position[1]);

		SendDataToAll(packet);
	}
}
