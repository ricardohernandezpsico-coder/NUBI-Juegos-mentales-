using System;
using System.Collections.Generic;
using Random = System.Random;

namespace NeuroVida.Games.Intrusa
{
    /// <summary>
    /// Decide qué ronda sigue: el tipo de grupo según el nivel, sin repetir grupo en la partida ni los de las últimas partidas, sin
    /// repetir regla dos veces seguidas (y con preferencia por reglas que aún no salieron en la partida), y cada
    /// <see cref="IntrusaContract.ReviewEvery"/> rondas una regla por repasar (fallada en OTRO día) con un grupo distinto del que se
    /// falló. Sin UnityEngine.
    /// </summary>
    public sealed class IntrusaDirector
    {
        private readonly IGroupSource _bank;
        private readonly Random _rng;
        private readonly HashSet<string> _avoid;
        private readonly List<string> _due;
        private readonly HashSet<string> _usedGroups = new HashSet<string>();
        private readonly HashSet<string> _usedRules = new HashSet<string>();
        private string _lastRule = "";
        private int _sinceReview;

        public int Rounds { get; private set; }
        /// <summary>Ids de todos los grupos de la partida, en orden (para evitarlos en las próximas).</summary>
        public List<string> UsedInOrder { get; } = new List<string>();

        public IntrusaDirector(IGroupSource bank, Random rng, IEnumerable<string> avoid = null, IEnumerable<string> reviewDue = null)
        {
            _bank = bank;
            _rng = rng;
            _avoid = avoid != null ? new HashSet<string>(avoid) : new HashSet<string>();
            _due = reviewDue != null ? new List<string>(reviewDue) : new List<string>();
        }

        /// <summary>El próximo grupo y su figura, o null si no queda ninguno. [Review] = vuelve una regla por repasar.</summary>
        public (IntrusaGroup group, bool review)? Next(int level)
        {
            IntrusaGroup g = null;
            bool review = false;
            if (_due.Count > 0 && Rounds > 0 && _sinceReview >= IntrusaContract.ReviewEvery - 1)
            {
                g = PickReview(level);
                if (g != null) review = true;
            }
            if (g == null) g = PickByLevel(level);
            if (g == null) return null;
            _usedGroups.Add(g.i);
            _usedRules.Add(g.k);
            _lastRule = g.k;
            UsedInOrder.Add(g.i);
            Rounds++;
            _sinceReview = review ? 0 : _sinceReview + 1;
            if (review) _due.Remove(g.k);
            return (g, review);
        }

        private IntrusaGroup PickReview(int level)
        {
            int want = IntrusaContract.TypeForLevel(level);
            foreach (string key in new List<string>(_due))
            {
                var list = _bank.OfRule(key);
                IntrusaGroup best = null;
                int bestGap = int.MaxValue;
                int start = _rng.Next(Math.Max(1, list.Count));
                for (int k = 0; k < list.Count; k++)
                {
                    var g = list[(start + k) % list.Count];
                    if (_usedGroups.Contains(g.i) || _avoid.Contains(g.i)) continue;
                    int gap = Math.Abs(g.t - want);
                    if (gap < bestGap) { bestGap = gap; best = g; }
                }
                if (best != null) return best;
            }
            return null;
        }

        private IntrusaGroup PickByLevel(int level)
        {
            var list = _bank.OfType(IntrusaContract.TypeForLevel(level));
            // de lo más estricto a lo más relajado: regla nueva en la partida, sin las de las últimas partidas, sin repetir regla seguida, sin repetir grupo
            return TryPick(list, true, true, true, true) ?? TryPick(list, true, false, true, true) ?? TryPick(list, false, false, true, true) ??
                   TryPick(list, false, false, false, true) ?? TryPick(list, false, false, false, false);
        }

        private IntrusaGroup TryPick(IReadOnlyList<IntrusaGroup> list, bool honorAvoid, bool preferNewRule, bool honorLastRule, bool honorUsed)
        {
            int n = list.Count;
            if (n == 0) return null;
            int start = _rng.Next(n);
            for (int k = 0; k < n; k++)
            {
                var g = list[(start + k) % n];
                if (honorUsed && _usedGroups.Contains(g.i)) continue;
                if (honorAvoid && _avoid.Contains(g.i)) continue;
                if (honorLastRule && g.k == _lastRule) continue;
                if (preferNewRule && _usedRules.Contains(g.k)) continue;
                return g;
            }
            return null;
        }
    }

    /// <summary>Lo que se mide en la partida, sin UnityEngine: por tipo de grupo (6) aciertos y rondas, tiempos de los aciertos, trampas,
    /// «¿Qué las une?», racha, y las láminas ganadas / reglas por repasar de la partida.</summary>
    public sealed class IntrusaTally
    {
        public readonly int[] Seen = new int[6];
        public readonly int[] Hits = new int[6];
        public readonly List<int> HitMs = new List<int>();
        public int BonusSeen, BonusHits;
        public int BestStreak;
        /// <summary>Claves de las reglas cuya lámina se ganó por primera vez en esta partida / que se fallaron (por repasar) / que se repasaron bien.</summary>
        public readonly List<string> NewPlates = new List<string>();
        public readonly List<string> NewReview = new List<string>();
        public readonly List<string> Mastered = new List<string>();
        public readonly List<string> Named = new List<string>();
        private int _streak;

        /// <summary>Una ronda resuelta: [correct] si se tocó la intrusa; [ms] = tiempo hasta el toque (-1 si no se mide).</summary>
        public void Add(IntrusaGroup g, bool correct, int ms)
        {
            int t = Math.Max(1, Math.Min(6, g.t)) - 1;
            Seen[t]++;
            if (correct)
            {
                Hits[t]++;
                if (ms > 0) HitMs.Add(ms);
                _streak++;
                if (_streak > BestStreak) BestStreak = _streak;
            }
            else _streak = 0;
        }

        public void AddBonus(bool named, string rule)
        {
            BonusSeen++;
            if (named) { BonusHits++; if (!Named.Contains(rule)) Named.Add(rule); }
        }

        public int Total { get { int n = 0; foreach (int v in Seen) n += v; return n; } }
        public int Correct { get { int n = 0; foreach (int v in Hits) n += v; return n; } }

        /// <summary>Rondas y aciertos por categoría de medida (4: tipo de cosa, uso, material/lugar/parte, trampas).</summary>
        public void Categories(out int[] seen, out int[] hits)
        {
            seen = new int[4]; hits = new int[4];
            for (int t = 0; t < 6; t++)
            {
                int c = IntrusaContract.CategoryOfType(t + 1);
                seen[c] += Seen[t];
                hits[c] += Hits[t];
            }
        }

        public int TrapSeen => Seen[4] + Seen[5];
        public int TrapHits => Hits[4] + Hits[5];
        public int MedianMs => IntrusaContract.MedianMs(HitMs);
    }
}
