using System.Runtime.InteropServices;

namespace TestTime;

/// <summary>
/// While the quiz is on screen, swallow the keys a kid would use to escape it:
/// Windows key, Alt+Tab, Alt+Esc, Alt+F4, Ctrl+Esc, Ctrl+Shift+Esc and media keys.
/// (Ctrl+Alt+Del cannot be blocked by any normal app.)
/// </summary>
sealed class KeyboardBlocker : IDisposable
{
    const int WH_KEYBOARD_LL = 13;
    const uint LLKHF_ALTDOWN = 0x20;
    const int VK_TAB = 0x09, VK_ESCAPE = 0x1B, VK_F4 = 0x73, VK_LWIN = 0x5B, VK_RWIN = 0x5C, VK_CONTROL = 0x11;

    readonly Native.LowLevelKeyboardProc proc;   // keep a reference so the GC does not collect the delegate
    IntPtr hook;

    public KeyboardBlocker()
    {
        proc = HookProc;
        hook = Native.SetWindowsHookEx(WH_KEYBOARD_LL, proc, Native.GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero) Log.Error($"Keyboard hook failed: {Marshal.GetLastWin32Error()}");
    }

    IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var k = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam);
            int vk = (int)k.vkCode;
            bool alt = (k.flags & LLKHF_ALTDOWN) != 0;
            bool ctrl = (Native.GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;

            bool block =
                vk is VK_LWIN or VK_RWIN ||
                (alt && vk is VK_TAB or VK_ESCAPE or VK_F4) ||
                (ctrl && vk == VK_ESCAPE) ||
                vk is >= 0xB0 and <= 0xB3;          // next / previous / stop / play-pause

            if (block) return (IntPtr)1;
        }
        return Native.CallNextHookEx(hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (hook != IntPtr.Zero)
        {
            Native.UnhookWindowsHookEx(hook);
            hook = IntPtr.Zero;
        }
    }
}
