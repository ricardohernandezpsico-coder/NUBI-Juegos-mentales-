using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Flecha de arcilla (crema, borde tinta, sombra dura y brillo) en las 4 direcciones. Se hornea una por
    /// dirección en vez de girar la imagen: así la sombra dura siempre cae hacia abajo, como en toda la app.
    /// Orden: 0 arriba, 1 abajo, 2 izquierda, 3 derecha (el de <c>ChipDirection</c>). <c>Image.color</c> en blanco.
    /// </summary>
    public static class ClayArrowSprite
    {
        private const int SizePx = 160;
        private const float Zoom = 1.12f;
        private const float Aa = 0.03f;
        private const float Line = 0.09f;
        private const float Drop = 0.09f;
        private static readonly Sprite[] Cache = new Sprite[4];

        private static readonly float[] Poly = { 0f, 0.84f, 0.64f, 0.10f, 0.26f, 0.10f, 0.26f, -0.78f, -0.26f, -0.78f, -0.26f, 0.10f, -0.64f, 0.10f };

        public static Sprite Get(int direction)
        {
            direction = Mathf.Clamp(direction, 0, 3);
            if (Cache[direction] != null) return Cache[direction];
            return Cache[direction] = ToSprite(Render(direction, SizePx), SizePx, SizePx);
        }

        /// <summary>Lleva el punto al sistema de la flecha que apunta ARRIBA.</summary>
        private static void ToUp(int direction, float x, float y, out float ux, out float uy)
        {
            switch (direction)
            {
                case 1: ux = -x; uy = -y; break;   // abajo
                case 2: ux = y; uy = -x; break;    // izquierda
                case 3: ux = -y; uy = x; break;    // derecha
                default: ux = x; uy = y; break;
            }
        }

        private static float Body(int direction, float x, float y)
        {
            ToUp(direction, x, y, out float ux, out float uy);
            return Polygon(ux, uy, Poly) - 0.07f;
        }

        /// <summary>Píxeles de la flecha (fila 0 = abajo). Separado de <see cref="Get"/> para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(int direction, int size) =>
            RenderClay(size, Zoom, Line, Drop, Aa, (x, y) => Body(direction, x, y), (ref Px p, float x, float y) =>
            {
                float body = Body(direction, x, y);
                p.Over(Cream, Cover(body, Aa));
                p.Over(new Color(1f, 1f, 1f, 0.6f), Cover(Ellipse(x, y, -0.18f, 0.30f, 0.12f, 0.06f), Aa) * Cover(body + 0.04f, Aa));
            });
    }
}
