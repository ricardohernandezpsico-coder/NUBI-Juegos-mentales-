using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// Todo el arte de «Constelaciones», horneado con el pincel de arcilla (<see cref="ClayRaster"/>), copiado del boceto aprobado (docs/previews/constelaciones-boceto.html, <c>drawObj</c>): los 12 objetos del
    /// espacio y los 4 gemelos (planeta sin anillo, cohete con llama grande, ovni con patas, casco con antena; el detalle que los distingue es GRANDE y de forma, nunca un tono parecido), la luz dormida (una esfera
    /// de arcilla oscura con un núcleo tibio), la luz abierta (un disco claro) y los aros de las parejas hechas (dorado continuo y celeste punteado). Sin sol, sin estrellas de puntas, sin media luna y sin cruces
    /// (docs/simbolos-neutros.md). Los objetos se dibujan en las coordenadas del boceto (x a la derecha, y HACIA ABAJO, unos ±22 unidades) y se hornean una sola vez; <see cref="Prewarm"/> los hornea de a uno por cuadro.
    /// </summary>
    public static class ConstelacionSprites
    {
        /// <summary>Unidades por lado de la caja de un objeto (el dibujo ocupa ±22) y píxeles por unidad.</summary>
        public const float ObjBox = 56f, ObjPpd = 4f;
        /// <summary>El radio con que se hornean las luces (se achican al radio del cielo, 28 a 38).</summary>
        public const float BakeR = 38f;
        public const float DormantBox = 2f * (BakeR + 4f), RingBox = 2f * (BakeR + 12f);

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static readonly Color InkC = Hex(0x1A1240);

        // ------------------------------------------------------------------ horneado

        private static Sprite Bake(string key, float box, float ppd, PaintFn paint)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            int n = Mathf.CeilToInt(box * ppd);
            var pix = new Color32[n * n];
            for (int iy = 0; iy < n; iy++)
            {
                float y = ((iy + 0.5f) / n - 0.5f) * box;
                for (int ix = 0; ix < n; ix++)
                {
                    float x = ((ix + 0.5f) / n - 0.5f) * box;
                    var p = new Px();
                    paint(ref p, x, y);
                    pix[iy * n + ix] = p.ToColor32();
                }
            }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(pix);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), ppd);
            Cache[key] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------ trazos en las coordenadas del boceto (y hacia abajo)

        /// <summary>Un camino hecho de rectas y curvas de segundo grado (aplanadas), como el de un canvas.</summary>
        private sealed class Path
        {
            public readonly List<float> Pts = new List<float>();
            private float _cx, _cy;

            public Path Move(float x, float y) { Pts.Add(x); Pts.Add(y); _cx = x; _cy = y; return this; }
            public Path Line(float x, float y) { Pts.Add(x); Pts.Add(y); _cx = x; _cy = y; return this; }

            public Path Quad(float qx, float qy, float x, float y)
            {
                float sx = _cx, sy = _cy;
                for (int i = 1; i <= 10; i++)
                {
                    float t = i / 10f, u = 1f - t;
                    Pts.Add(u * u * sx + 2f * u * t * qx + t * t * x);
                    Pts.Add(u * u * sy + 2f * u * t * qy + t * t * y);
                }
                _cx = x; _cy = y;
                return this;
            }

            public Path Arc(float cx, float cy, float r, float a0, float a1, int steps = 12)
            {
                for (int i = 0; i <= steps; i++)
                {
                    float a = a0 + (a1 - a0) * i / steps;
                    Pts.Add(cx + r * Mathf.Cos(a));
                    Pts.Add(cy + r * Mathf.Sin(a));
                }
                _cx = Pts[Pts.Count - 2]; _cy = Pts[Pts.Count - 1];
                return this;
            }

            public float[] Array(float dx = 0f, float dy = 0f)
            {
                var a = Pts.ToArray();
                for (int i = 0; i < a.Length; i += 2) { a[i] += dx; a[i + 1] += dy; }
                return a;
            }
        }

        private static float Poly(float x, float y, float[] pts) => Polygon(x, y, pts);

        /// <summary>Distancia a una línea gruesa (una cadena de cápsulas).</summary>
        private static float Stroke(float x, float y, float[] pts, float halfWidth, bool closed = false)
        {
            float d = float.MaxValue;
            int n = pts.Length / 2;
            for (int i = 0; i + 1 < n; i++) d = Mathf.Min(d, Capsule(x, y, pts[i * 2], pts[i * 2 + 1], pts[i * 2 + 2], pts[i * 2 + 3], halfWidth));
            if (closed && n > 2) d = Mathf.Min(d, Capsule(x, y, pts[(n - 1) * 2], pts[(n - 1) * 2 + 1], pts[0], pts[1], halfWidth));
            return d;
        }

        /// <summary>Un relleno con borde de tinta (el trazo del boceto, centrado en el contorno: mitad adentro y mitad afuera).</summary>
        private static void Fill(ref Px p, float sdf, Color fill, float aa, float ink = 1.3f)
        {
            p.Over(InkC, Cover(sdf - ink, aa));
            p.Over(fill, Cover(sdf + ink, aa));
        }

        /// <summary>Una línea de tinta (un trazo suelto).</summary>
        private static void Line(ref Px p, float sdf, float aa) => p.Over(InkC, Cover(sdf, aa));

        // ------------------------------------------------------------------ los objetos

        /// <summary>El objeto <paramref name="kind"/> (0..11) en su variante (1 = el gemelo de los 4 que lo tienen): una imagen de 56 unidades de lado, en blanco y color propios (no se tiñe).</summary>
        public static Sprite Object(int kind, int variant)
        {
            kind = Mathf.Clamp(kind, 0, ConstelacionContract.ObjectKinds - 1);
            variant = variant == 1 && ConstelacionContract.HasTwin(kind) ? 1 : 0;
            return Bake("obj" + kind + "_" + variant, ObjBox, ObjPpd, (ref Px p, float x, float yUp) =>
            {
                float y = -yUp;                       // las coordenadas del boceto van con y hacia abajo
                PaintObject(ref p, kind, variant, x, y, 1f / ObjPpd);
            });
        }

        private static void PaintObject(ref Px p, int kind, int v, float x, float y, float aa)
        {
            switch (kind)
            {
                case 0: Planet(ref p, v, x, y, aa); break;
                case 1: Rocket(ref p, v, x, y, aa); break;
                case 2: Comet(ref p, x, y, aa); break;
                case 3: Ufo(ref p, v, x, y, aa); break;
                case 4: Crystal(ref p, x, y, aa); break;
                case 5: Helmet(ref p, v, x, y, aa); break;
                case 6: Moon(ref p, x, y, aa); break;
                case 7: Satellite(ref p, x, y, aa); break;
                case 8: Telescope(ref p, x, y, aa); break;
                case 9: Asteroid(ref p, x, y, aa); break;
                case 10: Antenna(ref p, x, y, aa); break;
                default: Compass(ref p, x, y, aa); break;
            }
        }

        /// <summary>Un anillo inclinado (el del planeta): la mitad de atrás (<paramref name="back"/>) o la de adelante, con su borde de tinta.</summary>
        private static void PlanetRing(ref Px p, float x, float y, float aa, bool back)
        {
            // el anillo es una elipse (0,1) de 21 × 6,5 inclinada -0,35 rad
            float c = Mathf.Cos(0.35f), s = Mathf.Sin(0.35f), dx = x, dy = y - 1f;
            float lx = dx * c - dy * s, ly = dx * s + dy * c;
            float d = Mathf.Abs(Ellipse(lx, ly, 0f, 0f, 21f, 6.5f));
            float half = back ? Cover(ly, aa) : Cover(-ly, aa);         // atrás: y < 0 del anillo; adelante: y > 0
            p.Over(InkC, Cover(d - 3.5f, aa) * half);
            p.Over(Hex(0xFFD46B), Cover(d - 1.8f, aa) * half);
        }

        private static void Planet(ref Px p, int v, float x, float y, float aa)
        {
            if (v == 0) PlanetRing(ref p, x, y, aa, true);
            Fill(ref p, Circle(x, y, 0f, 0f, 13f), Hex(0xFF9F6B), aa);
            float inside = Cover(Circle(x, y, 0f, 0f, 11.7f), aa);
            p.Over(Hex(0xE9764A), inside * Cover(Mathf.Max(Mathf.Abs(y + 2f) - 2f, Mathf.Abs(x) - 14f), aa));
            p.Over(Hex(0xE9764A), inside * Cover(Mathf.Max(Mathf.Abs(y - 6.25f) - 1.25f, Mathf.Abs(x) - 14f), aa));
            if (v == 0) PlanetRing(ref p, x, y, aa, false);
        }

        private static readonly float[] FinL = new Path().Move(-8f, 3f).Line(-15f, 13f).Line(-8f, 11f).Array();
        private static readonly float[] FinR = new Path().Move(8f, 3f).Line(15f, 13f).Line(8f, 11f).Array();
        private static readonly float[] RocketBody = new Path().Move(0f, -19f).Quad(9f, -10f, 8f, -2f).Line(8f, 11f).Line(-8f, 11f).Line(-8f, -2f).Quad(-9f, -10f, 0f, -19f).Array();
        private static readonly float[] RocketNose = new Path().Move(0f, -19f).Quad(6.5f, -13f, 7f, -9f).Line(-7f, -9f).Quad(-6.5f, -13f, 0f, -19f).Array();
        private static readonly float[] FlameOut = new Path().Move(-8f, 10f).Quad(-6f, 22f, 0f, 30f).Quad(6f, 22f, 8f, 10f).Array();
        private static readonly float[] FlameIn = new Path().Move(-4f, 10f).Quad(-3f, 18f, 0f, 23f).Quad(3f, 18f, 4f, 10f).Array();

        private static void Rocket(ref Px p, int v, float x, float y, float aa)
        {
            if (v == 1)
            {
                y += 3f;                                        // con la llama grande el cohete sube 3 para que todo entre (translate(0,-3) del boceto)
                Fill(ref p, Poly(x, y, FlameOut), Hex(0xFF8A3D), aa);
                p.Over(Hex(0xFFE27A), Cover(Poly(x, y, FlameIn), aa));
            }
            Fill(ref p, Poly(x, y, FinL), Hex(0xE2524A), aa);
            Fill(ref p, Poly(x, y, FinR), Hex(0xE2524A), aa);
            Fill(ref p, Poly(x, y, RocketBody), Hex(0xEDEAFB), aa);
            Fill(ref p, Poly(x, y, RocketNose), Hex(0xE2524A), aa);
            Fill(ref p, Circle(x, y, 0f, 0f, 4.2f), Hex(0x7FD8FF), aa);
        }

        private static void Comet(ref Px p, float x, float y, float aa)
        {
            foreach (var (dy, w, col) in new[] { (-5f, 5f, new Color(127f / 255f, 216f / 255f, 1f, 0.55f)), (0f, 6f, new Color(191f / 255f, 233f / 255f, 1f, 0.8f)), (5f, 5f, new Color(127f / 255f, 216f / 255f, 1f, 0.55f)) })
            {
                var tail = new Path().Move(4f, -4f + dy * 0.2f).Quad(-6f, 4f + dy, -18f, 14f + dy * 0.6f).Array();
                p.Over(col, Cover(Stroke(x, y, tail, w / 2f), aa));
            }
            Fill(ref p, Circle(x, y, 7f, -7f, 8f), Hex(0xBFE9FF), aa);
            p.Over(new Color(1f, 1f, 1f, 0.8f), Cover(Circle(x, y, 5f, -9f, 2.4f), aa));
        }

        private static void Ufo(ref Px p, int v, float x, float y, float aa)
        {
            if (v == 1)
            {
                foreach (var seg in new[] { new[] { -9f, 4f, -13f, 15f }, new[] { -17f, 15f, -9f, 15f }, new[] { 9f, 4f, 13f, 15f }, new[] { 9f, 15f, 17f, 15f } })
                    Line(ref p, Capsule(x, y, seg[0], seg[1], seg[2], seg[3], 1.5f), aa);
            }
            var dome = new Path().Arc(0f, -3f, 9f, Mathf.PI, 2f * Mathf.PI, 16).Array();
            Fill(ref p, Poly(x, y, dome), Hex(0x9FF5D6), aa);
            Fill(ref p, Ellipse(x, y, 0f, 1f, 19f, 6.5f), Hex(0xA99FEA), aa);
            foreach (float dx in new[] { -10f, 0f, 10f }) p.Over(Hex(0xFFC94A), Cover(Circle(x, y, dx, 2f, 2.2f), aa));
        }

        private static readonly float[] CrystalPts = { 0f, -18f, 10f, -8f, 10f, 8f, 0f, 18f, -10f, 8f, -10f, -8f };

        private static void Crystal(ref Px p, float x, float y, float aa)
        {
            Fill(ref p, Poly(x, y, CrystalPts), Hex(0xC084FC), aa);
            var facet = new Color(1f, 1f, 1f, 0.5f);
            float inside = Cover(Poly(x, y, CrystalPts) + 1.3f, aa);
            p.Over(facet, Cover(Capsule(x, y, 0f, -18f, 0f, 18f, 0.9f), aa) * inside);
            p.Over(facet, Cover(Stroke(x, y, new[] { -10f, -8f, 0f, -2f, 10f, -8f }, 0.9f), aa) * inside);
        }

        private static void Helmet(ref Px p, int v, float x, float y, float aa)
        {
            if (v == 1)
            {
                Line(ref p, Capsule(x, y, 6f, -12f, 10f, -21f, 1.3f), aa);
                Fill(ref p, Circle(x, y, 10f, -22f, 3.6f), Hex(0xFFC94A), aa);
            }
            Fill(ref p, RoundBox(x, y, 0f, 14f, 11f, 3f, 0f), Hex(0xA99FEA), aa);
            Fill(ref p, Circle(x, y, 0f, 0f, 14.5f), Hex(0xF4F1FF), aa);
            Fill(ref p, RoundBox(x, y, 0f, -0.5f, 10f, 6.5f, 6f), Hex(0x163A5C), aa);
            var shine = new Path().Arc(-3f, -1f, 5f, Mathf.PI * 1.1f, Mathf.PI * 1.5f, 8).Array();
            p.Over(Hex(0x7FD8FF), Cover(Stroke(x, y, shine, 1f), aa));
        }

        private static void Moon(ref Px p, float x, float y, float aa)
        {
            Fill(ref p, Circle(x, y, 0f, 0f, 15f), Hex(0xE9E4C9), aa);
            foreach (var (dx, dy, r) in new[] { (-5f, -5f, 4f), (6f, 3f, 3.2f), (-2f, 8f, 2.4f), (7f, -8f, 2f) })
            {
                float sdf = Circle(x, y, dx, dy, r);
                p.Over(new Color(26f / 255f, 18f / 255f, 64f / 255f, 0.35f), Cover(sdf - 0.7f, aa));
                p.Over(Hex(0xCFC7A3), Cover(sdf + 0.7f, aa));
            }
        }

        private static void Satellite(ref Px p, float x, float y, float aa)
        {
            Line(ref p, Capsule(x, y, -8f, 0f, 8f, 0f, 1.2f), aa);
            foreach (int sx in new[] { -1, 1 })
            {
                float cx = sx < 0 ? -14.5f : 14.5f;
                Fill(ref p, RoundBox(x, y, cx, 0f, 6.5f, 6f, 0f), Hex(0x5B8DEF), aa);
                var grid = new Color(1f, 1f, 1f, 0.45f);
                p.Over(grid, Cover(Capsule(x, y, cx, -6f, cx, 6f, 0.6f), aa));
                p.Over(grid, Cover(Capsule(x, y, sx < 0 ? -21f : 8f, 0f, sx < 0 ? -8f : 21f, 0f, 0.6f), aa));
            }
            Fill(ref p, RoundBox(x, y, 0f, 0f, 6f, 8f, 3f), Hex(0xFFC94A), aa);
            Line(ref p, Capsule(x, y, 0f, -8f, 0f, -14f, 1f), aa);
            Fill(ref p, Circle(x, y, 0f, -15f, 2.4f), Hex(0xE2524A), aa);
        }

        private static void Telescope(ref Px p, float x, float y, float aa)
        {
            foreach (var leg in new[] { new[] { 0f, 3f, -9f, 18f }, new[] { 0f, 3f, 0f, 18f }, new[] { 0f, 3f, 9f, 18f } })
                Line(ref p, Capsule(x, y, leg[0], leg[1], leg[2], leg[3], 1.3f), aa);
            // el tubo, girado -0,55 rad
            float c = Mathf.Cos(0.55f), s = Mathf.Sin(0.55f), rx = x * c - y * s, ry = x * s + y * c;
            Fill(ref p, RoundBox(rx, ry, -2.5f, 0f, 13.5f, 5.5f, 3f), Hex(0x7FB2FF), aa);
            Fill(ref p, RoundBox(rx, ry, 12.5f, 0f, 2.5f, 7f, 0f), Hex(0x3E5BA9), aa);
            Fill(ref p, RoundBox(rx, ry, -17f, 0f, 2f, 3.5f, 0f), Hex(0x3E5BA9), aa);
        }

        private static readonly float[] AsteroidPts = { 0f, -15f, 9f, -11f, 15f, -2f, 12f, 9f, 3f, 15f, -8f, 13f, -15f, 4f, -13f, -8f };

        private static void Asteroid(ref Px p, float x, float y, float aa)
        {
            Fill(ref p, Poly(x, y, AsteroidPts), Hex(0xA08C7A), aa);
            foreach (var (dx, dy, r) in new[] { (-4f, -4f, 3.4f), (6f, 4f, 2.6f), (-5f, 7f, 2f) })
                p.Over(Hex(0x7E6B5B), Cover(Circle(x, y, dx, dy, r), aa));
        }

        private static void Antenna(ref Px p, float x, float y, float aa)
        {
            Fill(ref p, RoundBox(x, y, 0f, 16f, 9f, 2f, 0f), Hex(0xABA5D2), aa);
            Line(ref p, Capsule(x, y, 0f, 14f, 0f, 4f, 1.5f), aa);
            // el plato (media elipse hacia abajo) y su antenita, girados -0,55 rad
            float c = Mathf.Cos(0.55f), s = Mathf.Sin(0.55f), rx = x * c - y * s, ry = x * s + y * c;
            float dish = Mathf.Max(Ellipse(rx, ry, 0f, 0f, 15f, 7.5f), -ry);
            Fill(ref p, dish, Hex(0xDCD8F0), aa);
            Line(ref p, Capsule(rx, ry, 0f, 4f, 0f, -11f, 1f), aa);
            Fill(ref p, Circle(rx, ry, 0f, -12f, 2.6f), Hex(0xE2524A), aa);
        }

        private static readonly float[] NeedleN = { 0f, -11f, 4f, 0f, -4f, 0f };
        private static readonly float[] NeedleS = { 0f, 11f, 4f, 0f, -4f, 0f };

        private static void Compass(ref Px p, float x, float y, float aa)
        {
            Fill(ref p, Circle(x, y, 0f, 0f, 15f), Hex(0xF6EBD2), aa);
            p.Over(new Color(26f / 255f, 18f / 255f, 64f / 255f, 0.3f), Cover(Mathf.Abs(Circle(x, y, 0f, 0f, 11f)) - 0.7f, aa));
            p.Over(Hex(0xE2524A), Cover(Poly(x, y, NeedleN), aa));
            p.Over(Hex(0x6E6A9A), Cover(Poly(x, y, NeedleS), aa));
            p.Over(InkC, Cover(Circle(x, y, 0f, 0f, 2.2f), aa));
        }

        // ------------------------------------------------------------------ las luces

        /// <summary>La luz dormida: una esfera de arcilla oscura con un núcleo tibio (radio 38, sin animación continua; todas se ven iguales).</summary>
        public static Sprite Dormant()
        {
            const float ppd = 3f;
            return Bake("dormant", DormantBox, ppd, (ref Px p, float x, float yUp) =>
            {
                float y = -yUp, aa = 1f / ppd, r = BakeR;
                var rim = Hex(0x0B0A26);
                p.Over(rim, Cover(Circle(x, y, 0f, 0f, r) - 1.5f, aa));
                float t = Mathf.Clamp01(Mathf.Sqrt((x + r * 0.35f) * (x + r * 0.35f) + (y + r * 0.4f) * (y + r * 0.4f)) / (r * 1.4f));
                p.Over(Color.Lerp(Hex(0x3A3F86), Hex(0x171B4A), t), Cover(Circle(x, y, 0f, 0f, r) + 1.5f, aa));
                var arc = new Path().Arc(0f, 0f, r - 5f, Mathf.PI * 1.1f, Mathf.PI * 1.6f, 14).Array();
                p.Over(new Color(159f / 255f, 180f / 255f, 1f, 0.28f), Cover(Stroke(x, y, arc, 0.75f), aa));
                // el núcleo: un brillo suave de 22 dp y un punto claro
                float gr = Mathf.Sqrt(x * x + y * y) / 11f;
                if (gr < 1f) p.Over(new Color(170f / 255f, 190f / 255f, 1f, 0.85f * Mathf.Clamp01(gr < 0.35f ? 1f - gr / 0.35f * 0.55f : 0.45f * (1f - (gr - 0.35f) / 0.65f))), 1f);
                p.Over(Hex(0xE4E8FF), Cover(Circle(x, y, 0f, 0f, 2.6f), aa));
            });
        }

        /// <summary>La luz abierta: un disco claro (degradé de blanco a lila) con borde de tinta, donde se ve el objeto.</summary>
        public static Sprite Open()
        {
            const float ppd = 3f;
            return Bake("open", DormantBox, ppd, (ref Px p, float x, float yUp) =>
            {
                float y = -yUp, aa = 1f / ppd, r = BakeR;
                p.Over(InkC, Cover(Circle(x, y, 0f, 0f, r) - 1.5f, aa));
                float t = Mathf.Clamp01(Mathf.Sqrt((x + r * 0.35f) * (x + r * 0.35f) + (y + r * 0.4f) * (y + r * 0.4f)) / (r * 1.4f));
                p.Over(Color.Lerp(Color.white, Hex(0xC6BDF5), t), Cover(Circle(x, y, 0f, 0f, r) + 1.5f, aa));
            });
        }

        /// <summary>El aro de una pareja hecha de memoria: dorado continuo (radio R + 5, trazo 3,5).</summary>
        public static Sprite RingMemory()
        {
            const float ppd = 3f;
            return Bake("ringmem", RingBox, ppd, (ref Px p, float x, float yUp) =>
            {
                float aa = 1f / ppd, d = Mathf.Abs(Mathf.Sqrt(x * x + yUp * yUp) - (BakeR + 5f));
                p.Over(Hex(0xFFC94A), Cover(d - 1.75f, aa));
            });
        }

        /// <summary>El aro de una pareja hecha a la primera vista: celeste PUNTEADO (27 trazos; no depende solo del color).</summary>
        public static Sprite RingNew()
        {
            const float ppd = 3f;
            return Bake("ringnew", RingBox, ppd, (ref Px p, float x, float yUp) =>
            {
                float aa = 1f / ppd, d = Mathf.Abs(Mathf.Sqrt(x * x + yUp * yUp) - (BakeR + 5f));
                float ang = Mathf.Atan2(yUp, x) / (2f * Mathf.PI) + 0.5f;
                float f = Mathf.Repeat(ang * 27f, 1f);
                p.Over(Hex(0x7FD8FF), Cover(d - 1.75f, aa) * Mathf.Clamp01(f / 0.033f) * Mathf.Clamp01((0.5f - f) / 0.033f));
            });
        }

        /// <summary>Para las pruebas y el calentamiento: hornea todo, de a poco por cuadro (nada se traba).</summary>
        public static IEnumerator Prewarm()
        {
            Dormant(); Open(); RingMemory(); RingNew();
            yield return null;
            for (int k = 0; k < ConstelacionContract.ObjectKinds; k++)
            {
                Object(k, 0);
                if (ConstelacionContract.HasTwin(k)) Object(k, 1);
                yield return null;
            }
        }

        public static int CachedCount => Cache.Count;
    }
}
