using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Robots
{
    public class SpeedHud : View<SpeedHud> { }

    public class View<T> : MonoBehaviour where T : View<T>
    {
        public GameObject target;
        public TMP_Text textObject;
        public Image image;

        private static T _in;
        protected virtual void Awake() => _in = (T)this;
    
        public static void SetText(string text)
        {
            if (_in == null)
            {
                Debug.LogError("error");
                return;
            }
        
            if (_in.textObject != null)
            {
                _in.textObject.text = text;
            }
        }

        public static void SetImage(Sprite sprite)
        {
            if (_in == null)
            {
                Debug.LogError("error");
                return;
            }

            if (_in.image != null)
            {
                _in.image.sprite = sprite;
            }
        }

        public static void SetActive(bool active)
        { 
            if (_in == null)
            {
                Debug.LogError("error");
                return;
            }

            if (_in.gameObject != null)
            {
                _in.gameObject.SetActive(active);
            }
        }
    }
}