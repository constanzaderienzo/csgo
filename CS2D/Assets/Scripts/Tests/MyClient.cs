using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MyClient {

    public enum PacketType
    {
        SNAPSHOT    = 0,
        INPUT       = 1,
        ACK         = 2,
        PLAYER_JOINED_GAME   = 3,
        NEW_PLAYER_BROADCAST  = 4
    }
    private readonly GameObject playerPrefab;
    private readonly Channel channel;
    private readonly IPEndPoint serverEndpoint;
    List<Snapshot> interpolationBuffer = new List<Snapshot>();
    public int requiredSnapshots = 3;
    private bool clientPlaying = false;
    private bool hasReceievedAck = false;
    private float clientTime = 0f;
    private float packetsTime = 0f;
    private int pps;
    private List<Actions> clientActions;
    private List<ReliablePacket> packetsToSend = new List<ReliablePacket>();
    [SerializeField] int inputIndex;    
    private Dictionary<int, GameObject> players;
    public int id;
    private int lastRemoved;


    public MyClient(GameObject playerPrefab, Channel channel, IPEndPoint serverEndpoint, int pps, int id) {
        this.playerPrefab = playerPrefab;
        this.channel = channel;
        this.serverEndpoint = serverEndpoint;
        this.pps = pps;
        this.players = new Dictionary<int, GameObject>();
        this.id = id;
        this.inputIndex = 0;
        this.lastRemoved = 0;
        this.clientActions = new List<Actions>();
    }
    
    public void UpdateClient() 
    {
        
        if (hasReceievedAck)
        {
            packetsTime += Time.deltaTime;
            SendClientInput();
            ResendIfExpired();            
        }
        
        ProcessPacket();

    }
    
    private void ProcessPacket()
    {
        var packet = channel.GetPacket();
        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            switch(packetType)
            {
                case (int) PacketType.SNAPSHOT:
                    if(hasReceievedAck)
                        GetSnapshot(packet);
                    break;
                case (int) PacketType.ACK:
                    GetServerACK(packet);
                    break;
                case (int) PacketType.PLAYER_JOINED_GAME:
                    //TODO remove in prod
                    if (id == 1)
                    {
                        InitialSetUp(packet);
                        hasReceievedAck = true;
                    }
                    break;
                case (int) PacketType.NEW_PLAYER_BROADCAST:
                    NewPlayerBroadcastEvent newPlayer = NewPlayerBroadcastEvent.Deserialize(packet.buffer);
                    AddClient(newPlayer.playerId, newPlayer.newPlayer);
                    break;
                default:
                    Debug.Log("Unrecognized type in client" + packetType);
                    break;
            }
            packet.Free();
            packet = channel.GetPacket();
        }
    }

    private void InitialSetUp(Packet packet)
    {
        Debug.Log("Doing set up for player " + id);
        // Discarding packet number (this is because im reusing the snapshot logic)
        packet.buffer.GetInt(); 
        Dictionary<int, GameObject> currentPlayers = WorldInfo.DeserializeSetUp(packet.buffer, playerPrefab, id, players);
        foreach (var player in currentPlayers)
        {
            if (!players.ContainsKey(player.Key))
            {
                //Debug.Log("Adding player with id " + player.Key);
                players.Add(player.Key, player.Value);

            }
        }
    }

    private void GetSnapshot(Packet packet)
    {
        CubeEntity cubeEntity = new CubeEntity(players[id]);
        Snapshot snapshot = new Snapshot(cubeEntity);
        snapshot.Deserialize(packet.buffer);
        int size = interpolationBuffer.Count;
        if(size == 0 || snapshot.packetNumber > interpolationBuffer[size - 1].packetNumber) {
            interpolationBuffer.Add(snapshot);
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
        //Debug.Log("Player " + id + "received broadcast for player " + playerId);
        if (!players.ContainsKey(playerId))
        {
            //TODO remove in prod
            if(id == 1)
                SpawnPlayer(playerId, cubeEntity);
        }
        //Send ACK
        SendNewPlayerAck(playerId, (int) PacketType.NEW_PLAYER_BROADCAST);
    }

    private void SendNewPlayerAck(int playerId, int newPlayerBroadcast)
    {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(newPlayerBroadcast);
        packet.buffer.PutInt(playerId);
        // Add my id so the server can easily know which client sent it
        packet.buffer.PutInt(id);
        packet.buffer.Flush();
        channel.Send(packet, serverEndpoint);
    }

    private void SendReliablePacket(Packet packet, float timeout)
    {
        packetsToSend.Add(new ReliablePacket(packet, inputIndex, timeout, packetsTime));
        channel.Send(packet, serverEndpoint);
    }
    
    private void Resend(int index) 
    {
        channel.Send(packetsToSend[index].packet, serverEndpoint);
        //TODO check if should be packets time
        packetsToSend[index].sentTime = clientTime;
    }
    
    private void SendUnreliablePacket(Packet packet)
    {
        channel.Send(packet, serverEndpoint);
        packet.Free();
    }
    
    private void SendClientInput() 
    {
        inputIndex += 1;
        var action = new Actions(
            id,
            inputIndex, 
            Input.GetKeyDown(KeyCode.Space), 
            Input.GetKeyDown(KeyCode.LeftArrow), 
            Input.GetKeyDown(KeyCode.RightArrow)
        );

        clientActions.Add(action);
        
        var packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.INPUT);
        packet.buffer.PutInt(clientActions.Count);
        foreach (Actions currentAction in clientActions)
        {
            currentAction.SerializeInput(packet.buffer);
        }
        packet.buffer.Flush();

        SendReliablePacket(packet, 2f);
    }
    
    private void ResendIfExpired()
    {
        for (int i = 0; i < packetsToSend.Count; i++) 
        {
            if(packetsToSend[i].CheckIfExpired(packetsTime))
            {
                Debug.Log("Resending packet " + packetsToSend[i].packetIndex + "...");
                Resend(i);
            }
        }
    }
    
    private void CheckIfReliablePacketReceived(int index)
    {
        int toRemove = -1;
        var sizeAtMoment = packetsToSend.Count;
        for (int i = 0; i < sizeAtMoment; i++)
        {
            if(packetsToSend[i].packetIndex == index)
            {
                toRemove = i;
                break;
            }
        }
        if(toRemove != -1)
        {
            packetsToSend.RemoveAt(toRemove);
        }
    }
    
    private void GetServerACK(Packet packet) {
        int packetNumber = packet.buffer.GetInt();
        int quantity = packetNumber - lastRemoved;
        lastRemoved = packetNumber;
        while (quantity > 0) 
        {
            clientActions.RemoveAt(0);
            quantity--;
        }
        CheckIfReliablePacketReceived(packetNumber);
        
    }
    
    private void Interpolate() {
        var previousTime = (interpolationBuffer[0]).packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime); 
        Snapshot.CreateInterpolatedAndApply(interpolationBuffer[0], interpolationBuffer[1], players, t);

        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }
    
    public void SpawnPlayer(int playerId, CubeEntity playerCube)
    {
        Vector3 position = playerCube.position;
        Quaternion rotation = Quaternion.Euler(playerCube.eulerAngles);
        GameObject player = GameObject.Instantiate(playerPrefab, position, rotation) as GameObject;
        players.Add(playerId, player);
    }

    public Channel GetChannel()
    {
        return channel;
    }
}