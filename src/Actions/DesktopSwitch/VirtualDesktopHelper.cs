using System.Runtime.InteropServices;

namespace Hotkii;

static class VirtualDesktopHelper
{
    private static IVirtualDesktopManagerInternal? managerInternal;
    private static IVirtualDesktopManager? manager;
    private static bool initialized;
    private static string? initError;

    static void EnsureInitialized()
    {
        if (initialized) {
            return;
        }

        initialized = true;

        try {
            var shellType = Type.GetTypeFromCLSID(ComGuids.CLSID_ImmersiveShell)!;
            var shell = (IServiceProvider10) Activator.CreateInstance(shellType)!;

            var serviceClsid = ComGuids.CLSID_VirtualDesktopManagerInternal;
            var mgrInternalGuid = typeof(IVirtualDesktopManagerInternal).GUID;
            managerInternal = (IVirtualDesktopManagerInternal) shell.QueryService(
                ref serviceClsid, ref mgrInternalGuid);

            var mgrType = Type.GetTypeFromCLSID(ComGuids.CLSID_VirtualDesktopManager)!;
            manager = (IVirtualDesktopManager) Activator.CreateInstance(mgrType)!;

            Log.Write($"Virtual desktop API initialized ({GetDesktopCount()} desktops)");
        } catch (Exception ex) {
            initError = ex.Message;
            Log.Write($"Failed to initialize virtual desktop API: {ex.Message}");
        }
    }

    public static int GetDesktopCount()
    {
        EnsureInitialized();
        if (managerInternal == null) {
            return 0;
        }

        return managerInternal.GetCount();
    }

    public static void SwitchToDesktop(int index)
    {
        EnsureInitialized();
        if (managerInternal == null) {
            Log.Write($"Cannot switch desktop: {initError}");
            return;
        }

        int count = managerInternal.GetCount();
        if (index < 0 || index >= count) {
            Log.Write($"Desktop {index + 1} does not exist (have {count})");
            return;
        }

        managerInternal.GetDesktops(out var desktops);
        var iid = typeof(IVirtualDesktop).GUID;
        desktops.GetAt(index, ref iid, out var obj);
        var desktop = (IVirtualDesktop) obj;

        managerInternal.SwitchDesktop(desktop);
        Log.Write($"Switched to desktop {index + 1}");

        Marshal.ReleaseComObject(desktop);
        Marshal.ReleaseComObject(desktops);
    }

    public static bool IsWindowOnCurrentDesktop(IntPtr hwnd)
    {
        EnsureInitialized();
        if (manager == null) {
            return false;
        }

        try {
            return manager.IsWindowOnCurrentVirtualDesktop(hwnd);
        } catch {
            return false;
        }
    }
}
