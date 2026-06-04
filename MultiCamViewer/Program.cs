using Avalonia;
using LibVLCSharp.Shared;
using System;

namespace MultiCamViewer
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Core.Initialize();

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
