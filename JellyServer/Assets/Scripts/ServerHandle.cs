using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using UnityEngine;

public static class ServerHandle
{
	public static void JoinRequest(Packet packet, sbyte clientId)
	{
		string username = packet.ReadString();
		Server.instance.clients[clientId].AddPlayer(username);

		Debug.Log($"{username} has joined! (client-{clientId})");
	}

	public static void Input(Packet packet, sbyte clientId)
	{
		Server.instance.clients[clientId].player.GetComponent<PlayerMovement>().HandleInput(new Vector2(packet.ReadSByte(), packet.ReadSByte()));
	}
	public static void Heatbeat(Packet packet, sbyte clientId)
	{
		int ping = (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond - packet.ReadLong());
		Server.instance.clients[clientId].ping = ping;

		Server.instance.clients[clientId].timer.Stop();
		Server.instance.clients[clientId].timer.Start();

		ServerSend.Ping(ping, clientId);
	}
	public static void Disconnect(Packet packet, sbyte clientId)
	{
		Server.instance.clients[clientId].Disconnect();
	}
}
