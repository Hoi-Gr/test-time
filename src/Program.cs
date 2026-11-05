namespace TestTime;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(true, @"Local\TestTime_SingleInstance", out bool firstInstance);
        if (!firstInstance) return;

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Log.Error(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Error(e.ExceptionObject);

        var settings = AppSettings.Load();
        bool quizNow = args.Any(a => a.Equals("--quiz-now", StringComparison.OrdinalIgnoreCase));
        Application.Run(new TrayApp(settings, quizNow));
    }
}
