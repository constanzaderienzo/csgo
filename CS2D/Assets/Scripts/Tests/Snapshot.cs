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

    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, Dictionary<int, GameObject> gameObjects, float t) {
        Dictionary<int, CubeEntity> interpolatedCubeEntities = new Dictionary<int, CubeEntity>();

        foreach (var playerId in previous.worldInfo.players.Keys)
        {
            var previousCube = previous.worldInfo.players[playerId];
            var nextCube = next.worldInfo.players[playerId];
            var cubeEntity = CubeEntity.CreateInterpolated(previousCube, nextCube, gameObjects[playerId] ,t);
            interpolatedCubeEntities.Add(playerId, cubeEntity);
        }
        
        WorldInfo interpolatedWorldInfo = new WorldInfo(interpolatedCubeEntities);
        var snapshot = new Snapshot(-1, interpolatedWorldInfo);
        return snapshot;
    }

    public void Apply() {
        foreach (CubeEntity cubeEntity in worldInfo.players.Values)
        {
            cubeEntity.Apply();
        }
    }
}