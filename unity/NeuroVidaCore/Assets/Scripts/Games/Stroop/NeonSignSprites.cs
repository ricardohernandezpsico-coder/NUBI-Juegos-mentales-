using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Stroop
{
    /// <summary>
    /// Arte propio de Tinta o Palabra: la tarjeta es un LETRERO DE NEÓN de arcilla, montado con cuatro tornillos
    /// (este sprite) en las esquinas. Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class NeonSignSprites
    {
        private const int SizePx = 64;
        private static Sprite _screw;

        public static Sprite Screw() => _screw != null ? _screw : (_screw = ToSprite(RenderScrew(SizePx), SizePx, SizePx));

        /// <summary>Tornillo (fila 0 = abajo): cabeza gris lila con borde tinta, ranura en diagonal y brillo.</summary>
        public static Color32[] RenderScrew(int size) =>
            RenderClay(size, 1.12f, 0.14f, 0.1f, 0.06f, (x, y) => Circle(x, y, 0f, 0f, 0.74f), (ref Px p, float x, float y) =>
            {
                float head = Circle(x, y, 0f, 0f, 0.74f);
                p.Over(Hex(0xC9C3EE), Cover(head, 0.06f));
                p.Over(Ink, Cover(Capsule(x, y, -0.4f, -0.4f, 0.4f, 0.4f, 0.09f), 0.06f) * Cover(head + 0.1f, 0.06f));
                p.Over(new Color(1f, 1f, 1f, 0.6f), Cover(Ellipse(x, y, -0.28f, 0.34f, 0.18f, 0.1f), 0.06f));
            });
    }
}
