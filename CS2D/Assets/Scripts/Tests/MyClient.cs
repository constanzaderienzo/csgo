using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MyClient {

    private GameObject playerPrefab;
    private Channel channel;
    private Channel inputChannel;
    private Channel ackChannel;

    List<Snapshot> interpolationBuffer = new List<Snapshot>();
    public int requiredSnapshots = 3;
    private bool clientPlaying = false;
    private float clientTime = 0f;
    private float packetsTime = 0f;
    private int pps;
    private List<Actions> clientActions;
    private List<ReliablePacket> packetsToSend = new List<ReliablePacket>();
    [SerializeField] int inputIndex;    
    private Dictionary<int, GameObject> players;
    private int id;
    private int lastRemoved;


    public MyClient(GameObject playerPrefab, Channel channel, Channel inputChannel, Channel ackChannel, int pps, int id) {
        this.playerPrefab = playerPrefab;
        this.channel = channel;
        this.inputChannel = inputChannel;
        this.ackChannel = ackChannel;
        this.pps = pps;
        this.players = new Dictionary<int, GameObject>();
        this.id = id;
        this.lastRemoved = 0;
        this.clientActions = new List<Actions>();
    }

    public void UpdateClient() 
    {
        
        packetsTime += Time.deltaTime;

        GetServerACK();
        GetClientInput();
            
        ResendIfExpired();

        var packet = channel.GetPacket();

        if (packet != null) {
            CubeEntity cubeEntity = new CubeEntity(players[id]);
            Snapshot snapshot = new Snapshot(cubeEntity);
            snapshot.Deserialize(packet.buffer);

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

    public void AddClient(int playerId, CubeEntity cubeEntity) 
    {
        if (!players.ContainsKey(playerId))
        {
            if (id == 1)
            {
                if (id == playerId)
                {
                    Debug.Log("Spawning own with id = " + playerId);
                    Spawn(cubeEntity);
                }
                else 
                {
                    Debug.Log("Spawning player with id = " + playerId);
                    SpawnPlayer(playerId, cubeEntity);
                }
            }
        }
        //Send ACK
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
            id,
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
            if(lastRemoved < inputIndex) 
            {
                clientActions.RemoveRange(0, inputIndex - lastRemoved -1);
                lastRemoved = inputIndex;
            }
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


    public void Spawn(CubeEntity clientCube)
    {
            Debug.Log("Spawning own2 clientId = " + id);
            Vector3 position = clientCube.position;
            Quaternion rotation = Quaternion.Euler(clientCube.eulerAngles);
            players[id] = Object.Instantiate(playerPrefab, position, rotation) as GameObject;
    }
    
    public void SpawnPlayer(int playerId, CubeEntity playerCube)
    {
        Vector3 position = playerCube.position;
        Quaternion rotation = Quaternion.Euler(playerCube.eulerAngles);
        GameObject player = GameObject.Instantiate(playerPrefab, position, rotation) as GameObject;
        players[playerId] = player;
    }
}