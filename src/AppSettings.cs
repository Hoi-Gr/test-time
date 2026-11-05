using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TestTime;

public sealed class AppSettings
{
    // --- Timing ---
    public int IntervalMinutes { get; set; } = 15;
    public int ProblemsRequired { get; set; } = 2;

    // --- Content ---
    public string UiLanguage { get; set; } = "el";          // "el" or "en"
    public bool GreekQuestions { get; set; } = true;
    public bool EnglishQuestions { get; set; } = true;
    public bool MathQuestions { get; set; } = true;
    public bool LogicQuestions { get; set; } = true;
    public bool LanguageQuestions { get; set; } = true;
    public bool StrictAccents { get; set; }                 // false: τόνοι / final ς are not required

    // --- Behaviour ---
    public bool PauseMedia { get; set; } = true;
    public bool MuteAudio { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;
    public List<string> GameProcesses { get; set; } = new() { "RobloxPlayerBeta", "Windows10Universal" };
    public List<string> IgnoredMediaApps { get; set; } = new() { "Spotify" };

    // --- Parent PIN (salted SHA-256) ---
    public string PinSalt { get; set; } = "";
    public string PinHash { get; set; } = "";
    public bool PinIsDefault { get; set; } = true;

    // --- State (kept on disk so restarting the PC does not reset the timer) ---
    public double AccumulatedSeconds { get; set; }
    public DateTime? SnoozeUntil { get; set; }

    static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        AppSettings s;
        try
        {
            s = File.Exists(AppPaths.Settings)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppPaths.Settings)) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            s = new AppSettings();
        }

        if (string.IsNullOrEmpty(s.PinHash) || string.IsNullOrEmpty(s.PinSalt))
        {
            s.SetPin("1234");
            s.PinIsDefault = true;
        }
        s.IntervalMinutes = Math.Clamp(s.IntervalMinutes, 1, 240);
        s.ProblemsRequired = Math.Clamp(s.ProblemsRequired, 1, 10);
        if (s.UiLanguage is not ("el" or "en")) s.UiLanguage = "el";
        s.GameProcesses ??= new();
        s.IgnoredMediaApps ??= new();
        if (s.AccumulatedSeconds < 0 || double.IsNaN(s.AccumulatedSeconds)) s.AccumulatedSeconds = 0;
        s.Save();
        return s;
    }

    public void Save()
    {
        try
        {
            var tmp = AppPaths.Settings + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, Opts));
            File.Move(tmp, AppPaths.Settings, overwrite: true);
        }
        catch (Exception ex) { Log.Error(ex); }
    }

    public void SetPin(string pin)
    {
        PinSalt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        PinHash = Hash(pin);
        PinIsDefault = false;
    }

    public bool CheckPin(string pin)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(Hash(pin)), Convert.FromHexString(PinHash));
        }
        catch { return false; }
    }

    string Hash(string pin) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(PinSalt + ":" + pin.Trim())));
}
