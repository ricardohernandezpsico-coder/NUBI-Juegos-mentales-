using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Contacto
{
    public enum WordKind { Noun, Color, Count }

    /// <summary>
    /// Una cosa en la escena: un objeto (<see cref="Obj"/>: 0-11 los del cielo, 12-27 los hallazgos), de un color de la
    /// paleta (<see cref="Color"/> 0..4; -1 = sus colores propios) y cuántas copias hay (<see cref="Count"/> 1..3).
    /// </summary>
    public struct Thing : IEquatable<Thing>
    {
        public int Obj, Color, Count;

        public Thing(int obj, int color = -1, int count = 1)
        {
            Obj = obj;
            Color = color;
            Count = count;
        }

        public bool Equals(Thing o) => Obj == o.Obj && Color == o.Color && Count == o.Count;
        public override bool Equals(object obj) => obj is Thing t && Equals(t);
        public override int GetHashCode() => Obj * 100 + (Color + 1) * 10 + Count;
        public override string ToString() => $"{Obj}/{Color}/{Count}";
    }

    /// <summary>Una escena: las cosas que muestran los nuri, cuál nombran y con qué palabras (en orden nuri).</summary>
    public sealed class ContactScene
    {
        public Thing[] Things;
        public int Target;
        public int[] Phrase;
        /// <summary>La palabra de la lección que esta escena trae (la que se está descifrando). -1 en el repaso.</summary>
        public int Focus;
    }

    /// <summary>
    /// Lo que la persona va sabiendo de cada palabra de la lección: cuántas veces la oyó, cuántas seguidas acertó (sin
    /// contar la primera) y si ya quedó descifrada.
    /// </summary>
    public sealed class WordProgress
    {
        public int Word;
        public int Hearings;
        public int Streak;
        public bool Decoded;
        /// <summary>Veces que se oyó hasta quedar descifrada (-1 = todavía no).</summary>
        public int HearingsToDecode = -1;
        /// <summary>Número de escena en que se oyó por última vez (para repartir las palabras).</summary>
        public int LastScene = -1;
        /// <summary>Se oyó <see cref="ContactContract.MaxHearings"/> veces sin salir: queda para otro día (no se insiste).</summary>
        public bool Parked;
        public bool Open => !Decoded && !Parked;
    }

    /// <summary>
    /// La lección de una partida: qué palabras ya se conocen, cuáles se van a descifrar y qué rasgos entran en las frases
    /// (el color, desde que se conoce o se aprende algún color; la cantidad, igual).
    /// </summary>
    public sealed class ContactLesson
    {
        public readonly HashSet<int> Known = new HashSet<int>();
        public readonly List<int> Targets = new List<int>();
        public readonly Dictionary<int, WordProgress> Progress = new Dictionary<int, WordProgress>();
        public bool ColorOn, CountOn;

        public bool Practice => Targets.Count == 0;

        /// <summary>¿El objeto ya tiene nombre (se conocía o se descifró en esta partida)?</summary>
        public bool IsNamed(int obj) => Known.Contains(ContactContract.NounWord(obj));

        /// <summary>¿Terminó la lección? (cada palabra descifrada o dejada para otro día).</summary>
        public bool Finished
        {
            get
            {
                foreach (int w in Targets) if (Progress[w].Open) return false;
                return true;
            }
        }

        public int DecodedCount
        {
            get
            {
                int n = 0;
                foreach (int w in Targets) if (Progress[w].Decoded) n++;
                return n;
            }
        }

        /// <summary>Palabras que pueden sonar en una frase: las conocidas y las de la lección.</summary>
        public bool Speakable(int word) => Known.Contains(word) || Targets.Contains(word);
    }

    /// <summary>
    /// Reglas puras de "Primer Contacto": aprender palabras de un idioma extraterrestre (el nuri) SIN que nadie diga qué
    /// significan, deduciéndolo de escena en escena. Es el aprendizaje de palabras entre situaciones con que los niños
    /// aprenden a hablar (Yu y Smith, 2007; Smith y Yu, 2008; aprendizaje estadístico: Saffran, Aslin y Newport, 1996):
    /// cada escena es ambigua (varias cosas y una frase), pero la cosa nombrada está SIEMPRE cuando suena su palabra.
    /// <list type="bullet">
    /// <item>En cada escena los nuri muestran 2-5 cosas y dicen una frase; la persona toca la que cree que nombraron. No
    /// hay "bien" o "mal" en el momento: una palabra queda <b>descifrada</b> cuando se acierta dos veces seguidas sin
    /// contar la primera vez que se oye (la primera vez siempre es adivinar). Así se deduce, como un detective, y la
    /// medida no depende de la suerte de una vez.</item>
    /// <item>Descartar lo que ya tiene nombre ayuda (exclusividad mutua, Markman y Wachtel, 1988): una palabra nueva
    /// suele nombrar una cosa sin nombre. El juego lo mide.</item>
    /// <item>El idioma crece por etapas: cosas del cielo, colores ("KITU RA" = cohete coral: una mini gramática), los
    /// hallazgos de la Bitácora y los números ("KITU RA NAS" = dos cohetes corales).</item>
    /// <item>Las palabras de otros días se repasan al empezar (recuerdo con demora): la que se olvidó vuelve a la
    /// lección.</item>
    /// </list>
    /// El vocabulario es FIJO (mismo idioma para todos: se puede conversar entre jugadores) y sus números no cambian nunca:
    /// la app guarda el diccionario de cada persona con ellos. Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class ContactContract
    {
        public const string GameId = "contacto";
        public const int MaxLevel = 10;

        /// <summary>Objetos: 0-11 los del cielo (mismo orden que <c>ShapeKind</c>), 12-27 los hallazgos de la Bitácora.</summary>
        public const int SkyObjects = 12;
        public const int FindObjects = 16;
        public const int ObjectCount = SkyObjects + FindObjects;
        public const int ColorCount = 5;
        public const int MaxCount = 3;

        /// <summary>Repaso al empezar: hasta cuántas palabras de otros días.</summary>
        public const int MaxReview = 4;

        // ------------------------------------------------------------------ el idioma nuri

        /// <summary>
        /// Las palabras, por número (NO reordenar: la app guarda el diccionario con estos números). 0-11 cosas del cielo,
        /// 12-16 colores, 17-32 hallazgos, 33-35 números. Palabras que se pronuncian fácil en español y no son palabras
        /// del español (se revisaron para que no suenen a nada).
        /// </summary>
        public static readonly string[] Words =
        {
            // cielo: planeta, cohete, cometa, estrella, luna, ovni, satélite, sol, casco, asteroide, telescopio, cristal
            "ZOBA", "KITU", "FEDI", "NUPA", "LIRO", "GAMU", "TEBI", "SUKE", "MOVA", "DAKO", "PIFO", "NELI",
            // colores: coral, sol, cielo, uva, lima
            "RA", "KI", "LU", "ZO", "PE",
            // hallazgos: llave, campana, pluma, concha, reloj de arena, brújula, farol, corona, bellota, libro, copa, gema,
            // hongo, ancla, estrella de mar, paraguas
            "KORU", "BIMA", "FULE", "ZENO", "TAMI", "REKU", "LOPA", "GISE", "BUKO", "MEZU", "PAVI", "NOKI", "SIRU", "DEFO",
            "ZUMI", "FOGI",
            // números: uno, dos, tres
            "TUN", "NAS", "BEL",
        };

        public const int WordCount = 36;
        private const int FirstColor = 12, FirstFind = 17, FirstCount = 33;

        /// <summary>Qué significa cada palabra, en español (con artículo en las cosas: "KITU es un cohete").</summary>
        public static readonly string[] Meanings =
        {
            "un planeta", "un cohete", "un cometa", "una estrella", "una luna", "un ovni", "un satélite", "un sol", "un casco",
            "un asteroide", "un telescopio", "un cristal",
            "coral", "sol", "cielo", "uva", "lima",
            "una llave", "una campana", "una pluma", "una concha", "un reloj de arena", "una brújula", "un farol", "una corona",
            "una bellota", "un libro", "una copa", "una gema", "un hongo", "un ancla", "una estrella de mar", "un paraguas",
            "uno", "dos", "tres",
        };

        /// <summary>Nombres de los colores de la paleta, en el orden de los valores 0..4.</summary>
        public static readonly string[] ColorNames = { "coral", "sol", "cielo", "uva", "lima" };

        /// <summary>
        /// Orden en que se aprende el idioma: primero cosas del cielo (fáciles de nombrar), luego colores (frases de dos
        /// palabras), más cosas del cielo y colores, los hallazgos, los números (frases de tres) y el resto.
        /// </summary>
        public static readonly int[] Curriculum =
        {
            0, 1, 2, 3, 4, 5, 6, 7,
            12, 13, 14,
            8, 9, 10, 11,
            15, 16,
            17, 18, 19, 20, 21, 22, 23, 24,
            33, 34, 35,
            25, 26, 27, 28, 29, 30, 31, 32,
        };

        public static WordKind KindOf(int word) =>
            word >= FirstCount ? WordKind.Count : word >= FirstFind ? WordKind.Noun : word >= FirstColor ? WordKind.Color : WordKind.Noun;

        /// <summary>Valor de la palabra: objeto (sustantivos), color 0..4 o cantidad 1..3.</summary>
        public static int ValueOf(int word)
        {
            if (word >= FirstCount) return word - FirstCount + 1;
            if (word >= FirstFind) return SkyObjects + (word - FirstFind);
            if (word >= FirstColor) return word - FirstColor;
            return word;
        }

        public static int NounWord(int obj) => obj < SkyObjects ? obj : FirstFind + (obj - SkyObjects);
        public static int ColorWord(int color) => FirstColor + color;
        public static int CountWord(int count) => FirstCount + count - 1;

        /// <summary>Las cosas del cielo se pintan de cualquier color de la paleta; los hallazgos tienen los suyos.</summary>
        public static bool Colorable(int obj) => obj >= 0 && obj < SkyObjects;

        /// <summary>Frase nuri para una cosa: nombre, color (si el color está en juego y la cosa lo tiene) y cantidad.</summary>
        public static int[] PhraseFor(Thing t, bool colorOn, bool countOn)
        {
            var p = new List<int> { NounWord(t.Obj) };
            if (colorOn && t.Color >= 0) p.Add(ColorWord(t.Color));
            if (countOn) p.Add(CountWord(t.Count));
            return p.ToArray();
        }

        public static string PhraseText(IReadOnlyList<int> phrase)
        {
            var parts = new string[phrase.Count];
            for (int i = 0; i < phrase.Count; i++) parts[i] = Words[phrase[i]];
            return string.Join(" ", parts);
        }

        /// <summary>¿La cosa tiene el rasgo que nombra la palabra?</summary>
        public static bool Matches(int word, Thing t)
        {
            int v = ValueOf(word);
            switch (KindOf(word))
            {
                case WordKind.Color: return t.Color == v;
                case WordKind.Count: return t.Count == v;
                default: return t.Obj == v;
            }
        }

        /// <summary>¿La cosa calza con la frase completa?</summary>
        public static bool MatchesPhrase(IReadOnlyList<int> phrase, Thing t)
        {
            foreach (int w in phrase) if (!Matches(w, t)) return false;
            return true;
        }

        // ------------------------------------------------------------------ dificultad

        private static readonly int[] LessonByLevel = { 3, 3, 4, 4, 5, 5, 6, 6, 7, 8 };
        private static readonly int[] ObjectsByLevel = { 2, 3, 3, 4, 4, 4, 5, 5, 5, 5 };

        /// <summary>Palabras nuevas por partida: 3 → 8.</summary>
        public static int LessonSize(int level) => LessonByLevel[Clamp(level) - 1];

        /// <summary>Cosas por escena: 2 → 5 (más cosas = cada escena dice menos).</summary>
        public static int ObjectsPerScene(int level) => ObjectsByLevel[Clamp(level) - 1];

        /// <summary>Tope de escenas de una partida (si alguna palabra no sale, la transmisión se corta y queda para otro día).</summary>
        public static int MaxScenes(int lessonSize) => lessonSize * 5 + 3;

        /// <summary>Veces que se oye una palabra, como mucho, en una partida: si no sale, queda para otro día (sin culpa).</summary>
        public const int MaxHearings = 7;

        /// <summary>Escenas de la conversación (cuando ya se sabe todo el idioma).</summary>
        public const int PracticeScenes = 12;

        // ------------------------------------------------------------------ lección

        /// <summary>
        /// Arma la lección: primero las palabras que se olvidaron en el repaso, después las siguientes que no se conocen,
        /// en el orden del <see cref="Curriculum"/>. Un color necesita alguna cosa del cielo con nombre y un número alguna
        /// cosa con nombre: si no la hay, se agrega a la lección antes.
        /// </summary>
        public static ContactLesson BuildLesson(IEnumerable<int> known, IEnumerable<int> faded, int level)
        {
            var lesson = new ContactLesson();
            if (known != null) foreach (int w in known) if (w >= 0 && w < WordCount) lesson.Known.Add(w);
            var fadedSet = new HashSet<int>();
            if (faded != null) foreach (int w in faded) if (w >= 0 && w < WordCount) { fadedSet.Add(w); lesson.Known.Remove(w); }

            int size = LessonSize(level);
            var wanted = new List<int>();
            foreach (int w in Curriculum) if (fadedSet.Contains(w)) wanted.Add(w);
            foreach (int w in Curriculum) if (!lesson.Known.Contains(w) && !fadedSet.Contains(w)) wanted.Add(w);

            foreach (int w in wanted)
            {
                if (lesson.Targets.Count >= size) break;
                var kind = KindOf(w);
                if (kind == WordKind.Color && !HasNoun(lesson, sky: true)) AddFirstNoun(lesson, wanted, sky: true);
                if (kind == WordKind.Count && !HasNoun(lesson, sky: false)) AddFirstNoun(lesson, wanted, sky: false);
                if (!lesson.Targets.Contains(w)) lesson.Targets.Add(w);
            }
            while (lesson.Targets.Count > size && lesson.Targets.Count > 1) lesson.Targets.RemoveAt(lesson.Targets.Count - 1);

            foreach (int w in lesson.Targets) lesson.Progress[w] = new WordProgress { Word = w };
            lesson.ColorOn = AnySpeakable(lesson, WordKind.Color);
            lesson.CountOn = AnySpeakable(lesson, WordKind.Count);
            return lesson;
        }

        private static bool HasNoun(ContactLesson l, bool sky)
        {
            for (int w = 0; w < WordCount; w++)
                if (KindOf(w) == WordKind.Noun && l.Speakable(w) && (!sky || Colorable(ValueOf(w)))) return true;
            return false;
        }

        private static void AddFirstNoun(ContactLesson l, List<int> wanted, bool sky)
        {
            foreach (int w in wanted)
                if (KindOf(w) == WordKind.Noun && !l.Targets.Contains(w) && (!sky || Colorable(ValueOf(w)))) { l.Targets.Add(w); return; }
            for (int w = 0; w < WordCount; w++)
                if (KindOf(w) == WordKind.Noun && !l.Targets.Contains(w) && (!sky || Colorable(ValueOf(w)))) { l.Targets.Add(w); return; }
        }

        private static bool AnySpeakable(ContactLesson l, WordKind kind)
        {
            for (int w = 0; w < WordCount; w++) if (KindOf(w) == kind && l.Speakable(w)) return true;
            return false;
        }

        /// <summary>
        /// Qué palabra trae la próxima escena: una de la lección que falte, de las que hace más que no suenan (entre las dos
        /// más "olvidadas", al azar, para que no sea un turno fijo), sin repetir la anterior si hay otra.
        /// </summary>
        public static int NextFocus(ContactLesson lesson, int previous, Random rng)
        {
            var open = new List<WordProgress>();
            foreach (int w in lesson.Targets) if (lesson.Progress[w].Open) open.Add(lesson.Progress[w]);
            if (open.Count == 0) return -1;
            if (open.Count > 1) open.RemoveAll(p => p.Word == previous);
            open.Sort((a, b) => a.LastScene != b.LastScene ? a.LastScene.CompareTo(b.LastScene) : a.Hearings.CompareTo(b.Hearings));
            int pick = open.Count >= 2 && rng.Next(3) == 0 ? 1 : 0;
            return open[pick].Word;
        }

        // ------------------------------------------------------------------ escenas

        /// <summary>
        /// Arma una escena que trae la palabra <paramref name="focus"/>: la cosa nombrada la tiene (y el resto de la frase son
        /// palabras conocidas cuando se puede), y las demás cosas son distintas en algo que la frase dice. La mitad de las
        /// otras cosas son "casi iguales" (solo cambia el rasgo que se está aprendiendo): así cada escena enseña algo.
        /// </summary>
        public static ContactScene MakeScene(ContactLesson lesson, int focus, int objects, Random rng)
        {
            objects = Math.Max(2, Math.Min(5, objects));
            var target = TargetFor(lesson, focus, rng);
            var phrase = PhraseFor(target, lesson.ColorOn, lesson.CountOn);
            var things = new List<Thing> { target };
            int guard = 0;
            while (things.Count < objects && guard++ < 400)
            {
                Thing foil = rng.Next(2) == 0 ? NearMiss(lesson, target, focus, rng) : RandomThing(lesson, rng);
                if (MatchesPhrase(phrase, foil) || things.Contains(foil)) continue;
                things.Add(foil);
            }
            // Mezcla: la nombrada puede quedar en cualquier lugar.
            for (int i = things.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (things[i], things[j]) = (things[j], things[i]);
            }
            return new ContactScene { Things = things.ToArray(), Target = things.IndexOf(target), Phrase = phrase, Focus = focus };
        }

        /// <summary>
        /// Repaso de una palabra de otro día: la palabra sola y cosas que se distinguen justo en eso (otras cosas, o la misma
        /// cosa de otros colores, o en otras cantidades).
        /// </summary>
        public static ContactScene ReviewScene(ContactLesson lesson, int word, Random rng)
        {
            var things = new List<Thing>();
            Thing target;
            switch (KindOf(word))
            {
                case WordKind.Color:
                {
                    int obj = PickNoun(lesson, rng, sky: true, preferKnown: true);
                    target = new Thing(obj, ValueOf(word));
                    var colors = new List<int> { 0, 1, 2, 3, 4 };
                    Shuffle(colors, rng);
                    things.Add(target);
                    foreach (int c in colors) if (things.Count < 4 && c != target.Color) things.Add(new Thing(obj, c));
                    break;
                }
                case WordKind.Count:
                {
                    int obj = PickNoun(lesson, rng, sky: false, preferKnown: true);
                    int color = Colorable(obj) ? rng.Next(ColorCount) : -1;
                    target = new Thing(obj, color, ValueOf(word));
                    for (int c = 1; c <= MaxCount; c++) things.Add(new Thing(obj, color, c));
                    break;
                }
                default:
                {
                    target = new Thing(ValueOf(word));
                    things.Add(target);
                    int guard = 0;
                    while (things.Count < 4 && guard++ < 200)
                    {
                        var t = new Thing(rng.Next(ObjectCount));
                        if (!things.Contains(t)) things.Add(t);
                    }
                    break;
                }
            }
            for (int i = things.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (things[i], things[j]) = (things[j], things[i]);
            }
            return new ContactScene { Things = things.ToArray(), Target = things.IndexOf(target), Phrase = new[] { word }, Focus = -1 };
        }

        private static Thing TargetFor(ContactLesson lesson, int focus, Random rng)
        {
            var kind = focus >= 0 ? KindOf(focus) : WordKind.Noun;
            int obj;
            if (focus >= 0 && kind == WordKind.Noun) obj = ValueOf(focus);
            else obj = PickNoun(lesson, rng, sky: kind == WordKind.Color, preferKnown: true);

            int color = -1;
            if (lesson.ColorOn && Colorable(obj))
                color = kind == WordKind.Color ? ValueOf(focus) : PickValue(lesson, WordKind.Color, rng);
            int count = 1;
            if (lesson.CountOn) count = kind == WordKind.Count ? ValueOf(focus) : PickValue(lesson, WordKind.Count, rng);
            return new Thing(obj, color, count);
        }

        /// <summary>Otra cosa igual a la nombrada salvo en el rasgo que se aprende (o en otro, si ese no puede cambiar).</summary>
        private static Thing NearMiss(ContactLesson lesson, Thing target, int focus, Random rng)
        {
            var t = target;
            var kind = focus >= 0 ? KindOf(focus) : WordKind.Noun;
            if (kind == WordKind.Color && lesson.ColorOn && Colorable(t.Obj)) t.Color = OtherValue(lesson, WordKind.Color, t.Color, rng, ColorCount, 0);
            else if (kind == WordKind.Count && lesson.CountOn) t.Count = OtherValue(lesson, WordKind.Count, t.Count, rng, MaxCount, 1);
            else
            {
                // Cambia la cosa: otra del mismo tipo de "pintura" (así el color se mantiene cuando lo hay).
                int guard = 0;
                do t.Obj = rng.Next(ObjectCount);
                while ((t.Obj == target.Obj || Colorable(t.Obj) != Colorable(target.Obj)) && guard++ < 50);
                if (!Colorable(t.Obj)) t.Color = -1;
            }
            return t;
        }

        /// <summary>Una cosa cualquiera: la mitad de las veces, una que ya tiene nombre (para poder descartar).</summary>
        private static Thing RandomThing(ContactLesson lesson, Random rng)
        {
            int obj;
            var named = new List<int>();
            for (int o = 0; o < ObjectCount; o++) if (lesson.IsNamed(o)) named.Add(o);
            obj = named.Count > 0 && rng.Next(2) == 0 ? named[rng.Next(named.Count)] : rng.Next(ObjectCount);
            int color = lesson.ColorOn && Colorable(obj) ? PickValue(lesson, WordKind.Color, rng) : -1;
            int count = lesson.CountOn ? PickValue(lesson, WordKind.Count, rng) : 1;
            return new Thing(obj, color, count);
        }

        private static int PickNoun(ContactLesson lesson, Random rng, bool sky, bool preferKnown)
        {
            var known = new List<int>();
            var speakable = new List<int>();
            for (int w = 0; w < WordCount; w++)
            {
                if (KindOf(w) != WordKind.Noun || (sky && !Colorable(ValueOf(w)))) continue;
                if (lesson.Known.Contains(w)) known.Add(ValueOf(w));
                if (lesson.Speakable(w)) speakable.Add(ValueOf(w));
            }
            var pool = preferKnown && known.Count > 0 ? known : speakable;
            if (pool.Count == 0) return rng.Next(sky ? SkyObjects : ObjectCount);
            return pool[rng.Next(pool.Count)];
        }

        /// <summary>Un valor de color o cantidad que se pueda decir (conocido con 70%, si hay).</summary>
        private static int PickValue(ContactLesson lesson, WordKind kind, Random rng)
        {
            var known = new List<int>();
            var speakable = new List<int>();
            for (int w = 0; w < WordCount; w++)
            {
                if (KindOf(w) != kind) continue;
                if (lesson.Known.Contains(w)) known.Add(ValueOf(w));
                if (lesson.Speakable(w)) speakable.Add(ValueOf(w));
            }
            if (known.Count > 0 && (rng.Next(10) < 7 || speakable.Count == known.Count)) return known[rng.Next(known.Count)];
            if (speakable.Count > 0) return speakable[rng.Next(speakable.Count)];
            return kind == WordKind.Count ? 1 : 0;
        }

        /// <summary>Otro valor distinto (de los que se pueden decir si hay dos o más; si no, cualquiera).</summary>
        private static int OtherValue(ContactLesson lesson, WordKind kind, int current, Random rng, int max, int min)
        {
            var pool = new List<int>();
            for (int w = 0; w < WordCount; w++)
                if (KindOf(w) == kind && lesson.Speakable(w) && ValueOf(w) != current) pool.Add(ValueOf(w));
            if (pool.Count == 0) for (int v = min; v < min + max; v++) if (v != current) pool.Add(v);
            return pool[rng.Next(pool.Count)];
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ------------------------------------------------------------------ respuestas

        /// <summary>
        /// Anota la respuesta de una escena: por cada palabra de la lección que sonó, si la cosa tocada tiene su rasgo.
        /// La primera vez que se oye no cuenta para la racha (es adivinar); dos aciertos seguidos después = descifrada.
        /// Devuelve las palabras que quedaron descifradas con esta respuesta.
        /// </summary>
        public static List<int> Answer(ContactLesson lesson, ContactScene scene, int tapped, int sceneIndex)
        {
            var decoded = new List<int>();
            var t = scene.Things[tapped];
            foreach (int w in scene.Phrase)
            {
                if (!lesson.Progress.TryGetValue(w, out var p) || !p.Open) continue;
                p.Hearings++;
                p.LastScene = sceneIndex;
                bool ok = Matches(w, t);
                if (p.Hearings >= 2 && ok) p.Streak++;
                else p.Streak = 0;
                if (p.Streak >= 2)
                {
                    p.Decoded = true;
                    p.HearingsToDecode = p.Hearings;
                    lesson.Known.Add(w);
                    decoded.Add(w);
                }
                else if (p.Hearings >= MaxHearings) p.Parked = true;
            }
            return decoded;
        }

        /// <summary>
        /// ¿Esta escena sirve para ver si se descarta lo que ya tiene nombre? Sí cuando trae una palabra de COSA todavía no
        /// descifrada y entre las otras cosas hay alguna que ya tiene nombre (evaluar ANTES de <see cref="Answer"/>).
        /// </summary>
        public static bool IsExclusionChance(ContactLesson lesson, ContactScene scene)
        {
            if (scene.Focus < 0 || KindOf(scene.Focus) != WordKind.Noun) return false;
            if (!lesson.Progress.TryGetValue(scene.Focus, out var p) || !p.Open) return false;
            for (int i = 0; i < scene.Things.Length; i++)
                if (i != scene.Target && scene.Things[i].Obj != scene.Things[scene.Target].Obj && lesson.IsNamed(scene.Things[i].Obj)) return true;
            return false;
        }

        /// <summary>¿Descartó? = no eligió una cosa que ya tenía nombre (evaluar ANTES de <see cref="Answer"/>).</summary>
        public static bool Excluded(ContactLesson lesson, ContactScene scene, int tapped) => !lesson.IsNamed(scene.Things[tapped].Obj);

        /// <summary>Escenas por palabra: cuántas veces se oyó, en promedio, cada palabra descifrada hasta descifrarla. -1 = ninguna.</summary>
        public static float MeanHearings(ContactLesson lesson)
        {
            int n = 0, sum = 0;
            foreach (int w in lesson.Targets)
            {
                var p = lesson.Progress[w];
                if (!p.Decoded) continue;
                n++;
                sum += p.HearingsToDecode;
            }
            return n == 0 ? -1f : (float)sum / n;
        }

        /// <summary>Lo mínimo para descifrar una palabra: oírla 3 veces (la primera es adivinar; después, dos aciertos seguidos).</summary>
        public const int MinHearings = 3;

        public static int DecodePoints(int level, int hearings) =>
            100 + 20 * (Clamp(level) - 1) + (hearings <= MinHearings ? 50 : hearings == MinHearings + 1 ? 20 : 0);

        /// <summary>Puntaje 0-100: palabras descifradas (60%), qué tan rápido (20%: 3 / escenas por palabra) y nivel (20%).</summary>
        public static int Score(int decoded, int lessonSize, float meanHearings, int level)
        {
            float d = lessonSize <= 0 ? 0f : Math.Max(0f, Math.Min(1f, (float)decoded / lessonSize));
            float speed = meanHearings <= 0f ? 0f : Math.Max(0f, Math.Min(1f, MinHearings / meanHearings));
            float lv = (float)(Clamp(level) - 1) / (MaxLevel - 1);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.6f * d + 0.2f * speed + 0.2f * lv) * 100f)));
        }

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
