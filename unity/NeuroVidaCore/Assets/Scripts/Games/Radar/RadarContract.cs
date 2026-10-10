using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Radar
{
    /// <summary>Los seis tipos de cápsula: cada uno con su forma, su color y su nombre FIJOS (la identidad nunca es solo el color). En una ronda no se repite ninguno.</summary>
    public enum CapsuleType { Hexagon, Drop, Circle, Square, Triangle, Diamond }

    /// <summary>Un objeto que aparece en el destello: una cápsula (con su tipo) o una roca gris. <see cref="X"/> e <see cref="Y"/> en dp desde el centro del radar (y hacia abajo); <see cref="Tilt"/> en radianes.</summary>
    public sealed class RadarObject
    {
        public CapsuleType Type;
        public bool IsRock;
        public float X, Y, Tilt;
    }

    /// <summary>Una ronda de Rescate relámpago: qué cápsulas y rocas aparecen, dónde (posiciones continuas al azar, solo para dibujarlas) y cuánto dura el destello.</summary>
    public sealed class RadarRound
    {
        public RadarObject[] Capsules;
        public RadarObject[] Rocks;
        public int ExposureMs;
        public int Level;
        /// <summary>«¡Lluvia de cápsulas!»: 4 cápsulas, sin rocas y destello FIJO de 300 ms, fuera del motor de dificultad: sirve para que «Tu captura» se pueda comparar entre partidas.</summary>
        public bool Rain;

        public int Count => Capsules.Length;
        public int Needed => RadarContract.Needed(Count);

        /// <summary>El tipo de cada cápsula de la ronda.</summary>
        public HashSet<CapsuleType> Types()
        {
            var set = new HashSet<CapsuleType>();
            foreach (var c in Capsules) set.Add(c.Type);
            return set;
        }
    }

    /// <summary>Cómo le fue a una respuesta: cápsulas elegidas que estaban y elegidas que no estaban («de más»).</summary>
    public struct RadarAnswer
    {
        public int Hits, Extras;
    }

    /// <summary>
    /// La geometría de la zona del destello para un radio de pantalla dado (v4, 9-oct): el radio donde aparecen las cápsulas es <c>glassR − 34</c> (112 con el radar de 146 dp de las pantallas altas, 94 con el de 128 del layout de 16:9), la distancia mínima entre centros
    /// y el tamaño de una cápsula crecen con el radar. Todo depende SOLO del radio de la pantalla (y la pantalla no depende del nivel): el radio de la zona es FIJO en los 12 niveles (regla permanente 2: la zona del destello nunca crece con el nivel).
    /// </summary>
    public readonly struct RadarGeometry
    {
        public readonly float GlassR, SpawnRadius, MinSeparation, CapsuleSize;

        private RadarGeometry(float glassR)
        {
            GlassR = glassR;
            SpawnRadius = glassR - 34f;
            MinSeparation = 66f + (glassR - 128f) / 3f;           // 66 con 128; 72 con 146
            CapsuleSize = 55f + (glassR - 128f) / 6f;             // 55 con 128; 58 con 146
        }

        public static RadarGeometry For(float glassR) => new RadarGeometry(glassR);

        /// <summary>El radar de 128 dp del layout de la Tarea 62 (16:9).</summary>
        public static RadarGeometry Reference => new RadarGeometry(RadarContract.RadarRadius);
    }

    /// <summary>
    /// Reglas puras de «Rescate relámpago: qué cápsulas viste» (id <c>radar</c>, renovado el 9-oct; docs/diseno-rescate.md): VELOCIDAD DE PROCESAMIENTO VISUAL con informe total tras una exposición breve con máscara (Sperling, 1960; teoría de la atención
    /// visual: Bundesen, 1990; Habekost, 2015; capacidad de unos 4 objetos: Luck y Vogel, 1997; Cowan, 2001; destello sin aviso = alerta propia: Penning et al., 2021).
    /// Un relámpago ilumina unas cápsulas de escape a la deriva en posiciones continuas al azar; la estática borra la imagen y en el tablero se elige QUÉ cápsulas se vieron (nunca DÓNDE): el tablero tiene un orden fijo por tipo y no tiene ninguna relación
    /// con las posiciones del radar. Reglas de patentes (docs/diseno-rescate.md §9; probadas): sin lugares fijos ni cuadrícula, el radio de la zona del destello NO crece con el nivel, los aros del radar son adorno, sin objetivo central, el contraste y el color
    /// de cápsulas, rocas y fondo son iguales en todos los niveles, y ninguna medida es por lugar. Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class RadarContract
    {
        public const string GameId = "radar";
        public const string Title = "Rescate relámpago";
        public const int MaxLevel = 12;

        /// <summary>Reto: 120 s. Precisión: 12 rondas (9 normales y 3 lluvias).</summary>
        public const int RetoSeconds = 120;
        public const int PrecisionRounds = 12;

        // ------------------------------------------------------------------ la zona del destello (dp; FIJA: no cambia con el nivel)

        /// <summary>Radio de la pantalla del radar (dp). Fijo en todos los niveles.</summary>
        public const float RadarRadius = 128f;
        /// <summary>Las cápsulas y las rocas aparecen a menos de esto del centro (RR − 34), con al menos <see cref="MinSeparation"/> entre centros.</summary>
        public const float SpawnRadius = RadarRadius - 34f;
        public const float MinSeparation = 66f;
        /// <summary>El tamaño de una cápsula en el radar (dp de diámetro).</summary>
        public const float CapsuleSize = 55f;

        // ------------------------------------------------------------------ una ronda

        /// <summary>Espera antes del relámpago: de 1,5 a 3,5 s al azar (el destello llega sin aviso).</summary>
        public const float WatchMinSeconds = 1.5f, WatchMaxSeconds = 3.5f;
        /// <summary>La estática dentro del disco (ms).</summary>
        public const int MaskMs = 350;
        /// <summary>La revelación (s).</summary>
        public const float RevealSeconds = 2.3f;
        /// <summary>Tope de espera de la respuesta (s): después se evalúa lo que haya, como en Satélites.</summary>
        public const float AnswerTimeoutSeconds = 25f;

        /// <summary>Cada cuántas rondas llega una lluvia (la 4, la 8, la 12…), con cuántas cápsulas y con qué destello FIJO.</summary>
        public const int RainEvery = 4, RainCapsules = 4, RainExposureMs = 300;

        public static bool IsRainRound(int index) => index >= 0 && (index + 1) % RainEvery == 0;

        // ------------------------------------------------------------------ los 12 niveles (solo cambian la cantidad, la duración y las rocas)

        private static readonly int[] CapsuleCounts = { 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4 };
        private static readonly int[] Exposures = { 800, 600, 600, 450, 350, 280, 280, 220, 180, 150, 120, 100 };
        private static readonly int[] RockCounts = { 0, 0, 0, 0, 0, 1, 1, 1, 2, 2, 2, 2 };

        public static int Capsules(int level) => CapsuleCounts[Clamp(level) - 1];
        public static int ExposureMs(int level) => Exposures[Clamp(level) - 1];
        public static int Rocks(int level) => RockCounts[Clamp(level) - 1];

        public static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));

        // ------------------------------------------------------------------ el tablero: orden FIJO por tipo

        public const int TypeCount = 6;

        /// <summary>El orden de los seis botones del tablero (3 × 2): el mismo siempre; nunca depende de las posiciones del radar ni de la ronda.</summary>
        public static readonly CapsuleType[] BoardOrder = { CapsuleType.Hexagon, CapsuleType.Drop, CapsuleType.Circle, CapsuleType.Square, CapsuleType.Triangle, CapsuleType.Diamond };

        public static readonly string[] TypeNames = { "Hexágono", "Gota", "Círculo", "Cuadrado", "Triángulo", "Rombo" };

        public static string TypeName(CapsuleType t) => TypeNames[(int)t];

        // ------------------------------------------------------------------ armar una ronda

        /// <summary>Una ronda del nivel. Con <paramref name="geo"/> (la geometría del radar de la pantalla) las posiciones salen en dp de ESA pantalla; sin ella, la del radar de 128 dp.</summary>
        public static RadarRound NextRound(int level, Random rng, RadarGeometry? geo = null)
        {
            level = Clamp(level);
            return Build(Capsules(level), Rocks(level), ExposureMs(level), level, false, rng, geo);
        }

        public static RadarRound RainRound(int level, Random rng, RadarGeometry? geo = null) => Build(RainCapsules, 0, RainExposureMs, Clamp(level), true, rng, geo);

        /// <summary>Una ronda a medida (la ronda de práctica del tutorial: no alimenta el motor ni las medidas).</summary>
        public static RadarRound Custom(int capsules, int rocks, int ms, Random rng, RadarGeometry? geo = null) => Build(capsules, rocks, ms, 1, false, rng, geo);

        private static RadarRound Build(int capsules, int rocks, int ms, int level, bool rain, Random rng, RadarGeometry? geo)
        {
            var types = new List<CapsuleType>();
            for (int t = 0; t < TypeCount; t++) types.Add((CapsuleType)t);
            for (int i = types.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (types[i], types[j]) = (types[j], types[i]);
            }
            var spots = Place(capsules + rocks, rng, geo);
            var round = new RadarRound { Capsules = new RadarObject[capsules], Rocks = new RadarObject[rocks], ExposureMs = ms, Level = level, Rain = rain };
            for (int i = 0; i < capsules; i++) round.Capsules[i] = new RadarObject { Type = types[i], X = spots[i].x, Y = spots[i].y, Tilt = Tilt(rng) };
            for (int i = 0; i < rocks; i++) round.Rocks[i] = new RadarObject { IsRock = true, X = spots[capsules + i].x, Y = spots[capsules + i].y, Tilt = Tilt(rng) };
            return round;
        }

        private static float Tilt(Random rng) => ((float)rng.NextDouble() - 0.5f) * 0.6f;

        /// <summary>
        /// Posiciones CONTINUAS al azar dentro de un disco de radio fijo (<see cref="SpawnRadius"/>), con al menos <see cref="MinSeparation"/> entre centros: sin lugares fijos, sin cuadrícula, sin anillos. Funciona SIEMPRE con 6 objetos (4 cápsulas y
        /// 2 rocas): primero se prueba al azar y, si no cupo (es raro con 6), se acomoda un anillo de seis más el centro, girado al azar y con un pequeño corrimiento (nunca «si no cabe, al centro»). Una prueba con 10.000 semillas lo garantiza.
        /// </summary>
        public static List<(float x, float y)> Place(int count, Random rng, RadarGeometry? geometry = null)
        {
            var geo = geometry ?? RadarGeometry.Reference;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                var pts = new List<(float x, float y)>();
                bool ok = true;
                for (int i = 0; i < count && ok; i++)
                {
                    bool placed = false;
                    for (int tries = 0; tries < 200 && !placed; tries++)
                    {
                        double a = rng.NextDouble() * Math.PI * 2.0, r = Math.Sqrt(rng.NextDouble()) * geo.SpawnRadius;
                        float x = (float)(r * Math.Cos(a)), y = (float)(r * Math.Sin(a));
                        if (!FarEnough(pts, x, y, geo.MinSeparation)) continue;
                        pts.Add((x, y));
                        placed = true;
                    }
                    ok = placed;
                }
                if (ok) return pts;
            }
            return Ring(count, rng, geo);
        }

        /// <summary>El plan B: un anillo de seis puntos a 70 dp del centro y el centro (siete lugares a ≥ 70 dp entre sí), girado al azar, con un corrimiento de hasta 1,2 dp; se eligen <paramref name="count"/> al azar.</summary>
        public static List<(float x, float y)> Ring(int count, Random rng, RadarGeometry? geometry = null)
        {
            float radius = (geometry ?? RadarGeometry.Reference).MinSeparation + 4f;          // 70 con el radar de 128 dp
            double turn = rng.NextDouble() * Math.PI * 2.0;
            var all = new List<(float x, float y)> { (0f, 0f) };
            for (int k = 0; k < 6; k++) all.Add(((float)(radius * Math.Cos(turn + k * Math.PI / 3.0)), (float)(radius * Math.Sin(turn + k * Math.PI / 3.0))));
            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }
            var pts = new List<(float x, float y)>();
            for (int i = 0; i < Math.Min(count, all.Count); i++)
                pts.Add((all[i].x + ((float)rng.NextDouble() * 2f - 1f) * 1.2f, all[i].y + ((float)rng.NextDouble() * 2f - 1f) * 1.2f));
            return pts;
        }

        private static bool FarEnough(List<(float x, float y)> pts, float x, float y, float minSeparation)
        {
            foreach (var p in pts)
            {
                float dx = p.x - x, dy = p.y - y;
                if (dx * dx + dy * dy < minSeparation * minSeparation) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ respuesta

        /// <summary>Compara los tipos elegidos con las cápsulas que había (las rocas no están en el tablero: no se pueden elegir).</summary>
        public static RadarAnswer Evaluate(RadarRound round, IEnumerable<CapsuleType> picks)
        {
            var types = round.Types();
            var a = new RadarAnswer();
            foreach (var p in new HashSet<CapsuleType>(picks))
            {
                if (types.Contains(p)) a.Hits++;
                else a.Extras++;
            }
            return a;
        }

        /// <summary>Cápsulas que hay que acertar (netas) para que la ronda cuente como lograda: todas hasta 3; con 4, todas menos una.</summary>
        public static int Needed(int capsules) => capsules <= 3 ? capsules : capsules - 1;

        /// <summary>Éxito para el motor: aciertos − elegidas que no estaban ≥ <see cref="Needed"/>.</summary>
        public static bool Success(int capsules, int hits, int extras) => hits - extras >= Needed(capsules);

        public static bool Success(RadarRound round, RadarAnswer a) => Success(round.Count, a.Hits, a.Extras);

        /// <summary>Ronda perfecta (para la celebración y la racha, no para el motor): todas las cápsulas y ninguna de más.</summary>
        public static bool Perfect(int capsules, int hits, int extras) => hits == capsules && extras == 0;

        // ------------------------------------------------------------------ medidas

        /// <summary>
        /// «Tu captura»: cuántas cápsulas se nombran bien de un vistazo (de 4), en las lluvias. Cada lluvia vale <c>aciertos − 2 × elegidas que no estaban</c>: en una lluvia hay 4 cápsulas entre 6 tipos, así que adivinar acierta 2 de cada 3 veces y con el factor 2
        /// (= 4 que estaban / 2 que no estaban) adivinar da 0 en promedio, y quien marca solo lo que vio obtiene exactamente lo que vio. Se promedia sin cortar cada lluvia en 0 (así no se infla el azar) y el total tiene mínimo 0. -1 con menos de 2 lluvias.
        /// </summary>
        public static float Capture(IReadOnlyList<int> rainRaws)
        {
            if (rainRaws == null || rainRaws.Count < MinRains) return -1f;
            float sum = 0f;
            foreach (int r in rainRaws) sum += r;
            return Math.Max(0f, sum / rainRaws.Count);
        }

        public const int MinRains = 2;

        public static int RainRaw(int hits, int extras) => hits - 2 * extras;

        /// <summary>Rondas normales iniciales que no cuentan para «Tu vistazo» (la escalera todavía busca), rondas finales que se promedian y las que hacen falta para mostrarlo (5 o más).</summary>
        public const int GlanceSkip = 3, GlanceWindow = 12, GlanceMin = 5;

        /// <summary>«Tu vistazo» (ms): media geométrica de las duraciones REALES del destello de las rondas normales desde la 4.ª (las últimas 12). -1 con menos de 5 rondas contadas.</summary>
        public static int GlanceMs(IReadOnlyList<float> realMs)
        {
            if (realMs == null || realMs.Count < GlanceSkip + GlanceMin) return -1;
            int from = Math.Max(GlanceSkip, realMs.Count - GlanceWindow);
            double logSum = 0;
            int n = 0;
            for (int i = from; i < realMs.Count; i++)
            {
                logSum += Math.Log(Math.Max(1.0, realMs[i]));
                n++;
            }
            return (int)Math.Round(Math.Exp(logSum / n));
        }

        /// <summary>Cuántas cápsulas había en promedio en las mismas rondas que usa <see cref="GlanceMs"/> («con N a la vez»). -1 si no hay vistazo.</summary>
        public static float GlanceLoad(IReadOnlyList<int> counts)
        {
            if (counts == null || counts.Count < GlanceSkip + GlanceMin) return -1f;
            int from = Math.Max(GlanceSkip, counts.Count - GlanceWindow);
            float sum = 0f;
            for (int i = from; i < counts.Count; i++) sum += counts[i];
            return sum / (counts.Count - from);
        }

        /// <summary>Puntaje 0-100: rondas logradas (50 %) y rapidez del vistazo (50 %, escala logarítmica entre 800 y 100 ms). Sin vistazo medido, solo las rondas logradas.</summary>
        public static int Score(float accuracy, int glanceMs)
        {
            float acc = Math.Max(0f, Math.Min(1f, accuracy));
            if (glanceMs <= 0) return (int)Math.Round(acc * 100f);
            double speed = Math.Log(800.0 / Math.Max(1, glanceMs)) / Math.Log(800.0 / 100.0);
            speed = Math.Max(0.0, Math.Min(1.0, speed));
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.5 * acc + 0.5 * speed) * 100.0)));
        }

        // ------------------------------------------------------------------ textos

        public const string WatchMessage = "Atento al relámpago…", AskHint = "Elige solo las que viste", RainMessage = "¡Lluvia de cápsulas!", RescueLabel = "¡Rescatar!";
        public const string RedoMessage = "Otra vez", RedoHint = "La pausa cortó el destello";
        public static string AskMessage(int count) => "¿Qué cápsulas viste? (eran " + count + ")";
        public static string AllSafe(int streak) => streak >= 3 ? "¡Todos a salvo! Racha ×" + streak : "¡Todos a salvo!";
        public static string Partial(int hits, int count, int extras) => "Rescataste " + hits + " de " + count + (extras > 0 ? " · " + extras + (extras == 1 ? " no estaba" : " no estaban") : "");
        public static string RoundChip(int round, int total) => total > 0 ? "Ronda " + Math.Min(round, total) + " de " + total : "Ronda " + round;
        public static string StreakChip(int streak) => "Racha ×" + streak;

        public const string EndTag = "¡RESCATE COMPLETO!";
        public static string EndTitle(int rescued) => rescued == 1 ? "1 cápsula a salvo" : rescued + " cápsulas a salvo";
        public const string EndCapture = "Tu captura", EndShortest = "Destello más corto resuelto", EndPerfect = "Rondas perfectas", EndStreak = "Racha mayor", EndTrips = "Viajes a la estación";
        /// <summary>El aviso de arriba cuando la nave se llena y parte a la estación (v4).</summary>
        public const string TripNotice = "¡Nave llena! Viaje a la estación";
        public static string TripsValue(int trips) => trips.ToString();
        public static string CaptureValue(float capture) => capture < 0f ? "—" : capture.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',') + " de 4";
        public static string ShortestValue(int ms) => ms <= 0 ? "—" : ms + " ms";
        public static string PerfectValue(int perfect, int rounds) => perfect + " de " + rounds;
        public static string StreakValue(int best) => best >= 2 ? "×" + best : "—";
        public const string EndNote1 = "Captura: cuántas nombras bien en la lluvia;", EndNote2 = "una que no estaba descuenta el doble.", EndNote3 = "Medida de esta partida. No es un diagnóstico.";
        public const string NewRecord = "¡Récord nuevo!";
        public static string RecordLine(int best) => "Tu récord: " + best + (best == 1 ? " cápsula" : " cápsulas");
        public const string CountdownSub = "Mira un instante y elige qué cápsulas viste";
    }

    /// <summary>
    /// La nave y sus viajes a la estación (v4, 9-oct; docs/diseno-rescate.md §16): tiene 10 ventanas y cada cápsula rescatada enciende la suya. Al terminar una revelación con 10 o más a bordo la nave viaja a la estación (2,0 s: sale llena por la derecha y vuelve vacía por la
    /// izquierda); las que no cupieron (si había 9 y entraron 4, sobran 3) pasan a la nave nueva. Durante el viaje no hay destellos, el tablero está apagado y el tiempo del Reto NO corre (<see cref="RetoClock.Shift"/>). Lógica pura.
    /// </summary>
    public static class RadarCargo
    {
        public const int Capacity = 10;
        public const float TripSeconds = 2.0f;

        /// <summary>Las cápsulas a bordo: las rescatadas que todavía no viajaron.</summary>
        public static int OnBoard(int rescued, int trips) => Math.Max(0, rescued - Capacity * trips);

        /// <summary>¿Toca viaje? Exactamente al llegar a 10 a bordo (o más: si entraron varias a la vez), después de una revelación.</summary>
        public static bool TripDue(int onBoard) => onBoard >= Capacity;

        /// <summary>Las ventanas encendidas (nunca más de 10).</summary>
        public static int Shown(int onBoard) => Math.Min(Capacity, Math.Max(0, onBoard));

        /// <summary>Las que no cupieron y pasan a la nave nueva al volver.</summary>
        public static int CarriedOver(int onBoard) => Math.Max(0, onBoard - Capacity);
    }

    /// <summary>El reloj del Reto (120 s): el viaje a la estación no descuenta tiempo (se corre el final lo que dura el viaje). Lógica pura.</summary>
    public sealed class RetoClock
    {
        private float _endsAt;

        public RetoClock(float now, float seconds) { _endsAt = now + seconds; }

        public float Remaining(float now) => _endsAt - now;

        public bool Finished(float now) => now >= _endsAt;

        /// <summary>Corre el final <paramref name="seconds"/> hacia adelante: lo que dura un viaje no cuenta.</summary>
        public void Shift(float seconds) { _endsAt += seconds; }
    }
}
