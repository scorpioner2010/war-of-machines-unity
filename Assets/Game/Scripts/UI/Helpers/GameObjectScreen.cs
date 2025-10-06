using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Helpers
{
    public class GameObjectScreen<T> : MonoBehaviour where T : GameObjectScreen<T>
    {
        public GameObject screen;
        private static T _in;
        protected virtual void Awake()
        {
            _in = (T)this;

            if (screen == null)
            {
                Debug.LogError(gameObject.name);
            }
            screen.SetActive(false);
        }

        public static void SetActiveScreen(bool isActive)
        {
            if (_in == null)
            {
                return;
            }
           
            _in.screen.gameObject.SetActive(isActive);
        }
    }
    
    public class PopupScreen<T> : MonoBehaviour where T : PopupScreen<T>
    {
        public TMP_Text text;
        public Button hide;
        public GameObject popup;
        private static T _in;
        
        protected virtual void Awake()
        {
            _in = (T)this;
            hide.onClick.AddListener(Hide);
        }

        private void Hide()
        {
            _in.popup.SetActive(false);
        }

        public static void ShowText(string message, Color color)
        {
            if (_in == null)
            {
                return;
            }
            
            if (message == string.Empty)
            {
                message = "Server API is not available!";
            }
            
            _in.popup.SetActive(true);
            _in.gameObject.SetActive(true);
            _in.text.color = color;
            _in.text.text = message;
        }
    }
}