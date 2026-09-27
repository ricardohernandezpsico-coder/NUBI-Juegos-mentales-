using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroVida.Games.Constelacion
{
    public enum FluencyKind { Semantic, Letter }

    /// <summary>
    /// Una categoría de la ronda: "Animales" (semántica, con su lista de palabras y grupos) o "Palabras con P" (de letra:
    /// vale cualquier palabra que empiece con esa letra; los grupos son de sonido).
    /// </summary>
    public sealed class FluencyCategory
    {
        public string Id, Title, Prompt;
        public FluencyKind Kind;
        public char Letter;
        /// <summary>Solo semánticas: nombre visible de cada grupo ("del mar"), por índice.</summary>
        public readonly List<string> GroupNames = new List<string>();
        /// <summary>Solo semánticas: palabra que se muestra, por índice de palabra.</summary>
        public readonly List<string> Words = new List<string>();
        /// <summary>Solo semánticas: grupos de cada palabra (índices de <see cref="GroupNames"/>).</summary>
        public readonly List<List<int>> WordGroups = new List<List<int>>();
        /// <summary>Solo semánticas: forma normalizada (y variantes) → índice de palabra.</summary>
        public readonly Dictionary<string, int> Lookup = new Dictionary<string, int>();
        /// <summary>Largo máximo (en palabras) de una entrada: "estrella de mar" = 3.</summary>
        public int MaxTokens = 1;
    }

    /// <summary>Una palabra dicha en la ronda.</summary>
    public sealed class FluencyWord
    {
        /// <summary>Cómo se muestra ("tiburón").</summary>
        public string Display;
        /// <summary>Clave de "misma palabra": índice en la lista (semántica) o raíz normalizada (letra).</summary>
        public string Key;
        /// <summary>Grupos a los que pertenece: nombres de grupo (semántica) o rasgos de sonido (letra).</summary>
        public List<string> Groups = new List<string>();
        /// <summary>Segundos desde el comienzo de la ronda.</summary>
        public float Time;
        /// <summary>Ya se había dicho (no suma).</summary>
        public bool Repeat;
    }

    /// <summary>Una constelación: palabras seguidas del mismo grupo. <see cref="Count"/> 1 = palabra suelta.</summary>
    public struct FluencyCluster
    {
        public int Start, Count;
        /// <summary>El grupo que las une (nombre visible), o null si es una palabra suelta.</summary>
        public string Group;
        /// <summary>Tamaño a la manera de Troyer: se cuenta desde la segunda palabra (suelta = 0).</summary>
        public int Size => Count - 1;
    }

    /// <summary>
    /// Reglas puras de "Constelación de Palabras": FLUIDEZ VERBAL (decir en voz alta todas las palabras de una categoría,
    /// o que empiecen con una letra, en 60 segundos) puntuada en sus dos componentes según Troyer, Moscovitch y Winocur
    /// (1997): AGRUPAR (palabras seguidas del mismo grupo: tamaño medio de las constelaciones, contado desde la segunda
    /// palabra) y SALTAR (cambios de grupo). En español, las letras de fluidez fonológica habituales son P, M y R.
    /// <para>Lo que llega del reconocedor de voz es texto ("perro y gato un caballo"): se separa en palabras, se buscan
    /// las entradas de hasta 4 palabras ("estrella de mar"), se entienden plurales, diminutivos y femeninos regulares
    /// ("perritos", "leona") y variantes regionales (listadas en <see cref="FluencyLexicon"/>). Lo que no está en la
    /// lista no suma pero tampoco se castiga (puede faltar en la lista o ser un error del reconocedor).</para>
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class FluencyContract
    {
        public const string GameId = "constelacion";
        /// <summary>Duración de cada ronda (la de las pruebas de fluidez).</summary>
        public const float RoundSeconds = 60f;
        /// <summary>Largo mínimo de una palabra en las rondas de letra.</summary>
        public const int MinLetterWord = 3;

        /// <summary>Palabras de relleno al hablar ("perro y gato", "eh…"): no cuentan ni rompen nada.</summary>
        public static readonly HashSet<string> Fillers = new HashSet<string>
        {
            "y", "e", "o", "u", "el", "la", "los", "las", "lo", "un", "una", "unos", "unas", "de", "del", "al", "a",
            "que", "eh", "em", "ehh", "mm", "mmm", "este", "esta", "esto", "bueno", "ya", "otro", "otra", "tambien",
            "con", "pero", "pues", "porque", "para", "por", "mi", "me", "muy", "mas", "hay", "es", "son", "creo", "no",
            "si", "se", "le", "te", "ah", "oh", "haber", "veamos", "ver", "digo", "ademas",
        };

        private static readonly Dictionary<string, FluencyCategory> Cache = new Dictionary<string, FluencyCategory>();

        public static readonly string[] SemanticIds = { "animales", "frutas", "casa" };
        public static readonly char[] Letters = { 'p', 'm', 'r' };

        public static FluencyCategory Get(string id)
        {
            if (Cache.TryGetValue(id, out var c)) return c;
            switch (id)
            {
                case "animales": c = BuildSemantic(id, "Animales", "Di todos los animales que puedas", FluencyLexicon.Animals); break;
                case "frutas": c = BuildSemantic(id, "Frutas y verduras", "Di todas las frutas y verduras que puedas", FluencyLexicon.Produce); break;
                case "casa": c = BuildSemantic(id, "Cosas de la casa", "Di todas las cosas de la casa que puedas", FluencyLexicon.House); break;
                default:
                    if (id.Length == 7 && id.StartsWith("letra_", StringComparison.Ordinal))
                    {
                        char l = id[6];
                        c = new FluencyCategory
                        {
                            Id = id, Kind = FluencyKind.Letter, Letter = l,
                            Title = "Palabras con " + char.ToUpperInvariant(l),
                            Prompt = "Di palabras que empiecen con " + char.ToUpperInvariant(l) + " (sin nombres de personas ni lugares)",
                        };
                        break;
                    }
                    throw new ArgumentException("Categoría desconocida: " + id);
            }
            Cache[id] = c;
            return c;
        }

        // ------------------------------------------------------------------ lista de palabras

        private static FluencyCategory BuildSemantic(string id, string title, string prompt, string source)
        {
            var c = new FluencyCategory { Id = id, Title = title, Prompt = prompt, Kind = FluencyKind.Semantic };
            var canonicalIndex = new Dictionary<string, int>();
            foreach (var raw in source.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                int colon = line.IndexOf(':');
                int open = line.IndexOf('('), close = line.IndexOf(')');
                string groupName = line.Substring(open + 1, close - open - 1).Trim();
                int group = c.GroupNames.Count;
                c.GroupNames.Add(groupName);
                foreach (var item in line.Substring(colon + 1).Split(','))
                {
                    var forms = item.Split('|');
                    string display = forms[0].Trim();
                    if (display.Length == 0) continue;
                    string key = Normalize(display);
                    if (!canonicalIndex.TryGetValue(key, out int w))
                    {
                        w = c.Words.Count;
                        canonicalIndex[key] = w;
                        c.Words.Add(display);
                        c.WordGroups.Add(new List<int>());
                    }
                    if (!c.WordGroups[w].Contains(group)) c.WordGroups[w].Add(group);
                    foreach (var f in forms)
                    {
                        string n = Normalize(f);
                        if (n.Length == 0) continue;
                        c.Lookup[n] = w; // una variante repetida en otro grupo apunta a la misma palabra
                        c.MaxTokens = Math.Max(c.MaxTokens, n.Split(' ').Length);
                    }
                }
            }
            return c;
        }

        /// <summary>Minúsculas, sin tildes (la ñ se conserva), solo letras y un espacio entre palabras.</summary>
        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            bool space = false;
            foreach (char ch0 in s.ToLowerInvariant())
            {
                char ch = ch0;
                switch (ch)
                {
                    case 'á': case 'à': case 'ä': ch = 'a'; break;
                    case 'é': case 'è': case 'ë': ch = 'e'; break;
                    case 'í': case 'ì': case 'ï': ch = 'i'; break;
                    case 'ó': case 'ò': case 'ö': ch = 'o'; break;
                    case 'ú': case 'ù': case 'ü': ch = 'u'; break;
                }
                if (char.IsLetter(ch))
                {
                    if (space && sb.Length > 0) sb.Append(' ');
                    space = false;
                    sb.Append(ch);
                }
                else space = true;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Formas a probar para una palabra que no está tal cual en la lista: singular ("peces" → "pez", "leones" →
        /// "leon", "perros" → "perro"), diminutivo ("perrito" → "perro", "ratoncito" → "raton", "casita" → "casa") y
        /// femenino regular ("mona" → "mono"). Se prueban en orden; gana la primera que exista.
        /// </summary>
        public static List<string> Variants(string w)
        {
            var list = new List<string>();
            void Add(string v) { if (v.Length >= 2 && !list.Contains(v)) list.Add(v); }
            var singles = new List<string> { w };
            if (w.EndsWith("ces", StringComparison.Ordinal)) singles.Add(w.Substring(0, w.Length - 3) + "z");
            if (w.EndsWith("es", StringComparison.Ordinal)) singles.Add(w.Substring(0, w.Length - 2));
            if (w.EndsWith("s", StringComparison.Ordinal)) singles.Add(w.Substring(0, w.Length - 1));
            foreach (var s in singles)
            {
                Add(s);
                foreach (var (end, repl) in new[] { ("cito", ""), ("cita", ""), ("ecito", ""), ("quito", "co"), ("quita", "ca"),
                                                    ("guito", "go"), ("guita", "ga"), ("ito", "o"), ("ita", "a"), ("illo", "o"), ("illa", "a"), ("ito", "") })
                    if (s.EndsWith(end, StringComparison.Ordinal) && s.Length > end.Length + 1)
                        Add(s.Substring(0, s.Length - end.Length) + repl);
            }
            foreach (var v in list.ToArray())
                if (v.EndsWith("a", StringComparison.Ordinal)) Add(v.Substring(0, v.Length - 1) + "o");
            return list;
        }

        private static int Find(FluencyCategory c, string phrase)
        {
            if (c.Lookup.TryGetValue(phrase, out int w)) return w;
            // Variantes solo de la última palabra ("estrellas de mar" no; "perritos" sí).
            int sp = phrase.LastIndexOf(' ');
            string head = sp < 0 ? "" : phrase.Substring(0, sp + 1);
            string last = sp < 0 ? phrase : phrase.Substring(sp + 1);
            foreach (var v in Variants(last))
                if (c.Lookup.TryGetValue(head + v, out w)) return w;
            return -1;
        }

        // ------------------------------------------------------------------ una ronda

        /// <summary>
        /// Lee una frase del reconocedor y agrega a <paramref name="said"/> las palabras que cuentan (con
        /// <see cref="FluencyWord.Repeat"/> si ya se había dicho). Devuelve lo que no se pudo ubicar (no suma).
        /// <paramref name="alternatives"/>: otras lecturas del reconocedor; se usa la que ubique más palabras.
        /// </summary>
        public static List<string> Accept(FluencyCategory c, List<FluencyWord> said, string text, float time, IList<string> alternatives = null)
        {
            var best = Parse(c, text);
            if (alternatives != null)
                foreach (var alt in alternatives)
                {
                    var p = Parse(c, alt);
                    if (p.words.Count > best.words.Count) best = p;
                }
            var seen = new HashSet<string>();
            foreach (var w in said) seen.Add(w.Key);
            foreach (var w in best.words)
            {
                w.Time = time;
                w.Repeat = !seen.Add(w.Key);
                said.Add(w);
            }
            return best.unknown;
        }

        private static (List<FluencyWord> words, List<string> unknown) Parse(FluencyCategory c, string text)
        {
            var words = new List<FluencyWord>();
            var unknown = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return (words, unknown);
            // Se guardan las mayúsculas originales: en las rondas de letra, una palabra con mayúscula que no va al
            // principio es un nombre propio ("Pedro", "Perú") y no vale.
            var rawTokens = text.Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '¡', '¿', '-', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var tokens = new List<string>();
            var proper = new List<bool>();
            for (int i = 0; i < rawTokens.Length; i++)
            {
                string n = Normalize(rawTokens[i]);
                if (n.Length == 0) continue;
                tokens.Add(n);
                proper.Add(tokens.Count > 1 && char.IsUpper(rawTokens[i][0]));
            }

            if (c.Kind == FluencyKind.Letter)
            {
                foreach (var (t, i) in Indexed(tokens))
                {
                    if (Fillers.Contains(t)) continue;
                    if (t[0] != c.Letter || t.Length < MinLetterWord || proper[i]) { unknown.Add(t); continue; }
                    words.Add(new FluencyWord { Display = t, Key = Stem(t), Groups = SoundFeatures(t) });
                }
                return (words, unknown);
            }

            int k = 0;
            while (k < tokens.Count)
            {
                int found = -1, len = 0;
                for (int n = Math.Min(c.MaxTokens, tokens.Count - k); n >= 1 && found < 0; n--)
                {
                    found = Find(c, string.Join(" ", tokens.GetRange(k, n)));
                    if (found >= 0) len = n;
                }
                if (found >= 0)
                {
                    var groups = new List<string>();
                    foreach (int g in c.WordGroups[found]) groups.Add(c.GroupNames[g]);
                    words.Add(new FluencyWord { Display = c.Words[found], Key = c.Words[found], Groups = groups });
                    k += len;
                }
                else
                {
                    if (!Fillers.Contains(tokens[k]) && tokens[k].Length >= 3) unknown.Add(tokens[k]);
                    k++;
                }
            }
            return (words, unknown);
        }

        private static IEnumerable<(string, int)> Indexed(List<string> list)
        {
            for (int i = 0; i < list.Count; i++) yield return (list[i], i);
        }

        /// <summary>Raíz para no contar dos veces la misma palabra con otra terminación ("perro", "perritos").</summary>
        public static string Stem(string w)
        {
            var v = Variants(w);
            // La forma más corta que siga pareciéndose (singular sin diminutivo).
            string best = w;
            foreach (var s in v)
                if (s.Length < best.Length && s.Length >= Math.Min(w.Length, 3)) best = s;
            if (best.EndsWith("a", StringComparison.Ordinal) || best.EndsWith("o", StringComparison.Ordinal))
                best = best.Substring(0, best.Length - 1);
            return best;
        }

        /// <summary>
        /// Rasgos de sonido para agrupar en las rondas de letra (Troyer et al., 1997): empiezan igual (dos primeras letras),
        /// riman (tres últimas letras) o suenan casi igual (mismas consonantes, cambia una vocal: "pasa" / "pesa").
        /// </summary>
        public static List<string> SoundFeatures(string w)
        {
            var f = new List<string>();
            if (w.Length >= 2) f.Add("empiezan con «" + w.Substring(0, 2) + "»");
            if (w.Length >= 4) f.Add("terminan en «-" + w.Substring(w.Length - 3) + "»");
            var sk = new StringBuilder();
            foreach (char ch in w) sk.Append("aeiou".IndexOf(ch) >= 0 ? '·' : ch);
            f.Add("suenan parecido (" + sk + ")");
            return f;
        }

        // ------------------------------------------------------------------ medidas

        /// <summary>
        /// Constelaciones a la manera de Troyer: palabras SEGUIDAS que comparten un grupo (una palabra puede ser de varios
        /// grupos: la constelación sigue mientras quede algún grupo común a todas). Cuentan también las repetidas (son
        /// parte de la búsqueda); lo que no se pudo ubicar no entra.
        /// </summary>
        public static List<FluencyCluster> Clusters(IReadOnlyList<FluencyWord> words)
        {
            var result = new List<FluencyCluster>();
            int start = 0;
            HashSet<string> common = null;
            for (int i = 0; i < words.Count; i++)
            {
                var g = new HashSet<string>(words[i].Groups);
                if (common != null)
                {
                    var inter = new HashSet<string>(common);
                    inter.IntersectWith(g);
                    if (inter.Count > 0)
                    {
                        common = inter;
                        continue;
                    }
                    result.Add(Close(start, i - start, common));
                }
                start = i;
                common = g;
            }
            if (common != null) result.Add(Close(start, words.Count - start, common));
            return result;
        }

        private static FluencyCluster Close(int start, int count, HashSet<string> common)
        {
            string name = null;
            if (count > 1)
            {
                // Nombre estable: el primero en orden alfabético de los grupos comunes.
                var names = new List<string>(common);
                names.Sort(StringComparer.Ordinal);
                name = names.Count > 0 ? names[0] : null;
            }
            return new FluencyCluster { Start = start, Count = count, Group = name };
        }

        /// <summary>Tamaño medio de las constelaciones (desde la segunda palabra; sueltas = 0). -1 sin palabras.</summary>
        public static float MeanClusterSize(IReadOnlyList<FluencyCluster> clusters)
        {
            if (clusters.Count == 0) return -1f;
            float s = 0f;
            foreach (var c in clusters) s += c.Size;
            return s / clusters.Count;
        }

        /// <summary>Saltos: cambios de un grupo a otro (entre constelaciones, contando las palabras sueltas).</summary>
        public static int Switches(IReadOnlyList<FluencyCluster> clusters) => Math.Max(0, clusters.Count - 1);

        /// <summary>Palabras que suman (sin repetidas).</summary>
        public static int ValidCount(IReadOnlyList<FluencyWord> words)
        {
            int n = 0;
            foreach (var w in words) if (!w.Repeat) n++;
            return n;
        }

        /// <summary>Palabras que suman en cada cuarto de la ronda (0-15, 15-30, 30-45, 45-60 s): el ritmo.</summary>
        public static int[] Quarters(IReadOnlyList<FluencyWord> words, float seconds = RoundSeconds)
        {
            var q = new int[4];
            foreach (var w in words)
            {
                if (w.Repeat) continue;
                int i = Math.Max(0, Math.Min(3, (int)(w.Time / (seconds / 4f))));
                q[i]++;
            }
            return q;
        }

        /// <summary>Referencia de puntaje por ronda (no es una norma: solo lleva el puntaje a 0-100): 22 palabras en una
        /// categoría, 15 con una letra.</summary>
        public static float Reference(FluencyKind kind) => kind == FluencyKind.Semantic ? 22f : 15f;

        /// <summary>Puntaje 0-100: promedio de las rondas de palabras válidas / referencia.</summary>
        public static int Score(IReadOnlyList<int> validPerRound, IReadOnlyList<FluencyKind> kinds)
        {
            if (validPerRound.Count == 0) return 0;
            float s = 0f;
            for (int i = 0; i < validPerRound.Count; i++) s += Math.Min(1f, validPerRound[i] / Reference(kinds[i]));
            return Math.Max(0, Math.Min(100, (int)Math.Round(100f * s / validPerRound.Count)));
        }

        /// <summary>
        /// Las tres rondas de una partida: Animales (la categoría clásica) + otra categoría + una letra, sin repetir la
        /// misma combinación de la partida anterior si se puede. <paramref name="seed"/> la elige.
        /// </summary>
        public static string[] Plan(int seed)
        {
            var rng = new Random(seed);
            string other = SemanticIds[1 + rng.Next(SemanticIds.Length - 1)];
            char letter = Letters[rng.Next(Letters.Length)];
            return new[] { "animales", other, "letra_" + letter };
        }
    }
}
