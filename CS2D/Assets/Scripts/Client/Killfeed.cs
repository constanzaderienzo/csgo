﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Killfeed : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GameObject killFeedItemPrefab;

    public void OnKill(string player, string source)
    {
        Debug.Log(source + " killed " + player);
        GameObject go = (GameObject) Instantiate(killFeedItemPrefab, this.transform);
        go.GetComponent<KillfeedItem>().Setup(player,source);
    }
}
