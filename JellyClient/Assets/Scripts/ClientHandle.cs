using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ClientHandle
{
	public static void Ping(Packet packet)
	{
		Client.instance.ping = packet.ReadInt();
	}

	public static void RequestReceived(Packet packet)
	{
		string message = packet.ReadString();
		sbyte id = packet.ReadSByte();
		Client.instance.id = id;
		Client.instance.connected = true;
		Client.instance.heart.Start();

		Debug.Log($"Connected. {message}");
	}

	public static void PlayerJoined(Packet packet)
	{
		sbyte id = packet.ReadSByte();
		string username = packet.ReadString();

		if (id == Client.instance.id)
		{
			GameManager.instance.SpawnLocal();
		}
		else
		{
			GameManager.instance.Spawn(id, username);
		}
	}

	public static void PlayerLeft(Packet packet)
	{
		sbyte id = packet.ReadSByte();

		GameManager.instance.Despawn(id);
	}

	public static void Position(Packet packet)
	{
		sbyte id = packet.ReadSByte();
		Vector2 pos = new Vector2(packet.ReadFloat(), packet.ReadFloat());

		if (GameManager.instance.players.ContainsKey(id))
		{
			GameManager.instance.players[id].transform.position = pos;
		}
	}
}
