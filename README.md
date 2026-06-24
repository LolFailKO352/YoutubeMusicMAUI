# YouTube Music (Unofficial MAUI Client)

![YouTube Music App Banner](docs/images/banner_placeholder.png) <!-- Add banner here later -->

A modern, fast, and unofficial YouTube Music client built on the [.NET MAUI](https://dotnet.microsoft.com/en-us/apps/maui) platform. This application brings the best of YouTube Music straight to your desktop using a native interface, improved music recommendation algorithms, and deep system integration.

## ✨ Features

- **🎵 Native Playback**: Smooth and high-quality background music playback using the native MAUI MediaElement.
- **🧠 Smart Tailored Recommendations**: The app includes a unique algorithm that analyzes your personal library (saved tracks, artists, albums, and playlists) and automatically generates personalized radio and top picks tailored just for you on the home screen.
- **🎨 Modern UI/UX**: Native design inspired by Windows 11 / WinUI 3. Fully responsive layout that adapts to the window size, with support for automatic switching between Light and Dark mode.
- **🌍 Full Localization**: Built-in translation engine that translates the entire application on the fly for non-native users.
- **⚙️ System Integration**: The app runs in the system tray menu with quick playback controls (Play/Pause), allowing you to control music without having to open the app window.
- **🔍 Search and Explore**: Search for any tracks, artists, albums, or community playlists directly from YouTube Music.
- **📚 Library Management**: Easily browse your saved songs, favorite albums, and artists.

## 📸 Screenshots

| Home Screen | Player and Queue |
|-------------|----------------|
| ![Home](docs/images/screenshot_home.png) | ![Player](docs/images/screenshot_player.png) |

| Search Results | Personal Library |
|----------------|--------------|
| ![Search](docs/images/screenshot_search.png) | ![Library](docs/images/screenshot_library.png) |

*(Note: Don't forget to add your own images to the `docs/images/` folder later and update the filenames above!)*

<<<<<<< HEAD
## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or the latest available version with MAUI support).
- Visual Studio 2022 (with the **.NET Multi-platform App UI development** workload installed).

### Installation and Execution
1. **Clone the repository:**
   ```bash
   git clone https://github.com/your_name/YoutubeMusic.git
   cd YoutubeMusic
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Run the project:**
   The easiest way is to run the project directly through Visual Studio (target: `Windows Machine`), or via the command line:
   ```bash
   dotnet build -t:Run -f net10.0-windows10.0.19041.0
   ```

## 🔐 How Login Works
=======
## 🔐 Jak funguje přihlašování
>>>>>>> c60fe52c34b696b0f45c50ff2591f421182f10ee

The application uses a secure built-in browser window (WebView) to allow you to log in directly via Google/YouTube. After a successful login, the app securely retrieves "session cookies" in the background. Thanks to them, it gains access to your personal library and can generate personalized recommendations. This approach fully bypasses the need for an official (and often paid) API key.

## 🛠️ Technologies

- **Framework**: [.NET MAUI](https://dotnet.microsoft.com/apps/maui)
- **Architecture**: MVVM (Model-View-ViewModel) using [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- **Media**: `CommunityToolkit.Maui.MediaElement`
- **Data and API**: 
  - [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode)
  - Custom YouTube Music API integration

## 🤝 Contributing
Suggestions for improvements, bug reports, or pull requests are welcome! Check out the [Issues](https://github.com/your_name/YoutubeMusic/issues) tab.

## 📄 License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

*Disclaimer: This is an unofficial, community-created project. It is not sponsored, endorsed, or otherwise affiliated with Google LLC or YouTube.*
