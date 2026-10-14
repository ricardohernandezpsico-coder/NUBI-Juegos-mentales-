using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Disparate.Tests
{
    /// <summary>
    /// ¿Verdad o disparate? en plasticina (Tarea 97; la antena parabólica y la onda cortada, corrección del 14-oct): el arte de Blender cumple lo que prometió. La antena conserva su huella y los puntos donde el código pone la luz y las ondas (punta, plato, suelo), los íconos se leen contra los botones lima y coral, las barras del medidor
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
        public void TheAntenna_ReadsAsATiltedDish_WithItsArmAndItsLightApart()
        {
            // corrección del 14-oct: con el plato de frente y el pie se leía como una copa. Ahora el plato mira de lado (su interior celeste a la vista), el brazo del receptor es lila y la luz queda al final, aparte.
            var atlas = ReadAtlas();
            try
            {
                var px = Pixels(atlas, "antena", out int w, out int h);
                bool IsSun(Color32 p) => p.a >= 250 && p.r > 220 && p.g > 150 && p.b < 130;
                bool IsCream(Color32 p) => p.a >= 250 && p.r > 235 && p.g > 225 && p.b > 200 && p.b < 246;          // (sin el brillo blanco de la luz, que está pegado a ella)
                bool IsInterior(Color32 p) => p.a >= 250 && p.b > 200 && p.r < 140 && p.g > 150;
                bool IsLilac(Color32 p) => p.a >= 250 && Mathf.Abs(p.r - 142) < 40 && Mathf.Abs(p.g - 124) < 40 && p.b > 190;
                // 1) el interior del plato (celeste) está en su sitio: el centro del plato que usa el código cae dentro de él
                int dx = Mathf.RoundToInt(DisparateSprites.AntennaDish.x * w), dy = Mathf.RoundToInt(DisparateSprites.AntennaDish.y * h);
                int interior = 0, interiorLeft = 0, interiorRight = 0;
                double iy = 0;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (IsInterior(px[y * w + x])) { interior++; iy += y; if (x < dx) interiorLeft++; else interiorRight++; }
                Assert.Greater(interior, 0.03f * w * h, "el interior del plato (celeste) tiene que verse: es lo que lo separa de una copa");
                Assert.AreEqual(dy, iy / interior, 20.0, "el centro del plato (AntennaDish) cae en su interior");
                // 2) el brazo es lila y la luz queda lejos de lo crema: no se funden (tono contra tono)
                int lilacAbove = 0, sunPx = 0, sunXi = 0, sunYi = 0;
                for (int y = h * 3 / 4; y < h; y++) for (int x = 0; x < w; x++) if (IsLilac(px[y * w + x])) lilacAbove++;
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (IsSun(px[y * w + x])) { sunPx++; sunXi += x; sunYi += y; }
                Assert.Greater(lilacAbove, 150, "el brazo del receptor (lila) se ve arriba del plato");
                Assert.Greater(sunPx, 400, "la luz de la punta (sol) tiene cuerpo");
                float sunX = sunXi / (float)sunPx, sunY = sunYi / (float)sunPx;
                // caminando de la luz por el eje del brazo (hacia abajo y a la izquierda, a ~28° de la vertical) hay un tramo largo de brazo y receptor: ni sol, ni crema, ni celeste
                const float vx = -0.479f, vy = -0.878f;
                int armSteps = 0;
                for (float t = 22f; t < 170f; t += 1f)
                {
                    var q = px[Mathf.RoundToInt(sunY + vy * t) * w + Mathf.RoundToInt(sunX + vx * t)];
                    if (IsCream(q) || IsInterior(q)) break;
                    if (q.a >= 250 && !IsSun(q)) armSteps++;
                }
                Assert.GreaterOrEqual(armSteps, 60, "por el eje del brazo hay solo " + armSteps + " px de brazo y receptor entre la luz y el plato: la luz tiene que quedar separada de él");
                // 3) el mástil (lila) se separa del plato crema por tono: ~3:1 de luminosidad (un cuerpo, no un hueco)
                var mast = px[Mathf.RoundToInt(0.40f * h) * w + Mathf.RoundToInt(0.5f * w)];
                Assert.IsTrue(IsLilac(mast), "en el mástil hay lila (" + mast + ")");
                var cream = MeanOpaque(px, w, h, 0.7f, 1f);
                Assert.GreaterOrEqual(Contrast(Luminance(cream), Luminance(mast)), 2.5f, "el mástil lila se separa del plato crema");
                Assert.GreaterOrEqual(Contrast(Luminance(mast), Luminance(Sky)), 3f, "el mástil lila se ve contra el cielo");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheBrokenWave_StaysTwoSeparatePieces_WithAClearGap_AtSmallSizes()
        {
            // corrección del 14-oct: el ícono de DISPARATE se leía como dos manchas. Ahora es una onda cortada en dos tramos con un hueco claro, y a 24, 28 y 32 dp se siguen viendo DOS trazos separados.
            var atlas = ReadAtlas();
            try
            {
                var px = Pixels(atlas, "icono_ola", out int w, out int h);
                foreach (int dp in new[] { 24, 28, 32 })
                {
                    var mask = new bool[dp * dp];
                    int k = w / dp;
                    for (int y = 0; y < dp; y++)
                        for (int x = 0; x < dp; x++)
                        {
                            float cover = 0f;
                            for (int yy = 0; yy < k; yy++) for (int xx = 0; xx < k; xx++) cover += px[(y * k + yy) * w + x * k + xx].a >= 128 ? 1f : 0f;
                            mask[y * dp + x] = cover / (k * k) > 0.5f;
                        }
                    // componentes de 4 vecinos
                    var seen = new bool[dp * dp]; int pieces = 0;
                    for (int s0 = 0; s0 < dp * dp; s0++)
                    {
                        if (!mask[s0] || seen[s0]) continue;
                        pieces++;
                        var stack = new System.Collections.Generic.Stack<int>(); stack.Push(s0); seen[s0] = true;
                        while (stack.Count > 0)
                        {
                            int c = stack.Pop(), cx = c % dp, cy = c / dp;
                            foreach (var (nx, ny) in new[] { (cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1) })
                            {
                                if (nx < 0 || ny < 0 || nx >= dp || ny >= dp) continue;
                                int n = ny * dp + nx;
                                if (mask[n] && !seen[n]) { seen[n] = true; stack.Push(n); }
                            }
                        }
                    }
                    Assert.AreEqual(2, pieces, "a " + dp + " dp la onda tiene que verse en DOS tramos");
                    // el hueco: las columnas vacías entre el tramo izquierdo y el derecho
                    int first = dp, last = 0; var colHas = new bool[dp];
                    for (int x = 0; x < dp; x++) for (int y = 0; y < dp; y++) if (mask[y * dp + x]) { colHas[x] = true; first = Mathf.Min(first, x); last = Mathf.Max(last, x); }
                    int run = 0, gap = 0;
                    for (int x = first; x <= last; x++) { if (!colHas[x]) { run++; gap = Mathf.Max(gap, run); } else run = 0; }
                    Assert.GreaterOrEqual(gap, 2, "a " + dp + " dp el hueco entre los dos tramos mide " + gap + " px: hace falta uno claro");
                }
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
