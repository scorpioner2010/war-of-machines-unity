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
        private const string NetworkPrefabCollectionPath = "Assets/Game/Content/Configuration/Networking/PrefabObjects.asset";
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
                if (string.Equals(assetPath, "Assets/Game/Content/Configuration/Robots/RobotRegistry.asset", StringComparison.OrdinalIgnoreCase))
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
            changed |= SynchronizeReference(entry, "bodyVerticalSpringController", root.bodyVerticalSpringController, GetUniqueComponent<VehicleBodyVerticalSpringController>(entry, root, report), value => root.bodyVerticalSpringController = value, repair, report);
            PositionSync bodyPositionSync = GetUniqueComponent<PositionSync>(entry, root, report);
            if (bodyPositionSync != null)
            {
                changed |= SynchronizeReference(entry, "PositionSync.bodyVerticalSpringController", bodyPositionSync.bodyVerticalSpringController, root.bodyVerticalSpringController, value => bodyPositionSync.bodyVerticalSpringController = value, repair, report);
                if (root.bodyVerticalSpringController != null)
                {
                    Transform expectedChassis = bodyPositionSync.target != null
                        ? bodyPositionSync.target.parent
                        : null;
                    changed |= SynchronizeReference(entry, "VehicleBodyVerticalSpringController.chassis", root.bodyVerticalSpringController.chassis, expectedChassis, value => root.bodyVerticalSpringController.chassis = value, repair, report);
                }
            }
            changed |= SynchronizeReference(entry, "vehicleHUD", root.vehicleHUD, GetUniqueComponent<VehicleHUD>(entry, root, report), value => root.vehicleHUD = value, repair, report);
            changed |= SynchronizeReference(entry, "clientVisibility", root.clientVisibility, GetUniqueComponent<VehicleClientVisibility>(entry, root, report), value => root.clientVisibility = value, repair, report);
            changed |= SynchronizeReference(entry, "botBrain", root.botBrain, GetUniqueComponent<VehicleBotBrain>(entry, root, report), value => root.botBrain = value, repair, report);
            changed |= SynchronizeReference(entry, "hoverOutline", root.hoverOutline, GetUniqueComponent<VehicleHoverOutline>(entry, root, report), value => root.hoverOutline = value, repair, report);

            changed |= RepairClientVisibilityRenderers(entry, root, repair, report);
            changed |= RepairHoverOutlineRenderers(entry, root, repair, report);
            changed |= RepairMovementController(entry, root.objectMover, root, repair, report);
            changed |= RepairBotNavigator(entry, root.botBrain, root, repair, report);
            changed |= RepairRig(entry, root, repair, report);
            changed |= RepairIaWheelContracts(entry, root, repair, report);
            changed |= RepairRootAwareLinks(entry, root, repair, report);
            changed |= RepairMissingMeshReferences(entry, root, repair, report);
            return changed;
        }

        private static bool RepairIaWheelContracts(
            RegisteredPrefab entry,
            VehicleRoot root,
            bool repair,
            IntegrityReport report)
        {
            if (!entry.Code.StartsWith("ia_", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            TrackedVehicleAnimator animator = root.trackedVehicleAnimator;
            DeathLogic deathLogic = GetUniqueComponent<DeathLogic>(entry, root, report);
            List<Transform> leftTransforms = GetWheelTransforms(root.transform, true);
            List<Transform> rightTransforms = GetWheelTransforms(root.transform, false);
            bool changed = false;

            if (animator != null)
            {
                changed |= RepairWheelArray(entry, animator, true, leftTransforms, repair, report);
                changed |= RepairWheelArray(entry, animator, false, rightTransforms, repair, report);
            }

            if (deathLogic == null)
            {
                return changed;
            }

            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            List<DeathLogic.DetachableVisual> expectedVisuals = new List<DeathLogic.DetachableVisual>(renderers.Length);
            Dictionary<Renderer, Behaviour[]> existingBehaviours = GetExistingDeathBehaviours(deathLogic);
            bool physicsNeedsRepair = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (!IsVehiclePresentationRenderer(root, renderer))
                {
                    continue;
                }

                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                MeshCollider meshCollider = renderer.GetComponent<MeshCollider>();
                Rigidbody rigidbody = renderer.GetComponent<Rigidbody>();
                if (meshFilter == null || meshFilter.sharedMesh == null
                    || meshCollider == null || rigidbody == null
                    || !meshCollider.convex || meshCollider.enabled
                    || meshCollider.sharedMesh != meshFilter.sharedMesh || !rigidbody.isKinematic)
                {
                    physicsNeedsRepair = true;
                }

                if (!repair)
                {
                    continue;
                }

                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    report.AddError($"Robot '{entry.Code}' wheel/mesh renderer '{GetHierarchyPath(renderer.transform)}' has no MeshFilter mesh.");
                    continue;
                }

                if (meshCollider == null)
                {
                    meshCollider = renderer.gameObject.AddComponent<MeshCollider>();
                }

                if (rigidbody == null)
                {
                    rigidbody = renderer.gameObject.AddComponent<Rigidbody>();
                }

                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = true;
                meshCollider.enabled = false;
                rigidbody.isKinematic = true;
                rigidbody.useGravity = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(meshCollider);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rigidbody);

                existingBehaviours.TryGetValue(renderer, out Behaviour[] behavioursToDisable);
                expectedVisuals.Add(new DeathLogic.DetachableVisual
                {
                    root = renderer.gameObject,
                    collider = meshCollider,
                    rigidbody = rigidbody,
                    renderer = renderer,
                    meshCollider = meshCollider,
                    behavioursToDisable = behavioursToDisable ?? Array.Empty<Behaviour>()
                });
            }

            if (!repair)
            {
                if (physicsNeedsRepair)
                {
                    report.AddRepairable($"Robot '{entry.Code}' IA rendered mesh physics and DeathLogic wheel contracts");
                }

                if (!DeathVisualsMatch(deathLogic.detachableVisuals, renderers, root))
                {
                    report.AddRepairable($"Robot '{entry.Code}' DeathLogic.detachableVisuals");
                }

                return changed;
            }

            if (!DeathVisualsMatch(deathLogic.detachableVisuals, expectedVisuals))
            {
                deathLogic.detachableVisuals = expectedVisuals.ToArray();
                report.AddAppliedRepair($"Robot '{entry.Code}' DeathLogic.detachableVisuals and IA rendered mesh physics");
                changed = true;
            }

            if (physicsNeedsRepair)
            {
                changed = true;
            }

            return changed;
        }

        private static List<Transform> GetWheelTransforms(Transform root, bool left)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            List<Transform> result = new List<Transform>(transforms.Length);
            string sidePrefix = left ? "LeftWhels" : "RightWhels";

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (!transform.name.StartsWith("WheelA", StringComparison.OrdinalIgnoreCase)
                    || !HasAncestorWithPrefix(transform, sidePrefix))
                {
                    continue;
                }

                result.Add(transform);
            }

            return result;
        }

        private static bool HasAncestorWithPrefix(Transform transform, string prefix)
        {
            Transform current = transform.parent;
            while (current != null)
            {
                if (current.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool RepairWheelArray(
            RegisteredPrefab entry,
            TrackedVehicleAnimator animator,
            bool left,
            List<Transform> expectedTransforms,
            bool repair,
            IntegrityReport report)
        {
            TrackedVehicleAnimator.Wheel[] current = left ? animator.leftWheels : animator.rightWheels;
            List<TrackedVehicleAnimator.Wheel> expected = new List<TrackedVehicleAnimator.Wheel>(expectedTransforms.Count);
            HashSet<Transform> assigned = new HashSet<Transform>();
            float defaultSpeed = left ? 900f : -900f;

            if (current != null)
            {
                for (int i = 0; i < current.Length; i++)
                {
                    TrackedVehicleAnimator.Wheel wheel = current[i];
                    if (wheel == null || wheel.transform == null || !expectedTransforms.Contains(wheel.transform) || !assigned.Add(wheel.transform))
                    {
                        continue;
                    }

                    float magnitude = Mathf.Abs(wheel.rotationSpeed);
                    expected.Add(new TrackedVehicleAnimator.Wheel
                    {
                        transform = wheel.transform,
                        rotationSpeed = magnitude > 0.001f ? Mathf.Sign(defaultSpeed) * magnitude : defaultSpeed
                    });
                }
            }

            for (int i = 0; i < expectedTransforms.Count; i++)
            {
                Transform transform = expectedTransforms[i];
                if (assigned.Add(transform))
                {
                    expected.Add(new TrackedVehicleAnimator.Wheel
                    {
                        transform = transform,
                        rotationSpeed = defaultSpeed
                    });
                }
            }

            bool matches = WheelArrayMatches(current, expected);
            if (matches)
            {
                return false;
            }

            string sideName = left ? "leftWheels" : "rightWheels";
            string description = $"Robot '{entry.Code}' TrackedVehicleAnimator.{sideName}";
            if (!repair)
            {
                report.AddRepairable(description);
                return false;
            }

            if (left)
            {
                animator.leftWheels = expected.ToArray();
            }
            else
            {
                animator.rightWheels = expected.ToArray();
            }

            report.AddAppliedRepair(description);
            return true;
        }

        private static bool WheelArrayMatches(
            TrackedVehicleAnimator.Wheel[] current,
            List<TrackedVehicleAnimator.Wheel> expected)
        {
            if (current == null || current.Length != expected.Count)
            {
                return false;
            }

            for (int i = 0; i < expected.Count; i++)
            {
                if (current[i] == null
                    || current[i].transform != expected[i].transform
                    || !Mathf.Approximately(current[i].rotationSpeed, expected[i].rotationSpeed))
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<Renderer, Behaviour[]> GetExistingDeathBehaviours(DeathLogic deathLogic)
        {
            Dictionary<Renderer, Behaviour[]> result = new Dictionary<Renderer, Behaviour[]>();
            if (deathLogic.detachableVisuals == null)
            {
                return result;
            }

            for (int i = 0; i < deathLogic.detachableVisuals.Length; i++)
            {
                DeathLogic.DetachableVisual visual = deathLogic.detachableVisuals[i];
                if (visual != null && visual.renderer != null && !result.ContainsKey(visual.renderer))
                {
                    result.Add(visual.renderer, visual.behavioursToDisable ?? Array.Empty<Behaviour>());
                }
            }

            return result;
        }

        private static bool DeathVisualsMatch(
            DeathLogic.DetachableVisual[] current,
            MeshRenderer[] renderers,
            VehicleRoot root)
        {
            List<DeathLogic.DetachableVisual> expected = new List<DeathLogic.DetachableVisual>();
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (!IsVehiclePresentationRenderer(root, renderer))
                {
                    continue;
                }

                expected.Add(new DeathLogic.DetachableVisual
                {
                    renderer = renderer,
                    root = renderer.gameObject,
                    collider = renderer.GetComponent<MeshCollider>(),
                    meshCollider = renderer.GetComponent<MeshCollider>(),
                    rigidbody = renderer.GetComponent<Rigidbody>()
                });
            }

            return DeathVisualsMatch(current, expected);
        }

        private static bool DeathVisualsMatch(
            DeathLogic.DetachableVisual[] current,
            List<DeathLogic.DetachableVisual> expected)
        {
            if (current == null || current.Length != expected.Count)
            {
                return false;
            }

            for (int i = 0; i < expected.Count; i++)
            {
                DeathLogic.DetachableVisual actual = current[i];
                DeathLogic.DetachableVisual wanted = expected[i];
                if (actual == null
                    || actual.root != wanted.root
                    || actual.collider != wanted.collider
                    || actual.rigidbody != wanted.rigidbody
                    || actual.renderer != wanted.renderer
                    || actual.meshCollider != wanted.meshCollider)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool RepairClientVisibilityRenderers(RegisteredPrefab entry, VehicleRoot root, bool repair, IntegrityReport report)
        {
            if (root.clientVisibility == null)
            {
                return false;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            List<Renderer> expectedRenderers = new List<Renderer>(renderers.Length);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (IsVehiclePresentationRenderer(root, renderer))
                {
                    expectedRenderers.Add(renderer);
                }
            }

            if (expectedRenderers.Count == 0)
            {
                report.AddError($"Robot '{entry.Code}' has no non-armor renderers available for {nameof(VehicleClientVisibility)}.");
                return false;
            }

            VehicleClientVisibility clientVisibility = root.clientVisibility;
            if (ClientVisibilityRenderersMatch(clientVisibility, expectedRenderers))
            {
                return false;
            }

            string description = $"Robot '{entry.Code}' {nameof(VehicleClientVisibility)}.visualRenderers";
            if (!repair)
            {
                report.AddRepairable(description);
                return false;
            }

            clientVisibility.visualRenderers = expectedRenderers.ToArray();
            report.AddAppliedRepair(description);
            return true;
        }

        private static bool ClientVisibilityRenderersMatch(
            VehicleClientVisibility clientVisibility,
            List<Renderer> expectedRenderers)
        {
            if (clientVisibility.visualRenderers == null
                || clientVisibility.visualRenderers.Length != expectedRenderers.Count)
            {
                return false;
            }

            for (int i = 0; i < expectedRenderers.Count; i++)
            {
                if (clientVisibility.visualRenderers[i] != expectedRenderers[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool RepairHoverOutlineRenderers(RegisteredPrefab entry, VehicleRoot root, bool repair, IntegrityReport report)
        {
            if (root.hoverOutline == null)
            {
                return false;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            List<Renderer> expectedRenderers = new List<Renderer>(renderers.Length);
            List<int> expectedSubMeshCounts = new List<int>(renderers.Length);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!IsVehiclePresentationRenderer(root, renderer))
                {
                    continue;
                }

                expectedRenderers.Add(renderer);
                expectedSubMeshCounts.Add(GetRendererSubMeshCount(renderer));
            }

            if (expectedRenderers.Count == 0)
            {
                report.AddError($"Robot '{entry.Code}' has no non-armor renderers available for {nameof(VehicleHoverOutline)}.");
                return false;
            }

            VehicleHoverOutline hoverOutline = root.hoverOutline;
            if (HoverOutlineMatches(hoverOutline, expectedRenderers, expectedSubMeshCounts))
            {
                return false;
            }

            string description = $"Robot '{entry.Code}' {nameof(VehicleHoverOutline)}.outlineRenderers and outlineSubMeshCounts";
            if (!repair)
            {
                report.AddRepairable(description);
                return false;
            }

            hoverOutline.outlineRenderers = expectedRenderers.ToArray();
            hoverOutline.outlineSubMeshCounts = expectedSubMeshCounts.ToArray();
            report.AddAppliedRepair(description);
            return true;
        }

        private static bool HoverOutlineMatches(
            VehicleHoverOutline hoverOutline,
            List<Renderer> expectedRenderers,
            List<int> expectedSubMeshCounts)
        {
            if (hoverOutline.outlineRenderers == null
                || hoverOutline.outlineSubMeshCounts == null
                || hoverOutline.outlineRenderers.Length != expectedRenderers.Count
                || hoverOutline.outlineSubMeshCounts.Length != expectedSubMeshCounts.Count)
            {
                return false;
            }

            for (int i = 0; i < expectedRenderers.Count; i++)
            {
                if (hoverOutline.outlineRenderers[i] != expectedRenderers[i]
                    || hoverOutline.outlineSubMeshCounts[i] != expectedSubMeshCounts[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsVehiclePresentationRenderer(VehicleRoot root, Renderer renderer)
        {
            if (renderer == null || (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)))
            {
                return false;
            }

            if (root.vehicleHUD != null
                && (renderer.transform == root.vehicleHUD.transform || renderer.transform.IsChildOf(root.vehicleHUD.transform)))
            {
                return false;
            }

            return root.armorController == null || !IsArmorRenderer(root.armorController, renderer);
        }

        private static bool IsArmorRenderer(VehicleArmorController armorController, Renderer renderer)
        {
            return IsBelowAnyCollider(renderer.transform, armorController.turretColliders)
                   || IsBelowAnyCollider(renderer.transform, armorController.hullColliders);
        }

        private static bool IsBelowAnyCollider(Transform rendererTransform, Collider[] colliders)
        {
            if (colliders == null)
            {
                return false;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null
                    && (rendererTransform == collider.transform || rendererTransform.IsChildOf(collider.transform)))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetRendererSubMeshCount(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null)
            {
                return Mathf.Max(1, skinnedMeshRenderer.sharedMesh.subMeshCount);
            }

            if (renderer is MeshRenderer meshRenderer)
            {
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    return Mathf.Max(1, meshFilter.sharedMesh.subMeshCount);
                }
            }

            return Mathf.Max(1, renderer.sharedMaterials.Length);
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
                changed |= ClearReference(entry, "bodySwayController", root.bodySwayController, value => root.bodySwayController = value, repair, report);
                changed |= RepairWalkerIk(entry, root, repair, report);
                return changed;
            }

            if (entry.Code.StartsWith("ia_", StringComparison.OrdinalIgnoreCase))
            {
                bool changed = false;
                changed |= SynchronizeReference(entry, "trackedVehicleAnimator", root.trackedVehicleAnimator, GetUniqueComponent<TrackedVehicleAnimator>(entry, root, report), value => root.trackedVehicleAnimator = value, repair, report);
                changed |= SynchronizeReference(entry, "suspensionController", root.suspensionController, GetUniqueComponent<CaterpillarSuspensionController>(entry, root, report), value => root.suspensionController = value, repair, report);
                changed |= SynchronizeReference(entry, "bodySwayController", root.bodySwayController, GetUniqueComponent<VehicleBodySwayController>(entry, root, report), value => root.bodySwayController = value, repair, report);
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

        private static bool RepairMissingMeshReferences(RegisteredPrefab entry, VehicleRoot root, bool repair, IntegrityReport report)
        {
            bool changed = false;
            MeshCollider[] meshColliders = root.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < meshColliders.Length; i++)
            {
                MeshCollider meshCollider = meshColliders[i];
                if (!HasMissingMeshReference(meshCollider))
                {
                    continue;
                }

                Mesh expectedMesh = GetClosestVisualMesh(meshCollider.transform);
                changed |= SynchronizeMeshReference(entry, meshCollider, expectedMesh, value => meshCollider.sharedMesh = value, repair, report);
            }

            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (!HasMissingMeshReference(meshFilter) || meshFilter.name != "VisualMesh")
                {
                    continue;
                }

                Mesh expectedMesh = GetClosestSiblingMesh(meshFilter.transform);
                changed |= SynchronizeMeshReference(entry, meshFilter, expectedMesh, value => meshFilter.sharedMesh = value, repair, report);
            }

            return changed;
        }

        private static bool HasMissingMeshReference(Component component)
        {
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty("m_Mesh");
            return property != null &&
                   property.propertyType == SerializedPropertyType.ObjectReference &&
                   property.objectReferenceValue == null &&
                   property.objectReferenceInstanceIDValue != 0;
        }

        private static Mesh GetClosestVisualMesh(Transform transform)
        {
            MeshFilter[] meshFilters = transform.GetComponentsInChildren<MeshFilter>(true);
            Mesh nearestMesh = null;
            int nearestDistance = int.MaxValue;
            bool isAmbiguous = false;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter.sharedMesh == null)
                {
                    continue;
                }

                if (meshFilter.transform.parent == transform && meshFilter.name == "VisualMesh")
                {
                    return meshFilter.sharedMesh;
                }

                int distance = GetHierarchyDistance(transform, meshFilter.transform);
                if (distance < nearestDistance)
                {
                    nearestMesh = meshFilter.sharedMesh;
                    nearestDistance = distance;
                    isAmbiguous = false;
                }
                else if (distance == nearestDistance && nearestMesh != meshFilter.sharedMesh)
                {
                    isAmbiguous = true;
                }
            }

            return isAmbiguous ? null : nearestMesh;
        }

        private static Mesh GetClosestSiblingMesh(Transform transform)
        {
            Transform parent = transform.parent;
            if (parent == null)
            {
                return null;
            }

            MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>(true);
            Mesh nearestMesh = null;
            int nearestDistance = int.MaxValue;
            bool isAmbiguous = false;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter.transform == transform ||
                    meshFilter.sharedMesh == null ||
                    !meshFilter.name.StartsWith("VisualMesh", StringComparison.Ordinal))
                {
                    continue;
                }

                int distance = GetHierarchyDistance(parent, meshFilter.transform);
                if (distance < nearestDistance)
                {
                    nearestMesh = meshFilter.sharedMesh;
                    nearestDistance = distance;
                    isAmbiguous = false;
                }
                else if (distance == nearestDistance && nearestMesh != meshFilter.sharedMesh)
                {
                    isAmbiguous = true;
                }
            }

            return isAmbiguous ? null : nearestMesh;
        }

        private static int GetHierarchyDistance(Transform ancestor, Transform descendant)
        {
            int distance = 0;
            Transform current = descendant;
            while (current != null && current != ancestor)
            {
                distance++;
                current = current.parent;
            }

            return current == ancestor ? distance : int.MaxValue;
        }

        private static bool SynchronizeMeshReference(
            RegisteredPrefab entry,
            Component component,
            Mesh expectedMesh,
            Action<Mesh> assign,
            bool repair,
            IntegrityReport report)
        {
            string description = $"Robot '{entry.Code}' {GetHierarchyPath(component.transform)} {component.GetType().Name}.sharedMesh";
            if (expectedMesh == null)
            {
                report.AddError($"{description} has no unambiguous visual mesh source.");
                return false;
            }

            if (!repair)
            {
                report.AddRepairable(description);
                return false;
            }

            assign(expectedMesh);
            report.AddAppliedRepair(description);
            return true;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            return path;
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
