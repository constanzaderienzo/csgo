using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Snapshot 
{
    public int packetNumber;
    private List<CubeEntity> cubeEntities;

    public Snapshot(int packetNumber, List<CubeEntity> cubeEntities) {
        this.packetNumber = packetNumber;
        this.cubeEntities = cubeEntities;
    }

    public void Serialize(BitBuffer buffer) {
        buffer.PutInt(packetNumber);
        foreach (CubeEntity cubeEntity in cubeEntities)
        {
            cubeEntity.Serialize(buffer);   
        }
    }

    public void Deserialize(BitBuffer buffer) {
        packetNumber = buffer.GetInt();
        foreach (CubeEntity cubeEntity in cubeEntities)
        {
            cubeEntity.Deserialize(buffer);
        }
    }

    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t) {
        List<CubeEntity> interpolatedCubeEntities = new List<CubeEntity>();

        for (int i = 0; i < previous.cubeEntities.Count; i++)
        {
            var cubeEntity = CubeEntity.CreateInterpolated(previous.cubeEntities[i], next.cubeEntities[i], t);
            interpolatedCubeEntities.Add(cubeEntity);
        }
        
        var snapshot = new Snapshot(-1, interpolatedCubeEntities);
        return snapshot;
    }

    public void Apply() {
        
        foreach (CubeEntity cubeEntity in cubeEntities)
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