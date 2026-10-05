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
    /// y los íconos de los botones. Los sprites que giran se hornean con el diente 0 apuntando a la derecha; los que se tiñen (flechas, íconos) van en blanco.
    /// Cada uno se hornea una sola vez (se guarda en un diccionario) y <see cref="Prewarm"/> los hornea de a uno por cuadro durante la cuenta regresiva.
    /// </summary>
    public static class EngranajesSprites
    {
        public const float Ppd = 3.5f;                 // píxeles de textura por dp
        public enum GearSize { Big, Small, Jam }
        public enum Palette { Motor, Main, Station, Jam }

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<GearSize, float[]> GearPts = new Dictionary<GearSize, float[]>();

        // colores del boceto aprobado
        public static readonly Color GearInk = Ink;
        private static readonly Color[][] Pal =
        {
            new[] { Hex(0xCFEFFF), Hex(0x5FA3E0) },   // motor
            new[] { Hex(0xF1EDFF), Hex(0xA49BE0) },   // camino y ramas
            new[] { Hex(0xFFF1C2), Hex(0xE2B24A) },   // piezas del cohete (engranaje dorado)
            new[] { Hex(0xFFD6C8), Hex(0xE07E62) }    // trampa
        };

        public static float TipOf(GearSize s) => s == GearSize.Big ? EngranajesContract.BigTip : s == GearSize.Small ? EngranajesContract.SmallTip : EngranajesContract.JamTip;
        public static int TeethOf(GearSize s) => s == GearSize.Big ? EngranajesContract.BigTeeth : s == GearSize.Small ? EngranajesContract.SmallTeeth : EngranajesContract.JamTeeth;

        public static GearSize SizeOf(GearDef g) => g.N >= EngranajesContract.BigTeeth ? GearSize.Big : g.N >= EngranajesContract.SmallTeeth ? GearSize.Small : GearSize.Jam;

        public static Palette PaletteOf(GearDef g, int index)
        {
            if (index == 0 && g.Role == GearRole.Motor) return Palette.Motor;
            if (g.Role == GearRole.Station) return Palette.Station;
            if (g.Role == GearRole.Jam) return Palette.Jam;
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
            // casco: pared izquierda, curva de la nariz (dos cuadráticas), pared derecha; en coordenadas del boceto → locales (y hacia arriba)
            var pts = new List<float>();
            void Add(float bx, float by) { pts.Add(bx - HullX); pts.Add(HullY - by); }
            Add(262f, 428f); Add(262f, 142f);
            const int steps = 14;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1f - t;
                Add(u * u * 262f + 2f * u * t * 262f + t * t * 306f, u * u * 142f + 2f * u * t * 110f + t * t * 92f);
            }
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1f - t;
                Add(u * u * 306f + 2f * u * t * 350f + t * t * 350f, u * u * 92f + 2f * u * t * 110f + t * t * 142f);
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

        /// <summary>El aro fino que late alrededor de la pieza preguntada (radio 26, grosor 3; blanco: se tiñe de dorado).</summary>
        public static Sprite TargetRing() => Bake("tring", 64f, 64f, Ppd, (ref Px p, float x, float y) =>
            p.Over(Color.white, Cover(Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 26f) - 1.5f, Aa)));

        /// <summary>Aro punteado de radio unidad (el hueco de «Arma tú»): 22 rayitas; se escala al tamaño del hueco. Blanco.</summary>
        public static Sprite DashedRing() => Bake("dring", 2.4f, 2.4f, 90f, (ref Px p, float x, float y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float frac = (Mathf.Atan2(y, x) / (2f * Mathf.PI) + 1f) * 22f;
            bool on = frac - Mathf.Floor(frac) < 0.5f;
            if (on) p.Over(Color.white, Cover(Mathf.Abs(r - 1f) - 0.04f, 1f / 90f));
        });

        // ------------------------------------------------------------------ flechas e íconos (blancos: se tiñen con Image.color)

        private const float ArcStart = -Mathf.PI * 0.85f, ArcEnd = -Mathf.PI * 0.15f;

        private static float ArcStroke(float cx, float cy, float r, float lw)
        {
            // cx, cy en coordenadas de lienzo (y hacia abajo); el arco va de ArcStart a ArcEnd (por arriba)
            float ang = Mathf.Atan2(cy, cx), rad = Mathf.Sqrt(cx * cx + cy * cy);
            float d = Mathf.Abs(rad - r) - lw / 2f;
            if (ang < ArcStart || ang > ArcEnd)
            {
                float d0 = Circle(cx, cy, Mathf.Cos(ArcStart) * r, Mathf.Sin(ArcStart) * r, lw / 2f), d1 = Circle(cx, cy, Mathf.Cos(ArcEnd) * r, Mathf.Sin(ArcEnd) * r, lw / 2f);
                return Mathf.Min(d0, d1);
            }
            return Mathf.Min(d, Mathf.Min(Circle(cx, cy, Mathf.Cos(ArcStart) * r, Mathf.Sin(ArcStart) * r, lw / 2f), Circle(cx, cy, Mathf.Cos(ArcEnd) * r, Mathf.Sin(ArcEnd) * r, lw / 2f)));
        }

        private static float[] ArrowHead(float r)
        {
            // la punta está en el extremo final del arco (a1); como el boceto: punta a 9 y base a 8 con ±2,4 rad
            float tx = Mathf.Cos(ArcEnd) * r, ty = Mathf.Sin(ArcEnd) * r, tan = ArcEnd + Mathf.PI / 2f;
            return new[]
            {
                tx + Mathf.Cos(tan) * 9f, ty + Mathf.Sin(tan) * 9f,
                tx + Mathf.Cos(tan + 2.4f) * 8f, ty + Mathf.Sin(tan + 2.4f) * 8f,
                tx + Mathf.Cos(tan - 2.4f) * 8f, ty + Mathf.Sin(tan - 2.4f) * 8f
            };
        }

        /// <summary>Flecha de giro (arco con punta) de radio <paramref name="radius"/> y grosor <paramref name="lineWidth"/>, centrada en el engranaje. <paramref name="clockwise"/> = como el reloj.</summary>
        public static Sprite Arrow(float radius, float lineWidth, bool clockwise)
        {
            float box = 2f * (radius + 14f);
            var head = ArrowHead(radius);
            return Bake("arrow" + radius.ToString("0.#") + "_" + lineWidth.ToString("0.#") + (clockwise ? "c" : "a"), box, box, Ppd, (ref Px p, float x, float y) =>
            {
                float cx = clockwise ? x : -x, cy = -y;
                float d = Mathf.Min(ArcStroke(cx, cy, radius, lineWidth), Polygon(cx, cy, head));
                p.Over(Color.white, Cover(d, Aa));
            });
        }

        public static Sprite Icon(ButtonIcon icon)
        {
            const float box = 56f;
            switch (icon)
            {
                case ButtonIcon.Cw: return Bake("icon_cw", box, box, Ppd, (ref Px p, float x, float y) =>
                    p.Over(Color.white, Cover(Mathf.Min(ArcStroke(x, -y - 10f, 17f, 4f), Polygon(x, -y - 10f, ArrowHead(17f))), Aa)));
                case ButtonIcon.Ccw: return Bake("icon_ccw", box, box, Ppd, (ref Px p, float x, float y) =>
                    p.Over(Color.white, Cover(Mathf.Min(ArcStroke(-x, -y - 10f, 17f, 4f), Polygon(-x, -y - 10f, ArrowHead(17f))), Aa)));
                case ButtonIcon.X: return Bake("icon_x", box, box, Ppd, (ref Px p, float x, float y) =>
                    p.Over(Color.white, Cover(Mathf.Min(Capsule(x, y, -11f, -11f, 11f, 11f, 2f), Capsule(x, y, -11f, 11f, 11f, -11f, 2f)), Aa)));
                case ButtonIcon.Up: return Bake("icon_up", box, box, Ppd, (ref Px p, float x, float y) =>
                    p.Over(Color.white, Cover(Polygon(x, y, new[] { 0f, 14f, 12f, -6f, -12f, -6f }), Aa)));
                case ButtonIcon.Down: return Bake("icon_down", box, box, Ppd, (ref Px p, float x, float y) =>
                    p.Over(Color.white, Cover(Polygon(x, y, new[] { 0f, -14f, -12f, 6f, 12f, 6f }), Aa)));
                case ButtonIcon.Fast: return Bake("icon_fast", box, box, Ppd, (ref Px p, float x, float y) =>
                {
                    // dos triángulos que apuntan a la derecha (centrados en la casilla)
                    float t1 = Polygon(x, y, new[] { 2f, 0f, -18f, 12f, -18f, -12f });
                    float t2 = Polygon(x, y, new[] { 18f, 0f, -2f, 12f, -2f, -12f });
                    p.Over(Color.white, Cover(Mathf.Min(t1, t2), Aa));
                });
                case ButtonIcon.Slow: return Bake("icon_slow", box, box, Ppd, (ref Px p, float x, float y) =>
                {
                    float ring = Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 12f) - 2f;
                    float hands = Mathf.Min(Capsule(x, y, 0f, 0f, 0f, 8f, 2f), Capsule(x, y, 0f, 0f, 6f, -3f, 2f));
                    p.Over(Color.white, Cover(Mathf.Min(ring, hands), Aa));
                });
                case ButtonIcon.Gear: return Bake("icon_gear", box, box, Ppd, (ref Px p, float x, float y) =>
                    p.Over(Color.white, Cover(Subtract(Polygon(x, y, MiniGear(16f, 10)), Circle(x, y, 0f, 0f, 5f)), Aa)));
                default: return Bake("icon_belt", box, box, Ppd, (ref Px p, float x, float y) =>
                {
                    float c1 = Mathf.Abs(Circle(x, y, -13f, 0f, 7f)) - 2f, c2 = Mathf.Abs(Circle(x, y, 13f, 0f, 7f)) - 2f;
                    float l = Mathf.Min(Capsule(x, y, -13f, 7f, 13f, -7f, 2f), Capsule(x, y, -13f, -7f, 13f, 7f, 2f));
                    p.Over(Color.white, Cover(Mathf.Min(Mathf.Min(c1, c2), l), Aa));
                });
            }
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
            foreach (GearSize s in Enum.GetValues(typeof(GearSize)))
            {
                Silhouette(s);
                yield return null;
                foreach (Palette pal in Enum.GetValues(typeof(Palette)))
                {
                    if (s == GearSize.Jam && pal != Palette.Jam) continue;     // la trampa solo es coral
                    if (s != GearSize.Jam && pal == Palette.Jam) continue;
                    Gear(s, pal);
                    yield return null;
                }
            }
            Hub(); yield return null;
            Room(); yield return null;
            Hull(); yield return null;
            Dish(); Fan(); Door(); TargetRing(); DashedRing(); yield return null;
            CargoFrame(); CargoPlate(); CargoBox(); yield return null;
            foreach (ButtonIcon ic in Enum.GetValues(typeof(ButtonIcon))) { Icon(ic); }
            yield return null;
            foreach (var (r, lw) in new[] { (43f, 4f), (33f, 4f), (40f, 3.5f), (30f, 3.5f) })
            {
                Arrow(r, lw, true);
                Arrow(r, lw, false);
            }
        }

        /// <summary>Para las pruebas: cuántos sprites hay horneados.</summary>
        public static int BakedCount => Cache.Count;
    }
}
