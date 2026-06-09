using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Image fillImage;
    public TMP_Text label;
    public TMP_Text vehicleNameLabel;

    private void OnEnable()
    {
        Singleton<HealthBar>.Register(this);
    }

    private void OnDisable()
    {
        Singleton<HealthBar>.Unregister(this);
    }
}
