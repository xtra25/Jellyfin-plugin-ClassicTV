using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.ClassicTV.Configuration
{
    /// <summary>
    /// Una playlist que el plugin ha de generar: nom, usuari propietari i la seva pròpia selecció de sèries.
    /// No depèn de Jellyfin, així que es pot provar a part.
    /// </summary>
    public class PlaylistDefinition
    {
        /// <summary>
        /// Gets or sets the identifier of this definition (used by the configuration page; unrelated to the Jellyfin playlist id).
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Gets or sets the playlist name. Empty means the default name "ClassicTV Playlist - {user}".
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the id of the Jellyfin user that owns the playlist.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ids of the series mixed in this playlist.
        /// </summary>
        public List<string> SeriesIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the id of the Jellyfin playlist created for this definition (filled in by the task).
        /// It lets a playlist be renamed without losing track of it.
        /// </summary>
        public string JellyfinPlaylistId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Regles pures sobre les definicions de playlist.
    /// </summary>
    public static class PlaylistRules
    {
        /// <summary>
        /// Prefix of the default playlist name (the one used before several playlists per user existed).
        /// </summary>
        public const string DefaultNamePrefix = "ClassicTV Playlist - ";

        /// <summary>
        /// Name actually used for a playlist: the configured one, or the default one when it is empty.
        /// </summary>
        /// <param name="name">The configured name (may be empty).</param>
        /// <param name="username">The owner's user name.</param>
        /// <returns>The playlist name.</returns>
        public static string EffectiveName(string? name, string username) =>
            string.IsNullOrWhiteSpace(name) ? DefaultNamePrefix + username : name.Trim();

        /// <summary>
        /// Whether two playlist names are the same for Jellyfin lookups (ignores case and surrounding spaces).
        /// </summary>
        /// <param name="a">First name.</param>
        /// <param name="b">Second name.</param>
        /// <returns>True when they match.</returns>
        public static bool SameName(string? a, string? b) =>
            string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Converts the old configuration (one series list shared by a list of users) into one playlist per user,
        /// all with the default name so the playlists that already exist keep being updated.
        /// </summary>
        /// <param name="userIds">Old list of user ids.</param>
        /// <param name="seriesIds">Old list of series ids.</param>
        /// <returns>One definition per distinct user; empty if there are no users or no series.</returns>
        public static List<PlaylistDefinition> FromLegacy(IEnumerable<string>? userIds, IEnumerable<string>? seriesIds)
        {
            var series = (seriesIds ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (series.Count == 0)
            {
                return new List<PlaylistDefinition>();
            }

            return (userIds ?? Array.Empty<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(u => new PlaylistDefinition { UserId = u, SeriesIds = new List<string>(series) })
                .ToList();
        }
    }
}
