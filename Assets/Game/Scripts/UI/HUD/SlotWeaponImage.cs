using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class SlotWeaponImage : MonoBehaviour
    {
        public Image img;
        [SerializeField] private TMP_Text bullets;

        public void SetBullet(int amount)
        {
            if (amount == 0)
            {
                SetActiveBulletsView(false);
                return;
            }
            
            bullets.text = amount.ToString();
        }
        
        public void SetActiveBulletsView(bool isActive)
        {
            bullets.transform.parent.gameObject.SetActive(isActive);
        }
    }
}
