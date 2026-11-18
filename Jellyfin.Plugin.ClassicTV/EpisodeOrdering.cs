using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.ClassicTV
{
    /// <summary>
    /// Ordena els episodis d'una sèrie. No depèn de cap tipus de Jellyfin perquè es pugui provar de forma aïllada.
    /// </summary>
    public static class EpisodeOrdering
    {
        /// <summary>
        /// Ordena els elements en ordre de visionat: temporades numerades de menor a major,
        /// després els episodis sense temporada, i al final els especials (temporada 0).
        /// Dins de cada temporada, per número d'episodi (els que no en tenen, al final),
        /// i com a desempat per data d'estrena i nom, perquè el resultat sigui determinista.
        /// </summary>
        /// <typeparam name="T">Tipus de l'element (un episodi).</typeparam>
        /// <param name="items">Elements a ordenar.</param>
        /// <param name="season">Número de temporada, o null si no en té.</param>
        /// <param name="number">Número d'episodi dins de la temporada, o null si no en té.</param>
        /// <param name="premiere">Data d'estrena, o null.</param>
        /// <param name="name">Nom de l'episodi, o null.</param>
        /// <returns>Una llista nova amb els elements ordenats.</returns>
        public static List<T> Sort<T>(
            IEnumerable<T> items,
            Func<T, int?> season,
            Func<T, int?> number,
            Func<T, DateTime?> premiere,
            Func<T, string?> name)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(season);
            ArgumentNullException.ThrowIfNull(number);
            ArgumentNullException.ThrowIfNull(premiere);
            ArgumentNullException.ThrowIfNull(name);

            return items
                .OrderBy(e => SeasonGroup(season(e)))
                .ThenBy(e => season(e) ?? 0)
                .ThenBy(e => number(e) ?? int.MaxValue)
                .ThenBy(e => premiere(e) ?? DateTime.MaxValue)
                .ThenBy(e => name(e) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // 0 = temporades numerades, 1 = sense temporada, 2 = especials (temporada 0).
        private static int SeasonGroup(int? season) => season switch
        {
            null => 1,
            0 => 2,
            _ => 0
        };
    }
}
