# ClassicTV Plugin for Jellyfin

A Jellyfin plugin that allows mixing episodes from multiple selected series into an ordered playlist, maintaining the internal order of each series using a round-robin algorithm.

## Features

- ✅ **Several playlists per user**: Each playlist has its own name, owner and series selection (e.g. one user can have "classicstv laia" with 8 series and "series angles" with 3, while another has "classicstv" with 9)
- ✅ **Copy a selection between playlists**: Copy (replace or add) the series of one playlist into another from the configuration page
- ✅ **Round-robin algorithm**: Mixes episodes alternating between series
- ✅ **Respect internal order**: Maintains the chronological order of each series
- ✅ **User filtering**: Only includes episodes not watched by each user
- ✅ **Updated in place**: If the playlist already exists its content is replaced, but the playlist itself (Id, sharing settings, image) is kept and no duplicates are created. Renaming a playlist in the configuration renames the same Jellyfin playlist
- ✅ **Multilingual**: The configuration page and the scheduled task are available in **Catalan, English and Spanish**, and the language is detected automatically (see [Languages](#languages))
- ✅ **Scheduled task**: Regenerates the playlists automatically every day at 04:00 (configurable) or manually on demand

## Installation

### Requirements
- Jellyfin Server 12.x (the plugin must be built against Jellyfin 12; plugins built for 10.x or 11.x will not load)
- .NET 10 SDK to build it (the Jellyfin 12 server already ships its own runtime)

### Installation steps

1. **Download the plugin**
   - Download the `Jellyfin.Plugin.ClassicTV.dll` file from the releases section
   - Or compile from source code (see development section)

2. **Install in Jellyfin**
   - Stop the Jellyfin server
   - Create a `ClassicTV` folder inside the plugins folder and copy the `Jellyfin.Plugin.ClassicTV.dll` file into it (only the DLL; Jellyfin generates the rest). The plugins folder is:
     - **Windows**: `%PROGRAMDATA%\Jellyfin\Server\plugins`
     - **Linux**: `/var/lib/jellyfin/plugins`
     - **Docker**: Mount the plugins folder in the container
   - Restart the Jellyfin server

3. **Verify installation**
   - Go to **Dashboard** → **Plugins**
   - Look for "ClassicTV" in the list of installed plugins
   - The plugin should appear as "ClassicTV" in the "General" category

## Configuration

### Configure the playlists

The configuration page is part of the Jellyfin dashboard, so it is managed by an administrator for all users.

1. **Access configuration**
   - Go to **Dashboard** → **Plugins**
   - Find "ClassicTV" and click **Settings**

2. **Add playlists**
   - Click **+ Add playlist**; each playlist is a card with a **name**, an **owner user** and a checklist of all your series
   - Use the search box to find series (accents and capitals are ignored), **Selected only** to review the selection, **Select visible** and **Clear all** for bulk changes
   - A user can have as many playlists as wanted, as long as the names are different for that user

3. **Copy a selection**
   - At the bottom of each card, choose another playlist under **Copy series from** and click **Replace** (the card gets exactly those series) or **Add** (they are added to the ones already selected)

4. **Save**
   - **Save** saves the configuration; **Save and generate now** also starts the generation task
   - **Remove playlist** only removes it from the configuration; a playlist already created in Jellyfin is not deleted, it just stops being updated

Configurations from older versions (one list of series shared by a list of users) are migrated automatically on startup into one playlist per user, named "ClassicTV Playlist - [User]", so the playlists that already exist are updated instead of duplicated.

### Generate playlist

1. **Run the task manually**
   - Go to **Dashboard** → **Scheduled Tasks**
   - Find the ClassicTV task ("Generate ClassicTV playlists", or its translated name, see [Languages](#languages))
   - Click **Run Now**

2. **Verify the playlist**
   - Go to **My Library** → **Playlists** (as the owner user)
   - Look for the playlist with the name you configured
   - The playlist will contain mixed episodes from the series selected for it

### Automatic regeneration

The task has a default **daily trigger at 04:00**. Jellyfin only applies a task's default triggers when it has no saved triggers, so on an installation where the triggers of this task were already edited you may need to add it yourself:

- Go to **Dashboard** → **Scheduled Tasks** → the ClassicTV task
- Add, change or remove triggers (daily, weekly, every N hours, on startup)

Every run rebuilds the list from the episodes that are still unwatched, so what you have already watched disappears from the playlist. Because the order of the series is shuffled on every run, the interleaving of the remaining episodes can change between runs (the order inside each series never does).

## Languages

The plugin speaks **Catalan (ca), English (en) and Spanish (es)**, and chooses the language by itself; there is nothing to configure.

| Where | Which language is used |
|-------|------------------------|
| **Configuration page** (Dashboard → Plugins → ClassicTV) | The Jellyfin display language of the user who opens it (the `lang` of the web page). If the page does not expose it, the first supported language of the browser. |
| **Scheduled task** (name and description in Dashboard → Scheduled Tasks) | The **server** display language (the display-language setting under Dashboard → General), the same one Jellyfin uses to translate its own tasks. |

- Any other language (French, German...) falls back to **English**. If the Jellyfin language is not supported, the browser language is not used instead; English is.
- Regional variants are understood: `ca-ES`, `es-MX`, `es_419`, `en-GB`... Catalan, Spanish and English respectively.
- The language of the configuration page is detected every time the page is opened, so changing the Jellyfin language is enough.
- The default playlist name `ClassicTV Playlist - [User]` is **not** translated on purpose: it is the name used by playlists that already exist, and translating it would create duplicates.
- Log messages stay in English (as in most Jellyfin plugins), to make them easy to search for and report.
- To add another language: add a block to `TEXTS` in `Configuration/configPage.html` (same keys as the others), and a case in `Localizer.cs`.

## Usage

### How the algorithm works

The plugin mixes episodes using a **round-robin** algorithm:

1. **Sort episodes**: Each series keeps its internal order: numbered seasons ascending, then episodes without a season, and **specials (season 0) last**. Within a season, episodes are ordered by episode number (those without a number go last), then by premiere date and name
2. **Filter unwatched**: Only includes episodes that the user hasn't watched
3. **Alternating mix**: Takes one episode from each series in rotation. The order of the series is **shuffled randomly on every run**, so the playlist doesn't always start with the same series
4. **Example** (one possible result; the order of the series inside each round changes from run to run):
   ```
   Series A: Ep1, Ep2, Ep3, Ep4
   Series B: Ep1, Ep2
   Series C: Ep1, Ep2, Ep3

   Result: A1, B1, C1, A2, B2, C2, A3, C3, A4
   ```
5. **Same-name series**: Series are identified by their Id, not their name, so two series with the same name (e.g. a remake) are both included

### Playlist features

- **User-specific**: Each playlist belongs to one user, and only contains episodes that user hasn't watched
- **Unwatched episodes only**: Doesn't include already watched episodes
- **1000 episode limit**: To avoid performance issues
- **Round-robin order**: Maintains balance between series
- **Updated in place**: The plugin remembers the Jellyfin playlist created for each configured playlist (and falls back to the name, among the owner's own playlists). Its content is replaced; it keeps its Id, sharing settings and image, and is renamed if you changed the name in the configuration. There is a brief moment during the update where it is empty, and if an update fails midway the playlist stays empty until the next run. The playlist is only created if none exists, and left untouched when there are no unwatched episodes

## Development

### Compile from source code

1. **Clone the repository**
   ```bash
   git clone https://github.com/xtra25/Jellyfin-plugin-ClassicTV
   cd jellyfin-plugin-ClassicTV
   ```

2. **Development requirements**
   - .NET 10 SDK
   - Visual Studio 2022 or VS Code

3. **Compile**
   ```bash
   cd Jellyfin.Plugin.ClassicTV
   dotnet build
   ```

4. **Install**
   - Build with `dotnet build -c Release`; the DLL is generated in `bin/Release/net10.0/`
   - Copy `Jellyfin.Plugin.ClassicTV.dll` into a `ClassicTV` folder inside Jellyfin's plugins folder

### Project structure

```
Jellyfin.Plugin.ClassicTV/
├── Plugin.cs                    # Main plugin class
├── Configuration/
│   ├── PluginConfiguration.cs   # Plugin configuration (list of playlists)
│   ├── PlaylistDefinition.cs    # A playlist to generate + naming/migration rules (no Jellyfin dependencies)
│   └── configPage.html          # Web configuration page (ca/en/es texts + language detection)
├── EpisodeFetcher.cs            # Gets the unwatched episodes of each series
├── EpisodeOrdering.cs           # Episode ordering (no Jellyfin dependencies)
├── RoundRobinMixer.cs           # Round-robin mixing (no Jellyfin dependencies)
├── EpisodeMixer.cs              # Adapts the mixer to Jellyfin episodes
├── SeriesEpisodes.cs            # Episodes of one series (identified by Id)
├── GeneratePlaylistTask.cs      # Scheduled task to generate playlists
├── Localizer.cs                 # Task name/description in ca/en/es (no Jellyfin dependencies)
└── Jellyfin.Plugin.ClassicTV.csproj
Jellyfin.Plugin.ClassicTV.Tests/ # Tests for the ordering, mixing, playlist rules and languages
```

### Run the tests

The ordering, mixing, playlist-naming/migration and language-selection logic has no Jellyfin dependencies and has its own tests (no NuGet packages needed):

```bash
dotnet run --project Jellyfin.Plugin.ClassicTV.Tests
```

## Troubleshooting

### Configuration page doesn't appear
- Verify that the plugin is installed correctly
- Check Jellyfin logs for errors
- Make sure the DLL file is in the correct folder

### Playlist only has few episodes
- Verify that selected series have unwatched episodes
- Check that episodes aren't marked as watched
- Review logs to see how many episodes were found

### Error generating playlist
- Verify that selected series exist in your library
- Check that the playlist has an existing user and at least one series selected (otherwise it is skipped; the log says so)
- Check that you have permissions to create playlists
- Review Jellyfin logs for specific errors

### Playlist doesn't update
- If you see several playlists with the same name (created by older versions), the plugin updates the one it has registered (or the oldest by name) and leaves the others untouched; delete the extra ones by hand
- If you changed a name and a second playlist appeared, the playlist registered in the configuration could not be found in Jellyfin (for example it was deleted); the old one can be deleted by hand
- Manually run the ClassicTV task and check its trigger under **Scheduled Tasks**
- Verify that selected series haven't changed
- Check that unwatched episodes are available

## Logs

To diagnose problems, check Jellyfin logs. The plugin logs detailed information about:

- Selected series
- Episodes found per series
- Unwatched episodes per user
- Round-robin mixing process
- Playlist creation or update (with the playlist Id, so you can check it stays the same between runs)

## Contributing

1. Fork the repository
2. Create a branch for your feature (`git checkout -b feature/new-functionality`)
3. Commit your changes (`git commit -am 'Add new functionality'`)
4. Push to the branch (`git push origin feature/new-functionality`)
5. Create a Pull Request

## License

This project is licensed under the GNU General Public License v3.0 (GPLv3) - see the [LICENSE](LICENSE) file for details.

## Support

If you have problems or questions:

1. Check the [Troubleshooting](#troubleshooting) section
2. Search in [Issues](https://github.com/xtra25/Jellyfin-plugin-ClassicTV/issues)
3. Create a new issue with:
   - Jellyfin version
   - Operating system
   - Relevant logs
   - Detailed problem description

---

**Enjoy your mixed classic TV experience!** 📺✨
