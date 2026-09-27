using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Bitacora
{
    /// <summary>
    /// Arte de "Bitácora de Misión" con el pincel de arcilla común (<see cref="ClayRaster"/>):
    /// <list type="bullet">
    /// <item><see cref="Find"/>: los 16 hallazgos. Objetos cotidianos con nombre fácil (llave, campana, pluma...) y
    /// SILUETAS bien distintas: se reconocen por la forma, no solo por el color, y se pueden nombrar (la palabra y la
    /// imagen juntas se recuerdan mejor).</item>
    /// <item><see cref="Probe"/>: la sonda que recorre la ruta.</item>
    /// </list>
    /// Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class BitacoraSprites
    {
        private const float Zoom = 1.12f, Line = 0.06f, Drop = 0.075f, Aa = 0.02f;
        private static readonly Sprite[] _finds = new Sprite[BitacoraContract.FindCount];
        private static Sprite _probe;

        private static readonly Color Brown = Hex(0x9C6A4E);
        private static readonly Color Steel = Hex(0x8FA3C9);

        public static Sprite Find(int id)
        {
            id = Mathf.Clamp(id, 0, BitacoraContract.FindCount - 1);
            return _finds[id] != null ? _finds[id] : _finds[id] = ToSprite(RenderFind(id, 192), 192, 192);
        }

        public static Sprite Probe() => _probe != null ? _probe : _probe = ToSprite(RenderProbe(160), 160, 160);

        public static Color32[] RenderFind(int id, int size) =>
            RenderClay(size, Zoom, Line, Drop, Aa, (x, y) => Body(id, x, y), (ref Px p, float x, float y) => Paint(id, ref p, x, y));

        // ------------------------------------------------------------------ siluetas

        private static float Rot(float x, float y, float deg, out float ry)
        {
            Rotate(x, y, 0f, 0f, deg * Mathf.Deg2Rad, out float rx, out ry);
            return rx;
        }

        private static float Body(int id, float x, float y)
        {
            switch (id)
            {
                case 0: { float kx = Rot(x, y, -35f, out float ky); return Key(kx, ky); }
                case 1: return Bell(x, y);
                case 2: { float fx = Rot(x, y, -38f, out float fy); return Mathf.Min(FeatherVane(fx, fy), Capsule(fx, fy, 0f, -0.9f, 0f, 0.1f, 0.06f)); }
                case 3: return Shell(x, y);
                case 4: return Mathf.Min(HourglassGlass(x, y), Mathf.Min(RoundBox(x, y, 0f, 0.7f, 0.56f, 0.1f, 0.06f), RoundBox(x, y, 0f, -0.7f, 0.56f, 0.1f, 0.06f)));
                case 5: return Mathf.Min(Circle(x, y, 0f, -0.05f, 0.72f), Circle(x, y, 0f, 0.74f, 0.12f));
                case 6: return Lantern(x, y);
                case 7: return Crown(x, y);
                case 8: return Mathf.Min(Mathf.Min(Ellipse(x, y, 0f, -0.2f, 0.45f, 0.56f), AcornCap(x, y)), Capsule(x, y, 0f, 0.42f, 0.08f, 0.72f, 0.06f));
                case 9: return RoundBox(x, y, 0f, 0f, 0.6f, 0.74f, 0.1f);
                case 10: return Mathf.Min(Mathf.Min(CupBowl(x, y), Capsule(x, y, 0f, 0.05f, 0f, -0.5f, 0.09f)), Ellipse(x, y, 0f, -0.6f, 0.42f, 0.12f));
                case 11: return Gem(x, y);
                case 12: return Mathf.Min(MushroomCap(x, y), RoundBox(x, y, 0f, -0.38f, 0.22f, 0.38f, 0.14f));
                case 13: return Anchor(x, y);
                case 14: return Star(x, y, 0f, -0.04f, 5, 0.86f, 2.4f, 0.12f);
                default: return Umbrella(x, y);
            }
        }

        private static float Key(float x, float y) =>
            Mathf.Min(Mathf.Min(Mathf.Abs(Circle(x, y, -0.5f, 0f, 0.28f)) - 0.12f, Capsule(x, y, -0.22f, 0f, 0.78f, 0f, 0.1f)),
                      Mathf.Min(RoundBox(x, y, 0.5f, -0.16f, 0.06f, 0.12f, 0.03f), RoundBox(x, y, 0.7f, -0.2f, 0.07f, 0.16f, 0.03f)));

        private static float Bell(float x, float y)
        {
            float dome = Circle(x, y, 0f, 0.24f, 0.36f);
            float skirt = Polygon(x, y, new[] { -0.36f, 0.26f, 0.36f, 0.26f, 0.58f, -0.4f, -0.58f, -0.4f });
            float rim = RoundBox(x, y, 0f, -0.44f, 0.66f, 0.1f, 0.08f);
            float loop = Mathf.Abs(Circle(x, y, 0f, 0.68f, 0.13f)) - 0.05f;
            return Mathf.Min(Mathf.Min(Mathf.Min(dome, skirt), rim), Mathf.Min(loop, Circle(x, y, 0f, -0.64f, 0.13f)));
        }

        private static float FeatherVane(float x, float y)
        {
            float vane = Ellipse(x, y, 0f, 0.12f, 0.3f, 0.76f);
            // Muescas del borde (una a cada lado): lo hacen pluma y no hoja.
            float notches = Mathf.Min(Circle(x, y, 0.36f, 0.22f, 0.12f), Circle(x, y, -0.36f, -0.12f, 0.12f));
            return Mathf.Max(vane, -notches);
        }

        private static float Shell(float x, float y)
        {
            float fan = Mathf.Max(Circle(x, y, 0f, -0.42f, 0.9f), -(y + 0.42f));
            // Borde ondulado: festones sobre el arco.
            float scallops = 99f;
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.PI * (0.12f + 0.76f * i / 6f);
                scallops = Mathf.Min(scallops, Circle(x, y, 0.84f * Mathf.Cos(a), -0.42f + 0.84f * Mathf.Sin(a), 0.16f));
            }
            float hinge = RoundBox(x, y, 0f, -0.52f, 0.26f, 0.12f, 0.06f);
            return Mathf.Min(Mathf.Min(Mathf.Max(fan, Circle(x, y, 0f, -0.42f, 0.84f)), scallops), hinge);
        }

        private static float HourglassGlass(float x, float y) =>
            Polygon(x, y, new[] { -0.42f, 0.62f, 0.42f, 0.62f, 0.09f, 0f, 0.42f, -0.62f, -0.42f, -0.62f, -0.09f, 0f });

        private static float Lantern(float x, float y)
        {
            float body = RoundBox(x, y, 0f, -0.08f, 0.38f, 0.46f, 0.1f);
            float cap = Polygon(x, y, new[] { -0.46f, 0.36f, 0.46f, 0.36f, 0.2f, 0.58f, -0.2f, 0.58f });
            float handle = Mathf.Max(Mathf.Abs(Circle(x, y, 0f, 0.62f, 0.22f)) - 0.05f, -(y - 0.62f));
            float foot = RoundBox(x, y, 0f, -0.6f, 0.46f, 0.08f, 0.05f);
            return Mathf.Min(Mathf.Min(body, cap), Mathf.Min(handle, foot));
        }

        private static float Crown(float x, float y)
        {
            float shape = Polygon(x, y, new[] { -0.72f, -0.42f, 0.72f, -0.42f, 0.78f, 0.36f, 0.4f, 0.04f, 0f, 0.5f, -0.4f, 0.04f, -0.78f, 0.36f });
            float tips = Mathf.Min(Mathf.Min(Circle(x, y, -0.78f, 0.42f, 0.1f), Circle(x, y, 0.78f, 0.42f, 0.1f)), Circle(x, y, 0f, 0.58f, 0.11f));
            return Mathf.Min(shape, tips);
        }

        private static float AcornCap(float x, float y) => Mathf.Max(Ellipse(x, y, 0f, 0.22f, 0.56f, 0.3f), -(y - 0.1f));

        private static float CupBowl(float x, float y) => Mathf.Max(Circle(x, y, 0f, 0.42f, 0.56f), y - 0.42f);

        private static float Gem(float x, float y) =>
            Polygon(x, y, new[] { -0.66f, 0.24f, -0.34f, 0.56f, 0.34f, 0.56f, 0.66f, 0.24f, 0f, -0.7f });

        private static float MushroomCap(float x, float y) => Mathf.Max(Ellipse(x, y, 0f, 0.02f, 0.78f, 0.66f), -(y - 0.02f));

        private static float Anchor(float x, float y)
        {
            float ring = Mathf.Abs(Circle(x, y, 0f, 0.66f, 0.15f)) - 0.055f;
            float shaft = Capsule(x, y, 0f, 0.5f, 0f, -0.62f, 0.08f);
            float bar = Capsule(x, y, -0.36f, 0.3f, 0.36f, 0.3f, 0.07f);
            float arc = Mathf.Max(Mathf.Abs(Circle(x, y, 0f, -0.12f, 0.58f)) - 0.08f, y + 0.12f);
            float flukes = Mathf.Min(Polygon(x, y, new[] { -0.72f, -0.02f, -0.46f, -0.2f, -0.66f, -0.26f }),
                                     Polygon(x, y, new[] { 0.72f, -0.02f, 0.66f, -0.26f, 0.46f, -0.2f }));
            return Mathf.Min(Mathf.Min(Mathf.Min(ring, shaft), Mathf.Min(bar, arc)), flukes);
        }

        private static float UmbrellaCanopy(float x, float y)
        {
            float half = Mathf.Max(Circle(x, y, 0f, 0f, 0.84f), -y);
            float bites = Mathf.Min(Mathf.Min(Circle(x, y, -0.56f, -0.02f, 0.28f), Circle(x, y, 0f, -0.02f, 0.28f)), Circle(x, y, 0.56f, -0.02f, 0.28f));
            return Mathf.Max(half, -bites);
        }

        private static float Umbrella(float x, float y)
        {
            float handle = Capsule(x, y, 0f, 0.1f, 0f, -0.62f, 0.055f);
            float hook = Mathf.Max(Mathf.Abs(Circle(x, y, 0.16f, -0.62f, 0.16f)) - 0.055f, y + 0.62f);
            return Mathf.Min(Mathf.Min(UmbrellaCanopy(x, y), handle), Mathf.Min(hook, Circle(x, y, 0f, 0.86f, 0.07f)));
        }

        // ------------------------------------------------------------------ pintura

        private static void Fill(ref Px p, Color c, float sdf) => p.Over(c, Cover(sdf, Aa));
        private static void InkLine(ref Px p, float sdf, float w, float alpha = 1f) => p.Over(WithAlpha(ClayRaster.Ink, alpha), Cover(Mathf.Abs(sdf) - w, Aa));
        private static void Gloss(ref Px p, float x, float y, float cx, float cy, float rx, float ry, float inside) =>
            p.Over(new Color(1f, 1f, 1f, 0.45f), Cover(Ellipse(x, y, cx, cy, rx, ry), Aa) * Cover(inside, Aa));

        private static void Paint(int id, ref Px p, float x, float y)
        {
            float body = Body(id, x, y);
            switch (id)
            {
                case 0: // llave
                {
                    float kx = Rot(x, y, -35f, out float ky);
                    Fill(ref p, Sun, body);
                    Fill(ref p, Shade(Sun, 0.82f), Mathf.Max(body, -(ky + 0.02f)));
                    Gloss(ref p, kx, ky, -0.58f, 0.14f, 0.1f, 0.05f, body);
                    break;
                }
                case 1: // campana
                    Fill(ref p, Orange, body);
                    Fill(ref p, Shade(Orange, 0.8f), Mathf.Max(body, x - 0.1f) + 0.0f * y);
                    Fill(ref p, Sun, RoundBox(x, y, 0f, -0.44f, 0.66f, 0.1f, 0.08f));
                    Fill(ref p, Shade(Sun, 0.7f), Circle(x, y, 0f, -0.64f, 0.13f));
                    Gloss(ref p, x, y, -0.16f, 0.36f, 0.12f, 0.07f, body);
                    break;
                case 2: // pluma
                {
                    float fx = Rot(x, y, -38f, out float fy);
                    float vane = FeatherVane(fx, fy);
                    Fill(ref p, Grape, vane);
                    Fill(ref p, Tint(Grape, 0.35f), Mathf.Max(vane, fx));
                    Fill(ref p, Cream, Capsule(fx, fy, 0f, -0.9f, 0f, 0.1f, 0.06f) + 0.0f);
                    InkLine(ref p, Capsule(fx, fy, 0f, -0.55f, 0f, 0.8f, 0f), 0.022f, 0.8f);
                    break;
                }
                case 3: // concha
                {
                    Fill(ref p, Pink, body);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = Mathf.PI * (0.22f + 0.56f * i / 4f);
                        float rib = Capsule(x, y, 0f, -0.42f, 0.78f * Mathf.Cos(a), -0.42f + 0.78f * Mathf.Sin(a), 0f);
                        p.Over(WithAlpha(ClayRaster.Ink, 0.55f), Cover(rib - 0.022f, Aa) * Cover(body + 0.04f, Aa));
                    }
                    Fill(ref p, Shade(Pink, 0.8f), RoundBox(x, y, 0f, -0.52f, 0.26f, 0.12f, 0.06f));
                    Gloss(ref p, x, y, -0.3f, 0.12f, 0.14f, 0.06f, body);
                    break;
                }
                case 4: // reloj de arena
                {
                    float glass = HourglassGlass(x, y);
                    Fill(ref p, Tint(Sky, 0.55f), glass);
                    Fill(ref p, Sun, Mathf.Max(glass, y + 0.22f));                // arena abajo
                    Fill(ref p, Sun, Mathf.Max(glass, Mathf.Max(-(y - 0.26f), y - 0.5f) + 0f)); // arena arriba (queda poca)
                    InkLine(ref p, glass, 0.02f, 0.9f);
                    Fill(ref p, Brown, RoundBox(x, y, 0f, 0.7f, 0.56f, 0.1f, 0.06f));
                    Fill(ref p, Brown, RoundBox(x, y, 0f, -0.7f, 0.56f, 0.1f, 0.06f));
                    break;
                }
                case 5: // brújula
                {
                    float face = Circle(x, y, 0f, -0.05f, 0.72f);
                    Fill(ref p, Lime, body);
                    Fill(ref p, Cream, face + 0.12f);
                    float north = Polygon(x, y, new[] { 0f, 0.5f, 0.13f, -0.05f, -0.13f, -0.05f });
                    float south = Polygon(x, y, new[] { 0.13f, -0.05f, 0f, -0.6f, -0.13f, -0.05f });
                    Fill(ref p, Coral, north);
                    Fill(ref p, Steel, south);
                    Fill(ref p, ClayRaster.Ink, Circle(x, y, 0f, -0.05f, 0.06f));
                    Gloss(ref p, x, y, -0.3f, 0.28f, 0.12f, 0.06f, face + 0.12f);
                    break;
                }
                case 6: // farol
                {
                    Fill(ref p, Orange, body);
                    float window = RoundBox(x, y, 0f, -0.08f, 0.26f, 0.34f, 0.06f);
                    Fill(ref p, ClayRaster.Ink, window + 0.035f);
                    Fill(ref p, Tint(Sun, 0.35f), window);
                    Fill(ref p, Tint(Sun, 0.75f), Ellipse(x, y, 0f, -0.14f, 0.1f, 0.17f));
                    InkLine(ref p, Capsule(x, y, 0f, -0.42f, 0f, 0.26f, 0f), 0.025f);
                    break;
                }
                case 7: // corona
                    Fill(ref p, Sun, body);
                    Fill(ref p, Shade(Sun, 0.82f), RoundBox(x, y, 0f, -0.33f, 0.72f, 0.1f, 0.03f));
                    Fill(ref p, Coral, Circle(x, y, 0f, -0.1f, 0.1f));
                    Fill(ref p, Sky, Circle(x, y, -0.4f, -0.14f, 0.07f));
                    Fill(ref p, Sky, Circle(x, y, 0.4f, -0.14f, 0.07f));
                    Gloss(ref p, x, y, -0.4f, 0.14f, 0.1f, 0.05f, body);
                    break;
                case 8: // bellota
                {
                    float nut = Ellipse(x, y, 0f, -0.2f, 0.45f, 0.56f);
                    Fill(ref p, Orange, nut);
                    Gloss(ref p, x, y, -0.18f, -0.32f, 0.1f, 0.14f, nut);
                    float cap = AcornCap(x, y);
                    Fill(ref p, Brown, cap);
                    InkLine(ref p, Mathf.Abs(y - 0.2f) - 0.0f, 0.016f, 0.4f * Cover(cap + 0.04f, Aa));
                    Fill(ref p, Shade(Brown, 0.8f), Capsule(x, y, 0f, 0.42f, 0.08f, 0.72f, 0.06f));
                    break;
                }
                case 9: // libro
                    Fill(ref p, Sky, body);
                    Fill(ref p, Cream, RoundBox(x, y, 0.1f, -0.02f, 0.44f, 0.64f, 0.05f));
                    Fill(ref p, Shade(Sky, 0.78f), RoundBox(x, y, -0.5f, 0f, 0.1f, 0.74f, 0.06f));
                    for (int i = 0; i < 4; i++) InkLine(ref p, Capsule(x, y, -0.18f, 0.36f - 0.2f * i, 0.4f, 0.36f - 0.2f * i, 0f), 0.018f, 0.35f);
                    Fill(ref p, Coral, Polygon(x, y, new[] { 0.3f, 0.74f, 0.44f, 0.74f, 0.44f, 0.36f, 0.37f, 0.44f, 0.3f, 0.36f }));
                    break;
                case 10: // copa
                    Fill(ref p, Mint, body);
                    Fill(ref p, Shade(Mint, 0.8f), Mathf.Max(body, x - 0.12f));
                    Fill(ref p, Tint(Mint, 0.3f), RoundBox(x, y, 0f, 0.4f, 0.52f, 0.04f, 0.02f));
                    Gloss(ref p, x, y, -0.26f, 0.2f, 0.1f, 0.06f, body);
                    break;
                case 11: // gema
                    Fill(ref p, Sky, body);
                    Fill(ref p, Tint(Sky, 0.4f), Polygon(x, y, new[] { -0.34f, 0.56f, 0.34f, 0.56f, 0.2f, 0.24f, -0.2f, 0.24f }));
                    Fill(ref p, Shade(Sky, 0.8f), Polygon(x, y, new[] { 0.2f, 0.24f, 0.66f, 0.24f, 0f, -0.7f }));
                    InkLine(ref p, Capsule(x, y, -0.66f, 0.24f, 0.66f, 0.24f, 0f), 0.018f, 0.55f);
                    InkLine(ref p, Capsule(x, y, -0.2f, 0.24f, 0f, -0.7f, 0f), 0.018f, 0.45f);
                    InkLine(ref p, Capsule(x, y, 0.2f, 0.24f, 0f, -0.7f, 0f), 0.018f, 0.45f);
                    break;
                case 12: // hongo
                {
                    float cap = MushroomCap(x, y);
                    Fill(ref p, Cream, RoundBox(x, y, 0f, -0.38f, 0.22f, 0.38f, 0.14f));
                    Fill(ref p, Coral, cap);
                    Fill(ref p, Cream, Circle(x, y, -0.36f, 0.26f, 0.12f));
                    Fill(ref p, Cream, Circle(x, y, 0.12f, 0.46f, 0.1f));
                    Fill(ref p, Cream, Circle(x, y, 0.44f, 0.18f, 0.09f));
                    break;
                }
                case 13: // ancla
                    Fill(ref p, Grape, body);
                    Fill(ref p, Tint(Grape, 0.3f), Mathf.Max(body, -(x + 0.02f)) + 0.03f);
                    break;
                case 14: // estrella de mar
                    Fill(ref p, Orange, body);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = Mathf.PI / 2f + i * 2f * Mathf.PI / 5f;
                        for (int k = 1; k <= 2; k++)
                            Fill(ref p, Tint(Orange, 0.55f), Circle(x, y, 0.24f * k * Mathf.Cos(a), -0.04f + 0.24f * k * Mathf.Sin(a), 0.055f));
                    }
                    Fill(ref p, Tint(Orange, 0.55f), Circle(x, y, 0f, -0.04f, 0.07f));
                    break;
                default: // paraguas
                {
                    float canopy = UmbrellaCanopy(x, y);
                    Fill(ref p, Brown, body);
                    Fill(ref p, Lime, canopy);
                    float stripe = Mathf.Min(Mathf.Abs(x + 0.28f), Mathf.Abs(x - 0.28f)) - 0.1f;
                    Fill(ref p, Tint(Lime, 0.45f), Mathf.Max(canopy, stripe));
                    Fill(ref p, Lime, Circle(x, y, 0f, 0.86f, 0.07f));
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ sonda

        private static float ProbeBody(float x, float y) =>
            Mathf.Min(Mathf.Min(Circle(x, y, 0f, 0f, 0.46f), Mathf.Min(RoundBox(x, y, -0.72f, 0f, 0.22f, 0.16f, 0.04f), RoundBox(x, y, 0.72f, 0f, 0.22f, 0.16f, 0.04f))),
                      Mathf.Min(Capsule(x, y, 0f, 0.4f, 0f, 0.7f, 0.035f), Circle(x, y, 0f, 0.74f, 0.07f)));

        public static Color32[] RenderProbe(int size) => RenderClay(size, 1.12f, 0.07f, 0.08f, 0.025f, ProbeBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.025f;
            p.Over(Sky, Cover(Mathf.Min(RoundBox(x, y, -0.72f, 0f, 0.22f, 0.16f, 0.04f), RoundBox(x, y, 0.72f, 0f, 0.22f, 0.16f, 0.04f)), aa));
            p.Over(WithAlpha(ClayRaster.Ink, 0.5f), Cover(Mathf.Min(Mathf.Abs(x + 0.72f), Mathf.Abs(x - 0.72f)) - 0.02f, aa) * Cover(Mathf.Abs(y) - 0.16f, aa));
            p.Over(Cream, Cover(Circle(x, y, 0f, 0f, 0.46f), aa));
            p.Over(ClayRaster.Ink, Cover(Circle(x, y, 0f, 0.02f, 0.24f), aa));
            p.Over(Sky, Cover(Circle(x, y, 0f, 0.02f, 0.19f), aa));
            p.Over(new Color(1f, 1f, 1f, 0.8f), Cover(Circle(x, y, -0.07f, 0.09f, 0.06f), aa));
            p.Over(Cream, Cover(Capsule(x, y, 0f, 0.4f, 0f, 0.7f, 0.035f), aa));
            p.Over(Coral, Cover(Circle(x, y, 0f, 0.74f, 0.07f), aa));
        });
    }
}
