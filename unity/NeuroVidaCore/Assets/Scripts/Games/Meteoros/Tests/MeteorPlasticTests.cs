using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Meteoros.Tests
{
    /// <summary>
    /// Lluvia de meteoros en plasticina (Tarea 105): el arte de Blender cumple lo que prometió. La roca (nine-slice, 4 siluetas) tiene sus bordes de nine-slice y sus capas (brasa, grietas), la roca presionada es la misma pieza aplastada, más oscura y con la huella en una esquina
    /// (y no se sale del lienzo ni «salta»), la roca tintada conserva su color (el sol es el dorado de la bonificación: no se pierde), la palabra de la placa (tinta sobre la losa crema del kit) conserva su contraste, las dos estrellas de la constelación son redondas y se distinguen,
    /// y sin el atlas todo sigue funcionando con el arte de siempre. Las medidas con números las saca <c>tools/juegos/mide_meteoros.py</c>.
    /// </summary>
    public sealed class MeteorPlasticTests
    {
        private static readonly Color32 Sky = new Color32(0x0A, 0x0F, 0x33, 255);
        private static readonly Color32 Ink = new Color32(0x1A, 0x12, 0x40, 255);

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        private static Texture2D ReadAtlas(string group)
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", group, "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        private static Color32[] Pixels(Texture2D atlas, string group, string sprite, out int w, out int h)
        {
            var e = PlasticArt.Group(group).Entry(sprite);
            Assert.IsNotNull(e, "falta «" + sprite + "» en el atlas «" + group + "»");
            w = e.w; h = e.h;
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

        private struct Body { public int Width, Height, MinX, MaxX, MinY, MaxY, Area; public Color Mean; }

        private static Body Measure(Color32[] px, int w, int h)
        {
            var b = new Body { MinX = w, MinY = h };
            double r = 0, g = 0, bl = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = px[y * w + x];
                    if (p.a < 250) continue;
                    b.MinX = Mathf.Min(b.MinX, x); b.MaxX = Mathf.Max(b.MaxX, x); b.MinY = Mathf.Min(b.MinY, y); b.MaxY = Mathf.Max(b.MaxY, y); b.Area++;
                    r += p.r; g += p.g; bl += p.b;
                }
            b.Width = b.MaxX - b.MinX + 1; b.Height = b.MaxY - b.MinY + 1;
            b.Mean = new Color((float)(r / b.Area) / 255f, (float)(g / b.Area) / 255f, (float)(bl / b.Area) / 255f);
            return b;
        }

        [Test]
        public void TheRocks_AreNineSlice_WithTheirLayersOnTheSameCanvas()
        {
            Assert.IsTrue(MeteorSprites.Plastic, "falta el atlas de plasticina de Lluvia de meteoros");
            for (int i = 0; i < MeteorSprites.ShapeCount; i++)
            {
                var body = MeteorSprites.RockBody(i);
                Assert.IsNotNull(body, "roca " + i);
                Assert.IsNotNull(MeteorSprites.RockPressed(i), "roca presionada " + i);
                Assert.IsNotNull(MeteorSprites.Ember(i), "brasa " + i);
                Assert.IsNotNull(MeteorSprites.Cracks(i), "grietas " + i);
                Assert.AreEqual(300, (int)body.rect.width, "roca " + i + ": ancho"); Assert.AreEqual(228, (int)body.rect.height, "roca " + i + ": alto");
                foreach (var s in new[] { MeteorSprites.RockPressed(i), MeteorSprites.Ember(i), MeteorSprites.Cracks(i) })
                    Assert.AreEqual(body.rect.size, s.rect.size, "las capas de la roca " + i + " van en el mismo lienzo");
                // nine-slice: las esquinas no se estiran; el centro (0,64 × 0,4 unidades) sí
                Assert.AreEqual(112f, body.border.x, 0.5f); Assert.AreEqual(112f, body.border.z, 0.5f);
                Assert.AreEqual(90f, body.border.y, 0.5f); Assert.AreEqual(90f, body.border.w, 0.5f);
                Assert.AreEqual(body.border, MeteorSprites.RockPressed(i).border, "la presionada se estira igual");
            }
            var margin = MeteorSprites.RockMargin;
            Assert.Greater(margin.x, 20f, "lo que sobresale la imagen a cada lado (sombra y resplandor), en unidades");
            Assert.Greater(margin.z, 20f, "abajo");
            Assert.Greater(margin.y, margin.z, "arriba sobra más que abajo (la cúpula sube y la sombra cae): por eso el juego corre la imagen para centrar el cuerpo");
        }

        [Test]
        public void TheRockBody_IsTheSizeOfTheOldOne_AndStaysInsideItsCanvas()
        {
            var atlas = ReadAtlas(MeteorSprites.PlasticGroup);
            try
            {
                for (int i = 0; i < MeteorSprites.ShapeCount; i++)
                {
                    var px = Pixels(atlas, MeteorSprites.PlasticGroup, "roca_" + i, out int w, out int h);
                    var b = Measure(px, w, h);
                    // el cuerpo mide ~2 × 1,3 unidades de diseño (el de hoy: la elipse de 0,9 × 0,62 con ondulaciones) a 120 px por unidad
                    Assert.That(b.Width / 120f, Is.InRange(1.8f, 2.3f), "roca " + i + ": ancho del cuerpo (unidades)");
                    Assert.That(b.Height / 120f, Is.InRange(1.15f, 1.7f), "roca " + i + ": alto del cuerpo (unidades)");
                    Assert.GreaterOrEqual(Mathf.Min(b.MinX, b.MinY, w - 1 - b.MaxX, h - 1 - b.MaxY), 12, "roca " + i + ": no deja margen para la sombra y la brasa");
                    // la luz de la roca: gris claro (el código la tiñe)
                    Assert.Greater(Luminance(b.Mean), 0.6f, "roca " + i + ": se tiñe multiplicando: tiene que ser clara");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void ThePressedRocks_AreSquashed_Darker_AndDoNotJump()
        {
            var atlas = ReadAtlas(MeteorSprites.PlasticGroup);
            try
            {
                for (int i = 0; i < MeteorSprites.ShapeCount; i++)
                {
                    var normal = Pixels(atlas, MeteorSprites.PlasticGroup, "roca_" + i, out int w, out int h);
                    var pressed = Pixels(atlas, MeteorSprites.PlasticGroup, "roca_p_" + i, out int w2, out int h2);
                    Assert.AreEqual(w, w2); Assert.AreEqual(h, h2);
                    var a = Measure(normal, w, h); var b = Measure(pressed, w, h);
                    Assert.That(b.Width / (float)a.Width, Is.InRange(1.0f, 1.08f), "presionada " + i + ": un poco más ancha (" + b.Width + " contra " + a.Width + " px)");
                    Assert.LessOrEqual(b.Height / (float)a.Height, 1.12f, "presionada " + i + ": no crece de alto");
                    Assert.GreaterOrEqual(Mathf.Min(b.MinX, b.MinY, w - 1 - b.MaxX, h - 1 - b.MaxY), 6, "presionada " + i + ": se pega al borde del lienzo");
                    float shift = ((a.MinY + a.MaxY) - (b.MinY + b.MaxY)) / 2f;
                    Assert.LessOrEqual(Mathf.Abs(shift), 6f, "presionada " + i + ": al cambiar de imagen «salta» " + shift.ToString("0.0") + " px");
                    float ratio = Luminance(b.Mean) / Luminance(a.Mean);
                    Assert.That(ratio, Is.InRange(0.75f, 0.97f), "presionada " + i + ": un punto más oscura (luminosidad " + ratio.ToString("0.00") + " de la normal)");
                    Assert.LessOrEqual(DeltaE(a.Mean, b.Mean), 9f, "presionada " + i + ": el color se aleja de la normal");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheEmber_GlowsOnTheLeadingEdge_AndTheCracksSitOnTheSides()
        {
            var atlas = ReadAtlas(MeteorSprites.PlasticGroup);
            try
            {
                for (int i = 0; i < MeteorSprites.ShapeCount; i++)
                {
                    var ember = Pixels(atlas, MeteorSprites.PlasticGroup, "brasa_" + i, out int w, out int h);
                    double lowRight = 0, upLeft = 0, red = 0, total = 0;
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            var p = ember[y * w + x];
                            float a = p.a / 255f; if (a < 0.05f) continue;
                            total += a; red += a * (p.r >= 200 && p.b <= 120 ? 1 : 0);
                            if (x > w / 2 && y < h / 2) lowRight += a;       // las filas van de abajo hacia arriba
                            if (x < w / 2 && y > h / 2) upLeft += a;
                        }
                    Assert.Greater(total, 400, "brasa " + i + ": se ve");
                    Assert.Greater(lowRight, 3.0 * upLeft + 1, "brasa " + i + ": va en el borde que va por delante (abajo a la derecha)");
                    Assert.Greater(red / total, 0.8, "brasa " + i + ": es naranja-coral (nunca solo un color: además brilla junto al borde)");

                    var cracks = Pixels(atlas, MeteorSprites.PlasticGroup, "grietas_" + i, out int cw, out int ch);
                    int inkPx = 0, left = 0, right = 0;
                    for (int y = 0; y < ch; y++)
                        for (int x = 0; x < cw; x++)
                            if (cracks[y * cw + x].a >= 200) { inkPx++; if (x < cw * 0.4f) left++; else if (x > cw * 0.6f) right++; }
                    Assert.Greater(inkPx, 150, "grietas " + i + ": se ven");
                    Assert.Greater(left, 40, "grietas " + i + ": a la izquierda de la placa"); Assert.Greater(right, 40, "grietas " + i + ": a la derecha de la placa");
                    Assert.Less(inkPx - left - right, inkPx * 0.15f, "grietas " + i + ": el centro lo tapa la palabra");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheTintedRocks_KeepTheirColor_AndTheGoldOneStaysGold()
        {
            var atlas = ReadAtlas(MeteorSprites.PlasticGroup);
            try
            {
                for (int i = 0; i < MeteorSprites.ShapeCount; i++)
                {
                    var px = Pixels(atlas, MeteorSprites.PlasticGroup, "roca_" + i, out int w, out int h);
                    var mean = Measure(px, w, h).Mean;
                    foreach (RockTint tint in System.Enum.GetValues(typeof(RockTint)))
                    {
                        var design = MeteorSprites.TintColor(tint);
                        var tinted = new Color(mean.r * design.r, mean.g * design.g, mean.b * design.b);
                        Assert.LessOrEqual(DeltaE(tinted, design), 16f, "roca " + i + " teñida de " + tint + ": se aleja de su color");
                    }
                    var gold = MeteorSprites.TintColor(RockTint.Gold);
                    Assert.GreaterOrEqual(DeltaE(new Color(mean.r * gold.r, mean.g * gold.g, mean.b * gold.b), MeteorSprites.TintColor(RockTint.Grape)), 20f, "el dorado (bonificación) se sigue distinguiendo de las otras");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheWordOnThePlaque_KeepsItsContrast_AndThePlaqueStandsApartFromEveryRock()
        {
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Chip, SlabTone.Cream) || PlasticSlab.Available(SlabSize.Button, SlabTone.Cream), "la placa: losa chica crema del kit");
            var comun = ReadAtlas(PlasticSlab.Group);
            Color cream;
            try
            {
                var px = Pixels(comun, PlasticSlab.Group, "losa_ch_crema", out int sw, out int sh);
                double rr = 0, gg = 0, bb = 0; int n = 0;
                for (int y = (int)(sh * 0.4f); y < (int)(sh * 0.6f); y++)
                    for (int x = (int)(sw * 0.4f); x < (int)(sw * 0.6f); x++) { var c = px[y * sw + x]; rr += c.r; gg += c.g; bb += c.b; n++; }
                Assert.Greater(n, 20);
                cream = new Color((float)(rr / n) / 255f, (float)(gg / n) / 255f, (float)(bb / n) / 255f);
            }
            finally { Object.DestroyImmediate(comun); }
            Assert.GreaterOrEqual(Contrast(Ink, cream), 7f, "la palabra de tinta sobre la losa crema");
            // la placa contra cada roca (la roca teñida más oscura y la más clara): se separa por tono, y además por su sombra
            var atlas = ReadAtlas(MeteorSprites.PlasticGroup);
            try
            {
                var px = Pixels(atlas, MeteorSprites.PlasticGroup, "roca_0", out int w, out int h);
                var mean = Measure(px, w, h).Mean;
                foreach (RockTint tint in System.Enum.GetValues(typeof(RockTint)))
                {
                    var d = MeteorSprites.TintColor(tint);
                    var tinted = new Color(mean.r * d.r, mean.g * d.g, mean.b * d.b);
                    Assert.GreaterOrEqual(Contrast(cream, tinted), 1.15f, "la placa crema se separa de la roca " + tint + " (" + Contrast(cream, tinted).ToString("0.00") + ":1)");
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheConstellationStars_AreRoundSpheres_AndLitAndDimDiffer()
        {
            var atlas = ReadAtlas(MeteorSprites.PlasticGroup);
            try
            {
                var lit = Pixels(atlas, MeteorSprites.PlasticGroup, "estrella_encendida", out int w, out int h);
                var dim = Pixels(atlas, MeteorSprites.PlasticGroup, "estrella_apagada", out int w2, out int h2);
                Assert.AreEqual(96, w); Assert.AreEqual(96, h); Assert.AreEqual(w, w2);
                var a = Measure(lit, w, h); var b = Measure(dim, w, h);
                foreach (var m in new[] { a, b })
                {
                    float circle = Mathf.PI * (m.Width / 2f) * (m.Width / 2f);
                    Assert.That(m.Area / circle, Is.InRange(0.96f, 1.04f), "redonda: sin puntas (nada de estrella con puntas)");
                    Assert.AreEqual(m.Width, m.Height, 3f);
                    Assert.That(m.Width / (float)w, Is.InRange(0.45f, 0.65f), "el cuerpo ocupa lo que el de hoy (~0,55 del lienzo)");
                }
                Assert.GreaterOrEqual(Contrast(a.Mean, b.Mean), 3f, "encendida y apagada se distinguen por luminosidad, no solo por color");
                Assert.GreaterOrEqual(Contrast(a.Mean, Sky), 6f, "la encendida contra el cielo");
                Assert.GreaterOrEqual(Contrast(b.Mean, Sky), 1.3f, "la apagada se ve contra el cielo");
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void WithoutThePlasticAtlas_MeteorsKeepTheirProceduralArt()
        {
            PlasticArt.Disabled = true;
            Assert.IsFalse(MeteorSprites.Plastic);
            Assert.IsNull(MeteorSprites.RockBody(0)); Assert.IsNull(MeteorSprites.RockPressed(0)); Assert.IsNull(MeteorSprites.Ember(0)); Assert.IsNull(MeteorSprites.Cracks(0));
            Assert.AreEqual(Vector3.zero, MeteorSprites.RockMargin);
            for (int i = 0; i < MeteorSprites.ShapeCount; i++) Assert.IsNotNull(MeteorSprites.Rock(i, RockTint.Grape), "roca " + i);
            Assert.IsNotNull(MeteorSprites.Rock(0, RockTint.Dust));
            Assert.IsNotNull(MeteorSprites.StarLit()); Assert.IsNotNull(MeteorSprites.StarDim());
            int step = 0;
            Assert.IsTrue(MeteorSprites.Prewarm(ref step), "sin plasticina se siguen horneando las rocas durante la cuenta regresiva");
        }
    }
}
