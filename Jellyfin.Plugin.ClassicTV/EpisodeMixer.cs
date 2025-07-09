using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

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

            // Crear listas de episodios para cada serie
            var episodeLists = seriesEpisodes.Values.ToList();
            var currentIndexes = new int[episodeLists.Count]; // Índices actuales para cada serie

            bool hasMoreEpisodes;
            do
            {
                hasMoreEpisodes = false;

                // Recorrer cada serie y tomar el siguiente episodio disponible
                for (int i = 0; i < episodeLists.Count; i++)
                {
                    var episodeList = episodeLists[i];
                    var currentIndex = currentIndexes[i];

                    // Si esta serie tiene más episodios disponibles
                    if (currentIndex < episodeList.Count && result.Count < maxEpisodes)
                    {
                        var episode = episodeList[currentIndex];
                        result.Add(episode);
                        currentIndexes[i]++; // Avanzar al siguiente episodio de esta serie
                        hasMoreEpisodes = true;
                    }
                }
            } while (hasMoreEpisodes && result.Count < maxEpisodes);

            return result;
        }
    }
}
