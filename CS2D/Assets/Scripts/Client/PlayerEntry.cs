using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEntry : MonoBehaviour
{
    [SerializeField] private Text playerText, scoreText;

    private float enabledTime = 20f;
    

    public void Setup(string player, int score)
    {
        playerText.text = player;
        scoreText.text = score.ToString();
    }
}
