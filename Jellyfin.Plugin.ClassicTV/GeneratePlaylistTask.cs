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

            var fetcher = new EpisodeFetcher(_libraryManager);

            // Crear playlist para cada usuario
            foreach (var user in _userManager.Users)
            {
                try
                {
                    var episodesBySeries = fetcher.GetUnwatchedEpisodesBySeries(config.SeriesIds, user);
                    var mixedEpisodes = EpisodeMixer.MixEpisodesRoundRobin(episodesBySeries);

                    _logger.LogInformation("Processing {0} series for user {1} with total {2} unwatched episodes",
                        episodesBySeries.Count, user.Username, episodesBySeries.Values.Sum(e => e.Count));
                    _logger.LogInformation("Mixed playlist for user {0} will contain {1} episodes", user.Username, mixedEpisodes.Count);

                    if (mixedEpisodes.Count == 0)
                    {
                        _logger.LogInformation("No unwatched episodes for user {0}, skipping playlist creation", user.Username);
                        continue;
                    }

                    var request = new PlaylistCreationRequest
                    {
                        Name = $"ClassicTV Playlist - {user.Username}",
                        UserId = user.Id,
                        ItemIdList = mixedEpisodes.Select(e => e.Id).ToList()
                    };

                    await _playlistManager.CreatePlaylist(request);
                    _logger.LogInformation("Playlist created for user {0} with {1} episodes", user.Username, mixedEpisodes.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating playlist for user {0}", user.Username);
                }
            }
        }




    }
}
