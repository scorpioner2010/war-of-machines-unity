using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public static class VehicleColliderRegistry
    {
        private const uint WorldImpactRenderingLayerMask = 2u;
        private const float ImpactSurfaceDistanceEpsilon = 0.0001f;
        private const float ImpactSurfaceRaycastDistance = 100f;

        private static readonly Dictionary<Collider, VehicleColliderData> DataByCollider =
            new Dictionary<Collider, VehicleColliderData>(512);
        private static readonly Dictionary<int, VehicleRoot> RootsByObjectId =
            new Dictionary<int, VehicleRoot>(64);
        private static readonly Dictionary<VehicleRoot, List<Renderer>> ImpactRenderersByRoot =
            new Dictionary<VehicleRoot, List<Renderer>>(64);
        private static readonly Dictionary<VehicleRoot, List<MeshCollider>> ImpactMeshCollidersByRoot =
            new Dictionary<VehicleRoot, List<MeshCollider>>(64);
        private static readonly Dictionary<VehicleRoot, List<ImpactVisualPart>> ImpactVisualPartsByRoot =
            new Dictionary<VehicleRoot, List<ImpactVisualPart>>(64);
        private static readonly Dictionary<Collider, ImpactVisualSurfaceData> ImpactSurfacesByCollider =
            new Dictionary<Collider, ImpactVisualSurfaceData>(256);

        public static void RegisterRenderers(VehicleRoot root, Renderer[] renderers)
        {
            if (root == null || renderers == null)
            {
                return;
            }

            if (!ImpactRenderersByRoot.TryGetValue(root, out List<Renderer> registeredRenderers))
            {
                registeredRenderers = new List<Renderer>(renderers.Length);
                ImpactRenderersByRoot.Add(root, registeredRenderers);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.renderingLayerMask &= ~WorldImpactRenderingLayerMask;
                if (!registeredRenderers.Contains(renderer))
                {
                    registeredRenderers.Add(renderer);
                }
            }
        }

        public static void RegisterNetworkRoot(VehicleRoot root)
        {
            RegisterRoot(root);
        }

        public static void RegisterArmor(
            Collider collider,
            VehicleRoot root,
            VehicleArmorController armor,
            VehicleArmorController.ArmorZone zone)
        {
            if (collider == null || root == null || armor == null)
            {
                return;
            }

            DataByCollider[collider] = new VehicleColliderData(root, armor, zone, true);
            RegisterRoot(root);
        }

        public static void RegisterVehicle(Collider collider, VehicleRoot root)
        {
            if (collider == null || root == null)
            {
                return;
            }

            DataByCollider[collider] = new VehicleColliderData(
                root,
                null,
                VehicleArmorController.ArmorZone.Turret,
                false);
            RegisterRoot(root);
        }

        public static void Unregister(Collider collider, VehicleRoot root)
        {
            if (collider == null
                || !DataByCollider.TryGetValue(collider, out VehicleColliderData data))
            {
                return;
            }

            if (data.Root == root)
            {
                DataByCollider.Remove(collider);
                UnregisterRootIfUnused(root);
            }
        }

        public static bool TryGetRoot(Collider collider, out VehicleRoot root)
        {
            root = null;
            if (collider == null
                || !DataByCollider.TryGetValue(collider, out VehicleColliderData data))
            {
                return false;
            }

            root = data.Root;
            return root != null;
        }

        public static bool TryGetRoot(int networkObjectId, out VehicleRoot root)
        {
            root = null;
            if (!RootsByObjectId.TryGetValue(networkObjectId, out VehicleRoot registeredRoot))
            {
                return false;
            }

            if (registeredRoot == null)
            {
                RootsByObjectId.Remove(networkObjectId);
                return false;
            }

            root = registeredRoot;
            return true;
        }

        public static bool TryGetImpactRenderers(
            VehicleRoot root,
            out List<Renderer> renderers)
        {
            renderers = null;
            if (root == null || !ImpactRenderersByRoot.TryGetValue(root, out List<Renderer> registeredRenderers))
            {
                return false;
            }

            renderers = registeredRenderers;
            return renderers.Count > 0;
        }

        public static bool TryGetImpactRenderers(
            int networkObjectId,
            out List<Renderer> renderers)
        {
            renderers = null;
            if (!TryGetRoot(networkObjectId, out VehicleRoot root))
            {
                return false;
            }

            return TryGetImpactRenderers(root, out renderers);
        }

        public static void RegisterImpactMeshColliders(
            VehicleRoot root,
            List<MeshCollider> meshColliders)
        {
            if (root == null || meshColliders == null)
            {
                return;
            }

            if (!ImpactMeshCollidersByRoot.TryGetValue(root, out List<MeshCollider> registeredColliders))
            {
                registeredColliders = new List<MeshCollider>(meshColliders.Count);
                ImpactMeshCollidersByRoot.Add(root, registeredColliders);
            }

            for (int i = 0; i < meshColliders.Count; i++)
            {
                MeshCollider meshCollider = meshColliders[i];
                if (meshCollider != null && !registeredColliders.Contains(meshCollider))
                {
                    registeredColliders.Add(meshCollider);
                }
            }
        }

        public static void RegisterImpactVisualPart(
            VehicleRoot root,
            MeshCollider meshCollider,
            Renderer renderer)
        {
            if (root == null || (meshCollider == null && renderer == null))
            {
                return;
            }

            if (!ImpactVisualPartsByRoot.TryGetValue(root, out List<ImpactVisualPart> visualParts))
            {
                visualParts = new List<ImpactVisualPart>(16);
                ImpactVisualPartsByRoot.Add(root, visualParts);
            }

            for (int i = 0; i < visualParts.Count; i++)
            {
                ImpactVisualPart existingPart = visualParts[i];
                if (existingPart.MeshCollider == meshCollider && existingPart.Renderer == renderer)
                {
                    return;
                }
            }

            visualParts.Add(new ImpactVisualPart(meshCollider, renderer));
        }

        public static void UnregisterImpactMeshColliders(VehicleRoot root)
        {
            if (root != null)
            {
                ImpactMeshCollidersByRoot.Remove(root);
                ImpactVisualPartsByRoot.Remove(root);
            }
        }

        public static void RegisterImpactVisualSurface(
            Collider physicsCollider,
            MeshCollider[] meshColliders,
            Renderer[] renderers)
        {
            if (physicsCollider == null)
            {
                return;
            }

            ImpactVisualSurfaceData data = new ImpactVisualSurfaceData(meshColliders, renderers);
            if (data.MeshColliders.Count == 0 && data.Renderers.Count == 0)
            {
                return;
            }

            ImpactSurfacesByCollider[physicsCollider] = data;
        }

        public static void UnregisterImpactVisualSurface(Collider physicsCollider)
        {
            if (physicsCollider != null)
            {
                ImpactSurfacesByCollider.Remove(physicsCollider);
            }
        }

        public static bool TryGetImpactVisualSurface(
            Collider physicsCollider,
            Vector3 impactPoint,
            Vector3 impactNormal,
            out Vector3 surfacePosition,
            out Vector3 surfaceNormal)
        {
            surfacePosition = impactPoint;
            surfaceNormal = impactNormal.sqrMagnitude > 0.000001f
                ? impactNormal.normalized
                : Vector3.up;
            if (physicsCollider == null
                || !ImpactSurfacesByCollider.TryGetValue(physicsCollider, out ImpactVisualSurfaceData data))
            {
                return false;
            }

            return TryResolveImpactVisualSurface(
                data.MeshColliders,
                data.Renderers,
                null,
                impactPoint,
                surfaceNormal,
                out surfacePosition,
                out surfaceNormal,
                out _);
        }

        public static bool TryGetImpactVisualSurface(
            VehicleRoot root,
            Vector3 impactPoint,
            Vector3 impactNormal,
            out Vector3 surfacePosition,
            out Vector3 surfaceNormal)
        {
            return TryGetImpactVisualSurface(
                root,
                impactPoint,
                impactNormal,
                out surfacePosition,
                out surfaceNormal,
                out _);
        }

        public static bool TryGetImpactVisualSurface(
            VehicleRoot root,
            Vector3 impactPoint,
            Vector3 impactNormal,
            out Vector3 surfacePosition,
            out Vector3 surfaceNormal,
            out Renderer surfaceRenderer)
        {
            Vector3 safeNormal = impactNormal.sqrMagnitude > 0.000001f
                ? impactNormal.normalized
                : Vector3.up;
            surfacePosition = impactPoint;
            surfaceNormal = safeNormal;
            surfaceRenderer = null;
            if (root == null)
            {
                return false;
            }

            ImpactMeshCollidersByRoot.TryGetValue(root, out List<MeshCollider> meshColliders);
            ImpactRenderersByRoot.TryGetValue(root, out List<Renderer> renderers);
            ImpactVisualPartsByRoot.TryGetValue(root, out List<ImpactVisualPart> visualParts);
            return TryResolveImpactVisualSurface(
                meshColliders,
                renderers,
                visualParts,
                impactPoint,
                safeNormal,
                out surfacePosition,
                out surfaceNormal,
                out surfaceRenderer);
        }

        private static bool TryResolveImpactVisualSurface(
            List<MeshCollider> meshColliders,
            List<Renderer> renderers,
            List<ImpactVisualPart> visualParts,
            Vector3 impactPoint,
            Vector3 safeNormal,
            out Vector3 surfacePosition,
            out Vector3 surfaceNormal,
            out Renderer surfaceRenderer)
        {
            Vector3 rayDirection = -safeNormal;
            Ray surfaceRay = new Ray(
                impactPoint + safeNormal * ImpactSurfaceDistanceEpsilon,
                rayDirection);
            float closestDistance = float.PositiveInfinity;
            surfacePosition = impactPoint;
            surfaceNormal = safeNormal;
            surfaceRenderer = null;
            bool foundSurface = false;

            if (visualParts != null)
            {
                for (int i = 0; i < visualParts.Count; i++)
                {
                    ImpactVisualPart visualPart = visualParts[i];
                    MeshCollider meshCollider = visualPart.MeshCollider;
                    if (meshCollider == null || meshCollider.sharedMesh == null)
                    {
                        continue;
                    }

                    if (TryGetMeshColliderSurface(
                        meshCollider,
                        impactPoint,
                        safeNormal,
                        surfaceRay,
                        out RaycastHit meshHit))
                    {
                        float distance = (meshHit.point - impactPoint).magnitude;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            surfacePosition = meshHit.point;
                            surfaceNormal = meshHit.normal.sqrMagnitude > 0.000001f
                                ? meshHit.normal.normalized
                                : safeNormal;
                            if (Vector3.Dot(surfaceNormal, safeNormal) < 0f)
                            {
                                surfaceNormal = -surfaceNormal;
                            }

                            surfaceRenderer = visualPart.Renderer;
                            foundSurface = true;
                        }
                    }
                }
            }

            if (meshColliders != null)
            {
                for (int i = 0; i < meshColliders.Count; i++)
                {
                    MeshCollider meshCollider = meshColliders[i];
                    if (meshCollider == null || meshCollider.sharedMesh == null)
                    {
                        continue;
                    }

                    if (TryGetMeshColliderSurface(
                        meshCollider,
                        impactPoint,
                        safeNormal,
                        surfaceRay,
                        out RaycastHit meshHit))
                    {
                        float distance = (meshHit.point - impactPoint).magnitude;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            surfacePosition = meshHit.point;
                            surfaceNormal = meshHit.normal.sqrMagnitude > 0.000001f
                                ? meshHit.normal.normalized
                                : safeNormal;
                            if (Vector3.Dot(surfaceNormal, safeNormal) < 0f)
                            {
                                surfaceNormal = -surfaceNormal;
                            }

                            surfaceRenderer = null;
                            foundSurface = true;
                        }
                    }
                }
            }

            if (foundSurface)
            {
                return true;
            }

            return renderers != null
                && TryGetRendererBoundsSurface(
                    renderers,
                    impactPoint,
                    safeNormal,
                    out surfacePosition,
                    out surfaceRenderer);
        }

        private static bool TryGetMeshColliderSurface(
            MeshCollider meshCollider,
            Vector3 impactPoint,
            Vector3 safeNormal,
            Ray normalRay,
            out RaycastHit closestHit)
        {
            closestHit = default;
            bool foundHit = TryRaycastMeshCollider(
                meshCollider,
                normalRay,
                ImpactSurfaceRaycastDistance,
                out RaycastHit normalHit);
            if (foundHit)
            {
                closestHit = normalHit;
                return true;
            }

            Vector3 towardVisualCenter = meshCollider.bounds.center - impactPoint;
            float centerDistance = towardVisualCenter.magnitude;
            if (centerDistance > ImpactSurfaceDistanceEpsilon)
            {
                Vector3 centerDirection = towardVisualCenter / centerDistance;
                Ray centerRay = new Ray(
                    impactPoint + centerDirection * ImpactSurfaceDistanceEpsilon,
                    centerDirection);
                float castDistance = centerDistance + meshCollider.bounds.extents.magnitude + 1f;
                if (TryRaycastMeshCollider(meshCollider, centerRay, castDistance, out RaycastHit centerHit))
                {
                    closestHit = centerHit;
                    foundHit = true;
                }
            }

            if (!foundHit)
            {
                return false;
            }

            Vector3 hitNormal = closestHit.normal.sqrMagnitude > 0.000001f
                ? closestHit.normal.normalized
                : safeNormal;
            if (Vector3.Dot(hitNormal, safeNormal) < 0f)
            {
                hitNormal = -hitNormal;
            }

            closestHit.normal = hitNormal;
            return true;
        }

        private static bool TryRaycastMeshCollider(
            MeshCollider meshCollider,
            Ray ray,
            float distance,
            out RaycastHit hit)
        {
            List<GameObject> activatedObjects = null;
            Transform currentTransform = meshCollider.transform;
            while (currentTransform != null)
            {
                if (!currentTransform.gameObject.activeSelf)
                {
                    if (activatedObjects == null)
                    {
                        activatedObjects = new List<GameObject>(4);
                    }

                    activatedObjects.Add(currentTransform.gameObject);
                }

                currentTransform = currentTransform.parent;
            }

            if (activatedObjects != null)
            {
                for (int i = activatedObjects.Count - 1; i >= 0; i--)
                {
                    activatedObjects[i].SetActive(true);
                }
            }

            bool wasEnabled = meshCollider.enabled;
            if (!wasEnabled)
            {
                meshCollider.enabled = true;
            }

            try
            {
                Physics.SyncTransforms();
                return meshCollider.Raycast(ray, out hit, Mathf.Max(ImpactSurfaceDistanceEpsilon, distance));
            }
            finally
            {
                if (!wasEnabled && meshCollider != null)
                {
                    meshCollider.enabled = false;
                }

                if (activatedObjects != null)
                {
                    for (int i = 0; i < activatedObjects.Count; i++)
                    {
                        if (activatedObjects[i] != null)
                        {
                            activatedObjects[i].SetActive(false);
                        }
                    }
                }
            }
        }

        public static bool TryGetArmor(
            Collider collider,
            out VehicleArmorController armor,
            out VehicleArmorController.ArmorZone zone,
            out VehicleRoot root)
        {
            armor = null;
            zone = VehicleArmorController.ArmorZone.Turret;
            root = null;
            if (collider == null
                || !DataByCollider.TryGetValue(collider, out VehicleColliderData data)
                || !data.IsArmor)
            {
                return false;
            }

            armor = data.Armor;
            zone = data.Zone;
            root = data.Root;
            return armor != null && root != null;
        }

        public static bool TryGetData(Collider collider, out VehicleColliderData data)
        {
            if (collider == null)
            {
                data = default;
                return false;
            }

            return DataByCollider.TryGetValue(collider, out data);
        }

        private static void RegisterRoot(VehicleRoot root)
        {
            if (root == null || root.networkObject == null)
            {
                return;
            }

            RootsByObjectId[root.networkObject.ObjectId] = root;
        }

        private static void UnregisterRootIfUnused(VehicleRoot root)
        {
            if (root == null || root.networkObject == null)
            {
                return;
            }

            int networkObjectId = root.networkObject.ObjectId;
            if (!RootsByObjectId.TryGetValue(networkObjectId, out VehicleRoot registeredRoot)
                || registeredRoot != root)
            {
                return;
            }

            foreach (VehicleColliderData data in DataByCollider.Values)
            {
                if (data.Root == root)
                {
                    return;
                }
            }

            RootsByObjectId.Remove(networkObjectId);
            ImpactRenderersByRoot.Remove(root);
            ImpactMeshCollidersByRoot.Remove(root);
            ImpactVisualPartsByRoot.Remove(root);
        }

        private static bool TryGetRendererBoundsSurface(
            List<Renderer> renderers,
            Vector3 impactPoint,
            Vector3 safeNormal,
            out Vector3 surfacePosition,
            out Renderer surfaceRenderer)
        {
            surfacePosition = impactPoint;
            surfaceRenderer = null;
            if (renderers == null)
            {
                return false;
            }

            float closestDistance = float.PositiveInfinity;
            bool foundSurface = false;
            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                Vector3 towardCenter = bounds.center - impactPoint;
                float centerDistance = towardCenter.magnitude;
                if (centerDistance <= ImpactSurfaceDistanceEpsilon)
                {
                    continue;
                }

                Vector3 direction = towardCenter / centerDistance;
                Ray boundsRay = new Ray(
                    impactPoint + direction * ImpactSurfaceDistanceEpsilon,
                    direction);
                if (!bounds.IntersectRay(boundsRay, out float distance))
                {
                    continue;
                }

                distance = Mathf.Max(0f, distance);
                if (distance >= closestDistance)
                {
                    continue;
                }

                Vector3 candidate = boundsRay.origin + boundsRay.direction * distance;
                closestDistance = distance;
                surfacePosition = candidate;
                surfaceRenderer = renderer;
                foundSurface = true;
            }

            return foundSurface;
        }

        private sealed class ImpactVisualSurfaceData
        {
            public readonly List<MeshCollider> MeshColliders = new List<MeshCollider>(4);
            public readonly List<Renderer> Renderers = new List<Renderer>(4);

            public ImpactVisualSurfaceData(MeshCollider[] meshColliders, Renderer[] renderers)
            {
                AddUnique(meshColliders, MeshColliders);
                AddUnique(renderers, Renderers);
            }

            private static void AddUnique<T>(T[] source, List<T> destination)
                where T : Object
            {
                if (source == null)
                {
                    return;
                }

                for (int i = 0; i < source.Length; i++)
                {
                    T item = source[i];
                    if (item != null && !destination.Contains(item))
                    {
                        destination.Add(item);
                    }
                }
            }
        }

        private sealed class ImpactVisualPart
        {
            public readonly MeshCollider MeshCollider;
            public readonly Renderer Renderer;

            public ImpactVisualPart(MeshCollider meshCollider, Renderer renderer)
            {
                MeshCollider = meshCollider;
                Renderer = renderer;
            }
        }
    }

    public readonly struct VehicleColliderData
    {
        public VehicleColliderData(
            VehicleRoot root,
            VehicleArmorController armor,
            VehicleArmorController.ArmorZone zone,
            bool isArmor)
        {
            Root = root;
            Armor = armor;
            Zone = zone;
            IsArmor = isArmor;
        }

        public VehicleRoot Root { get; }
        public VehicleArmorController Armor { get; }
        public VehicleArmorController.ArmorZone Zone { get; }
        public bool IsArmor { get; }
    }
}
