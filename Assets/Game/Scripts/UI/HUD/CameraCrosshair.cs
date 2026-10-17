using Game.Scripts.UI.Screens;

namespace Game.Scripts.UI.HUD
{
    public class CameraCrosshair : UIScreenBase<CameraCrosshair>
    {
        private static CameraCrosshair _instance;
        private static bool _requestedVisible;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            ApplyRequestedVisibility();
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _instance = null;
            _requestedVisible = false;
        }

        public new static void SetActiveScreen(bool isActive)
        {
            _requestedVisible = isActive;

            if (_instance == null)
            {
                return;
            }

            _instance.ApplyRequestedVisibility();
        }

        private void ApplyRequestedVisibility()
        {
            if (screen == null)
            {
                return;
            }

            screen.SetActive(_requestedVisible);
        }
    }
}
