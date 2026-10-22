using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public class TreeItem : MonoBehaviour
    {
        public Button button;
        public TMP_Text vehicleName;
        public TMP_Text level;
        public TMP_Text price;
        public Image image;
        public VehicleClass vehicleType;
        public GameObject isClose;
        public GameObject isHave;
        public GameObject isSelect;
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform;

        public void SetSelected(bool selected)
        {
            if (isSelect != null)
            {
                isSelect.SetActive(selected);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = selected ? 1f : 0.35f;
            }
        }
    }
}
