using UnityEngine;

namespace Game.Scripts.UI.Screens
{
    public class UIScreenBase<T> : MonoBehaviour where T : UIScreenBase<T>
    {
        public GameObject screen;
        private static T _instance;

        protected virtual void Awake()
        {
            _instance = (T)this;

            if (screen == null)
            {
                return;
            }

            screen.SetActive(false);
        }

        public static void SetActiveScreen(bool isActive)
        {
            if (_instance == null || _instance.screen == null)
            {
                return;
            }
           
            _instance.screen.gameObject.SetActive(isActive);
        }
    }
}
