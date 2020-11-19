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
        if (mode == 1)
        {
            SceneManager.LoadScene("ServerCS");
        }
        else
        {
            SceneManager.LoadScene("ServerScene");
        }
    }
}

