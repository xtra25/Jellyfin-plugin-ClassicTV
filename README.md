# ClassicTV Plugin for Jellyfin

A Jellyfin plugin that allows mixing episodes from multiple selected series into an ordered playlist, maintaining the internal order of each series using a round-robin algorithm.

## Features

- ✅ **Multiple series selection**: Choose several series from the configuration interface
- ✅ **Round-robin algorithm**: Mixes episodes alternating between series
- ✅ **Respect internal order**: Maintains the chronological order of each series
- ✅ **User filtering**: Only includes episodes not watched by each user
- ✅ **Custom playlists and overwriting**: Creates a unique playlist for each user and overwrites it if it already exists (no duplicates are created)
- ✅ **Scheduled task**: Generates playlists automatically or manually

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
   - Copy the `Jellyfin.Plugin.ClassicTV.dll` file to the plugins folder:
     - **Windows**: `%PROGRAMDATA%\Jellyfin\Server\plugins`
     - **Linux**: `/var/lib/jellyfin/plugins`
     - **Docker**: Mount the plugins folder in the container
   - Restart the Jellyfin server

3. **Verify installation**
   - Go to **Dashboard** → **Plugins**
   - Look for "ClassicTV" in the list of installed plugins
   - The plugin should appear as "ClassicTV" in the "General" category

## Configuration

### Configure selected series

1. **Access configuration**
   - Go to **Dashboard** → **Plugins**
   - Find "ClassicTV" and click **Settings**

2. **Select series**
   - On the configuration page, you'll see a multiple selector with all your series
   - Hold **Ctrl** (Windows) or **Cmd** (Mac) to select multiple series
   - You can also use **Shift** to select ranges
   - Click **Save** to confirm the selection

### Generate playlist

1. **Run the task manually**
   - Go to **Dashboard** → **Scheduled Tasks**
   - Find "Generar playlist ClassicTV"
   - Click **Run Now**

2. **Verify the playlist**
   - Go to **My Library** → **Playlists**
   - Look for the playlist "ClassicTV Playlist - [YourUser]"
   - The playlist will contain mixed episodes from the selected series

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

- **User-specific**: Each user has their own playlist
- **Unwatched episodes only**: Doesn't include already watched episodes
- **1000 episode limit**: To avoid performance issues
- **Round-robin order**: Maintains balance between series
- **Overwrites existing playlists**: If a playlist with the same name already exists for the user, it's deleted and a new one is created (no duplicates accumulate)

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
   - The DLL file is generated in `bin/Debug/net10.0/`
   - Copy `Jellyfin.Plugin.ClassicTV.dll` to Jellyfin's plugins folder

### Project structure

```
Jellyfin.Plugin.ClassicTV/
├── Plugin.cs                    # Main plugin class
├── Configuration/
│   ├── PluginConfiguration.cs   # Plugin configuration
│   └── configPage.html          # Web configuration page
├── EpisodeFetcher.cs            # Gets the unwatched episodes of each series
├── EpisodeOrdering.cs           # Episode ordering (no Jellyfin dependencies)
├── RoundRobinMixer.cs           # Round-robin mixing (no Jellyfin dependencies)
├── EpisodeMixer.cs              # Adapts the mixer to Jellyfin episodes
├── SeriesEpisodes.cs            # Episodes of one series (identified by Id)
├── GeneratePlaylistTask.cs      # Scheduled task to generate playlists
└── Jellyfin.Plugin.ClassicTV.csproj
Jellyfin.Plugin.ClassicTV.Tests/ # Tests for the ordering and mixing logic
```

### Run the tests

The ordering and mixing logic has no Jellyfin dependencies and has its own tests (no NuGet packages needed):

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
- Check that you have permissions to create playlists
- Review Jellyfin logs for specific errors

### Playlist doesn't update
- If you see multiple playlists with the same name, update the plugin: now the playlist is overwritten correctly and no duplicates are created
- Manually run the "Generar playlist ClassicTV" task
- Verify that selected series haven't changed
- Check that unwatched episodes are available

## Logs

To diagnose problems, check Jellyfin logs. The plugin logs detailed information about:

- Selected series
- Episodes found per series
- Unwatched episodes per user
- Round-robin mixing process
- Playlist creation

## Contributing

1. Fork the repository
2. Create a branch for your feature (`git checkout -b feature/new-functionality`)
3. Commit your changes (`git commit -am 'Add new functionality'`)
4. Push to the branch (`git push origin feature/new-functionality`)
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

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
