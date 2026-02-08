using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace Hotkii;

class HotkeyConfig
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = new();

    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("args")]
    public JsonElement? Args { get; set; }

    // Key names match System.Windows.Forms.Keys enum values:
    // https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys
    public Keys ParseKey()
    {
        if (Enum.TryParse<Keys>(Key, ignoreCase: true, out var result)) {
            return result;
        }

        throw new ConfigException($"Unknown key: '{Key}'");
    }

    public HotkeyModifiers ParseModifiers()
    {
        var mods = HotkeyModifiers.None;
        foreach (var mod in Modifiers) {
            mods |= mod.ToLowerInvariant() switch
            {
                "ctrl" or "control" => HotkeyModifiers.Ctrl,
                "alt" => HotkeyModifiers.Alt,
                "shift" => HotkeyModifiers.Shift,
                "win" or "windows" => HotkeyModifiers.Win,
                _ => throw new ConfigException($"Unknown modifier: '{mod}'")
            };
        }
        return mods;
    }

    public string GetStringArg(string name)
    {
        if (Args is JsonElement el && el.TryGetProperty(name, out var val)) {
            return val.GetString() ?? "";
        }

        throw new ConfigException($"Action '{Action}' requires '{name}' argument");
    }

    public int GetIntArg(string name)
    {
        if (Args is JsonElement el && el.TryGetProperty(name, out var val)) {
            return val.GetInt32();
        }

        throw new ConfigException($"Action '{Action}' requires '{name}' argument");
    }

    public List<string> GetStringListArg(string name)
    {
        if (Args is JsonElement el && el.TryGetProperty(name, out var val)
            && val.ValueKind == JsonValueKind.Array) {
            return val.EnumerateArray()
                .Select(v => v.GetString() ?? "")
                .ToList();
        }
        throw new ConfigException($"Action '{Action}' requires '{name}' array argument");
    }
}

class AppConfig
{
    [JsonPropertyName("hotkeys")]
    public List<HotkeyConfig> Hotkeys { get; set; } = new();
}

class ConfigException : Exception
{
    public ConfigException(string message) : base(message) { }
}

static class ConfigLoader
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path)) {
            Log.Write($"Config not found, creating default: {path}");
            WriteDefault(path);
        }

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
        return config ?? new AppConfig();
    }

    static void WriteDefault(string path)
    {
        var defaultConfig = new AppConfig
        {
            Hotkeys = new List<HotkeyConfig>
            {
                MakeHotkey("F13", [], "switch-desktop", new { desktop = 1 }),
                MakeHotkey("F14", [], "switch-desktop", new { desktop = 2 }),
                MakeHotkey("F15", [], "switch-desktop", new { desktop = 3 }),
                MakeHotkey("F16", [], "switch-desktop", new { desktop = 4 }),

                MakeHotkey("D1", ["Ctrl"], "switch-desktop", new { desktop = 1 }),
                MakeHotkey("D2", ["Ctrl"], "switch-desktop", new { desktop = 2 }),
                MakeHotkey("D3", ["Ctrl"], "switch-desktop", new { desktop = 3 }),
                MakeHotkey("D4", ["Ctrl"], "switch-desktop", new { desktop = 4 }),

                MakeHotkey("A", ["Ctrl", "Alt"], "audio-cycle-devices",
                    new { devices = new[] { "Headphones", "Speakers" } }),

                MakeHotkey("B", ["Ctrl", "Alt"], "focus-app", new { process = "firefox" }),
                MakeHotkey("T", ["Ctrl", "Alt"], "focus-app", new { process = "WindowsTerminal" }),
            }
        };

        var json = JsonSerializer.Serialize(defaultConfig, JsonOptions);
        File.WriteAllText(path, json);
    }

    static HotkeyConfig MakeHotkey(string key, List<string> modifiers, string action, object args)
    {
        var argsElement = JsonSerializer.SerializeToElement(args);
        return new HotkeyConfig
        {
            Key = key,
            Modifiers = modifiers,
            Action = action,
            Args = argsElement
        };
    }
}
