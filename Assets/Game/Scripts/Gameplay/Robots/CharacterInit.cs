using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.Core.Services;
using Game.Scripts.Player.Data;
using UnityEngine;
using UEScene = UnityEngine.SceneManagement.Scene;

namespace Game.Scripts.Gameplay.Robots
{
    public enum InitValue
    {
        None,
        Player,
        Bot,
    }

    public class CharacterInit : NetworkBehaviour
    {
        public TankRoot playerRoot;

        private readonly SyncVar<int> _amountPlayersInRoom = new ();
        private readonly SyncVar<string> _loginName = new ();
        private readonly SyncVar<int> _ownerId = new ();
        public readonly SyncVar<InitValue> InitValue = new(Robots.InitValue.None);

        public UEScene currentScene;
        private void Awake() => InitValue.OnChange += Init;
        public int NetworkId => _ownerId.Value;

        [Server]
        public void ServerInit(int ownerId, int amountPlayersInRoom, InitValue initValue, string loginName, UEScene scene)
        {
            currentScene = scene;
            _amountPlayersInRoom.Value = amountPlayersInRoom;
            InitValue.Value = initValue;
            _loginName.Value = loginName;
            _ownerId.Value = ownerId;
        }

        private void Init(InitValue prev, InitValue next, bool server)
        {
            if (next == Robots.InitValue.Player)
            {
                if (playerRoot.HasOwnership())
                {
                    playerRoot.Init();
                }
            }
            else if (next == Robots.InitValue.Bot)
            {
                SetParameters("Bot");
            }
        }

        public override void OnStartServer()
        {
        }

        private async void SetParametersProcess()
        {
            bool isAllPlayers = false;

            while (!isAllPlayers)
            {
                TankRoot[] characters = FindObjectsByType<TankRoot>(FindObjectsSortMode.None);

                List<TankRoot> withoutBot = new();

                foreach (TankRoot root in characters)
                {
                    // фільтр за потреби
                }

                if (_amountPlayersInRoom.Value == withoutBot.Count)
                {
                    isAllPlayers = true;
                }

                await UniTask.DelayFrame(1);
            }

            IPlayerClientInfo player = ServiceLocator.Get<IPlayerClientInfo>();
            SetParametersServerRpc(player.Profile.username);
        }

        [ServerRpc(RequireOwnership = true)]
        private void SetParametersServerRpc(string loginName)
        {
            SetParameters(loginName);
            SetParametersObserversRpc(loginName);
        }

        [ObserversRpc]
        private void SetParametersObserversRpc(string loginName)
        {
            SetParameters(loginName);
        }

        private void SetParameters(string loginName)
        {
            gameObject.name = loginName + " (Player)";
            if (InitValue.Value == Robots.InitValue.Bot)
            {
                if (IsServer)
                {
                }
            }
        }
    }
}
