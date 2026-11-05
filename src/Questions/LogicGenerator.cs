namespace TestTime;

/// <summary>Number patterns, days of the week and "who is the tallest" puzzles.</summary>
static class LogicGenerator
{
    static readonly string[] DaysEl = { "Δευτέρα", "Τρίτη", "Τετάρτη", "Πέμπτη", "Παρασκευή", "Σάββατο", "Κυριακή" };
    static readonly string[] DaysEn = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    static string L(string lang, string el, string en) => lang == "en" ? en : el;

    static Question Q(string lang, string prompt, string answer, string? hint = null) => new()
    {
        Lang = lang, Category = "logic", Prompt = prompt, Answers = new[] { answer }, Hint = hint,
    };

    public static Question Create(string lang, Random r) => r.Next(4) switch
    {
        0 or 1 => Sequence(lang, r),
        2 => Days(lang, r),
        _ => Order(lang, r),
    };

    static Question Sequence(string lang, Random r)
    {
        var s = new int[5];
        switch (r.Next(4))
        {
            case 0:
            {
                int start = r.Next(1, 30), step = r.Next(2, 11);
                for (int i = 0; i < 5; i++) s[i] = start + i * step;
                break;
            }
            case 1:
            {
                int step = r.Next(2, 10), start = step * 5 + r.Next(5, 60);
                for (int i = 0; i < 5; i++) s[i] = start - i * step;
                break;
            }
            case 2:
            {
                int start = r.Next(1, 6);
                for (int i = 0; i < 5; i++) s[i] = start << i;      // ×2 each time
                break;
            }
            default:
            {
                s[0] = r.Next(1, 10);
                for (int i = 1; i < 5; i++) s[i] = s[i - 1] + i;     // +1, +2, +3, +4
                break;
            }
        }
        string seq = string.Join(", ", s.Take(4));
        return Q(lang,
            L(lang, $"Ποιος αριθμός συνεχίζει τη σειρά;\n{seq}, ?", $"Which number comes next?\n{seq}, ?"),
            s[4].ToString(),
            L(lang, "Βρες τι αλλάζει από τον έναν αριθμό στον επόμενο.", "Find what changes from one number to the next."));
    }

    static Question Days(string lang, Random r)
    {
        var days = lang == "en" ? DaysEn : DaysEl;
        int today = r.Next(7);
        switch (r.Next(3))
        {
            case 0:
            {
                int n = r.Next(2, 4);
                return Q(lang,
                    L(lang, $"Σήμερα είναι {days[today]}. Τι μέρα θα είναι σε {n} μέρες;",
                            $"Today is {days[today]}. What day will it be in {n} days?"),
                    days[(today + n) % 7]);
            }
            case 1:
                return Q(lang,
                    L(lang, $"Σήμερα είναι {days[today]}. Τι μέρα ήταν χθες;",
                            $"Today is {days[today]}. What day was it yesterday?"),
                    days[(today + 6) % 7]);
            default:
                return Q(lang,
                    L(lang, $"Αύριο είναι {days[(today + 1) % 7]}. Τι μέρα είναι σήμερα;",
                            $"Tomorrow is {days[(today + 1) % 7]}. What day is it today?"),
                    days[today]);
        }
    }

    static Question Order(string lang, Random r)
    {
        var pool = lang == "en"
            ? new[] { "Anna", "Helen", "Maria", "Sophie", "Kate" }
            : new[] { "Άννα", "Ελένη", "Μαρία", "Σοφία", "Κατερίνα" };
        var n = pool.OrderBy(_ => r.Next()).Take(3).ToArray();   // n[0] > n[1] > n[2]
        bool tall = r.Next(2) == 0;
        bool askLowest = r.Next(2) == 0;
        string answer = askLowest ? n[2] : n[0];

        if (lang == "en")
        {
            string cmp = tall ? "taller" : "older";
            string ask = tall ? (askLowest ? "shortest" : "tallest") : (askLowest ? "youngest" : "oldest");
            return Q(lang, $"{n[0]} is {cmp} than {n[1]}. {n[1]} is {cmp} than {n[2]}. Who is the {ask}?", answer);
        }
        else
        {
            string cmp = tall ? "ψηλότερη" : "μεγαλύτερη";
            string ask = tall ? (askLowest ? "πιο κοντή" : "πιο ψηλή") : (askLowest ? "μικρότερη" : "μεγαλύτερη");
            return Q(lang,
                $"Η {n[0]} είναι {cmp} από {Article(n[1])} {n[1]}. Η {n[1]} είναι {cmp} από {Article(n[2])} {n[2]}. Ποια είναι η {ask};",
                answer);
        }
    }

    /// <summary>την before vowels and κ, π, τ, ξ, ψ; otherwise τη.</summary>
    static string Article(string name)
    {
        char c = char.ToUpperInvariant(AnswerChecker.RemoveDiacritics(name)[0]);
        return "ΑΕΗΙΟΥΩΚΠΤΞΨ".Contains(c) ? "την" : "τη";
    }
}
