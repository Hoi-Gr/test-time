namespace TestTime;

/// <summary>
/// Lives in the system tray. Every 5 seconds it checks whether a video is playing or a game is in
/// front; that time is added up, and when it reaches the interval (15 min) the quiz appears.
/// </summary>
sealed class TrayApp : ApplicationContext
{
    readonly AppSettings s;
    readonly NotifyIcon tray;
    readonly System.Windows.Forms.Timer tick = new() { Interval = 5000 };
    readonly MediaController media = new();
    readonly QuestionProvider provider = new();

    ToolStripMenuItem status = null!, snoozeItem = null!;
    QuizSession? quiz;
    DateTime lastTick = DateTime.UtcNow, lastSave = DateTime.UtcNow;
    bool busy, dialogOpen;

    public TrayApp(AppSettings settings, bool quizNow)
    {
        s = settings;
        T.Lang = s.UiLanguage;
        Startup.Apply(s.StartWithWindows);

        tray = new NotifyIcon { Icon = AppIcon.Create(), Visible = true, Text = "Test Time" };
        BuildMenu();
        UpdateStatus();

        tick.Tick += async (_, _) => await OnTick();
        tick.Start();

        if (s.PinIsDefault)
            tray.ShowBalloonTip(8000, "Test Time", T.Get("default_pin"), ToolTipIcon.Info);

        if (quizNow)
        {
            var once = new System.Windows.Forms.Timer { Interval = 1500 };
            once.Tick += async (_, _) => { once.Stop(); once.Dispose(); await StartQuiz(); };
            once.Start();
        }
    }

    void BuildMenu()
    {
        var m = new ContextMenuStrip();
        status = new ToolStripMenuItem { Enabled = false };
        snoozeItem = new ToolStripMenuItem("", null, (_, _) => ToggleSnooze());
        m.Items.Add(status);
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(T.Get("quiz_now"), null, async (_, _) => await StartQuiz());
        m.Items.Add(snoozeItem);
        m.Items.Add(T.Get("settings"), null, (_, _) => OpenSettings());
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(T.Get("exit"), null, (_, _) => ExitApp());
        m.Opening += (_, _) => UpdateStatus();

        var old = tray.ContextMenuStrip;
        tray.ContextMenuStrip = m;
        old?.Dispose();
    }

    bool Snoozed => s.SnoozeUntil is DateTime until && DateTime.Now < until;

    async Task OnTick()
    {
        if (busy) return;
        busy = true;
        try
        {
            var now = DateTime.UtcNow;
            double dt = (now - lastTick).TotalSeconds;
            lastTick = now;
            if (dt < 0 || dt > 30) dt = tick.Interval / 1000.0;   // PC was asleep

            if (quiz != null)
            {
                await quiz.KeepEnforcingAsync();
                return;
            }

            if (s.SnoozeUntil != null && !Snoozed) { s.SnoozeUntil = null; s.Save(); }

            if (!Snoozed)
            {
                bool active = GameDetector.IsGameActive(s.GameProcesses)
                              || await media.IsAnythingPlayingAsync(s.IgnoredMediaApps);
                if (active) s.AccumulatedSeconds += dt;

                if (s.AccumulatedSeconds >= s.IntervalMinutes * 60 && !dialogOpen)
                    await StartQuiz();
            }

            if ((now - lastSave).TotalSeconds >= 60)
            {
                s.Save();
                lastSave = now;
            }
        }
        catch (Exception ex) { Log.Error(ex); }
        finally
        {
            busy = false;
            UpdateStatus();
        }
    }

    async Task StartQuiz()
    {
        if (quiz != null) return;
        var session = new QuizSession(s, provider, media);
        quiz = session;
        session.Finished += () =>
        {
            quiz = null;
            s.AccumulatedSeconds = 0;
            s.Save();
            UpdateStatus();
        };
        UpdateStatus();
        try { await session.StartAsync(); }
        catch (Exception ex)
        {
            Log.Error(ex);
            quiz = null;
        }
    }

    void UpdateStatus()
    {
        string text;
        if (quiz != null) text = T.Get("tip_quiz");
        else if (Snoozed) text = T.F("tip_snoozed", s.SnoozeUntil!.Value);
        else
        {
            int left = (int)Math.Ceiling(Math.Max(0, s.IntervalMinutes * 60 - s.AccumulatedSeconds) / 60.0);
            text = T.F("tip_next", left);
        }
        status.Text = text;
        snoozeItem.Text = Snoozed ? T.Get("unsnooze") : T.Get("snooze");
        var tip = "Test Time – " + text;
        tray.Text = tip.Length > 120 ? tip[..120] : tip;
    }

    bool AskPin()
    {
        if (dialogOpen || quiz != null) return false;
        dialogOpen = true;
        try { return PinPrompt.Ask(null, s); }
        finally { dialogOpen = false; }
    }

    void ToggleSnooze()
    {
        if (Snoozed)
        {
            s.SnoozeUntil = null;
        }
        else
        {
            if (!AskPin()) return;
            s.SnoozeUntil = DateTime.Now.AddHours(1);
        }
        s.Save();
        UpdateStatus();
    }

    void OpenSettings()
    {
        if (!AskPin()) return;
        dialogOpen = true;
        try
        {
            using var f = new SettingsForm(s);
            if (f.ShowDialog() == DialogResult.OK)
            {
                T.Lang = s.UiLanguage;
                Startup.Apply(s.StartWithWindows);
                BuildMenu();
            }
        }
        finally { dialogOpen = false; }
        UpdateStatus();
    }

    void ExitApp()
    {
        if (!AskPin()) return;
        tick.Stop();
        s.Save();
        tray.Visible = false;
        tray.Dispose();
        ExitThread();
    }
}

/// <summary>One quiz: pauses/mutes everything, shows the quiz, then restores everything.</summary>
sealed class QuizSession
{
    public event Action? Finished;

    readonly AppSettings s;
    readonly QuestionProvider provider;
    readonly MediaController media;
    readonly List<CoverForm> covers = new();
    QuizForm? form;
    KeyboardBlocker? keyboard;
    List<IntPtr> minimizedGames = new();
    InputLanguage? originalKeyboard;
    bool done;

    public QuizSession(AppSettings settings, QuestionProvider provider, MediaController media)
    {
        s = settings;
        this.provider = provider;
        this.media = media;
    }

    public async Task StartAsync()
    {
        if (s.PauseMedia) await media.PauseAllAsync();
        if (s.MuteAudio) AudioMuter.MuteOthers();
        minimizedGames = GameDetector.MinimizeGames(s.GameProcesses);
        originalKeyboard = InputLanguage.CurrentInputLanguage;
        keyboard = new KeyboardBlocker();

        foreach (var screen in Screen.AllScreens.Where(x => !x.Primary))
        {
            var c = new CoverForm(screen);
            c.Show();
            covers.Add(c);
        }

        form = new QuizForm(s, provider, covers);
        form.Completed += OnCompleted;
        form.Show();
    }

    /// <summary>Called every few seconds while the quiz is open, in case something started playing again.</summary>
    public async Task KeepEnforcingAsync()
    {
        if (done) return;
        if (s.PauseMedia) await media.PauseAllAsync();
        if (s.MuteAudio) AudioMuter.MuteOthers();
    }

    async void OnCompleted()
    {
        if (done) return;
        done = true;
        await Task.Yield();   // let the quiz form's click handler finish before we close it
        try
        {
            keyboard?.Dispose();
            foreach (var c in covers) { c.AllowClose = true; c.Close(); c.Dispose(); }
            covers.Clear();
            if (form != null) { form.AllowClose = true; form.Close(); form.Dispose(); }

            AudioMuter.Restore();
            GameDetector.Restore(minimizedGames);
            try { if (originalKeyboard != null) InputLanguage.CurrentInputLanguage = originalKeyboard; } catch { }
            if (s.PauseMedia) await media.ResumeAsync();
        }
        catch (Exception ex) { Log.Error(ex); }
        finally { Finished?.Invoke(); }
    }
}
