using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using Jellyfin.Data.Entities;

namespace Jellyfin.Plugin.ClassicTV
{
    public class EpisodeFetcher
    {
        private readonly ILibraryManager _libraryManager;

        public EpisodeFetcher(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public Dictionary<string, List<Episode>> GetUnwatchedEpisodesBySeries(List<string> seriesIds, User user)
        {
            var result = new Dictionary<string, List<Episode>>();

            foreach (var id in seriesIds)
            {
                if (!Guid.TryParse(id, out var guid))
                    continue;

                var item = _libraryManager.GetItemById(guid);
                if (item is Series series)
                {
                    // Obtener todos los episodios ordenados
                    var allEpisodes = series.GetRecursiveChildren()
                        .OfType<Episode>()
                        .OrderBy(e => e.ParentIndexNumber ?? 0)
                        .ThenBy(e => e.IndexNumber ?? 0)
                        .ToList();

                    // Filtrar solo los episodios no vistos
                    var unwatchedEpisodes = allEpisodes
                        .Where(e => !e.IsPlayed(user))
                        .ToList();

                    result[series.Name] = unwatchedEpisodes;
                }
            }

            return result;
        }

        // Método original para compatibilidad (si se necesita)
        public Dictionary<string, List<Episode>> GetOrderedEpisodesBySeries(List<string> seriesIds)
        {
            var result = new Dictionary<string, List<Episode>>();

            foreach (var id in seriesIds)
            {
                if (!Guid.TryParse(id, out var guid))
                    continue;

                var item = _libraryManager.GetItemById(guid);
                if (item is Series series)
                {
                    var episodes = series.GetRecursiveChildren()
                        .OfType<Episode>()
                        .OrderBy(e => e.ParentIndexNumber ?? 0)
                        .ThenBy(e => e.IndexNumber ?? 0)
                        .ToList();

                    result[series.Name] = episodes;
                }
            }

            return result;
        }
    }
}
