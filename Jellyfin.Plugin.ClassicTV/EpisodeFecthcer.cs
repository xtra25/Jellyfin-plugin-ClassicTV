using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.ClassicTV
{
    public class EpisodeFetcher
    {
        private readonly ILibraryManager _libraryManager;

        public EpisodeFetcher(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public Dictionary<string, List<Episode>> GetUnwatchedEpisodesBySeries(List<string> seriesIds, object? user, Guid userId)
        {
            var result = new Dictionary<string, List<Episode>>();

            foreach (var id in seriesIds)
            {
                if (!Guid.TryParse(id, out var guid))
                {
                    continue;
                }

                var item = _libraryManager.GetItemById(guid);
                if (item is Series series)
                {
                    var query = new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { BaseItemKind.Episode },
                        ParentId = series.Id,
                        Recursive = true
                    };

                    if (user != null)
                    {
                        TryAssignUserToQuery(query, user);
                        query.IsPlayed = false;
                    }

                    var allEpisodes = ExecuteQuery(query)
                        .OfType<Episode>()
                        .OrderBy(e => e.ParentIndexNumber ?? 0)
                        .ThenBy(e => e.IndexNumber ?? 0)
                        .ToList();

                    var unwatchedEpisodes = allEpisodes
                        .Where(e => IsUnplayedForUser(e, userId))
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
                    var query = new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { BaseItemKind.Episode },
                        ParentId = series.Id,
                        Recursive = true
                    };

                    var episodes = ExecuteQuery(query)
                        .OfType<Episode>()
                        .OrderBy(e => e.ParentIndexNumber ?? 0)
                        .ThenBy(e => e.IndexNumber ?? 0)
                        .ToList();

                    result[series.Name] = episodes;
                }
            }

            return result;
        }

        private static void TryAssignUserToQuery(InternalItemsQuery query, object user)
        {
            try
            {
                var setUserMethod = typeof(InternalItemsQuery).GetMethod("SetUser", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (setUserMethod != null)
                {
                    setUserMethod.Invoke(query, new[] { user });
                    return;
                }

                var userProperty = typeof(InternalItemsQuery).GetProperty("User", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (userProperty != null && userProperty.CanWrite)
                {
                    userProperty.SetValue(query, user);
                }
            }
            catch
            {
                // ignorar; el filtro por User es un optimizador y no debe romper la generación
            }
        }

        private IEnumerable<BaseItem> ExecuteQuery(InternalItemsQuery query)
        {
            // Jellyfin 10.9 expone GetItemList; en 10.11 se reemplazó por QueryItems.
            // Usar reflexión para soportar ambas versiones sin recompilar contra assemblies nuevos.
            var libraryManagerType = _libraryManager.GetType();

            // Preferir GetItemList si está disponible.
            var getItemList = libraryManagerType.GetMethod("GetItemList", BindingFlags.Instance | BindingFlags.Public, new[] { typeof(InternalItemsQuery) });
            if (getItemList != null)
            {
                if (getItemList.Invoke(_libraryManager, new object[] { query }) is IEnumerable<BaseItem> list)
                {
                    return list;
                }
            }

            // Fallback a QueryItems (retorna objeto con propiedad Items).
            var queryItems = libraryManagerType.GetMethod("QueryItems", BindingFlags.Instance | BindingFlags.Public, new[] { typeof(InternalItemsQuery) });
            if (queryItems != null)
            {
                var result = queryItems.Invoke(_libraryManager, new object[] { query });
                var itemsProp = result?.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.Public);
                if (itemsProp?.GetValue(result) is IEnumerable items)
                {
                    return items.OfType<BaseItem>();
                }
            }

            throw new MissingMethodException("ILibraryManager no expone ni GetItemList ni QueryItems compatibles.");
        }

        private static bool IsUnplayedForUser(Episode episode, Guid userId)
        {
            try
            {
                var userDataProperty = episode.GetType().GetProperty("UserData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (userDataProperty?.GetValue(episode) is IDictionary userDataDict && userDataDict.Contains(userId))
                {
                    var userData = userDataDict[userId];
                    var playedProperty = userData?.GetType().GetProperty("Played", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var played = (bool?)playedProperty?.GetValue(userData);
                    return !(played ?? false);
                }

                var userDataListProperty = episode.GetType().GetProperty("UserDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (userDataListProperty?.GetValue(episode) is IEnumerable userDataList)
                {
                    foreach (var entry in userDataList)
                    {
                        var idProperty = entry?.GetType().GetProperty("UserId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (idProperty?.GetValue(entry) is Guid entryUserId && entryUserId == userId)
                        {
                            var playedProperty = entry.GetType().GetProperty("Played", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            var played = (bool?)playedProperty?.GetValue(entry);
                            return !(played ?? false);
                        }
                    }
                }
            }
            catch
            {
                // ignorar y considerar el episodio como no visto
            }

            return true;
        }
    }
}
