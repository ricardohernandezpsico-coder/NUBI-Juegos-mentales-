using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Calculo.Tests
{
    /// <summary>
    /// Carga exacta en plasticina (Tarea 97): el arte de Blender cumple lo que prometió. El aro y el núcleo del reactor ocupan el lienzo y el lugar de hoy (cada uno con la medida de su diseño en dp), el núcleo es CLARO para que el código lo tiña (azul noche → dorado) y el número se lea en los dos
    /// colores, las losas que usan las celdas, las operaciones y los botones existen (la elegida, también hundida), y la interfaz se arma completa con y sin el atlas. Las medidas con números (para el informe) las saca <c>tools/juegos/mide_calculo.py</c>.
    /// </summary>
    public sealed class CalculoPlasticTests
    {
        private const int Px = 552;                    // el lienzo del reactor: ±92 dp a 3 px/dp
        private const float PxPerDp = 3f;

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        private static Texture2D ReadAtlas()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", "calculo", "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        private static Color32[] Pixels(Texture2D atlas, string sprite)
        {
            var e = PlasticArt.Group("calculo").Entry(sprite);
            Assert.IsNotNull(e, "falta el sprite «" + sprite + "» en el atlas de Carga exacta");
            Assert.AreEqual(Px, e.w, sprite + ": ancho");
            Assert.AreEqual(Px, e.h, sprite + ": el lienzo es cuadrado, igual en el aro y en el núcleo");
            var colors = atlas.GetPixels(e.x, e.y, e.w, e.h);
            var result = new Color32[colors.Length];
            for (int i = 0; i < colors.Length; i++) result[i] = colors[i];
            return result;
        }

        private static float Luminance(Color c)
        {
            float R(float v) { return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f); }
            return 0.2126f * R(c.r) + 0.7152f * R(c.g) + 0.0722f * R(c.b);
        }

        private static float Contrast(float la, float lb) => (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);

        private static void Box(Color32[] px, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = Px; maxX = 0; minY = Px; maxY = 0;
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++)
                    if (px[y * Px + x].a >= 250) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        }

        [Test]
        public void TheRing_AndTheCore_HaveTheDesignSizeAndCenter()
        {
            Assert.IsTrue(PlasticArt.Has("calculo", "reactor_aro"), "falta el atlas de plasticina de Carga exacta");
            var atlas = ReadAtlas();
            try
            {
                var ring = Pixels(atlas, "reactor_aro");
                Box(ring, out int rMinX, out int rMaxX, out int rMinY, out int rMaxY);
                float ringDp = (rMaxX - rMinX + 1) / PxPerDp;
                Assert.AreEqual(2f * CargaLayout.RingR, ringDp, 3f, "el aro mide " + ringDp.ToString("0.0") + " dp de lado a lado (el de hoy, " + 2f * CargaLayout.RingR + ")");
                Assert.AreEqual(Px / 2f, (rMinX + rMaxX) / 2f, 4f, "aro: corrido a los lados");
                Assert.AreEqual(Px / 2f, (rMinY + rMaxY) / 2f, 6f, "aro: corrido hacia arriba o abajo");
                Assert.AreEqual(0, ring[(Px / 2) * Px + Px / 2].a, "el hueco del aro está vacío: ahí va el núcleo");

                var core = Pixels(atlas, "reactor_nucleo");
                Box(core, out int cMinX, out int cMaxX, out int cMinY, out int cMaxY);
                float coreDp = (cMaxX - cMinX + 1) / PxPerDp;
                Assert.AreEqual(2f * CargaLayout.CoreR, coreDp, 3f, "el núcleo mide " + coreDp.ToString("0.0") + " dp (el de hoy, " + 2f * CargaLayout.CoreR + ")");
                Assert.AreEqual(Px / 2f, (cMinX + cMaxX) / 2f, 4f, "núcleo: corrido a los lados");
                Assert.AreEqual(Px / 2f, (cMinY + cMaxY) / 2f, 6f, "núcleo: corrido hacia arriba o abajo");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheCore_IsLight_SoTheCodeTintCarriesBothColors_AndTheNumberReadsOnBoth()
        {
            var atlas = ReadAtlas();
            try
            {
                var core = Pixels(atlas, "reactor_nucleo");
                double r = 0, g = 0, b = 0; int n = 0;
                for (int y = 0; y < Px; y++)
                    for (int x = 0; x < Px; x++)
                    {
                        var p = core[y * Px + x];
                        if (p.a < 250 || Mathf.Sqrt((x - Px / 2f) * (x - Px / 2f) + (y - Px / 2f) * (y - Px / 2f)) > 60f * PxPerDp) continue;     // la cara del núcleo (sin el canto)
                        r += p.r; g += p.g; b += p.b; n++;
                    }
                Assert.Greater(n, 20000, "la cara del núcleo");
                var gray = new Color((float)(r / n) / 255f, (float)(g / n) / 255f, (float)(b / n) / 255f);
                Assert.Greater(Luminance(gray), 0.5f, "el núcleo debe ser claro: el código lo tiñe con Image.color (multiplica)");
                // los dos colores a los que lo tiñe AnimateReactor, y el número encima (blanco sobre el oscuro, tinta sobre el dorado)
                var dark = new Color(13f / 255f, 20f / 255f, 53f / 255f) * gray;
                var gold = new Color(233f / 255f, 169f / 255f, 58f / 255f) * gray;
                Assert.GreaterOrEqual(Contrast(Luminance(Color.white), Luminance(dark)), 7f, "el número blanco sobre el núcleo apagado");
                Assert.GreaterOrEqual(Contrast(Luminance(new Color(26f / 255f, 18f / 255f, 64f / 255f)), Luminance(gold)), 4.5f, "el número de tinta sobre el núcleo dorado");
                Assert.GreaterOrEqual(Contrast(Luminance(gold), Luminance(dark)), 4f, "apagado y dorado se distinguen por luminosidad, no solo por color");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheRing_IsTheDeepBlueOfToday()
        {
            var atlas = ReadAtlas();
            try
            {
                var ring = Pixels(atlas, "reactor_aro");
                double r = 0, g = 0, b = 0; int n = 0;
                for (int y = 0; y < Px; y++)
                    for (int x = 0; x < Px; x++)
                    {
                        var p = ring[y * Px + x];
                        if (p.a < 250) continue;
                        r += p.r; g += p.g; b += p.b; n++;
                    }
                Assert.Greater(n, 20000, "el aro");
                Assert.AreEqual(0x23, r / n, 22.0, "rojo del aro (azul noche #232B57)");
                Assert.AreEqual(0x2B, g / n, 22.0, "verde del aro");
                Assert.AreEqual(0x57, b / n, 22.0, "azul del aro");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheSlabsOfTheCellsOperationsAndButtons_Exist_AndTheChosenCellHasASunkenLook()
        {
            foreach (var tone in new[] { SlabTone.Cream, SlabTone.Sky, SlabTone.Sun, SlabTone.Night, SlabTone.Dim })
                Assert.IsTrue(PlasticSlab.Available(SlabSize.Button, tone), "falta la losa md " + tone);
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Panel, SlabTone.Night) || PlasticSlab.Available(SlabSize.Button, SlabTone.Night), "falta la losa del panel del final");
            Assert.AreNotSame(PlasticSlab.Sprite(SlabSize.Button, SlabTone.Sky), PlasticSlab.Sprite(SlabSize.Button, SlabTone.Sky, true), "la celda elegida «hundida» es otra imagen");
            Assert.Greater(PlasticSlab.PressDrop(SlabSize.Button, SlabTone.Sky), 0f, "y el número baja con ella");
        }

        [Test]
        public void TheInterfaceBuildsWithEveryPieceOn_WithAndWithoutThePlasticAtlas()
        {
            foreach (bool disabled in new[] { false, true })
            {
                PlasticArt.Disabled = disabled;
                var go = new GameObject("CargaPlasticAudit");
                try
                {
                    var controller = go.AddComponent<CalculoGameController>();
                    var build = typeof(CalculoGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    build.Invoke(controller, null);
                    go.SetActive(true);
                    var problems = controller.AuditVisibility();
                    Assert.IsEmpty(problems, (disabled ? "sin plasticina" : "con plasticina") + ": piezas que no se verían: " + string.Join(" | ", problems));
                }
                finally { Object.DestroyImmediate(go); }
            }
        }
    }
}
