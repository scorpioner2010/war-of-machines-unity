using FishNet.Managing;
using Game.Scripts.Core.Services;
using Game.Scripts.Player.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Helpers
{
    public class StartServerButtons : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private Button connect;
        [SerializeField] private Button server;
        [SerializeField] private GameObject panel;
        [SerializeField] private bool test;

        private void Awake()
        {
            connect.transform.parent.gameObject.SetActive(true);
            
            if (test == false)
            {
#if UNITY_EDITOR == false && UNITY_SERVER == false 
     if (networkManager.ClientManager.StartConnection())
                {
                    connect.gameObject.SetActive(false);
                    server.gameObject.SetActive(false);
                    panel.gameObject.SetActive(false);
                    ServiceLocator.Register<IPlayerClientInfo>(new PlayerClientInfo());
                    Debug.Log("Client started");
                }
            return;
#endif
            }
            
            connect.onClick.AddListener(() =>
            {
                if (networkManager.ClientManager.StartConnection())
                {
                    connect.gameObject.SetActive(false);
                    server.gameObject.SetActive(false);
                    panel.gameObject.SetActive(false);
                    ServiceLocator.Register<IPlayerClientInfo>(new PlayerClientInfo());
                    //Debug.Log("Client started");
                }
            });
            
            server.onClick.AddListener(() =>
            {
                if (networkManager.ServerManager.StartConnection())
                {
                    server.gameObject.SetActive(false);
                    //Debug.Log("Server started");
                }
            });
        }
    }
}