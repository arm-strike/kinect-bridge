using System;
using System.Collections.Generic;

namespace KinectBridge
{
    public sealed class BridgeLogger
    {
        private sealed class LogEntry
        {
            public string Message;
            public DateTime LastLoggedUtc;
        }

        private readonly Dictionary<string, LogEntry> _entries = new Dictionary<string, LogEntry>();
        private readonly object _gate = new object();

        public void Debug(string message)
        {
            Write("DEBUG", message);
        }

        public void Info(string message)
        {
            Write("INFO", message);
        }

        public void Warn(string message)
        {
            Write("WARN", message);
        }

        public void Error(string message)
        {
            Write("ERROR", message);
        }

        public void WarnThrottled(string key, string message, TimeSpan interval)
        {
            if (ShouldWrite(key, message, interval, DateTime.UtcNow))
            {
                Warn(message);
            }
        }

        public void InfoThrottled(string key, string message, TimeSpan interval)
        {
            if (ShouldWrite(key, message, interval, DateTime.UtcNow))
            {
                Info(message);
            }
        }

        public void ErrorThrottled(string key, string message, TimeSpan interval)
        {
            if (ShouldWrite(key, message, interval, DateTime.UtcNow))
            {
                Error(message);
            }
        }

        public void Reset(string key)
        {
            lock (_gate)
            {
                _entries.Remove(key);
            }
        }

        private void Write(string level, string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "][" + level + "] " + message);
        }

        private bool ShouldWrite(string key, string message, TimeSpan interval, DateTime nowUtc)
        {
            lock (_gate)
            {
                LogEntry entry;
                if (!_entries.TryGetValue(key, out entry))
                {
                    entry = new LogEntry();
                    _entries[key] = entry;
                }

                if (!string.Equals(entry.Message, message, StringComparison.Ordinal))
                {
                    entry.Message = message;
                    entry.LastLoggedUtc = nowUtc;
                    return true;
                }

                if (nowUtc - entry.LastLoggedUtc >= interval)
                {
                    entry.LastLoggedUtc = nowUtc;
                    return true;
                }

                return false;
            }
        }
    }
}
