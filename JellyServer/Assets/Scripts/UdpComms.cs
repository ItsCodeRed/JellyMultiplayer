using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

public class ReceiveInfo
{
	public byte[] data = new byte[UdpComms.bufferSize];
	public EndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);
	public IPEndPoint SenderIp
	{ get { return (IPEndPoint)senderEp; } }
}

public class UdpComms
{
	public const int bufferSize = 8 * 1024;
	public Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
	private ReceiveInfo info;

	public delegate void DataHandler(Packet packet, ReceiveInfo receiveInfo);
	public DataHandler dataHandler;
	public ActionQueue actionQueue;

	public Dictionary<short, ReliableMessage> reliableMessages = new Dictionary<short, ReliableMessage>();

	public short nextReliablePacket = short.MinValue;

	public UdpComms(DataHandler dataHandler)
	{
		this.dataHandler = dataHandler;
		actionQueue = new ActionQueue();
	}

	public void Start(int port)
	{
		socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
		socket.Bind(new IPEndPoint(IPAddress.Any, port)); // listens on the port specified

		Receive();
	}

	public void Receive()
	{
		info = new ReceiveInfo(); // listens for messages from any sender
		socket.BeginReceiveFrom(info.data, 0, bufferSize, SocketFlags.None, ref info.senderEp, new AsyncCallback(ReceiveData), info);
	}

	private void ReceiveData(IAsyncResult result)
	{
		try
		{
			ReceiveInfo receiveInfo = (ReceiveInfo)result.AsyncState;
			int dataLength = socket.EndReceiveFrom(result, ref info.senderEp);
			receiveInfo.senderEp = info.senderEp;
			info = new ReceiveInfo();

			using Packet packetData = new Packet(receiveInfo.data);

			if (dataLength == packetData.ReadShort() + 2) // checks if the total bytes recieved equals the bytes reported by the packet (+2 to account for the written length).
			{
				PacketType type = (PacketType)packetData.ReadByte();

				if (type != PacketType.unreliable)
				{
					short typeData = packetData.ReadShort();

					if (type == PacketType.reliable || type == PacketType.heartbeat)
					{
						SendAck(typeData, receiveInfo.SenderIp);
					}
					else if (type == PacketType.ack)
					{
						ReceiveAck(typeData);
						return;
					}
				}
				byte[] remainingBytes = packetData.ReadBytes(packetData.UnreadLength());

				actionQueue.Add(() => {
					using Packet packet = new Packet(remainingBytes);

					dataHandler(packet, receiveInfo);
				});
			}
		}
		finally
		{
			Receive();
		}
	}

	public void SendAck(short messageId, IPEndPoint ip)
	{
		using Packet packet = new Packet();
		packet.Write((byte)PacketType.ack);
		packet.Write(messageId);

		Send(packet, ip);
	}

	public void ReceiveAck(short messageId)
	{
		reliableMessages[messageId].Dispose();
		reliableMessages.Remove(messageId);
	}

	public void Update()
	{
		actionQueue.ExecuteAll();
	}

	public void Send(Packet packet, IPEndPoint ip)
	{
		packet.SetReadPos(0);
		if ((PacketType)packet.ReadByte(false) == PacketType.reliable)
		{
			packet.Insert(1, BitConverter.GetBytes(nextReliablePacket));
			reliableMessages.Add(nextReliablePacket, new ReliableMessage(nextReliablePacket, packet, ip));
			nextReliablePacket++;
		}
		packet.WriteLength();

		socket.BeginSendTo(packet.ToArray(), 0, packet.Length(), SocketFlags.None, ip, (ar) => socket.EndSendTo(ar), null);
	}

	public void Resend(Packet packet, IPEndPoint ip)
	{
		packet.WriteLength();
		socket.BeginSendTo(packet.ToArray(), 0, packet.Length(), SocketFlags.None, ip, (ar) => socket.EndSendTo(ar), null);
	}
}
