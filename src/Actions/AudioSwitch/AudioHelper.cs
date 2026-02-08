using System.Runtime.InteropServices;

namespace Hotkii;

record AudioDevice(string Id, string Name);

static class AudioHelper
{
    const int STGM_READ = 0;
    const int DEVICE_STATE_ACTIVE = 1;

    private static IMMDeviceEnumerator? enumerator;
    private static bool initialized;
    private static string? initError;

    static void EnsureInitialized()
    {
        if (initialized) {
            return;
        }

        initialized = true;

        try {
            var type = Type.GetTypeFromCLSID(AudioGuids.CLSID_MMDeviceEnumerator, true)!;
            enumerator = (IMMDeviceEnumerator) Activator.CreateInstance(type)!;
        } catch (Exception ex) {
            initError = ex.Message;
            Console.WriteLine($"Failed to initialize audio API: {ex.Message}");
        }
    }

    public static List<AudioDevice> GetOutputDevices()
    {
        EnsureInitialized();
        var devices = new List<AudioDevice>();
        if (enumerator == null) {
            return devices;
        }

        enumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE_ACTIVE,
            out var collection);
        collection.GetCount(out uint count);

        for (uint i = 0; i < count; i++) {
            collection.Item(i, out var device);
            devices.Add(ReadDevice(device));
            Marshal.ReleaseComObject(device);
        }

        Marshal.ReleaseComObject(collection);
        return devices;
    }

    public static AudioDevice? GetDefaultDevice()
    {
        EnsureInitialized();
        if (enumerator == null) {
            return null;
        }

        int hr = enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole,
            out var device);
        if (hr != 0) {
            return null;
        }

        var result = ReadDevice(device);
        Marshal.ReleaseComObject(device);
        return result;
    }

    public static AudioDevice? FindDevice(string partialName)
    {
        var devices = GetOutputDevices();
        return devices.Find(d =>
            d.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase));
    }

    public static void SetDefaultDevice(string deviceId)
    {
        var policyConfig = (IPolicyConfig) new CPolicyConfigClient();
        try {
            // Set for all three roles to match Windows Sound Settings behavior
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);
        } finally {
            Marshal.ReleaseComObject(policyConfig);
        }
    }

    public static void SwitchToDevice(string partialName)
    {
        var device = FindDevice(partialName);
        if (device == null) {
            Console.WriteLine($"Audio device not found: '{partialName}'");
            return;
        }

        SetDefaultDevice(device.Id);
        Console.WriteLine($"Switched audio to: {device.Name}");
    }

    public static void CycleDevices(List<string> deviceNames)
    {
        var current = GetDefaultDevice();

        var candidates = new List<AudioDevice>();
        foreach (var name in deviceNames) {
            var device = FindDevice(name);
            if (device != null) {
                candidates.Add(device);
            }
        }

        if (candidates.Count == 0) {
            Console.WriteLine("No matching audio devices available");
            return;
        }

        int currentIndex = -1;
        if (current != null) {
            currentIndex = candidates.FindIndex(d => d.Id == current.Id);
        }

        int nextIndex = (currentIndex + 1) % candidates.Count;
        var next = candidates[nextIndex];

        SetDefaultDevice(next.Id);
        Console.WriteLine($"Switched audio to: {next.Name}");
    }

    public static void LogAvailableDevices()
    {
        var devices = GetOutputDevices();
        var current = GetDefaultDevice();
        Console.WriteLine($"Audio output devices ({devices.Count}):");
        foreach (var d in devices) {
            var marker = d.Id == current?.Id ? " (default)" : "";
            Console.WriteLine($"  {d.Name}{marker}");
        }
    }

    static AudioDevice ReadDevice(IMMDevice device)
    {
        device.GetId(out string id);
        device.OpenPropertyStore(STGM_READ, out var store);

        var key = AudioGuids.PKEY_Device_FriendlyName;
        store.GetValue(ref key, out var pv);
        string name = Marshal.PtrToStringUni(pv.pwszVal) ?? "(unknown)";
        if (pv.pwszVal != IntPtr.Zero) {
            Marshal.FreeCoTaskMem(pv.pwszVal);
        }

        Marshal.ReleaseComObject(store);
        return new AudioDevice(id, name);
    }
}
