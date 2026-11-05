using System.Runtime.InteropServices;

namespace TestTime;

/// <summary>
/// Mutes every app's sound (Roblox, browsers…) on the default speakers while the quiz is shown,
/// then un-mutes exactly the apps we muted. Uses the Windows Core Audio session API.
/// </summary>
static class AudioMuter
{
    static readonly List<ISimpleAudioVolume> muted = new();

    public static void MuteOthers()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
            enumerator.GetDefaultAudioEndpoint(0 /*eRender*/, 1 /*eMultimedia*/, out var device);
            var iid = typeof(IAudioSessionManager2).GUID;
            device.Activate(ref iid, 23 /*CLSCTX_ALL*/, IntPtr.Zero, out var obj);
            var mgr = (IAudioSessionManager2)obj;
            mgr.GetSessionEnumerator(out var sessions);
            sessions.GetCount(out int count);
            uint self = (uint)Environment.ProcessId;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    sessions.GetSession(i, out var ctl);
                    ctl.GetProcessId(out uint pid);
                    if (pid == 0 || pid == self) continue;      // system sounds / ourselves
                    var vol = (ISimpleAudioVolume)ctl;
                    vol.GetMute(out bool isMuted);
                    if (isMuted) continue;                       // already muted (by user or by us)
                    var ctx = Guid.Empty;
                    vol.SetMute(true, ref ctx);
                    muted.Add(vol);
                }
                catch (Exception ex) { Log.Error(ex); }
            }
        }
        catch (Exception ex) { Log.Error(ex); }
    }

    public static void Restore()
    {
        foreach (var vol in muted)
        {
            try { var ctx = Guid.Empty; vol.SetMute(false, ref ctx); }
            catch { /* app closed */ }
        }
        muted.Clear();
    }

    // ---- COM interop declarations (method order must match the Windows headers) ----

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumeratorCom { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        void GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        void Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
    }

    [ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionManager2
    {
        [PreserveSig] int GetAudioSessionControl(IntPtr sessionGuid, int flags, out IntPtr control);
        [PreserveSig] int GetSimpleAudioVolume(IntPtr sessionGuid, int flags, out IntPtr volume);
        void GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);
    }

    [ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionEnumerator
    {
        void GetCount(out int count);
        void GetSession(int index, out IAudioSessionControl2 session);
    }

    [ComImport, Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionControl2
    {
        // IAudioSessionControl
        [PreserveSig] int GetState(out int state);
        [PreserveSig] int GetDisplayName(out IntPtr name);
        [PreserveSig] int SetDisplayName(IntPtr name, IntPtr ctx);
        [PreserveSig] int GetIconPath(out IntPtr path);
        [PreserveSig] int SetIconPath(IntPtr path, IntPtr ctx);
        [PreserveSig] int GetGroupingParam(out Guid param);
        [PreserveSig] int SetGroupingParam(IntPtr param, IntPtr ctx);
        [PreserveSig] int RegisterAudioSessionNotification(IntPtr client);
        [PreserveSig] int UnregisterAudioSessionNotification(IntPtr client);
        // IAudioSessionControl2
        [PreserveSig] int GetSessionIdentifier(out IntPtr id);
        [PreserveSig] int GetSessionInstanceIdentifier(out IntPtr id);
        [PreserveSig] int GetProcessId(out uint pid);
    }

    [ComImport, Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISimpleAudioVolume
    {
        void SetMasterVolume(float level, ref Guid ctx);
        void GetMasterVolume(out float level);
        void SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid ctx);
        void GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }
}
