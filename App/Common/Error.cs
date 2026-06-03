using System;
using System.Windows.Forms;

namespace EcatDesktop.Common
{
    public static class Error
    {
        public static void Continue(Exception oException, string sContext)
        {
            Log.LogError(oException, sContext);
            MessageBox.Show(sContext + Environment.NewLine + Environment.NewLine + oException.Message,
                "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void Continue(string sContext, string sMessage)
        {
            Log.LogError(sContext + ": " + sMessage);
            MessageBox.Show(sMessage, sContext, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void Warning(string sContext, string sMessage)
        {
            Log.Warning(sContext + ": " + sMessage);
            MessageBox.Show(sMessage, sContext, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void Fatal(Exception oException, string sContext)
        {
            Log.LogError(oException, sContext);
            MessageBox.Show(sContext + Environment.NewLine + Environment.NewLine + oException.Message,
                "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
    }
}
