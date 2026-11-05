namespace TestTime;

static class PinPrompt
{
    static int failures;
    static DateTime lockedUntil = DateTime.MinValue;

    /// <summary>Asks for the parent PIN. After 5 wrong attempts it locks for 5 minutes.</summary>
    public static bool Ask(IWin32Window? owner, AppSettings s)
    {
        using var f = new Form
        {
            Text = "Test Time",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            TopMost = true,
            ShowInTaskbar = owner == null,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16),
            Font = new Font("Segoe UI", 11f),
        };
        var lbl = new Label { Text = T.Get("enter_pin"), AutoSize = true, Margin = new Padding(3, 3, 3, 8) };
        var box = new TextBox { UseSystemPasswordChar = true, Width = 240 };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = T.Get("cancel"), DialogResult = DialogResult.Cancel, AutoSize = true };
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        buttons.Controls.AddRange(new Control[] { ok, cancel });
        var flow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        flow.Controls.AddRange(new Control[] { lbl, box, buttons });
        f.Controls.Add(flow);
        f.AcceptButton = ok;
        f.CancelButton = cancel;
        f.Shown += (_, _) => { Native.ForceForeground(f.Handle); box.Focus(); };

        while (true)
        {
            if (DateTime.Now < lockedUntil)
            {
                lbl.Text = T.Get("pin_locked");
                box.Enabled = ok.Enabled = false;
            }

            if (f.ShowDialog(owner) != DialogResult.OK || DateTime.Now < lockedUntil) return false;

            if (s.CheckPin(box.Text))
            {
                failures = 0;
                return true;
            }

            failures++;
            box.Text = "";
            lbl.Text = T.Get("wrong_pin");
            if (failures >= 5)
            {
                failures = 0;
                lockedUntil = DateTime.Now.AddMinutes(5);
            }
        }
    }
}
