using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TreeItem : MonoBehaviour
{
    public Button button;
    public TMP_Text text;
    public Image image;
    public Vehicle vehicle;

    public void Init(Vehicle type)
    {
        vehicle =  type;

        if (vehicle == Vehicle.None)
        {
            button.gameObject.SetActive(false);
        }
    }
}

public enum Vehicle
{
    None = 0,
    Scout = 1,
    Guardian = 2,
    Colossus = 3,
}