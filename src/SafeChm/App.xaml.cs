using System.Windows;

namespace SafeChm;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ChmCache.Cleanup();

        if (e.Args.Length >= 2 && e.Args[0] == "--selftest")
        {
            var outFile = e.Args.Length >= 4 && e.Args[2] == "--out" ? e.Args[3] : null;
            int code = Selftest.Run(e.Args[1], outFile);
            Environment.ExitCode = code;
            Shutdown(code);
            return;
        }

        base.OnStartup(e);
        var win = new MainWindow(e.Args);
        MainWindow = win;
        win.Show();
    }
}
