using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Aterrizaje.Tests
{
    /// <summary>
    /// Aterrizaje Lunar en plasticina (Tarea 111): una de las tres de «la mecánica es la silueta» (docs/plasticina-juegos.md §13). Lo que se mide, y por qué:
    /// <list type="bullet">
    /// <item>LA REGLA MANDA: las marcas de extremo y del medio, los números y el asta de la bandera se quedan procedurales, en tinta y en el mismo x de siempre (±1 unidad = 1/3 dp), con o sin atlas (<see cref="TheRulerMarksAndThePole_StayExactlyWhereTheyWere_WithAndWithoutPlasticina"/>).
    /// La regla de plasticina es una losa crema lisa SIN marcas, rayas, grano ni cráteres (nada decorativo junto a la regla).</item>
    /// <item>LA NAVE ES SIMÉTRICA respecto de su eje vertical: su centro en x ES la respuesta. El centro de masa de su silueta coincide con el de hoy (±2 unidades) y el sprite es su propio espejo.</item>
    /// <item>La luna y las cordilleras (pegadas a la regla) siguen procedurales; el satélite no lleva «+»; la aspa de la insignia es diagonal; cada pieza mide lo de siempre.</item>
    /// </list>
    /// Estas pruebas miden sobre el ATLAS (se lee el PNG del disco: la textura importada no es legible). Las medidas con números las saca <c>tools/juegos/mide_aterrizaje.py</c>.
    /// </summary>
    public sealed class AterrizajePlasticTests
    {
        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        // ------------------------------------------------------------------ el atlas como píxeles

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
            /// <summary>El color a (xDp, yDp) del centro (y hacia ARRIBA).</summary>
            public Color32 AtDp(float xDp, float yDp) => At(Mathf.RoundToInt(W / 2f + xDp * Ppu - 0.5f), Mathf.RoundToInt(H / 2f + yDp * Ppu - 0.5f));
            public float AlphaAt(float xDp, float yDp) => AtDp(xDp, yDp).a / 255f;
        }

        private static Img Load(string sprite, string group = null)
        {
            group = group ?? LandingSprites.PlasticGroup;
            var e = PlasticArt.Group(group).Entry(sprite);
            Assert.IsNotNull(e, "falta «" + sprite + "» en el atlas «" + group + "»");
            var colors = Atlas(group).GetPixels(e.x, e.y, e.w, e.h);
            var px = new Color32[colors.Length];
            for (int i = 0; i < colors.Length; i++) px[i] = colors[i];
            return new Img { W = e.w, H = e.h, Ppu = e.ppu, Px = px };
        }

        private static float Luma(Color32 c) => (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;

        /// <summary>El centro de masa en x (píxeles, con el centro del píxel) de la silueta: lo que tiene alfa ≥ [alpha].</summary>
        private static float CentroidX(Color32[] px, int w, int h, byte alpha)
        {
            double sx = 0, n = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a >= alpha) { sx += x + 0.5; n++; }
            return n == 0 ? w / 2f : (float)(sx / n);
        }

        // ------------------------------------------------------------------ cada pieza mide lo de siempre

        [Test]
        public void EverySprite_IsInTheAtlas_AtTheFrameOfTheProceduralOne()
        {
            Assert.IsTrue(LandingSprites.Plastic, "falta el atlas de plasticina de Aterrizaje (correr tools/juegos/empaqueta.py aterrizaje)");
            // el mismo cuadro que el dibujo procedural al que reemplaza (en dp y con la misma proporción): así el controlador no cambia ninguna medida
            PlasticArt.Disabled = true;
            var proc = new Dictionary<string, Sprite> { { "nave", LandingSprites.Lander() }, { "gallardete", LandingSprites.Pennant() }, { "tierra", LandingSprites.Earth() }, { "satelite", LandingSprites.Satellite() }, { "domo_on", LandingSprites.Dome(true) }, { "domo_off", LandingSprites.Dome(false) } };
            PlasticArt.Disabled = false;
            var plastic = new Dictionary<string, Sprite> { { "nave", LandingSprites.Lander() }, { "gallardete", LandingSprites.Pennant() }, { "tierra", LandingSprites.Earth() }, { "satelite", LandingSprites.Satellite() }, { "domo_on", LandingSprites.Dome(true) }, { "domo_off", LandingSprites.Dome(false) } };
            var frameDp = new Dictionary<string, Vector2>
            {
                { "nave", new Vector2(2f * LandingSprites.LanderUnit, 2f * LandingSprites.LanderUnit) }, { "gallardete", new Vector2(LandingSprites.PennantW, LandingSprites.PennantH) }, { "tierra", new Vector2(LandingSprites.EarthSide, LandingSprites.EarthSide) },
                { "satelite", new Vector2(LandingSprites.SatelliteW, LandingSprites.SatelliteH) }, { "domo_on", new Vector2(LandingSprites.DomeW, LandingSprites.DomeH) }, { "domo_off", new Vector2(LandingSprites.DomeW, LandingSprites.DomeH) }
            };
            foreach (var kv in proc)
            {
                var p = plastic[kv.Key];
                Assert.IsNotNull(p, kv.Key);
                Assert.AreNotSame(kv.Value, p, kv.Key + ": sale del atlas");
                Assert.AreEqual(kv.Value.rect.width / kv.Value.rect.height, p.rect.width / p.rect.height, 0.01f, kv.Key + ": la misma proporción que el procedural");
                Assert.AreEqual(frameDp[kv.Key].x, p.rect.width / p.pixelsPerUnit, 0.6f, kv.Key + ": el mismo ancho en dp (el cuadro del dibujo de siempre)");
                Assert.AreEqual(frameDp[kv.Key].y, p.rect.height / p.pixelsPerUnit, 0.6f, kv.Key + ": el mismo alto en dp");
            }
            Assert.AreEqual(6 + 1, PlasticArt.Group(LandingSprites.PlasticGroup).Manifest.sprites.Length, "la nave, el gallardete, la Tierra, el satélite, 2 cúpulas y la regla");
            Assert.IsNotNull(LandingSprites.Ruler(), "la regla de plasticina");
            // lo que sigue procedural: la luna y las cordilleras (pegadas a la regla), la nebulosa, la estrella fugaz y la flecha de la guía
            PlasticArt.Disabled = true; var moon = LandingSprites.Moon(); var farRange = LandingSprites.Range(true); PlasticArt.Disabled = false;
            Assert.AreSame(moon, LandingSprites.Moon(), "la luna se queda procedural");
            Assert.AreSame(farRange, LandingSprites.Range(true), "las cordilleras se quedan procedurales");
            foreach (var s in new[] { LandingSprites.Nebula(), LandingSprites.ShootingStar(), LandingSprites.Arrow() }) Assert.IsNotNull(s);
        }

        // ------------------------------------------------------------------ la nave: simétrica; su centro en x ES la respuesta

        [Test]
        public void TheLander_IsItsOwnMirror_AndItsCenterOfMassMatchesTheProceduralOneWithinTwoUnits()
        {
            var lander = Load("nave");
            // 1) el sprite es su propio espejo horizontal (alfa y luminosidad): la luz del render no carga la nave a un lado
            int worstAlpha = 0; float worstLuma = 0f;
            for (int y = 0; y < lander.H; y++)
                for (int x = 0; x < lander.W / 2; x++)
                {
                    var a = lander.At(x, y); var b = lander.At(lander.W - 1 - x, y);
                    worstAlpha = Math.Max(worstAlpha, Mathf.Abs(a.a - b.a));
                    if (a.a > 200 && b.a > 200) worstLuma = Mathf.Max(worstLuma, Mathf.Abs(Luma(a) - Luma(b)));
                }
            Assert.LessOrEqual(worstAlpha, 2, "la silueta es idéntica a la izquierda y a la derecha");
            Assert.LessOrEqual(worstLuma, 0.02f, "y el relieve y la luz también (la nave se simetriza al empaquetarla)");
            // 2) el centro de masa de la silueta, en unidades del lienzo de 1080 (la nave se dibuja a 90 dp = 270 unidades): ±2 unidades del centro del cuadro, igual que la de hoy
            float unitsPerPx = LandingPlan.LanderSide * 3f / lander.W;
            float plastic = (CentroidX(lander.Px, lander.W, lander.H, 128) - lander.W / 2f) * unitsPerPx;
            const int size = 256;
            var proc = LandingSprites.RenderLander(size);
            float procedural = (CentroidX(proc, size, size, 128) - size / 2f) * (LandingPlan.LanderSide * 3f / size);
            Assert.AreEqual(0f, plastic, 2f, "el centro de masa de la nave de plasticina está en el centro del cuadro (±2 u): su centro en x es la respuesta");
            Assert.AreEqual(procedural, plastic, 2f, "y coincide con el de la nave de hoy (" + procedural + " u) a ±2 u");
            Assert.AreEqual(0f, procedural, 2f, "(la de hoy también está en el centro)");
        }

        [Test]
        public void TheLander_HasACabinAWindowAGoldBaseAndLegs_AndItFillsItsFrameLikeTheOldOne()
        {
            var im = Load("nave"); var proc = LandingSprites.RenderLander(256);
            // el mismo ancho y alto de silueta que la nave de hoy (en dp), ±3 dp: cabe en el cuadro igual que la de siempre (sus patas llegan a ±30 dp)
            int minX = im.W, maxX = -1, minY = im.H, maxY = -1;
            for (int y = 0; y < im.H; y++) for (int x = 0; x < im.W; x++) if (im.Px[y * im.W + x].a >= 128) { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
            int pMinX = 256, pMaxX = -1, pMinY = 256, pMaxY = -1;
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++) if (proc[y * 256 + x].a >= 128) { pMinX = Math.Min(pMinX, x); pMaxX = Math.Max(pMaxX, x); pMinY = Math.Min(pMinY, y); pMaxY = Math.Max(pMaxY, y); }
            float dpW = (maxX - minX + 1) / im.Ppu, dpH = (maxY - minY + 1) / im.Ppu;
            float pDpW = (pMaxX - pMinX + 1) / (256f / 72f), pDpH = (pMaxY - pMinY + 1) / (256f / 72f);
            Assert.AreEqual(pDpW, dpW, 5f, "el ancho de la silueta (" + dpW + " dp) como el de hoy (" + pDpW + ")");
            Assert.AreEqual(pDpH, dpH, 6f, "y el alto (" + dpH + " dp) como el de hoy (" + pDpH + ")");
            // la ventanilla celeste en el centro de la cabina, la base sol abajo
            var window = im.AtDp(0f, 9f); var baseColor = im.AtDp(0f, -6f);
            Assert.Greater(window.b, window.r + 40, "la ventanilla es celeste (a +9 dp del centro)");
            Assert.Greater(baseColor.r, baseColor.b + 80, "la base es dorada (a −6 dp del centro)");
            Assert.Greater(im.AlphaAt(24f, -24f) + im.AlphaAt(-24f, -24f), 1.2f, "las patas abiertas llegan a ±24 dp de ancho, abajo");
        }

        // ------------------------------------------------------------------ la regla de plasticina: una losa crema LISA, sin nada decorativo

        [Test]
        public void TheRuler_IsASmoothCreamSlab_WithoutMarksStripesGrainOrCraters_AndWithNineSliceBordersAndMargins()
        {
            var g = PlasticArt.Group(LandingSprites.PlasticGroup);
            var e = g.Entry("regla");
            Assert.IsNotNull(e, "la regla está en el atlas");
            Assert.IsTrue(e.borde != null && e.borde.Length == 4, "con su nine-slice (se estira a lo ancho)");
            Assert.IsTrue(e.margen != null && e.margen.Length == 4, "y los márgenes de la sombra de las losas del kit");
            Assert.Less(e.borde[0] + e.borde[2], e.w, "los bordes izquierdo y derecho dejan un centro que se estira");
            var im = Load("regla");
            // el cuerpo de la losa: de margen[0] a W − margen[2] en x y de margen[1] (desde arriba) a H − margen[3] en y; el cuerpo mide 20 dp de alto (la regla de siempre) y 39,7 de ancho
            float bodyW = (im.W - e.margen[0] - e.margen[2]) / 3f, bodyH = (im.H - e.margen[1] - e.margen[3]) / 3f;
            Assert.AreEqual(20f, bodyH, 1.5f, "la losa mide 20 dp de alto como la regla de siempre");
            Assert.Greater(bodyW, 30f, "y tiene un centro de ancho para estirarse");
            // la cara (el centro del cuerpo): crema, sin una sola marca, raya o grano: poca variación de luminosidad y nada oscuro
            int x0 = e.margen[0] + Mathf.RoundToInt(0.30f * (im.W - e.margen[0] - e.margen[2])), x1 = e.margen[0] + Mathf.RoundToInt(0.70f * (im.W - e.margen[0] - e.margen[2]));
            int topY = im.H - 1 - e.margen[1], hBody = im.H - e.margen[1] - e.margen[3];
            int y0 = topY - Mathf.RoundToInt(0.70f * hBody), y1 = topY - Mathf.RoundToInt(0.30f * hBody);
            double sum = 0, sum2 = 0; int n = 0; float min = 1f, max = 0f; double r = 0, gr = 0, b = 0;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var c = im.At(x, y); float l = Luma(c);
                    sum += l; sum2 += l * l; n++; min = Mathf.Min(min, l); max = Mathf.Max(max, l); r += c.r; gr += c.g; b += c.b;
                    Assert.GreaterOrEqual(c.a, 250, "la cara es opaca");
                }
            double mean = sum / n, std = Math.Sqrt(Math.Max(0.0, sum2 / n - mean * mean));
            Assert.Greater(min, 0.6f, "ningún píxel oscuro en la cara de la regla (ni una marca de tinta, ni un cráter) (mínimo " + min + ")");
            Assert.Less(max - min, 0.12f, "la cara es lisa: variación de luminosidad " + (max - min));
            Assert.Less(std, 0.03, "sin grano ni rayas: desviación " + std);
            Assert.Greater(r / n, b / n + 20.0, "es crema (cálida: más rojo que azul)");
            Assert.AreEqual(LandingSprites.RulerColor.r * 255f, (float)(r / n), 12f, "con el color de la regla de siempre (F3EBD3)");
        }

        // ------------------------------------------------------------------ la regla manda: las marcas y el asta, exactamente donde estaban

        private static float[] MarkCenters(bool plastic, float flagFrac)
        {
            PlasticArt.Disabled = !plastic;
            var go = new GameObject("LandingRulerAudit" + (plastic ? "On" : "Off"));
            try
            {
                var controller = go.AddComponent<LandingGameController>();
                var build = typeof(LandingGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                return controller.RulerMarkCentersDp(flagFrac);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); PlasticArt.Disabled = false; }
        }

        [Test]
        public void TheRulerMarksAndThePole_StayExactlyWhereTheyWere_WithAndWithoutPlasticina()
        {
            // el centro visual de cada marca (extremo izquierdo, medio, extremo derecho) y del asta: en x0, el medio, x1 y el lugar justo, a ±1 unidad (1/3 dp), con la regla de plasticina Y con la de siempre
            const float tolerance = 1f / 3f;
            foreach (float frac in new[] { 0f, 0.37f, 0.5f, 1f })
            {
                var expected = new[] { LandingPlan.RulerX0, LandingPlan.Width * 0.5f, LandingPlan.RulerX1, LandingPlan.FracX(frac) };
                var names = new[] { "la marca del extremo izquierdo", "la marca del medio", "la marca del extremo derecho", "el asta de la bandera (en " + frac + ")" };
                var on = MarkCenters(true, frac); var off = MarkCenters(false, frac);
                for (int i = 0; i < 4; i++)
                {
                    Assert.AreEqual(expected[i], off[i], tolerance, names[i] + ", la regla de siempre");
                    Assert.AreEqual(expected[i], on[i], tolerance, names[i] + ", la regla de plasticina");
                    Assert.AreEqual(off[i], on[i], tolerance, names[i] + ": con plasticina queda donde estaba");
                }
            }
        }

        private static string Read(string file) => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Aterrizaje", file));

        [Test]
        public void TheTicksPoleAndLabels_AreStillProceduralInk_AndNothingDecorativeIsNextToTheRuler()
        {
            string build = Read("LandingGameController.Build.cs");
            // las marcas son rectángulos de tinta (procedurales) y los números, texto con contorno de tinta; el asta, un rectángulo de tinta
            StringAssert.Contains("_tickL = Rr(_rulerLayer, \"EndTickL\", Ink, 1.7f)", build);
            StringAssert.Contains("_tickR = Rr(_rulerLayer, \"EndTickR\", Ink, 1.7f)", build);
            StringAssert.Contains("_tickMid = Rr(_rulerLayer, \"MidTick\", Ink, 1.3f)", build);
            StringAssert.Contains("_pole = Rr(_resultLayer, \"Pole\", Ink, 1.5f)", build);
            StringAssert.Contains("PlasticSlab.TryStyleCustom(_rulerFill, LandingSprites.PlasticGroup, \"regla\")", build, "solo el CUERPO de la regla es de plasticina");
            StringAssert.DoesNotContain("_tickL.enabled", build);
            StringAssert.DoesNotContain("_tickMid.enabled", build);
            string layout = Read("LandingGameController.Build.cs");
            StringAssert.Contains("SetRect(_tickL.rectTransform, LandingPlan.RulerX0, y, 3.4f, h + 12f)", layout, "la marca izquierda, en x0 y con su ancho de siempre");
            StringAssert.Contains("SetRect(_tickR.rectTransform, LandingPlan.RulerX1, y, 3.4f, h + 12f)", layout, "la derecha, en x1");
            StringAssert.Contains("SetRect(_tickMid.rectTransform, cx, y, 2.6f, h - 6f)", layout, "la del medio, en el medio");
            string scene = Read("LandingGameController.Scene.cs");
            StringAssert.Contains("SetRect(_pole.rectTransform, tx, _plan.RulerY - fh * 0.5f, 3f, fh)", scene, "el asta, en el lugar justo y de 3 dp de ancho");
            // la luna y las cordilleras (pegadas a la regla) y la nebulosa NO se pasan al atlas
            string sprites = Read("LandingSprites.cs");
            foreach (var proceduralPiece in new[] { "public static Sprite Moon() => Get(", "public static Sprite Range(bool far) => Get(", "public static Sprite Nebula() => Get(", "public static Sprite ShootingStar() => Get(", "public static Sprite Arrow() => Get(" })
                StringAssert.Contains(proceduralPiece, sprites, "se queda procedural: " + proceduralPiece);
        }

        // ------------------------------------------------------------------ el mundo y el marcador

        [Test]
        public void TheSatellite_HasTwoPanelsAndABody_NothingVerticalOnTheBody_NoPlusSign()
        {
            var sat = Load("satelite");
            // el cuadro de 32 × 12 dp: paneles a ±9,5 dp, cuerpo de 8 × 8 dp en el centro; nada vertical encima ni debajo del cuerpo (sería una cruz)
            Assert.Greater(sat.AlphaAt(-9.5f, 0f), 0.9f, "panel izquierdo"); Assert.Greater(sat.AlphaAt(9.5f, 0f), 0.9f, "panel derecho"); Assert.Greater(sat.AlphaAt(0f, 0f), 0.9f, "el cuerpo");
            Assert.Less(sat.AlphaAt(0f, 5.5f), 0.1f, "nada encima del cuerpo (el cuerpo mide 8 dp: llega a ±4)");
            Assert.Less(sat.AlphaAt(0f, -5.5f), 0.1f, "ni debajo");
            Assert.Less(sat.AlphaAt(-9.5f, 5.5f) + sat.AlphaAt(9.5f, 5.5f) + sat.AlphaAt(-9.5f, -5.5f) + sat.AlphaAt(9.5f, -5.5f), 0.4f, "ni sobre los paneles");
            Assert.Greater(sat.AtDp(-9.5f, 0f).b, sat.AtDp(-9.5f, 0f).r + 30, "los paneles son azules");
        }

        [Test]
        public void ThePennant_IsAGoldTriangleWhoseLeftEdgeSitsOnThePole_AndTheEarthHasOceanAndLand()
        {
            var pen = Load("gallardete");
            Assert.Greater(pen.AlphaAt(-14f, 0f), 0.9f, "el borde de la izquierda (donde está el asta) está lleno: su vértice de arriba a la izquierda a (−15, −9) dp");
            Assert.Less(pen.AlphaAt(-16.5f, 0f), 0.1f, "y nada más a la izquierda: el gallardete empieza donde el asta (3 dp del borde del cuadro)");
            Assert.Less(pen.AlphaAt(10f, 8f), 0.1f, "fuera del triángulo no hay nada");
            Assert.Greater(pen.AtDp(-8f, 0f).r, pen.AtDp(-8f, 0f).b + 80, "es dorado");
            var earth = Load("tierra"); int ocean = 0, land = 0;
            foreach (var c in earth.Px)
            {
                if (c.a > 240 && c.b > c.r + 60 && c.b > 150) ocean++;
                if (c.a > 240 && c.g > c.b + 30 && c.g > c.r + 10) land++;
            }
            Assert.Greater(ocean, 20000, "el océano azul"); Assert.Greater(land, 2000, "y las tierras lima");
            Assert.Greater(earth.AlphaAt(0f, 0f), 0.99f, "la esfera es opaca"); Assert.Less(earth.AlphaAt(0f, 61f), 0.05f, "y de radio 58 dp (el cuadro es de 128)");
        }

        [Test]
        public void TheDomes_OnIsLighterThanOff_AndTheBadgesAreTheCoinsOfTheCommonAtlas_WithADiagonalCross()
        {
            var on = Load("domo_on"); var off = Load("domo_off");
            Assert.Greater(Luma(on.AtDp(0f, 0f)), Luma(off.AtDp(0f, 0f)) + 0.2f, "la cúpula encendida (celeste) es más clara que la apagada (violeta)");
            Assert.Greater(on.AlphaAt(0f, 3f), 0.9f, "la media esfera ocupa el centro del cuadro de 28 × 20 dp");
            Assert.Less(on.AlphaAt(0f, -9.5f), 0.05f, "y no llena el cuadro hacia abajo");
            // las insignias son las monedas de lo común (nunca solo color: el ✓ y la aspa son formas); la aspa va en DIAGONAL, no en cruz
            PlasticArt.Disabled = false;
            Assert.AreSame(PlasticArt.Get("comun", "marca_ok"), LandingSprites.Badge(true), "el ✓ es la moneda de lo común");
            Assert.AreSame(PlasticArt.Get("comun", "marca_no"), LandingSprites.Badge(false), "la aspa también");
            var no = Load("marca_no", "comun");
            float diag = 0f, axes = 0f;
            for (int k = 0; k < 4; k++)
            {
                float a = (45f + 90f * k) * Mathf.Deg2Rad, b = 90f * k * Mathf.Deg2Rad;
                diag += Luma(no.At(Mathf.RoundToInt(no.W / 2f + 22f * Mathf.Cos(a)), Mathf.RoundToInt(no.H / 2f + 22f * Mathf.Sin(a))));
                axes += Luma(no.At(Mathf.RoundToInt(no.W / 2f + 22f * Mathf.Cos(b)), Mathf.RoundToInt(no.H / 2f + 22f * Mathf.Sin(b))));
            }
            Assert.Less(diag / 4f, axes / 4f - 0.2f, "los trazos de la aspa (oscuros) van por las diagonales: la aspa nunca es una cruz «+»");
        }
    }
}
