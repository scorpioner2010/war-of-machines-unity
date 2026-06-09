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
        private static readonly List<VehicleRoot> ActiveVehicles = new List<VehicleRoot>(64);

        public static event System.Action<VehicleRoot> LocalPlayerVehicleChanged;
        public static VehicleRoot LocalPlayerVehicle { get; private set; }

        public NetworkObject networkObject;
        public VehicleNetworkInitializer characterInit;
        public VehicleInputController inputManager;
        public VehicleAutoAimController autoAimController;
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
        public VehicleClientVisibility clientVisibility;
        public VehicleBotBrain botBrain;
        public VehicleHoverOutline hoverOutline;
        public ArmorMap[] armorMaps = System.Array.Empty<ArmorMap>();
        public Collider[] turretColliders = System.Array.Empty<Collider>();
        public VehicleColliderReference[] colliderReferences = System.Array.Empty<VehicleColliderReference>();
        public Component[] menuStripComponents = System.Array.Empty<Component>();

        [Header("Configured component lists")]
        [SerializeField] private MonoBehaviour[] rootAwareBehaviours = System.Array.Empty<MonoBehaviour>();
        [SerializeField] private MonoBehaviour[] initializableBehaviours = System.Array.Empty<MonoBehaviour>();
        [SerializeField] private MonoBehaviour[] statsConsumerBehaviours = System.Array.Empty<MonoBehaviour>();

        private readonly List<IVehicleInitializable> _initializables = new List<IVehicleInitializable>(16);
        private readonly List<IVehicleStatsConsumer> _statsConsumers = new List<IVehicleStatsConsumer>(16);
        private bool _componentsCached;
        private bool _registeredActiveVehicle;
        private VehicleRuntimeStats _runtimeStats;

        public bool IsMenu { get; set; }
        public VehicleRuntimeStats RuntimeStats => _runtimeStats;
        public bool HasRuntimeStats => _runtimeStats != null && _runtimeStats.IsValid;
        public static int ActiveVehicleCount => ActiveVehicles.Count;

        public static VehicleRoot GetActiveVehicle(int index)
        {
            if (index < 0 || index >= ActiveVehicles.Count)
            {
                return null;
            }

            return ActiveVehicles[index];
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            RegisterActiveVehicle();
        }

        private void OnDisable()
        {
            UnregisterActiveVehicle();
        }

        public override void OnStartServer()
        {
            CacheComponents();
            InitializeComponents(VehicleInitializationPhase.Server);
        }

        public override void OnStartClient()
        {
            if (clientVisibility != null)
            {
                clientVisibility.OnVehicleClientStarted();
            }
        }

        public void Init(bool isMenu = false)
        {
            IsMenu = isMenu;
            CacheComponents();
            if (vehicleHUD != null)
            {
                vehicleHUD.RefreshVisibility();
            }

            if (!IsOwner && !IsMenu)
            {
                return;
            }

            InitializeComponents(VehicleInitializationPhase.Owner);

            if (!IsMenu)
            {
                SetLocalPlayerVehicle(this);
                CameraCrosshair.SetActiveScreen(true);

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void OnDestroy()
        {
            UnregisterActiveVehicle();

            if (LocalPlayerVehicle == this)
            {
                SetLocalPlayerVehicle(null);
            }
        }

        private void RegisterActiveVehicle()
        {
            if (_registeredActiveVehicle)
            {
                return;
            }

            _registeredActiveVehicle = true;
            ActiveVehicles.Add(this);
        }

        private void UnregisterActiveVehicle()
        {
            if (!_registeredActiveVehicle)
            {
                return;
            }

            _registeredActiveVehicle = false;
            int index = ActiveVehicles.IndexOf(this);
            if (index >= 0)
            {
                ActiveVehicles.RemoveAt(index);
            }
        }

        private static void SetLocalPlayerVehicle(VehicleRoot vehicleRoot)
        {
            if (LocalPlayerVehicle == vehicleRoot)
            {
                return;
            }

            LocalPlayerVehicle = vehicleRoot;
            LocalPlayerVehicleChanged?.Invoke(vehicleRoot);
        }

        private void CacheComponents()
        {
            if (_componentsCached)
            {
                return;
            }

            _initializables.Clear();
            _statsConsumers.Clear();

            ApplyRootAware(characterInit);
            ApplyRootAware(inputManager);
            ApplyRootAware(autoAimController);
            ApplyRootAware(objectMover);
            ApplyRootAware(uiSenerd);
            ApplyRootAware(cameraController);
            ApplyRootAware(robotHullRotation);
            ApplyRootAware(weaponAimAtCamera);
            ApplyRootAware(gunReticleUIFollower);
            ApplyRootAware(shooterNet);
            ApplyRootAware(weaponReloadController);
            ApplyRootAware(caterpillarTrack);
            ApplyRootAware(footAnimator);
            ApplyRootAware(vehicleHUD);
            ApplyRootAware(clientVisibility);
            ApplyRootAware(botBrain);
            ApplyRootAware(hoverOutline);
            ApplyRootAware(rootAwareBehaviours);
            ApplyRootAware(armorMaps);
            ApplyRootAware(colliderReferences);

            AddInitializable(characterInit);
            AddInitializable(objectMover);
            AddInitializable(uiSenerd);
            AddInitializable(cameraController);
            AddInitializable(robotHullRotation);
            AddInitializable(weaponAimAtCamera);
            AddInitializable(gunReticleUIFollower);
            AddInitializable(shooterNet);
            AddInitializable(weaponReloadController);
            AddInitializable(autoAimController);
            AddInitializable(hoverOutline);
            AddInitializable(initializableBehaviours);

            AddStatsConsumer(health);
            AddStatsConsumer(objectMover);
            AddStatsConsumer(robotHullRotation);
            AddStatsConsumer(shooterNet);
            AddStatsConsumer(weaponReloadController);
            AddStatsConsumer(statsConsumerBehaviours);
            AddStatsConsumer(armorMaps);

            _componentsCached = true;
        }

        private void ApplyRootAware(MonoBehaviour behaviour)
        {
            if (behaviour is IVehicleRootAware rootAware)
            {
                rootAware.SetVehicleRoot(this);
            }
        }

        private void ApplyRootAware(MonoBehaviour[] behaviours)
        {
            if (behaviours == null)
            {
                return;
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                ApplyRootAware(behaviours[i]);
            }
        }

        private void ApplyRootAware(ArmorMap[] behaviours)
        {
            if (behaviours == null)
            {
                return;
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                ApplyRootAware(behaviours[i]);
            }
        }

        private void ApplyRootAware(VehicleColliderReference[] behaviours)
        {
            if (behaviours == null)
            {
                return;
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                ApplyRootAware(behaviours[i]);
            }
        }

        private void AddInitializable(MonoBehaviour behaviour)
        {
            if (behaviour is IVehicleInitializable initializable && !_initializables.Contains(initializable))
            {
                _initializables.Add(initializable);
            }
        }

        private void AddInitializable(MonoBehaviour[] behaviours)
        {
            if (behaviours == null)
            {
                return;
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                AddInitializable(behaviours[i]);
            }
        }

        private void AddStatsConsumer(MonoBehaviour behaviour)
        {
            if (behaviour is IVehicleStatsConsumer statsConsumer && !_statsConsumers.Contains(statsConsumer))
            {
                _statsConsumers.Add(statsConsumer);
            }
        }

        private void AddStatsConsumer(MonoBehaviour[] behaviours)
        {
            if (behaviours == null)
            {
                return;
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                AddStatsConsumer(behaviours[i]);
            }
        }

        private void AddStatsConsumer(ArmorMap[] behaviours)
        {
            if (behaviours == null)
            {
                return;
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                AddStatsConsumer(behaviours[i]);
            }
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
                    _runtimeStats.ViewRange,
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

        public void DestroyConfiguredMenuStripComponents()
        {
            if (menuStripComponents == null)
            {
                return;
            }

            for (int i = 0; i < menuStripComponents.Length; i++)
            {
                Component component = menuStripComponents[i];
                if (component != null)
                {
                    Destroy(component);
                }
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
            float viewRange,
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
                ViewRange = viewRange,
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
