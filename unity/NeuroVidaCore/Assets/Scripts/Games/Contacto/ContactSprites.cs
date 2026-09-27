using UnityEngine;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.Bitacora;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Contacto
{
    /// <summary>
    /// Arte de "Primer Contacto" con el pincel de arcilla común (<see cref="ClayRaster"/>):
    /// <list type="bullet">
    /// <item><see cref="Alien"/>: el nuri que habla (cuerpo de arcilla lima, tres ojos, antenas con luz). Dos versiones:
    /// boca cerrada (sonrisa) y abierta (al hablar).</item>
    /// <item><see cref="Tail"/>: la punta del globo de diálogo.</item>
    /// <item><see cref="Object"/>: las cosas que se nombran, con el arte ya existente: los íconos del cielo
    /// (<see cref="SymbolSprite"/>, pintados del color de la frase cuando el color está en juego) y los hallazgos de la
    /// Bitácora (<see cref="BitacoraSprites"/>, con sus colores).</item>
    /// </list>
    /// Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class ContactSprites
    {
        private const float Aa = 0.02f, Line = 0.05f;

        /// <summary>Los colores que nombran los nuri (mismo orden que los valores 0..4 de <see cref="ContactContract"/>).</summary>
        public static readonly Color[] Palette = { Coral, Sun, Sky, Grape, Lime };

        private static readonly Color Body = Hex(0x9BE564);
        private static Sprite _open, _closed, _tail;

        public static Sprite Alien(bool mouthOpen)
        {
            if (mouthOpen) return _open != null ? _open : _open = ToSprite(RenderAlien(320, true), 320, 320);
            return _closed != null ? _closed : _closed = ToSprite(RenderAlien(320, false), 320, 320);
        }

        public static Sprite Tail() => _tail != null ? _tail : _tail = ToSprite(RenderTail(96), 96, 96);

        /// <summary>El dibujo de una cosa (una copia; la cantidad la arma el juego).</summary>
        public static Sprite Object(Thing t)
        {
            if (!ContactContract.Colorable(t.Obj)) return BitacoraSprites.Find(t.Obj - ContactContract.SkyObjects);
            var kind = (ShapeKind)t.Obj;
            return t.Color >= 0 ? SymbolSprite.Colored(kind, Palette[t.Color]) : SymbolSprite.Get(kind, 0);
        }

        // ------------------------------------------------------------------ el nuri

        private static float Torso(float x, float y) => Ellipse(x, y, 0f, -0.12f, 0.6f, 0.62f);

        private static float Stalks(float x, float y) =>
            Mathf.Min(Capsule(x, y, -0.2f, 0.4f, -0.4f, 0.78f, 0.045f), Capsule(x, y, 0.2f, 0.4f, 0.4f, 0.78f, 0.045f));

        private static float Bulbs(float x, float y) => Mathf.Min(Circle(x, y, -0.42f, 0.84f, 0.11f), Circle(x, y, 0.42f, 0.84f, 0.11f));

        private static float Arms(float x, float y) =>
            Mathf.Min(Capsule(x, y, -0.5f, -0.18f, -0.8f, -0.02f, 0.09f), Capsule(x, y, 0.5f, -0.18f, 0.8f, -0.02f, 0.09f));

        private static float Feet(float x, float y) => Mathf.Min(Ellipse(x, y, -0.27f, -0.76f, 0.19f, 0.1f), Ellipse(x, y, 0.27f, -0.76f, 0.19f, 0.1f));

        private static float Silhouette(float x, float y) =>
            Mathf.Min(Mathf.Min(Torso(x, y), Mathf.Min(Stalks(x, y), Bulbs(x, y))), Mathf.Min(Arms(x, y), Feet(x, y)));

        public static Color32[] RenderAlien(int size, bool open) =>
            RenderClay(size, 1.12f, Line, 0.07f, Aa, Silhouette, (ref Px p, float x, float y) =>
            {
                Part(ref p, Stalks(x, y), Shade(Body, 0.85f));
                float bulbs = Bulbs(x, y);
                Part(ref p, bulbs, Sun);
                p.Over(new Color(1f, 1f, 1f, 0.6f), Cover(Mathf.Min(Circle(x, y, -0.45f, 0.88f, 0.035f), Circle(x, y, 0.39f, 0.88f, 0.035f)), Aa));
                Part(ref p, Arms(x, y), Body);
                Part(ref p, Feet(x, y), Shade(Body, 0.8f));
                float torso = Torso(x, y);
                Part(ref p, torso, Body);
                // Panza más clara y brillo de arcilla arriba a la izquierda.
                p.Over(Tint(Body, 0.35f), Cover(Ellipse(x, y, 0f, -0.36f, 0.34f, 0.26f), Aa) * Cover(torso + 0.02f, Aa));
                p.Over(new Color(1f, 1f, 1f, 0.4f), Cover(Ellipse(x, y, -0.3f, 0.26f, 0.1f, 0.06f), Aa));

                // Tres ojos: uno grande al centro y dos chicos arriba (mirando al centro, atentos).
                Eye(ref p, x, y, 0f, 0.12f, 0.19f, 0f);
                Eye(ref p, x, y, -0.33f, 0.24f, 0.1f, 0.3f);
                Eye(ref p, x, y, 0.33f, 0.24f, 0.1f, -0.3f);
                p.Over(WithAlpha(Pink, 0.55f), Cover(Mathf.Min(Ellipse(x, y, -0.37f, -0.06f, 0.08f, 0.045f), Ellipse(x, y, 0.37f, -0.06f, 0.08f, 0.045f)), Aa));

                if (open)
                {
                    float mouth = Ellipse(x, y, 0f, -0.2f, 0.13f, 0.11f);
                    p.Over(Ink, Cover(mouth, Aa));
                    p.Over(Coral, Cover(Ellipse(x, y, 0f, -0.27f, 0.08f, 0.04f), Aa) * Cover(mouth + 0.01f, Aa));
                }
                else
                {
                    float smile = Mathf.Max(Mathf.Abs(Circle(x, y, 0f, -0.02f, 0.16f)) - 0.025f, y + 0.1f);
                    p.Over(Ink, Cover(smile, Aa));
                }
            });

        private static void Eye(ref Px p, float x, float y, float cx, float cy, float r, float look)
        {
            float white = Circle(x, y, cx, cy, r);
            p.Over(Ink, Cover(white - 0.035f, Aa));
            p.Over(Cream, Cover(white, Aa));
            float px = cx + look * r * 0.5f, py = cy - r * 0.12f;
            p.Over(Ink, Cover(Circle(x, y, px, py, r * 0.5f), Aa));
            p.Over(Color.white, Cover(Circle(x, y, px - r * 0.18f, py + r * 0.2f, r * 0.16f), Aa));
        }

        /// <summary>Pieza con su borde tinta propio (para que se vean las uniones entre partes).</summary>
        private static void Part(ref Px p, float sdf, Color fill)
        {
            p.Over(Ink, Cover(sdf - Line, Aa));
            p.Over(fill, Cover(sdf, Aa));
        }

        // ------------------------------------------------------------------ globo

        private static readonly float[] TailPts = { 0f, 0.85f, 0.62f, -0.6f, -0.62f, -0.6f };

        public static Color32[] RenderTail(int size) =>
            RenderClay(size, 1.1f, 0.09f, 0f, 0.03f, (x, y) => Polygon(x, y, TailPts), (ref Px p, float x, float y) =>
                p.Over(Cream, Cover(Polygon(x, y, TailPts), 0.03f)));
    }
}
