using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.ClassicTV.Configuration;

namespace Jellyfin.Plugin.ClassicTV.Tests
{
    internal sealed record Ep(string Series, int? Season, int? Number, DateTime? Premiere = null, string? Name = null)
    {
        public override string ToString() => $"{Series}:S{Season?.ToString() ?? "-"}E{Number?.ToString() ?? "-"}";
    }

    internal static class Program
    {
        private static readonly List<(string Name, Action Body)> Tests = new();

        private static int Main()
        {
            // ---------- Ordenació ----------
            Add("Ordenació: les temporades s'ordenen numèricament (S10 després de S2)", () =>
            {
                var r = Sort(new Ep("A", 10, 1), new Ep("A", 2, 1), new Ep("A", 1, 1));
                Eq(new[] { 1, 2, 10 }, r.Select(e => e.Season!.Value));
            });

            Add("Ordenació: els especials (temporada 0) van al final", () =>
            {
                var r = Sort(new Ep("A", 0, 1), new Ep("A", 2, 1), new Ep("A", 1, 1));
                Eq(new int?[] { 1, 2, 0 }, r.Select(e => e.Season));
            });

            Add("Ordenació: sense temporada va entre les numerades i els especials", () =>
            {
                var r = Sort(new Ep("A", 0, 1), new Ep("A", null, 1), new Ep("A", 3, 1));
                Eq(new int?[] { 3, null, 0 }, r.Select(e => e.Season));
            });

            Add("Ordenació: dins d'una temporada, els episodis sense número van al final", () =>
            {
                var r = Sort(new Ep("A", 1, null), new Ep("A", 1, 2), new Ep("A", 1, 1));
                Eq(new int?[] { 1, 2, null }, r.Select(e => e.Number));
            });

            Add("Ordenació: desempat per data d'estrena i després per nom", () =>
            {
                var d1 = new DateTime(2000, 1, 1);
                var d2 = new DateTime(2001, 1, 1);
                var r = Sort(
                    new Ep("A", 1, 1, d2, "z"),
                    new Ep("A", 1, 1, d1, "b"),
                    new Ep("A", 1, 1, d1, "a"),
                    new Ep("A", 1, 1, null, "a"));
                Eq(new[] { "a", "b", "z", "a" }, r.Select(e => e.Name!));
                Eq(new DateTime?[] { d1, d1, d2, null }, r.Select(e => e.Premiere));
            });

            Add("Ordenació: no modifica la llista d'entrada", () =>
            {
                var input = new List<Ep> { new("A", 2, 1), new("A", 1, 1) };
                _ = Sort(input.ToArray());
                Eq(new int?[] { 2, 1 }, input.Select(e => e.Season));
            });

            // ---------- Barreja round-robin ----------
            Add("Barreja: l'exemple del README (A=4, B=2, C=3) funciona per rondes", () =>
            {
                var a = Series("A", 4); var b = Series("B", 2); var c = Series("C", 3);
                var r = RoundRobinMixer.Mix(new[] { a, b, c }, random: new Random(1));
                True(r.Count == 9, $"s'esperaven 9 episodis i n'hi ha {r.Count}");
                SetEq(new[] { "A1", "B1", "C1" }, r.Skip(0).Take(3).Select(Label));
                SetEq(new[] { "A2", "B2", "C2" }, r.Skip(3).Take(3).Select(Label));
                SetEq(new[] { "A3", "C3" }, r.Skip(6).Take(2).Select(Label));
                SetEq(new[] { "A4" }, r.Skip(8).Take(1).Select(Label));
            });

            Add("Barreja: manté l'ordre intern de cada sèrie (diverses llavors)", () =>
            {
                for (var seed = 0; seed < 100; seed++)
                {
                    var r = RoundRobinMixer.Mix(new[] { Series("A", 7), Series("B", 3), Series("C", 5), Series("D", 1) }, random: new Random(seed));
                    foreach (var s in new[] { "A", "B", "C", "D" })
                    {
                        var indexes = r.Where(e => e.Series == s).Select(e => e.Number!.Value).ToList();
                        Eq(indexes.OrderBy(i => i).ToList(), indexes);
                    }

                    True(r.Count == 16, "s'han perdut o duplicat episodis");
                }
            });

            Add("Barreja: el límit talla enmig d'una ronda", () =>
            {
                var r = RoundRobinMixer.Mix(new[] { Series("A", 4), Series("B", 2), Series("C", 3) }, maxItems: 4, random: new Random(1));
                True(r.Count == 4, $"s'esperaven 4 i n'hi ha {r.Count}");
                SetEq(new[] { "A1", "B1", "C1" }, r.Take(3).Select(Label));
            });

            Add("Barreja: el límit per defecte és 1000", () =>
            {
                var r = RoundRobinMixer.Mix(new[] { Series("A", 500), Series("B", 500), Series("C", 500) });
                True(r.Count == 1000, $"s'esperaven 1000 i n'hi ha {r.Count}");
            });

            Add("Barreja: llistes buides o nul·les s'ignoren; tot buit dóna llista buida", () =>
            {
                var r = RoundRobinMixer.Mix(new IReadOnlyList<Ep>?[] { null, Series("A", 0), Series("B", 2) }, random: new Random(1));
                Eq(new[] { "B1", "B2" }, r.Select(Label));
                True(RoundRobinMixer.Mix(new IReadOnlyList<Ep>?[] { null, Series("A", 0) }).Count == 0, "hauria de ser buida");
                True(RoundRobinMixer.Mix(Array.Empty<IReadOnlyList<Ep>>()).Count == 0, "sense llistes hauria de ser buida");
            });

            Add("Barreja: una sola sèrie passa tal qual", () =>
            {
                Eq(new[] { "A1", "A2", "A3" }, RoundRobinMixer.Mix(new[] { Series("A", 3) }).Select(Label));
            });

            Add("Barreja: maxItems <= 0 és un error", () =>
            {
                Throws<ArgumentOutOfRangeException>(() => RoundRobinMixer.Mix(new[] { Series("A", 2) }, maxItems: 0));
                Throws<ArgumentOutOfRangeException>(() => RoundRobinMixer.Mix(new[] { Series("A", 2) }, maxItems: -5));
            });

            Add("Barreja: l'ordre de les sèries canvia entre execucions (no comença sempre per la mateixa)", () =>
            {
                var firsts = new HashSet<string>();
                for (var seed = 0; seed < 50; seed++)
                {
                    firsts.Add(RoundRobinMixer.Mix(new[] { Series("A", 2), Series("B", 2), Series("C", 2) }, random: new Random(seed))[0].Series);
                }

                True(firsts.Count == 3, $"només han començat {firsts.Count} sèries diferents");
            });

            Add("Barreja: no modifica les llistes d'entrada", () =>
            {
                var a = Series("A", 3);
                var before = a.Select(Label).ToList();
                _ = RoundRobinMixer.Mix(new[] { a, Series("B", 3) }, random: new Random(3));
                Eq(before, a.Select(Label));
            });

            // ---------- Regles de les playlists ----------
            Add("Playlists: sense nom s'usa el nom per defecte amb l'usuari", () =>
            {
                True(PlaylistRules.EffectiveName("", "joan") == "ClassicTV Playlist - joan", "nom buit");
                True(PlaylistRules.EffectiveName("   ", "joan") == "ClassicTV Playlist - joan", "nom amb només espais");
                True(PlaylistRules.EffectiveName(null, "laia") == "ClassicTV Playlist - laia", "nom nul");
            });

            Add("Playlists: un nom propi es respecta (retallant espais)", () =>
            {
                True(PlaylistRules.EffectiveName("  classicstv laia ", "laia") == "classicstv laia", "nom propi");
            });

            Add("Playlists: la comparació de noms ignora majúscules i espais als extrems", () =>
            {
                True(PlaylistRules.SameName("ClassicTV", "  classictv "), "hauria de coincidir");
                True(!PlaylistRules.SameName("ClassicTV", "ClassicTV 2"), "no hauria de coincidir");
                True(!PlaylistRules.SameName(null, "x") && PlaylistRules.SameName(null, null), "nuls");
            });

            Add("Migració: una playlist per usuari, amb totes les sèries i el nom per defecte (buit)", () =>
            {
                var r = PlaylistRules.FromLegacy(new[] { "u1", "u2" }, new[] { "s1", "s2", "s3" });
                True(r.Count == 2, $"s'esperaven 2 playlists i n'hi ha {r.Count}");
                Eq(new[] { "u1", "u2" }, r.Select(p => p.UserId));
                True(r.All(p => p.Name == string.Empty && p.JellyfinPlaylistId == string.Empty), "noms i ids buits");
                True(r.All(p => p.SeriesIds.SequenceEqual(new[] { "s1", "s2", "s3" })), "totes les sèries");
                True(r.Select(p => p.Id).Distinct().Count() == 2 && r.All(p => p.Id.Length > 0), "ids de definició únics");
            });

            Add("Migració: ignora usuaris/sèries buits o repetits (sense distingir majúscules)", () =>
            {
                var r = PlaylistRules.FromLegacy(new[] { "U1", "u1", "", " ", "u2" }, new[] { "S1", "s1", "", "s2" });
                Eq(new[] { "U1", "u2" }, r.Select(p => p.UserId));
                Eq(new[] { "S1", "s2" }, r[0].SeriesIds);
            });

            Add("Migració: sense sèries o sense usuaris no crea res", () =>
            {
                True(PlaylistRules.FromLegacy(new[] { "u1" }, Array.Empty<string>()).Count == 0, "sense sèries");
                True(PlaylistRules.FromLegacy(Array.Empty<string>(), new[] { "s1" }).Count == 0, "sense usuaris");
                True(PlaylistRules.FromLegacy(null, null).Count == 0, "nuls");
            });

            Add("Migració: cada playlist té la seva pròpia llista de sèries (no compartida)", () =>
            {
                var r = PlaylistRules.FromLegacy(new[] { "u1", "u2" }, new[] { "s1", "s2" });
                r[0].SeriesIds.Add("s3");
                True(r[1].SeriesIds.Count == 2, "la llista de la segona playlist s'ha modificat");
            });

            // ---------- Idiomes (Localizer) ----------
            Add("Idioma: noms de cultura es redueixen a ca/es/en", () =>
            {
                var casos = new (string?, string)[]
                {
                    ("ca", "ca"), ("ca-ES", "ca"), ("ca_ES", "ca"), ("ca-valencia", "ca"), ("CA", "ca"), ("  ca-es ", "ca"),
                    ("es", "es"), ("es-ES", "es"), ("es-MX", "es"), ("es_419", "es"), ("ES-ar", "es"),
                    ("en", "en"), ("en-US", "en"), ("EN-gb", "en")
                };
                foreach (var (entrada, esperat) in casos)
                {
                    True(Localizer.Resolve(entrada) == esperat, $"'{entrada}' -> '{Localizer.Resolve(entrada)}' (esperat '{esperat}')");
                }
            });

            Add("Idioma: els idiomes no suportats, buits o nuls cauen a anglès", () =>
            {
                foreach (var entrada in new string?[] { "fr", "fr-FR", "de-DE", "pt-BR", "gl", "eu", "zh-CN", "", "   ", null, "-", "_", "cat", "esp", "spanish" })
                {
                    True(Localizer.Resolve(entrada) == "en", $"'{entrada}' hauria de ser 'en' i és '{Localizer.Resolve(entrada)}'");
                }
            });

            Add("Idioma: nom i descripció de la tasca existeixen en els tres idiomes i són diferents", () =>
            {
                var noms = new[] { "ca", "es", "en" }.Select(Localizer.TaskName).ToList();
                var descripcions = new[] { "ca", "es", "en" }.Select(Localizer.TaskDescription).ToList();
                True(noms.Distinct().Count() == 3, "els noms haurien de ser diferents: " + string.Join(" | ", noms));
                True(descripcions.Distinct().Count() == 3, "les descripcions haurien de ser diferents");
                True(noms.All(n => n.Contains("ClassicTV")), "el nom ha de dir ClassicTV");
                True(noms.Concat(descripcions).All(x => !string.IsNullOrWhiteSpace(x)), "text buit");
            });

            Add("Idioma: el nom i la descripció segueixen la cultura (i l'anglès és el per defecte)", () =>
            {
                True(Localizer.TaskName("ca-ES") == "Genera les llistes de reproducció de ClassicTV", "ca");
                True(Localizer.TaskName("es-MX") == "Generar las listas de reproducción de ClassicTV", "es");
                True(Localizer.TaskName("en-US") == "Generate ClassicTV playlists", "en");
                True(Localizer.TaskName("fr-FR") == "Generate ClassicTV playlists", "fr -> en");
                True(Localizer.TaskName(null) == "Generate ClassicTV playlists", "null -> en");
                True(Localizer.TaskDescription("ca").StartsWith("Barreja", StringComparison.Ordinal), "descripció ca");
                True(Localizer.TaskDescription("es").StartsWith("Mezcla", StringComparison.Ordinal), "descripció es");
                True(Localizer.TaskDescription("xx").StartsWith("Mixes", StringComparison.Ordinal), "descripció en");
            });

            return Run();
        }

        // ---------- Utilitats ----------
        private static List<Ep> Sort(params Ep[] items) =>
            EpisodeOrdering.Sort(items, e => e.Season, e => e.Number, e => e.Premiere, e => e.Name);

        private static IReadOnlyList<Ep> Series(string name, int count) =>
            Enumerable.Range(1, count).Select(i => new Ep(name, 1, i)).ToList();

        private static string Label(Ep e) => $"{e.Series}{e.Number}";

        private static void Add(string name, Action body) => Tests.Add((name, body));

        private static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Eq<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var e = expected.ToList();
            var a = actual.ToList();
            True(e.SequenceEqual(a), $"esperat [{string.Join(", ", e)}] però s'ha obtingut [{string.Join(", ", a)}]");
        }

        private static void SetEq<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var e = expected.OrderBy(x => x).ToList();
            var a = actual.OrderBy(x => x).ToList();
            True(e.SequenceEqual(a), $"esperat (en qualsevol ordre) [{string.Join(", ", e)}] però s'ha obtingut [{string.Join(", ", a)}]");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException($"s'esperava {typeof(TException).Name} i no s'ha llançat");
        }

        private static int Run()
        {
            var failed = 0;
            foreach (var (name, body) in Tests)
            {
                try
                {
                    body();
                    Console.WriteLine($"  OK    {name}");
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine($"  FALLA {name}\n        {ex.Message}");
                }
            }

            Console.WriteLine($"\n{Tests.Count - failed}/{Tests.Count} proves correctes");
            return failed == 0 ? 0 : 1;
        }
    }
}
