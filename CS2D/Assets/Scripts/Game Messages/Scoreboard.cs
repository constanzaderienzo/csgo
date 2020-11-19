using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scoreboard
{

    private Dictionary<int, int> counters, terrors;
    private int counterCount, terrorCount;
    public Dictionary<string, int> countersString, terrorsString;
    public Scoreboard(Dictionary<int, int> counters, Dictionary<int, int> terrors)
    {
        this.counters = counters;
        this.terrors = terrors;
    }

    public Scoreboard(){}

    public void Serialize(BitBuffer buffer, Dictionary<int, ClientInfo> clientInfos)
    {
        buffer.PutInt(counters.Count);
        foreach (var clientId in counters.Keys)
        {
            buffer.PutString(clientInfos[clientId].username);
            buffer.PutInt(counters[clientId]);
        }
        buffer.PutInt(terrors.Count);
        foreach (var clientId in terrors.Keys)
        {
            buffer.PutString(clientInfos[clientId].username);
            buffer.PutInt(terrors[clientId]);
        }
    }

    public static Scoreboard Deserialize(BitBuffer buffer)
    {
        Scoreboard scoreboard = new Scoreboard();
        scoreboard.countersString = new Dictionary<string, int>();
        scoreboard.terrorsString = new Dictionary<string, int>();
        
        scoreboard.counterCount = buffer.GetInt();
        for (int i = 0; i < scoreboard.counterCount; i++)
        {
            scoreboard.countersString.Add(buffer.GetString(), buffer.GetInt());
        }
        scoreboard.terrorCount = buffer.GetInt();
        for (int i = 0; i < scoreboard.terrorCount; i++)
        {
            scoreboard.terrorsString.Add(buffer.GetString(), buffer.GetInt());
        }
        
        return scoreboard;
    }
}
