using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class SlotWeaponImage : MonoBehaviour
    {
        public Image img;
        [SerializeField] private TMP_Text bullets;
        private int _lastBulletAmount = int.MinValue;

        public void SetBullet(int amount)
        {
            if (amount == 0)
            {
                SetActiveBulletsView(false);
                return;
            }

            if (bullets != null && _lastBulletAmount != amount)
            {
                _lastBulletAmount = amount;
                bullets.SetText("{0}", amount);
            }
        }
        
        public void SetActiveBulletsView(bool isActive)
        {
            bullets.transform.parent.gameObject.SetActive(isActive);
        }
    }
}
