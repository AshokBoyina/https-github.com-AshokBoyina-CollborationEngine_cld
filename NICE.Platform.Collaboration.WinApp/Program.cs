using Microsoft.Extensions.Configuration;
using NICE.Platform.Collaboration.WinApp;

// [STAThread] is REQUIRED for WebView2 (COM-based) and WinForms controls that use COM.
// Without it the main thread defaults to MTA apartment mode, and every call into
// WebView2 throws COMException 0x80010106 (RPC_E_CHANGED_MODE).
// Top-level statements cannot carry [STAThread] as an attribute, so we use an
// explicit Main method instead.

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        Application.Run(new MainForm(config));
    }
}
