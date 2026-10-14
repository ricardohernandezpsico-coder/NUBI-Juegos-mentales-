using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Secuencia.Tests
{
    /// <summary>
    /// Rastro de luz en plasticina (Tarea 99): el arte de Blender cumple lo que prometió. Los 9 luceros ocupan el lugar y el tamaño de los de hoy, son IDÉNTICOS entre sí (la identidad de un lucero es su posición, su color y su nota), conservan su color de diseño y los pares cercanos
    /// (7FD8FF / A9C7FF, FFC94A / FFE27A, 9FF5D6 / 8EE07A) se siguen distinguiendo, son redondos (sin puntas ni símbolos), contrastan con el cielo, y sin el atlas todo sigue funcionando con el arte de siempre. Las medidas con números (para el informe) las saca <c>tools/juegos/mide_rastro.py</c>.
    /// </summary>
    public sealed class RastroPlasticTests
    {
        private const int Px = 192;
        private static readonly Color32 Sky = new Color32(0x0A, 0x0F, 0x33, 255);

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        private static Texture2D ReadAtlas()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", "rastro", "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        private static Color32[] Pixels(Texture2D atlas, int orb)
        {
            var e = PlasticArt.Group(RastroSprites.PlasticGroup).Entry("lucero_" + orb);
            Assert.IsNotNull(e, "falta el lucero " + orb + " en el atlas de Rastro de luz");
            Assert.AreEqual(Px, e.w, "lucero " + orb + ": ancho");
            Assert.AreEqual(Px, e.h, "lucero " + orb + ": el lienzo es cuadrado, como el del procedural");
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

        /// <summary>La distancia de color CIELAB (ΔE76) entre dos colores sRGB.</summary>
        private static float DeltaE(Color32 a, Color32 b)
        {
            Vector3 Lab(Color32 c)
            {
                float Lin(float v) { v /= 255f; return v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f); }
                float r = Lin(c.r), g = Lin(c.g), bl = Lin(c.b);
                float x = (0.4124f * r + 0.3576f * g + 0.1805f * bl) / 0.95047f, y = 0.2126f * r + 0.7152f * g + 0.0722f * bl, z = (0.0193f * r + 0.1192f * g + 0.9505f * bl) / 1.08883f;
                float F(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;
                return new Vector3(116f * F(y) - 16f, 500f * (F(x) - F(y)), 200f * (F(y) - F(z)));
            }
            return Vector3.Distance(Lab(a), Lab(b));
        }

        private static Color32 FaceColor(Color32[] px)
        {
            double r = 0, g = 0, b = 0; int n = 0;
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++)
                {
                    var p = px[y * Px + x];
                    if (p.a < 250 || Mathf.Sqrt((x - Px / 2f) * (x - Px / 2f) + (y - Px / 2f) * (y - Px / 2f)) > 0.7f * Px / 2f) continue;
                    r += p.r; g += p.g; b += p.b; n++;
                }
            Assert.Greater(n, 5000, "la cara del lucero");
            return new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }

        private static Color32 Design(int i)
        {
            int c = RastroBoard.Colors[i];
            return new Color32((byte)((c >> 16) & 255), (byte)((c >> 8) & 255), (byte)(c & 255), 255);
        }

        [Test]
        public void TheNineOrbs_KeepTheFootprintOfToday_AndAreRound()
        {
            Assert.IsTrue(RastroSprites.Plastic, "falta el atlas de plasticina de Rastro de luz");
            var atlas = ReadAtlas();
            try
            {
                for (int i = 0; i < RastroBoard.Orbs; i++)
                {
                    var plastic = Pixels(atlas, i);
                    var old = RastroSprites.OrbPixels(ClayRaster.Hex(RastroBoard.Colors[i]), Px);
                    int pArea = 0, oArea = 0, minX = Px, maxX = 0, minY = Px, maxY = 0;
                    for (int y = 0; y < Px; y++)
                        for (int x = 0; x < Px; x++)
                        {
                            if (plastic[y * Px + x].a >= 250) { pArea++; minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                            if (old[y * Px + x].a >= 80) oArea++;                       // el de hoy se vuelve translúcido hacia el borde (alfa mínimo ~0,35)
                        }
                    float ratio = pArea / (float)oArea;
                    Assert.That(ratio, Is.InRange(0.9f, 1.1f), "lucero " + i + ": ocupa " + (ratio * 100f).ToString("0") + " % del de hoy (misma huella y mismo tamaño)");
                    Assert.AreEqual(Px / 2f, (minX + maxX) / 2f, 3f, "lucero " + i + ": corrido a los lados");
                    Assert.AreEqual(Px / 2f, (minY + maxY) / 2f, 3f, "lucero " + i + ": corrido hacia arriba o abajo");
                    // redondo: sin puntas ni símbolos (el área es la de un círculo de su ancho)
                    float circle = Mathf.PI * ((maxX - minX + 1) / 2f) * ((maxX - minX + 1) / 2f);
                    Assert.That(pArea / circle, Is.InRange(0.97f, 1.03f), "lucero " + i + ": no es un círculo");
                    Assert.AreEqual(maxX - minX, maxY - minY, 3f, "lucero " + i + ": ancho y alto");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheNineOrbs_AreIdentical_ExceptForTheirColor()
        {
            var atlas = ReadAtlas();
            try
            {
                var first = Pixels(atlas, 0);
                for (int i = 1; i < RastroBoard.Orbs; i++)
                {
                    var other = Pixels(atlas, i);
                    int both = 0, differ = 0;
                    for (int k = 0; k < first.Length; k++)
                    {
                        bool a = first[k].a >= 250, b = other[k].a >= 250;
                        if (a || b) { both++; if (a != b) differ++; }
                    }
                    Assert.Less(differ / (float)both, 0.005f, "lucero " + i + ": su silueta no es la de los demás (la identidad es solo posición, color y nota)");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void EveryOrb_KeepsItsDesignColor_AndTheCloseOnesStayApart()
        {
            var atlas = ReadAtlas();
            try
            {
                var faces = new Color32[RastroBoard.Orbs];
                for (int i = 0; i < RastroBoard.Orbs; i++)
                {
                    faces[i] = FaceColor(Pixels(atlas, i));
                    Assert.Less(DeltaE(faces[i], Design(i)), 14f, "lucero " + i + ": su color medio se aleja del de diseño");
                }
                for (int i = 0; i < RastroBoard.Orbs; i++)
                    for (int j = i + 1; j < RastroBoard.Orbs; j++)
                    {
                        float design = DeltaE(Design(i), Design(j));
                        if (design > 40f) continue;                                    // solo los pares cercanos
                        float plastic = DeltaE(faces[i], faces[j]);
                        Assert.GreaterOrEqual(plastic, 12f, "lucero " + i + " y " + j + ": se parecen demasiado (ΔE " + plastic.ToString("0.0") + ")");
                        Assert.GreaterOrEqual(plastic, design * 0.75f, "lucero " + i + " y " + j + ": la plasticina los acerca (ΔE " + plastic.ToString("0.0") + " contra " + design.ToString("0.0") + " de diseño)");
                    }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void EveryOrb_ContrastsWithTheSky()
        {
            var atlas = ReadAtlas();
            try
            {
                for (int i = 0; i < RastroBoard.Orbs; i++)
                    Assert.GreaterOrEqual(Contrast(Luminance(FaceColor(Pixels(atlas, i))), Luminance(Sky)), 4.5f, "lucero " + i + " contra el cielo");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheSlabsOfThePillsAndTheFinalPanel_Exist()
        {
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Chip, SlabTone.Night), "las píldoras del marcador");
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Panel, SlabTone.Night) || PlasticSlab.Available(SlabSize.Button, SlabTone.Night), "el panel del final");
        }

        [Test]
        public void WithoutThePlasticAtlas_RastroKeepsItsProceduralOrbs()
        {
            PlasticArt.Disabled = true;
            Assert.IsFalse(RastroSprites.Plastic);
            for (int i = 0; i < RastroBoard.Orbs; i++) Assert.IsNotNull(RastroSprites.Orb(i), "lucero " + i);
            Assert.IsNotNull(RastroSprites.XMark());
            Assert.IsNotNull(RastroSprites.DashedRing());
        }
    }
}
