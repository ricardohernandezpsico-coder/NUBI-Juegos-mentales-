using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Meteoros
{
    /// <summary>
    /// Decide qué meteoro sale a continuación (reglas puras, ver <see cref="MeteorContract"/>): 50% palabras y 50%
    /// inventadas con nunca más de 3 inventadas seguidas; un meteoro dorado cada 12; y cada 25 meteoros una "Lluvia de
    /// estrellas" de 6 palabras comunes y rápidas, fuera de la escalera de dificultad.
    /// </summary>
    public sealed class MeteorDirector
    {
        private readonly ILexicon _lex;
        private readonly Random _rng;
        private int _index;          // meteoros normales lanzados (sin contar la lluvia)
        private int _decoyRun;       // inventadas seguidas
        private int _showerLeft;

        public MeteorDirector(ILexicon lexicon, Random rng)
        {
            _lex = lexicon;
            _rng = rng;
        }

        /// <summary>Meteoros normales lanzados hasta ahora.</summary>
        public int Spawned => _index;
        public int DecoyRun => _decoyRun;
        public bool InShower => _showerLeft > 0;

        public MeteorSpec Next(int level, bool precision, bool senior)
        {
            var spec = MeteorContract.Spec(level);
            float fall = MeteorContract.FallSeconds(level, precision, senior);

            if (_showerLeft > 0)
            {
                _showerLeft--;
                _decoyRun = 0;
                _lex.PickWord(1, 1, 4, 7, _rng, out var sw, out int sb);
                return new MeteorSpec { Word = sw, IsWord = true, Band = sb, Shower = true, FallSeconds = fall * 0.55f };
            }

            MeteorSpec m;
            if (MeteorContract.IsGolden(_index))
            {
                int band = MeteorContract.GoldenBand(level);
                _lex.PickWord(band, band, spec.MinLen, Math.Max(spec.MaxLen, 8), _rng, out var gw, out int gb);
                _decoyRun = 0;
                m = new MeteorSpec { Word = gw, IsWord = true, Band = gb, Golden = true, FallSeconds = fall };
            }
            else if (MeteorContract.NextIsWord(_decoyRun, _rng))
            {
                _lex.PickWord(spec.MinBand, spec.MaxBand, spec.MinLen, spec.MaxLen, _rng, out var w, out int b);
                _decoyRun = 0;
                m = new MeteorSpec { Word = w, IsWord = true, Band = b, FallSeconds = fall };
            }
            else
            {
                _lex.PickDecoy(spec.Decoys, spec.MinLen, spec.MaxLen, _rng, out var d, out var kind, out int b);
                _decoyRun++;
                m = new MeteorSpec { Word = d, IsWord = false, Band = b, Decoy = kind, FallSeconds = fall };
            }

            _index++;
            if (MeteorContract.ShowerDue(_index)) _showerLeft = MeteorContract.ShowerSize;
            return m;
        }
    }

    /// <summary>
    /// Acumula la partida y calcula las medidas del final (sección 5 del diseño). Puro: sin Unity.
    /// Los meteoros de la Lluvia de estrellas no entran en las medidas (se tocan rápido, sin pensar).
    /// </summary>
    public sealed class MeteorTally
    {
        public readonly int[] BandSeen = new int[6];
        public readonly int[] BandHits = new int[6];
        public readonly int[] DecoySeen = new int[3];
        public readonly int[] DecoyTapped = new int[3];
        public readonly List<int> CommonMs = new List<int>();
        public readonly List<int> RareMs = new List<int>();
        public readonly List<string> RareWordsHit = new List<string>();
        public int WordsSeen, WordsHit, DecoysSeen, DecoysPassed;

        /// <summary>Una palabra real resuelta: tocada ([hit]) o dejada pasar. [ms] = tiempo hasta el toque (-1 si no se tocó).</summary>
        public void AddWord(MeteorSpec m, bool hit, int ms)
        {
            if (m.Shower) return;
            WordsSeen++;
            int b = Math.Max(1, Math.Min(6, m.Band)) - 1;
            BandSeen[b]++;
            if (!hit) return;
            WordsHit++;
            BandHits[b]++;
            if (ms > 0)
            {
                if (MeteorContract.IsCommonBand(m.Band)) CommonMs.Add(ms);
                else if (MeteorContract.IsRareBand(m.Band)) RareMs.Add(ms);
            }
            if (MeteorContract.IsRareBand(m.Band)) RareWordsHit.Add(m.Word);
        }

        /// <summary>Una inventada resuelta: dejada pasar (acierto) o tocada (falsa alarma).</summary>
        public void AddDecoy(MeteorSpec m, bool tapped)
        {
            DecoysSeen++;
            DecoySeen[(int)m.Decoy]++;
            if (tapped) DecoyTapped[(int)m.Decoy]++;
            else DecoysPassed++;
        }

        public int DecoysTappedTotal => DecoyTapped[0] + DecoyTapped[1] + DecoyTapped[2];
        public int[] Bands => MeteorContract.BandPercents(BandSeen, BandHits, DecoysSeen, DecoysTappedTotal);
        public int CommonMedianMs => MeteorContract.MedianMs(CommonMs);
        public int RareMedianMs => MeteorContract.MedianMs(RareMs);
        public string RareWords => MeteorContract.RareWordsCsv(RareWordsHit);
        public float BalancedAccuracy => MeteorContract.BalancedAccuracy(WordsSeen, WordsHit, DecoysSeen, DecoysPassed);
    }
}
