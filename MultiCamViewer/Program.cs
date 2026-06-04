using Avalonia;
using Serilog;
using System;

namespace MultiCamViewer
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().
                WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day).CreateLogger();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();
    }
}
