using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Disparate
{
    /// <summary>
    /// Arte de "¿Verdad o disparate?" con el pincel de arcilla común (<see cref="ClayRaster"/>), colores horneados:
    /// <list type="bullet">
    /// <item><see cref="Antenna"/>: la antena de la sala de radio — plataforma y base con volumen, mástil crema con remaches y
    /// patas, plato con borde celeste, brazo y luz en la punta; sombra dura hacia abajo. <see cref="AntennaTip"/> dice dónde
    /// queda la luz (para emitir las ondas desde ahí).</item>
    /// <item><see cref="CheckIcon"/> / <see cref="BrokenWaveIcon"/>: los íconos de los botones VERDAD (✓) y DISPARATE (onda rota).</item>
    /// <item><see cref="Band"/>: la cinta de luz de la transmisión. <see cref="NoiseTexture"/>: la estática (se repite).</item>
    /// </list>
    /// </summary>
    public static class DisparateSprites
    {
        private const int AntennaPx = 288;
        private const float AntennaZoom = 1.2f;
        private const float Aa = 0.02f;
        private static Sprite _antenna, _check, _wave, _band;
        private static Texture2D _noise;

        public static Sprite Antenna() => _antenna != null ? _antenna : _antenna = ToSprite(RenderAntenna(AntennaPx), AntennaPx, AntennaPx);
        public static Sprite CheckIcon() => _check != null ? _check : _check = ToSprite(RenderFlat(96, CheckShape, Ink), 96, 96);
        public static Sprite BrokenWaveIcon() => _wave != null ? _wave : _wave = ToSprite(RenderFlat(96, BrokenWaveShape, Ink), 96, 96);

        /// <summary>Dónde queda la luz de la punta de la antena, como fracción (0-1) del ancho y del alto del dibujo (desde abajo).</summary>
        public static Vector2 AntennaTip => new Vector2(0.5f, (1.02f / AntennaZoom + 1f) * 0.5f);
        /// <summary>Dónde queda el plato (de donde salen las ondas).</summary>
        public static Vector2 AntennaDish => new Vector2(0.5f, (0.78f / AntennaZoom + 1f) * 0.5f);
        /// <summary>Fracción del alto del dibujo donde termina la plataforma (el "suelo" de la antena).</summary>
        public static float AntennaFloor => ((-0.99f) / AntennaZoom + 1f) * 0.5f;

        // ------------------------------------------------------------------ antena

        private static float Platform(float x, float y) => RoundBox(x, y, 0f, -0.86f, 0.92f, 0.13f, 0.08f);
        private static float Base(float x, float y) => RoundBox(x, y, 0f, -0.62f, 0.40f, 0.15f, 0.07f);
        private static float LegL(float x, float y) => Capsule(x, y, -0.3f, -0.52f, -0.05f, -0.02f, 0.05f);
        private static float LegR(float x, float y) => Capsule(x, y, 0.3f, -0.52f, 0.05f, -0.02f, 0.05f);
        private static float Mast(float x, float y) => RoundBox(x, y, 0f, 0.12f, 0.075f, 0.5f, 0.04f);
        private static float Bowl(float x, float y) => Intersect(Circle(x, y, 0f, 0.74f, 0.52f), y - 0.74f);
        private static float Rim(float x, float y) => Ellipse(x, y, 0f, 0.74f, 0.52f, 0.1f);
        private static float Arm(float x, float y) => Capsule(x, y, 0f, 0.74f, 0f, 1.0f, 0.035f);
        private static float Tip(float x, float y) => Circle(x, y, 0f, 1.02f, 0.085f);

        private static float AntennaBody(float x, float y) =>
            Union(Union(Union(Platform(x, y), Base(x, y)), Union(LegL(x, y), LegR(x, y))),
                  Union(Union(Mast(x, y), Union(Bowl(x, y), Rim(x, y))), Union(Arm(x, y), Tip(x, y))));

        public static Color32[] RenderAntenna(int size) => RenderClay(size, AntennaZoom, 0.05f, 0.06f, Aa, AntennaBody, (ref Px p, float x, float y) =>
        {
            var steel = Hex(0x39408F);
            // plataforma y base con relieve (claro arriba a la izquierda)
            float plat = Platform(x, y);
            p.Over(Shade(steel, Mathf.Clamp(1.05f + (-x * 0.1f + y * 0.9f) * 0.2f, 0.8f, 1.2f)), Cover(plat, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.22f), Cover(Capsule(x, y, -0.8f, -0.8f, 0.8f, -0.8f, 0.012f), Aa) * Cover(plat + 0.03f, Aa));
            float bs = Base(x, y);
            p.Over(Shade(Grape, Mathf.Clamp(1f + (-x * 0.35f + y * 0.5f) * 0.25f, 0.82f, 1.15f)), Cover(bs, Aa));
            for (int i = -1; i <= 1; i += 2) p.Over(WithAlpha(Ink, 0.45f), Cover(Circle(x, y, i * 0.28f, -0.62f, 0.028f), Aa) * Cover(bs + 0.02f, Aa));
            // patas y mástil crema
            float legs = Union(LegL(x, y), LegR(x, y));
            p.Over(Shade(Cream, 0.9f), Cover(legs, Aa));
            float mast = Mast(x, y);
            p.Over(Shade(Cream, Mathf.Clamp(1f + (-x * 5f) * 0.12f, 0.82f, 1.08f)), Cover(mast, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Capsule(x, y, -0.035f, -0.3f, -0.035f, 0.55f, 0.013f), Aa) * Cover(mast + 0.02f, Aa));
            for (int i = 0; i < 5; i++) p.Over(WithAlpha(Ink, 0.4f), Cover(Circle(x, y, 0f, -0.3f + 0.18f * i, 0.022f), Aa) * Cover(mast + 0.02f, Aa));
            // plato: cuenco crema con relieve y borde celeste por dentro
            float bowl = Bowl(x, y);
            p.Over(Shade(Cream, Mathf.Clamp(1f + (-x * 0.5f + (y - 0.74f) * 0.9f) * 0.22f, 0.78f, 1.12f)), Cover(bowl, Aa));
            p.Over(WithAlpha(Shade(Grape, 0.9f), 0.4f), Cover(bowl, Aa) * Mathf.Clamp01(x * 0.9f));
            float rim = Rim(x, y);
            p.Over(Cream, Cover(rim, Aa));
            p.Over(Sky, Cover(Ellipse(x, y, 0f, 0.745f, 0.45f, 0.065f), Aa));
            p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(x, y, -0.15f, 0.76f, 0.16f, 0.02f), Aa));
            p.Over(WithAlpha(Ink, 0.55f), Cover(Mathf.Abs(rim) - 0.012f, Aa) * 0.7f);
            // brazo y luz de la punta
            p.Over(Shade(Cream, 0.92f), Cover(Arm(x, y), Aa));
            float tip = Tip(x, y);
            p.Over(Sun, Cover(tip, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.8f), Cover(Circle(x, y, -0.025f, 1.045f, 0.028f), Aa) * Cover(tip + 0.01f, Aa));
        });

        // ------------------------------------------------------------------ íconos planos (tinta sobre transparente)

        private static float CheckShape(float x, float y) =>
            Union(Capsule(x, y, -0.55f, 0.0f, -0.18f, -0.4f, 0.17f), Capsule(x, y, -0.18f, -0.4f, 0.6f, 0.46f, 0.17f));

        private static float BrokenWaveShape(float x, float y)
        {
            float l = Union(Union(Capsule(x, y, -0.85f, 0.0f, -0.66f, 0.38f, 0.11f), Capsule(x, y, -0.66f, 0.38f, -0.46f, -0.38f, 0.11f)),
                            Capsule(x, y, -0.46f, -0.38f, -0.24f, 0.2f, 0.11f));
            float r = Union(Union(Capsule(x, y, 0.2f, -0.2f, 0.4f, 0.38f, 0.11f), Capsule(x, y, 0.4f, 0.38f, 0.6f, -0.38f, 0.11f)),
                            Capsule(x, y, 0.6f, -0.38f, 0.85f, 0.05f, 0.11f));
            return Union(l, r);
        }

        public static Color32[] RenderFlat(int size, ShapeFn shape, Color color)
        {
            var px = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int pxl = 0; pxl < size; pxl++)
                {
                    float x = (pxl + 0.5f) / size * 2f - 1f;
                    float y = (py + 0.5f) / size * 2f - 1f;
                    float a = Cover(shape(x, y), 0.04f);
                    px[py * size + pxl] = new Color32((byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), (byte)(a * 255f));
                }
            return px;
        }

        // ------------------------------------------------------------------ cinta de luz y estática

        /// <summary>Cinta de luz horizontal: blanca, suave arriba y abajo y desvanecida en las puntas (se tiñe con Image.color).</summary>
        public static Sprite Band()
        {
            if (_band != null) return _band;
            const int w = 256, h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                    float u = (x + 0.5f) / w;
                    float core = Mathf.Pow(1f - v, 1.6f);
                    float ends = Mathf.Clamp01(Mathf.Min(u, 1f - u) / 0.18f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(255f * core * ends));
                }
            tex.SetPixels32(px);
            tex.Apply();
            _band = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _band;
        }

        /// <summary>Estática: puntos blancos sueltos de opacidad desigual, para una RawImage con <c>uvRect</c> que se mueve al azar.</summary>
        public static Texture2D NoiseTexture()
        {
            if (_noise != null) return _noise;
            const int n = 128;
            _noise = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var rng = new System.Random(77);
            var px = new Color32[n * n];
            for (int i = 0; i < px.Length; i++)
            {
                float r = (float)rng.NextDouble();
                byte a = (byte)(r > 0.55f ? 255f * (r - 0.55f) / 0.45f : 0f);
                px[i] = new Color32(255, 255, 255, a);
            }
            _noise.SetPixels32(px);
            _noise.Apply();
            return _noise;
        }
    }
}
