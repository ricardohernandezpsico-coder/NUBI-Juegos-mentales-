using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Constelacion
{
    /// <summary>
    /// Arte procedural de Constelación de Palabras, con el pincel de arcilla común (<see cref="ClayRaster"/>):
    /// <list type="bullet">
    /// <item><see cref="Star"/>: la estrella de cada palabra (cuatro puntas redondeadas y centro claro), una por color de
    /// constelación; <see cref="Neutral"/> es la de una palabra suelta (crema).</item>
    /// <item><see cref="Mic"/>: el micrófono de arcilla (botón para empezar a hablar).</item>
    /// </list>
    /// Colores horneados: <c>Image.color</c> en blanco (salvo para atenuar).
    /// </summary>
    public static class ConstellationSprites
    {
        /// <summary>Colores de las constelaciones, en el orden en que se forman.</summary>
        public static readonly Color[] Colors = { Sky, Sun, Lime, Pink, Grape, Coral, Mint, Orange };

        private static readonly Sprite[] _stars = new Sprite[8];
        private static Sprite _neutral, _mic;

        public static Sprite Star(int colorIndex)
        {
            int i = ((colorIndex % _stars.Length) + _stars.Length) % _stars.Length;
            return _stars[i] != null ? _stars[i] : _stars[i] = ToSprite(RenderStar(Colors[i], 160), 160, 160);
        }

        public static Sprite Neutral() => _neutral != null ? _neutral : _neutral = ToSprite(RenderStar(Cream, 160), 160, 160);

        public static Sprite Mic() => _mic != null ? _mic : _mic = ToSprite(RenderMic(256), 256, 256);

        private static float StarBody(float x, float y) =>
            Mathf.Min(ClayRaster.Star(x, y, 0f, 0f, 4, 0.9f, 2.2f, 0.08f), Circle(x, y, 0f, 0f, 0.34f));

        public static Color32[] RenderStar(Color c, int size) => RenderClay(size, 1.1f, 0.06f, 0.07f, 0.025f, StarBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.025f;
            float s = StarBody(x, y);
            p.Over(c, Cover(s, aa));
            p.Over(Tint(c, 0.45f), Cover(s + 0.1f, aa));
            p.Over(Tint(c, 0.8f), Cover(Circle(x, y, 0f, 0f, 0.2f), aa));
            p.Over(new Color(1f, 1f, 1f, 0.85f), Cover(Circle(x, y, -0.07f, 0.08f, 0.07f), aa));
        });

        // ------------------------------------------------------------------ micrófono

        private static float Head(float x, float y) => RoundBox(x, y, 0f, 0.26f, 0.26f, 0.42f, 0.26f);
        private static float Cradle(float x, float y) =>
            Mathf.Max(Mathf.Abs(Circle(x, y, 0f, 0.12f, 0.42f)) - 0.06f, y - 0.12f);
        private static float Stem(float x, float y) =>
            Mathf.Min(Capsule(x, y, 0f, -0.3f, 0f, -0.62f, 0.07f), RoundBox(x, y, 0f, -0.68f, 0.3f, 0.07f, 0.06f));
        private static float MicBody(float x, float y) => Mathf.Min(Mathf.Min(Head(x, y), Cradle(x, y)), Stem(x, y));

        public static Color32[] RenderMic(int size) => RenderClay(size, 1.1f, 0.06f, 0.07f, 0.02f, MicBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.02f;
            p.Over(Cream, Cover(Cradle(x, y), aa));
            p.Over(Cream, Cover(Stem(x, y), aa));
            float h = Head(x, y);
            p.Over(Coral, Cover(h, aa));
            // Rejilla: tres ranuras tinta y el brillo de la izquierda.
            for (int i = 0; i < 3; i++)
                p.Over(Shade(Coral, 0.62f), Cover(RoundBox(x, y, 0f, 0.44f - 0.16f * i, 0.15f, 0.025f, 0.02f), aa) * Cover(h + 0.04f, aa));
            p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(x, y, -0.13f, 0.4f, 0.05f, 0.18f), aa) * Cover(h + 0.03f, aa));
        });
    }
}
