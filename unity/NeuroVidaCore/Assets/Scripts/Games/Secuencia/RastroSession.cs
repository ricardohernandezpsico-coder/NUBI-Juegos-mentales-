using System;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>Qué cartel va antes de la muestra: el «¡NUEVO!» de por vida, el aviso de cambio de modo, o ninguno. Si coinciden, solo el «¡NUEVO!».</summary>
    public enum RastroNotice { None, NewMode, ModeChanged }

    /// <summary>Una ronda de «Rastro de luz»: qué familia, qué recorre la chispa (<see cref="Shown"/>) y qué hay que repetir, en orden (<see cref="Target"/>).</summary>
    public sealed class RastroRound
    {
        public RastroMode Mode;
        /// <summary>Nivel presentado (1..16) con que se armó la ronda.</summary>
        public int Level;
        public RastroLevel Params;
        public int[] Shown;
        public int[] Target;
        /// <summary>Solo «el cielo gira»: grados que gira el tablero tras la muestra (con signo).</summary>
        public float TurnDegrees;
        /// <summary>Velocidad de la chispa en dp/s (ya con el −15 % de mayores).</summary>
        public float SparkSpeed;
        /// <summary>Espera de la chispa en cada lucero (ms).</summary>
        public float WaitMs;
        /// <summary>Ronda guiada del tutorial: no suma puntos, no toca el DDA, las vidas ni el avance y no se guarda.</summary>
        public bool Guided;
        /// <summary>Primera vez de por vida que la persona llega a este modo: la pantalla «¡NUEVO!» va antes de la muestra.</summary>
        public bool IsNewMode;
        /// <summary>La familia es distinta a la de la ronda anterior de la partida (la primera ronda y la guiada no cambian de modo).</summary>
        public bool ModeChanged;
        public int Crossings;
        public RastroNotice Notice => IsNewMode ? RastroNotice.NewMode : ModeChanged ? RastroNotice.ModeChanged : RastroNotice.None;

        public int Asked => Target.Length;
    }

    /// <summary>Rondas, aciertos y mejor largo por familia, modos vistos: lo que sale en la telemetría <c>ras_*</c>.</summary>
    public sealed class RastroTally
    {
        public readonly int[] Rounds = new int[RastroModes.Count];
        public readonly int[] Hits = new int[RastroModes.Count];
        public readonly int[] BestLength = new int[RastroModes.Count];
        public int ModesSeen;
        public int TotalRounds { get { int n = 0; foreach (int v in Rounds) n += v; return n; } }
        public int TotalHits { get { int n = 0; foreach (int v in Hits) n += v; return n; } }

        public void Add(RastroRound r, bool correct)
        {
            int m = (int)r.Mode;
            Rounds[m]++;
            ModesSeen |= RastroModes.Bit(r.Mode);
            if (!correct) return;
            Hits[m]++;
            if (r.Asked > BestLength[m]) BestLength[m] = r.Asked;
        }
    }

    /// <summary>
    /// Decide qué familia sigue. Reglas del diseño: la primera ronda es el rastro simple; el rastro simple aparece siempre en la mezcla
    /// (nunca dos rondas seguidas de modos nuevos) y por eso ningún modo nuevo se repite más de 2 veces seguidas; y la PRIMERA vez de por vida
    /// que un modo queda disponible sale en la ronda siguiente (con su pantalla «¡NUEVO!»). Sin UnityEngine.
    /// </summary>
    public sealed class RastroDirector
    {
        private readonly Random _rng;
        private readonly bool _simpleOnly;
        private int _nonSimpleStreak;
        private int _rounds;

        /// <summary>Bits de los modos que la persona ya conoce (de por vida).</summary>
        public int Unlocked { get; private set; }
        /// <summary>Bits de los modos que se desbloquearon por primera vez en esta partida.</summary>
        public int NewlyUnlocked { get; private set; }

        public RastroDirector(Random rng, int unlockedMask, bool simpleOnly)
        {
            _rng = rng;
            Unlocked = unlockedMask | RastroModes.Bit(RastroMode.Rastro);
            _simpleOnly = simpleOnly;
        }

        public RastroMode Next(int level, out bool isNew)
        {
            isNew = false;
            RastroMode pick = Choose(level, out isNew);
            _rounds++;
            _nonSimpleStreak = pick == RastroMode.Rastro ? 0 : _nonSimpleStreak + 1;
            return pick;
        }

        private RastroMode Choose(int level, out bool isNew)
        {
            isNew = false;
            if (_simpleOnly || _rounds == 0 || _nonSimpleStreak >= 2) return RastroMode.Rastro;
            var families = RastroLadder.FamiliesAt(level);
            // un modo disponible que todavía no conoce: sale ahora, con su «¡NUEVO!»
            foreach (var m in families)
            {
                if (m == RastroMode.Rastro || (Unlocked & RastroModes.Bit(m)) != 0) continue;
                Unlocked |= RastroModes.Bit(m);
                NewlyUnlocked |= RastroModes.Bit(m);
                isNew = true;
                return m;
            }
            // mezcla: el rastro simple pesa 3 y cada modo nuevo 2
            int total = 0;
            foreach (var m in families) total += m == RastroMode.Rastro ? 3 : 2;
            int r = _rng.Next(total);
            foreach (var m in families)
            {
                r -= m == RastroMode.Rastro ? 3 : 2;
                if (r < 0) return m;
            }
            return RastroMode.Rastro;
        }
    }

    /// <summary>El resultado de contar una ronda: si el nivel real cambió y si la ronda contó (la guiada no cuenta).</summary>
    public readonly struct RastroOutcome
    {
        public readonly DdaChange Change;
        public readonly bool Counted;
        public RastroOutcome(DdaChange change, bool counted) { Change = change; Counted = counted; }
    }

    /// <summary>
    /// El estado de una partida de «Rastro de luz» sin escena: el motor común (<see cref="AdaptiveDifficulty"/>), el director, el conteo, las vidas
    /// y cuándo termina. El controlador solo dibuja y lee el dedo. Cada ronda completa es UN ensayo del motor común; las 3 vidas ya no deciden
    /// la dificultad, solo terminan la partida. La ronda guiada del tutorial pasa por <see cref="Complete"/> sin dejar rastro.
    /// </summary>
    public sealed class RastroSession
    {
        private readonly Random _rng;
        public readonly AdaptiveDifficulty Dda;
        public readonly RastroTally Tally = new RastroTally();
        public readonly RastroDirector Director;
        public readonly bool Senior;
        public readonly bool Assessment;
        public readonly bool Timed;
        public int Lives { get; private set; } = RastroContract.Lives;
        public int Errors { get; private set; }
        /// <summary>«Confusión de modo» tomada por familia en esta partida (a lo más 1 por modo): no cuenta como error ni toca las vidas ni el DDA.</summary>
        public readonly int[] ModeConfusions = new int[RastroModes.Count];
        private RastroMode? _lastMode;
        public int RoundsPlayed => Tally.TotalRounds;
        public int NewModes => Director.NewlyUnlocked;

        public RastroSession(SequenceConfigDetails config, Random rng, int unlockedMask)
        {
            _rng = rng;
            Senior = DdaUserProfileConfig.ParseAgeBand(config.age_band) == AgeBand.Senior;
            Assessment = config.assessment;
            Timed = config.timed;
            Dda = RastroContract.CreateEngine(config);
            // evaluación inicial: solo el rastro simple (los modos nuevos no se miden con 2 errores)
            Director = new RastroDirector(rng, unlockedMask, simpleOnly: config.assessment);
        }

        /// <summary>La ronda que sigue, con el nivel que presenta el motor común.</summary>
        public RastroRound NextRound()
        {
            int level = Dda.PresentedLevel;
            var mode = Director.Next(level, out bool isNew);
            var p = RastroLadder.Get(level);
            var round = Build(p, mode);
            round.IsNewMode = isNew;
            round.ModeChanged = _lastMode.HasValue && _lastMode.Value != mode;
            _lastMode = mode;
            return round;
        }

        /// <summary>
        /// ¿El primer toque de la respuesta es el de OTRO modo (no un fallo de memoria)? Al revés: tocó el primer lucero mostrado en vez del último.
        /// En marcha: tocó el primer lucero de toda la muestra en vez del primero de las últimas N. Siempre que no sea el lucero correcto
        /// (si coincide con el primero de las últimas N, es un acierto). El rastro simple, el giro y la ronda guiada no tienen confusión de modo.
        /// </summary>
        public static bool IsModeConfusion(RastroRound round, int firstTap)
        {
            if (round.Guided || (round.Mode != RastroMode.Reves && round.Mode != RastroMode.Marcha)) return false;
            return firstTap == round.Shown[0] && firstTap != round.Target[0];
        }

        /// <summary>Si el primer toque es una confusión de modo y todavía no se usó la segunda oportunidad de ese modo en la partida, la gasta y devuelve
        /// true: la ronda NO cuenta (ni error, ni vida, ni DDA) y se repite la misma muestra. La segunda vez cuenta como error normal.</summary>
        public bool TryModeConfusion(RastroRound round, int firstTap)
        {
            if (!IsModeConfusion(round, firstTap) || ModeConfusions[(int)round.Mode] > 0) return false;
            ModeConfusions[(int)round.Mode]++;
            return true;
        }

        /// <summary>La ronda guiada del tutorial: dos luces, el rastro simple, a la velocidad del nivel 2. No toca nada del estado de la partida.</summary>
        public RastroRound GuidedRound()
        {
            var p = RastroLadder.Get(2);
            var path = RastroPaths.Generate(_rng, RastroContract.GuidedLength, 0, 0);
            return new RastroRound
            {
                Mode = RastroMode.Rastro, Level = 2, Params = p, Shown = path, Target = (int[])path.Clone(),
                SparkSpeed = p.SparkSpeed * (Senior ? 0.85f : 1f), WaitMs = p.WaitMs, Guided = true
            };
        }

        private RastroRound Build(RastroLevel p, RastroMode mode)
        {
            int asked = p.Length(mode);
            int[] shown, target;
            int crossings;
            if (mode == RastroMode.Marcha)
            {
                int n = _rng.Next(p.ShownMin, p.ShownMax + 1);
                shown = RastroPaths.Generate(_rng, n, p.CrossMax, p.CrossTarget);
                target = new int[asked];
                Array.Copy(shown, shown.Length - asked, target, 0, asked);
            }
            else
            {
                shown = RastroPaths.Generate(_rng, asked, p.CrossMax, p.CrossTarget);
                target = (int[])shown.Clone();
                if (mode == RastroMode.Reves) Array.Reverse(target);
            }
            crossings = RastroPaths.Crossings(shown);
            float turn = 0f;
            if (mode == RastroMode.Gira)
            {
                float mag = p.TurnMin + (float)_rng.NextDouble() * (p.TurnMax - p.TurnMin);
                turn = _rng.Next(2) == 0 ? -mag : mag;
            }
            return new RastroRound
            {
                Mode = mode, Level = p.Index, Params = p, Shown = shown, Target = target, TurnDegrees = turn, Crossings = crossings,
                SparkSpeed = p.SparkSpeed * (Senior ? 0.85f : 1f),
                WaitMs = p.WaitMs * (mode == RastroMode.Marcha ? 0.75f : 1f)
            };
        }

        /// <summary>Cuenta una ronda: el motor común, el conteo y las vidas. La guiada no cuenta nada.</summary>
        public RastroOutcome Complete(RastroRound round, bool correct)
        {
            if (round.Guided) return new RastroOutcome(DdaChange.None, false);
            Tally.Add(round, correct);
            var change = Dda.Register(correct);
            if (!correct) { Lives--; Errors++; }
            return new RastroOutcome(change, true);
        }

        /// <summary>¿Terminó la partida? Sin vidas; en la evaluación inicial con 2 errores o ~75 s; en el Reto al llegar a 90 s; en Precisión con 14 rondas.</summary>
        public bool IsOver(float elapsedSeconds)
        {
            if (Lives <= 0) return true;
            if (Assessment) return Errors >= RastroContract.AssessmentMaxErrors || elapsedSeconds >= RastroContract.AssessmentSeconds;
            if (Timed) return elapsedSeconds >= RastroContract.RetoSeconds;
            return RoundsPlayed >= RastroContract.PrecisionRounds;
        }
    }
}
