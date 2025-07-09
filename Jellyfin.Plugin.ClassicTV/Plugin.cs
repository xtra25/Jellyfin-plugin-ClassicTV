using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.ClassicTV.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ClassicTV;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<Plugin> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{Plugin}"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        _logger = logger;
        _logger.LogInformation("ClassicTV Plugin constructor called");
        Instance = this;
        _logger.LogInformation("ClassicTV Plugin instance set");
    }

    /// <inheritdoc />
    public override string Name => "ClassicTV";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("eb5d7894-8eef-4b36-aa6f-5d124e828ce1");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        _logger.LogInformation("ClassicTV Plugin GetPages() called");

        try
        {
            var resourcePath = "Jellyfin.Plugin.ClassicTV.Configuration.configPage.html";
            _logger.LogInformation("Registering configuration page with resource path: {ResourcePath}", resourcePath);

            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = resourcePath
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPages() method");
            return new PluginPageInfo[0];
        }
    }
}
