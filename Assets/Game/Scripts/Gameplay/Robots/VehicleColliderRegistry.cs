using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public static class VehicleColliderRegistry
    {
        private static readonly Dictionary<Collider, VehicleColliderData> DataByCollider =
            new Dictionary<Collider, VehicleColliderData>(512);
        private static readonly Dictionary<int, VehicleRoot> RootsByObjectId =
            new Dictionary<int, VehicleRoot>(64);
        private static readonly Dictionary<VehicleRoot, List<Renderer>> ImpactRenderersByRoot =
            new Dictionary<VehicleRoot, List<Renderer>>(64);

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
                if (renderer != null && !registeredRenderers.Contains(renderer))
                {
                    registeredRenderers.Add(renderer);
                }
            }
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
