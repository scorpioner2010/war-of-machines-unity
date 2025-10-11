using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameplayTimerUI : MonoBehaviour
{
    public TMP_Text timerText;
    public static GameplayTimerUI _in;

    private void Awake()
    {
        _in = this;
    }

    public static void SetTime(float time)
    {
        _in.timerText.text = time.ToString();
    }
}
