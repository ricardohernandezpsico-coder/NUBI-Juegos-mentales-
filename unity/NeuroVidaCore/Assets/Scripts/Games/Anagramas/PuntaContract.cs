using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroVida.Games.Anagramas
{
    /// <summary>Cómo se encontró una palabra: dice el color del lucero (docs/diseno-punta-de-la-lengua.md §3).</summary>
    public enum PuntaTier
    {
        /// <summary>Dorado: sola, sin ayudas.</summary>
        Solo,
        /// <summary>Plateado: con 1 o 2 ayudas (el largo y la primera letra).</summary>
        Pista,
        /// <summary>Cobre: con las letras justas (la 3.ª ayuda).</summary>
        Letras,
        /// <summary>Azul: se la mostró Nubi.</summary>
        Vista
    }

    /// <summary>
    /// Reglas puras de «En la punta de la lengua» (reemplaza a Anagramas, id <c>anagramas</c>; ver docs/diseno-punta-de-la-lengua.md): Nubi capta
    /// la definición de una palabra; se la busca en la memoria y se arma con letras. Una escalera de 3 ayudas aclara la señal y nunca deja trabado.
    /// </summary>
    public static class PuntaContract
    {
        public const string GameId = "anagramas";
        public const int MaxLevel = 5;
        /// <summary>Precisión: 8 palabras, sin reloj.</summary>
        public const int PrecisionWords = 8;
        /// <summary>Reto: 120 s, todas las que alcances.</summary>
        public const int RetoSeconds = 120;
        /// <summary>Ritmo completo del Reto (palabras en 120 s): da el techo de «alcance» del puntaje.</summary>
        public const int RetoTargetWords = 8;
        /// <summary>Las ayudas: 1 cuántas letras, 2 la primera letra, 3 solo sus letras. Después, el botón dice «Ver la palabra».</summary>
        public const int HelpSteps = 3;
        /// <summary>Palabras azules que esperan volver (las guarda la app).</summary>
        public const int MaxPending = 20;
        /// <summary>Cuántas azules vuelven como máximo en una partida de Precisión, y cada cuántas palabras vuelve una en el Reto.</summary>
        public const int MaxBlueInPrecision = 2;
        public const int BlueEveryInReto = 4;
        /// <summary>Cuántas palabras recientes se evitan entre partidas.</summary>
        public const int RecentMax = 80;

        /// <summary>El texto de la definición se escribe solo: una pausa y después ~22 ms por letra.</summary>
        public const float TypeDelaySeconds = 0.25f;
        public const float TypeSecondsPerLetter = 0.022f;

        // ------------------------------------------------------------------ dificultad

        private static readonly int[] ExtraByLevel = { 2, 2, 3, 3, 4 };

        /// <summary>Letras de más que traen las fichas con «¡La tengo!»: 2 en el nivel 1 … 4 en el 5; los mayores, una menos (mínimo 1).</summary>
        public static int ExtraLetters(int level, bool senior)
        {
            int n = ExtraByLevel[Math.Max(1, Math.Min(MaxLevel, level)) - 1];
            return senior ? Math.Max(1, n - 1) : n;
        }

        /// <summary>Letras que se usan de relleno (las más comunes del español: confunden sin ser injustas).</summary>
        private const string ExtraPool = "AEIOSRNLTCDMPUBGHFVZ";

        /// <summary>Las fichas con «¡La tengo!»: las de la palabra más <paramref name="extra"/> de relleno, mezcladas. El relleno prefiere letras que
        /// la palabra NO usa (así cada ficha de más se descarta por completo); nunca queda la palabra armada de entrada.</summary>
        public static char[] BuildTiles(string word, int extra, Random rng)
        {
            var all = new List<char>(word.ToCharArray());
            var pool = new List<char>();
            foreach (char c in ExtraPool) if (word.IndexOf(c) < 0) pool.Add(c);
            var fallback = new List<char>(ExtraPool);
            for (int i = 0; i < extra; i++)
            {
                var from = pool.Count > 0 ? pool : fallback;
                int k = rng.Next(from.Count);
                all.Add(from[k]);
                from.RemoveAt(k);
            }
            return Shuffle(all.ToArray(), word, rng);
        }

        /// <summary>Solo las letras de la palabra (3.ª ayuda), mezcladas y nunca ya en orden.</summary>
        public static char[] OnlyLetters(string word, Random rng) => Shuffle(word.ToCharArray(), word, rng);

        private static char[] Shuffle(char[] letters, string word, Random rng)
        {
            if (letters.Length < 3) return letters;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                for (int i = letters.Length - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    char t = letters[i]; letters[i] = letters[j]; letters[j] = t;
                }
                if (new string(letters) != word && !StartsWith(letters, word)) return letters;
            }
            Array.Reverse(letters);
            return letters;
        }

        private static bool StartsWith(char[] letters, string word)
        {
            if (letters.Length < word.Length) return false;
            for (int i = 0; i < word.Length; i++) if (letters[i] != word[i]) return false;
            return true;
        }

        // ------------------------------------------------------------------ cómo se encontró

        /// <summary>El color del lucero: mostrada = azul; sola = dorado; 1-2 ayudas = plateado; las 3 = cobre.</summary>
        public static PuntaTier TierOf(int helps, bool shownByNubi)
        {
            if (shownByNubi) return PuntaTier.Vista;
            if (helps <= 0) return PuntaTier.Solo;
            return helps <= 2 ? PuntaTier.Pista : PuntaTier.Letras;
        }

        /// <summary>Peso de cada color en el puntaje (no se ve en el juego: las ayudas solo cambian el color del lucero).</summary>
        public static float Weight(PuntaTier tier)
        {
            switch (tier)
            {
                case PuntaTier.Solo: return 1f;
                case PuntaTier.Pista: return 0.85f;
                case PuntaTier.Letras: return 0.6f;
                default: return 0f;
            }
        }

        /// <summary>Puntos de la palabra para el marcador del Reto: 100 / 70 / 40 / 0, más un empujón por racha (sola o con 1-2 ayudas).</summary>
        public static int PointsFor(PuntaTier tier, int streakAfter)
        {
            switch (tier)
            {
                case PuntaTier.Solo: return 100 + Math.Min(Math.Max(streakAfter - 1, 0), 5) * 10;
                case PuntaTier.Pista: return 70 + Math.Min(Math.Max(streakAfter - 1, 0), 5) * 10;
                case PuntaTier.Letras: return 40;
                default: return 0;
            }
        }

        /// <summary>Puntaje 0-100: peso de cada palabra / palabras jugadas. En el Reto, además, por el ritmo (hasta 8 palabras en 120 s).</summary>
        public static int Score(PuntaTally tally, bool reto)
        {
            if (tally.Total <= 0) return 0;
            float sum = 0f;
            foreach (var r in tally.Results) sum += Weight(r.Tier);
            float score = sum / tally.Total * 100f;
            if (reto) score *= Math.Min(1f, tally.Total / (float)RetoTargetWords);
            return Math.Max(0, Math.Min(100, (int)Math.Round(score)));
        }

        // ------------------------------------------------------------------ definición que se escribe

        /// <summary>Cuántas letras de la definición ya se escribieron a los <paramref name="elapsed"/> segundos de aparecer la tarjeta.</summary>
        public static int TypedCount(float elapsed, int total, bool reduceMotion)
        {
            if (reduceMotion) return total;
            if (elapsed <= TypeDelaySeconds) return 0;
            return Math.Min(total, (int)((elapsed - TypeDelaySeconds) / TypeSecondsPerLetter));
        }

        /// <summary>Segundos hasta que la definición está completa.</summary>
        public static float TypeSeconds(int total, bool reduceMotion) => reduceMotion ? 0f : TypeDelaySeconds + total * TypeSecondsPerLetter;

        // ------------------------------------------------------------------ validar

        /// <summary>Acierta si se armó exactamente la palabra (comparando las fichas, sin tilde).</summary>
        public static bool IsAccepted(string formedTiles, string targetTiles) => string.Equals(formedTiles, targetTiles, StringComparison.Ordinal);

        // ------------------------------------------------------------------ guardado simple (palabras separadas por «;»)

        public static List<string> ParseWords(string csv)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(csv)) return list;
            foreach (var part in csv.Split(';'))
            {
                string w = part.Trim();
                if (w.Length > 0 && !list.Contains(w)) list.Add(w);
            }
            return list;
        }

        public static string FormatWords(IEnumerable<string> words) => string.Join(";", words);

        /// <summary>Las usadas hoy al final de la lista de recientes (sin repetir), recortada a <see cref="RecentMax"/>.</summary>
        public static string PushRecent(string recentCsv, IEnumerable<string> usedInOrder)
        {
            var list = ParseWords(recentCsv);
            foreach (var w in usedInOrder)
            {
                list.Remove(w);
                list.Add(w);
            }
            if (list.Count > RecentMax) list.RemoveRange(0, list.Count - RecentMax);
            return FormatWords(list);
        }
    }

    /// <summary>
    /// Qué cuenta para el DDA común: sola o con 1-2 ayudas = acierto; con las letras justas cuenta MEDIO (una sí y otra no, desde la primera); mostrada por
    /// Nubi = error. El estado vive por partida.
    /// </summary>
    public sealed class PuntaCredit
    {
        private int _lettersSeen;

        public bool CountsAsHit(PuntaTier tier)
        {
            switch (tier)
            {
                case PuntaTier.Solo:
                case PuntaTier.Pista:
                    return true;
                case PuntaTier.Letras:
                    return _lettersSeen++ % 2 == 0;
                default:
                    return false;
            }
        }
    }

    public readonly struct PuntaResult
    {
        public readonly string Word;
        public readonly PuntaTier Tier;
        public PuntaResult(string word, PuntaTier tier) { Word = word; Tier = tier; }
    }

    /// <summary>Las cuentas de la partida para la medida del final (docs/diseno-punta-de-la-lengua.md §6): cuántas solas, con pista, con letras y mostradas,
    /// el tiempo medio hasta «¡La tengo!» de las que salieron solas, y las palabras azules que vuelven.</summary>
    public sealed class PuntaTally
    {
        public readonly List<PuntaResult> Results = new List<PuntaResult>();
        private readonly HashSet<string> _pending;
        private readonly List<string> _cleared = new List<string>();
        private long _soloMs;
        private int _soloCount;

        public PuntaTally(IEnumerable<string> pending = null)
        {
            _pending = new HashSet<string>(pending ?? new string[0]);
        }

        public int Count(PuntaTier tier)
        {
            int n = 0;
            foreach (var r in Results) if (r.Tier == tier) n++;
            return n;
        }

        public int Total => Results.Count;
        public int Solo => Count(PuntaTier.Solo);
        public int Pista => Count(PuntaTier.Pista);
        public int Letras => Count(PuntaTier.Letras);
        public int Vista => Count(PuntaTier.Vista);
        /// <summary>Palabras que la persona encontró (sola o con alguna ayuda, no mostradas).</summary>
        public int Found => Total - Vista;

        /// <summary><paramref name="thinkMs"/> = de que la definición terminó de escribirse a «¡La tengo!» (solo importa en las encontradas solas).</summary>
        public void Add(string word, PuntaTier tier, int thinkMs)
        {
            Results.Add(new PuntaResult(word, tier));
            if (tier == PuntaTier.Solo && thinkMs >= 0)
            {
                _soloMs += thinkMs;
                _soloCount++;
            }
            // una azul que volvió y se encontró sola o con 1-2 ayudas deja de esperar
            if ((tier == PuntaTier.Solo || tier == PuntaTier.Pista) && _pending.Contains(word) && !_cleared.Contains(word)) _cleared.Add(word);
        }

        /// <summary>Tiempo medio hasta «¡La tengo!» de las encontradas solas (ms); -1 si no hubo.</summary>
        public int MeanSoloMs => _soloCount > 0 ? (int)(_soloMs / _soloCount) : -1;

        public static char Code(PuntaTier tier)
        {
            switch (tier)
            {
                case PuntaTier.Solo: return 'o';
                case PuntaTier.Pista: return 'p';
                case PuntaTier.Letras: return 'c';
                default: return 'a';
            }
        }

        /// <summary>«palabra:o;otra:a»: cada palabra con su lucero (o oro, p plata, c cobre, a azul), en el orden jugado.</summary>
        public string WordsCsv()
        {
            var sb = new StringBuilder();
            foreach (var r in Results)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(r.Word).Append(':').Append(Code(r.Tier));
            }
            return sb.ToString();
        }

        /// <summary>Las que Nubi tuvo que mostrar (vuelven otro día).</summary>
        public string BlueCsv()
        {
            var list = new List<string>();
            foreach (var r in Results) if (r.Tier == PuntaTier.Vista && !list.Contains(r.Word)) list.Add(r.Word);
            return PuntaContract.FormatWords(list);
        }

        /// <summary>Las azules pendientes que esta partida encontró sola o con 1-2 ayudas.</summary>
        public string ClearedCsv() => PuntaContract.FormatWords(_cleared);
    }

    /// <summary>Elige las palabras de la partida: del nivel que pide el DDA, sin repetir y sin las de partidas recientes, alternando categorías, y con las
    /// azules pendientes que vuelven (Precisión: hasta 2, nunca de primera; Reto: una cada 4).</summary>
    public sealed class PuntaDirector
    {
        private readonly PuntaBank _bank;
        private readonly Random _rng;
        private readonly HashSet<string> _recent;
        private readonly HashSet<string> _used = new HashSet<string>();
        private readonly List<string> _usedInOrder = new List<string>();
        private readonly List<PuntaWord> _blue = new List<PuntaWord>();
        private readonly HashSet<int> _blueSlots = new HashSet<int>();
        private readonly bool _precision;
        private string _lastCategory = "";

        public IReadOnlyList<string> UsedInOrder => _usedInOrder;

        public PuntaDirector(PuntaBank bank, Random rng, IEnumerable<string> recent, IEnumerable<string> pending, bool precision)
        {
            _bank = bank;
            _rng = rng;
            _precision = precision;
            _recent = new HashSet<string>(recent ?? new string[0]);
            var pool = new List<PuntaWord>();
            foreach (var w in pending ?? new string[0])
            {
                var found = bank.Find(w);
                if (found != null && !pool.Contains(found)) pool.Add(found);
            }
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = pool[i]; pool[i] = pool[j]; pool[j] = t;
            }
            if (precision)
            {
                int n = Math.Min(PuntaContract.MaxBlueInPrecision, pool.Count);
                var slots = new List<int>();
                for (int s = 1; s < PuntaContract.PrecisionWords; s++) slots.Add(s);
                for (int i = 0; i < n; i++)
                {
                    int k = rng.Next(slots.Count);
                    _blueSlots.Add(slots[k]);
                    slots.RemoveAt(k);
                    _blue.Add(pool[i]);
                }
            }
            else _blue.AddRange(pool);
        }

        private bool IsBlueTurn(int index)
        {
            if (_blue.Count == 0) return false;
            return _precision ? _blueSlots.Contains(index) : index >= 2 && (index - 2) % PuntaContract.BlueEveryInReto == 0;
        }

        /// <summary>La palabra que sigue. <paramref name="index"/> = cuántas se jugaron ya; <paramref name="level"/> = el nivel que pide el DDA.</summary>
        public PuntaWord Next(int level, int index, out bool isBlue)
        {
            isBlue = false;
            if (IsBlueTurn(index))
            {
                PuntaWord blue = null;
                foreach (var b in _blue) if (!_used.Contains(b.Word)) { blue = b; break; }
                if (blue != null)
                {
                    _blue.Remove(blue);
                    isBlue = true;
                    return Take(blue);
                }
            }
            var pick = PickNormal(level);
            return Take(pick);
        }

        private PuntaWord Take(PuntaWord w)
        {
            _used.Add(w.Word);
            _usedInOrder.Add(w.Word);
            _lastCategory = w.Category;
            return w;
        }

        private PuntaWord PickNormal(int level)
        {
            // el nivel pedido; si se agotó, los vecinos (primero el de abajo, que es más amable)
            for (int spread = 0; spread < PuntaContract.MaxLevel; spread++)
            {
                foreach (int l in spread == 0 ? new[] { level } : new[] { level - spread, level + spread })
                {
                    if (l < 1 || l > PuntaContract.MaxLevel) continue;
                    var pick = From(_bank.OfLevel(l), true) ?? From(_bank.OfLevel(l), false);
                    if (pick != null) return pick;
                }
            }
            // todo usado: se vuelve a empezar
            _used.Clear();
            return From(_bank.OfLevel(level), false) ?? _bank.All[_rng.Next(_bank.All.Count)];
        }

        private PuntaWord From(IReadOnlyList<PuntaWord> list, bool skipRecent)
        {
            var all = new List<PuntaWord>();
            var other = new List<PuntaWord>();
            foreach (var w in list)
            {
                if (_used.Contains(w.Word) || (skipRecent && _recent.Contains(w.Word))) continue;
                all.Add(w);
                if (w.Category != _lastCategory) other.Add(w);
            }
            var from = other.Count > 0 ? other : all;
            return from.Count == 0 ? null : from[_rng.Next(from.Count)];
        }
    }
}
