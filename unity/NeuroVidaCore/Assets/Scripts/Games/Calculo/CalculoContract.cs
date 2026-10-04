using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroVida.Games.Calculo
{
    public enum CargaOp { Add, Sub, Mul, Div }

    public enum CargaReject { None, NotExact, Negative }

    /// <summary>Un paso del camino: <c>A op B = R</c> (siempre con A el primero que se toca; en la resta A es el mayor y en la división el que se reparte).</summary>
    public readonly struct CargaStep
    {
        public readonly int A;
        public readonly CargaOp Op;
        public readonly int B;
        public readonly int R;
        public CargaStep(int a, CargaOp op, int b, int r) { A = a; Op = op; B = b; R = r; }
        public override string ToString() => A + " " + CargaRules.Symbol(Op) + " " + B + " = " + R;
    }

    /// <summary>
    /// Reglas puras de una jugada de «Carga exacta» (docs/diseno-carga-exacta.md §3): se juntan DOS celdas con una operación y queda UNA con el resultado.
    /// Sin resultados negativos ni divisiones que no den exactas (la jugada no se hace y se explica con cariño).
    /// </summary>
    public static class CargaRules
    {
        public static string Symbol(CargaOp op)
        {
            switch (op)
            {
                case CargaOp.Add: return "+";
                case CargaOp.Sub: return "−";
                case CargaOp.Mul: return "×";
                default: return "÷";
            }
        }

        /// <summary>true si la jugada vale. El orden importa: <c>a</c> es la primera celda tocada y <c>b</c> la segunda.</summary>
        public static bool Apply(int a, CargaOp op, int b, out int result, out CargaReject reject)
        {
            result = 0;
            reject = CargaReject.None;
            switch (op)
            {
                case CargaOp.Add:
                    result = a + b;
                    return true;
                case CargaOp.Sub:
                    if (a < b) { reject = CargaReject.Negative; return false; }
                    result = a - b;
                    return true;
                case CargaOp.Mul:
                    result = a * b;
                    return true;
                default:
                    if (b == 0 || a % b != 0) { reject = CargaReject.NotExact; return false; }
                    result = a / b;
                    return true;
            }
        }

        /// <summary>El aviso amable de una jugada que no se hizo; si dando vuelta las celdas sí valdría, lo dice («prueba al revés»).</summary>
        public static string Explain(int a, CargaOp op, int b, CargaReject reject)
        {
            switch (reject)
            {
                case CargaReject.Negative:
                    return "No hay energía negativa: prueba al revés";
                case CargaReject.NotExact:
                    return a != 0 && b % a == 0 ? "Esa división no da exacta: prueba al revés" : "Esa división no da exacta";
                default:
                    return "";
            }
        }
    }

    /// <summary>Qué pide cada nivel (docs/diseno-carga-exacta.md §5).</summary>
    public sealed class CargaLevel
    {
        public int Level;
        public int CellsMin, CellsMax;
        public CargaOp[] Ops;
        /// <summary>Cuántas celdas se juntan en la solución que se construye (el camino más corto que encuentra el solucionador puede ser menor).</summary>
        public int UseMin, UseMax;
        public int MaxNumber;
        /// <summary>Probabilidad de «usa todas las celdas» (solo el nivel 5): entonces solo cuenta si queda UNA celda con la carga.</summary>
        public float AllChance;
        /// <summary>Camino más corto mínimo (para que las cargas de los niveles altos no se resuelvan con una sola cuenta).</summary>
        public int MinShortest;
    }

    /// <summary>Una carga: las celdas, la carga que pide el reactor y un camino más corto (el que usa la pista de Nubi).</summary>
    public sealed class CargaPuzzle
    {
        public int Level;
        public int[] Cells;
        public int Target;
        public bool RequireAll;
        public CargaOp[] Ops;
        public List<CargaStep> Solution;
        /// <summary>true si salió de la carga de respaldo del nivel (no debería pasar jamás; las pruebas lo vigilan).</summary>
        public bool Fallback;
        public int ShortestSteps => Solution.Count;

        public string Key
        {
            get
            {
                var sorted = (int[])Cells.Clone();
                Array.Sort(sorted);
                return string.Join(",", sorted) + ">" + Target + (RequireAll ? "!" : "");
            }
        }
    }

    public static class CalculoContract
    {
        public const string GameId = "calculo";
        public const int MaxLevel = 5;
        /// <summary>Precisión = 8 cargas; Reto = 120 s (docs/diseno-carga-exacta.md §5). Nada cae ni apura dentro de una carga.</summary>
        public const int PrecisionLoads = 8;
        public const int RetoSeconds = 120;
        /// <summary>Cuántas cargas «de ritmo completo» caben en 120 s (una cada ~20 s): con ellas el Reto vale 100.</summary>
        public const float RetoReferenceLoads = 6f;
        /// <summary>Segundos sin lograr la carga para que Nubi dé la pista (20 s en mayores).</summary>
        public const float HintSeconds = 25f;
        public const float HintSecondsSenior = 20f;
        /// <summary>Si pasan tanto tiempo DESPUÉS de la pista sin lograrla, la carga se deja para otro día: nunca se queda trabado.</summary>
        public const float GiveUpAfterHintSeconds = 60f;
        public const int MinTarget = 8, MaxTarget = 99;

        private static readonly CargaOp[] Add = { CargaOp.Add };
        private static readonly CargaOp[] AddSub = { CargaOp.Add, CargaOp.Sub };
        private static readonly CargaOp[] AddSubMul = { CargaOp.Add, CargaOp.Sub, CargaOp.Mul };
        private static readonly CargaOp[] All4 = { CargaOp.Add, CargaOp.Sub, CargaOp.Mul, CargaOp.Div };

        public static readonly CargaLevel[] Levels =
        {
            new CargaLevel { Level = 1, CellsMin = 3, CellsMax = 3, Ops = Add,       UseMin = 2, UseMax = 2, MaxNumber = 9,  AllChance = 0f,   MinShortest = 1 },
            new CargaLevel { Level = 2, CellsMin = 3, CellsMax = 4, Ops = AddSub,    UseMin = 2, UseMax = 3, MaxNumber = 12, AllChance = 0f,   MinShortest = 1 },
            new CargaLevel { Level = 3, CellsMin = 4, CellsMax = 4, Ops = AddSubMul, UseMin = 2, UseMax = 3, MaxNumber = 9,  AllChance = 0f,   MinShortest = 1 },
            new CargaLevel { Level = 4, CellsMin = 4, CellsMax = 4, Ops = All4,      UseMin = 3, UseMax = 3, MaxNumber = 10, AllChance = 0f,   MinShortest = 2 },
            new CargaLevel { Level = 5, CellsMin = 4, CellsMax = 5, Ops = All4,      UseMin = 4, UseMax = 4, MaxNumber = 10, AllChance = 0.35f, MinShortest = 2 },
        };

        public static CargaLevel LevelSpec(int level) => Levels[Math.Max(1, Math.Min(MaxLevel, level)) - 1];

        public static float HintAfter(bool senior) => senior ? HintSecondsSenior : HintSeconds;

        /// <summary>Puntaje 0-100: cada carga lograda sola vale 1 y con pista, ½. Precisión: sobre 8 cargas. Reto: sobre el ritmo de referencia.</summary>
        public static int Score(CargaTally tally, bool endless)
        {
            if (tally == null) return 0;
            float credit = tally.SolvedAlone + 0.5f * tally.Hinted;
            float denom = endless ? RetoReferenceLoads : PrecisionLoads;
            return Math.Max(0, Math.Min(100, (int)Math.Round(100f * credit / denom)));
        }

        /// <summary>El nivel que se muestra en el marcador (mismo para todos los modos).</summary>
        public static string LevelNote(int level)
        {
            switch (level)
            {
                case 1: return "Solo sumas";
                case 2: return "Sumas y restas";
                case 3: return "Aparecen las multiplicaciones";
                case 4: return "Aparecen las divisiones";
                default: return "Con más celdas";
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------- solucionador

    /// <summary>
    /// El solucionador (búsqueda a lo ancho sobre pares de celdas): dice si una carga tiene solución, cuál es la más corta (la pista de Nubi) y cuántos pasos tiene
    /// (para «Camino corto»). Acepta cualquier camino: solo importa llegar a la carga (con «usa todas», quedando UNA celda).
    /// </summary>
    public static class CargaSolver
    {
        /// <summary>El camino más corto, o null si no hay.</summary>
        public static List<CargaStep> Shortest(IReadOnlyList<int> cells, IReadOnlyList<CargaOp> ops, int target, bool requireAll)
        {
            var start = new List<int>(cells);
            start.Sort();
            string startKey = Key(start);
            var parent = new Dictionary<string, KeyValuePair<string, CargaStep>>();
            var seen = new HashSet<string> { startKey };
            var frontier = new List<List<int>> { start };
            if (Goal(start, target, requireAll)) return new List<CargaStep>();
            while (frontier.Count > 0)
            {
                var next = new List<List<int>>();
                foreach (var state in frontier)
                {
                    string stateKey = Key(state);
                    for (int i = 0; i < state.Count; i++)
                    {
                        for (int j = i + 1; j < state.Count; j++)
                        {
                            foreach (var op in ops)
                            {
                                if (!Combine(state[i], state[j], op, out var step)) continue;
                                var child = new List<int>(state.Count - 1);
                                for (int k = 0; k < state.Count; k++) if (k != i && k != j) child.Add(state[k]);
                                child.Add(step.R);
                                child.Sort();
                                string key = Key(child);
                                if (!seen.Add(key)) continue;
                                parent[key] = new KeyValuePair<string, CargaStep>(stateKey, step);
                                if (Goal(child, target, requireAll)) return Rebuild(parent, key);
                                next.Add(child);
                            }
                        }
                    }
                }
                frontier = next;
            }
            return null;
        }

        public static bool Solvable(IReadOnlyList<int> cells, IReadOnlyList<CargaOp> ops, int target, bool requireAll) =>
            Shortest(cells, ops, target, requireAll) != null;

        private static bool Goal(List<int> state, int target, bool requireAll)
        {
            if (requireAll) return state.Count == 1 && state[0] == target;
            return state.Contains(target);
        }

        /// <summary>Junta dos valores con la operación en el sentido que vale (la resta y la división se hacen del mayor al menor).</summary>
        private static bool Combine(int x, int y, CargaOp op, out CargaStep step)
        {
            int a = x, b = y;
            if ((op == CargaOp.Sub || op == CargaOp.Div) && a < b) { a = y; b = x; }
            if (CargaRules.Apply(a, op, b, out int r, out _)) { step = new CargaStep(a, op, b, r); return true; }
            step = default;
            return false;
        }

        private static List<CargaStep> Rebuild(Dictionary<string, KeyValuePair<string, CargaStep>> parent, string key)
        {
            var path = new List<CargaStep>();
            while (parent.TryGetValue(key, out var p))
            {
                path.Add(p.Value);
                key = p.Key;
            }
            path.Reverse();
            return path;
        }

        private static string Key(List<int> sorted)
        {
            var sb = new StringBuilder();
            foreach (int v in sorted) sb.Append(v).Append(',');
            return sb.ToString();
        }
    }

    // ---------------------------------------------------------------------------------------------------- tablero

    /// <summary>
    /// El tablero de una carga (puro): las celdas de ahora, «Deshacer» (un paso atrás) y «Empezar de nuevo» (las celdas originales). Los errores no cuestan nada.
    /// Al juntar, la celda tocada primero desaparece y la segunda se queda con el resultado (conserva su <see cref="Cell.Id"/>: la pantalla la sigue).
    /// </summary>
    public sealed class CargaBoard
    {
        public readonly struct Cell
        {
            public readonly int Value;
            public readonly int Id;
            public Cell(int value, int id) { Value = value; Id = id; }
        }

        public readonly struct Move
        {
            public readonly CargaStep Step;
            public readonly int IdA, IdB;
            public Move(CargaStep step, int idA, int idB) { Step = step; IdA = idA; IdB = idB; }
        }

        private readonly int[] _original;
        private List<Cell> _cells = new List<Cell>();
        private readonly Stack<List<Cell>> _history = new Stack<List<Cell>>();

        public CargaBoard(IReadOnlyList<int> cells)
        {
            _original = new int[cells.Count];
            for (int i = 0; i < cells.Count; i++) _original[i] = cells[i];
            Reset();
        }

        public IReadOnlyList<Cell> Cells => _cells;
        public int Count => _cells.Count;
        /// <summary>Pasos hechos (las jugadas que siguen en pie).</summary>
        public int Steps => _history.Count;
        public bool CanUndo => _history.Count > 0;

        public void Reset()
        {
            _history.Clear();
            _cells = new List<Cell>();
            for (int i = 0; i < _original.Length; i++) _cells.Add(new Cell(_original[i], i));
        }

        public bool Undo()
        {
            if (_history.Count == 0) return false;
            _cells = _history.Pop();
            return true;
        }

        public bool TryMerge(int indexA, CargaOp op, int indexB, out Move move, out CargaReject reject)
        {
            move = default;
            reject = CargaReject.None;
            if (indexA == indexB || indexA < 0 || indexB < 0 || indexA >= _cells.Count || indexB >= _cells.Count) return false;
            var a = _cells[indexA];
            var b = _cells[indexB];
            if (!CargaRules.Apply(a.Value, op, b.Value, out int r, out reject)) return false;
            _history.Push(new List<Cell>(_cells));
            var next = new List<Cell>(_cells.Count - 1);
            for (int k = 0; k < _cells.Count; k++)
            {
                if (k == indexA) continue;
                next.Add(k == indexB ? new Cell(r, b.Id) : _cells[k]);
            }
            _cells = next;
            move = new Move(new CargaStep(a.Value, op, b.Value, r), a.Id, b.Id);
            return true;
        }

        /// <summary>La celda que llegó a la carga, o -1. Con «usa todas» solo cuenta si queda UNA celda.</summary>
        public int Winning(int target, bool requireAll)
        {
            if (requireAll && _cells.Count != 1) return -1;
            for (int i = 0; i < _cells.Count; i++) if (_cells[i].Value == target) return i;
            return -1;
        }

        /// <summary>Alguna celda vale la carga (aunque «usa todas» pida seguir).</summary>
        public bool HasTarget(int target) => IndexOfValue(target) >= 0;

        public int IndexOfValue(int value, int skip = -1)
        {
            for (int i = 0; i < _cells.Count; i++) if (i != skip && _cells[i].Value == value) return i;
            return -1;
        }

        public int IndexOfId(int id)
        {
            for (int i = 0; i < _cells.Count; i++) if (_cells[i].Id == id) return i;
            return -1;
        }

        /// <summary>Queda una sola celda y no es la carga: hay que deshacer y probar otro camino.</summary>
        public bool Stuck(int target) => _cells.Count == 1 && _cells[0].Value != target;
    }

    // ---------------------------------------------------------------------------------------------------- generador

    /// <summary>
    /// Arma cada carga combinando al azar algunas celdas con las operaciones del nivel (así siempre hay solución). Reglas (docs/diseno-carga-exacta.md §4): nada
    /// negativo, ninguna división inexacta, sin «×1» ni «÷1»; la carga entre 8 y 99 y que no esté ya entre las celdas; sin celdas repetidas (salvo una cuando hay 5).
    /// El solucionador confirma que se llega a la carga y deja el camino más corto para la pista.
    /// </summary>
    public static class CargaGenerator
    {
        private const int MaxIntermediate = 120;

        public static CargaPuzzle Generate(int level, System.Random rng)
        {
            var spec = CalculoContract.LevelSpec(level);
            for (int tries = 0; tries < 3000; tries++)
            {
                bool all = spec.AllChance > 0f && rng.NextDouble() < spec.AllChance;
                int n = all ? spec.UseMin : rng.Next(spec.CellsMin, spec.CellsMax + 1);
                int use = all ? n : Math.Min(n, rng.Next(spec.UseMin, spec.UseMax + 1));
                var nums = new int[n];
                for (int i = 0; i < n; i++) nums[i] = 1 + rng.Next(spec.MaxNumber);
                int target = Build(nums, use, spec.Ops, rng, out bool onlyAdds);
                if (target < 0) continue;
                if (target < CalculoContract.MinTarget || target > CalculoContract.MaxTarget) continue;
                if (Array.IndexOf(nums, target) >= 0) continue;
                if (Distinct(nums) < n - (n >= 5 ? 1 : 0)) continue;
                if (spec.Ops.Length > 1 && onlyAdds && rng.NextDouble() < 0.6) continue;     // que no sean siempre sumas
                var shuffled = Shuffle(nums, rng);
                var path = CargaSolver.Shortest(shuffled, spec.Ops, target, all);
                if (path == null || path.Count < spec.MinShortest) continue;
                return new CargaPuzzle { Level = spec.Level, Cells = shuffled, Target = target, RequireAll = all, Ops = spec.Ops, Solution = path };
            }
            return FallbackFor(spec);
        }

        /// <summary>Junta <paramref name="use"/> de las celdas al azar; devuelve la carga o -1 si alguna cuenta no valía.</summary>
        private static int Build(int[] nums, int use, CargaOp[] ops, System.Random rng, out bool onlyAdds)
        {
            onlyAdds = true;
            var pool = new List<int>();
            for (int i = 0; i < use; i++) pool.Add(nums[i]);
            while (pool.Count > 1)
            {
                int i = rng.Next(pool.Count);
                int j = rng.Next(pool.Count - 1);
                if (j >= i) j++;
                var op = ops[rng.Next(ops.Length)];
                int a = pool[i], b = pool[j];
                if (op == CargaOp.Sub && a < b) { int t = a; a = b; b = t; }
                if (op == CargaOp.Div && (b == 0 || a % b != 0))
                {
                    if (a != 0 && b % a == 0) { int t = a; a = b; b = t; }
                    else return -1;
                }
                if (op == CargaOp.Mul && (a == 1 || b == 1)) return -1;
                if (op == CargaOp.Div && b == 1) return -1;
                if (!CargaRules.Apply(a, op, b, out int r, out _)) return -1;
                if (r <= 0 || r > MaxIntermediate) return -1;
                if (op != CargaOp.Add) onlyAdds = false;
                var next = new List<int>();
                for (int k = 0; k < pool.Count; k++) if (k != i && k != j) next.Add(pool[k]);
                next.Add(r);
                pool = next;
            }
            return pool[0];
        }

        private static int Distinct(int[] nums)
        {
            var set = new HashSet<int>(nums);
            return set.Count;
        }

        private static int[] Shuffle(int[] nums, System.Random rng)
        {
            var a = (int[])nums.Clone();
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = a[i]; a[i] = a[j]; a[j] = t;
            }
            return a;
        }

        /// <summary>Una carga a mano por nivel, solo por si el sorteo no diera (no debería): siempre tiene solución.</summary>
        public static CargaPuzzle FallbackFor(CargaLevel spec)
        {
            int[] cells;
            int target;
            switch (spec.Level)
            {
                case 1: cells = new[] { 3, 5, 7 }; target = 12; break;
                case 2: cells = new[] { 4, 9, 11 }; target = 15; break;
                case 3: cells = new[] { 3, 4, 6, 9 }; target = 24; break;
                case 4: cells = new[] { 2, 3, 5, 8 }; target = 22; break;
                default: cells = new[] { 2, 3, 4, 7 }; target = 17; break;
            }
            return new CargaPuzzle
            {
                Level = spec.Level, Cells = cells, Target = target, RequireAll = false, Ops = spec.Ops,
                Solution = CargaSolver.Shortest(cells, spec.Ops, target, false), Fallback = true
            };
        }
    }

    /// <summary>
    /// Reparte las cargas de una partida: del nivel que toca, sin repetir ninguna (ni la misma carga ni dos seguidas con el mismo número).
    /// </summary>
    public sealed class CargaDirector
    {
        private readonly System.Random _rng;
        private readonly HashSet<string> _used = new HashSet<string>();
        private int _lastTarget = -1;

        public CargaDirector(System.Random rng) { _rng = rng; }

        public CargaPuzzle Next(int level)
        {
            CargaPuzzle p = null;
            for (int i = 0; i < 40; i++)
            {
                p = CargaGenerator.Generate(level, _rng);
                if (!_used.Contains(p.Key) && p.Target != _lastTarget) break;
            }
            _used.Add(p.Key);
            _lastTarget = p.Target;
            return p;
        }
    }

    // ---------------------------------------------------------------------------------------------------- medidas

    public enum CargaOutcome { Alone, Hinted, Missed }

    /// <summary>La mitad del crédito para el DDA: con pista la carga cuenta como acierto una vez sí y otra no (como en «En la punta de la lengua»).</summary>
    public sealed class CargaCredit
    {
        private int _hintedSeen;

        public bool CountsAsHit(CargaOutcome outcome)
        {
            switch (outcome)
            {
                case CargaOutcome.Alone: return true;
                case CargaOutcome.Hinted: return _hintedSeen++ % 2 == 0;
                default: return false;
            }
        }
    }

    /// <summary>
    /// Las cuentas de la partida para la medida del final (docs/diseno-carga-exacta.md §6): cargas logradas, cuántas sin pista, caminos cortos y el tiempo medio por carga
    /// lograda sola (el de las que tuvieron pista incluiría la espera hasta la pista).
    /// </summary>
    public sealed class CargaTally
    {
        public int Total { get; private set; }
        public int SolvedAlone { get; private set; }
        public int Hinted { get; private set; }
        public int Missed { get; private set; }
        public int ShortPaths { get; private set; }
        private long _aloneMs;

        public int Solved => SolvedAlone + Hinted;
        /// <summary>Tiempo medio, en ms, de las cargas logradas sin pista. -1 si no hubo ninguna.</summary>
        public int MeanAloneMs => SolvedAlone == 0 ? -1 : (int)(_aloneMs / SolvedAlone);

        /// <param name="stepsUsed">jugadas que quedaron en pie al lograrla</param>
        /// <param name="shortestSteps">pasos del camino más corto de esa carga</param>
        public void Record(CargaOutcome outcome, int stepsUsed, int shortestSteps, int ms)
        {
            Total++;
            switch (outcome)
            {
                case CargaOutcome.Alone:
                    SolvedAlone++;
                    _aloneMs += Math.Max(0, ms);
                    if (stepsUsed <= shortestSteps) ShortPaths++;
                    break;
                case CargaOutcome.Hinted:
                    Hinted++;
                    break;
                default:
                    Missed++;
                    break;
            }
        }
    }
}
