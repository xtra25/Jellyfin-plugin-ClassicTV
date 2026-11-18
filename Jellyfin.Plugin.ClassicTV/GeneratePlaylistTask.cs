using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ClassicTV.Configuration;
using MediaBrowser.Controller.Configuration;
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
        private readonly IServerConfigurationManager _serverConfiguration;
        private readonly ILogger<GeneratePlaylistTask> _logger;

        public GeneratePlaylistTask(
            ILibraryManager libraryManager,
            IUserManager userManager,
            IPlaylistManager playlistManager,
            IServerConfigurationManager serverConfiguration,
            ILogger<GeneratePlaylistTask> logger)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _playlistManager = playlistManager;
            _serverConfiguration = serverConfiguration;
            _logger = logger;
        }

        // Nombre y descripción en el idioma de la interfaz del servidor (catalán, castellano o inglés).
        public string Name => Localizer.TaskName(ServerCulture());
        public string Description => Localizer.TaskDescription(ServerCulture());
        public string Category => "ClassicTV";

        private string? ServerCulture()
        {
            try
            {
                return _serverConfiguration.Configuration.UICulture;
            }
            catch (Exception)
            {
                return CultureInfo.CurrentUICulture.Name;
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Regeneración diaria a las 04:00. Jellyfin solo aplica este valor si la tarea no tiene
            // triggers guardados; se puede cambiar o quitar en Panel -> Tareas programadas.
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
            };
        }

        public string Key => "ClassicTV_PlaylistGenerator";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;

            if (config == null || config.Playlists.Count == 0)
            {
                _logger.LogWarning("No playlists configured");
                return;
            }

            _logger.LogInformation("GeneratePlaylistTask: Starting generation of {PlaylistCount} configured playlists", config.Playlists.Count);

            var definitions = config.Playlists.ToList();
            var fetcher = new EpisodeFetcher(_libraryManager);
            var claimedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var claimedPlaylistIds = new HashSet<Guid>();
            var configChanged = false;
            var processed = 0;

            foreach (var definition in definitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (await GenerateForPlaylistAsync(fetcher, definition, definitions, claimedKeys, claimedPlaylistIds).ConfigureAwait(false))
                    {
                        configChanged = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GeneratePlaylistTask: Error generating playlist '{PlaylistName}' for user {UserId}", definition.Name, definition.UserId);
                }

                processed++;
                progress.Report(100.0 * processed / definitions.Count);
            }

            if (configChanged)
            {
                try
                {
                    // Guarda los Id de las playlists de Jellyfin para poder renombrarlas sin perderles la pista.
                    Plugin.Instance?.SaveConfiguration();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GeneratePlaylistTask: Could not save the playlist ids in the configuration; playlists will be found by name next time");
                }
            }
        }

        // Devuelve true si se ha cambiado el Id de la playlist de Jellyfin guardado en la definición.
        private async Task<bool> GenerateForPlaylistAsync(
            EpisodeFetcher fetcher,
            PlaylistDefinition definition,
            IReadOnlyCollection<PlaylistDefinition> allDefinitions,
            HashSet<string> claimedKeys,
            HashSet<Guid> claimedPlaylistIds)
        {
            if (!Guid.TryParse(definition.UserId, out var userGuid))
            {
                _logger.LogWarning("GeneratePlaylistTask: Invalid user ID format {UserId} in playlist '{PlaylistName}', skipping", definition.UserId, definition.Name);
                return false;
            }

            // GetUserById se invoca por reflexión para no depender del tipo User (cambia de namespace entre versiones).
            var getUserByIdMethod = _userManager.GetType().GetMethod("GetUserById", new[] { typeof(Guid) });
            var user = getUserByIdMethod?.Invoke(_userManager, new object[] { userGuid });

            if (user == null)
            {
                _logger.LogWarning("GeneratePlaylistTask: Could not find user {UserId} for playlist '{PlaylistName}', skipping", definition.UserId, definition.Name);
                return false;
            }

            var username = user.GetType().GetProperty("Username")?.GetValue(user)?.ToString() ?? definition.UserId;
            var ownerId = user.GetType().GetProperty("Id")?.GetValue(user) is Guid extractedId ? extractedId : userGuid;
            var playlistName = PlaylistRules.EffectiveName(definition.Name, username);

            // Dos definiciones con el mismo nombre para el mismo usuario acabarían pisándose entre sí.
            if (!claimedKeys.Add(ownerId.ToString("N") + "|" + playlistName.ToLowerInvariant()))
            {
                _logger.LogWarning("GeneratePlaylistTask: Playlist '{PlaylistName}' for user {Username} is configured more than once; skipping the duplicate", playlistName, username);
                return false;
            }

            if (definition.SeriesIds.Count == 0)
            {
                _logger.LogInformation("GeneratePlaylistTask: Playlist '{PlaylistName}' for user {Username} has no series selected; skipping", playlistName, username);
                return false;
            }

            _logger.LogInformation("GeneratePlaylistTask: Processing playlist '{PlaylistName}' for user {Username} ({SeriesCount} series selected)", playlistName, username, definition.SeriesIds.Count);

            var episodesBySeries = fetcher.GetUnwatchedEpisodesBySeries(definition.SeriesIds, user);
            foreach (var series in episodesBySeries)
            {
                _logger.LogInformation("GeneratePlaylistTask: Series '{SeriesName}' has {EpisodeCount} unwatched episodes for user {Username}", series.SeriesName, series.Episodes.Count, username);
            }

            var mixedEpisodes = EpisodeMixer.MixEpisodesRoundRobin(episodesBySeries);

            _logger.LogInformation("GeneratePlaylistTask: Playlist '{PlaylistName}': {SeriesCount} series, {TotalEpisodes} unwatched episodes, {EpisodeCount} in the mixed playlist",
                playlistName, episodesBySeries.Count, episodesBySeries.Sum(s => s.Episodes.Count), mixedEpisodes.Count);

            if (mixedEpisodes.Count == 0)
            {
                _logger.LogInformation("GeneratePlaylistTask: No unwatched episodes for playlist '{PlaylistName}' of user {Username}; an existing playlist is left unchanged", playlistName, username);
                return false;
            }

            var itemIds = mixedEpisodes.Select(e => e.Id).ToList();
            var existing = FindExistingPlaylist(ownerId, definition, playlistName, allDefinitions, claimedPlaylistIds);

            if (existing == null)
            {
                _logger.LogInformation("GeneratePlaylistTask: Creating new playlist '{PlaylistName}' for user {Username}", playlistName, username);

                var result = await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
                {
                    Name = playlistName,
                    UserId = ownerId,
                    ItemIdList = itemIds
                }).ConfigureAwait(false);

                _logger.LogInformation("GeneratePlaylistTask: Playlist {PlaylistId} created for user {Username} with {EpisodeCount} episodes", result.Id, username, itemIds.Count);

                if (Guid.TryParse(result.Id, out var createdId))
                {
                    claimedPlaylistIds.Add(createdId);
                }

                return StoreJellyfinId(definition, result.Id);
            }

            claimedPlaylistIds.Add(existing.Id);

            _logger.LogInformation("GeneratePlaylistTask: Updating existing playlist '{PlaylistName}' ({PlaylistId}) for user {Username} in place", playlistName, existing.Id, username);

            // Con Ids informado, UpdatePlaylist vacía la playlist y la rellena con esa lista (Jellyfin 12.0 y 12.4:
            // PlaylistManager.UpdatePlaylist). Users y Public a null = no se tocan, así que se conservan el Id,
            // los permisos de compartición y la imagen.
            var update = new PlaylistUpdateRequest
            {
                Id = existing.Id,
                UserId = ownerId,
                Ids = itemIds
            };

            if (!string.Equals(existing.Name, playlistName, StringComparison.Ordinal))
            {
                _logger.LogInformation("GeneratePlaylistTask: Renaming playlist {PlaylistId} from '{OldName}' to '{PlaylistName}'", existing.Id, existing.Name, playlistName);
                update.Name = playlistName;
            }

            await _playlistManager.UpdatePlaylist(update).ConfigureAwait(false);

            _logger.LogInformation("GeneratePlaylistTask: Playlist {PlaylistId} updated for user {Username} with {EpisodeCount} episodes", existing.Id, username, itemIds.Count);

            return StoreJellyfinId(definition, existing.Id.ToString("N"));
        }

        private static bool StoreJellyfinId(PlaylistDefinition definition, string jellyfinPlaylistId)
        {
            if (Guid.TryParse(definition.JellyfinPlaylistId, out var current)
                && Guid.TryParse(jellyfinPlaylistId, out var created)
                && current == created)
            {
                return false;
            }

            definition.JellyfinPlaylistId = jellyfinPlaylistId;
            return true;
        }

        // Busca la playlist de Jellyfin de esta definición: primero por el Id guardado (sobrevive a los cambios de nombre)
        // y, si no, por nombre entre las playlists propias del usuario (la más antigua primero).
        // Se descartan las ya usadas en esta ejecución y las guardadas por otras definiciones.
        // Si la consulta falla, la excepción se propaga: crear otra playlist a ciegas generaría duplicados.
        private Playlist? FindExistingPlaylist(
            Guid ownerId,
            PlaylistDefinition definition,
            string playlistName,
            IReadOnlyCollection<PlaylistDefinition> allDefinitions,
            HashSet<Guid> claimedPlaylistIds)
        {
            var reservedByOthers = allDefinitions
                .Where(d => !ReferenceEquals(d, definition) && Guid.TryParse(d.JellyfinPlaylistId, out _))
                .Select(d => Guid.Parse(d.JellyfinPlaylistId))
                .ToHashSet();

            var candidates = _playlistManager.GetPlaylists(ownerId)
                .Where(p => p.OwnerUserId == ownerId && !claimedPlaylistIds.Contains(p.Id))
                .ToList();

            if (Guid.TryParse(definition.JellyfinPlaylistId, out var storedId))
            {
                var byId = candidates.FirstOrDefault(p => p.Id == storedId);
                if (byId != null)
                {
                    return byId;
                }
            }

            var byName = candidates
                .Where(p => !reservedByOthers.Contains(p.Id) && PlaylistRules.SameName(p.Name, playlistName))
                .OrderBy(p => p.DateCreated)
                .ToList();

            if (byName.Count > 1)
            {
                _logger.LogWarning("GeneratePlaylistTask: Found {PlaylistCount} playlists named '{PlaylistName}'; updating the oldest one and leaving the others untouched", byName.Count, playlistName);
            }

            return byName.FirstOrDefault();
        }
    }
}
