using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Aterrizaje
{
    /// <summary>
    /// Arte procedural de Aterrizaje Lunar, con el pincel de arcilla común (<see cref="ClayRaster"/>):
    /// <list type="bullet">
    /// <item><see cref="Lander"/>: módulo lunar (cabina crema con ventanilla, base dorada, tres patas con zapatas). Sus
    /// zapatas tocan el borde inferior del dibujo (<see cref="LanderFootY"/>): así se posa justo sobre la regla.</item>
    /// <item><see cref="Flag"/>: bandera coral en su asta; la punta del asta marca el lugar exacto.</item>
    /// </list>
    /// Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class LandingSprites
    {
        private const float Zoom = 1.08f;
        /// <summary>Altura de las zapatas en el dibujo (-1..1, antes del zoom): el controlador alinea eso con la regla.</summary>
        public const float LanderFootY = -0.86f;
        /// <summary>Fracción del alto del sprite (desde abajo) donde apoyan las zapatas.</summary>
        public static float LanderFootFraction => (LanderFootY / Zoom + 1f) * 0.5f;

        private static Sprite _lander, _flag;

        public static Sprite Lander() => _lander != null ? _lander : _lander = ToSprite(RenderLander(256), 256, 256);
        public static Sprite Flag() => _flag != null ? _flag : _flag = ToSprite(RenderFlag(192), 192, 192);

        private static float Cabin(float x, float y) => RoundBox(x, y, 0f, 0.22f, 0.42f, 0.34f, 0.2f);
        private static float Base(float x, float y) => Polygon(x, y, new[] { -0.6f, -0.36f, 0.6f, -0.36f, 0.46f, -0.08f, -0.46f, -0.08f });

        private static float Legs(float x, float y) =>
            Mathf.Min(Capsule(x, y, -0.5f, -0.3f, -0.74f, -0.8f, 0.045f),
                Mathf.Min(Capsule(x, y, 0.5f, -0.3f, 0.74f, -0.8f, 0.045f), Capsule(x, y, 0f, -0.34f, 0f, -0.8f, 0.045f)));

        private static float Feet(float x, float y) =>
            Mathf.Min(RoundBox(x, y, -0.76f, -0.82f, 0.13f, 0.04f, 0.04f),
                Mathf.Min(RoundBox(x, y, 0.76f, -0.82f, 0.13f, 0.04f, 0.04f), RoundBox(x, y, 0f, -0.82f, 0.11f, 0.04f, 0.04f)));

        private static float Antenna(float x, float y) =>
            Mathf.Min(Capsule(x, y, 0.18f, 0.54f, 0.3f, 0.8f, 0.025f), Circle(x, y, 0.3f, 0.82f, 0.06f));

        private static float LanderBody(float x, float y) =>
            Mathf.Min(Mathf.Min(Cabin(x, y), Base(x, y)), Mathf.Min(Mathf.Min(Legs(x, y), Feet(x, y)), Antenna(x, y)));

        public static Color32[] RenderLander(int size) => RenderClay(size, Zoom, 0.05f, 0.06f, 0.02f, LanderBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.02f;
            p.Over(Hex(0x9C94D6), Cover(Legs(x, y), aa));
            p.Over(Hex(0x9C94D6), Cover(Feet(x, y), aa));
            p.Over(Cream, Cover(Antenna(x, y), aa));
            p.Over(Coral, Cover(Circle(x, y, 0.3f, 0.82f, 0.06f), aa));
            float b = Base(x, y);
            p.Over(Sun, Cover(b, aa));
            // Arrugas del papel dorado (líneas finas).
            float crease = Mathf.Min(Capsule(x, y, -0.3f, -0.3f, -0.2f, -0.12f, 0.012f), Capsule(x, y, 0.22f, -0.32f, 0.3f, -0.14f, 0.012f));
            p.Over(Shade(Sun, 0.75f), Cover(crease, aa) * Cover(b + 0.02f, aa));
            float c = Cabin(x, y);
            p.Over(Ink, Cover(Mathf.Abs(b) - 0.03f, aa) * Cover(c - 0.05f, aa)); // costura entre cabina y base
            p.Over(Cream, Cover(c, aa));
            p.Over(new Color(1f, 1f, 1f, 0.6f), Cover(Ellipse(x, y, -0.2f, 0.44f, 0.14f, 0.05f), aa) * Cover(c + 0.03f, aa));
            float win = Circle(x, y, 0.04f, 0.22f, 0.17f);
            p.Over(Ink, Cover(win - 0.045f, aa));
            p.Over(Sky, Cover(win, aa));
            p.Over(new Color(1f, 1f, 1f, 0.7f), Cover(Ellipse(x, y, -0.02f, 0.29f, 0.06f, 0.03f), aa));
        });

        private static float Pole(float x, float y) => Capsule(x, y, -0.5f, -0.9f, -0.5f, 0.8f, 0.05f);
        private static float Pennant(float x, float y) =>
            Polygon(x, y, new[] { -0.48f, 0.78f, 0.6f, 0.62f + 0.06f * Mathf.Sin(x * 6f), 0.62f, 0.3f, -0.48f, 0.18f });

        private static float FlagBody(float x, float y) => Mathf.Min(Pole(x, y), Pennant(x, y));

        public static Color32[] RenderFlag(int size) => RenderClay(size, 1.05f, 0.05f, 0.05f, 0.025f, FlagBody, (ref Px p, float x, float y) =>
        {
            p.Over(Cream, Cover(Pole(x, y), 0.025f));
            float pen = Pennant(x, y);
            p.Over(Coral, Cover(pen, 0.025f));
            p.Over(Cream, Cover(Circle(x, y, 0.06f, 0.48f, 0.1f), 0.025f) * Cover(pen, 0.025f));
        });
    }
}
