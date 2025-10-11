using FishNet.Object;
using Game.Script.Player.UI;
using Game.Scripts.Gameplay.Robots.t1;
using Game.Scripts.Gameplay.Robots.t2;
using Game.Scripts.UI.HUD;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class TankRoot : NetworkBehaviour
    {
        public NetworkObject networkObject;
        public CharacterInit characterInit;
        public InputManager inputManager;
        public Health health;
        public ObjectMover objectMover;
        public UISenerd uiSenerd;
        public CameraController cameraController;
        public RobotHullRotation robotHullRotation;
        public WeaponAimAtCamera weaponAimAtCamera;
        public GunReticleUIFollower gunReticleUIFollower;
        public ShooterNet shooterNet;
        public WeaponReloadController weaponReloadController;
        public CaterpillarTrack caterpillarTrack;
        public RobotFootAnimator footAnimator;
        public NickNameView nickNameView;

        public bool IsMenu { get; set; }

        public override void OnStartServer()
        {
            robotHullRotation.Init();
        }

        public override void OnStartClient()
        {
            // nothing
        }

        public void Init(bool isMenu = false)
        {
            IsMenu = isMenu;

            if (!IsOwner)
            {
                return;
            }

            cameraController.Init();

            if (IsMenu)
            {
                return;
            }

            uiSenerd.Init();
            robotHullRotation.Init();
            CameraCrosshair.SetActiveScreen(true);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (CameraSync.In != null)
            {
                weaponAimAtCamera.Init(CameraSync.In.transform);
                gunReticleUIFollower.Init();
                weaponReloadController.Init();
            }
        }
    }
}