using System;

namespace Jellyfin.Plugin.ClassicTV
{
    /// <summary>
    /// Textos del plugin que Jellyfin mostra tal com els hi dona el servidor (nom i descripció de la tasca programada),
    /// en català, castellà i anglès. Se'n tria l'idioma pel de la interfície del servidor. No depèn de Jellyfin.
    /// </summary>
    public static class Localizer
    {
        public const string Catalan = "ca";
        public const string Spanish = "es";
        public const string English = "en";

        /// <summary>
        /// Reduces a culture name ("ca", "ca-ES", "es_MX", "en-US", "fr"...) to one of the supported languages; English when unknown.
        /// </summary>
        /// <param name="culture">Culture name, may be null or empty.</param>
        /// <returns>"ca", "es" or "en".</returns>
        public static string Resolve(string? culture)
        {
            var baseLanguage = (culture ?? string.Empty).Trim().ToLowerInvariant().Split('-', '_')[0];

            return baseLanguage switch
            {
                Catalan => Catalan,
                Spanish => Spanish,
                _ => English
            };
        }

        public static string TaskName(string? culture) => Resolve(culture) switch
        {
            Catalan => "Genera les llistes de reproducció de ClassicTV",
            Spanish => "Generar las listas de reproducción de ClassicTV",
            _ => "Generate ClassicTV playlists"
        };

        public static string TaskDescription(string? culture) => Resolve(culture) switch
        {
            Catalan => "Barreja (round-robin) els episodis sense veure de les sèries de cada llista de reproducció configurada i crea o actualitza cada llista.",
            Spanish => "Mezcla (round-robin) los episodios sin ver de las series de cada lista de reproducción configurada y crea o actualiza cada lista.",
            _ => "Mixes (round-robin) the unwatched episodes of the series of each configured playlist and creates or updates each playlist."
        };
    }
}
