using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
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

            if (config.UserIds.Count == 0)
            {
                _logger.LogWarning("No users configured");
                return;
            }

            _logger.LogInformation("GeneratePlaylistTask: Starting playlist generation for {SeriesCount} series and {UserCount} users", config.SeriesIds.Count, config.UserIds.Count);

            var fetcher = new EpisodeFetcher(_libraryManager);
            var processed = 0;

            // Crear playlist solo para los usuarios seleccionados
            foreach (var userId in config.UserIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await GenerateForUserAsync(fetcher, config.SeriesIds, userId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GeneratePlaylistTask: Error creating playlist for user {UserId}", userId);
                }

                processed++;
                progress.Report(100.0 * processed / config.UserIds.Count);
            }
        }

        private async Task GenerateForUserAsync(EpisodeFetcher fetcher, IEnumerable<string> seriesIds, string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("GeneratePlaylistTask: Invalid user ID format {UserId}, skipping", userId);
                return;
            }

            // GetUserById se invoca por reflexión para no depender del tipo User (cambia de namespace entre versiones).
            var getUserByIdMethod = _userManager.GetType().GetMethod("GetUserById", new[] { typeof(Guid) });
            var user = getUserByIdMethod?.Invoke(_userManager, new object[] { userGuid });

            if (user == null)
            {
                _logger.LogWarning("GeneratePlaylistTask: Could not find user {UserId}, skipping", userId);
                return;
            }

            var username = user.GetType().GetProperty("Username")?.GetValue(user)?.ToString() ?? userId;
            var userIdForPlaylist = user.GetType().GetProperty("Id")?.GetValue(user) is Guid extractedId ? extractedId : userGuid;

            _logger.LogInformation("GeneratePlaylistTask: Processing user {Username}", username);

            var episodesBySeries = fetcher.GetUnwatchedEpisodesBySeries(seriesIds, user);
            foreach (var series in episodesBySeries)
            {
                _logger.LogInformation("GeneratePlaylistTask: Series '{SeriesName}' has {EpisodeCount} unwatched episodes for user {Username}", series.SeriesName, series.Episodes.Count, username);
            }

            var mixedEpisodes = EpisodeMixer.MixEpisodesRoundRobin(episodesBySeries);

            _logger.LogInformation("GeneratePlaylistTask: Processing {SeriesCount} series for user {Username} with total {TotalEpisodes} unwatched episodes",
                episodesBySeries.Count, username, episodesBySeries.Sum(s => s.Episodes.Count));
            _logger.LogInformation("GeneratePlaylistTask: Mixed playlist for user {Username} will contain {EpisodeCount} episodes", username, mixedEpisodes.Count);

            var playlistName = $"ClassicTV Playlist - {username}";

            if (mixedEpisodes.Count == 0)
            {
                _logger.LogInformation("GeneratePlaylistTask: No unwatched episodes for user {Username}; skipping playlist creation (an existing playlist '{PlaylistName}' is left unchanged)", username, playlistName);
                return;
            }

            DeleteExistingPlaylist(userIdForPlaylist, username, playlistName);

            _logger.LogInformation("GeneratePlaylistTask: Creating new playlist '{PlaylistName}' for user {Username}", playlistName, username);

            var request = new PlaylistCreationRequest
            {
                Name = playlistName,
                UserId = userIdForPlaylist,
                ItemIdList = mixedEpisodes.Select(e => e.Id).ToList()
            };

            await _playlistManager.CreatePlaylist(request).ConfigureAwait(false);
            _logger.LogInformation("GeneratePlaylistTask: Playlist created for user {Username} with {EpisodeCount} episodes", username, mixedEpisodes.Count);
        }

        // Busca y elimina la playlist existente (si la hay) para no acumular duplicados.
        private void DeleteExistingPlaylist(Guid userId, string username, string playlistName)
        {
            try
            {
                // Lambda amb tipus inferit: evita nombrar el tipus Playlist (varia entre versions de Jellyfin).
                var findPlaylist = () => _playlistManager.GetPlaylists(userId)
                    .FirstOrDefault(p => string.Equals(p.Name?.Trim(), playlistName.Trim(), StringComparison.OrdinalIgnoreCase));

                var existingPlaylist = findPlaylist();

                if (existingPlaylist == null)
                {
                    _logger.LogDebug("No existing playlist named '{PlaylistName}' found for user {Username}", playlistName, username);
                    return;
                }

                _logger.LogInformation("Found existing playlist '{PlaylistName}' for user {Username}, deleting it", playlistName, username);

                try
                {
                    _libraryManager.DeleteItem(existingPlaylist, new DeleteOptions());
                    _logger.LogInformation("Deleted playlist '{PlaylistName}' for user {Username}", playlistName, username);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting playlist '{PlaylistName}' for user {Username}", playlistName, username);
                }

                if (findPlaylist() != null)
                {
                    _logger.LogWarning("Playlist '{PlaylistName}' still exists after deletion for user {Username}", playlistName, username);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while checking/deleting existing playlist for user {Username}", username);
            }
        }
    }
}
