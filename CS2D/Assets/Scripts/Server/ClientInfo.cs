using System.Net;

public class ClientInfo
{
    public string username;
    public IPEndPoint ipEndPoint;
    public int inputId;
    public float life;
    public bool isDead;
    public float timeToRespawn;
    public bool disconnected;
    public bool waiting;
    public int packetNumber;
    /// <summary>
    /// -1 = N/A
    /// Team 0 = Counter Terrorists
    /// Team 1 = Terrorists
    /// </summary>
    public int team;
    public ClientInfo(string username, IPEndPoint endPoint, int team)
    {
        this.username = username;
        ipEndPoint = endPoint;
        inputId = 1;
        life = 100f;
        isDead = false;
        disconnected = false;
        packetNumber = 1;
        this.team = team;
        waiting = false;
    }

    public ClientInfo(ClientInfo clientInfo)
    {
        username = clientInfo.username;
        inputId = clientInfo.inputId;
        life = clientInfo.life;
        isDead = clientInfo.isDead;
        disconnected = clientInfo.disconnected;
        packetNumber = clientInfo.packetNumber;
        team = clientInfo.team;
        waiting = clientInfo.waiting;
    }
    public ClientInfo(){}


    public void Serialize(BitBuffer buffer)
    {
        buffer.PutString(username);
        buffer.PutInt(inputId);
        buffer.PutFloat(life);
        buffer.PutBit(isDead);
        buffer.PutBit(disconnected);
        buffer.PutInt(packetNumber);
        buffer.PutInt(team);
        buffer.PutBit(waiting);
    }

    public static ClientInfo DeserializeInfo(BitBuffer buffer)
    {
        ClientInfo playerInfo = new ClientInfo();
        playerInfo.username = buffer.GetString();
        playerInfo.inputId = buffer.GetInt();
        playerInfo.life = buffer.GetFloat();
        playerInfo.isDead = buffer.GetBit();
        playerInfo.disconnected = buffer.GetBit();
        playerInfo.packetNumber = buffer.GetInt();
        playerInfo.team = buffer.GetInt();
        playerInfo.waiting = buffer.GetBit();
        return playerInfo;
    }
}
