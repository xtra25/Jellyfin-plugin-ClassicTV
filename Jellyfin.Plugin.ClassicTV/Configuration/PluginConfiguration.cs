using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ClassicTV.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Playlists = new List<PlaylistDefinition>();
        SeriesIds = new List<string>();
        UserIds = new List<string>();
    }

    /// <summary>
    /// Gets or sets the playlists to generate (each with its own owner and series).
    /// </summary>
    public List<PlaylistDefinition> Playlists { get; set; }

    /// <summary>
    /// Gets or sets the old shared list of series. Only read to migrate old configurations; it is emptied once migrated.
    /// </summary>
    public List<string> SeriesIds { get; set; }

    /// <summary>
    /// Gets or sets the old list of users. Only read to migrate old configurations; it is emptied once migrated.
    /// </summary>
    public List<string> UserIds { get; set; }
}
