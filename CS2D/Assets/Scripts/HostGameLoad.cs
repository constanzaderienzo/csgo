using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameLoad : MonoBehaviour
{
    // Deathmatch = 0 
    // CS = 1
    private int mode;

    public void SetMode(int mode)
    {
        Debug.Log("Setting mode " + mode);
        this.mode = mode;
    }

    public void CreateRoom()
    {
        switch (mode)
        {
            case 0:
                Debug.Log("Loading deathmatch server");
                SceneManager.LoadScene("ServerScene");
                break;
            case 1:
                Debug.Log("Loading cs server");
                SceneManager.LoadScene("ServerCS");
                break;
        }
    }
}

