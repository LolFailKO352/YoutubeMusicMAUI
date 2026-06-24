# YouTube Music (Unofficial MAUI Client)

![YouTube Music App Banner](docs/images/banner_placeholder.png) <!-- Sem později doplňte banner -->

Moderní, rychlý a neoficiální YouTube Music klient postavený na platformě [.NET MAUI](https://dotnet.microsoft.com/en-us/apps/maui). Tato aplikace přináší to nejlepší z YouTube Music přímo na váš desktop pomocí nativního rozhraní, vylepšených algoritmů pro doporučování hudby a hluboké integrace do systému.

## ✨ Funkce

- **🎵 Nativní přehrávání**: Plynulé a kvalitní přehrávání hudby na pozadí pomocí nativního MAUI MediaElementu.
- **🧠 Chytrá doporučení na míru**: Aplikace obsahuje unikátní algoritmus, který analyzuje vaši osobní knihovnu (uložené skladby, interprety, alba a playlisty) a na domovské obrazovce vám automaticky generuje personalizované rádio a ty nejlepší tipy přímo pro vás.
- **🎨 Moderní UI/UX**: Nativní design v duchu Windows 11 / WinUI 3. Plně responzivní rozložení, které se přizpůsobí velikosti okna, s podporou automatického přepínání mezi světlým (Light) a tmavým (Dark) režimem.
- **🌍 Plná lokalizace**: Vestavěný překladatelský engine, který za chodu překládá kompletně celou aplikaci pro cizojazyčné uživatele.
- **⚙️ Integrace do systému**: Aplikace běží v systémové liště (Tray menu) s rychlými ovládacími prvky pro přehrávání (Play/Pause), takže můžete ovládat hudbu, aniž byste museli okno aplikace otevírat.
- **🔍 Hledání a prozkoumávání**: Vyhledávejte jakékoliv skladby, interprety, alba nebo komunitní playlisty přímo z YouTube Music.
- **📚 Správa knihovny**: Jednoduše procházejte své uložené písničky, oblíbená alba a interprety.

## 📸 Snímky obrazovky

| Domovská obrazovka | Přehrávač a fronta |
|-------------|----------------|
| ![Domů](docs/images/screenshot_home.png) | ![Přehrávač](docs/images/screenshot_player.png) |

| Výsledky hledání | Osobní knihovna |
|----------------|--------------|
| ![Hledání](docs/images/screenshot_search.png) | ![Knihovna](docs/images/screenshot_library.png) |

*(Poznámka: Nezapomeňte později přidat své vlastní obrázky do složky `docs/images/` a upravit názvy souborů výše!)*

## 🚀 Jak začít

### Prerekvizity
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (nebo nejnovější dostupná verze s podporou MAUI).
- Visual Studio 2022 (s nainstalovaným balíčkem **.NET Multi-platform App UI development**).

### Instalace a spuštění
1. **Naklonujte repozitář:**
   ```bash
   git clone https://github.com/vase_jmeno/YoutubeMusic.git
   cd YoutubeMusic
   ```

2. **Obnovte závislosti (Restore):**
   ```bash
   dotnet restore
   ```

3. **Spusťte projekt:**
   Nejjednodušší cesta je spustit projekt přímo přes Visual Studio (cíl: `Windows Machine`), případně přes příkazovou řádku:
   ```bash
   dotnet build -t:Run -f net10.0-windows10.0.19041.0
   ```

## 🔐 Jak funguje přihlašování

Aplikace používá bezpečné vestavěné okno prohlížeče (WebView) k tomu, abyste se mohli přihlásit přímo přes Google/YouTube. Po úspěšném přihlášení si aplikace sama na pozadí vytáhne bezpečné "session cookies". Díky nim získá přístup k vaší osobní knihovně a může vám generovat doporučení na míru. Tento postup plně obchází nutnost využívání oficiálního (a často placeného) API klíče.

## 🛠️ Technologie

- **Framework**: [.NET MAUI](https://dotnet.microsoft.com/apps/maui)
- **Architektura**: MVVM (Model-View-ViewModel) s využitím [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- **Média**: `CommunityToolkit.Maui.MediaElement`
- **Data a API**: 
  - [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode)
  - Vlastní integrace YouTube Music API

## 🤝 Přispívání
Návrhy na vylepšení, nahlášení chyb nebo "pull requesty" jsou vítány! Podívejte se do záložky [Issues](https://github.com/vase_jmeno/YoutubeMusic/issues).

## 📄 Licence
Tento projekt je šířen pod licencí MIT. Více podrobností naleznete v souboru [LICENSE](LICENSE).

*Upozornění: Toto je neoficiální, komunitně tvořený projekt. Není sponzorován, schválen ani nijak spojen se společnostmi Google LLC nebo YouTube.*
