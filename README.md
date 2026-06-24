# YouTube Music (Unofficial MAUI Client)

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
| <img width="1903" height="1128" alt="image" src="https://github.com/user-attachments/assets/c719fab8-7d62-4a9b-9c10-0dc40c2b858a" /> | <img width="1904" height="1129" alt="image" src="https://github.com/user-attachments/assets/36f492cf-651e-4756-9172-90ec9268990d" />


| Search Results | Personal Library |
|----------------|--------------|
| <img width="1904" height="1129" alt="image" src="https://github.com/user-attachments/assets/6fec368f-5886-43c9-8378-356d86a6363d" /> | <img width="1904" height="1129" alt="image" src="https://github.com/user-attachments/assets/1bf7efef-03c2-47e9-aaa1-61ce1997ec72" />


*(Note: Don't forget to add your own images to the `docs/images/` folder later and update the filenames above!)*

<<<<<<< HEAD
## 🔐 How Login Works

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
