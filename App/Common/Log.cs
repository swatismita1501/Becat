using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace EcatDesktop.Common
{
    public static class Log
    {
        private static readonly object omLogLock = new object();
        private static string smLogFilePath;
        private static bool bmStarted;

        public static string LogFilePath
        {
            get { return smLogFilePath; }
        }

        public static void Startup()
        {
            try
            {
                p_EnsureStarted();
                Information("Application startup.");
            }
            catch
            {
                // Logging must never stop application startup.
            }
        }

        public static void Shutdown()
        {
            try
            {
                Information("Application shutdown.");
            }
            catch
            {
                // Logging must never throw during shutdown.
            }
        }

        public static void Information(string sMessage)
        {
            p_Write("INFO", sMessage, null);
        }

        public static void Warning(string sMessage)
        {
            p_Write("WARN", sMessage, null);
        }

        public static void LogError(string sMessage)
        {
            p_Write("ERROR", sMessage, null);
        }

        public static void LogError(Exception oException, string sMessage)
        {
            p_Write("ERROR", sMessage, oException);
        }

        private static void p_EnsureStarted()
        {
            if (bmStarted)
            {
                return;
            }

            lock (omLogLock)
            {
                if (bmStarted)
                {
                    return;
                }

                string sLogFolder = Path.Combine(Application.StartupPath, "Logs");
                Directory.CreateDirectory(sLogFolder);
                smLogFilePath = Path.Combine(sLogFolder, "EcatDesktop-" + DateTime.Today.ToString("yyyyMMdd") + ".log");
                bmStarted = true;
            }
        }

        private static void p_Write(string sLevel, string sMessage, Exception oException)
        {
            try
            {
                p_EnsureStarted();
                StringBuilder oLine = new StringBuilder();
                oLine.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                oLine.Append(" [");
                oLine.Append(sLevel);
                oLine.Append("] ");
                oLine.Append(sMessage ?? string.Empty);
                if (oException != null)
                {
                    oLine.Append(Environment.NewLine);
                    oLine.Append(oException.ToString());
                }

                lock (omLogLock)
                {
                    File.AppendAllText(smLogFilePath, oLine.ToString() + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Logging is intentionally fail-safe.
            }
        }
    }
}
