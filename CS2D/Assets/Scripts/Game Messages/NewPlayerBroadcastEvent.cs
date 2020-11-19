public class NewPlayerBroadcastEvent
{
    public ClientEntity newPlayer;
    public float time;
    public int playerId;
    public int destinationId;
    public int team;
    public NewPlayerBroadcastEvent(int playerId, ClientEntity newPlayer, float time, int destinationId, int team)
    {
        this.playerId = playerId;
        this.newPlayer = newPlayer;
        this.time = time;
        this.destinationId = destinationId;
        this.team = team;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(playerId);
        buffer.PutFloat(time);
        buffer.PutInt(team);
        newPlayer.Serialize(buffer);
    }

    public static NewPlayerBroadcastEvent Deserialize(BitBuffer buffer)
    {
        int playerId = buffer.GetInt();
        float time = buffer.GetFloat();
        int team = buffer.GetInt();
        ClientEntity player = ClientEntity.DeserializeInfo(buffer);
        return new NewPlayerBroadcastEvent(playerId, player, time, -1, team);
    }
}
