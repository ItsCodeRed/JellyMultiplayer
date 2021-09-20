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
	private ReceiveInfo receiveInfo;

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

	public void Start(IPEndPoint endPoint)
	{
		socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
		socket.Connect(endPoint); // connects to the end point specified

		Receive();
	}

	public void Receive()
	{
		receiveInfo = new ReceiveInfo(); // listens for messages from any sender
		socket.BeginReceiveFrom(receiveInfo.data, 0, bufferSize, SocketFlags.None, ref receiveInfo.senderEp, new AsyncCallback(ReceiveData), receiveInfo);
	}

	private void ReceiveData(IAsyncResult result)
	{
		try
		{
			receiveInfo = (ReceiveInfo)result.AsyncState;
			int dataLength = socket.EndReceiveFrom(result, ref receiveInfo.senderEp);

			using Packet packetData = new Packet(receiveInfo.data);
			if (dataLength == packetData.ReadShort() + 2) // checks if the total bytes recieved equals the bytes reported by the packet (+2 to account for the written length).
			{
				PacketType type = (PacketType)packetData.ReadByte();

				short typeData = 0;
				if (type != PacketType.unreliable)
				{
					typeData = packetData.ReadShort();
				}

				if (type == PacketType.reliable)
				{
					SendAck(typeData);
				}
				else if (type == PacketType.ack)
				{
					ReceiveAck(typeData);
					return;
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

	public void SendAck(int messageId)
	{
		using Packet packet = new Packet();
		packet.Write((byte)PacketType.ack);
		packet.Write((short)messageId);

		Send(packet);
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

	public void Send(Packet packet)
	{
		IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(Client.instance.ip), Client.instance.port);

		packet.SetReadPos(0);
		if ((PacketType)packet.ReadByte(false) == PacketType.reliable || (PacketType)packet.ReadByte(false) == PacketType.heartbeat)
		{
			packet.Insert(1, BitConverter.GetBytes(nextReliablePacket));
			reliableMessages.Add(nextReliablePacket, new ReliableMessage(nextReliablePacket, packet));
			if ((PacketType)packet.ReadByte(false) == PacketType.heartbeat)
				reliableMessages[nextReliablePacket].completedAction = () => { Client.instance.udpComms.actionQueue.Add(() => { Client.instance.Disconnect(); }); };
			nextReliablePacket++;
		}
		packet.WriteLength();

		socket.BeginSendTo(packet.ToArray(), 0, packet.Length(), SocketFlags.None, serverEndPoint, (ar) => socket.EndSendTo(ar), null);
	}

	public void Resend(Packet packet)
	{
		IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(Client.instance.ip), Client.instance.port);
		packet.WriteLength();

		socket.BeginSendTo(packet.ToArray(), 0, packet.Length(), SocketFlags.None, serverEndPoint, (ar) => socket.EndSendTo(ar), null);
	}
}
