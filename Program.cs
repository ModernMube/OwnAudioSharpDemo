using Avalonia;
using Avalonia.ReactiveUI;
using System;
using OwnaudioLegacy;
using System.Diagnostics;

namespace OwnaAvalonia
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            OwnAudioEngine.Initialize();

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch(Exception ex) 
            { 
                Debug.WriteLine($"OwnAudio ERROR: {ex.Message}"); 
            }            
        }
            

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
    }
}
