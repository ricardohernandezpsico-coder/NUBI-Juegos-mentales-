using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Marca de respuesta en arcilla: ✓ (disco lima con visto tinta) o ✗ (disco coral con cruz tinta). Acierto y error
    /// se distinguen por FORMA además de por color. Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class AnswerMarkSprite
    {
        private const int SizePx = 128;
        private const float Zoom = 1.1f;
        private const float Aa = 0.035f;
        private const float Line = 0.1f;
        private const float Drop = 0.1f;
        private static Sprite _check, _cross;

        public static Sprite Check() => _check != null ? _check : _check = ToSprite(Render(true, SizePx), SizePx, SizePx);
        public static Sprite Cross() => _cross != null ? _cross : _cross = ToSprite(Render(false, SizePx), SizePx, SizePx);

        private static float Disc(float x, float y) => Circle(x, y, 0f, 0f, 0.8f);

        /// <summary>Píxeles de la marca (fila 0 = abajo). Separado para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(bool check, int size) => RenderClay(size, Zoom, Line, Drop, Aa, Disc, (ref Px p, float x, float y) =>
        {
            float disc = Disc(x, y);
            p.Over(check ? Lime : Coral, Cover(disc, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.4f), Cover(Ellipse(x, y, -0.3f, 0.46f, 0.2f, 0.08f), Aa) * Cover(disc + 0.03f, Aa));
            float glyph = check
                ? Mathf.Min(Capsule(x, y, -0.36f, 0.02f, -0.1f, -0.26f, 0.12f), Capsule(x, y, -0.1f, -0.26f, 0.38f, 0.30f, 0.12f))
                : Mathf.Min(Capsule(x, y, -0.3f, -0.3f, 0.3f, 0.3f, 0.12f), Capsule(x, y, -0.3f, 0.3f, 0.3f, -0.3f, 0.12f));
            p.Over(Ink, Cover(glyph, Aa));
        });
    }
}
