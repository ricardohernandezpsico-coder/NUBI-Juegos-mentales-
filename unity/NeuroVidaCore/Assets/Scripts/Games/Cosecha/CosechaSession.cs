using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Cosecha
{
    public enum SubmitKind { TooShort, Repeat, Invalid, Valid }

    /// <summary>Qué pasó al sembrar: [Word] es la palabra válida con su tilde (null si no lo fue).</summary>
    public struct SubmitResult
    {
        public SubmitKind Kind;
        public HarvestWord Word;
        public int Points;
        public bool Star;
    }

    /// <summary>Una palabra encontrada, en orden, con el segundo de la cosecha en que se sembró.</summary>
    public sealed class FoundWord
    {
        public HarvestWord Word;
        public string Normalized;
        public float Time;
        public int Points;
    }

    /// <summary>
    /// Una cosecha (una ronda de 7 letras), sin UnityEngine: la bandeja (qué fichas se tocaron y en qué orden), las palabras
    /// ya encontradas y los puntos. Tocar una ficha la suma a la bandeja; tocar la última de la bandeja la devuelve; sembrar
    /// valida la palabra contra la lista de la ronda comparando SIN tildes. Las palabras ocultas se aceptan (planta, puntos)
    /// pero no cuentan en la lista ni en las medidas.
    /// </summary>
    public sealed class CosechaSession
    {
        public readonly HarvestRound Round;
        /// <summary>Índices de las fichas tocadas, en orden.</summary>
        public readonly List<int> Tray = new List<int>();
        public readonly List<FoundWord> Found = new List<FoundWord>();
        public int Points { get; private set; }
        public int Hints { get; private set; }

        private readonly Dictionary<string, HarvestWord> _index = new Dictionary<string, HarvestWord>();
        private readonly HashSet<string> _foundSet = new HashSet<string>();

        public CosechaSession(HarvestRound round)
        {
            Round = round;
            foreach (var w in round.palabras)
            {
                string n = CosechaContract.Normalize(w.p);
                // dos escrituras con la misma forma sin tilde: se queda la más común
                if (!_index.TryGetValue(n, out var cur) || w.b < cur.b) _index[n] = w;
            }
        }

        public string Letters => Round.letras;
        public ICollection<string> FoundNormalized => _foundSet;

        /// <summary>Lo que dice la bandeja ahora, sin tildes y en minúsculas.</summary>
        public string TrayText
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                foreach (int i in Tray) sb.Append(Round.letras[i]);
                return sb.ToString();
            }
        }

        public bool InTray(int tile) => Tray.Contains(tile);

        /// <summary>Toca una ficha: si no está en la bandeja, entra; si es la última de la bandeja, sale. Devuelve true si algo cambió.</summary>
        public bool TapTile(int tile)
        {
            if (tile < 0 || tile >= Round.letras.Length) return false;
            if (!Tray.Contains(tile)) { Tray.Add(tile); return true; }
            if (Tray[Tray.Count - 1] == tile) { Tray.RemoveAt(Tray.Count - 1); return true; }
            return false;
        }

        public bool RemoveLast()
        {
            if (Tray.Count == 0) return false;
            Tray.RemoveAt(Tray.Count - 1);
            return true;
        }

        public void Clear() => Tray.Clear();

        /// <summary>Siembra lo que haya en la bandeja en el segundo [time] de la cosecha. La bandeja se vacía salvo si era muy corta.</summary>
        public SubmitResult Submit(float time)
        {
            string text = TrayText;
            if (text.Length < CosechaContract.MinLetters) return new SubmitResult { Kind = SubmitKind.TooShort };
            Tray.Clear();
            if (_foundSet.Contains(text)) return new SubmitResult { Kind = SubmitKind.Repeat, Word = _index[text] };
            if (!_index.TryGetValue(text, out var w)) return new SubmitResult { Kind = SubmitKind.Invalid };
            int pts = CosechaContract.Points(w);
            _foundSet.Add(text);
            Found.Add(new FoundWord { Word = w, Normalized = text, Time = time, Points = pts });
            Points += pts;
            return new SubmitResult { Kind = SubmitKind.Valid, Word = w, Points = pts, Star = w.estrella };
        }

        public void AddHint() => Hints++;

        /// <summary>Palabras comunes encontradas (sin ocultas).</summary>
        public int CommonFound
        {
            get { int n = 0; foreach (var f in Found) if (f.Word.IsCommon) n++; return n; }
        }

        /// <summary>Palabras que cuentan para las medidas (todas las encontradas menos las ocultas).</summary>
        public int CountedWords
        {
            get { int n = 0; foreach (var f in Found) if (!f.Word.oculta) n++; return n; }
        }

        public bool Success => CosechaContract.Success(CommonFound, CosechaContract.CommonTotal(Round));
    }

    /// <summary>
    /// Lo que se mide en toda la partida (3 cosechas), sin UnityEngine: palabras y comunes, racimos y saltos, ritmo (primeros
    /// contra últimos 20 s), la palabra estrella o la más larga y rara, las que faltaron y las pistas. Las ocultas no entran.
    /// </summary>
    public sealed class CosechaTally
    {
        public int Words, CommonFound, CommonTotal, Clusters, Jumps, Hints;
        public int Cosechas, CosechasWon;
        public int PeakLevel = 1;
        public string Star = "";
        public string Best = "";
        private int _bestKey = -1;
        private int _firstSum, _lastSum;
        private readonly List<HarvestRound> _rounds = new List<HarvestRound>();
        private readonly Dictionary<string, HashSet<string>> _foundByRound = new Dictionary<string, HashSet<string>>();
        public readonly List<float> FastTimes = new List<float>();

        public void AddCosecha(CosechaSession s, int seconds, int level)
        {
            Cosechas++;
            PeakLevel = Math.Max(PeakLevel, level);
            CommonTotal += CosechaContract.CommonTotal(s.Round);
            CommonFound += s.CommonFound;
            if (s.Success) CosechasWon++;
            Hints += s.Hints;
            _rounds.Add(s.Round);
            _foundByRound[s.Round.id] = new HashSet<string>(s.FoundNormalized);

            string prev = null;
            var times = new List<float>();
            foreach (var f in s.Found)
            {
                if (f.Word.oculta) continue;               // las ocultas no cuentan en nada
                Words++;
                times.Add(f.Time);
                if (prev != null) { if (CosechaContract.IsCluster(prev, f.Normalized)) Clusters++; else Jumps++; }
                prev = f.Normalized;
                if (f.Word.estrella && Star.Length == 0) Star = f.Word.p;
                int key = f.Normalized.Length * 10 + Math.Min(9, f.Word.b);
                if (key > _bestKey) { _bestKey = key; Best = f.Word.p; }
            }
            CosechaContract.Window(times, seconds, out int first, out int last);
            _firstSum += first;
            _lastSum += last;
        }

        public int ClusterPercent => CosechaContract.ClusterPercent(Clusters, Jumps, Words);
        public int First20 => Cosechas > 0 ? (int)Math.Round(_firstSum / (double)Cosechas) : 0;
        public int Last20 => Cosechas > 0 ? (int)Math.Round(_lastSum / (double)Cosechas) : 0;
        public float CommonShare => CommonTotal > 0 ? (float)CommonFound / CommonTotal : 0f;

        /// <summary>"También podías": hasta 5 comunes que no se encontraron, separadas por coma en la telemetría.</summary>
        public List<string> Missed => CosechaContract.Missed(_rounds, _foundByRound, CosechaContract.MissedCount);
    }
}
