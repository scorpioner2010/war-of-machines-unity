using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class GameplayGUI : MonoBehaviour
    {
        public static GameplayGUI In;
        
        [SerializeField] private Button buttonPause;
        
        public PauseMenu pauseMenu;

        public void Awake()
        {
            In = this;
            buttonPause.onClick.AddListener(pauseMenu.OpenPause);
        }
        
        public void UpdateHealth(float healthPercentage)
        {
           
        }

        public void OnDestroy()
        {
            buttonPause.onClick.RemoveAllListeners();
        }
    }
}
