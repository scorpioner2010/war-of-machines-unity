using System;
using FishNet.Object;
using Cysharp.Threading.Tasks;
using FishNet.Object.Synchronizing;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.UI.Loading;

public class GameplayTimer : NetworkBehaviour
{
    public float startTime = 100;
    public NetworkObject networkObject;
    public readonly SyncVar<float> Timer = new(0);
    public ServerRoom serverRoom;
    
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
        Timer.Value = startTime;
        
        while (Timer.Value >= 0)
        {
            await UniTask.Delay(1000);
            if (serverRoom != null && serverRoom.isGameFinished)
            {
                return;
            }

            Timer.Value -= 1;
        }

        if (IsServerInitialized && GameplaySpawner.In != null && serverRoom != null && !serverRoom.isGameFinished)
        {
            GameplaySpawner.In.HandleTimeExpired(serverRoom);
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
