using System.Windows.Forms;
using System.Drawing;
using AMInfinityBattery;
using HidSharp;

namespace AMInfinityBatterySysTray
{
    internal class TrayContext : ApplicationContext
    {
        private readonly int[] _batteryThresholds = { 30, 20, 10, 5 };
        private readonly HashSet<int> _notifiedThresholds = [];

        private readonly NotifyIcon _trayIcon;
        private readonly System.Windows.Forms.Timer _timer;

        private StartupManager? _startupManager;

        private int? _lastMouseBatteryThresholdCheck;
        private int? _lastDongleBatteryThresholdCheck;

        private DateTime _lastHover;
        private HidDevice? _device;

        public TrayContext()
        {
            // Create Tray Icon.
            _trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Information,
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = TextFormat(null, null)
            };

            // Add Exit menu item.
            _trayIcon.ContextMenuStrip.Items.Add("Exit", null, (_, __) => ExitThread());

            // Handle mouse hover to update battery status.
            _trayIcon.MouseMove += OnTrayIconMouseMove;

            // Set up timer to update battery status every 10 seconds.
            _timer = new System.Windows.Forms.Timer
            {
                Interval = 10_000 // 10 seconds
            };
            _timer.Tick += async (_, __) => await UpdateBatteryAsync();
            _timer.Start();

            // Allow immediate battery threshold check.
            _lastMouseBatteryThresholdCheck = 100;

            // Initialize Startup Manager on application idle, after SynchronizationContext is ready.
            Application.Idle += InitializeStartupManager;
        }

        protected override void ExitThreadCore()
        {
            _timer.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            base.ExitThreadCore();
        }

        private async void InitializeStartupManager(object? sender, EventArgs e)
        {
            // Unsubscribe from Idle event, we only need to run this once.
            Application.Idle -= InitializeStartupManager;

            // Initial battery status update.
            await UpdateBatteryAsync();

            if (_notifiedThresholds.Count == 0)
                _trayIcon.ShowBalloonTip(3_000, Program.ApplicationName,
                    $"Mouse battery is at {_lastMouseBatteryThresholdCheck?.ToString() ?? "--"}%.\nDongle battery is at {_lastDongleBatteryThresholdCheck?.ToString() ?? "--"}%.",
                    ToolTipIcon.Info);

            // Initialize Startup Manager after initial load.
            _startupManager = new StartupManager();

            // Repair startup entry if needed.
            _startupManager.Repair();

            // Add Start on logon menu item.
            var startupItem = new ToolStripMenuItem("Start on logon")
            {
                Checked = _startupManager.IsEnabled()
            };

            startupItem.Click += (s, e) =>
            {
                bool newState = !startupItem.Checked;
                _startupManager.SetEnabled(newState);
                startupItem.Checked = newState;
            };

            _trayIcon.ContextMenuStrip?.Items.Insert(0, startupItem);
        }

        private async Task UpdateBatteryAsync()
        {
            // Offload HID I/O to a background thread.
            var (mouse, dongle) = await Task.Run(() =>
            {
                if (_device == null)
                    _device = LocalDevice.Get(Constants.VendorId, Constants.ProductId);

                if (_device == null)
                    return (null, null);

                return Reader.GetBattery(_device);
            });

            // Update tray icon text.
            _trayIcon.Text = TextFormat(mouse, dongle);

            // Check for battery threshold notifications.
            CheckBatteryThresholds(mouse, dongle);
        }

        private void CheckBatteryThresholds(int? mouseBattery, int? dongleBattery)
        {
            if (mouseBattery.HasValue)
            {
                // Check if battery level has increased since last check.
                if (mouseBattery >= (_lastMouseBatteryThresholdCheck ?? 100))
                {
                    _notifiedThresholds.Clear();
                }
                else
                {
                    foreach (var threshold in _batteryThresholds)
                    {
                        if (mouseBattery <= threshold && !_notifiedThresholds.Contains(threshold))
                        {
                            // Show notification.
                            if (threshold <= 5)
                            {
                                _trayIcon.ShowBalloonTip(3_000, "Critical Battery Warning",
                                    $"Mouse battery is at {mouseBattery}%.\nDongle battery is at {dongleBattery}%.",
                                    ToolTipIcon.Error);
                            }
                            else
                            {
                                _trayIcon.ShowBalloonTip(3_000, "Low Battery Warning",
                                    $"Mouse battery is at {mouseBattery}%.\nDongle battery is at {dongleBattery}%.",
                                    ToolTipIcon.Warning);
                            }

                            _notifiedThresholds.Add(threshold);
                        }
                    }
                }
            }

            _lastMouseBatteryThresholdCheck = mouseBattery;
            _lastDongleBatteryThresholdCheck = dongleBattery;
        }

        private async void OnTrayIconMouseMove(object? sender, MouseEventArgs e)
        {
            // Throttle updates to once per second on constant mouse hover.
            if ((DateTime.Now - _lastHover).TotalMilliseconds >= 1000)
            {
                // Update battery status on mouse hover.
                await UpdateBatteryAsync();

                _lastHover = DateTime.Now;
            }
        }

        private static string TextFormat(int? battery, int? dongle)
        {
            return $"Angry Miao Infinity Battery\nMouse: {battery?.ToString() ?? "--"}% - Dongle: {dongle?.ToString() ?? "--"}%";
        }
    }
}
