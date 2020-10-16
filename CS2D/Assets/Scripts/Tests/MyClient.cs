using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class MyClient {

    public enum PacketType
    {
        SNAPSHOT    = 0,
        INPUT       = 1,
        ACK         = 2,
        PLAYER_JOINED_GAME   = 3,
        NEW_PLAYER_BROADCAST  = 4,
        KILLFEED_EVENT = 5
    }
    private readonly GameObject playerPrefab;
    private readonly GameObject otherPlayerPrefab;
    private readonly GameObject conciliateGameObject;
    private readonly GameObject playerUIPrefab;
    private GameObject playerUIInstance;
    private readonly Channel channel;
    private readonly IPEndPoint serverEndpoint;
    private readonly List<Snapshot> interpolationBuffer;
    private readonly int requiredSnapshots = 3;
    private bool clientPlaying = false;
    private bool hasReceievedAck = false;
    private float clientTime = 0f;
    private float packetsTime = 0f;
    private readonly int pps;
    private readonly List<Actions> clientActions;
    private readonly Dictionary<int, Actions> appliedActions;
    private readonly List<ReliablePacket> packetsToSend;
    private List<Packet> queuedInputs;
    private int inputIndex;    
    private readonly Dictionary<int, GameObject> players;
    public int id;
    private int lastRemoved;
    private readonly float speed = 10.0f;
    public float gravity = 50.0F;
    private float epsilon;
    private List<Actions> queuedActions;
    private PlayerShoot playerShoot;


    public MyClient(GameObject playerPrefab, GameObject otherPlayerClientPrefab, GameObject playerUIPrefab, Channel channel,
        IPEndPoint serverEndpoint, int pps, int id) {
        this.playerPrefab = playerPrefab;
        otherPlayerPrefab = otherPlayerClientPrefab;
        this.playerUIPrefab = playerUIPrefab;
        this.channel = channel;
        this.serverEndpoint = serverEndpoint;
        this.pps = pps;
        this.id = id;
        epsilon = 0.5f;
        players = new Dictionary<int, GameObject>();
        inputIndex = 0;
        lastRemoved = 0;
        clientActions = new List<Actions>();
        interpolationBuffer = new List<Snapshot>();
        packetsToSend = new List<ReliablePacket>();
        queuedInputs = new List<Packet>();
        queuedActions = new List<Actions>();
    }

    public void FixedUpdate()
    {
        if (hasReceievedAck)
        {
            ApplyClientInputs();
            SendQueuedInputs();
        }
    }

    private void ApplyClientInputs()
    {
        foreach (Actions action in queuedActions)
        {
            ApplyClientInput(action, players[id].GetComponent<CharacterController>());        
        }

        queuedActions.RemoveRange(0, queuedActions.Count);
    }
    
    private void SendQueuedInputs()
    {
        foreach (Packet packet in queuedInputs)
        {
            SendReliablePacket(packet, 2f);
        }
        queuedInputs.RemoveRange(0, queuedInputs.Count);
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
                    GetServerAck(packet);
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
                case (int) PacketType.KILLFEED_EVENT:
                    int killedId = packet.buffer.GetInt();
                    int sourceId = packet.buffer.GetInt();
                    if(id == 1)
                        DeathEvent(killedId, sourceId);
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
        Dictionary<int, GameObject> currentPlayers = WorldInfo.DeserializeSetUp(packet.buffer, otherPlayerPrefab, id, players);
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
        ClientEntity playerEntity = new ClientEntity(players[id]);
        Snapshot snapshot = new Snapshot(playerEntity);
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
            Reconciliation();
        }
    }
    
    private void AddClient(int playerId, ClientEntity playerEntity) 
    {
        //Debug.Log("Player " + id + "received broadcast for player " + playerId);
        if (!players.ContainsKey(playerId))
        {
            //TODO remove in prod
            if (id == 1)
            {
                SpawnPlayer(playerId, playerEntity);
                //Camera.main.GetComponent<CameraFollow>().SetTarget(players[1].transform);
                playerShoot = players[id].GetComponent<PlayerShoot>();

            }
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
        packetsToSend.Add(new ReliablePacket(packet, inputIndex, timeout, packetsTime, id));
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
        int hitPlayerId = -1;
        if (Input.GetMouseButtonDown(0) && id == 1)
        {
            //hitPlayerId = CheckForHits();
            hitPlayerId = playerShoot.Shoot();
            Animator animator = players[id].GetComponent<Animator>();
            //animator.SetBool("Shoot_b", true);
        }
        else if(id == 1)
        {
            //Animator animator = players[id].GetComponent<Animator>();
            //animator.SetBool("Shoot_b", false);
        }
        var action = new Actions(
            id,
            inputIndex, 
            Input.GetKey(KeyCode.Space), 
            Input.GetKey(KeyCode.A), 
            Input.GetKey(KeyCode.D),
            Input.GetKey(KeyCode.W),
            Input.GetKey(KeyCode.S),
            players[id].transform.eulerAngles,
            hitPlayerId
        );

        SeeIfItsColliding();
        queuedActions.Add(action);
        clientActions.Add(action);
        
        var packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.INPUT);
        packet.buffer.PutInt(clientActions.Count);
        foreach (Actions currentAction in clientActions)
        {
            currentAction.SerializeInput(packet.buffer);
        }
        packet.buffer.Flush();
        queuedInputs.Add(packet);
    }

    private void SeeIfItsColliding()
    {
        //throw new NotImplementedException();
    }

    private int CheckForHits()
    {        
        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 8;

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        layerMask = ~layerMask;

        RaycastHit hit;
        Transform transform = players[id].transform;
        
        Vector3 positionWithOffset = transform.position + new Vector3(0f,2.1f,0f);
        Vector3 directionWithOffset = Vector3.forward - new Vector3(0f,0.05f,0f);
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(positionWithOffset, transform.TransformDirection(directionWithOffset), out hit, Mathf.Infinity, layerMask))
        {
            Debug.DrawRay(positionWithOffset, transform.TransformDirection(directionWithOffset) * hit.distance, Color.yellow);
            Debug.Log("Did Hit " + hit.collider.gameObject.name );
            int number;
            if (Int32.TryParse(hit.collider.gameObject.name, out number))
                return number;
            return -1;
        }
        return -1;
    }

    private void ApplyClientInput(Actions action, CharacterController controller)
    {
        Vector3 direction = new Vector3();
        
        if (action.jump && controller.isGrounded)
        {
            direction = controller.transform.up;
            controller.Move(direction * (speed * 10 * Time.fixedDeltaTime)); 
        }
        if (action.left) {
            direction = -controller.transform.right;
            controller.Move(direction * (speed * Time.fixedDeltaTime)); 
        }
        if (action.right) {
            direction = controller.transform.right;
            controller.Move(direction * (speed * Time.fixedDeltaTime)); 
        }
        if (action.up) {
            direction = controller.transform.forward;
            controller.Move(direction * (speed * Time.fixedDeltaTime)); 
        }
        if (action.down) {
            direction = -controller.transform.forward;
            controller.Move(direction * (speed * Time.fixedDeltaTime)); 
        }

        direction.y -= gravity * Time.fixedDeltaTime;
        controller.Move(direction * Time.fixedDeltaTime);
    }
    
    private void ResendIfExpired()
    {
        for (int i = 0; i < packetsToSend.Count; i++) 
        {
            if(packetsToSend[i].CheckIfExpired(packetsTime))
            {
                Debug.Log("Resending packet " + packetsToSend[i].packetIndex + " id " + packetsToSend[i].id);
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
            packetsToSend.RemoveRange(0, toRemove);
        }
    }
    
    private void GetServerAck(Packet packet) {
        int packetNumber = packet.buffer.GetInt();
        int quantity = packetNumber - lastRemoved;
        lastRemoved = packetNumber;
        //Debug.Log("Got packet " + packetNumber);
        while (quantity > 0) 
        {
            //Debug.Log("Removing action with id " + clientActions[0]);
            clientActions.RemoveAt(0);
            quantity--;
        }
        CheckIfReliablePacketReceived(packetNumber);
        
    }
    
    private void Interpolate() {
        var previousTime = (interpolationBuffer[0]).packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime); 
        Snapshot.CreateInterpolatedAndApply(interpolationBuffer[0], interpolationBuffer[1], players, t, id);
        
        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }

    private void Reconciliation()
    {
        Snapshot snapshot = interpolationBuffer[interpolationBuffer.Count - 1];
        ClientEntity playerEntity = snapshot.worldInfo.players[id];
        ClientInfo playerInfo = snapshot.worldInfo.playersInfo[id];
        PlayerInfoUpdate(playerInfo);
        GameObject gameObject = new GameObject();
        gameObject.AddComponent<CharacterController>();
        gameObject.GetComponent<CharacterController>().center = new Vector3(0f, 1.5f, 0f);
        gameObject.GetComponent<CharacterController>().height = 3f;
        gameObject.transform.position = playerEntity.position;
        gameObject.transform.eulerAngles = playerEntity.eulerAngles;
        gameObject.layer = 8; 
        
        for (int i = 0; i < clientActions.Count; i++)
        {
            ApplyClientInput(clientActions[i], gameObject.GetComponent<CharacterController>());
        }

        var calculatedPosition = gameObject.GetComponent<CharacterController>().transform.position;
        var actualPosition = players[id].GetComponent<CharacterController>().transform.position;
        //Debug.Log("Actual " + actualPosition + " calculated " + calculatedPosition);
        if (Vector3.Distance(calculatedPosition, actualPosition) >= epsilon) 
        {
            Debug.Log("Had to reconcile");
            players[id].GetComponent<CharacterController>().transform.position = gameObject.GetComponent<CharacterController>().transform.position;
        }

        GameObject.Destroy(gameObject);
    }
    
    private void PlayerInfoUpdate(ClientInfo clientInfo)
    {
        Text text = GameObject.Find("HealthText").GetComponent<Text>();
        if (clientInfo.life < 20)
        {
            text.text = "<color=red>" + clientInfo.life + "</color>";
        }
        else if (clientInfo.life < 60)
        {
            text.text = "<color=orange>" + clientInfo.life + "</color>";
        }
        else
        {
            text.text = clientInfo.life.ToString();
        }
    }

    private void SpawnPlayer(int playerId, ClientEntity playerEntity)
    {
        Vector3 position = playerEntity.position;
        Debug.Log("Spawning  player at " + playerEntity.position.y);
        Quaternion rotation = Quaternion.Euler(playerEntity.eulerAngles);
        GameObject player; 
        if (playerId == id)
        {
            Debug.Log("In own");
            player = GameObject.Instantiate(playerPrefab, position, rotation);
            playerUIInstance = GameObject.Instantiate(playerUIPrefab);
        }
        else
        {
            player = GameObject.Instantiate(otherPlayerPrefab, position, rotation);

        }
        players.Add(playerId, player);
        player.name = playerId.ToString();
    }

    public Channel GetChannel()
    {
        return channel;
    }

    private void DeathEvent(int killedId, int sourceId)
    {
        if (sourceId == id)
        {
            Text points = GameObject.Find("KillText").GetComponent<Text>();
            points.text = (Int32.Parse(points.text) + 1).ToString();
        }
        Killfeed killfeed = GameObject.Find("Killfeed").GetComponent<Killfeed>();
        killfeed.OnKill(killedId,sourceId);
    }
}