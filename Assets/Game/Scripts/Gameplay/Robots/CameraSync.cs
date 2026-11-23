using RAV.Scripts.Diagnostics;
using UnityEngine;

namespace RAV.Scripts.Gameplay.Robots
{
    [DefaultExecutionOrder(-90)]
    public class CameraSync : MonoBehaviour
    {
        public static CameraSync In;
        public Transform target;
        public Camera gameplayCamera;

        private Transform _lastLoggedTarget;
        
        private void Awake()
        {
            In = this;
            GameplayRuntimeDiagnostics.Record("camera_sync_awake", "sync", name, "position", transform.position);
            LogDiagnostics("AWAKE");
        }

        private void OnEnable()
        {
            In = this;
            GameplayRuntimeDiagnostics.Record("camera_sync_enabled", "sync", name, "position", transform.position);
            LogDiagnostics("ENABLE");
        }

        private void OnDisable()
        {
            GameplayRuntimeDiagnostics.Record("camera_sync_disabled", "sync", name, "target", target != null ? target.name : string.Empty);
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
            if (_lastLoggedTarget != target)
            {
                _lastLoggedTarget = target;
                GameplayRuntimeDiagnostics.Record(
                    "camera_sync_target_changed",
                    "sync", name,
                    "target", target != null ? target.name : string.Empty,
                    "target_position", target != null ? target.position : Vector3.zero);
            }

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
