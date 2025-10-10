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
        public Vehicle vehicleType;
        public GameObject isClose;
        public GameObject isHave;
        public RectTransform rectTransform;
    }
}