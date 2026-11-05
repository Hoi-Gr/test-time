using System.Diagnostics;

namespace TestTime;

sealed class SettingsForm : Form
{
    readonly AppSettings s;
    readonly ComboBox cmbLang = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    readonly NumericUpDown numInterval = new() { Minimum = 1, Maximum = 240, Width = 80 };
    readonly NumericUpDown numProblems = new() { Minimum = 1, Maximum = 10, Width = 80 };
    readonly CheckBox chkGreek = new(), chkEnglish = new(), chkMath = new(), chkLogic = new(), chkLanguage = new(),
        chkStrict = new(), chkPause = new(), chkMute = new(), chkStartup = new();
    readonly TextBox txtGames = new() { Width = 380 }, txtIgnored = new() { Width = 380 };
    readonly TextBox txtPin = new() { Width = 160, UseSystemPasswordChar = true }, txtPin2 = new() { Width = 160, UseSystemPasswordChar = true };

    public SettingsForm(AppSettings settings)
    {
        s = settings;
        Text = T.Get("s_title");
        Icon = AppIcon.Create();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        TopMost = true;
        Font = new Font("Segoe UI", 10f);
        Padding = new Padding(12);

        var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };

        void Row(string label, Control c)
        {
            grid.Controls.Add(new Label { Text = T.Get(label), AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 12, 3) });
            c.Anchor = AnchorStyles.Left;
            grid.Controls.Add(c);
        }
        void Check(CheckBox c, string label, bool value)
        {
            c.Text = T.Get(label);
            c.Checked = value;
            c.AutoSize = true;
            grid.Controls.Add(new Label { AutoSize = true });
            grid.Controls.Add(c);
        }

        cmbLang.Items.AddRange(new object[] { "Ελληνικά", "English" });
        cmbLang.SelectedIndex = s.UiLanguage == "en" ? 1 : 0;
        numInterval.Value = s.IntervalMinutes;
        numProblems.Value = s.ProblemsRequired;
        txtGames.Text = string.Join(", ", s.GameProcesses);
        txtIgnored.Text = string.Join(", ", s.IgnoredMediaApps);

        Row("s_ui_lang", cmbLang);
        Row("s_interval", numInterval);
        Row("s_problems", numProblems);
        Check(chkGreek, "s_greek", s.GreekQuestions);
        Check(chkEnglish, "s_english", s.EnglishQuestions);
        Check(chkMath, "s_math", s.MathQuestions);
        Check(chkLogic, "s_logic", s.LogicQuestions);
        Check(chkLanguage, "s_language", s.LanguageQuestions);
        Check(chkStrict, "s_strict", s.StrictAccents);
        Check(chkPause, "s_pause", s.PauseMedia);
        Check(chkMute, "s_mute", s.MuteAudio);
        Check(chkStartup, "s_startup", s.StartWithWindows);
        Row("s_games", txtGames);
        Row("s_ignored", txtIgnored);
        Row("s_newpin", txtPin);
        Row("s_confirmpin", txtPin2);

        var save = new Button { Text = T.Get("s_save"), AutoSize = true };
        var cancel = new Button { Text = T.Get("cancel"), AutoSize = true, DialogResult = DialogResult.Cancel };
        var folder = new Button { Text = T.Get("s_folder"), AutoSize = true };
        var reset = new Button { Text = T.Get("s_reset"), AutoSize = true };
        save.Click += (_, _) => SaveClicked();
        folder.Click += (_, _) => { try { Process.Start("explorer.exe", AppPaths.Dir); } catch (Exception ex) { Log.Error(ex); } };
        reset.Click += (_, _) => { s.AccumulatedSeconds = 0; s.Save(); MessageBox.Show(this, T.Get("s_reset_done"), Text); };

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 16, 0, 0) };
        buttons.Controls.AddRange(new Control[] { save, cancel, folder, reset });
        grid.Controls.Add(buttons);
        grid.SetColumnSpan(buttons, 2);

        Controls.Add(grid);
        AcceptButton = save;
        CancelButton = cancel;
    }

    static List<string> SplitList(string text) =>
        text.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    void SaveClicked()
    {
        if (txtPin.Text.Length > 0 || txtPin2.Text.Length > 0)
        {
            if (txtPin.Text != txtPin2.Text) { MessageBox.Show(this, T.Get("s_pin_mismatch"), Text); return; }
            if (txtPin.Text.Trim().Length < 4) { MessageBox.Show(this, T.Get("s_pin_short"), Text); return; }
        }

        s.UiLanguage = cmbLang.SelectedIndex == 1 ? "en" : "el";
        s.IntervalMinutes = (int)numInterval.Value;
        s.ProblemsRequired = (int)numProblems.Value;
        s.GreekQuestions = chkGreek.Checked;
        s.EnglishQuestions = chkEnglish.Checked;
        s.MathQuestions = chkMath.Checked;
        s.LogicQuestions = chkLogic.Checked;
        s.LanguageQuestions = chkLanguage.Checked;
        s.StrictAccents = chkStrict.Checked;
        s.PauseMedia = chkPause.Checked;
        s.MuteAudio = chkMute.Checked;
        s.StartWithWindows = chkStartup.Checked;
        s.GameProcesses = SplitList(txtGames.Text);
        s.IgnoredMediaApps = SplitList(txtIgnored.Text);
        if (txtPin.Text.Length > 0) s.SetPin(txtPin.Text);
        s.Save();

        DialogResult = DialogResult.OK;
        Close();
    }
}
