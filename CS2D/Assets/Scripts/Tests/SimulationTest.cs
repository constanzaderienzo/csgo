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

    private int channelPort;
    private int inputPort;
    private int ackPort;

    private Dictionary<int, CubeEntity> clients;

    [SerializeField] private GameObject cubeServer;
    [SerializeField] private GameObject cubePrefab;


    void Awake()
    {
        channelPort = 9000;
        inputPort = 9001;
        ackPort = 9002;
        clients = new Dictionary<int, CubeEntity>();
    }
    // Start is called before the first frame update
    void Start() {
        channel = new Channel(channelPort);
        inputChannel = new Channel(inputPort);
        ackChannel = new Channel(ackPort);
        myClient = new MyClient(cubePrefab, channel, inputChannel, ackChannel, pps, 1);
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
