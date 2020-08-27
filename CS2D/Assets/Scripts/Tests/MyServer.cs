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


    public MyServer(GameObject cubeServer, Channel channel, Channel inputChannel, Channel ackChannel, int pps) {
        this.cubeServer = cubeServer;
        this.channel = channel;
        this.inputChannel = inputChannel;
        this.ackChannel = ackChannel;
        this.pps = pps;
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
                    cubeEntity.ApplyClientInput(action);
                }
            }
            SendACK(lastActionIndex);
        }
    }

    private void SendACK(int inputIndex) {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(inputIndex);
        packet.buffer.Flush();
        string serverIP = "127.0.0.1";
        int port = 9001;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        Debug.Log("Sending to ack channel a packet with " + packet.buffer.GetAvailableByteCount());
        ackChannel.Send(packet, remoteEp);
        packet.Free();
    }

    public void UpdateServer() {
        accum += Time.deltaTime;
        ServerReceivesClientInput();

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
            Debug.Log("Sending to channel a packet with " + packet.buffer.GetAvailableByteCount());
            channel.Send(packet, remoteEp);
            packet.Free();
            // Restart accum
            accum -= sendRate;
        }
    }

}