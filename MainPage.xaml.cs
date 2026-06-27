using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Melodium.ViewModels;

#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
#endif

namespace Melodium;

public partial class MainPage : ContentPage
{
    private bool _isUserDraggingSlider = false;

    public MainPage(ViewModels.MainViewModel viewModel, Services.IAudioService audioService)
    {
        InitializeComponent();
        BindingContext = viewModel;

        if (audioService is Services.MauiAudioService mauiAudioService)
        {
            mauiAudioService.Initialize(AudioPlayer);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MainViewModel vm)
        {
            vm.PropertyChanged += Vm_PropertyChanged;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is MainViewModel vm)
        {
            vm.PropertyChanged -= Vm_PropertyChanged;
        }
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsFullScreenPlayerVisible))
        {
            var vm = (MainViewModel)sender!;
            if (vm.IsFullScreenPlayerVisible)
            {
                FullScreenGrid.IsVisible = true;
                FullScreenGrid.Opacity = 0;
                FullScreenGrid.TranslationY = 150;
                
                FullScreenGrid.FadeTo(1, 350, Easing.CubicOut);
                FullScreenGrid.TranslateTo(0, 0, 350, Easing.CubicOut);
            }
            else
            {
                FullScreenGrid.FadeTo(0, 300, Easing.CubicIn);
                FullScreenGrid.TranslateTo(0, 150, 300, Easing.CubicIn).ContinueWith(t => 
                {
                    MainThread.BeginInvokeOnMainThread(() => FullScreenGrid.IsVisible = false);
                });
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.CurrentView))
        {
            // Fade out, pak hned fade in s drobným posunem pro plynulejší pocit
            ViewsAreaGrid.Opacity = 0;
            ViewsAreaGrid.TranslationY = 20;
            
            ViewsAreaGrid.FadeTo(1, 300, Easing.CubicOut);
            ViewsAreaGrid.TranslateTo(0, 0, 300, Easing.CubicOut);
        }
    }

    // --- Slider Drag události ---

    private void OnSliderDragStarted(object? sender, EventArgs e)
    {
        _isUserDraggingSlider = true;
    }

    private void OnSliderDragCompleted(object? sender, EventArgs e)
    {
        _isUserDraggingSlider = false;
        if (BindingContext is MainViewModel vm && sender is Microsoft.Maui.Controls.Slider slider)
        {
            var newPosition = TimeSpan.FromSeconds(slider.Value);
            vm.SeekTo(newPosition);
        }
    }

    private void OnVolumeSliderHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if (sender is Microsoft.Maui.Controls.Slider slider && slider.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement nativeSlider)
        {
            nativeSlider.PointerWheelChanged += (s, args) =>
            {
                var delta = args.GetCurrentPoint(nativeSlider).Properties.MouseWheelDelta;
                if (delta > 0)
                {
                    slider.Value = Math.Min(1.0, slider.Value + 0.01);
                }
                else if (delta < 0)
                {
                    slider.Value = Math.Max(0.0, slider.Value - 0.01);
                }
                args.Handled = true;
            };
        }
#endif
    }

    private void OnDiscordRpcToggled(object? sender, ToggledEventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] OnDiscordRpcToggled to {e.Value}");
            vm.IsDiscordRpcEnabled = e.Value;
        }
    }

    // --- Přihlašovací WebView navigace a extrakce cookies ---

    private async void OnLoginWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        var url = e.Url;
        System.Diagnostics.Debug.WriteLine($"WebView Navigated to: {url}");

        // Pokud jsme na music.youtube.com a nejsme na přihlašovací stránce Google accounts
        if (url.Contains("music.youtube.com") && !url.Contains("accounts.google.com"))
        {
            if (BindingContext is ViewModels.MainViewModel vm)
            {
                vm.StatusMessage = "Detekováno přihlášení, extrahuji cookies...";
                var cookies = await GetWebViewCookiesAsync();
                
                // Hledáme klíčové přihlašovací cookies od YouTube
                if (cookies.Count > 0 && cookies.Any(c => c.Name.Contains("SID") || c.Name.Contains("PAPISID")))
                {
                    await vm.SaveSessionAsync(cookies);
                    vm.StatusMessage = "Přihlášení bylo úspěšně načteno a uloženo.";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Cookies zatím neobsahují přihlašovací údaje.");
                }
            }
        }
    }

    private async Task<List<Cookie>> GetWebViewCookiesAsync()
    {
        var cookieList = new List<Cookie>();
        try
        {
#if WINDOWS
            if (LoginWebView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 nativeWebView)
            {
                if (nativeWebView.CoreWebView2 == null)
                {
                    await nativeWebView.EnsureCoreWebView2Async();
                }
                var cookies = await nativeWebView.CoreWebView2.CookieManager.GetCookiesAsync("https://music.youtube.com");
                foreach (var c in cookies)
                {
                    cookieList.Add(new Cookie(c.Name, c.Value)
                    {
                        Domain = c.Domain,
                        Path = c.Path
                    });
                }
            }
#elif ANDROID
            if (LoginWebView.Handler?.PlatformView is Android.Webkit.WebView androidWebView)
            {
                var cookieString = Android.Webkit.CookieManager.Instance.GetCookie("https://music.youtube.com");
                if (!string.IsNullOrEmpty(cookieString))
                {
                    var pairs = cookieString.Split(';');
                    foreach (var pair in pairs)
                    {
                        var parts = pair.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            var name = parts[0].Trim();
                            var value = parts[1].Trim();
                            cookieList.Add(new Cookie(name, value)
                            {
                                Domain = ".youtube.com",
                                Path = "/"
                            });
                        }
                    }
                }
            }
#elif IOS || MACCATALYST
            if (LoginWebView.Handler?.PlatformView is WebKit.WKWebView wkWebView)
            {
                var cookieStore = wkWebView.Configuration.WebsiteDataStore.HttpCookieStore;
                var cookies = await cookieStore.GetAllCookiesAsync();
                foreach (var c in cookies)
                {
                    if (c.Domain.Contains("youtube.com"))
                    {
                        cookieList.Add(new Cookie(c.Name, c.Value)
                        {
                            Domain = c.Domain,
                            Path = c.Path
                        });
                    }
                }
            }
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Chyba při stahování cookies: {ex.Message}");
        }
        return cookieList;
    }
}