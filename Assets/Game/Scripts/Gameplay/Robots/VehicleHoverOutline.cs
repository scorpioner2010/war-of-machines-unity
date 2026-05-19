using System.Collections.Generic;
using Game.Scripts.Client;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Rendering;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public class VehicleHoverOutline : MonoBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        private const int RaycastBufferSize = 128;

        private static readonly RaycastHit[] RaycastBuffer = new RaycastHit[RaycastBufferSize];
        private static readonly Dictionary<Collider, VehicleRoot> RootByCollider = new Dictionary<Collider, VehicleRoot>(256);
        private static Camera _fallbackCamera;

        public VehicleRoot vehicleRoot;
        public LayerMask hoverMask = ~0;

        private readonly List<Renderer> _targetRenderers = new List<Renderer>(64);
        private readonly List<int> _targetSubMeshCounts = new List<int>(64);
        private VehicleHoverOutline _currentTarget;
        private bool _controlsLocalHover;
        private bool _outlineBuilt;
        private bool _outlined;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            _controlsLocalHover = context.IsOwner && !context.IsMenu;

            if (!_controlsLocalHover)
            {
                ClearCurrentTarget();
            }

            SetOutlined(false);
        }

        public void SetOutlined(bool outlined)
        {
            if (_outlined == outlined)
            {
                return;
            }

            _outlined = outlined;

            if (_outlined)
            {
                GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
                EnsureOutlineRenderers();
                ScreenSpaceHoverOutlineFeature.SetTarget(
                    this,
                    _targetRenderers,
                    _targetSubMeshCounts,
                    settings.hoverOutlineColor,
                    settings.hoverOutlineWidth
                );
                return;
            }

            ScreenSpaceHoverOutlineFeature.ClearTarget(this);
        }

        private void Update()
        {
            if (!_controlsLocalHover)
            {
                return;
            }

            UpdateHoverTarget();
        }

        private void OnDisable()
        {
            ClearCurrentTarget();
            SetOutlined(false);
        }

        private void OnDestroy()
        {
            ClearCurrentTarget();
            SetOutlined(false);
        }

        private void UpdateHoverTarget()
        {
            VehicleHoverOutline target = TryFindHoverTarget(out VehicleRoot targetRoot)
                ? GetOrCreateOutline(targetRoot)
                : null;

            if (_currentTarget == target)
            {
                return;
            }

            ClearCurrentTarget();
            _currentTarget = target;

            if (_currentTarget != null)
            {
                _currentTarget.SetOutlined(true);
            }
        }

        private void ClearCurrentTarget()
        {
            if (_currentTarget != null)
            {
                _currentTarget.SetOutlined(false);
                _currentTarget = null;
            }
        }

        private bool TryFindHoverTarget(out VehicleRoot targetRoot)
        {
            targetRoot = null;

            Camera cam = GetGameplayCamera();
            if (cam == null)
            {
                return false;
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            float maxDistance = Mathf.Max(0.1f, settings.hoverOutlineMaxDistance);
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                RaycastBuffer,
                maxDistance,
                hoverMask,
                QueryTriggerInteraction.Ignore
            );

            VehicleRoot bestRoot = null;
            float bestEnemyDistance = float.PositiveInfinity;
            float closestBlockingDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = RaycastBuffer[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                VehicleRoot hitRoot = ResolveVehicleRoot(hitCollider);
                if (hitRoot == vehicleRoot)
                {
                    continue;
                }

                float hitDistance = RaycastBuffer[i].distance;
                if (IsValidEnemyTarget(hitRoot))
                {
                    if (hitDistance < bestEnemyDistance)
                    {
                        bestEnemyDistance = hitDistance;
                        bestRoot = hitRoot;
                    }
                }
                else if (hitDistance < closestBlockingDistance)
                {
                    closestBlockingDistance = hitDistance;
                }
            }

            if (bestRoot == null || closestBlockingDistance < bestEnemyDistance)
            {
                return false;
            }

            targetRoot = bestRoot;
            return true;
        }

        private VehicleHoverOutline GetOrCreateOutline(VehicleRoot targetRoot)
        {
            if (targetRoot == null)
            {
                return null;
            }

            VehicleHoverOutline targetOutline = targetRoot.GetComponent<VehicleHoverOutline>();
            if (targetOutline == null)
            {
                targetOutline = targetRoot.gameObject.AddComponent<VehicleHoverOutline>();
                targetOutline.SetVehicleRoot(targetRoot);
            }

            return targetOutline;
        }

        private void EnsureOutlineRenderers()
        {
            if (_outlineBuilt)
            {
                return;
            }

            _outlineBuilt = true;
            _targetRenderers.Clear();
            _targetSubMeshCounts.Clear();

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!IsValidOutlineRenderer(renderer))
                {
                    continue;
                }

                _targetRenderers.Add(renderer);
                _targetSubMeshCounts.Add(GetSubMeshCount(renderer));
            }
        }

        private static bool IsValidOutlineRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            if (renderer is MeshRenderer)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                return meshFilter != null && meshFilter.sharedMesh != null;
            }

            SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
            return skinnedRenderer != null && skinnedRenderer.sharedMesh != null;
        }

        private static int GetSubMeshCount(Renderer renderer)
        {
            MeshRenderer meshRenderer = renderer as MeshRenderer;
            if (meshRenderer != null)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    return Mathf.Max(1, meshFilter.sharedMesh.subMeshCount);
                }
            }

            SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
            if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
            {
                return Mathf.Max(1, skinnedRenderer.sharedMesh.subMeshCount);
            }

            return 1;
        }

        private bool IsValidEnemyTarget(VehicleRoot targetRoot)
        {
            if (targetRoot == null || targetRoot == vehicleRoot)
            {
                return false;
            }

            if (targetRoot.health != null && targetRoot.health.IsDead)
            {
                return false;
            }

            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            if (!settings.hoverOutlineRejectSameTeam)
            {
                return true;
            }

            if (vehicleRoot == null || vehicleRoot.characterInit == null || targetRoot.characterInit == null)
            {
                return true;
            }

            MatchTeam localTeam = vehicleRoot.characterInit.Team.Value;
            MatchTeam targetTeam = targetRoot.characterInit.Team.Value;
            return !MatchTeamUtility.AreSameAssignedTeam(localTeam, targetTeam);
        }

        private static VehicleRoot ResolveVehicleRoot(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return null;
            }

            if (RootByCollider.TryGetValue(hitCollider, out VehicleRoot cachedRoot))
            {
                return cachedRoot;
            }

            ArmorMap armorMap = hitCollider.GetComponent<ArmorMap>();
            if (armorMap == null)
            {
                armorMap = hitCollider.GetComponentInParent<ArmorMap>();
            }

            if (armorMap != null)
            {
                if (armorMap.vehicleRoot != null)
                {
                    RootByCollider[hitCollider] = armorMap.vehicleRoot;
                    return armorMap.vehicleRoot;
                }

                VehicleRoot armorRoot = armorMap.GetComponentInParent<VehicleRoot>();
                RootByCollider[hitCollider] = armorRoot;
                return armorRoot;
            }

            VehicleRoot root = hitCollider.GetComponentInParent<VehicleRoot>();
            RootByCollider[hitCollider] = root;
            return root;
        }

        private static Camera GetGameplayCamera()
        {
            if (CameraSync.In != null && CameraSync.In.gameplayCamera != null)
            {
                return CameraSync.In.gameplayCamera;
            }

            if (_fallbackCamera != null)
            {
                return _fallbackCamera;
            }

            _fallbackCamera = Camera.main;
            return _fallbackCamera;
        }
    }
}
