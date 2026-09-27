using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Acoplamiento
{
    /// <summary>Una celda de un módulo (coordenadas enteras de grilla; y hacia arriba).</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is Cell c && Equals(c);
        public override int GetHashCode() => X * 397 ^ Y;
    }

    /// <summary>Un intento de acoplamiento: la forma del puerto, si el módulo que llega es su reflejo y cuánto viene girado.</summary>
    public sealed class DockingTrial
    {
        public Cell[] Shape;
        public bool Mirrored;
        /// <summary>Giro del módulo en grados (antihorario, puede ser negativo).</summary>
        public int AngleDeg;
        public int Level;
        /// <summary>Diferencia angular con el puerto (0..180), la que importa para girar mentalmente.</summary>
        public int Disparity => DockingContract.Disparity(AngleDeg);
    }

    /// <summary>
    /// Reglas puras de "Acoplamiento": ROTACIÓN MENTAL (Shepard y Metzler, 1971; en figuras planas, Cooper y Shepard,
    /// 1973). Llega un módulo girado y hay que decidir si encaja en el puerto (es la misma pieza, girada) o si es su
    /// reflejo en espejo (no encaja por más que se gire). El tiempo de respuesta crece con el ángulo: esa pendiente da
    /// la velocidad de giro mental. Las habilidades espaciales mejoran con práctica y esa mejora dura y se transfiere
    /// (meta-análisis de Uttal et al., 2013). Las piezas son poliominós quirales al azar (distintos de su reflejo).
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class DockingContract
    {
        public const string GameId = "acoplamiento";
        public const int MaxLevel = 12;

        /// <summary>Reto: 2 minutos de acoplamientos.</summary>
        public const int RetoSeconds = 120;
        /// <summary>Precisión (sin reloj): cantidad de módulos.</summary>
        public const int PrecisionTrials = 24;

        /// <summary>Ángulos (0..180) de las columnas de "tu curva de giro".</summary>
        public static readonly int[] CurveBins = { 0, 45, 90, 135, 180 };

        // ------------------------------------------------------------------ dificultad

        /// <summary>Celdas de la pieza: 4 (niveles 1-3), 5 (4-6), 6 (7-9), 7 (10-12).</summary>
        public static int Cells(int level) => 4 + (Clamp(level) - 1) / 3;

        /// <summary>Giro máximo: 90° en el nivel 1, 135° en el 2, 180° desde el 3.</summary>
        public static int MaxAngle(int level) => Clamp(level) == 1 ? 90 : Clamp(level) == 2 ? 135 : 180;

        /// <summary>Paso de los ángulos: de a 45° hasta el nivel 4; de a 15° después (giros "raros").</summary>
        public static int AngleStep(int level) => Clamp(level) <= 4 ? 45 : 15;

        /// <summary>Tiempo para decidir (ms): 6000 → 2500. Precisión: sin apuro (12 s, solo para no quedar colgado).</summary>
        public static int DeadlineMs(int level, bool precision) =>
            precision ? 12000 : 6000 - (int)Math.Round(3500.0 * (Clamp(level) - 1) / (MaxLevel - 1));

        public static DockingTrial NextTrial(int level, Random rng)
        {
            int l = Clamp(level);
            int step = AngleStep(l), max = MaxAngle(l);
            int angle = step * rng.Next(max / step + 1);
            if (angle != 0 && angle != 180 && rng.NextDouble() < 0.5) angle = -angle;
            return new DockingTrial
            {
                Shape = RandomChiralShape(Cells(l), rng),
                Mirrored = rng.NextDouble() < 0.5,
                AngleDeg = angle,
                Level = l,
            };
        }

        /// <summary>Diferencia angular (0..180) de un giro cualquiera.</summary>
        public static int Disparity(int angleDeg)
        {
            int a = ((angleDeg % 360) + 360) % 360;
            return a > 180 ? 360 - a : a;
        }

        // ------------------------------------------------------------------ piezas

        /// <summary>Poliominó conexo de <paramref name="n"/> celdas, distinto de su reflejo (quiral).</summary>
        public static Cell[] RandomChiralShape(int n, Random rng)
        {
            for (int attempt = 0; attempt < 500; attempt++)
            {
                var set = new HashSet<Cell> { new Cell(0, 0) };
                var list = new List<Cell> { new Cell(0, 0) };
                while (list.Count < n)
                {
                    var from = list[rng.Next(list.Count)];
                    int d = rng.Next(4);
                    var c = new Cell(from.X + (d == 0 ? 1 : d == 1 ? -1 : 0), from.Y + (d == 2 ? 1 : d == 3 ? -1 : 0));
                    if (set.Add(c)) list.Add(c);
                }
                var shape = Normalize(list);
                if (IsChiral(shape)) return shape;
            }
            // Respaldo (no debería pasar): una L, que siempre es quiral.
            return Normalize(new List<Cell> { new Cell(0, 0), new Cell(0, 1), new Cell(0, 2), new Cell(1, 0) });
        }

        /// <summary>¿La pieza es distinta de su reflejo? (ningún giro de 90° del reflejo coincide con ella).</summary>
        public static bool IsChiral(IReadOnlyList<Cell> shape)
        {
            var original = Key(Normalize(shape));
            var mirror = new List<Cell>();
            foreach (var c in shape) mirror.Add(new Cell(-c.X, c.Y));
            var r = mirror;
            for (int k = 0; k < 4; k++)
            {
                if (Key(Normalize(r)) == original) return false;
                r = Rotate90(r);
            }
            return true;
        }

        private static List<Cell> Rotate90(IReadOnlyList<Cell> cells)
        {
            var o = new List<Cell>();
            foreach (var c in cells) o.Add(new Cell(-c.Y, c.X));
            return o;
        }

        /// <summary>Lleva la pieza a coordenadas desde (0, 0) y en orden fijo.</summary>
        public static Cell[] Normalize(IReadOnlyList<Cell> cells)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var c in cells) { minX = Math.Min(minX, c.X); minY = Math.Min(minY, c.Y); }
            var o = new List<Cell>();
            foreach (var c in cells) o.Add(new Cell(c.X - minX, c.Y - minY));
            o.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            return o.ToArray();
        }

        private static string Key(IReadOnlyList<Cell> cells)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in cells) sb.Append(c.X).Append(',').Append(c.Y).Append(';');
            return sb.ToString();
        }

        public static bool IsConnected(IReadOnlyList<Cell> cells)
        {
            if (cells.Count == 0) return false;
            var all = new HashSet<Cell>(cells);
            var seen = new HashSet<Cell> { cells[0] };
            var stack = new Stack<Cell>();
            stack.Push(cells[0]);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                foreach (var n in new[] { new Cell(c.X + 1, c.Y), new Cell(c.X - 1, c.Y), new Cell(c.X, c.Y + 1), new Cell(c.X, c.Y - 1) })
                    if (all.Contains(n) && seen.Add(n)) stack.Push(n);
            }
            return seen.Count == all.Count;
        }

        // ------------------------------------------------------------------ puntaje y medidas

        /// <summary>Puntos de un acierto: base + rapidez + nivel + racha.</summary>
        public static int Points(bool correct, float rtMs, int deadlineMs, int level, int streak)
        {
            if (!correct) return 0;
            float speed = Math.Max(0f, Math.Min(1f, (deadlineMs - rtMs) / deadlineMs));
            return 60 + (int)Math.Round(40f * speed) + 10 * (Clamp(level) - 1) + 15 * Math.Min(Math.Max(streak - 1, 0), 10);
        }

        /// <summary>
        /// "Tu giro mental" (grados por segundo): recta de mínimos cuadrados del tiempo de respuesta contra el ángulo, en
        /// los aciertos. La pendiente (ms por grado) es el costo de girar; su inversa, la velocidad. -1 si hay menos de 8
        /// aciertos, menos de 3 ángulos distintos, menos de 70% de aciertos en total (con muchas respuestas al azar la
        /// curva sale plana y parecería un giro rapidísimo) o la recta no sube (sin efecto de giro medible).
        /// </summary>
        /// <summary>Proporción mínima de aciertos (incluidas las respuestas sin tiempo) para calcular el giro mental.</summary>
        public const float MinRotationAccuracy = 0.7f;

        public static int RotationSpeed(IReadOnlyList<int> disparities, IReadOnlyList<float> rtsMs, IReadOnlyList<bool> correct)
        {
            if (disparities == null || rtsMs == null || correct == null) return -1;
            int n = Math.Min(disparities.Count, Math.Min(rtsMs.Count, correct.Count));
            var xs = new List<double>();
            var ys = new List<double>();
            var distinct = new HashSet<int>();
            for (int i = 0; i < n; i++)
            {
                if (!correct[i]) continue;
                xs.Add(disparities[i]);
                ys.Add(rtsMs[i]);
                distinct.Add(Bin(disparities[i]));
            }
            if (xs.Count < 8 || distinct.Count < 3) return -1;
            if (xs.Count < MinRotationAccuracy * n) return -1;
            double mx = 0, my = 0;
            for (int i = 0; i < xs.Count; i++) { mx += xs[i]; my += ys[i]; }
            mx /= xs.Count;
            my /= xs.Count;
            double sxy = 0, sxx = 0;
            for (int i = 0; i < xs.Count; i++)
            {
                sxy += (xs[i] - mx) * (ys[i] - my);
                sxx += (xs[i] - mx) * (xs[i] - mx);
            }
            if (sxx <= 0) return -1;
            double slope = sxy / sxx; // ms por grado
            if (slope <= 0.05) return -1;
            return (int)Math.Round(Math.Max(20.0, Math.Min(2000.0, 1000.0 / slope)));
        }

        /// <summary>Columna de la curva (índice en <see cref="CurveBins"/>) más cercana a un ángulo.</summary>
        public static int Bin(int disparity)
        {
            int best = 0;
            for (int i = 1; i < CurveBins.Length; i++)
                if (Math.Abs(CurveBins[i] - disparity) < Math.Abs(CurveBins[best] - disparity)) best = i;
            return best;
        }

        /// <summary>"Tu curva de giro": tiempo medio de los aciertos en cada columna de ángulo (-1 = sin datos).</summary>
        public static int[] Curve(IReadOnlyList<int> disparities, IReadOnlyList<float> rtsMs, IReadOnlyList<bool> correct)
        {
            var sum = new double[CurveBins.Length];
            var cnt = new int[CurveBins.Length];
            int n = Math.Min(disparities.Count, Math.Min(rtsMs.Count, correct.Count));
            for (int i = 0; i < n; i++)
            {
                if (!correct[i]) continue;
                int b = Bin(disparities[i]);
                sum[b] += rtsMs[i];
                cnt[b]++;
            }
            var o = new int[CurveBins.Length];
            for (int b = 0; b < o.Length; b++) o[b] = cnt[b] > 0 ? (int)Math.Round(sum[b] / cnt[b]) : -1;
            return o;
        }

        /// <summary>Puntaje 0-100: aciertos (60%) y nivel más alto alcanzado (40%).</summary>
        public static int Score(float accuracy, int peakLevel)
        {
            float acc = Math.Max(0f, Math.Min(1f, accuracy));
            float lv = (float)(Clamp(peakLevel) - 1) / (MaxLevel - 1);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.6f * acc + 0.4f * lv) * 100f)));
        }

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
