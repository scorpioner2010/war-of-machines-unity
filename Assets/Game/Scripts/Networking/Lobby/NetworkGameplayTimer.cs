using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using RAV.Scripts.Gameplay.Battle;
using RAV.Scripts.Server;
using RAV.Scripts.UI.HUD;

namespace RAV.Scripts.Networking.Lobby
{
    public class NetworkGameplayTimer : NetworkBehaviour
    {
        public NetworkObject networkObject;
        public readonly SyncVar<float> Timer = new(0);
        public ServerRoom serverRoom;

        public override void OnStartServer()
        {
            RunTimer().Forget();
        }

        public override void OnStartClient()
        {
            GameplayTimerDisplay.SetTime(Timer.Value);
            Timer.OnChange += (prev, next, server) =>
            {
                GameplayTimerDisplay.SetTime(next);
            };
        }

        private async UniTask RunTimer()
        {
            Timer.Value = ServerSettings.GetMatchScene().matchDurationSeconds;

            while (IsServerInitialized
                && serverRoom != null
                && !serverRoom.isGameFinished
                && BattleCountdownGate.IsServerCountdownActive(serverRoom.roomId))
            {
                await UniTask.Delay(100);
            }

            if (!IsServerInitialized || serverRoom == null || serverRoom.isGameFinished)
            {
                return;
            }

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
