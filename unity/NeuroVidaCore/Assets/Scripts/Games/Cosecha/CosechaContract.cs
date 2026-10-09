using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Cosecha
{
    /// <summary>Una palabra de una ronda (sale de Resources/Lexico/cosecha_es.json: lo escribe tools/cosecha/rondas.py).
    /// <c>p</c> = la palabra con su tilde; <c>b</c> = banda de uso 1 (la conoce todo el mundo) a 6 (rara); <c>comun</c> = banda 1-3;
    /// <c>estrella</c> = usa las 7 letras; <c>oculta</c> = se acepta si alguien la forma, pero nunca se muestra ni cuenta como común.</summary>
    [Serializable]
    public sealed class HarvestWord
    {
        public string p;
        public int b;
        public bool comun;
        public bool estrella;
        public bool oculta;

        public bool IsCommon => comun && !oculta;
        public bool IsRare => b >= 4;
    }

    /// <summary>Un juego de 7 letras (sin tilde, la ñ es su propia letra) con TODAS las palabras válidas que se pueden formar.</summary>
    [Serializable]
    public sealed class HarvestRound
    {
        public string id;
        public int nivel;
        public string letras;
        public HarvestWord[] palabras;
    }

    [Serializable]
    public sealed class HarvestData
    {
        public int version;
        public string idioma;
        public string fuente;
        public string licencia;
        public HarvestRound[] rondas;
    }

    /// <summary>
    /// Reglas puras de "Cosecha de palabras" (juego estrella de Lenguaje; ver docs/diseno-cosecha-de-palabras.md). Siete letras
    /// giran en órbita alrededor de un planeta: se tocan en orden para formar palabras (3 letras o más, cada ficha una vez por
    /// palabra) y cada una brota como una planta en el huerto. Pide PRODUCIR palabras desde la memoria bajo presión de tiempo
    /// (fluidez verbal). "Tu manera de buscar" (racimos y saltos) retoma el análisis de Troyer, Moscovitch y Winocur (1997) solo
    /// como DESCRIPCIÓN de cómo se buscó: ese estudio lo validó con fluidez por categoría o por letra inicial, no con juegos de
    /// letras fijas, y no hay normas ni comparaciones. Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class CosechaContract
    {
        public const string GameId = "cosecha";
        public const int MaxLevel = 10;

        /// <summary>3 cosechas por partida; Reto = 60 s cada una, Precisión = 90 s.</summary>
        public const int Cosechas = 3;
        public const int RetoSeconds = 60;
        public const int PrecisionSeconds = 90;
        public const int MinLetters = 3;
        public const int LetterCount = 7;
        public const int StarMultiplier = 3;
        /// <summary>Sin sembrar durante tantos segundos, Nubi ilumina la primera letra de una palabra común que falte.</summary>
        public const float HintSeconds = 15f;
        public const float SeniorHintSeconds = 10f;
        /// <summary>La cosecha se logra si se encuentra al menos esta fracción de las palabras comunes de la ronda.</summary>
        public const float SuccessShare = 0.35f;
        /// <summary>Racha de brillo en la órbita: tantas palabras en menos de tantos segundos.</summary>
        public const int FastWords = 3;
        public const float FastSeconds = 10f;
        /// <summary>Cuántas partidas anteriores se evitan al elegir ronda.</summary>
        public const int RecentGames = 5;
        /// <summary>Palabras mínimas para decir "cómo buscaste" y cuántas palabras se piden en "también podías".</summary>
        public const int MinClusterWords = 10;
        public const int MissedCount = 5;
        /// <summary>Ventana del "ritmo": primeros y últimos tantos segundos de cada cosecha.</summary>
        public const float WindowSeconds = 20f;
        /// <summary>Tamaño de la letra de la ficha, en sp (Atkinson Hyperlegible Bold): 30 y, en mayores, 34. Toque mínimo 64 dp.</summary>
        public static int LetterSizeSp(bool senior) => senior ? 34 : 30;
        public const float MinTouchDp = 64f;

        public static int CosechaSeconds(bool precision) => precision ? PrecisionSeconds : RetoSeconds;
        public static float HintAfter(bool senior) => senior ? SeniorHintSeconds : HintSeconds;
        public static int ClampLevel(int level) => Math.Max(1, Math.Min(MaxLevel, level));

        // ------------------------------------------------------------------ letras y palabras

        /// <summary>La palabra sin tilde (á → a; ü → u): la ficha "a" vale para "á". La ñ se queda: es su propia letra.</summary>
        public static string Normalize(string w)
        {
            if (string.IsNullOrEmpty(w)) return "";
            var sb = new System.Text.StringBuilder(w.Length);
            foreach (char c in w.ToLowerInvariant())
            {
                switch (c)
                {
                    case 'á': sb.Append('a'); break;
                    case 'é': sb.Append('e'); break;
                    case 'í': sb.Append('i'); break;
                    case 'ó': sb.Append('o'); break;
                    case 'ú': case 'ü': sb.Append('u'); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Puntos de una palabra: por largo (3 → 10 … 7 → 100); una rara (banda 4-5, no común) vale la mitad más; la estrella × 3.</summary>
        public static int Points(HarvestWord w)
        {
            int len = Normalize(w.p).Length;
            int[] table = { 0, 0, 0, 10, 20, 35, 55, 100 };
            int pts = table[Math.Max(0, Math.Min(7, len))];
            if (w.IsRare) pts = pts * 3 / 2;
            if (w.estrella) pts *= StarMultiplier;
            return pts;
        }

        public static int CommonTotal(HarvestRound r)
        {
            int n = 0;
            if (r?.palabras != null) foreach (var w in r.palabras) if (w.IsCommon) n++;
            return n;
        }

        /// <summary>¿Se logró la cosecha? Al menos el <see cref="SuccessShare"/> de las comunes disponibles.</summary>
        public static bool Success(int commonFound, int commonTotal) =>
            commonTotal > 0 && commonFound >= (int)Math.Ceiling(commonTotal * (double)SuccessShare - 1e-9);

        /// <summary>Puntaje 0-100: las comunes encontradas (70%, 60% de las comunes ya da todo) y el nivel más alto (30%).</summary>
        public static int Score(float commonShare, int peakLevel)
        {
            float a = Math.Max(0f, Math.Min(1f, commonShare / 0.6f));
            float lv = (float)(ClampLevel(peakLevel) - 1) / (MaxLevel - 1);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.7f * a + 0.3f * lv) * 100f)));
        }

        // ------------------------------------------------------------------ ronda de práctica (tutorial con Nubi)

        /// <summary>Las siete letras de la práctica (las de la ronda de respaldo): alcanzan para CASA, ROSA, ROCA, COSA, CASO y SACA, y nada más hace falta.</summary>
        public const string PracticeLetters = "iraoasc";
        /// <summary>La palabra que Nubi pide formar en la práctica, y la que se arma «por error» para mostrar «Borrar».</summary>
        public const string PracticeWord = "casa", PracticeMistake = "ro";

        /// <summary>La ronda de la práctica: seis palabras muy conocidas (banda 1), ninguna rara ni estrella. No pasa por el banco, el nivel ni el puntaje: solo existe mientras dura el tutorial.</summary>
        public static HarvestRound PracticeRound()
        {
            var words = new List<HarvestWord>();
            foreach (var p in new[] { "casa", "rosa", "roca", "cosa", "caso", "saca" }) words.Add(new HarvestWord { p = p, b = 1, comun = true });
            return new HarvestRound { id = "practica", nivel = 1, letras = PracticeLetters, palabras = words.ToArray() };
        }

        /// <summary>Qué fichas hay que tocar, en orden, para escribir <paramref name="word"/> con <paramref name="letters"/> (cada ficha una vez); null si no se puede.</summary>
        public static int[] TilesFor(string letters, string word)
        {
            if (string.IsNullOrEmpty(letters) || string.IsNullOrEmpty(word)) return null;
            var used = new bool[letters.Length];
            var tiles = new int[word.Length];
            for (int i = 0; i < word.Length; i++)
            {
                tiles[i] = -1;
                for (int k = 0; k < letters.Length; k++)
                {
                    if (used[k] || letters[k] != word[i]) continue;
                    used[k] = true;
                    tiles[i] = k;
                    break;
                }
                if (tiles[i] < 0) return null;
            }
            return tiles;
        }

        // ------------------------------------------------------------------ pista

        /// <summary>La palabra común que falta y se ilumina: la MÁS CORTA (empata por uso más común y luego por orden). La ficha
        /// que se ilumina es la de su primera letra. Null si no falta ninguna.</summary>
        public static HarvestWord PickHint(HarvestRound r, ICollection<string> foundNormalized)
        {
            HarvestWord best = null;
            int bestLen = int.MaxValue;
            foreach (var w in r.palabras)
            {
                if (!w.IsCommon) continue;
                string n = Normalize(w.p);
                if (foundNormalized.Contains(n)) continue;
                bool better = best == null || n.Length < bestLen ||
                              (n.Length == bestLen && (w.b < best.b || (w.b == best.b && string.CompareOrdinal(n, Normalize(best.p)) < 0)));
                if (better) { best = w; bestLen = n.Length; }
            }
            return best;
        }

        // ------------------------------------------------------------------ medidas

        /// <summary>
        /// ¿La palabra [cur] sale en RACIMO de la anterior? Comparte las 2 primeras letras, o la raíz (casa → casas, caso → casero:
        /// una es el comienzo de la otra sin la vocal o la "s" del final). Si no, es un SALTO.
        /// </summary>
        public static bool IsCluster(string previous, string current)
        {
            string a = Normalize(previous), b = Normalize(current);
            if (a.Length < 2 || b.Length < 2) return false;
            if (a.Substring(0, 2) == b.Substring(0, 2)) return true;
            string sa = Stem(a), sb = Stem(b);
            return sa.Length >= 3 && sb.Length >= 3 && (sa.StartsWith(sb, StringComparison.Ordinal) || sb.StartsWith(sa, StringComparison.Ordinal));
        }

        private static string Stem(string w)
        {
            if (w.EndsWith("es", StringComparison.Ordinal) && w.Length > 4) w = w.Substring(0, w.Length - 2);
            else if (w.EndsWith("s", StringComparison.Ordinal) && w.Length > 3) w = w.Substring(0, w.Length - 1);
            if (w.Length > 3 && "aeo".IndexOf(w[w.Length - 1]) >= 0) w = w.Substring(0, w.Length - 1);
            return w;
        }

        /// <summary>Porcentaje de palabras en racimo (0-100) entre las que tuvieron una anterior; -1 con menos de
        /// <see cref="MinClusterWords"/> palabras en total.</summary>
        public static int ClusterPercent(int clusters, int jumps, int totalWords)
        {
            if (totalWords < MinClusterWords || clusters + jumps <= 0) return -1;
            return (int)Math.Round(100.0 * clusters / (clusters + jumps));
        }

        /// <summary>Cuántas palabras se encontraron en los primeros y en los últimos <see cref="WindowSeconds"/> de una cosecha de [seconds].</summary>
        public static void Window(IReadOnlyList<float> times, int seconds, out int first, out int last)
        {
            first = last = 0;
            foreach (float t in times)
            {
                if (t < WindowSeconds) first++;
                if (t >= seconds - WindowSeconds) last++;
            }
        }

        /// <summary>"También podías": hasta [count] palabras comunes que no se encontraron, de las más usadas y cortas. Sin ocultas, sin repetir.</summary>
        public static List<string> Missed(IEnumerable<HarvestRound> rounds, IDictionary<string, HashSet<string>> foundByRound, int count)
        {
            var pool = new List<HarvestWord>();
            var seen = new HashSet<string>();
            foreach (var r in rounds)
            {
                foundByRound.TryGetValue(r.id, out var found);
                foreach (var w in r.palabras)
                {
                    if (!w.IsCommon || w.estrella) continue;
                    string n = Normalize(w.p);
                    if (found != null && found.Contains(n)) continue;
                    if (seen.Add(n)) pool.Add(w);
                }
            }
            pool.Sort((x, y) =>
            {
                int c = x.b.CompareTo(y.b);
                if (c != 0) return c;
                c = Normalize(x.p).Length.CompareTo(Normalize(y.p).Length);
                return c != 0 ? c : string.CompareOrdinal(Normalize(x.p), Normalize(y.p));
            });
            var res = new List<string>();
            foreach (var w in pool) { if (res.Count >= count) break; res.Add(w.p); }
            return res;
        }

        // ------------------------------------------------------------------ rondas de las últimas partidas

        /// <summary>Ids de las rondas de las últimas partidas: partidas separadas por ';' e ids por ','.</summary>
        public static List<List<string>> ParseRecent(string stored)
        {
            var games = new List<List<string>>();
            if (string.IsNullOrEmpty(stored)) return games;
            foreach (var g in stored.Split(';'))
            {
                var ids = new List<string>();
                foreach (var id in g.Split(','))
                    if (!string.IsNullOrWhiteSpace(id)) ids.Add(id.Trim());
                if (ids.Count > 0) games.Add(ids);
            }
            return games;
        }

        /// <summary>Agrega las rondas de esta partida y se queda con las últimas <see cref="RecentGames"/>.</summary>
        public static string PushRecent(string stored, IEnumerable<string> thisGame)
        {
            var games = ParseRecent(stored);
            var mine = new List<string>(thisGame);
            if (mine.Count > 0) games.Add(mine);
            while (games.Count > RecentGames) games.RemoveAt(0);
            var parts = new List<string>();
            foreach (var g in games) parts.Add(string.Join(",", g));
            return string.Join(";", parts);
        }

        public static HashSet<string> RecentIds(string stored)
        {
            var set = new HashSet<string>();
            foreach (var g in ParseRecent(stored)) foreach (var id in g) set.Add(id);
            return set;
        }
    }
}
