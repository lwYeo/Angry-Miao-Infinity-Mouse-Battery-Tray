using Microsoft.Win32;
using System.Windows.Forms;

namespace AMInfinityBatterySysTray
{
    public class StartupManager
    {
        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private readonly string _exePath;

        public StartupManager()
        {
            _exePath = "\"" + Application.ExecutablePath + "\"";
        }

        /// <summary>
        /// Returns true if the startup entry exists.
        /// </summary>
        public bool IsEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, writable: false))
            {
                if (key == null) return false;

                return key.GetValue(Program.ApplicationName) != null;
            }
        }

        /// <summary>
        /// Enables or disables the Run-at-Startup entry.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, writable: true);

            if (key == null) return;

            if (enabled)
                key.SetValue(Program.ApplicationName, _exePath);
            else
                key.DeleteValue(Program.ApplicationName, false);
        }

        /// <summary>
        /// Repairs the entry if the stored path is wrong,
        /// or if the file no longer exists.
        /// Safe to call on every application start.
        /// </summary>
        public void Repair()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, writable: true))
            {
                if (key == null) return;

                if (key.GetValue(Program.ApplicationName) is not string stored)
                    return;

                string unquoted = stored.Trim('"');

                bool pathValid = File.Exists(unquoted);
                bool pathMatches = String.Equals(stored, _exePath, StringComparison.OrdinalIgnoreCase);

                if (!pathValid || !pathMatches)
                    key.SetValue(Program.ApplicationName, _exePath);
            }
        }
    }
}