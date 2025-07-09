using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Tasks;
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
            return new[] { new TaskTriggerInfo { Type = TaskTriggerType.Manual } };
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

            var user = _userManager.Users.FirstOrDefault();
            if (user == null || mixedEpisodes.Count == 0)
            {
                _logger.LogWarning("Cannot create playlist: no user or episodes.");
                return;
            }

            await _playlistManager.CreatePlaylistAsync(user, "ClassicTV Playlist", mixedEpisodes.Cast<BaseItem>().ToList());
            _logger.LogInformation("Playlist created with {0} episodes", mixedEpisodes.Count);


        }




    }
}
