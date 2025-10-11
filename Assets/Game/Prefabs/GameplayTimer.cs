using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.Core.Services;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Player.Data;
using Game.Scripts.UI.Loading;
using UnityEngine;
using UEScene = UnityEngine.SceneManagement.Scene;

public class GameplayTimer : NetworkBehaviour
{
    public float startTime = 100;
    public NetworkObject networkObject;
    public readonly SyncVar<float> Timer = new(0);
    
    public override void OnStartServer()
    {
        TimerStart();
    }

    public override void OnStartClient()
    {
        Timer.OnChange += (prev, next, server) =>
        {
            GameplayTimerUI.SetTime(next);
        };
    }

    private async void TimerStart()
    {
        bool isTimerActive = true;
        Timer.Value = startTime;
        
        while (isTimerActive)
        {
            await UniTask.Delay(1000);
            Timer.Value -= 1;

            if (Timer.Value < 0)
            {
                isTimerActive = false;
            }
        }

        TimeFinish();
    }

    private void TimeFinish()
    {
        TimeFinishObserversRpc();
    }

    [ObserversRpc]
    private void TimeFinishObserversRpc()
    {
        GameplaySpawner.In.ReturnToMainMenu();
        LoadingScreenManager.ShowLoading();
        LoadingScreenManager.HideLoading(); //hide with delay 1 second
    }
}
