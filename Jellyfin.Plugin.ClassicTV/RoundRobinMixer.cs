using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.ClassicTV
{
    /// <summary>
    /// Barreja diverses llistes en round-robin mantenint l'ordre intern de cada llista.
    /// No depèn de cap tipus de Jellyfin perquè es pugui provar de forma aïllada.
    /// </summary>
    public static class RoundRobinMixer
    {
        /// <summary>
        /// Nombre màxim d'elements per defecte (per evitar problemes de rendiment).
        /// </summary>
        public const int DefaultMaxItems = 1000;

        /// <summary>
        /// Barreja les llistes alternant un element de cada una per torn.
        /// L'ordre de les llistes es barreja a l'atzar a cada crida, de manera que
        /// no sempre comença la mateixa. Les llistes buides o nul·les s'ignoren.
        /// </summary>
        /// <typeparam name="T">Tipus dels elements.</typeparam>
        /// <param name="sources">Llistes a barrejar (cada una ja en el seu ordre final).</param>
        /// <param name="maxItems">Nombre màxim d'elements del resultat (ha de ser major que 0).</param>
        /// <param name="random">Generador aleatori; si és null s'usa el compartit. Es pot passar-ne un amb llavor per a proves.</param>
        /// <returns>Una llista nova amb la barreja.</returns>
        public static List<T> Mix<T>(IEnumerable<IReadOnlyList<T>?> sources, int maxItems = DefaultMaxItems, Random? random = null)
        {
            ArgumentNullException.ThrowIfNull(sources);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxItems);

            var lists = sources
                .Where(l => l != null && l.Count > 0)
                .Select(l => l!)
                .ToList();

            Shuffle(lists, random ?? Random.Shared);

            var result = new List<T>();
            var positions = new int[lists.Count];
            var madeProgress = true;

            while (madeProgress && result.Count < maxItems)
            {
                madeProgress = false;

                for (var i = 0; i < lists.Count && result.Count < maxItems; i++)
                {
                    if (positions[i] < lists[i].Count)
                    {
                        result.Add(lists[i][positions[i]]);
                        positions[i]++;
                        madeProgress = true;
                    }
                }
            }

            return result;
        }

        private static void Shuffle<T>(IList<T> list, Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
