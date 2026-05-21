using Game.Scripts.Diagnostics;
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
        private const int CountList = 300;
        private readonly int[] _values = new int[CountList];
        private int _valueIndex;
        private int _valueCount;
        public int middleFps;
        
        private int _valueSum;
        private string _fpsLabel = "0 FPS";
        private string _middleFpsLabel = "0 FPS middle(10s)";
        private int _lastLabelFps = int.MinValue;
        private int _lastLabelMiddleFps = int.MinValue;

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (ProfileScope.Measure("OnGUI.FPSCounter", DiagnosticsCategories.Editor))
            {
                GUI.Label(new Rect(10, 250, 160, 25), _fpsLabel, _textStyle);
                GUI.Label(new Rect(10, 200, 220, 25), _middleFpsLabel, _textStyle);
            }
#endif
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FPSCounterBehaviour();
            CalculateMiddleFPS();
#else
            enabled = false;
#endif
        }
    
        private void CalculateMiddleFPS()
        {
            int fps = Mathf.RoundToInt(_fps);
            if (_valueCount < CountList)
            {
                _values[_valueIndex] = fps;
                _valueSum += fps;
                _valueCount++;
            }
            else
            {
                _valueSum -= _values[_valueIndex];
                _values[_valueIndex] = fps;
                _valueSum += fps;
            }

            _valueIndex = (_valueIndex + 1) % CountList;
            middleFps = _valueCount > 0 ? _valueSum / _valueCount : 0;
            if (_lastLabelFps != fps)
            {
                _lastLabelFps = fps;
                _fpsLabel = fps + " FPS";
            }

            if (_lastLabelMiddleFps != middleFps)
            {
                _lastLabelMiddleFps = middleFps;
                _middleFpsLabel = middleFps + " FPS middle(10s)";
            }
        }
    }
}
