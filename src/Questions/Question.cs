using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TestTime;

public sealed class Question
{
    public string Lang { get; init; } = "el";          // "el" or "en"
    public string Category { get; init; } = "math";    // "math", "logic", "language"
    public string Prompt { get; init; } = "";
    public string[] Answers { get; init; } = Array.Empty<string>();
    public string? Hint { get; init; }
    public string Key => Lang + "|" + Prompt;
}

/// <summary>
/// Forgiving answer checking for an 8-year-old: ignores capitals, extra spaces, punctuation,
/// leading articles ("τα βιβλία" = "βιβλία", "a dog" = "dog"), thousand separators,
/// and (unless strict mode is on) Greek accents and final ς.
/// </summary>
static class AnswerChecker
{
    static readonly string[] GreekArticles = { "ο", "η", "το", "οι", "τα", "τον", "την", "τη", "του", "της", "των", "ένας", "μία", "μια", "ένα" };
    static readonly string[] EnglishArticles = { "a", "an", "the", "to" };

    public static bool IsCorrect(Question q, string input, bool strictAccents)
    {
        var given = Normalize(input, q.Lang, strictAccents);
        if (given.Length == 0) return false;

        foreach (var a in q.Answers)
        {
            if (given == Normalize(a, q.Lang, strictAccents)) return true;

            if (TryNumber(a, out var expected) &&
                (TryNumber(input, out var g) || LeadingNumber(input, out g)) &&
                Math.Abs(expected - g) < 1e-9)
                return true;
        }
        return false;
    }

    public static string Normalize(string s, string lang, bool strict)
    {
        s = s.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", " ");
        s = s.Trim(' ', '.', '!', '?', ';', ',', '"', '\'', '«', '»', '·', '’');
        if (!strict) s = Loose(s);

        var articles = lang == "en" ? EnglishArticles : GreekArticles;
        var words = s.Split(' ');
        if (words.Length > 1 && articles.Any(a => (strict ? a : Loose(a)) == words[0]))
            s = string.Join(' ', words.Skip(1));
        return s;
    }

    static string Loose(string s) => RemoveDiacritics(s).Replace('ς', 'σ');

    public static string RemoveDiacritics(string s)
    {
        var d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    static bool TryNumber(string s, out double value)
    {
        s = s.Trim().Replace(" ", "");
        if (Regex.IsMatch(s, @"^-?\d{1,3}(\.\d{3})+$")) s = s.Replace(".", "");   // 1.000 -> 1000
        s = s.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>"24 καραμέλες" -> 24</summary>
    static bool LeadingNumber(string s, out double value)
    {
        var m = Regex.Match(s.Trim(), @"^-?\d[\d.,]*(?=\s|$)");
        value = 0;
        return m.Success && TryNumber(m.Value.TrimEnd('.', ','), out value);
    }
}
