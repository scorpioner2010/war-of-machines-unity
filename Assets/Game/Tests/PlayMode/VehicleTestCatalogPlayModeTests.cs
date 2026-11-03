using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
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
            Type prefabType = Type.GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
            Type statsType = Type.GetType("Game.Scripts.Gameplay.Robots.VehicleRuntimeStats, Assembly-CSharp");
            Type controllerType = Type.GetType("Game.Scripts.Testing.VehicleTestSceneController, Assembly-CSharp");
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
        public IEnumerator OfflineMenuDataKeepsSelectionAndDistinctVehicleStats()
        {
            GameObject testObject = new GameObject("OfflineMenuDataRegressionController");
            Type testModeType = Type.GetType("Game.Scripts.Testing.ClientMenuTestMode, Assembly-CSharp");
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
