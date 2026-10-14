using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Disparate.Tests
{
    /// <summary>
    /// ¿Verdad o disparate? en plasticina (Tarea 97): el arte de Blender cumple lo que prometió. La antena conserva su huella y los puntos donde el código pone la luz y las ondas (punta, plato, suelo), los íconos se leen contra los botones lima y coral, las barras del medidor
    /// se estiran sin deformar los extremos y se distinguen por luminosidad, las losas de la placa, los botones y el panel del final existen, y sin el atlas todo sigue funcionando con el arte de siempre. Las medidas con números (para el informe) las saca <c>tools/juegos/mide_disparate.py</c>.
    /// </summary>
    public sealed class DisparatePlasticTests
    {
        private const int AntennaPx = 420, IconPx = 128;
        private static readonly Color32 Sky = new Color32(0x07, 0x0A, 0x28, 255);
        private static readonly Color32 Lime = new Color32(0x9B, 0xE5, 0x64, 255);
        private static readonly Color32 Coral = new Color32(0xFF, 0x6B, 0x4A, 255);

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        private static Texture2D ReadAtlas()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", "disparate", "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        private static Color32[] Pixels(Texture2D atlas, string sprite, out int w, out int h)
        {
            var e = PlasticArt.Group(DisparateSprites.PlasticGroup).Entry(sprite);
            Assert.IsNotNull(e, "falta el sprite «" + sprite + "» en el atlas de Disparate");
            w = e.w; h = e.h;
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

        private static void Box(Color32[] px, int w, int h, out int minX, out int maxX, out int minY, out int maxY, out int area)
        {
            minX = w; maxX = 0; minY = h; maxY = 0; area = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a >= 250) { area++; minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        }

        private static Color32 MeanOpaque(Color32[] px, int w, int h, float lo, float hi)
        {
            double r = 0, g = 0, b = 0; int n = 0;
            for (int i = 0; i < w * h; i++)
            {
                var p = px[i];
                if (p.a < 250) continue;
                float l = Luminance(p);
                if (l < lo || l > hi) continue;
                r += p.r; g += p.g; b += p.b; n++;
            }
            Assert.Greater(n, 50, "no hay píxeles de esa luminosidad (" + lo + " a " + hi + ")");
            return new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }

        [Test]
        public void ThePlasticAntenna_KeepsTheFootprint_AndTheAnchorsWhereTheCodePutsTheLight()
        {
            Assert.IsTrue(DisparateSprites.Plastic, "falta el atlas de plasticina de Disparate");
            var atlas = ReadAtlas();
            try
            {
                var plastic = Pixels(atlas, "antena", out int w, out int h);
                Assert.AreEqual(AntennaPx, w); Assert.AreEqual(AntennaPx, h, "el lienzo es cuadrado, como el del procedural");
                var old = DisparateSprites.RenderAntenna(AntennaPx);
                Box(plastic, w, h, out int pMinX, out int pMaxX, out int pMinY, out int pMaxY, out int pArea);
                Box(old, AntennaPx, AntennaPx, out int oMinX, out int oMaxX, out int oMinY, out int oMaxY, out int oArea);
                float ratio = pArea / (float)oArea;
                Assert.That(ratio, Is.InRange(0.7f, 1.3f), "la antena de plasticina ocupa " + (ratio * 100f).ToString("0") + " % de la de hoy (misma huella y mismo tamaño)");
                Assert.AreEqual((oMinX + oMaxX) / 2f, (pMinX + pMaxX) / 2f, 12f, "antena: corrida a los lados");
                Assert.AreEqual((oMinY + oMaxY) / 2f, (pMinY + pMaxY) / 2f, 16f, "antena: corrida hacia arriba o abajo");
                // la luz de la punta (sol) y el plato (crema) caen donde el código los espera (DisparateSprites.AntennaTip y AntennaDish, como fracción del lienzo)
                int tx = Mathf.RoundToInt(DisparateSprites.AntennaTip.x * AntennaPx), ty = Mathf.RoundToInt(DisparateSprites.AntennaTip.y * AntennaPx);
                bool sun = false;
                for (int y = ty - 14; y <= ty + 14 && !sun; y++)
                    for (int x = tx - 14; x <= tx + 14 && !sun; x++)
                    { var p = plastic[y * w + x]; sun = p.a >= 250 && p.r > 220 && p.g > 150 && p.b < 130; }
                Assert.IsTrue(sun, "la luz de la punta no está donde el código la pone (" + tx + ", " + ty + ")");
                int dx = Mathf.RoundToInt(DisparateSprites.AntennaDish.x * AntennaPx), dy = Mathf.RoundToInt(DisparateSprites.AntennaDish.y * AntennaPx);
                Assert.GreaterOrEqual(plastic[(dy - 10) * w + dx].a, 250, "el plato no está donde el código emite las ondas (" + dx + ", " + dy + ")");
                // el suelo de la antena: justo por encima de AntennaFloor hay cuerpo
                int fy = Mathf.RoundToInt(DisparateSprites.AntennaFloor * AntennaPx);
                Assert.GreaterOrEqual(plastic[(fy + 14) * w + AntennaPx / 2 + 60].a, 250, "la plataforma no llega al suelo que usa el código para el medidor (" + fy + ")");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheAntennaMastAndDish_ContrastWithTheSky()
        {
            var atlas = ReadAtlas();
            try
            {
                var px = Pixels(atlas, "antena", out int w, out int h);
                var cream = MeanOpaque(px, w, h, 0.6f, 1f);
                Assert.GreaterOrEqual(Contrast(Luminance(cream), Luminance(Sky)), 8f, "el mástil y el plato crema contra el cielo");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheIcons_ReadAgainstTheLimeAndCoralButtons_AndKeepTheirSizeOfToday()
        {
            var atlas = ReadAtlas();
            try
            {
                foreach (var icon in new[] { "icono_visto", "icono_ola" })
                {
                    var px = Pixels(atlas, icon, out int w, out int h);
                    Assert.AreEqual(IconPx, w, icon); Assert.AreEqual(IconPx, h, icon);
                    var ink = MeanOpaque(px, w, h, 0f, 0.04f);
                    Assert.GreaterOrEqual(Contrast(Luminance(ink), Luminance(Lime)), 7f, icon + ": la tinta contra el botón VERDAD (lima)");
                    Assert.GreaterOrEqual(Contrast(Luminance(ink), Luminance(Coral)), 4.5f, icon + ": la tinta contra el botón DISPARATE (coral)");
                }
                // el tamaño: lo que ocupa cada ícono de plasticina del lienzo, contra el procedural (96 px)
                PlasticArt.Disabled = true;
                var oldCheck = DisparateSprites.CheckIcon().texture.GetPixels32();
                var oldWave = DisparateSprites.BrokenWaveIcon().texture.GetPixels32();
                PlasticArt.Disabled = false;
                for (int k = 0; k < 2; k++)
                {
                    var plastic = Pixels(atlas, k == 0 ? "icono_visto" : "icono_ola", out int w, out int h);
                    Box(plastic, w, h, out int pMinX, out int pMaxX, out int pMinY, out int pMaxY, out _);
                    Box(k == 0 ? oldCheck : oldWave, 96, 96, out int oMinX, out int oMaxX, out int oMinY, out int oMaxY, out _);
                    string name = k == 0 ? "visto" : "onda rota";
                    Assert.AreEqual((oMaxX - oMinX + 1) / 96f, (pMaxX - pMinX + 1) / (float)w, 0.14f, name + ": ancho (fracción del lienzo)");
                    Assert.AreEqual((oMaxY - oMinY + 1) / 96f, (pMaxY - pMinY + 1) / (float)h, 0.14f, name + ": alto (fracción del lienzo)");
                }
            }
            finally { Object.DestroyImmediate(atlas); PlasticArt.Disabled = false; }
        }

        [Test]
        public void TheMeterBars_StretchWithoutDeformingTheEnds_AndLitAndUnlitDifferInLuminance()
        {
            var lit = DisparateSprites.MeterBar(true);
            var off = DisparateSprites.MeterBar(false);
            Assert.IsNotNull(lit); Assert.IsNotNull(off);
            Assert.AreEqual(0f, lit.border.x, 0.01f, "sin borde a los lados: solo se estira en vertical");
            Assert.AreEqual(0f, lit.border.z, 0.01f);
            Assert.Greater(lit.border.y, 20f, "los extremos redondos (con su sombra) no se estiran");
            Assert.AreEqual(lit.border.y, lit.border.w, 0.01f);
            Assert.AreEqual(13.6f, DisparateSprites.MeterMargin, 0.5f, "lo que sobresale la barra por la sombra");
            // el cuerpo mide 34 u de ancho: el sprite, 34 + 2 × margen
            Assert.AreEqual(34f + 2f * DisparateSprites.MeterMargin, lit.rect.width, 1.5f, "ancho del sprite del medidor");
            var atlas = ReadAtlas();
            try
            {
                var pl = Pixels(atlas, "medidor_encendida", out int w, out int h);
                var po = Pixels(atlas, "medidor_apagada", out _, out _);
                var litC = MeanOpaque(pl, w, h, 0.05f, 1f);
                var offC = MeanOpaque(po, w, h, 0.01f, 1f);
                Assert.GreaterOrEqual(Contrast(Luminance(litC), Luminance(offC)), 3f, "encendida y apagada se distinguen por la luminosidad");
                Assert.GreaterOrEqual(Contrast(Luminance(offC), Luminance(Sky)), 1.5f, "la barra apagada se ve contra el cielo");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheSlabsOfThePlateTheButtonsAndTheFinalPanel_Exist()
        {
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Button, SlabTone.Cream), "la placa de la transmisión");
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Button, SlabTone.Lime), "el botón VERDAD");
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Button, SlabTone.Coral), "el botón DISPARATE");
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Panel, SlabTone.Night) || PlasticSlab.Available(SlabSize.Button, SlabTone.Night), "el panel del final");
            Assert.AreEqual(SlabTone.Lime, PlasticSlab.Nearest(NeuroVida.Games.Shared.NeuroStyle.Lime));
            Assert.AreEqual(SlabTone.Coral, PlasticSlab.Nearest(NeuroVida.Games.Shared.NeuroStyle.Coral));
        }

        [Test]
        public void WithoutThePlasticAtlas_DisparateKeepsItsProceduralArt()
        {
            PlasticArt.Disabled = true;
            Assert.IsFalse(DisparateSprites.Plastic);
            Assert.IsNotNull(DisparateSprites.Antenna());
            Assert.IsNotNull(DisparateSprites.CheckIcon());
            Assert.IsNotNull(DisparateSprites.BrokenWaveIcon());
            Assert.IsNull(DisparateSprites.MeterBar(true), "sin plasticina, las barras son el rectángulo redondeado de siempre");
            Assert.AreEqual(0f, DisparateSprites.MeterMargin);
        }

        [Test]
        public void TheInterfaceBuilds_WithAndWithoutThePlasticAtlas_AndTheSlabsAreOnlyThereWithIt()
        {
            foreach (bool disabled in new[] { false, true })
            {
                PlasticArt.Disabled = disabled;
                var go = new GameObject("DisparatePlasticAudit");
                try
                {
                    var controller = go.AddComponent<DisparateGameController>();
                    var build = typeof(DisparateGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    build.Invoke(controller, null);
                    int slabs = 0;
                    foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (t.name == "Slab") slabs++;
                    if (disabled) Assert.AreEqual(0, slabs, "sin plasticina no hay losas");
                    else Assert.GreaterOrEqual(slabs, 4, "con plasticina: la placa, los dos botones y el panel del final son losas (" + slabs + ")");
                }
                finally { Object.DestroyImmediate(go); }
            }
        }
    }
}
