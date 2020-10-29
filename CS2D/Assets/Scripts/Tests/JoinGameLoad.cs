using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JoinGameLoad : MonoBehaviour
{

    [SerializeField] private uint roomSize = 6;

    private float time;
    public string address;
    public string username;
    private bool hasId;
    public int id;
    public InputField joinAddressInput;
    public InputField joinIdInput;


    private IPEndPoint serverEndpoint;

    private Dictionary<int, MyClient> clients;
    private List<ReliablePacket> sentPlayerJoinEvents;

    private Channel loadChannel;
    private float sentTime;
    private int retries;
    private void Awake()
    {
        joinAddressInput.onEndEdit.AddListener((value) => SetAddress(value));
        joinIdInput.onEndEdit.AddListener((value) => SetUsername(value));
        loadChannel = new Channel(9001);
        sentTime = -1f;
        retries = 1;
    }

    private void Update()
    {
        time += Time.fixedDeltaTime;
        
         ReceiveHealthAck();
         CheckIfExpired();
    }

    private void CheckIfExpired()
    {
        if (sentTime > -1f && time > sentTime + (0.5f * retries))
        {
            Debug.Log("Time expired");
            retries++;
            if (retries > 3)
            {
                Debug.Log("Server unavailable");
            }
        }
    }

    public void SetAddress(string name)
    {
        Debug.Log(name);
        hasId = true;
        address = name;
    }
    
    // public void SetId(string userId)
    // {
    //     Debug.Log(userId);
    //     int idOut;
    //     hasId = Int32.TryParse(userId, out idOut);
    //     id = idOut;
    // }
    public void SetUsername(string username)
    {
        this.username = username;
    }

    public void JoinRoom()
    {

        // TODO check if server exists (maybe with a test connection or health ping)
        // TODO check username 
        IPAddress parsedAddress;
        try
        {
            parsedAddress = IPAddress.Parse(address);
            if (!string.IsNullOrEmpty(address) && hasId )
            {
                Debug.Log("Joining room " + address + " with room for " + roomSize + " with id " + id);
                if (sentTime < 0f)
                {
                    Packet packet = Packet.Obtain();
                    packet.buffer.PutInt((int)PacketType.HEALTH);
                    packet.buffer.PutString(username);
                    packet.buffer.Flush();
                    loadChannel.Send(packet, new IPEndPoint(parsedAddress, 9000));
                }
                //Todo disable button and show loading
                sentTime = time;
            }
        }
        catch (Exception e)
        {
            Debug.Log("Invalid address");
        }
        

    }

    private void ReceiveHealthAck()
    {
        var packet = loadChannel.GetPacket();
        while (packet != null)
        {
            Debug.Log("Received health ack");
            int packetType = packet.buffer.GetInt();
            if (packetType == (int) PacketType.HEALTH)
            {
                bool validUsername = packet.buffer.GetBit();
                if (validUsername)
                {
                    id = packet.buffer.GetInt();
                    loadChannel.Disconnect();
                    SceneManager.LoadScene("ClientScene");
                    sentTime = -1f;
                }
                else
                {
                    Debug.Log("Invalid username");
                }
            }
            packet.Free();
            packet = loadChannel.GetPacket();
        }


    }
    
}