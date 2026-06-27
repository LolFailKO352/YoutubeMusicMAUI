using Microsoft.Extensions.DependencyInjection;
using Melodium.Services;
using Melodium.ViewModels;

namespace Melodium
{
    public partial class App : Application
    {
        public App()
        {
#if WINDOWS
            var userDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Melodium", "WebView2");
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);
#endif
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ytm_crash.txt"), "Unhandled: " + (e.ExceptionObject?.ToString() ?? "Unknown exception"));
                }
                catch { }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                try
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ytm_crash.txt"), "Unobserved: " + e.Exception.ToString());
                }
                catch { }
            };
        }

#if WINDOWS
        private H.NotifyIcon.TaskbarIcon? _trayIcon;
#endif

        public static bool IsExiting { get; set; } = false;

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            
#if WINDOWS
            _trayIcon = new H.NotifyIcon.TaskbarIcon
            {
                ToolTipText = "Melodium",
                IconSource = "trayicon.ico"
            };

            var menu = new Microsoft.Maui.Controls.MenuFlyout
            {
                new Microsoft.Maui.Controls.MenuFlyoutItem { Text = "Pozastavit / Přehrát", Command = new Command(() => TogglePlayPause_Click(null, EventArgs.Empty)) },
                new Microsoft.Maui.Controls.MenuFlyoutItem { Text = "Zobrazit / Skrýt aplikaci", Command = new Command(() => ShowHideWindow_Click(null, EventArgs.Empty)) },
                new Microsoft.Maui.Controls.MenuFlyoutItem { Text = "Ukončit", Command = new Command(() => ExitApplication_Click(null, EventArgs.Empty)) }
            };

            Microsoft.Maui.Controls.FlyoutBase.SetContextFlyout(_trayIcon, menu);
            _trayIcon.ForceCreate();

            // Překlad tray menu ihned při startu
            var savedLang = Preferences.Default.Get("AppLanguage", "cs");
            if (savedLang != "cs")
            {
                Task.Run(async () =>
                {
                    var ts = IPlatformApplication.Current?.Services.GetService<TranslationService>();
                    if (ts != null)
                    {
                        var t0 = await ts.TranslateAsync("Pozastavit / Přehrát", savedLang);
                        var t1 = await ts.TranslateAsync("Zobrazit / Skrýt aplikaci", savedLang);
                        var t2 = await ts.TranslateAsync("Ukončit", savedLang);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            var flyout = (Microsoft.Maui.Controls.MenuFlyout?)Microsoft.Maui.Controls.FlyoutBase.GetContextFlyout(_trayIcon);
                            if (flyout != null && flyout.Count >= 3)
                            {
                                if (flyout[0] is Microsoft.Maui.Controls.MenuFlyoutItem item0) item0.Text = t0;
                                if (flyout[1] is Microsoft.Maui.Controls.MenuFlyoutItem item1) item1.Text = t1;
                                if (flyout[2] is Microsoft.Maui.Controls.MenuFlyoutItem item2) item2.Text = t2;
                            }
                        });
                    }
                });
            }

            MainViewModel.LanguageChanged += async (s, culture) =>
            {
                var ts = IPlatformApplication.Current?.Services.GetService<TranslationService>();
                if (ts != null)
                {
                    var targetLang = culture.TwoLetterISOLanguageName;
                    var t0 = await ts.TranslateAsync("Pozastavit / Přehrát", targetLang);
                    var t1 = await ts.TranslateAsync("Zobrazit / Skrýt aplikaci", targetLang);
                    var t2 = await ts.TranslateAsync("Ukončit", targetLang);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var flyout = (Microsoft.Maui.Controls.MenuFlyout?)Microsoft.Maui.Controls.FlyoutBase.GetContextFlyout(_trayIcon);
                        if (flyout != null && flyout.Count >= 3)
                        {
                            if (flyout[0] is Microsoft.Maui.Controls.MenuFlyoutItem item0) item0.Text = t0;
                            if (flyout[1] is Microsoft.Maui.Controls.MenuFlyoutItem item1) item1.Text = t1;
                            if (flyout[2] is Microsoft.Maui.Controls.MenuFlyoutItem item2) item2.Text = t2;
                        }
                    });
                }
            };
#endif
            
            return window;
        }

        private void ShowHideWindow_Click(object? sender, EventArgs e)
        {
#if WINDOWS
            var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.Maui.MauiWinUIWindow;
            if (window != null)
            {
                if (window.AppWindow.IsVisible)
                {
                    window.AppWindow.Hide();
                }
                else
                {
                    window.AppWindow.Show();
                }
            }
#endif
        }

        private void TogglePlayPause_Click(object? sender, EventArgs e)
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is AppShell shell && shell.CurrentPage is MainPage mainPage)
            {
                if (mainPage.BindingContext is MainViewModel vm)
                {
                    if (vm.PlayPauseCommand.CanExecute(null))
                    {
                        vm.PlayPauseCommand.Execute(null);
                    }
                }
            }
        }

        private void ExitApplication_Click(object? sender, EventArgs e)
        {
            IsExiting = true;
            Application.Current?.Quit();
        }
    }
}