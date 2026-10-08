using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Correo
{
    /// <summary>
    /// Todo el arte de «La estación de correo», horneado con el pincel de arcilla (<see cref="ClayRaster"/>) y copiado del boceto aprobado (docs/previews/correo-estacion-boceto.html: <c>drawLetter</c>, <c>drawSymbol</c>, <c>drawBox</c>,
    /// <c>drawSafe</c>, <c>drawBeacon</c>, <c>drawMailShip</c>, <c>drawSack</c>): las cartas (con sello, con sello dorado y con lazo) para cada planeta, los planetas-buzón con su figura neutra (círculo, triángulo, cuadrado, gota),
    /// el dial de la caja fuerte (marcas y manija: NADA de rueda de 3 rayos, que parece el símbolo de la paz), la torre del faro con su lámpara, la nave del correo con su sobre de emblema y su propulsor, el saco dorado, el marco punteado
    /// de la cinta y las marcas ✓ / raya del resumen. Sin sol, sin estrellas de puntas, sin media luna, sin cruces (docs/simbolos-neutros.md). Las coordenadas son las del boceto (x a la derecha, y HACIA ABAJO, en dp, con el origen
    /// en el centro de cada pieza); <see cref="Prewarm"/> los hornea de a uno por cuadro.
    /// </summary>
    public static class MailSprites
    {
        /// <summary>Píxeles por dp con que se hornea cada pieza (las cartas, 3; lo chico, 4).</summary>
        public const float LetterPpd = 3f, SmallPpd = 4f;
        /// <summary>La caja de una carta (la carta mide 112×76: lo que sobra es la sombra y el borde) y de los demás dibujos (dp).</summary>
        public const float LetterBoxW = 128f, LetterBoxH = 92f;
        public const float PlanetBox = 64f, PlanetR = 28f, DialBox = 32f, TowerBoxW = 44f, TowerBoxH = 66f, LampBox = 28f, ShipBoxW = 100f, ShipBoxH = 44f, FlameBoxW = 20f, FlameBoxH = 16f, SackBox = 40f, FrameBoxW = 140f, FrameBoxH = 100f, CheckBox = 32f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Color InkC = Hex(0x1A1240);
        private static readonly Color Red = Hex(0xE2524A);
        private static readonly Color Gold = Hex(0xFFC94A);

        private static readonly Color[] PlanetCol = { Hex(0xFF8A6B), Hex(0x7FD8FF), Hex(0xA6E36B), Hex(0xB79BFF) };
        private static readonly Color[] PlanetDark = { Hex(0xC9533A), Hex(0x3E9BC4), Hex(0x6AA63A), Hex(0x7B5FD6) };

        // ------------------------------------------------------------------ horneado

        private static Sprite Bake(string key, float boxW, float boxH, float ppd, PaintFn paint)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            int nx = Mathf.CeilToInt(boxW * ppd), ny = Mathf.CeilToInt(boxH * ppd);
            var pix = new Color32[nx * ny];
            for (int iy = 0; iy < ny; iy++)
            {
                float yUp = ((iy + 0.5f) / ny - 0.5f) * boxH;
                for (int ix = 0; ix < nx; ix++)
                {
                    float x = ((ix + 0.5f) / nx - 0.5f) * boxW;
                    var p = new Px();
                    paint(ref p, x, -yUp);                // el boceto usa y hacia abajo
                    pix[iy * nx + ix] = p.ToColor32();
                }
            }
            var tex = new Texture2D(nx, ny, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(pix);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, nx, ny), new Vector2(0.5f, 0.5f), ppd);
            Cache[key] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------ trazos (rectas, curvas de segundo y de tercer grado aplanadas)

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
            public Path Cubic(float ax, float ay, float bx, float by, float x, float y)
            {
                float sx = _cx, sy = _cy;
                for (int i = 1; i <= 10; i++)
                {
                    float t = i / 10f, u = 1f - t;
                    Pts.Add(u * u * u * sx + 3f * u * u * t * ax + 3f * u * t * t * bx + t * t * t * x);
                    Pts.Add(u * u * u * sy + 3f * u * u * t * ay + 3f * u * t * t * by + t * t * t * y);
                }
                _cx = x; _cy = y;
                return this;
            }
            public Path Arc(float cx, float cy, float r, float a0, float a1, int steps = 14)
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

        private static float Stroke(float x, float y, float[] pts, float halfWidth)
        {
            float d = float.MaxValue;
            int n = pts.Length / 2;
            for (int i = 0; i + 1 < n; i++) d = Mathf.Min(d, Capsule(x, y, pts[i * 2], pts[i * 2 + 1], pts[i * 2 + 2], pts[i * 2 + 3], halfWidth));
            return d;
        }

        /// <summary>Un relleno con borde de tinta (el trazo del boceto, centrado en el contorno: mitad adentro y mitad afuera; <paramref name="ink"/> es la mitad del grosor).</summary>
        private static void Fill(ref Px p, float sdf, Color fill, float aa, float ink = 1.5f)
        {
            p.Over(InkC, Cover(sdf - ink, aa));
            p.Over(fill, Cover(sdf + ink, aa));
        }

        // ------------------------------------------------------------------ las figuras neutras (docs §5)

        private static readonly float[] DropShape = new Path()
            .Move(0f, -0.6f).Cubic(0.35f, -0.15f, 0.5f, 0.1f, 0.5f, 0.22f).Arc(0f, 0.22f, 0.5f, 0f, Mathf.PI).Cubic(-0.5f, 0.1f, -0.35f, -0.15f, 0f, -0.6f).Array();

        /// <summary>La distancia con signo a la figura del planeta <paramref name="planet"/> (0 círculo, 1 triángulo, 2 cuadrado, 3 gota) de tamaño <paramref name="s"/> centrada en (cx, cy).</summary>
        private static float Symbol(int planet, float x, float y, float cx, float cy, float s)
        {
            switch (planet)
            {
                case 0: return Circle(x, y, cx, cy, s * 0.5f);
                case 1: return Polygon(x, y, new[] { cx, cy - s * 0.55f, cx + s * 0.52f, cy + s * 0.4f, cx - s * 0.52f, cy + s * 0.4f });
                case 2: return RoundBox(x, y, cx, cy, s * 0.42f, s * 0.42f, s * 0.16f);
                default:
                    {
                        var pts = new float[DropShape.Length];
                        for (int i = 0; i < pts.Length; i += 2) { pts[i] = cx + DropShape[i] * s; pts[i + 1] = cy + DropShape[i + 1] * s; }
                        return Polygon(x, y, pts);
                    }
            }
        }

        // ------------------------------------------------------------------ las cartas

        /// <summary>La carta de un sello del planeta <paramref name="planet"/>: normal, con sello dorado (señal focal) o con lazo (señal no focal, que cruza la carta). 128×92 dp con su sombra.</summary>
        public static Sprite Letter(int planet, MailCue cue)
        {
            planet = Mathf.Clamp(planet, 0, MailContract.PlanetCount - 1);
            return Bake("letter" + planet + "_" + (int)cue, LetterBoxW, LetterBoxH, LetterPpd, (ref Px p, float x, float y) => PaintLetter(ref p, planet, cue, x, y, 1f / LetterPpd));
        }

        // ------------------------------------------------------------------ el sello (docs §12: el dorado se distingue por FORMA y TAMAÑO, no solo por color)

        /// <summary>El sello normal mide 30 dp (liso); el dorado ~1,2 veces más (36 dp) y con el borde dentado de un sello postal. Un cuadrado con dientes, NO rayos: nada que parezca sol o estrella.</summary>
        public const float SealSize = 30f, GoldSealScale = 1.2f;
        /// <summary>Las perforaciones del sello dorado: mordidas de medio círculo (radio y paso en dp), cinco por lado, a todo el borde.</summary>
        public const float PerfRadius = 1.9f, PerfPitch = 5.5f;
        public const int PerfPerSide = 5;

        /// <summary>La mitad del lado del sello (15 dp el normal, 18 el dorado).</summary>
        public static float SealHalf(bool gold) => SealSize * (gold ? GoldSealScale : 1f) / 2f;
        /// <summary>El centro del sello en la carta (dp, y hacia abajo, origen en el centro de la carta): el dorado se corre un poco hacia adentro para no pegarse al borde del papel.</summary>
        public static float SealCx(bool gold) => gold ? 32f : 34f;
        public static float SealCy(bool gold) => gold ? -14f : -16f;

        /// <summary>La distancia con signo al sello (negativa adentro): liso y de esquinas redondas el normal; el dorado, más grande y con una mordida a cada <see cref="PerfPitch"/> en sus cuatro lados.</summary>
        public static float SealShape(bool gold, float x, float y, float cx, float cy)
        {
            float half = SealHalf(gold);
            float d = RoundBox(x, y, cx, cy, half, half, 5f);
            if (!gold) return d;
            float bite = float.MaxValue;
            float lx = x - cx, ly = y - cy;
            float maxOff = PerfPitch * (PerfPerSide / 2);
            float ax = Mathf.Clamp(Mathf.Round(lx / PerfPitch) * PerfPitch, -maxOff, maxOff);
            float ay = Mathf.Clamp(Mathf.Round(ly / PerfPitch) * PerfPitch, -maxOff, maxOff);
            bite = Mathf.Min(bite, Circle(lx, ly, ax, -half, PerfRadius));     // arriba
            bite = Mathf.Min(bite, Circle(lx, ly, ax, half, PerfRadius));      // abajo
            bite = Mathf.Min(bite, Circle(lx, ly, -half, ay, PerfRadius));     // izquierda
            bite = Mathf.Min(bite, Circle(lx, ly, half, ay, PerfRadius));      // derecha
            return Mathf.Max(d, -bite);
        }

        private static readonly float[] FoldLine = { -50f, -32f, 0f, 4f, 50f, -32f };

        private static void PaintLetter(ref Px p, int planet, MailCue cue, float x, float y, float aa)
        {
            const float w = 112f, h = 76f;
            p.Over(new Color(0f, 0f, 0f, 0.35f), Cover(RoundBox(x, y, 3f, 5f, w / 2f, h / 2f, 10f), aa));
            float paper = RoundBox(x, y, 0f, 0f, w / 2f, h / 2f, 10f);
            Fill(ref p, paper, Hex(0xF6EFDC), aa, 1.5f);
            float inside = Cover(paper + 1.5f, aa);
            p.Over(new Color(26f / 255f, 18f / 255f, 64f / 255f, 0.25f), Cover(Stroke(x, y, FoldLine, 1f), aa) * inside);
            if (cue == MailCue.Lazo)
            {
                float bx = -w * 0.18f;
                Fill(ref p, RoundBox(x, y, bx, 0f, 5f, h / 2f, 0f), Red, aa, 1f);
                for (int s = -1; s <= 1; s += 2)
                {
                    float rx, ry;
                    Rotate(x, y, bx + s * 9f, -h * 0.05f, -s * 0.4f, out rx, out ry);
                    Fill(ref p, Ellipse(rx, ry, 0f, 0f, 9f, 6f), Red, aa, 1f);
                }
                Fill(ref p, Circle(x, y, bx, -h * 0.05f, 4f), Red, aa, 1f);
            }
            bool gold = cue == MailCue.Gold;
            float sx = SealCx(gold), sy = SealCy(gold), half = SealHalf(gold);
            float seal = SealShape(gold, x, y, sx, sy);
            if (gold) { p.Over(Hex(0xB07A12), Cover(seal, aa)); p.Over(Gold, Cover(seal + 2.5f, aa)); }                // el borde café va todo hacia adentro: las mordidas dejan ver el papel
            else { p.Over(InkC, Cover(seal - 1f, aa)); p.Over(Color.white, Cover(seal + 1f, aa)); }
            p.Over(PlanetDark[planet], Cover(Symbol(planet, x, y, sx, sy, 17f * half / SealHalf(false)), aa));
            // líneas de dirección
            float x0 = -w / 2f + 14f + (cue == MailCue.Lazo ? 24f : 0f);
            var ink35 = new Color(26f / 255f, 18f / 255f, 64f / 255f, 0.35f);
            p.Over(ink35, Cover(Capsule(x, y, x0, 10f, x0 + 42f, 10f, 1.5f), aa));
            p.Over(ink35, Cover(Capsule(x, y, x0, 20f, x0 + 32f, 20f, 1.5f), aa));
        }

        // ------------------------------------------------------------------ el planeta de cada buzón

        /// <summary>El planeta del buzón <paramref name="planet"/>: una esfera de arcilla con su color, la figura del planeta en blanco y un borde de tinta (radio 28 en 64×64 dp).</summary>
        public static Sprite Planet(int planet)
        {
            planet = Mathf.Clamp(planet, 0, MailContract.PlanetCount - 1);
            return Bake("planet" + planet, PlanetBox, PlanetBox, SmallPpd, (ref Px p, float x, float y) =>
            {
                float aa = 1f / SmallPpd, R = PlanetR;
                float sdf = Circle(x, y, 0f, 0f, R);
                p.Over(InkC, Cover(sdf - 1.5f, aa));
                // degradado radial: blanco brillante arriba a la izquierda, el color del planeta y su tono oscuro hacia el borde
                float t = Mathf.Clamp01(Mathf.Sqrt((x + R * 0.35f) * (x + R * 0.35f) + (y + R * 0.4f) * (y + R * 0.4f)) / (R * 1.35f));
                Color c = t < 0.25f ? Color.Lerp(Color.white, PlanetCol[planet], t / 0.25f) : Color.Lerp(PlanetCol[planet], PlanetDark[planet], (t - 0.25f) / 0.75f);
                p.Over(c, Cover(sdf + 1.5f, aa));
                p.Over(Color.white, Cover(Symbol(planet, x, y, 0f, 0f, R * 0.9f), aa));
            });
        }

        // ------------------------------------------------------------------ la caja fuerte: un dial con marcas (y una manija)

        /// <summary>El dial de la caja fuerte (gira al guardar un encargo): un disco oscuro con aro dorado, ocho marcas y un centro. NO es una rueda de radios (parecería el símbolo de la paz).</summary>
        public static Sprite Dial() => Bake("dial", DialBox, DialBox, SmallPpd, (ref Px p, float x, float y) =>
        {
            float aa = 1f / SmallPpd;
            p.Over(Hex(0x2A2350), Cover(Circle(x, y, 0f, 0f, 13f), aa));
            p.Over(Gold, Cover(Mathf.Abs(Circle(x, y, 0f, 0f, 13f)) - 1.25f, aa));
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                p.Over(Gold, Cover(Circle(x, y, Mathf.Cos(a) * 9f, Mathf.Sin(a) * 9f, 1.4f), aa));
            }
            p.Over(Gold, Cover(Circle(x, y, 0f, 0f, 3.5f), aa));
        });

        // ------------------------------------------------------------------ el faro

        private static readonly float[] TowerPts = { -12f, 26f, -8f, -12f, 8f, -12f, 12f, 26f };

        /// <summary>La torre del faro (sin la lámpara): un trapecio claro con dos franjas rojas. El origen es el pie de la lámpara (la lámpara va 20 dp arriba).</summary>
        public static Sprite Tower() => Bake("tower", TowerBoxW, TowerBoxH, SmallPpd, (ref Px p, float x, float y) =>
        {
            float aa = 1f / SmallPpd;
            Fill(ref p, Polygon(x, y, TowerPts), Hex(0xEDEAFB), aa, 1.25f);
            p.Over(Red, Cover(RoundBox(x, y, 0f, 5.5f, 10f, 3.5f, 0f), aa));
            p.Over(Red, Cover(RoundBox(x, y, 0f, -5.5f, 9f, 2.5f, 0f), aa));
        });

        /// <summary>La lámpara del faro: apagada (gris violeta) o encendida (amarillo claro).</summary>
        public static Sprite Lamp(bool on) => Bake(on ? "lampOn" : "lampOff", LampBox, LampBox, SmallPpd, (ref Px p, float x, float y) =>
        {
            float aa = 1f / SmallPpd;
            Fill(ref p, Circle(x, y, 0f, 0f, 9f), on ? Hex(0xFFE27A) : Hex(0x6E6A9A), aa, 1.25f);
        });

        // ------------------------------------------------------------------ la nave del correo y el saco dorado

        private static readonly float[] ShipBody = new Path().Move(-38f, 4f).Quad(-36f, -16f, 0f, -16f).Quad(30f, -16f, 40f, 0f).Quad(30f, 14f, 0f, 14f).Quad(-36f, 14f, -38f, 4f).Array();
        private static readonly float[] ShipFlap = { -18f, -8f, -7f, 0f, 4f, -8f };

        /// <summary>La nave del correo (arcilla clara, ventanilla celeste y un sobre dorado de emblema); sin el propulsor. 100×44 dp con el origen en el centro de la nave.</summary>
        public static Sprite Ship() => Bake("ship", ShipBoxW, ShipBoxH, SmallPpd, (ref Px p, float x, float y) =>
        {
            float aa = 1f / SmallPpd;
            Fill(ref p, Polygon(x, y, ShipBody), Hex(0xECE8FF), aa, 1.5f);
            Fill(ref p, Circle(x, y, 20f, -3f, 6f), Hex(0x7FD8FF), aa, 1f);
            float env = RoundBox(x, y, -7f, -0.5f, 11f, 7.5f, 3f);
            Fill(ref p, env, Gold, aa, 1f);
            p.Over(InkC, Cover(Stroke(x, y, ShipFlap, 1f), aa) * Cover(env + 1f, aa));
        });

        private static readonly float[] FlameOuter = new Path().Move(-36f, -6f).Quad(-52f, 0f, -36f, 6f).Array();
        private static readonly float[] FlameInner = new Path().Move(-36f, -3f).Quad(-45f, 0f, -36f, 3f).Array();

        /// <summary>El propulsor de la nave: una llama corta (naranja con un centro amarillo). El borde derecho (donde nace) está en x = −36 de la nave; se estira desde ahí.</summary>
        public static Sprite Flame() => Bake("flame", FlameBoxW, FlameBoxH, SmallPpd, (ref Px p, float x, float y) =>
        {
            float aa = 1f / SmallPpd;
            float wx = x - 46f;                     // el centro del dibujo es x = −46 de la nave
            p.Over(Hex(0xFF9F6B), Cover(Polygon(wx, y, FlameOuter), aa));
            p.Over(Hex(0xFFE27A), Cover(Polygon(wx, y, FlameInner), aa));
        });

        private static readonly float[] SackBody = new Path().Move(-14f, -6f).Quad(-18f, 16f, 0f, 16f).Quad(18f, 16f, 14f, -6f).Array();

        /// <summary>El saco dorado de cartas (el de la nave del correo y el aviso «¡Llega un saco!»).</summary>
        public static Sprite Sack() => Bake("sack", SackBox, SackBox, SmallPpd, (ref Px p, float x, float y) =>
        {
            float aa = 1f / SmallPpd;
            Fill(ref p, Polygon(x, y, SackBody), Hex(0xE8B85A), aa, 1.25f);
            Fill(ref p, RoundBox(x, y, 0f, -7.5f, 9f, 2.5f, 0f), Hex(0xC98A2E), aa, 1.25f);
        });

        // ------------------------------------------------------------------ el marco punteado de la cinta

        /// <summary>El marco punteado (132×92, esquinas de 16) donde espera la carta de adelante: trazos de 8 y huecos de 8, en blanco (se tiñe de celeste o de dorado).</summary>
        public static Sprite DashFrame() => Bake("frame", FrameBoxW, FrameBoxH, SmallPpd, (ref Px p, float x, float y) =>
        {
            float aa = 1f / SmallPpd;
            var dashes = FrameDashes();
            float d = float.MaxValue;
            for (int i = 0; i < dashes.Count; i += 4) d = Mathf.Min(d, Capsule(x, y, dashes[i], dashes[i + 1], dashes[i + 2], dashes[i + 3], 1.5f));
            p.Over(Color.white, Cover(d, aa));
        });

        private static List<float> _frameDashes;

        private static List<float> FrameDashes()
        {
            if (_frameDashes != null) return _frameDashes;
            // el perímetro del rectángulo redondeado de 132×92 con esquinas de 16, muestreado cada ~1 dp y cortado en trazos de 8 con huecos de 8
            const float hw = 66f, hh = 46f, r = 16f;
            var perim = new List<Vector2>();
            void Corner(float cx, float cy, float a0) { for (int i = 0; i <= 8; i++) { float a = a0 + i * Mathf.PI / 16f; perim.Add(new Vector2(cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a))); } }
            perim.Add(new Vector2(-hw + r, -hh));
            perim.Add(new Vector2(hw - r, -hh));
            Corner(hw - r, -hh + r, -Mathf.PI / 2f);
            perim.Add(new Vector2(hw, hh - r));
            Corner(hw - r, hh - r, 0f);
            perim.Add(new Vector2(-hw + r, hh));
            Corner(-hw + r, hh - r, Mathf.PI / 2f);
            perim.Add(new Vector2(-hw, -hh + r));
            Corner(-hw + r, -hh + r, Mathf.PI);
            perim.Add(perim[0]);
            // puntos equiespaciados cada 1 dp
            var pts = new List<Vector2> { perim[0] };
            float acc = 0f;
            for (int i = 1; i < perim.Count; i++)
            {
                Vector2 a = perim[i - 1], b = perim[i];
                float len = Vector2.Distance(a, b);
                float pos = 0f;
                while (pos + (1f - acc) <= len)
                {
                    pos += 1f - acc;
                    acc = 0f;
                    pts.Add(new Vector2(a.x + (b.x - a.x) * (pos / len), a.y + (b.y - a.y) * (pos / len)));
                }
                acc += len - pos;
            }
            var segs = new List<float>();
            bool on = true;
            float run = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                if (on) { segs.Add(pts[i - 1].x); segs.Add(pts[i - 1].y); segs.Add(pts[i].x); segs.Add(pts[i].y); }
                run += 1f;
                if (run >= 8f) { run = 0f; on = !on; }
            }
            _frameDashes = segs;
            return segs;
        }

        // ------------------------------------------------------------------ marcas del resumen: ✓ en círculo lleno, o raya en aro vacío (se distinguen por la forma, no solo por el color)

        private static readonly float[] CheckPath = { -6f, 0f, -1.5f, 5f, 7f, -5f };

        public static Sprite CheckOk() => Bake("checkOk", CheckBox, CheckBox, SmallPpd, (ref Px p, float x, float y) =>
        {
            float aa = 1f / SmallPpd;
            p.Over(Hex(0x9FF5D6), Cover(Circle(x, y, 0f, 0f, 13f), aa));
            p.Over(InkC, Cover(Stroke(x, y, CheckPath, 1.75f), aa));
        });

        public static Sprite CheckNo() => Bake("checkNo", CheckBox, CheckBox, SmallPpd, (ref Px p, float x, float y) =>
        {
            float aa = 1f / SmallPpd;
            var coral = Hex(0xFFB4A0);
            p.Over(coral, Cover(Mathf.Abs(Circle(x, y, 0f, 0f, 12f)) - 1.5f, aa));
            p.Over(coral, Cover(Capsule(x, y, -6f, 0f, 6f, 0f, 1.75f), aa));
        });

        /// <summary>La barra del día: un degradado del celeste de la mañana al amarillo del mediodía, el naranja de la tarde y el violeta de la noche (256×16 dp con las puntas redondas).</summary>
        public static Sprite DayBar() => Bake("dayBar", 256f, 16f, 2f, (ref Px p, float x, float y) =>
        {
            float aa = 0.5f;
            float t = Mathf.Clamp01((x + 128f) / 256f);
            Color c = t < 0.5f ? Color.Lerp(Hex(0x7FD8FF), Hex(0xFFE27A), t / 0.5f) : t < 0.75f ? Color.Lerp(Hex(0xFFE27A), Hex(0xFF9F6B), (t - 0.5f) / 0.25f) : Color.Lerp(Hex(0xFF9F6B), Hex(0x7B5FD6), (t - 0.75f) / 0.25f);
            p.Over(c, Cover(RoundBox(x, y, 0f, 0f, 128f, 8f, 8f), aa));
        });

        // ------------------------------------------------------------------ horneado de a poco

        /// <summary>Hornea todo de a una pieza por cuadro (durante la cuenta regresiva); cada <c>yield</c> es una pieza.</summary>
        public static IEnumerator Prewarm()
        {
            for (int pl = 0; pl < MailContract.PlanetCount; pl++)
            {
                Planet(pl);
                yield return null;
                Letter(pl, MailCue.None);
                yield return null;
                Letter(pl, MailCue.Gold);
                yield return null;
                Letter(pl, MailCue.Lazo);
                yield return null;
            }
            Dial(); yield return null;
            Tower(); yield return null;
            Lamp(false); Lamp(true); yield return null;
            Ship(); Flame(); yield return null;
            Sack(); yield return null;
            DashFrame(); yield return null;
            CheckOk(); CheckNo(); DayBar(); yield return null;
        }

        /// <summary>Todas las claves que se hornean (para las pruebas: ninguna pieza se queda sin dibujo).</summary>
        public static int BakedCount => Cache.Count;
    }
}
