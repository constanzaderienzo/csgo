using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Snapshot 
{
    public int packetNumber;
    public CubeEntity cubeEntity;
    public WorldInfo worldInfo;

    public Snapshot(int packetNumber, WorldInfo worldInfo) {
        this.packetNumber = packetNumber;
        this.worldInfo = worldInfo;
    }

    public Snapshot(int packetNumber, CubeEntity cubeEntity, WorldInfo worldInfo) {
        this.packetNumber = packetNumber;
        this.cubeEntity = cubeEntity;
        this.worldInfo = worldInfo;
    }
    public Snapshot(CubeEntity cubeEntity) {
        this.packetNumber = -1;
        this.cubeEntity = cubeEntity;
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
            bool isDead = false;
            if (previous.worldInfo.playersInfo[playerId].isDead)
            {
                Debug.Log("Player " + playerId + " is dead");
                isDead = true;
            }
            if (playerId != id && !isDead)
            {
                var previousCube = previous.worldInfo.players[playerId];
                var nextCube = next.worldInfo.players[playerId];
                CubeEntity.CreateInterpolatedAndApply(previousCube, nextCube, gameObjects[playerId] ,t);
            }
        }

    }

    public void Apply() {
        foreach (CubeEntity cubeEntity in worldInfo.players.Values)
        {
            cubeEntity.Apply();
        }
    }
}