using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    public sealed class CameraRuntimeZoomOverridePlayModeTests
    {
        [Test]
        public void EnabledOverrideMovesCameraToConfiguredRuntimeZoomIndex()
        {
            GameObject settingsObject = new GameObject("ClientSettingsRuntimeZoomOverrideTest");
            GameObject cameraObject = new GameObject("CameraRuntimeZoomOverrideTest");
            FieldInfo clientSettingsIn = null;
            object previousClientSettings = null;

            try
            {
                Type clientSettingsType = GetType("Game.Scripts.Client.ClientSettings, Assembly-CSharp");
                Type runtimeSettingsType = GetType("Game.Scripts.Client.GameplayRuntimeSettings, Assembly-CSharp");
                Type profileType = GetType("Game.Scripts.Client.CameraZoomProfile, Assembly-CSharp");
                Type cameraControllerType = GetType("Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");

                clientSettingsIn = clientSettingsType.GetField("In", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(clientSettingsIn);
                previousClientSettings = clientSettingsIn.GetValue(null);

                Component clientSettings = settingsObject.AddComponent(clientSettingsType);
                object runtimeSettings = CreateRuntimeSettings(runtimeSettingsType, profileType);
                SetField(runtimeSettings, "runtimeZoomOverrideEnabled", true);
                SetField(runtimeSettings, "runtimeZoomIndex", 0);
                SetField(clientSettings, "gameplayRuntime", runtimeSettings);

                Component cameraController = cameraObject.AddComponent(cameraControllerType);
                MethodInfo applyOverride = cameraControllerType.GetMethod(
                    "ApplyRuntimeZoomOverride",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                PropertyInfo currentZoomStep = cameraControllerType.GetProperty(
                    "CurrentZoomStep",
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(applyOverride);
                Assert.IsNotNull(currentZoomStep);

                SetField(runtimeSettings, "runtimeZoomIndex", 1);
                bool applied = (bool)applyOverride.Invoke(cameraController, new object[] { false });

                Assert.IsTrue(applied);
                Assert.That((int)currentZoomStep.GetValue(cameraController), Is.EqualTo(1));

                SetField(runtimeSettings, "runtimeZoomIndex", 99);
                bool invalidApplied = (bool)applyOverride.Invoke(cameraController, new object[] { false });

                Assert.IsFalse(invalidApplied);
                Assert.That((int)currentZoomStep.GetValue(cameraController), Is.EqualTo(1));

                SetField(runtimeSettings, "runtimeZoomOverrideEnabled", false);
                bool disabledApplied = (bool)applyOverride.Invoke(cameraController, new object[] { false });

                Assert.IsFalse(disabledApplied);
                Assert.That((int)currentZoomStep.GetValue(cameraController), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(settingsObject);
                if (clientSettingsIn != null)
                {
                    clientSettingsIn.SetValue(null, previousClientSettings);
                }
            }
        }

        [Test]
        public void RuntimeZoomOverrideFieldsSurviveValidationAndCopy()
        {
            Type runtimeSettingsType = GetType("Game.Scripts.Client.GameplayRuntimeSettings, Assembly-CSharp");
            object source = Activator.CreateInstance(runtimeSettingsType);
            object target = Activator.CreateInstance(runtimeSettingsType);

            SetField(source, "runtimeZoomOverrideEnabled", true);
            SetField(source, "runtimeZoomIndex", -2);
            Invoke(source, "Validate");

            Invoke(target, "CopyFrom", source);

            Assert.IsTrue((bool)GetField(source, "runtimeZoomOverrideEnabled"));
            Assert.That((int)GetField(source, "runtimeZoomIndex"), Is.EqualTo(-2));
            Assert.IsTrue((bool)GetField(target, "runtimeZoomOverrideEnabled"));
            Assert.That((int)GetField(target, "runtimeZoomIndex"), Is.EqualTo(-2));
        }

        [Test]
        public void SpectatorVisibilityOverrideRevealsHiddenTargetAndRestoresMapVisibility()
        {
            GameObject vehicleObject = new GameObject("SpectatorVisibilityVehicle");
            GameObject visualObject = new GameObject("SpectatorVisibilityVisual");
            GameObject cameraObject = new GameObject("SpectatorVisibilityCamera");
            try
            {
                Type vehicleRootType = GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
                Type visibilityType = GetType("Game.Scripts.Gameplay.Robots.VehicleClientVisibility, Assembly-CSharp");
                Type cameraControllerType = GetType("Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");
                visualObject.transform.SetParent(vehicleObject.transform, false);
                MeshRenderer renderer = visualObject.AddComponent<MeshRenderer>();
                Component vehicleRoot = vehicleObject.AddComponent(vehicleRootType);
                Component visibility = vehicleObject.AddComponent(visibilityType);
                Component cameraController = cameraObject.AddComponent(cameraControllerType);
                SetField(vehicleRoot, "clientVisibility", visibility);
                SetField(visibility, "visualRenderers", new[] { renderer });
                Invoke(visibility, "SetVehicleRoot", vehicleRoot);
                Invoke(visibility, "SetVisibilityOverride", true, false);
                Invoke(visibility, "OnVehicleClientStarted");

                Assert.IsFalse((bool)GetProperty(visibility, "IsVisible"));
                Assert.IsTrue(renderer.forceRenderingOff);

                Invoke(cameraController, "UpdateSpectatorVisibilityOverride", vehicleRoot);

                Assert.IsTrue((bool)GetProperty(visibility, "IsVisible"));
                Assert.IsFalse(renderer.forceRenderingOff);

                Invoke(cameraController, "UpdateSpectatorVisibilityOverride", new object[] { null });

                Assert.IsFalse((bool)GetProperty(visibility, "IsVisible"));
                Assert.IsTrue(renderer.forceRenderingOff);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(vehicleObject);
                UnityEngine.Object.DestroyImmediate(visualObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void SpectatorVisibilityOverrideSurvivesClientStartRefresh()
        {
            GameObject vehicleObject = new GameObject("SpectatorStartVisibilityVehicle");
            GameObject visualObject = new GameObject("SpectatorStartVisibilityVisual");
            GameObject cameraObject = new GameObject("SpectatorStartVisibilityCamera");
            try
            {
                Type vehicleRootType = GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
                Type visibilityType = GetType("Game.Scripts.Gameplay.Robots.VehicleClientVisibility, Assembly-CSharp");
                Type cameraControllerType = GetType("Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");
                visualObject.transform.SetParent(vehicleObject.transform, false);
                MeshRenderer renderer = visualObject.AddComponent<MeshRenderer>();
                Component vehicleRoot = vehicleObject.AddComponent(vehicleRootType);
                Component visibility = vehicleObject.AddComponent(visibilityType);
                Component cameraController = cameraObject.AddComponent(cameraControllerType);
                SetField(vehicleRoot, "clientVisibility", visibility);
                SetField(visibility, "visualRenderers", new[] { renderer });
                Invoke(visibility, "SetVehicleRoot", vehicleRoot);
                Invoke(visibility, "SetVisibilityOverride", true, false);

                Invoke(cameraController, "UpdateSpectatorVisibilityOverride", vehicleRoot);
                Invoke(visibility, "OnVehicleClientStarted");

                Assert.IsTrue((bool)GetProperty(visibility, "IsVisible"));
                Assert.IsFalse(renderer.forceRenderingOff);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(vehicleObject);
                UnityEngine.Object.DestroyImmediate(visualObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void DeathDetachPreservesSiblingCameraRigOutsideDetachedCameraPoint()
        {
            GameObject vehicleObject = new GameObject("DeathDetachVehicle");
            GameObject cameraObject = new GameObject("DeathDetachCameraPoint");
            GameObject rigObject = new GameObject("DeathDetachCameraRig");
            try
            {
                Type cameraControllerType = GetType("Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");
                cameraObject.transform.SetParent(vehicleObject.transform, false);
                rigObject.transform.SetParent(vehicleObject.transform, false);
                Component cameraController = cameraObject.AddComponent(cameraControllerType);
                SetField(cameraController, "rig", rigObject.transform);

                Invoke(cameraController, "DetachFromVehicleForDeath");
                UnityEngine.Object.DestroyImmediate(vehicleObject);

                Assert.IsNotNull(cameraObject);
                Assert.IsNull(rigObject.transform.parent);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(rigObject);
                UnityEngine.Object.DestroyImmediate(vehicleObject);
            }
        }

        [Test]
        public void DeathWreckCameraDoesNotMoveItsDetachedPivot()
        {
            GameObject vehicleObject = new GameObject("StableWreckVehicle");
            GameObject cameraObject = new GameObject("StableWreckCamera");
            GameObject rigObject = new GameObject("StableWreckRig");
            try
            {
                Type cameraControllerType = GetType(
                    "Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");
                rigObject.transform.position = new Vector3(4f, 2f, 8f);
                cameraObject.transform.SetParent(vehicleObject.transform, true);
                Component cameraController = cameraObject.AddComponent(cameraControllerType);
                SetField(cameraController, "rig", rigObject.transform);
                Invoke(cameraController, "DetachFromVehicleForDeath");
                SetField(
                    cameraController,
                    "_deathSpectatorPhase",
                    Enum.Parse(cameraControllerType.GetNestedType(
                        "DeathSpectatorPhase",
                        BindingFlags.NonPublic), "DeathWreck"));

                Vector3 pivotPosition = rigObject.transform.position;
                Invoke(cameraController, "ApplyCameraTransform", true);
                Invoke(cameraController, "ApplyCameraTransform", true);

                Assert.That(rigObject.transform.position, Is.EqualTo(pivotPosition));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(vehicleObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(rigObject);
            }
        }

        [Test]
        public void DeathSpectatorUsesDetachedCameraPointWhenWreckRigIsDestroyed()
        {
            GameObject cameraObject = new GameObject("DestroyedWreckRigCamera");
            GameObject rigObject = new GameObject("DestroyedWreckRig");
            try
            {
                Type cameraControllerType = GetType(
                    "Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");
                rigObject.transform.SetParent(cameraObject.transform, false);
                Component cameraController = cameraObject.AddComponent(cameraControllerType);
                SetField(cameraController, "rig", rigObject.transform);
                Invoke(cameraController, "DetachFromVehicleForDeath");
                SetField(
                    cameraController,
                    "_deathSpectatorPhase",
                    Enum.Parse(cameraControllerType.GetNestedType(
                        "DeathSpectatorPhase",
                        BindingFlags.NonPublic), "DeathWreck"));

                UnityEngine.Object.DestroyImmediate(rigObject);

                MethodInfo getCameraRig = cameraControllerType.GetMethod(
                    "GetCameraRig",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(getCameraRig);
                Assert.AreSame(cameraObject.transform, getCameraRig.Invoke(cameraController, null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(rigObject);
            }
        }

        [Test]
        public void DeathSpectatorTransitionsToWreckWhenResolvedTargetIsDestroyed()
        {
            GameObject cameraObject = new GameObject("DestroyedSpectatorTargetCamera");
            GameObject targetObject = new GameObject("DestroyedSpectatorTarget");
            try
            {
                Type cameraControllerType = GetType(
                    "Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");
                Type phaseType = cameraControllerType.GetNestedType(
                    "DeathSpectatorPhase",
                    BindingFlags.NonPublic);
                Component cameraController = cameraObject.AddComponent(cameraControllerType);
                Component targetRoot = targetObject.AddComponent(
                    GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp"));
                SetField(cameraController, "_deathSpectatorPhase", Enum.Parse(phaseType, "KillerIntro"));
                SetField(cameraController, "_pendingKillerObjectId", -1);
                SetField(cameraController, "_spectatorTarget", targetRoot);

                UnityEngine.Object.DestroyImmediate(targetObject);
                Invoke(cameraController, "UpdateDeathSpectator");

                Assert.IsTrue((bool)GetProperty(cameraController, "IsDeathSpectatorWreck"));
                Assert.IsNull(GetProperty(cameraController, "SpectatorTarget"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void DeathSpectatorCachesLocalTeamBeforeVehicleRootIsDestroyed()
        {
            GameObject vehicleObject = new GameObject("SpectatorTeamVehicle");
            GameObject cameraObject = new GameObject("SpectatorTeamCamera");
            try
            {
                Type vehicleRootType = GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
                Type initializerType = GetType("Game.Scripts.Gameplay.Robots.VehicleNetworkInitializer, Assembly-CSharp");
                Type cameraControllerType = GetType("Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");
                Component vehicleRoot = vehicleObject.AddComponent(vehicleRootType);
                Component initializer = vehicleObject.AddComponent(initializerType);
                Component cameraController = cameraObject.AddComponent(cameraControllerType);
                SetField(vehicleRoot, "characterInit", initializer);
                SetField(cameraController, "vehicleRoot", vehicleRoot);

                Type matchTeamType = GetType("Game.Scripts.Networking.Lobby.MatchTeam, Assembly-CSharp");
                object teamA = Enum.Parse(matchTeamType, "TeamA");
                SetTeam(initializer, "TeamA");

                Invoke(cameraController, "CacheSpectatorTeam");
                UnityEngine.Object.DestroyImmediate(vehicleObject);

                object cachedTeam = GetField(cameraController, "_spectatorTeam");
                Assert.AreEqual(teamA, cachedTeam);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(vehicleObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void VehicleVisibilityKeepsAlliesVisibleAfterLocalVehicleDestroy()
        {
            GameObject localObject = new GameObject("VisibilityLocalVehicle");
            GameObject targetObject = new GameObject("VisibilityAllyVehicle");
            GameObject visualObject = new GameObject("VisibilityAllyVisual");
            try
            {
                Type vehicleRootType = GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
                Type initializerType = GetType("Game.Scripts.Gameplay.Robots.VehicleNetworkInitializer, Assembly-CSharp");
                Type visibilityType = GetType("Game.Scripts.Gameplay.Robots.VehicleClientVisibility, Assembly-CSharp");
                Component localRoot = localObject.AddComponent(vehicleRootType);
                Component localInitializer = localObject.AddComponent(initializerType);
                Component targetRoot = targetObject.AddComponent(vehicleRootType);
                Component targetInitializer = targetObject.AddComponent(initializerType);
                Component visibility = targetObject.AddComponent(visibilityType);
                PrepareNetworkBehaviour(localRoot);
                PrepareNetworkBehaviour(targetRoot);
                visualObject.transform.SetParent(targetObject.transform, false);
                MeshRenderer renderer = visualObject.AddComponent<MeshRenderer>();
                SetField(localRoot, "characterInit", localInitializer);
                SetField(targetRoot, "characterInit", targetInitializer);
                SetField(visibility, "visualRenderers", new[] { renderer });
                SetTeam(localInitializer, "TeamA");
                SetTeam(targetInitializer, "TeamA");
                Invoke(visibility, "SetVehicleRoot", targetRoot);
                Invoke(visibility, "CacheLocalPlayerTeam", localRoot);
                Invoke(visibility, "OnVehicleClientStarted");

                UnityEngine.Object.DestroyImmediate(localObject);
                Invoke(visibility, "RefreshVisibility");

                Assert.IsTrue((bool)GetProperty(visibility, "IsVisible"));
                Assert.IsFalse(renderer.forceRenderingOff);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(localObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(visualObject);
            }
        }

        [Test]
        public void VehicleVisibilityKeepsSpottedEnemyVisibleAfterLocalVehicleDestroy()
        {
            GameObject localObject = new GameObject("VisibilitySnapshotLocalVehicle");
            GameObject targetObject = new GameObject("VisibilitySnapshotEnemyVehicle");
            GameObject visualObject = new GameObject("VisibilitySnapshotEnemyVisual");
            try
            {
                Type vehicleRootType = GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
                Type initializerType = GetType("Game.Scripts.Gameplay.Robots.VehicleNetworkInitializer, Assembly-CSharp");
                Type visibilityType = GetType("Game.Scripts.Gameplay.Robots.VehicleClientVisibility, Assembly-CSharp");
                Component localRoot = localObject.AddComponent(vehicleRootType);
                Component localInitializer = localObject.AddComponent(initializerType);
                Component targetRoot = targetObject.AddComponent(vehicleRootType);
                Component targetInitializer = targetObject.AddComponent(initializerType);
                Component visibility = targetObject.AddComponent(visibilityType);
                PrepareNetworkBehaviour(localRoot);
                PrepareNetworkBehaviour(targetRoot);
                visualObject.transform.SetParent(targetObject.transform, false);
                MeshRenderer renderer = visualObject.AddComponent<MeshRenderer>();
                SetField(localRoot, "characterInit", localInitializer);
                SetField(targetRoot, "characterInit", targetInitializer);
                SetField(visibility, "visualRenderers", new[] { renderer });
                SetTeam(localInitializer, "TeamA");
                SetTeam(targetInitializer, "TeamB");
                Invoke(visibility, "SetVehicleRoot", targetRoot);
                Invoke(visibility, "CacheLocalPlayerTeam", localRoot);
                Invoke(visibility, "OnVehicleClientStarted");
                int targetObjectId = (int)GetProperty(GetField(targetRoot, "networkObject"), "ObjectId");

                Type visibilityStateType = GetType("Game.Scripts.UI.HUD.GameplayMapVisibilityState, Assembly-CSharp");
                Type relationType = GetType("Game.Scripts.Networking.Lobby.MapVehicleVisibilityRelation, Assembly-CSharp");
                MethodInfo clearMethod = visibilityStateType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);
                MethodInfo applyMethod = visibilityStateType.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(clearMethod, "GameplayMapVisibilityState.Clear");
                Assert.IsNotNull(applyMethod, "GameplayMapVisibilityState.Apply");
                clearMethod.Invoke(null, null);
                Array relations = Array.CreateInstance(typeof(byte), 1);
                relations.SetValue((byte)Enum.Parse(relationType, "Enemy"), 0);
                applyMethod.Invoke(null, new object[]
                {
                    1,
                    1,
                    new[] { targetObjectId },
                    relations,
                    new[] { Vector3.zero },
                    new[] { 0f },
                    new[] { -1f },
                    0f
                });
                UnityEngine.Object.DestroyImmediate(localObject);
                Invoke(visibility, "RefreshVisibility");

                Assert.IsTrue((bool)GetProperty(visibility, "IsVisible"));
                Assert.IsFalse(renderer.forceRenderingOff);
            }
            finally
            {
                Type visibilityStateType = Type.GetType("Game.Scripts.UI.HUD.GameplayMapVisibilityState, Assembly-CSharp");
                MethodInfo clearMethod = visibilityStateType != null
                    ? visibilityStateType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static)
                    : null;
                clearMethod?.Invoke(null, null);
                UnityEngine.Object.DestroyImmediate(localObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(visualObject);
            }
        }

        [Test]
        public void VehicleVisibilityHidesEnemyLastKnownModelAfterDirectSpotExpires()
        {
            GameObject localObject = new GameObject("VisibilityMemoryLocalVehicle");
            GameObject targetObject = new GameObject("VisibilityMemoryEnemyVehicle");
            GameObject visualObject = new GameObject("VisibilityMemoryEnemyVisual");
            try
            {
                Type vehicleRootType = GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
                Type initializerType = GetType("Game.Scripts.Gameplay.Robots.VehicleNetworkInitializer, Assembly-CSharp");
                Type visibilityType = GetType("Game.Scripts.Gameplay.Robots.VehicleClientVisibility, Assembly-CSharp");
                Component localRoot = localObject.AddComponent(vehicleRootType);
                Component localInitializer = localObject.AddComponent(initializerType);
                Component targetRoot = targetObject.AddComponent(vehicleRootType);
                Component targetInitializer = targetObject.AddComponent(initializerType);
                Component visibility = targetObject.AddComponent(visibilityType);
                PrepareNetworkBehaviour(localRoot);
                PrepareNetworkBehaviour(targetRoot);
                visualObject.transform.SetParent(targetObject.transform, false);
                MeshRenderer renderer = visualObject.AddComponent<MeshRenderer>();
                SetField(localRoot, "characterInit", localInitializer);
                SetField(targetRoot, "characterInit", targetInitializer);
                SetField(visibility, "visualRenderers", new[] { renderer });
                SetTeam(localInitializer, "TeamA");
                SetTeam(targetInitializer, "TeamB");
                Invoke(visibility, "SetVehicleRoot", targetRoot);
                Invoke(visibility, "CacheLocalPlayerTeam", localRoot);
                Invoke(visibility, "OnVehicleClientStarted");

                Type visibilityStateType = GetType("Game.Scripts.UI.HUD.GameplayMapVisibilityState, Assembly-CSharp");
                Type relationType = GetType("Game.Scripts.Networking.Lobby.MapVehicleVisibilityRelation, Assembly-CSharp");
                MethodInfo clearMethod = visibilityStateType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);
                MethodInfo applyMethod = visibilityStateType.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(clearMethod, "GameplayMapVisibilityState.Clear");
                Assert.IsNotNull(applyMethod, "GameplayMapVisibilityState.Apply");
                clearMethod.Invoke(null, null);
                Array relations = Array.CreateInstance(typeof(byte), 1);
                relations.SetValue((byte)Enum.Parse(relationType, "EnemyLastKnown"), 0);
                applyMethod.Invoke(null, new object[]
                {
                    1,
                    1,
                    new[] { -1 },
                    relations,
                    new[] { new Vector3(12f, 0f, 4f) },
                    new[] { 90f },
                    new[] { 2f },
                    0f
                });
                Invoke(visibility, "RefreshVisibility");

                Assert.IsFalse((bool)GetProperty(visibility, "IsVisible"));
                Assert.IsTrue(renderer.forceRenderingOff);
            }
            finally
            {
                Type visibilityStateType = Type.GetType("Game.Scripts.UI.HUD.GameplayMapVisibilityState, Assembly-CSharp");
                MethodInfo clearMethod = visibilityStateType != null
                    ? visibilityStateType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static)
                    : null;
                clearMethod?.Invoke(null, null);
                UnityEngine.Object.DestroyImmediate(localObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(visualObject);
            }
        }

        [Test]
        public void DeathSpectatorWreckClearsKillerVisibilityOverride()
        {
            GameObject vehicleObject = new GameObject("SpectatorWreckTarget");
            GameObject visualObject = new GameObject("SpectatorWreckVisual");
            GameObject cameraObject = new GameObject("SpectatorWreckCamera");
            try
            {
                Type vehicleRootType = GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
                Type visibilityType = GetType("Game.Scripts.Gameplay.Robots.VehicleClientVisibility, Assembly-CSharp");
                Type cameraControllerType = GetType("Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");
                visualObject.transform.SetParent(vehicleObject.transform, false);
                MeshRenderer renderer = visualObject.AddComponent<MeshRenderer>();
                Component vehicleRoot = vehicleObject.AddComponent(vehicleRootType);
                Component visibility = vehicleObject.AddComponent(visibilityType);
                Component cameraController = cameraObject.AddComponent(cameraControllerType);
                SetField(vehicleRoot, "clientVisibility", visibility);
                SetField(visibility, "visualRenderers", new[] { renderer });
                Invoke(visibility, "SetVehicleRoot", vehicleRoot);
                Invoke(visibility, "SetVisibilityOverride", true, false);
                Invoke(visibility, "OnVehicleClientStarted");
                Invoke(cameraController, "UpdateSpectatorVisibilityOverride", vehicleRoot);

                Assert.IsTrue((bool)GetProperty(visibility, "IsVisible"));
                Assert.IsFalse(renderer.forceRenderingOff);

                Invoke(cameraController, "EnterDeathWreck");

                Assert.IsFalse((bool)GetProperty(visibility, "IsVisible"));
                Assert.IsTrue(renderer.forceRenderingOff);
                Assert.IsNull(GetProperty(cameraController, "SpectatorTarget"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(vehicleObject);
                UnityEngine.Object.DestroyImmediate(visualObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void DeathSpectatorMouseDirectionsWrapInOppositeDirections()
        {
            Type cameraControllerType = GetType("Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");
            MethodInfo wrapIndex = cameraControllerType.GetMethod(
                "WrapIndex",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(wrapIndex, "CameraController.WrapIndex");

            Assert.That((int)wrapIndex.Invoke(null, new object[] { 4, 3 }), Is.EqualTo(1));
            Assert.That((int)wrapIndex.Invoke(null, new object[] { -1, 3 }), Is.EqualTo(2));
            Assert.That((int)wrapIndex.Invoke(null, new object[] { 0, 3 }), Is.EqualTo(0));
        }

        [Test]
        public void VehicleInputControllerExposesOwnerRequestAndSynchronizedState()
        {
            Type inputControllerType = GetType(
                "Game.Scripts.Gameplay.Robots.VehicleInputController, Assembly-CSharp");

            Assert.IsNotNull(inputControllerType.GetProperty(
                "IsAutonomous",
                BindingFlags.Public | BindingFlags.Instance));
            Assert.IsNotNull(inputControllerType.GetMethod(
                "RequestAutonomousControl",
                BindingFlags.Public | BindingFlags.Instance));
            Assert.IsNotNull(inputControllerType.GetMethod(
                "SetAutonomousControlServerRpc",
                BindingFlags.NonPublic | BindingFlags.Instance));
            Assert.IsNotNull(inputControllerType.GetMethod(
                "ServerSetAutonomousControl",
                BindingFlags.NonPublic | BindingFlags.Instance));
        }

        [Test]
        public void VehicleBotBrainProvidesExplicitStopLifecycle()
        {
            Type botBrainType = GetType(
                "Game.Scripts.Gameplay.Robots.VehicleBotBrain, Assembly-CSharp");

            Assert.IsNotNull(botBrainType.GetMethod(
                "StartBrain",
                BindingFlags.Public | BindingFlags.Instance));
            Assert.IsNotNull(botBrainType.GetMethod(
                "StopBrain",
                BindingFlags.Public | BindingFlags.Instance));
        }

        [Test]
        public void ClientSceneAuthorsAutonomousButtonAndHudIndicator()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string scenePath = Path.Combine(projectRoot, "Assets", "Game", "Scenes", "Client.unity");
            string sceneText = File.ReadAllText(scenePath);

            StringAssert.Contains("m_Name: ButtonAutonomousControl", sceneText);
            StringAssert.Contains("m_Name: AutonomousControlIndicator", sceneText);
            StringAssert.Contains("autonomousControlButton: {fileID: 990100000000000005}", sceneText);
            StringAssert.Contains("autonomousControlText: {fileID: 990200000000000003}", sceneText);
            StringAssert.Contains(
                "m_NormalColor: {r: 0.12, g: 0.68, b: 0.2, a: 1}",
                sceneText);
            StringAssert.Contains("m_text: Autonomous Control", sceneText);
        }

        private static object CreateRuntimeSettings(Type runtimeSettingsType, Type profileType)
        {
            object runtimeSettings = Activator.CreateInstance(runtimeSettingsType);
            Array profiles = Array.CreateInstance(profileType, 2);
            object defaultProfile = Activator.CreateInstance(profileType);
            object forwardProfile = Activator.CreateInstance(profileType);
            SetField(defaultProfile, "zoomIndex", 0);
            SetField(defaultProfile, "cameraDistance", 10f);
            SetField(defaultProfile, "fieldOfView", 60f);
            SetField(forwardProfile, "zoomIndex", 1);
            SetField(forwardProfile, "cameraDistance", 6f);
            SetField(forwardProfile, "fieldOfView", 50f);
            profiles.SetValue(defaultProfile, 0);
            profiles.SetValue(forwardProfile, 1);
            SetField(runtimeSettings, "cameraZoomProfiles", profiles);
            return runtimeSettings;
        }

        private static Type GetType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            Assert.IsNotNull(type, assemblyQualifiedName);
            return type;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.IsNotNull(field, fieldName);
            return field.GetValue(target);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static void PrepareNetworkBehaviour(Component networkBehaviour)
        {
            Type networkObjectType = GetType("FishNet.Object.NetworkObject, FishNet.Runtime");
            Component networkObject = networkBehaviour.gameObject.AddComponent(networkObjectType);
            SetField(networkBehaviour, "_networkObjectCache", networkObject);

            FieldInfo vehicleNetworkObject = FindField(networkBehaviour.GetType(), "networkObject");
            if (vehicleNetworkObject != null)
            {
                vehicleNetworkObject.SetValue(networkBehaviour, networkObject);
            }
        }

        private static void SetTeam(Component initializer, string teamName)
        {
            FieldInfo teamField = initializer.GetType().GetField("Team", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(teamField, "Team");
            object teamSyncVar = teamField.GetValue(initializer);
            Type teamType = GetType("Game.Scripts.Networking.Lobby.MatchTeam, Assembly-CSharp");
            object teamValue = Enum.Parse(teamType, teamName);
            SetField(teamSyncVar, "_initialValue", teamValue);
            SetField(teamSyncVar, "_value", teamValue);
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(property, propertyName);
            return property.GetValue(target);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, methodName);
            return method.Invoke(target, arguments);
        }
    }
}
