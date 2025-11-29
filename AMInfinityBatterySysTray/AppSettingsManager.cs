using Microsoft.Win32;

namespace AMInfinityBatterySysTray
{
    public static class AppSettingsManager
    {
        private const string SETTINGS_KEY = @"Software\AMInfinityBatterySysTray";

        public static int? GetInt(string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey(SETTINGS_KEY, writable: false);
            return key?.GetValue(name) as int?;
        }

        public static void SetInt(string name, int value)
        {
            using var key = Registry.CurrentUser.CreateSubKey(SETTINGS_KEY);
            key?.SetValue(name, value, RegistryValueKind.DWord);
        }
    }
}
