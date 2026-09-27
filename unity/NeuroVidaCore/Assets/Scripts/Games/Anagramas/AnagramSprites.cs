using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Anagramas
{
    /// <summary>
    /// Arte propio de Anagramas: el hueco hundido donde encaja cada letra (arcilla con sombra interior arriba y
    /// canto de luz abajo: se entiende que ahí "cae" una ficha) y los íconos de los botones Borrar / Pista /
    /// Pasar (ícono + texto, nunca solo ícono). Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class AnagramSprites
    {
        public enum Icon { Backspace, Hint, Skip }

        private const int SlotPx = 128;
        private const int IconPx = 128;
        private const float Aa = 0.035f;
        private static Sprite _slot;
        private static readonly Sprite[] Icons = new Sprite[3];

        public static Sprite Slot() => _slot != null ? _slot : (_slot = ToSprite(RenderSlot(SlotPx), SlotPx, SlotPx));

        public static Sprite ActionIcon(Icon icon)
        {
            int i = (int)icon;
            if (Icons[i] != null) return Icons[i];
            return Icons[i] = ToSprite(RenderIcon(icon, IconPx), IconPx, IconPx);
        }

        /// <summary>Hueco hundido (fila 0 = abajo).</summary>
        public static Color32[] RenderSlot(int size)
        {
            var pixels = new Color32[size * size];
            var baseColor = Hex(0x2A1F5C);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size * 2f - 1f, y = (py + 0.5f) / size * 2f - 1f;
                    float box = RoundBox(x, y, 0f, 0f, 0.86f, 0.86f, 0.26f);
                    var p = new Px();
                    p.Over(WithAlpha(Ink, 0.9f), Cover(box, Aa));
                    p.Over(WithAlpha(baseColor, 0.9f), Cover(box + 0.06f, Aa));
                    // Sombra interior arriba (el hueco es hondo) y canto de luz abajo.
                    p.Over(WithAlpha(Ink, 0.75f), Cover(Mathf.Max(box + 0.06f, -RoundBox(x, y, 0f, -0.14f, 0.86f, 0.86f, 0.26f)), Aa));
                    p.Over(WithAlpha(Grape, 0.55f), Cover(Mathf.Max(box + 0.06f, -RoundBox(x, y, 0f, 0.06f, 0.80f, 0.80f, 0.24f)), Aa));
                    pixels[py * size + px] = p.ToColor32();
                }
            }
            return pixels;
        }

        private static readonly float[] BackTag = { -0.86f, 0f, -0.36f, 0.52f, 0.80f, 0.52f, 0.80f, -0.52f, -0.36f, -0.52f };
        private static readonly float[] SkipA = { -0.72f, 0.56f, 0.02f, 0f, -0.72f, -0.56f };
        private static readonly float[] SkipB = { -0.12f, 0.56f, 0.62f, 0f, -0.12f, -0.56f };

        private static float IconBody(Icon icon, float x, float y)
        {
            switch (icon)
            {
                case Icon.Backspace: return Polygon(x, y, BackTag) - 0.08f;
                case Icon.Hint: return Mathf.Min(Circle(x, y, 0f, 0.20f, 0.52f), RoundBox(x, y, 0f, -0.52f, 0.26f, 0.24f, 0.08f));
                default:
                    return Mathf.Min(Mathf.Min(Polygon(x, y, SkipA) - 0.06f, Polygon(x, y, SkipB) - 0.06f), RoundBox(x, y, 0.72f, 0f, 0.09f, 0.58f, 0.06f));
            }
        }

        /// <summary>Ícono de acción (fila 0 = abajo).</summary>
        public static Color32[] RenderIcon(Icon icon, int size) =>
            RenderClay(size, 1.14f, 0.1f, 0.08f, Aa, (x, y) => IconBody(icon, x, y), (ref Px p, float x, float y) =>
            {
                float body = IconBody(icon, x, y);
                if (icon == Icon.Hint)
                {
                    // Ampolleta sol con base crema y dos rayitas de brillo adentro.
                    float bulb = Circle(x, y, 0f, 0.20f, 0.52f);
                    p.Over(Cream, Cover(RoundBox(x, y, 0f, -0.52f, 0.26f, 0.24f, 0.08f), Aa));
                    p.Over(Ink, Cover(Mathf.Abs(y + 0.50f) - 0.03f, Aa) * Cover(RoundBox(x, y, 0f, -0.52f, 0.26f, 0.24f, 0.08f), Aa));
                    p.Over(Ink, Cover(bulb - 0.1f, Aa));
                    p.Over(Sun, Cover(bulb, Aa));
                    p.Over(new Color(1f, 1f, 1f, 0.7f), Cover(Ellipse(x, y, -0.2f, 0.42f, 0.12f, 0.08f), Aa));
                    p.Over(Shade(Sun, 0.8f), Cover(Capsule(x, y, -0.12f, -0.06f, 0f, 0.14f, 0.035f), Aa));
                    p.Over(Shade(Sun, 0.8f), Cover(Capsule(x, y, 0.12f, -0.06f, 0f, 0.14f, 0.035f), Aa));
                    return;
                }
                p.Over(Cream, Cover(body, Aa));
                if (icon == Icon.Backspace)
                {
                    float cross = Mathf.Min(Capsule(x, y, 0.02f, 0.2f, 0.42f, -0.2f, 0.06f), Capsule(x, y, 0.02f, -0.2f, 0.42f, 0.2f, 0.06f));
                    p.Over(Ink, Cover(cross, Aa));
                }
                p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Ellipse(x, y, -0.1f, 0.34f, 0.2f, 0.06f), Aa) * Cover(body + 0.04f, Aa));
            });
    }
}
