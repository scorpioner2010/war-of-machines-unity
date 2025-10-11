using System;
using Game.Scripts.Gameplay.Robots;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Script.Player.UI
{
    public class NickNameView : MonoBehaviour
    {
        public TankRoot tankRoot;
        private Camera _mainCamera;
        [SerializeField] private TMP_Text nickName;
        [SerializeField] private Image hpView;

        private void Start()
        {
            tankRoot.health.OnDamaged += (f, f1, arg3) =>
            {
                float cur01 = Mathf.Clamp01(f1 / Mathf.Max(1f, arg3));
                hpView.fillAmount = cur01;
            };
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