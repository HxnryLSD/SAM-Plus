/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.IO;
using System.Text;

namespace SAM.API
{
    /// <summary>
    /// Simple file-based logger for error tracking.
    /// Logs are written to "logs/error.log" in the application directory.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new();
        private static readonly string _logDirectory;
        private static readonly string _logFilePath;

        static Logger()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            _logFilePath = Path.Combine(_logDirectory, "error.log");
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void Warn(string message)
        {
            WriteLog("WARN", message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        /// <summary>
        /// Logs an exception with full stack trace.
        /// </summary>
        public static void Error(Exception ex, string context = null)
        {
            var sb = new StringBuilder();
            
            if (!string.IsNullOrEmpty(context))
            {
                sb.AppendLine($"Context: {context}");
            }
            
            sb.AppendLine($"Exception: {ex.GetType().FullName}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                sb.AppendLine($"InnerException: {ex.InnerException.GetType().FullName}");
                sb.AppendLine($"InnerMessage: {ex.InnerException.Message}");
                sb.AppendLine($"InnerStackTrace: {ex.InnerException.StackTrace}");
            }

            WriteLog("ERROR", sb.ToString());
        }

        /// <summary>
        /// Logs a fatal error (unhandled exception).
        /// </summary>
        public static void Fatal(Exception ex, string source)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== FATAL UNHANDLED EXCEPTION ===");
            sb.AppendLine($"Source: {source}");
            sb.AppendLine($"Exception: {ex.GetType().FullName}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");
            
            var inner = ex.InnerException;
            while (inner != null)
            {
                sb.AppendLine($"--- Inner Exception ---");
                sb.AppendLine($"Type: {inner.GetType().FullName}");
                sb.AppendLine($"Message: {inner.Message}");
                sb.AppendLine($"StackTrace: {inner.StackTrace}");
                inner = inner.InnerException;
            }
            
            sb.AppendLine($"=================================");

            WriteLog("FATAL", sb.ToString());
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }

                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logLine = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";

                    File.AppendAllText(_logFilePath, logLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Logging should never crash the application
            }
        }

        /// <summary>
        /// Gets the path to the log file.
        /// </summary>
        public static string LogFilePath => _logFilePath;
    }
}
