using System.Runtime.InteropServices;

namespace Hotkii;

//
// COM interface definitions for Windows 11 virtual desktop API.
// These are undocumented interfaces; GUIDs target Windows 11 22H2+ (build 22621+).
//
// The vtable layout of IVirtualDesktopManagerInternal differs between 22H2/23H2
// and 24H2 (SwitchDesktopAndMoveForegroundView was inserted after SwitchDesktop).
// We only declare methods up to SwitchDesktop (slots 0-6), which are identical
// across both versions.
//
// GUIDs and vtable layouts sourced from:
//   https://github.com/MScholtes/VirtualDesktop
//   https://github.com/Ciantic/VirtualDesktopAccessor
//
// IVirtualDesktopManager is the only officially documented interface:
//   https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ivirtualdesktopmanager
//

internal static class ComGuids
{
    public static readonly Guid CLSID_ImmersiveShell =
        new("C2F03A33-21F5-47FA-B4BB-156362A2F239");
    public static readonly Guid CLSID_VirtualDesktopManagerInternal =
        new("C5E0CDCA-7B6E-41B2-9FC4-D93975CC467B");
    public static readonly Guid CLSID_VirtualDesktopManager =
        new("AA509086-5CA9-4C25-8F95-589D3C07B48A");
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
internal interface IServiceProvider10
{
    [return: MarshalAs(UnmanagedType.IUnknown)]
    object QueryService(ref Guid service, ref Guid riid);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
internal interface IObjectArray
{
    void GetCount(out int count);
    void GetAt(int index, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object obj);
}

// Windows 11 22H2+ (build 22621+)
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("3F07F4BE-B107-441A-AF0F-39D82529072C")]
internal interface IVirtualDesktop
{
    bool IsViewVisible(IntPtr view);
    Guid GetId();
}

// Slots 0-6 are stable across 22H2, 23H2, and 24H2.
// We stop at SwitchDesktop to avoid the vtable divergence.
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("53F5CA0B-158F-4124-900C-057158060B27")]
internal interface IVirtualDesktopManagerInternal
{
    int GetCount();
    void MoveViewToDesktop(IntPtr view, IVirtualDesktop desktop);
    bool CanViewMoveDesktops(IntPtr view);
    IVirtualDesktop GetCurrentDesktop();
    void GetDesktops(out IObjectArray desktops);
    [PreserveSig]
    int GetAdjacentDesktop(IVirtualDesktop from, int direction, out IVirtualDesktop desktop);
    void SwitchDesktop(IVirtualDesktop desktop);
}

// Documented, stable interface
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
internal interface IVirtualDesktopManager
{
    bool IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow);
    Guid GetWindowDesktopId(IntPtr topLevelWindow);
    void MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
}
