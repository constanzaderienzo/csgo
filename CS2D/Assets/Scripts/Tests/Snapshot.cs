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

    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t) {
        Dictionary<int, CubeEntity> interpolatedCubeEntities = new Dictionary<int, CubeEntity>();

        for (int i = 0; i < previous.worldInfo.players.Count; i++)
        {
            var previousCube = previous.worldInfo.players[i];
            var nextCube = next.worldInfo.players[i];
            var cubeEntity = CubeEntity.CreateInterpolated(previousCube, nextCube, t);
            interpolatedCubeEntities.Add(i, cubeEntity);
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

    public void SerializeInput(List<List<int>> clientActions, BitBuffer buffer) {
        buffer.PutInt(clientActions[clientActions.Count - 1][0]);
        buffer.PutInt(clientActions[clientActions.Count - 1][1]);
        buffer.PutInt(clientActions[clientActions.Count - 1][2]);
        buffer.PutInt(clientActions[clientActions.Count - 1][3]);
        buffer.PutInt(clientActions[clientActions.Count - 1][4]);
        buffer.PutInt(clientActions[clientActions.Count - 1][5]);
    }

    public void DeserializeInput(BitBuffer buffer) {
        int index = buffer.GetInt();
        bool jumps = buffer.GetInt() == 1 ? true : false;
        bool movesLeft = buffer.GetInt() == 1 ? true : false;
        bool movesRight = buffer.GetInt() == 1 ? true : false;
        bool movesUp = buffer.GetInt() == 1 ? true : false;
        bool movesDown = buffer.GetInt() == 1 ? true : false;
    }
}