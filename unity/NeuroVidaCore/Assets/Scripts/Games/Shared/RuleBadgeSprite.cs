using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Insignias de arcilla de las reglas que cambian (Tinta o Palabra y Cambio de Chip): disco del color de la
    /// regla con su dibujo propio en crema y borde tinta. La regla se reconoce por FORMA además de por color y
    /// texto (nunca solo color). Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class RuleBadgeSprite
    {
        public enum Kind { Ink, Word, Direction, Position }

        public static readonly Color InkColor = Hex(0x60A5FA);
        public static readonly Color WordColor = Hex(0x34D399);
        public static readonly Color DirectionColor = Hex(0x38BDF8);
        public static readonly Color PositionColor = Hex(0xF472B6);

        private const int SizePx = 160;
        private const float Zoom = 1.08f;
        private const float Aa = 0.03f;
        private const float Line = 0.08f;
        private const float Thin = 0.055f;
        private const float Drop = 0.08f;
        private static readonly Color InkBlue = Hex(0x2E3B8F);
        private static readonly Sprite[] Cache = new Sprite[4];

        public static Sprite Get(Kind kind)
        {
            int i = (int)kind;
            if (Cache[i] != null) return Cache[i];
            return Cache[i] = ToSprite(Render(kind, SizePx), SizePx, SizePx);
        }

        private static float Disc(float x, float y) => Circle(x, y, 0f, 0f, 0.84f);

        /// <summary>Píxeles de la insignia (fila 0 = abajo). Separado de <see cref="Get"/> para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(Kind kind, int size) => RenderClay(size, Zoom, Line, Drop, Aa, Disc, (ref Px p, float x, float y) =>
        {
            Color bg = kind == Kind.Ink ? InkColor : kind == Kind.Word ? WordColor : kind == Kind.Direction ? DirectionColor : PositionColor;
            float disc = Disc(x, y);
            p.Over(bg, Cover(disc, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.4f), Cover(Ellipse(x, y, -0.34f, 0.5f, 0.22f, 0.09f), Aa) * Cover(disc + 0.03f, Aa));
            switch (kind)
            {
                case Kind.Ink: InkDrop(ref p, x, y); break;
                case Kind.Word: Speech(ref p, x, y); break;
                case Kind.Direction: Arrow(ref p, x, y); break;
                default: Position(ref p, x, y); break;
            }
        });

        private static void Part(ref Px p, float sdf, Color fill, float line = Thin)
        {
            p.Over(Ink, Cover(sdf - line, Aa));
            p.Over(fill, Cover(sdf, Aa));
        }

        private static void InkDrop(ref Px p, float x, float y)
        {
            // Gota de tinta azul noche con brillo y dos salpicaduras.
            float drop = UnevenCapsule(x + 0.04f, y + 0.26f, 0.34f, 0.05f, 0.66f);
            Part(ref p, drop, InkBlue);
            p.Over(new Color(1f, 1f, 1f, 0.7f), Cover(Ellipse(x, y, -0.14f, -0.20f, 0.06f, 0.12f), Aa));
            Part(ref p, Circle(x, y, 0.40f, -0.40f, 0.08f), InkBlue);
            Part(ref p, Circle(x, y, 0.48f, -0.16f, 0.05f), InkBlue);
        }

        private static readonly float[] SpeechTail = { -0.26f, -0.22f, -0.40f, -0.50f, -0.04f, -0.26f };

        private static void Speech(ref Px p, float x, float y)
        {
            // Globo de texto crema con dos renglones: "lo que DICE la palabra".
            float bubble = Mathf.Min(RoundBox(x, y, 0f, 0.06f, 0.50f, 0.34f, 0.16f), Polygon(x, y, SpeechTail));
            Part(ref p, bubble, Cream);
            p.Over(Ink, Cover(Capsule(x, y, -0.28f, 0.16f, 0.28f, 0.16f, 0.055f), Aa));
            p.Over(Ink, Cover(Capsule(x, y, -0.28f, -0.04f, 0.10f, -0.04f, 0.055f), Aa));
        }

        private static readonly float[] ArrowPoly = { 0f, 0.58f, 0.40f, 0.10f, 0.15f, 0.10f, 0.15f, -0.52f, -0.15f, -0.52f, -0.15f, 0.10f, -0.40f, 0.10f };

        private static void Arrow(ref Px p, float x, float y)
        {
            // Flecha crema inclinada: "hacia dónde APUNTA".
            Rotate(x, y, 0f, 0f, 0.5f, out float rx, out float ry); // inclinada hacia arriba a la derecha
            Part(ref p, Polygon(rx, ry, ArrowPoly) - 0.03f, Cream);
        }

        private static void Position(ref Px p, float x, float y)
        {
            // Marcador de ubicación crema con centro coral: "DÓNDE ESTÁ".
            float pin = UnevenCapsule(x, -(y - 0.20f), 0.36f, 0.04f, 0.58f);
            Part(ref p, pin, Cream);
            Part(ref p, Circle(x, y, 0f, 0.20f, 0.14f), Hex(0xFF6B4A));
        }
    }
}
