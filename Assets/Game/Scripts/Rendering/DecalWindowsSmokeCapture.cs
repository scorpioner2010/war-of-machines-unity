using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Game.Rendering
{
    public sealed class DecalWindowsSmokeCapture : MonoBehaviour
    {
        [SerializeField] private float captureDelay = 2f;

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(captureDelay);
            yield return new WaitForEndOfFrame();

            CaptureFrame();
        }

        private void CaptureFrame()
        {
            Camera camera = FindFirstObjectByType<Camera>();
            if (camera == null)
                throw new InvalidOperationException("Decal smoke capture requires a camera.");

            int width = Mathf.Max(Screen.width, 1);
            int height = Mathf.Max(Screen.height, 1);
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;

            Texture2D frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            frame.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            frame.Apply();

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            Destroy(renderTexture);

            string outputPath = Environment.GetEnvironmentVariable("DECAL_SMOKE_OUTPUT");
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = Path.Combine(Application.persistentDataPath, "DecalWindowsSmoke.png");

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(outputPath, frame.EncodeToPNG());

            Color center = frame.GetPixel(width / 2, height / 2);
            Color reference = frame.GetPixel((int)(width * 0.68f), height / 2);
            float difference = Mathf.Abs(center.r - reference.r) + Mathf.Abs(center.g - reference.g) + Mathf.Abs(center.b - reference.b);
            bool passed = difference > 0.08f;
            Debug.Log($"DECAL_WINDOWS_SMOKE_RESULT passed={passed} output={outputPath} center={center} reference={reference} difference={difference}");

            Destroy(frame);
            Application.Quit(passed ? 0 : 11);
        }
    }
}
