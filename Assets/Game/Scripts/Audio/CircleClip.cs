using UnityEngine;

namespace Game.Scripts.Audio
{
    [System.Serializable]
    public class CircleClip
    {
        private AudioPreset _mainClip;
        private float _timer;
        public string name;
        private float _updateTime;
            
        public CircleClip(AudioPreset clip, float time, string clipName)
        {
            _updateTime = time;
            name = clipName;
            _mainClip = clip;
            _mainClip.SetLoop(true);
            UpdateTime();
        }

        public void UpdateTime()
        {
            _timer = Time.time + _updateTime;
        }
    }
}
