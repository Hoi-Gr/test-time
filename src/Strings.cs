namespace TestTime;

/// <summary>UI text in Greek and English.</summary>
static class T
{
    public static string Lang { get; set; } = "el";

    static readonly Dictionary<string, (string el, string en)> D = new()
    {
        ["title"] = ("Ώρα για τεστ!", "Test Time!"),
        ["subtitle"] = ("Λύσε {0} προβλήματα για να συνεχίσεις.", "Solve {0} problems to keep playing."),
        ["progress"] = ("Πρόβλημα {0} από {1}", "Problem {0} of {1}"),
        ["check"] = ("Έλεγχος", "Check"),
        ["skip"] = ("Άλλη ερώτηση", "Another question"),
        ["correct"] = ("Σωστά! Μπράβο! ★", "Correct! Well done! ★"),
        ["wrong"] = ("Όχι ακόμα, δοκίμασε ξανά!", "Not quite, try again!"),
        ["reveal"] = ("Η σωστή απάντηση ήταν: {0}\nΠάμε σε άλλη!", "The answer was: {0}\nLet's try another one!"),
        ["done"] = ("Τέλεια! Καλή διασκέδαση! ★★", "Great job! Have fun! ★★"),
        ["parent"] = ("Γονέας", "Parent"),
        ["hint"] = ("Βοήθεια: {0}", "Hint: {0}"),
        ["cat_math"] = ("Μαθηματικά", "Math"),
        ["cat_logic"] = ("Σκέψη & λογική", "Logic"),
        ["cat_language"] = ("Γλώσσα", "Language"),
        ["paused"] = ("Παύση – ώρα για τεστ!", "Paused – it's Test Time!"),

        ["enter_pin"] = ("Κωδικός γονέα (PIN):", "Parent PIN:"),
        ["wrong_pin"] = ("Λάθος κωδικός. Δοκίμασε ξανά:", "Wrong PIN. Try again:"),
        ["pin_locked"] = ("Πολλές λάθος προσπάθειες. Περίμενε λίγα λεπτά.", "Too many wrong attempts. Wait a few minutes."),
        ["cancel"] = ("Άκυρο", "Cancel"),

        ["quiz_now"] = ("Τεστ τώρα", "Test now"),
        ["snooze"] = ("Παύση για 1 ώρα (γονέας)", "Pause for 1 hour (parent)"),
        ["unsnooze"] = ("Συνέχεια παρακολούθησης", "Resume monitoring"),
        ["settings"] = ("Ρυθμίσεις… (γονέας)", "Settings… (parent)"),
        ["exit"] = ("Έξοδος (γονέας)", "Exit (parent)"),
        ["tip_next"] = ("Επόμενο τεστ σε {0} λεπτά παιχνιδιού", "Next test in {0} min of play"),
        ["tip_snoozed"] = ("Σε παύση έως {0:HH:mm}", "Paused until {0:HH:mm}"),
        ["tip_quiz"] = ("Τεστ σε εξέλιξη", "Test in progress"),
        ["default_pin"] = ("Ο κωδικός γονέα είναι 1234. Άλλαξέ τον από τις Ρυθμίσεις.", "The parent PIN is 1234. Change it in Settings."),

        ["s_title"] = ("Test Time – Ρυθμίσεις", "Test Time – Settings"),
        ["s_ui_lang"] = ("Γλώσσα εφαρμογής:", "App language:"),
        ["s_interval"] = ("Τεστ κάθε (λεπτά παιχνιδιού):", "Test every (minutes of play):"),
        ["s_problems"] = ("Προβλήματα ανά τεστ:", "Problems per test:"),
        ["s_greek"] = ("Ερωτήσεις στα Ελληνικά", "Questions in Greek"),
        ["s_english"] = ("Ερωτήσεις στα Αγγλικά", "Questions in English"),
        ["s_math"] = ("Μαθηματικά", "Math"),
        ["s_logic"] = ("Λογική", "Logic"),
        ["s_language"] = ("Γλώσσα (λεξιλόγιο, ορθογραφία, γραμματική)", "Language (vocabulary, spelling, grammar)"),
        ["s_strict"] = ("Αυστηρός έλεγχος τόνων", "Strict accent checking"),
        ["s_pause"] = ("Παύση βίντεο κατά το τεστ", "Pause videos during the test"),
        ["s_mute"] = ("Σίγαση ήχου κατά το τεστ", "Mute sound during the test"),
        ["s_startup"] = ("Εκκίνηση μαζί με τα Windows", "Start with Windows"),
        ["s_games"] = ("Παιχνίδια (ονόματα διεργασιών, με κόμμα):", "Games (process names, comma separated):"),
        ["s_ignored"] = ("Αγνόησε εφαρμογές ήχου (με κόμμα):", "Ignore media apps (comma separated):"),
        ["s_newpin"] = ("Νέος κωδικός γονέα:", "New parent PIN:"),
        ["s_confirmpin"] = ("Επιβεβαίωση κωδικού:", "Confirm PIN:"),
        ["s_save"] = ("Αποθήκευση", "Save"),
        ["s_folder"] = ("Ιστορικό / φάκελος", "History / folder"),
        ["s_reset"] = ("Μηδενισμός χρόνου", "Reset timer"),
        ["s_pin_mismatch"] = ("Οι δύο κωδικοί δεν ταιριάζουν.", "The two PINs do not match."),
        ["s_pin_short"] = ("Ο κωδικός πρέπει να έχει τουλάχιστον 4 χαρακτήρες.", "The PIN must be at least 4 characters."),
        ["s_reset_done"] = ("Ο χρόνος μηδενίστηκε.", "The timer was reset."),
    };

    public static string Get(string key) =>
        D.TryGetValue(key, out var v) ? (Lang == "en" ? v.en : v.el) : key;

    public static string F(string key, params object[] args) => string.Format(Get(key), args);
}
