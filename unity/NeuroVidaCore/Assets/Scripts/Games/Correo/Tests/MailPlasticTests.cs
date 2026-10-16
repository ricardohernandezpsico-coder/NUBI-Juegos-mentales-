using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Correo.Tests
{
    /// <summary>
    /// La estación de correo en plasticina (Tarea 108): el arte de Blender (<c>tools/juegos/correo.py</c>) cumple lo que prometió. Estas pruebas miden sobre el ATLAS (se lee el PNG del disco: la textura importada no es legible) lo mismo que
    /// <see cref="MailArtTests"/> mide sobre el arte procedural, con los MISMOS umbrales (el sello dorado más grande y de borde dentado, los 4 planetas distintos, el dial de 8 marcas, la lámpara encendida más clara), y agrega lo que la plasticina prometió: cada pieza
    /// mide lo de siempre, las 12 cartas se distinguen sin el color, las 4 figuras se distinguen por la forma, las franjas de la torre caben en su trapecio, la marca de raya no es una cruz y la tapa del reloj presionada (más oscura, aplastada, con la huella de un dedo).
    /// Las medidas con números las saca <c>tools/juegos/mide_correo.py</c>.
    /// </summary>
    public sealed class MailPlasticTests
    {
        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        // ------------------------------------------------------------------ el atlas como píxeles

        private static Texture2D _atlas;

        private static Texture2D Atlas()
        {
            if (_atlas != null) return _atlas;
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", MailSprites.PlasticGroup, "atlas.png");
            _atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(_atlas.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return _atlas;
        }

        private sealed class Img
        {
            public int W, H;
            public float Ppu;
            public Color32[] Px;                                    // origen abajo a la izquierda, como Texture2D
            public Color32 At(int x, int y) => Px[Mathf.Clamp(y, 0, H - 1) * W + Mathf.Clamp(x, 0, W - 1)];
            /// <summary>El color a (xDp, yDp) del centro (y hacia ARRIBA).</summary>
            public Color32 AtDp(float xDp, float yDp) => At(Mathf.RoundToInt(W / 2f + xDp * Ppu), Mathf.RoundToInt(H / 2f + yDp * Ppu));
            public float AlphaAt(float xDp, float yDp) => AtDp(xDp, yDp).a / 255f;
        }

        private static Img Load(string sprite, string group = null)
        {
            group = group ?? MailSprites.PlasticGroup;
            var e = PlasticArt.Group(group).Entry(sprite);
            Assert.IsNotNull(e, "falta «" + sprite + "» en el atlas «" + group + "»");
            Texture2D atlas;
            if (group == MailSprites.PlasticGroup) atlas = Atlas();
            else
            {
                atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.IsTrue(atlas.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath, "Resources", "Plasticina", group, "atlas.png"))));
            }
            var colors = atlas.GetPixels(e.x, e.y, e.w, e.h);
            var px = new Color32[colors.Length];
            for (int i = 0; i < colors.Length; i++) px[i] = colors[i];
            return new Img { W = e.w, H = e.h, Ppu = e.ppu, Px = px };
        }

        /// <summary>Una carta: el color a (xDp, yDp) con el origen en el centro de la carta y la y HACIA ABAJO (como las medidas del dibujo de siempre).</summary>
        private static Color32 CardAt(Img card, float xDp, float yDpDown) => card.AtDp(xDp, -yDpDown);

        private static float Luma(Color32 c) => (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;

        private static float Luminance(Color32 c)
        {
            float R(float v) { v /= 255f; return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f); }
            return 0.2126f * R(c.r) + 0.7152f * R(c.g) + 0.0722f * R(c.b);
        }

        private static float Contrast(Color32 a, Color32 b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        private struct Box { public int MinX, MaxX, MinY, MaxY; public int Width => MaxX - MinX + 1; public int Height => MaxY - MinY + 1; }

        private static Box Body(Img im, byte alpha)
        {
            var b = new Box { MinX = im.W, MinY = im.H };
            for (int i = 0; i < im.Px.Length; i++)
                if (im.Px[i].a >= alpha) { int x = i % im.W, y = i / im.W; b.MinX = Math.Min(b.MinX, x); b.MaxX = Math.Max(b.MaxX, x); b.MinY = Math.Min(b.MinY, y); b.MaxY = Math.Max(b.MaxY, y); }
            return b;
        }

        private static Color32 MeanColor(Img im, byte alpha)
        {
            double r = 0, g = 0, b = 0; int n = 0;
            foreach (var c in im.Px) if (c.a >= alpha) { r += c.r; g += c.g; b += c.b; n++; }
            return n == 0 ? new Color32(0, 0, 0, 255) : new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }

        /// <summary>Cuántos píxeles distintos hay entre dos imágenes del mismo tamaño SOLO por forma y luminancia (como lo vería quien no distingue colores): alfa distinto, o luminancia relativa distinta en más de 0,25.</summary>
        private static int GrayDifferences(Img a, Img b)
        {
            int d = 0;
            for (int i = 0; i < a.Px.Length; i++)
            {
                var pa = a.Px[i]; var pb = b.Px[i];
                if (Mathf.Abs(pa.a - pb.a) > 128) { d++; continue; }
                if (pa.a > 200 && pb.a > 200 && Mathf.Abs(Mathf.Sqrt(Luminance(pa)) - Mathf.Sqrt(Luminance(pb))) > 0.25f) d++;
            }
            return d;
        }

        private static bool IsGold(Color32 c) => c.a > 200 && c.r > 170 && c.g > 120 && c.b < 110 && c.r - c.b > 100;          // con la luz, el oro de una marca sombreada baja hasta (180, 127, 55)

        // ------------------------------------------------------------------ cada pieza mide lo de siempre

        private static readonly MailCue[] Cues = { MailCue.None, MailCue.Gold, MailCue.Lazo };

        [Test]
        public void EverySprite_IsInTheAtlas_AtTheSizeOfTheProceduralOne()
        {
            Assert.IsTrue(MailSprites.Plastic, "falta el atlas de plasticina de Correo (correr tools/juegos/empaqueta.py correo)");
            // lo que reemplaza: el mismo lienzo (en píxeles y en píxeles por dp) que el dibujo procedural
            PlasticArt.Disabled = true;
            var proc = new Dictionary<string, Sprite> { { "planeta", MailSprites.Planet(0) }, { "dial", MailSprites.Dial() }, { "torre", MailSprites.Tower() }, { "lampara", MailSprites.Lamp(false) }, { "nave", MailSprites.Ship() }, { "saco", MailSprites.Sack() }, { "raya", MailSprites.CheckNo() } };
            var procLetters = new Dictionary<string, Sprite>();
            foreach (var cue in Cues) for (int p = 0; p < MailContract.PlanetCount; p++) procLetters[p + "/" + cue] = MailSprites.Letter(p, cue);
            PlasticArt.Disabled = false;
            var plastic = new Dictionary<string, Sprite> { { "planeta", MailSprites.Planet(0) }, { "dial", MailSprites.Dial() }, { "torre", MailSprites.Tower() }, { "lampara", MailSprites.Lamp(false) }, { "nave", MailSprites.Ship() }, { "saco", MailSprites.Sack() }, { "raya", MailSprites.CheckNo() } };
            foreach (var kv in proc)
            {
                var p = plastic[kv.Key];
                Assert.IsNotNull(p, kv.Key);
                Assert.AreNotSame(kv.Value, p, kv.Key + ": sale del atlas");
                Assert.AreEqual(kv.Value.rect.width, p.rect.width, 1f, kv.Key + ": mismo ancho que el procedural (así el controlador no cambia ninguna medida)");
                Assert.AreEqual(kv.Value.rect.height, p.rect.height, 1f, kv.Key + ": mismo alto");
                Assert.AreEqual(kv.Value.pixelsPerUnit, p.pixelsPerUnit, 1e-3f, kv.Key + ": mismos píxeles por dp");
            }
            foreach (var cue in Cues)
                for (int p = 0; p < MailContract.PlanetCount; p++)
                {
                    var l = MailSprites.Letter(p, cue); var o = procLetters[p + "/" + cue];
                    Assert.AreNotSame(o, l, "la carta " + p + "/" + cue + " sale del atlas");
                    Assert.AreEqual(o.rect.width, l.rect.width, 1f, "carta " + p + "/" + cue + ": 384 px (128 dp a 3 px por dp)");
                    Assert.AreEqual(o.rect.height, l.rect.height, 1f);
                    Assert.AreEqual(o.pixelsPerUnit, l.pixelsPerUnit, 1e-3f);
                }
            Assert.AreEqual(MailSprites.Lamp(true).rect.size, MailSprites.Lamp(false).rect.size, "las dos lámparas miden igual");
            Assert.AreNotSame(MailSprites.Lamp(true), MailSprites.Lamp(false));
            // lo nuevo de plasticina: sus lienzos traen su margen y el controlador lo compensa
            Assert.AreEqual(new Vector2(MailSprites.DoorBoxW * 4f, MailSprites.DoorBoxH * 4f), MailSprites.Door().rect.size, "la puerta: 62 × 68 dp en un lienzo de 66 × 72");
            Assert.AreEqual(new Vector2(MailSprites.HandleBoxW * 4f, MailSprites.HandleBoxH * 4f), MailSprites.Handle().rect.size, "la manija: 28 × 7 dp en 32 × 12");
            Assert.AreEqual(MailSprites.ClockBaseBox * 4f, MailSprites.ClockBase().rect.width, "la base del reloj: un aro de 59 dp en 64");
            Assert.AreEqual(MailSprites.ClockCoverBox * 4f, MailSprites.ClockCover(false).rect.width, "la tapa: un disco de 47 dp en 50");
            Assert.AreEqual(MailSprites.ClockCover(false).rect.size, MailSprites.ClockCover(true).rect.size, "la presionada mide como la normal");
            Assert.AreEqual(12 + 4 + 1 + 1 + 1 + 1 + 2 + 1 + 1 + 1 + 3, PlasticArt.Group(MailSprites.PlasticGroup).Manifest.sprites.Length, "12 cartas, 4 planetas, dial, puerta, manija, torre, 2 lámparas, nave, saco, raya y 3 del reloj");
            // lo que sigue procedural
            foreach (var s in new[] { MailSprites.Flame(), MailSprites.DashFrame(), MailSprites.DayBar() }) Assert.IsNotNull(s, "el propulsor, el marco punteado y la barra del día siguen procedurales");
            Assert.IsNotNull(MailSprites.CheckOk(), "la marca ✓: la moneda de lo común");
        }

        // ------------------------------------------------------------------ las cartas y el sello dorado

        [Test]
        public void EveryLetter_HasAPaper_ATransparentCorner_AndTheThreeKindsLookDifferentWithoutColor()
        {
            var all = new List<Img>(); var names = new List<string>(); var kinds = new List<MailCue>();
            foreach (var cue in Cues)
                for (int p = 0; p < MailContract.PlanetCount; p++)
                {
                    var im = Load("carta_" + p + "_" + (int)cue);
                    int opaque = 0; foreach (var c in im.Px) if (c.a > 200) opaque++;
                    Assert.Greater(opaque / (float)im.Px.Length, 0.5f, "la carta ocupa la mayor parte de su caja (" + p + "/" + cue + ")");
                    Assert.AreEqual(0, Mathf.Max(im.Px[0].a, Mathf.Max(im.Px[im.W - 1].a, Mathf.Max(im.Px[(im.H - 1) * im.W].a, im.Px[im.Px.Length - 1].a))), "las esquinas son transparentes (" + p + "/" + cue + ")");
                    all.Add(im); names.Add(p + "/" + cue); kinds.Add(cue);
                }
            // la señal (sello dorado, lazo o ninguna) se distingue por forma y claridad: nada depende solo del color
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                    if (kinds[i] != kinds[j]) Assert.Greater(GrayDifferences(all[i], all[j]), 1500, names[i] + " y " + names[j] + " se distinguen por forma y claridad, no solo por el color");
        }

        /// <summary>La figura oscura del sello NORMAL de la carta [planet] (17 dp): lo que no es blanco a menos de 12 dp del centro del sello (el borde de tinta queda a 14-15 dp).</summary>
        private static bool[] SealFigure(Img card)
        {
            var m = new bool[card.Px.Length];
            float cx = MailSprites.SealCx(false), cyUp = -MailSprites.SealCy(false);
            for (int iy = 0; iy < card.H; iy++)
                for (int ix = 0; ix < card.W; ix++)
                {
                    float x = (ix + 0.5f - card.W / 2f) / card.Ppu, y = (iy + 0.5f - card.H / 2f) / card.Ppu;
                    if (Mathf.Abs(x - cx) >= 12f || Mathf.Abs(y - cyUp) >= 12f) continue;
                    var c = card.Px[iy * card.W + ix];
                    m[iy * card.W + ix] = c.a > 200 && Luma(c) < 0.85f;
                }
            return m;
        }

        [Test]
        public void TheFourSealFigures_AreDistinctShapes_BigEnoughToRead_AndStandOutFromTheWhiteSeal()
        {
            var figs = new List<bool[]>();
            for (int p = 0; p < MailContract.PlanetCount; p++)
            {
                var card = Load("carta_" + p + "_0");
                var f = SealFigure(card); int area = 0; foreach (var b in f) if (b) area++;
                Assert.Greater(area / (card.Ppu * card.Ppu), 100f, "la figura del sello de " + p + " mide más de 100 dp² (a 17 dp de lado)");
                figs.Add(f);
            }
            for (int i = 0; i < figs.Count; i++)
                for (int j = i + 1; j < figs.Count; j++)
                {
                    int both = 0, either = 0;
                    for (int k = 0; k < figs[i].Length; k++) { if (figs[i][k] && figs[j][k]) both++; if (figs[i][k] || figs[j][k]) either++; }
                    Assert.Less(both / (float)Mathf.Max(1, either), 0.93f, "las figuras de los sellos " + i + " y " + j + " tienen siluetas distintas (IoU " + both / (float)Mathf.Max(1, either) + "): la forma es la pista, no el color");
                }
        }

        [Test]
        public void TheGoldSeal_IsBiggerThanTheNormalOne_AndHasAPostalEdge_ManyShortRoundedTeeth_NotRays()
        {
            var normal = Load("carta_0_0"); var gold = Load("carta_0_1");
            float cx = MailSprites.SealCx(true), cy = MailSprites.SealCy(true), half = MailSprites.SealHalf(true);
            float paperL = Luma(CardAt(normal, -30f, -20f));
            // (a) el sello dorado cabe en el papel (112 × 76) y no pisa las líneas de dirección
            Assert.LessOrEqual(cx + half, 56f - 4f, "no se sale del papel por la derecha");
            Assert.GreaterOrEqual(cy - half, -38f + 4f, "ni por arriba");
            Assert.Less(cy + half, 10f - 2f, "y no pisa las líneas de dirección");
            // (b) el interior dorado se aparta del papel en grises, y el borde es oscuro (café)
            Assert.Greater(Mathf.Abs(Luma(CardAt(gold, cx - 8f, cy + 10f)) - paperL), 0.08f, "el dorado no es del tono del papel");
            // (c) la mancha del sello (lo que se aparta del papel en su zona) es bastante mayor que la del normal: más grande por forma y tamaño, no solo por color
            int bg = Blob(gold, paperL), bn = Blob(normal, paperL);
            Assert.Greater(bg, bn * 1.25f, "en grises la mancha del sello dorado es más grande que la del normal (" + bg + " contra " + bn + ")");
            // (d) a lo largo del borde de arriba (0,7 dp adentro) los dientes oscuros alternan con el papel: son MUCHOS (cinco por lado), cortos y redondos, nunca rayos
            int teeth = 0; bool wasDark = false;
            for (float dx = -12f; dx <= 12f; dx += 0.25f)
            {
                bool dark = Luma(CardAt(gold, cx + dx, cy - half + 0.7f)) < paperL - 0.2f;
                if (dark && !wasDark) teeth++;
                wasDark = dark;
            }
            Assert.GreaterOrEqual(teeth, 4, "el borde de arriba tiene al menos 4 dientes oscuros separados por el papel (hay " + teeth + ")");
            // (e) el normal es liso: ninguna alternancia a lo largo de su borde
            float ncx = MailSprites.SealCx(false), ncy = MailSprites.SealCy(false), nh = MailSprites.SealHalf(false);
            int normalTeeth = 0; bool wasDarkN = false;
            for (float dx = -9f; dx <= 9f; dx += 0.25f)
            {
                bool dark = Luma(CardAt(normal, ncx + dx, ncy - nh + 0.7f)) < paperL - 0.2f;
                if (dark && !wasDarkN) normalTeeth++;
                wasDarkN = dark;
            }
            Assert.LessOrEqual(normalTeeth, 1, "el sello normal es liso (un solo borde continuo)");
        }

        private static int Blob(Img card, float paperL)
        {
            int n = 0;
            for (int iy = 0; iy < card.H; iy++)
                for (int ix = 0; ix < card.W; ix++)
                {
                    float x = (ix + 0.5f - card.W / 2f) / card.Ppu, yUp = (iy + 0.5f - card.H / 2f) / card.Ppu, yDown = -yUp;
                    if (x < 10f || x > 56f || yDown < -38f || yDown > 8f) continue;
                    var c = card.Px[iy * card.W + ix];
                    if (c.a > 200 && Mathf.Abs(Luma(c) - paperL) > 0.08f) n++;
                }
            return n;
        }

        // ------------------------------------------------------------------ las 4 figuras: la forma es la pista principal

        private static bool[] WhiteFigure(Img planet)
        {
            var m = new bool[planet.Px.Length];
            for (int i = 0; i < m.Length; i++) { var c = planet.Px[i]; m[i] = c.a > 200 && c.r > 225 && c.g > 215 && c.b > 205; }
            return m;
        }

        [Test]
        public void ThePlanets_AreFourDistinctFigures_TheShapeTellsThemApart_AndTheyAreNeverOnlyAColor()
        {
            var figs = new List<bool[]>(); var cols = new List<Color32>();
            for (int p = 0; p < 4; p++)
            {
                var im = Load("planeta_" + p);
                var f = WhiteFigure(im); int area = 0; foreach (var b in f) if (b) area++;
                Assert.Greater(area / (float)(im.W * im.H), 0.04f, "el planeta " + p + " lleva su figura blanca, grande");
                Assert.Greater(Body(im, 240).Width / im.Ppu, 50f, "el planeta " + p + " ocupa su caja (radio 28 en 64 dp)");
                figs.Add(f); cols.Add(MeanColor(im, 240));
            }
            for (int i = 0; i < 4; i++)
                for (int j = i + 1; j < 4; j++)
                {
                    int both = 0, either = 0;
                    for (int k = 0; k < figs[i].Length; k++) { if (figs[i][k] && figs[j][k]) both++; if (figs[i][k] || figs[j][k]) either++; }
                    Assert.Less(both / (float)Mathf.Max(1, either), 0.93f, "las figuras de los planetas " + i + " y " + j + " tienen siluetas distintas (IoU " + both / (float)Mathf.Max(1, either) + ")");
                }
        }

        // ------------------------------------------------------------------ el dial, la puerta, el faro

        [Test]
        public void TheDial_HasEightGoldMarks_NotASpokedWheel()
        {
            var dial = Load("dial");
            int marks = 0, between = 0;
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f, b = (k + 0.5f) * Mathf.PI / 4f;
                if (IsGold(dial.AtDp(9f * Mathf.Cos(a), 9f * Mathf.Sin(a)))) marks++;
                if (IsGold(dial.AtDp(9f * Mathf.Cos(b), 9f * Mathf.Sin(b)))) between++;
            }
            Assert.GreaterOrEqual(marks, 7, "las ocho marcas doradas a 9 dp del centro");
            Assert.LessOrEqual(between, 1, "y nada dorado entre ellas: son puntos, no radios (la rueda de 3 rayos parece el símbolo de la paz)");
            Assert.IsTrue(IsGold(dial.AtDp(0f, 0f)), "el centro dorado");
        }

        [Test]
        public void TheDoorAndTheHandle_MeasureWhatTheOldOnesDid()
        {
            var door = Load("caja_puerta"); var b = Body(door, 200);
            Assert.AreEqual(62f, b.Width / door.Ppu, 2f, "la puerta mide 62 dp de ancho");
            Assert.AreEqual(68f, b.Height / door.Ppu, 2f, "y 68 de alto");
            var handle = Load("manija"); var h = Body(handle, 200);
            Assert.AreEqual(28f, h.Width / handle.Ppu, 1.5f, "la manija mide 28 dp");
            Assert.AreEqual(7f, h.Height / handle.Ppu, 1.5f, "y 7 de alto");
            Assert.GreaterOrEqual(Contrast(MeanColor(handle, 250), MeanColor(door, 250)), 2.5f, "la manija dorada se ve sobre la puerta");
        }

        [Test]
        public void TheTowerStripes_FitInsideTheTrapezoid_AndTheLampOnIsLighterThanTheLampOff()
        {
            var tower = Load("torre");
            foreach (float y in new[] { 5.5f, -5.5f })
            {
                int rowY = Mathf.RoundToInt(tower.H / 2f + y * tower.Ppu);
                int r0 = int.MaxValue, r1 = -1, t0 = int.MaxValue, t1 = -1;
                for (int x = 0; x < tower.W; x++)
                {
                    var c = tower.At(x, rowY);
                    if (c.a > 200) { t0 = Math.Min(t0, x); t1 = Math.Max(t1, x); }
                    if (c.a > 200 && c.r > 190 && c.g < 130 && c.b < 120) { r0 = Math.Min(r0, x); r1 = Math.Max(r1, x); }
                }
                Assert.Greater(r1, r0, "la franja roja a y = " + y);
                Assert.GreaterOrEqual(r0 - t0, 1, "la franja roja no se sale del trapecio por la izquierda (y = " + y + ")");
                Assert.GreaterOrEqual(t1 - r1, 1, "ni por la derecha (y = " + y + ")");
            }
            var off = Load("lampara_0"); var on = Load("lampara_1");
            Assert.Greater(Luminance(on.AtDp(0f, 0f)), Luminance(off.AtDp(0f, 0f)), "encendida es más clara que apagada");
        }

        // ------------------------------------------------------------------ el reloj, la nave, el saco y la raya

        [Test]
        public void TheClock_HasABaseWithARingAndADeepFill_AndALilacCoverThatStandsOut()
        {
            var bas = Load("reloj_base"); var cover = Load("reloj_tapa");
            var fill = bas.AtDp(0f, 0f); var ring = bas.AtDp(28f, 0f);
            Assert.Greater(bas.AlphaAt(0f, 0f), 0.95f, "el fondo es opaco");
            Assert.AreEqual(59f, Body(bas, 128).Width / bas.Ppu, 2.5f, "el aro mide 59 dp (el de siempre)");
            Assert.AreEqual(47f, Body(cover, 128).Width / cover.Ppu, 2f, "la tapa es un disco de 47 dp");
            var tint = MeanColor(cover, 250);
            Assert.Greater(tint.b, tint.g + 30, "la tapa es lila (violeta claro)");
            Assert.GreaterOrEqual(Contrast(tint, new Color32(0x0A, 0x0F, 0x33, 255)), 4.5f, "la tapa se ve contra el cielo");
            Assert.Greater(Contrast(tint, fill), 3f, "la tapa se ve sobre el fondo del reloj");
            Assert.AreNotEqual(Luminance(ring), Luminance(fill), "el aro y el fondo no son lo mismo");
        }

        [Test]
        public void ThePressedCover_IsFlatterADotDarker_WithAFingerprint()
        {
            var cover = Load("reloj_tapa"); var pressed = Load("reloj_tapa_p");
            Assert.AreEqual(cover.W, pressed.W); Assert.AreEqual(cover.H, pressed.H, "el mismo lienzo");
            var bn = Body(cover, 128); var bp = Body(pressed, 128);
            Assert.GreaterOrEqual(bp.Width, bn.Width, "aplastada: un poco más ancha que la normal (+4 %: la plasticina se ensancha al hundirse; su espesor baja 8 %)");
            Assert.LessOrEqual(bp.Width, bn.Width * 1.08f, "pero cabe en el mismo lienzo");
            float ratio = Luminance(MeanColor(pressed, 250)) / Luminance(MeanColor(cover, 250));
            Assert.That(ratio, Is.InRange(0.75f, 0.99f), "un punto más oscura (×0,93 en el color)");
            // la huella: la cara se oscurece pareja salvo donde el dedo hundió la plasticina (arriba a la izquierda)
            var ratios = new float[cover.Px.Length]; var opaque = new List<float>();
            for (int i = 0; i < ratios.Length; i++)
                if (cover.Px[i].a >= 250 && pressed.Px[i].a >= 250) { ratios[i] = Luminance(pressed.Px[i]) / Mathf.Max(Luminance(cover.Px[i]), 1e-4f); opaque.Add(ratios[i]); }
            opaque.Sort(); float median = opaque[opaque.Count / 2];
            float Dev(float cx, float cy, float rad)
            {
                double sum = 0; int n = 0;
                for (int y = 0; y < cover.H; y++)
                    for (int x = 0; x < cover.W; x++)
                    {
                        int i = y * cover.W + x;
                        if (cover.Px[i].a < 250 || pressed.Px[i].a < 250) continue;
                        float dx = (x - cover.W / 2f) / cover.Ppu - cx, dy = (y - cover.H / 2f) / cover.Ppu - cy;
                        if (dx * dx + dy * dy > rad * rad) continue;
                        sum += Mathf.Abs(ratios[i] - median) / median; n++;
                    }
                return n == 0 ? 0f : (float)(sum / n);
            }
            Assert.Greater(Dev(-9f, 7f, 5f), 0.03f, "la huella de un dedo se ve en la cara de la tapa");
            Assert.Less(Dev(9f, -7f, 5f), 0.02f, "y la cara de enfrente solo está más oscura, pareja");
        }

        [Test]
        public void TheShip_HasAGoldEnvelopeAndABlueWindow_TheSackIsGoldish_AndTheRayaIsARingWithOneHorizontalBar_NotACross()
        {
            var ship = Load("nave"); int gold = 0, blue = 0;
            foreach (var c in ship.Px)
            {
                if (c.a > 200 && c.r > 230 && c.g > 170 && c.b < 120) gold++;
                if (c.a > 200 && c.b > 220 && c.g > 180 && c.r < 170) blue++;
            }
            Assert.Greater(gold, 100, "el sobre dorado de la nave"); Assert.Greater(blue, 100, "y la ventanilla celeste");
            var sack = Load("saco"); var mean = MeanColor(sack, 250);
            Assert.Greater(mean.r, mean.b + 40, "el saco es dorado, cálido");
            var raya = Load("marca_raya");
            Assert.Less(raya.AlphaAt(0f, 5f), 0.1f, "la raya: nada arriba del centro");
            Assert.Less(raya.AlphaAt(0f, -5f), 0.1f, "ni abajo (sin barra vertical: no es una cruz)");
            Assert.Greater(raya.AlphaAt(4f, 0f), 0.9f, "pero sí la barra horizontal, a los lados");
            Assert.Greater(raya.AlphaAt(-4f, 0f), 0.9f);
            Assert.Greater(raya.AlphaAt(12f, 0f), 0.9f, "y el aro");
        }

        // ------------------------------------------------------------------ el juego usa el arte

        private static string Read(string file) => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Correo", file));

        [Test]
        public void TheController_UsesTheKitSlabs_ThePressedStates_AndTheAtlasPieces()
        {
            string build = Read("MailGameController.Build.cs");
            StringAssert.Contains("PlasticSlab.Attach(b.Root, SlabSize.Button, SlabTone.Night)", build, "los buzones son losas «noche» del kit");
            StringAssert.Contains("PlasticSlab.Attach(_safeRoot, SlabSize.Button, SlabTone.Night)", build, "la caja fuerte también");
            StringAssert.Contains("PlasticSlab.Attach(_beaconRoot, SlabSize.Button, SlabTone.Night)", build, "y el faro");
            StringAssert.Contains("PlasticTouch.On(", build, "todo lo que se toca tiene su estado presionado");
            foreach (var card in new[] { "_rushBg", "_radioBg", "_peekBg", "_routineCard", "_introCard", "_endCard", "r.Card" }) StringAssert.Contains("SlabInstead(" + card + ",", build, "«" + card + "» es una losa del kit");
            StringAssert.Contains("SlabTone.Lime", build, "el botón de la hoja es una losa lima"); StringAssert.Contains("SlabTone.Sky", build, "el del resumen, celeste");
            string main = Read("MailGameController.cs");
            foreach (var needle in new[] { "_boxes[box].Touch?.Press()", "_safeTouch?.Press()", "_beaconTouch?.Press()", "_clockTouch?.Press()", "_briefTouch?.Press()", "_recapTouch?.Press()" }) StringAssert.Contains(needle, main, "el toque aceptado hunde la pieza: " + needle);
            string scene = Read("MailGameController.Scene.cs");
            StringAssert.Contains("MailSprites.ClockCover(_clockPressedShown)", scene, "la tapa del reloj presionada");
            string sprites = Read("MailSprites.cs");
            foreach (var needle in new[] { "\"carta_\"", "\"planeta_\"", "\"dial\"", "\"torre\"", "\"lampara_1\"", "\"nave\"", "\"saco\"", "\"marca_raya\"", "\"caja_puerta\"", "\"manija\"", "\"reloj_base\"", "\"reloj_tapa_p\"" }) StringAssert.Contains(needle, sprites);
        }
    }
}
