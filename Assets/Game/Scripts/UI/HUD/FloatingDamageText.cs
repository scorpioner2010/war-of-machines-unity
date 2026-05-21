using System.Collections.Generic;
using Game.Scripts.Client;
using Game.Scripts.Gameplay.Robots;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class FloatingDamageText : MonoBehaviour
    {
        private const int DefaultMaxInactive = 32;

        private static readonly Dictionary<int, FloatingDamageTextPool> Pools = new Dictionary<int, FloatingDamageTextPool>(8);
        private static readonly Dictionary<int, FloatingDamageTextPool> Owners = new Dictionary<int, FloatingDamageTextPool>(64);

        public TMP_Text text;
        private Camera _camera;
        private float _duration;
        private float _elapsed;
        private float _moveUp;
        private float _endScale;
        private Vector3 _startPosition;
        private Color _visibleColor = Color.white;
        private bool _playing;

        public static void Prewarm(FloatingDamageText prefab, int count, int maxInactive)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            FloatingDamageTextPool pool = GetPool(prefab, maxInactive);
            int targetInactive = Mathf.Min(count, pool.MaxInactive);
            while (pool.Inactive.Count < targetInactive)
            {
                FloatingDamageText instance = CreateInstance(pool);
                instance.PrepareForPoolRelease();
                pool.Inactive.Push(instance);
            }
        }

        public static FloatingDamageText Rent(FloatingDamageText prefab, Vector3 position, Quaternion rotation, Transform parent, int maxInactive)
        {
            if (prefab == null)
            {
                return null;
            }

            FloatingDamageTextPool pool = GetPool(prefab, maxInactive);
            FloatingDamageText instance;
            if (pool.Inactive.Count > 0)
            {
                instance = pool.Inactive.Pop();
            }
            else
            {
                instance = CreateInstance(pool);
            }

            Transform instanceTransform = instance.transform;
            instanceTransform.SetParent(parent, false);
            instanceTransform.SetPositionAndRotation(position, rotation);
            if (!instance.gameObject.activeSelf)
            {
                instance.gameObject.SetActive(true);
            }

            return instance;
        }

        public void SetText(string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }

            BeginAnimation();
        }

        public void SetDamage(int value)
        {
            if (text != null)
            {
                text.SetText("{0}", value);
            }

            BeginAnimation();
        }

        private void LateUpdate()
        {
            if (_camera != null)
            {
                transform.forward = _camera.transform.forward;
            }

            if (!_playing)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = _duration > 0.0001f ? Mathf.Clamp01(_elapsed / _duration) : 1f;
            float eased = 1f - (1f - t) * (1f - t);

            transform.position = _startPosition + Vector3.up * (_moveUp * eased);
            float scale = Mathf.Lerp(1f, _endScale, eased);
            transform.localScale = new Vector3(scale, scale, scale);

            if (text != null)
            {
                Color color = _visibleColor;
                color.a = 1f - t;
                text.color = color;
            }

            if (t >= 1f)
            {
                ReleaseOrDestroy();
            }
        }

        internal void PrepareForPoolRelease()
        {
            _playing = false;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void BeginAnimation()
        {
            _camera = CameraSync.In != null ? CameraSync.In.gameplayCamera : null;
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            _duration = Mathf.Max(0.01f, settings.floatingDamageTextDuration);
            _moveUp = settings.floatingDamageTextMoveUp;
            _endScale = Mathf.Max(0.01f, settings.floatingDamageTextEndScale);
            _elapsed = 0f;
            _startPosition = transform.position;
            transform.localScale = Vector3.one;

            if (text != null)
            {
                _visibleColor = text.color;
                _visibleColor.a = 1f;
                text.color = _visibleColor;
            }

            _playing = true;
        }

        private void ReleaseOrDestroy()
        {
            if (!Release(this))
            {
                Destroy(gameObject);
            }
        }

        private static bool Release(FloatingDamageText instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (!Owners.TryGetValue(instance.GetInstanceID(), out FloatingDamageTextPool pool))
            {
                return false;
            }

            if (pool.Inactive.Count >= pool.MaxInactive)
            {
                Owners.Remove(instance.GetInstanceID());
                Destroy(instance.gameObject);
                return true;
            }

            instance.PrepareForPoolRelease();
            instance.transform.SetParent(pool.Root, false);
            pool.Inactive.Push(instance);
            return true;
        }

        private static FloatingDamageTextPool GetPool(FloatingDamageText prefab, int maxInactive)
        {
            int prefabId = prefab.GetInstanceID();
            if (Pools.TryGetValue(prefabId, out FloatingDamageTextPool pool))
            {
                if (maxInactive > pool.MaxInactive)
                {
                    pool.MaxInactive = maxInactive;
                }

                return pool;
            }

            pool = new FloatingDamageTextPool(prefab, Mathf.Max(1, maxInactive));
            Pools.Add(prefabId, pool);
            return pool;
        }

        private static FloatingDamageText CreateInstance(FloatingDamageTextPool pool)
        {
            FloatingDamageText instance = Instantiate(pool.Prefab, pool.Root);
            Owners[instance.GetInstanceID()] = pool;
            return instance;
        }

        private sealed class FloatingDamageTextPool
        {
            public readonly FloatingDamageText Prefab;
            public readonly Stack<FloatingDamageText> Inactive;
            public readonly Transform Root;
            public int MaxInactive;

            public FloatingDamageTextPool(FloatingDamageText prefab, int maxInactive)
            {
                Prefab = prefab;
                MaxInactive = Mathf.Max(1, maxInactive > 0 ? maxInactive : DefaultMaxInactive);
                Inactive = new Stack<FloatingDamageText>(MaxInactive);
                GameObject rootObject = new GameObject("FloatingDamageTextPool_" + prefab.name);
                rootObject.hideFlags = HideFlags.HideInHierarchy;
                DontDestroyOnLoad(rootObject);
                Root = rootObject.transform;
            }
        }
    }
}
