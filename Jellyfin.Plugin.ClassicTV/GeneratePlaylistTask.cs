using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Querying;
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

            // Crear playlist solo para los usuarios seleccionados
            foreach (var userId in config.UserIds)
            {
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    _logger.LogWarning("GeneratePlaylistTask: Invalid user ID format {UserId}, skipping", userId);
                    continue;
                }

                string username = userId;
                Guid userIdForPlaylist = userGuid;
                object? user = null;

                try
                {
                    var getUserByIdMethod = _userManager.GetType().GetMethod("GetUserById", new[] { typeof(Guid) });
                    if (getUserByIdMethod != null)
                    {
                        user = getUserByIdMethod.Invoke(_userManager, new object[] { userGuid });
                    }

                    if (user == null)
                    {
                        _logger.LogWarning("GeneratePlaylistTask: Could not find user {UserId}, skipping", userId);
                        continue;
                    }

                    var usernameProp = user.GetType().GetProperty("Username");
                    if (usernameProp != null)
                    {
                        username = usernameProp.GetValue(user)?.ToString() ?? username;
                    }

                    var idProp = user.GetType().GetProperty("Id");
                    if (idProp != null && idProp.GetValue(user) is Guid extractedId)
                    {
                        userIdForPlaylist = extractedId;
                    }

                    _logger.LogInformation("GeneratePlaylistTask: Processing user {Username}", username);

                    var episodesBySeries = fetcher.GetUnwatchedEpisodesBySeries(config.SeriesIds, user, userIdForPlaylist);
                    var mixedEpisodes = EpisodeMixer.MixEpisodesRoundRobin(episodesBySeries);

                    _logger.LogInformation("GeneratePlaylistTask: Processing {SeriesCount} series for user {Username} with total {TotalEpisodes} unwatched episodes",
                        episodesBySeries.Count, username, episodesBySeries.Values.Sum(e => e.Count));
                    _logger.LogInformation("GeneratePlaylistTask: Mixed playlist for user {Username} will contain {EpisodeCount} episodes", username, mixedEpisodes.Count);

                    if (mixedEpisodes.Count == 0)
                    {
                        _logger.LogInformation("GeneratePlaylistTask: No unwatched episodes for user {Username}, skipping playlist creation", username);
                        continue;
                    }

                    var playlistName = $"ClassicTV Playlist - {username}";

                    // Buscar y eliminar playlist existente si existe
                    try
                    {
                        var userPlaylists = _playlistManager.GetPlaylists(userIdForPlaylist);
                        foreach (var p in userPlaylists)
                        {
                            _logger.LogDebug("Found playlist: '{PlaylistName}' for user {Username}", p.Name, username);
                        }
                        var existingPlaylist = userPlaylists
                            .FirstOrDefault(p => string.Equals(p.Name?.Trim(), playlistName.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (existingPlaylist != null)
                        {
                            _logger.LogInformation("Found existing playlist '{PlaylistName}' for user {Username}, deleting it", playlistName, username);
                            try
                            {
                                _libraryManager.DeleteItem(existingPlaylist, new MediaBrowser.Controller.Library.DeleteOptions());
                                _logger.LogInformation("Deleted playlist '{PlaylistName}' for user {Username}", playlistName, username);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error deleting playlist '{PlaylistName}' for user {Username}", playlistName, username);
                            }
                            // Confirmar si sigue existiendo
                            var afterDelete = _playlistManager.GetPlaylists(userIdForPlaylist)
                                .FirstOrDefault(p => string.Equals(p.Name?.Trim(), playlistName.Trim(), StringComparison.OrdinalIgnoreCase));
                            if (afterDelete != null)
                            {
                                _logger.LogWarning("Playlist '{PlaylistName}' still exists after deletion for user {Username}", playlistName, username);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("No existing playlist named '{PlaylistName}' found for user {Username}", playlistName, username);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error while checking/deleting existing playlist for user {Username}", username);
                    }

                    _logger.LogInformation("GeneratePlaylistTask: Creating new playlist '{PlaylistName}' for user {Username}", playlistName, username);

                    var request = new PlaylistCreationRequest
                    {
                        Name = playlistName,
                        UserId = userIdForPlaylist,
                        ItemIdList = mixedEpisodes.Select(e => e.Id).ToList()
                    };

                    await _playlistManager.CreatePlaylist(request);
                    _logger.LogInformation("GeneratePlaylistTask: Playlist created for user {Username} with {EpisodeCount} episodes", username, mixedEpisodes.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GeneratePlaylistTask: Error creating playlist for user {UserId}", userId);
                }
            }
        }




    }
}
