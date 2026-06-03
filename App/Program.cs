using System;
using System.Threading;
using System.Windows.Forms;
using EcatDesktop.Common;
using EcatDesktop.Forms;
using EcatDesktop.Services;

namespace EcatDesktop
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += p_ApplicationThreadException;
            AppDomain.CurrentDomain.UnhandledException += p_CurrentDomainUnhandledException;

            try
            {
                Log.Startup();
                Log.Information("Creating application services.");

                var oDataStore = new AppDataStore();
                var oRuntime = new AppRuntime(
                    oDataStore,
                    new JobSetupService(oDataStore),
                    new JobProcessingService(oDataStore),
                    new ArchiveService(oDataStore));

                Log.Information("Starting main form.");
                Application.Run(new MainForm(oRuntime));
            }
            catch (Exception oException)
            {
                Error.Fatal(oException, "Program startup failed");
            }
            finally
            {
                Log.Shutdown();
            }
        }

        private static void p_ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Error.Continue(e.Exception, "Unhandled UI exception");
        }

        private static void p_CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception oException = e.ExceptionObject as Exception;
            if (oException != null)
            {
                Error.Fatal(oException, "Unhandled application exception");
            }
            else
            {
                Log.LogError("Unhandled application exception: " + Convert.ToString(e.ExceptionObject));
            }
        }
    }
}
