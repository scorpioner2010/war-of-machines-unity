using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public static class AngleQuantization
    {
        private const float Factor = 100f;

        public static short QuantizeAngle01(float deg)
        {
            float clamped = Mathf.Clamp(deg, -180f, 180f);
            return (short)Mathf.RoundToInt(clamped * Factor);
        }

        public static float DequantizeAngle01(short q)
        {
            return q / Factor;
        }
    }
}
