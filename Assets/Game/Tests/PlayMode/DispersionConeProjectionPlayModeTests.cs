using System.Collections;
using Game.Scripts.Gameplay.Robots;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public sealed class DispersionConeProjectionPlayModeTests
    {
        private readonly System.Collections.Generic.List<GameObject> _createdObjects =
            new System.Collections.Generic.List<GameObject>(1);

        [UnityTest]
        public IEnumerator NarrowerFovProducesLargerProjectedDispersionRing()
        {
            GameObject cameraObject = new GameObject("Dispersion projection camera");
            _createdObjects.Add(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            camera.fieldOfView = 60f;
            yield return null;

            Vector3 origin = Vector3.zero;
            Vector3 aimPoint = new Vector3(0f, 0f, 20f);
            Assert.IsTrue(
                DispersionConeProjection.TryGetScreenDiameter(camera, origin, aimPoint, 4f, out float wideFovDiameter),
                "The wide-FOV cone projection should be valid.");

            camera.fieldOfView = 40f;
            Assert.IsTrue(
                DispersionConeProjection.TryGetScreenDiameter(camera, origin, aimPoint, 4f, out float narrowFovDiameter),
                "The narrow-FOV cone projection should be valid.");
            Assert.Greater(
                narrowFovDiameter,
                wideFovDiameter,
                "The same physical dispersion cone must occupy more screen space after zooming in.");

            float expectedFovScale = DispersionConeProjection.GetFovScale(60f, 40f);
            Assert.That(
                narrowFovDiameter / wideFovDiameter,
                Is.EqualTo(expectedFovScale).Within(0.03f),
                "The projected ring should follow the camera FOV scale rather than an arbitrary zoom step.");
        }

        [UnityTest]
        public IEnumerator ProjectedRingContainsSampledPhysicalConeBoundary()
        {
            GameObject cameraObject = new GameObject("Dispersion boundary camera");
            _createdObjects.Add(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            camera.fieldOfView = 45f;
            yield return null;

            Vector3 origin = new Vector3(0.35f, -0.2f, 0f);
            Vector3 aimPoint = new Vector3(2f, 1.5f, 30f);
            const float dispersionDeg = 6f;
            Assert.IsTrue(
                DispersionConeProjection.TryGetScreenDiameter(camera, origin, aimPoint, dispersionDeg, out float ringDiameter),
                "The off-center cone projection should be valid.");

            Vector3 baseDirection = (aimPoint - origin).normalized;
            Vector3 screenCenter = camera.WorldToScreenPoint(aimPoint);
            float targetDistance = Vector3.Distance(origin, aimPoint);
            for (int i = 0; i < 128; i++)
            {
                float angle = 2f * Mathf.PI * i / 128f;
                Vector3 boundaryDirection = DispersionConeProjection.BuildDispersedDirection(
                    baseDirection,
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                    dispersionDeg);
                Vector3 screenBoundary = camera.WorldToScreenPoint(origin + boundaryDirection * targetDistance);
                float radius = Vector2.Distance(
                    new Vector2(screenCenter.x, screenCenter.y),
                    new Vector2(screenBoundary.x, screenBoundary.y));
                Assert.LessOrEqual(
                    radius,
                    ringDiameter * 0.5f + 0.01f,
                    "A physical boundary direction was projected outside the displayed reticle.");
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }
    }
}
