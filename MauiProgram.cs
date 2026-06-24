using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
#if WINDOWS
using H.NotifyIcon;
#endif
using YoutubeMusic;

namespace YoutubeMusic;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
#if WINDOWS
            .UseNotifyIcon()
#endif
            .UseMauiCommunityToolkit() // Přidáno
            .UseMauiCommunityToolkitMediaElement(false) // Přidáno pro audio
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windows => windows.OnWindowCreated(window =>
                {
                    window.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop
                    {
                        Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                    };

                    if (window is Microsoft.Maui.MauiWinUIWindow winUIWindow)
                    {
                        winUIWindow.ExtendsContentIntoTitleBar = true;
                        var appWindow = winUIWindow.AppWindow;
                        if (appWindow != null)
                        {
                            appWindow.Closing += (s, e) =>
                            {
                                if (!YoutubeMusic.App.IsExiting)
                                {
                                    e.Cancel = true;
                                    appWindow.Hide();
                                }
                            };
                        }
                    }
                }));
#endif
            });

        // Registrace služeb pro Dependency Injection
        builder.Services.AddSingleton<Services.YouTubeMusicService>();
        builder.Services.AddSingleton<Services.IAudioService, Services.MauiAudioService>();
        builder.Services.AddSingleton<Services.TranslationService>();
        builder.Services.AddTransient<ViewModels.MainViewModel>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}