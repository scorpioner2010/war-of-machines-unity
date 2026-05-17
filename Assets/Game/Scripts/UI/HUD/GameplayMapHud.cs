using Game.Scripts.Gameplay.Robots;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class GameplayMapHud : MonoBehaviour
    {
        public Image miniMapImage;
        public Image fullMapImage;
        public RectTransform localPlayerIconMini;
        public RectTransform localPlayerIconFull;
        public GameObject fullMapRoot;
        public Vector2 worldMin = new Vector2(-100f, -100f);
        public Vector2 worldMax = new Vector2(100f, 100f);
        public KeyCode fullMapKey = KeyCode.M;
        public bool rotateIcons = true;

        private VehicleRoot _localPlayer;
        private RectTransform _miniMapRect;
        private RectTransform _fullMapRect;
        private bool _fullMapVisible;

        private void Awake()
        {
            CacheRects();
            _fullMapVisible = true;
            SetFullMapVisible(false);
            SetIconVisible(localPlayerIconMini, false);
            SetIconVisible(localPlayerIconFull, false);
        }

        private void OnEnable()
        {
            VehicleRoot.LocalPlayerVehicleChanged += SetLocalPlayer;
            SetLocalPlayer(VehicleRoot.LocalPlayerVehicle);
        }

        private void OnDisable()
        {
            VehicleRoot.LocalPlayerVehicleChanged -= SetLocalPlayer;
        }

        private void Update()
        {
            bool shouldShowFullMap = Input.GetKey(fullMapKey);
            SetFullMapVisible(shouldShowFullMap);

            if (_localPlayer == null)
            {
                SetIconVisible(localPlayerIconMini, false);
                SetIconVisible(localPlayerIconFull, false);
                return;
            }

            Vector3 worldPosition = _localPlayer.transform.position;
            float yaw = _localPlayer.transform.eulerAngles.y;

            UpdateIcon(_miniMapRect, localPlayerIconMini, worldPosition, yaw, true);
            UpdateIcon(_fullMapRect, localPlayerIconFull, worldPosition, yaw, _fullMapVisible);
        }

        public void SetLocalPlayer(VehicleRoot vehicleRoot)
        {
            _localPlayer = vehicleRoot;
        }

        private void CacheRects()
        {
            _miniMapRect = miniMapImage != null ? miniMapImage.rectTransform : null;
            _fullMapRect = fullMapImage != null ? fullMapImage.rectTransform : null;
        }

        private void UpdateIcon(
            RectTransform mapRect,
            RectTransform iconRect,
            Vector3 worldPosition,
            float yaw,
            bool visible)
        {
            if (!visible || mapRect == null || iconRect == null || !TryGetNormalizedPosition(worldPosition, out Vector2 normalized))
            {
                SetIconVisible(iconRect, false);
                return;
            }

            SetIconVisible(iconRect, true);

            Rect rect = mapRect.rect;
            Vector2 localMapPosition = new Vector2(
                (normalized.x - mapRect.pivot.x) * rect.width,
                (normalized.y - mapRect.pivot.y) * rect.height);

            iconRect.position = mapRect.TransformPoint(localMapPosition);

            if (rotateIcons)
            {
                iconRect.localRotation = Quaternion.Euler(0f, 0f, -yaw);
            }
            else
            {
                iconRect.localRotation = Quaternion.identity;
            }
        }

        private bool TryGetNormalizedPosition(Vector3 worldPosition, out Vector2 normalized)
        {
            float width = worldMax.x - worldMin.x;
            float height = worldMax.y - worldMin.y;
            if (Mathf.Abs(width) <= 0.0001f || Mathf.Abs(height) <= 0.0001f)
            {
                normalized = default;
                return false;
            }

            normalized = new Vector2(
                (worldPosition.x - worldMin.x) / width,
                (worldPosition.z - worldMin.y) / height);
            return true;
        }

        private void SetFullMapVisible(bool visible)
        {
            if (_fullMapVisible == visible)
            {
                return;
            }

            _fullMapVisible = visible;
            GameObject root = fullMapRoot != null
                ? fullMapRoot
                : fullMapImage != null && fullMapImage.transform.parent != null
                    ? fullMapImage.transform.parent.gameObject
                    : null;

            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        private static void SetIconVisible(RectTransform iconRect, bool visible)
        {
            if (iconRect != null && iconRect.gameObject.activeSelf != visible)
            {
                iconRect.gameObject.SetActive(visible);
            }
        }
    }
}
