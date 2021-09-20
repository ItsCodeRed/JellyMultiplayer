using System;
using System.Timers;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class ReliableMessage : IDisposable
{
	public short key;
	public IPEndPoint ip;
	public byte[] data;

	private Timer timer;
	private float timeInterval = 500;
	private float intervalMultiplier = 2;
	private int retriesNum = 0;
	private readonly int maxRetries = 3;

	public ReliableMessage(short id, Packet packet, IPEndPoint ip)
	{
		key = id;
		data = packet.ToArray();
		timer = new Timer
		{
			Interval = timeInterval,
			AutoReset = false,
		};
		timer.Elapsed += (s, e) => Resend();
		timer.Start();
		this.ip = ip;
	}

	public void Resend()
	{
		if (retriesNum >= maxRetries)
		{
			Server.instance.udpComms.ReceiveAck(key);
			Dispose();
			return;
		}

		using (Packet packet = new Packet(data))
		{
			Server.instance.udpComms.Resend(packet, ip);
		}

		retriesNum++;
		timeInterval *= intervalMultiplier;
		timer.Interval = timeInterval;
		timer.Start();
	}

	public void Dispose()
	{
		timer.Stop();
		key = 0;
		ip = null;
		data = null;
		timer = null;
		timeInterval = 0;
		intervalMultiplier = 0;
		retriesNum = 0;

		GC.SuppressFinalize(this);
	}
}
