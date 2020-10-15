using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HostGame : MonoBehaviour
{
    [SerializeField]
    private uint roomSize = 6;
    
    private string roomName;

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
    }
    
}

