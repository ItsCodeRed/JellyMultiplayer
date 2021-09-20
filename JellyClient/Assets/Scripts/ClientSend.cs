using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ClientSend
{
	private static void SendData(Packet packet)
	{
		if (Client.instance.connected)
		{
			packet.Insert(1, Client.instance.id);
			Client.instance.udpComms.Send(packet);
		}
	}

	public static void RequestJoin()
	{
		using Packet packet = new Packet(PacketType.reliable, ClientPackets.joinRequest);
		packet.Write(Client.instance.username);
		packet.Insert(1, Client.instance.id);

		Client.instance.udpComms.Send(packet);
	}

	public static void HeartBeat()
	{
		using Packet packet = new Packet(PacketType.heartbeat, ClientPackets.heartbeat);
		packet.Write(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond);

		SendData(packet);
	}

	public static void Disconnect()
	{
		using Packet packet = new Packet(PacketType.reliable, ClientPackets.disconnect);

		GameManager.instance.Despawn(Client.instance.id);

		SendData(packet);
	}

	public static void Input(sbyte[] input)
	{
		using Packet packet = new Packet(PacketType.unreliable, ClientPackets.input);

		packet.Write(input[0]);
		packet.Write(input[1]);

		SendData(packet);
	}
}