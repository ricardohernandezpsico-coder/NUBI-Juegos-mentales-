using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Anagramas.Tests
{
    /// <summary>
    /// En la punta de la lengua en plasticina (Tarea 101): el arte de Blender cumple lo que prometió. La ficha-letra, la casilla y la perla miden lo que dice su imagen (el código las escala con eso), la letra de tinta se lee sobre cada estado de la ficha (la ficha gris se tiñe con
    /// <c>Image.color</c>), los cuatro metales de los luceros (oro, plata, cobre y azul: el color ES el dato) siguen distinguiéndose con la perla teñida, la interfaz se arma completa con y sin el atlas y sin el atlas todo sigue funcionando con el arte de siempre. Las medidas con números las saca <c>tools/juegos/mide_punta.py</c>.
    /// </summary>
    public sealed class PuntaPlasticTests
    {
        private static readonly Color32 Ink = new Color32(0x1A, 0x12, 0x40, 255);
        private static readonly Color32 Sky = new Color32(0x0A, 0x0F, 0x33, 255);

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        private static Texture2D ReadAtlas()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", "punta", "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        private static Color32[] Pixels(Texture2D atlas, string sprite, int expectedSize)
        {
            var e = PlasticArt.Group("punta").Entry(sprite);
            Assert.IsNotNull(e, "falta «" + sprite + "» en el atlas de En la punta de la lengua");
            Assert.AreEqual(expectedSize, e.w, sprite + ": ancho");
            Assert.AreEqual(expectedSize, e.h, sprite + ": el lienzo es cuadrado");
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

        private static float Contrast(Color a, Color b) { float la = Luminance(a), lb = Luminance(b); return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f); }

        private static float DeltaE(Color a, Color b)
        {
            Vector3 Lab(Color c)
            {
                float Lin(float v) { return v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f); }
                float r = Lin(c.r), g = Lin(c.g), bl = Lin(c.b);
                float x = (0.4124f * r + 0.3576f * g + 0.1805f * bl) / 0.95047f, y = 0.2126f * r + 0.7152f * g + 0.0722f * bl, z = (0.0193f * r + 0.1192f * g + 0.9505f * bl) / 1.08883f;
                float F(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;
                return new Vector3(116f * F(y) - 16f, 500f * (F(x) - F(y)), 200f * (F(y) - F(z)));
            }
            return Vector3.Distance(Lab(a), Lab(b));
        }

        private static void BodyBox(Color32[] px, int size, out int width, out int height)
        {
            int minX = size, maxX = 0, minY = size, maxY = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (px[y * size + x].a >= 250) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
            width = maxX - minX + 1; height = maxY - minY + 1;
        }

        /// <summary>El color medio del centro de la pieza (la cara, donde va la letra).</summary>
        private static Color Center(Color32[] px, int size, float radius)
        {
            double r = 0, g = 0, b = 0; int n = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = px[y * size + x];
                    if (p.a < 250 || Mathf.Sqrt((x - size / 2f) * (x - size / 2f) + (y - size / 2f) * (y - size / 2f)) > radius * size / 2f) continue;
                    r += p.r; g += p.g; b += p.b; n++;
                }
            Assert.Greater(n, 100, "la cara de la pieza");
            return new Color((float)(r / n) / 255f, (float)(g / n) / 255f, (float)(b / n) / 255f);
        }

        [Test]
        public void ThePieces_MeasureWhatTheirImageSays_SoTheCodeScalesThemRight()
        {
            Assert.IsTrue(PlasticArt.Has("punta", "ficha"), "falta el atlas de plasticina de En la punta de la lengua");
            var atlas = ReadAtlas();
            try
            {
                BodyBox(Pixels(atlas, "ficha", 192), 192, out int fw, out _);
                Assert.AreEqual(0.8f, fw / 192f, 0.03f, "el cuerpo de la ficha mide 0,8 del lado de su imagen");
                BodyBox(Pixels(atlas, "casilla", 128), 128, out int cw, out _);
                Assert.AreEqual(0.833f, cw / 128f, 0.035f, "el cuerpo de la casilla mide 0,833 del lado");
                BodyBox(Pixels(atlas, "perla", 64), 64, out int pw, out _);
                Assert.AreEqual(0.94f, pw / 64f, 0.04f, "el cuerpo de la perla mide 0,94 del lado");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheInkLetter_ReadsOnEveryStateOfTheTile()
        {
            var atlas = ReadAtlas();
            try
            {
                var gray = Center(Pixels(atlas, "ficha", 192), 192, 0.44f);           // el centro de la ficha (donde va la letra: ~55 % del radio del cuerpo)
                Assert.Greater(Luminance(gray), 0.7f, "la ficha sin teñir es clara (el código la tiñe: multiplica)");
                var states = new (string, Color)[]
                {
                    ("normal", new Color(244f / 255f, 241f / 255f, 1f)), ("fija por ayuda", new Color(247f / 255f, 221f / 255f, 148f / 255f)),
                    ("oro", new Color(1f, 201f / 255f, 74f / 255f)), ("plata", new Color(214f / 255f, 222f / 255f, 240f / 255f)),
                    ("cobre", new Color(230f / 255f, 150f / 255f, 90f / 255f)), ("azul", new Color(127f / 255f, 170f / 255f, 1f)),
                };
                foreach (var s in states)
                    Assert.GreaterOrEqual(Contrast(Ink, gray * s.Item2), 4.5f, "letra de tinta sobre la ficha «" + s.Item1 + "»");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheFourMetals_StayApart_WithThePearlTinted()
        {
            var atlas = ReadAtlas();
            try
            {
                var gray = Center(Pixels(atlas, "perla", 64), 64, 0.7f);
                var metals = new[] { new Color(1f, 201f / 255f, 74f / 255f), new Color(214f / 255f, 222f / 255f, 240f / 255f), new Color(230f / 255f, 150f / 255f, 90f / 255f), new Color(127f / 255f, 170f / 255f, 1f) };
                for (int i = 0; i < 4; i++)
                {
                    Assert.GreaterOrEqual(Contrast(gray * metals[i], Sky), 4.5f, "la perla " + i + " contra el cielo");
                    for (int j = i + 1; j < 4; j++)
                    {
                        float design = DeltaE(metals[i], metals[j]), plastic = DeltaE(gray * metals[i], gray * metals[j]);
                        Assert.GreaterOrEqual(plastic, 12f, "metales " + i + " y " + j + ": se parecen demasiado con la perla teñida (ΔE " + plastic.ToString("0.0") + ")");
                        Assert.GreaterOrEqual(plastic, design * 0.8f, "metales " + i + " y " + j + ": la plasticina los acerca (ΔE " + plastic.ToString("0.0") + " contra " + design.ToString("0.0") + " de diseño)");
                    }
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheCardTheButtonsAndTheFinalPanel_UseKitSlabs()
        {
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Panel, SlabTone.Night) || PlasticSlab.Available(SlabSize.Button, SlabTone.Night), "la tarjeta de la definición y el panel del final");
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Button, SlabTone.Sky), "el botón principal");
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Button, SlabTone.Night), "el botón secundario");
        }

        [Test]
        public void TheInterfaceBuildsWithEveryPieceOn_WithAndWithoutThePlasticAtlas()
        {
            foreach (bool disabled in new[] { false, true })
            {
                PlasticArt.Disabled = disabled;
                var go = new GameObject("PuntaPlasticAudit");
                try
                {
                    var controller = go.AddComponent<PuntaGameController>();
                    var build = typeof(PuntaGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
