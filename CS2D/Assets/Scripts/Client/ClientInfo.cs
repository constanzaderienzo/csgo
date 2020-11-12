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
    public int packetNumber;
    public ClientInfo(string username, IPEndPoint endPoint)
    {
        this.username = username;
        ipEndPoint = endPoint;
        inputId = 1;
        life = 100f;
        isDead = false;
        disconnected = false;
        packetNumber = 1;
    }

    public ClientInfo(ClientInfo clientInfo)
    {
        username = clientInfo.username;
        inputId = clientInfo.inputId;
        life = clientInfo.life;
        isDead = clientInfo.isDead;
        disconnected = clientInfo.disconnected;
        packetNumber = clientInfo.packetNumber;
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
        return playerInfo;
    }
}
