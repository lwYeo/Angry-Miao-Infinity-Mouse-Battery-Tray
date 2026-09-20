using System.Reflection;
using System.Windows.Forms;

namespace AMInfinityBatterySysTray
{
    internal static class Program
    {
        public static readonly string ApplicationName = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "AMInfinityBatterySysTray";

        [STAThread]
        static void Main()
        {
            using var mutex = new Mutex(false, ApplicationName);

            if (mutex.WaitOne(0, false))
            {
                try
                {
                    ApplicationConfiguration.Initialize();
                    Application.Run(new TrayContext());
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}