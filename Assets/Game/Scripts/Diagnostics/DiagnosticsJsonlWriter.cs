using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Game.Scripts.Diagnostics
{
    public sealed class DiagnosticsJsonlWriter : IDisposable
    {
        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private readonly Thread _thread;
        private volatile bool _running;
        private volatile bool _failed;

        public string FilePath { get; private set; }

        public DiagnosticsJsonlWriter(string sessionId)
        {
            FilePath = BuildFilePath(sessionId);
            _running = true;
            _thread = new Thread(WriterLoop)
            {
                IsBackground = true,
                Name = "DiagnosticsJsonlWriter"
            };
            _thread.Start();
        }

        public void Enqueue(string line)
        {
            if (!_running || _failed || string.IsNullOrEmpty(line))
            {
                return;
            }

            _queue.Enqueue(line);
            _signal.Set();
        }

        public void Dispose()
        {
            _running = false;
            _signal.Set();
            if (_thread != null && _thread.IsAlive)
            {
                _thread.Join(500);
            }

            _signal.Dispose();
        }

        private void WriterLoop()
        {
            try
            {
                string directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (FileStream stream = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    while (_running || !_queue.IsEmpty)
                    {
                        bool wrote = FlushQueuedLines(writer);
                        if (wrote)
                        {
                            writer.Flush();
                        }
                        else if (_running)
                        {
                            _signal.WaitOne(250);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogWarning("Diagnostics JSONL writer disabled: " + ex.Message);
            }
        }

        private bool FlushQueuedLines(StreamWriter writer)
        {
            if (writer == null)
            {
                return false;
            }

            bool wrote = false;
            while (_queue.TryDequeue(out string line))
            {
                writer.WriteLine(line);
                wrote = true;
            }

            return wrote;
        }

        private static string BuildFilePath(string sessionId)
        {
            string root = Directory.GetCurrentDirectory();
            string directory = Path.Combine(root, "diagnostics", "logs");
            string safeSession = string.IsNullOrEmpty(sessionId) ? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") : sessionId;
            return Path.Combine(directory, "session-" + safeSession + ".jsonl");
        }
    }
}
