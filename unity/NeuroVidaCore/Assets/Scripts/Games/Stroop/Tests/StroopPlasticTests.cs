using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Stroop.Tests
{
    /// <summary>
    /// Tinta o Palabra en plasticina (Tarea 100): EL COLOR DE LA TINTA ES EL ESTÍMULO, así que el arte se mide contra el color de diseño. Los 4 botones de respuesta (ROJO, AZUL, AMARILLO, BLANCO) llevan la tinta con la luz de plasticina SIN moverle el tono (ΔE ≤ 6 de su color),
    /// siguen distinguiéndose entre sí, el nombre se lee sobre cada uno, el fondo de la tarjeta sigue siendo #141B36 (sobre él se midieron los contrastes de las 4 tintas), la versión presionada es otra imagen (más oscura), y sin el atlas todo sigue funcionando con el arte de siempre.
    /// Las medidas con números (para el informe) las saca <c>tools/juegos/mide_stroop.py</c>, que además comprueba la separación con daltonismo (CIEDE2000, regla ≥ 20).
    /// </summary>
    public sealed class StroopPlasticTests
    {
        private static readonly string[] Names = { "rojo", "azul", "amarillo", "blanco" };
        private static readonly Color32 Ink = new Color32(0x1A, 0x12, 0x40, 255);          // el nombre sobre AZUL, AMARILLO y BLANCO
        private static readonly Color32 White = new Color32(255, 255, 255, 255);          // el nombre sobre ROJO
        private static readonly Color32 CardFill = new Color32(0x14, 0x1B, 0x36, 255);

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        private static Texture2D ReadAtlas()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", "stroop", "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        private static Color32[] Pixels(Texture2D atlas, string sprite, out int w, out int h)
        {
            var e = PlasticArt.Group(StroopSprites.PlasticGroup).Entry(sprite);
            Assert.IsNotNull(e, "falta «" + sprite + "» en el atlas de Tinta o Palabra");
            w = e.w; h = e.h;
            var colors = atlas.GetPixels(e.x, e.y, e.w, e.h);
            var result = new Color32[colors.Length];
            for (int i = 0; i < colors.Length; i++) result[i] = colors[i];
            return result;
        }

        /// <summary>El color medio de la cara de una losa: el centro de su cuerpo (los márgenes de la sombra no cuentan).</summary>
        private static Color32 Face(Texture2D atlas, string sprite)
        {
            var px = Pixels(atlas, sprite, out int w, out int h);
            int minX = w, maxX = 0, minY = h, maxY = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a >= 250) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
            int cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
            double r = 0, g = 0, b = 0; int n = 0;
            for (int y = cy - 10; y < cy + 10; y++)
                for (int x = cx - 10; x < cx + 10; x++) { var p = px[y * w + x]; r += p.r; g += p.g; b += p.b; n++; }
            return new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }

        private static float Luminance(Color32 c)
        {
            float R(float v) { v /= 255f; return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f); }
            return 0.2126f * R(c.r) + 0.7152f * R(c.g) + 0.0722f * R(c.b);
        }

        private static float Contrast(Color32 a, Color32 b) { float la = Luminance(a), lb = Luminance(b); return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f); }

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

        private static Color32 Design(int i) => new Color32((byte)(StroopContract.Colors[i].r * 255f + 0.5f), (byte)(StroopContract.Colors[i].g * 255f + 0.5f), (byte)(StroopContract.Colors[i].b * 255f + 0.5f), 255);

        [Test]
        public void EveryInkButton_KeepsItsInkColor_UnderThePlasticLight()
        {
            Assert.IsTrue(PlasticSlab.AvailableCustom(StroopSprites.PlasticGroup, "losa_md_boton_rojo"), "falta el atlas de plasticina de Tinta o Palabra");
            var atlas = ReadAtlas();
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    var face = Face(atlas, "losa_md_boton_" + Names[i]);
                    float d = DeltaE(face, Design(i));
                    Assert.LessOrEqual(d, 6f, "botón " + Names[i] + ": la cara es " + face + " y la tinta de diseño " + Design(i) + " (ΔE " + d.ToString("0.0") + "): la luz no debe mover el color de la tinta");
                    var pressed = Face(atlas, "losa_md_boton_" + Names[i] + "_p");
                    Assert.Less(Luminance(pressed), Luminance(face) + 0.001f, "botón " + Names[i] + ": al tocarlo la losa se hunde (más oscura)");
                    Assert.AreNotSame(PlasticArt.Get(StroopSprites.PlasticGroup, "losa_md_boton_" + Names[i]), PlasticArt.Get(StroopSprites.PlasticGroup, "losa_md_boton_" + Names[i] + "_p"), "la presionada es otra imagen");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheFourInks_StayApart_AndTheirNamesReadOnTheButtons()
        {
            var atlas = ReadAtlas();
            try
            {
                var faces = new Color32[4];
                for (int i = 0; i < 4; i++) faces[i] = Face(atlas, "losa_md_boton_" + Names[i]);
                for (int i = 0; i < 4; i++)
                    for (int j = i + 1; j < 4; j++)
                        Assert.GreaterOrEqual(DeltaE(faces[i], faces[j]), 30f, Names[i] + " y " + Names[j] + ": las tintas deben seguir lejos entre sí (la separación con daltonismo la mide mide_stroop.py)");
                // el nombre: blanco sobre ROJO (5,0 de diseño), tinta sobre AZUL (4,9), AMARILLO (11,2) y BLANCO (15,9); se pide ≥ 4,5
                Assert.GreaterOrEqual(Contrast(White, faces[0]), 4.5f, "BLANCO sobre ROJO");
                Assert.GreaterOrEqual(Contrast(Ink, faces[1]), 4.5f, "tinta sobre AZUL");
                Assert.GreaterOrEqual(Contrast(Ink, faces[2]), 4.5f, "tinta sobre AMARILLO");
                Assert.GreaterOrEqual(Contrast(Ink, faces[3]), 4.5f, "tinta sobre BLANCO");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheCardFill_IsStill141B36_SoTheMeasuredContrastsOfTheInksHold()
        {
            var atlas = ReadAtlas();
            try
            {
                var fill = Face(atlas, "losa_gr_tarjeta_fondo");
                Assert.LessOrEqual(DeltaE(fill, CardFill), 5f, "el fondo de la tarjeta (" + fill + ") se aleja de #141B36");
                // lo que rendía cada tinta sobre #141B36 (palabra grande y con brillo); ahora sobre el fondo de plasticina no baja más de 0,3
                float[] design = { 3.4f, 4.7f, 10.8f, 15.4f };
                for (int i = 0; i < 4; i++)
                    Assert.GreaterOrEqual(Contrast(Design(i), fill), design[i] - 0.3f, "la palabra " + Names[i].ToUpper() + " sobre el fondo de plasticina");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheShoreRibbonAndCornerSlabs_Exist_WithTheirRuleColors()
        {
            foreach (var n in new[] { "losa_md_cinta_tinta", "losa_md_cinta_palabra", "losa_gr_tarjeta_fondo", "losa_gr_tarjeta_tinta", "losa_gr_tarjeta_palabra", "losa_ch_chip_tinta", "losa_ch_chip_palabra" })
                Assert.IsTrue(PlasticSlab.AvailableCustom(StroopSprites.PlasticGroup, n), "falta la losa " + n);
            var atlas = ReadAtlas();
            try
            {
                Assert.LessOrEqual(DeltaE(Face(atlas, "losa_gr_tarjeta_tinta"), new Color32(0x7C, 0x5C, 0xE0, 255)), 6f, "la orilla de la TINTA (violeta)");
                Assert.LessOrEqual(DeltaE(Face(atlas, "losa_gr_tarjeta_palabra"), new Color32(0x15, 0x9A, 0x8C, 255)), 6f, "la orilla de la PALABRA (turquesa)");
            }
            finally { Object.DestroyImmediate(atlas); }
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Button, SlabTone.Coral), "el borde coral de la tarjeta al fallar");
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Panel, SlabTone.Night) || PlasticSlab.Available(SlabSize.Button, SlabTone.Night), "el panel del final");
        }

        [Test]
        public void TheCustomSlabs_HaveTheirNineSliceAndMargins_LikeTheKitOnes()
        {
            var g = PlasticArt.Group(StroopSprites.PlasticGroup);
            Assert.IsNotNull(g);
            int slabs = 0;
            foreach (var name in g.Names)
            {
                if (!name.StartsWith("losa_")) continue;
                var e = g.Entry(name);
                Assert.IsTrue(e.borde != null && e.borde.Length == 4, name + ": sin borde de nine-slice");
                foreach (var b in e.borde) Assert.Greater(b, 20, name + ": borde de nine-slice demasiado chico");
                Assert.IsTrue(e.margen != null && e.margen.Length == 4, name + ": sin márgenes de sombra");
                Assert.Greater(PlasticSlab.MarginsCustom(StroopSprites.PlasticGroup, name).x, 0f, name);
                slabs++;
            }
            Assert.AreEqual(15, slabs, "4 botones × 2 (normal y presionada), 2 de cinta, 3 de tarjeta y 2 de esquina");
        }

        [Test]
        public void TheRuleIcons_KeepTheirSizeOfToday_AndStayLightSoTheCodeCanTintThem()
        {
            var atlas = ReadAtlas();
            try
            {
                foreach (var pair in new[] { ("gota", StroopSprites.RenderDrop(128)), ("libro", StroopSprites.RenderBook(128)) })
                {
                    var plastic = Pixels(atlas, pair.Item1, out int w, out int h);
                    Assert.AreEqual(128, w); Assert.AreEqual(128, h);
                    int pArea = 0, oArea = 0, pMinX = w, pMaxX = 0, oMinX = w, oMaxX = 0;
                    for (int y = 0; y < 128; y++)
                        for (int x = 0; x < 128; x++)
                        {
                            if (plastic[y * 128 + x].a >= 250) { pArea++; pMinX = Mathf.Min(pMinX, x); pMaxX = Mathf.Max(pMaxX, x); }
                            if (pair.Item2[y * 128 + x].a >= 250) { oArea++; oMinX = Mathf.Min(oMinX, x); oMaxX = Mathf.Max(oMaxX, x); }
                        }
                    float ratio = pArea / (float)oArea;
                    Assert.That(ratio, Is.InRange(0.75f, 1.3f), pair.Item1 + ": ocupa " + (ratio * 100f).ToString("0") + " % del de hoy (misma huella y mismo tamaño)");
                    Assert.AreEqual((oMinX + oMaxX) / 2f, (pMinX + pMaxX) / 2f, 5f, pair.Item1 + ": corrido a los lados");
                    double l = 0; int n = 0;
                    foreach (var p in plastic) if (p.a >= 250 && Luminance(p) > 0.4f) { l += Luminance(p); n++; }
                    Assert.Greater(n, 1000, pair.Item1 + ": el cuerpo claro");
                    Assert.Greater(l / n, 0.6, pair.Item1 + ": debe ser claro (el código lo tiñe con Image.color: multiplica)");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void WithoutThePlasticAtlas_StroopKeepsItsProceduralArt()
        {
            PlasticArt.Disabled = true;
            Assert.IsFalse(StroopSprites.Plastic);
            Assert.IsNotNull(StroopSprites.Drop());
            Assert.IsNotNull(StroopSprites.Book());
            Assert.IsNotNull(StroopSprites.Beam());
            Assert.IsFalse(PlasticSlab.AvailableCustom(StroopSprites.PlasticGroup, "losa_md_boton_rojo"), "sin plasticina no hay losas con el color de la tinta: queda la ficha de siempre");
        }
    }
}
