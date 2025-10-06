using System;
using UnityEngine;

namespace Game.Scripts.Core.Utils
{
    public struct EventBool
    {
        private bool _value;
        public event Action<bool> OnChange;
    
        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value)
                {
                    return;
                }

                OnChange?.Invoke(value);
                _value = value;
            }
        }
    }

    public class ValueTimeUpdater : MonoBehaviour
    {
        public EventBool IsActive;
        private float _timer;

        public void Remove()
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        public void UpdateValue(bool isActive, float time = 0)
        {
            IsActive.Value = isActive;
            _timer = Time.time + time;
        }

        private void Update()
        {
            if (_timer < Time.time && IsActive.Value)
            {
                UpdateValue(false);
            }
        }

        public static ValueTimeUpdater InitUpdater(string updaterName, Transform parent)
        {
            GameObject objUpdater = new GameObject(updaterName);
            objUpdater.transform.parent = parent;
            ValueTimeUpdater updater = objUpdater.AddComponent<ValueTimeUpdater>();
            return updater;
        }
    }
}