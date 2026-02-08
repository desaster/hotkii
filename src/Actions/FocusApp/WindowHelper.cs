using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Hotkii;

//
// Finds windows by process name on the current virtual desktop and brings
// them to the foreground. Consecutive presses cycle through multiple windows.
//
// ForceForeground uses a well-known workaround for Windows foreground lock
// restrictions (simulated Alt keypress + AttachThreadInput):
//   https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow
//   https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows
//

static class WindowHelper
{
    delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern int GetWindowTextLength(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern bool AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    static extern int GetCurrentThreadId();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool BringWindowToTop(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr hwnd);

    const int SW_RESTORE = 9;
    const byte VK_MENU = 0x12;
    const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    const uint KEYEVENTF_KEYUP = 0x0002;

    // Tracks the last focused window per process name, and which process
    // was focused most recently. Only cycle to the next window when the
    // same process hotkey is pressed consecutively.
    static readonly Dictionary<string, IntPtr> lastFocused = new();
    static string? lastProcessName;

    public static void FocusProcess(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0) {
            Log.Write($"No process '{processName}' running");
            return;
        }

        var pids = new HashSet<int>(processes.Select(p => p.Id));
        foreach (var p in processes) {
            p.Dispose();
        }

        var windows = new List<IntPtr>();

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) {
                return true;
            }

            if (GetWindowTextLength(hwnd) == 0) {
                return true;
            }

            GetWindowThreadProcessId(hwnd, out int pid);
            if (!pids.Contains(pid)) {
                return true;
            }

            if (!VirtualDesktopHelper.IsWindowOnCurrentDesktop(hwnd)) {
                return true;
            }

            windows.Add(hwnd);
            return true;
        }, IntPtr.Zero);

        // Sort by handle for a stable order (EnumWindows returns Z-order
        // which shifts every time we bring a window to front)
        windows.Sort((a, b) => a.ToInt64().CompareTo(b.ToInt64()));

        if (windows.Count == 0) {
            Log.Write($"No window for '{processName}' on current desktop");
            return;
        }

        // Only cycle to the next window if the same process hotkey was
        // pressed consecutively. Otherwise return to the last focused window.
        IntPtr target;
        lastFocused.TryGetValue(processName, out var lastHwnd);
        int lastIndex = windows.IndexOf(lastHwnd);

        bool consecutive = lastProcessName == processName;
        if (consecutive && lastIndex >= 0) {
            target = windows[(lastIndex + 1) % windows.Count];
        } else if (lastIndex >= 0) {
            target = lastHwnd;
        } else {
            target = windows[0];
        }

        lastFocused[processName] = target;
        lastProcessName = processName;
        ForceForeground(target);
        Log.Write($"Focusing: {processName} (hwnd: 0x{target:X})"
            + (windows.Count > 1 ? $" [{windows.IndexOf(target) + 1}/{windows.Count}]" : ""));
    }

    static void ForceForeground(IntPtr hwnd)
    {
        if (IsIconic(hwnd)) {
            ShowWindow(hwnd, SW_RESTORE);
        }

        var foregroundHwnd = GetForegroundWindow();
        int foregroundThread = GetWindowThreadProcessId(foregroundHwnd, out _);
        int targetThread = GetWindowThreadProcessId(hwnd, out _);
        int currentThread = GetCurrentThreadId();

        // Simulate an Alt keypress so Windows considers us eligible to
        // change the foreground window
        keybd_event(VK_MENU, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
        keybd_event(VK_MENU, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);

        if (foregroundThread != currentThread) {
            AttachThreadInput(currentThread, foregroundThread, true);
        }

        if (targetThread != currentThread) {
            AttachThreadInput(currentThread, targetThread, true);
        }

        SetForegroundWindow(hwnd);
        BringWindowToTop(hwnd);

        if (foregroundThread != currentThread) {
            AttachThreadInput(currentThread, foregroundThread, false);
        }

        if (targetThread != currentThread) {
            AttachThreadInput(currentThread, targetThread, false);
        }
    }
}
