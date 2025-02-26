using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Snapshot 
{
    public int packetNumber;
    public WorldInfo worldInfo;

    public Snapshot(int packetNumber, WorldInfo worldInfo) {
        this.packetNumber = packetNumber;
        this.worldInfo = worldInfo;
    }

    public Snapshot() {
        this.packetNumber = -1;
        
    }

    public void Serialize(BitBuffer buffer) {
        buffer.PutInt(packetNumber);
        worldInfo.Serialize(buffer);
    }

    public void Deserialize(BitBuffer buffer) {
        packetNumber = buffer.GetInt();
        worldInfo = WorldInfo.Deserialize(buffer);
    }

    public static void CreateInterpolatedAndApply(Snapshot previous, Snapshot next, Dictionary<int, GameObject> gameObjects, float t, int id) {
        
        foreach (var playerId in previous.worldInfo.players.Keys)
        {
            
            bool isDead = previous.worldInfo.playersInfo[playerId].isDead;
            bool disconnected = previous.worldInfo.playersInfo[playerId].disconnected;
            if (isDead || disconnected)
            {
                gameObjects[playerId].SetActive(false);
            }
            else if (!gameObjects[playerId].activeSelf)
            {
                gameObjects[playerId].SetActive(true);
            }
            if (playerId != id && !isDead)
            {
                var previousPlayerEntity = previous.worldInfo.players[playerId];
                var nextPlayerEntity = next.worldInfo.players[playerId];
                ClientEntity.CreateInterpolatedAndApply(previousPlayerEntity, nextPlayerEntity, gameObjects[playerId], t);
            }
        }

    }
}