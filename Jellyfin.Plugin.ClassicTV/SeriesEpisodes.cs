using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.ClassicTV
{
    /// <summary>
    /// Episodis (ja ordenats) d'una sèrie. S'identifica per Id i no per nom, perquè dues sèries
    /// poden dir-se igual (per exemple, un remake) i no s'han de trepitjar.
    /// </summary>
    /// <param name="SeriesId">Id de la sèrie.</param>
    /// <param name="SeriesName">Nom de la sèrie (només per als logs).</param>
    /// <param name="Episodes">Episodis en ordre de visionat.</param>
    public sealed record SeriesEpisodes(Guid SeriesId, string SeriesName, IReadOnlyList<Episode> Episodes);
}
