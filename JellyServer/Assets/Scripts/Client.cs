using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Timers;
using System;
using UnityEngine;

public class Client
{
	public sbyte id;
	public IPEndPoint ip;
	public bool connected = false;
	public string username;
	public int ping = 0;
	public Timer timer;
	public const float disconnectTime = 7500;

	public GameObject player = null;

	public Client(sbyte _id)
	{
		id = _id;
		timer = new Timer
		{
			Interval = disconnectTime,
			AutoReset = false,
		};
		timer.Elapsed += (s, e) => Disconnect();
	}

	public void Connect(IPEndPoint _ip)
	{
		ip = _ip;
		timer.Start();
		connected = true;
	}

	public void AddPlayer(string username)
	{
		this.username = username;
		player = GameManager.instance.Spawn(id);

		foreach (Client client in Server.instance.connectedClients)
		{
			if (client.player != null && client.id != id)
			{
				ServerSend.PlayerJoined(client, id);
			}
		}

		foreach (Client client in Server.instance.connectedClients)
		{
			if (client.player != null)
			{
				ServerSend.PlayerJoined(this, client.id);
			}
		}
	}

	public void Disconnect()
	{
		Debug.Log($"Client-{id} has disconnected!");

		ip = null;
		timer.Stop();
		connected = false;

		Server.instance.connectedClients.Remove(this);
		ServerSend.PlayerLeft(id);
		GameManager.instance.Despawn(player);
	}
}
