using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Piloto.Tests
{
    /// <summary>
    /// Piloto Estelar en plasticina (Tarea 95): el arte de Blender cumple lo que prometió. Cada señal conserva la huella del procedural al que reemplaza, las «parecidas» (misma forma y mismo color) se distinguen por su detalle de tinta (grande, plano y de alto contraste),
    /// las formas y la nave contrastan con el cielo, y sin el atlas todo sigue funcionando con el arte de siempre. Las medidas con números (para el informe) las saca <c>tools/juegos/mide_piloto.py</c>.
    /// </summary>
    public sealed class PilotPlasticTests
    {
        private const int Px = 192;
        private static readonly string[] Shapes = { "hexagono", "gota", "circulo", "cuadrado", "triangulo" };
        private static readonly string[] Details = { "punto", "anillo", "franja" };

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        private static Texture2D ReadAtlas()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", "piloto", "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        private static Color32[] Pixels(Texture2D atlas, string sprite, int expectedWidth)
        {
            var e = PlasticArt.Group("piloto").Entry(sprite);
            Assert.IsNotNull(e, "falta el sprite «" + sprite + "» en el atlas de Piloto");
            Assert.AreEqual(expectedWidth, e.w, sprite);
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

        private static void Box(Color32[] px, int size, out int minX, out int maxX, out int minY, out int maxY, out int area)
        {
            minX = size; maxX = 0; minY = size; maxY = 0; area = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (px[y * size + x].a >= 250) { area++; minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        }

        [Test]
        public void PlasticSignals_ExistInBothSizes_AndKeepTheFootprintOfTheProceduralOnes()
        {
            Assert.IsTrue(PilotSignalSprites.Plastic, "falta el atlas de plasticina de Piloto");
            var atlas = ReadAtlas();
            try
            {
                for (int s = 0; s < 5; s++)
                    for (int d = 0; d < 3; d++)
                    {
                        var plastic = Pixels(atlas, "senal_" + Shapes[s] + "_" + Details[d], Px);
                        Pixels(atlas, "chica_" + Shapes[s] + "_" + Details[d], 96);
                        var old = PilotSignalSprites.Render((SignalShape)s, (SignalDetail)d, Px);
                        Box(plastic, Px, out int pMinX, out int pMaxX, out int pMinY, out int pMaxY, out int pArea);
                        Box(old, Px, out int oMinX, out int oMaxX, out int oMinY, out int oMaxY, out int oArea);
                        string name = Shapes[s] + " " + Details[d];
                        float ratio = pArea / (float)oArea;
                        Assert.That(ratio, Is.InRange(0.85f, 1.15f), name + ": la señal de plasticina ocupa " + (ratio * 100f).ToString("0") + " % de la de hoy (misma huella y mismo tamaño)");
                        Assert.AreEqual((oMinX + oMaxX) / 2f, (pMinX + pMaxX) / 2f, 6f, name + ": corrida a los lados");
                        Assert.AreEqual((oMinY + oMaxY) / 2f, (pMinY + pMaxY) / 2f, 8f, name + ": corrida hacia arriba o abajo");
                    }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void PlasticSignals_ContrastWithTheSky_AndTheirDetailWithTheBody()
        {
            var atlas = ReadAtlas();
            try
            {
                float sky = Luminance(new Color32(0x1C, 0x1B, 0x52, 255));          // el tono más claro del cielo (NeuroStyle.NightBottom, el de los tres del fondo de la app): el peor caso para el contraste
                for (int s = 0; s < 5; s++)
                    for (int d = 0; d < 3; d++)
                    {
                        var px = Pixels(atlas, "senal_" + Shapes[s] + "_" + Details[d], Px);
                        double body = 0, ink = 0; int nBody = 0, nInk = 0;
                        foreach (var p in px)
                        {
                            if (p.a < 250) continue;
                            float l = Luminance(p);
                            if (l < 0.03f) { ink += l; nInk++; }
                            else if (l > 0.08f) { body += l; nBody++; }
                        }
                        string name = Shapes[s] + " " + Details[d];
                        Assert.Greater(nBody, 6000, name + ": cuerpo de la señal");
                        Assert.Greater(nInk, 1000, name + ": el detalle de tinta se ve (" + nInk + " píxeles; el punto, el más chico, tiene ~1350)");
                        float bodyL = (float)(body / nBody), inkL = (float)(ink / nInk);
                        Assert.GreaterOrEqual(Contrast(bodyL, sky), 3f, name + ": la forma contra el cielo");
                        Assert.GreaterOrEqual(Contrast(bodyL, inkL), 4.5f, name + ": el detalle contra el cuerpo (lo que separa a las parecidas)");
                    }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void PlasticLookalikes_DifferByTheirDetail_AndKeepTheColorOfTheirShape()
        {
            var atlas = ReadAtlas();
            try
            {
                for (int s = 0; s < 5; s++)
                {
                    var dot = Pixels(atlas, "senal_" + Shapes[s] + "_punto", Px);
                    var ring = Pixels(atlas, "senal_" + Shapes[s] + "_anillo", Px);
                    var stripe = Pixels(atlas, "senal_" + Shapes[s] + "_franja", Px);
                    var pairs = new[] { (dot, ring, "punto/anillo"), (dot, stripe, "punto/franja"), (ring, stripe, "anillo/franja") };
                    foreach (var pair in pairs)
                    {
                        int both = 0, differ = 0;
                        for (int i = 0; i < pair.Item1.Length; i++)
                        {
                            if (pair.Item1[i].a < 250 || pair.Item2[i].a < 250) continue;
                            both++;
                            if (Mathf.Abs(Luminance(pair.Item1[i]) - Luminance(pair.Item2[i])) > 0.2f) differ++;
                        }
                        Assert.GreaterOrEqual(differ / (float)both, 0.06f, Shapes[s] + " " + pair.Item3 + ": las parecidas casi no se distinguen (" + (differ * 100f / both).ToString("0.0") + " % de la forma)");
                    }
                    // el color del cuerpo es el MISMO en las tres (el detalle es lo único que cambia)
                    Color MeanBody(Color32[] px)
                    {
                        double r = 0, g = 0, b = 0; int n = 0;
                        foreach (var p in px) { if (p.a >= 250 && Luminance(p) > 0.1f) { r += p.r; g += p.g; b += p.b; n++; } }
                        return new Color((float)(r / n) / 255f, (float)(g / n) / 255f, (float)(b / n) / 255f);
                    }
                    var a = MeanBody(dot); var b2 = MeanBody(ring); var c = MeanBody(stripe);
                    Assert.AreEqual(a.r, b2.r, 0.03f, Shapes[s] + ": el color del cuerpo cambia con el detalle");
                    Assert.AreEqual(a.g, c.g, 0.03f, Shapes[s]);
                    Assert.AreEqual(a.b, c.b, 0.03f, Shapes[s]);
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void ThePlasticShip_KeepsTheFootprintOfTheProceduralOne_AndContrastsWithTheSky()
        {
            var atlas = ReadAtlas();
            try
            {
                var plastic = Pixels(atlas, "nave", Px);
                var old = PilotShipSprite.Render(Px);
                Box(plastic, Px, out int pMinX, out int pMaxX, out int pMinY, out int pMaxY, out int pArea);
                Box(old, Px, out int oMinX, out int oMaxX, out int oMinY, out int oMaxY, out int oArea);
                float ratio = pArea / (float)oArea;
                Assert.That(ratio, Is.InRange(0.8f, 1.25f), "la nave de plasticina ocupa " + (ratio * 100f).ToString("0") + " % de la de hoy");
                Assert.AreEqual((oMinX + oMaxX) / 2f, (pMinX + pMaxX) / 2f, 8f, "nave: corrida a los lados");
                Assert.AreEqual((oMinY + oMaxY) / 2f, (pMinY + pMaxY) / 2f, 10f, "nave: corrida hacia arriba o abajo");
                double l = 0; int n = 0;
                foreach (var p in plastic) if (p.a >= 250 && Luminance(p) > 0.4f) { l += Luminance(p); n++; }
                Assert.Greater(n, 1500, "el casco crema de la nave");
                Assert.GreaterOrEqual(Contrast((float)(l / n), Luminance(new Color32(0x0A, 0x0F, 0x33, 255))), 3f, "el casco contra el cielo");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void ThePressedSignals_ExistForAll15_AndOnlyWithThePlasticAtlas()
        {
            // Tarea 104 (regla del 14-oct): la señal que la persona toca se ve presionada (aplastada, un punto más oscura, con la huella de un dedo)
            Assert.IsTrue(PilotSignalSprites.HasPressed, "faltan las señales presionadas en el atlas");
            foreach (SignalShape shape in System.Enum.GetValues(typeof(SignalShape)))
                foreach (SignalDetail detail in System.Enum.GetValues(typeof(SignalDetail)))
                {
                    var pressed = PilotSignalSprites.GetPressed(shape, detail);
                    Assert.IsNotNull(pressed, "falta la señal presionada " + shape + " " + detail);
                    Assert.AreNotEqual(PilotSignalSprites.Get(shape, detail), pressed);
                }
            PlasticArt.Disabled = true;
            Assert.IsFalse(PilotSignalSprites.HasPressed);
            Assert.IsNull(PilotSignalSprites.GetPressed(SignalShape.Hexagon, SignalDetail.Dot), "sin plasticina el juego deja la señal de siempre");
        }

        [Test]
        public void WithoutThePlasticAtlas_PilotKeepsItsProceduralArt()
        {
            PlasticArt.Disabled = true;
            Assert.IsFalse(PilotSignalSprites.Plastic);
            foreach (SignalShape shape in System.Enum.GetValues(typeof(SignalShape)))
                foreach (SignalDetail detail in System.Enum.GetValues(typeof(SignalDetail)))
                {
                    Assert.IsNotNull(PilotSignalSprites.Get(shape, detail));
                    Assert.IsNotNull(PilotSignalSprites.Get(shape, detail, small: true), "sin plasticina la versión chica es la misma procedural");
                }
            Assert.IsNotNull(PilotShipSprite.Get());
        }
    }
}
