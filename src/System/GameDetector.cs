using System.Diagnostics;

namespace TestTime;

/// <summary>Detects whether a listed game (Roblox by default) is the window the kid is using right now.</summary>
static class GameDetector
{
    static HashSet<string> Names(IEnumerable<string> list) =>
        new(list.Select(n => Path.GetFileNameWithoutExtension(n.Trim())).Where(n => n.Length > 0),
            StringComparer.OrdinalIgnoreCase);

    static string? ProcessName(uint pid)
    {
        try { using var p = Process.GetProcessById((int)pid); return p.ProcessName; }
        catch { return null; }
    }

    /// <summary>True when the foreground window belongs to a game process.</summary>
    public static bool IsGameActive(IEnumerable<string> games)
    {
        var names = Names(games);
        if (names.Count == 0) return false;

        var fg = Native.GetForegroundWindow();
        if (fg == IntPtr.Zero) return false;
        Native.GetWindowThreadProcessId(fg, out uint pid);
        var name = ProcessName(pid);
        if (name == null) return false;
        if (names.Contains(name)) return true;

        // Microsoft Store apps (e.g. Roblox from the Store) live inside ApplicationFrameHost:
        // the real app process owns a child window.
        if (name.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase))
        {
            bool found = false;
            Native.EnumChildWindows(fg, (child, _) =>
            {
                Native.GetWindowThreadProcessId(child, out uint cpid);
                if (cpid != pid && ProcessName(cpid) is string cn && names.Contains(cn))
                {
                    found = true;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }
        return false;
    }

    /// <summary>Minimizes visible game windows so the quiz is guaranteed to be on top. Returns them for restoring.</summary>
    public static List<IntPtr> MinimizeGames(IEnumerable<string> games)
    {
        var result = new List<IntPtr>();
        foreach (var n in Names(games))
        {
            foreach (var p in Process.GetProcessesByName(n))
            {
                try
                {
                    var h = p.MainWindowHandle;
                    if (h != IntPtr.Zero && Native.IsWindowVisible(h) && !Native.IsIconic(h))
                    {
                        Native.ShowWindow(h, Native.SW_MINIMIZE);
                        result.Add(h);
                    }
                }
                catch { }
                finally { p.Dispose(); }
            }
        }
        return result;
    }

    public static void Restore(IEnumerable<IntPtr> windows)
    {
        foreach (var h in windows)
        {
            try { Native.ShowWindow(h, Native.SW_RESTORE); } catch { }
        }
    }
}
