using System.Collections.Generic;
using UnityEngine;

public class WorldInfo
{
    public Dictionary<int, ClientEntity> players;
    public Dictionary<int, ClientInfo> playersInfo;

    public WorldInfo()
    {
        players = new Dictionary<int, ClientEntity>();
        playersInfo = new Dictionary<int, ClientInfo>();
    }

    public WorldInfo(Dictionary<int, ClientEntity> players, Dictionary<int, ClientInfo> clientInfos)
    {
        this.players = players;
        playersInfo = clientInfos;
    }

    public void addPlayer(int playerId, ClientEntity player)
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
        Dictionary<int, ClientEntity> currentPlayers = new Dictionary<int, ClientEntity>();
        Dictionary<int, ClientInfo> currentPlayersInfo = new Dictionary<int, ClientInfo>();
        for (int i = 0; i < quantity; i++)
        {
            int playerId = buffer.GetInt();
            ClientEntity player = ClientEntity.DeserializeInfo(buffer);
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
            ClientEntity playerEntity = ClientEntity.DeserializeInfo(packetBuffer);
            if (playerId != id && !players.ContainsKey(playerId))
            {
                Vector3 position = playerEntity.position;
                Quaternion rotation = Quaternion.Euler(playerEntity.eulerAngles);
                Debug.Log("Instantiating player " + playerId);
                GameObject player = GameObject.Instantiate(otherPlayerPrefab, position, rotation);
                player.name = playerId.ToString();
                currentPlayers.Add(playerId, player);                
            }
        }

        return currentPlayers;
    }

    public void AddPlayer(int clientId, ClientEntity clientEntity, ClientInfo clientInfo)
    {
        players[clientId] = clientEntity;
        playersInfo[clientId] = clientInfo;
    }
}
