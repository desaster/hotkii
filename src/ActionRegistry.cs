namespace Hotkii;

static class ActionRegistry
{
    private static readonly Dictionary<string, Action<HotkeyConfig>> handlers = new();

    public static void Register(string actionName, Action<HotkeyConfig> handler)
    {
        handlers[actionName] = handler;
    }

    public static Action? Resolve(HotkeyConfig hotkey)
    {
        if (handlers.TryGetValue(hotkey.Action, out var handler)) {
            return () =>
            {
                try {
                    handler(hotkey);
                } catch (Exception ex) {
                    Console.WriteLine($"Error in action '{hotkey.Action}': {ex.Message}");
                }
            };
        }

        Console.WriteLine($"  Unknown action: '{hotkey.Action}'");
        return null;
    }
}
