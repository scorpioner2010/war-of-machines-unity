using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    /// <summary>
    /// Стабільний "танковий кивок" без пружин:
    /// - швидкість і прискорення з Δпозиції (без Rigidbody),
    /// - фільтри позиції/швидкості/прискорення,
    /// - компенсація горбиків через проектування на площину ґрунту,
    /// - експоненційне довертання з параметром швидкості.
    /// </summary>
    public class TankNosePitch : MonoBehaviour
    {
        public TankRoot tankRoot;
        
        [Header("References")]
        public Transform baseTransform;            // хто задає forward та позицію (зазвичай шасі)
        public Transform movingTransform;

        [Header("Limits")]
        [Tooltip("Максимальний модуль кута кивка, °")]
        public float maxPitchAbs = 10f;

        [Header("Response")]
        [Tooltip("Скільки градусів на 1 м/с² базової інтенсивності")]
        public float degPerAccelBase = 0.12f;

        [Tooltip("Інтенсивність качання (масштаб на degPerAccelBase)")]
        [Range(0f, 2f)] public float swayIntensity = 1f;

        [Tooltip("Швидкість довертання до таргету (чим більше — тим швидше)")]
        public float swaySpeed = 6f; // 1/сек; 6 = агресивно, 2-3 = плавно

        [Header("Noise Rejection")]
        [Tooltip("Ігнорувати вертикальні коливання рельєфу")]
        public bool ignoreVertical = true;

        [Tooltip("Проектувати рух на площину ґрунту (GroundPhysicsMotor.GroundNormal)")]
        public bool useGroundPlane = true;

        [Tooltip("Поріг прискорення (м/с²), менше якого вважаємо шумом")]
        public float accelDeadZone = 0.25f;

        [Header("Filtering (seconds)")]
        [Tooltip("Фільтр позиції перед диференціюванням")]
        public float posFilterTime = 0.06f;

        [Tooltip("Фільтр швидкості")]
        public float speedFilterTime = 0.10f;

        [Tooltip("Фільтр прискорення")]
        public float accelFilterTime = 0.10f;

        // --- runtime state ---
        private Vector3 _posSmoothed;
        private Vector3 _posSmoothedPrev;
        private bool _posInit;

        private float _fwdSpeedSmoothed;     // м/с
        private float _fwdSpeedSmoothedPrev; // для похідної

        private float _accelSmoothed;        // м/с²

        private float _currentPitch;         // застосований кут, °
        
        private void Start()
        {
            if (movingTransform == null) movingTransform = transform;
            _currentPitch = movingTransform.localEulerAngles.x;
        }

        private void Update()
        {
            if (tankRoot.IsServer)
            {
                return;
            }
            
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            /*
            
            // === 1) Вхідні дані: позиція бази та нормаль ґрунту ===
            Vector3 curPos = baseTransform.position;
            Vector3 planeNormal = useGroundPlane ? groundPhysicsMotor.GroundNormal.normalized : Vector3.up;

            // === 2) Згладжуємо позицію (EMA) для приглушення тремтіння від рельєфу ===
            float aPos = 1f - Mathf.Exp(-dt / Mathf.Max(1e-4f, posFilterTime));
            if (!_posInit)
            {
                _posSmoothed = curPos;
                _posSmoothedPrev = curPos;
                _posInit = true;
            }
            else
            {
                _posSmoothed = Vector3.Lerp(_posSmoothed, curPos, aPos);
            }

            // Рух за кадр у згладженому просторі
            Vector3 dPos = _posSmoothed - _posSmoothedPrev;
            _posSmoothedPrev = _posSmoothed;

            // За потреби — прибираємо вертикаль
            if (ignoreVertical)
                dPos.y = 0f;

            // За потреби — проектуємо на площину ґрунту (краще гасить «горбики»)
            if (useGroundPlane)
                dPos = Vector3.ProjectOnPlane(dPos, planeNormal);

            // === 3) Обчислюємо поздовжню швидкість (проєкція на forward бази, спроєктований на ту саму площину) ===
            Vector3 fwd = baseTransform.forward;
            if (ignoreVertical || useGroundPlane)
                fwd = Vector3.ProjectOnPlane(fwd, planeNormal);
            if (fwd.sqrMagnitude > 1e-6f)
                fwd.Normalize();

            float fwdSpeed = Vector3.Dot(dPos, fwd) / dt; // м/с

            // Згладжуємо швидкість
            float aSpd = 1f - Mathf.Exp(-dt / Mathf.Max(1e-4f, speedFilterTime));
            _fwdSpeedSmoothed = Mathf.Lerp(_fwdSpeedSmoothed, fwdSpeed, aSpd);

            // === 4) Прискорення як похідна від фільтрованої швидкості ===
            float accelRaw = (_fwdSpeedSmoothed - _fwdSpeedSmoothedPrev) / dt;
            _fwdSpeedSmoothedPrev = _fwdSpeedSmoothed;

            // Фільтр прискорення
            float aAcc = 1f - Mathf.Exp(-dt / Mathf.Max(1e-4f, accelFilterTime));
            _accelSmoothed = Mathf.Lerp(_accelSmoothed, accelRaw, aAcc);

            // Мертва зона
            float accel = (Mathf.Abs(_accelSmoothed) < accelDeadZone) ? 0f : _accelSmoothed;

            // === 5) Цільовий кут: розгін -> ніс вниз (мінус), гальмування -> вгору (плюс) ===
            float degPerAccel = degPerAccelBase * swayIntensity;
            float targetPitch = Mathf.Clamp(-accel * degPerAccel, -maxPitchAbs, maxPitchAbs);

            // === 6) Експоненційне довертання до таргету (швидкість за параметром swaySpeed) ===
            float k = 1f - Mathf.Exp(-swaySpeed * dt); // 0..1
            _currentPitch = Mathf.LerpAngle(_currentPitch, targetPitch, k);

            // === 7) Застосувати тільки по локальному X ===
            Vector3 e = movingTransform.localEulerAngles;
            e.x = _currentPitch;
            movingTransform.localEulerAngles = e;
            
            */
        }
    }
}
