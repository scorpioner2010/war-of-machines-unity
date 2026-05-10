using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Script.Player.UI;
using Game.Scripts.Core.Services;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Player.Data;
using UnityEngine;
using UEScene = UnityEngine.SceneManagement.Scene;

namespace Game.Scripts.Gameplay.Robots
{
    public enum PlayerType
    {
        None,
        Player,
        Bot,
    }

    public class CharacterInit : NetworkBehaviour, IVehicleRootAware
    {
        public VehicleRoot playerRoot;

        private readonly SyncVar<int> _amountPlayersInRoom = new ();
        public readonly SyncVar<string> LoginName = new ("");
        public readonly SyncVar<PlayerType> PlayerType = new(Robots.PlayerType.None);

        public void SetVehicleRoot(VehicleRoot vehicleRoot)
        {
            playerRoot = vehicleRoot;
        }
        public readonly SyncVar<MatchTeam> Team = new(MatchTeam.None);
        

        public UEScene currentScene;

        [Server]
        public void ServerInit(int amountPlayersInRoom, PlayerType playerType, string loginName, MatchTeam team, UEScene scene)
        {
            currentScene = scene;
            _amountPlayersInRoom.Value = amountPlayersInRoom;
            PlayerType.Value = playerType;
            LoginName.Value = loginName;
            Team.Value = team;
        }

        public override void OnStartServer()
        {
        }

        public override void OnStartClient()
        {
            if (IsOwner)
            {
                playerRoot.Init();
                SetNickNameProcessAsync().Forget();
            }
        }

        private async UniTask SetNickNameProcessAsync()
        {
            bool isActiveProcess = true;

            while (isActiveProcess)
            {
                await UniTask.Delay(500);

                VehicleRoot[] players = FindObjectsByType<VehicleRoot>(FindObjectsSortMode.None);
                
                bool allNicksSet = true;
                foreach (VehicleRoot root in players)
                {
                    if (string.IsNullOrEmpty(root.characterInit.LoginName.Value))
                    {
                        allNicksSet = false;
                        break;
                    }
                }
                
                if (allNicksSet)
                {
                    if (CameraSync.In == null || CameraSync.In.gameplayCamera == null)
                    {
                        return;
                    }

                    Camera cam = CameraSync.In.gameplayCamera;

                    foreach (VehicleRoot root in players)
                    {
                        if (OwnerId != root.OwnerId)
                        {
                            root.vehicleHUD.SetCamera(cam);
                            root.vehicleHUD.SetNick(root.characterInit.LoginName.Value);
                        }
                    }

                    isActiveProcess = false;
                }
            }
        }
    }
}
