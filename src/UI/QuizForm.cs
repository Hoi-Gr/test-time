namespace TestTime;

/// <summary>The full-screen quiz. Raises <see cref="Completed"/> when enough problems are solved or a parent unlocks it.</summary>
sealed class QuizForm : LockedForm
{
    public event Action? Completed;

    readonly AppSettings s;
    readonly QuestionProvider provider;
    readonly IReadOnlyList<Form> covers;

    readonly Label lblTitle = new(), lblProgress = new(), lblCategory = new(), lblQuestion = new(), lblFeedback = new();
    readonly TextBox txt = new();
    readonly Button btnCheck = new(), btnSkip = new(), btnParent = new();
    readonly System.Windows.Forms.Timer topTimer = new() { Interval = 700 };

    Question current = null!;
    int solved, wrongTries;
    bool waiting, dialogOpen, finished;

    float K => DeviceDpi / 96f;

    public QuizForm(AppSettings settings, QuestionProvider provider, IReadOnlyList<Form> covers)
        : base(Screen.PrimaryScreen ?? Screen.AllScreens[0])
    {
        s = settings;
        this.provider = provider;
        this.covers = covers;
        KeyPreview = true;

        StyleLabel(lblTitle, 40, FontStyle.Bold, Theme.Title);
        StyleLabel(lblProgress, 18, FontStyle.Regular, Theme.Muted);
        StyleLabel(lblCategory, 14, FontStyle.Italic, Theme.Muted);
        StyleLabel(lblQuestion, 30, FontStyle.Bold, Theme.Text);
        StyleLabel(lblFeedback, 22, FontStyle.Bold, Theme.Text);
        lblTitle.Text = T.Get("title");

        txt.Font = new Font("Segoe UI", 30f);
        txt.TextAlign = HorizontalAlignment.Center;
        txt.BorderStyle = BorderStyle.FixedSingle;

        StyleButton(btnCheck, T.Get("check"), Theme.Accent, 20);
        StyleButton(btnSkip, T.Get("skip"), Theme.Card, 16);
        StyleButton(btnParent, T.Get("parent"), Theme.Bg, 11);
        btnParent.ForeColor = Theme.Muted;

        btnCheck.Click += (_, _) => Check();
        btnSkip.Click += (_, _) => { if (!waiting) NextQuestion(); };
        btnParent.Click += (_, _) => ParentUnlock();
        AcceptButton = btnCheck;

        Controls.AddRange(new Control[] { lblTitle, lblProgress, lblCategory, lblQuestion, txt, btnCheck, btnSkip, lblFeedback, btnParent });

        topTimer.Tick += (_, _) => KeepOnTop();
        Load += (_, _) => { NextQuestion(); topTimer.Start(); };
        Shown += (_, _) => { Native.ForceForeground(Handle); txt.Focus(); };
        Resize += (_, _) => DoLayout();
        FormClosed += (_, _) => topTimer.Dispose();
    }

    static void StyleLabel(Label l, float size, FontStyle style, Color color)
    {
        l.AutoSize = true;
        l.Font = new Font("Segoe UI", size, style);
        l.ForeColor = color;
        l.BackColor = Color.Transparent;
        l.TextAlign = ContentAlignment.MiddleCenter;
    }

    static void StyleButton(Button b, string text, Color back, float size)
    {
        b.Text = text;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = back;
        b.ForeColor = Color.White;
        b.Font = new Font("Segoe UI", size, FontStyle.Bold);
        b.Cursor = Cursors.Hand;
        b.TabStop = false;
    }

    void DoLayout()
    {
        int W = ClientSize.Width, H = ClientSize.Height;
        int w = Math.Min((int)(1100 * K), W - 80);
        foreach (var l in new[] { lblTitle, lblProgress, lblCategory, lblQuestion, lblFeedback })
            l.MaximumSize = new Size(w, 0);

        int y = Math.Max(30, H / 9);
        y = Place(lblTitle, y) + (int)(8 * K);
        y = Place(lblProgress, y) + (int)(2 * K);
        y = Place(lblCategory, y) + (int)(36 * K);
        y = Place(lblQuestion, y) + (int)(36 * K);

        txt.Width = Math.Min((int)(520 * K), w);
        txt.Location = new Point((W - txt.Width) / 2, y);
        y = txt.Bottom + (int)(24 * K);

        btnCheck.Size = new Size((int)(220 * K), (int)(64 * K));
        btnSkip.Size = new Size((int)(260 * K), (int)(64 * K));
        int gap = (int)(20 * K);
        int x = (W - (btnCheck.Width + gap + btnSkip.Width)) / 2;
        btnCheck.Location = new Point(x, y);
        btnSkip.Location = new Point(x + btnCheck.Width + gap, y);
        y = btnCheck.Bottom + (int)(28 * K);

        Place(lblFeedback, y);

        btnParent.Size = new Size((int)(150 * K), (int)(40 * K));
        btnParent.Location = new Point(W - btnParent.Width - 20, H - btnParent.Height - 20);
    }

    int Place(Label l, int y)
    {
        l.Location = new Point((ClientSize.Width - l.Width) / 2, y);
        return l.Bottom;
    }

    void NextQuestion()
    {
        if (finished) return;
        current = provider.Next(s);
        wrongTries = 0;
        waiting = false;
        lblProgress.Text = T.F("progress", solved + 1, s.ProblemsRequired);
        lblCategory.Text = $"{T.Get("cat_" + current.Category)} · {(current.Lang == "en" ? "English" : "Ελληνικά")}";
        lblQuestion.Text = current.Prompt;
        lblFeedback.Text = "";
        txt.Text = "";
        SetInputEnabled(true);
        SwitchKeyboard(current.Lang);
        DoLayout();
        txt.Focus();
    }

    void SetInputEnabled(bool on)
    {
        txt.Enabled = on;
        btnCheck.Enabled = on;
        btnSkip.Enabled = on;
    }

    void Feedback(string text, Color color)
    {
        lblFeedback.ForeColor = color;
        lblFeedback.Text = text;
        DoLayout();
    }

    async void Check()
    {
        if (waiting || finished) return;
        var answer = txt.Text.Trim();
        if (answer.Length == 0) { txt.Focus(); return; }

        bool ok = AnswerChecker.IsCorrect(current, answer, s.StrictAccents);
        History.Append(current, answer, ok);

        if (ok)
        {
            solved++;
            waiting = true;
            SetInputEnabled(false);
            Feedback(T.Get("correct"), Theme.Good);
            await Task.Delay(1200);
            if (finished) return;
            if (solved >= s.ProblemsRequired)
            {
                lblProgress.Text = "";
                lblQuestion.Text = "";
                Feedback(T.Get("done"), Theme.Good);
                await Task.Delay(1500);
                Finish();
            }
            else NextQuestion();
            return;
        }

        wrongTries++;
        if (wrongTries >= 3)
        {
            // Don't let the child get stuck: show the answer and move on (this one doesn't count).
            waiting = true;
            SetInputEnabled(false);
            Feedback(T.F("reveal", current.Answers[0]), Theme.Bad);
            await Task.Delay(3500);
            NextQuestion();
        }
        else
        {
            var msg = T.Get("wrong");
            if (!string.IsNullOrEmpty(current.Hint)) msg += "\n" + T.F("hint", current.Hint);
            Feedback(msg, Theme.Bad);
            txt.SelectAll();
            txt.Focus();
        }
    }

    void ParentUnlock()
    {
        dialogOpen = true;
        try
        {
            if (PinPrompt.Ask(this, s)) Finish();
        }
        finally { dialogOpen = false; }
    }

    void Finish()
    {
        if (finished) return;
        finished = true;
        topTimer.Stop();
        Completed?.Invoke();
    }

    void KeepOnTop()
    {
        if (dialogOpen || finished) return;
        foreach (var c in covers) Native.MakeTopmost(c.Handle);
        if (Native.GetForegroundWindow() != Handle)
        {
            Native.ForceForeground(Handle);
            if (txt.Enabled) txt.Focus();
        }
    }

    /// <summary>Switch the keyboard to Greek for Greek questions and English for English ones, if installed.</summary>
    static void SwitchKeyboard(string lang)
    {
        try
        {
            var target = InputLanguage.InstalledInputLanguages.Cast<InputLanguage>()
                .FirstOrDefault(l => l.Culture.TwoLetterISOLanguageName == lang);
            if (target != null) InputLanguage.CurrentInputLanguage = target;
        }
        catch { }
    }
}
