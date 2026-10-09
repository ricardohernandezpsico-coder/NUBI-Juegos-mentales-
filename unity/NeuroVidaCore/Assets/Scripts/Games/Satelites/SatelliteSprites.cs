using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Satelites
{
    /// <summary>
    /// Arte de «Satélites: enciende tu planeta» con el pincel de arcilla común (<see cref="ClayRaster"/>): el planeta a oscuras con sus continentes tenues, el satélite (disco con dos paneles; todos IGUALES: la señal es el aro dorado y el sobre,
    /// no el dibujo), el sobre del mensaje, la nube de polvo, el aro punteado del que faltó y el medio disco de las rondas parciales. Se hornea una vez (durante la cuenta regresiva) y se reutiliza. Todo plano.
    /// </summary>
    public static class SatelliteSprites
    {
        private const float Aa = 0.03f;

        /// <summary>El paso suave entre dos bordes (como el <c>smoothstep</c> de los sombreadores): 0 antes de <paramref name="e0"/>, 1 después de <paramref name="e1"/>. (<c>Mathf.SmoothStep</c> de Unity NO es esto: interpola entre dos valores.)</summary>
        private static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        private static readonly Color BodyFill = Hex(0xE9E6FF);
        private static readonly Color BodyCore = Hex(0x5A5296);
        private static readonly Color PanelFill = Hex(0x7FB6FF);
        private static readonly Color EnvelopeFill = Hex(0xFFF4D6);

        private static Sprite _body, _planet, _envelope, _cloud, _dashed, _half;

        /// <summary>Del radio del disco del satélite al lado del sprite (dp de lado = 14 × esto): el dibujo trae el borde, la sombra y los paneles (9 dp a cada lado).</summary>
        public const float BodySpriteSideInDiscRadii = 4.55f;
        /// <summary>Lo mismo para el planeta: lado = radio del planeta × esto.</summary>
        public const float PlanetSpriteSideInRadii = 2f * 1.14f / 0.9f;

        public static Sprite Body() => _body != null ? _body : _body = ToSprite(RenderBody(128), 128, 128);
        public static Sprite Planet() => _planet != null ? _planet : _planet = ToSprite(RenderPlanet(256), 256, 256);
        public static Sprite Envelope() => _envelope != null ? _envelope : _envelope = ToSprite(RenderEnvelope(96), 96, 96);
        public static Sprite Cloud() => _cloud != null ? _cloud : _cloud = ToSprite(RenderCloud(256), 256, 256);
        /// <summary>Aro punteado (blanco: se tiñe con <c>Image.color</c>): el satélite con mensaje que no se marcó.</summary>
        public static Sprite DashedRing() => _dashed != null ? _dashed : _dashed = ToSprite(RenderDashed(256), 256, 256);
        /// <summary>El medio disco (la mitad de la izquierda) de una ronda parcial en la fila de rondas; blanco.</summary>
        public static Sprite HalfDisc() => _half != null ? _half : _half = ToSprite(RenderHalf(96), 96, 96);

        /// <summary>Hornea todos los sprites, de a uno por cuadro (durante la cuenta regresiva).</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            Body(); Planet();
            yield return null;
            Envelope(); Cloud();
            yield return null;
            DashedRing(); HalfDisc();
        }

        // ------------------------------------------------------------------ el satélite

        private const float BodyZoom = 1.25f, BodyDiscR = 0.55f;

        private static float BodyShape(float x, float y)
        {
            float disc = Circle(x, y, 0f, 0f, BodyDiscR);
            float left = RoundBox(x, y, -0.76f, 0f, 0.18f, 0.15f, 0.05f);
            float right = RoundBox(x, y, 0.76f, 0f, 0.18f, 0.15f, 0.05f);
            float arm = RoundBox(x, y, 0f, 0f, 0.7f, 0.05f, 0.025f);
            return Union(Union(disc, arm), Union(left, right));
        }

        public static Color32[] RenderBody(int size) => RenderClay(size, BodyZoom, 0.06f, 0.07f, Aa, BodyShape, (ref Px p, float x, float y) =>
        {
            p.Over(Hex(0x9C94D6), Cover(RoundBox(x, y, 0f, 0f, 0.7f, 0.05f, 0.025f) + 0.025f, Aa));
            p.Over(PanelFill, Cover(RoundBox(x, y, -0.76f, 0f, 0.18f, 0.15f, 0.05f) + 0.04f, Aa));
            p.Over(PanelFill, Cover(RoundBox(x, y, 0.76f, 0f, 0.18f, 0.15f, 0.05f) + 0.04f, Aa));
            p.Over(Shade(PanelFill, 0.7f), Cover(Mathf.Abs(x + 0.76f) - 0.012f, Aa) * Cover(RoundBox(x, y, -0.76f, 0f, 0.18f, 0.15f, 0.05f) + 0.05f, Aa));
            p.Over(Shade(PanelFill, 0.7f), Cover(Mathf.Abs(x - 0.76f) - 0.012f, Aa) * Cover(RoundBox(x, y, 0.76f, 0f, 0.18f, 0.15f, 0.05f) + 0.05f, Aa));
            float disc = Circle(x, y, 0f, 0f, BodyDiscR);
            p.Over(BodyFill, Cover(disc + 0.05f, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Ellipse(x, y, -0.18f, 0.22f, 0.18f, 0.09f), Aa) * Cover(disc + 0.08f, Aa));
            p.Over(BodyCore, Cover(Circle(x, y, 0f, 0f, 0.2f), Aa));
        });

        // ------------------------------------------------------------------ el planeta

        private const float PlanetZoom = 1.14f, PlanetR = 0.9f;

        public static Color32[] RenderPlanet(int size) => RenderClay(size, PlanetZoom, 0.05f, 0.05f, Aa, (x, y) => Circle(x, y, 0f, 0f, PlanetR), (ref Px p, float x, float y) =>
        {
            float disc = Circle(x, y, 0f, 0f, PlanetR);
            // degradé de la noche: más claro arriba a la izquierda, muy oscuro abajo a la derecha
            float d = Mathf.Clamp01(Mathf.Sqrt((x + 0.28f) * (x + 0.28f) + (y - 0.32f) * (y - 0.32f)) / 1.5f);
            Color baseC = Color.Lerp(Hex(0x3B3F86), Hex(0x151843), Smooth(0f, 1f, d));
            p.Over(baseC, Cover(disc + 0.03f, Aa));
            // continentes tenues
            Color land = new Color(0.47f, 0.51f, 0.82f, 0.22f);
            float[] cx = { -0.30f, 0.26f, 0.38f, -0.12f }, cy = { 0.16f, -0.20f, 0.38f, -0.42f }, rx = { 0.34f, 0.28f, 0.17f, 0.20f }, ry = { 0.24f, 0.19f, 0.12f, 0.13f };
            float cont = float.MaxValue;
            for (int i = 0; i < 4; i++) cont = Mathf.Min(cont, Ellipse(x, y, cx[i], cy[i], rx[i], ry[i]));
            p.Over(land, Cover(cont, Aa) * Cover(disc + 0.06f, Aa));
            // brillo suave
            p.Over(new Color(1f, 1f, 1f, 0.12f), Cover(Ellipse(x, y, -0.34f, 0.42f, 0.28f, 0.14f), Aa * 3f) * Cover(disc + 0.1f, Aa));
        });

        // ------------------------------------------------------------------ el sobre

        public static Color32[] RenderEnvelope(int size) => RenderClay(size, 1.3f, 0.07f, 0.06f, Aa, (x, y) => RoundBox(x, y, 0f, 0f, 0.78f, 0.55f, 0.1f), (ref Px p, float x, float y) =>
        {
            float box = RoundBox(x, y, 0f, 0f, 0.78f, 0.55f, 0.1f);
            p.Over(EnvelopeFill, Cover(box + 0.05f, Aa));
            // la solapa: una V desde las esquinas de arriba (y crece hacia arriba: fila 0 = abajo)
            float flap = Mathf.Min(Capsule(x, y, -0.7f, 0.42f, 0f, -0.05f, 0.04f), Capsule(x, y, 0.7f, 0.42f, 0f, -0.05f, 0.04f));
            p.Over(Ink, Cover(flap, Aa) * Cover(box + 0.1f, Aa));
        });

        // ------------------------------------------------------------------ la nube de polvo (plana y opaca)

        public static Color32[] RenderCloud(int size)
        {
            var pixels = new Color32[size * size];
            // cinco bultos que se solapan (los del boceto), en una caja de 2 × 2 unidades: la silueta es la unión de los cinco círculos
            float[] bx = { -0.34f, 0f, 0.34f, -0.16f, 0.18f }, by = { -0.06f, 0.16f, -0.04f, -0.22f, -0.2f }, br = { 0.44f, 0.52f, 0.44f, 0.36f, 0.38f };
            var baseColor = new Color(74f / 255f, 64f / 255f, 138f / 255f);
            var rimColor = new Color(132f / 255f, 122f / 255f, 200f / 255f);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size * 2f - 1f;
                    float y = (py + 0.5f) / size * 2f - 1f;
                    float sdf = float.MaxValue;
                    for (int i = 0; i < 5; i++)
                        sdf = Mathf.Min(sdf, Mathf.Sqrt((x - bx[i]) * (x - bx[i]) + (y - by[i]) * (y - by[i])) - br[i]);
                    float cover = 1f - Smooth(-0.012f, 0.012f, sdf);
                    if (cover <= 0f) { pixels[py * size + px] = new Color32(0, 0, 0, 0); continue; }
                    var c = Color.Lerp(baseColor, rimColor, Smooth(-0.085f, -0.05f, sdf));          // un borde claro: se lee como una forma plana
                    float speck = Mathf.Sin(x * 41f + y * 17f) * Mathf.Sin(x * 23f - y * 37f);       // unas motas: polvo
                    if (speck > 0.93f) c = Color.Lerp(c, Color.white, 0.22f);
                    c.a = 0.97f * cover;
                    pixels[py * size + px] = c;
                }
            }
            return pixels;
        }

        // ------------------------------------------------------------------ aros

        public static Color32[] RenderDashed(int size)
        {
            var pixels = new Color32[size * size];
            const float outer = 0.47f, inner = 0.40f;
            const int dashes = 14;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size - 0.5f, y = (py + 0.5f) / size - 0.5f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    float ang = Mathf.Atan2(y, x) / (2f * Mathf.PI) + 0.5f;          // 0..1
                    float seg = Mathf.Repeat(ang * dashes, 1f);
                    float on = Smooth(0.05f, 0.12f, seg) * (1f - Smooth(0.56f, 0.63f, seg));
                    float ring = (1f - Smooth(outer - 0.012f, outer, r)) * Smooth(inner - 0.012f, inner, r);
                    pixels[py * size + px] = new Color(1f, 1f, 1f, ring * on);
                }
            }
            return pixels;
        }

        // ------------------------------------------------------------------ el medio disco de las rondas parciales

        public static Color32[] RenderHalf(int size)
        {
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size - 0.5f, y = (py + 0.5f) / size - 0.5f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    float disc = 1f - Smooth(0.46f, 0.485f, r);
                    float half = 1f - Smooth(-0.01f, 0.01f, x);           // solo la mitad de la izquierda
                    pixels[py * size + px] = new Color(1f, 1f, 1f, disc * half);
                }
            }
            return pixels;
        }
    }
}
