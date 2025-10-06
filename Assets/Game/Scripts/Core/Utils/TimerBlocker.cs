using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Core.Utils
{
    public class TimerBlocker
    {
        private float _delay;
        private CancellationTokenSource _cts;
        
        private float _timer;
        private Action _someLogic;
        
        public TimerBlocker(float delay) => _delay = delay;

        public void SetDelay(float delay)
        {
            _delay = delay;
        }
        
        public void Block() => _timer = Time.time + _delay;
        public bool IsBlock() => _timer > Time.time;

        public void Stop()
        {
            if (_cts != null)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }
        }
        
        public async void Invoke(Action someLogic)
        {
            if (_cts != null)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }

            _cts = new CancellationTokenSource();
            
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_delay), cancellationToken: _cts.Token);
                someLogic?.Invoke();
            }
            catch (OperationCanceledException)
            {
                //Debug.LogError("restore cancel");
            }
        }
    }
}