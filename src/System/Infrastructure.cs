using System.Drawing.Drawing2D;
using System.Text;
using Microsoft.Win32;

namespace TestTime;

static class AppPaths
{
    public static readonly string Dir = Init();

    static string Init()
    {
        var d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TestTime");
        Directory.CreateDirectory(d);
        return d;
    }

    public static string Settings => Path.Combine(Dir, "settings.json");
    public static string History => Path.Combine(Dir, "history.csv");
    public static string ErrorLog => Path.Combine(Dir, "errors.log");
    public static string CustomQuestions => Path.Combine(Dir, "questions.json");
}

static class Log
{
    public static void Error(object? ex)
    {
        try { File.AppendAllText(AppPaths.ErrorLog, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {ex}{Environment.NewLine}"); }
        catch { }
    }
}

/// <summary>Every answer is logged to history.csv so the parent can see progress (opens in Excel).</summary>
static class History
{
    public static void Append(Question q, string given, bool correct)
    {
        try
        {
            bool newFile = !File.Exists(AppPaths.History);
            var sb = new StringBuilder();
            if (newFile) sb.AppendLine("time,language,category,question,answer_given,expected,correct");
            sb.AppendLine(string.Join(",",
                Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), Csv(q.Lang), Csv(q.Category),
                Csv(q.Prompt), Csv(given), Csv(q.Answers.FirstOrDefault() ?? ""), correct ? "yes" : "no"));
            File.AppendAllText(AppPaths.History, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: newFile));
        }
        catch (Exception ex) { Log.Error(ex); }
    }

    static string Csv(string s) => "\"" + s.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
}

static class Startup
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Apply(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true) ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (enable && Environment.ProcessPath is string exe)
                key.SetValue("TestTime", $"\"{exe}\"");
            else
                key.DeleteValue("TestTime", throwOnMissingValue: false);
        }
        catch (Exception ex) { Log.Error(ex); }
    }
}

static class Theme
{
    public static readonly Color Bg = Color.FromArgb(36, 32, 74);
    public static readonly Color Card = Color.FromArgb(58, 50, 120);
    public static readonly Color Accent = Color.FromArgb(124, 77, 255);
    public static readonly Color Title = Color.FromArgb(255, 213, 79);
    public static readonly Color Text = Color.White;
    public static readonly Color Muted = Color.FromArgb(190, 185, 230);
    public static readonly Color Good = Color.FromArgb(105, 240, 174);
    public static readonly Color Bad = Color.FromArgb(255, 160, 150);
}

static class AppIcon
{
    public static Icon Create()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);
            using var b = new SolidBrush(Theme.Accent);
            g.FillEllipse(b, 1, 1, 30, 30);
            using var f = new Font("Segoe UI", 18, FontStyle.Bold, GraphicsUnit.Pixel);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("T", f, Brushes.White, new RectangleF(0, 0, 32, 32), sf);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
