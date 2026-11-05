using Windows.Media.Control;

namespace TestTime;

/// <summary>
/// Uses the Windows "media session" API — the same one behind the volume flyout — to see whether
/// any video/audio is playing (YouTube in Chrome/Edge/Firefox, Netflix, Disney+, VLC, Movies & TV…)
/// and to pause and later resume it.
/// </summary>
sealed class MediaController
{
    GlobalSystemMediaTransportControlsSessionManager? manager;
    readonly List<GlobalSystemMediaTransportControlsSession> pausedByUs = new();

    async Task<GlobalSystemMediaTransportControlsSessionManager?> GetManager()
    {
        if (manager != null) return manager;
        try { manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync(); }
        catch (Exception ex) { Log.Error(ex); }
        return manager;
    }

    static bool IsPlaying(GlobalSystemMediaTransportControlsSession s)
    {
        try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing; }
        catch { return false; }
    }

    static string AppId(GlobalSystemMediaTransportControlsSession s)
    {
        try { return s.SourceAppUserModelId ?? ""; } catch { return ""; }
    }

    public async Task<bool> IsAnythingPlayingAsync(IReadOnlyCollection<string> ignoredApps)
    {
        var m = await GetManager();
        if (m == null) return false;
        try
        {
            foreach (var s in m.GetSessions())
            {
                if (!IsPlaying(s)) continue;
                var id = AppId(s);
                if (ignoredApps.Any(i => i.Length > 0 && id.Contains(i, StringComparison.OrdinalIgnoreCase))) continue;
                return true;
            }
        }
        catch (Exception ex) { Log.Error(ex); }
        return false;
    }

    public async Task PauseAllAsync()
    {
        var m = await GetManager();
        if (m == null) return;
        try
        {
            foreach (var s in m.GetSessions())
            {
                if (!IsPlaying(s)) continue;
                try
                {
                    if (await s.TryPauseAsync()) pausedByUs.Add(s);
                }
                catch (Exception ex) { Log.Error(ex); }
            }
        }
        catch (Exception ex) { Log.Error(ex); }
    }

    public async Task ResumeAsync()
    {
        var list = pausedByUs.ToList();
        pausedByUs.Clear();
        foreach (var s in list)
        {
            try
            {
                if (!IsPlaying(s)) await s.TryPlayAsync();
            }
            catch { /* the tab/app may have been closed meanwhile */ }
        }
    }
}
