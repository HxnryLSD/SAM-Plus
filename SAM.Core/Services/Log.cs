/* Copyright (c) 2024-2026 Rick (rick 'at' gibbed 'dot' us)
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

using System.Runtime.CompilerServices;
using System.Text;
using SAM.Core.Utilities;

namespace SAM.Core.Services;

/// <summary>
/// Centralized logging service with detailed output.
/// Call <see cref="Initialize"/> at application startup to configure the app name.
/// </summary>
public static class Log
{
    private static readonly object _lock = new();
    private static readonly StringBuilder _logBuffer = new();
    private static string _logFilePath = string.Empty;
    private static string _appName = "SAM";
    private static bool _initialized;

    /// <summary>
    /// Initializes the logging service with the specified application name.
    /// Should be called once at application startup.
    /// </summary>
    /// <param name="appName">The application name (e.g., "SAM.WinUI" or "SAM.Manager")</param>
    public static void Initialize(string appName)
    {
        if (_initialized)
        {
            return;
        }

        _appName = appName;
        
        // Migrate old logs from root to Logs folder
        AppPaths.MigrateLegacyLogs();
        
        // Clean up old log files (older than 10 minutes)
        AppPaths.CleanupOldLogs();
        
        var filePrefix = appName.ToLowerInvariant().Replace(".", "_");
        _logFilePath = Path.Combine(AppPaths.LogsPath, $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        
        _initialized = true;
        
        Info("========================================");
        Info($"{_appName} Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Info($"Log file: {_logFilePath}");
        Info($"OS: {Environment.OSVersion}");
        Info($".NET: {Environment.Version}");
        Info($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        Info($"64-bit Process: {Environment.Is64BitProcess}");
        Info("========================================");
    }

    private static void EnsureInitialized()
    {
        if (!_initialized)
        {
            Initialize("SAM");
        }
    }

    public static void Trace(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        WriteLog("TRACE", message, memberName, sourceFilePath, sourceLineNumber);
    }

    public static void Debug(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        WriteLog("DEBUG", message, memberName, sourceFilePath, sourceLineNumber);
    }

    public static void Info(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        WriteLog("INFO", message, memberName, sourceFilePath, sourceLineNumber);
    }

    public static void Warn(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        WriteLog("WARN", message, memberName, sourceFilePath, sourceLineNumber);
    }

    public static void Error(
        string message,
        Exception? ex = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var fullMessage = ex is null ? message : $"{message}\nException: {ex}";
        WriteLog("ERROR", fullMessage, memberName, sourceFilePath, sourceLineNumber);
    }

    public static void Exception(
        Exception ex,
        string? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var message = context is not null 
            ? $"{context}\nException Type: {ex.GetType().FullName}\nMessage: {ex.Message}\nStackTrace:\n{ex.StackTrace}"
            : $"Exception Type: {ex.GetType().FullName}\nMessage: {ex.Message}\nStackTrace:\n{ex.StackTrace}";
        
        if (ex.InnerException is not null)
        {
            message += $"\n--- Inner Exception ---\nType: {ex.InnerException.GetType().FullName}\nMessage: {ex.InnerException.Message}\nStackTrace:\n{ex.InnerException.StackTrace}";
        }
        
        WriteLog("EXCEPTION", message, memberName, sourceFilePath, sourceLineNumber);
    }

    public static void Method(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        WriteLog("METHOD", ">>> Entering method", memberName, sourceFilePath, sourceLineNumber);
    }

    public static void MethodExit(
        string? result = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var message = result is not null ? $"<<< Exiting method (result: {result})" : "<<< Exiting method";
        WriteLog("METHOD", message, memberName, sourceFilePath, sourceLineNumber);
    }

    private static void WriteLog(string level, string message, string memberName, string sourceFilePath, int sourceLineNumber)
    {
        EnsureInitialized();
        
        var fileName = Path.GetFileName(sourceFilePath);
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] [{level,-9}] [{fileName}:{sourceLineNumber}] {memberName}() - {message}";
        
        lock (_lock)
        {
            // Write to Debug output
            System.Diagnostics.Debug.WriteLine(logLine);
            
            // Write to console (visible in Output window)
            Console.WriteLine(logLine);
            
            // Buffer for file
            _logBuffer.AppendLine(logLine);
            
            // Flush to file periodically (every 10 lines or on error/exception)
            if (_logBuffer.Length > 2000 || level is "ERROR" or "EXCEPTION")
            {
                FlushToFile();
            }
        }
    }

    public static void FlushToFile()
    {
        lock (_lock)
        {
            if (_logBuffer.Length > 0 && !string.IsNullOrEmpty(_logFilePath))
            {
                try
                {
                    File.AppendAllText(_logFilePath, _logBuffer.ToString());
                    _logBuffer.Clear();
                }
                catch
                {
                    // Ignore file write errors
                }
            }
        }
    }

    public static string GetLogFilePath() => _logFilePath;
}
