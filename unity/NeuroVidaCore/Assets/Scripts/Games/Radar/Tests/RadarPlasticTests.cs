using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Radar.Tests
{
    /// <summary>
    /// Rescate relámpago en plasticina (Tarea 94, el piloto): el arte de Blender cumple lo que prometió. MISMA huella que el procedural al que reemplaza (la silueta es lo que se reconoce en el destello), contraste ≥ 3:1 contra el vidrio del radar, las seis siluetas distintas,
    /// el vidrio igual de oscuro, y sin el atlas todo sigue funcionando con el arte de siempre. Las medidas con números (para el informe) las saca <c>tools/juegos/mide_radar.py</c>.
    /// </summary>
    public sealed class RadarPlasticTests
    {
        private const int Px = 224;

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        private static Texture2D ReadAtlas()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", "radar", "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        /// <summary>Los píxeles de una cápsula del atlas (fila 0 = abajo, como en Unity).</summary>
        private static Color32[] CapsulePixels(Texture2D atlas, CapsuleType t)
        {
            var e = PlasticArt.Group("radar").Entry("capsula_" + new[] { "hexagono", "gota", "circulo", "cuadrado", "triangulo", "rombo" }[(int)t]);
            Assert.IsNotNull(e, t.ToString());
            Assert.AreEqual(Px, e.w, "la cápsula mide " + Px + " px de ancho en el atlas");
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

        [Test]
        public void PlasticCapsules_KeepTheFootprintOfTheProceduralOnes()
        {
            Assert.IsTrue(RadarSprites.Plastic, "falta el atlas de plasticina de Rescate");
            var atlas = ReadAtlas();
            try
            {
                foreach (CapsuleType t in System.Enum.GetValues(typeof(CapsuleType)))
                {
                    var plastic = CapsulePixels(atlas, t);
                    var old = RadarSprites.RenderCapsule(t, Px);
                    int a = 0, b = 0;
                    int minX = Px, maxX = 0, minY = Px, maxY = 0, oMinX = Px, oMaxX = 0, oMinY = Px, oMaxY = 0;
                    for (int y = 0; y < Px; y++)
                        for (int x = 0; x < Px; x++)
                        {
                            if (plastic[y * Px + x].a >= 250) { a++; minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                            if (old[y * Px + x].a >= 250) { b++; oMinX = Mathf.Min(oMinX, x); oMaxX = Mathf.Max(oMaxX, x); oMinY = Mathf.Min(oMinY, y); oMaxY = Mathf.Max(oMaxY, y); }
                        }
                    float ratio = a / (float)b;
                    Assert.That(ratio, Is.InRange(0.85f, 1.15f), t + ": la cápsula de plasticina ocupa " + (ratio * 100f).ToString("0") + " % de la de hoy (misma huella y mismo tamaño)");
                    // y está en el mismo sitio (el centro de la caja no se corre más de 6 px de 224)
                    Assert.AreEqual((oMinX + oMaxX) / 2f, (minX + maxX) / 2f, 6f, t + ": corrida a los lados");
                    Assert.AreEqual((oMinY + oMaxY) / 2f, (minY + maxY) / 2f, 8f, t + ": corrida hacia arriba o abajo");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void PlasticCapsules_ContrastWithTheRadarGlassAtLeast3To1()
        {
            var atlas = ReadAtlas();
            try
            {
                float glassLight = Luminance(new Color32(0x11, 0x49, 0x5C, 255));          // el vidrio más claro (el centro: GlassColor, #11495C)
                foreach (CapsuleType t in System.Enum.GetValues(typeof(CapsuleType)))
                {
                    var px = CapsulePixels(atlas, t);
                    double sum = 0; int n = 0;
                    for (int y = 0; y < Px; y++)
                        for (int x = 0; x < Px; x++)
                        {
                            if (px[y * Px + x].a < 250) continue;
                            float dx = (x + 0.5f - Px / 2f) / (Px / 2f), dy = (y + 0.5f - Px / 2f) / (Px / 2f);
                            if (Mathf.Sqrt(dx * dx + dy * dy) < 0.40f) continue;       // sin la ventanilla del centro
                            sum += Luminance(px[y * Px + x]); n++;
                        }
                    Assert.Greater(n, 5000, t + ": cuerpo de la cápsula");
                    float c = Contrast((float)(sum / n), glassLight);
                    Assert.GreaterOrEqual(c, 3f, t + ": la cápsula rinde " + c.ToString("0.0") + ":1 contra el vidrio del radar (mínimo 3:1)");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void PlasticSilhouettes_AreDistinctWithoutColor()
        {
            var atlas = ReadAtlas();
            try
            {
                var masks = new System.Collections.Generic.List<bool[]>();
                foreach (CapsuleType t in System.Enum.GetValues(typeof(CapsuleType)))
                {
                    var px = CapsulePixels(atlas, t);
                    var m = new bool[Px * Px];
                    for (int i = 0; i < m.Length; i++) m[i] = px[i].a >= 250;
                    masks.Add(m);
                }
                for (int i = 0; i < masks.Count; i++)
                    for (int j = i + 1; j < masks.Count; j++)
                    {
                        int inter = 0, union = 0;
                        for (int k = 0; k < masks[i].Length; k++) { if (masks[i][k] && masks[j][k]) inter++; if (masks[i][k] || masks[j][k]) union++; }
                        float iou = inter / (float)union;
                        Assert.LessOrEqual(iou, 0.95f, "las siluetas " + i + " y " + j + " se parecen demasiado (IoU " + iou.ToString("0.00") + "; hoy el par más parecido, hexágono y círculo, da 0,90)");
                    }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheGlassOfTheRadarIsTheSameDarkGlassAsBefore_AtEveryLevel()
        {
            // con plasticina el vidrio va aparte, debajo del aro: es el MISMO dibujo del radar procedural (mismo color y contraste; la dificultad no lo toca)
            var glass = RadarSprites.RenderGlass(128);
            var scope = RadarSprites.RenderScope(128);
            int compared = 0;
            for (int y = 40; y < 90; y += 5)
                for (int x = 40; x < 90; x += 5)
                {
                    var a = glass[y * 128 + x]; var b = scope[y * 128 + x];
                    Assert.AreEqual(b.r, a.r, 2, "r en " + x + "," + y);
                    Assert.AreEqual(b.g, a.g, 2, "g en " + x + "," + y);
                    Assert.AreEqual(b.b, a.b, 2, "b en " + x + "," + y);
                    Assert.Greater(a.a, 200, "el vidrio es opaco por dentro");
                    compared++;
                }
            Assert.Greater(compared, 80);
            Assert.AreEqual(0, glass[0].a, "y transparente en la esquina (el aro de plasticina lo enmarca)");
            Assert.Less(Luminance(glass[74 * 128 + 78]), 0.09f, "oscuro: el destello conserva su contraste");
        }

        [Test]
        public void TheShipAndItsSeatsHaveTheirMeasures()
        {
            Assert.IsTrue(RadarSprites.Plastic);
            Assert.AreEqual(240f, RadarSprites.ShipCanvas.x, 0.01f, "lienzo de la nave: 240 dp de ancho");
            Assert.AreEqual(100f, RadarSprites.ShipCanvas.y, 0.01f, "y 100 de alto");
            Assert.That(RadarSprites.SeatDy, Is.InRange(3f, 9f), "el centro de los asientos sube unos dp sobre el de la nave");
            Assert.AreEqual(20f, RadarSprites.SeatSide, 0.01f);
            Assert.IsNotNull(RadarSprites.GlassSprite());
        }

        [Test]
        public void WithoutThePlasticAtlas_RescueKeepsItsProceduralArt()
        {
            PlasticArt.Disabled = true;
            Assert.IsFalse(RadarSprites.Plastic);
            Assert.AreEqual(RadarSprites.ShipSide, RadarSprites.ShipCanvas.x, 0.01f, "el lienzo cuadrado de siempre");
            Assert.AreEqual(RadarSprites.ShipSide, RadarSprites.ShipCanvas.y, 0.01f);
            Assert.AreEqual(2f, RadarSprites.SeatDy, 0.01f);
            foreach (CapsuleType t in System.Enum.GetValues(typeof(CapsuleType)))
            {
                Assert.IsNotNull(RadarSprites.CapsuleSprite(t));
                Assert.IsNotNull(RadarSprites.Emblem(t));
            }
            Assert.IsNotNull(RadarSprites.Rock());
            Assert.IsNotNull(RadarSprites.Scope());
            Assert.IsNotNull(RadarSprites.Ship());
            Assert.IsNotNull(RadarSprites.Window());
        }
    }
}
