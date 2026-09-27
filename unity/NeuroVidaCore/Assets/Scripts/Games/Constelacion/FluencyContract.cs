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
        /// <summary>Formas que el reconocedor suele escribir mal ("boa constructor"): se entienden, pero se muestra la
        /// palabra bien escrita. En la lista van con "~" delante.</summary>
        public readonly HashSet<string> ShowCanonical = new HashSet<string>();
        /// <summary>Cómo SUENA cada forma (ver <see cref="FluencyContract.Phonetic"/>) → palabra: para entender lo que el
        /// reconocedor escribió parecido ("fregata" → fragata, "ozocoala" → koala).</summary>
        public readonly Dictionary<string, int> PhoneticExact = new Dictionary<string, int>();
        public readonly List<(string key, int word, int tokens)> PhoneticList = new List<(string, int, int)>();
        /// <summary>Palabras que se le pasan al reconocedor para que las favorezca (las de la lista, como se escriben).</summary>
        public readonly List<string> BiasPhrases = new List<string>();
    }

    /// <summary>Una palabra dicha en la ronda.</summary>
    public sealed class FluencyWord
    {
        /// <summary>Cómo se muestra: tal como se dijo ("escorpión", "perritos", "tigre de bengala").</summary>
        public string Display;
        /// <summary>La palabra de la lista a la que corresponde ("alacrán" para "escorpión"; en las de letra, la dicha).
        /// Es la que entra a la colección "tu cielo de palabras".</summary>
        public string Canonical;
        /// <summary>Clave de "misma palabra": índice en la lista (semántica) o raíz normalizada (letra).</summary>
        public string Key;
        /// <summary>Grupos a los que pertenece: nombres de grupo (semántica) o rasgos de sonido (letra).</summary>
        public List<string> Groups = new List<string>();
        /// <summary>Segundos desde el comienzo de la ronda.</summary>
        public float Time;
        /// <summary>Ya se había dicho (no suma).</summary>
        public bool Repeat;
        /// <summary>En qué palabra de la frase (<see cref="FluencyContract.Tokenize"/>) empieza: con eso el juego le pone la
        /// hora en que apareció en los resultados parciales del reconocedor.</summary>
        public int TokenIndex;
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
                            Prompt = "Palabras que empiecen con " + char.ToUpperInvariant(l) + " · sin nombres propios",
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
                    foreach (var f0 in forms)
                    {
                        string f = f0.Trim();
                        bool misheard = f.StartsWith("~", StringComparison.Ordinal);
                        if (misheard) f = f.Substring(1);
                        string n = Normalize(f);
                        if (n.Length == 0) continue;
                        c.Lookup[n] = w; // una variante repetida en otro grupo apunta a la misma palabra
                        c.MaxTokens = Math.Max(c.MaxTokens, n.Split(' ').Length);
                        if (misheard) c.ShowCanonical.Add(n);
                        else if (!c.BiasPhrases.Contains(f)) c.BiasPhrases.Add(f);
                    }
                }
            }
            foreach (var kv in c.Lookup)
            {
                string ph = Phonetic(kv.Key);
                if (ph.Length == 0) continue;
                if (!c.PhoneticExact.ContainsKey(ph)) c.PhoneticExact[ph] = kv.Value;
                c.PhoneticList.Add((ph, kv.Value, kv.Key.Split(' ').Length));
            }
            return c;
        }

        /// <summary>
        /// Cómo suena una palabra en español, simplificado, para comparar lo que escribió el reconocedor con la lista:
        /// sin espacios ni h muda; b = v; s = z = c (ante e, i); k = c = qu; j = g (ante e, i); y = ll; letras dobles, una.
        /// ("avosetta" y "avoceta" suenan igual: "aboseta").
        /// </summary>
        public static string Phonetic(string normalized)
        {
            if (string.IsNullOrEmpty(normalized)) return "";
            string w = normalized.Replace(" ", "");
            w = w.Replace("ch", "#").Replace("h", "").Replace("#", "ch");
            w = w.Replace("ge", "je").Replace("gi", "ji").Replace("gue", "ge").Replace("gui", "gi");
            w = w.Replace("qu", "k").Replace("ce", "se").Replace("ci", "si").Replace("z", "s").Replace("x", "ks");
            w = w.Replace("ch", "#").Replace("c", "k").Replace("#", "ch");
            w = w.Replace("v", "b").Replace("ll", "y").Replace("w", "u");
            var sb = new StringBuilder(w.Length);
            foreach (char ch in w)
                if (sb.Length == 0 || sb[sb.Length - 1] != ch) sb.Append(ch);
            return sb.ToString();
        }

        /// <summary>Distancia de edición (letras que hay que cambiar, agregar o quitar), cortando si pasa de <paramref name="max"/>.</summary>
        public static int EditDistance(string a, string b, int max)
        {
            if (Math.Abs(a.Length - b.Length) > max) return max + 1;
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                int rowMin = cur[0];
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(prev[j] + 1, cur[j - 1] + 1), prev[j - 1] + cost);
                    rowMin = Math.Min(rowMin, cur[j]);
                }
                if (rowMin > max) return max + 1;
                var t = prev; prev = cur; cur = t;
            }
            return prev[b.Length];
        }

        /// <summary>Cuántas letras de diferencia se aceptan según el largo: cortas, ninguna ("dato" no es "gato").</summary>
        public static int FuzzyAllowance(int phoneticLength) => phoneticLength >= 9 ? 2 : phoneticLength >= 5 ? 1 : 0;

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

        private static int Find(FluencyCategory c, List<string> tokens) => Find(c, tokens, false, out _);

        /// <summary>
        /// Busca la palabra de la lista para estas palabras dichas. Primero tal cual y con sus variantes (plural,
        /// diminutivo...). Con <paramref name="fuzzy"/>, además, por cómo SUENA: igual, o con una letra de diferencia (dos
        /// en las largas). <paramref name="showCanonical"/>: mostrar la palabra de la lista (lo dicho estaba mal escrito).
        /// </summary>
        private static int Find(FluencyCategory c, List<string> tokens, bool fuzzy, out bool showCanonical)
        {
            showCanonical = false;
            string phrase = string.Join(" ", tokens);
            if (c.Lookup.TryGetValue(phrase, out int w)) { showCanonical = c.ShowCanonical.Contains(phrase); return w; }
            // Variantes de la última palabra ("perritos" → "perro") y, en las entradas largas, de la primera
            // ("estrellas de mar" → "estrella de mar").
            int n = tokens.Count;
            string head = n > 1 ? string.Join(" ", tokens.GetRange(0, n - 1)) + " " : "";
            var lastVariants = Variants(tokens[n - 1]);
            foreach (var v in lastVariants)
                if (c.Lookup.TryGetValue(head + v, out w)) { showCanonical = c.ShowCanonical.Contains(head + v); return w; }
            if (n > 1)
            {
                string tail = " " + string.Join(" ", tokens.GetRange(1, n - 1));
                foreach (var v in Variants(tokens[0]))
                    if (c.Lookup.TryGetValue(v + tail, out w)) return w;
            }
            if (!fuzzy) return -1;

            // Por cómo suena (lo que el reconocedor escribió parecido). Se muestra la palabra bien escrita.
            showCanonical = true;
            foreach (var v in lastVariants)
            {
                string key = Phonetic(head + v);
                if (key.Length < 3) continue;
                if (c.PhoneticExact.TryGetValue(key, out w)) return w;
            }
            foreach (var v in lastVariants)
            {
                string key = Phonetic(head + v);
                int allow = FuzzyAllowance(key.Length);
                if (allow == 0) continue;
                int best = -1, bestD = allow + 1;
                foreach (var (pk, word, toks) in c.PhoneticList)
                {
                    if (toks != n || pk[0] != key[0]) continue;
                    int d = EditDistance(key, pk, allow);
                    if (d < bestD) { bestD = d; best = word; }
                }
                if (best >= 0) return best;
            }
            showCanonical = false;
            return -1;
        }

        // ------------------------------------------------------------------ una ronda

        /// <summary>
        /// Lee una frase del reconocedor y agrega a <paramref name="said"/> las palabras que cuentan (con
        /// <see cref="FluencyWord.Repeat"/> si ya se había dicho), todas con la hora <paramref name="time"/>. Devuelve lo
        /// que no se pudo ubicar (no suma). <paramref name="alternatives"/>: otras lecturas del reconocedor; se usa la
        /// que ubique más palabras. (El juego usa <see cref="Read"/> para poner a cada palabra la hora en que apareció.)
        /// </summary>
        public static List<string> Accept(FluencyCategory c, List<FluencyWord> said, string text, float time, IList<string> alternatives = null)
        {
            var words = Read(c, said, text, alternatives, out var unknown);
            foreach (var w in words)
            {
                w.Time = time;
                said.Add(w);
            }
            return unknown;
        }

        /// <summary>
        /// Lee una frase SIN agregarla: las palabras que ubica (con <see cref="FluencyWord.TokenIndex"/> = en qué palabra
        /// de <see cref="Tokenize"/> empieza, y <see cref="FluencyWord.Repeat"/> respecto de <paramref name="said"/> y de
        /// lo anterior de la misma frase). Sirve para mostrar las estrellas mientras la persona habla (resultados
        /// parciales del reconocedor) y confirmarlas cuando la frase termina.
        /// </summary>
        public static List<FluencyWord> Read(FluencyCategory c, IReadOnlyList<FluencyWord> said, string text, IList<string> alternatives, out List<string> unknown)
        {
            var best = Parse(c, text);
            if (alternatives != null)
                foreach (var alt in alternatives)
                {
                    var p = Parse(c, alt);
                    if (p.words.Count > best.words.Count) best = p;
                }
            var seen = new HashSet<string>();
            if (said != null) foreach (var w in said) seen.Add(w.Key);
            foreach (var w in best.words) w.Repeat = !seen.Add(w.Key);
            unknown = best.unknown;
            return best.words;
        }

        /// <summary>Palabras de una frase, normalizadas, con la marca de "nombre propio" (mayúscula que no va primera).</summary>
        public static List<(string word, bool proper, string raw)> Tokenize(string text)
        {
            var list = new List<(string, bool, string)>();
            if (string.IsNullOrWhiteSpace(text)) return list;
            var raw = text.Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '¡', '¿', '-', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var r in raw)
            {
                string n = Normalize(r);
                if (n.Length == 0) continue;
                // "raw": como se dijo, en minúsculas y con tildes (para mostrar).
                var shown = new StringBuilder();
                foreach (char ch in r.ToLowerInvariant()) if (char.IsLetter(ch)) shown.Append(ch);
                list.Add((n, list.Count > 0 && char.IsUpper(r[0]), shown.ToString()));
            }
            return list;
        }

        /// <summary>
        /// Palabras que precisan un tipo sin ser un animal/cosa en sí ("tiburón blanco", "gato doméstico"). También vale
        /// "de/del + una palabra" ("tigre de bengala", "león del atlas", "gallina de guinea"). Solo se unen si van pegadas
        /// a lo que precisan; "elefante es muy común" no se une (hay palabras en medio).
        /// </summary>
        public static readonly HashSet<string> Modifiers = new HashSet<string>
        {
            "blanco", "negro", "gris", "pardo", "rojo", "azul", "verde", "dorado", "rosado", "amarillo", "cafe", "moteado",
            "manchado", "rayado", "grande", "gigante", "enano", "pequeño", "comun", "real", "salvaje", "domestico",
            "marino", "polar", "artico", "africano", "asiatico", "americano", "australiano", "europeo", "andino",
            "chileno", "siberiano", "tropical", "silvestre", "electrico", "volador", "nocturno", "calvo", "imperial",
            "emperador", "rey", "reina", "morado", "maduro",
        };

        private static bool IsModifier(string t)
        {
            if (Modifiers.Contains(t)) return true;
            // Femenino y plural de un adjetivo de la lista ("blanca", "negros", "africanas").
            foreach (var v in Variants(t)) if (Modifiers.Contains(v)) return true;
            return false;
        }

        private static (List<FluencyWord> words, List<string> unknown) Parse(FluencyCategory c, string text)
        {
            var words = new List<FluencyWord>();
            var unknown = new List<string>();
            var toks = Tokenize(text);
            if (toks.Count == 0) return (words, unknown);

            if (c.Kind == FluencyKind.Letter)
            {
                for (int i = 0; i < toks.Count; i++)
                {
                    var (t, proper, raw) = toks[i];
                    if (Fillers.Contains(t)) continue;
                    // En las rondas de letra, una palabra con mayúscula que no va al principio es un nombre propio.
                    if (t[0] != c.Letter || t.Length < MinLetterWord || proper) { unknown.Add(t); continue; }
                    words.Add(new FluencyWord { Display = raw, Canonical = raw, Key = Stem(t), Groups = SoundFeatures(t), TokenIndex = i });
                }
                return (words, unknown);
            }

            var tokens = new List<string>();
            foreach (var t in toks) tokens.Add(t.word);
            int k = 0;
            while (k < tokens.Count)
            {
                int found = -1, len = 0;
                bool asListed = false;
                // Primero tal cual (en cualquier largo); solo si nada calza, por cómo suena.
                for (int pass = 0; pass < 2 && found < 0; pass++)
                    for (int n = Math.Min(c.MaxTokens, tokens.Count - k); n >= 1 && found < 0; n--)
                    {
                        if (pass == 1 && Fillers.Contains(tokens[k])) break;
                        found = Find(c, tokens.GetRange(k, n), pass == 1, out asListed);
                        if (found >= 0) len = n;
                    }
                if (found >= 0)
                {
                    var groups = new List<string>();
                    foreach (int g in c.WordGroups[found]) groups.Add(c.GroupNames[g]);
                    int next = k + len;
                    // Un tipo: "tiburón blanco", "tigre de bengala" (cuenta como otra palabra, en los mismos grupos).
                    var extra = new List<string>();
                    if (next < tokens.Count && IsModifier(tokens[next]) && FindAt(c, tokens, next) < 0)
                    {
                        extra.Add(tokens[next]);
                        next++;
                    }
                    else if (next + 1 < tokens.Count && (tokens[next] == "de" || tokens[next] == "del") &&
                             !Fillers.Contains(tokens[next + 1]) && tokens[next + 1].Length >= 3 && FindAt(c, tokens, next + 1) < 0)
                    {
                        extra.Add(tokens[next]);
                        extra.Add(tokens[next + 1]);
                        next += 2;
                    }
                    var shown = new List<string>();
                    for (int t = k; t < next; t++) shown.Add(toks[t].raw);
                    string display = asListed
                        ? c.Words[found] + (next > k + len ? " " + string.Join(" ", shown.GetRange(len, shown.Count - len)) : "")
                        : string.Join(" ", shown);
                    string canonical = c.Words[found] + (extra.Count > 0 ? " " + string.Join(" ", shown.GetRange(len, shown.Count - len)) : "");
                    words.Add(new FluencyWord
                    {
                        Display = display, Canonical = canonical, Key = extra.Count > 0 ? Normalize(canonical) : c.Words[found],
                        Groups = groups, TokenIndex = k,
                    });
                    k = next;
                }
                else
                {
                    if (!Fillers.Contains(tokens[k]) && tokens[k].Length >= 3) unknown.Add(tokens[k]);
                    k++;
                }
            }
            return (words, unknown);
        }

        /// <summary>¿Empieza una entrada de la lista en la palabra <paramref name="at"/>?</summary>
        private static int FindAt(FluencyCategory c, List<string> tokens, int at)
        {
            for (int n = Math.Min(c.MaxTokens, tokens.Count - at); n >= 1; n--)
            {
                int f = Find(c, tokens.GetRange(at, n));
                if (f >= 0) return f;
            }
            return -1;
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
        /// Las dos rondas de una partida (un minuto cada una, como las pruebas de fluidez): una de categoría (Animales, la
        /// clásica, la mitad de las veces; si no, Frutas y verduras o Cosas de la casa) y una de letra (P, M o R).
        /// <paramref name="seed"/> la elige.
        /// </summary>
        public static string[] Plan(int seed)
        {
            var rng = new Random(seed);
            double r = rng.NextDouble();
            string semantic = r < 0.5 ? "animales" : r < 0.75 ? "frutas" : "casa";
            char letter = Letters[rng.Next(Letters.Length)];
            return new[] { semantic, "letra_" + letter };
        }
    }
}
