using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Bodega.Tests
{
    /// <summary>
    /// Bodega de carga en plasticina (Tarea 107): el arte de Blender (<c>tools/juegos/bodega.py</c>) cumple lo que prometió. Estas pruebas miden sobre el ATLAS (se lee el PNG del disco: la textura importada no es legible) lo mismo que
    /// <see cref="BodegaArtTests"/> mide sobre el arte procedural, con los MISMOS umbrales (la hoja de la puerta llena solo la mitad de arriba, el borde dorado de la esclusa), y agrega lo que la plasticina prometió: cada pieza mide lo de siempre, los 12 objetos
    /// caben en su lienzo y se ven sobre el interior oscuro, ningún par de objetos se parece en luminancia (nada solo por el color), la escotilla presionada (la hoja con la huella de un dedo, un punto más oscura), la caja con UNA cinta (sin «+») y
    /// los tres círculos del carro. Las medidas con números las saca <c>tools/juegos/mide_bodega.py</c>.
    /// </summary>
    public sealed class BodegaPlasticTests
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
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", BodegaSprites.PlasticGroup, "atlas.png");
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
            /// <summary>El color a (xDp, yDp) del centro (y hacia arriba).</summary>
            public Color32 AtDp(float xDp, float yDp) => At(Mathf.RoundToInt(W / 2f + xDp * Ppu), Mathf.RoundToInt(H / 2f + yDp * Ppu));
            public float AlphaAt(float xDp, float yDp) => AtDp(xDp, yDp).a / 255f;
        }

        private static Img Load(string sprite)
        {
            var e = PlasticArt.Group(BodegaSprites.PlasticGroup).Entry(sprite);
            Assert.IsNotNull(e, "falta «" + sprite + "» en el atlas «bodega»");
            var colors = Atlas().GetPixels(e.x, e.y, e.w, e.h);
            var px = new Color32[colors.Length];
            for (int i = 0; i < colors.Length; i++) px[i] = colors[i];
            return new Img { W = e.w, H = e.h, Ppu = e.ppu, Px = px };
        }

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

        // ------------------------------------------------------------------ cada pieza mide lo de siempre

        [Test]
        public void EverySprite_IsInTheAtlas_AtTheSizeOfTheProceduralOne()
        {
            Assert.IsTrue(BodegaSprites.Plastic, "falta el atlas de plasticina de Bodega (correr tools/juegos/empaqueta.py bodega)");
            // lo que reemplaza: el mismo lienzo (en píxeles y en píxeles por dp) que el dibujo procedural
            PlasticArt.Disabled = true;
            var proc = new Dictionary<string, Sprite>
            {
                { "marco", BodegaSprites.HatchFrame() }, { "aro", BodegaSprites.HatchRim() }, { "interior_0", BodegaSprites.HatchInterior(0) }, { "interior_1", BodegaSprites.HatchInterior(1) }, { "interior_2", BodegaSprites.HatchInterior(2) },
                { "puerta", BodegaSprites.DoorLeaf() }, { "robot", BodegaSprites.RobotBody() }, { "caja", BodegaSprites.Crate() }, { "esclusa", BodegaSprites.Airlock() }, { "obj_0", BodegaSprites.Object(0) }
            };
            PlasticArt.Disabled = false;
            var plastic = new Dictionary<string, Sprite>
            {
                { "marco", BodegaSprites.HatchFrame() }, { "aro", BodegaSprites.HatchRim() }, { "interior_0", BodegaSprites.HatchInterior(0) }, { "interior_1", BodegaSprites.HatchInterior(1) }, { "interior_2", BodegaSprites.HatchInterior(2) },
                { "puerta", BodegaSprites.DoorLeaf() }, { "robot", BodegaSprites.RobotBody() }, { "caja", BodegaSprites.Crate() }, { "esclusa", BodegaSprites.Airlock() }, { "obj_0", BodegaSprites.Object(0) }
            };
            foreach (var kv in proc)
            {
                var p = plastic[kv.Key];
                Assert.IsNotNull(p, kv.Key);
                Assert.AreNotSame(kv.Value, p, kv.Key + ": sale del atlas");
                Assert.AreEqual(kv.Value.rect.width, p.rect.width, 1f, kv.Key + ": mismo ancho que el procedural (así el controlador no cambia ninguna medida)");
                Assert.AreEqual(kv.Value.rect.height, p.rect.height, 1f, kv.Key + ": mismo alto");
                if (kv.Key != "obj_0") Assert.AreEqual(kv.Value.pixelsPerUnit, p.pixelsPerUnit, 1e-3f, kv.Key + ": mismos píxeles por dp");
            }
            // lo nuevo de plasticina: sus lienzos traen su margen y el controlador lo compensa
            Assert.AreEqual(228f, BodegaSprites.DoorLeafPressed().rect.width, "la hoja presionada mide como la normal");
            Assert.AreEqual(BodegaSprites.DoorLeaf().rect.size, BodegaSprites.DoorLeafPressed().rect.size);
            Assert.AreEqual(228f, BodegaSprites.DoorDisc().rect.width, "el disco de la puerta de la tarjeta de entrada (57 dp para uno de 52)");
            Assert.AreEqual(new Vector2(128f, 80f), BodegaSprites.Visor().rect.size, "visor 32 × 20 dp para uno de 30 × 18");
            Assert.AreEqual(40f, BodegaSprites.Eye().rect.width, "ojo 10 dp para un disco de 6");
            Assert.AreEqual(new Vector2(24f, 56f), BodegaSprites.AntennaStalk().rect.size);
            Assert.AreEqual(40f, BodegaSprites.AntennaBall().rect.width);
            for (int s = 0; s < 3; s++) Assert.AreEqual(160f, BodegaSprites.TrayCircle(s).rect.width, "círculo del carro " + s + ": 40 dp para uno de 38");
            for (int i = 0; i < BodegaContract.ObjectCount; i++) Assert.AreEqual(192f, BodegaSprites.Object(i).rect.width, "objeto " + i + ": 192 px como el de siempre");
            Assert.AreEqual(12 + 2 + 3 + 3 + 5 + 1 + 1 + 3, PlasticArt.Group(BodegaSprites.PlasticGroup).Manifest.sprites.Length, "12 objetos, marco y aro, 3 interiores, 3 puertas, 5 del robot, la caja, la esclusa y 3 círculos");
            // lo que sigue procedural
            foreach (var s in new[] { BodegaSprites.Hull(), BodegaSprites.SealRing() }) Assert.IsNotNull(s, "el casco y el aro de sello siguen procedurales");
        }

        // ------------------------------------------------------------------ los 12 objetos

        [Test]
        public void TheTwelveObjects_FitTheirCanvas_AreBigEnough_AndStandOutOnTheDarkInterior()
        {
            var dark = Load("interior_0").AtDp(0f, 0f);
            for (int i = 0; i < BodegaContract.ObjectCount; i++)
            {
                var im = Load("obj_" + i);
                var b = Body(im, 240);                                                                    // el cuerpo opaco (sin la sombra de contacto)
                float side = Mathf.Max(b.Width, b.Height) / (im.W / 2.24f);                       // unidades de diseño (el objeto de siempre ocupa ±0,8 en un lienzo de ±1,12)
                Assert.Greater(side, 1.25f, BodegaContract.ObjectNames[i] + ": se ve (silueta grande: " + side + " unidades)");
                Assert.LessOrEqual(side, 1.85f, BodegaContract.ObjectNames[i] + ": cabe en su lienzo (" + side + " unidades)");
                Assert.Greater(Mathf.Min(Mathf.Min(b.MinX, im.W - 1 - b.MaxX), Mathf.Min(b.MinY, im.H - 1 - b.MaxY)), 8, BodegaContract.ObjectNames[i] + ": no se pega al borde del lienzo (que cortaría la sombra)");
                Assert.GreaterOrEqual(Contrast(MeanColor(im, 240), dark), 3f, BodegaContract.ObjectNames[i] + ": se ve contra el interior oscuro de la escotilla");
            }
        }

        [Test]
        public void NoTwoObjectsLookAlikeInLuminance_SoNothingDependsOnColorAlone()
        {
            var all = new List<Img>();
            for (int i = 0; i < BodegaContract.ObjectCount; i++) all.Add(Load("obj_" + i));
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                {
                    int d = GrayDifferences(all[i], all[j]);
                    Assert.Greater(d, 1500, BodegaContract.ObjectNames[i] + " y " + BodegaContract.ObjectNames[j] + " se distinguen por forma y claridad, no solo por el color (" + d + " píxeles distintos de " + all[i].Px.Length + ")");
                }
        }

        // ------------------------------------------------------------------ la escotilla

        [Test]
        public void TheDoorLeaf_FillsOnlyTheHalfNextToItsJoint_AndTheTwoLeavesTogetherCoverTheHatch()
        {
            // los mismos puntos y umbrales que la prueba del dibujo procedural
            var leaf = Load("puerta");
            Assert.Greater(leaf.AlphaAt(0f, 12f), 0.9f, "la hoja ocupa la mitad de arriba");
            Assert.Less(leaf.AlphaAt(0f, -12f), 0.05f, "y no la de abajo (la otra hoja es la misma girada 180°)");
            Assert.Less(leaf.AlphaAt(0f, 29f), 0.05f, "no pasa del radio de la escotilla");
            Assert.Less(leaf.AlphaAt(40f, 0f), 0.05f);
            Assert.Greater(leaf.AlphaAt(-24f, 3f) + leaf.AlphaAt(24f, 3f), 1.8f, "cubre el ancho de la escotilla pegada a la junta");
            var disc = Load("puerta_disco");
            Assert.Greater(disc.AlphaAt(0f, 12f), 0.9f); Assert.Greater(disc.AlphaAt(0f, -12f), 0.9f, "el disco de la tarjeta de entrada es entero");
        }

        [Test]
        public void ThePressedLeaf_IsTheSameShapeADotDarker_WithAFingerprint_AwayFromTheHandle()
        {
            var leaf = Load("puerta"); var pressed = Load("puerta_p");
            Assert.AreEqual(leaf.W, pressed.W); Assert.AreEqual(leaf.H, pressed.H, "el mismo lienzo (la escotilla entera se aplasta por escala: la hoja gira bien)");
            var bl = Body(leaf, 128); var bp = Body(pressed, 128);
            Assert.AreEqual(bl.Width, bp.Width, 2, "la misma silueta"); Assert.AreEqual(bl.Height, bp.Height, 2);
            float ratio = Luminance(MeanColor(pressed, 250)) / Luminance(MeanColor(leaf, 250));
            Assert.That(ratio, Is.InRange(0.75f, 0.99f), "un punto más oscura (×0,93 en el color)");
            // la huella: la cara de la hoja se oscurece pareja (×0,83 de luminosidad) salvo donde el dedo hundió la plasticina (a (0, +14) dp, entre el tirador y el aro de la hoja)
            var ratios = new float[leaf.Px.Length]; var opaque = new List<float>();
            for (int i = 0; i < ratios.Length; i++)
                if (leaf.Px[i].a >= 250 && pressed.Px[i].a >= 250) { ratios[i] = Luminance(pressed.Px[i]) / Mathf.Max(Luminance(leaf.Px[i]), 1e-4f); opaque.Add(ratios[i]); }
            opaque.Sort(); float median = opaque[opaque.Count / 2];
            float Dev(float cx, float cy, float rad)
            {
                double sum = 0; int n = 0;
                for (int y = 0; y < leaf.H; y++)
                    for (int x = 0; x < leaf.W; x++)
                    {
                        int i = y * leaf.W + x;
                        if (leaf.Px[i].a < 250 || pressed.Px[i].a < 250) continue;
                        float dx = (x - leaf.W / 2f) / leaf.Ppu - cx, dy = (y - leaf.H / 2f) / leaf.Ppu - cy;
                        if (dx * dx + dy * dy > rad * rad) continue;
                        sum += Mathf.Abs(ratios[i] - median) / median; n++;
                    }
                return n == 0 ? 0f : (float)(sum / n);
            }
            Assert.Greater(Dev(0f, 14f, 5f), 0.05f, "la huella de un dedo se ve en la cara de la hoja");
            Assert.Less(Dev(-10f, 10f, 5f), 0.02f, "y la cara de al lado solo está más oscura, pareja");
            Assert.Less(Dev(10f, 10f, 5f), 0.02f);
        }

        [Test]
        public void TheTappedHatch_LooksPressedAtLeast80ms_OnlyWithPlasticArt_AndOnlyIfItWasJustTapped()
        {
            Assert.GreaterOrEqual(BodegaMotion.PressedSeconds, 0.08f, "como en el resto de los juegos: al menos 80 ms");
            Assert.AreEqual(PlasticTouch.MinSeconds, BodegaMotion.PressedSeconds);
            Assert.IsTrue(BodegaMotion.PressedLook(true, 5f, 5.0f), "en el mismo instante del toque");
            Assert.IsTrue(BodegaMotion.PressedLook(true, 5f, 5.07f), "a los 70 ms todavía");
            Assert.IsFalse(BodegaMotion.PressedLook(true, 5f, 5.09f), "a los 90 ms ya se abre");
            Assert.IsFalse(BodegaMotion.PressedLook(false, 5f, 5.01f), "sin plasticina se queda el achicado de siempre");
            Assert.IsFalse(BodegaMotion.PressedLook(true, -10f, 0.01f), "una escotilla que nadie tocó (PressAt = −10) no");
            Assert.AreEqual(1.04f, BodegaMotion.PressWide); Assert.AreEqual(0.92f, BodegaMotion.PressTall); Assert.AreEqual(0.93f, BodegaMotion.PressDark);
        }

        // ------------------------------------------------------------------ el robot, la caja, la esclusa y el carro

        [Test]
        public void TheRobot_HasAVisorThatStandsOutOnItsBody_AndEyesOnTheVisor()
        {
            var body = Load("robot"); var visor = Load("visor"); var eye = Load("ojo");
            var cb = body.At(20, body.H / 2); var cv = visor.AtDp(0f, 0f); var ce = eye.AtDp(0f, 0f);
            Assert.GreaterOrEqual(Contrast(cv, cb), 4.5f, "el visor oscuro se ve sobre el cuerpo claro");
            Assert.GreaterOrEqual(Contrast(ce, cv), 4.5f, "los ojos claros se ven sobre el visor");
            foreach (var im in new[] { body, visor, eye, Load("tallo"), Load("bola") }) Assert.Greater(Body(im, 200).Width, 4, "cada pieza del robot tiene cuerpo");
        }

        [Test]
        public void TheCrate_HasOneStrap_NotAPlusSign()
        {
            // regla de símbolos neutros (docs/simbolos-neutros.md): la cinta de la caja de siempre cruzaba en «+»; ahora es UNA, de lado a lado
            var crate = Load("caja");
            float L(float x, float y) => Luminance(crate.AtDp(x, y));
            float strap = L(0f, 2f), above = L(0f, 8.5f), below = L(0f, -6f);
            Assert.Less(strap, above * 0.85f, "una cinta más oscura que la madera de arriba");
            Assert.Less(strap, below * 0.85f, "y que la de abajo");
            Assert.AreEqual(L(0f, 8.5f), L(7f, 8.5f), 0.03f, "nada vertical: el centro de arriba es igual que un costado");
            Assert.AreEqual(L(0f, -6f), L(7f, -6f), 0.03f, "ni abajo");
            Assert.AreEqual(L(-9f, 2f), L(9f, 2f), 0.05f, "y la cinta llega de lado a lado");
        }

        [Test]
        public void TheAirlock_HasAGoldFrame_AndAPlanetBehindItsGlass()
        {
            var air = Load("esclusa");
            Assert.Greater(air.AlphaAt(BodegaSprites.WindowR - 1f, 0f), 0.9f, "el marco dorado de la esclusa");
            int gold = 0;
            foreach (var c in air.Px) if (c.a > 200 && c.r > 200 && c.g > 140 && c.b < 150 && c.r > c.b + 80) gold++;
            Assert.Greater(gold, 400, "borde dorado");
            Assert.Less(air.AlphaAt(0f, BodegaSprites.WindowR + 2f), 0.5f, "el contorno es redondo");
            var planet = air.AtDp(8f, -8f); var glass = air.AtDp(-20f, 18f);
            Assert.Greater(planet.b, glass.b + 60, "el planeta celeste se separa del cielo oscuro");
        }

        [Test]
        public void TheThreeTrayCircles_TellApartPendingFoundFromMemoryAndFoundWithAnError()
        {
            var pend = Load("carro_0"); var ok = Load("carro_1"); var err = Load("carro_2");
            Color32 Ring(Img im) => im.AtDp(17.5f, 0f);
            Assert.Greater(Luminance(Ring(ok)), Luminance(Ring(err)), "el borde menta (de memoria) es más claro que el lila (con error)");
            Assert.Greater(Luminance(Ring(err)), Luminance(Ring(pend)), "y el del círculo pendiente es el más tenue");
            Assert.GreaterOrEqual(Contrast(Ring(ok), pend.AtDp(0f, 0f)), 3f, "el borde menta se ve sobre el fondo del carro");
            Assert.IsNotNull(BodegaSprites.CheckBadge(), "la marca ✓ de lo común (o la blanca de siempre)");
        }

        // ------------------------------------------------------------------ el juego usa el arte

        private static string Read(string file) => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Bodega", file));

        [Test]
        public void TheController_ShowsThePressedHatch_AndTheCardsAreKitSlabs()
        {
            string scene = Read("BodegaGameController.Scene.cs");
            StringAssert.Contains("BodegaMotion.PressedLook(", scene);
            StringAssert.Contains("BodegaSprites.DoorLeafPressed()", scene);
            StringAssert.Contains("ApplyPressed(", scene);
            StringAssert.Contains("PressWide", scene);
            string build = Read("BodegaGameController.Build.cs");
            StringAssert.Contains("PlasticSlab.TryStyle(_cardBg, SlabSize.Button, SlabTone.Cream)", build, "la tarjeta de arriba es una losa CREMA del kit (Tarea 129)");
            StringAssert.Contains("PlasticSlab.TryStyle(_toastBg, SlabSize.Button, SlabTone.Cream)", build, "el aviso también");
            StringAssert.Contains("PlasticSlab.TryStyle(_introCard, SlabSize.Panel, SlabTone.Cream)", build, "y la tarjeta de entrada");
            string sprites = Read("BodegaSprites.cs");
            foreach (var needle in new[] { "\"marco\"", "\"aro\"", "\"puerta\"", "\"puerta_p\"", "\"robot\"", "\"caja\"", "\"esclusa\"", "\"obj_\"", "\"carro_\"" }) StringAssert.Contains(needle, sprites);
        }
    }
}
