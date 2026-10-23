using Game.Scripts.Gameplay.Robots;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    public sealed class DispersionShotMathPlayModeTests
    {
        [Test]
        public void ZeroDispersionPreservesBaseDirection()
        {
            Vector3 baseDirection = new Vector3(0.2f, 0.1f, 1f).normalized;
            Vector3 result = DispersionConeProjection.BuildDispersedDirection(
                baseDirection,
                new Vector2(1f, 0f),
                0f);

            Assert.That(Vector3.Angle(baseDirection, result), Is.LessThan(0.001f));
        }

        [Test]
        public void DispersionProducesAFiniteNormalizedDirection()
        {
            Vector3 result = DispersionConeProjection.BuildDispersedDirection(
                Vector3.forward,
                new Vector2(0.75f, -0.25f),
                6f);

            Assert.That(result.sqrMagnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.IsFalse(float.IsNaN(result.x) || float.IsInfinity(result.x));
            Assert.IsFalse(float.IsNaN(result.y) || float.IsInfinity(result.y));
            Assert.IsFalse(float.IsNaN(result.z) || float.IsInfinity(result.z));
        }
    }
}
