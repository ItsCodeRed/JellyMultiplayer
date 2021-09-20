using System;
using System.Timers;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class ReliableMessage : IDisposable
{
	public short key;
	public byte[] data;
	public Action completedAction = null;

	private Timer timer;
	private float timeInterval = 500;
	private float intervalMultiplier = 2;
	private int retriesNum = 0;
	private readonly int maxRetries = 3;

	public ReliableMessage(short id, Packet packet)
	{
		key = id;
		data = packet.ToArray();
		timer = new Timer();
		timer.Interval = timeInterval;
		timer.Elapsed += (s, e) => Resend();
		timer.AutoReset = false;
		timer.Start();
	}

	public void Resend()
	{
		if (retriesNum >= maxRetries)
		{
			Client.instance.udpComms.ReceiveAck(key);
			completedAction?.Invoke();
			Dispose();
			return;
		}

		using (Packet packet = new Packet(data))
		{
			Client.instance.udpComms.Resend(packet);
		}

		retriesNum++;
		timeInterval *= intervalMultiplier;
		timer.Interval = timeInterval;
		timer.Start();
	}

	public void Dispose()
	{
		timer.Stop();
		timer.Dispose();
		key = 0;
		data = null;
		timeInterval = 0;
		intervalMultiplier = 0;
		retriesNum = 0;

		GC.SuppressFinalize(this);
	}
}
