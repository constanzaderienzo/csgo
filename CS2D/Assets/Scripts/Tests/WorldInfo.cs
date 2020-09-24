using System.Collections.Generic;

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
            CubeEntity player = CubeEntity.Deserialize(buffer);
            currentPlayers[playerId] = player;
        }
        return new WorldInfo(currentPlayers);
    }
}
