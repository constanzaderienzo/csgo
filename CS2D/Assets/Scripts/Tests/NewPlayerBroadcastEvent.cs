public class NewPlayerBroadcastEvent
{
    public ClientEntity newPlayer;
    public float time;
    public int playerId;
    public int destinationId;
    public NewPlayerBroadcastEvent(int playerId, ClientEntity newPlayer, float time, int destinationId)
    {
        this.playerId = playerId;
        this.newPlayer = newPlayer;
        this.time = time;
        this.destinationId = destinationId;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(playerId);
        buffer.PutFloat(time);
        newPlayer.Serialize(buffer);
    }

    public static NewPlayerBroadcastEvent Deserialize(BitBuffer buffer)
    {
        int playerId = buffer.GetInt();
        float time = buffer.GetFloat();
        ClientEntity player = new ClientEntity();
        player.Deserialize(buffer);
        return new NewPlayerBroadcastEvent(playerId, player, time, -1);
    }
}
