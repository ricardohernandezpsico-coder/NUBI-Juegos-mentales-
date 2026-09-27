using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Trafico
{
    /// <summary>
    /// Arte procedural de Tráfico Estelar, con el pincel de arcilla común (<see cref="ClayRaster"/>):
    /// <list type="bullet">
    /// <item><see cref="Port"/>: planeta-puerto de un color, con anillo y su SÍMBOLO en el centro.</item>
    /// <item><see cref="Pod"/>: cápsula de carga del mismo color y el mismo símbolo. Color + forma: se reconocen aunque
    /// no se distingan los colores.</item>
    /// <item><see cref="SwitchKnob"/>: el desvío (disco de arcilla con una flecha; el controlador lo gira hacia la
    /// salida activa).</item>
    /// </list>
    /// Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class TrafficSprites
    {
        public static readonly Color[] Colors = { Coral, Sun, Sky, Lime, Grape, Pink, Mint, Orange };

        private static readonly Sprite[] _ports = new Sprite[8];
        private static readonly Sprite[] _pods = new Sprite[8];
        private static Sprite _knob;

        public static Sprite Port(int color)
        {
            color = Mathf.Clamp(color, 0, 7);
            return _ports[color] != null ? _ports[color] : _ports[color] = ToSprite(RenderPort(color, 192), 192, 192);
        }

        public static Sprite Pod(int color)
        {
            color = Mathf.Clamp(color, 0, 7);
            return _pods[color] != null ? _pods[color] : _pods[color] = ToSprite(RenderPod(color, 128), 128, 128);
        }

        public static Sprite SwitchKnob() => _knob != null ? _knob : _knob = ToSprite(RenderKnob(160), 160, 160);

        // ------------------------------------------------------------------ símbolos

        /// <summary>Símbolo de cada color (SDF, centrado en el origen, tamaño ~<paramref name="s"/>).</summary>
        public static float Glyph(int color, float x, float y, float s)
        {
            switch (color)
            {
                case 0: return HeartUnit(x / s, (y + s * 0.55f) / s) * s;                               // corazón
                case 1: return Star(x, y, 0f, 0.02f * s, 5, s * 0.95f, 2.6f, s * 0.06f);                // estrella
                case 2: return Polygon(x, y, new[] { 0f, s * 0.95f, s * 0.7f, 0f, 0f, -s * 0.95f, -s * 0.7f, 0f }); // rombo
                case 3: return Polygon(x, y, new[] { 0f, s * 0.85f, s * 0.85f, -s * 0.6f, -s * 0.85f, -s * 0.6f }); // triángulo
                case 4: return Crescent(x + s * 0.1f, y, s * 0.45f, s * 0.8f, s * 0.62f);                // luna
                case 5: return Mathf.Min(RoundBox(x, y, 0f, 0f, s * 0.8f, s * 0.26f, s * 0.12f),          // cruz
                                         RoundBox(x, y, 0f, 0f, s * 0.26f, s * 0.8f, s * 0.12f));
                case 6: return RoundBox(x, y, 0f, 0f, s * 0.68f, s * 0.68f, s * 0.14f);                  // cuadrado
                default: return Mathf.Abs(Circle(x, y, 0f, 0f, s * 0.62f)) - s * 0.2f;                  // aro
            }
        }

        // ------------------------------------------------------------------ piezas

        private static float PortBody(float x, float y) =>
            Mathf.Min(Circle(x, y, 0f, 0f, 0.66f), Mathf.Max(Ellipse(x, y, 0f, 0f, 0.95f, 0.26f), -Ellipse(x, y, 0f, 0f, 0.8f, 0.14f)));

        public static Color32[] RenderPort(int color, int size) => RenderClay(size, 1.08f, 0.06f, 0.07f, 0.018f, PortBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.018f;
            var c = Colors[color];
            float ring = Mathf.Max(Ellipse(x, y, 0f, 0f, 0.95f, 0.26f), -Ellipse(x, y, 0f, 0f, 0.8f, 0.14f));
            // Anillo de atrás (debajo del planeta), planeta, anillo de adelante (mitad inferior).
            p.Over(Tint(c, 0.45f), Cover(ring, aa) * (y > 0f ? 1f : 0f));
            float body = Circle(x, y, 0f, 0f, 0.66f);
            p.Over(Ink, Cover(body - 0.05f, aa));
            p.Over(c, Cover(body, aa));
            p.Over(new Color(1f, 1f, 1f, 0.35f), Cover(Ellipse(x, y, -0.24f, 0.38f, 0.2f, 0.07f), aa));
            float front = ring;
            p.Over(Ink, Cover(front - 0.045f, aa) * (y <= 0f ? 1f : 0f));
            p.Over(Tint(c, 0.45f), Cover(front, aa) * (y <= 0f ? 1f : 0f));
            // Símbolo en crema con contorno tinta.
            float g = Glyph(color, x, y - 0.04f, 0.3f);
            p.Over(Ink, Cover(g - 0.045f, aa));
            p.Over(Cream, Cover(g, aa));
        });

        private static float PodBody(float x, float y) =>
            Mathf.Min(Circle(x, y, 0f, 0f, 0.62f), Mathf.Min(Capsule(x, y, -0.66f, 0f, -0.9f, -0.3f, 0.1f), Capsule(x, y, 0.66f, 0f, 0.9f, -0.3f, 0.1f)));

        public static Color32[] RenderPod(int color, int size) => RenderClay(size, 1.1f, 0.08f, 0.09f, 0.03f, PodBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.03f;
            var c = Colors[color];
            p.Over(Shade(c, 0.8f), Cover(Mathf.Min(Capsule(x, y, -0.66f, 0f, -0.9f, -0.3f, 0.1f), Capsule(x, y, 0.66f, 0f, 0.9f, -0.3f, 0.1f)), aa));
            float body = Circle(x, y, 0f, 0f, 0.62f);
            p.Over(c, Cover(body, aa));
            p.Over(new Color(1f, 1f, 1f, 0.4f), Cover(Ellipse(x, y, -0.22f, 0.36f, 0.18f, 0.07f), aa));
            float g = Glyph(color, x, y - 0.02f, 0.3f);
            p.Over(Ink, Cover(g - 0.06f, aa));
            p.Over(Cream, Cover(g, aa));
        });

        private static float KnobBody(float x, float y) => Circle(x, y, 0f, 0f, 0.72f);

        public static Color32[] RenderKnob(int size) => RenderClay(size, 1.1f, 0.08f, 0.1f, 0.025f, KnobBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.025f;
            float disc = KnobBody(x, y);
            p.Over(Hex(0x2A3590), Cover(disc, aa));
            p.Over(new Color(1f, 1f, 1f, 0.28f), Cover(Ellipse(x, y, -0.26f, 0.4f, 0.2f, 0.07f), aa) * Cover(disc + 0.03f, aa));
            float arrow = Mathf.Min(Capsule(x, y, 0f, -0.38f, 0f, 0.3f, 0.09f),
                Mathf.Min(Capsule(x, y, -0.27f, 0.06f, 0f, 0.34f, 0.09f), Capsule(x, y, 0.27f, 0.06f, 0f, 0.34f, 0.09f)));
            p.Over(Cream, Cover(arrow, aa));
        });
    }
}
