using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Bitacora
{
    /// <summary>
    /// Generador de números con semilla propio (xorshift32): la misma semilla da SIEMPRE la misma misión, en cualquier
    /// versión de Unity o de .NET. Hace falta porque la misión se recibe en una partida (transmisión) y se informa en
    /// otra, minutos u horas después (informe): la app solo guarda la semilla y el nivel.
    /// </summary>
    public struct MissionRng
    {
        private uint _s;

        public MissionRng(int seed)
        {
            _s = (uint)seed ^ 0x9E3779B9u;
            if (_s == 0) _s = 0x6D2B79F5u;
            for (int i = 0; i < 4; i++) NextUInt(); // mezcla las semillas parecidas
        }

        public uint NextUInt()
        {
            _s ^= _s << 13;
            _s ^= _s >> 17;
            _s ^= _s << 5;
            return _s;
        }

        /// <summary>Entero en [0, max).</summary>
        public int Next(int max) => max <= 1 ? 0 : (int)(NextUInt() % (uint)max);

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    /// <summary>
    /// Una misión: planetas en el mapa, la ruta de la sonda (qué planetas visitó y en qué orden) y el hallazgo que dejó
    /// registrado en cada parada. Posiciones normalizadas del mapa (x 0..1 izquierda a derecha, y 0..1 arriba a abajo).
    /// </summary>
    public sealed class Mission
    {
        public int Seed, Level;
        /// <summary>Planetas del mapa: color (0..7, el mismo índice que los planetas-puerto de Tráfico Estelar).</summary>
        public int[] PlanetColors;
        public float[] PlanetX, PlanetY;
        /// <summary>Ruta: índices de planetas del mapa en el orden en que la sonda los visitó.</summary>
        public int[] Route;
        /// <summary>Hallazgo (0..FindCount-1) registrado en cada parada de la ruta.</summary>
        public int[] Finds;
        /// <summary>Hallazgos que se ofrecen al recordar: los de la misión y algunos que NO estuvieron, mezclados.</summary>
        public int[] Drawer;

        public int Items => Route.Length;
        public int Planets => PlanetColors.Length;

        /// <summary>Hallazgo registrado en ese planeta del mapa; -1 si la sonda no pasó por ahí.</summary>
        public int FindAt(int mapPlanet)
        {
            for (int i = 0; i < Route.Length; i++) if (Route[i] == mapPlanet) return Finds[i];
            return -1;
        }

        public bool IsInMission(int find) => Array.IndexOf(Finds, find) >= 0;
    }

    /// <summary>
    /// Reglas puras de "Bitácora de Misión": memoria episódica (qué, dónde y en qué orden) con recuerdo DIFERIDO. La
    /// persona recibe una transmisión (hallazgos en planetas, uno por uno, y guarda cada uno en la bitácora), hace un
    /// primer repaso con ayuda, y al rato (después de otros juegos de la sesión, o de una patrulla corta si juega suelto)
    /// informa: qué había en cada planeta y en qué orden pasó la sonda.
    /// <list type="bullet">
    /// <item>El recuerdo con demora es lo que miden las pruebas de memoria (aprendizaje y recuerdo diferido, tipo
    /// RAVLT); separar lo aprendido de lo guardado da la "retención". Recordar con pistas de lugar (planeta → hallazgo)
    /// mide la memoria asociativa "qué-dónde"; el orden de la ruta, el "cuándo".</item>
    /// <item>Practicar el recuerdo afianza (Roediger y Karpicke, 2006); imaginar una escena que une dos cosas ayuda a
    /// recordarlas (Bower, 1970): es el consejo del juego.</item>
    /// </list>
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class BitacoraContract
    {
        public const string GameId = "bitacora";
        public const int MaxLevel = 10;
        public const int FindCount = 16;
        public const int PlanetCount = 8;

        /// <summary>Patrulla (solo al jugar suelto): segundos de tarea de relleno entre el repaso y el informe.</summary>
        public const int PatrolSeconds = 45;

        /// <summary>Minutos mínimos entre la transmisión y el informe en la sesión diaria (si no terminó los juegos).</summary>
        public const int MinDailyDelayMinutes = 10;

        private static readonly int[] ItemsByLevel = { 3, 3, 4, 4, 5, 5, 6, 6, 7, 8 };

        /// <summary>Paradas de la misión (hallazgos a recordar): 3 → 8.</summary>
        public static int Items(int level) => ItemsByLevel[Clamp(level) - 1];

        /// <summary>Planetas del mapa que la sonda NO visita (sirven de confusión en la ruta): 0, 1 desde el nivel 3, 2 desde el 6.</summary>
        public static int ExtraPlanets(int level) => Math.Min(PlanetCount - Items(level), Clamp(level) >= 6 ? 2 : Clamp(level) >= 3 ? 1 : 0);

        /// <summary>Hallazgos que NO estuvieron en la misión y se ofrecen igual al recordar: 2 → 5.</summary>
        public static int Lures(int items) => Math.Max(2, Math.Min(5, items / 2 + 1));

        /// <summary>Lugares posibles de los planetas en el mapa (separados, sin montarse).</summary>
        private static readonly float[,] Slots =
        {
            { 0.17f, 0.16f }, { 0.50f, 0.10f }, { 0.83f, 0.18f }, { 0.30f, 0.40f }, { 0.70f, 0.38f },
            { 0.13f, 0.66f }, { 0.48f, 0.64f }, { 0.86f, 0.62f }, { 0.28f, 0.90f }, { 0.68f, 0.88f },
        };

        public static int SlotCount => Slots.GetLength(0);

        /// <summary>Arma la misión de esa semilla y ese nivel (determinista).</summary>
        public static Mission Generate(int seed, int level)
        {
            level = Clamp(level);
            var rng = new MissionRng(seed);
            int items = Items(level);
            int planets = Math.Min(PlanetCount, items + ExtraPlanets(level));

            var colors = new List<int>();
            for (int i = 0; i < PlanetCount; i++) colors.Add(i);
            rng.Shuffle(colors);
            var slots = new List<int>();
            for (int i = 0; i < SlotCount; i++) slots.Add(i);
            rng.Shuffle(slots);

            var m = new Mission
            {
                Seed = seed, Level = level,
                PlanetColors = new int[planets], PlanetX = new float[planets], PlanetY = new float[planets]
            };
            for (int p = 0; p < planets; p++)
            {
                m.PlanetColors[p] = colors[p];
                m.PlanetX[p] = Slots[slots[p], 0];
                m.PlanetY[p] = Slots[slots[p], 1];
            }

            // Ruta: los primeros "items" planetas del mapa (ya mezclados), en un orden al azar.
            var route = new List<int>();
            for (int p = 0; p < items; p++) route.Add(p);
            rng.Shuffle(route);
            m.Route = route.ToArray();

            var finds = new List<int>();
            for (int i = 0; i < FindCount; i++) finds.Add(i);
            rng.Shuffle(finds);
            m.Finds = finds.GetRange(0, items).ToArray();
            var drawer = finds.GetRange(0, items + Lures(items));
            rng.Shuffle(drawer);
            m.Drawer = drawer.ToArray();
            return m;
        }

        // ------------------------------------------------------------------ respuestas y medidas

        /// <summary>Qué paradas se recordaron bien (hallazgo correcto en su planeta). chosen[i] = hallazgo elegido para la parada i (-1 = ninguno).</summary>
        public static bool[] Check(Mission m, IReadOnlyList<int> chosen)
        {
            var ok = new bool[m.Items];
            for (int i = 0; i < m.Items && i < chosen.Count; i++) ok[i] = chosen[i] == m.Finds[i];
            return ok;
        }

        /// <summary>Hallazgos elegidos que NUNCA estuvieron en la misión (la memoria "completó un hueco").</summary>
        public static int Intrusions(Mission m, IReadOnlyList<int> chosen)
        {
            int n = 0;
            foreach (int c in chosen) if (c >= 0 && !m.IsInMission(c)) n++;
            return n;
        }

        /// <summary>Paradas de la ruta puestas en su lugar (tapped[i] = planeta del mapa elegido como parada i).</summary>
        public static int OrderCorrect(Mission m, IReadOnlyList<int> tapped)
        {
            int n = 0;
            for (int i = 0; i < m.Items && i < tapped.Count; i++) if (tapped[i] == m.Route[i]) n++;
            return n;
        }

        /// <summary>
        /// Retención: de lo que se aprendió en el primer repaso, qué parte se recordó después. -1 si no se aprendió nada
        /// (no hay qué retener).
        /// </summary>
        public static float Retention(IReadOnlyList<bool> learned, IReadOnlyList<bool> recalled)
        {
            int l = 0, kept = 0;
            for (int i = 0; i < learned.Count && i < recalled.Count; i++)
            {
                if (!learned[i]) continue;
                l++;
                if (recalled[i]) kept++;
            }
            return l == 0 ? -1f : (float)kept / l;
        }

        /// <summary>Máscara de bits (parada i = bit i), para que la app guarde qué se aprendió entre la transmisión y el informe.</summary>
        public static int Mask(IReadOnlyList<bool> flags)
        {
            int mask = 0;
            for (int i = 0; i < flags.Count && i < 31; i++) if (flags[i]) mask |= 1 << i;
            return mask;
        }

        public static bool[] FromMask(int mask, int items)
        {
            var f = new bool[items];
            for (int i = 0; i < items && i < 31; i++) f[i] = (mask & (1 << i)) != 0;
            return f;
        }

        public static int Count(IReadOnlyList<bool> flags)
        {
            int n = 0;
            foreach (bool b in flags) if (b) n++;
            return n;
        }

        public static int Points(bool correct, int level, int streak) =>
            correct ? 80 + 15 * (Clamp(level) - 1) + 20 * Math.Min(Math.Max(streak - 1, 0), 8) : 0;

        /// <summary>Puntaje 0-100: recuerdo en su lugar (60%), orden de la ruta (20%) y nivel (20%).</summary>
        public static int Score(float recallRate, float orderRate, int level)
        {
            float r = Math.Max(0f, Math.Min(1f, recallRate));
            float o = Math.Max(0f, Math.Min(1f, orderRate));
            float lv = (float)(Clamp(level) - 1) / (MaxLevel - 1);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.6f * r + 0.2f * o + 0.2f * lv) * 100f)));
        }

        // ------------------------------------------------------------------ textos

        /// <summary>Nombres de los planetas (mismo orden de colores que Tráfico Estelar: coral, sol, cielo, lima, uva, rosa, menta, naranja).</summary>
        public static readonly string[] PlanetNames = { "Coral", "Sol", "Cielo", "Lima", "Uva", "Rosa", "Menta", "Naranja" };

        /// <summary>Hallazgos, con artículo (se leen en la transmisión: "Planeta Coral · una campana").</summary>
        public static readonly string[] FindNames =
        {
            "una llave", "una campana", "una pluma", "una concha", "un reloj de arena", "una brújula", "un farol",
            "una corona", "una bellota", "un libro", "una copa", "una gema", "un hongo", "un ancla", "una estrella de mar",
            "un paraguas",
        };

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
