using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class GameplayPlayerListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text vehicleTypeText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image healthBack;
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private Color aliveNicknameColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color aliveVehicleTypeColor = new Color(0.78f, 0.86f, 0.92f, 1f);
        [SerializeField] private Color deadTextColor = new Color(0.32f, 0.32f, 0.32f, 1f);
        [SerializeField] private Color deadHealthColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        [SerializeField] private Color healthBackColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private float deadAlpha = 0.72f;

        private string _nickname;
        private string _vehicleType;
        private float _currentHealth;
        private float _maxHealth = 1f;
        private bool _isDead;
        private Color _aliveHealthColor = Color.white;
        private RectTransform _healthFillRect;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            ConfigureHealthImage();
            Apply();
        }

        public void SetData(string nickname, string vehicleType, float currentHealth, float maxHealth, bool isDead, Color healthColor)
        {
            _nickname = string.IsNullOrEmpty(nickname) ? "-" : nickname;
            _vehicleType = string.IsNullOrEmpty(vehicleType) ? "-" : vehicleType;
            _currentHealth = Mathf.Max(0f, currentHealth);
            _maxHealth = Mathf.Max(1f, maxHealth);
            _isDead = isDead || _currentHealth <= 0f;
            _aliveHealthColor = healthColor;
            Apply();
        }

        public void SetHealth(float currentHealth, float maxHealth, Color healthColor)
        {
            _currentHealth = Mathf.Max(0f, currentHealth);
            _maxHealth = Mathf.Max(1f, maxHealth);
            _aliveHealthColor = healthColor;

            if (_currentHealth <= 0f)
            {
                _isDead = true;
            }

            ApplyHealth();
        }

        public void SetDead(bool isDead)
        {
            _isDead = isDead;
            ApplyVisualState();
            ApplyHealth();
        }

        private void ConfigureHealthImage()
        {
            if (healthFill == null)
            {
                return;
            }

            healthFill.type = Image.Type.Simple;
            _healthFillRect = healthFill.transform as RectTransform;

            if (_healthFillRect != null)
            {
                _healthFillRect.anchorMin = new Vector2(0f, 0f);
                _healthFillRect.anchorMax = new Vector2(1f, 1f);
                _healthFillRect.offsetMin = Vector2.zero;
                _healthFillRect.offsetMax = Vector2.zero;
            }
        }

        private void Apply()
        {
            if (nicknameText != null)
            {
                nicknameText.text = string.IsNullOrEmpty(_nickname) ? "-" : _nickname;
            }

            if (vehicleTypeText != null)
            {
                vehicleTypeText.text = string.IsNullOrEmpty(_vehicleType) ? "-" : _vehicleType;
            }

            ApplyVisualState();
            ApplyHealth();
        }

        private void ApplyVisualState()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = _isDead ? deadAlpha : 1f;
            }

            if (nicknameText != null)
            {
                nicknameText.color = _isDead ? deadTextColor : aliveNicknameColor;
            }

            if (vehicleTypeText != null)
            {
                vehicleTypeText.color = _isDead ? deadTextColor : aliveVehicleTypeColor;
            }

            if (hpText != null)
            {
                hpText.color = _isDead ? deadTextColor : aliveVehicleTypeColor;
            }

            if (healthBack != null)
            {
                healthBack.color = _isDead ? deadHealthColor : healthBackColor;
            }
        }

        private void ApplyHealth()
        {
            float value = GetHealth01();
            ApplyHealthText();

            if (healthFill != null)
            {
                healthFill.color = _isDead ? deadHealthColor : _aliveHealthColor;
            }

            if (_healthFillRect != null)
            {
                _healthFillRect.anchorMin = new Vector2(0f, 0f);
                _healthFillRect.anchorMax = new Vector2(value, 1f);
                _healthFillRect.offsetMin = Vector2.zero;
                _healthFillRect.offsetMax = Vector2.zero;
            }
        }

        private float GetHealth01()
        {
            if (_isDead)
            {
                return 0f;
            }

            return Mathf.Clamp01(_currentHealth / Mathf.Max(1f, _maxHealth));
        }

        private void ApplyHealthText()
        {
            if (hpText == null)
            {
                return;
            }

            float current = _isDead ? 0f : Mathf.Clamp(_currentHealth, 0f, _maxHealth);
            int currentRounded = Mathf.RoundToInt(current);
            int maxRounded = Mathf.RoundToInt(Mathf.Max(1f, _maxHealth));
            hpText.text = currentRounded + "/" + maxRounded;
        }
    }
}
