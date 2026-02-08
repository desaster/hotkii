using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Hotkii;

[Flags]
enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Ctrl = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
    NoRepeat = 0x4000
}

//
// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey
//
// Some Win+key and Win+Alt+key combinations are reserved by Windows and will
// fail to register. For a full list of built-in Windows shortcuts, see:
//   https://support.microsoft.com/en-us/windows/keyboard-shortcuts-in-windows-dcc61a57-8ff0-cffe-9796-cb9706c75eec
//

class HotkeyManager : IMessageFilter, IDisposable
{
    const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly Dictionary<int, Action> callbacks = new();
    private int nextId = 1;

    public HotkeyManager()
    {
        Application.AddMessageFilter(this);
    }

    public bool Register(Keys key, HotkeyModifiers modifiers, Action callback)
    {
        int id = nextId++;
        uint mods = (uint) modifiers | (uint) HotkeyModifiers.NoRepeat;

        if (!RegisterHotKey(IntPtr.Zero, id, mods, (uint) key)) {
            Log.Write($"  Failed to register hotkey: {modifiers}+{key}");
            return false;
        }

        callbacks[id] = callback;
        Log.Write($"  Registered hotkey: {FormatHotkey(key, modifiers)}");
        return true;
    }

    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg != WM_HOTKEY) {
            return false;
        }

        int id = m.WParam.ToInt32();
        if (callbacks.TryGetValue(id, out var callback)) {
            callback();
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        Application.RemoveMessageFilter(this);
        foreach (int id in callbacks.Keys) {
            UnregisterHotKey(IntPtr.Zero, id);
        }

        callbacks.Clear();
    }

    static string FormatHotkey(Keys key, HotkeyModifiers modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(HotkeyModifiers.Win)) {
            parts.Add("Win");
        }

        if (modifiers.HasFlag(HotkeyModifiers.Ctrl)) {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(HotkeyModifiers.Alt)) {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(HotkeyModifiers.Shift)) {
            parts.Add("Shift");
        }

        parts.Add(key.ToString());
        return string.Join("+", parts);
    }
}
