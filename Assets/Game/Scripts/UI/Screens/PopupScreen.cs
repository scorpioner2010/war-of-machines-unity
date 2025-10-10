using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Screens
{
    public class PopupScreen<T> : MonoBehaviour where T : PopupScreen<T>
    {
        public TMP_Text text;
        
        public Button ok;
        public Button confirm;
        public Button cancel;
        
        public GameObject popup;
        
        private static T _in;
        
        protected virtual void Awake()
        {
            _in = (T)this;
            ok.onClick.AddListener(Hide);
            cancel.onClick.AddListener(Hide);
        }

        private void Hide()
        {
            _in.popup.SetActive(false);
        }

        public static void ShowText(string message, Color color, Action act = null, TypePopup  typePopup = TypePopup.Info)
        {
            if (_in == null)
            {
                return;
            }
            
            if (message == string.Empty)
            {
                message = "Server API is not available!";
            }

            if (act != null)
            {
                _in.confirm.onClick.RemoveAllListeners();
                _in.confirm.onClick.AddListener(() =>
                {
                    act?.Invoke();
                    _in.confirm.onClick.RemoveAllListeners();
                    _in.Hide();
                });
            }

            _in.confirm.gameObject.SetActive(typePopup == TypePopup.Confirm);
            _in.cancel.gameObject.SetActive(typePopup == TypePopup.Confirm);
            _in.ok.gameObject.SetActive(typePopup == TypePopup.Info);
            
            _in.popup.SetActive(true);
            _in.gameObject.SetActive(true);
            _in.text.color = color;
            _in.text.text = message;
        }
    }
}