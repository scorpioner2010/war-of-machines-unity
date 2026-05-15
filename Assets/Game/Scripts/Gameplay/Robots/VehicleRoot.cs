using FishNet.Object;
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
        public VehicleNetworkInitializer characterInit;
        public VehicleInputController inputManager;
        public VehicleHealth health;
        public VehicleMovementController objectMover;
        public VehicleHudInitializer uiSenerd;
        public CameraController cameraController;
        public VehicleTurretRotationController robotHullRotation;
        public WeaponAimController weaponAimAtCamera;
        public WeaponReticlePresenter gunReticleUIFollower;
        public NetworkWeaponShooter shooterNet;
        public WeaponReloadController weaponReloadController;
        public CaterpillarTrack caterpillarTrack;
        public RobotFootAnimator footAnimator;
        public VehicleHUD vehicleHUD;

        private readonly List<IVehicleInitializable> _initializables = new List<IVehicleInitializable>(16);
        private readonly List<IVehicleStatsConsumer> _statsConsumers = new List<IVehicleStatsConsumer>(16);
        private bool _componentsCached;
        private VehicleRuntimeStats _runtimeStats;

        public bool IsMenu { get; set; }
        public VehicleRuntimeStats RuntimeStats => _runtimeStats;
        public bool HasRuntimeStats => _runtimeStats != null && _runtimeStats.IsValid;

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
                characterInit = GetComponentInChildren<VehicleNetworkInitializer>(true);
            }
            if (inputManager == null)
            {
                inputManager = GetComponentInChildren<VehicleInputController>(true);
            }
            if (health == null)
            {
                health = GetComponentInChildren<VehicleHealth>(true);
            }
            if (objectMover == null)
            {
                objectMover = GetComponentInChildren<VehicleMovementController>(true);
            }
            if (uiSenerd == null)
            {
                uiSenerd = GetComponentInChildren<VehicleHudInitializer>(true);
            }
            if (cameraController == null)
            {
                cameraController = GetComponentInChildren<CameraController>(true);
            }
            if (robotHullRotation == null)
            {
                robotHullRotation = GetComponentInChildren<VehicleTurretRotationController>(true);
            }
            if (weaponAimAtCamera == null)
            {
                weaponAimAtCamera = GetComponentInChildren<WeaponAimController>(true);
            }
            if (gunReticleUIFollower == null)
            {
                gunReticleUIFollower = GetComponentInChildren<WeaponReticlePresenter>(true);
            }
            if (shooterNet == null)
            {
                shooterNet = GetComponentInChildren<NetworkWeaponShooter>(true);
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

                if (behaviour is IVehicleStatsConsumer statsConsumer)
                {
                    _statsConsumers.Add(statsConsumer);
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

        public void ServerApplyRuntimeStats(VehicleRuntimeStats stats, bool syncObservers)
        {
            ApplyRuntimeStats(stats);

            if (syncObservers && IsServerInitialized && IsSpawned && HasRuntimeStats)
            {
                RuntimeStatsObserversRpc(
                    _runtimeStats.VehicleId,
                    _runtimeStats.Code,
                    _runtimeStats.Name,
                    _runtimeStats.Level,
                    _runtimeStats.MaxHealth,
                    _runtimeStats.Penetration,
                    _runtimeStats.ShellSpeed,
                    _runtimeStats.ShellsCount,
                    _runtimeStats.DamageMin,
                    _runtimeStats.DamageMax,
                    _runtimeStats.ReloadTime,
                    _runtimeStats.Accuracy,
                    _runtimeStats.AimTime,
                    _runtimeStats.Speed,
                    _runtimeStats.Acceleration,
                    _runtimeStats.TraverseSpeed,
                    _runtimeStats.TurretTraverseSpeed,
                    _runtimeStats.HullArmor.Front,
                    _runtimeStats.HullArmor.Side,
                    _runtimeStats.HullArmor.Rear,
                    _runtimeStats.TurretArmor.Front,
                    _runtimeStats.TurretArmor.Side,
                    _runtimeStats.TurretArmor.Rear
                );
            }
        }

        public void ApplyRuntimeStats(VehicleRuntimeStats stats)
        {
            if (stats == null || !stats.IsValid)
            {
                return;
            }

            _runtimeStats = stats.Clone();
            CacheComponents();
            ApplyRuntimeStatsToComponents();
        }

        private void ApplyRuntimeStatsToComponents()
        {
            for (int i = 0; i < _statsConsumers.Count; i++)
            {
                IVehicleStatsConsumer consumer = _statsConsumers[i];
                if (consumer != null)
                {
                    consumer.ApplyVehicleStats(_runtimeStats);
                }
            }
        }

        [ObserversRpc(BufferLast = true)]
        private void RuntimeStatsObserversRpc(
            int vehicleId,
            string code,
            string vehicleName,
            int level,
            float maxHealth,
            float penetration,
            float shellSpeed,
            int shellsCount,
            float damageMin,
            float damageMax,
            float reloadTime,
            float accuracy,
            float aimTime,
            float speed,
            float acceleration,
            float traverseSpeed,
            float turretTraverseSpeed,
            int hullFront,
            int hullSide,
            int hullRear,
            int turretFront,
            int turretSide,
            int turretRear)
        {
            VehicleRuntimeStats stats = new VehicleRuntimeStats
            {
                VehicleId = vehicleId,
                Code = code,
                Name = vehicleName,
                Level = level,
                MaxHealth = maxHealth,
                Penetration = penetration,
                ShellSpeed = shellSpeed,
                ShellsCount = shellsCount,
                DamageMin = damageMin,
                DamageMax = damageMax,
                ReloadTime = reloadTime,
                Accuracy = accuracy,
                AimTime = aimTime,
                Speed = speed,
                Acceleration = acceleration,
                TraverseSpeed = traverseSpeed,
                TurretTraverseSpeed = turretTraverseSpeed,
                HullArmor = new VehicleArmorValues
                {
                    Front = hullFront,
                    Side = hullSide,
                    Rear = hullRear
                },
                TurretArmor = new VehicleArmorValues
                {
                    Front = turretFront,
                    Side = turretSide,
                    Rear = turretRear
                }
            };

            ApplyRuntimeStats(stats);
        }
    }
}
