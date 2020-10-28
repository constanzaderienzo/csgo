using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = UnityEngine.Random;

public class MyServer : MonoBehaviour {

    [SerializeField]
    private GameObject serverPrefab;
    private Channel channel;
    private float accum = 0f;
    private float serverTime = 0f;
    private int packetNumber = 0;
    private int pps;
    private readonly float speed = 10.0f;
    public float gravity = 50.0F;
    private Dictionary<int, GameObject> clientsGameObjects;
    private Dictionary<int, ClientInfo> clients;
    private List<NewPlayerBroadcastEvent> newPlayerBroadcastEvents;
    private Dictionary<Actions, GameObject> queuedClientInputs;
    private float timeToRespawn = 200f;
    
    private void Awake()
    {
        channel = new Channel(9000);
        pps = 60;
        clientsGameObjects = new Dictionary<int, GameObject>();
        clients = new Dictionary<int, ClientInfo>();
        newPlayerBroadcastEvents = new List<NewPlayerBroadcastEvent>();
        queuedClientInputs = new Dictionary<Actions, GameObject>();
    }

    public void FixedUpdate()
    {
        ApplyClientInputs();
        RespawnDeadClients();
    }

    private void RespawnDeadClients()
    {
        foreach (var entry in clients)
        {
            ClientInfo clientInfo = entry.Value;
            if (clientInfo.isDead)
            {
                if (clientInfo.timeToRespawn <= 0f)
                {
                    RespawnPlayer(entry.Key);
                }
                else
                {
                    clientInfo.timeToRespawn -= 1f;
                }
            }
        }
    }

    private void RespawnPlayer(int clientId)
    {
        //Debug.Log("Respawning player");
        clientsGameObjects[clientId].transform.position = RandomSpawnPosition();
        clientsGameObjects[clientId].SetActive(true);
        clientsGameObjects[clientId].GetComponent<CharacterController>().enabled = false;
        clients[clientId].isDead = false;
        clients[clientId].life = 100f;
    }

    private Vector3 RandomSpawnPosition()
    {
        Vector3 minPosition = new Vector3(-80f, 0f, -23f);
        Vector3 maxPosition = new Vector3(70f,0f, 24f);
        // TODO check if its colliding
        return new Vector3(Random.Range(minPosition.x, maxPosition.x), Random.Range(minPosition.y, maxPosition.y), Random.Range(minPosition.z, maxPosition.z) );
    }

    private void ApplyClientInputs()
    {
        foreach (KeyValuePair<Actions, GameObject> entry in queuedClientInputs)
        {
            
            ApplyClientInput(entry.Key, entry.Value);

        }
        
        queuedClientInputs = new Dictionary<Actions, GameObject>();
    }

    public void Update() {
        accum += Time.fixedDeltaTime;    
        serverTime += Time.fixedDeltaTime;
        
        ProcessPacket();
        
        float sendRate = (1f/pps);
        if (accum >= sendRate) {
            SendSnapshot();
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
                case (int) PacketType.PLAYER_DISCONNECT:
                    PlayerDisconnected(packet);
                    break;
                case (int) PacketType.HEALTH:
                    ReplyHealthPing(packet);
                    break;
                default:
                    Debug.Log("Unrecognized type in server");
                    break;
            }
            packet.Free();
            packet = channel.GetPacket();
        }
    }

    private void ReplyHealthPing(Packet packet)
    {
        Debug.Log("Received health ping " + packet.fromEndPoint);
        var outPacket = Packet.Obtain();
        outPacket.buffer.PutInt((int) PacketType.HEALTH);
        bool validUsername;
        int clientId = packet.buffer.GetInt();
        if (clients.ContainsKey(clientId))
            validUsername = false;
        else
            validUsername = true;
        
        outPacket.buffer.PutBit(validUsername);
        outPacket.buffer.Flush();
        channel.Send(outPacket, packet.fromEndPoint);
    }

    private void PlayerDisconnected(Packet packet)
    {
        int clientId = packet.buffer.GetInt();
        clients[clientId].disconnected = true;
        clientsGameObjects[clientId].SetActive(false);
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
            if(action.inputIndex > client.inputId) {
                client.inputId = action.inputIndex;
                queuedClientInputs.Add(action, clientsGameObjects[action.id]); 
            }
        }
        
        if(clientId != -1 && client != null)
            SendAck(client.inputId, clients[clientId].ipEndPoint, (int) PacketType.ACK);
    }

    private void SendAck(int inputIndex, IPEndPoint clientEndpoint, int ackType) {
        if(ackType == (int) PacketType.PLAYER_JOINED_GAME_ACK)
            Debug.Log("Sending ack ");
        var packet = Packet.Obtain();
        packet.buffer.PutInt(ackType);
        packet.buffer.PutInt(inputIndex);
        packet.buffer.Flush();
        channel.Send(packet, clientEndpoint);
    }

    private void ApplyClientInput(Actions action, GameObject player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        Vector3 direction = new Vector3();
        player.transform.eulerAngles = new Vector3(action.rotationX, action.rotationY, action.rotationZ);

        if (action.jump && controller.isGrounded)
        {
            direction = player.transform.up;
            controller.Move(direction * (speed * 10 * Time.fixedDeltaTime));
        }

        if (action.left)
        {
            direction = -player.transform.right;
            controller.Move(direction * (speed * Time.fixedDeltaTime));
        }

        if (action.right)
        {
            direction = player.transform.right;
            controller.Move(direction * (speed * Time.fixedDeltaTime));
        }

        if (action.up)
        {
            direction = player.transform.forward;
            controller.Move(direction * (speed * Time.fixedDeltaTime));
        }

        if (action.down)
        {
            direction = -player.transform.forward;
            controller.Move(direction * (speed * Time.fixedDeltaTime));
        }

        direction.y -= gravity * Time.fixedDeltaTime;
        controller.Move(direction * Time.fixedDeltaTime);
        
        if(action.hitPlayerId != -1)
            ApplyHit(action.hitPlayerId, action.id);
    }

    private void ApplyHit(int actionHitPlayerId, int sourceId)
    {
        clients[actionHitPlayerId].life -= 10f;
        if (clients[actionHitPlayerId].life <= 0f)
        {
            clients[actionHitPlayerId].isDead = true;
            clientsGameObjects[actionHitPlayerId].SetActive(false);
            clientsGameObjects[actionHitPlayerId].GetComponent<CharacterController>().enabled = false;
            clients[actionHitPlayerId].timeToRespawn = timeToRespawn;
            SendKillfeedEvent(actionHitPlayerId, sourceId);
        }
    }

    private void SendKillfeedEvent(int killedId, int sourceId)
    {
        foreach (var id in clients.Keys)
        {
            if (!clients[id].disconnected)
            {
                IPEndPoint clientEndpoint = clients[id].ipEndPoint;
                var packet = Packet.Obtain();
                packet.buffer.PutInt((int) PacketType.KILLFEED_EVENT);
                packet.buffer.PutInt(killedId);
                packet.buffer.PutInt(sourceId);
                packet.buffer.Flush();
                //Debug.Log("Sending broadcast to playerId  " + id + "with port " + clients[id].ipEndPoint.Port);
                channel.Send(packet, clientEndpoint);
            }
        }
    }

    private void SendSnapshot()
    {    
        WorldInfo currentWorldInfo = GenerateCurrentWorldInfo();
        foreach (var clientId in clients.Keys)
        {
            if (!clients[clientId].disconnected) 
            {
                //serialize
                var packet = Packet.Obtain();
                //packetNumber += 1;
                clients[clientId].packetNumber += 1;
                packet.buffer.PutInt((int) PacketType.SNAPSHOT);
                Snapshot currentSnapshot = new Snapshot(clients[clientId].packetNumber, currentWorldInfo);
                currentSnapshot.Serialize(packet.buffer);
                packet.buffer.Flush();
                channel.Send(packet, clients[clientId].ipEndPoint);
            }
        }  
    }

    private WorldInfo GenerateCurrentWorldInfo()
     {
        WorldInfo currentWorldInfo = new WorldInfo();
        
        foreach (var clientId in clientsGameObjects.Keys)
        {
            ClientEntity clientEntity = new ClientEntity(clientsGameObjects[clientId]);
            ClientInfo clientInfo = new ClientInfo(clients[clientId]);
            currentWorldInfo.AddPlayer(clientId, clientEntity, clientInfo);
        }

        return currentWorldInfo;
     }

    private void JoinPlayer(Packet packet)
    {
        int clientId = packet.buffer.GetInt();
        IPEndPoint endPoint = packet.fromEndPoint;
        Debug.Log("Client with id " + clientId + " and endpoint " + endPoint.Address + endPoint.Port + " was added");
        ClientInfo clientInfo = new ClientInfo(clientId, endPoint);
        clients[clientId] = clientInfo;
        SendAck(clientId, endPoint, (int)PacketType.PLAYER_JOINED_GAME_ACK);
        AddPlayerToWorld(clientId);
    }

    private void AddPlayerToWorld(int clientId)
    {
        float xPosition = Random.Range(-4f, 4f);
        float yPosition = 0f;
        float zPosition = Random.Range(-4f, 4f);
        Vector3 position = new Vector3(xPosition, yPosition, zPosition);
        Quaternion rotation = Quaternion.Euler(Vector3.zero);
        GameObject newClient = Instantiate(serverPrefab, position, rotation);
        clientsGameObjects[clientId] = newClient;
       
        //Send Broadcast
        BroadcastNewPlayer(clientId, position, rotation.eulerAngles);
        
        //Send world info so the player can do initial set up
        SendWorldStatusToNewPlayer(clientId);
    }

    private void SendWorldStatusToNewPlayer(int clientId)
    {
        WorldInfo currentWorldInfo = GenerateCurrentWorldInfo();
        Snapshot currentSnapshot = new Snapshot(1, currentWorldInfo);
        
        var packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.PLAYER_JOINED_GAME);
        currentSnapshot.Serialize(packet.buffer);
        packet.buffer.Flush();
        channel.Send(packet, clients[clientId].ipEndPoint);
    }

    private void BroadcastNewPlayer(int newPlayerId, Vector3 position, Vector3 rotation)
    {
        ClientEntity newPlayer = new ClientEntity(clientsGameObjects[newPlayerId], position, rotation);
        foreach (var id in clients.Keys)
        {
            if (!clients[id].disconnected)
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
    
    private void OnDestroy() {
        channel.Disconnect();
    }
}