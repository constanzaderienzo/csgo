using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = UnityEngine.Random;

public class ServerCS : MonoBehaviour {

    [SerializeField]
    private GameObject counterPrefab, terroristPrefab;
    private Channel channel;
    private float accum = 0f;
    private float serverTime = 0f;
    private int packetNumber = 0;
    private int pps;
    private readonly float runningSpeed = 10.0f;
    private readonly float walkingSpeed = 7.0f;
    private readonly float crouchingSpeed = 5.5f;
    public float gravity = 50.0F;
    private Dictionary<int, GameObject> clientsGameObjects;
    private Dictionary<int, ClientInfo> clients;
    private List<NewPlayerBroadcastEvent> newPlayerBroadcastEvents;
    private Dictionary<Actions, GameObject> queuedClientInputs;
    private List<int> counterterrorists;
    private List<int> terrorists;
    private int leftCounterAlive, leftTerrorAlive;
    private List<int> queuedPlayers;
    private float timeToRespawn = 200f;
    private int round;
    private bool isPlaying;
    private int csScore, terrorScore;
    private SpawnSite counterSite, terrorSite;
    private Dictionary<int, int> counterScoreboard, terrorScoreboard;
    private void Awake()
    {
        Application.targetFrameRate = 60;
        channel = new Channel(9000);
        pps = 60;
        clientsGameObjects = new Dictionary<int, GameObject>();
        clients = new Dictionary<int, ClientInfo>();
        newPlayerBroadcastEvents = new List<NewPlayerBroadcastEvent>();
        queuedClientInputs = new Dictionary<Actions, GameObject>();
        counterterrorists = new List<int>();
        terrorists = new List<int>();
        counterScoreboard = new Dictionary<int, int>();
        terrorScoreboard = new Dictionary<int, int>();
        queuedPlayers = new List<int>();
        round = 0;
        csScore = 0;
        terrorScore = 0;
        isPlaying = false;
        counterSite = new SpawnSite(-99f, -77f, 0f, 21f);
        terrorSite = new SpawnSite(42f, 70f, -32f, -22f);
    }

    public void FixedUpdate()
    {
        ApplyClientInputs();
    }

    private void RespawnClients()
    {
        foreach (var entry in clients)
        {
            RespawnPlayer(entry.Key);
        }
    }

    private void RespawnPlayer(int clientId)
    {
        Debug.Log("Respawning player");
        clientsGameObjects[clientId].transform.position = RandomSpawnPosition(clients[clientId].team);
        clientsGameObjects[clientId].SetActive(true);
        clientsGameObjects[clientId].GetComponent<CharacterController>().enabled = true;
        clients[clientId].isDead = false;
        clients[clientId].life = 100f;
    }

    private Vector3 RandomSpawnPosition(int team)
    {
        SpawnSite spawnSite = team == 0 ? counterSite : terrorSite;
        Vector3 newPosition;
        do
        {
            newPosition = new Vector3(Random.Range(spawnSite.minX, spawnSite.maxX), 0f, Random.Range(spawnSite.minZ, spawnSite.maxZ));
        } while (Physics.CheckSphere(newPosition + new Vector3(0f, 1f, 0f), 1));
        
        return newPosition;
    }

    private void ApplyClientInputs()
    {
        foreach (KeyValuePair<Actions, GameObject> entry in queuedClientInputs)
        {
            
            ApplyClientInput(entry.Key, entry.Value);

        }
        
        queuedClientInputs.Clear();
    }

    public void Update() {
        accum += Time.fixedDeltaTime;    
        serverTime += Time.fixedDeltaTime;
        
        ProcessPacket();
        
        float sendRate = (1f/pps);
        if (accum >= sendRate) {
            if (clients.Count >= 2)
            {
                if (!isPlaying && round == 0)
                {
                    leftCounterAlive = counterterrorists.Count;
                    leftTerrorAlive = terrorists.Count;
                    isPlaying = true;
                }
                SendSnapshot();
                accum -= sendRate;
            }
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
                case (int) PacketType.SHOTS:
                    ServerReceivesClientShots(packet);
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
        bool validUsername = true;
        string clientUsername = packet.buffer.GetString();
        foreach (var clientInfo in clients.Values)
        {
            if (clientInfo.username == clientUsername)
            {
                validUsername = false;
                break;
            }
        }
        
        outPacket.buffer.PutBit(validUsername);
        outPacket.buffer.PutInt(clients.Count + 1);
        outPacket.buffer.Flush();
        channel.Send(outPacket, packet.fromEndPoint);
    }

    private void PlayerDisconnected(Packet packet)
    {
        int clientId = packet.buffer.GetInt();
        clients[clientId].disconnected = true;
        clientsGameObjects[clientId].SetActive(false);
        if (counterterrorists.Contains(clientId))
        {
            counterterrorists.Remove(clientId);
        }
        else
        {
            terrorists.Remove(clientId);
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
            if(action.inputIndex > client.inputId && !client.waiting) {
                client.inputId = action.inputIndex;
                queuedClientInputs.Add(action, clientsGameObjects[action.id]); 
            }
        }
        
        if(clientId != -1 && client != null)
            SendAck(client.inputId, clients[clientId].ipEndPoint, (int) PacketType.ACK, (int) PacketType.INPUT);
    }

    private void ServerReceivesClientShots(Packet packet)
    {
        int packetNumber = packet.buffer.GetInt();
        int clientId = packet.buffer.GetInt();
        int hitPlayer = packet.buffer.GetInt();
        string weaponName = packet.buffer.GetString();
        float damageTaken = packet.buffer.GetFloat();
        ChangeWeapon(weaponName, clientId);
        if(hitPlayer != -1 && !clients[clientId].waiting)
            ApplyHit(hitPlayer, clientId, damageTaken);
        
        SendAck(packetNumber, clients[clientId].ipEndPoint, (int) PacketType.ACK, (int) PacketType.SHOTS);
    }

    private void ChangeWeapon(string weaponName, int clientId)
    {
        clientsGameObjects[clientId].GetComponentInChildren<WeaponHolsterServer>().SetWeapon(weaponName);
    }

    private void SendAck(int inputIndex, IPEndPoint clientEndpoint, int ackType, int packetType ) {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(ackType);
        packet.buffer.PutInt(inputIndex);
        packet.buffer.PutInt(packetType);
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
            direction += player.transform.up * (runningSpeed * 10 * Time.fixedDeltaTime);
        }

        if (action.ctrl)
        {
            if (action.left)
            {
                direction += -player.transform.right * (crouchingSpeed * Time.fixedDeltaTime);
            }
            if (action.right) {
                direction += player.transform.right * (crouchingSpeed * Time.fixedDeltaTime) ;
            }
            if (action.up) {
                direction += player.transform.forward * (crouchingSpeed * Time.fixedDeltaTime);
            }
            if (action.down) {
                direction += -player.transform.forward * (crouchingSpeed * Time.fixedDeltaTime);
            }
        }
        else if (action.shift)
        {
            if (action.left)
            {
                direction += -player.transform.right * (walkingSpeed * Time.fixedDeltaTime);
            }
            if (action.right) {
                direction += player.transform.right * (walkingSpeed * Time.fixedDeltaTime) ;
            }
            if (action.up) {
                direction += player.transform.forward * (walkingSpeed * Time.fixedDeltaTime);
            }
            if (action.down) {
                direction += -player.transform.forward * (walkingSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            if (action.left)
            {
                direction += -player.transform.right * (runningSpeed * Time.fixedDeltaTime);
            }
            if (action.right) {
                direction += player.transform.right * (runningSpeed * Time.fixedDeltaTime) ;
            }
            if (action.up) {
                direction += player.transform.forward * (runningSpeed * Time.fixedDeltaTime);
            }
            if (action.down) {
                direction += -player.transform.forward * (runningSpeed * Time.fixedDeltaTime);
            }
        }

        action.animationState.SetToAnimator(player.GetComponent<Animator>());
        PlaySounds(direction != new Vector3(0f,0f,0f), action.ctrl, action.shift, player.GetComponent<AudioSource>());
        direction.y -= gravity * Time.fixedDeltaTime;
        controller.Move(direction);
        
    }
    
    private void PlaySounds(bool moving, bool crouching, bool walking, AudioSource audioSource)
    {
        if (moving && crouching)
        {
            audioSource.volume = 0f;
            if(!audioSource.isPlaying)
                audioSource.Play();
        }
        else if (moving && walking)
        {
            audioSource.volume = 0.5f;
            if(!audioSource.isPlaying)
                audioSource.Play();        
        }
        else if (moving)
        {
            if(!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            audioSource.Pause();
        }
    }
    
    private void ApplyHit(int actionHitPlayerId, int sourceId, float damage)
    {
        clients[actionHitPlayerId].life -= damage;
        
        if (clients[actionHitPlayerId].life <= 0f)
        {
            clients[actionHitPlayerId].isDead = true;
            clientsGameObjects[actionHitPlayerId].SetActive(false);
            clientsGameObjects[actionHitPlayerId].GetComponent<CharacterController>().enabled = false;
            clients[actionHitPlayerId].timeToRespawn = timeToRespawn;
            AddToScoreboard(actionHitPlayerId, sourceId);
            SendKillfeedEvent(clients[actionHitPlayerId].username, clients[sourceId].username);
            
            if (counterterrorists.Contains(actionHitPlayerId))
            {
                leftCounterAlive--;
                if (leftCounterAlive == 0)
                {
                    terrorScore++;
                    StartNewRound(1);
                }
            }
            else
            {
                leftTerrorAlive--;
                Debug.Log("Terror " + leftTerrorAlive);
                if (leftTerrorAlive == 0)
                {
                    csScore++;
                    StartNewRound(0);
                }
            }
            
        }
        else
        {
            SendShotEvent(actionHitPlayerId, sourceId);
        }
    }

    private void AddToScoreboard(int killed, int killer)
    {
        int teamKilled = clients[killed].team;
        if (counterterrorists.Contains(killer))
        {
            // Substracts one as he killled teammate
            if (teamKilled == 0)
            {
                counterScoreboard[killer]--;
            }
            else
            {
                counterScoreboard[killer]++;
            }
        }
        else
        {
            // Substracts one as he killled teammate
            if (teamKilled == 1)
            {
                terrorScoreboard[killer]--;
            }
            else
            {
                terrorScoreboard[killer]++;
            }
        }
    }

    private void StartNewRound(int team)
    {
        isPlaying = false;
        round++;
        if (csScore == 2 || terrorScore == 2 )
        {
            SendGameWonEvent(team);
            DisconnectClients();
        }
        else
        {
            foreach (var clientId in queuedPlayers)
            {
                AddPlayerToWorld(clientId);
                clients[clientId].waiting = false;
                SendWaitOver(clientId);
            }
            queuedPlayers.Clear();
            RespawnClients();
            SendRoundWonEvent();
            leftCounterAlive = counterterrorists.Count;
            leftTerrorAlive = terrorists.Count;
            isPlaying = true;
        }
    }

    private void SendWaitOver(int clientId)
    {
        var packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.WAIT_OVER);
        packet.buffer.Flush();
        channel.Send(packet, clients[clientId].ipEndPoint);
    }

    private void SendRoundWonEvent()
    {
        foreach (var id in clients.Keys)
        {
            if (!clients[id].disconnected)
            {
                IPEndPoint clientEndpoint = clients[id].ipEndPoint;
                var packet = Packet.Obtain();
                packet.buffer.PutInt((int) PacketType.ROUND_WON);
                packet.buffer.PutInt(csScore);
                packet.buffer.PutInt(terrorScore);
                packet.buffer.Flush();
                channel.Send(packet, clientEndpoint);
            }
        }
    }

    private void DisconnectClients()
    {
        clientsGameObjects.Clear();
        clients.Clear();
        newPlayerBroadcastEvents.Clear();
        queuedClientInputs.Clear();
        counterterrorists.Clear();
        terrorists.Clear();
        queuedPlayers.Clear();
        round = 0;
        csScore = 0;
        terrorScore = 0;
        isPlaying = false;
    }
    
    private void SendGameWonEvent(int team)
    {
        Scoreboard scoreboard = new Scoreboard(counterScoreboard, terrorScoreboard);
        foreach (var id in clients.Keys)
        {
            if (!clients[id].disconnected)
            {
                IPEndPoint clientEndpoint = clients[id].ipEndPoint;
                var packet = Packet.Obtain();
                packet.buffer.PutInt((int) PacketType.GAME_WON);
                packet.buffer.PutInt(team);
                scoreboard.Serialize(packet.buffer, clients);
                packet.buffer.Flush();
                channel.Send(packet, clientEndpoint);
            }
        }
    }

    private void SendKillfeedEvent(string killedUsername, string sourceUsername)
    {
        foreach (var id in clients.Keys)
        {
            if (!clients[id].disconnected)
            {
                IPEndPoint clientEndpoint = clients[id].ipEndPoint;
                var packet = Packet.Obtain();
                packet.buffer.PutInt((int) PacketType.KILLFEED_EVENT);
                packet.buffer.PutString(killedUsername);
                packet.buffer.PutString(sourceUsername);
                packet.buffer.Flush();
                //Debug.Log("Sending broadcast to playerId  " + id + "with port " + clients[id].ipEndPoint.Port);
                channel.Send(packet, clientEndpoint);
            }
        }
    }
    
    private void SendShotEvent(int shotId, int shooterId)
    {
        foreach (var id in clients.Keys)
        {
            if (!clients[id].disconnected)
            {
                IPEndPoint clientEndpoint = clients[id].ipEndPoint;
                var packet = Packet.Obtain();
                packet.buffer.PutInt((int) PacketType.SHOTS);
                packet.buffer.PutInt(shotId);
                packet.buffer.PutInt(shooterId);
                packet.buffer.Flush();
                channel.Send(packet, clientEndpoint);
            }
        }
    }

    private void SendSnapshot()
    {    
        WorldInfo currentWorldInfo = GenerateCurrentWorldInfo();
        foreach (var clientId in clients.Keys)
        {
            if (!clients[clientId].disconnected && !clients[clientId].waiting) 
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
        string clientUsername = packet.buffer.GetString();
        int clientId = clients.Count + 1;
        IPEndPoint endPoint = packet.fromEndPoint;
        Debug.Log("Client with id " + clientId + " and endpoint " + endPoint.Address + endPoint.Port + " was added");
        ClientInfo clientInfo;
        if (counterterrorists.Count < terrorists.Count)
        {
            clientInfo = new ClientInfo(clientUsername, endPoint, 0);
            counterterrorists.Add(clientId);
            counterScoreboard.Add(clientId, 0);
        }
        else
        {
            clientInfo = new ClientInfo(clientUsername, endPoint, 1);
            terrorists.Add(clientId);
            terrorScoreboard.Add(clientId, 0);
        }

        clients[clientId] = clientInfo;
        SendJoinedAck(clientId, endPoint, (int)PacketType.PLAYER_JOINED_GAME_ACK, isPlaying);
        if (!isPlaying)
        {
            AddPlayerToWorld(clientId);
            
        }
        else
        {
            queuedPlayers.Add(clientId);
            clients[clientId].waiting = isPlaying;
        }

    }
    
    private void SendJoinedAck(int inputIndex, IPEndPoint clientEndpoint, int ackType, bool waiting ) {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(ackType);
        packet.buffer.PutInt(inputIndex);
        packet.buffer.PutBit(waiting);
        packet.buffer.Flush();
        channel.Send(packet, clientEndpoint);
    }

    private void AddPlayerToWorld(int clientId)
    {
        Vector3 position = RandomSpawnPosition(clients[clientId].team);
        Quaternion rotation = Quaternion.Euler(Vector3.zero);
        GameObject serverPrefab = clients[clientId].team == 1 ?  terroristPrefab : counterPrefab; 
        GameObject newClient = Instantiate(serverPrefab, position, rotation);
        clientsGameObjects[clientId] = newClient;

        //Send Broadcast
        BroadcastNewPlayer(clientId, position, rotation.eulerAngles, clients[clientId].team);
        
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

    private void BroadcastNewPlayer(int newPlayerId, Vector3 position, Vector3 rotation, int team)
    {
        ClientEntity newPlayer = new ClientEntity(clientsGameObjects[newPlayerId], position, rotation);
        foreach (var id in clients.Keys)
        {
            if (!clients[id].disconnected)
            {
                IPEndPoint clientEndpoint = clients[id].ipEndPoint;
                var packet = Packet.Obtain();
                packet.buffer.PutInt((int) PacketType.NEW_PLAYER_BROADCAST);
                NewPlayerBroadcastEvent newPlayerEvent = new NewPlayerBroadcastEvent(newPlayerId, newPlayer, serverTime, id, team);
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