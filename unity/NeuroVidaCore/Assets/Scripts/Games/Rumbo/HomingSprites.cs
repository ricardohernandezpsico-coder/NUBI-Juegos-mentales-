using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Rumbo
{
    /// <summary>
    /// Arte procedural de "Rumbo a Casa", con el pincel de arcilla común (<see cref="ClayRaster"/>). Todo se ve desde
    /// arriba (vista de cabina y mapa):
    /// <list type="bullet">
    /// <item><see cref="Ship"/>: la nave apuntando hacia arriba (casco crema, alas coral, cabina celeste). La versión sin
    /// sombra (<c>shadow: false</c>) es la que gira en el mapa del final: así ninguna sombra dura queda torcida.</item>
    /// <item><see cref="Base"/>: tu casa, una estación en anillo con ventanas encendidas y una casita en el centro.</item>
    /// <item><see cref="Crystal"/>: los cristales de la ruta (un color por parada).</item>
    /// <item><see cref="Beacon"/>: el faro, una estrella de cuatro puntas muy lejana.</item>
    /// <item><see cref="AimArrow"/>: la flecha con la que se apunta hacia casa (sin sombra: gira).</item>
    /// <item><see cref="Fog"/>: la niebla alrededor de la nave (despejada cerca, espesa lejos).</item>
    /// </list>
    /// Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class HomingSprites
    {
        private const float Aa = 0.02f;
        /// <summary>Colores de los cristales, uno por parada (se repiten desde la sexta).</summary>
        public static readonly Color[] CrystalColors = { Lime, Sky, Grape, Pink, Sun };

        private static Sprite _ship, _shipFlat, _base, _beacon, _arrow, _fog;
        private static readonly Sprite[] _crystals = new Sprite[5];

        public static Sprite Ship(bool shadow = true)
        {
            if (shadow) return _ship != null ? _ship : _ship = ToSprite(RenderShip(256, true), 256, 256);
            return _shipFlat != null ? _shipFlat : _shipFlat = ToSprite(RenderShip(256, false), 256, 256);
        }

        public static Sprite Base() => _base != null ? _base : _base = ToSprite(RenderBase(320), 320, 320);
        public static Sprite Beacon() => _beacon != null ? _beacon : _beacon = ToSprite(RenderBeacon(192), 192, 192);
        public static Sprite AimArrow() => _arrow != null ? _arrow : _arrow = ToSprite(RenderArrow(256), 256, 256);

        public static Sprite Crystal(int index)
        {
            int i = ((index % _crystals.Length) + _crystals.Length) % _crystals.Length;
            return _crystals[i] != null ? _crystals[i] : _crystals[i] = ToSprite(RenderCrystal(i, 192), 192, 192);
        }

        // ------------------------------------------------------------------ nave

        private static readonly float[] HullPts = { 0f, 0.86f, 0.25f, 0.4f, 0.3f, -0.36f, 0.17f, -0.6f, -0.17f, -0.6f, -0.3f, -0.36f, -0.25f, 0.4f };
        private static readonly float[] WingR = { 0.24f, 0.08f, 0.74f, -0.4f, 0.72f, -0.62f, 0.27f, -0.44f };
        private static readonly float[] WingL = { -0.24f, 0.08f, -0.74f, -0.4f, -0.72f, -0.62f, -0.27f, -0.44f };

        private static float Hull(float x, float y) => Polygon(x, y, HullPts) - 0.03f;
        private static float Wings(float x, float y) => Mathf.Min(Polygon(x, y, WingR), Polygon(x, y, WingL)) - 0.03f;
        private static float Nozzle(float x, float y) => RoundBox(x, y, 0f, -0.66f, 0.13f, 0.08f, 0.04f);
        private static float ShipBody(float x, float y) => Mathf.Min(Mathf.Min(Hull(x, y), Wings(x, y)), Nozzle(x, y));

        public static Color32[] RenderShip(int size, bool shadow) =>
            RenderClay(size, 1.1f, 0.055f, shadow ? 0.07f : 0f, Aa, ShipBody, (ref Px p, float x, float y) =>
            {
                p.Over(Hex(0x3A3060), Cover(Nozzle(x, y), Aa));
                float w = Wings(x, y);
                p.Over(Coral, Cover(w, Aa));
                p.Over(Shade(Coral, 0.8f), Cover(w, Aa) * Cover(Mathf.Abs(x) - 0.52f + 0.0f, Aa) * Cover(-(Mathf.Abs(x) - 0.62f), Aa));
                float h = Hull(x, y);
                p.Over(Cream, Cover(h, Aa));
                // Franja coral en el casco y brillo a la izquierda (la luz viene de arriba a la izquierda).
                p.Over(Coral, Cover(RoundBox(x, y, 0f, -0.2f, 0.4f, 0.05f, 0.02f), Aa) * Cover(h + 0.02f, Aa));
                p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(x, y, -0.13f, 0.05f, 0.04f, 0.26f), Aa) * Cover(h + 0.03f, Aa));
                float win = Ellipse(x, y, 0f, 0.3f, 0.12f, 0.19f);
                p.Over(Ink, Cover(win - 0.04f, Aa));
                p.Over(Sky, Cover(win, Aa));
                p.Over(new Color(1f, 1f, 1f, 0.75f), Cover(Ellipse(x, y, -0.04f, 0.37f, 0.035f, 0.06f), Aa));
            });

        // ------------------------------------------------------------------ base (casa)

        private static float Ring(float x, float y) => Mathf.Abs(Circle(x, y, 0f, 0f, 0.7f)) - 0.14f;
        private static float Spokes(float x, float y) =>
            Mathf.Min(Mathf.Min(Capsule(x, y, -0.6f, 0f, 0.6f, 0f, 0.07f), Capsule(x, y, 0f, -0.6f, 0f, 0.6f, 0.07f)), float.MaxValue);
        private static float Hub(float x, float y) => Circle(x, y, 0f, 0f, 0.3f);
        private static float BaseBody(float x, float y) => Mathf.Min(Mathf.Min(Ring(x, y), Spokes(x, y)), Hub(x, y));

        private static readonly float[] HousePts = { 0f, 0.17f, 0.15f, 0.04f, 0.15f, -0.14f, -0.15f, -0.14f, -0.15f, 0.04f };

        public static Color32[] RenderBase(int size) => RenderClay(size, 1.08f, 0.05f, 0.06f, Aa, BaseBody, (ref Px p, float x, float y) =>
        {
            p.Over(Grape, Cover(Spokes(x, y), Aa));
            float ring = Ring(x, y);
            p.Over(Cream, Cover(ring, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Mathf.Abs(Circle(x, y, 0f, 0f, 0.77f)) - 0.025f, Aa) * Cover(ring + 0.02f, Aa));
            // Ventanas encendidas (luz cálida de casa), repartidas por el anillo.
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f + 0.26f;
                float wx = Mathf.Cos(a) * 0.7f, wy = Mathf.Sin(a) * 0.7f;
                float win = Circle(x, y, wx, wy, 0.055f);
                p.Over(Ink, Cover(win - 0.025f, Aa));
                p.Over(Sun, Cover(win, Aa));
            }
            float hub = Hub(x, y);
            p.Over(Coral, Cover(hub, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.35f), Cover(Ellipse(x, y, -0.1f, 0.12f, 0.1f, 0.06f), Aa) * Cover(hub + 0.02f, Aa));
            float house = Polygon(x, y, HousePts) - 0.015f;
            p.Over(Ink, Cover(house - 0.03f, Aa));
            p.Over(Cream, Cover(house, Aa));
            p.Over(Coral, Cover(RoundBox(x, y, 0f, -0.08f, 0.04f, 0.06f, 0.015f), Aa));
        });

        // ------------------------------------------------------------------ cristal

        private static readonly float[] GemPts = { 0f, 0.86f, 0.44f, 0.38f, 0.44f, -0.38f, 0f, -0.86f, -0.44f, -0.38f, -0.44f, 0.38f };
        private static float Gem(float x, float y) => Polygon(x, y, GemPts) - 0.02f;

        public static Color32[] RenderCrystal(int colorIndex, int size)
        {
            var c = CrystalColors[colorIndex % CrystalColors.Length];
            return RenderClay(size, 1.12f, 0.06f, 0.07f, Aa, Gem, (ref Px p, float x, float y) =>
            {
                float g = Gem(x, y);
                p.Over(c, Cover(g, Aa));
                // Facetas: izquierda en sombra, derecha iluminada, franja central clara y punta de arriba brillante.
                p.Over(Shade(c, 0.78f), Cover(g, Aa) * Cover(x + 0.16f, Aa));
                p.Over(Tint(c, 0.3f), Cover(g + 0.01f, Aa) * Cover(0.16f - x, Aa));
                p.Over(Tint(c, 0.55f), Cover(Polygon(x, y, new[] { 0f, 0.8f, 0.36f, 0.4f, 0f, 0.3f, -0.36f, 0.4f }), Aa));
                p.Over(new Color(1f, 1f, 1f, 0.8f), Cover(Ellipse(x, y, -0.2f, 0.28f, 0.05f, 0.14f), Aa));
            });
        }

        // ------------------------------------------------------------------ faro

        private static float BeaconBody(float x, float y) => Mathf.Min(Circle(x, y, 0f, 0f, 0.6f), Mathf.Abs(Circle(x, y, 0f, 0f, 0.88f)) - 0.07f);   // lucero redondo con su aro (antes, estrella de 4 puntas)

        public static Color32[] RenderBeacon(int size) => RenderClay(size, 1.1f, 0.05f, 0.06f, Aa, BeaconBody, (ref Px p, float x, float y) =>
        {
            float s = BeaconBody(x, y);
            p.Over(Sun, Cover(s, Aa));
            p.Over(Tint(Sun, 0.5f), Cover(s + 0.08f, Aa));
            float core = Circle(x, y, 0f, 0f, 0.24f);
            p.Over(Cream, Cover(core, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.9f), Cover(Circle(x, y, -0.06f, 0.06f, 0.07f), Aa));
        });

        // ------------------------------------------------------------------ flecha de rumbo

        private static float ArrowBody(float x, float y) =>
            Mathf.Min(Capsule(x, y, 0f, -0.82f, 0f, 0.46f, 0.1f), Polygon(x, y, new[] { 0f, 0.95f, 0.34f, 0.42f, -0.34f, 0.42f }) - 0.03f);

        public static Color32[] RenderArrow(int size) => RenderClay(size, 1.08f, 0.06f, 0f, Aa, ArrowBody, (ref Px p, float x, float y) =>
        {
            float a = ArrowBody(x, y);
            p.Over(Sun, Cover(a, Aa));
            p.Over(Tint(Sun, 0.45f), Cover(a, Aa) * Cover(x + 0.02f, Aa) * Cover(-x - 0.06f + 0.1f, Aa));
        });

        /// <summary>Alto de la flecha, en fracción del sprite, donde está la cola (el pivote que queda sobre la nave).</summary>
        public const float ArrowTailFraction = (-0.92f / 1.08f + 1f) * 0.5f;

        // ------------------------------------------------------------------ dial de rumbo

        private static Sprite _dial;

        /// <summary>Anillo fino con 12 marcas alrededor de la nave (en blanco, se tiñe con <c>Image.color</c>). Gira con la
        /// nave, no con el mapa: marca "adelante", no el norte.</summary>
        public static Sprite Dial()
        {
            if (_dial != null) return _dial;
            const int size = 512;
            var px = new Color32[size * size];
            const float aa = 1.5f / size;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Cover(Mathf.Abs(r - 0.93f) - 0.008f, aa * 2f);
                    float ang = Mathf.Atan2(dx, dy) / (Mathf.PI * 2f) * 12f;
                    float nearTick = Mathf.Abs(ang - Mathf.Round(ang)) * (Mathf.PI * 2f / 12f) * r;
                    bool major = Mathf.Abs(Mathf.Round(ang)) % 3 == 0;
                    float tick = Cover(nearTick - (major ? 0.012f : 0.007f), aa * 2f) * Cover(Mathf.Abs(r - 0.93f) - (major ? 0.06f : 0.035f), aa * 2f);
                    float a = Mathf.Max(ring * 0.8f, tick);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            return _dial = ToSprite(px, size, size);
        }

        // ------------------------------------------------------------------ niebla

        /// <summary>Radio del dibujo de la niebla, en unidades del mapa (la imagen mide el doble de ancho).</summary>
        public const float FogRadius = 1500f;

        public static Sprite Fog()
        {
            if (_fog != null) return _fog;
            const int size = 256;
            var px = new Color32[size * size];
            var night = Hex(0x0C0826);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * FogRadius;
                    float k = Mathf.Clamp01((d - (HomingContract.FogClear - 60f)) / (HomingContract.FogGone + 80f - (HomingContract.FogClear - 60f)));
                    float a = k * k * (3f - 2f * k) * 0.66f + Mathf.Clamp01((d - HomingContract.FogGone) / 900f) * 0.1f;
                    px[y * size + x] = new Color(night.r, night.g, night.b, a);
                }
            return _fog = ToSprite(px, size, size);
        }
    }
}
