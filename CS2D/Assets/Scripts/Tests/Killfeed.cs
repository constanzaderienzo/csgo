using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Killfeed : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GameObject killFeedItemPrefab;
    void Start()
    {
        //GameManager.instance.onPlayerKilledCallback += OnKill;
    }

    public void OnKill(int player, int source)
    {
        Debug.Log(source + " killed " + player);
        GameObject go = (GameObject) Instantiate(killFeedItemPrefab, this.transform);
        go.GetComponent<KillfeedItem>().Setup(player,source);
    }
}
