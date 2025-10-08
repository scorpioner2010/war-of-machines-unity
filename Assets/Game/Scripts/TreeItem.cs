using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TreeItem : MonoBehaviour
{
    public Button button;
    public TMP_Text vehicleName;
    public TMP_Text level;
    public Image image;
    
    public Vehicle vehicleType;
}

public enum Vehicle
{
    None = 0,
    Scout = 1,
    Guardian = 2,
    Colossus = 3,
}