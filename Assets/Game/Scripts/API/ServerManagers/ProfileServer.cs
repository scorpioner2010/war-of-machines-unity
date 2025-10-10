using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.Models;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Core.Services;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.MenuController;
using Game.Scripts.Player.Data;
using Game.Scripts.Server;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.MainMenu;
using Game.Scripts.UI.Tree;
using NewDropDude.Script.API.ServerManagers;
using UnityEngine;

namespace Game.Scripts.API.ServerManagers
{
    public class ProfileServer : NetworkBehaviour
    {
        private static ProfileServer _in;
        public static List<PlayerDataAPIInfo> PlayersDataAPIInfos = new();
        public DevelopmentTree  developmentTree;

        private void Awake() => _in = this;
        
        public static PlayerProfile GetProfileByClientId(int clientId)
        {
            foreach (PlayerDataAPIInfo dataAPIInfo in PlayersDataAPIInfos)
            {
                if (dataAPIInfo.ClientId == clientId)
                {
                    return dataAPIInfo.PlayerDataAPI;
                }
            }

            return null;
        }

        public static void UpdateProfile()
        {
            Loading.Show();
            _in.GetProfileServerRpc(_in.ClientManager.Connection.ClientId);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void GetProfileServerRpc(int clientId)
        {
            RequestGetProfile(clientId);
        }

        private async void RequestGetProfile(int clientId)
        {
            (bool isSuccess, string message, PlayerProfile profile) data;
            string token = RegisterServer.GetToken(clientId);
            
            if (ServerSettings.In.isTestMode)
            {
                PlayerProfile profile = new PlayerProfile();
                profile.username = GameplayAssistant.GenerateName(10);
                data = (true, "111",profile);
            }
            else
            {
                data = await PlayersManager.GetMyProfile(token);
            }
            
            NetworkConnection senderConn = ServerManager.Clients[clientId];
            
            AddPlayerDataAPI(data.profile, clientId);
            TargetRpcUpdateProfile(senderConn, data.isSuccess, data.message, data.profile);
        }

        
        private void AddPlayerDataAPI(PlayerProfile data, int clientId)
        {
            PlayersDataAPIInfos.RemoveAll(x => x.ClientId == clientId);
            
            PlayersDataAPIInfos.Add(new PlayerDataAPIInfo
            {
                PlayerDataAPI = data,
                ClientId = clientId,
            });
        }
        
        [TargetRpc]
        private void TargetRpcUpdateProfile(NetworkConnection target, bool success, string errorMessage, PlayerProfile profile)
        {
            Loading.Hide();
            
            if (success)
            {
                IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
                clientInfo.SetPlayerData(profile);
                clientInfo.SetClientId(target.ClientId);
                RobotView.GenerateIcons();
                MainMenu.In.UpdatePlayerInfo(clientInfo.Profile);
                developmentTree.Init();
            }
            else
            {
                Popup.ShowText(errorMessage, Color.red);
            }
        }
    }
}
