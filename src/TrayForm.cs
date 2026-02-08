using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Hotkii;

class TrayForm : Form
{
    private NotifyIcon trayIcon;
    private HotkeyManager hotkeyManager;
    private LogForm logForm;

    public TrayForm()
    {
        hotkeyManager = new HotkeyManager();
        logForm = new LogForm();

        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        Visible = false;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add($"Hotkii {BuildVersion.Git}").Enabled = false;
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Debug Log", null, OnShowLog);
        contextMenu.Items.Add("Exit", null, OnExit);

        trayIcon = new NotifyIcon
        {
            Text = "Hotkii",
            Icon = LoadIcon(),
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        RegisterActions();
        AudioHelper.LogAvailableDevices();
        LoadConfig();
    }

    private void RegisterActions()
    {
        ActionRegistry.Register("switch-desktop", hotkey =>
        {
            int desktop = hotkey.GetIntArg("desktop");
            VirtualDesktopHelper.SwitchToDesktop(desktop - 1);
        });

        ActionRegistry.Register("audio-set-device", hotkey =>
        {
            string device = hotkey.GetStringArg("device");
            AudioHelper.SwitchToDevice(device);
        });

        ActionRegistry.Register("audio-cycle-devices", hotkey =>
        {
            var devices = hotkey.GetStringListArg("devices");
            AudioHelper.CycleDevices(devices);
        });

        ActionRegistry.Register("focus-app", hotkey =>
        {
            string process = hotkey.GetStringArg("process");
            WindowHelper.FocusProcess(process);
        });
    }

    private void LoadConfig()
    {
        var exeDir = AppContext.BaseDirectory;
        var configPath = Path.Combine(exeDir, "config.json");

        AppConfig config;
        try {
            config = ConfigLoader.Load(configPath);
        } catch (Exception ex) {
            Log.Write($"Error loading config: {ex.Message}");
            return;
        }

        Log.Write($"Config: {configPath}");
        Log.Write($"Loading {config.Hotkeys.Count} hotkey(s)...");

        int registered = 0, failed = 0;
        foreach (var hotkey in config.Hotkeys) {
            try {
                var key = hotkey.ParseKey();
                var modifiers = hotkey.ParseModifiers();
                var action = ActionRegistry.Resolve(hotkey);

                if (action == null) {
                    failed++;
                    continue;
                }

                if (hotkeyManager.Register(key, modifiers, action)) {
                    registered++;
                } else {
                    failed++;
                }
            } catch (ConfigException ex) {
                Log.Write($"  Config error: {ex.Message}");
                failed++;
            }
        }

        Log.Write($"Ready ({registered} hotkeys active"
            + (failed > 0 ? $", {failed} failed" : "") + ")");
    }

    private void OnShowLog(object? sender, EventArgs e)
    {
        logForm.Show();
        logForm.BringToFront();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        Application.Exit();
    }

    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(false);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        hotkeyManager.Dispose();
        trayIcon.Visible = false;
        trayIcon.Dispose();
        base.OnFormClosed(e);
    }

    private static Icon LoadIcon()
    {
        var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Hotkii.icon.ico");
        return new Icon(stream!);
    }
}
