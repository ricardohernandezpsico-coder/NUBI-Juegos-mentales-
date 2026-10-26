using System;
using NeuroVida.Contracts;
using NeuroVida.Games.Acoplamiento;
using NeuroVida.Games.Disparate;
using NeuroVida.Games.Radar;
using NeuroVida.Games.Stroop;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El CONTRARRELOJ, fase 1 (Tarea 176, 26-oct; <c>docs/contrarreloj.md</c>, diseño de Fable en <c>auditoria-fable/contrarreloj.md</c> y su anexo): «La escalera de 2 minutos». Un modo para superarse uno mismo contra el tiempo, como el Puzzle Rush de chess.com: 120 s fijos, la dificultad adaptativa APAGADA y una
    /// PISTA FIJA —la misma para todas las personas ese día— que parte siempre del paso 1 y sube un paso cada N aciertos (no seguidos). Un error no baja de paso y NO hace esperar (solo da el aviso breve de error, <see cref="ErrorFeedbackSeconds"/>); la MARCA es «aciertos menos errores» con piso en 0 (Tarea 178: antes el error costaba una pausa
    /// de 3, 6 o 9 s y la marca eran solo los aciertos; Ricardo la encontró demasiado larga). Se dice «N aciertos en 2 minutos».
    /// Esta pieza es lógica PURA (sin escena): la pista (el paso por aciertos, la semilla de cada ensayo), la marca neta, el mínimo antes de poder responder en los juegos de dos opciones, el reloj y los textos. Cada juego (Tinta o Palabra, ¿Verdad o disparate?, Rescate relámpago, Acoplamiento) la usa si la config trae
    /// <c>time_trial</c>; <c>TimeTrialTests</c> prueba la pista y <c>TimeTrialFairnessTests</c> prueba que adivinar no gana (con la marca neta: media del azar menor que el 70 % de la del cuidado y su p90 menor que la media del cuidado, en los cuatro).
    /// </summary>
    public static class TimeTrial
    {
        /// <summary>La partida dura siempre esto (s): un solo tiempo hace al modo «bien definido» y a la marca legible.</summary>
        public const int Seconds = 120;
        public const string Name = "Contrarreloj";
        /// <summary>El subtítulo de la cuenta regresiva del Contrarreloj (en vez de la consigna del juego o de «Punto de partida»).</summary>
        public const string CountdownSub = "Contrarreloj de 2 minutos";

        /// <summary>
        /// Lo que dura el aviso de un error en Tinta o Palabra y en ¿Verdad o disparate? (s): se ve la corrección y se sigue; el reloj corre (en el Reto dura 1,5 s y alarga el reloj, aquí no). Acoplamiento deja su revelación de siempre (1,5 s) y Rescate no tiene aviso de más. Un error ya NO hace esperar (Tarea 178).
        /// </summary>
        public const float ErrorFeedbackSeconds = 0.8f;
        /// <summary>Antes de poder responder en los juegos de DOS opciones (cuyo azar acierta la mitad): ¿Verdad o disparate? deja leer la frase (1,5 s, o 0,25 s por palabra si es más larga) y en Acoplamiento el puerto se abre a los 1,2 s.</summary>
        public const float DisparateMinThinkSeconds = 1.5f, DisparateSecondsPerWord = 0.25f, DockingPortOpensSeconds = 1.2f;

#if UNITY_EDITOR
        /// <summary>SOLO el Editor (el smoke y las pruebas): acorta la partida para llegar al final. En el teléfono no existe.</summary>
        public static int SecondsOverride;
#endif

        /// <summary>Los segundos de la partida: <see cref="Seconds"/> (en el Editor, el smoke puede acortarlos).</summary>
        public static int TotalSeconds
        {
            get
            {
#if UNITY_EDITOR
                if (SecondsOverride > 0) return SecondsOverride;
#endif
                return Seconds;
            }
        }

        // ------------------------------------------------------------------ la pista de cada juego

        /// <summary>
        /// La pista de un juego: su VERSIÓN (si la pista o la regla de la marca cambian —los pasos, los generadores, cómo se cuenta—, se sube y el récord de la persona empieza de nuevo; el viejo queda en el detalle), cada cuántos aciertos sube un paso y cuántos pasos tiene. La versión también entra en la semilla: otra versión, otra pista.
        /// Versión 2 (Tarea 178): la marca es aciertos menos errores y el error ya no cuesta pausa.
        /// </summary>
        public readonly struct Track
        {
            public readonly string GameId;
            public readonly int Version, StepEvery, MaxLevel;

            public Track(string gameId, int version, int stepEvery, int maxLevel)
            {
                GameId = gameId;
                Version = version;
                StepEvery = stepEvery;
                MaxLevel = maxLevel;
            }
        }

        // Tinta o Palabra: 5 pasos, uno cada 8 aciertos (el 5.º llega al acierto 32). Los otros: 12 pasos, uno cada 3 (el 12.º, al acierto 33), y Acoplamiento cada 2 (el 12.º, al 22.º: con «cada 3» nadie pasaría del 8 en 2 minutos).
        private static readonly Track[] Tracks =
        {
            new Track(StroopContract.GameId, 2, 8, StroopContract.MaxLevel),
            new Track(DisparateContract.GameId, 2, 3, DisparateContract.MaxLevel),
            new Track(RadarContract.GameId, 2, 3, RadarContract.MaxLevel),
            new Track(DockingContract.GameId, 2, 2, DockingContract.MaxLevel),
        };

        /// <summary>Los juegos de la primera tanda, en el orden de la rotación del día (la app rota la fecha sobre esta lista).</summary>
        public static string[] GameIds
        {
            get
            {
                var ids = new string[Tracks.Length];
                for (int i = 0; i < Tracks.Length; i++) ids[i] = Tracks[i].GameId;
                return ids;
            }
        }

        public static bool Supports(string gameId) => TryTrack(gameId, out _);

        public static bool TryTrack(string gameId, out Track track)
        {
            foreach (var t in Tracks)
                if (t.GameId == gameId) { track = t; return true; }
            track = default;
            return false;
        }

        public static int TrackVersion(string gameId) => TryTrack(gameId, out var t) ? t.Version : 0;
        public static int MaxLevel(string gameId) => TryTrack(gameId, out var t) ? t.MaxLevel : 0;
        public static int StepEvery(string gameId) => TryTrack(gameId, out var t) ? t.StepEvery : 0;

        /// <summary>El paso de la pista con tantos aciertos: siempre desde el 1 y sube uno cada <c>StepEvery</c>; en el último se queda.</summary>
        public static int LevelFor(string gameId, int hits)
        {
            if (!TryTrack(gameId, out var t)) return 1;
            return Math.Min(t.MaxLevel, 1 + Math.Max(0, hits) / t.StepEvery);
        }

        /// <summary>El primer acierto que deja el paso <paramref name="level"/> (el 2.º paso llega a los N aciertos, el 3.º a los 2 N…).</summary>
        public static int HitsToReach(string gameId, int level) => TryTrack(gameId, out var t) ? Math.Max(0, level - 1) * t.StepEvery : 0;

        // ------------------------------------------------------------------ la marca y el mínimo antes de responder

        /// <summary>La MARCA de una partida: aciertos menos errores, con piso en 0 (nunca negativa). Es la misma fórmula que usa la app (<c>TimeTrialDay.markOf</c>; una prueba en cada lado la vigila con los mismos casos).</summary>
        public static int Mark(int hits, int errors) => Math.Max(0, hits - errors);

        /// <summary>Segundos desde que el ensayo es respondible hasta que se acepta la respuesta (0 = en cuanto se puede): ¿Verdad o disparate? (la frase se lee) y Acoplamiento (el puerto se abre).</summary>
        public static float MinThinkSeconds(string gameId, int words = 0)
        {
            if (gameId == DisparateContract.GameId) return Math.Max(DisparateMinThinkSeconds, DisparateSecondsPerWord * Math.Max(0, words));
            if (gameId == DockingContract.GameId) return DockingPortOpensSeconds;
            return 0f;
        }

        // ------------------------------------------------------------------ la semilla: fecha + versión de la pista + juego

        /// <summary>
        /// La semilla de la pista de un día (<paramref name="dayKey"/> = la fecha como número, aaaammdd): la fecha, la versión de la pista y el id del juego. Es una función pura y estable (SplitMix64): el mismo día, el mismo juego y la misma versión dan la misma pista en cualquier teléfono, y mañana otra.
        /// </summary>
        public static ulong Seed(int dayKey, string gameId)
        {
            unchecked
            {
                ulong h = 14695981039346656037UL;                                  // FNV-1a del id: así «stroop» y «radar» no comparten pista un mismo día
                foreach (char c in gameId ?? "") { h ^= c; h *= 1099511628211UL; }
                ulong x = (ulong)(uint)dayKey * 0x9E3779B97F4A7C15UL ^ ((ulong)(uint)TrackVersion(gameId) << 32) ^ h;
                return Mix(x);
            }
        }

        /// <summary>
        /// El azar del ENSAYO número <paramref name="index"/> de la pista (<paramref name="stream"/> separa dos usos dentro del mismo ensayo: el contenido y, por ejemplo, la espera del relámpago). Cada ensayo tiene su propia semilla —no un solo flujo que se va gastando—, así que quien falla un ensayo
        /// no desplaza lo que viene: el ensayo 12 usa el mismo azar para todos, y solo su contenido depende del paso en que esté la persona.
        /// </summary>
        public static Random TrialRng(ulong seed, int index, int stream = 0)
        {
            unchecked
            {
                return new TrackRandom(Mix(seed ^ ((ulong)(uint)index * 0xD1B54A32D192ED03UL) ^ ((ulong)(uint)stream * 0x8CB92BA72F3D8DD7UL)));
            }
        }

        /// <summary>SplitMix64: una vuelta que mezcla bien todos los bits.</summary>
        internal static ulong Mix(ulong x)
        {
            unchecked
            {
                x += 0x9E3779B97F4A7C15UL;
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }

        /// <summary>
        /// Un <see cref="System.Random"/> con SU PROPIO algoritmo (SplitMix64): la pista no depende de la implementación de <c>System.Random</c> de cada versión de Unity, Mono o .NET, y un servidor podrá reproducirla igual (la fase 2 del Contrarreloj vuelve a jugar la pista para recalcular los aciertos).
        /// Los generadores de los juegos (<c>Sequencer.Next</c>, <c>NextRound</c>, <c>NextTrial</c>, <c>SentenceBank.Pick</c>) reciben un <see cref="Random"/> y usan solo <c>Next(n)</c> y <c>NextDouble()</c>.
        /// </summary>
        public sealed class TrackRandom : Random
        {
            private ulong _state;

            public TrackRandom(ulong seed) : base(0) { _state = seed; }

            private ulong NextU64()
            {
                unchecked
                {
                    _state += 0x9E3779B97F4A7C15UL;
                    ulong z = _state;
                    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                    return z ^ (z >> 31);
                }
            }

            protected override double Sample() => (NextU64() >> 11) * (1.0 / 9007199254740992.0);          // 53 bits: [0, 1)
            public override double NextDouble() => Sample();
            public override int Next() => (int)(NextU64() >> 33);
            public override int Next(int maxValue) => maxValue <= 0 ? 0 : Math.Min(maxValue - 1, (int)(Sample() * maxValue));
            public override int Next(int minValue, int maxValue) => maxValue <= minValue ? minValue : minValue + Next(maxValue - minValue);

            public override void NextBytes(byte[] buffer)
            {
                for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)(NextU64() >> 56);
            }
        }

        // ------------------------------------------------------------------ la partida

        /// <summary>
        /// Lo que lleva la cuenta de un Contrarreloj en marcha (sin escena): aciertos, errores, la marca y el paso. Si la config trae <c>time_trial</c> de un juego que lo tiene, <see cref="Begin"/> arma una y fuerza <c>timed</c> (siempre es contra el reloj); si no, devuelve null y el juego juega como siempre.
        /// </summary>
        public sealed class Run
        {
            public readonly string GameId;
            public readonly ulong Seed;
            public readonly int DayKey;
            /// <summary>Ensayos ya resueltos (los aciertos y los errores): el ensayo que viene lleva este número.</summary>
            public int Trials { get; private set; }
            public int Hits { get; private set; }
            public int Errors { get; private set; }
            /// <summary>La marca de ahora: aciertos menos errores, con piso en 0. Es lo que muestra el chip y lo que se compara.</summary>
            public int Score => Mark(Hits, Errors);
            /// <summary>El paso más alto que alcanzó la pista en la partida (nunca baja).</summary>
            public int PeakLevel { get; private set; } = 1;
            private int _shownScore = -1, _shownSecond = -1;

            public Run(string gameId, int dayKey)
            {
                GameId = gameId;
                DayKey = dayKey;
                Seed = TimeTrial.Seed(dayKey, gameId);
            }

            /// <summary>El paso de la pista en que está la persona ahora.</summary>
            public int Level => LevelFor(GameId, Hits);

            /// <summary>Registra un ensayo resuelto; true si con él SUBIÓ de paso.</summary>
            public bool Register(bool correct)
            {
                int before = Level;
                Trials++;
                if (correct) Hits++;
                else Errors++;
                int after = Level;
                PeakLevel = Math.Max(PeakLevel, after);
                return after > before;
            }

            /// <summary>El azar del ensayo que viene (el siguiente al último resuelto); ver <see cref="TimeTrial.TrialRng"/>.</summary>
            public Random Rng(int stream = 0) => TimeTrial.TrialRng(Seed, Trials, stream);

            /// <summary>Pone en el marcador la marca (chip de la derecha: «23 aciertos»; baja con un error, sin signos) y el tiempo que queda (la fase de la fila de consigna: «1:12»); solo toca el marcador cuando algo cambió.</summary>
            public void UpdateHud(GameHud hud, float secondsLeft)
            {
                if (hud == null) return;
                if (_shownScore != Score)
                {
                    _shownScore = Score;
                    hud.SetInfoFit(HitsText(Score), Score.ToString());
                }
                int second = Math.Max(0, (int)Math.Ceiling(secondsLeft));
                if (_shownSecond != second)
                {
                    _shownSecond = second;
                    hud.SetPhase(ClockText(secondsLeft));
                }
            }

            /// <summary>Anota la partida en la telemetría: los <c>tt_*</c> (los aciertos y los errores SIN restar: la marca la calcula la app con <see cref="Mark"/>) y, como la dificultad adaptativa no corrió, <c>mode_trials</c> y <c>mode_hits</c> en 0 (el resto —<c>end_rating</c>— queda como lo dejó la persona).</summary>
            public void Fill(StroopSessionMetrics m)
            {
                m.tt_hits = Hits;
                m.tt_errors = Errors;
                m.tt_peak_level = PeakLevel;
                m.tt_seconds = TotalSeconds;
                m.tt_track_version = TrackVersion(GameId);
                m.mode_trials = 0;
                m.mode_hits = 0;
            }
        }

        /// <summary>Un Contrarreloj para esta config, o null si no lo es (la config no lo pide o el juego no lo tiene). Con él, <c>timed</c> queda en true.</summary>
        public static Run Begin(SequenceInitConfig config)
        {
            if (config == null || config.config == null || !config.config.time_trial || !Supports(config.game_id)) return null;
            config.config.timed = true;
            return new Run(config.game_id, config.config.track_seed);
        }

        // ------------------------------------------------------------------ textos

        /// <summary>«23 aciertos» / «1 acierto» / «0 aciertos»: el chip del marcador, con la MARCA (aciertos menos errores; una palabra por concepto: acierto; «nivel» no se muestra).</summary>
        public static string HitsText(int hits) => hits == 1 ? "1 acierto" : hits + " aciertos";

        /// <summary>El tiempo que queda como «1:12» (los segundos, redondeados hacia arriba: «0:01» hasta que llega a cero).</summary>
        public static string ClockText(float secondsLeft)
        {
            int s = Math.Max(0, (int)Math.Ceiling(secondsLeft));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
