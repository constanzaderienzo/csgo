using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
public class MyServer {
    private GameObject serverPrefab;
    private Channel channel;
    private int ackPort;
    private Channel inputChannel;
    private Channel ackChannel;
    private float accum = 0f;
    private Dictionary<int, int> lastActionIndex;
    private int packetNumber = 0;
    private int pps;
    private Dictionary<int, GameObject> clientsCubes;
    private Dictionary<int, ClientInfo> clients;

    public MyServer(GameObject serverPrefab, Channel channel, Channel inputChannel, Channel ackChannel, int pps) {
        this.serverPrefab = serverPrefab;
        this.channel = channel;
        this.inputChannel = inputChannel;
        this.ackChannel = ackChannel;
        this.pps = pps;
        this.clientsCubes = new Dictionary<int, GameObject>();
        this.clients = new Dictionary<int, ClientInfo>();
        // clientsCubes.Add(1, new GameObject());
        // string serverIP = "127.0.0.1";
        // int port = 9002;
        // var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        // clients.Add(1, new ClientInfo(1, remoteEp));
        this.lastActionIndex = new Dictionary<int, int>();
    }
    private void ServerReceivesClientInput(){
        var packet = inputChannel.GetPacket();

        if (packet != null) {
            var buffer = packet.buffer;
            int actionsCount = buffer.GetInt();
            int index = -1;
            for (int i = 0; i < actionsCount; i++) {
                Actions action = new Actions();
                action.DeserializeInput(buffer);
                index = action.id;
                if(!lastActionIndex.ContainsKey(action.id))
                    lastActionIndex.Add(action.id, -1);
                if(action.inputIndex > lastActionIndex[action.id]) {
                    // mover el cubo
                    Debug.Assert(action.id == 1);
                    lastActionIndex[action.id] = action.inputIndex;
                    var rigidBody = clientsCubes[action.id].GetComponent<Rigidbody>();
                    ApplyClientInput(action, rigidBody);
                }
            }
            SendACK(lastActionIndex[index], index);
        }
    }

    private void SendACK(int inputIndex, int clientId) {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(inputIndex);
        packet.buffer.Flush();
        ackChannel.Send(packet, clients[clientId].ipEndPoint);
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

    public void UpdateServer() {
        accum += Time.deltaTime;    
        ServerReceivesClientInput();
        float sendRate = (1f/pps);
        if (accum >= sendRate) {
            SendWorldInfo();
            // Restart accum
            accum -= sendRate;
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
            string serverIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
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
}