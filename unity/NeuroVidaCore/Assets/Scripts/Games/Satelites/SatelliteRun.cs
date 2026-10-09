using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Satelites
{
    /// <summary>
    /// Lo que va sumando una partida de Satélites (lógica pura): rondas, mensajes entregados (= luces), rondas perfectas, racha de rondas perfectas y el nivel más alto superado completo. De acá salen «Encendiste N luces», «Tu seguimiento»,
    /// el puntaje y los datos que viajan a la app. Una ronda es perfecta si se encontraron TODOS los que traían mensaje.
    /// </summary>
    public sealed class SatelliteRun
    {
        private readonly List<(int hits, int targets, int total)> _rounds = new List<(int hits, int targets, int total)>();

        public IReadOnlyList<(int hits, int targets, int total)> Rounds => _rounds;
        public int RoundsPlayed => _rounds.Count;
        /// <summary>Mensajes entregados en toda la partida: cada uno enciende una luz.</summary>
        public int Lights { get; private set; }
        public int TargetsTotal { get; private set; }
        public int Perfect { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int BestCleared { get; private set; }

        /// <summary>Suma una ronda; devuelve true si fue perfecta. <paramref name="level"/> es el nivel con que se jugó.</summary>
        public bool Add(int hits, int targets, int total, int level)
        {
            hits = Math.Max(0, Math.Min(targets, hits));
            _rounds.Add((hits, targets, total));
            Lights += hits;
            TargetsTotal += targets;
            bool perfect = SatelliteContract.IsPerfect(hits, targets);
            if (perfect)
            {
                Perfect++;
                Streak++;
                BestStreak = Math.Max(BestStreak, Streak);
                BestCleared = Math.Max(BestCleared, level);
            }
            else Streak = 0;
            return perfect;
        }

        /// <summary>Los mensajes entregados sobre los que había que entregar (0 a 1).</summary>
        public float HitRate => TargetsTotal > 0 ? (float)Lights / TargetsTotal : 0f;

        /// <summary>«Tu seguimiento»: cuántos se siguieron de verdad a la vez, descontando los aciertos por suerte (-1 sin rondas).</summary>
        public float Capacity => SatelliteContract.Capacity(_rounds);

        /// <summary>Cuántos había que seguir, en promedio (-1 sin rondas).</summary>
        public float MeanTargets
        {
            get
            {
                if (_rounds.Count == 0) return -1f;
                float sum = 0f;
                foreach (var r in _rounds) sum += r.targets;
                return sum / _rounds.Count;
            }
        }

        /// <summary>La velocidad del nivel más alto superado completo, respecto de la del nivel 1 (-1 si ninguno).</summary>
        public float SpeedReached => BestCleared > 0 ? SatelliteContract.SpeedFactor(BestCleared) : -1f;

        public int Score => SatelliteContract.Score(HitRate, BestCleared);
    }
}
