using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestTime;

/// <summary>
/// Picks the next question according to the settings, mixing generated problems with the
/// built-in question bank (Data/questions.json) and an optional parent-made bank in
/// %AppData%\TestTime\questions.json (same format; its questions are added to the built-in ones).
/// </summary>
sealed class QuestionProvider
{
    readonly Random rng = new();
    readonly List<Question> bank = LoadBank();
    readonly Queue<string> recent = new();
    readonly HashSet<string> recentSet = new();

    sealed class BankItem
    {
        [JsonPropertyName("lang")] public string Lang { get; set; } = "el";
        [JsonPropertyName("cat")] public string Cat { get; set; } = "language";
        [JsonPropertyName("q")] public string Q { get; set; } = "";
        [JsonPropertyName("a")] public string[] A { get; set; } = Array.Empty<string>();
        [JsonPropertyName("hint")] public string? Hint { get; set; }
    }

    static readonly JsonSerializerOptions Opts = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };

    static List<Question> LoadBank()
    {
        var list = new List<Question>();
        try
        {
            using var st = typeof(QuestionProvider).Assembly.GetManifestResourceStream("TestTime.questions.json");
            if (st != null) Add(list, JsonSerializer.Deserialize<List<BankItem>>(st, Opts));
        }
        catch (Exception ex) { Log.Error(ex); }

        try
        {
            if (File.Exists(AppPaths.CustomQuestions))
                Add(list, JsonSerializer.Deserialize<List<BankItem>>(File.ReadAllText(AppPaths.CustomQuestions), Opts));
        }
        catch (Exception ex) { Log.Error(ex); }
        return list;
    }

    static void Add(List<Question> list, List<BankItem>? items)
    {
        if (items == null) return;
        foreach (var i in items)
        {
            if (string.IsNullOrWhiteSpace(i.Q) || i.A.Length == 0) continue;
            list.Add(new Question
            {
                Lang = i.Lang == "en" ? "en" : "el",
                Category = i.Cat is "math" or "logic" ? i.Cat : "language",
                Prompt = i.Q.Trim(),
                Answers = i.A,
                Hint = i.Hint,
            });
        }
    }

    public Question Next(AppSettings s)
    {
        var langs = new List<string>();
        if (s.GreekQuestions) langs.Add("el");
        if (s.EnglishQuestions) langs.Add("en");
        if (langs.Count == 0) langs.Add("el");

        var cats = new List<string>();
        if (s.MathQuestions) cats.Add("math");
        if (s.LogicQuestions) cats.Add("logic");
        if (s.LanguageQuestions) cats.Add("language");
        if (cats.Count == 0) cats.Add("math");

        for (int attempt = 0; attempt < 40; attempt++)
        {
            string lang = langs[rng.Next(langs.Count)];
            string cat = cats[rng.Next(cats.Count)];
            Question? q = cat switch
            {
                "math" => rng.Next(6) == 0 ? FromBank(lang, cat) ?? MathGenerator.Create(lang, rng) : MathGenerator.Create(lang, rng),
                "logic" => rng.Next(2) == 0 ? LogicGenerator.Create(lang, rng) : FromBank(lang, cat) ?? LogicGenerator.Create(lang, rng),
                _ => FromBank(lang, cat),
            };
            if (q == null || (recentSet.Contains(q.Key) && attempt < 30)) continue;
            Remember(q);
            return q;
        }

        var fallback = MathGenerator.Create(langs[0], rng);
        Remember(fallback);
        return fallback;
    }

    Question? FromBank(string lang, string cat)
    {
        var all = bank.Where(b => b.Lang == lang && b.Category == cat).ToList();
        if (all.Count == 0) return null;
        var fresh = all.Where(b => !recentSet.Contains(b.Key)).ToList();
        var pool = fresh.Count > 0 ? fresh : all;    // small bank: allow repeats rather than switching category
        return pool[rng.Next(pool.Count)];
    }

    void Remember(Question q)
    {
        recent.Enqueue(q.Key);
        recentSet.Add(q.Key);
        while (recent.Count > 60) recentSet.Remove(recent.Dequeue());
    }
}
