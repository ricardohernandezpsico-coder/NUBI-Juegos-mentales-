using System;

namespace NeuroVida.Games.Piloto
{
    /// <summary>Las formas de las señales: sin estrellas con puntas, cruces ni medias lunas (docs/simbolos-neutros.md). Cada forma tiene SIEMPRE el mismo color: las parecidas (misma forma, otro detalle) se distinguen por el detalle, no por el color.</summary>
    public enum SignalShape { Hexagon, Drop, Circle, Square, Triangle }

    /// <summary>El detalle que lleva una señal dentro de su forma.</summary>
    public enum SignalDetail { Dot, Ring, Stripe }

    /// <summary>Una señal: qué es (forma y detalle), si es la de la misión y si nace en los costados.</summary>
    public readonly struct PilotSignalKind
    {
        public readonly SignalShape Shape;
        public readonly SignalDetail Detail;
        public readonly bool IsTarget, Peripheral;

        public PilotSignalKind(SignalShape shape, SignalDetail detail, bool isTarget, bool peripheral)
        {
            Shape = shape;
            Detail = detail;
            IsTarget = isTarget;
            Peripheral = peripheral;
        }
    }

    /// <summary>La misión de un sector: «hexágono con punto», por ejemplo.</summary>
    public readonly struct PilotMission : IEquatable<PilotMission>
    {
        public readonly SignalShape Shape;
        public readonly SignalDetail Detail;

        public PilotMission(SignalShape shape, SignalDetail detail)
        {
            Shape = shape;
            Detail = detail;
        }

        public bool Equals(PilotMission o) => Shape == o.Shape && Detail == o.Detail;
        public override bool Equals(object obj) => obj is PilotMission m && Equals(m);
        public override int GetHashCode() => (int)Shape * 31 + (int)Detail;
        public override string ToString() => PilotContract.MissionName(this);
    }

    /// <summary>
    /// Reglas puras de «Piloto Estelar: la ruta de las balizas» (renovado el 9-oct; ver docs/diseno-piloto.md). Un dedo, en la franja de abajo, guía la nave por una ruta de balizas; el otro atrapa SOLO las señales de la misión (forma y detalle).
    /// Las dos tareas van SIEMPRE juntas, desde el primer segundo hasta el final (regla permanente de Ricardo, 9-oct): ninguna se presenta sola dentro de una partida ni se mide sola, y no existe un «costo de multitarea».
    /// Cada tarea tiene su propio motor común de dificultad (pilotaje: ventanas de 1,5 s, objetivo 85 %; señales: por señal, objetivo 80 %). Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class PilotContract
    {
        public const string GameId = "piloto";
        public const string Title = "Piloto Estelar";
        public const int MaxLevel = 9;

        // ------------------------------------------------------------------ el vuelo

        /// <summary>Reto: vuelo de 90 s en 3 sectores de 30 s.</summary>
        public const int FlightSeconds = 90, SectorSeconds = 30, Sectors = 3;
        /// <summary>Inicio suave: los primeros 10 s la ruta usa el nivel de pilotaje − 2 y las señales salen más espaciadas y duran más. Las dos tareas siguen juntas.</summary>
        public const float GentleSeconds = 10f, GentleDriveLevels = 2f, GentleGapFactor = 1.4f, GentleExposureFactor = 1.25f;
        /// <summary>Precisión (sin reloj): velocidad × 0,8 y exposición × 1,3; termina tras 24 señales, con un sector cada 8.</summary>
        public const int PrecisionSignals = 24, PrecisionSectorEvery = 8;
        public const float PrecisionSpeedFactor = 0.8f, PrecisionExposureFactor = 1.3f;

        // ------------------------------------------------------------------ los dos motores comunes de dificultad (docs/DDA-comun.md)

        /// <summary>Pilotaje: un ensayo por ventana de 1,5 s, objetivo 85 % (el descenso por ventana fallada es 0,85 niveles). Señales: un ensayo por señal resuelta, objetivo 80 % (el descenso por error es 1 nivel).</summary>
        public const float DriveStepUp = 0.15f, DriveTarget = 0.85f, SignalStepUp = 0.25f, SignalTarget = 0.80f;

        // ------------------------------------------------------------------ pilotaje (dp lógicos de un campo de 360 de ancho)

        public const float FieldWidth = 360f;
        /// <summary>Una baliza cada 64 dp de ruta; cada una guarda su centro y su ancho al crearse (así un cambio de nivel no deforma lo visible).</summary>
        public const float BeaconGap = 64f;
        /// <summary>La nave cuenta como «dentro» a menos de (medio ancho − 10 dp) del centro.</summary>
        public const float InsideMargin = 10f;
        /// <summary>La nave sigue la x del dedo con inercia: factor por segundo del <c>lerp</c>.</summary>
        public const float ShipFollow = 9f;
        public const float ShipSize = 44f;
        public const float DriveWindowSeconds = 1.5f, DriveWindowPass = 0.85f;
        /// <summary>Distancia (dp) de la que la ruta no se acerca al borde del campo.</summary>
        public const float RouteEdgeMargin = 16f;
        /// <summary>La ruta serpentea en función de la distancia recorrida dividida por esto (dp): no depende del alto de la pantalla.</summary>
        public const float RouteWavelength = 640f;

        private static float Lv01(int level) => (Clamp(level) - 1) / (float)(MaxLevel - 1);

        /// <summary>Avance de la ruta (dp/s): 150 → 294.</summary>
        public static float Speed(int level, bool precision) => (150f + 144f * Lv01(level)) * (precision ? PrecisionSpeedFactor : 1f);

        /// <summary>Medio ancho de la ruta (dp): 104 → 54.</summary>
        public static float HalfWidth(int level) => 104f - 50f * Lv01(level);

        /// <summary>Cuánto serpentea (fracción del ancho): 0,10 → 0,30.</summary>
        public static float Curve(int level) => 0.10f + 0.20f * Lv01(level);

        /// <summary>El nivel de pilotaje con que se vuela: en el inicio suave, dos menos (mínimo 1).</summary>
        public static int DriveLevel(int level, bool gentle) => gentle ? Math.Max(1, level - (int)GentleDriveLevels) : Clamp(level);

        /// <summary>
        /// Centro de la ruta (dp, de 0 a <see cref="FieldWidth"/>) a la distancia <paramref name="p"/> (dp), con la curva <paramref name="curve"/> y el medio ancho <paramref name="half"/>: la suma de dos senos de frecuencias que no son múltiplos
        /// (no se repite a la vista), sin salirse nunca del campo.
        /// </summary>
        public static float CenterAt(float p, float curve, float half)
        {
            float d = p / RouteWavelength;
            float wave = 0.62f * (float)Math.Sin(d * 2.1f) + 0.38f * (float)Math.Sin(d * 3.7f + 1.3f);
            float x = FieldWidth * (0.5f + curve * wave);
            return Math.Max(half + RouteEdgeMargin, Math.Min(FieldWidth - half - RouteEdgeMargin, x));
        }

        /// <summary>¿Está la nave dentro de la ruta (con el margen interior de 10 dp)?</summary>
        public static bool Inside(float shipX, float center, float half) => Math.Abs(shipX - center) <= half - InsideMargin;

        // ------------------------------------------------------------------ señales

        /// <summary>Segundos entre una señal y la siguiente: 2,2 → 1,1.</summary>
        public static float Gap(int level, bool gentle) => (2.2f - 1.1f * Lv01(level)) * (gentle ? GentleGapFactor : 1f);

        /// <summary>Cuánto dura visible cada señal (ms): 1600 → 650.</summary>
        public static int ExposureMs(int level, bool precision, bool gentle)
        {
            float ms = 1600f - 950f * Lv01(level);
            if (precision) ms *= PrecisionExposureFactor;
            if (gentle) ms *= GentleExposureFactor;
            return (int)Math.Round(ms);
        }

        /// <summary>Probabilidad de que un distractor sea PARECIDO a la misión (misma forma, otro detalle): 0 en 1-2, 30 % en 3-4, 55 % desde el 5.</summary>
        public static float LookalikeChance(int level)
        {
            int l = Clamp(level);
            return l <= 2 ? 0f : l <= 4 ? 0.3f : 0.55f;
        }

        /// <summary>Desde el nivel 5 algunas señales (45 %) nacen en los bordes: campo visual útil.</summary>
        public static bool PeripheralLevel(int level) => Clamp(level) >= 5;
        public const float PeripheralChance = 0.45f;

        public const float TargetChance = 0.4f;
        /// <summary>El toque va a la señal viva más cercana dentro de esta distancia (dp; un toque de 80 dp de diámetro).</summary>
        public const float TouchRadius = 40f;
        /// <summary>El diámetro de una señal (dp) y el del anillo que se vacía con el tiempo que le queda.</summary>
        public const float SignalSize = 34f, SignalRingSize = 54f;

        public const int ShapeCount = 5, DetailCount = 3;

        public static PilotMission PickMission(Random rng, PilotMission? previous)
        {
            PilotMission m;
            do m = new PilotMission((SignalShape)rng.Next(ShapeCount), (SignalDetail)rng.Next(DetailCount));
            while (previous.HasValue && m.Equals(previous.Value));
            return m;
        }

        /// <summary>
        /// La próxima señal: el 40 % es de la misión; entre las demás, las parecidas (misma forma, OTRO detalle) con la probabilidad del nivel y el resto de otra forma con cualquier detalle. Como cada forma tiene su color fijo, las parecidas tienen el
        /// mismo color que la misión.
        /// </summary>
        public static PilotSignalKind NextSignal(int level, PilotMission mission, Random rng)
        {
            bool target = rng.NextDouble() < TargetChance;
            SignalShape shape = mission.Shape;
            SignalDetail detail = mission.Detail;
            if (!target)
            {
                if (rng.NextDouble() < LookalikeChance(level))
                {
                    do detail = (SignalDetail)rng.Next(DetailCount); while (detail == mission.Detail);
                }
                else
                {
                    do shape = (SignalShape)rng.Next(ShapeCount); while (shape == mission.Shape);
                    detail = (SignalDetail)rng.Next(DetailCount);
                }
            }
            bool peripheral = PeripheralLevel(level) && rng.NextDouble() < PeripheralChance;
            return new PilotSignalKind(shape, detail, target, peripheral);
        }

        // ------------------------------------------------------------------ puntos, hiperimpulso y medida

        public const int PointsPerCatch = 10;
        public const int HyperEvery = 5, HyperCleanWindows = 3;
        public const float HyperSeconds = 6f, HyperSpeedFactor = 1.15f;

        public static int Points(bool hyper) => PointsPerCatch * (hyper ? 2 : 1);

        /// <summary>Las señales de la misión que se atraparon menos los toques equivocados, sobre las señales de la misión (0 a 1; «reconocimiento corregido», Snodgrass y Corwin, 1988). null si hubo menos de <see cref="MinTargetsForMeasure"/>.</summary>
        public static float? SignalScore(int hits, int falseAlarms, int targets) =>
            targets < MinTargetsForMeasure ? (float?)null : Math.Max(0f, Math.Min(1f, (hits - falseAlarms) / (float)targets));

        public const int MinTargetsForMeasure = 8;

        /// <summary>Puntaje 0-100 del vuelo: señales (55 %) y tiempo dentro de la ruta (45 %); sin medida de señales cuenta solo la ruta.</summary>
        public static int Score(float? signalScore, float inLaneFraction)
        {
            float lane = Math.Max(0f, Math.Min(1f, inLaneFraction));
            if (!signalScore.HasValue) return (int)Math.Round(lane * 100f);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.55f * signalScore.Value + 0.45f * lane) * 100f)));
        }

        /// <summary>De las dos tareas (pilotaje y señales), la de menor proporción de aciertos: un Desafío en Piloto se supera solo si se superan las dos. Sin ensayos en una, cuenta la otra.</summary>
        public static (int hits, int trials) WeakerOf(int hitsA, int trialsA, int hitsB, int trialsB)
        {
            if (trialsA <= 0) return (hitsB, trialsB);
            if (trialsB <= 0) return (hitsA, trialsA);
            return (float)hitsA / trialsA <= (float)hitsB / trialsB ? (hitsA, trialsA) : (hitsB, trialsB);
        }

        // ------------------------------------------------------------------ sectores y textos

        public static readonly string[] SectorNames = { "Nebulosa azul", "Cinturón de hielo", "Mar de polvo coral", "Puerto lunar" };

        public static string SectorName(int sector) => SectorNames[((sector % SectorNames.Length) + SectorNames.Length) % SectorNames.Length];

        /// <summary>El sector (0..2) en que va el vuelo a los <paramref name="seconds"/> s (Reto) o tras <paramref name="resolved"/> señales (Precisión: un sector cada 8).</summary>
        public static int SectorAt(float seconds) => Math.Min(Sectors - 1, Math.Max(0, (int)(seconds / SectorSeconds)));
        public static int SectorAfter(int resolved) => Math.Min(Sectors - 1, Math.Max(0, resolved / PrecisionSectorEvery));

        public static readonly string[] ShapeNames = { "hexágono", "gota", "círculo", "cuadrado", "triángulo" };
        public static readonly string[] DetailNames = { "con punto", "con anillo", "con franja" };

        public static string MissionName(PilotMission m) => ShapeNames[(int)m.Shape] + " " + DetailNames[(int)m.Detail];

        public const string MissionLabel = "MISIÓN:", NewMissionLabel = "NUEVA MISIÓN:";
        public static string SectorHud(int sector) => "Sector " + (sector + 1) + " de " + Sectors + " · " + SectorName(sector);
        public static string SectorBannerTag(int sector) => "SECTOR " + (sector + 1);

        /// <summary>El aviso de misión (Tarea 63): arriba el título (en el primer sector todavía nada cambió: «¡Tu misión!»; en los otros, «¡Nueva misión!»), al medio la forma con su detalle dibujada y su nombre, abajo «Sector N · nombre».</summary>
        public const string FirstMissionTitle = "¡Tu misión!", NewMissionTitle = "¡Nueva misión!";
        public static string MissionNoticeTitle(int sector) => sector <= 0 ? FirstMissionTitle : NewMissionTitle;
        public static string MissionNoticeFoot(int sector) => "Sector " + (sector + 1) + " · " + SectorName(sector);

        /// <summary>
        /// El latido de la tarjeta de misión al cambiar (Tarea 63; 1,06 desde la Tarea 64: a 1,12 la tarjeta se salía por los bordes): escala 1 → 1,06 → 1, dos veces en 0,8 s (<see cref="PulseBeatSeconds"/> por latido). Fuera de esos 0,8 s vale 1. Con «quitar animaciones» el juego no lo usa (queda el brillo fijo y el tono).
        /// </summary>
        public const float PulseBeatSeconds = 0.4f, PulsePeak = 1.06f;
        public const int PulseBeats = 2;
        public static float MissionPulseScale(float secondsSinceChange)
        {
            if (secondsSinceChange < 0f || secondsSinceChange >= PulseBeats * PulseBeatSeconds) return 1f;
            float k = secondsSinceChange / PulseBeatSeconds;
            return 1f + (PulsePeak - 1f) * (float)Math.Sin(Math.PI * (k - Math.Floor(k)));
        }

        /// <summary>El chip de arriba del marcador: «Sector 2 de 3».</summary>
        public static string SectorChip(int sector) => "Sector " + (sector + 1) + " de " + Sectors;

        // ------------------------------------------------------------------ textos de la partida (todos a 14 dp o más)

        public const string CountdownSub = "Guía la nave y atrapa tu misión";
        public const string StripLabel = "‹  Desliza aquí  ›", StripLabelGentle = "‹  Desliza aquí para guiar la nave  ›", StripHint = "Con el otro dedo, toca solo tu misión";
        public const string WrongText = "No era de tu misión", GoneText = "Se fue";
        public const string HyperTitle = "¡HIPERIMPULSO!", HyperSub = "Puntos ×2";
        public const string HyperChip = " ×2";
        public static string PointsText(int points) => points.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.') + " pts";
        public static string PrecisionChip(int resolved, int total = PrecisionSignals) => Math.Min(resolved, total) + " de " + total;

        public const string EndTag = "LLEGASTE AL PUERTO";
        public static string EndTitle(int points) => PointsText(points);
        public const string EndLane = "En la ruta", EndMission = "Señales de tu misión", EndWrong = "Toques equivocados", EndLevel = "Tu nivel de señales", EndStreak = "Racha mayor", EndMeasure = "Tus señales a los mandos";
        public static string LaneValue(float fraction) => (int)Math.Round(Math.Max(0f, Math.Min(1f, fraction)) * 100f) + " %";
        public static string MissionValue(int hits, int targets) => hits + " de " + targets;
        public static string LevelValue(int level) => Clamp(level) + " de " + MaxLevel;
        public static string StreakValue(int best) => "×" + best;
        public static string MeasureValue(float? score) => score.HasValue ? (int)Math.Round(score.Value * 100f) + " %" : "—";
        public const string MeasureTooFew = "faltan señales";
        public const string EndNote1 = "Todo medido mientras hacías las dos cosas a la vez.", EndNote2 = "Medida de esta partida. No es un diagnóstico.";

        public static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
