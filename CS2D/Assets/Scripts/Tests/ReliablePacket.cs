using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ReliablePacket 
{
    public Packet packet;
    public int packetIndex;
    public float timeout;
    public float sentTime;

    public ReliablePacket(Packet packet, int packetIndex, float timeout, float sentTime) {
        this.packet = packet;
        this.packetIndex = packetIndex;
        this.timeout = timeout;
        this.sentTime = sentTime;
    }

    public bool CheckIfExpired(float currentTime)
    {
        // Debug.Log(currentTime >= (sentTime + timeout));
        if(currentTime >= (sentTime + timeout))
            return true;
        return false;
    }
    
}