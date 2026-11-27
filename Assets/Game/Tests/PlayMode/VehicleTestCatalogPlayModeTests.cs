using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RAV.Tests.PlayMode
{
    public sealed class VehicleTestCatalogPlayModeTests
    {
        [UnityTest]
        public IEnumerator TestCatalogExcludesVehiclesWithoutPrefabs()
        {
            GameObject controllerObject = new GameObject("VehicleTestCatalogRegressionController");
            GameObject prefabObject = new GameObject("VehicleTestCatalogRegressionPrefab");
            Type registryType = Type.GetType("RobotRegistry, Assembly-CSharp");
            Type itemType = Type.GetType("RobotRegistry+Item, Assembly-CSharp");
            Type prefabType = Type.GetType("RAV.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
            Type statsType = Type.GetType("RAV.Scripts.Gameplay.Robots.VehicleRuntimeStats, Assembly-CSharp");
            Type controllerType = Type.GetType("RAV.Scripts.Testing.VehicleTestSceneController, Assembly-CSharp");
            Assert.IsNotNull(registryType);
            Assert.IsNotNull(itemType);
            Assert.IsNotNull(prefabType);
            Assert.IsNotNull(statsType);
            Assert.IsNotNull(controllerType);

            UnityEngine.Object registry = ScriptableObject.CreateInstance(registryType);
            Component prefab = prefabObject.AddComponent(prefabType);
            Component controller = controllerObject.AddComponent(controllerType);
            try
            {
                IList items = (IList)registryType.GetField("items").GetValue(registry);
                object item = Activator.CreateInstance(itemType);
                itemType.GetField("code").SetValue(item, "valid_vehicle");
                itemType.GetField("prefab").SetValue(item, prefab);
                items.Add(item);
                controllerType.GetField("registry").SetValue(controller, registry);

                Array source = Array.CreateInstance(statsType, 2);
                object validStats = Activator.CreateInstance(statsType);
                object missingStats = Activator.CreateInstance(statsType);
                statsType.GetField("Code").SetValue(validStats, "valid_vehicle");
                statsType.GetField("Code").SetValue(missingStats, "missing_vehicle");
                source.SetValue(validStats, 0);
                source.SetValue(missingStats, 1);

                MethodInfo filter = controllerType.GetMethod(
                    "FilterVehiclesWithPrefabs",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(filter);

                Array filtered = (Array)filter.Invoke(controller, new object[] { source });
                Assert.That(filtered, Has.Length.EqualTo(1));
                object filteredStats = filtered.GetValue(0);
                Assert.That(statsType.GetField("Code").GetValue(filteredStats), Is.EqualTo("valid_vehicle"));
            }
            finally
            {
                UnityEngine.Object.Destroy(controllerObject);
                UnityEngine.Object.Destroy(prefabObject);
                UnityEngine.Object.Destroy(registry);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RegistryFallbackIncludesOnlyValidPrefabs()
        {
            GameObject controllerObject = new GameObject("VehicleTestRegistryFallbackController");
            GameObject validPrefabObject = new GameObject("VehicleTestRegistryFallbackPrefab");
            GameObject duplicatePrefabObject = new GameObject("VehicleTestRegistryFallbackDuplicatePrefab");
            Type registryType = Type.GetType("RobotRegistry, Assembly-CSharp");
            Type itemType = Type.GetType("RobotRegistry+Item, Assembly-CSharp");
            Type prefabType = Type.GetType("RAV.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
            Type statsType = Type.GetType("RAV.Scripts.Gameplay.Robots.VehicleRuntimeStats, Assembly-CSharp");
            Type controllerType = Type.GetType("RAV.Scripts.Testing.VehicleTestSceneController, Assembly-CSharp");
            Assert.IsNotNull(registryType);
            Assert.IsNotNull(itemType);
            Assert.IsNotNull(prefabType);
            Assert.IsNotNull(statsType);
            Assert.IsNotNull(controllerType);

            UnityEngine.Object registry = ScriptableObject.CreateInstance(registryType);
            Component validPrefab = validPrefabObject.AddComponent(prefabType);
            Component duplicatePrefab = duplicatePrefabObject.AddComponent(prefabType);
            Component controller = controllerObject.AddComponent(controllerType);
            try
            {
                IList items = (IList)registryType.GetField("items").GetValue(registry);
                object validItem = Activator.CreateInstance(itemType);
                itemType.GetField("code").SetValue(validItem, "registry_vehicle");
                itemType.GetField("prefab").SetValue(validItem, validPrefab);
                items.Add(validItem);

                object duplicateItem = Activator.CreateInstance(itemType);
                itemType.GetField("code").SetValue(duplicateItem, "registry_vehicle");
                itemType.GetField("prefab").SetValue(duplicateItem, duplicatePrefab);
                items.Add(duplicateItem);

                object invalidItem = Activator.CreateInstance(itemType);
                itemType.GetField("code").SetValue(invalidItem, "missing_prefab");
                items.Add(invalidItem);

                controllerType.GetField("registry").SetValue(controller, registry);
                MethodInfo buildFallback = controllerType.GetMethod(
                    "BuildRegistryFallbackVehicles",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(buildFallback);

                Array fallback = (Array)buildFallback.Invoke(controller, null);
                Assert.That(fallback, Has.Length.EqualTo(1));
                object fallbackStats = fallback.GetValue(0);
                Assert.That(statsType.GetField("Code").GetValue(fallbackStats), Is.EqualTo("registry_vehicle"));
                Assert.That((float)statsType.GetField("MaxHealth").GetValue(fallbackStats), Is.GreaterThan(0f));
            }
            finally
            {
                UnityEngine.Object.Destroy(controllerObject);
                UnityEngine.Object.Destroy(validPrefabObject);
                UnityEngine.Object.Destroy(duplicatePrefabObject);
                UnityEngine.Object.Destroy(registry);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator VehicleTestServerAuthenticationUsesValidRemoteConnection()
        {
            Type authenticatorType = Type.GetType(
                "RAV.Scripts.Networking.Authentication.ConnectionTicketAuthenticator, Assembly-CSharp");
            Assert.IsNotNull(authenticatorType);

            MethodInfo shouldBypassServerAuthentication = authenticatorType.GetMethod(
                "ShouldBypassServerAuthentication",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(shouldBypassServerAuthentication);
            Assert.IsTrue((bool)shouldBypassServerAuthentication.Invoke(null, new object[] { true }));
            Assert.IsFalse((bool)shouldBypassServerAuthentication.Invoke(null, new object[] { false }));

            MethodInfo authenticateVehicleTestConnection = authenticatorType.GetMethod(
                "AuthenticateVehicleTestConnection",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(authenticateVehicleTestConnection);

            Type connectionType = Type.GetType("FishNet.Connection.NetworkConnection, FishNet.Runtime");
            Type sessionsType = Type.GetType("RAV.Scripts.Networking.Sessions.ServerPlayerSessions, Assembly-CSharp");
            Assert.IsNotNull(connectionType);
            Assert.IsNotNull(sessionsType);

            object connection = Activator.CreateInstance(connectionType);
            MethodInfo markAuthenticated = connectionType.GetMethod(
                "ConnectionAuthenticated",
                BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo tryGetAuthenticated = null;
            MethodInfo removeSession = sessionsType.GetMethod("Remove", BindingFlags.Public | BindingFlags.Static);
            foreach (MethodInfo method in sessionsType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name == "TryGetAuthenticated" && method.GetParameters().Length == 2)
                {
                    tryGetAuthenticated = method;
                    break;
                }
            }

            Assert.IsNotNull(markAuthenticated);
            Assert.IsNotNull(tryGetAuthenticated);
            Assert.IsNotNull(removeSession);

            try
            {
                markAuthenticated.Invoke(connection, null);
                authenticateVehicleTestConnection.Invoke(null, new[] { connection });
                object[] sessionArguments = { connection, null };
                Assert.IsTrue((bool)tryGetAuthenticated.Invoke(null, sessionArguments));
            }
            finally
            {
                removeSession.Invoke(null, new object[] { -1 });
            }

            yield return null;
        }

        [Test]
        public void ClientVehicleTestModeBypassesMovementAndFiringCountdownGate()
        {
            Type gateType = Type.GetType(
                "RAV.Scripts.Gameplay.Battle.BattleCountdownGate, Assembly-CSharp");
            Type testModeType = Type.GetType(
                "RAV.Scripts.Testing.ClientVehicleTestMode, Assembly-CSharp");
            Assert.IsNotNull(gateType);
            Assert.IsNotNull(testModeType);

            FieldInfo activeField = testModeType.GetField(
                "_active",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(activeField);

            bool previousActive = (bool)activeField.GetValue(null);
            MethodInfo beginClient = gateType.GetMethod("BeginClient");
            MethodInfo endClient = gateType.GetMethod("EndClient");
            MethodInfo beginServer = gateType.GetMethod("BeginServer");
            MethodInfo isClientBlocked = gateType.GetProperty("IsClientMovementAndFiringBlocked").GetGetMethod();
            MethodInfo isServerActive = gateType.GetMethod("IsServerCountdownActive");
            MethodInfo clearServerLocks = gateType.GetMethod("ClearAllServerLocks");
            Assert.IsNotNull(beginClient);
            Assert.IsNotNull(endClient);
            Assert.IsNotNull(beginServer);
            Assert.IsNotNull(isClientBlocked);
            Assert.IsNotNull(isServerActive);
            Assert.IsNotNull(clearServerLocks);

            endClient.Invoke(null, null);
            clearServerLocks.Invoke(null, null);

            try
            {
                activeField.SetValue(null, false);
                beginClient.Invoke(null, null);
                beginServer.Invoke(null, new object[] { "vehicle-test-regression" });
                Assert.IsTrue((bool)isClientBlocked.Invoke(null, null));
                Assert.IsTrue((bool)isServerActive.Invoke(null, new object[] { "vehicle-test-regression" }));

                activeField.SetValue(null, true);
                Assert.IsFalse((bool)isClientBlocked.Invoke(null, null));
                Assert.IsFalse((bool)isServerActive.Invoke(null, new object[] { "vehicle-test-regression" }));
                beginServer.Invoke(null, new object[] { "vehicle-test-regression-disabled" });
                Assert.IsFalse((bool)isServerActive.Invoke(null, new object[] { "vehicle-test-regression-disabled" }));
            }
            finally
            {
                activeField.SetValue(null, previousActive);
                endClient.Invoke(null, null);
                clearServerLocks.Invoke(null, null);
            }
        }

        [UnityTest]
        public IEnumerator OfflineMenuDataKeepsSelectionAndDistinctVehicleStats()
        {
            GameObject testObject = new GameObject("OfflineMenuDataRegressionController");
            Type testModeType = Type.GetType("RAV.Scripts.Testing.ClientMenuTestMode, Assembly-CSharp");
            Assert.IsNotNull(testModeType);

            Component testMode = testObject.AddComponent(testModeType);
            testModeType.GetField("enableOfflineMenuTest").SetValue(testMode, true);

            try
            {
                MethodInfo createVehicles = testModeType.GetMethod("TryCreateVehicles");
                MethodInfo selectVehicle = testModeType.GetMethod("TrySelectVehicle");
                MethodInfo createProfile = testModeType.GetMethod("TryCreateProfile");
                MethodInfo createTrees = testModeType.GetMethod("TryCreateDevelopmentTrees");
                Assert.IsNotNull(createVehicles);
                Assert.IsNotNull(selectVehicle);
                Assert.IsNotNull(createProfile);
                Assert.IsNotNull(createTrees);

                object[] vehicleArguments = { null };
                Assert.IsTrue((bool)createVehicles.Invoke(null, vehicleArguments));
                Array vehicles = (Array)vehicleArguments[0];
                Assert.That(vehicles, Has.Length.EqualTo(15));

                HashSet<int> vehicleIds = new HashSet<int>();
                HashSet<string> statSignatures = new HashSet<string>();
                for (int index = 0; index < vehicles.Length; index++)
                {
                    object vehicle = vehicles.GetValue(index);
                    Assert.IsNotNull(vehicle);
                    Type vehicleType = vehicle.GetType();
                    int vehicleId = (int)vehicleType.GetField("id").GetValue(vehicle);
                    string vehicleCode = (string)vehicleType.GetField("code").GetValue(vehicle);
                    int hp = (int)vehicleType.GetField("hp").GetValue(vehicle);
                    int penetration = (int)vehicleType.GetField("penetration").GetValue(vehicle);
                    float damageMin = (float)vehicleType.GetField("damageMin").GetValue(vehicle);
                    float damageMax = (float)vehicleType.GetField("damageMax").GetValue(vehicle);
                    float reloadTime = (float)vehicleType.GetField("reloadTime").GetValue(vehicle);
                    float viewRange = (float)vehicleType.GetField("viewRange").GetValue(vehicle);
                    float speed = (float)vehicleType.GetField("speed").GetValue(vehicle);

                    Assert.IsTrue(vehicleIds.Add(vehicleId));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(vehicleCode));
                    Assert.IsTrue(statSignatures.Add(
                        hp + "|"
                        + penetration + "|"
                        + damageMin + "|"
                        + damageMax + "|"
                        + reloadTime + "|"
                        + viewRange + "|"
                        + speed));
                }

                Type vehicleTypeForSelection = vehicles.GetValue(7).GetType();
                int firstSelectedId = (int)vehicleTypeForSelection.GetField("id").GetValue(vehicles.GetValue(7));
                Assert.IsTrue((bool)selectVehicle.Invoke(null, new object[] { firstSelectedId }));
                object[] profileArguments = { null };
                Assert.IsTrue((bool)createProfile.Invoke(null, profileArguments));
                object selectedProfile = profileArguments[0];
                object selectedVehicle = selectedProfile.GetType().GetMethod("GetSelected").Invoke(selectedProfile, null);
                Assert.That((int)selectedVehicle.GetType().GetField("vehicleId").GetValue(selectedVehicle), Is.EqualTo(firstSelectedId));

                int secondSelectedId = (int)vehicleTypeForSelection.GetField("id").GetValue(vehicles.GetValue(14));
                Assert.IsTrue((bool)selectVehicle.Invoke(null, new object[] { secondSelectedId }));
                profileArguments[0] = null;
                Assert.IsTrue((bool)createProfile.Invoke(null, profileArguments));
                object secondProfile = profileArguments[0];
                object secondSelectedVehicle = secondProfile.GetType().GetMethod("GetSelected").Invoke(secondProfile, null);
                Assert.That((int)secondSelectedVehicle.GetType().GetField("vehicleId").GetValue(secondSelectedVehicle), Is.EqualTo(secondSelectedId));

                object[] treeArguments = { null, null };
                Assert.IsTrue((bool)createTrees.Invoke(null, treeArguments));
                Array treeVehicles = (Array)treeArguments[0];
                Array graphs = (Array)treeArguments[1];
                Assert.That(treeVehicles, Has.Length.EqualTo(vehicles.Length));
                Assert.That(graphs, Has.Length.EqualTo(2));

                int nodeCount = 0;
                for (int graphIndex = 0; graphIndex < graphs.Length; graphIndex++)
                {
                    object graph = graphs.GetValue(graphIndex);
                    Assert.IsNotNull(graph);
                    Array nodes = (Array)graph.GetType().GetField("nodes").GetValue(graph);
                    Assert.IsNotNull(nodes);
                    nodeCount += nodes.Length;
                }

                Assert.That(nodeCount, Is.EqualTo(vehicles.Length));
            }
            finally
            {
                UnityEngine.Object.Destroy(testObject);
            }

            yield return null;
        }
    }
}
