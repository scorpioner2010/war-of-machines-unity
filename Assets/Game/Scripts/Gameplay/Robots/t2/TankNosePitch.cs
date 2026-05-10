using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    /// <summary>
    /// Stable tank nose pitch based on filtered movement without Rigidbody springs.
    /// </summary>
    public class TankNosePitch : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;
        
        [Header("References")]
        public Transform baseTransform;
        public Transform movingTransform;

        [Header("Limits")]
        [Tooltip("Maximum absolute pitch angle, degrees")]
        public float maxPitchAbs = 10f;

        [Header("Response")]
        [Tooltip("Degrees per 1 m/s^2 at base intensity")]
        public float degPerAccelBase = 0.12f;

        [Tooltip("Sway intensity multiplier for degPerAccelBase")]
        [Range(0f, 2f)] public float swayIntensity = 1f;

        [Tooltip("Target follow speed; higher values respond faster")]
        public float swaySpeed = 6f;

        [Header("Noise Rejection")]
        [Tooltip("Ignore vertical terrain movement")]
        public bool ignoreVertical = true;

        [Tooltip("Project movement onto the ground plane")]
        public bool useGroundPlane = true;

        [Tooltip("Acceleration threshold in m/s^2 below which movement is treated as noise")]
        public float accelDeadZone = 0.25f;

        [Header("Filtering (seconds)")]
        [Tooltip("Position smoothing before differentiation")]
        public float posFilterTime = 0.06f;

        [Tooltip("Speed smoothing")]
        public float speedFilterTime = 0.10f;

        [Tooltip("Acceleration smoothing")]
        public float accelFilterTime = 0.10f;

        private Vector3 _posSmoothed;
        private Vector3 _posSmoothedPrev;
        private bool _posInit;

        private float _fwdSpeedSmoothed;
        private float _fwdSpeedSmoothedPrev;

        private float _accelSmoothed;

        private float _currentPitch;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }
        
        private void Start()
        {
            if (movingTransform == null)
            {
                movingTransform = transform;
            }

            _currentPitch = movingTransform.localEulerAngles.x;
        }

        private void Update()
        {
            if (vehicleRoot == null || vehicleRoot.IsServerInitialized)
            {
                return;
            }
            
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }
        }
    }
}
