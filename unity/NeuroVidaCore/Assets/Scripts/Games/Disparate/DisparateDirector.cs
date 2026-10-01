using System;
using System.Collections.Generic;
using Random = System.Random;

namespace NeuroVida.Games.Disparate
{
    /// <summary>
    /// Decide qué frase llega ahora: el tipo según el nivel, verdad o disparate con nunca más de 3 iguales seguidas (se
    /// garantiza al sacar, sin confiar en el orden del archivo), sin repetir frase en la partida y evitando las de las
    /// últimas partidas, y la Ráfaga cada 12 frases. Sin UnityEngine.
    /// </summary>
    public sealed class DisparateDirector
    {
        private readonly ISentenceSource _bank;
        private readonly Random _rng;
        private readonly HashSet<string> _avoid;
        private readonly HashSet<string> _used = new HashSet<string>();
        private readonly DisparateContract.AnswerSequencer _answers = new DisparateContract.AnswerSequencer();
        private int _burstsDone, _burstLeft;

        /// <summary>Frases normales sacadas (las de ráfaga no cuentan).</summary>
        public int Spawned { get; private set; }
        public bool InBurst => _burstLeft > 0;
        public int BurstLeft => _burstLeft;
        /// <summary>Ids de todas las frases de la partida, en orden (para evitarlas en las próximas).</summary>
        public List<string> UsedInOrder { get; } = new List<string>();

        public DisparateDirector(ISentenceSource bank, Random rng, IEnumerable<string> avoid = null)
        {
            _bank = bank;
            _rng = rng;
            _avoid = avoid != null ? new HashSet<string>(avoid) : new HashSet<string>();
        }

        public SentenceSpec Next(int level, bool precision, bool senior)
        {
            if (_burstLeft == 0 && DisparateContract.BurstDue(Spawned, _burstsDone))
            {
                _burstsDone++;
                _burstLeft = DisparateContract.BurstSize;
            }
            bool burst = _burstLeft > 0;
            int type = burst ? (int)SentenceType.Short : (int)DisparateContract.TypeForLevel(level);
            bool truth = _answers.Next(_rng);
            var s = _bank.Pick(type, truth, _rng, _used, _avoid, burst ? 3 : 0);
            if (s == null) return null;
            // si no había de la respuesta pedida (banco chico), se registra la que salió de verdad
            _answers.Record(s.v);
            _used.Add(s.i);
            UsedInOrder.Add(s.i);
            if (burst) _burstLeft--; else Spawned++;
            return new SentenceSpec
            {
                S = s,
                Burst = burst,
                SignalSeconds = burst ? DisparateContract.BurstSeconds(precision, senior) : DisparateContract.SignalSeconds(level, precision, senior)
            };
        }
    }

    /// <summary>Lo que se mide en la partida, sin UnityEngine: aciertos y tiempos por tipo de frase, por clase de disparate y la mejor racha.
    /// Las frases de Ráfaga no entran. NO hay ningún cálculo de sesgo (tender a decir verdad o disparate).</summary>
    public sealed class DisparateTally
    {
        public readonly int[] Seen = new int[6];
        public readonly int[] Hits = new int[6];
        public readonly List<int>[] HitMs = { new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>() };
        public readonly List<int> HitWords = new List<int>();
        public readonly List<int> HitWordsMs = new List<int>();
        public int EvidentSeen, EvidentHits, SubtleSeen, SubtleHits;
        public int BestStreak;
        public readonly List<string> Unclear = new List<string>();
        private int _streak;

        /// <summary>Una frase resuelta: [correct] si se respondió bien; [ms] = tiempo desde que quedó legible (-1 si se agotó la señal).</summary>
        public void Add(SentenceItem s, bool correct, int ms)
        {
            int t = Math.Max(1, Math.Min(6, s.t)) - 1;
            Seen[t]++;
            if (!s.v)
            {
                if (s.IsSubtle) SubtleSeen++; else EvidentSeen++;
            }
            if (correct)
            {
                Hits[t]++;
                if (!s.v)
                {
                    if (s.IsSubtle) SubtleHits++; else EvidentHits++;
                }
                if (ms > 0)
                {
                    HitMs[t].Add(ms);
                    HitWords.Add(s.n);
                    HitWordsMs.Add(ms);
                }
                _streak++;
                if (_streak > BestStreak) BestStreak = _streak;
            }
            else _streak = 0;
        }

        /// <summary>La racha de una frase de Ráfaga: sigue la racha sin entrar en las medidas.</summary>
        public void AddBurst(bool correct)
        {
            if (correct) { _streak++; if (_streak > BestStreak) BestStreak = _streak; }
            else _streak = 0;
        }

        public int Total { get { int n = 0; foreach (int v in Seen) n += v; return n; } }
        public int Correct { get { int n = 0; foreach (int v in Hits) n += v; return n; } }
        public int Wpm => DisparateContract.WordsPerMinute(HitWords, HitWordsMs);

        public int[] TypeMeanMs()
        {
            var r = new int[6];
            for (int t = 0; t < 6; t++) r[t] = DisparateContract.MeanMs(HitMs[t]);
            return r;
        }

        public void ReportUnclear(string id)
        {
            if (!string.IsNullOrEmpty(id) && !Unclear.Contains(id)) Unclear.Add(id);
        }

        public string UnclearCsv => string.Join(",", Unclear);
    }
}
