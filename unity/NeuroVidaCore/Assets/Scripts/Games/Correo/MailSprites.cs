using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Correo
{
    /// <summary>
    /// Arte de "Correo Estelar" con el pincel de arcilla común (<see cref="ClayRaster"/>): el sobre que se recoge en la
    /// ruta, el paquete que se entrega, el botón de la radio y el reloj (tapado o destapado). Los planetas son los
    /// planetas-puerto de Tráfico Estelar (color + símbolo) y la nave, la de Piloto Estelar. Colores horneados:
    /// <c>Image.color</c> en blanco.
    /// </summary>
    public static class MailSprites
    {
        private const float Aa = 0.02f, Line = 0.06f;
        private static readonly Color Kraft = Hex(0xD9A066);
        private static Sprite _envelope, _package, _radio, _clockFace, _clockCover, _shieldFull, _shieldEmpty;
        private static readonly Sprite[] _damage = new Sprite[3];

        public static Sprite Envelope() => _envelope != null ? _envelope : _envelope = ToSprite(RenderEnvelope(160), 160, 160);
        public static Sprite Package() => _package != null ? _package : _package = ToSprite(RenderPackage(160), 160, 160);
        public static Sprite Radio() => _radio != null ? _radio : _radio = ToSprite(RenderRadio(224), 224, 224);
        public static Sprite ClockFace() => _clockFace != null ? _clockFace : _clockFace = ToSprite(RenderClock(192, false), 192, 192);
        public static Sprite ClockCover() => _clockCover != null ? _clockCover : _clockCover = ToSprite(RenderClock(192, true), 192, 192);
        /// <summary>Un segmento del escudo: entero (celeste, con brillo) o roto (hueco oscuro con una grieta): se distinguen
        /// por la forma, no solo por el color.</summary>
        public static Sprite ShieldPip(bool full) => full
            ? (_shieldFull != null ? _shieldFull : _shieldFull = ToSprite(RenderShieldPip(112, true), 112, 112))
            : (_shieldEmpty != null ? _shieldEmpty : _shieldEmpty = ToSprite(RenderShieldPip(112, false), 112, 112));
        /// <summary>Daño que se pone ENCIMA de la nave de Piloto (<c>ChipShipSprite</c>, mismo tamaño y encuadre):
        /// 1 = una grieta, 2 = dos grietas y una quemadura.</summary>
        public static Sprite ShipDamage(int damage)
        {
            int d = Mathf.Clamp(damage, 1, 2);
            return _damage[d] != null ? _damage[d] : _damage[d] = ToSprite(RenderShipDamage(192, d), 192, 192);
        }

        // ------------------------------------------------------------------ sobre y paquete

        private static float EnvBody(float x, float y) => RoundBox(x, y, 0f, 0f, 0.8f, 0.54f, 0.12f);

        public static Color32[] RenderEnvelope(int size) =>
            RenderClay(size, 1.1f, Line, 0.08f, Aa, EnvBody, (ref Px p, float x, float y) =>
            {
                float body = EnvBody(x, y);
                p.Over(Cream, Cover(body, Aa));
                // Solapa: una "V" de tinta desde las esquinas de arriba hasta el centro.
                float flap = Mathf.Min(Capsule(x, y, -0.68f, 0.42f, 0f, -0.06f, 0.035f), Capsule(x, y, 0.68f, 0.42f, 0f, -0.06f, 0.035f));
                p.Over(Ink, Cover(flap, Aa) * Cover(body + 0.02f, Aa));
                // Sello coral en el vértice.
                float seal = Circle(x, y, 0f, -0.08f, 0.15f);
                p.Over(Ink, Cover(seal - 0.035f, Aa));
                p.Over(Coral, Cover(seal, Aa));
                p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Circle(x, y, -0.05f, -0.03f, 0.045f), Aa));
            });

        private static float BoxBody(float x, float y) => RoundBox(x, y, 0f, -0.04f, 0.66f, 0.62f, 0.1f);

        public static Color32[] RenderPackage(int size) =>
            RenderClay(size, 1.1f, Line, 0.08f, Aa, BoxBody, (ref Px p, float x, float y) =>
            {
                float body = BoxBody(x, y);
                p.Over(Kraft, Cover(body, Aa));
                p.Over(Shade(Kraft, 0.85f), Cover(RoundBox(x, y, 0.33f, -0.04f, 0.33f, 0.62f, 0.02f), Aa) * Cover(body + 0.02f, Aa));
                // Cinta sol en cruz, con moño.
                float ribbon = Mathf.Min(RoundBox(x, y, 0f, -0.04f, 0.1f, 0.62f, 0.01f), RoundBox(x, y, 0f, -0.04f, 0.66f, 0.1f, 0.01f));
                p.Over(Ink, Cover(ribbon - 0.03f, Aa) * Cover(body + 0.01f, Aa));
                p.Over(Sun, Cover(ribbon, Aa) * Cover(body + 0.01f, Aa));
                float bow = Mathf.Min(Ellipse(x, y, -0.18f, 0.66f, 0.17f, 0.11f), Ellipse(x, y, 0.18f, 0.66f, 0.17f, 0.11f));
                p.Over(Ink, Cover(bow - 0.04f, Aa));
                p.Over(Sun, Cover(bow, Aa));
            });

        // ------------------------------------------------------------------ escudo y daño de la nave

        private static readonly float[] ShieldPts = { 0f, 0.78f, 0.62f, 0.56f, 0.58f, -0.08f, 0f, -0.8f, -0.58f, -0.08f, -0.62f, 0.56f };

        private static float ShieldBody(float x, float y) => Polygon(x, y, ShieldPts) - 0.08f;

        public static Color32[] RenderShieldPip(int size, bool full) =>
            RenderClay(size, 1.12f, Line, full ? 0.08f : 0f, Aa, ShieldBody, (ref Px p, float x, float y) =>
            {
                float body = ShieldBody(x, y);
                if (!full)
                {
                    // Hueco apagado (se distingue del cielo) partido por una grieta crema en zigzag.
                    p.Over(Hex(0x4A5299), Cover(body, Aa));
                    float crack = Mathf.Min(Mathf.Min(Capsule(x, y, -0.06f, 0.72f, 0.12f, 0.3f, 0.055f), Capsule(x, y, 0.12f, 0.3f, -0.1f, -0.05f, 0.055f)),
                        Capsule(x, y, -0.1f, -0.05f, 0.06f, -0.45f, 0.055f));
                    p.Over(Ink, Cover(crack - 0.03f, Aa));
                    p.Over(WithAlpha(Cream, 0.85f), Cover(crack, Aa));
                    return;
                }
                p.Over(Sky, Cover(body, Aa));
                p.Over(Shade(Sky, 0.82f), Cover(Mathf.Max(body, -x), Aa));
                p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(x, y, -0.24f, 0.38f, 0.14f, 0.2f), Aa));
            });

        // La silueta del casco de la nave de Piloto (ChipShipSprite, mirando arriba), para recortar las grietas.
        private static float ShipHull(float u, float v) =>
            Mathf.Max(Mathf.Max(Circle(u, v, -0.62f, -0.06f, 0.92f), Circle(u, v, 0.62f, -0.06f, 0.92f)), -0.58f - v);

        private static float Zigzag(float x, float y, float[] pts, float r)
        {
            float d = 99f;
            for (int i = 0; i + 3 < pts.Length; i += 2) d = Mathf.Min(d, Capsule(x, y, pts[i], pts[i + 1], pts[i + 2], pts[i + 3], r));
            return d;
        }

        private static readonly float[] Crack1 = { 0.34f, -0.08f, 0.17f, -0.17f, 0.21f, -0.29f, 0.07f, -0.4f };
        private static readonly float[] Crack2 = { -0.34f, 0.2f, -0.17f, 0.1f, -0.21f, -0.02f, -0.09f, -0.12f };

        public static Color32[] RenderShipDamage(int size, int damage)
        {
            const float zoom = 1.1f;
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = ((px + 0.5f) / size * 2f - 1f) * zoom;
                    float y = ((py + 0.5f) / size * 2f - 1f) * zoom;
                    float inside = Cover(ShipHull(x, y) + 0.06f, Aa);
                    var p = new Px();
                    if (inside > 0f)
                    {
                        if (damage >= 2) p.Over(new Color(Ink.r, Ink.g, Ink.b, 0.35f), inside * Cover(Ellipse(x, y, 0.12f, -0.3f, 0.17f, 0.13f), 0.12f));
                        p.Over(new Color(1f, 1f, 1f, 0.7f), inside * Cover(Zigzag(x - 0.022f, y + 0.022f, Crack1, 0.04f), Aa));
                        p.Over(Ink, inside * Cover(Zigzag(x, y, Crack1, 0.036f), Aa));
                        if (damage >= 2)
                        {
                            p.Over(new Color(1f, 1f, 1f, 0.7f), inside * Cover(Zigzag(x - 0.022f, y + 0.022f, Crack2, 0.04f), Aa));
                            p.Over(Ink, inside * Cover(Zigzag(x, y, Crack2, 0.036f), Aa));
                        }
                    }
                    pixels[py * size + px] = p.ToColor32();
                }
            }
            return pixels;
        }

        // ------------------------------------------------------------------ radio

        private static float RadioButton(float x, float y) => Circle(x, y, 0f, 0f, 0.86f);

        public static Color32[] RenderRadio(int size) =>
            RenderClay(size, 1.1f, Line, 0.08f, Aa, RadioButton, (ref Px p, float x, float y) =>
            {
                float disc = RadioButton(x, y);
                p.Over(Grape, Cover(disc, Aa));
                p.Over(new Color(1f, 1f, 1f, 0.35f), Cover(Ellipse(x, y, -0.3f, 0.45f, 0.22f, 0.12f), Aa));
                // Antena con luz sol.
                float antenna = Capsule(x, y, 0.2f, 0.1f, 0.36f, 0.52f, 0.045f);
                p.Over(Ink, Cover(antenna, Aa));
                float bulb = Circle(x, y, 0.38f, 0.56f, 0.09f);
                p.Over(Ink, Cover(bulb - 0.03f, Aa));
                p.Over(Sun, Cover(bulb, Aa));
                // Cuerpo crema con parlante de puntos y una perilla.
                float body = RoundBox(x, y, 0f, -0.16f, 0.46f, 0.3f, 0.08f);
                p.Over(Ink, Cover(body - 0.045f, Aa));
                p.Over(Cream, Cover(body, Aa));
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 2; j++)
                        p.Over(Ink, Cover(Circle(x, y, -0.3f + 0.13f * i, -0.08f - 0.15f * j, 0.04f), Aa));
                float knob = Circle(x, y, 0.24f, -0.16f, 0.1f);
                p.Over(Ink, Cover(knob - 0.03f, Aa));
                p.Over(Coral, Cover(knob, Aa));
                // Ondas que salen de la antena.
                for (int k = 0; k < 2; k++)
                {
                    float r = 0.18f + 0.13f * k;
                    float arc = Mathf.Max(Mathf.Abs(Circle(x, y, 0.38f, 0.56f, r)) - 0.025f, -(x - 0.38f) + (y - 0.56f) * 0.2f);
                    p.Over(Cream, Cover(Mathf.Max(arc, -(y - 0.5f)), Aa));
                }
            });

        // ------------------------------------------------------------------ reloj

        private static float ClockBody(float x, float y) => Circle(x, y, 0f, 0f, 0.84f);

        /// <summary>Reloj de la misión: destapado (esfera crema con marcas) o tapado (tapa oscura con el reborde del reloj).</summary>
        public static Color32[] RenderClock(int size, bool covered) =>
            RenderClay(size, 1.1f, Line, 0.08f, Aa, ClockBody, (ref Px p, float x, float y) =>
            {
                float body = ClockBody(x, y);
                if (covered)
                {
                    p.Over(Hex(0x2A3590), Cover(body, Aa));
                    p.Over(Hex(0x1B2466), Cover(Circle(x, y, 0f, 0f, 0.62f), Aa));
                    p.Over(new Color(1f, 1f, 1f, 0.25f), Cover(Ellipse(x, y, -0.3f, 0.45f, 0.2f, 0.1f), Aa));
                    return;
                }
                p.Over(Cream, Cover(body, Aa));
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6f;
                    float r0 = i % 3 == 0 ? 0.58f : 0.66f;
                    float tick = Capsule(x, y, Mathf.Sin(a) * r0, Mathf.Cos(a) * r0, Mathf.Sin(a) * 0.72f, Mathf.Cos(a) * 0.72f, i % 3 == 0 ? 0.04f : 0.025f);
                    p.Over(Ink, Cover(tick, Aa));
                }
                p.Over(Coral, Cover(Circle(x, y, 0f, 0f, 0.09f), Aa));
            });
    }
}
