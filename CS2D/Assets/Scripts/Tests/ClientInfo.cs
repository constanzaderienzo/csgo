using System.Net;

public class ClientInfo
{
    public int id;
    public IPEndPoint ipEndPoint;
    public int lastInputApplied;

    public ClientInfo(int id, IPEndPoint endPoint)
    {
        this.id = id;
        this.ipEndPoint = endPoint;
        lastInputApplied = 0;
    }
}
