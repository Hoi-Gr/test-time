namespace TestTime;
static class AppPaths { public static string Dir = Path.GetTempPath(); public static string Settings => Path.Combine(Dir,"tt-settings.json"); public static string CustomQuestions => Path.Combine(Dir,"tt-none.json"); }
static class Log { public static void Error(object? e) => Console.WriteLine("LOG ERROR: " + e); }
static class Program {
  static int fails;
  static void Expect(bool cond, string what) { if (!cond) { fails++; Console.WriteLine("FAIL: " + what); } }
  static Question Q(string lang, params string[] a) => new() { Lang = lang, Answers = a };
  static void Main() {
    // Answer checker
    Expect(AnswerChecker.IsCorrect(Q("el","βιβλία"), "τα βιβλια", false), "article + no accent");
    Expect(!AnswerChecker.IsCorrect(Q("el","βιβλία"), "βιβλια", true), "strict accents rejects");
    Expect(AnswerChecker.IsCorrect(Q("el","μικρός"), "Μικρος", false), "final sigma + caps");
    Expect(AnswerChecker.IsCorrect(Q("el","ο"), "ο", false), "single article answer");
    Expect(AnswerChecker.IsCorrect(Q("el","1000"), "1.000", false), "thousands sep");
    Expect(AnswerChecker.IsCorrect(Q("el","24"), "24 καραμέλες", false), "number + unit");
    Expect(!AnswerChecker.IsCorrect(Q("el","24"), "25", false), "wrong number");
    Expect(AnswerChecker.IsCorrect(Q("en","dog"), " A Dog. ", false), "english article");
    Expect(AnswerChecker.IsCorrect(Q("en","Tuesday"), "tuesday", false), "english caps");
    Expect(AnswerChecker.IsCorrect(Q("el","εί"), "ει", false), "missing letters lenient");
    Expect(!AnswerChecker.IsCorrect(Q("el","7"), "", false), "empty");

    // Generators: every question must accept its own first answer
    var r = new Random(1);
    foreach (var lang in new[]{"el","en"})
      for (int i = 0; i < 3000; i++) {
        var m = MathGenerator.Create(lang, r); var l = LogicGenerator.Create(lang, r);
        foreach (var q in new[]{m,l}) {
          Expect(AnswerChecker.IsCorrect(q, q.Answers[0], false) && AnswerChecker.IsCorrect(q, q.Answers[0], true), "self-answer: " + q.Prompt);
          Expect(int.TryParse(q.Answers[0], out var n) ? n >= 0 : q.Category == "logic", "non-negative: " + q.Prompt);
        }
      }

    // Provider + bank
    var p = new QuestionProvider();
    var s = new AppSettings();
    var seen = new Dictionary<string,int>();
    for (int i = 0; i < 2000; i++) { var q = p.Next(s); var k = q.Lang + "/" + q.Category; seen[k] = seen.GetValueOrDefault(k) + 1;
      Expect(AnswerChecker.IsCorrect(q, q.Answers[0], false), "bank self-answer: " + q.Prompt); }
    foreach (var kv in seen.OrderBy(k => k.Key)) Console.WriteLine($"{kv.Key}: {kv.Value}");
    s.EnglishQuestions = false; s.MathQuestions = false; s.LogicQuestions = false;
    for (int i = 0; i < 200; i++) { var q = p.Next(s); Expect(q.Lang == "el" && q.Category == "language", "filter: " + q.Prompt); }

    Console.WriteLine("\nSamples:"); s = new AppSettings();
    for (int i = 0; i < 12; i++) { var q = p.Next(s); Console.WriteLine($"[{q.Lang}/{q.Category}] {q.Prompt.Replace("\n"," ")}  => {q.Answers[0]}"); }
    Console.WriteLine(fails == 0 ? "\nALL CHECKS PASSED" : $"\n{fails} FAILURES");
  }
}
