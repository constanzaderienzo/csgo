using System.Net;

public class ClientInfo
{
    public int id;
    public IPEndPoint ipEndPoint;
    public int lastInputApplied;
    public float life;
    public bool isDead;

    public ClientInfo(int id, IPEndPoint endPoint)
    {
        this.id = id;
        ipEndPoint = endPoint;
        lastInputApplied = 0;
        life = 100f;
        isDead = false;
    }

    public ClientInfo(ClientInfo clientInfo)
    {
        life = clientInfo.life;
        isDead = clientInfo.isDead;
    }
    public ClientInfo(){}


    public void Serialize(BitBuffer buffer)
    {
        buffer.PutFloat(life);
        buffer.PutBit(isDead);
    }

    public static ClientInfo DeserializeInfo(BitBuffer buffer)
    {
        ClientInfo playerInfo = new ClientInfo();
        playerInfo.life = buffer.GetFloat();
        playerInfo.isDead = buffer.GetBit();
        return playerInfo;
    }
}
