using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.MenuController;
using Game.Scripts.Server;
using Game.Scripts.UI.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NewDropDude.Script.API.ServerManagers
{
    public class RegisterServer : NetworkBehaviour
    {
        [SerializeField] private TMP_InputField userLoginInputField;   
        [SerializeField] private TMP_InputField userPasswordInputField;  
        
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton; 
        
        private const string LastLoginName = "LastLogin";
        private static readonly string LastPasswordName = "LastPassword";
        public static readonly List<PlayerTokenInfo> PlayerTokenInfo = new();

        public static string LastLogin
        {
            get => PlayerPrefs.GetString(LastLoginName, string.Empty);
            set => PlayerPrefs.SetString(LastLoginName, value);
        }

        public static string LastPassword
        {
            get => PlayerPrefs.GetString(LastPasswordName, string.Empty);
            set => PlayerPrefs.SetString(LastPasswordName, value);
        }

        public static string GetToken(int clientId)
        {
            foreach (PlayerTokenInfo playerTokenInfo in PlayerTokenInfo)
            {
                if (clientId == playerTokenInfo.id)
                {
                    return playerTokenInfo.token;
                }
            }

            return string.Empty;
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();
            
            loginButton.onClick.AddListener(LoginButtonClicked);
            registerButton.onClick.AddListener(RegisterButtonClicked);
            
            userLoginInputField.text = LastLogin;
            userPasswordInputField.text = LastPassword;
            
            MenuManager.OpenMenu(MenuType.Auth);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            loginButton.onClick.RemoveListener(LoginButtonClicked);
            registerButton.onClick.RemoveListener(RegisterButtonClicked);
        }

        private void LoginButtonClicked()
        {
            Loading.Show();
            
            string userLogin = userLoginInputField.text.Trim();
            string password = userPasswordInputField.text.Trim();
            
            RequestLoginServerRpc(userLogin, password, ClientManager.Connection.ClientId);
        }
        
        private void RegisterButtonClicked()
        {
            Loading.Show();
            
            string userLogin = userLoginInputField.text.Trim();
            string password = userPasswordInputField.text.Trim();
            
            RequestRegisterServerRpc(userLogin, password, ClientManager.Connection.ClientId);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestLoginServerRpc(string userLogin, string password, int clientID)
        {
            RequestLogin(userLogin, password, clientID);
        }

        private async void RequestLogin(string userLogin, string password, int clientID)
        {
            (bool isSuccess, string message, string token) result;
            
            if (ServerSettings.In.isTestMode)
            {
                result = (true, "111", "222");
            }
            else
            {
                result = await RegisterManager.SendLoginRequest(userLogin, password);
            }
            
            PlayerTokenInfo existing = PlayerTokenInfo.FirstOrDefault(p => p.id == clientID);
            
            if (existing != null)
            {
                existing.token = result.token;
            }
            else
            {
                PlayerTokenInfo.Add(new PlayerTokenInfo(clientID, result.token));
            }
            
            NetworkConnection senderConn = ServerManager.Clients[clientID];
            TargetLoginResponseRpc(senderConn, result.isSuccess, result.message);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestRegisterServerRpc(string userLogin, string password, int clientID)
        {
            RequestRegister(userLogin, password, clientID);
        }
        
        private async void RequestRegister(string userLogin, string password, int clientID)
        {
            (bool isSuccess, string message) result = await RegisterManager.SendRegisterRequest(userLogin, password);
            
            NetworkConnection senderConn = ServerManager.Clients[clientID];
            TargetRegisterResponseRpc(senderConn, result.isSuccess, result.message);
        }

        [TargetRpc]
        private void TargetLoginResponseRpc(NetworkConnection target, bool success, string errorMessage)
        {
            Loading.Hide();
            
            if (success)
            {
                MenuManager.CloseMenu(MenuType.Auth);
                MenuManager.OpenMenu(MenuType.MainMenu);
            }
            else
            {
                Popup.ShowText(errorMessage, Color.red);
            }
            
            string userLogin = userLoginInputField.text.Trim();
            string password = userPasswordInputField.text.Trim();

            LastLogin = userLogin;
            LastPassword = password;
        }
        
        [TargetRpc]
        private void TargetRegisterResponseRpc(NetworkConnection target, bool success, string errorMessage)
        {
            if (success)
            {
                Popup.ShowText("Success!", Color.green);
            }
            else
            {
                Popup.ShowText(errorMessage, Color.red);
            }
            
            Loading.Hide();
        }
    }
}

[System.Serializable]
public class PlayerTokenInfo
{
    public int id;
    public string token;

    public PlayerTokenInfo(int id, string token)
    {
        this.id = id;
        this.token = token;
    }
}
