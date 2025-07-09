using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Logging;


namespace Jellyfin.Plugin.ClassicTV
{
    public class GeneratePlaylistTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IPlaylistManager _playlistManager;
        private readonly ILogger<GeneratePlaylistTask> _logger;

        public GeneratePlaylistTask(
            ILibraryManager libraryManager,
            IUserManager userManager,
            IPlaylistManager playlistManager,
            ILogger<GeneratePlaylistTask> logger)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _playlistManager = playlistManager;
            _logger = logger;
        }

        public string Name => "Generar playlist ClassicTV";
        public string Description => "Mezcla episodios de series seleccionadas y crea una playlist ordenada round-robin.";
        public string Category => "ClassicTV";


        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Solo ejecución manual desde el panel de tareas
            return Array.Empty<TaskTriggerInfo>();
        }

        public string Key => "ClassicTV_PlaylistGenerator";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;

            if (config == null || config.SeriesIds.Count == 0)
            {
                _logger.LogWarning("No series configured");
                return;
            }

            _logger.LogInformation("GeneratePlaylistTask: Starting playlist generation for {SeriesCount} series", config.SeriesIds.Count);

            var fetcher = new EpisodeFetcher(_libraryManager);

            // Crear playlist para cada usuario
            foreach (var user in _userManager.Users)
            {
                try
                {
                    _logger.LogInformation("GeneratePlaylistTask: Processing user {Username}", user.Username);

                    var episodesBySeries = fetcher.GetUnwatchedEpisodesBySeries(config.SeriesIds, user);
                    var mixedEpisodes = EpisodeMixer.MixEpisodesRoundRobin(episodesBySeries);

                    _logger.LogInformation("GeneratePlaylistTask: Processing {SeriesCount} series for user {Username} with total {TotalEpisodes} unwatched episodes",
                        episodesBySeries.Count, user.Username, episodesBySeries.Values.Sum(e => e.Count));
                    _logger.LogInformation("GeneratePlaylistTask: Mixed playlist for user {Username} will contain {EpisodeCount} episodes", user.Username, mixedEpisodes.Count);

                    if (mixedEpisodes.Count == 0)
                    {
                        _logger.LogInformation("GeneratePlaylistTask: No unwatched episodes for user {Username}, skipping playlist creation", user.Username);
                        continue;
                    }

                    var request = new PlaylistCreationRequest
                    {
                        Name = $"ClassicTV Playlist - {user.Username}",
                        UserId = user.Id,
                        ItemIdList = mixedEpisodes.Select(e => e.Id).ToList()
                    };

                    await _playlistManager.CreatePlaylist(request);
                    _logger.LogInformation("GeneratePlaylistTask: Playlist created for user {Username} with {EpisodeCount} episodes", user.Username, mixedEpisodes.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GeneratePlaylistTask: Error creating playlist for user {Username}", user.Username);
                }
            }
        }




    }
}
