using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Game.Scripts.Core.Utils
{
    public class FPSCounter : MonoBehaviour
    {
        private float _accum;
        private int _frames;
        private float _timeleft;
        private float _fps;
        private float _updateInterval = 0.1f;
        private GUIStyle _textStyle = new();
        private List<int> _values = new();
        private const int CountList = 300;
        public int middleFps;
        
        private int _valueSum;

        private void OnGUI()
        {
            //Display the fps and round to 2 decimals
            GUI.Label(new Rect(10, 250, 100, 25), _fps.ToString("0", CultureInfo.InvariantCulture) + " FPS", _textStyle);
            GUI.Label(new Rect(10, 200, 100, 25), middleFps.ToString("0", CultureInfo.InvariantCulture) + " FPS meddle(10s)", _textStyle);
        }

        private void Start()
        {
            _textStyle.fontStyle = FontStyle.Bold;
            _textStyle.fontSize = 25;
            _textStyle.normal.textColor = Color.white;
            _timeleft = _updateInterval;
        }

        private void FPSCounterBehaviour()
        {
            _timeleft -= Time.deltaTime;
            _accum += Time.timeScale / Time.deltaTime;
            ++_frames;

            if (_timeleft <= 0)
            {
                _fps = (_accum / _frames);
                _timeleft = _updateInterval;
                _accum = 0;
                _frames = 0;
            }
        }
    
        private void Update()
        {
            FPSCounterBehaviour();
            CalculateMiddleFPS();
        }
    
        private void CalculateMiddleFPS()
        {
            if (_values.Count >= CountList)
            {
                _valueSum -= _values[0];
                _values.RemoveAt(0);
            }

            _values.Add((int)_fps);
            _valueSum += (int)_fps;
            middleFps = (_valueSum / _values.Count);
        }
    }
}
