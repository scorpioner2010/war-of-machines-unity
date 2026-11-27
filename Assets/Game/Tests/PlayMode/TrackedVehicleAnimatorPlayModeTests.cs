#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RAV.Tests.PlayMode
{
    public sealed class TrackedVehicleAnimatorPlayModeTests
    {
        private static readonly Type AnimatorType = Type.GetType(
            "RAV.Scripts.Gameplay.Robots.t2.TrackedVehicleAnimator, Assembly-CSharp");
        private static readonly MethodInfo ResolveTrackSpeedsMethod = AnimatorType?.GetMethod(
            "ResolveTrackSpeeds",
            BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly string[] IronAlliancePrefabPaths =
        {
            "Assets/Game/Content/Robots/IronAlliance/IA_L1_Starter/IA_L1_Starter.prefab",
            "Assets/Game/Content/Robots/IronAlliance/IA_L2_Scout/IA_L2_Scout.prefab",
            "Assets/Game/Content/Robots/IronAlliance/IA_L2_Guardian/IA_L2_Guardian.prefab",
            "Assets/Game/Content/Robots/IronAlliance/IA_L2_Colossus/IA_L2_Colossus.prefab"
        };

        [Test]
        public void LeftAndRightWheelSignsProduceTheSamePhysicalRollDirection()
        {
            for (int prefabIndex = 0; prefabIndex < IronAlliancePrefabPaths.Length; prefabIndex++)
            {
                string path = IronAlliancePrefabPaths[prefabIndex];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, "Missing prefab: " + path);

                Assert.IsNotNull(AnimatorType, "Missing tracked animator type: " + path);
                Component animator = prefab.GetComponentInChildren(AnimatorType);
                Assert.IsNotNull(animator, "Missing tracked animator: " + path);

                Array leftWheels = GetWheelArray(animator, "leftWheels");
                Array rightWheels = GetWheelArray(animator, "rightWheels");
                Assert.IsNotEmpty(leftWheels, "Missing left wheels: " + path);
                Assert.IsNotEmpty(rightWheels, "Missing right wheels: " + path);

                float rightRollSign = GetPhysicalRollSign(animator, rightWheels.GetValue(0));
                Assert.That(Mathf.Abs(rightRollSign), Is.EqualTo(1f), path);

                AssertWheelGroupMatches(animator, rightWheels, rightRollSign, path + " right");
                AssertWheelGroupMatches(animator, leftWheels, rightRollSign, path + " left");
            }
        }

        [Test]
        public void MovementInputsMatchTheTrackedVehicleCommandTableForEveryRobot()
        {
            Assert.IsNotNull(AnimatorType);
            Assert.IsNotNull(ResolveTrackSpeedsMethod);

            for (int prefabIndex = 0; prefabIndex < IronAlliancePrefabPaths.Length; prefabIndex++)
            {
                string path = IronAlliancePrefabPaths[prefabIndex];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Component animator = prefab.GetComponentInChildren(AnimatorType);
                float forwardSpeed = Mathf.Abs(GetFloatField(animator, "forwardBackwardSpeed"));
                float turnInPlaceSpeed = Mathf.Abs(GetFloatField(animator, "turnInPlaceSpeed"));

                AssertTrackSpeeds(animator, 1f, 0f, forwardSpeed, forwardSpeed, path + " forward");
                AssertTrackSpeeds(animator, -1f, 0f, -forwardSpeed, -forwardSpeed, path + " reverse");
                AssertTrackSpeeds(animator, 0f, -1f, -turnInPlaceSpeed, turnInPlaceSpeed, path + " pivot left");
                AssertTrackSpeeds(animator, 0f, 1f, turnInPlaceSpeed, -turnInPlaceSpeed, path + " pivot right");

                AssertMovingTurn(animator, 1f, -1f, path + " forward left");
                AssertMovingTurn(animator, 1f, 1f, path + " forward right");
                AssertMovingTurn(animator, -1f, 1f, path + " reverse left");
                AssertMovingTurn(animator, -1f, -1f, path + " reverse right");
            }
        }

        private static void AssertWheelGroupMatches(
            Component animator,
            Array wheels,
            float expectedRollSign,
            string context)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                object wheel = wheels.GetValue(i);
                Assert.IsNotNull(wheel, context + " wheel " + i);
                Transform wheelTransform = GetWheelTransform(wheel);
                Assert.IsNotNull(wheelTransform, context + " transform " + i);

                float physicalRollSign = GetPhysicalRollSign(animator, wheel);
                Assert.That(
                    physicalRollSign,
                    Is.EqualTo(expectedRollSign),
                    context + " wheel " + i + " has an opposite physical roll direction");
            }
        }

        private static void AssertMovingTurn(
            Component animator,
            float forwardInput,
            float turnInput,
            string context)
        {
            object[] arguments = { forwardInput, turnInput, 0f, 0f };
            ResolveTrackSpeedsMethod.Invoke(animator, arguments);
            float leftSpeed = (float)arguments[2];
            float rightSpeed = (float)arguments[3];

            Assert.That(Mathf.Sign(leftSpeed), Is.EqualTo(Mathf.Sign(forwardInput)), context + " left direction");
            Assert.That(Mathf.Sign(rightSpeed), Is.EqualTo(Mathf.Sign(forwardInput)), context + " right direction");
            Assert.That(Mathf.Abs(leftSpeed), Is.Not.EqualTo(Mathf.Abs(rightSpeed)), context + " should have an outer side");
        }

        private static void AssertTrackSpeeds(
            Component animator,
            float forwardInput,
            float turnInput,
            float expectedLeftSpeed,
            float expectedRightSpeed,
            string context)
        {
            object[] arguments = { forwardInput, turnInput, 0f, 0f };
            ResolveTrackSpeedsMethod.Invoke(animator, arguments);

            Assert.That((float)arguments[2], Is.EqualTo(expectedLeftSpeed).Within(0.001f), context + " left speed");
            Assert.That((float)arguments[3], Is.EqualTo(expectedRightSpeed).Within(0.001f), context + " right speed");
        }

        private static float GetFloatField(Component animator, string fieldName)
        {
            return (float)AnimatorType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance)
                .GetValue(animator);
        }

        private static Array GetWheelArray(Component animator, string fieldName)
        {
            return (Array)AnimatorType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance)
                .GetValue(animator);
        }

        private static Transform GetWheelTransform(object wheel)
        {
            return (Transform)wheel.GetType().GetField("transform", BindingFlags.Public | BindingFlags.Instance)
                .GetValue(wheel);
        }

        private static float GetPhysicalRollSign(Component animator, object wheel)
        {
            Transform wheelTransform = GetWheelTransform(wheel);
            float rotationSpeed = (float)wheel.GetType()
                .GetField("rotationSpeed", BindingFlags.Public | BindingFlags.Instance)
                .GetValue(wheel);
            Vector3 wheelAxis = wheelTransform.TransformDirection(Vector3.forward);
            float geometrySign = Mathf.Sign(Vector3.Dot(
                Vector3.Cross(wheelAxis, animator.transform.up),
                animator.transform.forward));
            return Mathf.Sign(rotationSpeed) * geometrySign;
        }
    }
}
#endif
