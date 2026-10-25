using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Engranajes;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Engranajes.Tests
{
    /// <summary>Lo que se ve y se oye de «Engranajes»: la disposición en cada altura de pantalla, el arte horneado (los dientes en su lugar), los sonidos, que lo dibujado esté encendido y
    /// opaco, y que el reloj sea el de la pausa.</summary>
    public class EngranajesLayoutTests
    {
        private static readonly float[] Heights = { 560f, 600f, 640f, 700f, 740f, 780f, 860f, 960f };

        [Test]
        public void EveryScreenHeight_FitsEverythingInOrder_WithoutOverlap()
        {
            foreach (float h in Heights)
            {
                var m = EngranajesLayout.Compute(h);
                string where = "alto " + h;
                Assert.GreaterOrEqual(m.StripTop, EngranajesLayout.HudDp, where + ": la cabecera va bajo el marcador");
                Assert.Greater(m.QuestionTop, m.StripTop + EngranajesLayout.StripH - 0.01f, where + ": la consigna va bajo la cabecera");
                float sceneTop = m.SceneCenterLogicalY - EngranajesLayout.SceneHeight * m.SceneScale / 2f;
                float sceneBottom = m.SceneCenterLogicalY + EngranajesLayout.SceneHeight * m.SceneScale / 2f;
                Assert.GreaterOrEqual(sceneTop, m.StripTop + EngranajesLayout.StripH - 2.5f, where + ": la mesa va bajo la cabecera «Cohete número N»");
                Assert.LessOrEqual(sceneBottom, m.ButtonsTop, where + ": la cuenta de cambios y «Arrancar» van bajo la escena");
                Assert.LessOrEqual(m.ButtonsTop + m.ButtonH, m.SayTop, where + ": el aviso va bajo la barra de abajo");
                Assert.LessOrEqual(m.SayTop + m.SayH, h + 0.01f, where + ": el aviso cabe en la pantalla");
                Assert.GreaterOrEqual(m.ButtonH, 52f, where + ": la barra de abajo mide al menos 52 dp");
                Assert.GreaterOrEqual(m.SceneScale, EngranajesLayout.MinScale - 0.001f, where + ": la escena no se achica de más");
                Assert.LessOrEqual(m.SceneScale, EngranajesLayout.MaxScale + 0.001f, where + ": la escena no se agranda");
            }
        }

        [Test]
        public void TheScene_StaysInsideTheScreenWidth_AtEveryScale()
        {
            foreach (float h in Heights)
            {
                var m = EngranajesLayout.Compute(h);
                var left = EngranajesLayout.SceneToLogical(m, EngranajesLayout.TableLeft, 280f);       // el borde izquierdo de la mesa (la flecha del motor y su rótulo caen adentro)
                var right = EngranajesLayout.SceneToLogical(m, EngranajesLayout.TableRight, 280f);     // el borde derecho de la mesa (los carteles llegan a 369)
                Assert.GreaterOrEqual(left.x, 8f, "alto " + h + ": la mesa deja 8 dp o más a la izquierda");
                Assert.LessOrEqual(right.x, EngranajesLayout.W - 8f, "alto " + h + ": la mesa deja 8 dp o más a la derecha");
                // el corrimiento de 4,5 dp es fijo (centra la escena a escala 1,0): a escala k la mesa queda 4,5 × (1 − k) dp descentrada, o sea que los dos márgenes difieren 9 × (1 − k) dp (a la escala máxima, 0,88, poco más de 1 dp)
                Assert.AreEqual(left.x, EngranajesLayout.W - right.x, 9f * (1f - m.SceneScale) + 0.6f, "alto " + h + ": la mesa queda centrada en la pantalla");
            }
        }

        [Test]
        public void TheWorkshopTable_StartsUnderTheRocketHeader_TheAirIsSpread_AndTheSayGoes10DpUnderTheButtons()
        {
            // Tarea 164 (partes E y «aire repartido»): la mesa empieza bajo «Cohete número N» (a 16 dp de él y, si sobra aire, hasta 36 dp más abajo), los botones van a 8 dp de la mesa más lo que se reparta y el aviso queda 10 dp bajo ellos
            foreach (float h in Heights)
            {
                var m = EngranajesLayout.Compute(h);
                string where = "alto " + h;
                float tableTop = m.SceneCenterLogicalY - EngranajesLayout.SceneHeight * m.SceneScale / 2f;
                Assert.GreaterOrEqual(tableTop, EngranajesLayout.TableTopDp - 0.01f, where + ": la mesa empieza bajo la cabecera");
                Assert.LessOrEqual(tableTop, EngranajesLayout.TableTopDp + EngranajesLayout.MaxTopShift + 0.01f, where + ": la mesa no baja más de 36 dp de la cabecera");
                Assert.AreEqual(EngranajesLayout.SayGap, m.SayTop - (m.ButtonsTop + m.ButtonH), 0.01f, where + ": el aviso queda 10 dp bajo los botones");
                Assert.LessOrEqual(m.ButtonsTop - (tableTop + EngranajesLayout.SceneHeight * m.SceneScale), 8f + EngranajesLayout.MaxGap + 0.01f, where + ": los botones no se despegan de la mesa más de 72 dp");
            }
            // en un teléfono alto (800 dp) la escena empezaba antes hacia 174 dp (120 + el 40 % del aire que sobraba): sigue más de 30 dp más arriba
            var tall = EngranajesLayout.Compute(800f);
            float before = 174.4f, now = tall.SceneCenterLogicalY - EngranajesLayout.SceneHeight * tall.SceneScale / 2f;
            Assert.GreaterOrEqual(before - now, 30f, "la escena sigue ~35 dp más arriba que antes de la Tarea 164 en un teléfono de 800 dp");
            Assert.AreEqual(108f, EngranajesLayout.TableTopDp, 0.01f);
            Assert.Greater(tall.SceneScale, 0.8f);
            // el aire se reparte: bajo los botones queda menos cielo vacío (antes ~196 dp), los botones son más altos (≥ 110) y bajo el aviso quedan al menos 40 dp
            Assert.LessOrEqual(800f - (tall.ButtonsTop + tall.ButtonH), 160f, "bajo los botones no quedan más de 160 dp vacíos");
            Assert.GreaterOrEqual(tall.ButtonH, 110f, "los botones crecen con el aire");
            Assert.GreaterOrEqual(800f - tall.Bottom, 40f, "bajo el aviso quedan al menos 40 dp");
        }

        [Test]
        public void TheWindow_IsInsideTheTable_WithAFrameOfAtLeast12Dp_AndTheCartelsFit()
        {
            Assert.GreaterOrEqual((EngranajesLayout.WindowCx - EngranajesLayout.WindowW / 2f) - EngranajesLayout.TableLeft, 12f, "marco de la izquierda");
            Assert.GreaterOrEqual(EngranajesLayout.SceneBottom - (EngranajesLayout.WindowCy + EngranajesLayout.WindowH / 2f), 12f, "marco de abajo");
            Assert.GreaterOrEqual((EngranajesLayout.WindowCy - EngranajesLayout.WindowH / 2f) - EngranajesLayout.SceneTop, 12f, "marco de arriba");
            Assert.GreaterOrEqual(EngranajesLayout.TableRight, EngranajesLayout.CartelRight + 4f, "los carteles, que llegan a 369, caben en la mesa");
            Assert.AreEqual((EngranajesLayout.TableLeft + EngranajesLayout.TableRight) / 2f, 180f - EngranajesLayout.SceneShiftX, 0.01f, "la mesa está centrada en el eje de la escena");
            Assert.AreEqual(280f, (EngranajesLayout.SceneTop + EngranajesLayout.SceneBottom) / 2f, 0.01f, "el centro de la escena sigue en 280 (el origen de B())");
        }

        [Test]
        public void TheWidestCartel_StaysAtLeast16DpFromTheRightEdge_AtEveryScale()
        {
            foreach (float h in Heights)
            {
                var m = EngranajesLayout.Compute(h);
                var right = EngranajesLayout.SceneToLogical(m, EngranajesLayout.CartelRight, 142f);
                Assert.LessOrEqual(right.x, EngranajesLayout.W - EngranajesLayout.CartelMargin, "alto " + h + ": el cartel de la turbina y de la antena deja 16 dp o más a la derecha");
            }
        }

        [Test]
        public void TheCounterAndTheStartButton_FitSideBySide_AndTheStartButtonIsTheBigOne()
        {
            foreach (float h in Heights)
            {
                var m = EngranajesLayout.Compute(h);
                var counter = EngranajesLayout.CounterRect(m);
                var start = EngranajesLayout.StartRect(m);
                string where = "alto " + h;
                Assert.GreaterOrEqual(counter.x, 0f, where);
                Assert.LessOrEqual(start.xMax, EngranajesLayout.W, where);
                Assert.Greater(start.x, counter.xMax, where + ": no se pisan");
                Assert.GreaterOrEqual(counter.width, 100f, where);
                Assert.GreaterOrEqual(start.width, 200f, where + ": «Arrancar» es el botón grande");
                Assert.GreaterOrEqual(start.height, 52f, where);
            }
        }

        [Test]
        public void ATapOnTheScreen_CanBeTurnedBackIntoASceneSpotAndBack()
        {
            foreach (float h in Heights)
            {
                var m = EngranajesLayout.Compute(h);
                foreach (var p in new[] { new Vector2(40f, 150f), new Vector2(240f, 300f), new Vector2(306f, 442f) })
                {
                    var logical = EngranajesLayout.SceneToLogical(m, p.x, p.y);
                    var back = EngranajesLayout.LogicalToScene(m, logical.x, logical.y);
                    Assert.AreEqual(p.x, back.x, 1e-3f, "alto " + h);
                    Assert.AreEqual(p.y, back.y, 1e-3f, "alto " + h);
                }
            }
        }

        [Test]
        public void TheIntroCards_FitInTheScreen_EvenIfEachLineBreaksInTwoAt14dpOrMore()
        {
            // la tarjeta «NUEVO» tiene el ancho de la pantalla menos márgenes; con letra de 16 dp (48 px del lienzo) cada línea ocupa a lo más dos
            float widthPx = (EngranajesLayout.W - 36f - 28f) * 3f;
            foreach (Intro intro in Enum.GetValues(typeof(Intro)))
            {
                if (intro == Intro.None) continue;
                int lines = 0;
                foreach (var line in EngranajesContract.IntroText(intro))
                {
                    var measure = NeuroVida.Games.Shared.CoachText.Measure(line, widthPx, 48);
                    Assert.LessOrEqual(measure.Lines, 2, intro + ": «" + line + "» ocupa " + measure.Lines + " líneas");
                    lines += measure.Lines;
                }
                Assert.LessOrEqual(lines, 6, intro + ": la tarjeta no crece de más");
            }
        }
    }

    public class EngranajesArtTests
    {
        // estas pruebas leen los píxeles del dibujo PROCEDURAL (el que queda sin el atlas de plasticina o si falla); lo de plasticina lo mide EngranajesPlasticTests sobre el atlas
        [SetUp]
        public void SetUp() { PlasticArt.Disabled = true; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        /// <summary>El alfa (0..1) del sprite en el punto (x, y) dp desde el centro, y hacia arriba.</summary>
        private static float AlphaAt(Sprite s, float xDp, float yDp)
        {
            var tex = s.texture;
            float ppd = s.pixelsPerUnit;
            int px = Mathf.Clamp(Mathf.RoundToInt(tex.width / 2f + xDp * ppd), 0, tex.width - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(tex.height / 2f + yDp * ppd), 0, tex.height - 1);
            return tex.GetPixel(px, py).a;
        }

        [Test]
        public void TheGearSprites_HaveTheirToothPointingRight_AndAGapBetweenTeeth()
        {
            foreach (var size in new[] { EngranajesSprites.GearSize.Big, EngranajesSprites.GearSize.Small })
            {
                var pal = EngranajesSprites.Palette.Main;
                var s = EngranajesSprites.Gear(size, pal);
                float tip = EngranajesSprites.TipOf(size);
                int n = EngranajesSprites.TeethOf(size);
                float p = 2f * Mathf.PI / n;
                Assert.Greater(AlphaAt(s, tip - 2.5f, 0f), 0.95f, size + ": el diente 0 está a la derecha");
                // en el medio entre dos dientes (a medio paso) el borde está más adentro: a la altura de la punta hay hueco
                float gx = Mathf.Cos(p / 2f) * (tip - 1.5f), gy = Mathf.Sin(p / 2f) * (tip - 1.5f);
                Assert.Less(AlphaAt(s, gx, gy), 0.1f, size + ": entre dos dientes hay hueco");
                Assert.Greater(AlphaAt(s, 0f, 0f), 0.9f, size + ": el centro está lleno");
                var sil = EngranajesSprites.Silhouette(size);
                Assert.Greater(AlphaAt(sil, tip - 2.5f, 0f), 0.9f);
            }
        }

        [Test]
        public void TheTwoNeighbourGears_FitTheirToothIntoTheGapOfTheOther_InTheSprites()
        {
            // dos engranajes unidos (grande y chico) con las fases del contrato: en el punto de contacto el diente de uno está donde el otro tiene hueco
            var a = new GearDef { X = 0f, Y = 0f, N = EngranajesContract.BigTeeth, Pitch = EngranajesContract.BigPitch, Tip = EngranajesContract.BigTip, A = 0.37f };
            var b = new GearDef { X = 50f, Y = 0f, N = EngranajesContract.SmallTeeth, Pitch = EngranajesContract.SmallPitch, Tip = EngranajesContract.SmallTip };
            b.A = EngranajesContract.MeshPhase(a, b);
            var sa = EngranajesSprites.Gear(EngranajesSprites.GearSize.Big, EngranajesSprites.Palette.Main);
            var sb = EngranajesSprites.Gear(EngranajesSprites.GearSize.Small, EngranajesSprites.Palette.Station);
            // se mide el solapamiento de los RELLENOS (no de los bordes tinta, que al tocarse se pisan un poco) en una rejilla fina cerca del contacto, y se compara con el mismo par girado medio paso
            int Overlap(float angleB)
            {
                int both = 0;
                for (float x = 14f; x <= 36f; x += 0.5f)
                    for (float y = -9f; y <= 9f; y += 0.5f)
                        if (FillAtRotated(sa, x, y, a.A) && FillAtRotated(sb, x - 50f, y, angleB)) both++;
                return both;
            }
            int fit = Overlap(b.A);
            int clash = Overlap(b.A + Mathf.PI / b.N);
            Assert.Greater(clash, 15, "con los dientes enfrentados (mal encajados) se nota el choque: " + clash + " (encajados: " + fit + ")");
            Assert.LessOrEqual(fit * 3, clash, "encajados casi no se pisan y mal encajados sí: " + fit + " contra " + clash);
        }

        /// <summary>El alfa del sprite girado <paramref name="radians"/> en el sentido del reloj (y hacia abajo en la pantalla del boceto) en el punto (x, y) del boceto (y hacia abajo).</summary>
        /// <summary>true si en ese punto hay RELLENO claro del engranaje (no el borde tinta oscuro).</summary>
        private static bool FillAtRotated(Sprite s, float x, float yDown, float radians)
        {
            float cA = Mathf.Cos(radians), sA = Mathf.Sin(radians);
            float xs = x * cA + yDown * sA, ys = x * sA - yDown * cA;
            var tex = s.texture;
            float ppd = s.pixelsPerUnit;
            int px = Mathf.Clamp(Mathf.RoundToInt(tex.width / 2f + xs * ppd), 0, tex.width - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(tex.height / 2f + ys * ppd), 0, tex.height - 1);
            var c = tex.GetPixel(px, py);
            return c.a > 0.9f && (c.r + c.g + c.b) / 3f > 0.5f;
        }

        private static float AlphaAtRotated(Sprite s, float x, float yDown, float radians)
        {
            // un punto del boceto (x, yDown) con el engranaje girado <radians> en el sentido del reloj se busca en el sprite sin girar, girándolo al revés
            float cA = Mathf.Cos(radians), sA = Mathf.Sin(radians);
            return AlphaAt(s, x * cA + yDown * sA, x * sA - yDown * cA);
        }

        [Test]
        public void EverySprite_IsBaked_AndWithoutAnEmptyTexture()
        {
            var bake = EngranajesSprites.Prewarm();
            while (bake.MoveNext()) { }
            var sprites = new List<Sprite>
            {
                EngranajesSprites.Room(), EngranajesSprites.Hull(), EngranajesSprites.Dish(), EngranajesSprites.Fan(), EngranajesSprites.Door(), EngranajesSprites.CargoFrame(),
                EngranajesSprites.CargoPlate(), EngranajesSprites.CargoBox(), EngranajesSprites.Hub(), EngranajesSprites.Pulley(), EngranajesSprites.Rack(), EngranajesSprites.Wrench(),
                EngranajesSprites.Check(), EngranajesSprites.MiniArrow(true), EngranajesSprites.MiniArrow(false)
            };
            foreach (var w in EngranajesSprites.BeltWidths) sprites.Add(EngranajesSprites.BeltCap(w));
            foreach (var sprite in sprites)
            {
                Assert.IsNotNull(sprite);
                float filled = 0f;
                var px = sprite.texture.GetPixels32();
                foreach (var c in px) if (c.a > 20) filled++;
                Assert.Greater(filled / px.Length, 0.01f, "el sprite " + sprite.name + " no puede estar vacío");
            }
            foreach (EngranajesSprites.Glyph g in Enum.GetValues(typeof(EngranajesSprites.Glyph)))
            {
                var icon = EngranajesSprites.GlyphSprite(g);
                int filled = 0;
                foreach (var c in icon.texture.GetPixels32()) if (c.a > 128) filled++;
                Assert.Greater(filled, 150, "el ícono " + g + " se dibuja");
            }
            // la flecha en un sentido y en el otro son espejos
            var cw = EngranajesSprites.Arrow(41f, 4f, true);
            var ccw = EngranajesSprites.Arrow(41f, 4f, false);
            int mirrored = 0, samples = 0;
            for (float x = -30f; x <= 30f; x += 1.5f)
                for (float y = 0f; y <= 44f; y += 1.5f)
                {
                    samples++;
                    if (Mathf.Abs(AlphaAt(cw, x, y) - AlphaAt(ccw, -x, y)) > 0.3f) mirrored++;
                }
            Assert.Less(mirrored, samples * 0.03f, "las dos flechas de giro son espejo (" + mirrored + " de " + samples + " puntos distintos; solo cambia el redondeo del borde)");
            Assert.AreNotEqual(AlphaAt(cw, 25f, 30f) + AlphaAt(cw, -25f, 30f), 0f, "la flecha se dibuja arriba");
        }

        // Los sonidos de Engranajes en «Madera cálida» (Tarea 139) los prueba EngranajesSoundTests: los diez pasos, la compuerta, el despegue, lo que pide antes de la cuenta y el toque del kit de «Arrancar».
    }

    public class EngranajesVisibilityTests
    {
        [Test]
        public void EveryPieceTheGameDraws_IsOnAndOpaque_AndEveryMachineFitsTheViews()
        {
            // lección de Punta (3-oct): una pieza creada APAGADA, o sin imagen, deja un cuadrado blanco o nada sobre el cielo oscuro. Además se arman 12 máquinas de cada nivel para ver que caben en los pools.
            var go = new GameObject("EngranajesAudit");
            try
            {
                var controller = go.AddComponent<EngranajesGameController>();
                // solo se arma la interfaz (Awake también estiliza el panel de resultado, y eso usa Destroy, que en modo edición no se puede)
                var build = typeof(EngranajesGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                var problems = controller.AuditVisibility();
                Assert.IsEmpty(problems, "Piezas que no se verían: " + string.Join(" | ", problems));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void ATouchWithAllTheKeysUsed_ChangesNothing_ShowsTheCoinAndTheNotice_AndAnUndoStillWorks()
        {
            var go = new GameObject("EngranajesBlocked");
            try
            {
                var controller = go.AddComponent<EngranajesGameController>();
                var build = typeof(EngranajesGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                var problems = controller.AuditBlockedTouch();
                Assert.IsEmpty(problems, string.Join(" | ", problems));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void TheWorkshopTable_IsACreamSlabUnderEverything_TheWindowKeepsThePanelsPlace_AndTheSayIsCream()
        {
            var go = new GameObject("EngranajesTable");
            try
            {
                var controller = go.AddComponent<EngranajesGameController>();
                var build = typeof(EngranajesGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                var problems = controller.AuditTable();
                Assert.IsEmpty(problems, string.Join(" | ", problems));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void TheVolume_EveryGearHasItsCantoAndItsSoftShadowBelowAllTheGears_AndTheCantoNeverTurnsWithTheGear()
        {
            var go = new GameObject("EngranajesVolume");
            try
            {
                var controller = go.AddComponent<EngranajesGameController>();
                var build = typeof(EngranajesGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                var problems = controller.AuditVolume();
                Assert.IsEmpty(problems, string.Join(" | ", problems));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void TheCantoEffect_RepeatsTheMeshDownwardOnTheScreen_DeepestFirst_AndTheFaceLast()
        {
            var go = new GameObject("Canto", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            try
            {
                var effect = PlasticCanto.Add(go.GetComponent<UnityEngine.UI.Image>(), 12f, new Color(0.1f, 0.1f, 0.3f), new Color(0.3f, 0.3f, 0.6f), 3);
                var vh = new UnityEngine.UI.VertexHelper();
                vh.AddVert(new Vector3(0f, 0f, 0f), Color.white, Vector2.zero); vh.AddVert(new Vector3(0f, 10f, 0f), Color.white, Vector2.zero); vh.AddVert(new Vector3(10f, 10f, 0f), Color.white, Vector2.zero);
                vh.AddTriangle(0, 1, 2);
                effect.ModifyMesh(vh);
                var stream = new List<UIVertex>();
                vh.GetUIVertexStream(stream);
                Assert.AreEqual(3 * 4, stream.Count, "3 pasos de canto + la cara");
                Assert.AreEqual(-12f, stream[0].position.y, 0.001f, "la copia más honda, 12 unidades más abajo");
                Assert.AreEqual(-8f, stream[3].position.y, 0.001f);
                Assert.AreEqual(-4f, stream[6].position.y, 0.001f);
                Assert.AreEqual(0f, stream[9].position.y, 0.001f, "la cara, al final: va encima");
                Assert.Greater(stream[0].color.b, 0, "tiene color");
                Assert.Less(stream[0].color.r, stream[6].color.r, "lo hondo es más oscuro que lo que está junto a la cara");
                // girado 90°: el corrimiento local se gira al revés para que en la pantalla siga siendo hacia abajo
                go.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, 0f, 90f);
                var vh2 = new UnityEngine.UI.VertexHelper();
                vh2.AddVert(new Vector3(0f, 0f, 0f), Color.white, Vector2.zero); vh2.AddVert(new Vector3(0f, 10f, 0f), Color.white, Vector2.zero); vh2.AddVert(new Vector3(10f, 10f, 0f), Color.white, Vector2.zero);
                vh2.AddTriangle(0, 1, 2);
                effect.ModifyMesh(vh2);
                var s2 = new List<UIVertex>();
                vh2.GetUIVertexStream(s2);
                var local = s2[0].position - s2[9].position;
                var onScreen = Quaternion.Euler(0f, 0f, 90f) * local;
                Assert.AreEqual(0f, onScreen.x, 0.001f);
                Assert.AreEqual(-12f, onScreen.y, 0.001f, "en la pantalla el canto sigue hacia abajo aunque el gráfico esté girado");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void TheController_UsesOnlyTheGameClock_SoThePauseStopsEverything()
        {
            // la pausa detiene GameClock: nada del juego puede medir el tiempo con otro reloj ni esperar con WaitForSeconds (que seguiría corriendo)
            foreach (var file in new[] { "EngranajesGameController.cs", "EngranajesGameController.Build.cs", "EngranajesGameController.Scene.cs" })
            {
                string path = Path.Combine(Application.dataPath, "Scripts", "Games", "Engranajes", file);
                Assert.IsTrue(File.Exists(path), path);
                string text = File.ReadAllText(path);
                foreach (var banned in new[] { "Time.time", "Time.deltaTime", "Time.unscaledTime", "Time.unscaledDeltaTime", "WaitForSeconds", "WaitForSecondsRealtime", "Time.realtimeSinceStartup" })
                    Assert.IsFalse(text.Contains(banned), file + ": usa «" + banned + "» en vez de GameClock");
            }
        }

        [Test]
        public void ThePauseAndHowTo_AreWired_AndTheGameIsRegistered()
        {
            string root = Path.Combine(Application.dataPath, "Scripts");
            string main = File.ReadAllText(Path.Combine(root, "Games", "Engranajes", "EngranajesGameController.cs"));
            StringAssert.Contains("override bool HowToReady", main);
            StringAssert.Contains("override void HowToSuspend", main);
            StringAssert.Contains("override void HowToResume", main);
            StringAssert.Contains("HowToClock.Shift(_endsAt", main, "«Cómo se juega» y las tarjetas no gastan tiempo del Reto");
            string entry = File.ReadAllText(Path.Combine(root, "Bootstrap", "GameEntryPoint.cs"));
            StringAssert.Contains("EngranajesGameController.GameId", entry);
        }
    }
}
