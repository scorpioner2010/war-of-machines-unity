using System;
using Game.Scripts.Gameplay.Robots;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Script.Player.UI
{
    public class VehicleHUD : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;
        private Camera _mainCamera;
        [SerializeField] private TMP_Text nickName;
        [SerializeField] private Image hpView;
        public FloatingText floatingTextPrefab;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }
        
        private void Start()
        {
            if (vehicleRoot == null || vehicleRoot.health == null)
            {
                enabled = false;
                return;
            }

            vehicleRoot.health.OnDamaged += OnDamaged;
        }

        private void OnDestroy()
        {
            if (vehicleRoot != null && vehicleRoot.health != null)
            {
                vehicleRoot.health.OnDamaged -= OnDamaged;
            }
        }

        private void OnDamaged(float damageAmount, float currentHealth, float maxHealth)
        {
            float cur01 = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
            hpView.fillAmount = cur01;
            ShowFloatingText(damageAmount);
        }
        
        private void ShowFloatingText(float dmg)
        {
            FloatingText t = Instantiate(floatingTextPrefab, transform.position, Quaternion.identity, transform);
            string damage = Mathf.RoundToInt(dmg).ToString();
            t.SetText(damage);
        }

        public void SetNick(string nick)
        {
            nickName.text = nick;
            gameObject.SetActive(true);
        }
        
        public void SetCamera(Camera cam)
        {
            _mainCamera = cam;
        }

        public void SetActiveView(bool active)
        {
            nickName.gameObject.SetActive(active);
        }
        
        private void LateUpdate()
        {
            if (_mainCamera != null)
            {
                transform.forward = _mainCamera.transform.forward;
            }
        }
    }
}
