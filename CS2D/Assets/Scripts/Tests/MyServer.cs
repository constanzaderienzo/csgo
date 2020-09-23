using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
public class MyServer {
    [SerializeField] private GameObject cubeServer;
    private Channel channel;

    private int ackPort;
    private Channel inputChannel;
    private Channel ackChannel;

    private float accum = 0f;
    private Dictionary<string, int> lastActionIndex;
    private int packetNumber = 0;
    private int pps;
    List<Entity> players;
    public MyServer(GameObject cubeServer, Channel channel, Channel inputChannel, Channel ackChannel, int pps) {
        this.cubeServer = cubeServer;
        this.channel = channel;
        this.inputChannel = inputChannel;
        this.ackChannel = ackChannel;
        this.pps = pps;
        this.players = new List<Entity>();
        this.lastActionIndex = new Dictionary<string, int>();
    }
    private void ServerReceivesClientInput(){
        var packet = inputChannel.GetPacket();

        if (packet != null) {
            var buffer = packet.buffer;
            int actionsCount = buffer.GetInt();
            string index = null;
            for (int i = 0; i < actionsCount; i++) {
                Actions action = new Actions();
                action.DeserializeInput(buffer);
                index = action.id;
                if(action.inputIndex > lastActionIndex[action.id]) {
                    // mover el cubo
                    lastActionIndex[action.id] = action.inputIndex;
                    GameObject cube = null;
                    foreach (Entity player in players)
                    {
                        if(player.id == action.id){
                            cube = player.gameObject;
                        }   
                    }
                    var cubeEntity = new CubeEntity(cube, action.id);
                    cubeEntity.ApplyClientInput(action);
                }
            }
            SendACK(lastActionIndex[index]);
        }
    }

    private void SendACK(int inputIndex) {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(inputIndex);
        packet.buffer.Flush();
        string serverIP = "127.0.0.1";
        int port = 9002;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        ackChannel.Send(packet, remoteEp);
        packet.Free();
    }

    public void UpdateServer() {
        accum += Time.deltaTime;    
        ServerReceivesClientInput();
        float sendRate = (1f/pps);
        if (accum >= sendRate) {
            packetNumber += 1;
            var packet = Packet.Obtain();
            List<CubeEntity> cubeEntities = new List<CubeEntity>();
            for (int i = 0; i < players.Count; i++)
            {
                var cubeEntity = new CubeEntity(players[i].gameObject, players[i].id);
                cubeEntities.Add(cubeEntity);
            }
            var snapshot = new Snapshot(packetNumber, cubeEntities);
            snapshot.Serialize(packet.buffer);
            packet.buffer.Flush();

            string serverIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet, remoteEp);
            packet.Free();
            // Restart accum
            accum -= sendRate;
        }
    }
}