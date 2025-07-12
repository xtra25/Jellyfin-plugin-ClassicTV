using MediaBrowser.Model.Plugins;
using System.Collections.Generic;


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
        SeriesIds = new List<string>();
        UserIds = new List<string>();
    }

    /// <summary>
    /// Gets or sets the list of selected series IDs.
    /// </summary>
    public List<string> SeriesIds { get; set; }

    /// <summary>
    /// Gets or sets the list of selected user IDs.
    /// </summary>
    public List<string> UserIds { get; set; }
}
