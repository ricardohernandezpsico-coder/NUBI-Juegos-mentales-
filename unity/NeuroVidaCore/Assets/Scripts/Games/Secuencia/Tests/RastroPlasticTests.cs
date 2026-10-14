using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Secuencia.Tests
{
    /// <summary>
    /// Rastro de luz en plasticina (Tarea 99; el estado presionado de cada lucero, regla del 14-oct): el arte de Blender cumple lo que prometió. Los 9 luceros ocupan el lugar y el tamaño de los de hoy, son IDÉNTICOS entre sí (la identidad de un lucero es su posición, su color y su nota), conservan su color de diseño y los pares cercanos
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

        private static Color32[] Pixels(Texture2D atlas, int orb) => PixelsOf(atlas, "lucero_" + orb);

        private static Color32[] PixelsOf(Texture2D atlas, string name)
        {
            var e = PlasticArt.Group(RastroSprites.PlasticGroup).Entry(name);
            Assert.IsNotNull(e, "falta «" + name + "» en el atlas de Rastro de luz");
            Assert.AreEqual(Px, e.w, name + ": ancho");
            Assert.AreEqual(Px, e.h, name + ": el lienzo es cuadrado, como el del procedural");
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

        private static void Box(Color32[] px, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = Px; maxX = 0; minY = Px; maxY = 0;
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++)
                    if (px[y * Px + x].a >= 250) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        }

        /// <summary>El detalle fino de la cara (la desviación de la luminosidad menos su desenfoque de 7 px): lo que suma la huella, sin el degradé de la luz.</summary>
        private static float FineDetail(Color32[] px)
        {
            var lum = new float[Px * Px];
            for (int i = 0; i < lum.Length; i++) lum[i] = Luminance(px[i]) * 255f;
            var tmp = new float[Px * Px]; var blur = new float[Px * Px];
            const int r = 3;
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++) { float a = 0; for (int k = -r; k <= r; k++) a += lum[y * Px + Mathf.Clamp(x + k, 0, Px - 1)]; tmp[y * Px + x] = a / (2 * r + 1); }
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++) { float a = 0; for (int k = -r; k <= r; k++) a += tmp[Mathf.Clamp(y + k, 0, Px - 1) * Px + x]; blur[y * Px + x] = a / (2 * r + 1); }
            double sum = 0, sum2 = 0; int n = 0;
            float cy = Px / 2f + 0.05f * Px / 2f;
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++)
                {
                    if (px[y * Px + x].a < 250 || Mathf.Sqrt((x - Px / 2f) * (x - Px / 2f) + (y - cy) * (y - cy)) > 0.5f * Px / 2f) continue;
                    float d = lum[y * Px + x] - blur[y * Px + x]; sum += d; sum2 += d * d; n++;
                }
            Assert.Greater(n, 3000, "la cara del lucero");
            return Mathf.Sqrt((float)(sum2 / n - (sum / n) * (sum / n)));
        }

        [Test]
        public void ThePressedOrbs_AreSquashed_Darker_WithAFingerprint_AndKeepTheColorRules()
        {
            // regla del 14-oct: todo objeto de plasticina que se toca tiene su estado presionado, aplastado y con huella. El color es la identidad: la regla de ΔE sigue valiendo en los dos estados.
            Assert.IsTrue(RastroSprites.HasPressed, "faltan los luceros presionados en el atlas");
            var atlas = ReadAtlas();
            try
            {
                var faces = new Color32[RastroBoard.Orbs];
                for (int i = 0; i < RastroBoard.Orbs; i++)
                {
                    var normal = Pixels(atlas, i); var pressed = PixelsOf(atlas, "lucero_p_" + i);
                    Box(normal, out int nMinX, out int nMaxX, out int nMinY, out int nMaxY);
                    Box(pressed, out int pMinX, out int pMaxX, out int pMinY, out int pMaxY);
                    float nW = nMaxX - nMinX + 1, pW = pMaxX - pMinX + 1, nH = nMaxY - nMinY + 1, pH = pMaxY - pMinY + 1;
                    Assert.That(pW / nW, Is.InRange(1.01f, 1.08f), "lucero " + i + ": el presionado es más ancho que el normal (" + pW + " contra " + nW + " px)");
                    Assert.LessOrEqual(pH, nH + 3f, "lucero " + i + ": el presionado no es más alto");
                    Assert.Less(pH / pW, 1f, "lucero " + i + ": aplastado (más ancho que alto)");
                    float drop = (nMinY + nMaxY) / 2f - (pMinY + pMaxY) / 2f;
                    Assert.LessOrEqual(Mathf.Abs(drop), 2.5f, "lucero " + i + ": al cambiar de imagen el lucero «salta» " + drop.ToString("0.0") + " px (el lienzo va centrado en la esfera aplastada)");
                    Assert.Greater(Mathf.Min(pMinX, pMinY, Px - 1 - pMaxX, Px - 1 - pMaxY), 0, "lucero " + i + ": el presionado se pega al borde del lienzo");
                    faces[i] = FaceColor(pressed);
                    var faceNormal = FaceColor(normal);
                    Assert.Less(Luminance(faces[i]), Luminance(faceNormal), "lucero " + i + ": el presionado es un punto más oscuro");
                    Assert.That(DeltaE(faces[i], faceNormal), Is.InRange(1.5f, 8f), "lucero " + i + ": el presionado se aleja demasiado del normal (o no se nota)");
                    Assert.Less(DeltaE(faces[i], Design(i)), 14f, "lucero " + i + ": el presionado se aleja del color de diseño");
                    Assert.GreaterOrEqual(Contrast(Luminance(faces[i]), Luminance(Sky)), 4.5f, "lucero " + i + ": el presionado contra el cielo");
                    float fineN = FineDetail(normal), fineP = FineDetail(pressed);
                    Assert.GreaterOrEqual(fineP, 2f, "lucero " + i + ": la huella se tiene que ver");
                    Assert.GreaterOrEqual(fineP, 3f * fineN, "lucero " + i + ": el presionado trae la huella (" + fineP.ToString("0.0") + " contra " + fineN.ToString("0.0") + " del normal)");
                }
                for (int i = 0; i < RastroBoard.Orbs; i++)
                    for (int j = i + 1; j < RastroBoard.Orbs; j++)
                    {
                        float design = DeltaE(Design(i), Design(j));
                        if (design > 40f) continue;
                        float plastic = DeltaE(faces[i], faces[j]);
                        Assert.GreaterOrEqual(plastic, 12f, "presionados " + i + " y " + j + ": se parecen demasiado (ΔE " + plastic.ToString("0.0") + ")");
                        Assert.GreaterOrEqual(plastic, design * 0.75f, "presionados " + i + " y " + j + ": la plasticina los acerca (ΔE " + plastic.ToString("0.0") + " contra " + design.ToString("0.0") + " de diseño)");
                    }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        private static object GetField(object target, string name) =>
            target.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetValue(target);

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).SetValue(target, value);

        [Test]
        public void AnOrb_ShowsItsPressedImage_WhileTheFingerIsOnIt_AndGoesBackWhenItLetsGo()
        {
            foreach (bool disabled in new[] { false, true })
            {
                PlasticArt.Disabled = disabled;
                bool reduce = GameFeel.ReduceMotion;
                var go = new GameObject("RastroPressedAudit");
                try
                {
                    var controller = go.AddComponent<RastroGameController>();
                    typeof(RastroGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(controller, null);
                    var place = typeof(RastroGameController).GetMethod("PlaceOrbs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var orbs = (System.Array)GetField(controller, "_orbs");
                    Sprite BodyOf(int i) => ((UnityEngine.UI.Image)GetField(orbs.GetValue(i), "Body")).sprite;

                    SetField(controller, "_pressedOrb", 2);                       // el dedo está sobre el lucero 2
                    place.Invoke(controller, new object[] { 0.016f });
                    for (int i = 0; i < RastroBoard.Orbs; i++)
                        Assert.AreEqual(i == 2 && !disabled ? RastroSprites.OrbPressed(2) : RastroSprites.Orb(i), BodyOf(i),
                            (disabled ? "sin plasticina" : "con plasticina") + ": el lucero " + i + (i == 2 ? " con el dedo encima" : " sin tocar"));
                    if (!disabled) Assert.IsNotNull(RastroSprites.OrbPressed(2));

                    GameFeel.ReduceMotion = true;                                  // «quitar animaciones»: solo cambia la imagen, sin rebote
                    place.Invoke(controller, new object[] { 0.016f });
                    var body = ((UnityEngine.UI.Image)GetField(orbs.GetValue(2), "Body")).rectTransform;
                    Assert.AreEqual(Vector3.one, body.localScale, "con «quitar animaciones» el lucero no cambia de escala");
                    GameFeel.ReduceMotion = reduce;

                    SetField(controller, "_pressedOrb", -1);                       // el dedo se levantó: pasados los 80 ms vuelve a la imagen de siempre
                    SetField(orbs.GetValue(2), "PressUntil", -1f);
                    place.Invoke(controller, new object[] { 0.016f });
                    Assert.AreEqual(RastroSprites.Orb(2), BodyOf(2), "al soltar vuelve el lucero de siempre");
                }
                finally { GameFeel.ReduceMotion = reduce; Object.DestroyImmediate(go); }
            }
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
            Assert.IsFalse(RastroSprites.HasPressed);
            for (int i = 0; i < RastroBoard.Orbs; i++) { Assert.IsNotNull(RastroSprites.Orb(i), "lucero " + i); Assert.IsNull(RastroSprites.OrbPressed(i), "sin plasticina no hay lucero presionado: el juego deja el de siempre"); }
            Assert.IsNotNull(RastroSprites.XMark());
            Assert.IsNotNull(RastroSprites.DashedRing());
        }
    }
}
