using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Parejas.Tests
{
    /// <summary>
    /// Constelaciones en plasticina (Tarea 106): el arte de Blender (<c>tools/juegos/constelaciones.py</c>) cumple lo que prometió. Estas pruebas miden sobre el ATLAS (se lee el PNG del disco: la textura importada no es legible) lo mismo que
    /// <see cref="ConstelacionArtTests"/> mide sobre el arte procedural, con los MISMOS umbrales: los 12 objetos y los 4 gemelos tienen la silueta grande de un objeto y caben en su luz; los gemelos se distinguen por un detalle GRANDE de forma
    /// (anillo, llama, patas, antena); la luz dormida es oscura con un núcleo claro y la abierta es clara; los aros siguen siendo procedurales. Además: el borde de tinta que separa cada objeto del disco claro, que ningún par de objetos se
    /// distinga solo por el color (se comparan en luminancia), y que la luz dormida presionada es la misma esfera aplastada, un punto más oscura, con el núcleo intacto (la huella cae lejos de él: `mide_parejas.py` lo cuenta) y sin saltar de lugar.
    /// Las medidas con números las saca <c>tools/juegos/mide_parejas.py</c>.
    /// </summary>
    public sealed class ConstelacionPlasticTests
    {
        private static readonly Color32 Ink = new Color32(0x1A, 0x12, 0x40, 255);

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        // ------------------------------------------------------------------ el atlas como píxeles

        private static Texture2D _atlas;

        private static Texture2D Atlas()
        {
            if (_atlas != null) return _atlas;
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", ConstelacionSprites.PlasticGroup, "atlas.png");
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

            /// <summary>El alfa a (xDp, yDp) unidades del centro (y hacia arriba), como <c>AlphaAt</c> de las pruebas procedurales.</summary>
            public float AlphaAt(float xDp, float yDp) => At(Mathf.RoundToInt(W / 2f + xDp * Ppu), Mathf.RoundToInt(H / 2f + yDp * Ppu)).a / 255f;
        }

        private static Img Load(string sprite)
        {
            var e = PlasticArt.Group(ConstelacionSprites.PlasticGroup).Entry(sprite);
            Assert.IsNotNull(e, "falta «" + sprite + "» en el atlas «parejas»");
            var colors = Atlas().GetPixels(e.x, e.y, e.w, e.h);
            var px = new Color32[colors.Length];
            for (int i = 0; i < colors.Length; i++) px[i] = colors[i];
            return new Img { W = e.w, H = e.h, Ppu = e.ppu, Px = px };
        }

        private static Img Obj(int kind, int variant) => Load("obj_" + kind + "_" + variant);

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

        private static int Filled(Img im)
        {
            int n = 0;
            foreach (var c in im.Px) if (c.a > 20) n++;
            return n;
        }

        /// <summary>Cuántos píxeles distintos hay entre dos imágenes del mismo tamaño (la misma medida que usan las pruebas procedurales).</summary>
        private static int Differences(Img a, Img b)
        {
            int d = 0;
            for (int i = 0; i < a.Px.Length; i++)
            {
                var pa = a.Px[i]; var pb = b.Px[i];
                if (Mathf.Abs(pa.a - pb.a) > 80 || (pa.a > 200 && pb.a > 200 && (Mathf.Abs(pa.r - pb.r) + Mathf.Abs(pa.g - pb.g) + Mathf.Abs(pa.b - pb.b)) > 120)) d++;
            }
            return d;
        }

        /// <summary>Lo mismo, pero SOLO por luminancia (como lo vería quien no distingue colores): alfa distinto, o luminancia relativa distinta en más de 0,25.</summary>
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

        private struct Box { public int MinX, MaxX, MinY, MaxY; public int Width => MaxX - MinX + 1; public int Height => MaxY - MinY + 1; public float CenterX => (MinX + MaxX) / 2f; public float CenterY => (MinY + MaxY) / 2f; }

        private static Box Body(Img im, byte alpha)
        {
            var b = new Box { MinX = im.W, MinY = im.H };
            for (int i = 0; i < im.Px.Length; i++)
                if (im.Px[i].a >= alpha) { int x = i % im.W, y = i / im.W; b.MinX = Math.Min(b.MinX, x); b.MaxX = Math.Max(b.MaxX, x); b.MinY = Math.Min(b.MinY, y); b.MaxY = Math.Max(b.MaxY, y); }
            return b;
        }

        private static float MeanLuminance(Img im, byte alpha)
        {
            double sum = 0; int n = 0;
            foreach (var c in im.Px) if (c.a >= alpha) { sum += Luminance(c); n++; }
            return n == 0 ? 0f : (float)(sum / n);
        }

        // ------------------------------------------------------------------ los 16 objetos

        [Test]
        public void EverySprite_IsInTheAtlas_AtTheSizeOfTheProceduralOne_AndTheTwinsAreOtherImages()
        {
            Assert.IsTrue(ConstelacionSprites.Plastic, "falta el atlas de plasticina de Constelaciones (correr tools/juegos/empaqueta.py parejas)");
            for (int k = 0; k < ConstelacionContract.ObjectKinds; k++)
            {
                var s = ConstelacionSprites.Object(k, 0);
                Assert.IsNotNull(s, ConstelacionContract.ObjectNames[k]);
                Assert.AreEqual((int)(ConstelacionSprites.ObjBox * ConstelacionSprites.ObjPpd), (int)s.rect.width, ConstelacionContract.ObjectNames[k] + ": mide lo mismo que el procedural (la caja de 56 unidades)");
                Assert.AreEqual(ConstelacionSprites.ObjPpd, s.pixelsPerUnit, 1e-3f);
                if (ConstelacionContract.HasTwin(k)) Assert.AreNotSame(s, ConstelacionSprites.Object(k, 1), ConstelacionContract.ObjectNames[k] + ": el gemelo es otra imagen");
                else Assert.AreSame(s, ConstelacionSprites.Object(k, 1), ConstelacionContract.ObjectNames[k] + ": sin gemelo, la variante no cambia nada");
            }
            foreach (var s in new[] { ConstelacionSprites.Dormant(), ConstelacionSprites.DormantPressed(), ConstelacionSprites.Open() })
            {
                Assert.IsNotNull(s);
                Assert.AreEqual(Mathf.CeilToInt(ConstelacionSprites.DormantBox * 3f), (int)s.rect.width, "la luz mide lo mismo que la procedural (la caja de 84 unidades a 3 px por unidad)");
                Assert.AreEqual(3f, s.pixelsPerUnit, 1e-3f);
            }
            Assert.AreEqual(16 + 3, PlasticArt.Group(ConstelacionSprites.PlasticGroup).Manifest.sprites.Length, "16 objetos (12 + 4 gemelos) y 3 luces (dormida, presionada, abierta)");
            foreach (var s in new[] { ConstelacionSprites.RingMemory(), ConstelacionSprites.RingNew() }) Assert.IsNotNull(s, "los aros siguen procedurales");
        }

        [Test]
        public void EveryObjectFitsInsideItsLight_AndHasTheLargeSilhouetteOfAnObject()
        {
            // el dibujo ocupa ±22 unidades; mostrado a R × 1,32 / 40 por unidad cabe adentro de la luz de radio R (los mismos umbrales que las pruebas procedurales)
            for (int k = 0; k < ConstelacionContract.ObjectKinds; k++)
                foreach (int v in ConstelacionContract.HasTwin(k) ? new[] { 0, 1 } : new[] { 0 })
                {
                    var im = Obj(k, v);
                    Assert.Greater(Filled(im), 120, ConstelacionContract.ObjectNames[k] + ": no puede estar vacío");
                    var b = Body(im, 40);
                    float w = b.Width / im.Ppu, h = b.Height / im.Ppu;
                    Assert.Greater(Math.Max(w, h), 24f, ConstelacionContract.ObjectNames[k] + " " + v + ": se ve (silueta grande)");
                    Assert.LessOrEqual(Math.Max(w, h), 54f, ConstelacionContract.ObjectNames[k] + " " + v + ": cabe en la luz (a R × 1,32 / 40 por unidad, 54 unidades ocupan el 90 % de su diámetro)");
                }
        }

        [Test]
        public void EveryObject_HasTheInkOutlineOfTheSketch_ThatSeparatesItFromTheLightDisc()
        {
            // el borde de tinta del boceto (1,3 unidades hacia afuera de la silueta): lo que separa a un objeto claro (el casco, el cohete) del disco claro de la luz abierta
            var open = Load("abierta");
            var face = open.At(open.W / 2 - Mathf.RoundToInt(12f * open.Ppu), open.H / 2 + Mathf.RoundToInt(12f * open.Ppu));
            Assert.GreaterOrEqual(Contrast(Ink, face), 7f, "la tinta se separa del disco claro");
            for (int k = 0; k < ConstelacionContract.ObjectKinds; k++)
            {
                var im = Obj(k, 0);
                int ink = 0;
                foreach (var c in im.Px)
                    if (c.a > 240 && Mathf.Abs(c.r - Ink.r) + Mathf.Abs(c.g - Ink.g) + Mathf.Abs(c.b - Ink.b) < 60) ink++;
                // 1,3 unidades de ancho a 4 px por unidad alrededor de una silueta de al menos 24 unidades de largo: más de 300 px de tinta
                Assert.Greater(ink, 300, ConstelacionContract.ObjectNames[k] + ": le falta el borde de tinta (" + ink + " px)");
                // y la silueta exterior ES tinta: el píxel opaco más externo de cada fila del medio
                var b = Body(im, 240);
                int row = Mathf.RoundToInt(b.CenterY);
                for (int x = 0; x < im.W; x++)
                    if (im.At(x, row).a > 240) { Assert.Less(Mathf.Abs(im.At(x, row).r - Ink.r) + Mathf.Abs(im.At(x, row).g - Ink.g) + Mathf.Abs(im.At(x, row).b - Ink.b), 120, ConstelacionContract.ObjectNames[k] + ": el borde de la silueta es de tinta"); break; }
            }
        }

        [Test]
        public void TheTwinsDifferFromTheirOriginalsByALargeShapeDetail_NeverByATone()
        {
            // planeta sin anillo, cohete con llama grande, ovni con patas, casco con antena
            foreach (int kind in ConstelacionContract.TwinKinds)
            {
                int d = Differences(Obj(kind, 0), Obj(kind, 1));
                Assert.Greater(d, 700, ConstelacionContract.ObjectNames[kind] + ": el gemelo se distingue por una forma grande (" + d + " píxeles distintos)");
                Assert.Greater(GrayDifferences(Obj(kind, 0), Obj(kind, 1)), 500, ConstelacionContract.ObjectNames[kind] + ": también en luminancia (no solo por el color)");
            }
            // el planeta con anillo es más ancho que el planeta sin anillo (forma, no tono): el anillo sale a los costados, inclinado
            var withRing = Obj(0, 0);
            var without = Obj(0, 1);
            Assert.Greater(withRing.AlphaAt(19.5f, 6f) + withRing.AlphaAt(-19.5f, -6f), 0.8f, "el anillo sale a los costados (inclinado)");
            Assert.Less(without.AlphaAt(19.5f, 6f) + without.AlphaAt(-19.5f, -6f), 0.1f, "sin anillo no hay nada ahí");
            // el cohete con llama grande llega más abajo
            Assert.Greater(Obj(1, 1).AlphaAt(0f, -24f), 0.8f, "la llama grande");
            Assert.Less(Obj(1, 0).AlphaAt(0f, -24f), 0.1f);
            // el ovni con patas llega más abajo que el ovni sin patas
            Assert.Greater(Obj(3, 1).AlphaAt(-11f, -13f) + Obj(3, 1).AlphaAt(11f, -13f), 0.8f, "las patas del ovni");
            Assert.Less(Obj(3, 0).AlphaAt(-11f, -13f) + Obj(3, 0).AlphaAt(11f, -13f), 0.1f);
            // el casco con antena llega más arriba y a la derecha
            Assert.Greater(Obj(5, 1).AlphaAt(10f, 22f), 0.8f, "la bolita de la antena del casco");
            Assert.Less(Obj(5, 0).AlphaAt(10f, 22f), 0.1f);
        }

        [Test]
        public void TheSatellitePanels_HaveThreeParallelStripes_NotAPlusSign()
        {
            // regla de símbolos neutros (docs/simbolos-neutros.md): una división en «+» se lee como cruz; cada panel lleva tres rayas verticales y nada atravesado
            var im = Obj(7, 0);
            float L(float x, float yUp) => Luminance(im.At(Mathf.RoundToInt(im.W / 2f + x * im.Ppu), Mathf.RoundToInt(im.H / 2f + yUp * im.Ppu)));
            foreach (float cx in new[] { -14.5f, 14.5f })
            {
                foreach (float dx in new[] { -3.5f, 0f, 3.5f })
                {
                    Assert.Greater(L(cx + dx, 0f), L(cx + dx + 1.75f, 0f) + 0.08f, "una raya clara a " + (cx + dx) + " (el hueco de al lado es el panel)");
                    Assert.AreEqual(L(cx + dx, 0f), L(cx + dx, 3.5f), 0.08f, "la raya sigue arriba y abajo: es vertical");
                }
                Assert.AreEqual(L(cx + 1.75f, 0f), L(cx + 1.75f, 3.5f), 0.05f, "nada cruza el panel de lado a lado: no hay «+»");
                Assert.AreEqual(L(cx - 1.75f, 0f), L(cx - 1.75f, -3.5f), 0.05f);
            }
        }

        [Test]
        public void NoTwoObjectsLookAlikeInLuminance_SoNothingDependsOnColorAlone()
        {
            var all = new System.Collections.Generic.List<(string name, Img im)>();
            for (int k = 0; k < ConstelacionContract.ObjectKinds; k++) all.Add((ConstelacionContract.ObjectNames[k], Obj(k, 0)));
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                {
                    int d = GrayDifferences(all[i].im, all[j].im);
                    Assert.Greater(d, 900, all[i].name + " y " + all[j].name + " se distinguen por forma y claridad, no solo por el color (" + d + " píxeles distintos de " + all[i].im.Px.Length + ")");
                }
        }

        // ------------------------------------------------------------------ las luces

        [Test]
        public void TheDormantLightAndTheOpenLightLookDifferent_TheDormantOneIsDarkWithABrightCore_AndTheOpenOneIsLight()
        {
            var dormant = Load("dormante"); var open = Load("abierta");
            Assert.Greater(Differences(dormant, open), 3000, "una luz dormida y una abierta se distinguen");
            // dormida: oscura, con un núcleo claro al medio
            var center = dormant.At(dormant.W / 2, dormant.H / 2);
            var edge = dormant.At(dormant.W / 2 + Mathf.RoundToInt(30f * dormant.Ppu), dormant.H / 2);
            Assert.Greater(center.b / 255f, edge.b / 255f + 0.2f, "tiene un núcleo tibio");
            Assert.Less((edge.r + edge.g + edge.b) / 255f, 1.2f, "la esfera es oscura");
            // abierta: clara (el mismo punto que mira la prueba procedural)
            var oc = open.At(open.W / 2 - Mathf.RoundToInt(12f * open.Ppu), open.H / 2 + Mathf.RoundToInt(12f * open.Ppu));
            Assert.Greater((oc.r + oc.g + oc.b) / 255f, 2.2f, "el disco abierto es claro");
            // la dormida es una esfera redonda de 76 unidades (radio 38) y la abierta un disco del mismo tamaño
            foreach (var im in new[] { dormant, open })
            {
                var b = Body(im, 240);
                Assert.AreEqual(2f * ConstelacionSprites.BakeR, b.Width / im.Ppu, 3f, "la luz mide el diámetro de siempre");
                Assert.AreEqual(b.Width, b.Height, 6, "redonda");
            }
            // la dormida se separa del cielo (el brillo del borde y la luz de arriba): contraste contra el cielo del juego
            var sky = new Color32(0x0A, 0x0F, 0x33, 255);
            float bright = 0f;
            for (int a = 0; a < 360; a += 10)
            {
                var p = dormant.At(dormant.W / 2 + Mathf.RoundToInt(Mathf.Cos(a * Mathf.Deg2Rad) * 33f * dormant.Ppu), dormant.H / 2 + Mathf.RoundToInt(Mathf.Sin(a * Mathf.Deg2Rad) * 33f * dormant.Ppu));
                bright = Mathf.Max(bright, Contrast(p, sky));
            }
            Assert.Greater(bright, 1.6f, "la esfera dormida se ve contra el cielo");
        }

        [Test]
        public void ThePressedDormantLight_IsTheSameSphereSquashed_Darker_WithTheCoreIntact_AndDoesNotJump()
        {
            var normal = Load("dormante"); var pressed = Load("dormante_p");
            Assert.AreEqual(normal.W, pressed.W); Assert.AreEqual(normal.H, pressed.H, "el mismo lienzo");
            var bn = Body(normal, 240); var bp = Body(pressed, 240);
            Assert.GreaterOrEqual(bp.Width, bn.Width, "aplastada: no más angosta");
            Assert.LessOrEqual(bp.Width / (float)bn.Width, 1.07f, "~4 % más ancha");
            Assert.Less(bp.Height / (float)bp.Width, bn.Height / (float)bn.Width - 0.01f, "más baja que ancha: la esfera está aplastada (vista desde arriba su alto casi no cambia, su ancho sí)");
            Assert.That(bp.Height / (float)bn.Height, Is.InRange(0.9f, 1.05f), "pero sigue siendo la misma esfera");
            Assert.Less(Mathf.Abs(bp.CenterX - normal.W / 2f), 3f, "centrada: la luz no salta al cambiar de imagen");
            Assert.Less(Mathf.Abs(bp.CenterY - normal.H / 2f), 3f);
            Assert.Greater(Mathf.Min(Mathf.Min(bp.MinX, normal.W - 1 - bp.MaxX), Mathf.Min(bp.MinY, normal.H - 1 - bp.MaxY)), 6, "no se pega al borde del lienzo (que cortaría la sombra)");
            float ratio = MeanLuminance(pressed, 240) / MeanLuminance(normal, 240);
            Assert.That(ratio, Is.InRange(0.75f, 0.99f), "un punto más oscura (×0,93 en el color)");
            Assert.Greater(Differences(normal, pressed), 600, "la huella y el aplastamiento se ven");
            // el núcleo sigue siendo lo más claro de la esfera y tan claro como el de la normal (la huella no lo pisa ni lo tapa)
            float CoreLuminance(Img im, int r)
            {
                float best = 0f;
                for (int y = im.H / 2 - 40; y < im.H / 2 + 40; y++)
                    for (int x = im.W / 2 - 40; x < im.W / 2 + 40; x++)
                    {
                        float sum = 0f; int n = 0;
                        for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++) { sum += Luminance(im.At(x + dx, y + dy)); n++; }
                        best = Mathf.Max(best, sum / n);
                    }
                return best;
            }
            float coreNormal = CoreLuminance(normal, 3), corePressed = CoreLuminance(pressed, 3);
            Assert.Greater(corePressed, 0.45f, "el núcleo claro está en la luz presionada");
            Assert.Greater(corePressed, coreNormal * 0.75f, "casi tan claro como el de la normal (un poco más oscuro, como todo lo presionado: el color ×0,93 es ~×0,84 en luminancia)");
        }

        // ------------------------------------------------------------------ el juego usa el arte

        private static string Read(string file) => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Parejas", file));

        [Test]
        public void TheController_ShowsThePressedLight_AndTheCardsAreKitSlabs()
        {
            string scene = Read("ConstelacionGameController.Scene.cs");
            StringAssert.Contains("ConstelacionSprites.DormantPressed()", scene);
            StringAssert.Contains("ConstelacionMotion.PressedLook(", scene);
            StringAssert.Contains("v.Pressed", scene);
            string build = Read("ConstelacionGameController.Build.cs");
            StringAssert.Contains("PlasticSlab.TryStyle(_cardBg, SlabSize.Button, SlabTone.Night)", build, "la tarjeta de arriba es una losa «noche» del kit");
            StringAssert.Contains("PlasticSlab.TryStyle(_introCard, SlabSize.Panel, SlabTone.Cream)", build, "la tarjeta de entrada es una lámina CREMA (Tarea 129)");
            string sprites = Read("ConstelacionSprites.cs");
            foreach (var needle in new[] { "\"dormante\"", "\"abierta\"", "\"obj_\"" })
                StringAssert.Contains(needle, sprites);
        }
    }
}
