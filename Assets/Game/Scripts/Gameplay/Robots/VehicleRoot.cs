using FishNet.Object;
using Game.Script.Player.UI;
using Game.Scripts.Gameplay.Robots.t1;
using Game.Scripts.Gameplay.Robots.t2;
using Game.Scripts.UI.HUD;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleRoot : NetworkBehaviour
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
        public VehicleHUD vehicleHUD;

        private readonly List<IVehicleInitializable> _initializables = new List<IVehicleInitializable>(16);
        private bool _componentsCached;

        public bool IsMenu { get; set; }

        private void Awake()
        {
            CacheComponents();
        }

        public override void OnStartServer()
        {
            CacheComponents();
            InitializeComponents(VehicleInitializationPhase.Server);
        }

        public override void OnStartClient()
        {
        }

        public void Init(bool isMenu = false)
        {
            IsMenu = isMenu;
            CacheComponents();

            if (!IsOwner && !IsMenu)
            {
                return;
            }

            InitializeComponents(VehicleInitializationPhase.Owner);

            if (!IsMenu)
            {
                CameraCrosshair.SetActiveScreen(true);

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void CacheComponents()
        {
            if (_componentsCached)
            {
                return;
            }

            if (networkObject == null)
            {
                networkObject = GetComponent<NetworkObject>();
            }
            if (characterInit == null)
            {
                characterInit = GetComponentInChildren<CharacterInit>(true);
            }
            if (inputManager == null)
            {
                inputManager = GetComponentInChildren<InputManager>(true);
            }
            if (health == null)
            {
                health = GetComponentInChildren<Health>(true);
            }
            if (objectMover == null)
            {
                objectMover = GetComponentInChildren<ObjectMover>(true);
            }
            if (uiSenerd == null)
            {
                uiSenerd = GetComponentInChildren<UISenerd>(true);
            }
            if (cameraController == null)
            {
                cameraController = GetComponentInChildren<CameraController>(true);
            }
            if (robotHullRotation == null)
            {
                robotHullRotation = GetComponentInChildren<RobotHullRotation>(true);
            }
            if (weaponAimAtCamera == null)
            {
                weaponAimAtCamera = GetComponentInChildren<WeaponAimAtCamera>(true);
            }
            if (gunReticleUIFollower == null)
            {
                gunReticleUIFollower = GetComponentInChildren<GunReticleUIFollower>(true);
            }
            if (shooterNet == null)
            {
                shooterNet = GetComponentInChildren<ShooterNet>(true);
            }
            if (weaponReloadController == null)
            {
                weaponReloadController = GetComponentInChildren<WeaponReloadController>(true);
            }
            if (caterpillarTrack == null)
            {
                caterpillarTrack = GetComponentInChildren<CaterpillarTrack>(true);
            }
            if (footAnimator == null)
            {
                footAnimator = GetComponentInChildren<RobotFootAnimator>(true);
            }
            if (vehicleHUD == null)
            {
                vehicleHUD = GetComponentInChildren<VehicleHUD>(true);
            }

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is IVehicleRootAware rootAware)
                {
                    rootAware.SetVehicleRoot(this);
                }

                if (behaviour is IVehicleInitializable initializable)
                {
                    _initializables.Add(initializable);
                }
            }

            _componentsCached = true;
        }

        private void InitializeComponents(VehicleInitializationPhase phase)
        {
            VehicleInitializationContext context = new VehicleInitializationContext(this, phase, IsMenu);
            for (int i = 0; i < _initializables.Count; i++)
            {
                IVehicleInitializable initializable = _initializables[i];
                if (initializable != null)
                {
                    initializable.OnVehicleInitialized(context);
                }
            }
        }
    }
}
