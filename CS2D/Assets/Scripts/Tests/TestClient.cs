// using System.Collections;
// using System.Collections.Generic;
// using System.Net;
// using UnityEngine;

// public class TestClient {

//     private Channel channel;
//     private GameObject player;
//     private GameObject playerPrefab;

//     private List<Snapshot> interpolationBuffer;
//     private Dictionary<int, GameObject> players;
//     private int requiredSnapshots = 3;
//     private bool clientPlaying = false;
//     private float clientTime = 0f;
//     private float packetsTime = 0f;
//     private int pps;

//     public TestClient(GameObject cubeClient, int portNumber, int pps) 
//     {
//         channel = new Channel(portNumber);
//         interpolationBuffer = new List<Snapshot>();
//     }

//     public void UpdateClient()
//     {
//         // Read packets
//         // Proccess message
//         var packet = channel.GetPacket();

//     }

//     public void ReadServerMessage()
//     {

//     }

//     private void Interpolate() {
//         var previousTime = (interpolationBuffer[0]).packetNumber * (1f/pps);
//         var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
//         var t =  (clientTime - previousTime) / (nextTime - previousTime); 
//         var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t);
//         interpolatedSnapshot.Apply();

//         if(clientTime > nextTime) {
//             interpolationBuffer.RemoveAt(0);
//         }
//     }

// }