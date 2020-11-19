using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClientDeathmatch : MonoBehaviour{
    
    [SerializeField]
    private GameObject playerPrefab;
    [SerializeField]
    private GameObject otherPlayerPrefab;
    private readonly GameObject conciliateGameObject;
    [SerializeField]
    private GameObject playerUIPrefab;
    private GameObject playerUIInstance;
    private GameObject deadScreen;
    private Channel channel;
    private IPEndPoint serverEndpoint;
    private List<Snapshot> interpolationBuffer;
    private readonly int requiredSnapshots = 3;
    private float time;
    private bool clientPlaying = false;
    private bool hasReceievedAck = false;
    private float clientTime = 0f;
    private float packetsTime = 0f;
    private readonly int pps = 60;
    private List<Actions> clientActions;
    private readonly Dictionary<int, Actions> appliedActions;
    private List<ReliablePacket> packetsToSend;
    private List<ReliablePacket> shotsToSend;
    private ReliablePacket sentJoinEvent;
    private List<Packet> queuedInputs;
    private int inputIndex;
    private int shotsPacketIndex;
    private Dictionary<int, GameObject> players;
    public int id;
    private int lastRemoved;
    private readonly float runningSpeed = 10.0f;
    private readonly float walkingSpeed = 7.0f;
    private readonly float crouchingSpeed = 5.5f;
    public float gravity = 50.0F;
    private float epsilon;
    private List<Actions> queuedActions;
    private PlayerShoot playerShoot;
    public GameObject exitPanel;
    private bool paused;
    private string username;
    private Animator animator;
    private AudioSource audioSource;
    public AudioClip emptyClip;
    public GameObject bloodSpatter;
    private int hitPlayerId = -1;
    private int delay;
    
    private void Awake()
    {
        Application.targetFrameRate = 60;
        Debug.Log("Awaking");
        delay = 0;
        JoinGameLoad joinGameLoad = GameObject.Find("NetworkManager").GetComponent<JoinGameLoad>();
        deadScreen = GameObject.Find("Died");
        deadScreen.SetActive(false);
        SetUsername(joinGameLoad.username);
        SetId(joinGameLoad.id);
        SetServerEndpoint(joinGameLoad.address);
        epsilon = 0.5f;
        players = new Dictionary<int, GameObject>();
        inputIndex = 0;
        lastRemoved = 0;
        paused = false; 
        clientActions = new List<Actions>();
        interpolationBuffer = new List<Snapshot>();
        packetsToSend = new List<ReliablePacket>();
        shotsToSend = new List<ReliablePacket>();
        queuedInputs = new List<Packet>();
        queuedActions = new List<Actions>();
        channel = new Channel(9000 + id);
        //TODO put joinGameLoadAddress
        serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);
        SendJoinToServer();
    }

    private void SendJoinToServer()
    {
        Packet packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.PLAYER_JOINED_GAME);
        packet.buffer.PutString(username);
        packet.buffer.Flush();
        // In this case packetIndex is not used. 
        sentJoinEvent = new ReliablePacket(packet, id, 1f, time, -1); 
        channel.Send(packet, serverEndpoint);
    }

    private void SetId(int id)
    {
        Debug.Log("Setting id " + id);
        this.id = id;
    }

    public void SetUsername(string username)
    {
        this.username = username;
    }
    
    public void SetServerEndpoint(string address)
    {
        Debug.Log(address);
        serverEndpoint = new IPEndPoint(IPAddress.Parse(address), 9000);
    }

    public void FixedUpdate()
    {
        if (hasReceievedAck)
        {
            //Muestrea
            TakeClientInput();
            
            //Send inputs 
            SendQueuedInputs();
            
            //Apply to player
            ApplyClientInputs();
            
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
            SendReliablePacket(packet, 2f, PacketType.INPUT);
        }
        queuedInputs.RemoveRange(0, queuedInputs.Count);
    }

    public void Update()
    {
        time += Time.fixedDeltaTime;
        ResendPlayerJoinedIfExpired();
        if (hasReceievedAck) 
        {
            packetsTime += Time.fixedDeltaTime;
            ResendIfExpired();         
            SendShots();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            paused = !paused;
            
            if (paused)
            {
                Cursor.lockState = CursorLockMode.None;
                exitPanel.SetActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                exitPanel.SetActive(false);
            }
        }

        Lag();
        
        ProcessPacket();

    }

    private void Lag()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            delay++;
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            delay = delay-- > 0 ? delay-- : 0;
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
                case (int) PacketType.SNAPSHOT:
                    if(hasReceievedAck)
                        GetSnapshot(packet);
                    break;
                case (int) PacketType.ACK:
                    GetServerAck(packet);
                    break;
                case (int) PacketType.PLAYER_JOINED_GAME_ACK:
                    id = packet.buffer.GetInt();
                    sentJoinEvent = null;
                    break;
                case (int) PacketType.PLAYER_JOINED_GAME:
                    InitialSetUp(packet);
                    hasReceievedAck = true;
                    break;
                case (int) PacketType.NEW_PLAYER_BROADCAST:
                    NewPlayerBroadcastEvent newPlayer = NewPlayerBroadcastEvent.Deserialize(packet.buffer);
                    AddClient(newPlayer.playerId, newPlayer.newPlayer);
                    break;
                case (int) PacketType.SHOTS:
                    int shotId = packet.buffer.GetInt();
                    int shooterId = packet.buffer.GetInt();
                    ShotEvent(shotId, shooterId);
                    break;
                case (int) PacketType.KILLFEED_EVENT:
                    string killedUsername = packet.buffer.GetString();
                    string sourceUsername = packet.buffer.GetString();
                    DeathEvent(killedUsername, sourceUsername);
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
        Dictionary<int, GameObject> currentPlayers = WorldInfo.DeserializeSetUp(packet.buffer, otherPlayerPrefab, otherPlayerPrefab, id, players);
        foreach (var player in currentPlayers)
        {
            if (!players.ContainsKey(player.Key))
            {
                Debug.Log("Adding player with id " + player.Key);
                players.Add(player.Key, player.Value);
            }
        }
    }

    private void GetSnapshot(Packet packet)
    {
        Snapshot snapshot = new Snapshot();
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
            clientTime += Time.fixedDeltaTime;
            Interpolate();
            Reconciliation();
        }
    }
    
    private void AddClient(int playerId, ClientEntity playerEntity) 
    {
        if (!players.ContainsKey(playerId))
        {
            SpawnPlayer(playerId, playerEntity);
            playerShoot = players[id].GetComponent<PlayerShoot>();
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

    private void SendReliablePacket(Packet packet, float timeout, PacketType packetType )
    {
        if (packetType == PacketType.INPUT)
        {
            packetsToSend.Add(new ReliablePacket(packet, inputIndex, timeout, packetsTime, id));
        }
        else if (packetType == PacketType.SHOTS)
        {
            shotsToSend.Add(new ReliablePacket(packet, shotsPacketIndex, timeout, packetsTime, id));
        }

        if (delay > 0)
        {
            StartCoroutine(AddLag(packet));
            
        }
        else
        {
            channel.Send(packet, serverEndpoint);
        }
    }

    private IEnumerator AddLag(Packet packet)
    {
        Debug.Log("Waiting for delay");
        yield return new WaitForSeconds	(delay);
        channel.Send(packet, serverEndpoint);
    }
    
    private void Resend(int index) 
    {
        channel.Send(packetsToSend[index].packet, serverEndpoint);
        //TODO check if should be packets time
        packetsToSend[index].sentTime = clientTime;
    }
    
    private void ResendPlayerJoinedIfExpired()
    {
        if (sentJoinEvent != null && sentJoinEvent.CheckIfExpired(time))
        {
            Debug.Log("Resending");
            channel.Send(sentJoinEvent.packet, serverEndpoint);
            sentJoinEvent.sentTime = time;
        }
        
    }
    
    private void SendUnreliablePacket(Packet packet)
    {
        channel.Send(packet, serverEndpoint);
        packet.Free();
    }
    
    private void TakeClientInput() 
    {
        inputIndex += 1;

        var action = new Actions(
            id,
            inputIndex,
            Input.GetKey(KeyCode.Space),
            Input.GetKey(KeyCode.A),
            Input.GetKey(KeyCode.D),
            Input.GetKey(KeyCode.W),
            Input.GetKey(KeyCode.S),
            Input.GetKey(KeyCode.LeftShift),
            Input.GetKey(KeyCode.LeftControl),
            players[id].transform.eulerAngles,
            AnimationState.GetFromAnimator(animator)
        );

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
    
    private void SendShots()
    {
        hitPlayerId = -1;
        PlayerWeapon weapon = players[id].gameObject.transform.GetComponentInChildren<WeaponHolster>().GetSelectedWeapon();
        ChangeAnimation(weapon);
        if (Input.GetMouseButtonDown(0) && !paused)
        {
            if (weapon.clip > 0 || weapon.name == "Pistol")
            {
                hitPlayerId = playerShoot.Shoot(weapon);
                animator.SetBool("Shoot_b", true);
                players[id].gameObject.transform.GetComponentInChildren<MuzzleFlash>().Shoot();
                weapon.Shoot();
            }
            else if (weapon.ammo > 0)
            {
                weapon.Reload();
                if(weapon.clip > 0)
                    StartCoroutine(ReloadThenShoot(weapon));
            }
            else
            {
                AudioSource.PlayClipAtPoint(emptyClip, players[id].transform.position);
            }
        }
        else
        {
            animator.SetBool("Shoot_b", false);
        }

        shotsPacketIndex++;

        var packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.SHOTS);
        packet.buffer.PutInt(shotsPacketIndex);
        packet.buffer.PutInt(id);
        packet.buffer.PutInt(hitPlayerId);
        packet.buffer.PutString(weapon.name);
        packet.buffer.PutFloat(weapon.damage);
        packet.buffer.Flush();
        SendReliablePacket(packet, 2f, PacketType.SHOTS);
    }

    private void ChangeAnimation(PlayerWeapon weapon)
    {
        int weaponType = 0; 
        switch (weapon.name)
        {
            case "Pistol":
                weaponType = 1;
                break;
            case "AK47":
                weaponType = 2; 
                break;
            case "Shotgun":
                weaponType = 4;
                break;
            case "SniperRifle":
                weaponType = 5;
                break;
            case "MAC10":
                weaponType = 7;
                break;
        }
        if(animator.GetInteger("WeaponType_int") != weaponType)
            animator.SetInteger("WeaponType_int", weaponType);
        
    }
    
    private IEnumerator ReloadThenShoot(PlayerWeapon weapon)
    {
        animator.SetBool("Reload_b", true);
        yield return new WaitForSeconds(1.567f);
        animator.SetBool("Reload_b", false);
    }
    
    private void ApplyClientInput(Actions action, CharacterController controller)
    {
        Vector3 direction = new Vector3();
        
        if (action.jump && controller.isGrounded)
        {
            direction += controller.transform.up * (runningSpeed * 10 * Time.fixedDeltaTime);
        }

        if (action.ctrl)
        {
            if (action.left)
            {
                direction += -controller.transform.right * (crouchingSpeed * Time.fixedDeltaTime);
            }
            if (action.right) {
                direction += controller.transform.right * (crouchingSpeed * Time.fixedDeltaTime) ;
            }
            if (action.up) {
                direction += controller.transform.forward * (crouchingSpeed * Time.fixedDeltaTime);
            }
            if (action.down) {
                direction += -controller.transform.forward * (crouchingSpeed * Time.fixedDeltaTime);
            }
        }
        else if (action.shift)
        {
            if (action.left)
            {
                direction += -controller.transform.right * (walkingSpeed * Time.fixedDeltaTime);
            }
            if (action.right) {
                direction += controller.transform.right * (walkingSpeed * Time.fixedDeltaTime) ;
            }
            if (action.up) {
                direction += controller.transform.forward * (walkingSpeed * Time.fixedDeltaTime);
            }
            if (action.down) {
                direction += -controller.transform.forward * (walkingSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            if (action.left)
            {
                direction += -controller.transform.right * (runningSpeed * Time.fixedDeltaTime);
            }
            if (action.right) {
                direction += controller.transform.right * (runningSpeed * Time.fixedDeltaTime) ;
            }
            if (action.up) {
                direction += controller.transform.forward * (runningSpeed * Time.fixedDeltaTime);
            }
            if (action.down) {
                direction += -controller.transform.forward * (runningSpeed * Time.fixedDeltaTime);
            }
        }

        PlaySounds(direction != new Vector3(0f,0f,0f), action.ctrl, action.shift);
        //Animate(direction != new Vector3(0f,0f,0f), action.ctrl, action.shift);

        direction.y -= gravity * Time.fixedDeltaTime;
        controller.Move(direction);
    }

    private void PlaySounds(bool moving, bool crouching, bool walking)
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
    
    private void Animate(bool moving, bool ctrl, bool shift)
    {
        if (ctrl)
        {
            animator.SetFloat("Speed_f", 0f);
            animator.SetBool("Crouch_b", true);
        }
        else if (moving && shift)
        {
            animator.SetFloat("Speed_f", 0.2f);
        }
        else if (moving)
        {
            animator.SetFloat("Speed_f", 0.6f);
        }
        else
        {
            animator.SetBool("Crouch_b", false);
            animator.SetFloat("Speed_f", 0f);
        }
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
    
    private void CheckIfReliablePacketReceived(int index, int ackType)
    {
        int toRemove = -1;
        if (ackType == (int) PacketType.INPUT)
        {
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
        else if (ackType == (int) PacketType.SHOTS)
        {
            
            var sizeAtMoment = shotsToSend.Count;
            for (int i = 0; i < sizeAtMoment; i++)
            {
                if(shotsToSend[i].packetIndex == index)
                {
                    toRemove = i;
                    break;
                }
            }
            if(toRemove != -1)
            {
                shotsToSend.RemoveRange(0, toRemove);
            }
        }
    }
    
    private void GetServerAck(Packet packet) {
        int packetNumber = packet.buffer.GetInt();
        int ackType = packet.buffer.GetInt();
        if (ackType == (int) PacketType.SHOTS)
        {
            CheckIfReliablePacketReceived(packetNumber, ackType);
            return;
        }
        
        int quantity = packetNumber - lastRemoved;
        lastRemoved = packetNumber;
        //Debug.Log("Got packet " + packetNumber);
        while (quantity > 0) 
        {
            //Debug.Log("Removing action with id " + clientActions[0]);
            clientActions.RemoveAt(0);
            quantity--;
        }
        CheckIfReliablePacketReceived(packetNumber, ackType);
        
    }
    
    private void Interpolate() {
        var previousTime = (interpolationBuffer[0]).packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime); 
        
        Snapshot.CreateInterpolatedAndApply(interpolationBuffer[0], interpolationBuffer[1], players, t, this.id);
        
        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }

    private void Reconciliation()
    {
        Snapshot snapshot = interpolationBuffer[interpolationBuffer.Count - 1];
        ClientEntity playerEntity = snapshot.worldInfo.players[id];
        ClientInfo playerInfo = snapshot.worldInfo.playersInfo[id];

        HandlePlayerRespawn(playerInfo);
        PlayerInfoUpdate(playerInfo);
        //DebugConsoleUpdate(playerEntity);
        
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

        if (Vector3.Distance(calculatedPosition, actualPosition) >= epsilon && !playerInfo.isDead) 
        {
            Debug.Log("Had to reconcile");
            players[id].GetComponent<CharacterController>().transform.position = gameObject.GetComponent<CharacterController>().transform.position;
        }

        Destroy(gameObject);
    }

    private void HandlePlayerRespawn(ClientInfo clientInfo)
    {
        if(!clientInfo.isDead && deadScreen.activeSelf)
            deadScreen.SetActive(false);
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
    
    private void DebugConsoleUpdate(ClientEntity clientEntity)
    {
        Text text = GameObject.Find("IdText").GetComponent<Text>();
        Text postText = GameObject.Find("PositionText").GetComponent<Text>();
        text.text = "Id: " + id.ToString();
        postText.text = "Position: " + clientEntity.position.ToString();

    }

    private void SpawnPlayer(int playerId, ClientEntity playerEntity)
    {
        Vector3 position = playerEntity.position;
        Debug.Log("Spawning player " + playerId );
        Quaternion rotation = Quaternion.Euler(playerEntity.eulerAngles);
        GameObject player; 
        if (playerId == id)
        {
            Debug.Log("In own");
            player = Instantiate(playerPrefab, position, rotation);
            animator = player.GetComponent<Animator>();
            audioSource = player.GetComponent<AudioSource>();
        }
        else
        {
            Debug.Log("In other");
            player = Instantiate(otherPlayerPrefab, position, rotation);

        }

        players[playerId] = player;
        player.name = playerId.ToString();
    }

    private void ShotEvent(int shotId, int shooterId)
    {
        // We dont want to see our own blood spatter
        if (shotId != id)
        {
            Vector3 position = players[shotId].transform.position + new Vector3(0f, 2.1f, 0f);
            Instantiate(bloodSpatter, position , Quaternion.identity);
        }
        if (shooterId != id)
        {
            players[shooterId].GetComponentInChildren<MuzzleFlash>().Shoot();
        }
         
    }
    
    private void DeathEvent(string killedUsername, string sourceUsername)
    {
        if (sourceUsername == username)
        {
            Text points = GameObject.Find("KillText").GetComponent<Text>();
            points.text = (Int32.Parse(points.text) + 1).ToString();
        }
        else if (killedUsername == username)
        {
            deadScreen.SetActive(true);
            Text killer = GameObject.Find("PlayerDeadText").GetComponent<Text>();
            killer.text = sourceUsername + " killed you.";
        }
            
        Killfeed killfeed = GameObject.Find("Killfeed").GetComponent<Killfeed>();
        killfeed.OnKill(killedUsername,sourceUsername);
    }

    public void Disconnect()
    {
        Debug.Log("Disconnecting...");
        Packet packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.PLAYER_DISCONNECT);
        // In this case packet index is playerId
        packet.buffer.PutInt(id);
        packet.buffer.Flush();
        channel.Send(packet, serverEndpoint);
        packet.Free();

        
        SceneManager.LoadScene("LobbyCS");
    }

    private void OnDestroy()
    {
        channel.Disconnect();
        Destroy(gameObject);
    }
}