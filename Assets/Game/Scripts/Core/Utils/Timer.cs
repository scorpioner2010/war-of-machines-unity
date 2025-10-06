using UnityEngine;

namespace Game.Scripts.Core.Utils
{
    public class Timer
    {
        public readonly float Delay;
        private float _timer;

        public Timer(float delay)
        {
            Delay = delay;
        }
        
        public bool Try()
        {
            if (_timer < Time.time)
            {
                _timer = Time.time + Delay;
                return true;
            }

            return false;
        }
    }
}
