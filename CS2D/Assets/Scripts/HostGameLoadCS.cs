using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HostGameLoadCS : MonoBehaviour
{
    [SerializeField]
    private uint roomSize = 6;
    
    private string roomName;
    public InputField roomNameInput;

    public void Awake()
    {
        roomNameInput.onEndEdit.AddListener((value) => SetRoomName(value));
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
        SceneManager.LoadScene("ServerCS");
    }
    
}

