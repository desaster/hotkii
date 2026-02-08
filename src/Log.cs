namespace Hotkii;

static class Log
{
    const int MaxEntries = 10_000;

    static readonly List<string> buffer = new();
    static event Action<string>? OnMessage;

    public static void Write(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";

        if (buffer.Count >= MaxEntries) {
            buffer.RemoveAt(0);
        }

        buffer.Add(line);
        OnMessage?.Invoke(line);
    }

    public static string GetAll()
    {
        return string.Join(Environment.NewLine, buffer);
    }

    public static void Subscribe(Action<string> handler)
    {
        OnMessage += handler;
    }

    public static void Unsubscribe(Action<string> handler)
    {
        OnMessage -= handler;
    }
}
