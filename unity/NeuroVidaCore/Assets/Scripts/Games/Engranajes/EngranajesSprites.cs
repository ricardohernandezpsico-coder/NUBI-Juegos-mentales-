using System;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Engranajes
{
    /// <summary>
    /// Todo el arte de «Engranajes», horneado con el pincel de arcilla (<see cref="ClayRaster"/>) en dp (1 dp = 1 unidad del boceto; y hacia arriba dentro de cada sprite).
    /// Engranajes de dientes que ENCAJAN (el diente de uno entra en el hueco del vecino: la misma geometría que <see cref="EngranajesContract.MeshPhase"/>), con relleno
    /// degradé, borde tinta, aro interior y rayos; el cohete (casco, aletas y tobera), la placa de la sala, el radar, la turbina, el portón y el elevador, las flechas de giro
    /// las poleas y los extremos de las correas, las barras dentadas, la llave inglesa de la cuenta de cambios, el ✓ de los carteles y los íconos. Los sprites que giran se hornean con el diente 0 apuntando a la derecha; los que se tiñen (flechas, íconos) van en blanco.
    /// Cada uno se hornea una sola vez (se guarda en un diccionario) y <see cref="Prewarm"/> los hornea de a uno por cuadro durante la cuenta regresiva.
    /// </summary>
    public static class EngranajesSprites
    {
        public const float Ppd = 3.5f;                 // píxeles de textura por dp
        public enum GearSize { Big, Small }
        public enum Palette { Motor, Main, Station }
        /// <summary>Íconos sueltos (blancos: se tiñen): triángulo hacia arriba o abajo (carteles de la carga y la compuerta) y el «play» de «Arrancar».</summary>
        public enum Glyph { Up, Down, Play }

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<GearSize, float[]> GearPts = new Dictionary<GearSize, float[]>();

        // colores del boceto aprobado
        public static readonly Color GearInk = Ink;
        private static readonly Color[][] Pal =
        {
            new[] { Hex(0xCFEFFF), Hex(0x5FA3E0) },   // motor
            new[] { Hex(0xF1EDFF), Hex(0xA49BE0) },   // camino y ramas
            new[] { Hex(0xFFF1C2), Hex(0xE2B24A) }    // piezas del cohete (engranaje dorado)
        };

        public static float TipOf(GearSize s) => s == GearSize.Big ? EngranajesContract.BigTip : EngranajesContract.SmallTip;
        public static int TeethOf(GearSize s) => s == GearSize.Big ? EngranajesContract.BigTeeth : EngranajesContract.SmallTeeth;

        public static GearSize SizeOf(GearDef g) => g.N >= EngranajesContract.BigTeeth ? GearSize.Big : GearSize.Small;

        public static Palette PaletteOf(GearDef g, int index)
        {
            if (index == 0 && g.Role == GearRole.Motor) return Palette.Motor;
            if (g.Role == GearRole.Station) return Palette.Station;
            return Palette.Main;
        }

        /// <summary>Lado (dp) del sprite de un engranaje: la punta más el borde y un respiro.</summary>
        public static float GearBox(GearSize s) => 2f * (TipOf(s) + 3f);

        // ------------------------------------------------------------------ horneado general

        private static Sprite Bake(string key, float wDp, float hDp, float ppd, PaintFn paint)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            int w = Mathf.CeilToInt(wDp * ppd), h = Mathf.CeilToInt(hDp * ppd);
            var pix = new Color32[w * h];
            for (int iy = 0; iy < h; iy++)
            {
                float y = ((iy + 0.5f) / h - 0.5f) * hDp;
                for (int ix = 0; ix < w; ix++)
                {
                    float x = ((ix + 0.5f) / w - 0.5f) * wDp;
                    var p = new Px();
                    paint(ref p, x, y);
                    pix[iy * w + ix] = p.ToColor32();
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(pix);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppd);
            Cache[key] = sprite;
            return sprite;
        }

        private static float Aa => 1f / Ppd;

        /// <summary>Un relleno con borde: primero la silueta entera del color del borde (hasta <paramref name="w"/> afuera) y encima el relleno (hasta <paramref name="w"/> adentro). Así no queda una
        /// costura translúcida entre el relleno y el borde (si se pintaran los dos con su propio antialiasing, en el límite sumarían solo el 75 % de opacidad).</summary>
        private static void Outlined(ref Px p, float sdf, float w, Color fill, Color edge, float aa)
        {
            p.Over(edge, Cover(sdf - w, aa));
            p.Over(fill, Cover(sdf + w, aa));
        }

        // ------------------------------------------------------------------ engranajes

        private static float[] GearPolygon(GearSize s)
        {
            if (GearPts.TryGetValue(s, out var cached)) return cached;
            float tip = TipOf(s), root = tip - EngranajesContract.ToothDepth;
            int n = TeethOf(s);
            var pts = new float[n * 8];
            float p = 2f * Mathf.PI / n;
            int k2 = 0;
            for (int k = 0; k < n; k++)
            {
                float t = k * p;
                foreach (var (a, r) in new[] { (t - 0.32f * p, root), (t - 0.17f * p, tip), (t + 0.17f * p, tip), (t + 0.32f * p, root) })
                {
                    pts[k2++] = Mathf.Cos(a) * r;
                    pts[k2++] = Mathf.Sin(a) * r;
                }
            }
            GearPts[s] = pts;
            return pts;
        }

        /// <summary>Distancia con signo al contorno del engranaje (negativa adentro). Solo se mide contra el polígono cerca de los dientes.</summary>
        private static float GearSdf(float x, float y, GearSize s)
        {
            float tip = TipOf(s), root = tip - EngranajesContract.ToothDepth;
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > tip + 2.5f) return r - tip;
            if (r < root - 2.5f) return r - root;
            return Polygon(x, y, GearPolygon(s));
        }

        private static float SpokesSdf(float x, float y, float tip, int spokes, float radius)
        {
            float step = 2f * Mathf.PI / spokes;
            float ang = Mathf.Atan2(y, x);
            float sector = Mathf.Round(ang / step);
            Rotate(x, y, 0f, 0f, -sector * step, out float rx, out float ry);
            return Capsule(rx, ry, tip * 0.2f, 0f, radius, 0f, 1.6f);
        }

        /// <summary>El engranaje de arcilla (gira): degradé, aro interior y rayos, borde tinta. El diente 0 apunta a la derecha.</summary>
        public static Sprite Gear(GearSize size, Palette palette)
        {
            float tip = TipOf(size), box = GearBox(size);
            float ringR = (tip - EngranajesContract.ToothDepth) * 0.72f;
            int spokes = TeethOf(size) >= EngranajesContract.BigTeeth ? 5 : 3;
            var c0 = Pal[(int)palette][0];
            var c1 = Pal[(int)palette][1];
            // degradé radial del boceto: de un foco chico arriba a la izquierda hasta la punta del engranaje
            float fx = -tip * 0.35f, fy = tip * 0.35f, r0 = 2f, dr = tip - 2f;
            float qa = fx * fx + fy * fy - dr * dr;
            var inkSoft = WithAlpha(Ink, 0.28f);
            return Bake("gear" + (int)size + "_" + (int)palette, box, box, Ppd, (ref Px p, float x, float y) =>
            {
                float aa = Aa;
                float sdf = GearSdf(x, y, size);
                float dx = x - fx, dy = y - fy;
                float qb = 2f * (dx * fx + dy * fy) - 2f * r0 * dr, qc = dx * dx + dy * dy - r0 * r0;
                float t;
                if (Mathf.Abs(qa) < 1e-4f) t = Mathf.Clamp01(-qc / Mathf.Min(qb, -1e-4f));
                else
                {
                    float disc = Mathf.Max(0f, qb * qb - 4f * qa * qc), sq = Mathf.Sqrt(disc);
                    float t1 = (-qb + sq) / (2f * qa), t2 = (-qb - sq) / (2f * qa);
                    t = Mathf.Clamp01(Mathf.Max(t1, t2));
                }
                Outlined(ref p, sdf, 1.2f, Color.Lerp(c0, c1, t), Ink, aa);
                float r = Mathf.Sqrt(x * x + y * y);
                float ring = Mathf.Abs(r - ringR) - 1f;
                float spoke = SpokesSdf(x, y, tip, spokes, ringR);
                p.Over(inkSoft, Mathf.Max(Cover(ring, aa), Cover(spoke, aa)) * Cover(sdf + 1.2f, aa));
            });
        }

        /// <summary>La silueta del engranaje (blanca; se tiñe de negro translúcido y se corre hacia abajo para dar la sombra dura).</summary>
        public static Sprite Silhouette(GearSize size)
        {
            float box = GearBox(size);
            return Bake("sil" + (int)size, box, box, Ppd, (ref Px p, float x, float y) => p.Over(Color.white, Cover(GearSdf(x, y, size) - 1.2f, Aa)));
        }

        /// <summary>El cubo central (no gira): disco tinta con un brillo. Lado 2 = 4 veces el radio del cubo (el cubo mide 0,18 de la punta).</summary>
        public static Sprite Hub() => Bake("hub", 2f, 2f, 40f, (ref Px p, float x, float y) =>
        {
            const float aa = 1f / 40f;
            p.Over(Ink, Cover(Circle(x, y, 0f, 0f, 0.72f), aa));
            p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Circle(x, y, -0.2f, 0.24f, 0.24f), aa));
        });

        // ------------------------------------------------------------------ la sala y el cohete

        /// <summary>La placa de la sala (242 × 348 dp): degradé, borde, cuatro tornillos y rayas tenues.</summary>
        public static Sprite Room() => Bake("room", 242f, 348f, 3f, (ref Px p, float x, float y) =>
        {
            const float aa = 1f / 3f;
            float sdf = RoundBox(x, y, 0f, 0f, 121f, 174f, 18f);
            float k = Mathf.Clamp01((174f - y) / 348f);          // 0 arriba, 1 abajo
            p.Over(Hex(0x2C3566), Cover(sdf - 0.75f, aa));
            p.Over(Color.Lerp(Hex(0x161D40), Hex(0x0F1531), k), Cover(sdf + 0.75f, aa));
            // rayas tenues cada 25 dp
            float lx = x + 121f;
            float fold = Mathf.Abs(lx / 25f - Mathf.Round(lx / 25f)) * 25f;
            if (lx > 20f && lx < 235f && Mathf.Abs(y) < 170f) p.Over(new Color(127f / 255f, 216f / 255f, 1f, 0.05f), Cover(fold - 0.5f, aa));
            foreach (var (sx, sy) in new[] { (-109f, 162f), (109f, 162f), (-109f, -162f), (109f, -162f) })
                p.Over(Hex(0x2C3566), Cover(Circle(x, y, sx, sy, 4f), aa));
        });

        private const float HullX = 306f, HullY = 267f;                     // centro del sprite del casco, en coordenadas del boceto
        private static float[] _hullBody;

        private static float[] HullBody()
        {
            if (_hullBody != null) return _hullBody;
            // casco: pared izquierda, curva de la nariz (dos cuadráticas; la punta en 104: el cohete baja un poco para que el radar no pise la consigna), pared derecha; en coordenadas del boceto → locales (y hacia arriba)
            var pts = new List<float>();
            void Add(float bx, float by) { pts.Add(bx - HullX); pts.Add(HullY - by); }
            Add(262f, 428f); Add(262f, 150f);
            const int steps = 14;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1f - t;
                Add(u * u * 262f + 2f * u * t * 262f + t * t * 306f, u * u * 150f + 2f * u * t * 122f + t * t * 104f);
            }
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1f - t;
                Add(u * u * 306f + 2f * u * t * 350f + t * t * 350f, u * u * 104f + 2f * u * t * 122f + t * t * 150f);
            }
            Add(350f, 428f);
            _hullBody = pts.ToArray();
            return _hullBody;
        }

        /// <summary>El casco del cohete (116 × 358 dp, centrado en (306, 267) del boceto): degradé lila, aletas rojas y tobera, bordes tinta.</summary>
        public static Sprite Hull() => Bake("hull", 116f, 358f, Ppd, (ref Px p, float x, float y) =>
        {
            float aa = Aa;
            // en el tramo recto de las paredes la distancia es solo la horizontal (así no se mide contra todo el polígono en cada píxel)
            float body = y < HullY - 190f && y > HullY - 380f ? Mathf.Abs(x) - 44f : Polygon(x, y, HullBody());
            float u = Mathf.Clamp01((x + 44f) / 88f);
            Color fill = u < 0.45f ? Color.Lerp(Hex(0xD9D4F7), Hex(0xF4F1FF), u / 0.45f) : Color.Lerp(Hex(0xF4F1FF), Hex(0xB7AFE6), (u - 0.45f) / 0.55f);
            Outlined(ref p, body, 1.5f, fill, Ink, aa);
            // aletas
            var fin = Hex(0xC93C3C);
            var left = new[] { 262f - HullX, HullY - 368f, 252f - HullX, HullY - 440f, 262f - HullX, HullY - 428f };
            var right = new[] { 350f - HullX, HullY - 368f, 360f - HullX, HullY - 440f, 350f - HullX, HullY - 428f };
            foreach (var f in new[] { left, right })
            {
                float d = Polygon(x, y, f);
                Outlined(ref p, d, 1.25f, fin, Ink, aa);
            }
            // tobera
            float noz = RoundBox(x, y, 0f, HullY - 433f, 18f, 5f, 3f);
            Outlined(ref p, noz, 1.25f, Hex(0x4A5080), Ink, aa);
        });

        // ------------------------------------------------------------------ las piezas del cohete

        /// <summary>El radar de la antena (gira con su engranaje): elipse celeste de borde tinta con un punto coral en la punta.</summary>
        public static Sprite Dish() => Bake("dish", 48f, 24f, Ppd, (ref Px p, float x, float y) =>
        {
            float aa = Aa;
            float e = Ellipse(x, y, 0f, 0f, 20f, 7f);
            Outlined(ref p, e, 1.2f, Hex(0xBFE9FF), Ink, aa);
            p.Over(Hex(0xFF8C6B), Cover(Circle(x, y, 18f, 0f, 3.5f), aa));
        });

        /// <summary>La turbina (gira con su engranaje): cuatro aspas doradas con borde tinta y un cubo.</summary>
        public static Sprite Fan() => Bake("fan", 40f, 40f, Ppd, (ref Px p, float x, float y) =>
        {
            float aa = Aa;
            float cx = x, cy = -y;                                // coordenadas de lienzo (y hacia abajo), como el boceto
            for (int j = 0; j < 4; j++)
            {
                Rotate(cx, cy, 0f, 0f, -j * Mathf.PI / 2f, out float rx, out float ry);
                Rotate(rx, ry + 7f, 0f, 0f, -0.4f, out float ex, out float ey);
                float d = Ellipse(ex, ey, 0f, 0f, 4.5f, 8f);
                Outlined(ref p, d, 1f, Hex(0xFFC94A), Ink, aa);
            }
            p.Over(Ink, Cover(Circle(x, y, 0f, 0f, 3f), aa));
        });

        /// <summary>El portón de la compuerta (40 × 48): gris violeta con borde tinta y cuatro rayas.</summary>
        public static Sprite Door() => Bake("door", 44f, 52f, Ppd, (ref Px p, float x, float y) =>
        {
            float aa = Aa;
            float d = RoundBox(x, y, 0f, 0f, 20f, 24f, 6f);
            Outlined(ref p, d, 1.2f, Hex(0x8C86B8), Ink, aa);
            foreach (float ly in new[] { 14f, 5f, -4f, -13f })
                p.Over(WithAlpha(Ink, 0.35f), Cover(RoundBox(x, y, 0f, ly, 14f, 1f, 0f), aa) * Cover(d + 1.2f, aa));
        });

        /// <summary>El marco del elevador de carga (44 × 64): solo el contorno.</summary>
        public static Sprite CargoFrame() => Bake("cframe", 48f, 68f, Ppd, (ref Px p, float x, float y) =>
            p.Over(Hex(0x6B5FB8), Cover(Mathf.Abs(RoundBox(x, y, 0f, 0f, 22f, 32f, 6f)) - 1f, Aa)));

        /// <summary>La plataforma dorada del elevador (36 × 6).</summary>
        public static Sprite CargoPlate() => Bake("cplate", 40f, 10f, Ppd, (ref Px p, float x, float y) =>
        {
            float d = RoundBox(x, y, 0f, 0f, 18f, 3f, 2f);
            Outlined(ref p, d, 1f, Hex(0xE9C46A), Ink, Aa);
        });

        /// <summary>La caja de carga (28 × 20): celeste con borde tinta.</summary>
        public static Sprite CargoBox() => Bake("cbox", 32f, 24f, Ppd, (ref Px p, float x, float y) =>
        {
            float d = RoundBox(x, y, 0f, 0f, 14f, 10f, 4f);
            Outlined(ref p, d, 1f, Hex(0x7FD8FF), Ink, Aa);
        });

        /// <summary>Una polea (14 de radio) en el eje del engranaje de cada extremo de una correa: disco oscuro, disco violeta y un agujero de eje.</summary>
        public static Sprite Pulley() => Bake("pulley", 40f, 40f, Ppd, (ref Px p, float x, float y) =>
        {
            float aa = Aa;
            float r = Mathf.Sqrt(x * x + y * y);
            p.Over(Hex(0x2A2350), Cover(r - (EngranajesContract.PulleyRadius + 3f), aa));
            p.Over(Hex(0x4A4390), Cover(r - (EngranajesContract.PulleyRadius - 2f), aa));
            p.Over(Ink, Cover(r - 4f, aa));
        });

        /// <summary>La media vuelta de la correa alrededor de una polea (radio de la línea central 14, grosor <paramref name="width"/>): la mitad de la izquierda, con puntas redondas. Blanca: se tiñe
        /// (oscuro, claro o dorado según la capa). Se gira para que mire hacia afuera de la correa.</summary>
        public static Sprite BeltCap(float width)
        {
            float box = 2f * (EngranajesContract.PulleyRadius + width / 2f + 2f);
            return Bake("cap" + width.ToString("0.#"), box, box, Ppd, (ref Px p, float x, float y) =>
            {
                float r = EngranajesContract.PulleyRadius;
                float d = Mathf.Abs(Mathf.Sqrt(x * x + y * y) - r) - width / 2f;
                if (x > 0f) d = Mathf.Min(Circle(x, y, 0f, r, width / 2f), Circle(x, y, 0f, -r, width / 2f));       // en la mitad de la derecha solo quedan las puntas redondas
                p.Over(Color.white, Cover(d, Aa));
            });
        }

        /// <summary>La barra dentada de la carga y de la compuerta (20 × 84 dp, centrada en la barra de 9 × 80): violeta con borde tinta y diez dientes tinta a la izquierda.</summary>
        public static Sprite Rack() => Bake("rack", 20f, 84f, Ppd, (ref Px p, float x, float y) =>
        {
            float aa = Aa;
            float d = RoundBox(x, y, 0f, 0f, 4.5f, 40f, 3f);
            Outlined(ref p, d, 1f, Hex(0x8E83D8), Ink, aa);
            float teeth = 1000f;
            for (float ty = -36f; ty < 40f; ty += 8f) teeth = Mathf.Min(teeth, RoundBox(x, y, -5.5f, -(ty + 2f), 2f, 2f, 0f));
            p.Over(Ink, Cover(teeth, aa));
        });

        /// <summary>La llave inglesa de la cuenta de cambios (30 × 30): mango y boca abierta, girada 45°. Blanca: se tiñe de dorado al usarla.</summary>
        public static Sprite Wrench() => Bake("wrench", 30f, 30f, Ppd, (ref Px p, float x, float y) =>
        {
            const float s = 15f, c = 0.70710678f;
            // lienzo con y hacia abajo y la figura girada 45° en el sentido del reloj: se deshace el giro para medir contra el mango y la boca
            float cx = x, cy = -y;
            float ux = cx * c + cy * c, uy = -cx * c + cy * c;
            float handle = Capsule(ux, -uy, 0f, s * 0.2f, 0f, -s * 0.9f, s * 0.15f);
            float hy = uy + s * 0.45f;
            float ring = Mathf.Abs(Mathf.Sqrt(ux * ux + hy * hy) - s * 0.32f) - s * 0.12f;
            float toward = Mathf.Atan2(hy, ux) + Mathf.PI / 2f;                     // el ángulo respecto de «arriba» (la boca de la llave)
            float gap = Mathf.Abs(Mathf.Atan2(Mathf.Sin(toward), Mathf.Cos(toward)));
            if (gap < 0.75f) ring = 1f;
            p.Over(Color.white, Cover(Mathf.Min(handle, ring), Aa));
        });

        /// <summary>El ✓ de un cartel que cumplió (14 × 14). Blanco: se tiñe de menta.</summary>
        public static Sprite Check() => Bake("check", 14f, 14f, Ppd, (ref Px p, float x, float y) =>
            p.Over(Color.white, Cover(Mathf.Min(Capsule(x, y, -5f, 0.5f, -1.5f, -4.5f, 1.5f), Capsule(x, y, -1.5f, -4.5f, 5.5f, 4.5f, 1.5f)), Aa)));

        /// <summary>Íconos sueltos: triángulo arriba / abajo y «play» (blancos).</summary>
        public static Sprite GlyphSprite(Glyph g)
        {
            const float box = 56f;
            switch (g)
            {
                case Glyph.Up: return Bake("glyph_up", box, box, Ppd, (ref Px p, float x, float y) =>
                    p.Over(Color.white, Cover(Polygon(x, y, new[] { 0f, 14f, 12f, -6f, -12f, -6f }), Aa)));
                case Glyph.Down: return Bake("glyph_down", box, box, Ppd, (ref Px p, float x, float y) =>
                    p.Over(Color.white, Cover(Polygon(x, y, new[] { 0f, -14f, -12f, 6f, 12f, 6f }), Aa)));
                default: return Bake("glyph_play", box, box, Ppd, (ref Px p, float x, float y) =>
                    p.Over(Color.white, Cover(Polygon(x, y, new[] { -9f, 14f, -9f, -14f, 13f, 0f }), Aa)));
            }
        }

        // ------------------------------------------------------------------ flechas e íconos (blancos: se tiñen con Image.color)

        private const float ArcStart = -Mathf.PI * 0.775f, ArcEnd = -Mathf.PI * 0.225f;

        private static float ArcStroke(float cx, float cy, float r, float lw, float a0, float a1)
        {
            // cx, cy en coordenadas de lienzo (y hacia abajo); el arco va de a0 a a1 (por arriba)
            float ang = Mathf.Atan2(cy, cx), rad = Mathf.Sqrt(cx * cx + cy * cy);
            float d0 = Circle(cx, cy, Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, lw / 2f), d1 = Circle(cx, cy, Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, lw / 2f);
            if (ang < a0 || ang > a1) return Mathf.Min(d0, d1);
            return Mathf.Min(Mathf.Abs(rad - r) - lw / 2f, Mathf.Min(d0, d1));
        }

        private static float[] ArrowHead(float r, float a1, float size)
        {
            // la punta está en el extremo final del arco (a1); como el boceto: punta a 1,5 veces el tamaño y base a 1,35 con ±2,4 rad
            float tx = Mathf.Cos(a1) * r, ty = Mathf.Sin(a1) * r, tan = a1 + Mathf.PI / 2f;
            return new[]
            {
                tx + Mathf.Cos(tan) * size * 1.5f, ty + Mathf.Sin(tan) * size * 1.5f,
                tx + Mathf.Cos(tan + 2.4f) * size * 1.35f, ty + Mathf.Sin(tan + 2.4f) * size * 1.35f,
                tx + Mathf.Cos(tan - 2.4f) * size * 1.35f, ty + Mathf.Sin(tan - 2.4f) * size * 1.35f
            };
        }

        /// <summary>Flecha de giro del motor (arco con punta) de radio <paramref name="radius"/> y grosor <paramref name="lineWidth"/>, centrada en el engranaje. <paramref name="clockwise"/> = como el reloj.</summary>
        public static Sprite Arrow(float radius, float lineWidth, bool clockwise)
        {
            float box = 2f * (radius + 14f);
            var head = ArrowHead(radius, ArcEnd, 6f);
            return Bake("arrow" + radius.ToString("0.#") + "_" + lineWidth.ToString("0.#") + (clockwise ? "c" : "a"), box, box, Ppd, (ref Px p, float x, float y) =>
            {
                float cx = clockwise ? x : -x, cy = -y;
                float d = Mathf.Min(ArcStroke(cx, cy, radius, lineWidth, ArcStart, ArcEnd), Polygon(cx, cy, head));
                p.Over(Color.white, Cover(d, Aa));
            });
        }

        /// <summary>La flechita de giro de un cartel (radio 6,5, casi una vuelta entera: 1,3 π). Blanca: se tiñe.</summary>
        public static Sprite MiniArrow(bool clockwise)
        {
            const float r = 6.5f, a0 = -Mathf.PI * 1.15f, a1 = Mathf.PI * 0.15f;
            var head = ArrowHead(r, a1, 4f);
            return Bake("miniarrow" + (clockwise ? "c" : "a"), 22f, 22f, Ppd, (ref Px p, float x, float y) =>
            {
                float cx = clockwise ? x : -x, cy = -y;
                p.Over(Color.white, Cover(Mathf.Min(ArcStroke(cx, cy, r, 2.6f, a0, a1), Polygon(cx, cy, head)), Aa));
            });
        }

        private static float[] MiniGear(float tip, int n)
        {
            float root = tip - 6f, p = 2f * Mathf.PI / n;
            var pts = new float[n * 8];
            int k2 = 0;
            for (int k = 0; k < n; k++)
            {
                float t = k * p;
                foreach (var (a, r) in new[] { (t - 0.32f * p, root), (t - 0.17f * p, tip), (t + 0.17f * p, tip), (t + 0.32f * p, root) })
                {
                    pts[k2++] = Mathf.Cos(a) * r;
                    pts[k2++] = Mathf.Sin(a) * r;
                }
            }
            return pts;
        }

        // ------------------------------------------------------------------ cuántos sprites hay y su horneado por tandas

        /// <summary>Hornea todo lo de la partida, de a una pieza por cuadro (para que la cuenta regresiva no se trabe). Lo que ya está horneado no se repite.</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            foreach (GearSize sz in Enum.GetValues(typeof(GearSize)))
            {
                Silhouette(sz);
                yield return null;
                foreach (Palette pal in Enum.GetValues(typeof(Palette)))
                {
                    Gear(sz, pal);
                    yield return null;
                }
            }
            Hub(); yield return null;
            Room(); yield return null;
            Hull(); yield return null;
            Dish(); Fan(); Door(); yield return null;
            CargoFrame(); CargoPlate(); CargoBox(); yield return null;
            Pulley(); Rack(); Wrench(); Check(); yield return null;
            foreach (var w in BeltWidths) BeltCap(w);
            yield return null;
            foreach (Glyph g in Enum.GetValues(typeof(Glyph))) GlyphSprite(g);
            MiniArrow(true); MiniArrow(false);
            yield return null;
            foreach (var r in new[] { EngranajesContract.BigTip + 8f, EngranajesContract.SmallTip + 8f })
            {
                Arrow(r, 4f, true);
                Arrow(r, 4f, false);
            }
        }

        /// <summary>Los tres grosores de la correa: el brillo dorado (16), el borde oscuro (8) y la banda clara (4,5).</summary>
        public static readonly float[] BeltWidths = { 16f, 8f, 4.5f };

        /// <summary>Para las pruebas: cuántos sprites hay horneados.</summary>
        public static int BakedCount => Cache.Count;
    }
}
