﻿using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SimulationTest : MonoBehaviour
{
    public enum PacketType
    {
        SNAPSHOT    = 0,
        INPUT       = 1,
        ACK         = 2,
        PLAYER_JOINED_GAME   = 3,
        NEW_PLAYER_BROADCAST  = 4
    }
    private Channel channel;
    private Channel inputChannel;
    private Channel ackChannel;
    private MyServer myServer;

    private bool connected = true;
    public int pps = 10;
    private float time;
    private int channelPort;
    private IPEndPoint serverEndpoint;

    private Dictionary<int, MyClient> clients;
    private List<ReliablePacket> sentPlayerJoinEvents;
    [SerializeField] private GameObject serverPrefab;
    [SerializeField] private GameObject clientPrefab;

    // Start is called before the first frame update
    void Start() {
        time = 0f;
        channelPort = 9000;
        clients = new Dictionary<int, MyClient>();
        channel = new Channel(channelPort);
        serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), channelPort);
        myServer = new MyServer(serverPrefab, channel, pps);
        sentPlayerJoinEvents = new List<ReliablePacket>();
    }

    private void OnDestroy() {
        channel.Disconnect();
    }

    // Update is called once per frame
    void Update() {
        time += Time.deltaTime;
        if(Input.GetKeyDown(KeyCode.J))
        {
            int newPlayerId = CreateNewPlayer();
            SendJoinToServer(newPlayerId);
        }
        CheckForPlayerJoinedACK();
        ResendPlayerJoinedIfExpired();

        foreach (MyClient client in clients.Values)
        {
            client.UpdateClient();
        }
        myServer.UpdateServer();        
    }
    private void CheckForPlayerJoinedACK()
    {
        List<int> toRemove = new List<int>();
        for (int i = 0; i < sentPlayerJoinEvents.Count; i++)
        {
            //Packet index is actually clientId in this context
            int clientId = sentPlayerJoinEvents[i].packetIndex;
            MyClient client = clients[clientId];
            if (ReceivedPlayerAck(client))
            {
                toRemove.Add(i);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            sentPlayerJoinEvents[i].packet.Free();
            sentPlayerJoinEvents.RemoveAt(toRemove[i]);
        }
    }
    private bool ReceivedPlayerAck(MyClient client)
    {
        var packet = client.GetChannel().GetPacket();
        if(packet != null)
        {
            int packetType = packet.buffer.GetInt();
            if(packetType == (int) PacketType.PLAYER_JOINED_GAME)
            {
                int clientId = packet.buffer.GetInt();
                Debug.Log("Received player ack " + clientId);
                if(clientId == 1)
                {
                    client.id = clientId;
                    CubeEntity cube = new CubeEntity(clientPrefab);
                }
                return true;
            }
        }
        return false;
    }
    private void SendJoinToServer(int newPlayerId)
    {
        Packet packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.PLAYER_JOINED_GAME);
        packet.buffer.PutInt(newPlayerId);
        packet.buffer.Flush();
        sentPlayerJoinEvents.Add(new ReliablePacket(packet, newPlayerId, 1f, time));
        clients[newPlayerId].GetChannel().Send(packet, serverEndpoint);
        packet.Free();
    }
    private void ResendPlayerJoinedIfExpired()
    {
        for (int i = 0; i < sentPlayerJoinEvents.Count; i++) 
        {
            if(sentPlayerJoinEvents[i].CheckIfExpired(time))
            {
                Resend(i);
            }
        }
    }
    private void Resend(int index) 
    {
        channel.Send(sentPlayerJoinEvents[index].packet, serverEndpoint);
        sentPlayerJoinEvents[index].sentTime = time;
    }
    private int CreateNewPlayer()
    {
        int clientId = clients.Count + 1;
        Channel clientChannel = new Channel(9000 + clientId);
        MyClient client = new MyClient(clientPrefab, clientChannel, serverEndpoint, pps, clientId);
        clients[clientId] = client;
        return clientId;
    }
}
