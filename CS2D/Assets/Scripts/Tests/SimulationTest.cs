using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SimulationTest : MonoBehaviour
{
    private Channel channel;
    private Channel inputChannel;
    private Channel ackChannel;

    private MyClient myClient;
    private MyServer myServer;

    private bool connected = true;
    public int pps = 10;

    [SerializeField] private GameObject cubeServer;
    [SerializeField] private GameObject cubeClient;


    // Start is called before the first frame update
    void Start() {
        channel = new Channel(9000);
        inputChannel = new Channel(9001);
        ackChannel = new Channel(9002);
        myClient = new MyClient(cubeClient, channel, inputChannel, ackChannel, pps);
        myServer = new MyServer(cubeServer, channel, inputChannel, ackChannel, pps);
    }

    private void OnDestroy() {
        channel.Disconnect();
        inputChannel.Disconnect();
        ackChannel.Disconnect();
    }

    // Update is called once per frame
    void Update() {
        myServer.UpdateServer();        
        myClient.UpdateClient();
    }

}
