# UniversalLauncher 🚀

A lightweight, fast, and user-friendly game hub designed to organize all your installed PC games in one single place.

## 🎯 Objective
The main goal of **UniversalLauncher** is to provide a clean and minimal alternative to bloated game launchers. It’s built for speed and simplicity, allowing you to focus on what matters: **playing your games.**

## ✨ Features
*   **Unified Library**: Group games from different platforms (Steam, Epic, GOG, Riot, Unisoft, Battlenet, Xbox, Rockstar, EA or standalone installs) into one organized view.
*   **Auto-Caching System**: Automatically fetches and saves high-quality game covers and icons to your local drive for a beautiful visual experience without re-downloading.
*   **Personalization & Folders**: Create your own folders and fully customize their look. Organize your game library by genre, platform, favorites, or however you prefer to keep everything tidy.
*   **Fast & Lightweight**: Built with .NET and WPF-UI to ensure minimal RAM usage and instant startup times.
*   **Modern Interface**: Features a sleek Fluent Design (Windows 11 style) for a native OS feel.

## ⚙️ How it Works
UniversalLauncher uses the **SteamGridDB API** to fetch professional artwork for your games. To keep the app fast, it caches these images locally after the first search.

### 🔑 Prerequisites
To use the image-fetching feature, you need to provide your own API Key:
1. Get a free API Key from [SteamGridDB](https://www.steamgriddb.com/profile/api).
2. Create a file named `secrets.json` in the application root folder.
3. Add your key in the following format:
```json
{
  "SteamGridApiKey": "YOUR_API_KEY_HERE"
}
