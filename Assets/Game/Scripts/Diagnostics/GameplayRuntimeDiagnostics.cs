using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RAV.Scripts.Diagnostics
{
    public static class GameplayRuntimeDiagnostics
    {
        private const string RelativeLogPath = "Logs/GameplayRuntimeDiagnostics.log";
        private const string LogPathEnvironmentVariable = "RAV_GAMEPLAY_DIAGNOSTICS_PATH";
        private const long MaxLogBytes = 5 * 1024 * 1024;
        private const int RetainedLogFiles = 2;
        private static readonly object Sync = new object();
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static readonly string ProcessSessionId = Guid.NewGuid().ToString("N");
        private static readonly Regex BearerSecret = new Regex(
            @"\bBearer\s+[^\s,;]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex InlineSecret = new Regex(
            @"\b(password|pwd|secret|token|credential|authorization)\b\s*[:=]\s*[^\s,;]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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
                .Append(Application.isBatchMode ? "batch" : "interactive")
                .Append(" session_id=")
                .Append(ProcessSessionId);

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
                        .Append(SanitizeFieldValue(key, fields[i + 1]));
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

                    RotateIfNeeded(logPath);

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

        private static void RotateIfNeeded(string logPath)
        {
            if (!File.Exists(logPath) || new FileInfo(logPath).Length < MaxLogBytes)
            {
                return;
            }

            string oldestBackup = logPath + "." + RetainedLogFiles.ToString(CultureInfo.InvariantCulture);
            if (File.Exists(oldestBackup))
            {
                File.Delete(oldestBackup);
            }

            for (int index = RetainedLogFiles - 1; index >= 1; index--)
            {
                string source = logPath + "." + index.ToString(CultureInfo.InvariantCulture);
                string destination = logPath + "." + (index + 1).ToString(CultureInfo.InvariantCulture);
                if (File.Exists(source))
                {
                    File.Move(source, destination);
                }
            }

            File.Move(logPath, logPath + ".1");
            _headerWritten = false;
        }

        private static string SanitizeFieldValue(string key, object value)
        {
            string normalizedKey = key.Replace("_", string.Empty).Replace("-", string.Empty);
            if (normalizedKey.IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedKey.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedKey.IndexOf("secret", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedKey.IndexOf("credential", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedKey.IndexOf("authorization", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedKey.IndexOf("cookie", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedKey.IndexOf("privatekey", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedKey.IndexOf("connectionstring", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "[REDACTED]";
            }

            string formatted = FormatValue(value);
            if (formatted.Length > 512)
            {
                formatted = formatted.Substring(0, 512) + "_truncated";
            }

            if (normalizedKey.IndexOf("reason", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedKey.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedKey.IndexOf("message", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedKey.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                formatted = BearerSecret.Replace(formatted, "Bearer_[REDACTED]");
                formatted = InlineSecret.Replace(formatted, "$1=[REDACTED]");
            }

            return Sanitize(formatted);
        }

        private static string GetLogPath()
        {
            if (!string.IsNullOrEmpty(_logPath))
            {
                return _logPath;
            }

            string configuredPath = Environment.GetEnvironmentVariable(LogPathEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                _logPath = Path.GetFullPath(configuredPath.Trim());
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
