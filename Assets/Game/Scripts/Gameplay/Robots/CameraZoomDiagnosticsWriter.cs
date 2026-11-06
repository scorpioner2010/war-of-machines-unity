using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    internal static class CameraZoomDiagnosticsWriter
    {
        private const string RelativeLogPath = "Logs/CameraZoomDiagnostics.log";
        private static readonly object Sync = new object();
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static string _logPath;
        private static bool _headerWritten;
        private static bool _writeFailureReported;

        public static string LogPath => GetLogPath();

        public static void Write(string message)
        {
            try
            {
                lock (Sync)
                {
                    string logPath = GetLogPath();
                    string directory = Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (FileStream stream = new FileStream(
                               logPath,
                               FileMode.Append,
                               FileAccess.Write,
                               FileShare.ReadWrite))
                    using (StreamWriter writer = new StreamWriter(stream, Utf8WithoutBom))
                    {
                        if (!_headerWritten)
                        {
                            writer.WriteLine(
                                $"==== CameraZoomDiagnostics process started {FormatTimestamp(DateTime.Now)} path={logPath} ====");
                            _headerWritten = true;
                        }

                        writer.WriteLine(
                            $"{FormatTimestamp(DateTime.Now)} frame={Time.frameCount} {message}");
                        writer.Flush();
                    }
                }
            }
            catch (Exception exception)
            {
                ReportWriteFailure(exception);
            }
        }

        private static string GetLogPath()
        {
            if (!string.IsNullOrEmpty(_logPath))
            {
                return _logPath;
            }

            DirectoryInfo dataDirectory = new DirectoryInfo(Application.dataPath);
            DirectoryInfo projectDirectory = dataDirectory.Parent;
            string rootPath = projectDirectory != null
                ? projectDirectory.FullName
                : dataDirectory.FullName;
            _logPath = Path.Combine(rootPath, RelativeLogPath.Replace('/', Path.DirectorySeparatorChar));
            return _logPath;
        }

        private static string FormatTimestamp(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        private static void ReportWriteFailure(Exception exception)
        {
            if (_writeFailureReported)
            {
                return;
            }

            _writeFailureReported = true;
            Debug.LogWarning($"[CameraZoomDiag] Failed to write {RelativeLogPath}: {exception.Message}");
        }
    }
}
