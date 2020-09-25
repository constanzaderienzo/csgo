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
    private int ackPort;
    private float accum = 0f;
    private int packetNumber = 0;
    private int pps;
    private Dictionary<int, GameObject> clientsCubes;
    private Dictionary<int, ClientInfo> clients;

    public MyServer(GameObject serverPrefab, Channel channel, int pps) {
        this.serverPrefab = serverPrefab;
        this.channel = channel;
        this.pps = pps;
        this.clientsCubes = new Dictionary<int, GameObject>();
        this.clients = new Dictionary<int, ClientInfo>();
        // clientsCubes.Add(1, new GameObject());
        // string serverIP = "127.0.0.1";
        // int port = 9002;
        // var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        // clients.Add(1, new ClientInfo(1, remoteEp));
    }

    public void UpdateServer() {
        accum += Time.deltaTime;    
        var packet = channel.GetPacket();
        ProcessPacket(packet);
        float sendRate = (1f/pps);
        if (accum >= sendRate) {
            SendWorldInfo();
            // Restart accum
            accum -= sendRate;
        }
    }

    private void ProcessPacket(Packet packet)
    {
        if(packet != null)
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
            }
        }
    }

    private void ServerReceivesClientInput(Packet packet){
        int actionsCount = packet.buffer.GetInt();
        int clientId = -1;
        ClientInfo client = null;
        for (int i = 0; i < actionsCount; i++) {
            Actions action = new Actions();
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
            SendACK(client.lastInputApplied, clientId, (int) PacketType.ACK);

    }

    private void SendACK(int inputIndex, int clientId, int ackType) {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(ackType);
        packet.buffer.PutInt(inputIndex);
        packet.buffer.Flush();
        channel.Send(packet, clients[clientId].ipEndPoint);
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

    private void SendWorldInfo()
    {
        WorldInfo currentWorldInfo = GenerateCurrentWorldInfo();
        foreach (var clientId in clients.Keys)
        {
            //serialize
            var packet = Packet.Obtain();
            packetNumber += 1;
            CubeEntity cubeEntity = new CubeEntity(clientsCubes[clientId]);
            Snapshot currentSnapshot = new Snapshot(packetNumber, cubeEntity, currentWorldInfo);
            currentSnapshot.Serialize(packet.buffer);
            packet.buffer.Flush();
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
        var last = clients.Keys.Count + 1;
        ClientInfo clientInfo = new ClientInfo(clientId, endPoint);
        clients.Add(last, clientInfo);
        //TODO Send ACK 
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
    }
    
    private void ClientReceivedNewPlayerBroadcast(Packet packet)
    {
        int clientId = packet.buffer.GetInt();
        int newPlayerId = packet.buffer.GetInt();
    }
}