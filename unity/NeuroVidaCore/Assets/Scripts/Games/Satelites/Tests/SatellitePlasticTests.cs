using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Satelites.Tests
{
    /// <summary>
    /// Satélites en plasticina (Tarea 98): el arte de Blender cumple lo que prometió. Cada pieza conserva la huella del procedural al que reemplaza (mismo lienzo y misma escala: el controlador no cambia ninguna medida), el satélite es UN dibujo sin nada vertical sobre el brazo
    /// (sería una cruz) y contrasta con el cielo, el planeta sigue oscuro (las luces doradas son lo que resalta), la nube de polvo es OPACA (tapa de verdad a los satélites que pasan detrás) y sin el atlas todo sigue funcionando con el arte de siempre.
    /// Las medidas con números (para el informe) las saca <c>tools/juegos/mide_satelites.py</c>.
    /// </summary>
    public sealed class SatellitePlasticTests
    {
        private const int BodyPx = 192, PlanetPx = 512, EnvelopePx = 128, CloudPx = 448;
        private static readonly Color32 Sky = new Color32(0x05, 0x08, 0x1D, 255);
        private static readonly Color32 GoldLight = new Color32(0xFF, 0xE7, 0xA8, 255);       // el núcleo de las luces del planeta

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        private static Texture2D ReadAtlas()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", "satelites", "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        private static Color32[] Pixels(Texture2D atlas, string sprite, int expectedSize)
        {
            var e = PlasticArt.Group(SatelliteSprites.PlasticGroup).Entry(sprite);
            Assert.IsNotNull(e, "falta el sprite «" + sprite + "» en el atlas de Satélites");
            Assert.AreEqual(expectedSize, e.w, sprite + ": ancho");
            Assert.AreEqual(expectedSize, e.h, sprite + ": el lienzo es cuadrado, como el del procedural");
            var colors = atlas.GetPixels(e.x, e.y, e.w, e.h);
            var result = new Color32[colors.Length];
            for (int i = 0; i < colors.Length; i++) result[i] = colors[i];
            return result;
        }

        private static float Luminance(Color32 c)
        {
            float R(float v) { v /= 255f; return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f); }
            return 0.2126f * R(c.r) + 0.7152f * R(c.g) + 0.0722f * R(c.b);
        }

        private static float Contrast(float la, float lb) => (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);

        private static void Box(Color32[] px, int size, int alphaMin, out int minX, out int maxX, out int minY, out int maxY, out int area)
        {
            minX = size; maxX = 0; minY = size; maxY = 0; area = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (px[y * size + x].a >= alphaMin) { area++; minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        }

        private static Color32 Mean(Color32[] px, int size, float lo, float hi, float radius = 2f)
        {
            double r = 0, g = 0, b = 0; int n = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = px[y * size + x];
                    if (p.a < 250) continue;
                    float l = Luminance(p);
                    if (l < lo || l > hi) continue;
                    if (Mathf.Sqrt((x - size / 2f) * (x - size / 2f) + (y - size / 2f) * (y - size / 2f)) > radius * size) continue;
                    r += p.r; g += p.g; b += p.b; n++;
                }
            Assert.Greater(n, 100, "no hay píxeles de esa luminosidad (" + lo + " a " + hi + ")");
            return new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }

        private static void SameFootprint(string name, Color32[] plastic, Color32[] old, int size, int alphaMin, float shiftTolerancePx)
        {
            Box(plastic, size, alphaMin, out int pMinX, out int pMaxX, out int pMinY, out int pMaxY, out int pArea);
            Box(old, size, alphaMin, out int oMinX, out int oMaxX, out int oMinY, out int oMaxY, out int oArea);
            float ratio = pArea / (float)oArea;
            Assert.That(ratio, Is.InRange(0.8f, 1.25f), name + ": la pieza de plasticina ocupa " + (ratio * 100f).ToString("0") + " % de la de hoy (misma huella y mismo tamaño)");
            Assert.AreEqual((oMinX + oMaxX) / 2f, (pMinX + pMaxX) / 2f, shiftTolerancePx, name + ": corrida a los lados");
            Assert.AreEqual((oMinY + oMaxY) / 2f, (pMinY + pMaxY) / 2f, shiftTolerancePx, name + ": corrida hacia arriba o abajo");
        }

        [Test]
        public void PlasticPieces_KeepTheFootprintOfTheProceduralOnes()
        {
            Assert.IsTrue(SatelliteSprites.Plastic, "falta el atlas de plasticina de Satélites");
            var atlas = ReadAtlas();
            try
            {
                SameFootprint("satélite", Pixels(atlas, "satelite", BodyPx), SatelliteSprites.RenderBody(BodyPx), BodyPx, 250, 8f);
                SameFootprint("planeta", Pixels(atlas, "planeta", PlanetPx), SatelliteSprites.RenderPlanet(PlanetPx), PlanetPx, 250, 14f);
                SameFootprint("sobre", Pixels(atlas, "sobre", EnvelopePx), SatelliteSprites.RenderEnvelope(EnvelopePx), EnvelopePx, 250, 6f);
                SameFootprint("nube", Pixels(atlas, "nube", CloudPx), SatelliteSprites.RenderCloud(CloudPx), CloudPx, 200, 12f);          // la nube de hoy es de alfa 0,97: se cuenta desde 200
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheSatellite_IsOneDrawing_WithNothingVerticalOnTheArm_AndContrastsWithTheSky()
        {
            var atlas = ReadAtlas();
            try
            {
                var px = Pixels(atlas, "satelite", BodyPx);
                Box(px, BodyPx, 250, out int minX, out int maxX, out int minY, out int maxY, out _);
                float aspect = (maxY - minY + 1) / (float)(maxX - minX + 1);
                Assert.Less(aspect, 0.7f, "la silueta es mucho más ancha que alta (" + aspect.ToString("0.00") + "): un disco con brazo y paneles, sin antena vertical (sería una cruz)");
                Assert.IsNotNull(SatelliteSprites.Body());
                Assert.AreSame(SatelliteSprites.Body(), SatelliteSprites.Body(), "todos los satélites comparten la MISMA imagen (sin luz ni giro por instancia)");
                Assert.GreaterOrEqual(Contrast(Luminance(Mean(px, BodyPx, 0.55f, 1f, 0.2f)), Luminance(Sky)), 8f, "el disco contra el cielo");
                Assert.GreaterOrEqual(Contrast(Luminance(Mean(px, BodyPx, 0.25f, 0.55f)), Luminance(Sky)), 4f, "los paneles contra el cielo");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void ThePlanet_StaysDark_SoTheGoldenLightsStandOut()
        {
            var atlas = ReadAtlas();
            try
            {
                var px = Pixels(atlas, "planeta", PlanetPx);
                var mean = Mean(px, PlanetPx, 0f, 1f, 0.34f);                      // la cara del planeta (el radio es ~0,41 del lienzo)
                float l = Luminance(mean);
                Assert.Less(l, 0.08f, "el planeta debe ser oscuro (luminancia " + l.ToString("0.000") + "): las luces doradas son lo que resalta");
                Assert.Greater(l, 0.004f, "pero no negro: se ve contra el cielo");
                Assert.GreaterOrEqual(Contrast(Luminance(GoldLight), l), 7f, "las luces doradas sobre el planeta");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheDustCloud_IsOpaque_SoItReallyHidesTheSatellitesBehindIt()
        {
            var atlas = ReadAtlas();
            try
            {
                var px = Pixels(atlas, "nube", CloudPx);
                // el interior (a más de ~0,06 del borde de los cinco círculos) es 100 % opaco
                float[] bx = { -0.34f, 0f, 0.34f, -0.16f, 0.18f }, by = { -0.06f, 0.16f, -0.04f, -0.22f, -0.2f }, br = { 0.44f, 0.52f, 0.44f, 0.36f, 0.38f };
                int inside = 0, opaque = 0;
                for (int y = 0; y < CloudPx; y++)
                    for (int x = 0; x < CloudPx; x++)
                    {
                        float fx = (x + 0.5f) / CloudPx * 2f - 1f, fy = (y + 0.5f) / CloudPx * 2f - 1f;
                        float sdf = float.MaxValue;
                        for (int i = 0; i < 5; i++) sdf = Mathf.Min(sdf, Mathf.Sqrt((fx - bx[i]) * (fx - bx[i]) + (fy - by[i]) * (fy - by[i])) - br[i]);
                        if (sdf > -0.06f) continue;
                        inside++;
                        if (px[y * CloudPx + x].a >= 250) opaque++;
                    }
                Assert.Greater(inside, 40000, "el interior de la nube");
                Assert.GreaterOrEqual(opaque / (float)inside, 0.995f, "la nube debe tapar: " + (opaque * 100f / inside).ToString("0.0") + " % de su interior es opaco");
                Assert.GreaterOrEqual(Contrast(Luminance(Mean(px, CloudPx, 0.01f, 1f, 0.3f)), Luminance(Sky)), 1.8f, "la nube contra el cielo (la de hoy rinde ~2,3:1)");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheEnvelopeFlap_ReadsAgainstThePaper()
        {
            var atlas = ReadAtlas();
            try
            {
                var px = Pixels(atlas, "sobre", EnvelopePx);
                var paper = Mean(px, EnvelopePx, 0.6f, 1f);
                var ink = Mean(px, EnvelopePx, 0f, 0.04f);
                Assert.GreaterOrEqual(Contrast(Luminance(paper), Luminance(ink)), 7f, "la solapa de tinta contra el papel");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheSlabsOfTheCrossLabelAndTheSurpriseCard_Exist()
        {
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Chip, SlabTone.Night), "el rótulo «¿Aquí se cruzaron?»");
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Panel, SlabTone.Night) || PlasticSlab.Available(SlabSize.Button, SlabTone.Night), "el aviso de sorpresa");
        }

        [Test]
        public void ThePressedSatellite_IsAnotherImage_AndOnlyWithThePlasticAtlas()
        {
            // Tarea 104 (regla del 14-oct): el satélite que la persona toca al responder se ve presionado (aplastado, un punto más oscuro, con la huella de un dedo)
            Assert.IsNotNull(SatelliteSprites.BodyPressed(), "falta el satélite presionado en el atlas");
            Assert.AreNotEqual(SatelliteSprites.Body(), SatelliteSprites.BodyPressed());
            PlasticArt.Disabled = true;
            Assert.IsNull(SatelliteSprites.BodyPressed(), "sin plasticina el juego deja el satélite de siempre");
        }

        [Test]
        public void WithoutThePlasticAtlas_SatellitesKeepsItsProceduralArt()
        {
            PlasticArt.Disabled = true;
            Assert.IsFalse(SatelliteSprites.Plastic);
            Assert.IsNotNull(SatelliteSprites.Body());
            Assert.IsNotNull(SatelliteSprites.Planet());
            Assert.IsNotNull(SatelliteSprites.Envelope());
            Assert.IsNotNull(SatelliteSprites.Cloud());
            Assert.IsNotNull(SatelliteSprites.DashedRing());
            Assert.IsNotNull(SatelliteSprites.HalfDisc());
        }
    }
}
