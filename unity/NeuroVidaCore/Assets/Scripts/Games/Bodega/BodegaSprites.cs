using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Bitacora;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Bodega
{
    /// <summary>
    /// Todo el arte de «Bodega de carga», horneado con el pincel de arcilla (<see cref="ClayRaster"/>) en dp (1 dp = 1 unidad del boceto; y hacia arriba dentro de cada sprite): el casco de la bodega,
    /// la esclusa de carga (posición 0 del anillo) con su planeta y su aro de sello punteado, el marco y el interior de cada escotilla, la hoja de la puerta (media luna que se desliza hacia un lado), el cuerpo del robot, la caja, el ✓ y los 12 objetos.
    /// Diez de los objetos reutilizan las siluetas de arcilla de Bitácora de Misión (<see cref="BitacoraSprites.RenderFind"/>; sin tocar ese juego); la manzana y la taza son nuevas (la «copa» parecía un cáliz
    /// y la estrella de mar y el ancla quedan fuera: regla de símbolos). Lo que se tiñe en pantalla va en blanco. Cada uno se hornea una sola vez y <see cref="Prewarm"/> los hornea de a uno por cuadro.
    /// </summary>
    public static class BodegaSprites
    {
        public const float Ppd = 3f;
        public const float HatchR = BodegaLayout.HatchR, FrameR = BodegaLayout.HatchR + BodegaLayout.FrameW, WindowR = BodegaLayout.HatchR + 6f, RobotR = 26f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>De cada objeto de la bodega al hallazgo de Bitácora cuya silueta usa; -1 = silueta propia (manzana y taza).</summary>
        private static readonly int[] FromBitacora = { 0, 1, 6, -1, 12, -1, 15, 9, 11, 4, 2, 8 };

        // colores del boceto aprobado
        public static readonly Color HullInk = Hex(0x2C3566);
        public static readonly Color Gold = Hex(0xFFC94A);
        public static readonly Color Door0 = Hex(0xECE8FF), Door1 = Hex(0x9C92DA);

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

        private static float Aa(float ppd) => 1f / ppd;

        /// <summary>Un relleno con borde: primero la silueta entera del color del borde y encima el relleno (sin costura translúcida entre los dos).</summary>
        private static void Outlined(ref Px p, float sdf, float w, Color fill, Color edge, float aa)
        {
            p.Over(edge, Cover(sdf - w, aa));
            p.Over(fill, Cover(sdf + w, aa));
        }

        private static Color Lerp3(Color a, Color b, Color c, float t, float mid) => t < mid ? Color.Lerp(a, b, t / mid) : Color.Lerp(b, c, (t - mid) / (1f - mid));

        // ------------------------------------------------------------------ la bodega

        /// <summary>El casco redondo de la bodega: degradé radial, borde y un aro tenue (radio 169 dp).</summary>
        public static Sprite Hull()
        {
            const float ppd = 2.5f, r0 = BodegaLayout.HullR;
            float box = 2f * (r0 + 3f);
            return Bake("hull", box, box, ppd, (ref Px p, float x, float y) =>
            {
                float aa = Aa(ppd), r = Mathf.Sqrt(x * x + y * y);
                float t = Mathf.Clamp01((r - 40f) / (BodegaLayout.Ring + 46f - 40f));
                var fill = Lerp3(Hex(0x1B2350), Hex(0x141B42), Hex(0x0C1130), t, 0.7f);
                p.Over(HullInk, Cover(r - (r0 + 1f), aa));
                p.Over(fill, Cover(r - (r0 - 1f), aa));
                p.Over(new Color(127f / 255f, 216f / 255f, 1f, 0.08f), Cover(Mathf.Abs(r - (BodegaLayout.Ring - 42f)) - 0.5f, aa) * Cover(r - r0, aa));
            });
        }

        /// <summary>La esclusa de carga (la posición 0 del anillo): un vidrio al espacio con marco dorado, cielo, planeta celeste con su anillo, estrellas y un reflejo.</summary>
        public static Sprite Airlock()
        {
            const float ppd = 4f, hr = BodegaLayout.HatchR;
            float box = 2f * (WindowR + 2f);
            return Bake("airlock", box, box, ppd, (ref Px p, float x, float y) =>
            {
                float aa = Aa(ppd);
                p.Over(Gold, Cover(Circle(x, y, 0f, 0f, WindowR + 1.5f), aa));
                p.Over(Hex(0x2A2350), Cover(Circle(x, y, 0f, 0f, WindowR - 1.5f), aa));
                float inside = Cover(Circle(x, y, 0f, 0f, hr), aa);
                float k = Mathf.Clamp01((hr - y) / (2f * hr));            // 0 arriba, 1 abajo
                p.Over(Color.Lerp(Hex(0x06123A), Hex(0x1A2D6A), k), inside);
                p.Over(Hex(0x7FD8FF), Cover(Circle(x, y, 8f, -8f, 13f), aa) * inside);
                Rotate(x, y, 8f, -8f, 0.4f, out float rx, out float ry);
                p.Over(new Color(1f, 1f, 1f, 0.35f), Cover(Ellipse(rx, ry, 8f, -8f, 16f, 3.5f), aa) * inside);          // el anillo del planeta
                foreach (var (sx, sy, sr) in new[] { (-12f, 10f, 1.4f), (-4f, 16f, 1f), (-16f, -4f, 1.1f), (14f, 12f, 0.9f) })
                    p.Over(Color.white, Cover(Circle(x, y, sx, sy, sr), aa) * inside);
                p.Over(new Color(1f, 1f, 1f, 0.18f), Cover(Circle(x, y, -8f, 8f, 8f), aa));
            });
        }

        /// <summary>El marco de una escotilla (disco azul noche con borde tinta).</summary>
        public static Sprite HatchFrame()
        {
            const float ppd = 4f;
            float box = 2f * (FrameR + 2.5f);
            return Bake("hframe", box, box, ppd, (ref Px p, float x, float y) =>
            {
                float aa = Aa(ppd), sdf = Circle(x, y, 0f, 0f, FrameR);
                Outlined(ref p, sdf, 1.5f, Hex(0x232B57), Ink, aa);
            });
        }

        /// <summary>El interior de una escotilla: 0 = vacío (oscuro), 1 = con algo guardado (luz cálida morada), 2 = cerrada (plano del color del marco: se ve solo el borde alrededor de la puerta).</summary>
        public static Sprite HatchInterior(int kind)
        {
            const float ppd = 4f;
            float box = 2f * (HatchR + 1f);
            return Bake("hin" + kind, box, box, ppd, (ref Px p, float x, float y) =>
            {
                float aa = Aa(ppd), r = Mathf.Sqrt(x * x + y * y);
                var c = kind == 2 ? Hex(0x232B57) : Color.Lerp(kind == 1 ? Hex(0x5A4A7A) : Hex(0x151933), Hex(0x0B0E24), Mathf.Clamp01((r - 2f) / (HatchR - 2f)));
                p.Over(c, Cover(r - HatchR, aa));
            });
        }

        /// <summary>El aro de tinta que va encima de la máscara redonda de la escotilla (tapa el borde sin suavizar de la máscara).</summary>
        public static Sprite HatchRim()
        {
            const float ppd = 4f;
            float box = 2f * (FrameR + 2.5f);
            return Bake("hrim", box, box, ppd, (ref Px p, float x, float y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                p.Over(Ink, Cover(Mathf.Abs(r - (HatchR + 0.4f)) - 1.1f, Aa(ppd)));
            });
        }

        /// <summary>El aro de sello punteado de la esclusa (18 marcas doradas; gira con la bodega). Radio 37 dp. Sin rayitas radiales: parecían un sol.</summary>
        public static Sprite SealRing()
        {
            const float ppd = 4f;
            const float radius = BodegaLayout.HatchR + 10f;
            float box = 2f * (radius + 3f);
            return Bake("seal", box, box, ppd, (ref Px p, float x, float y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                float u = Mathf.Atan2(y, x) / (2f * Mathf.PI) * 18f;
                float frac = u - Mathf.Floor(u);                       // 0..1 dentro de la marca
                float on = frac < 7f / 13f ? 1f : 0f;
                p.Over(new Color(Gold.r, Gold.g, Gold.b, 0.75f), Cover(Mathf.Abs(r - radius) - 1.5f, Aa(ppd)) * on);
            });
        }

        /// <summary>La hoja de una puerta: media luna que se desliza hacia un lado (la otra es la misma girada 180°). Ocupa la mitad de arriba; el centro del sprite es el centro de la escotilla. Con
        /// borde tinta, un aro interior y un tirador cerca de la junta.</summary>
        public static Sprite DoorLeaf()
        {
            const float ppd = 4f;
            float box = 2f * (HatchR + 1.5f);
            return Bake("door", box, box, ppd, (ref Px p, float x, float y) =>
            {
                float aa = Aa(ppd), r = Mathf.Sqrt(x * x + y * y);
                float sdf = Mathf.Max(r - (HatchR - 1f), -y);
                float t = Mathf.Clamp01(((x - y) / (2f * HatchR)) + 0.5f);
                Outlined(ref p, sdf, 1.1f, Color.Lerp(Door0, Door1, t), Ink, aa);
                float ang = Mathf.Atan2(y, x);
                if (y > 0f && ang > 0.25f && ang < Mathf.PI - 0.25f)
                    p.Over(new Color(Ink.r, Ink.g, Ink.b, 0.3f), Cover(Mathf.Abs(r - (HatchR - 8f)) - 1f, aa) * Cover(sdf + 1.1f, aa));
                p.Over(Ink, Cover(Circle(x, y, 0f, 6f, 2.6f), aa));
            });
        }

        // ------------------------------------------------------------------ el robot, la caja, el ✓

        /// <summary>El cuerpo del robot: disco de arcilla con degradé (un foco claro arriba a la izquierda) y borde tinta.</summary>
        public static Sprite RobotBody()
        {
            const float ppd = 4f;
            float box = 2f * (RobotR + 3.5f);
            return Bake("robot", box, box, ppd, (ref Px p, float x, float y) =>
            {
                float aa = Aa(ppd), r = Mathf.Sqrt(x * x + y * y);
                float dx = x + 10f, dy = y - 12f;
                float t = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 3f) / 27f);
                p.Over(Ink, Cover(r - (RobotR + 1.5f), aa));
                p.Over(Color.Lerp(Hex(0xF4F1FF), Hex(0xA99FEA), t), Cover(r - (RobotR - 1.5f), aa));
            });
        }

        /// <summary>La caja cerrada que el robot cambia de lugar (madera clara con cinta): 30 × 24 dp.</summary>
        public static Sprite Crate()
        {
            const float ppd = 4f;
            return Bake("crate", 38f, 32f, ppd, (ref Px p, float x, float y) =>
            {
                float aa = Aa(ppd), sdf = RoundBox(x, y, 0f, 0f, 15f, 12f, 5f);
                Outlined(ref p, sdf, 1.3f, Hex(0xC9A26B), Ink, aa);
                var tape = new Color(Ink.r, Ink.g, Ink.b, 0.45f);
                p.Over(tape, Mathf.Max(Cover(Mathf.Abs(y - 2f) - 1f, aa), Cover(Mathf.Abs(x) - 1f, aa)) * Cover(sdf + 1.3f, aa));
            });
        }

        /// <summary>El ✓ (blanco: se tiñe de menta) de 12 × 10 dp.</summary>
        public static Sprite Check()
        {
            const float ppd = 6f;
            return Bake("check", 14f, 12f, ppd, (ref Px p, float x, float y) =>
            {
                float aa = Aa(ppd);
                float d = Mathf.Min(Capsule(x, y, -4.5f, -0.5f, -1.5f, -3.8f, 1.3f), Capsule(x, y, -1.5f, -3.8f, 4.8f, 3.6f, 1.3f));
                p.Over(Color.white, Cover(d, aa));
            });
        }

        // ------------------------------------------------------------------ los 12 objetos

        public static Sprite Object(int id)
        {
            id = Mathf.Clamp(id, 0, BodegaContract.ObjectCount - 1);
            string key = "obj" + id;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            const int size = 192;
            int b = FromBitacora[id];
            var pixels = b >= 0 ? BitacoraSprites.RenderFind(b, size) : RenderClay(size, 1.12f, 0.06f, 0.075f, 0.02f, (x, y) => NewBody(id, x, y), (ref Px p, float x, float y) => NewPaint(id, ref p, x, y));
            var sprite = ToSprite(pixels, size, size);
            Cache[key] = sprite;
            return sprite;
        }

        public static Color32[] RenderObject(int id, int size)
        {
            int b = FromBitacora[Mathf.Clamp(id, 0, BodegaContract.ObjectCount - 1)];
            return b >= 0 ? BitacoraSprites.RenderFind(b, size) : RenderClay(size, 1.12f, 0.06f, 0.075f, 0.02f, (x, y) => NewBody(id, x, y), (ref Px p, float x, float y) => NewPaint(id, ref p, x, y));
        }

        private static readonly Color AppleRed = Hex(0xE2524A), Wood = Hex(0x9C6A4E);

        private static float AppleBody(float x, float y)
        {
            float lobes = Mathf.Min(Circle(x, y, -0.24f, -0.06f, 0.5f), Circle(x, y, 0.24f, -0.06f, 0.5f));
            float body = Mathf.Min(lobes, Circle(x, y, 0f, -0.22f, 0.52f));
            body = Mathf.Max(body, -Circle(x, y, 0f, 0.62f, 0.17f));                      // el hoyito de arriba
            Rotate(x, y, 0.3f, 0.7f, 0.55f, out float lx, out float ly);
            float leaf = Ellipse(lx, ly, 0.3f, 0.7f, 0.2f, 0.09f);
            return Mathf.Min(Mathf.Min(body, Capsule(x, y, 0f, 0.42f, 0.04f, 0.82f, 0.06f)), leaf);
        }

        private static float CupBody(float x, float y)
        {
            float cup = RoundBox(x, y, -0.08f, 0f, 0.5f, 0.5f, 0.16f);
            float handle = Mathf.Max(Mathf.Abs(Circle(x, y, 0.5f, 0.02f, 0.27f)) - 0.09f, -(x - 0.38f));
            float saucer = RoundBox(x, y, -0.04f, -0.6f, 0.68f, 0.08f, 0.06f);
            return Mathf.Min(Mathf.Min(cup, handle), saucer);
        }

        private static float NewBody(int id, float x, float y) => id == 3 ? AppleBody(x, y) : CupBody(x, y);

        private static void Fill(ref Px p, Color c, float sdf) => p.Over(c, Cover(sdf, 0.02f));

        private static void Gloss(ref Px p, float x, float y, float cx, float cy, float rx, float ry, float inside) =>
            p.Over(new Color(1f, 1f, 1f, 0.45f), Cover(Ellipse(x, y, cx, cy, rx, ry), 0.02f) * Cover(inside, 0.02f));

        private static void NewPaint(int id, ref Px p, float x, float y)
        {
            float body = NewBody(id, x, y);
            if (id == 3)
            {
                float apple = Mathf.Max(Mathf.Min(Mathf.Min(Circle(x, y, -0.24f, -0.06f, 0.5f), Circle(x, y, 0.24f, -0.06f, 0.5f)), Circle(x, y, 0f, -0.22f, 0.52f)), -Circle(x, y, 0f, 0.62f, 0.17f));
                Fill(ref p, AppleRed, apple);
                Fill(ref p, Shade(AppleRed, 0.8f), Mathf.Max(apple, -(x - 0.22f)));
                Gloss(ref p, x, y, -0.3f, 0.18f, 0.12f, 0.2f, apple);
                Fill(ref p, Wood, Capsule(x, y, 0f, 0.42f, 0.04f, 0.82f, 0.06f));
                Rotate(x, y, 0.3f, 0.7f, 0.55f, out float lx, out float ly);
                Fill(ref p, Mint, Ellipse(lx, ly, 0.3f, 0.7f, 0.2f, 0.09f));
            }
            else
            {
                float cup = RoundBox(x, y, -0.08f, 0f, 0.5f, 0.5f, 0.16f);
                Fill(ref p, Sky, body);
                Fill(ref p, Shade(Sky, 0.8f), Mathf.Max(cup, -(x - 0.22f)));
                Fill(ref p, Cream, Mathf.Max(cup, -(y - 0.34f)));                           // el borde de arriba
                Fill(ref p, Shade(Wood, 1.05f), Mathf.Max(Mathf.Abs(y - 0.36f) - 0.03f, Mathf.Abs(x + 0.08f) - 0.4f));
                Fill(ref p, Grape, RoundBox(x, y, -0.04f, -0.6f, 0.68f, 0.08f, 0.06f));
                Gloss(ref p, x, y, -0.4f, 0.0f, 0.07f, 0.2f, cup);
            }
        }

        // ------------------------------------------------------------------ calentamiento

        /// <summary>Hornea todo, de a uno por cuadro (durante la cuenta regresiva).</summary>
        public static IEnumerator Prewarm()
        {
            Hull();
            yield return null;
            Airlock();
            SealRing();
            HatchFrame();
            yield return null;
            HatchInterior(0); HatchInterior(1); HatchInterior(2); HatchRim(); DoorLeaf();
            yield return null;
            RobotBody(); Crate(); Check();
            yield return null;
            for (int i = 0; i < BodegaContract.ObjectCount; i++)
            {
                Object(i);
                if (i % 3 == 2) yield return null;
            }
        }

        public static int CachedCount => Cache.Count;
    }
}
