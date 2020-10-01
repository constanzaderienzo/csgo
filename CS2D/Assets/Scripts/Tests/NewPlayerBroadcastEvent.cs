public class NewPlayerBroadcastEvent
{
    public CubeEntity newPlayer;
    public float time;
    public int playerId;
    public int destinationId;
    public NewPlayerBroadcastEvent(int playerId, CubeEntity newPlayer, float time, int destinationId)
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
        CubeEntity player = new CubeEntity(null);
        player.Deserialize(buffer);
        return new NewPlayerBroadcastEvent(playerId, player, time, -1);
    }
}
