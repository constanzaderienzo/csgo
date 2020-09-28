using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
public class MyServer {
    public enum PacketType
    {
        SNAPSHOT    = 0,
        INPUT       = 1,
        ACK         = 2,
        PLAYER_JOINED_GAME   = 3,
        NEW_PLAYER_BROADCAST  = 4
    }
    private GameObject serverPrefab;
    private Channel channel;
    private float accum = 0f;
    private float serverTime = 0f;
    private int packetNumber = 0;
    private int pps;
    private Dictionary<int, GameObject> clientsCubes;
    private Dictionary<int, ClientInfo> clients;
    private List<NewPlayerBroadcastEvent> newPlayerBroadcastEvents;

    public MyServer(GameObject serverPrefab, Channel channel, int pps) {
        this.serverPrefab = serverPrefab;
        this.channel = channel;
        this.pps = pps;
        this.clientsCubes = new Dictionary<int, GameObject>();
        this.clients = new Dictionary<int, ClientInfo>();
        this.newPlayerBroadcastEvents = new List<NewPlayerBroadcastEvent>();
    }

    public void UpdateServer() {
        accum += Time.deltaTime;    
        serverTime += Time.deltaTime;
        ProcessPacket();
        float sendRate = (1f/pps);
        if (accum >= sendRate) {
            SendSnapshot();
            // Restart accum
            accum -= sendRate;
        }
    }

    private void ProcessPacket()
    {
        var packet = channel.GetPacket();
        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            switch(packetType)
            {
                case (int) PacketType.INPUT:
                    ServerReceivesClientInput(packet);
                    break;
                case (int) PacketType.PLAYER_JOINED_GAME:
                    JoinPlayer(packet);
                    break;
                case (int) PacketType.NEW_PLAYER_BROADCAST:
                    ClientReceivedNewPlayerBroadcast(packet);
                    break;
                default:
                    Debug.Log("Unrecognized type in server");
                    break;
            }
            packet.Free();
            packet = channel.GetPacket();
        }
    }

    private void ServerReceivesClientInput(Packet packet){
        int actionsCount = packet.buffer.GetInt();
        int clientId = -1;
        ClientInfo client = null;
        for (int i = 0; i < actionsCount; i++) {
            Actions action = new Actions();
            Debug.Log(packet.buffer.GetAvailableByteCount());
            action.DeserializeInput(packet.buffer);
            clientId = action.id;
            client = clients[clientId];
            if(action.inputIndex > client.lastInputApplied) {
                // mover el cubo
                client.lastInputApplied = action.inputIndex;
                var rigidBody = clientsCubes[action.id].GetComponent<Rigidbody>();
                ApplyClientInput(action, rigidBody);
            }
        }
        if(clientId != -1 && client != null)
            SendACK(client.lastInputApplied, clients[clientId].ipEndPoint, (int) PacketType.ACK);
    }

    private void SendACK(int inputIndex, IPEndPoint clientEndpoint, int ackType) {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(ackType);
        packet.buffer.PutInt(inputIndex);
        packet.buffer.Flush();
        if(ackType == (int) MyClient.PacketType.ACK)
            Debug.Log("Sending ACK in server");
        channel.Send(packet, clientEndpoint);
        packet.Free();
    }

    private void ApplyClientInput(Actions action, Rigidbody rigidbody)
    {
        // 0 = jumps
        // 1 = left
        // 2 = right
        if (action.jump) {
           rigidbody.AddForceAtPosition(Vector3.up * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (action.left) {
           rigidbody.AddForceAtPosition(Vector3.left * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (action.right) {
           rigidbody.AddForceAtPosition(Vector3.right * 5, Vector3.zero, ForceMode.Impulse);
        }
    }

    private void SendSnapshot()
    {    
        WorldInfo currentWorldInfo = GenerateCurrentWorldInfo();
        foreach (var clientId in clients.Keys)
        {
            //serialize
            var packet = Packet.Obtain();
            packetNumber += 1;
            packet.buffer.PutInt((int) PacketType.SNAPSHOT);
            CubeEntity cubeEntity = new CubeEntity(clientsCubes[clientId]);
            Snapshot currentSnapshot = new Snapshot(packetNumber, cubeEntity, currentWorldInfo);
            currentSnapshot.Serialize(packet.buffer);
            packet.buffer.Flush();
            //Debug.Log("Sending snapshot to client " + clientId + "in " + clients[clientId].ipEndPoint.Address + ":" + clients[clientId].ipEndPoint.Port);
            //Debug.Log("Packet " + packet.buffer.GetAvailableByteCount());
            channel.Send(packet, clients[clientId].ipEndPoint);
            packet.Free();
        }  
    }

    private WorldInfo GenerateCurrentWorldInfo()
     {
        WorldInfo currentWorldInfo = new WorldInfo();
        foreach (var clientId in clientsCubes.Keys)
        {
            CubeEntity clientEntity = new CubeEntity(clientsCubes[clientId]);
            currentWorldInfo.addPlayer(clientId, clientEntity);
        }

        return currentWorldInfo;
     }

    private void JoinPlayer(Packet packet)
    {
        int clientId = packet.buffer.GetInt();
        IPEndPoint endPoint = packet.fromEndPoint;
        //Debug.Log("Client with id " + clientId + " and endpoint " + endPoint.Address + endPoint.Port + " was added");
        var last = clients.Keys.Count + 1;
        ClientInfo clientInfo = new ClientInfo(clientId, endPoint);
        clients.Add(last, clientInfo);
        SendACK(last, endPoint, (int)PacketType.PLAYER_JOINED_GAME);
        AddPlayerToWorld(clientId);
    }

    private void AddPlayerToWorld(int clientId)
    {
        float xPosition = Random.Range(-4f, 4f);
        float yPosition = 1f;
        float zPosition = Random.Range(-4f, 4f);
        Vector3 position = new Vector3(xPosition, yPosition, zPosition);
        Quaternion rotation = Quaternion.Euler(Vector3.zero);
        GameObject newCube = GameObject.Instantiate(serverPrefab, position, rotation);
        clientsCubes[clientId] = newCube;
        //Send Broadcast
        BroadcastNewPlayer(clientId, position, rotation.eulerAngles);
    }

    private void BroadcastNewPlayer(int newPlayerId, Vector3 position, Vector3 rotation)
    {
        CubeEntity newPlayer = new CubeEntity(clientsCubes[newPlayerId], position, rotation);
        foreach (var id in clients.Keys)
        {
            IPEndPoint clientEndpoint = clients[id].ipEndPoint;
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) PacketType.NEW_PLAYER_BROADCAST);
            NewPlayerBroadcastEvent newPlayerEvent = new NewPlayerBroadcastEvent(newPlayerId, newPlayer, serverTime, id);
            newPlayerEvent.Serialize(packet.buffer);
            packet.buffer.Flush();
            //Debug.Log("Sending broadcast to playerId  " + id + "with port " + clients[id].ipEndPoint.Port);
            channel.Send(packet, clientEndpoint);
            newPlayerBroadcastEvents.Add(newPlayerEvent);
        }
    }
    
    private void ClientReceivedNewPlayerBroadcast(Packet packet)
    {
        int newPlayerId = packet.buffer.GetInt();
        int clientId = packet.buffer.GetInt();
        int toRemove = -1;
        for (int i = 0; i < newPlayerBroadcastEvents.Count; i++)
        {
            if (newPlayerBroadcastEvents[i].playerId == newPlayerId &&
                newPlayerBroadcastEvents[i].destinationId == clientId)
            {
                toRemove = i;
                break;
            }
        }

        if (toRemove != -1)
            newPlayerBroadcastEvents.RemoveAt(toRemove);
    }
}