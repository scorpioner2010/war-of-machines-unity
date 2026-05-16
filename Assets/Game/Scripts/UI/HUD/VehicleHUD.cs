using Game.Scripts.Gameplay.Robots;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class VehicleHUD : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;
        private Camera _mainCamera;
        [SerializeField] private TMP_Text nickName;
        [SerializeField] private Image hpView;
        public FloatingDamageText floatingTextPrefab;

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
            vehicleRoot.health.onDeath.AddListener(OnDeath);
        }

        private void OnDestroy()
        {
            if (vehicleRoot != null && vehicleRoot.health != null)
            {
                vehicleRoot.health.OnDamaged -= OnDamaged;
                vehicleRoot.health.onDeath.RemoveListener(OnDeath);
            }
        }

        private void OnDamaged(float damageAmount, float currentHealth, float maxHealth)
        {
            float cur01 = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
            hpView.fillAmount = cur01;
            ShowFloatingText(damageAmount);
        }

        private void OnDeath()
        {
            gameObject.SetActive(false);
        }
        
        private void ShowFloatingText(float dmg)
        {
            FloatingDamageText text = Instantiate(floatingTextPrefab, transform.position, Quaternion.identity, transform);
            string damage = Mathf.RoundToInt(dmg).ToString();
            text.SetText(damage);
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
