namespace TestTime;

/// <summary>A full-screen, always-on-top window that the user cannot close (Alt+F4) and that is hidden from Alt+Tab.</summary>
class LockedForm : Form
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool AllowClose { get; set; }

    public LockedForm(Screen screen)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = screen.Bounds;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        Text = "Test Time";
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80;   // WS_EX_TOOLWINDOW – not shown in Alt+Tab
            return cp;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!AllowClose && e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
        base.OnFormClosing(e);
    }
}

/// <summary>Blanks out secondary monitors while the quiz is shown on the main one.</summary>
sealed class CoverForm : LockedForm
{
    public CoverForm(Screen screen) : base(screen)
    {
        var lbl = new Label
        {
            Text = T.Get("paused"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 32f, FontStyle.Bold),
            ForeColor = Theme.Muted,
        };
        Controls.Add(lbl);
    }
}
