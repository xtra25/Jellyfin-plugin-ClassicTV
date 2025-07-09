using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.ClassicTV
{
    public static class EpisodeMixer
    {
        /// <summary>
        /// Mezcla los episodios de varias series en orden interno usando round-robin.
        /// Limita el número total de episodios para evitar problemas de memoria.
        /// </summary>
        public static List<Episode> MixEpisodesRoundRobin(Dictionary<string, List<Episode>> seriesEpisodes)
        {
            const int maxEpisodes = 1000; // Límite máximo de episodios
            var result = new List<Episode>();
            var enumerators = seriesEpisodes.Values
                .Select(list => list.GetEnumerator())
                .ToList();

            bool added;
            do
            {
                added = false;
                foreach (var enumerator in enumerators)
                {
                    if (enumerator.MoveNext() && result.Count < maxEpisodes)
                    {
                        result.Add(enumerator.Current);
                        added = true;
                    }
                }
            } while (added && result.Count < maxEpisodes);

            return result;
        }
    }
}
