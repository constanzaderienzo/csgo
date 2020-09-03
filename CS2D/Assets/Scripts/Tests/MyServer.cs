using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
public class MyServer {
    [SerializeField] private GameObject cubeServer;
    private Channel channel;
    private Channel inputChannel;
    private Channel ackChannel;

    private float accum = 0f;
    private int lastActionIndex = 0;
    private int packetNumber = 0;
    private int pps;

    private bool connected;

    public MyServer(GameObject cubeServer, Channel channel, Channel inputChannel, Channel ackChannel, int pps) {
        this.cubeServer = cubeServer;
        this.channel = channel;
        this.inputChannel = inputChannel;
        this.ackChannel = ackChannel;
        this.pps = pps;
        this.connected = true;
    }
    private void ServerReceivesClientInput(){
        var packet = inputChannel.GetPacket();

        if (packet != null) {
            var buffer = packet.buffer;
            int actionsCount = buffer.GetInt();

            for (int i = 0; i < actionsCount; i++) {
                Actions action = new Actions();
                action.DeserializeInput(buffer);
                if(action.inputIndex > lastActionIndex) {
                    // mover el cubo
                    lastActionIndex = action.inputIndex;
                    var cubeEntity = new CubeEntity(cubeServer);
                    if(action.connected)
                        this.connected = !connected;
                    cubeEntity.ApplyClientInput(action);
                }
            }
            // SendACK(lastActionIndex);
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
        if(connected) 
        {
            // If we want to send 10 pckts persecond we need a sendRate of 1/10
            float sendRate = (1f/pps);
            if (accum >= sendRate) {
                packetNumber += 1;
                //serialize
                var packet = Packet.Obtain();
                var cubeEntity = new CubeEntity(cubeServer);
                var snapshot = new Snapshot(packetNumber, cubeEntity);
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

}