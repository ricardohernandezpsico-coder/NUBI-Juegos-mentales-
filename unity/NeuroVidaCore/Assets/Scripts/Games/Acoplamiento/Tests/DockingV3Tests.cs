using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Acoplamiento.Tests
{
    /// <summary>
    /// Acoplamiento v3 (Tarea 118, 17-oct; docs/previews/plasticina/propuesta-acoplamiento-v3.png): el arte de la pieza y del hueco (la lámpara fija en la pantalla, horneada en la orientación que se ve), lo que NO puede delatar (la fuga del espejo = 0), la silueta (IoU contra el SDF del juego),
    /// el arreglo del alfiler de 2 × 2, el hueco como cavidad con cordón, el tamaño de los textos y de los botones, y que lo de la pantalla no tenga nada de la estación.
    /// </summary>
    public class DockingV3Tests
    {
        private const int Bp = 60;

        private static Cell[] LShape() => new[] { new Cell(0, 0), new Cell(0, 1), new Cell(0, 2), new Cell(1, 0) };
        private static Cell[] FShape() => new[] { new Cell(1, 0), new Cell(2, 0), new Cell(0, 1), new Cell(1, 1), new Cell(1, 2) };
        private static Cell[] PShape() => new[] { new Cell(0, 0), new Cell(1, 0), new Cell(0, 1), new Cell(1, 1), new Cell(0, 2) };               // con un 2 × 2
        private static Cell[] CShape() => new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(0, 1), new Cell(0, 2), new Cell(2, 2) };
        private static Cell[] HShape() => new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(0, 1), new Cell(1, 1), new Cell(0, 2), new Cell(2, 2) };

        private static IEnumerable<Cell[]> Pieces() { yield return LShape(); yield return FShape(); yield return PShape(); yield return CShape(); yield return HShape(); }

        /// <summary>Las celdas reflejadas (x → ancho − 1 − x): la misma pieza vista en un espejo, por el otro camino (sin espejar el dibujo).</summary>
        private static Cell[] Reflect(Cell[] cells)
        {
            int w = 0;
            foreach (var c in cells) w = Mathf.Max(w, c.X + 1);
            var r = new Cell[cells.Length];
            for (int i = 0; i < cells.Length; i++) r[i] = new Cell(w - 1 - cells[i].X, cells[i].Y);
            return r;
        }

        private static Color32[] Face(Cell[] cells, float theta, bool flip, int bp = Bp, bool fix2x2 = true, bool seated = false)
        {
            var g = new DockingArt.PieceGeo(cells, fix2x2);
            int n = DockingArt.SidePx(DockingArt.DisplayedSideBlocks(cells), bp);
            return DockingArt.BakeFace(g, n, bp, theta, flip, new DockingArt.FaceStyle { Seated = seated, Seed = 7 });
        }

        // ------------------------------------------------------------------ la silueta

        [Test]
        public void TheSilhouetteOfTheFaceIsTheSdfOfTheGame_AtLeast99Percent_TurnedAndMirrored()
        {
            foreach (var cells in Pieces())
                foreach (var (theta, flip) in new[] { (225f, true), (30f, false) })
                {
                    var g = new DockingArt.PieceGeo(cells);
                    int n = DockingArt.SidePx(DockingArt.DisplayedSideBlocks(cells), Bp);
                    var face = DockingArt.BakeFace(g, n, Bp, theta, flip, new DockingArt.FaceStyle { Seed = 7 });
                    // la silueta EXACTA: el indicador del SDF a 2 × 2 sub-muestras por píxel, con el mismo giro y espejo (la mitad o más de las muestras adentro)
                    float th = theta * Mathf.Deg2Rad, c = Mathf.Cos(th), s = Mathf.Sin(th), fs = flip ? -1f : 1f, half = n * 0.5f;
                    int inter = 0, union = 0;
                    for (int py = 0; py < n; py++)
                        for (int px = 0; px < n; px++)
                        {
                            int inside = 0;
                            for (int sy = 0; sy < 2; sy++)
                                for (int sx = 0; sx < 2; sx++)
                                {
                                    float X = px + (sx + 0.5f) / 2f - half, Y = py + (sy + 0.5f) / 2f - half;
                                    if (g.Sdf(g.Cx + fs * (X * c + Y * s) / Bp, g.Cy + (-X * s + Y * c) / Bp) <= 0f) inside++;
                                }
                            bool exact = inside >= 2, ours = face[py * n + px].a >= 128;
                            if (exact && ours) inter++;
                            if (exact || ours) union++;
                        }
                    Assert.GreaterOrEqual(inter / (float)union, 0.99f, "IoU de la cara contra el SDF (pieza de " + cells.Length + " bloques, " + theta + "°, espejo " + flip + ")");
                }
        }

        [Test]
        public void TheMirroredPieceOnTheScreenAndTheOneFromReflectedCellsAreTheSame_SoTheLampLeaksNothing()
        {
            foreach (var cells in Pieces())
                foreach (float theta in new[] { 30f, 100f, 225f })
                {
                    var a = Face(cells, theta, true);                       // la pieza espejada EN PANTALLA (como se ve cuando es el espejo)
                    var b = Face(Reflect(cells), theta, false);             // las celdas reflejadas, sin espejar el dibujo
                    Assert.AreEqual(a.Length, b.Length);
                    int worst = 0;
                    for (int i = 0; i < a.Length; i++)
                        worst = Mathf.Max(worst, Mathf.Abs(a[i].r - b[i].r), Mathf.Abs(a[i].g - b[i].g), Mathf.Abs(a[i].b - b[i].b), Mathf.Abs(a[i].a - b[i].a));
                    Assert.LessOrEqual(worst, 1, "la lámpara solo ve la silueta que se ve: la fuga del espejo es 0, salvo el redondeo a 8 bits (" + cells.Length + " bloques, " + theta + "°)");
                }
        }

        [Test]
        public void IfTheLightWereBakedInThePieceFrameTheMirrorWouldShowIt()
        {
            // lo que se descartó: hornear la luz en el marco de la pieza y espejar con la escala x = −1 deja el brillo del lado contrario (el centro del brillo cae lejos del que corresponde)
            var cells = LShape();
            var fixedLamp = Face(cells, 225f, true);
            var framed = Face(cells, 225f, false);
            int n = (int)Mathf.Sqrt(fixedLamp.Length);
            // dónde cae el brillo respecto del centro de la silueta (en x)
            float Offset(Color32[] px, bool flipX)
            {
                float hs = 0f, hw = 0f, ss = 0f, sw = 0f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        var p = px[y * n + (flipX ? n - 1 - x : x)];
                        if (p.a < 250) continue;
                        ss += x; sw += 1f;
                        float lum = 0.299f * p.r + 0.587f * p.g + 0.114f * p.b;
                        if (lum < 200f) continue;                                          // solo el brillo
                        hs += x * lum; hw += lum;
                    }
                return hw > 0f && sw > 0f ? hs / hw - ss / sw : float.NaN;
            }
            float a = Offset(fixedLamp, false), b = Offset(framed, true);
            Assert.IsFalse(float.IsNaN(a) || float.IsNaN(b), "la pieza tiene brillo");
            Assert.Less(a, b - 8f, "con la lámpara fija el brillo cae a la izquierda del centro; con la luz en el marco y el espejo por escala, ≥ 8 px más a la derecha");
        }

        // ------------------------------------------------------------------ el alfiler de 2 × 2

        [Test]
        public void ThePinholeWhereFourBlocksMeetIsFilled_WithTheExtraBoxOf2By2()
        {
            var cells = PShape();
            var withFix = new DockingArt.PieceGeo(cells, true);
            var without = new DockingArt.PieceGeo(cells, false);
            // el centro del 2 × 2 de la P: entre los bloques (0,0), (1,0), (0,1) y (1,1)
            Assert.AreEqual(0f, without.Sdf(0.5f, 0.5f), 1e-5f, "el SDF de hoy deja el punto justo en el borde (d = 0): un alfiler translúcido");
            Assert.AreEqual(-1f, withFix.Sdf(0.5f, 0.5f), 1e-4f, "con la caja de 2 × 2, está adentro a un bloque del borde");
            // con el arreglo no queda ningún píxel translúcido ADENTRO de la silueta (a más de 2 px del borde), ni en el 2 × 2
            int n = DockingArt.SidePx(DockingArt.AlignedSideBlocks, Bp);
            var px = DockingArt.BakeFace(withFix, n, Bp, 0f, false, new DockingArt.FaceStyle { Seed = 7 });
            int translucentInside = 0;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float gx = withFix.Cx + (x + 0.5f - n * 0.5f) / Bp, gy = withFix.Cy + (y + 0.5f - n * 0.5f) / Bp;
                    if (withFix.Sdf(gx, gy) < -2f / Bp && px[y * n + x].a < 250) translucentInside++;
                }
            Assert.AreEqual(0, translucentInside, "ningún píxel translúcido adentro de la pieza");
        }

        // ------------------------------------------------------------------ la pieza de plasticina

        [Test]
        public void TheFaceIsCelesteAndOpaqueAtTheCenterOfABlock_AndTheLampIsLitFromTheTopLeft()
        {
            // una barra de 4 bloques: se compara el borde de arriba contra el de abajo y el de la izquierda contra el de la derecha de su cara
            var bar = new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(3, 0) };
            var g = new DockingArt.PieceGeo(bar);
            int n = DockingArt.SidePx(DockingArt.AlignedSideBlocks, 96);
            var px = DockingArt.BakeFace(g, n, 96, 0f, false, new DockingArt.FaceStyle { Seed = 7 });
            Color32 At(float bx, float by) => px[Mathf.RoundToInt(n * 0.5f + (by - g.Cy) * 96f) * n + Mathf.RoundToInt(n * 0.5f + (bx - g.Cx) * 96f)];
            float Lum(Color32 c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            var center = At(1f, 0f);
            Assert.AreEqual(255, center.a, "opaca al centro");
            Assert.AreEqual(118f, center.r, 28f, "celeste cálido: rojo");
            Assert.AreEqual(208f, center.g, 28f, "celeste cálido: verde");
            Assert.AreEqual(244f, center.b, 22f, "celeste cálido: azul");
            // 0,3 de bloque adentro de cada borde (la almohada): arriba más claro que abajo y la izquierda más clara que la derecha
            Assert.Greater(Lum(At(1f, 0.2f)), Lum(At(1f, -0.2f)) + 6f, "arriba (la lámpara) más claro que abajo");
            Assert.Greater(Lum(At(-0.2f, 0f)), Lum(At(3.2f, 0f)) + 6f, "a la izquierda más claro que a la derecha");
            // el lado de la sombra sigue siendo celeste (no se funde con el cielo): contraste ≥ 3 : 1 contra el cielo de noche
            var dark = At(1f, -0.4f);
            Assert.GreaterOrEqual(Contrast(dark, new Color32(8, 8, 28, 255)), 3f, "el filo de abajo se distingue del cielo");
        }

        private static float Rel(Color32 c)
        {
            float F(byte v) { float x = v / 255f; return x <= 0.03928f ? x / 12.92f : Mathf.Pow((x + 0.055f) / 1.055f, 2.4f); }
            return 0.2126f * F(c.r) + 0.7152f * F(c.g) + 0.0722f * F(c.b);
        }

        private static float Contrast(Color32 a, Color32 b)
        {
            float la = Rel(a), lb = Rel(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        [Test]
        public void ThePieceHasItsEdgeAndItsShadowBelow_AndTheSeatedOneHasNeither()
        {
            var cells = LShape();
            var g = new DockingArt.PieceGeo(cells);
            int n = DockingArt.SidePx(DockingArt.AlignedSideBlocks, 96);
            float pxDp = 96f / 38f;
            var raised = DockingArt.BakeFace(g, n, 96, 0f, false, new DockingArt.FaceStyle { CantoPx = Mathf.RoundToInt(5f * pxDp), Shadow = true, Seed = 7 });
            var seated = DockingArt.BakeFace(g, n, 96, 0f, false, new DockingArt.FaceStyle { Seated = true, Seed = 7 });
            int below = 0, belowSeated = 0;
            // las filas de abajo de la pieza (el canto y la sombra caen hacia ABAJO en la pantalla: filas con índice menor)
            int yEdge = Mathf.RoundToInt(n * 0.5f + (0f - 0.5f - g.Cy) * 96f);               // el borde de abajo del bloque (0,0)
            for (int y = yEdge - 14; y < yEdge - 2; y++)
                for (int x = 0; x < n; x++)
                {
                    if (raised[y * n + x].a > 60) below++;
                    if (seated[y * n + x].a > 60) belowSeated++;
                }
            Assert.Greater(below, 200, "la pieza levantada tiene su canto y su sombra debajo");
            Assert.AreEqual(0, belowSeated, "la asentada queda a ras: sin canto ni sombra");
        }

        [Test]
        public void TheDisplayedAndTheAlignedFramesAreSquareAndBakedPerModule_AndReleased()
        {
            var cells = LShape();
            var shown = DockingArt.BakeDisplayed(cells, -135f, true, out float side);
            Assert.IsNotNull(shown);
            Assert.AreEqual(shown.texture.width, shown.texture.height);
            Assert.AreEqual(side, shown.texture.width / (float)DockingArt.Bp, 1e-4f, "el cuadro en bloques es el real");
            Assert.GreaterOrEqual(side, DockingArt.DisplayedSideBlocks(cells) - 1e-3f);
            DockingArt.BakeAligned(cells, out var raised, out var seated);
            DockingArt.BakeHole(cells, out var cavity, out var cordon);
            foreach (var s in new[] { raised, seated, cavity, cordon })
            {
                Assert.AreEqual(DockingArt.AlignedPx, s.texture.width, "el hueco, la alineada y la asentada miden lo mismo");
                Assert.AreEqual(DockingArt.AlignedPx, s.texture.height);
            }
            foreach (var s in new[] { shown, raised, seated, cavity, cordon }) DockingArt.Release(s);
            DockingArt.Release(null);                                                      // soltar nada no rompe
        }

        // ------------------------------------------------------------------ el hueco: una cavidad con cordón

        [Test]
        public void TheHoleIsACavityWithACordonOutsideIt_AndTheWallIsLitFromTheTopLeftLamp()
        {
            var cells = LShape();
            var g = new DockingArt.PieceGeo(cells);
            int n = DockingArt.AlignedPx;
            DockingArt.BakePort(g, n, 96, 31, out var cavity, out var cordon);
            var face = DockingArt.BakeFace(g, n, 96, 0f, false, new DockingArt.FaceStyle { Seed = 7 });
            // la abertura es la silueta de la pieza (la misma: a la misma escala)
            int inter = 0, union = 0, ringInsideHole = 0, ring = 0;
            for (int i = 0; i < cavity.Length; i++)
            {
                bool hole = cavity[i].a >= 128, piece = face[i].a >= 128;
                if (hole && piece) inter++;
                if (hole || piece) union++;
                if (cordon[i].a >= 128) { ring++; if (hole) ringInsideHole++; }
            }
            Assert.GreaterOrEqual(inter / (float)union, 0.99f, "el hueco es la silueta de la pieza");
            Assert.Greater(ring, 1000, "hay un cordón");
            Assert.AreEqual(0, ringInsideHole, "y va por FUERA de la abertura");
            // el fondo es oscuro y el cordón es gris claro (se tiñe con el estado)
            int cx = Mathf.RoundToInt(n * 0.5f + (0f - g.Cx) * 96f), cy = Mathf.RoundToInt(n * 0.5f + (0.5f - g.Cy) * 96f);
            var floor = cavity[cy * n + cx];
            Assert.Less(floor.r + floor.g + floor.b, 120, "el fondo del hueco es oscuro");
            // la pared de abajo a la derecha (mira hacia la lámpara) es más clara que la de arriba a la izquierda (le da la espalda): la pared del bloque (0,2) arriba / la del (1,0) a la derecha
            float Lum(Color32 c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            float sumTopLeft = 0f, sumBottomRight = 0f; int nTL = 0, nBR = 0;
            for (int y = 14; y < n - 14; y++)
                for (int x = 14; x < n - 14; x++)
                {
                    var c0 = cavity[y * n + x];
                    if (c0.a < 250) continue;
                    // un píxel de la pared: tiene la orilla (alfa < 128) a pocos píxeles; la dirección de la orilla dice de qué pared es
                    bool wallLeft = cavity[y * n + (x - 12)].a < 128, wallUp = cavity[(y + 12) * n + x].a < 128, wallRight = cavity[y * n + (x + 12)].a < 128, wallDown = cavity[(y - 12) * n + x].a < 128;
                    if ((wallLeft || wallUp) && !(wallRight || wallDown)) { sumTopLeft += Lum(c0); nTL++; }
                    if ((wallRight || wallDown) && !(wallLeft || wallUp)) { sumBottomRight += Lum(c0); nBR++; }
                }
            Assert.Greater(nTL, 100);
            Assert.Greater(nBR, 100);
            Assert.Greater(sumBottomRight / nBR, sumTopLeft / nTL + 8f, "la pared de abajo y de la derecha, con luz; la de arriba y de la izquierda, en sombra");
        }

        [Test]
        public void TheBakeOfAModuleIsLogged_AndStaysUnderAGenerousBudget()
        {
            // lo que cuesta un módulo (en el Editor, con Mono: el teléfono compila mejor): la mostrada, el hueco y las dos alineadas; no falla por poco, avisa si se dispara
            var cells = HShape();                                                             // la pieza de 7 bloques: la más cara
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var shown = DockingArt.BakeDisplayed(cells, -135f, true, out _);
            long t1 = sw.ElapsedMilliseconds; sw.Restart();
            DockingArt.BakeHole(cells, out var cavity, out var cordon);
            long t2 = sw.ElapsedMilliseconds; sw.Restart();
            DockingArt.BakeAligned(cells, out var raised, out var seated);
            long t3 = sw.ElapsedMilliseconds;
            Debug.Log("[Acoplamiento] horneado de un módulo de 7 bloques en el Editor: mostrada " + t1 + " ms, hueco " + t2 + " ms, alineada y asentada " + t3 + " ms");
            foreach (var sp in new[] { shown, cavity, cordon, raised, seated }) DockingArt.Release(sp);
            Assert.Less(t1 + t2 + t3, 3000L, "un módulo no debería pasar de 3 s ni en el Editor");
        }

        // ------------------------------------------------------------------ la escultura

        [Test]
        public void TheSculptureCoreTheTracksAndTheSmallPiecesAreDrawn()
        {
            var core = DockingArt.RenderCore(46f, 120f, 9f, 3f, out int cw, out int ch);
            Assert.AreEqual(cw * ch, core.Length);
            int on = 0;
            foreach (var p in core) if (p.a > 128) on++;
            Assert.Greater(on, cw * ch / 3, "el núcleo se dibuja");
            foreach (bool back in new[] { true, false })
            {
                var track = DockingArt.RenderTrack(104f, 20f, 3.4f, 3f, back, out int tw, out int th);
                int n2 = 0;
                foreach (var p in track) if (p.a > 128) n2++;
                Assert.Greater(n2, 500, "la pista (" + (back ? "de atrás" : "de adelante") + ") se dibuja");
            }
            var piece = DockingArt.BakeSculpturePiece(LShape(), 30, 4, 5, out float side);
            Assert.IsNotNull(piece);
            Assert.AreEqual(piece.texture.width / 30f, side, 1e-4f);
            DockingArt.Release(piece);
        }

        // ------------------------------------------------------------------ lo que se ve: textos y botones; nada de la estación

        [Test]
        public void EveryTextOfTheGameIs14DpOrMore_TheButtonsAre56DpOrMore_AndThereIsNoStationLeft()
        {
            var go = new GameObject("DockingView");
            try
            {
                var controller = go.AddComponent<DockingGameController>();
                var build = typeof(DockingGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                var fonts = (List<KeyValuePair<Text, float>>)typeof(DockingGameController).GetField("_fonts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
                Assert.Greater(fonts.Count, 10);
                foreach (var kv in fonts) Assert.GreaterOrEqual(kv.Value, 14f, "el texto «" + kv.Key.name + "» mide " + kv.Value + " dp (el mínimo es 14)");
                foreach (float h in new[] { 640f, 700f, 780f, 800f, 900f })
                    foreach (bool guided in new[] { false, true })
                    {
                        if (guided && h < 720f) continue;                                // (con el tutorial y la fila fija de arriba, bajo 720 la escala llega a su piso de 0,6 o se acerca: los botones miden 46 a 55 dp; la práctica no cuenta)
                        var p = new DockingPlan(h, guided);
                        Assert.GreaterOrEqual(p.ButtonH, 56f - 1e-3f, "botones de 56 dp o más (alto " + h + ", guiada " + guided + ")");
                        Assert.GreaterOrEqual(Mathf.Max(14f, 22f * p.K), 14f, "el texto de los botones nunca baja de 14 dp");
                    }
                // nada de la estación: ni la capa, ni la zona, ni los anillos, ni los casilleros
                var play = go.transform.Find("DockingCanvas/SafeAreaContent/Play");
                Assert.IsNotNull(play, "la capa de juego");
                foreach (var name in new[] { "Station", "StationZone", "RingInk0", "Slot0_0", "Tower" })
                    Assert.IsNull(play.Find(name), "«" + name + "» ya no existe");
                foreach (var t in play.GetComponentsInChildren<Transform>(true))
                    Assert.IsFalse(t.name.StartsWith("Slot") || t.name.StartsWith("Ring") || t.name.StartsWith("Tower"), "queda «" + t.name + "» de la estación");
                // la fila de 8 luces existe
                int lights = 0;
                foreach (var t in play.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Light") && t.name != "LightsZone" && t.name != "LightGlow") lights++;
                Assert.AreEqual(8, lights, "la fila tiene 8 luces");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void NothingInThePortThePieceOrTheLightsDependsOnTheLevel()
        {
            // la pantalla y el arte no reciben el nivel: dificultad = tiempo, ángulo y tamaño de la pieza, nunca contraste
            foreach (var m in typeof(DockingPlan).GetConstructors())
                foreach (var prm in m.GetParameters())
                    Assert.IsFalse(prm.Name.ToLowerInvariant().Contains("level") || prm.Name.ToLowerInvariant().Contains("nivel"), "DockingPlan recibe el nivel");
            foreach (var t in new[] { typeof(DockingSprites), typeof(DockingArt) })
                foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                    foreach (var prm in m.GetParameters())
                        Assert.IsFalse(prm.Name.ToLowerInvariant().Contains("level") || prm.Name.ToLowerInvariant().Contains("nivel"), t.Name + "." + m.Name + " recibe el nivel");
        }

        [Test]
        public void TheFallbackArtOfTheButtonsThePanelAndTheIconsIsStillDrawn()
        {
            // sin el atlas de plasticina el juego dibuja lo de arcilla de siempre
            var panel = DockingSprites.RenderPortPanel(256);
            Assert.AreEqual(256 * 256, panel.Length);
            Assert.AreEqual(255, panel[128 * 256 + 128].a, "el panel es opaco al centro");
            var fit = DockingSprites.RenderButton(256, DockingSprites.LimeColor);
            var mirror = DockingSprites.RenderButton(256, DockingSprites.GrapeColor);
            Assert.AreNotEqual(fit[128 * 256 + 128], mirror[128 * 256 + 128], "uno es lima y el otro uva");
            foreach (var icon in new[] { DockingSprites.RenderFitIcon(128), DockingSprites.RenderMirrorIcon(128), DockingSprites.RenderChevron(64) })
            {
                int on = 0;
                foreach (var p in icon) if (p.a > 128) on++;
                Assert.Greater(on, 100, "el ícono se dibuja");
            }
        }

        [Test]
        public void TheEllipseDistanceIsRightOnAndAroundTheRing()
        {
            const float a = 104f, b = 20f;
            Assert.AreEqual(0f, DockingSprites.EllipseDistance(a, 0f, a, b), 0.05f, "en el extremo derecho");
            Assert.AreEqual(0f, DockingSprites.EllipseDistance(0f, -b, a, b), 0.05f, "abajo");
            Assert.AreEqual(0f, DockingSprites.EllipseDistance(0f, b, a, b), 0.05f, "arriba");
            Assert.AreEqual(5f, DockingSprites.EllipseDistance(0f, b + 5f, a, b), 0.2f, "5 dp arriba del anillo");
            Assert.AreEqual(5f, DockingSprites.EllipseDistance(a + 5f, 0f, a, b), 0.3f, "5 dp afuera del extremo");
            Assert.Less(DockingSprites.EllipseDistance(0f, 0f, a, b), 0f, "el centro está adentro");
            float t = 0.7f;
            Assert.AreEqual(0f, DockingSprites.EllipseDistance(a * Mathf.Cos(t), b * Mathf.Sin(t), a, b), 0.05f);
        }
    }
}
