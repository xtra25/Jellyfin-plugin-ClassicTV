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
            var episodesBySeries = fetcher.GetOrderedEpisodesBySeries(config.SeriesIds);
            var mixedEpisodes = EpisodeMixer.MixEpisodesRoundRobin(episodesBySeries);

            _logger.LogInformation("Processing {0} series with total {1} episodes", episodesBySeries.Count, episodesBySeries.Values.Sum(e => e.Count));
            _logger.LogInformation("Mixed playlist will contain {0} episodes", mixedEpisodes.Count);

            var user = _userManager.Users.FirstOrDefault();
            if (user == null || mixedEpisodes.Count == 0)
            {
                _logger.LogWarning("Cannot create playlist: no user or episodes.");
                return;
            }

            var request = new PlaylistCreationRequest
            {
                Name = "ClassicTV Playlist",
                UserId = user.Id,
                ItemIdList = mixedEpisodes.Select(e => e.Id).ToList()
            };

            await _playlistManager.CreatePlaylist(request);
            _logger.LogInformation("Playlist created with {0} episodes", mixedEpisodes.Count);
        }




    }
}
