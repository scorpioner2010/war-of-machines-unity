using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Audio
{
    public class ButtonSoundClick : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(() =>
            {
                SoundCaller.PlayOneShot("SFX_UI_Button_Mouse_Thick_Generic_1");
            });
        }
    }
}
