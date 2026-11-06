using Game.Scripts.Diagnostics;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DefaultExecutionOrder(-90)]
    public class CameraSync : MonoBehaviour
    {
        public static CameraSync In;
        public Transform target;
        public Camera gameplayCamera;
        
        private void Awake()
        {
            In = this;
            LogDiagnostics("AWAKE");
        }

        private void OnEnable()
        {
            In = this;
            LogDiagnostics("ENABLE");
        }

        private void OnDisable()
        {
            LogDiagnostics("DISABLE_BEFORE_CLEAR");
            if (In == this)
            {
                In = null;
            }
        }

        private void OnDestroy()
        {
            LogDiagnostics("DESTROY_BEFORE_CLEAR");
            if (In == this)
            {
                In = null;
            }
        }

        public Transform GetAimTransform()
        {
            if (target != null)
            {
                return target;
            }

            return transform;
        }

        public Quaternion GetAimRotation()
        {
            Transform aimTransform = GetAimTransform();
            return aimTransform != null ? aimTransform.rotation : transform.rotation;
        }

        public Vector3 GetAimForward()
        {
            return GetAimRotation() * Vector3.forward;
        }

        public void SyncToTarget()
        {
            if (target != null)
            {
                transform.SetPositionAndRotation(target.position, target.rotation);
            }
        }

        private void LateUpdate()
        {
            using (ProfileScope.Measure("Camera.Sync.LateUpdate", DiagnosticsCategories.Render))
            {
                SyncToTarget();
            }
        }

        private void LogDiagnostics(string stage)
        {
            CameraZoomDiagnosticsWriter.Write(
                $"[CameraZoomDiag][Sync] stage={stage} sync={name}#{GetInstanceID()} frame={Time.frameCount} " +
                $"active={isActiveAndEnabled} target={(target != null ? target.name + "#" + target.GetInstanceID() : "null")} " +
                $"syncPos={transform.position} syncRot={transform.eulerAngles} " +
                $"gameplayCamera={(gameplayCamera != null ? gameplayCamera.name + "#" + gameplayCamera.GetInstanceID() : "null")}");
        }
    }
}
