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
    public int id;
    public InputField joinAddressInput;
    public InputField joinIdInput;
    public Text errorText;

    private IPEndPoint serverEndpoint;

    private Dictionary<int, MyClient> clients;
    private List<ReliablePacket> sentPlayerJoinEvents;
    public Button joinButton;
    private Channel loadChannel;
    private float sentTime;
    private int retries;
    private bool receivedACK;
    private void Awake()
    {
        joinAddressInput.onEndEdit.AddListener((value) => SetAddress(value));
        joinIdInput.onEndEdit.AddListener((value) => SetUsername(value));
        sentTime = -1f;
        retries = 1;
        errorText.text = "";
        receivedACK = false;
    }

    private void Update()
    {
        time += Time.fixedDeltaTime;
        if (loadChannel != null)
        {
            ReceiveHealthAck();
            CheckIfExpired();
        }

    }

    private void CheckIfExpired()
    {
        if (sentTime > -1f && !receivedACK && time > sentTime + (0.5f * retries))
        {
            Debug.Log("Time expired");
            retries++;
            if (retries > 3)
            {
                errorText.text = "Server unavailable";
                joinButton.interactable = true;
            }
        }
    }

    public void SetAddress(string name)
    {

        address = name;
    }
    
    public void SetUsername(string username)
    {
        this.username = username;
    }

    public void JoinRoom()
    {

        retries = 0;
        sentTime = -1f;
        IPAddress parsedAddress;
        try
        {
            parsedAddress = IPAddress.Parse(address);
            if (!string.IsNullOrEmpty(address) && !string.IsNullOrEmpty(username) )
            {
                Debug.Log("Joining room " + address + " with room for " + roomSize + " with id " + id);
                if (sentTime < 0f)
                {
                    if(loadChannel == null)
                        loadChannel = new Channel(8999);
                    Packet packet = Packet.Obtain();
                    packet.buffer.PutInt((int)PacketType.HEALTH);
                    packet.buffer.PutString(username);
                    packet.buffer.Flush();
                    Debug.Log("Pinging " + parsedAddress);
                    loadChannel.Send(packet, new IPEndPoint(parsedAddress, 9000));
                    sentTime = time;
                    joinButton.interactable = false;

                }
                //Todo show loading
            }
            else
            {
                errorText.text = "Please fill in all required fields";
            }
        }
        catch (Exception e)
        {
            Debug.Log("Invalid address");
            errorText.text = "Invalid address";
            joinButton.interactable = true;
        }
        

    }

    private void ReceiveHealthAck()
    {
        var packet = loadChannel.GetPacket();
        while (packet != null)
        {
            Debug.Log("Received health ack");
            receivedACK = true;
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
                    errorText.text = "Invalid username, please try another one.";
                    joinButton.interactable = true;

                }
            }
            packet.Free();
            packet = loadChannel.GetPacket();
        }
        
    }

}