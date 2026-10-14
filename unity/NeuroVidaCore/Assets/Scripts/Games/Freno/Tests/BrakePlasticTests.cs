using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Freno.Tests
{
    /// <summary>
    /// Freno de Emergencia en plasticina (Tarea 96): el arte de Blender cumple lo que prometió. Cada pieza conserva la huella del procedural al que reemplaza (mismo lienzo, misma escala: el controlador no cambia ninguna medida), la flecha se lee contra el botón (apagado y encendido
    /// se distinguen por luminosidad, no solo por color), los tres cohetes comparten el casco crema y se distinguen por el acento de su carril, el cohete CABE entre las torres de la plataforma, y sin el atlas todo sigue funcionando con el arte de siempre.
    /// Las medidas con números (para el informe) las saca <c>tools/juegos/mide_freno.py</c>.
    /// </summary>
    public sealed class BrakePlasticTests
    {
        private const int Button = 256, Rocket = 256, Pad = 320, Stop = 400;
        private static readonly Color32 Ground = new Color32(0x59, 0x58, 0x90, 255);     // el suelo de la luna (medido en la captura del juego)
        private static readonly Color32 Sky = new Color32(0x07, 0x0A, 0x28, 255);

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        private static Texture2D ReadAtlas()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", "freno", "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        private static Color32[] Pixels(Texture2D atlas, string sprite, int expectedSize)
        {
            var e = PlasticArt.Group(BrakeSprites.PlasticGroup).Entry(sprite);
            Assert.IsNotNull(e, "falta el sprite «" + sprite + "» en el atlas de Freno");
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

        private static void Box(Color32[] px, int size, out int minX, out int maxX, out int minY, out int maxY, out int area)
        {
            minX = size; maxX = 0; minY = size; maxY = 0; area = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (px[y * size + x].a >= 250) { area++; minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        }

        /// <summary>El color medio de los píxeles opacos cuya luminancia cae entre <paramref name="lo"/> y <paramref name="hi"/> (y, si se pide, dentro de <paramref name="radius"/> del centro, en fracción del lado).</summary>
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
            Assert.Greater(n, 200, "no hay píxeles de esa luminosidad (" + lo + " a " + hi + ")");
            return new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }

        private static void SameFootprint(string name, Color32[] plastic, Color32[] old, int size, float shiftTolerancePx)
        {
            Box(plastic, size, out int pMinX, out int pMaxX, out int pMinY, out int pMaxY, out int pArea);
            Box(old, size, out int oMinX, out int oMaxX, out int oMinY, out int oMaxY, out int oArea);
            float ratio = pArea / (float)oArea;
            Assert.That(ratio, Is.InRange(0.8f, 1.2f), name + ": la pieza de plasticina ocupa " + (ratio * 100f).ToString("0") + " % de la de hoy (misma huella y mismo tamaño)");
            Assert.AreEqual((oMinX + oMaxX) / 2f, (pMinX + pMaxX) / 2f, shiftTolerancePx, name + ": corrida a los lados");
            Assert.AreEqual((oMinY + oMaxY) / 2f, (pMinY + pMaxY) / 2f, shiftTolerancePx, name + ": corrida hacia arriba o abajo");
        }

        [Test]
        public void PlasticPieces_KeepTheFootprintOfTheProceduralOnes()
        {
            Assert.IsTrue(BrakeSprites.Plastic, "falta el atlas de plasticina de Freno");
            var atlas = ReadAtlas();
            try
            {
                SameFootprint("botón apagado", Pixels(atlas, "boton_apagado", Button), BrakeSprites.RenderButton(Button, false), Button, 10f);
                SameFootprint("botón encendido", Pixels(atlas, "boton_encendido", Button), BrakeSprites.RenderButton(Button, true), Button, 10f);
                SameFootprint("señal de alto", Pixels(atlas, "alto", Stop), BrakeSprites.RenderStopSign(Stop), Stop, 14f);
                SameFootprint("plataforma", Pixels(atlas, "plataforma", Pad), BrakeSprites.RenderLaunchPad(Pad), Pad, 12f);
            }
            finally { UnityEngine.Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheArrow_ReadsAgainstTheButton_AndLitAndUnlitDifferInLuminance()
        {
            var atlas = ReadAtlas();
            try
            {
                var off = Pixels(atlas, "boton_apagado", Button);
                var on = Pixels(atlas, "boton_encendido", Button);
                var offBody = Mean(off, Button, 0.01f, 0.3f, 0.45f);
                var offArrow = Mean(off, Button, 0.5f, 1f, 0.25f);
                var onBody = Mean(on, Button, 0.3f, 1f, 0.45f);
                var onArrow = Mean(on, Button, 0f, 0.03f, 0.25f);
                Assert.GreaterOrEqual(Contrast(Luminance(offArrow), Luminance(offBody)), 4.5f, "la flecha crema contra el botón apagado");
                Assert.GreaterOrEqual(Contrast(Luminance(onArrow), Luminance(onBody)), 4.5f, "la flecha de tinta contra el botón encendido");
                Assert.GreaterOrEqual(Contrast(Luminance(onBody), Luminance(offBody)), 3f, "encendido y apagado se distinguen por la luminosidad, no solo por el color");
                Assert.GreaterOrEqual(Contrast(Luminance(onBody), Luminance(Ground)), 3f, "el botón encendido contra el suelo de la luna");
                Assert.GreaterOrEqual(Contrast(Luminance(offBody), Luminance(Ground)), 1.5f, "el botón apagado contra el suelo de la luna (sin borde de tinta: lo separa la sombra de contacto)");
            }
            finally { UnityEngine.Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheStopSign_ContrastsWithTheSky_AndKeepsItsCreamRing()
        {
            var atlas = ReadAtlas();
            try
            {
                var px = Pixels(atlas, "alto", Stop);
                var body = Mean(px, Stop, 0.15f, 0.6f, 0.45f);
                var ring = Mean(px, Stop, 0.6f, 1f, 0.45f);
                Assert.GreaterOrEqual(Contrast(Luminance(body), Luminance(Sky)), 4.5f, "el octágono contra el cielo");
                Assert.GreaterOrEqual(Contrast(Luminance(ring), Luminance(body)), 1.5f, "el aro crema se ve sobre el coral");
                Assert.Greater(body.r, body.g + 40, "el cuerpo sigue siendo coral");
            }
            finally { UnityEngine.Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheThreeRockets_ShareTheCreamHull_AndDifferByTheLaneAccent()
        {
            var atlas = ReadAtlas();
            try
            {
                string[] names = { "cohete_coral", "cohete_cielo", "cohete_lima" };
                var accents = new Color32[3];
                var hulls = new Color32[3];
                for (int i = 0; i < 3; i++)
                {
                    var px = Pixels(atlas, names[i], Rocket);
                    Box(px, Rocket, out int minX, out int maxX, out int minY, out int maxY, out _);
                    float fill = (maxY - minY + 1) / (float)Rocket;
                    Assert.That(fill, Is.InRange(0.80f, 0.95f), names[i] + ": llena " + (fill * 100f).ToString("0") + " % del alto (el de hoy, ~89 %)");
                    Assert.AreEqual(Rocket / 2f, (minX + maxX) / 2f, 8f, names[i] + ": centrado a los lados");
                    hulls[i] = Mean(px, Rocket, 0.8f, 1f, 0.45f);                                              // el casco crema (el acento lima, de L ≈ 0,62, queda afuera)
                    Assert.GreaterOrEqual(Contrast(Luminance(hulls[i]), Luminance(Ground)), 4.5f, names[i] + ": el casco crema contra el suelo");
                    Assert.GreaterOrEqual(Contrast(Luminance(hulls[i]), Luminance(Sky)), 8f, names[i] + ": el casco crema contra el cielo");
                    // el acento: la punta (las filas de arriba del cuerpo)
                    double r = 0, g = 0, b = 0; int n = 0;
                    for (int y = maxY - 30; y <= maxY - 12; y++)
                        for (int x = 0; x < Rocket; x++)
                        {
                            var p = px[y * Rocket + x];
                            if (p.a < 250) continue;
                            r += p.r; g += p.g; b += p.b; n++;
                        }
                    Assert.Greater(n, 100, names[i] + ": la punta");
                    accents[i] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
                }
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(hulls[0].r, hulls[i].r, 8f, "el casco es el mismo en los tres");
                    Assert.AreEqual(hulls[0].g, hulls[i].g, 8f);
                    Assert.AreEqual(hulls[0].b, hulls[i].b, 8f);
                    for (int j = i + 1; j < 3; j++)
                    {
                        float d = Mathf.Sqrt(Mathf.Pow(accents[i].r - accents[j].r, 2) + Mathf.Pow(accents[i].g - accents[j].g, 2) + Mathf.Pow(accents[i].b - accents[j].b, 2));
                        Assert.Greater(d, 80f, names[i] + " y " + names[j] + ": los acentos de los carriles casi no se distinguen");
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheRocket_FitsBetweenThePadTowers()
        {
            // El cohete mide 230 u (lado de su lienzo) y la plataforma, 300 u: se comparan sus anchos en unidades del juego.
            var atlas = ReadAtlas();
            try
            {
                var rocket = Pixels(atlas, "cohete_coral", Rocket);
                Box(rocket, Rocket, out int rMinX, out int rMaxX, out _, out _, out _);
                float rocketU = (rMaxX - rMinX + 1) / (float)Rocket * 230f;
                var pad = Pixels(atlas, "plataforma", Pad);
                // las torres: en una fila por encima de la losa solo hay torres; el hueco entre ellas es el espacio del cohete
                int row = Pad * 45 / 100;                                                                      // (la fila 0 es la de abajo; la losa llega al 30 %, las torres al 56 %)
                int left = -1, right = -1;
                for (int x = 0; x < Pad / 2; x++) if (pad[row * Pad + x].a >= 250) left = x;                 // el borde de adentro de la torre izquierda
                for (int x = Pad - 1; x >= Pad / 2; x--) if (pad[row * Pad + x].a >= 250) right = x;         // el de la torre derecha
                Assert.Greater(left, 0, "no se encontró la torre izquierda");
                Assert.Greater(right, Pad / 2, "no se encontró la torre derecha");
                float gapU = (right - left - 1) / (float)Pad * 300f;
                Assert.LessOrEqual(rocketU, gapU + 2f, "el cohete (" + rocketU.ToString("0") + " u de ancho) no cabe entre las torres (" + gapU.ToString("0") + " u)");
            }
            finally { UnityEngine.Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void RocketAngle_MatchesTheSpriteInUse()
        {
            Assert.IsTrue(BrakeSprites.Plastic);
            for (int i = 0; i < 6; i++) Assert.IsNotNull(BrakeSprites.Rocket(i), "cohete " + i);
            Assert.AreEqual(0f, BrakeSprites.RocketAngle, "el de plasticina viene recto: no se gira");
            PlasticArt.Disabled = true;
            Assert.AreEqual(45f, BrakeSprites.RocketAngle, "el procedural viene en diagonal: se gira 45°");
        }

        [Test]
        public void WithoutThePlasticAtlas_FrenoKeepsItsProceduralArt()
        {
            PlasticArt.Disabled = true;
            Assert.IsFalse(BrakeSprites.Plastic);
            Assert.IsNotNull(BrakeSprites.StopSign());
            Assert.IsNotNull(BrakeSprites.LaunchPad());
            Assert.IsNotNull(BrakeSprites.LaunchButton(false));
            Assert.IsNotNull(BrakeSprites.LaunchButton(true));
            for (int i = 0; i < 6; i++) Assert.IsNotNull(BrakeSprites.Rocket(i), "sin plasticina el cohete es el ícono de siempre");
        }
    }
}
