# Test Time

A Windows 10/11 tray app that earns screen time with short quizzes. After every **15 minutes of
active play**, the screen locks and shows a full-screen quiz. The child has to answer **2 problems**
before playback continues. The interval and problem count can be changed in Settings.

## What counts as play

- **Video:** anything Windows reports as playing media, such as YouTube or other video in Chrome,
  Edge or Firefox, Netflix, Disney+, VLC or Movies & TV. Apps listed under "ignore media apps"
  (Spotify by default) don't count.
- **Games:** the time counts when a listed game is the window in front. The default list is
  `RobloxPlayerBeta` (Roblox from roblox.com) and `Windows10Universal` (Roblox from the Microsoft
  Store). You can add other games by process name, e.g. `Minecraft.Windows`.

The timer only runs during play. It is saved to disk, so restarting doesn't reset it.

## What happens at quiz time

1. Every playing video is paused, all app sound is muted and the Roblox window is minimized.
2. A full-screen quiz covers the main screen. Other monitors are blanked.
3. Win key, Alt+Tab, Alt+F4, Ctrl+Esc, Ctrl+Shift+Esc and the media keys are blocked.
4. Wrong answers show "try again", plus a hint if the question has one. After 3 misses the app
   shows the answer and moves to a new question (that one doesn't count). "Another question" skips.
5. Once the required number of answers are correct, the sound comes back, Roblox is restored and
   paused videos resume.

The **Parent** button (bottom right) unlocks the screen with the PIN.

## Questions (Γ' Δημοτικού level)

| | Greek | English |
|---|---|---|
| **Math** (endless, generated) | numbers up to 1000, + − × :, times tables, missing number, word problems, double/half, rounding, ×10/×100, hours/metres/kilos | same, in English |
| **Logic** | number patterns, days of the week, "who is the tallest", odd one out, riddles | same |
| **Language** | plurals, opposites, articles, syllables, past tense (αόριστος), diminutives, spelling, synonyms | vocabulary (Greek→English), numbers in words, opposites, plurals, am/is/are, has/have, colours, days, months |

Answer checking is forgiving. It ignores capitals and extra spaces. It accepts a leading article
("τα βιβλία" = "βιβλία", "a dog" = "dog"), "1.000" for 1000 and a unit after the number
("24 καραμέλες"). Greek accents and final ς are not required unless "Strict accent checking" is
switched on. The keyboard switches to Greek or English automatically to match each question,
if both layouts are installed.

**Adding your own questions:** create `%AppData%\TestTime\questions.json` in the same format as
`src/Data/questions.json`, then restart the app:

```json
[
  { "lang": "el", "cat": "language", "q": "Γράψε τον πληθυντικό:\nο φίλος → οι …", "a": ["φίλοι"] },
  { "lang": "en", "cat": "logic", "q": "What comes next? 5, 10, 15, ?", "a": ["20"], "hint": "Count by 5." }
]
```

## Parent controls (tray icon → right-click)

- **Status:** time left until the next test.
- **Test now:** start a quiz right away (no PIN needed).
- **Pause for 1 hour:** needs the PIN.
- **Settings:** needs the PIN. Covers interval, number of problems, languages, categories, strict
  accents, pause/mute, start with Windows, game list, ignored apps, a new PIN, the history folder
  and a timer reset.
- **Exit:** needs the PIN.

**Default PIN: `1234`.** Change it on first use. Five wrong PIN attempts lock the PIN prompt for 5 minutes.

Every answer is logged to `%AppData%\TestTime\history.csv`, which opens in Excel. Settings are in
`settings.json` in the same folder.

## Build (from WSL / Linux)

```bash
./build.sh            # → dist/TestTime.exe
```

The .NET SDK, NuGet cache and intermediate files are kept in `.tools/` and `.build/` inside this folder.

## Install on the kid's PC

**Download:** get `TestTime.exe` from the [latest release](https://github.com/Hoi-Gr/test-time/releases/latest). Windows SmartScreen may warn that it's from an unknown publisher; click *More info → Run anyway*.

1. Copy `TestTime.exe` to e.g. `C:\Users\<kid>\AppData\Local\TestTime\TestTime.exe`.
2. Run it once while logged into the **kid's Windows account**. It adds itself to start-up
   (current user only) and shows a "T" icon in the tray.
3. Right-click the tray icon → Settings → set your own PIN.

To test without waiting 15 minutes, run `TestTime.exe --quiz-now` or use **Test now** in the tray menu.

## Known limits

- **Ctrl+Alt+Del** can't be blocked by any app. From there a determined kid could open Task
  Manager and end `TestTime.exe`. For stronger protection, give the kid a **standard** (non-admin)
  Windows account and turn on Microsoft Family Safety. Task Manager can also be disabled for that
  account with the Group Policy Editor.
- The kid can edit the settings file in `%AppData%`, since it lives in their own profile.
- Games running in exclusive full-screen mode can sometimes cover topmost windows. That's why
  game windows are also minimized at quiz time.
- Roblox keeps running behind the quiz (it's online, so it can't be truly paused). Sound is
  muted, and an idle player may get kicked after ~20 minutes.
