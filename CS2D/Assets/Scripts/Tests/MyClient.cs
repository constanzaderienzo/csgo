using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MyClient {

    [SerializeField] private GameObject cubeClient;
    private Channel channel;
    private Channel inputChannel;

    private Channel ackChannel;

    List<Snapshot> interpolationBuffer = new List<Snapshot>();
    public int requiredSnapshots = 3;
    private bool clientPlaying = false;
    private float clientTime = 0f;
    private int pps;
    private List<Actions> clientActions = new List<Actions>();
    [SerializeField] int inputIndex;    


    public MyClient(GameObject cubeClient, Channel channel, Channel inputChannel, Channel ackChannel, int pps) {
        this.cubeClient = cubeClient;
        this.channel = channel;
        this.inputChannel = inputChannel;
        this.ackChannel = ackChannel;
        this.pps = pps;
    }

    public void UpdateClient() {
        
        GetServerACK();
        GetClientInput();

        var packet = channel.GetPacket();

        if (packet != null) {
            var cubeEntity = new CubeEntity(cubeClient);
            var snapshot = new Snapshot(-1, cubeEntity);
            var buffer = packet.buffer;

            snapshot.Deserialize(buffer);

            int size = interpolationBuffer.Count;
            if(size == 0 || snapshot.packetNumber > interpolationBuffer[size - 1].packetNumber) {
                interpolationBuffer.Add(snapshot);
            }
        }

        if (interpolationBuffer.Count >= requiredSnapshots) {
            clientPlaying = true;
        }
        else if (interpolationBuffer.Count <= 1) {
            clientPlaying = false;
        }
        if (clientPlaying) {
            clientTime += Time.deltaTime;
            Interpolate();
        }
    }

    
    private void GetClientInput() {
        
        inputIndex += 1;

        var action = new Actions(inputIndex, Input.GetKeyDown(KeyCode.Space), Input.GetKeyDown(KeyCode.LeftArrow), Input.GetKeyDown(KeyCode.RightArrow));
        clientActions.Add(action);

        var packet = Packet.Obtain();
        
        packet.buffer.PutInt(clientActions.Count);

        foreach (Actions currentAction in clientActions)
        {
            currentAction.SerializeInput(packet.buffer);
        }
        
        packet.buffer.Flush();

        string serverIP = "127.0.0.1";
        int port = 9001;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        Debug.Log("Sending to input channel packet with " + packet.buffer.GetAvailableByteCount());
        inputChannel.Send(packet, remoteEp);
        packet.Free();
    }

    private void GetServerACK() {
        var packet = ackChannel.GetPacket();

        // Capaz tiene que ser un while
        if (packet != null) {
            int inputIndex = packet.buffer.GetInt();
            Debug.Log("Index " + inputIndex);
            Debug.Log("List count" + clientActions.Count);
            clientActions.RemoveRange(0, inputIndex-1);
        }
    }

    private void Interpolate() {
        var previousTime = (interpolationBuffer[0]).packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime); 
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t);
        interpolatedSnapshot.Apply();

        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }

}