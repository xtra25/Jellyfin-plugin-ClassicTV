using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.ClassicTV
{
    public static class EpisodeMixer
    {
        /// <summary>
        /// Mescla els episodis de diverses sèries en round-robin mantenint l'ordre intern de cada sèrie.
        /// Limita el nombre total d'episodis per evitar problemes de rendiment.
        /// </summary>
        /// <param name="series">Episodis de cada sèrie, ja ordenats.</param>
        /// <param name="maxEpisodes">Nombre màxim d'episodis del resultat.</param>
        /// <returns>Llista mesclada.</returns>
        public static List<Episode> MixEpisodesRoundRobin(IEnumerable<SeriesEpisodes> series, int maxEpisodes = RoundRobinMixer.DefaultMaxItems)
        {
            return RoundRobinMixer.Mix(series.Select(s => s.Episodes), maxEpisodes);
        }
    }
}
