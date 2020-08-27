using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Snapshot 
{
    public int packetNumber;
    private CubeEntity cubeEntity;

    public Snapshot(int packetNumber, CubeEntity cubeEntity) {
        this.packetNumber = packetNumber;
        this.cubeEntity = cubeEntity;
    }

    public void Serialize(BitBuffer buffer) {
        buffer.PutInt(packetNumber);
        cubeEntity.Serialize(buffer);
    }

    public void Deserialize(BitBuffer buffer) {
        packetNumber = buffer.GetInt();
        cubeEntity.Deserialize(buffer);
    }

    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t) {
        var cubeEntity = CubeEntity.CreateInterpolated(previous.cubeEntity, next.cubeEntity, t);
        var snapshot = new Snapshot(-1, cubeEntity);
        return snapshot;
    }

    public void Apply() {
        cubeEntity.Apply();
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