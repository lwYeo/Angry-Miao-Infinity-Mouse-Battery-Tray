using System.Reflection;
using System.Windows.Forms;

namespace AMInfinityBatterySysTray
{
    internal static class Program
    {
        public static readonly string ApplicationName = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "AMInfinityBatterySysTray";

        private static readonly Mutex Mutex = new(false, ApplicationName);

        [STAThread]
        static void Main()
        {
            if (Mutex.WaitOne(0, false))
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new TrayContext());
            }
        }
    }
}