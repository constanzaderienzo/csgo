using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;

    public delegate void OnPlayerKilledCallback(int player, int source);

    public OnPlayerKilledCallback onPlayerKilledCallback;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one gamemanager in scene");
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
