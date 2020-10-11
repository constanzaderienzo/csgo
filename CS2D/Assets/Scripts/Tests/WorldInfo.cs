using System.Collections.Generic;
using UnityEngine;

public class WorldInfo
{
    public Dictionary<int, CubeEntity> players;
    public Dictionary<int, ClientInfo> playersInfo;

    public WorldInfo()
    {
        players = new Dictionary<int, CubeEntity>();
        playersInfo = new Dictionary<int, ClientInfo>();
    }

    public WorldInfo(Dictionary<int, CubeEntity> players, Dictionary<int, ClientInfo> clientInfos)
    {
        this.players = players;
        playersInfo = clientInfos;
    }

    public void addPlayer(int playerId, CubeEntity player)
    {
        players[playerId] = player;
        playersInfo[playerId] = new ClientInfo(playerId, null);
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(players.Count);
        foreach (var playerId in players.Keys)
        {
            buffer.PutInt(playerId);
            players[playerId].Serialize(buffer);
            playersInfo[playerId].Serialize(buffer);
        }
    }

    public static WorldInfo Deserialize(BitBuffer buffer)
    {
        int quantity = buffer.GetInt();
        Dictionary<int, CubeEntity> currentPlayers = new Dictionary<int, CubeEntity>();
        Dictionary<int, ClientInfo> currentPlayersInfo = new Dictionary<int, ClientInfo>();
        for (int i = 0; i < quantity; i++)
        {
            int playerId = buffer.GetInt();
            CubeEntity player = CubeEntity.DeserializeInfo(buffer);
            ClientInfo playerInfo = ClientInfo.DeserializeInfo(buffer);
            currentPlayers[playerId] = player;
            currentPlayersInfo[playerId] = playerInfo;
        }
        return new WorldInfo(currentPlayers, currentPlayersInfo);
    }

    public static Dictionary<int, GameObject> DeserializeSetUp(BitBuffer packetBuffer, GameObject otherPlayerPrefab, int id, Dictionary<int, GameObject> players)
    {
        Dictionary<int, GameObject> currentPlayers = new Dictionary<int, GameObject>();
        int quantity = packetBuffer.GetInt();
        for (int i = 0; i < quantity; i++)
        {
            int playerId = packetBuffer.GetInt();
            CubeEntity playerCube = CubeEntity.DeserializeInfo(packetBuffer);
            if (playerId != id && !players.ContainsKey(playerId))
            {
                Vector3 position = playerCube.position;
                Quaternion rotation = Quaternion.Euler(playerCube.eulerAngles);
                Debug.Log("Instanciating player " + playerId);
                GameObject player = GameObject.Instantiate(otherPlayerPrefab, position, rotation);
                player.name = playerId.ToString();
                currentPlayers.Add(playerId, player);                
            }
        }

        return currentPlayers;
    }

    public void addPlayer(int clientId, CubeEntity clientEntity, ClientInfo clientInfo)
    {
        players[clientId] = clientEntity;
        playersInfo[clientId] = clientInfo;

    }
}
