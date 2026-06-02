using System.Collections;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.UI.Screens;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class DamageScreen : UIScreenBase<DamageScreen>
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeOutDuration = 1f;

        private static DamageScreen _instance;

        private VehicleHealth _subscribedHealth;
        private Coroutine _flashRoutine;

        protected override void Awake()
        {
            _instance = this;
            EnsureScreenReady();
            if (screen != null)
            {
                screen.SetActive(false);
            }

            SetAlpha(0f);
        }

        private void OnEnable()
        {
            VehicleRoot.LocalPlayerVehicleChanged += OnLocalPlayerVehicleChanged;
            BindToVehicle(VehicleRoot.LocalPlayerVehicle);
            EnsureScreenReady();
        }

        private void OnDisable()
        {
            VehicleRoot.LocalPlayerVehicleChanged -= OnLocalPlayerVehicleChanged;
            UnbindHealth();

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            SetAlpha(0f);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static void Pulse()
        {
            if (_instance == null)
            {
                return;
            }

            _instance.PlayHitFlash();
        }

        private void OnLocalPlayerVehicleChanged(VehicleRoot vehicleRoot)
        {
            BindToVehicle(vehicleRoot);
        }

        private void BindToVehicle(VehicleRoot vehicleRoot)
        {
            VehicleHealth nextHealth = vehicleRoot != null ? vehicleRoot.health : null;
            if (_subscribedHealth == nextHealth)
            {
                return;
            }

            UnbindHealth();
            _subscribedHealth = nextHealth;
            if (_subscribedHealth != null)
            {
                _subscribedHealth.OnDamaged += OnLocalVehicleDamaged;
            }
        }

        private void UnbindHealth()
        {
            if (_subscribedHealth != null)
            {
                _subscribedHealth.OnDamaged -= OnLocalVehicleDamaged;
                _subscribedHealth = null;
            }
        }

        private void OnLocalVehicleDamaged(float damageAmount, float currentHealth, float maxHealth)
        {
            if (damageAmount <= 0f)
            {
                return;
            }

            PlayHitFlash();
        }

        private void PlayHitFlash()
        {
            EnsureScreenReady();
            if (canvasGroup == null || screen == null)
            {
                return;
            }

            if (!screen.activeSelf)
            {
                screen.SetActive(true);
            }

            SetAlpha(1f);

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                return;
            }

            _flashRoutine = StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            float duration = Mathf.Max(0f, fadeOutDuration);
            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    SetAlpha(Mathf.Lerp(1f, 0f, t));
                    yield return null;
                }
            }

            SetAlpha(0f);
            if (screen != null)
            {
                screen.SetActive(false);
            }

            _flashRoutine = null;
        }

        private void EnsureScreenReady()
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }
}
