using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Radar
{
    /// <summary>Lo que pasó en una ronda ya resuelta: si cuenta como lograda para el motor y si fue perfecta (todas las cápsulas y ninguna de más).</summary>
    public readonly struct RoundOutcome
    {
        public readonly bool Success, Perfect;
        public readonly int Hits, Extras, Count;

        public RoundOutcome(bool success, bool perfect, int hits, int extras, int count)
        {
            Success = success;
            Perfect = perfect;
            Hits = hits;
            Extras = extras;
            Count = count;
        }
    }

    /// <summary>
    /// Lo que suma una partida de Rescate relámpago (lógica pura): rondas, cápsulas rescatadas, rondas perfectas, rachas, «Tu vistazo» (duraciones REALES de las rondas normales) y «Tu captura» (las lluvias, que quedan fuera del motor y de la precisión).
    /// El premio de la partida son las cápsulas rescatadas y su récord (no es una medida, como las luces de Satélites).
    /// </summary>
    public sealed class RadarRun
    {
        private readonly List<float> _realMs = new List<float>();
        private readonly List<int> _counts = new List<int>();
        private readonly List<int> _rainRaws = new List<int>();

        public int RoundsPlayed { get; private set; }
        /// <summary>Rondas normales (sin lluvias): las que cuentan para el motor, la precisión y «Tu vistazo».</summary>
        public int NormalRounds { get; private set; }
        public int Successes { get; private set; }
        public int Perfect { get; private set; }
        public int Rescued { get; private set; }
        public int WrongPicks { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        /// <summary>El destello más corto (ms reales) con que se resolvió una ronda normal perfecta; 0 si ninguna.</summary>
        public int ShortestPerfectMs { get; private set; }
        public int Rains => _rainRaws.Count;
        /// <summary>Los viajes de la nave a la estación (cada vez que se llenó con 10 a bordo).</summary>
        public int Trips { get; private set; }
        public void AddTrip() { Trips++; }
        public IReadOnlyList<float> RealMs => _realMs;
        public IReadOnlyList<int> RainRaws => _rainRaws;

        /// <summary>Registra una ronda resuelta. <paramref name="realMs"/> = lo que de verdad duró el destello (medido a 60 cuadros por segundo).</summary>
        public RoundOutcome Add(RadarRound round, int hits, int extras, float realMs)
        {
            int n = round.Count;
            hits = Math.Max(0, Math.Min(n, hits));
            extras = Math.Max(0, extras);
            bool success = RadarContract.Success(n, hits, extras), perfect = RadarContract.Perfect(n, hits, extras);
            RoundsPlayed++;
            Rescued += hits;
            WrongPicks += extras;
            if (perfect)
            {
                Perfect++;
                Streak++;
                BestStreak = Math.Max(BestStreak, Streak);
            }
            else Streak = 0;
            if (round.Rain) _rainRaws.Add(RadarContract.RainRaw(hits, extras));
            else
            {
                NormalRounds++;
                if (success) Successes++;
                _realMs.Add(realMs);
                _counts.Add(n);
                if (perfect && realMs > 0f)
                {
                    int ms = (int)Math.Round(realMs);
                    ShortestPerfectMs = ShortestPerfectMs == 0 ? ms : Math.Min(ShortestPerfectMs, ms);
                }
            }
            return new RoundOutcome(success, perfect, hits, extras, n);
        }

        public float Accuracy => NormalRounds > 0 ? (float)Successes / NormalRounds : 0f;
        public int GlanceMs => RadarContract.GlanceMs(_realMs);
        public float GlanceLoad => GlanceMs > 0 ? RadarContract.GlanceLoad(_counts) : -1f;
        public float Capture => RadarContract.Capture(_rainRaws);
        public int Score => RadarContract.Score(Accuracy, GlanceMs);
    }
}
