using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Serialization;

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
    public int pps = 60;
    private float time;
    private int channelPort;
    private IPEndPoint serverEndpoint;

    private Dictionary<int, MyClient> clients;
    private List<ReliablePacket> sentPlayerJoinEvents;
    [SerializeField] GameObject playerServerPrefab;
    [SerializeField] GameObject playerClientPrefab;
    [SerializeField] GameObject otherPlayerClientPrefab;
    [SerializeField] GameObject playerUIPrefab;

    // Start is called before the first frame update
    void Start() {
        time = 0f;
        channelPort = 9000;
        clients = new Dictionary<int, MyClient>();
        channel = new Channel(channelPort);
        serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), channelPort);
        myServer = new MyServer(playerServerPrefab, channel, pps);
        sentPlayerJoinEvents = new List<ReliablePacket>();
    }

    private void OnDestroy() {
        foreach(MyClient client in clients.Values)
        {
            client.GetChannel().Disconnect();
        }
    }

    // Update is called once per frame
    void Update() {
        time += Time.deltaTime;
        if(Input.GetKeyDown(KeyCode.J))
        {
            int newPlayerId = CreateNewPlayer();
            SendJoinToServer(newPlayerId);
        }
        CheckForPlayerJoinedAck();
        ResendPlayerJoinedIfExpired();

        foreach (MyClient client in clients.Values)
        {
            client.UpdateClient();
        }
        myServer.UpdateServer();        
    }

    private void FixedUpdate()
    {
        foreach (MyClient client in clients.Values)
        {
            client.FixedUpdate();
        }
        myServer.FixedUpdate();
    }

    private void CheckForPlayerJoinedAck()
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
        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            if(packetType == (int) PacketType.PLAYER_JOINED_GAME)
            {
                int clientId = packet.buffer.GetInt();
                if(clientId == 1)
                {
                    client.id = clientId;
                    CubeEntity cube = new CubeEntity(playerClientPrefab);
                }
                return true;
            }
            packet = client.GetChannel().GetPacket();
        }
        return false;
    }
    private void SendJoinToServer(int newPlayerId)
    {
        Packet packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.PLAYER_JOINED_GAME);
        // In this case packet index is playerId
        packet.buffer.PutInt(newPlayerId);
        packet.buffer.Flush();
        sentPlayerJoinEvents.Add(new ReliablePacket(packet, newPlayerId, 1f, time, -1));
        clients[newPlayerId].GetChannel().Send(packet, serverEndpoint);
        packet.Free();
    }
    private void ResendPlayerJoinedIfExpired()
    {
        for (int i = 0; i < sentPlayerJoinEvents.Count; i++) 
        {
            if(sentPlayerJoinEvents[i].CheckIfExpired(time))
            {
                Debug.Log("Resending");
                Resend(i);
            }
        }
    }
    private void Resend(int index)
    {
        int clientId = sentPlayerJoinEvents[index].packetIndex;
        clients[clientId].GetChannel().Send(sentPlayerJoinEvents[index].packet, serverEndpoint);
        sentPlayerJoinEvents[index].sentTime = time;
    }
    private int CreateNewPlayer()
    {
        int clientId = clients.Count + 1;
        Channel clientChannel = new Channel(9000 + clientId);
        Debug.Log("Creating new client with id " + clientId);
        MyClient client = new MyClient(playerClientPrefab, otherPlayerClientPrefab, playerUIPrefab, clientChannel, serverEndpoint, pps, clientId);
        clients.Add(clientId, client);
        return clientId;
    }
}
