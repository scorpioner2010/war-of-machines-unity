using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Script.Player.UI;
using Game.Scripts.Core.Services;
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

    public class CharacterInit : NetworkBehaviour
    {
        public TankRoot playerRoot;

        private readonly SyncVar<int> _amountPlayersInRoom = new ();
        public readonly SyncVar<string> LoginName = new ("");
        public readonly SyncVar<PlayerType> PlayerType = new(Robots.PlayerType.None);
        

        public UEScene currentScene;

        [Server]
        public void ServerInit(int amountPlayersInRoom, PlayerType playerType, string loginName, UEScene scene)
        {
            currentScene = scene;
            _amountPlayersInRoom.Value = amountPlayersInRoom;
            PlayerType.Value = playerType;
            LoginName.Value = loginName;
        }

        public override void OnStartServer()
        {
        }

        public override void OnStartClient()
        {
            if (IsOwner)
            {
                playerRoot.Init();
                SetNickNameProcess();
            }
        }

        private async void SetNickNameProcess()
        {
            bool isActiveProcess = true;

            while (isActiveProcess)
            {
                await UniTask.Delay(500);

                TankRoot[] players = FindObjectsByType<TankRoot>(FindObjectsSortMode.None);
                
                bool allNicksSet = true;
                foreach (TankRoot root in players)
                {
                    if (string.IsNullOrEmpty(root.characterInit.LoginName.Value))
                    {
                        allNicksSet = false;
                        break;
                    }
                }
                
                if (allNicksSet)
                {
                    Camera cam = CameraSync.In.gameplayCamera;

                    foreach (TankRoot root in players)
                    {
                        if (OwnerId != root.OwnerId)
                        {
                            root.nickNameView.SetCamera(cam);
                            root.nickNameView.SetNick(root.characterInit.LoginName.Value);
                        }
                    }

                    isActiveProcess = false;
                }
            }
        }
    }
}
