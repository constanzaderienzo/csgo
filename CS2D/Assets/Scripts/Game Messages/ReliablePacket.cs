using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ReliablePacket 
{
    public Packet packet;
    public int packetIndex;
    public float timeout;
    public float sentTime;
    public int id;

    public ReliablePacket(Packet packet, int packetIndex, float timeout, float sentTime, int id) {
        this.packet = packet;
        this.packetIndex = packetIndex;
        this.timeout = timeout;
        this.sentTime = sentTime;
        this.id = id;
    }

    public bool CheckIfExpired(float currentTime)
    {
        if(currentTime >= (sentTime + timeout))
            return true;
        return false;
    }
    
}