using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using Game.Scripts.API.Endpoints;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Sessions;
using Game.Scripts.UI.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.API.ServerManagers
{
    public class RegisterServer : NetworkBehaviour
    {
        [SerializeField] private TMP_InputField userLoginInputField;   
        [SerializeField] private TMP_InputField userPasswordInputField;  
        
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton; 
        
        private const string LastLoginName = "LastLogin";
        private const string LastPasswordName = "LastPassword";

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

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            ServerPlayerSessions.Clear();
        }

        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                ServerPlayerSessions.UpsertConnection(connection);
            }
            else if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                ServerPlayerSessions.Remove(connection.ClientId);
            }
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
            
            RequestLoginServerRpc(userLogin, password);
        }
        
        private void RegisterButtonClicked()
        {
            Loading.Show();
            
            string userLogin = userLoginInputField.text.Trim();
            string password = userPasswordInputField.text.Trim();
            
            RequestRegisterServerRpc(userLogin, password);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestLoginServerRpc(string userLogin, string password, NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            RequestLoginAsync(userLogin, password, sender).Forget();
        }

        private async UniTask RequestLoginAsync(string userLogin, string password, NetworkConnection sender)
        {
            (bool isSuccess, string message, string token) result = await RegisterManager.SendLoginRequest(userLogin, password);

            if (result.isSuccess)
            {
                ServerPlayerSessions.SetToken(sender, result.token);
            }
            else
            {
                ServerPlayerSessions.ClearAuth(sender.ClientId);
            }

            TargetLoginResponseRpc(sender, result.isSuccess, result.message);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestRegisterServerRpc(string userLogin, string password, NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            RequestRegisterAsync(userLogin, password, sender).Forget();
        }
        
        private async UniTask RequestRegisterAsync(string userLogin, string password, NetworkConnection sender)
        {
            (bool isSuccess, string message) result = await RegisterManager.SendRegisterRequest(userLogin, password);
            TargetRegisterResponseRpc(sender, result.isSuccess, result.message);
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
