using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Stroop
{
    /// <summary>
    /// Arte propio de «Tinta o Palabra» (modelo «Dos orillas»): la GOTA de la orilla de la TINTA y de cada botón, el LIBRO de la orilla de la PALABRA y el HAZ DE LUZ de
    /// cada orilla (una compuerta de luz, estética espacial: nada de papel). Todos van en blanco para teñirlos con <c>Image.color</c> (el borde tinta oscuro se
    /// mantiene oscuro al teñir). Se hornean una sola vez.
    /// </summary>
    public static class StroopSprites
    {
        private const int SizePx = 128;
        private const float Zoom = 1.1f;
        private const float Aa = 0.03f;
        private const float Line = 0.085f;
        private static Sprite _drop, _book, _beam;

        /// <summary>Gota de tinta blanca (para teñir) con borde tinta y un brillo.</summary>
        public static Sprite Drop() => _drop != null ? _drop : (_drop = ToSprite(RenderDrop(SizePx), SizePx, SizePx));

        /// <summary>Libro abierto blanco (para teñir) con borde tinta, lomo y renglones.</summary>
        public static Sprite Book() => _book != null ? _book : (_book = ToSprite(RenderBook(SizePx), SizePx, SizePx));

        /// <summary>Haz de luz horizontal: opaco en el borde y se apaga hacia adentro (la orilla izquierda lo usa tal cual; la derecha, volteado).</summary>
        public static Sprite Beam() => _beam != null ? _beam : (_beam = BuildBeam());

        public static Color32[] RenderDrop(int size) =>
            RenderClay(size, Zoom, Line, 0.07f, Aa, (x, y) => UnevenCapsule(x, y + 0.2f, 0.42f, 0.05f, 0.78f), (ref Px p, float x, float y) =>
            {
                float drop = UnevenCapsule(x, y + 0.2f, 0.42f, 0.05f, 0.78f);
                p.Over(Color.white, Cover(drop, Aa));
                p.Over(new Color(1f, 1f, 1f, 0.9f), Cover(Ellipse(x, y, -0.18f, -0.14f, 0.07f, 0.16f), Aa) * Cover(drop + 0.04f, Aa));
                p.Over(new Color(0f, 0f, 0f, 0.12f), Cover(Ellipse(x, y, 0.14f, -0.40f, 0.20f, 0.10f), Aa) * Cover(drop + 0.04f, Aa));
            });

        private static float BookBody(float x, float y) =>
            Mathf.Min(RoundBox(x, y, -0.42f, 0f, 0.44f, 0.62f, 0.07f), RoundBox(x, y, 0.42f, 0f, 0.44f, 0.62f, 0.07f));

        public static Color32[] RenderBook(int size) =>
            RenderClay(size, Zoom, Line, 0.07f, Aa, BookBody, (ref Px p, float x, float y) =>
            {
                float body = BookBody(x, y);
                p.Over(Color.white, Cover(body, Aa));
                // lomo al centro y renglones de texto en las dos páginas
                p.Over(Ink, Cover(Capsule(x, y, 0f, -0.58f, 0f, 0.58f, 0.04f), Aa));
                for (int k = 0; k < 3; k++)
                {
                    float yy = 0.30f - k * 0.26f;
                    p.Over(new Color(0.10f, 0.07f, 0.25f, 0.85f), Cover(Capsule(x, y, -0.78f, yy, -0.16f, yy, 0.035f), Aa) * Cover(body + 0.02f, Aa));
                    p.Over(new Color(0.10f, 0.07f, 0.25f, 0.85f), Cover(Capsule(x, y, 0.16f, yy, 0.78f, yy, 0.035f), Aa) * Cover(body + 0.02f, Aa));
                }
            });

        private static Sprite BuildBeam()
        {
            const int w = 128, h = 4;
            var px = new Color32[w * h];
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                // núcleo brillante pegado al borde y una cola larga que se apaga
                float a = Mathf.Clamp01(Mathf.Pow(1f - t, 1.6f) * 0.85f + (t < 0.06f ? (0.06f - t) / 0.06f * 0.35f : 0f));
                var c = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                for (int y = 0; y < h; y++) px[y * w + x] = c;
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 100f);
        }
    }
}
