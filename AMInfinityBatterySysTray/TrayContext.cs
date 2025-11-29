using AMInfinityBattery;
using HidSharp;
using System.Drawing;
using System.Windows.Forms;

namespace AMInfinityBatterySysTray
{
    internal class TrayContext : ApplicationContext
    {
        private readonly int[] _batteryThresholds = { 30, 20, 10, 5 };
        private readonly HashSet<int> _notifiedThresholds = [];

        private readonly NotifyIcon _trayIcon;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly SemaphoreSlim _updateLock = new(1, 1);

        private (bool Popup30, bool Popup20, bool Popup10) _popupSettings;

        private int? _lastMouseBatteryCheck;
        private int? _lastDongleBatteryCheck;
        private ToolTipIcon? _LastPopupIcon;

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
            _trayIcon.MouseMove += (s, e) => _ = OnTrayIconMouseMove(s, e);
            _trayIcon.DoubleClick += (_, __)
                => ShowBatteryPopup(Program.ApplicationName, _LastPopupIcon ?? ToolTipIcon.Info, _lastMouseBatteryCheck, _lastDongleBatteryCheck);

            // Set up timer to update battery status every 10 seconds.
            _timer = new System.Windows.Forms.Timer
            {
                Interval = 10_000 // 10 seconds
            };
            _timer.Tick += async (_, __) => await UpdateBatteryAsync();
            _timer.Start();

            // Allow immediate battery threshold check.
            _lastMouseBatteryCheck = 100;

            // Initialize Startup Manager on application idle, after SynchronizationContext is ready.
            Application.Idle += OnFirstIdle;
        }

        protected override void ExitThreadCore()
        {
            _timer.Stop();
            _timer.Dispose();
            _updateLock.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            base.ExitThreadCore();
        }

        private void OnFirstIdle(object? sender, EventArgs e)
        {
            // Unsubscribe from Idle event, we only need to run this once.
            Application.Idle -= OnFirstIdle;
            _ = InitializeStartupManager(sender, e);
        }

        private async Task InitializeStartupManager(object? sender, EventArgs e)
        {
            try
            {
                // Initialize buttons after initial load.
                await InitializeMenu_CreateStartupButton(0);
                await InitializeMenu_CreateBatteryPopupButton(1, "Popup30", "30% popup");
                await InitializeMenu_CreateBatteryPopupButton(2, "Popup20", "20% popup");
                await InitializeMenu_CreateBatteryPopupButton(3, "Popup10", "10% popup");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize application: {ex.Message}", Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExitThread();
            }
            finally
            {
                // Initial battery status update.
                await UpdateBatteryAsync();

                // Show initial battery status popup if no thresholds have been notified yet.
                if (_notifiedThresholds.Count == 0)
                    ShowBatteryPopup(Program.ApplicationName, ToolTipIcon.Info, _lastMouseBatteryCheck, _lastDongleBatteryCheck);
            }
        }

        private async Task InitializeMenu_CreateStartupButton(int buttonIndex)
        {
            // Repair startup entry if needed.
            await Task.Run(StartupManager.Repair);

            // Add Start on logon menu item.
            var item = new ToolStripMenuItem("Start on logon")
            {
                Checked = StartupManager.IsEnabled()
            };

            item.Click += (s, e) =>
            {
                bool newState = !item.Checked;
                StartupManager.SetEnabled(newState);
                item.Checked = newState;
            };

            _trayIcon.ContextMenuStrip?.Items.Insert(buttonIndex, item);
        }

        private async Task InitializeMenu_CreateBatteryPopupButton(int buttonIndex, string fieldName, string label)
        {
            bool result = false;

            await Task.Run(() =>
            {
                // Load initial popup settings from registry.
                var resultInt = AppSettingsManager.GetInt(fieldName) ?? 1;
                result = resultInt != 0;
            });

            UpdateBatteryPopupSettings(fieldName, result);

            var item = new ToolStripMenuItem(label)
            {
                Checked = result
            };

            item.Click += (s, e) =>
            {
                bool newState = !item.Checked;
                AppSettingsManager.SetInt(fieldName, newState ? 1 : 0);
                UpdateBatteryPopupSettings(fieldName, newState);
                item.Checked = newState;
            };

            _trayIcon.ContextMenuStrip?.Items.Insert(buttonIndex, item);
        }

        private async Task UpdateBatteryAsync()
        {
            if (!await _updateLock.WaitAsync(0)) return;
            try
            {
                // Offload HID I/O to a background thread.
                var (mouse, dongle) = await Task.Run(() =>
                {
                    try
                    {
                        _device ??= LocalDevice.Get(Constants.VendorId, Constants.ProductId);

                        return _device == null
                            ? ((int? Mouse, int? Dongle))(null, null)
                            : Reader.GetBattery(_device);
                    }
                    catch
                    {
                        // On any error, reset device reference for next attempt.
                        _device = null;
                        return ((int? Mouse, int? Dongle))(null, null);
                    }
                });
                
                // Update tray icon text.
                _trayIcon.Text = TextFormat(mouse, dongle);

                // Check for battery threshold notifications.
                CheckBatteryThresholds(mouse, dongle);
            }
            finally { _updateLock.Release(); }
        }

        private void CheckBatteryThresholds(int? mouseBattery, int? dongleBattery)
        {
            if (mouseBattery.HasValue)
            {
                // Check if battery level has increased since last check.
                if (mouseBattery > (_lastMouseBatteryCheck ?? 100))
                {
                    _notifiedThresholds.Clear();
                    _trayIcon.Icon = SystemIcons.Information;
                }
                else
                {
                    var isNotifiedLowBattery = false;

                    foreach (var threshold in _batteryThresholds)
                    {
                        if (mouseBattery <= threshold && !_notifiedThresholds.Contains(threshold))
                        {
                            // Show notification.
                            if (threshold == 5)
                            {
                                _trayIcon.Icon = SystemIcons.Error;
                                _notifiedThresholds.Add(threshold);

                                ShowBatteryPopup("Critical Battery Warning", ToolTipIcon.Error, mouseBattery, dongleBattery);
                                break;
                            }
                            else
                            {
                                _trayIcon.Icon = SystemIcons.Warning;

                                // Continue scanning thresholds if popup disabled.
                                if (threshold == 10 && !_popupSettings.Popup10)
                                    continue;
                                else if (threshold == 20 && !_popupSettings.Popup20)
                                    continue;
                                else if (threshold == 30 && !_popupSettings.Popup30)
                                    continue;

                                _notifiedThresholds.Add(threshold);

                                if (!isNotifiedLowBattery) // Only show one low battery popup at a time.
                                {
                                    isNotifiedLowBattery = true;
                                    ShowBatteryPopup("Low Battery Warning", ToolTipIcon.Warning, mouseBattery, dongleBattery);
                                }
                            }
                        }
                    }
                }
            }

            _lastMouseBatteryCheck = mouseBattery;
            _lastDongleBatteryCheck = dongleBattery;
        }

        private async Task OnTrayIconMouseMove(object? sender, MouseEventArgs e)
        {
            try
            {
                // Throttle updates to once per second on constant mouse hover.
                if ((DateTime.Now - _lastHover).TotalMilliseconds >= 1000)
                {
                    // Update battery status on mouse hover.
                    await UpdateBatteryAsync();

                    _lastHover = DateTime.Now;
                }
            }
            catch { } // Ignore exceptions from hover updates.
        }

        private static string TextFormat(int? battery, int? dongle)
        {
            return $"Angry Miao Infinity Battery\r\nMouse: {battery?.ToString() ?? "--"}% - Dongle: {dongle?.ToString() ?? "--"}%";
        }

        private void ShowBatteryPopup(string title, ToolTipIcon icon, int? mouseBattery, int? dongleBattery, int duration = 5_000)
        {
            string message = $"Mouse battery is at {mouseBattery?.ToString() ?? "--"}%.\r\nDongle battery is at {dongleBattery?.ToString() ?? "--"}%.";
            _trayIcon.ShowBalloonTip(duration, title, message, icon);
            _LastPopupIcon = icon;
        }

        private void UpdateBatteryPopupSettings(string fieldName, bool result)
        {
            switch (fieldName)
            {
                case "Popup30":
                    _popupSettings.Popup30 = result;
                    break;
                case "Popup20":
                    _popupSettings.Popup20 = result;
                    break;
                case "Popup10":
                    _popupSettings.Popup10 = result;
                    break;
            }
        }
    }
}
