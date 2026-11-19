using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Diagnostics
{
    public static class GameplayRuntimeDiagnostics
    {
        private const string RelativeLogPath = "Logs/GameplayRuntimeDiagnostics.log";
        private static readonly object Sync = new object();
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static string _logPath;
        private static bool _headerWritten;
        private static bool _writeFailureReported;

        public static string LogPath => GetLogPath();

        public static void Record(string eventName, params object[] fields)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            StringBuilder line = new StringBuilder(256);
            line.Append("timestamp=")
                .Append(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture))
                .Append(" event=")
                .Append(Sanitize(eventName))
                .Append(" frame=")
                .Append(Time.frameCount.ToString(CultureInfo.InvariantCulture))
                .Append(" scene=")
                .Append(Sanitize(SceneManager.GetActiveScene().name))
                .Append(" process=")
                .Append(Application.isBatchMode ? "batch" : "interactive");

            if (fields != null)
            {
                for (int i = 0; i + 1 < fields.Length; i += 2)
                {
                    string key = fields[i] as string;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    line.Append(' ')
                        .Append(Sanitize(key))
                        .Append('=')
                        .Append(Sanitize(FormatValue(fields[i + 1])));
                }
            }

            WriteLine(line.ToString());
        }

        private static void WriteLine(string line)
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
                                $"==== GameplayRuntimeDiagnostics process started {DateTime.UtcNow:O} path={logPath} ====");
                            _headerWritten = true;
                        }

                        writer.WriteLine(line);
                        writer.Flush();
                    }
                }
            }
            catch (Exception exception)
            {
                if (_writeFailureReported)
                {
                    return;
                }

                _writeFailureReported = true;
                Debug.LogWarning($"[GameplayDiag] Failed to write {RelativeLogPath}: {exception.Message}");
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

        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "-";
            }

            return value
                .Replace("\r", "_")
                .Replace("\n", "_")
                .Replace(" ", "_")
                .Replace("=", ":")
                .Replace("|", ":");
        }
    }
}
