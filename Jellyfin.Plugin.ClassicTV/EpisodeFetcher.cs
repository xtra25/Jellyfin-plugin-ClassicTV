using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.ClassicTV
{
    public class EpisodeFetcher
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly ILibraryManager _libraryManager;

        public EpisodeFetcher(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Obté, per cada sèrie, els episodis que l'usuari encara no ha vist, en ordre de visionat.
        /// </summary>
        /// <param name="seriesIds">Ids de les sèries (text). Els repetits i els no vàlids s'ignoren.</param>
        /// <param name="user">L'usuari de Jellyfin (es passa com a object per no lligar el plugin al tipus User de cada versió).</param>
        /// <returns>Una entrada per cada sèrie trobada, encara que no tingui episodis pendents.</returns>
        /// <exception cref="InvalidOperationException">Si no es pot aplicar l'usuari a la consulta; sense usuari el filtre de "no vist" no s'aplicaria i la playlist inclouria episodis vistos.</exception>
        public List<SeriesEpisodes> GetUnwatchedEpisodesBySeries(IEnumerable<string> seriesIds, object user)
        {
            ArgumentNullException.ThrowIfNull(seriesIds);
            ArgumentNullException.ThrowIfNull(user);

            var result = new List<SeriesEpisodes>();
            var seen = new HashSet<Guid>();

            foreach (var id in seriesIds)
            {
                if (!Guid.TryParse(id, out var guid) || !seen.Add(guid))
                {
                    continue;
                }

                if (_libraryManager.GetItemById(guid) is not Series series)
                {
                    continue;
                }

                var query = new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Episode },
                    ParentId = series.Id,
                    Recursive = true
                };

                AssignUser(query, user);
                query.IsPlayed = false;

                var episodes = EpisodeOrdering.Sort(
                    ExecuteQuery(query).OfType<Episode>(),
                    e => e.ParentIndexNumber,
                    e => e.IndexNumber,
                    e => e.PremiereDate,
                    e => e.Name);

                result.Add(new SeriesEpisodes(series.Id, series.Name, episodes));
            }

            return result;
        }

        // Assigna l'usuari a la consulta per reflexió (SetUser o propietat User, segons la versió de Jellyfin)
        // i comprova que s'ha assignat de debò. Abans l'error s'ignorava en silenci.
        private static void AssignUser(InternalItemsQuery query, object user)
        {
            var type = typeof(InternalItemsQuery);

            var setUser = type.GetMethods(AnyInstance)
                .FirstOrDefault(m => m.Name == "SetUser" && m.GetParameters().Length == 1);

            if (setUser != null)
            {
                setUser.Invoke(query, new[] { user });
            }
            else
            {
                var userProperty = type.GetProperty("User", AnyInstance);
                if (userProperty == null || !userProperty.CanWrite)
                {
                    throw new InvalidOperationException("InternalItemsQuery no exposa SetUser ni una propietat User escrivible.");
                }

                userProperty.SetValue(query, user);
            }

            if (type.GetProperty("User", AnyInstance)?.GetValue(query) == null)
            {
                throw new InvalidOperationException("No s'ha pogut assignar l'usuari a la consulta d'episodis.");
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
    }
}
