using System.Globalization;

namespace TestTime;

/// <summary>
/// Endless math problems at the level of Γ' Δημοτικού (3rd grade, Greece):
/// numbers up to 1000, times tables, exact division, missing numbers,
/// word problems, double/half, rounding, ×10/×100 and units of measurement.
/// </summary>
static class MathGenerator
{
    static string L(string lang, string el, string en) => lang == "en" ? en : el;

    static Question Q(string lang, string prompt, int answer, string? hint = null) => new()
    {
        Lang = lang,
        Category = "math",
        Prompt = prompt,
        Answers = new[] { answer.ToString(CultureInfo.InvariantCulture) },
        Hint = hint,
    };

    public static Question Create(string lang, Random r) => r.Next(12) switch
    {
        0 or 1 => Add(lang, r),
        2 or 3 => Sub(lang, r),
        4 or 5 => Mul(lang, r),
        6 => Div(lang, r),
        7 => Missing(lang, r),
        8 or 9 => Word(lang, r),
        10 => DoubleHalfRound(lang, r),
        _ => Measures(lang, r),
    };

    static Question Add(string lang, Random r)
    {
        int a = r.Next(100, 800), b = r.Next(10, 1000 - a);
        return Q(lang, L(lang, $"Υπολόγισε:\n{a} + {b} = ?", $"Calculate:\n{a} + {b} = ?"), a + b);
    }

    static Question Sub(string lang, Random r)
    {
        int a = r.Next(200, 1000), b = r.Next(10, a);
        return Q(lang, L(lang, $"Υπολόγισε:\n{a} − {b} = ?", $"Calculate:\n{a} − {b} = ?"), a - b);
    }

    static Question Mul(string lang, Random r)
    {
        int a = r.Next(2, 11), b = r.Next(2, 11);
        return Q(lang, L(lang, $"Υπολόγισε:\n{a} × {b} = ?", $"Calculate:\n{a} × {b} = ?"), a * b);
    }

    static Question Div(string lang, Random r)
    {
        int b = r.Next(2, 11), q = r.Next(2, 11);
        return Q(lang, L(lang, $"Υπολόγισε:\n{b * q} : {b} = ?", $"Calculate:\n{b * q} ÷ {b} = ?"), q,
            L(lang, $"Ποιος αριθμός επί {b} κάνει {b * q};", $"What number times {b} makes {b * q}?"));
    }

    static Question Missing(string lang, Random r)
    {
        string head = L(lang, "Ποιος αριθμός λείπει;", "Which number is missing?");
        switch (r.Next(3))
        {
            case 0:
            {
                int a = r.Next(10, 500), c = a + r.Next(10, 400);
                return Q(lang, $"{head}\n{a} + ___ = {c}", c - a);
            }
            case 1:
            {
                int a = r.Next(2, 11), b = r.Next(2, 11);
                return Q(lang, $"{head}\n{a} × ___ = {a * b}", b);
            }
            default:
            {
                int a = r.Next(100, 1000), b = r.Next(10, a);
                return Q(lang, $"{head}\n{a} − ___ = {a - b}", b);
            }
        }
    }

    static Question Word(string lang, Random r)
    {
        switch (r.Next(6))
        {
            case 0:
            {
                int a = r.Next(15, 60), b = r.Next(3, a);
                return Q(lang, L(lang,
                    $"Η Ελένη έχει {a} καραμέλες. Δίνει {b} στη φίλη της. Πόσες καραμέλες της μένουν;",
                    $"Helen has {a} sweets. She gives {b} to her friend. How many sweets does she have left?"), a - b);
            }
            case 1:
            {
                int a = r.Next(2, 11), n = r.Next(2, 11);
                return Q(lang, L(lang,
                    $"Σε κάθε κουτί υπάρχουν {a} μολύβια. Πόσα μολύβια υπάρχουν σε {n} κουτιά;",
                    $"Each box has {a} pencils. How many pencils are there in {n} boxes?"), a * n);
            }
            case 2:
            {
                int g = r.Next(2, 7), k = r.Next(2, 9);
                return Q(lang, L(lang,
                    $"{g * k} παιδιά χωρίζονται σε ομάδες των {g}. Πόσες ομάδες γίνονται;",
                    $"{g * k} children make groups of {g}. How many groups are there?"), k);
            }
            case 3:
            {
                int a = r.Next(10, 90), b = r.Next(10, 90);
                return Q(lang, L(lang,
                    $"Ο Νίκος διάβασε {a} σελίδες τη Δευτέρα και {b} σελίδες την Τρίτη. Πόσες σελίδες διάβασε συνολικά;",
                    $"Nick read {a} pages on Monday and {b} pages on Tuesday. How many pages did he read in total?"), a + b);
            }
            case 4:
            {
                int p = r.Next(3, 20), m = r.Next(p + 1, 51);
                return Q(lang, L(lang,
                    $"Ένα παιχνίδι κοστίζει {p} ευρώ. Έχεις {m} ευρώ. Πόσα ευρώ σου περισσεύουν αν το αγοράσεις;",
                    $"A toy costs {p} euros. You have {m} euros. How many euros are left after you buy it?"), m - p);
            }
            default:
            {
                int n = r.Next(2, 8);
                bool dogs = r.Next(2) == 0;
                return dogs
                    ? Q(lang, L(lang, $"Πόσα πόδια έχουν μαζί {n} σκύλοι;", $"How many legs do {n} dogs have altogether?"), 4 * n)
                    : Q(lang, L(lang, $"Πόσα πόδια έχουν μαζί {n} κότες;", $"How many legs do {n} chickens have altogether?"), 2 * n);
            }
        }
    }

    static Question DoubleHalfRound(string lang, Random r)
    {
        switch (r.Next(4))
        {
            case 0:
            {
                int n = r.Next(11, 250);
                return Q(lang, L(lang, $"Ποιο είναι το διπλάσιο του {n};", $"What is double {n}?"), 2 * n);
            }
            case 1:
            {
                int n = r.Next(5, 200) * 2;
                return Q(lang, L(lang, $"Ποιο είναι το μισό του {n};", $"What is half of {n}?"), n / 2);
            }
            case 2:
            {
                int n = r.Next(11, 990);
                if (n % 10 == 0) n += 3;
                return Q(lang, L(lang, $"Στρογγυλοποίησε το {n} στην πλησιέστερη δεκάδα.", $"Round {n} to the nearest ten."),
                    (n + 5) / 10 * 10,
                    L(lang, "Κοίτα το τελευταίο ψηφίο: 5 και πάνω πάμε προς τα πάνω.", "Look at the last digit: 5 or more rounds up."));
            }
            default:
            {
                int n = r.Next(2, 100), m = r.Next(2) == 0 ? 10 : 100;
                return Q(lang, L(lang, $"Υπολόγισε:\n{n} × {m} = ?", $"Calculate:\n{n} × {m} = ?"), n * m);
            }
        }
    }

    static Question Measures(string lang, Random r)
    {
        switch (r.Next(4))
        {
            case 0:
            {
                int h = r.Next(1, 6);
                return Q(lang, L(lang, $"Πόσα λεπτά έχουν {h} {(h == 1 ? "ώρα" : "ώρες")};",
                    $"How many minutes are in {h} hour{(h == 1 ? "" : "s")}?"), 60 * h);
            }
            case 1:
            {
                int m = r.Next(1, 10);
                return Q(lang, L(lang, $"Πόσα εκατοστά είναι {m} {(m == 1 ? "μέτρο" : "μέτρα")};",
                    $"How many centimetres are in {m} metre{(m == 1 ? "" : "s")}?"), 100 * m);
            }
            case 2:
            {
                int w = r.Next(1, 6);
                return Q(lang, L(lang, $"Πόσες μέρες έχουν {w} {(w == 1 ? "εβδομάδα" : "εβδομάδες")};",
                    $"How many days are in {w} week{(w == 1 ? "" : "s")}?"), 7 * w);
            }
            default:
            {
                int k = r.Next(1, 6);
                return Q(lang, L(lang, $"Πόσα γραμμάρια είναι {k} {(k == 1 ? "κιλό" : "κιλά")};",
                    $"How many grams are in {k} kilo{(k == 1 ? "" : "s")}?"), 1000 * k);
            }
        }
    }
}
