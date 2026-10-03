using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// El cielo de «Rastro de luz»: nueve luceros de cristal en un anillo irregular más uno al centro, en coordenadas lógicas de
    /// 360 × 640 dp (las del boceto aprobado), y la geometría que usan el juego y sus pruebas: rotación del tablero («el cielo gira»),
    /// qué lucero toca un dedo, qué tramos entre luceros quedan despejados y cuándo dos tramos se cruzan. Sin escenas ni sprites.
    /// </summary>
    public static class RastroBoard
    {
        public const int Orbs = 9;
        public const float CenterX = 180f, CenterY = 352f;
        /// <summary>Radio del disco punteado que marca el tablero.</summary>
        public const float DiscRadius = 150f;

        /// <summary>Diámetro del lucero (dp): ≥ 56; mayores 64.</summary>
        public const float OrbDiameter = 56f, OrbDiameterSenior = 64f;
        /// <summary>Radio de enganche del dedo (dp). La zona de toque es de 72 dp o más (mayores 76), por eso 36 y no los 34 del boceto.</summary>
        public const float HitRadius = 36f, HitRadiusSenior = 38f;
        /// <summary>Un tramo del camino nunca pasa a menos de esta distancia (dp) del centro de un lucero que no es suyo: si no, al
        /// deslizar el dedo en línea recta de uno a otro se «engancharía» sin querer el del medio y contaría como error.</summary>
        public const float ClearDistance = 42f;

        // Posiciones base (ángulo 0): un anillo de 8 de radio 122 ± 14 dp con ángulos irregulares (±8°) y uno casi al centro.
        // Separación mínima entre centros 79 dp (≥ 76 = luceros de 64 dp + 12 dp de aire), y todo cabe en 360 dp incluso girado.
        private static readonly float[] BaseX = { 177.6f, 258.3f, 313.0f, 253.5f, 182.2f, 105.0f, 62.5f, 74.8f, 175.9f };
        private static readonly float[] BaseY = { 231.5f, 262.8f, 351.1f, 443.9f, 481.7f, 442.5f, 348.0f, 269.6f, 352.8f };

        /// <summary>Colores de los luceros (#RRGGBB) y notas (Hz, pentatónica): una por lucero, siempre la misma.</summary>
        public static readonly int[] Colors = { 0x7FD8FF, 0xB8A4FF, 0xFFC94A, 0x8EE07A, 0xFF9F7A, 0x9FF5D6, 0xF7A8E0, 0xFFE27A, 0xA9C7FF };
        public static readonly float[] Notes = { 392f, 440f, 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f };

        public static Vector2 Center => new Vector2(CenterX, CenterY);
        public static Vector2 Base(int i) => new Vector2(BaseX[i], BaseY[i]);

        /// <summary>Posición del lucero <paramref name="i"/> con el tablero girado <paramref name="angleDeg"/> grados (los luceros conservan
        /// su identidad: lo que gira es el cielo, no el orden).</summary>
        public static Vector2 Position(int i, float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad, cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            float dx = BaseX[i] - CenterX, dy = BaseY[i] - CenterY;
            return new Vector2(CenterX + dx * cos - dy * sin, CenterY + dx * sin + dy * cos);
        }

        /// <summary>El lucero más cercano a <paramref name="p"/> si está a <paramref name="radius"/> dp o menos; -1 si ninguno.</summary>
        public static int OrbAt(Vector2 p, float angleDeg, float radius)
        {
            int best = -1;
            float bestD = radius;
            for (int i = 0; i < Orbs; i++)
            {
                float d = Vector2.Distance(p, Position(i, angleDeg));
                if (d <= bestD) { bestD = d; best = i; }
            }
            return best;
        }

        // ------------------------------------------------------------------ tramos

        private static bool[,] _clear;

        /// <summary>true si el tramo recto del lucero <paramref name="a"/> al <paramref name="b"/> está despejado (ver <see cref="ClearDistance"/>).
        /// No depende del giro del tablero: la geometría es rígida.</summary>
        public static bool SegmentClear(int a, int b)
        {
            if (_clear == null)
            {
                var m = new bool[Orbs, Orbs];
                for (int i = 0; i < Orbs; i++)
                    for (int j = 0; j < Orbs; j++)
                    {
                        if (i == j) continue;
                        bool ok = true;
                        for (int c = 0; c < Orbs && ok; c++)
                            if (c != i && c != j && DistanceToSegment(Base(c), Base(i), Base(j)) < ClearDistance) ok = false;
                        m[i, j] = ok;
                    }
                _clear = m;
            }
            return _clear[a, b];
        }

        public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>true si el tramo a1-a2 cruza al b1-b2 (cruce propio; los tramos vecinos comparten un extremo y no cuentan).</summary>
        public static bool SegmentsCross(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            float d1 = Cross(b2 - b1, a1 - b1), d2 = Cross(b2 - b1, a2 - b1);
            float d3 = Cross(a2 - a1, b1 - a1), d4 = Cross(a2 - a1, b2 - a1);
            return d1 * d2 < 0f && d3 * d4 < 0f;
        }

        private static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

        /// <summary>Punto de control de la curva (Bézier cuadrática) por la que vuela la chispa del tramo <paramref name="k"/> (1, 2, 3…): curva a un
        /// lado y al otro alternadamente, un 22 % del largo del tramo.</summary>
        public static Vector2 CurveControl(Vector2 from, Vector2 to, int k)
        {
            Vector2 mid = (from + to) * 0.5f;
            Vector2 d = to - from;
            float len = Mathf.Max(1e-3f, d.magnitude);
            float bend = (k % 2 == 1 ? 1f : -1f) * len * 0.22f;
            return new Vector2(mid.x - d.y / len * bend, mid.y + d.x / len * bend);
        }

        public static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }
    }

    /// <summary>
    /// Genera los caminos de luz. Cada lucero aparece a lo sumo una vez por camino (así nunca se repite el mismo lucero seguido), cada tramo
    /// está despejado (<see cref="RastroBoard.SegmentClear"/>) y los cruces entre tramos no vecinos no pasan del tope del nivel
    /// (Parmentier, Elford y Maybery, 2005: los cruces hacen más difícil recordar el camino). El generador busca llegar al número de cruces
    /// pedido; si no lo logra, entrega el que más se acerque sin pasarse.
    /// </summary>
    public static class RastroPaths
    {
        public const int MaxLength = RastroBoard.Orbs;

        /// <summary>Cuántos pares de tramos no vecinos del camino se cruzan.</summary>
        public static int Crossings(IList<int> path)
        {
            int n = 0;
            for (int i = 0; i + 1 < path.Count; i++)
                for (int j = i + 2; j + 1 < path.Count; j++)
                    if (RastroBoard.SegmentsCross(RastroBoard.Base(path[i]), RastroBoard.Base(path[i + 1]), RastroBoard.Base(path[j]), RastroBoard.Base(path[j + 1]))) n++;
            return n;
        }

        /// <param name="length">Luces del camino (2..9).</param>
        /// <param name="maxCross">Tope de cruces (0 = ninguno).</param>
        /// <param name="targetCross">Cruces que se buscan producir (≤ maxCross).</param>
        public static int[] Generate(Random rng, int length, int maxCross, int targetCross)
        {
            length = Math.Max(2, Math.Min(MaxLength, length));
            targetCross = Math.Min(targetCross, maxCross);
            int[] best = null;
            int bestCross = -1;
            for (int attempt = 0; attempt < 80; attempt++)
            {
                var path = Build(rng, length, maxCross);
                if (path == null) continue;
                int c = Crossings(path);
                if (c >= targetCross) return path;
                if (c > bestCross) { best = path; bestCross = c; }
            }
            return best ?? Ring(rng, length);
        }

        /// <summary>Camino de respaldo sin cruces: el anillo recorrido en orden desde un lucero cualquiera (y el del centro, si hacen falta 9).</summary>
        public static int[] Ring(Random rng, int length)
        {
            length = Math.Max(2, Math.Min(MaxLength, length));
            int start = rng.Next(8);
            int dir = rng.Next(2) == 0 ? 1 : -1;
            var path = new List<int>();
            for (int k = 0; k < Math.Min(length, 8); k++) path.Add(((start + dir * k) % 8 + 8) % 8);
            if (length == 9) path.Add(8);
            return path.ToArray();
        }

        // Búsqueda en profundidad con orden al azar y un tope de pasos (para no quedarse en un rincón sin salida).
        private static int[] Build(Random rng, int length, int maxCross)
        {
            var path = new List<int>(length);
            var used = new bool[RastroBoard.Orbs];
            int budget = 600;
            int first = rng.Next(RastroBoard.Orbs);
            path.Add(first);
            used[first] = true;
            return Extend(rng, path, used, length, maxCross, ref budget) ? path.ToArray() : null;
        }

        private static bool Extend(Random rng, List<int> path, bool[] used, int length, int maxCross, ref int budget)
        {
            if (path.Count == length) return true;
            if (--budget < 0) return false;
            int last = path[path.Count - 1];
            var order = new List<int>();
            for (int c = 0; c < RastroBoard.Orbs; c++)
                if (!used[c] && RastroBoard.SegmentClear(last, c)) order.Add(c);
            for (int i = order.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = order[i]; order[i] = order[j]; order[j] = t; }
            foreach (int c in order)
            {
                path.Add(c);
                used[c] = true;
                if (Crossings(path) <= maxCross && Extend(rng, path, used, length, maxCross, ref budget)) return true;
                path.RemoveAt(path.Count - 1);
                used[c] = false;
            }
            return false;
        }
    }
}
