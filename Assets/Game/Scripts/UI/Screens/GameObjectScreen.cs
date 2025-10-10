using UnityEngine;

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
}