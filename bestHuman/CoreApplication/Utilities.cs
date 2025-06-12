using System;
using System.IO;

namespace CoreApplication
{
    public static class Logger
    {
        private static string _logFilePath = "app.log";

        public static void Initialize(string logFilePath = "app.log")
        {
            _logFilePath = logFilePath;
            // 启动时清空或创建日志文件
            try
            {
                File.WriteAllText(_logFilePath, $"Log initialized at {DateTime.Now}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing log file: {ex.Message}");
            }
        }

        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARNING", message);
        }

        public static void LogError(string message, Exception? ex = null)
        {
            string logMessage = $"ERROR: {message}";
            if (ex != null)
            {
                logMessage += $"{Environment.NewLine}Exception: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
            }
            WriteLog("ERROR", logMessage);
        }

        private static void WriteLog(string level, string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                Console.WriteLine(logEntry); // 同时输出到控制台
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
                Console.WriteLine(logEntry); // 确保至少在控制台输出
            }
        }
    }
}