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
    private float packetsTime = 0f;
    private int pps;
    private List<Actions> clientActions = new List<Actions>();
    private List<ReliablePacket> packetsToSend = new List<ReliablePacket>();
    [SerializeField] int inputIndex;    
    
    private List<Entity> players;
    private string myId;
    private string id;
    private int lastRemoved = 0;


    public MyClient(GameObject cubeClient, Channel channel, Channel inputChannel, Channel ackChannel, int pps) {
        this.cubeClient = cubeClient;
        this.channel = channel;
        this.inputChannel = inputChannel;
        this.ackChannel = ackChannel;
        this.pps = pps;
    }

    public void UpdateClient() 
    {
        
        packetsTime += Time.deltaTime;

        GetServerACK();
        GetClientInput();
            
        ResendIfExpired();

        var packet = channel.GetPacket();

        if (packet != null) {
            List<CubeEntity> cubeEntities = new List<CubeEntity>();
            foreach (Entity entity in players)
            {
                var cubeEntity = new CubeEntity(entity.gameObject, entity.id);   
                cubeEntities.Add(cubeEntity);   
            }
            var snapshot = new Snapshot(-1, cubeEntities);
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

    public void AddClient(Entity entity) 
    {
        myId = entity.id;
        players.Add(entity);
    }
    private void SendReliablePacket(Packet packet, float timeout)
    {
        packetsToSend.Add(new ReliablePacket(packet, clientActions.Count, timeout, packetsTime));
        string serverIP = "127.0.0.1";
        int port = 9001;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        inputChannel.Send(packet, remoteEp);
    }

    private void Resend(int index) 
    {
        string serverIP = "127.0.0.1";
        int port = 9001;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        inputChannel.Send(packetsToSend[index].packet, remoteEp);
        packetsToSend[index].sentTime = clientTime;
    }
    private void SendUnreliablePacket(Packet packet)
    {
        string serverIP = "127.0.0.1";
        int port = 9001;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        inputChannel.Send(packet, remoteEp);
        packet.Free();
    }
    private void GetClientInput() 
    {
        inputIndex += 1;
        var action = new Actions(
            myId,
            inputIndex, 
            Input.GetKeyDown(KeyCode.Space), 
            Input.GetKeyDown(KeyCode.LeftArrow), 
            Input.GetKeyDown(KeyCode.RightArrow), 
            Input.GetKeyDown(KeyCode.D)
        );

        clientActions.Add(action);

        var packet = Packet.Obtain();
        
        packet.buffer.PutInt(clientActions.Count);

        foreach (Actions currentAction in clientActions)
        {
            currentAction.SerializeInput(packet.buffer);
        }
        packet.buffer.Flush();

        SendReliablePacket(packet, 1f);
    }
    private void ResendIfExpired()
    {
        for (int i = 0; i < packetsToSend.Count; i++) 
        {
            if(packetsToSend[i].CheckIfExpired(packetsTime))
            {
                Resend(i);
            }
        }
    }

    private void CheckIfReliablePacketReceived(int index)
    {
        List<int> toRemove = new List<int>();
        for (int i = 0; i < packetsToSend.Count; i++)
        {
            if(packetsToSend[i].packetIndex == index)
            {
                toRemove.Add(i);
            }
        }
        if(toRemove.Count != 0)
        {
            foreach (int i in toRemove)
            {
                packetsToSend[i].packet.Free();
                packetsToSend.RemoveAt(i);
            }
        }
    }
    private void GetServerACK() {
        var packet = ackChannel.GetPacket();

        // Capaz tiene que ser un while
        if (packet != null) {
            int inputIndex = packet.buffer.GetInt();
            clientActions.RemoveRange(0, inputIndex - lastRemoved -1);
            lastRemoved = inputIndex;
            CheckIfReliablePacketReceived(inputIndex);
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