using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Disparate
{
    /// <summary>
    /// Arte de "¿Verdad o disparate?" con el pincel de arcilla común (<see cref="ClayRaster"/>), colores horneados:
    /// <list type="bullet">
    /// <item><see cref="Antenna"/>: la antena parabólica de la sala de radio — plataforma y base con volumen, mástil lila, plato oblicuo con su interior celeste, brazo y receptor, y la luz de la punta aparte; sombra dura hacia abajo. <see cref="AntennaTip"/> dice dónde
    /// queda la luz (para emitir las ondas desde ahí).</item>
    /// <item><see cref="CheckIcon"/> / <see cref="BrokenWaveIcon"/>: los íconos de los botones VERDAD (✓) y DISPARATE (onda rota).</item>
    /// <item><see cref="Band"/>: la cinta de luz de la transmisión. <see cref="NoiseTexture"/>: la estática (se repite).</item>
    /// </list>
    /// <para><b>Plasticina (Tarea 97):</b> si el atlas <c>Resources/Plasticina/disparate</c> existe, la antena, los dos íconos y las barras del medidor (<see cref="MeterBar"/>) salen de ahí (renders de Blender, <c>tools/juegos/disparate.py</c>, el mismo lienzo y la misma escala:
    /// <see cref="AntennaFloor"/> vale igual; <see cref="AntennaTip"/> y <see cref="AntennaDish"/> se movieron con la antena parabólica del 14-oct: el plato inclinado, el brazo del receptor y la luz aparte, para que no se lea como una copa). Sin el atlas, el arte procedural de abajo (también una parabólica, con las mismas anclas). La cinta de luz y la estática son luz y efecto: se quedan.</para>
    /// </summary>
    public static class DisparateSprites
    {
        private const int AntennaPx = 288;
        private const float AntennaZoom = 1.2f;
        private const float Aa = 0.02f;
        private static Sprite _antenna, _check, _wave, _band;
        private static Texture2D _noise;

        public const string PlasticGroup = "disparate";

        /// <summary>true si el atlas de plasticina de «¿Verdad o disparate?» existe (y no está desactivado).</summary>
        public static bool Plastic => PlasticArt.Has(PlasticGroup, "antena");

        public static Sprite Antenna() => PlasticArt.Get(PlasticGroup, "antena") ?? (_antenna != null ? _antenna : _antenna = ToSprite(RenderAntenna(AntennaPx), AntennaPx, AntennaPx));
        public static Sprite CheckIcon() => PlasticArt.Get(PlasticGroup, "icono_visto") ?? (_check != null ? _check : _check = ToSprite(RenderFlat(96, CheckShape, Ink), 96, 96));
        public static Sprite BrokenWaveIcon() => PlasticArt.Get(PlasticGroup, "icono_ola") ?? (_wave != null ? _wave : _wave = ToSprite(RenderFlat(96, BrokenWaveShape, Ink), 96, 96));

        /// <summary>
        /// La barra del medidor de señal (encendida o apagada) como nine-slice VERTICAL (los extremos redondos no se estiran), o null sin plasticina (el que llama usa su rectángulo redondeado de siempre).
        /// La imagen sobresale <see cref="MeterMargin"/> por cada lado (donde cae la sombra): la barra mide de ancho 34 u y la imagen, 34 + 2 × margen.
        /// </summary>
        public static Sprite MeterBar(bool lit) => PlasticArt.Get(PlasticGroup, lit ? "medidor_encendida" : "medidor_apagada");

        /// <summary>Lo que sobresale la barra de plasticina del medidor alrededor de su cuerpo, en unidades del lienzo (0 sin plasticina).</summary>
        public static float MeterMargin => PlasticArt.Measure(PlasticGroup, "medidor_margen_u", 0f);

        /// <summary>
        /// Dónde queda la luz de la punta de la antena, como fracción (0-1) del ancho y del alto del dibujo (desde abajo). La antena es una parabólica con el plato inclinado a la derecha (14-oct): la luz queda al final del brazo, a la derecha del centro. Valen igual con
        /// plasticina (medido sobre el render por <c>tools/juegos/mide_disparate.py</c>) y con el dibujo de respaldo, que se rehízo con las mismas anclas.
        /// </summary>
        public static Vector2 AntennaTip => new Vector2(0.678f, 0.902f);
        /// <summary>Dónde queda el plato (de donde salen las ondas): el centro de su boca.</summary>
        public static Vector2 AntennaDish => new Vector2(0.603f, 0.648f);
        /// <summary>Fracción del alto del dibujo donde termina la plataforma (el "suelo" de la antena).</summary>
        public static float AntennaFloor => ((-0.99f) / AntennaZoom + 1f) * 0.5f;

        // ------------------------------------------------------------------ antena

        private static float Platform(float x, float y) => RoundBox(x, y, 0f, -0.86f, 0.92f, 0.13f, 0.08f);
        private static float Base(float x, float y) => RoundBox(x, y, 0f, -0.62f, 0.40f, 0.15f, 0.07f);
        // la antena PARABÓLICA (corrección del 14-oct, también el dibujo de respaldo): un plato oblicuo (su boca es una elipse inclinada con el interior celeste), un mástil lila con su base, el brazo del receptor que sale del centro del plato,
        // el receptor de tinta suave y la luz de la punta aparte. Las anclas son las mismas que las de plasticina (AntennaTip y AntennaDish).
        private const float DishCx = 0.247f, DishCy = 0.355f, DishRx = 0.62f, DishRy = 0.36f, DishAngle = -0.62f;        // el lado largo de la boca baja hacia la derecha (~35°)
        private const float ArmBx = 0.4272f, ArmBy = 0.9648f;                                                              // donde queda la luz (AntennaTip)
        private static float DishU(float x, float y) => (x - DishCx) * Mathf.Cos(DishAngle) + (y - DishCy) * Mathf.Sin(DishAngle);
        private static float DishV(float x, float y) => -(x - DishCx) * Mathf.Sin(DishAngle) + (y - DishCy) * Mathf.Cos(DishAngle);
        private static float Mast(float x, float y) => Capsule(x, y, 0f, -0.54f, 0.0f, 0.2f, 0.066f);
        private static float Dish(float x, float y) => Ellipse(DishU(x, y), DishV(x, y), 0f, 0f, DishRx, DishRy);
        private static float DishInside(float x, float y) => Ellipse(DishU(x, y), DishV(x, y), 0f, 0f, DishRx * 0.88f, DishRy * 0.84f);
        private static float Arm(float x, float y) => Capsule(x, y, DishCx - 0.12f, DishCy - 0.1f, ArmBx - 0.07f, ArmBy - 0.12f, 0.04f);
        private static float Receiver(float x, float y) => Capsule(x, y, ArmBx - 0.075f, ArmBy - 0.135f, ArmBx - 0.04f, ArmBy - 0.07f, 0.075f);
        private static float Tip(float x, float y) => Circle(x, y, ArmBx, ArmBy, 0.105f);

        private static float AntennaBody(float x, float y) =>
            Union(Union(Union(Platform(x, y), Base(x, y)), Union(Mast(x, y), Dish(x, y))), Union(Arm(x, y), Union(Receiver(x, y), Tip(x, y))));

        public static Color32[] RenderAntenna(int size) => RenderClay(size, AntennaZoom, 0.05f, 0.06f, Aa, AntennaBody, (ref Px p, float x, float y) =>
        {
            var steel = Hex(0x39408F);
            var lilac = Hex(0x8E7CE0);
            // plataforma y base con relieve (claro arriba a la izquierda)
            float plat = Platform(x, y);
            p.Over(Shade(steel, Mathf.Clamp(1.05f + (-x * 0.1f + y * 0.9f) * 0.2f, 0.8f, 1.2f)), Cover(plat, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.22f), Cover(Capsule(x, y, -0.8f, -0.8f, 0.8f, -0.8f, 0.012f), Aa) * Cover(plat + 0.03f, Aa));
            float bs = Base(x, y);
            p.Over(Shade(Grape, Mathf.Clamp(1f + (-x * 0.35f + y * 0.5f) * 0.25f, 0.82f, 1.15f)), Cover(bs, Aa));
            for (int i = -1; i <= 1; i += 2) p.Over(WithAlpha(Ink, 0.45f), Cover(Circle(x, y, i * 0.28f, -0.62f, 0.028f), Aa) * Cover(bs + 0.02f, Aa));
            // mástil lila
            float mast = Mast(x, y);
            p.Over(Shade(lilac, Mathf.Clamp(1f + (-x * 5f) * 0.1f, 0.85f, 1.1f)), Cover(mast, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.35f), Cover(Capsule(x, y, -0.03f, -0.5f, -0.03f, 0.15f, 0.012f), Aa) * Cover(mast + 0.02f, Aa));
            // plato: la cara de afuera crema, un labio, y el interior celeste con un brillo
            float dish = Dish(x, y);
            p.Over(Shade(Cream, Mathf.Clamp(1f + (-DishU(x, y) * 0.2f + DishV(x, y) * 0.3f), 0.82f, 1.08f)), Cover(dish, Aa));
            p.Over(Sky, Cover(DishInside(x, y), Aa));
            p.Over(WithAlpha(Shade(Sky, 0.7f), 0.35f), Cover(DishInside(x, y), Aa) * Mathf.Clamp01((DishU(x, y) / DishRx + 0.2f) * 0.8f));
            p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Ellipse(DishU(x, y), DishV(x, y), -0.2f, 0.1f, 0.17f, 0.04f), Aa) * Cover(DishInside(x, y), Aa));
            // brazo del receptor (lila), receptor (tinta suave) y la luz de la punta
            p.Over(Shade(lilac, 0.95f), Cover(Arm(x, y), Aa));
            p.Over(Hex(0x4A3F8C), Cover(Receiver(x, y), Aa));
            float tip = Tip(x, y);
            p.Over(Sun, Cover(tip, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.8f), Cover(Circle(x, y, ArmBx - 0.04f, ArmBy + 0.035f, 0.03f), Aa) * Cover(tip + 0.01f, Aa));
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
