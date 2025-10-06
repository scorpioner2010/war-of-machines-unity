using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Audio
{
    public class ButtonSoundClick : MonoBehaviour
    {
        private void Awake()
        {
            Button button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                SoundCaller.PlayOneShot("SFX_UI_Button_Mouse_Thick_Generic_1");
            });
        }
    }
}
