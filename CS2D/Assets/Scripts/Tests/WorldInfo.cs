using System.Collections.Generic;
using UnityEngine;

public class WorldInfo
{
    public Dictionary<int, CubeEntity> players;

    public WorldInfo()
    {
        players = new Dictionary<int, CubeEntity>();
    }

    public WorldInfo(Dictionary<int, CubeEntity> players)
    {
        this.players = players;
    }

    public void addPlayer(int playerId, CubeEntity player)
    {
        players[playerId] = player;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(players.Count);
        foreach (var playerId in players.Keys)
        {
            buffer.PutInt(playerId);
            players[playerId].Serialize(buffer);
        }
    }

    public static WorldInfo Deserialize(BitBuffer buffer)
    {
        int quantity = buffer.GetInt();
        Dictionary<int, CubeEntity> currentPlayers = new Dictionary<int, CubeEntity>();
        for (int i = 0; i < quantity; i++)
        {
            int playerId = buffer.GetInt();
            CubeEntity player = CubeEntity.DeserializeInfo(buffer);
            currentPlayers[playerId] = player;
        }
        return new WorldInfo(currentPlayers);
    }

    public static Dictionary<int, GameObject> DeserializeSetUp(BitBuffer packetBuffer, GameObject playerPrefab, int id)
    {
        Dictionary<int, GameObject> currentPlayers = new Dictionary<int, GameObject>();
        int quantity = packetBuffer.GetInt();
        Debug.Log("Quantity " + quantity);
        for (int i = 0; i < quantity; i++)
        {
            int playerId = packetBuffer.GetInt();
            Debug.Log("Player " + playerId);
            CubeEntity playerCube = CubeEntity.DeserializeInfo(packetBuffer);
            if (playerId != id)
            {
                Vector3 position = playerCube.position;
                Quaternion rotation = Quaternion.Euler(playerCube.eulerAngles);
                GameObject player = GameObject.Instantiate(playerPrefab, position, rotation) as GameObject;
                currentPlayers.Add(playerId, player);                
            }
        }

        return currentPlayers;
    }
}
