using System;
using System.Collections.Generic;
using FishNet.Object;
using Game.Scripts.AI.WaypointGraph;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Gameplay.Robots.t1;
using Game.Scripts.Gameplay.Robots.t2;
using Game.Scripts.UI.HUD;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Game.Scripts.Editor
{
    internal static class VehiclePrefabIntegrityEditor
    {
        private const string NetworkPrefabCollectionPath = "Assets/Game/GameResources/PrefabObjects.asset";
        private static readonly HashSet<string> QueuedAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static bool _repairAllQueued;
        private static bool _repairQueued;
        private static bool _isRepairing;

        [MenuItem("Tools/WOM/Robots/Validate Registered Prefabs")]
        private static void ValidateRegisteredPrefabs()
        {
            IntegrityReport registryReport = new IntegrityReport();
            List<RegisteredPrefab> entries = GetRegisteredPrefabs(registryReport);
            ValidateNetworkPrefabCollection(entries, registryReport);
            ProcessPrefabs(entries, false, registryReport);
            registryReport.Log("validation");
        }

        [MenuItem("Tools/WOM/Robots/Repair Registered Prefabs")]
        private static void RepairRegisteredPrefabs()
        {
            RepairAllRegisteredPrefabs(true);
        }

        internal static void RepairAndValidateFromBatchMode()
        {
            RepairAllRegisteredPrefabs(true);
        }

        internal static void QueueRepairForChangedAssets(string[] importedAssets)
        {
            if (_isRepairing || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (importedAssets == null)
            {
                return;
            }

            for (int i = 0; i < importedAssets.Length; i++)
            {
                string assetPath = importedAssets[i];
                if (string.Equals(assetPath, "Assets/Game/GameResources/RobotRegistry.asset", StringComparison.OrdinalIgnoreCase))
                {
                    _repairAllQueued = true;
                    break;
                }

                if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    QueuedAssetPaths.Add(assetPath);
                }
            }

            QueueRepair();
        }

        internal static void QueueFullRepair()
        {
            if (_isRepairing || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            _repairAllQueued = true;
            QueueRepair();
        }

        private static void QueueRepair()
        {
            if (_repairQueued)
            {
                return;
            }

            _repairQueued = true;
            EditorApplication.delayCall += RunQueuedRepair;
        }

        private static void RunQueuedRepair()
        {
            _repairQueued = false;
            if (_isRepairing || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            IntegrityReport registryReport = new IntegrityReport();
            List<RegisteredPrefab> entries = GetRegisteredPrefabs(registryReport);
            List<RegisteredPrefab> entriesToRepair = GetAffectedPrefabs(entries);
            _repairAllQueued = false;
            QueuedAssetPaths.Clear();

            if (entriesToRepair.Count == 0)
            {
                return;
            }

            ProcessPrefabs(entriesToRepair, true, registryReport);
            if (registryReport.AppliedRepairs > 0 || registryReport.HasErrors)
            {
                registryReport.Log("automatic repair");
            }
        }

        private static void RepairAllRegisteredPrefabs(bool logResult)
        {
            IntegrityReport repairReport = new IntegrityReport();
            List<RegisteredPrefab> entries = GetRegisteredPrefabs(repairReport);
            ValidateNetworkPrefabCollection(entries, repairReport);
            ProcessPrefabs(entries, true, repairReport);

            IntegrityReport validationReport = new IntegrityReport();
            List<RegisteredPrefab> validationEntries = GetRegisteredPrefabs(validationReport);
            ValidateNetworkPrefabCollection(validationEntries, validationReport);
            ProcessPrefabs(validationEntries, false, validationReport);

            if (logResult)
            {
                repairReport.Log("repair");
                validationReport.Log("post-repair validation");
            }
        }

        private static List<RegisteredPrefab> GetAffectedPrefabs(List<RegisteredPrefab> entries)
        {
            if (_repairAllQueued)
            {
                return entries;
            }

            List<RegisteredPrefab> affected = new List<RegisteredPrefab>();
            for (int i = 0; i < entries.Count; i++)
            {
                RegisteredPrefab entry = entries[i];
                if (IsAffectedByQueuedAsset(entry.AssetPath))
                {
                    affected.Add(entry);
                }
            }

            return affected;
        }

        private static bool IsAffectedByQueuedAsset(string prefabPath)
        {
            if (QueuedAssetPaths.Contains(prefabPath))
            {
                return true;
            }

            string[] dependencies = AssetDatabase.GetDependencies(prefabPath, true);
            for (int i = 0; i < dependencies.Length; i++)
            {
                if (QueuedAssetPaths.Contains(dependencies[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<RegisteredPrefab> GetRegisteredPrefabs(IntegrityReport report)
        {
            List<RegisteredPrefab> entries = new List<RegisteredPrefab>();
            string[] registryGuids = AssetDatabase.FindAssets("t:RobotRegistry");
            if (registryGuids.Length == 0)
            {
                report.AddError("RobotRegistry asset was not found.");
                return entries;
            }

            HashSet<string> vehicleCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> prefabPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int registryIndex = 0; registryIndex < registryGuids.Length; registryIndex++)
            {
                string registryPath = AssetDatabase.GUIDToAssetPath(registryGuids[registryIndex]);
                global::RobotRegistry registry = AssetDatabase.LoadAssetAtPath<global::RobotRegistry>(registryPath);
                if (registry == null || registry.items == null)
                {
                    report.AddError($"RobotRegistry at '{registryPath}' is empty or unreadable.");
                    continue;
                }

                for (int itemIndex = 0; itemIndex < registry.items.Count; itemIndex++)
                {
                    global::RobotRegistry.Item item = registry.items[itemIndex];
                    if (item == null || string.IsNullOrWhiteSpace(item.code))
                    {
                        report.AddError($"RobotRegistry at '{registryPath}' has an item without a code.");
                        continue;
                    }

                    if (!vehicleCodes.Add(item.code))
                    {
                        report.AddError($"RobotRegistry contains duplicate vehicle code '{item.code}'.");
                        continue;
                    }

                    if (item.prefab == null)
                    {
                        report.AddError($"Robot '{item.code}' has no VehicleRoot prefab.");
                        continue;
                    }

                    if (item.icon == null)
                    {
                        report.AddError($"Robot '{item.code}' has no menu icon.");
                    }

                    string prefabPath = AssetDatabase.GetAssetPath(item.prefab);
                    if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        report.AddError($"Robot '{item.code}' points to an invalid prefab asset.");
                        continue;
                    }

                    if (!prefabPaths.Add(prefabPath))
                    {
                        report.AddError($"Robot '{item.code}' duplicates prefab '{prefabPath}'.");
                        continue;
                    }

                    entries.Add(new RegisteredPrefab(item.code, prefabPath));
                }
            }

            return entries;
        }

        private static void ValidateNetworkPrefabCollection(List<RegisteredPrefab> entries, IntegrityReport report)
        {
            if (!AssetDatabase.LoadMainAssetAtPath(NetworkPrefabCollectionPath))
            {
                report.AddError($"FishNet prefab collection was not found at '{NetworkPrefabCollectionPath}'.");
                return;
            }

            HashSet<string> collectionDependencies = new HashSet<string>(
                AssetDatabase.GetDependencies(NetworkPrefabCollectionPath, true),
                StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.Count; i++)
            {
                RegisteredPrefab entry = entries[i];
                if (!collectionDependencies.Contains(entry.AssetPath))
                {
                    report.AddError($"Robot '{entry.Code}' is missing from FishNet prefab collection '{NetworkPrefabCollectionPath}'.");
                }
            }
        }

        private static void ProcessPrefabs(List<RegisteredPrefab> entries, bool repair, IntegrityReport report)
        {
            if (entries.Count == 0)
            {
                return;
            }

            _isRepairing = repair;
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    ProcessPrefab(entries[i], repair, report);
                }

                if (repair && report.ChangedPrefabCount > 0)
                {
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                _isRepairing = false;
            }
        }

        private static void ProcessPrefab(RegisteredPrefab entry, bool repair, IntegrityReport report)
        {
            GameObject prefabContents = null;
            try
            {
                prefabContents = PrefabUtility.LoadPrefabContents(entry.AssetPath);
                VehicleRoot root = prefabContents.GetComponent<VehicleRoot>();
                if (root == null)
                {
                    report.AddError($"Robot '{entry.Code}' has no VehicleRoot on its prefab root.");
                    return;
                }

                report.CheckedPrefabCount++;
                bool changed = RepairPrefab(entry, root, repair, report);
                if (repair && changed)
                {
                    EditorUtility.SetDirty(root);
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, entry.AssetPath);
                    report.ChangedPrefabCount++;
                }
            }
            catch (Exception exception)
            {
                report.AddError($"Robot '{entry.Code}' could not be processed: {exception.Message}");
            }
            finally
            {
                if (prefabContents != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }
        }

        private static bool RepairPrefab(RegisteredPrefab entry, VehicleRoot root, bool repair, IntegrityReport report)
        {
            bool changed = false;
            ValidateMissingScripts(entry, root, report);
            ValidateConfiguredComponentArray(entry, root, "rootAwareBehaviours", report);
            ValidateConfiguredComponentArray(entry, root, "initializableBehaviours", report);
            ValidateConfiguredComponentArray(entry, root, "statsConsumerBehaviours", report);

            NetworkObject networkObject = root.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                report.AddError($"Robot '{entry.Code}' is missing NetworkObject on the prefab root.");
            }
            else
            {
                changed |= SynchronizeReference(entry, "networkObject", root.networkObject, networkObject, value => root.networkObject = value, repair, report);
            }

            changed |= SynchronizeReference(entry, "characterInit", root.characterInit, GetUniqueComponent<VehicleNetworkInitializer>(entry, root, report), value => root.characterInit = value, repair, report);
            changed |= SynchronizeReference(entry, "inputManager", root.inputManager, GetUniqueComponent<VehicleInputController>(entry, root, report), value => root.inputManager = value, repair, report);
            changed |= SynchronizeReference(entry, "autoAimController", root.autoAimController, GetUniqueComponent<VehicleAutoAimController>(entry, root, report), value => root.autoAimController = value, repair, report);
            changed |= SynchronizeReference(entry, "health", root.health, GetUniqueComponent<VehicleHealth>(entry, root, report), value => root.health = value, repair, report);
            changed |= SynchronizeReference(entry, "objectMover", root.objectMover, GetUniqueComponent<VehicleMovementController>(entry, root, report), value => root.objectMover = value, repair, report);
            changed |= SynchronizeReference(entry, "uiSenerd", root.uiSenerd, GetUniqueComponent<VehicleHudInitializer>(entry, root, report), value => root.uiSenerd = value, repair, report);
            changed |= SynchronizeReference(entry, "cameraController", root.cameraController, GetUniqueComponent<CameraController>(entry, root, report), value => root.cameraController = value, repair, report);
            changed |= SynchronizeReference(entry, "robotHullRotation", root.robotHullRotation, GetUniqueComponent<VehicleTurretRotationController>(entry, root, report), value => root.robotHullRotation = value, repair, report);
            changed |= SynchronizeReference(entry, "weaponAimAtCamera", root.weaponAimAtCamera, GetUniqueComponent<WeaponAimController>(entry, root, report), value => root.weaponAimAtCamera = value, repair, report);
            changed |= SynchronizeReference(entry, "gunReticleUIFollower", root.gunReticleUIFollower, GetUniqueComponent<WeaponReticlePresenter>(entry, root, report), value => root.gunReticleUIFollower = value, repair, report);
            changed |= SynchronizeReference(entry, "shooterNet", root.shooterNet, GetUniqueComponent<NetworkWeaponShooter>(entry, root, report), value => root.shooterNet = value, repair, report);
            changed |= SynchronizeReference(entry, "weaponReloadController", root.weaponReloadController, GetUniqueComponent<WeaponReloadController>(entry, root, report), value => root.weaponReloadController = value, repair, report);
            changed |= SynchronizeReference(entry, "armorController", root.armorController, GetUniqueComponent<VehicleArmorController>(entry, root, report), value => root.armorController = value, repair, report);
            changed |= SynchronizeReference(entry, "groundAlignmentController", root.groundAlignmentController, GetUniqueComponent<VehicleGroundAlignmentController>(entry, root, report), value => root.groundAlignmentController = value, repair, report);
            changed |= SynchronizeReference(entry, "vehicleHUD", root.vehicleHUD, GetUniqueComponent<VehicleHUD>(entry, root, report), value => root.vehicleHUD = value, repair, report);
            changed |= SynchronizeReference(entry, "clientVisibility", root.clientVisibility, GetUniqueComponent<VehicleClientVisibility>(entry, root, report), value => root.clientVisibility = value, repair, report);
            changed |= SynchronizeReference(entry, "botBrain", root.botBrain, GetUniqueComponent<VehicleBotBrain>(entry, root, report), value => root.botBrain = value, repair, report);
            changed |= SynchronizeReference(entry, "hoverOutline", root.hoverOutline, GetUniqueComponent<VehicleHoverOutline>(entry, root, report), value => root.hoverOutline = value, repair, report);

            changed |= RepairMovementController(entry, root.objectMover, root, repair, report);
            changed |= RepairBotNavigator(entry, root.botBrain, root, repair, report);
            changed |= RepairRig(entry, root, repair, report);
            changed |= RepairRootAwareLinks(entry, root, repair, report);
            return changed;
        }

        private static bool RepairMovementController(
            RegisteredPrefab entry,
            VehicleMovementController movementController,
            VehicleRoot root,
            bool repair,
            IntegrityReport report)
        {
            if (movementController == null)
            {
                return false;
            }

            CharacterController controller = movementController.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = GetUniqueComponent<CharacterController>(entry, root, report);
            }

            if (controller == null)
            {
                return false;
            }

            return SynchronizeReference(
                entry,
                "VehicleMovementController.controller",
                movementController.controller,
                controller,
                value => movementController.controller = value,
                repair,
                report);
        }

        private static bool RepairBotNavigator(
            RegisteredPrefab entry,
            VehicleBotBrain botBrain,
            VehicleRoot root,
            bool repair,
            IntegrityReport report)
        {
            if (botBrain == null)
            {
                return false;
            }

            BotNavigator navigator = GetUniqueComponent<BotNavigator>(entry, root, report);
            return SynchronizeReference(
                entry,
                "VehicleBotBrain.navigator",
                botBrain.navigator,
                navigator,
                value => botBrain.navigator = value,
                repair,
                report);
        }

        private static bool RepairRig(RegisteredPrefab entry, VehicleRoot root, bool repair, IntegrityReport report)
        {
            if (entry.Code.StartsWith("nv_", StringComparison.OrdinalIgnoreCase))
            {
                bool changed = false;
                changed |= SynchronizeReference(entry, "walkerAnimationController", root.walkerAnimationController, GetUniqueComponent<WalkerAnimationController>(entry, root, report), value => root.walkerAnimationController = value, repair, report);
                changed |= ClearReference(entry, "trackedVehicleAnimator", root.trackedVehicleAnimator, value => root.trackedVehicleAnimator = value, repair, report);
                changed |= ClearReference(entry, "suspensionController", root.suspensionController, value => root.suspensionController = value, repair, report);
                changed |= RepairWalkerIk(entry, root, repair, report);
                return changed;
            }

            if (entry.Code.StartsWith("ia_", StringComparison.OrdinalIgnoreCase))
            {
                bool changed = false;
                changed |= SynchronizeReference(entry, "trackedVehicleAnimator", root.trackedVehicleAnimator, GetUniqueComponent<TrackedVehicleAnimator>(entry, root, report), value => root.trackedVehicleAnimator = value, repair, report);
                changed |= SynchronizeReference(entry, "suspensionController", root.suspensionController, GetUniqueComponent<CaterpillarSuspensionController>(entry, root, report), value => root.suspensionController = value, repair, report);
                changed |= ClearReference(entry, "walkerAnimationController", root.walkerAnimationController, value => root.walkerAnimationController = value, repair, report);
                return changed;
            }

            report.AddWarning($"Robot '{entry.Code}' has an unknown rig prefix; rig-specific validation was skipped.");
            return false;
        }

        private static bool RepairWalkerIk(RegisteredPrefab entry, VehicleRoot root, bool repair, IntegrityReport report)
        {
            bool changed = false;
            TwoBoneIKConstraint[] constraints = root.GetComponentsInChildren<TwoBoneIKConstraint>(true);
            if (constraints.Length == 0)
            {
                report.AddError($"Walker robot '{entry.Code}' has no TwoBoneIKConstraint components.");
                return false;
            }

            for (int i = 0; i < constraints.Length; i++)
            {
                TwoBoneIKConstraint constraint = constraints[i];
                TwoBoneIKConstraintData data = constraint.data;
                if (data.root == null || data.mid == null || data.tip == null || data.target == null || data.hint == null)
                {
                    report.AddError($"Walker robot '{entry.Code}' has an incomplete TwoBoneIKConstraint on '{constraint.name}'.");
                    continue;
                }

                changed |= SynchronizeIkHorizontalPosition(entry, constraint.name, "target", data.root, data.target, repair, report);
                changed |= SynchronizeIkHorizontalPosition(entry, constraint.name, "hint", data.root, data.hint, repair, report);
            }

            return changed;
        }

        private static bool SynchronizeIkHorizontalPosition(
            RegisteredPrefab entry,
            string constraintName,
            string transformRole,
            Transform boneRoot,
            Transform target,
            bool repair,
            IntegrityReport report)
        {
            if (boneRoot.parent != target.parent)
            {
                report.AddWarning($"Walker robot '{entry.Code}' has a TwoBoneIK {transformRole} outside the root sibling plane on '{constraintName}'; automatic X alignment was skipped.");
                return false;
            }

            if (Mathf.Approximately(boneRoot.localPosition.x, target.localPosition.x))
            {
                return false;
            }

            string description = $"Robot '{entry.Code}' {constraintName}.{transformRole}.localPosition.x -> {boneRoot.localPosition.x:F3}";
            if (!repair)
            {
                report.AddRepairable(description);
                return false;
            }

            Vector3 position = target.localPosition;
            position.x = boneRoot.localPosition.x;
            target.localPosition = position;
            report.AddAppliedRepair(description);
            return true;
        }

        private static bool RepairRootAwareLinks(RegisteredPrefab entry, VehicleRoot root, bool repair, IntegrityReport report)
        {
            bool changed = false;
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !(behaviour is IVehicleRootAware))
                {
                    continue;
                }

                string propertyName = behaviour is VehicleNetworkInitializer ? "playerRoot" : "vehicleRoot";
                SerializedObject serializedObject = new SerializedObject(behaviour);
                SerializedProperty property = serializedObject.FindProperty(propertyName);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (property.objectReferenceValue == root)
                {
                    continue;
                }

                string description = $"Robot '{entry.Code}' {behaviour.GetType().Name}.{propertyName}";
                if (!repair)
                {
                    report.AddRepairable(description);
                    continue;
                }

                property.objectReferenceValue = root;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                report.AddAppliedRepair(description);
                changed = true;
            }

            return changed;
        }

        private static void ValidateMissingScripts(RegisteredPrefab entry, VehicleRoot root, IntegrityReport report)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    report.AddError($"Robot '{entry.Code}' has a missing script component.");
                    return;
                }
            }
        }

        private static void ValidateConfiguredComponentArray(
            RegisteredPrefab entry,
            VehicleRoot root,
            string propertyName,
            IntegrityReport report)
        {
            SerializedObject serializedObject = new SerializedObject(root);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                report.AddError($"Robot '{entry.Code}' is missing VehicleRoot.{propertyName}.");
                return;
            }

            for (int i = 0; i < property.arraySize; i++)
            {
                Component component = property.GetArrayElementAtIndex(i).objectReferenceValue as Component;
                if (component == null)
                {
                    report.AddError($"Robot '{entry.Code}' VehicleRoot.{propertyName}[{i}] is missing.");
                    continue;
                }

                if (component.transform != root.transform && !component.transform.IsChildOf(root.transform))
                {
                    report.AddError($"Robot '{entry.Code}' VehicleRoot.{propertyName}[{i}] points outside the prefab.");
                }
            }
        }

        private static T GetUniqueComponent<T>(RegisteredPrefab entry, VehicleRoot root, IntegrityReport report)
            where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            if (components.Length == 1)
            {
                return components[0];
            }

            if (components.Length == 0)
            {
                report.AddError($"Robot '{entry.Code}' is missing required component {typeof(T).Name}.");
            }
            else
            {
                report.AddError($"Robot '{entry.Code}' has {components.Length} components of required type {typeof(T).Name}; automatic binding is ambiguous.");
            }

            return null;
        }

        private static bool SynchronizeReference<T>(
            RegisteredPrefab entry,
            string fieldName,
            T current,
            T expected,
            Action<T> assign,
            bool repair,
            IntegrityReport report)
            where T : Component
        {
            if (expected == null || current == expected)
            {
                return false;
            }

            string description = $"Robot '{entry.Code}' VehicleRoot.{fieldName}";
            if (!repair)
            {
                report.AddRepairable(description);
                return false;
            }

            assign(expected);
            report.AddAppliedRepair(description);
            return true;
        }

        private static bool ClearReference<T>(
            RegisteredPrefab entry,
            string fieldName,
            T current,
            Action<T> assign,
            bool repair,
            IntegrityReport report)
            where T : Component
        {
            if (current == null)
            {
                return false;
            }

            string description = $"Robot '{entry.Code}' VehicleRoot.{fieldName} must be empty for this rig";
            if (!repair)
            {
                report.AddRepairable(description);
                return false;
            }

            assign(null);
            report.AddAppliedRepair(description);
            return true;
        }

        private readonly struct RegisteredPrefab
        {
            public RegisteredPrefab(string code, string assetPath)
            {
                Code = code;
                AssetPath = assetPath;
            }

            public string Code { get; }
            public string AssetPath { get; }
        }

        private sealed class IntegrityReport
        {
            private readonly List<string> _errors = new List<string>();
            private readonly List<string> _warnings = new List<string>();

            public int CheckedPrefabCount { get; set; }
            public int ChangedPrefabCount { get; set; }
            public int AppliedRepairs { get; private set; }
            public int RepairableIssues { get; private set; }
            public bool HasErrors => _errors.Count > 0;

            public void AddAppliedRepair(string description)
            {
                AppliedRepairs++;
            }

            public void AddRepairable(string description)
            {
                RepairableIssues++;
            }

            public void AddError(string description)
            {
                _errors.Add(description);
            }

            public void AddWarning(string description)
            {
                _warnings.Add(description);
            }

            public void Log(string action)
            {
                if (!HasErrors && RepairableIssues == 0)
                {
                    Debug.Log($"[VehiclePrefabIntegrity] {action}: checked {CheckedPrefabCount} registered prefabs; repaired {AppliedRepairs} bindings across {ChangedPrefabCount} prefabs; validation passed.");
                }
                else
                {
                    Debug.LogError($"[VehiclePrefabIntegrity] {action}: checked {CheckedPrefabCount} registered prefabs; repaired {AppliedRepairs} bindings across {ChangedPrefabCount} prefabs; remaining repairable issues: {RepairableIssues}; errors: {_errors.Count}.");
                }

                for (int i = 0; i < _warnings.Count; i++)
                {
                    Debug.LogWarning($"[VehiclePrefabIntegrity] {_warnings[i]}");
                }

                for (int i = 0; i < _errors.Count; i++)
                {
                    Debug.LogError($"[VehiclePrefabIntegrity] {_errors[i]}");
                }
            }
        }
    }

    [InitializeOnLoad]
    internal static class VehiclePrefabIntegrityAutoRepair
    {
        static VehiclePrefabIntegrityAutoRepair()
        {
            EditorApplication.delayCall += VehiclePrefabIntegrityEditor.QueueFullRepair;
        }
    }

    internal sealed class VehiclePrefabIntegrityPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            VehiclePrefabIntegrityEditor.QueueRepairForChangedAssets(importedAssets);
        }
    }

    public static class VehiclePrefabIntegrityBatch
    {
        public static void RepairAndValidate()
        {
            VehiclePrefabIntegrityEditor.RepairAndValidateFromBatchMode();
        }
    }
}
