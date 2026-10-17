using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Engranajes.Tests
{
    /// <summary>
    /// Engranajes «Taller de reparación» en plasticina (Tarea 112): una de las de «la mecánica es la silueta» (docs/plasticina-juegos.md §14). Lo que se mide, y por qué:
    /// <list type="bullet">
    /// <item>LOS DIENTES SON LA MECÁNICA: el contorno es el trapecio exacto de <c>EngranajesSprites.GearPolygon</c> (punta ±0,17 y raíz ±0,32 del paso angular, holgura cero contra el vecino). El ancho de la punta del diente y el del hueco son los de hoy a ±0,3 dp, y
    /// los huecos son transparentes (detrás va el disco oscuro de apoyo). Con el par grande-chico en reposo (<c>MeshPhase</c>) los rellenos no se pisan y lo que se ve de hueco no es mayor que hoy.</item>
    /// <item>LO QUE GIRA no lleva ni luz ni sombra que giren con la pieza: la turbina y el radar se renderizaron desde arriba, sin suelo y con la luz promediada en cuatro; los engranajes giran con su degradé de COLOR y sus rayos (5 y 3), que muestran el sentido del giro.</item>
    /// <item>NADA DE «+»: la turbina tiene 5 aspas (el número no es una pista del juego), el cruce de la correa «ocho» es de unos 31° y el cartel que falla lleva su aspa diagonal.</item>
    /// <item>El sentido (a favor o en contra del reloj) se distingue por la CABEZA de la flecha (espejos), también en escala de grises y a tamaño real.</item>
    /// <item>Lo que se toca tiene su imagen presionada (el engranaje del motor, la polea de la correa tocada, «Arrancar»); el contorno de los dientes no cambia al presionar.</item>
    /// </list>
    /// Estas pruebas miden sobre el ATLAS (se lee el PNG del disco: la textura importada no es legible) y contra el dibujo de siempre (<see cref="PlasticArt.Disabled"/>). Las medidas con números las saca <c>tools/juegos/mide_engranajes.py</c>.
    /// </summary>
    public sealed class EngranajesPlasticTests
    {
        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        // ------------------------------------------------------------------ el atlas (y el dibujo de siempre) como píxeles

        private static readonly Dictionary<string, Texture2D> Atlases = new Dictionary<string, Texture2D>();

        private static Texture2D Atlas(string group)
        {
            if (Atlases.TryGetValue(group, out var t) && t != null) return t;
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", group, "atlas.png");
            t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(t.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            Atlases[group] = t;
            return t;
        }

        private sealed class Img
        {
            public int W, H;
            public float Ppu;
            public Color32[] Px;                                    // origen abajo a la izquierda, como Texture2D
            public Color32 At(int x, int y) => Px[Mathf.Clamp(y, 0, H - 1) * W + Mathf.Clamp(x, 0, W - 1)];
            public Color32 AtDp(float xDp, float yDp) => At(Mathf.RoundToInt(W / 2f + xDp * Ppu - 0.5f), Mathf.RoundToInt(H / 2f + yDp * Ppu - 0.5f));
            public float AlphaAt(float xDp, float yDp) => AtDp(xDp, yDp).a / 255f;
            public float WidthDp => W / Ppu;
            public float HeightDp => H / Ppu;

            /// <summary>Un campo 0..1 (el valor por píxel: el alfa, o el alfa por la luz) interpolado en (xDp, yDp) del centro (y hacia ARRIBA): con el umbral en 0,5 el borde se ubica con precisión de sub-píxel (no de píxel entero).</summary>
            public float Field(Func<Color32, float> value, float xDp, float yDp)
            {
                float fx = W / 2f + xDp * Ppu - 0.5f, fy = H / 2f + yDp * Ppu - 0.5f;
                int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
                float tx = fx - x0, ty = fy - y0;
                float v00 = value(At(x0, y0)), v10 = value(At(x0 + 1, y0)), v01 = value(At(x0, y0 + 1)), v11 = value(At(x0 + 1, y0 + 1));
                return (v00 * (1 - tx) + v10 * tx) * (1 - ty) + (v01 * (1 - tx) + v11 * tx) * ty;
            }
        }

        private static Img Load(string sprite, string group = null)
        {
            group = group ?? EngranajesSprites.PlasticGroup;
            var e = PlasticArt.Group(group).Entry(sprite);
            Assert.IsNotNull(e, "falta «" + sprite + "» en el atlas «" + group + "»");
            var colors = Atlas(group).GetPixels(e.x, e.y, e.w, e.h);
            var px = new Color32[colors.Length];
            for (int i = 0; i < colors.Length; i++) px[i] = colors[i];
            return new Img { W = e.w, H = e.h, Ppu = e.ppu, Px = px };
        }

        /// <summary>El dibujo procedural (de siempre) de un sprite horneado, como píxeles. Hay que llamarlo con <see cref="PlasticArt.Disabled"/> encendido al pedir el sprite.</summary>
        private static Img FromSprite(Sprite s)
        {
            Assert.IsNotNull(s);
            var tex = s.texture;
            return new Img { W = tex.width, H = tex.height, Ppu = s.pixelsPerUnit, Px = tex.GetPixels32() };
        }

        private static float Luma(Color32 c) => (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;

        /// <summary>El relleno CLARO de un engranaje: el alfa por una rampa de luz (la tinta oscura del borde de siempre, #1A1240, vale 0; la placa de plasticina, 1). Lo que se VE de un engranaje sobre la sala oscura.</summary>
        private static float FillValue(Color32 c) => c.a / 255f * Mathf.Clamp01((Luma(c) - 0.12f) / 0.2f);

        /// <summary>La silueta completa (el alfa): con la tinta, en el dibujo de siempre.</summary>
        private static float SilValue(Color32 c) => c.a / 255f;

        /// <summary>La media tinta del borde del engranaje de siempre: el borde oscuro va CENTRADO en el contorno, 1,2 dp hacia adentro y 1,2 hacia afuera (<c>Outlined(..., 1.2f, ...)</c>).</summary>
        private const float InkHalf = 1.2f;

        /// <summary>Cuánto más largo es el arco entre dos flancos paralelos desplazados 1 dp en perpendicular: 1 / cos(inclinación del flanco respecto del radio).</summary>
        private static float FlankSlant(int n, float tip)
        {
            float p = 2f * Mathf.PI / n, root = tip - EngranajesContract.ToothDepth;
            float tangential = root * 0.15f * p;                                          // lo que se corre el flanco de lado al bajar los 6 dp del diente
            return Mathf.Sqrt(1f + tangential * tangential / (EngranajesContract.ToothDepth * EngranajesContract.ToothDepth));
        }

        // ------------------------------------------------------------------ los dientes: el contorno del trapecio y lo que mide hoy

        /// <summary>El semiancho angular (radianes) del trapecio a la distancia [r] del centro, para un diente de [n] dientes y punta en [tip]: la polilínea punta/raíz de <c>EngranajesSprites.GearPolygon</c>.</summary>
        private static float ExpectedHalfAngle(int n, float tip, float r)
        {
            float p = 2f * Mathf.PI / n, root = tip - EngranajesContract.ToothDepth;
            // el flanco va de (0,17 p, tip) a (0,32 p, root) en polares; a la altura r el punto del flanco está a la fracción f de la altura del diente
            var a = new Vector2(Mathf.Cos(0.17f * p) * tip, Mathf.Sin(0.17f * p) * tip);
            var b = new Vector2(Mathf.Cos(0.32f * p) * root, Mathf.Sin(0.32f * p) * root);
            // se busca por bisección el punto del segmento a-b a la distancia r del centro
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 40; i++)
            {
                float mid = (lo + hi) * 0.5f;
                var q = Vector2.Lerp(a, b, mid);
                if (q.magnitude > r) lo = mid; else hi = mid;
            }
            var pt = Vector2.Lerp(a, b, (lo + hi) * 0.5f);
            return Mathf.Atan2(pt.y, pt.x);
        }

        /// <summary>Recorre el círculo de radio [r] (dp) entre −medio paso y +medio paso del diente 0 (a la derecha) y devuelve el ancho de arco (dp) del relleno que rodea al diente y el del hueco hasta los dos lados.</summary>
        private static void ToothAndGap(Img im, Func<Color32, float> value, int n, float r, out float tooth, out float gap)
        {
            float p = 2f * Mathf.PI / n;
            const int steps = 4000;
            int inside = 0;
            float lo = float.NaN, hi = float.NaN;
            for (int i = 0; i <= steps; i++)
            {
                float a = -p / 2f + p * i / steps;
                bool on = im.Field(value, Mathf.Cos(a) * r, Mathf.Sin(a) * r) >= 0.5f;
                if (on) { inside++; if (float.IsNaN(lo)) lo = a; hi = a; }
            }
            Assert.Greater(inside, 0, "no hay diente en el círculo de radio " + r);
            tooth = (hi - lo) * r;
            gap = (p - (hi - lo)) * r;
        }

        [Test]
        public void EverySprite_IsInTheAtlas_AtTheFrameOfTheProceduralOne()
        {
            Assert.IsTrue(EngranajesSprites.Plastic, "falta el atlas de plasticina de Engranajes (correr tools/juegos/empaqueta.py engranajes)");
            var names = new List<string>();
            foreach (int size in new[] { 0, 1 }) { foreach (int pal in new[] { 0, 1, 2 }) names.Add("gear_" + size + "_" + pal); names.Add("gear_" + size + "_0_p"); }
            names.AddRange(new[] { "radar", "turbina", "polea", "polea_p", "porton", "marco_carga", "plataforma", "caja_carga", "barra", "casco", "flecha_grande_cw", "flecha_grande_ccw", "flecha_chico_cw", "flecha_chico_ccw", "miniflecha_cw", "miniflecha_ccw", "tri_arriba", "tri_abajo", "tri_play" });
            names.AddRange(new[] { "domo", "moneda", "placa_neutra", "placa_ok", "placa_mala", "pastilla_motor", "pastilla_motor_oro" });                                  // el volumen (Tarea 115): las luces, las llaves, los carteles y el rótulo MOTOR
            var group = PlasticArt.Group(EngranajesSprites.PlasticGroup);
            foreach (var n in names) Assert.IsNotNull(group.Entry(n), "falta «" + n + "» en el atlas de Engranajes");
            Assert.AreEqual(names.Count, group.Manifest.sprites.Length, "el atlas trae justo las piezas que el juego usa");

            // el mismo cuadro en dp que el dibujo procedural al que reemplaza (así el controlador no cambia ninguna medida)
            PlasticArt.Disabled = true;
            var proc = new Dictionary<string, Sprite>
            {
                { "gear_0_0", EngranajesSprites.Gear(EngranajesSprites.GearSize.Big, EngranajesSprites.Palette.Motor) }, { "gear_1_2", EngranajesSprites.Gear(EngranajesSprites.GearSize.Small, EngranajesSprites.Palette.Station) },
                { "radar", EngranajesSprites.Dish() }, { "turbina", EngranajesSprites.Fan() }, { "polea", EngranajesSprites.Pulley() }, { "porton", EngranajesSprites.Door() }, { "marco_carga", EngranajesSprites.CargoFrame() },
                { "plataforma", EngranajesSprites.CargoPlate() }, { "caja_carga", EngranajesSprites.CargoBox() }, { "barra", EngranajesSprites.Rack() }, { "casco", EngranajesSprites.Hull() },
                { "flecha_grande_cw", EngranajesSprites.Arrow(EngranajesContract.BigTip + 8f, 4f, true) }, { "flecha_chico_ccw", EngranajesSprites.Arrow(EngranajesContract.SmallTip + 8f, 4f, false) },
                { "miniflecha_cw", EngranajesSprites.MiniArrow(true) }, { "tri_arriba", EngranajesSprites.GlyphSprite(EngranajesSprites.Glyph.Up) }, { "tri_play", EngranajesSprites.GlyphSprite(EngranajesSprites.Glyph.Play) }
            };
            var procSize = new Dictionary<string, Vector2>();
            foreach (var kv in proc) procSize[kv.Key] = new Vector2(kv.Value.rect.width / kv.Value.pixelsPerUnit, kv.Value.rect.height / kv.Value.pixelsPerUnit);
            PlasticArt.Disabled = false;
            foreach (var kv in proc)
            {
                var im = Load(kv.Key);
                Assert.AreEqual(procSize[kv.Key].x, im.WidthDp, 0.6f, kv.Key + ": el mismo ancho en dp que el dibujo de siempre");
                Assert.AreEqual(procSize[kv.Key].y, im.HeightDp, 0.6f, kv.Key + ": el mismo alto en dp");
            }
            // los getters devuelven el del atlas (no el horneado)
            Assert.AreNotSame(proc["radar"], EngranajesSprites.Dish());
            Assert.AreNotSame(proc["turbina"], EngranajesSprites.Fan());
            // lo que se queda procedural: la sala, el cubo, la silueta de la sombra, las correas, la llave inglesa
            foreach (var s in new[] { EngranajesSprites.Room(), EngranajesSprites.Hub(), EngranajesSprites.Silhouette(EngranajesSprites.GearSize.Big), EngranajesSprites.BeltCap(16f), EngranajesSprites.Wrench() }) Assert.IsNotNull(s);
        }

        [Test]
        public void TheTeeth_AreTheExactTrapezoid_AndTheContourIsTheOneOfToday_Within0Point3Dp()
        {
            foreach (var size in new[] { EngranajesSprites.GearSize.Big, EngranajesSprites.GearSize.Small })
            {
                float tip = EngranajesSprites.TipOf(size);
                int n = EngranajesSprites.TeethOf(size);
                float p = 2f * Mathf.PI / n;
                PlasticArt.Disabled = true;
                var before = FromSprite(EngranajesSprites.Gear(size, EngranajesSprites.Palette.Motor));
                PlasticArt.Disabled = false;
                var after = Load("gear_" + (int)size + "_0");
                float slant = FlankSlant(n, tip);
                // junto a la punta (el ancho de la punta del diente), a media altura y cerca de la raíz (que está a 6 dp de la punta; más abajo de 4,5 la tinta de hoy llena el hueco y ya no hay contorno que comparar)
                foreach (float r in new[] { tip - 0.5f, tip - 3f, tip - 4.5f })
                {
                    float half = ExpectedHalfAngle(n, tip, r);
                    float expectedTooth = 2f * half * r, expectedGap = (p - 2f * half) * r;
                    ToothAndGap(after, SilValue, n, r, out float toothAfter, out float gapAfter);
                    // el dibujo de siempre: su CONTORNO es el trapecio, con la tinta (1,2 dp) centrada en él; la silueta con tinta es 2,4 dp más ancha que el contorno y su relleno claro, 2,4 más angosto
                    ToothAndGap(before, SilValue, n, r, out float silTooth, out float silGap);
                    float toothBefore = silTooth - 2f * InkHalf * slant, gapBefore = silGap + 2f * InkHalf * slant;
                    string where = size + " a " + r.ToString("0.0") + " dp: ";
                    Assert.AreEqual(expectedTooth, toothAfter, 0.3f, where + "el diente de plasticina es el trapecio exacto (punta ±0,17 · raíz ±0,32 del paso): " + toothAfter.ToString("0.00") + " contra " + expectedTooth.ToString("0.00"));
                    Assert.AreEqual(expectedGap, gapAfter, 0.3f, where + "y el hueco es el exacto: " + gapAfter.ToString("0.00") + " contra " + expectedGap.ToString("0.00"));
                    Assert.AreEqual(toothBefore, toothAfter, 0.3f, where + "el ancho del diente (su contorno) es el de hoy: " + toothBefore.ToString("0.00") + " → " + toothAfter.ToString("0.00"));
                    Assert.AreEqual(gapBefore, gapAfter, 0.3f, where + "el ancho del hueco es el de hoy: " + gapBefore.ToString("0.00") + " → " + gapAfter.ToString("0.00"));
                    // lo que se VE hoy es solo el relleno claro (la tinta, casi igual al fondo, no se distingue): en los flancos el hueco que se ve de hoy es mayor que el de plasticina, nunca menor
                    if (r < tip - 1.5f)
                    {
                        ToothAndGap(before, FillValue, n, r, out _, out float visibleGapBefore);
                        Assert.LessOrEqual(gapAfter, visibleGapBefore + 0.3f, where + "el hueco que se ve no es mayor que hoy: " + visibleGapBefore.ToString("0.00") + " → " + gapAfter.ToString("0.00"));
                    }
                }
                // y pegado a la raíz (0,5 dp) el hueco de plasticina sigue siendo el exacto (la esquina de la raíz no se rellena)
                {
                    float r = tip - 5.5f, half = ExpectedHalfAngle(n, tip, r);
                    ToothAndGap(after, SilValue, n, r, out float toothRoot, out float gapRoot);
                    Assert.AreEqual(2f * half * r, toothRoot, 0.3f, size + " a " + r.ToString("0.0") + " dp: el diente pegado a la raíz es el exacto");
                    Assert.AreEqual((p - 2f * half) * r, gapRoot, 0.3f, size + " a " + r.ToString("0.0") + " dp: y el hueco también");
                }
                // los huecos son transparentes: detrás va el disco oscuro de apoyo, como hoy
                float mid = p / 2f;
                for (int k = 0; k < n; k++)
                {
                    float a = k * p + mid;
                    Assert.Less(after.AlphaAt(Mathf.Cos(a) * (tip - 1.5f), Mathf.Sin(a) * (tip - 1.5f)), 0.05f, size + ": el hueco " + k + " es transparente");
                    Assert.Greater(after.AlphaAt(Mathf.Cos(k * p) * (tip - 1.5f), Mathf.Sin(k * p) * (tip - 1.5f)), 0.95f, size + ": el diente " + k + " es opaco");
                }
                // el diente 0 apunta a la derecha y el centro está lleno
                Assert.Greater(after.AlphaAt(tip - 2.5f, 0f), 0.95f);
                Assert.Greater(after.AlphaAt(0f, 0f), 0.95f);
            }
        }

        /// <summary>El valor del campo del sprite girado [radians] en el sentido del reloj (y hacia abajo en la pantalla del boceto) en el punto (x, yDown) del boceto.</summary>
        private static float ValueAtRotated(Img im, Func<Color32, float> value, float x, float yDown, float radians)
        {
            float cA = Mathf.Cos(radians), sA = Mathf.Sin(radians);
            return im.Field(value, x * cA + yDown * sA, x * sA - yDown * cA);
        }

        [Test]
        public void TheTwoNeighbourGears_AtRest_DoNotOverlap_AndTheVisibleGapIsNotLargerThanToday()
        {
            // el par grande-chico con las fases del contrato (MeshPhase): en el punto de contacto el diente de uno está donde el otro tiene hueco, con holgura cero
            var a = new GearDef { X = 0f, Y = 0f, N = EngranajesContract.BigTeeth, Pitch = EngranajesContract.BigPitch, Tip = EngranajesContract.BigTip, A = 0.37f };
            var b = new GearDef { X = 50f, Y = 0f, N = EngranajesContract.SmallTeeth, Pitch = EngranajesContract.SmallPitch, Tip = EngranajesContract.SmallTip };
            b.A = EngranajesContract.MeshPhase(a, b);
            PlasticArt.Disabled = true;
            var oldA = FromSprite(EngranajesSprites.Gear(EngranajesSprites.GearSize.Big, EngranajesSprites.Palette.Motor));
            var oldB = FromSprite(EngranajesSprites.Gear(EngranajesSprites.GearSize.Small, EngranajesSprites.Palette.Station));
            PlasticArt.Disabled = false;
            var newA = Load("gear_0_0");
            var newB = Load("gear_1_2");

            // 0,25 dp de rejilla en la zona de contacto: dp² con relleno de los DOS (se pisan) y dp² con relleno de NINGUNO (se ve el disco oscuro de atrás: el hueco que se ve); con la tinta de hoy ([value] = la silueta) o sin ella
            void Measure(Img ia, Img ib, Func<Color32, float> value, float angleB, out float overlap, out float visibleGap)
            {
                int both = 0, none = 0;
                for (float x = 14f; x <= 36f; x += 0.25f)
                    for (float y = -9f; y <= 9f; y += 0.25f)
                    {
                        bool fa = ValueAtRotated(ia, value, x, y, a.A) >= 0.5f, fb = ValueAtRotated(ib, value, x - 50f, y, angleB) >= 0.5f;
                        if (fa && fb) both++;
                        else if (!fa && !fb) none++;
                    }
                overlap = both * 0.0625f; visibleGap = none * 0.0625f;
            }
            Measure(oldA, oldB, FillValue, b.A, out float overlapBefore, out float gapBefore);               // lo que se ve hoy: los rellenos claros (la tinta no se distingue del fondo)
            Measure(oldA, oldB, SilValue, b.A, out float inkOverlapBefore, out _);                          // con la tinta, las siluetas de hoy se pisan en sus bordes
            Measure(newA, newB, SilValue, b.A, out float overlapAfter, out float gapAfter);
            Measure(newA, newB, SilValue, b.A + Mathf.PI / b.N, out float clash, out _);
            Assert.LessOrEqual(overlapAfter, 1.0f, "en reposo (holgura cero) las dos placas casi no se pisan: " + overlapAfter.ToString("0.00") + " dp² (con la tinta, las siluetas de hoy se pisan " + inkOverlapBefore.ToString("0.0") + " dp²; los rellenos claros de hoy, " + overlapBefore.ToString("0.00") + ")");
            Assert.LessOrEqual(gapAfter, gapBefore + 0.5f, "lo que se ve de hueco no es mayor que hoy: " + gapAfter.ToString("0.0") + " dp² contra " + gapBefore.ToString("0.0"));
            Assert.Greater(clash, 4f, "con los dientes enfrentados (mal encajados) sí se nota el choque: " + clash.ToString("0.0") + " dp²");
            Assert.LessOrEqual(overlapAfter * 3f, clash, "encajados casi no se pisan y mal encajados sí: " + overlapAfter.ToString("0.00") + " contra " + clash.ToString("0.00"));
        }

        // ------------------------------------------------------------------ lo que gira: sin luz ni sombra que giren; los rayos y el degradé muestran el sentido

        /// <summary>La luminosidad del sprite en el círculo de radio [r] dp: [count] muestras desde el ángulo 0 (hacia la derecha) en sentido antihorario.</summary>
        private static float[] LumaAround(Img im, float r, int count)
        {
            var v = new float[count];
            for (int i = 0; i < count; i++)
            {
                float a = 2f * Mathf.PI * i / count;
                v[i] = Luma(im.AtDp(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }
            return v;
        }

        /// <summary>Cuántas hondonadas hay en la luminosidad circular: tramos continuos que quedan más de [depth] por debajo del promedio de ±[window] muestras (los rayos del engranaje, más oscuros que la placa), y la más honda.</summary>
        private static int CountDips(float[] v, int window, float depth, out float deepest)
        {
            int n = v.Length;
            deepest = 0f;
            var below = new bool[n];
            int any = 0;
            for (int i = 0; i < n; i++)
            {
                float sum = 0f;
                for (int k = -window; k <= window; k++) sum += v[(i + k + n) % n];
                float high = v[i] - sum / (2 * window + 1);
                deepest = Mathf.Min(deepest, high);
                below[i] = high < -depth;
                if (below[i]) any++;
            }
            if (any == 0 || any == n) return 0;
            int start = 0;
            while (below[start]) start++;                                      // empezar fuera de una hondonada para no partirla
            int dips = 0;
            for (int k = 0; k < n; k++)
            {
                if (!below[(start + k) % n]) continue;
                dips++;
                while (k < n && below[(start + k) % n]) k++;
            }
            return dips;
        }

        [Test]
        public void TheGears_ShowTheirSpokes_5AndFor3_AndTheyAreNotSymmetric_SoTheSenseOfRotationIsSeen()
        {
            foreach (var size in new[] { 0, 1 })
            {
                var gear = size == 0 ? EngranajesSprites.GearSize.Big : EngranajesSprites.GearSize.Small;
                int spokes = size == 0 ? 5 : 3;
                float root = EngranajesSprites.TipOf(gear) - EngranajesContract.ToothDepth;
                foreach (int pal in new[] { 0, 1, 2 })
                {
                    var im = Load("gear_" + size + "_" + pal);
                    // los rayos a 0,5 de la raíz: se cuentan como «hondonadas» de luminosidad respecto del entorno cercano
                    var around = LumaAround(im, root * 0.5f, 720);
                    int dips = CountDips(around, 720 / spokes / 3, 0.035f, out float deepest);
                    Assert.AreEqual(spokes, dips, size + "/" + pal + ": se cuentan " + spokes + " rayos (hondonadas de luminosidad ≥ 0,035; la más honda " + deepest.ToString("0.000") + ")");
                    // el degradé y los rayos no son simétricos: el espejo izquierda-derecha y el giro de 180° se ven distintos (mide el sentido)
                    double mirror = 0, turn = 0; int count = 0;
                    for (float y = -root; y <= root; y += 0.5f)
                        for (float x = -root; x <= root; x += 0.5f)
                        {
                            if (x * x + y * y > root * root * 0.9f) continue;
                            float l = Luma(im.AtDp(x, y));
                            mirror += Mathf.Abs(l - Luma(im.AtDp(-x, y)));
                            turn += Mathf.Abs(l - Luma(im.AtDp(-x, -y)));
                            count++;
                        }
                    Assert.Greater(mirror / count, 0.02, size + "/" + pal + ": el engranaje no es su propio espejo (diferencia media " + (mirror / count).ToString("0.000") + ")");
                    Assert.Greater(turn / count, 0.02, size + "/" + pal + ": ni coincide al girarlo 180° (diferencia media " + (turn / count).ToString("0.000") + ")");
                }
            }
        }

        [Test]
        public void TheGear_ThatTurns_CarriesNoLightOrShadowThatTurnsWithIt_TheTurbineAndTheRadarEither()
        {
            // la turbina (5 aspas, sin degradé): ninguna aspa recibe más luz que otra
            var fan = Load("turbina");
            var blades = FanBladeAngles(fan, 12f, out var lumas);
            Assert.AreEqual(5, blades.Count);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (float l in lumas) { lo = Mathf.Min(lo, l); hi = Mathf.Max(hi, l); }
            Assert.Less(hi - lo, 0.05f, "las cinco aspas reciben la misma luz: ninguna luz gira con la turbina (diferencia " + (hi - lo).ToString("0.000") + ")");
            // el radar (la elipse, sin el punto coral): el lado izquierdo y el derecho, el de arriba y el de abajo, tienen la misma luminosidad media
            var radar = Load("radar");
            double l1 = 0, l2 = 0, u1 = 0, u2 = 0; int n1 = 0, n2 = 0, m1 = 0, m2 = 0;
            for (float y = -6f; y <= 6f; y += 0.4f)
                for (float x = -16f; x <= 12f; x += 0.4f)
                {
                    var c = radar.AtDp(x, y);
                    if (c.a < 250 || IsCoral(c) || (x * x) / (18f * 18f) + (y * y) / (5.5f * 5.5f) > 1f) continue;
                    if (x < -2f) { l1 += Luma(c); n1++; } else if (x > 2f && x < 10f) { l2 += Luma(c); n2++; }
                    if (y > 1f) { u1 += Luma(c); m1++; } else if (y < -1f) { u2 += Luma(c); m2++; }
                }
            Assert.Greater(n1 * n2 * m1 * m2, 0, "se midió el radar");
            Assert.AreEqual(l1 / n1, l2 / n2, 0.04, "el radar: izquierda y derecha reciben la misma luz");
            Assert.AreEqual(u1 / m1, u2 / m2, 0.04, "y arriba y abajo también");
            // el engranaje presionado del motor es el mismo contorno y no trae ni una huella (un dedo fuera del centro giraría con el engranaje)
            var pressed = Load("gear_0_0_p"); var plain = Load("gear_0_0");
            int diff = 0, total = 0;
            for (int i = 0; i < plain.Px.Length; i++) { bool a = plain.Px[i].a > 128, b = pressed.Px[i].a > 128; if (a || b) total++; if (a != b) diff++; }
            Assert.Less(diff, total * 0.01, "el contorno de los dientes no cambia al presionar (" + diff + " de " + total + " píxeles)");
        }

        private static bool IsCoral(Color32 c) => c.r > 200 && c.g > 90 && c.g < 190 && c.b < 150 && c.r > c.g + 40;

        /// <summary>El radar tiene el punto coral en su punta derecha y nada coral a la izquierda: sigue siendo ASIMÉTRICO (un radar que gira se lee).</summary>
        [Test]
        public void TheRadar_StaysAsymmetric_TheCoralDotIsAtItsRightTip()
        {
            var radar = Load("radar");
            double sx = 0, sy = 0; int n = 0, left = 0;
            for (float y = -12f; y <= 12f; y += 0.25f)
                for (float x = -24f; x <= 24f; x += 0.25f)
                {
                    var c = radar.AtDp(x, y);
                    if (c.a < 200 || !IsCoral(c)) continue;
                    if (x < 0f) left++; else { sx += x; sy += y; n++; }
                }
            Assert.Greater(n, 100, "el punto coral se ve");
            Assert.AreEqual(0, left, "y no hay nada coral a la izquierda");
            Assert.AreEqual(18.0, sx / n, 1.5, "el punto está en la punta derecha del radar (x = 18 dp)");
            Assert.AreEqual(0.0, sy / n, 1.0);
        }

        // ------------------------------------------------------------------ nada de «+»: la turbina de 5 aspas, el cruce de la correa y el cartel que falla

        private static bool IsGold(Color32 c) => c.a > 200 && c.r > 215 && c.g > 150 && c.g < 235 && c.b < 150;

        /// <summary>Los ángulos (grados, 0 = derecha, antihorario) del centro de cada aspa dorada que cruza el círculo de radio [r] dp, y la luminosidad media de cada una.</summary>
        private static List<float> FanBladeAngles(Img im, float r, out List<float> lumas)
        {
            lumas = new List<float>();
            var angles = new List<float>();
            const int steps = 1440;
            var on = new bool[steps];
            for (int i = 0; i < steps; i++) { float a = 2f * Mathf.PI * i / steps; on[i] = IsGold(im.AtDp(Mathf.Cos(a) * r, Mathf.Sin(a) * r)); }
            // el borde de tinta y el suavizado parten una aspa en dos o dejan una astilla de un par de grados: se rellenan los huecos de menos de 3° entre dorados y se ignoran los tramos de menos de 8°
            for (int i = 0; i < steps; i++)
            {
                if (on[i]) continue;
                int run = 0;
                while (run < 12 && !on[(i + run) % steps]) run++;
                if (run < 12 && on[(i + run) % steps] && on[(i - 1 + steps) % steps]) for (int k = 0; k < run; k++) on[(i + k) % steps] = true;
            }
            for (int i = 0; i < steps; i++)
            {
                if (!on[i] || on[(i - 1 + steps) % steps]) continue;
                int run = 0;
                while (run < steps && on[(i + run) % steps]) run++;
                if (run < 32 && run < steps) for (int k = 0; k < run; k++) on[(i + k) % steps] = false;
            }
            int start = 0;
            while (start < steps && on[start]) start++;                                          // empezar en un hueco para no partir una aspa
            for (int k = 0; k < steps; k++)
            {
                int i = (start + k) % steps;
                if (!on[i]) continue;
                int j = k; double lumaSum = 0; int len = 0;
                while (j < steps && on[(start + j) % steps]) { float a = 2f * Mathf.PI * ((start + j) % steps) / steps; lumaSum += Luma(im.AtDp(Mathf.Cos(a) * r, Mathf.Sin(a) * r)); len++; j++; }
                float mid = (start + k + (j - k - 1) / 2f) % steps;
                angles.Add(mid * 360f / steps);
                lumas.Add((float)(lumaSum / len));
                k = j;
            }
            return angles;
        }

        [Test]
        public void TheTurbine_HasFiveBlades_NotACross_InThePlasticAndInTheProceduralDrawing()
        {
            Assert.AreEqual(5, EngranajesSprites.FanBlades, "el número de aspas no es una pista del juego (lo que se lee es el sentido del giro): 5, nunca 4 (una cruz «+»)");
            foreach (bool plastic in new[] { true, false })
            {
                PlasticArt.Disabled = !plastic;
                var im = plastic ? Load("turbina") : FromSprite(EngranajesSprites.Fan());
                PlasticArt.Disabled = false;
                var blades = FanBladeAngles(im, 12f, out _);
                string who = plastic ? "plasticina" : "procedural";
                Assert.AreEqual(5, blades.Count, who + ": se cuentan 5 aspas a 12 dp del centro");
                blades.Sort();
                for (int i = 0; i < blades.Count; i++)
                {
                    float step = (blades[(i + 1) % blades.Count] - blades[i] + 360f) % 360f;
                    Assert.AreEqual(72f, step, 14f, who + ": las aspas están repartidas cada 72°");
                    for (int j = i + 1; j < blades.Count; j++)
                    {
                        float d = Mathf.Abs(blades[i] - blades[j]); d = Mathf.Min(d, 360f - d);
                        Assert.Less(d, 160f, who + ": ningún par de aspas queda enfrentado (180°): no hay un «+»");
                    }
                }
            }
        }

        [Test]
        public void TheCrossedBelt_CrossesAtAbout31Degrees_NeverAStraightPlus()
        {
            // cada correa une dos engranajes de la cuadrícula con uno en medio: 2 pasos (100 u) de largo; las tiras salen del borde de las poleas (r = 14) y al cruzarse forman un ángulo de 2·atan(2r/d)
            var rng = new System.Random(5);
            float worst = 0f; int belts = 0;
            for (int level = 1; level <= EngranajesContract.MaxLevel; level++)
                for (int k = 0; k < 40; k++)
                {
                    var m = EngranajesContract.Generate(level, rng);
                    foreach (var l in m.Links)
                    {
                        if (l.Type != LinkType.Belt) continue;
                        var ga = m.Gears[l.A]; var gb = m.Gears[l.B];
                        float d = Vector2.Distance(new Vector2(ga.X, ga.Y), new Vector2(gb.X, gb.Y));
                        Assert.GreaterOrEqual(d, 2f * EngranajesContract.GridStep - 0.01f, "una correa une engranajes a dos pasos o más");
                        float angle = 2f * Mathf.Atan(2f * EngranajesContract.PulleyRadius / d) * Mathf.Rad2Deg;
                        worst = Mathf.Max(worst, angle);
                        belts++;
                    }
                }
            Assert.Greater(belts, 50, "se revisaron correas de todos los niveles");
            Assert.LessOrEqual(worst, 35f, "al cruzarse las dos tiras forman unos 31° como mucho: un ocho tendido, nunca una cruz «+» recta (90°); el peor fue " + worst.ToString("0.0") + "°");
            Assert.GreaterOrEqual(worst, 25f, "y se nota que se cruzan");
        }

        [Test]
        public void TheCartelThatFails_CarriesTheCrossMark_AndTheOneThatMetCarriesTheCheck()
        {
            var go = new GameObject("EngranajesCartelMarks");
            try
            {
                var controller = go.AddComponent<EngranajesGameController>();
                var build = typeof(EngranajesGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                var marks = controller.VerdictMarks();
                Assert.IsTrue(marks[0] && !marks[1], "el cartel que cumplió lleva su ✓ y no el aspa");
                Assert.IsTrue(!marks[2] && marks[3], "el cartel que falló lleva su aspa y no el ✓: acierto y error no van solo por color");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            // la aspa es la moneda coral de lo común (diagonal, nunca un «+») y el ✓ la verde; sin lo común, la blanca de siempre que se tiñe
            if (EngranajesSprites.BadgesAreCoins)
            {
                Assert.AreSame(PlasticArt.Get("comun", "marca_no"), EngranajesSprites.CrossBadge());
                Assert.AreSame(PlasticArt.Get("comun", "marca_ok"), EngranajesSprites.CheckBadge());
            }
            PlasticArt.Disabled = true;
            Assert.IsFalse(EngranajesSprites.BadgesAreCoins);
            var cross = FromSprite(EngranajesSprites.CrossBadge());
            PlasticArt.Disabled = false;
            // el aspa procedural es diagonal: hay tinta en las esquinas y NO en los puntos medios de los bordes (una cruz «+» sería al revés)
            Assert.Greater(cross.AlphaAt(3f, 3f) + cross.AlphaAt(-3f, 3f) + cross.AlphaAt(3f, -3f) + cross.AlphaAt(-3f, -3f), 3.5f, "el aspa pasa por las cuatro diagonales");
            Assert.Less(cross.AlphaAt(0f, 5.5f) + cross.AlphaAt(0f, -5.5f) + cross.AlphaAt(5.5f, 0f) + cross.AlphaAt(-5.5f, 0f), 0.2f, "y no por los ejes (no es un «+»)");
        }

        // ------------------------------------------------------------------ el sentido del giro: la cabeza de la flecha, también en escala de grises y a tamaño real

        [Test]
        public void TheTurnArrows_AreMirrors_AndTheirHeadTellsTheSense_EvenInGrayscaleAtRealSize()
        {
            foreach (var name in new[] { "flecha_grande", "flecha_chico", "miniflecha" })
            {
                var cw = Load(name + "_cw"); var ccw = Load(name + "_ccw");
                Assert.AreEqual(cw.W, ccw.W); Assert.AreEqual(cw.H, ccw.H);
                // en escala de grises y con el alfa (lo que se ve sobre el fondo oscuro): luminosidad × alfa. La luz del render no se espeja con la geometría, así que el espejo se exige a la SILUETA; el sentido, al gris
                float Gray(Img im, int x, int y) { var c = im.At(x, y); return Luma(c) * c.a / 255f; }
                int silhouette = 0, mirroredSilhouette = 0, plainSilhouette = 0, strongSense = 0;
                for (int y = 0; y < cw.H; y++)
                    for (int x = 0; x < cw.W; x++)
                    {
                        bool a = cw.At(x, y).a > 128, flipped = ccw.At(cw.W - 1 - x, y).a > 128, plain = ccw.At(x, y).a > 128;
                        if (a) silhouette++;
                        if (a != flipped) mirroredSilhouette++;
                        if (a != plain) plainSilhouette++;
                        if (Mathf.Abs(Gray(cw, x, y) - Gray(ccw, x, y)) > 0.3f) strongSense++;
                    }
                Assert.Less(mirroredSilhouette, silhouette * 0.03f, name + ": la silueta de la flecha contra el reloj es el espejo exacto de la otra (" + mirroredSilhouette + " de " + silhouette + " píxeles)");
                Assert.Greater(plainSilhouette, silhouette * 0.25f, name + ": sin espejar, las dos siluetas se distinguen (" + plainSilhouette + " de " + silhouette + " píxeles)");
                // a tamaño real (los dp): píxeles de la cabeza que cambian de lado, en dp²
                float dp2 = strongSense / (cw.Ppu * cw.Ppu);
                Assert.Greater(dp2, name == "miniflecha" ? 3f : 12f, name + ": la cabeza cambia de lado en " + dp2.ToString("0.0") + " dp² en escala de grises");
            }
        }

        // ------------------------------------------------------------------ lo que se toca se ve presionado; el trazado de siempre se conserva

        [Test]
        public void ThePressedStates_AreDarkerAndFlatter_WithTheSameSilhouette()
        {
            foreach (var pair in new[] { new[] { "gear_0_0", "gear_0_0_p" }, new[] { "gear_1_0", "gear_1_0_p" }, new[] { "polea", "polea_p" } })
            {
                var plain = Load(pair[0]); var down = Load(pair[1]);
                Assert.AreEqual(plain.W, down.W, pair[1] + ": el mismo cuadro");
                double a = 0, b = 0; int n = 0, silhouetteDiff = 0, total = 0;
                for (int i = 0; i < plain.Px.Length; i++)
                {
                    bool pa = plain.Px[i].a > 128, pb = down.Px[i].a > 128;
                    if (pa || pb) total++;
                    if (pa != pb) silhouetteDiff++;
                    if (plain.Px[i].a > 250 && down.Px[i].a > 250) { a += Luma(plain.Px[i]); b += Luma(down.Px[i]); n++; }
                }
                Assert.Less(silhouetteDiff, total * 0.01, pair[1] + ": la silueta no cambia al presionar");
                Assert.Less(b / n, a / n * 0.975, pair[1] + ": está más oscuro (" + (a / n).ToString("0.000") + " → " + (b / n).ToString("0.000") + ")");
            }
            Assert.IsNotNull(EngranajesSprites.GearPressed(EngranajesSprites.GearSize.Big));
            Assert.IsNotNull(EngranajesSprites.PulleyPressed());
            PlasticArt.Disabled = true;
            Assert.IsNull(EngranajesSprites.GearPressed(EngranajesSprites.GearSize.Big), "sin plasticina no hay imagen presionada: se queda la respuesta de siempre");
            Assert.IsNull(EngranajesSprites.PulleyPressed());
        }

        /// <summary>No mide nada: deja el dibujo PROCEDURAL (el de antes de la plasticina) en <c>unity/test-results/engranajes-antes/*.png</c> para que <c>tools/juegos/mide_engranajes.py</c> y <c>hoja_engranajes.py</c> comparen el antes y el después con píxeles reales.</summary>
        [Test]
        public void TheProceduralDrawing_IsSavedForTheMeasureScript()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "test-results", "engranajes-antes"));
            Directory.CreateDirectory(dir);
            PlasticArt.Disabled = true;
            var all = new Dictionary<string, Sprite>
            {
                { "radar", EngranajesSprites.Dish() }, { "turbina", EngranajesSprites.Fan() }, { "polea", EngranajesSprites.Pulley() }, { "porton", EngranajesSprites.Door() }, { "marco_carga", EngranajesSprites.CargoFrame() },
                { "plataforma", EngranajesSprites.CargoPlate() }, { "caja_carga", EngranajesSprites.CargoBox() }, { "barra", EngranajesSprites.Rack() }, { "casco", EngranajesSprites.Hull() },
                { "flecha_grande_cw", EngranajesSprites.Arrow(EngranajesContract.BigTip + 8f, 4f, true) }, { "flecha_grande_ccw", EngranajesSprites.Arrow(EngranajesContract.BigTip + 8f, 4f, false) },
                { "flecha_chico_cw", EngranajesSprites.Arrow(EngranajesContract.SmallTip + 8f, 4f, true) }, { "flecha_chico_ccw", EngranajesSprites.Arrow(EngranajesContract.SmallTip + 8f, 4f, false) },
                { "miniflecha_cw", EngranajesSprites.MiniArrow(true) }, { "miniflecha_ccw", EngranajesSprites.MiniArrow(false) },
                { "tri_arriba", EngranajesSprites.GlyphSprite(EngranajesSprites.Glyph.Up) }, { "tri_abajo", EngranajesSprites.GlyphSprite(EngranajesSprites.Glyph.Down) }, { "tri_play", EngranajesSprites.GlyphSprite(EngranajesSprites.Glyph.Play) },
                { "check", EngranajesSprites.Check() }, { "aspa", EngranajesSprites.Cross() }
            };
            foreach (var size in new[] { EngranajesSprites.GearSize.Big, EngranajesSprites.GearSize.Small })
                foreach (EngranajesSprites.Palette pal in Enum.GetValues(typeof(EngranajesSprites.Palette)))
                    all["gear_" + (int)size + "_" + (int)pal] = EngranajesSprites.Gear(size, pal);
            PlasticArt.Disabled = false;
            foreach (var kv in all)
            {
                var tex = kv.Value.texture;
                File.WriteAllBytes(Path.Combine(dir, kv.Key + ".png"), tex.EncodeToPNG());
                File.WriteAllText(Path.Combine(dir, kv.Key + ".ppu"), kv.Value.pixelsPerUnit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            Assert.IsTrue(File.Exists(Path.Combine(dir, "gear_0_0.png")));
        }

        [Test]
        public void EveryPieceTheGameDraws_IsOnAndOpaque_WithPlasticina_AndWithoutIt()
        {
            foreach (bool plastic in new[] { true, false })
            {
                PlasticArt.Disabled = !plastic;
                var go = new GameObject("EngranajesAuditPlastic" + plastic);
                try
                {
                    var controller = go.AddComponent<EngranajesGameController>();
                    var build = typeof(EngranajesGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    build.Invoke(controller, null);
                    go.SetActive(true);
                    var problems = controller.AuditVisibility();
                    Assert.IsEmpty(problems, (plastic ? "con plasticina" : "sin plasticina") + ": piezas que no se verían: " + string.Join(" | ", problems));
                }
                finally { UnityEngine.Object.DestroyImmediate(go); PlasticArt.Disabled = false; }
            }
        }
    }
}
