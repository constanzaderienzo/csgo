using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KillfeedItem : MonoBehaviour
{

    [SerializeField] private Text text;

    private float enabledTime = 20f;


    private void FixedUpdate()
    {
        if (enabledTime > 0f)
            enabledTime -= Time.fixedDeltaTime;
        
        if (enabledTime <= 0f)
            Destroy(gameObject);
    }

    public void Setup(int player, int source)
    {
        text.text = "<b>" + source + "</b>" + " killed " + "<i>" + player + "</i>";
    }
    
}
