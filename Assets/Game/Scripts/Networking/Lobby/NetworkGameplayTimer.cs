using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.UI.HUD;

namespace Game.Scripts.Networking.Lobby
{
    public class NetworkGameplayTimer : NetworkBehaviour
    {
        public float startTime = 100;
        public NetworkObject networkObject;
        public readonly SyncVar<float> Timer = new(0);
        public ServerRoom serverRoom;

        public override void OnStartServer()
        {
            RunTimer().Forget();
        }

        public override void OnStartClient()
        {
            Timer.OnChange += (prev, next, server) =>
            {
                GameplayTimerDisplay.SetTime(next);
            };
        }

        private async UniTask RunTimer()
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
        }
    }
}
