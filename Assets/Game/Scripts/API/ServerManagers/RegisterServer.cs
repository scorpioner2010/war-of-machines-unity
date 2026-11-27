using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using RAV.Scripts.API.Endpoints;
using RAV.Scripts.Diagnostics;
using RAV.Scripts.MenuController;
using RAV.Scripts.Networking.Sessions;
using RAV.Scripts.Testing;
using RAV.Scripts.UI.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RAV.Scripts.API.ServerManagers
{
    public class RegisterServer : NetworkBehaviour
    {
        [SerializeField] private TMP_InputField userLoginInputField;   
        [SerializeField] private TMP_InputField userPasswordInputField;  
        
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton; 
        
        private const string LastLoginName = "LastLogin";

        public static string LastLogin
        {
            get => PlayerPrefs.GetString(LastLoginName, string.Empty);
            set => PlayerPrefs.SetString(LastLoginName, value);
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
        
        private void Awake()
        {
#if UNITY_EDITOR
            if (ClientVehicleTestMode.IsEnabled)
            {
                return;
            }
#endif

            if (loginButton == null || registerButton == null)
            {
                return;
            }

            loginButton.onClick.AddListener(LoginButtonClicked);
            registerButton.onClick.AddListener(RegisterButtonClicked);
            
            userLoginInputField.text = LastLogin;
            userPasswordInputField.text = string.Empty;
            
        }

        private void OnDestroy()
        {
            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(LoginButtonClicked);
            }

            if (registerButton != null)
            {
                registerButton.onClick.RemoveListener(RegisterButtonClicked);
            }
        }

        private void LoginButtonClicked()
        {
            string userLogin = userLoginInputField.text.Trim();
            string password = userPasswordInputField.text.Trim();

            RequestLoginAsync(userLogin, password).Forget();
        }
        
        private void RegisterButtonClicked()
        {
            string userLogin = userLoginInputField.text.Trim();
            string password = userPasswordInputField.text.Trim();
            RequestRegisterAsync(userLogin, password).Forget();
        }

        private async UniTask RequestLoginAsync(string userLogin, string password)
        {
            using (StandardLoadingOverlay.BeginRequest())
            {
                GameplayRuntimeDiagnostics.Record(
                    "login_request_started",
                    "username", userLogin,
                    "username_length", userLogin != null ? userLogin.Length : 0);
                (bool isSuccess, string message, string token, string refreshToken, string expiresAtUtc) result = await RegisterManager.SendLoginRequest(userLogin, password);

                if (result.isSuccess)
                {
                    GameplayRuntimeDiagnostics.Record(
                        "login_request_succeeded",
                        "username", userLogin,
                        "token_received", !string.IsNullOrEmpty(result.token),
                        "expires_at", result.expiresAtUtc);
                    ClientAuthenticationState.SetSession(result.token, result.refreshToken, result.expiresAtUtc);
                    MenuManager.CloseMenu(MenuType.Auth);
                    MenuManager.OpenMenu(MenuType.MainMenu);
                }
                else
                {
                    GameplayRuntimeDiagnostics.Record(
                        "login_request_failed",
                        "username", userLogin,
                        "message", result.message);
                    MenuManager.OpenMenu(MenuType.Auth);
                    Popup.ShowText(result.message, Color.red);
                }
            }
        }

        private async UniTask RequestRegisterAsync(string userLogin, string password)
        {
            using (StandardLoadingOverlay.BeginRequest())
            {
                (bool isSuccess, string message) result = await RegisterManager.SendRegisterRequest(userLogin, password);

                if (result.isSuccess)
                {
                    Popup.ShowText("Success!", Color.green);
                    MenuManager.OpenMenu(MenuType.Auth);
                }
                else
                {
                    MenuManager.OpenMenu(MenuType.Auth);
                    Popup.ShowText(result.message, Color.red);
                }

                if (result.isSuccess)
                {
                    LastLogin = userLoginInputField != null ? userLoginInputField.text.Trim() : string.Empty;
                }
            }
        }
    }
}
