using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// Los estados PRESIONADOS de la Tarea 104 (regla del 14-oct: todo objeto de plasticina que se toca tiene su imagen presionada: aplastada, un punto más oscura y con la huella de un dedo): la señal de Piloto, el botón de Freno, el satélite de Satélites y la ficha-letra de Punta.
    /// Cada una, contra su imagen normal: el mismo lienzo, algo más ancha (no se sale del lienzo), el centro no «salta», un punto más oscura sin alejarse del color (ΔE) y, en las señales, el detalle de tinta (el estímulo) no se pisa. La huella se mide con
    /// <c>tools/juegos/mide_presionados.py</c> (la ve en la hoja de capturas). Los luceros de Rastro tienen su propia prueba (<c>RastroPlasticTests</c>).
    /// </summary>
    public sealed class PressedSpritesTests
    {
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

        private static Color32[] Pixels(Texture2D atlas, string group, string sprite, out int size)
        {
            var e = PlasticArt.Group(group).Entry(sprite);
            Assert.IsNotNull(e, "falta «" + sprite + "» en el atlas «" + group + "»");
            Assert.AreEqual(e.w, e.h, sprite + ": el lienzo es cuadrado");
            size = e.w;
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

        private struct Body { public int Width, Height, MinX, MaxX, MinY, MaxY; public Color Mean; public int Ink; }

        private static Body Measure(Color32[] px, int n)
        {
            var b = new Body { MinX = n, MinY = n };
            double r = 0, g = 0, bl = 0; int count = 0;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var p = px[y * n + x];
                    if (p.a < 250) continue;
                    b.MinX = Mathf.Min(b.MinX, x); b.MaxX = Mathf.Max(b.MaxX, x); b.MinY = Mathf.Min(b.MinY, y); b.MaxY = Mathf.Max(b.MaxY, y);
                    r += p.r; g += p.g; bl += p.b; count++;
                    if (Luminance(new Color32(p.r, p.g, p.b, 255)) < 0.04f) b.Ink++;
                }
            b.Width = b.MaxX - b.MinX + 1; b.Height = b.MaxY - b.MinY + 1;
            b.Mean = new Color((float)(r / count) / 255f, (float)(g / count) / 255f, (float)(bl / count) / 255f);
            return b;
        }

        private static void CheckPair(Texture2D atlas, string group, string normalName, string pressedName, bool checkInk)
        {
            var normal = Pixels(atlas, group, normalName, out int n);
            var pressed = Pixels(atlas, group, pressedName, out int m);
            Assert.AreEqual(n, m, pressedName + ": el mismo lienzo que el normal");
            var a = Measure(normal, n); var b = Measure(pressed, n);
            Assert.That(b.Width / (float)a.Width, Is.InRange(1.0f, 1.08f), pressedName + ": un poco más ancha que la normal (" + b.Width + " contra " + a.Width + " px)");
            Assert.LessOrEqual(b.Height / (float)a.Height, 1.12f, pressedName + ": no crece de alto");
            Assert.GreaterOrEqual(Mathf.Min(b.MinX, b.MinY, n - 1 - b.MaxX, n - 1 - b.MaxY), 3, pressedName + ": se pega al borde del lienzo (se recortaría)");
            float shift = ((a.MinY + a.MaxY) - (b.MinY + b.MaxY)) / 2f;
            Assert.LessOrEqual(Mathf.Abs(shift), 4f, pressedName + ": al cambiar de imagen «salta» " + shift.ToString("0.0") + " px");
            float ratio = Luminance(b.Mean) / Luminance(a.Mean);
            Assert.That(ratio, Is.InRange(0.75f, 0.97f), pressedName + ": un punto más oscura (luminosidad " + ratio.ToString("0.00") + " de la normal)");
            Assert.LessOrEqual(DeltaE(a.Mean, b.Mean), 9f, pressedName + ": el color se aleja de la normal");
            if (checkInk) Assert.GreaterOrEqual(b.Ink / (float)a.Ink, 0.95f, pressedName + ": el detalle de tinta (el estímulo) no se pisa");
        }

        [Test]
        public void ThePressedPilotSignals_AreSquashedAndDarker_AndKeepTheirInkDetail()
        {
            var atlas = ReadAtlas("piloto");
            try
            {
                foreach (var f in new[] { "hexagono", "gota", "circulo", "cuadrado", "triangulo" })
                    foreach (var d in new[] { "punto", "anillo", "franja" })
                        CheckPair(atlas, "piloto", "senal_" + f + "_" + d, "senal_p_" + f + "_" + d, true);
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void ThePressedBrakeButtons_AreSquashedAndDarker()
        {
            var atlas = ReadAtlas("freno");
            try
            {
                CheckPair(atlas, "freno", "boton_encendido", "boton_encendido_p", false);
                CheckPair(atlas, "freno", "boton_apagado", "boton_apagado_p", false);
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void ThePressedSatellite_AndTheLetterTile_AreSquashedAndDarker()
        {
            var sat = ReadAtlas("satelites");
            var punta = ReadAtlas("punta");
            try
            {
                CheckPair(sat, "satelites", "satelite", "satelite_p", false);
                CheckPair(punta, "punta", "ficha", "ficha_p", false);
            }
            finally { Object.DestroyImmediate(sat); Object.DestroyImmediate(punta); }
        }

        [Test]
        public void TheLetterOnThePressedTile_StillReads_OnEveryState()
        {
            // la ficha-letra presionada se tiñe como la normal (crema, fija por ayuda y los cuatro metales): la letra de tinta sigue leyéndose (≥ 4,5:1)
            var atlas = ReadAtlas("punta");
            try
            {
                var px = Pixels(atlas, "punta", "ficha_p", out int n);
                double r = 0, g = 0, b = 0; int count = 0;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        var p = px[y * n + x];
                        if (p.a < 250 || Mathf.Sqrt((x - n / 2f) * (x - n / 2f) + (y - n / 2f) * (y - n / 2f)) > 0.55f * 0.8f * n) continue;
                        r += p.r; g += p.g; b += p.b; count++;
                    }
                var gray = new Color((float)(r / count) / 255f, (float)(g / count) / 255f, (float)(b / count) / 255f);
                var ink = new Color(0x1A / 255f, 0x12 / 255f, 0x40 / 255f);
                var states = new[]
                {
                    new Color(244f / 255f, 241f / 255f, 1f), new Color(247f / 255f, 221f / 255f, 148f / 255f), new Color(1f, 201f / 255f, 74f / 255f),
                    new Color(214f / 255f, 222f / 255f, 240f / 255f), new Color(230f / 255f, 150f / 255f, 90f / 255f), new Color(127f / 255f, 170f / 255f, 1f),
                };
                foreach (var tint in states)
                {
                    float la = Luminance(gray * tint), lb = Luminance(ink);
                    Assert.GreaterOrEqual((Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f), 4.5f, "la letra sobre la ficha presionada teñida de " + tint);
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }
    }
}
