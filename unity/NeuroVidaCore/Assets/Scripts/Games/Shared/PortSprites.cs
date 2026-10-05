using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Los planetas-puerto de colores: ocho planetas con anillo, cada uno de un color y con un SÍMBOLO neutro en el centro (color + forma: se reconocen aunque no se
    /// distingan los colores). Los usan Bitácora de Misión y Correo Estelar. Vivían en Tráfico Estelar (<c>TrafficSprites</c>), retirado el 4-oct-2026.
    /// Símbolos (4-oct: nada de estrella con puntas, media luna ni cruz): corazón, hexágono, rombo, triángulo, gota, ola, cuadrado y aro.
    /// Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class PortSprites
    {
        public static readonly Color[] Colors = { Coral, Sun, Sky, Lime, Grape, Pink, Mint, Orange };

        private static readonly Sprite[] _ports = new Sprite[8];

        public static Sprite Port(int color)
        {
            color = Mathf.Clamp(color, 0, 7);
            return _ports[color] != null ? _ports[color] : _ports[color] = ToSprite(RenderPort(color, 192), 192, 192);
        }

        // ------------------------------------------------------------------ símbolos

        /// <summary>Símbolo de cada color (SDF, centrado en el origen, tamaño ~<paramref name="s"/>).</summary>
        public static float Glyph(int color, float x, float y, float s)
        {
            switch (color)
            {
                case 0: return HeartUnit(x / s, (y + s * 0.55f) / s) * s;                               // corazón
                case 1: return Polygon(x, y, new[] { 0f, s * 0.92f, s * 0.8f, s * 0.46f, s * 0.8f, -s * 0.46f, 0f, -s * 0.92f, -s * 0.8f, -s * 0.46f, -s * 0.8f, s * 0.46f }); // hexágono
                case 2: return Polygon(x, y, new[] { 0f, s * 0.95f, s * 0.7f, 0f, 0f, -s * 0.95f, -s * 0.7f, 0f }); // rombo
                case 3: return Polygon(x, y, new[] { 0f, s * 0.85f, s * 0.85f, -s * 0.6f, -s * 0.85f, -s * 0.6f }); // triángulo
                case 4: return DropShape(x, y, s);                                                        // gota
                case 5: return WaveShape(x, y, s);                                                        // ola
                case 6: return RoundBox(x, y, 0f, 0f, s * 0.68f, s * 0.68f, s * 0.14f);                  // cuadrado
                default: return Mathf.Abs(Circle(x, y, 0f, 0f, s * 0.62f)) - s * 0.2f;                  // aro
            }
        }

        /// <summary>Gota: un círculo abajo y una punta redonda arriba (tangentes al círculo).</summary>
        private static float DropShape(float x, float y, float s) =>
            Mathf.Min(Circle(x, y, 0f, -s * 0.18f, s * 0.56f), Polygon(x, y, new[] { 0f, s * 0.95f, s * 0.486f, s * 0.097f, -s * 0.486f, s * 0.097f }));

        /// <summary>Ola: una franja ondulada, con los extremos cortados.</summary>
        private static float WaveShape(float x, float y, float s)
        {
            float band = (Mathf.Abs(y - s * 0.34f * Mathf.Sin(x / s * 3.9f)) - s * 0.17f) * 0.75f;
            return Mathf.Max(band, Mathf.Abs(x) - s * 0.95f);
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
    }
}
