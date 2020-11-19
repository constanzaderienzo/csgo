using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreTable : MonoBehaviour
{
    [SerializeField] private GameObject playerEntryPrefab;

    public void SetUp(Dictionary<string, int> players)
    {
        foreach (var player in players)
        {
            GameObject go = (GameObject) Instantiate(playerEntryPrefab, this.transform);
            go.GetComponent<PlayerEntry>().Setup(player.Key, player.Value);
        }

    }
}
