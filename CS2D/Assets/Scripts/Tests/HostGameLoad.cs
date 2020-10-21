using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameLoad : MonoBehaviour
{
    [SerializeField]
    private uint roomSize = 6;
    
    private string roomName;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void SetRoomName(string name)
    {
        roomName = name;
    }

    public void CreateRoom()
    {
        if (!string.IsNullOrEmpty(roomName))
        {
            Debug.Log("Creating room " + roomName + " with room for " + roomSize);
            
        }
        //Load in mode host 
        SceneManager.LoadScene("ServerScene");
    }
    
}

