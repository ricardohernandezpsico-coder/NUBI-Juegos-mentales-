using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Acoplamiento
{
    /// <summary>
    /// Las figuras de la pieza que se hornean POR MÓDULO (v3, Tarea 118; docs/previews/plasticina/propuesta-acoplamiento-v3.png; la receta de `tools/juegos/acopla_v3.py`): el módulo como una almohada de plasticina sin adornos y el hueco del puerto como una cavidad con cordón, con la LÁMPARA fija en la pantalla
    /// (brillo arriba a la izquierda, sombra abajo a la derecha). La luz NO va horneada en el marco de la pieza: el SDF se evalúa ya girado y espejado en la pantalla y la luz sale de la silueta que se VE, así que durante la decisión nada delata el espejo (la pieza espejada en pantalla y la de celdas reflejadas dan
    /// exactamente lo mismo). Todo se calcula con el SDF de la pieza (más el arreglo de la caja de 2 × 2: sin él, el punto donde se juntan 4 bloques deja un alfiler translúcido) y es lógica pura sobre <c>Color32[]</c> (fila 0 = ABAJO): se prueba sin Unity.
    /// <list type="bullet">
    /// <item><see cref="BakeFace"/>: la cara de la pieza (almohada: la silueta desenfocada como relieve, sin aristas en los cuellos y las esquinas), con su canto de 5 dp hacia abajo y su sombra de contacto, o «asentada» (a ras del puerto: sin canto, con la sombra de la pared de arriba y la izquierda).</item>
    /// <item><see cref="BakePort"/>: la cavidad (pared iluminada por la misma lámpara y fondo oscuro) y el cordón de plasticina del borde (en gris: se tiñe con el estado del puerto).</item>
    /// <item><see cref="RenderTrack"/> y <see cref="RenderCore"/>: la pista de un anillo y el núcleo de la escultura de la pantalla final.</item>
    /// </list>
    /// </summary>
    public static class DockingArt
    {
        /// <summary>El bloque de referencia del plan v3 (dp) y los píxeles por bloque con que se hornea (la pantalla lo muestra a 114: un escalado de 1,19).</summary>
        public const float BlockDp = 38f;
        public const int Bp = 96;

        /// <summary>El lado, en bloques, del cuadro de la pieza DERECHA (el hueco, el módulo alineado y el asentado): la caja de 4 × 4 más 0,9 por lado (canto, sombra y borde).</summary>
        public const float AlignedSideBlocks = 5.8f;

        /// <summary>El cuadro REAL de la pieza derecha (el pedido, redondeado a un múltiplo de 4 px): lo que la pantalla tiene que dibujar a la escala del bloque para que el hueco, la pieza alineada y la asentada midan lo mismo que la pieza mostrada.</summary>
        public static readonly int AlignedPx = SidePx(AlignedSideBlocks);
        public static readonly float AlignedFrameBlocks = AlignedPx / (float)Bp;

        /// <summary>El radio del redondeo de las esquinas convexas (bloques) y el borde de tinta heredado (ya no se dibuja).</summary>
        public const float CornerRadius = 0.16f;

        // ------------------------------------------------------------------ la caja y la geometría
        /// <summary>La caja de la pieza: el centro de su cuadro delimitador, en coordenadas de bloque.</summary>
        public static void PieceCenter(IReadOnlyList<Cell> cells, out float cx, out float cy, out int w, out int h)
        {
            w = 0;
            h = 0;
            foreach (var c in cells) { w = Mathf.Max(w, c.X + 1); h = Mathf.Max(h, c.Y + 1); }
            cx = (w - 1) * 0.5f;
            cy = (h - 1) * 0.5f;
        }

        /// <summary>El lado, en bloques, del cuadro de la pieza MOSTRADA (girada y espejada): lo que cabe en el círculo que envuelve su caja, más 0,9 por lado.</summary>
        public static float DisplayedSideBlocks(IReadOnlyList<Cell> cells)
        {
            PieceCenter(cells, out _, out _, out int w, out int h);
            return 2f * (Mathf.Sqrt(w * w + h * h) * 0.5f + 0.9f);
        }

        /// <summary>Los píxeles (múltiplo de 4) de un lado de [blocks] bloques a [bp] píxeles por bloque.</summary>
        public static int SidePx(float blocks, int bp = Bp) => Mathf.CeilToInt(blocks * bp / 4f) * 4;

        /// <summary>
        /// La pieza como un conjunto de cajas redondeadas (una por bloque, un «puente» por cada par de vecinos y, con <paramref name="fix2x2"/>, una caja de 2 × 2 por cada 2 × 2 de bloques): la distancia con signo es el mínimo de las de las cajas. Sin la caja de 2 × 2, el punto donde se juntan 4 bloques
        /// queda justo en d = 0 (la unión de los puentes lo deja en su borde) y se dibuja un alfiler translúcido: eso era el agujero de 2 × 2 px.
        /// </summary>
        public sealed class PieceGeo
        {
            public readonly float Cx, Cy;
            public readonly int W, H;
            private readonly float[] _cx, _cy, _hx, _hy;
            private readonly int _n;

            public PieceGeo(IReadOnlyList<Cell> cells, bool fix2x2 = true)
            {
                var set = new HashSet<Cell>(cells);
                PieceCenter(cells, out Cx, out Cy, out W, out H);
                var bx = new List<float>(); var by = new List<float>(); var hx = new List<float>(); var hy = new List<float>();
                void Add(float x, float y, float a, float b) { bx.Add(x); by.Add(y); hx.Add(a); hy.Add(b); }
                foreach (var c in cells)
                {
                    Add(c.X, c.Y, 0.5f, 0.5f);
                    if (set.Contains(new Cell(c.X + 1, c.Y))) Add(c.X + 0.5f, c.Y, 1f, 0.5f);
                    if (set.Contains(new Cell(c.X, c.Y + 1))) Add(c.X, c.Y + 0.5f, 0.5f, 1f);
                    if (fix2x2 && set.Contains(new Cell(c.X + 1, c.Y)) && set.Contains(new Cell(c.X, c.Y + 1)) && set.Contains(new Cell(c.X + 1, c.Y + 1))) Add(c.X + 0.5f, c.Y + 0.5f, 1f, 1f);
                }
                _cx = bx.ToArray(); _cy = by.ToArray(); _hx = hx.ToArray(); _hy = hy.ToArray(); _n = _cx.Length;
            }

            /// <summary>La distancia con signo (bloques) en el punto (gx, gy) de las coordenadas de bloque de la pieza.</summary>
            public float Sdf(float gx, float gy)
            {
                float d = 1e5f;
                for (int i = 0; i < _n; i++)
                {
                    float ax = Mathf.Abs(gx - _cx[i]) - _hx[i], ay = Mathf.Abs(gy - _cy[i]) - _hy[i];
                    if (d > 0f && (ax > 0f || ay > 0f) && (ax > 0f ? ax * ax : 0f) + (ay > 0f ? ay * ay : 0f) >= d * d) continue;                 // una caja redondeada cabe en su caja: si la caja ya está más lejos que lo mejor, no mejora
                    float qx = ax + CornerRadius, qy = ay + CornerRadius;
                    float ox = qx > 0f ? qx : 0f, oy = qy > 0f ? qy : 0f;
                    float v = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - CornerRadius;
                    if (v < d) d = v;
                }
                return d;
            }

            /// <summary>La distancia con signo y su gradiente (la dirección en que crece; el de la caja que da el mínimo).</summary>
            public float SdfGrad(float gx, float gy, out float dgx, out float dgy)
            {
                float d = 1e5f; dgx = 0f; dgy = 1f;
                for (int i = 0; i < _n; i++)
                {
                    float px = gx - _cx[i], py = gy - _cy[i];
                    float ax = Mathf.Abs(px) - _hx[i], ay = Mathf.Abs(py) - _hy[i];
                    if (d > 0f && (ax > 0f || ay > 0f) && (ax > 0f ? ax * ax : 0f) + (ay > 0f ? ay * ay : 0f) >= d * d) continue;
                    float qx = ax + CornerRadius, qy = ay + CornerRadius;
                    float ox = qx > 0f ? qx : 0f, oy = qy > 0f ? qy : 0f;
                    float len = Mathf.Sqrt(ox * ox + oy * oy);
                    float v = len + Mathf.Min(Mathf.Max(qx, qy), 0f) - CornerRadius;
                    if (v >= d) continue;
                    d = v;
                    float sx = px < 0f ? -1f : 1f, sy = py < 0f ? -1f : 1f;
                    if (len > 1e-6f) { dgx = sx * ox / len; dgy = sy * oy / len; }
                    else if (qx > qy) { dgx = sx; dgy = 0f; }
                    else { dgx = 0f; dgy = sy; }
                }
                return d;
            }
        }

        // ------------------------------------------------------------------ la lámpara
        /// <summary>La lámpara FIJA de la pantalla (x a la derecha, y ARRIBA, z al ojo): arriba a la izquierda. L = (−0,55; 0,62; 0,56) normalizada; H, la mitad entre L y el ojo.</summary>
        private static readonly float Lx = -0.5499f, Ly = 0.6198f, Lz = 0.5599f, Hx = -0.3113f, Hy = 0.3509f, Hz = 0.8831f;

        private static void Lamp(float nx, float ny, float nz, float spec, float hard, out float diffuse, out float specular)
        {
            float inv = 1f / Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
            nx *= inv; ny *= inv; nz *= inv;
            float ndl = Mathf.Max(0f, nx * Lx + ny * Ly + nz * Lz), nh = Mathf.Max(0f, nx * Hx + ny * Hy + nz * Hz);
            diffuse = ndl / Lz;                                                    // 1 en la cara plana, 0 de espaldas a la lámpara
            specular = Mathf.Pow(nh, hard) * spec;
        }

        // ------------------------------------------------------------------ colores
        /// <summary>El celeste cálido y saturado de la pieza, de la sombra profunda a la luz de plasticina (t = 0,62: la cara plana, ~ 118, 208, 244).</summary>
        private static readonly float[] RampT = { 0f, 0.28f, 0.62f, 0.84f, 1f };
        private static readonly Vector3[] RampC =
        {
            new Vector3(14f, 52f, 128f) / 255f, new Vector3(34f, 104f, 192f) / 255f, new Vector3(118f, 208f, 244f) / 255f, new Vector3(168f, 232f, 255f) / 255f, new Vector3(240f, 252f, 255f) / 255f
        };

        private static Vector3[] _rampLut;

        /// <summary>La rampa de la pieza como una tabla de 513 entradas (la interpolación de la rampa se calcula una sola vez: se pide por cada píxel de la cara).</summary>
        private static Vector3[] RampLut()
        {
            if (_rampLut != null) return _rampLut;
            var lut = new Vector3[513];
            for (int j = 0; j < lut.Length; j++)
            {
                float t = j / 512f;
                Vector3 c = RampC[RampC.Length - 1];
                for (int i = 1; i < RampT.Length; i++)
                    if (t <= RampT[i]) { c = Vector3.Lerp(RampC[i - 1], RampC[i], (t - RampT[i - 1]) / (RampT[i] - RampT[i - 1])); break; }
                lut[j] = c;
            }
            return _rampLut = lut;
        }

        public static Color PieceColor(float t)
        {
            var c = RampLut()[Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(t) * 512f), 0, 512)];
            return new Color(c.x, c.y, c.z, 1f);
        }

        /// <summary>El canto: el celeste más oscuro (40, 104, 176 por 0,70 junto a la cara y 0,48 al fondo) y el color de la sombra de contacto.</summary>
        private static readonly Vector3 CantoBase = new Vector3(40f, 104f, 176f) / 255f, ShadowColor = new Vector3(0.02f, 0.02f, 0.10f);
        private static readonly Vector3 FloorColor = new Vector3(7f, 8f, 24f) / 255f, WallColor = new Vector3(112f, 96f, 224f) / 255f;

        // ------------------------------------------------------------------ ruido: el grano fino de la plasticina (isótropo: no tiene dirección)
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 362437);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        private static float ValueNoise(float x, float y, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0, y0, seed), b = Hash(x0 + 1, y0, seed), c = Hash(x0, y0 + 1, seed), d = Hash(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>El grano: 1 ± ~2 % de grano fino (2 px) y ± ~2,4 % de variación a mano (28 px).</summary>
        private static float Grain(int px, int py, int seed) => 1f + 0.034f * (ValueNoise(px / 2f, py / 2f, seed) - 0.5f) * 2f + 0.024f * (ValueNoise(px / 28f, py / 28f, seed + 91) - 0.5f) * 2f;

        // ------------------------------------------------------------------ utilidades de campos (a un cuarto de la resolución)
        private static float Sample(float[] a, int n, float u, float v)
        {
            if (n < 2) return a[0];
            u = Mathf.Clamp(u, 0f, n - 1f); v = Mathf.Clamp(v, 0f, n - 1f);
            int i0 = Mathf.Min(n - 2, (int)u), j0 = Mathf.Min(n - 2, (int)v);
            float fu = u - i0, fv = v - j0;
            int k = j0 * n + i0;
            return Mathf.Lerp(Mathf.Lerp(a[k], a[k + 1], fu), Mathf.Lerp(a[k + n], a[k + n + 1], fu), fv);
        }

        private static float[] Blur(float[] a, int n, float sigma)
        {
            if (sigma <= 0.01f) return a;
            int r = Mathf.CeilToInt(sigma * 3f);
            var k = new float[2 * r + 1];
            float sum = 0f;
            for (int i = -r; i <= r; i++) { k[i + r] = Mathf.Exp(-0.5f * i * i / (sigma * sigma)); sum += k[i + r]; }
            for (int i = 0; i < k.Length; i++) k[i] /= sum;
            var tmp = new float[n * n];
            var o = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float s = 0f;
                    for (int i = -r; i <= r; i++) s += k[i + r] * a[y * n + Mathf.Clamp(x + i, 0, n - 1)];
                    tmp[y * n + x] = s;
                }
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float s = 0f;
                    for (int i = -r; i <= r; i++) s += k[i + r] * tmp[Mathf.Clamp(y + i, 0, n - 1) * n + x];
                    o[y * n + x] = s;
                }
            return o;
        }

        private static float Cover(float d, float aa) => Mathf.Clamp01(0.5f - d / aa);

        // ------------------------------------------------------------------ la cara de la pieza
        /// <summary>Cómo se hornea una cara: el canto (px hacia abajo; 0 = sin canto), la sombra de contacto, «asentada» (a ras del puerto: lámpara más suave y la sombra de la pared de arriba y la izquierda) y la semilla del grano.</summary>
        public struct FaceStyle
        {
            public int CantoPx;
            public bool Shadow, Seated;
            public int Seed;
        }

        /// <summary>
        /// La pieza de plasticina de [g] vista en la pantalla (cuadro de [n] × [n] px, múltiplo de 4, centrado en el centro de su caja; fila 0 = ABAJO): girada [thetaDeg]° (antihorario) y espejada si [flip], a [bp] píxeles por bloque. La luz sale de la silueta VISTA: ver la cabecera de la clase.
        /// Pasos: el SDF a un cuarto de la resolución → la silueta desenfocada como relieve (una almohada de 15 dp de ancho) → sus normales → la lámpara; a resolución completa solo se evalúa el SDF exacto en el borde (el alfa analítico) y el resto sale de interpolar.
        /// </summary>
        public static Color32[] BakeFace(PieceGeo g, int n, float bp, float thetaDeg, bool flip, FaceStyle st)
        {
            var result = new Color32[n * n];
            Drain(FaceRoutine(g, n, bp, thetaDeg, flip, st, result));
            return result;
        }

        /// <summary>Cuántas filas se hornean por cuadro cuando se hornea de a poco (<see cref="FaceRoutine"/>, <see cref="PortRoutine"/>): cada paso cuesta unos pocos milisegundos y el juego no se traba.</summary>
        public const int StepRows = 32;

        /// <summary>Corre una rutina de horneado hasta el final (las pruebas y el tutorial hornean de una vez).</summary>
        public static void Drain(IEnumerator routine)
        {
            while (routine.MoveNext()) { }
        }

        /// <summary>Como <see cref="BakeFace"/> pero DE A POCO: llena [result] (n × n) y cede el cuadro cada <see cref="StepRows"/> filas (y entre las etapas), para correr como corrutina sin trabar el juego.</summary>
        public static IEnumerator FaceRoutine(PieceGeo g, int n, float bp, float thetaDeg, bool flip, FaceStyle st, Color32[] result)
        {
            const int q = 4;
            float pxDp = bp / BlockDp, w = 15f * pxDp, hMax = w;
            int nq = n / q;
            float th = thetaDeg * Mathf.Deg2Rad, c = Mathf.Cos(th), s = Mathf.Sin(th), half = n * 0.5f, fs = flip ? -1f : 1f, aaq = q / bp;
            // 1) el SDF y la silueta a un cuarto de resolución
            var d4 = new float[nq * nq];
            var a4 = new float[nq * nq];
            for (int j = 0; j < nq; j++)
            {
                float Y = j * q + q * 0.5f - half;
                for (int i = 0; i < nq; i++)
                {
                    float X = i * q + q * 0.5f - half;
                    float d = g.Sdf(g.Cx + fs * (X * c + Y * s) / bp, g.Cy + (-X * s + Y * c) / bp);
                    d4[j * nq + i] = d;
                    a4[j * nq + i] = Cover(d, aaq);
                }
            }
            yield return null;
            // 2) el relieve: la silueta desenfocada (sigma = media almohada) → una altura de cuarto de círculo → sus normales y la lámpara
            var blurred = Blur(a4, nq, w * 0.5f / q);
            var h4 = new float[nq * nq];
            for (int i = 0; i < h4.Length; i++)
            {
                float u = a4[i] > 0.001f ? Mathf.Clamp01((blurred[i] - 0.5f) * 2f) : 0f;
                h4[i] = hMax * Mathf.Sqrt(Mathf.Max(0f, 1f - (1f - u) * (1f - u)));
            }
            var dif4 = new float[nq * nq];
            var spec4 = new float[nq * nq];
            for (int j = 0; j < nq; j++)
                for (int i = 0; i < nq; i++)
                {
                    float gx = (h4[j * nq + Mathf.Min(nq - 1, i + 1)] - h4[j * nq + Mathf.Max(0, i - 1)]) / (2f * q);
                    float gy = (h4[Mathf.Min(nq - 1, j + 1) * nq + i] - h4[Mathf.Max(0, j - 1) * nq + i]) / (2f * q);
                    Lamp(-gx, -gy, 1f, 0.20f, 12f, out dif4[j * nq + i], out spec4[j * nq + i]);
                }
            yield return null;
            // 3) la cara a resolución completa
            var face = new Color32[n * n];
            var alpha = new float[n * n];
            float lampK = st.Seated ? 0.8f : 1f;
            for (int py = 0; py < n; py++)
            {
                if (py % StepRows == StepRows - 1) yield return null;
                float Y = py + 0.5f - half, vq = (py + 0.5f) / q - 0.5f;
                for (int px = 0; px < n; px++)
                {
                    float X = px + 0.5f - half, uq = (px + 0.5f) / q - 0.5f;
                    float dc = Sample(d4, nq, uq, vq), dPx = dc * bp;
                    if (dPx > 5f) continue;
                    float gxp = g.Cx + fs * (X * c + Y * s) / bp, gyp = g.Cy + (-X * s + Y * c) / bp;
                    float d, a;
                    if (dPx < -5f) { d = dc; a = 1f; }
                    else { d = g.Sdf(gxp, gyp); a = Cover(d, 1f / bp); }
                    if (a <= 0f) continue;
                    float din = Mathf.Max(-d, 0f) * bp;
                    float dif = Sample(dif4, nq, uq, vq), sp = Sample(spec4, nq, uq, vq);
                    float ao = 1f - 0.14f * Mathf.Clamp01(1f - din / (w * 0.55f));
                    float t = Mathf.Clamp(0.60f + 0.28f * lampK * (dif - 1f), 0.40f, 1f) * ao + sp * 0.55f;
                    float gr = Grain(px, py, st.Seed);
                    var col = PieceColor(Mathf.Clamp01(t) * (0.94f + 0.06f * gr));
                    float m = 0.985f + 0.015f * gr;
                    float r = col.r * m, gg = col.g * m, b = col.b * m;
                    if (st.Seated && din < 26f)
                    {
                        // la sombra de la pared: la abertura corrida 3,2 dp a la derecha y 4 dp hacia abajo deja una banda a lo largo de los bordes de arriba y de la izquierda
                        float d2 = g.Sdf(g.Cx + (X - 3.2f * pxDp) / bp, g.Cy + (Y + 4f * pxDp) / bp);
                        float band = Mathf.Clamp01(0.5f + d2 * bp / 3.2f);
                        float k = 1f - 0.55f * band;
                        r *= k; gg *= k; b *= k;
                    }
                    int idx = py * n + px;
                    alpha[idx] = a;
                    face[idx] = new Color32(ToByte(r), ToByte(gg), ToByte(b), ToByte(a));
                }
            }
            if (st.CantoPx <= 0 && !st.Shadow) { Array.Copy(face, result, face.Length); yield break; }
            // 4) el canto (la silueta corrida hacia ABAJO en la pantalla, más oscura) y la sombra de contacto, DEBAJO de la cara
            int K = st.CantoPx;
            int shiftQ = Mathf.RoundToInt((K + 3f * pxDp) / q);
            var shifted = new float[nq * nq];
            for (int j = 0; j < nq; j++)
                for (int i = 0; i < nq; i++) shifted[j * nq + i] = a4[Mathf.Min(nq - 1, j + shiftQ) * nq + i];
            var sh4 = st.Shadow ? Blur(shifted, nq, 1.6f * pxDp / q) : null;
            yield return null;
            for (int py = 0; py < n; py++)
            {
                if (py % (StepRows * 2) == StepRows * 2 - 1) yield return null;
                float vq = (py + 0.5f) / q - 0.5f;
                for (int px = 0; px < n; px++)
                {
                    int idx = py * n + px;
                    float fa = alpha[idx];
                    if (fa >= 0.999f) { result[idx] = face[idx]; continue; }
                    // el canto: de lo más hondo (i = K) a lo más cercano (i = 1); lo cercano tapa lo hondo
                    float cr = 0f, cg = 0f, cb = 0f, ca = 0f;
                    for (int i = K; i >= 1; i--)
                    {
                        if (py + i >= n) continue;
                        float a = alpha[(py + i) * n + px];
                        if (a <= 0f) continue;
                        float f = 0.70f - 0.22f * (i / (float)K);
                        cr = CantoBase.x * f * a + cr * (1f - a); cg = CantoBase.y * f * a + cg * (1f - a); cb = CantoBase.z * f * a + cb * (1f - a);
                        ca = a + ca * (1f - a);
                    }
                    float sa = st.Shadow ? Mathf.Clamp01(Sample(sh4, nq, (px + 0.5f) / q - 0.5f, vq) * 0.50f) : 0f;
                    // debajo = canto sobre sombra; encima, la cara
                    float ur = cr + ShadowColor.x * sa * (1f - ca), ug = cg + ShadowColor.y * sa * (1f - ca), ub = cb + ShadowColor.z * sa * (1f - ca), ua = ca + sa * (1f - ca);
                    float oa = fa + ua * (1f - fa);
                    if (oa <= 0.002f) continue;
                    var f0 = face[idx];
                    float fr = f0.r / 255f * fa, fg = f0.g / 255f * fa, fb = f0.b / 255f * fa;
                    float outR = fr + ur * (1f - fa), outG = fg + ug * (1f - fa), outB = fb + ub * (1f - fa);
                    result[idx] = new Color32(ToByte(outR / oa), ToByte(outG / oa), ToByte(outB / oa), ToByte(oa));
                }
            }
        }

        private static byte ToByte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

        // ------------------------------------------------------------------ el puerto: la cavidad y el cordón
        /// <summary>
        /// El hueco del puerto para la pieza DERECHA (cuadro de [n] × [n] px, centrado en la caja de la pieza, fila 0 = ABAJO), a [bp] píxeles por bloque: [cavity] (la abertura: fondo oscuro y la pared de [6 dp] con la lámpara: en sombra arriba y a la izquierda, con luz abajo y a la derecha; más la sombra
        /// que echa el borde de arriba sobre el fondo) y [cordon] (un tubo de plasticina de 4,6 dp justo por fuera de la abertura, GRIS: se tiñe con <c>Image.color</c> del estado: verde agua, lima o celeste). El interior lejos del borde es un color liso: solo el borde se calcula con el SDF exacto y su gradiente.
        /// </summary>
        public static void BakePort(PieceGeo g, int n, float bp, int seed, out Color32[] cavity, out Color32[] cordon)
        {
            cavity = new Color32[n * n];
            cordon = new Color32[n * n];
            Drain(PortRoutine(g, n, bp, seed, cavity, cordon));
        }

        /// <summary>Como <see cref="BakePort"/> pero DE A POCO: llena [cavity] y [cordon] (n × n) y cede el cuadro cada <see cref="StepRows"/> filas.</summary>
        public static IEnumerator PortRoutine(PieceGeo g, int n, float bp, int seed, Color32[] cavity, Color32[] cordon)
        {
            const int q = 4;
            float pxDp = bp / BlockDp, wc = 4.6f * pxDp, pw = 6f * pxDp;
            int nq = n / q;
            float half = n * 0.5f;
            var d4 = new float[nq * nq];
            for (int j = 0; j < nq; j++)
            {
                float Y = j * q + q * 0.5f - half;
                for (int i = 0; i < nq; i++) d4[j * nq + i] = g.Sdf(g.Cx + (i * q + q * 0.5f - half) / bp, g.Cy + Y / bp);
            }
            yield return null;
            var floor = new Color32(ToByte(FloorColor.x), ToByte(FloorColor.y), ToByte(FloorColor.z), 255);
            for (int py = 0; py < n; py++)
            {
                if (py % StepRows == StepRows - 1) yield return null;
                float Y = py + 0.5f - half, vq = (py + 0.5f) / q - 0.5f;
                for (int px = 0; px < n; px++)
                {
                    float X = px + 0.5f - half, uq = (px + 0.5f) / q - 0.5f;
                    float dPx0 = Sample(d4, nq, uq, vq) * bp;
                    if (dPx0 > wc + 5f) continue;
                    int idx = py * n + px;
                    if (dPx0 < -33f) { cavity[idx] = floor; continue; }                               // el fondo liso, lejos de las paredes y de la sombra del borde (28 px)
                    float gx = g.Cx + X / bp, gy = g.Cy + Y / bp;
                    float d = g.SdfGrad(gx, gy, out float dgx, out float dgy), dPx = d * bp;
                    float ab = Cover(d, 1f / bp), ac = Cover(d - wc / bp, 1f / bp), ring = Mathf.Clamp01(ac - ab);
                    // la altura (px) como función de la distancia: el cordón es un tubo (semicírculo) y la cavidad una pared que baja al fondo; su derivada por el gradiente de la distancia da las normales
                    float dh;
                    if (d > 0f)
                    {
                        float u = Mathf.Clamp01(dPx / wc), gu = Mathf.Sqrt(Mathf.Max(0f, 1f - (2f * u - 1f) * (2f * u - 1f)) + 0.02f);
                        dh = 0.95f * (-2f * (2f * u - 1f) / gu);
                    }
                    else
                    {
                        float sI = Mathf.Clamp01(-dPx / pw);
                        dh = 8.4f * sI * (1f - sI);
                    }
                    Lamp(-dh * dgx, -dh * dgy, 1f, d > 0f ? 0.6f : 0f, 30f, out float dif, out float spec);
                    float gr = Grain(px, py, seed);
                    if (ring > 0.002f)
                    {
                        float t = Mathf.Clamp01(Mathf.Clamp01(0.60f + 0.45f * (dif - 1f)) + spec);
                        // gris del cordón: la rampa del color de estado (0,30 en la sombra profunda, 0,62 a t = 0,35 y 1 desde t = 0,62) como multiplicador del color de estado
                        float v = t < 0.35f ? Mathf.Lerp(0.30f, 0.62f, t / 0.35f) : t < 0.62f ? Mathf.Lerp(0.62f, 1f, (t - 0.35f) / 0.27f) : 1f;
                        v = Mathf.Clamp01(v * (0.97f + 0.03f * gr));
                        cordon[idx] = new Color32(ToByte(v), ToByte(v), ToByte(v), ToByte(ring));
                    }
                    if (ab > 0f)
                    {
                        float din = Mathf.Max(-dPx, 0f);
                        float wall = Mathf.Clamp01(1f - din / (pw * 1.05f)), tw = Mathf.Clamp(0.30f + 0.85f * dif, 0f, 1.6f);
                        float mix = Mathf.Pow(Mathf.Clamp01(wall * 1.15f), 0.9f);
                        float r = Mathf.Clamp01(WallColor.x * tw) * mix + FloorColor.x * (1f - mix), gg = Mathf.Clamp01(WallColor.y * tw) * mix + FloorColor.y * (1f - mix), b = Mathf.Clamp01(WallColor.z * tw) * mix + FloorColor.z * (1f - mix);
                        if (din > pw * 0.5f && din < 28f)
                        {
                            // la sombra del borde de arriba (y de la izquierda) sobre el fondo: la abertura corrida 3,8 dp a la derecha y 4,6 dp hacia abajo
                            float d2 = g.Sdf(g.Cx + (X - 3.8f * pxDp) / bp, g.Cy + (Y + 4.6f * pxDp) / bp);
                            float sombra = Mathf.Clamp01(0.5f + d2 * bp / 4.4f) * Mathf.Clamp01((din - pw * 0.5f) / (pw * 0.2f));
                            float k = 1f - 0.55f * sombra;
                            r *= k; gg *= k; b *= k;
                        }
                        cavity[idx] = new Color32(ToByte(r), ToByte(gg), ToByte(b), ToByte(ab));
                    }
                }
            }
        }

        // ------------------------------------------------------------------ la escultura de la pantalla final
        /// <summary>Una pista elíptica de anillo como un tubo de plasticina GRIS (se tiñe lima o lavanda): [rxDp] × [ryDp] de radios, [thickDp] de grosor, a [pxDp] píxeles por dp; [back] = la mitad de arriba de la elipse (la que pasa detrás del núcleo), si no la de abajo. Devuelve el cuadro (w × h px).</summary>
        public static Color32[] RenderTrack(float rxDp, float ryDp, float thickDp, float pxDp, bool back, out int w, out int h)
        {
            float pad = 6f;
            w = Mathf.CeilToInt((2f * (rxDp + thickDp) + 2f * pad) * pxDp / 4f) * 4;
            h = Mathf.CeilToInt((2f * (ryDp + thickDp) + 2f * pad) * pxDp / 4f) * 4;
            var px = new Color32[w * h];
            float rx = rxDp * pxDp, ry = ryDp * pxDp, half = thickDp * pxDp * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float Y = y + 0.5f - h * 0.5f;                                              // y ARRIBA (fila 0 = abajo)
                bool isBack = Y > 0f;
                if (isBack != back) continue;
                for (int x = 0; x < w; x++)
                {
                    float X = x + 0.5f - w * 0.5f;
                    float dist = DockingSprites.EllipseDistance(X, Y, rx, ry);
                    if (Mathf.Abs(dist) > half + 1f) continue;
                    float a = Mathf.Clamp01(half + 0.5f - Mathf.Abs(dist));
                    // un tubo: la altura es un semicírculo a lo ancho del trazo; la normal sale de la dirección a la elipse
                    float e = 0.5f;
                    float dx = DockingSprites.EllipseDistance(X + e, Y, rx, ry) - DockingSprites.EllipseDistance(X - e, Y, rx, ry), dy = DockingSprites.EllipseDistance(X, Y + e, rx, ry) - DockingSprites.EllipseDistance(X, Y - e, rx, ry);
                    float u = Mathf.Clamp(dist / half, -1f, 1f), slope = -u / Mathf.Sqrt(1.02f - u * u);
                    float sdx = slope * dx / (2f * e), sdy = slope * dy / (2f * e);
                    Lamp(-sdx, -sdy, 1f, 0f, 12f, out float dif, out _);
                    float t = Mathf.Clamp01(0.60f + 0.45f * (dif - 1f));
                    float v = t < 0.35f ? Mathf.Lerp(0.30f, 0.62f, t / 0.35f) : t < 0.62f ? Mathf.Lerp(0.62f, 1f, (t - 0.35f) / 0.27f) : 1f;
                    px[y * w + x] = new Color32(ToByte(v), ToByte(v), ToByte(v), ToByte(a));
                }
            }
            return px;
        }

        /// <summary>El núcleo de la escultura: un cilindro de plasticina lavanda (claro a la izquierda, sombra a la derecha) con su tapa elíptica arriba, de [wDp] × [hDp] a [pxDp] píxeles por dp. Devuelve (w × h px; la tapa llega hasta la fila ry).</summary>
        public static Color32[] RenderCore(float wDp, float hDp, float capRyDp, float pxDp, out int w, out int h)
        {
            w = Mathf.CeilToInt(wDp * pxDp / 4f) * 4;
            int ry = Mathf.RoundToInt(capRyDp * pxDp);
            h = Mathf.CeilToInt((hDp * pxDp + 2f * ry) / 4f) * 4;
            var px = new Color32[w * h];
            float cx = (w - 1) * 0.5f, rx = w * 0.5f;
            var stopsT = new[] { 0f, 0.35f, 0.62f, 0.86f, 1f };
            var stops = new[] { new Vector3(60f, 46f, 140f), new Vector3(112f, 92f, 206f), new Vector3(170f, 146f, 236f), new Vector3(214f, 196f, 252f), new Vector3(248f, 240f, 255f) };
            Color Ramp(float t)
            {
                t = Mathf.Clamp01(t);
                for (int i = 1; i < stopsT.Length; i++)
                    if (t <= stopsT[i]) { var c = Vector3.Lerp(stops[i - 1], stops[i], (t - stopsT[i - 1]) / (stopsT[i] - stopsT[i - 1])) / 255f; return new Color(c.x, c.y, c.z); }
                return new Color(0.97f, 0.94f, 1f);
            }
            int topCapRow = h - ry;                                                          // la fila donde está el centro de la tapa de arriba (y ARRIBA)
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Clamp((x - cx) / rx, -1f, 1f);
                    float inTop = (x - cx) / rx, vTop = (y - topCapRow) / (float)ry;
                    float capD = Mathf.Sqrt(inTop * inTop + vTop * vTop);                    // dentro de la elipse de la tapa si < 1
                    float inBot = (x - cx) / rx, vBot = (y - ry) / (float)ry;
                    float botD = Mathf.Sqrt(inBot * inBot + vBot * vBot);
                    bool body = y >= ry && y <= topCapRow;
                    bool cap = capD <= 1f;                                                    // la tapa de arriba es una elipse entera (su mitad de abajo cubre el borde del cuerpo)
                    bool bottom = botD <= 1f && y <= ry;
                    if (!body && !cap && !bottom) continue;
                    float a = 1f;
                    if (cap) a = Mathf.Clamp01((1f - capD) * rx * 0.5f + 0.5f);
                    else if (bottom) a = Mathf.Clamp01((1f - botD) * rx * 0.5f + 0.5f);
                    else a = Mathf.Clamp01((rx - Mathf.Abs(x - cx)) + 0.5f);
                    float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx));
                    Lamp(nx, 0f, nz, 0.25f, 14f, out float dif, out float sp);
                    float t = 0.60f + 0.36f * (dif - 1f) + sp * 0.4f;
                    if (cap) t = 0.86f;
                    var col = Ramp(t);
                    px[y * w + x] = new Color32(ToByte(col.r), ToByte(col.g), ToByte(col.b), ToByte(a));
                }
            return px;
        }

        // ------------------------------------------------------------------ sprites
        /// <summary>Un sprite cuadrado de píxeles ya horneados ([n] × [n], fila 0 = abajo); 1 unidad = el ancho. TEXTURA NUEVA: quien lo pide la libera con <see cref="Release"/>.</summary>
        public static Sprite MakeSprite(Color32[] px, int n) => ToSprite(px, n, n);

        private static Sprite ToSprite(Color32[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        /// <summary>Cómo se hornean la pieza levantada (con su canto de 5 dp y su sombra) y la asentada (a ras del hueco).</summary>
        public static FaceStyle RaisedStyle() => new FaceStyle { CantoPx = Mathf.RoundToInt(5f * Bp / BlockDp), Shadow = true, Seed = 7 };
        public static FaceStyle SeatedStyle() => new FaceStyle { Seated = true, Seed = 7 };

        /// <summary>Los píxeles de un lado del cuadro de la pieza MOSTRADA, y lo que mide en bloques (el cuadro es un poco más grande que lo pedido: múltiplo de 4 px).</summary>
        public static int DisplayedPx(IReadOnlyList<Cell> cells) => SidePx(DisplayedSideBlocks(cells));

        /// <summary>El módulo MOSTRADO (con la lámpara en la orientación que se ve) y su cuadro en bloques; TEXTURA NUEVA en cada llamada: quien la pide la libera con <see cref="Release"/>.</summary>
        public static Sprite BakeDisplayed(IReadOnlyList<Cell> cells, float thetaDeg, bool flip, out float sideBlocks)
        {
            int n = DisplayedPx(cells);
            sideBlocks = n / (float)Bp;
            return ToSprite(BakeFace(new PieceGeo(cells), n, Bp, thetaDeg, flip, RaisedStyle()), n, n);
        }

        /// <summary>El módulo ALINEADO (0° y sin espejo, para la revelación: con su canto y su sombra); misma silueta y cuadro que el hueco.</summary>
        public static Sprite BakeAlignedRaised(IReadOnlyList<Cell> cells)
        {
            int n = AlignedPx;
            return ToSprite(BakeFace(new PieceGeo(cells), n, Bp, 0f, false, RaisedStyle()), n, n);
        }

        /// <summary>El módulo ASENTADO (a ras del hueco: sin canto ni sombra, con la sombra de la pared de arriba y de la izquierda).</summary>
        public static Sprite BakeAlignedSeated(IReadOnlyList<Cell> cells)
        {
            int n = AlignedPx;
            return ToSprite(BakeFace(new PieceGeo(cells), n, Bp, 0f, false, SeatedStyle()), n, n);
        }

        /// <summary>Las dos juntas (para las pruebas; el juego las hornea en cuadros distintos).</summary>
        public static void BakeAligned(IReadOnlyList<Cell> cells, out Sprite raised, out Sprite seated)
        {
            raised = BakeAlignedRaised(cells);
            seated = BakeAlignedSeated(cells);
        }

        /// <summary>El hueco del puerto: la cavidad y el cordón (gris: se tiñe).</summary>
        public static void BakeHole(IReadOnlyList<Cell> cells, out Sprite cavity, out Sprite cordon)
        {
            var g = new PieceGeo(cells);
            int n = AlignedPx;
            BakePort(g, n, Bp, 31, out var cav, out var cor);
            cavity = ToSprite(cav, n, n);
            cordon = ToSprite(cor, n, n);
        }

        /// <summary>Una pieza chica de la escultura (alineada, con un canto de [cantoPx] px y su sombra) a [bp] píxeles por bloque, y su cuadro en bloques.</summary>
        public static Sprite BakeSculpturePiece(IReadOnlyList<Cell> cells, int bp, int cantoPx, int seed, out float sideBlocks)
        {
            var g = new PieceGeo(cells);
            int n = SidePx(AlignedSideBlocks, bp);
            sideBlocks = n / (float)bp;
            return ToSprite(BakeFace(g, n, bp, 0f, false, new FaceStyle { CantoPx = cantoPx, Shadow = true, Seed = seed }), n, n);
        }

        public static Sprite TrackSprite(float rxDp, float ryDp, float thickDp, float pxDp, bool back, out float wDp, out float hDp)
        {
            var px = RenderTrack(rxDp, ryDp, thickDp, pxDp, back, out int w, out int h);
            wDp = w / pxDp; hDp = h / pxDp;
            return ToSprite(px, w, h);
        }

        public static Sprite CoreSprite(float wDp, float hDp, float capRyDp, float pxDp, out float totalHDp)
        {
            var px = RenderCore(wDp, hDp, capRyDp, pxDp, out int w, out int h);
            totalHDp = h / pxDp;
            return ToSprite(px, w, h);
        }

        /// <summary>Libera la textura de un sprite horneado.</summary>
        public static void Release(Sprite sprite)
        {
            if (sprite == null) return;
            var tex = sprite.texture;
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(sprite);
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(sprite);                                // en las pruebas EditMode Destroy avisa con un error
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
