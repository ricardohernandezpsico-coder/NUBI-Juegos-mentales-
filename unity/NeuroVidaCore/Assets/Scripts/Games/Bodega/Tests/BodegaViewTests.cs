using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Bodega;

namespace NeuroVida.Games.Bodega.Tests
{
    /// <summary>El «flow» (resortes amortiguados con dt real, nunca velocidad fija) y las reglas de movimiento de «Bodega de carga».</summary>
    public class BodegaMotionTests
    {
        /// <summary>Simula un resorte a <paramref name="fps"/> cuadros por segundo durante <paramref name="seconds"/> y devuelve el recorrido (x, v) de cada cuadro.</summary>
        private static List<(float x, float v)> Run(float target, float freq, float zeta, float fps, float seconds, float x0 = 0f)
        {
            var path = new List<(float, float)>();
            float x = x0, v = 0f, dt = 1f / fps;
            for (float t = 0f; t < seconds; t += dt)
            {
                BodegaMotion.Spring(ref x, ref v, target, dt, freq, zeta);
                path.Add((x, v));
            }
            return path;
        }

        [Test]
        public void TheRobotSpring_StartsFromRest_SpeedsUpAndSlowsDown_NeverAtAFixedSpeed()
        {
            var path = Run(100f, BodegaMotion.RobotFreq, BodegaMotion.RobotZeta, 60f, 3f);
            Assert.Less(Math.Abs(path[0].x), 3f, "arranca suave, no de golpe");
            float peak = 0f;
            int peakAt = 0;
            for (int i = 0; i < path.Count; i++) if (path[i].v > peak) { peak = path[i].v; peakAt = i; }
            Assert.Greater(peakAt, 5, "primero acelera");
            Assert.Less(peakAt, path.Count - 20, "después frena");
            Assert.Less(path[0].v, peak * 0.4f, "al principio va lento");
            Assert.Greater(path[5].v, path[0].v, "y va tomando velocidad");
            Assert.Less(Math.Abs(path[path.Count - 1].x - 100f), 1f, "llega a su objetivo");
            // la velocidad cambia de cuadro a cuadro de forma continua (sin saltos)
            for (int i = 1; i < path.Count; i++) Assert.Less(Math.Abs(path[i].v - path[i - 1].v), peak * 0.4f, "sin saltos de velocidad en el cuadro " + i);
        }

        [Test]
        public void TheDoors_OpenAndOvershootJustABit_AndCloseFirmWithoutBouncing()
        {
            var open = Run(1f, BodegaMotion.DoorOpenFreq, BodegaMotion.DoorOpenZeta, 60f, 2f);
            float max = 0f;
            foreach (var p in open) max = Mathf.Max(max, p.x);
            Assert.Greater(max, 1.005f, "abre y se pasa apenas");
            Assert.Less(max, 1.2f, "pero poco");
            Assert.Less(Math.Abs(open[open.Count - 1].x - 1f), 0.01f);
            var close = Run(0f, BodegaMotion.DoorOpenFreq, BodegaMotion.DoorCloseZeta, 60f, 2f, 1f);
            float min = 1f;
            foreach (var p in close) min = Mathf.Min(min, p.x);
            Assert.Greater(min, -0.05f, "cierra firme: casi no rebota");
            Assert.Less(Math.Abs(close[close.Count - 1].x), 0.01f);
        }

        [Test]
        public void TheAntennaAndTheHangingCrate_LagAndBounce()
        {
            var ant = Run(1f, BodegaMotion.AntennaFreq, BodegaMotion.AntennaZeta, 60f, 3f);
            int crossings = 0;
            for (int i = 1; i < ant.Count; i++) if ((ant[i - 1].x - 1f) * (ant[i].x - 1f) < 0f) crossings++;
            Assert.GreaterOrEqual(crossings, 3, "la antena rebota varias veces (amortiguación 0,3)");
            var sway = Run(1f, BodegaMotion.SwayFreq, BodegaMotion.SwayZeta, 60f, 4f);
            crossings = 0;
            for (int i = 1; i < sway.Count; i++) if ((sway[i - 1].x - 1f) * (sway[i].x - 1f) < 0f) crossings++;
            Assert.GreaterOrEqual(crossings, 2, "la caja colgando se mece");
        }

        [Test]
        public void TheSpring_GivesTheSameResultAtAnyFrameRate_AndIgnoresHugeFrames()
        {
            var a = Run(50f, 1.5f, 0.8f, 30f, 2f);
            var b = Run(50f, 1.5f, 0.8f, 120f, 2f);
            Assert.Less(Math.Abs(a[a.Count - 1].x - b[b.Count - 1].x), 1.5f, "30 y 120 cuadros por segundo llegan casi igual");
            // un cuadro enorme (el teléfono se trabó) se acota a 0,05 s y nada explota
            float x = 0f, v = 0f;
            BodegaMotion.Spring(ref x, ref v, 100f, 5f, 3.2f, 0.3f);
            Assert.IsFalse(float.IsNaN(x) || float.IsInfinity(x));
            Assert.Less(Math.Abs(x), 150f);
            Assert.AreEqual(0.05f, BodegaMotion.ClampDt(5f));
            Assert.AreEqual(0f, BodegaMotion.ClampDt(-1f));
            BodegaMotion.Spring(ref x, ref v, 100f, 0f, 3.2f, 0.3f);
        }

        [Test]
        public void TheEasings_GoFromZeroToOne_TheSpinHasAKickBackAndSettlesSoftly_AndTheBobDoesNotRepeat()
        {
            Assert.AreEqual(0f, BodegaMotion.EaseSpin(0f), 1e-4f);
            Assert.AreEqual(1f, BodegaMotion.EaseSpin(1f), 1e-3f);
            Assert.Less(BodegaMotion.EaseSpin(0.06f), -0.02f, "un pequeño impulso hacia atrás");
            Assert.GreaterOrEqual(BodegaMotion.EaseSpin(0.06f), -0.036f, "de 3,5 %");
            Assert.AreEqual(0f, BodegaMotion.EaseSpin(0.12f), 1e-3f);
            float max = 0f;
            for (float t = 0.12f; t <= 1f; t += 0.01f) max = Mathf.Max(max, BodegaMotion.EaseSpin(t));
            Assert.Greater(max, 1.0f, "se asienta suave: se pasa un poco y vuelve");
            Assert.Less(max, 1.1f);
            Assert.AreEqual(0f, BodegaMotion.EaseBack(0f), 1e-4f);
            Assert.AreEqual(1f, BodegaMotion.EaseBack(1f), 1e-4f);
            float pop = 0f;
            for (float t = 0f; t <= 1f; t += 0.01f) pop = Mathf.Max(pop, BodegaMotion.EaseBack(t));
            Assert.That(pop, Is.InRange(1.05f, 1.15f), "el objeto aparece con un rebote de ~1,08");
            Assert.AreEqual(0f, BodegaMotion.Bob(1.234f, false), "con «quitar animaciones» no flota");
            var distinct = new HashSet<float>();
            for (float t = 0f; t < 6f; t += 0.37f) distinct.Add(Mathf.Round(BodegaMotion.Bob(t, true) * 1000f));
            Assert.Greater(distinct.Count, 12, "las dos ondas no coinciden: nunca se ve mecánico");
            Assert.AreEqual(0f, BodegaMotion.AngleDelta(1f, 1f), 1e-5f);
            Assert.AreEqual(-Mathf.PI / 2f, BodegaMotion.AngleDelta(0f, 3f * Mathf.PI / 2f), 1e-4f, "por el lado corto");
            Assert.AreEqual(1.5f, BodegaMotion.SpinSeconds);
            Assert.AreEqual(0.22f, BodegaMotion.MaxLean);
        }
    }

    /// <summary>La disposición de «Bodega de carga» en cada altura de pantalla y el arte, los sonidos y la vista.</summary>
    public class BodegaLayoutTests
    {
        private static readonly float[] Heights = { 600f, 640f, 700f, 740f, 780f, 860f, 960f };

        [Test]
        public void EveryScreenHeight_FitsEverythingInOrder_WithoutOverlap()
        {
            foreach (float h in Heights)
            {
                var m = BodegaLayout.Compute(h);
                string where = "alto " + h;
                Assert.GreaterOrEqual(m.CardTop, BodegaLayout.HudDp, where + ": la tarjeta va bajo el marcador");
                float boardTop = m.BoardCenterY - BodegaLayout.HullR * m.Scale, boardBottom = m.BoardCenterY + BodegaLayout.HullR * m.Scale;
                Assert.GreaterOrEqual(boardTop, m.CardTop + BodegaLayout.CardH - 0.01f, where + ": la bodega va bajo la tarjeta");
                Assert.LessOrEqual(boardBottom, m.TrayLabelY - BodegaLayout.TrayLabelH / 2f + 0.01f, where + ": el carro va bajo la bodega");
                Assert.LessOrEqual(m.TrayCenterY + BodegaLayout.TrayCircle / 2f, m.ToastTop + 0.01f, where + ": el aviso va bajo el carro");
                Assert.LessOrEqual(m.Bottom, h + 0.01f, where + ": todo cabe en la pantalla");
                Assert.GreaterOrEqual(m.Scale, BodegaLayout.MinScale - 0.001f, where);
                Assert.LessOrEqual(m.Scale, BodegaLayout.MaxScale + 0.001f, where);
                // las escotillas se tocan en un círculo de al menos 48 dp
                Assert.GreaterOrEqual(2f * BodegaLayout.TouchR * m.Scale, 48f, where + ": escotillas tocables de al menos 48 dp");
                // la bodega entra en el ancho
                Assert.LessOrEqual(2f * BodegaLayout.HullR * m.Scale, BodegaLayout.W - 8f, where);
            }
        }

        [Test]
        public void ABoardPointAndItsScreenSpot_ConvertBackAndForth()
        {
            foreach (float h in Heights)
            {
                var m = BodegaLayout.Compute(h);
                foreach (var p in new[] { new Vector2(180f, 335f), new Vector2(60f, 220f), new Vector2(300f, 460f) })
                {
                    BodegaLayout.BoardToLogical(m, p.x, p.y, out float lx, out float ly);
                    BodegaLayout.LogicalToBoard(m, lx, ly, out float bx, out float by);
                    Assert.AreEqual(p.x, bx, 1e-3f, "alto " + h);
                    Assert.AreEqual(p.y, by, 1e-3f, "alto " + h);
                }
            }
        }

        [Test]
        public void TheHatchesOfTheRing_DoNotOverlapEachOther_AtEveryCount()
        {
            foreach (int hatches in new[] { 6, 8, 10 })
            {
                int places = hatches + 1;
                float chord = 2f * BodegaLayout.Ring * Mathf.Sin(Mathf.PI / places);
                Assert.Greater(chord, 2f * (BodegaLayout.HatchR + BodegaLayout.FrameW) - 0.5f, hatches + " escotillas: los marcos no se pisan (cuerda " + chord + ")");
                // el ring de la esclusa (con su aro de sello) también cabe
                Assert.GreaterOrEqual(chord, (BodegaLayout.HatchR + 10f + 1.5f) + (BodegaLayout.HatchR + BodegaLayout.FrameW) - 1f, hatches + " escotillas: el aro de sello de la esclusa no pisa a las vecinas");
            }
            BodegaLayout.RingPoint(0, 9, 0f, BodegaLayout.Ring, out float x, out float y, out _);
            Assert.AreEqual(BodegaLayout.CenterX, x, 1e-3f, "la posición 0 (la esclusa) queda arriba");
            Assert.Less(y, BodegaLayout.CenterY);
        }

        [Test]
        public void TheTray_FitsTheLargestOrderInsideTheScreenWidth()
        {
            int n = 7;
            Assert.GreaterOrEqual(BodegaLayout.TrayX(0, n) - BodegaLayout.TrayCircle / 2f, 8f);
            Assert.LessOrEqual(BodegaLayout.TrayX(n - 1, n) + BodegaLayout.TrayCircle / 2f, BodegaLayout.W - 8f);
            for (int k = 1; k < n; k++) Assert.Greater(BodegaLayout.TrayX(k, n) - BodegaLayout.TrayX(k - 1, n), BodegaLayout.TrayCircle, "los círculos no se pisan");
        }

        [Test]
        public void TheIntroCards_FitInTheScreen_EvenIfEachLineBreaksInTwoAt14dpOrMore()
        {
            float widthPx = (BodegaLayout.W - 36f - 28f) * 3f;
            foreach (Intro intro in Enum.GetValues(typeof(Intro)))
            {
                if (intro == Intro.None) continue;
                int lines = 0;
                foreach (var line in BodegaContract.IntroText(intro))
                {
                    var measure = NeuroVida.Games.Shared.CoachText.Measure(line, widthPx, 48);
                    Assert.LessOrEqual(measure.Lines, 2, intro + ": «" + line + "» ocupa " + measure.Lines + " líneas");
                    lines += measure.Lines;
                }
                Assert.LessOrEqual(lines, 6, intro + ": la tarjeta no crece de más");
            }
        }
    }

    public class BodegaArtTests
    {
        private static float AlphaAt(Sprite s, float xDp, float yDp)
        {
            var tex = s.texture;
            float ppd = s.pixelsPerUnit;
            int px = Mathf.Clamp(Mathf.RoundToInt(tex.width / 2f + xDp * ppd), 0, tex.width - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(tex.height / 2f + yDp * ppd), 0, tex.height - 1);
            return tex.GetPixel(px, py).a;
        }

        [Test]
        public void EverySprite_IsBaked_AndWithoutAnEmptyTexture()
        {
            var bake = BodegaSprites.Prewarm();
            while (bake.MoveNext()) { }
            var sprites = new List<Sprite>
            {
                BodegaSprites.Hull(), BodegaSprites.Airlock(), BodegaSprites.SealRing(), BodegaSprites.HatchFrame(), BodegaSprites.HatchInterior(0), BodegaSprites.HatchInterior(1),
                BodegaSprites.HatchInterior(2), BodegaSprites.HatchRim(), BodegaSprites.DoorLeaf(), BodegaSprites.RobotBody(), BodegaSprites.Crate(), BodegaSprites.Check()
            };
            for (int i = 0; i < BodegaContract.ObjectCount; i++) sprites.Add(BodegaSprites.Object(i));
            foreach (var sprite in sprites)
            {
                Assert.IsNotNull(sprite);
                float filled = 0f;
                var px = sprite.texture.GetPixels32();
                foreach (var c in px) if (c.a > 20) filled++;
                Assert.Greater(filled / px.Length, 0.04f, "el sprite " + sprite.name + " no puede estar vacío");
            }
        }

        [Test]
        public void TheDoorLeaf_FillsOnlyTheHalfNextToItsJoint_AndTheTwoLeavesTogetherCoverTheHatch()
        {
            var leaf = BodegaSprites.DoorLeaf();
            Assert.Greater(AlphaAt(leaf, 0f, 12f), 0.9f, "la hoja ocupa la mitad de arriba");
            Assert.Less(AlphaAt(leaf, 0f, -12f), 0.05f, "y no la de abajo (la otra hoja es la misma girada 180°)");
            Assert.Less(AlphaAt(leaf, 0f, 29f), 0.05f, "no pasa del radio de la escotilla");
            Assert.Less(AlphaAt(leaf, 40f, 0f), 0.05f);
        }

        [Test]
        public void TheSealRing_IsDotted_NotRadialRays_AndTheAirlockHasAGoldFrame()
        {
            var seal = BodegaSprites.SealRing();
            float radius = BodegaLayout.HatchR + 10f;
            int on = 0, off = 0;
            for (int i = 0; i < 360; i += 2)
            {
                float a = i * Mathf.Deg2Rad;
                if (AlphaAt(seal, Mathf.Cos(a) * radius, Mathf.Sin(a) * radius) > 0.4f) on++; else off++;
            }
            Assert.Greater(on, 40, "el aro tiene marcas");
            Assert.Greater(off, 40, "con huecos entre ellas (punteado)");
            Assert.Less(AlphaAt(seal, 0f, 0f), 0.05f, "el centro del aro está vacío: sin rayitas radiales (parecían un sol)");
            Assert.Less(AlphaAt(seal, radius * 0.6f, 0f), 0.05f);
            var air = BodegaSprites.Airlock();
            Assert.Greater(AlphaAt(air, BodegaSprites.WindowR - 1f, 0f), 0.9f, "el marco dorado de la esclusa");
            var px = air.texture.GetPixels32();
            int gold = 0;
            foreach (var c in px) if (c.a > 200 && c.r > 220 && c.g > 170 && c.g < 230 && c.b < 120) gold++;
            Assert.Greater(gold, 400, "borde dorado");
        }

        [Test]
        public void TheSharedSilhouettes_ComeFromBitacora_ButTheCupStarfishAndAnchorStayOut()
        {
            // 10 de los 12 objetos usan la silueta de Bitácora (sin tocar ese juego); manzana y taza son nuevas
            string src = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Bodega", "BodegaSprites.cs"));
            StringAssert.Contains("BitacoraSprites.RenderFind", src);
            var used = new HashSet<int> { 0, 1, 6, 12, 15, 9, 11, 4, 2, 8 };
            foreach (int banned in new[] { 10, 13, 14, 3 })       // copa, ancla, flor (estrella de mar reemplazada) y concha
                Assert.IsFalse(used.Contains(banned));
            Assert.AreEqual(12, BodegaContract.ObjectCount);
        }

        [Test]
        public void EverySound_IsSynthesized_WithAudibleButNotLoudSamples()
        {
            var warm = BodegaSounds.Prewarm();
            while (warm.MoveNext()) { }
            var clips = new List<AudioClip>
            {
                BodegaSounds.Open(), BodegaSounds.Close(), BodegaSounds.Thud(), BodegaSounds.Hum(), BodegaSounds.Beam(), BodegaSounds.Clank(), BodegaSounds.Whoosh(), BodegaSounds.Arrive(),
                BodegaSounds.Ratchet(), BodegaSounds.Ping(), BodegaSounds.Perfect(), BodegaSounds.Chime(), BodegaSounds.Finale(), BodegaSounds.Spark(1), BodegaSounds.Spark(8)
            };
            for (int h = 0; h < 10; h++) clips.Add(BodegaSounds.Note(h));
            foreach (var clip in clips)
            {
                Assert.IsNotNull(clip);
                var data = new float[clip.samples];
                clip.GetData(data, 0);
                float peak = 0f;
                foreach (float f in data) peak = Mathf.Max(peak, Mathf.Abs(f));
                Assert.Greater(peak, 0.03f, clip.name + ": se oye");
                Assert.LessOrEqual(peak, 0.5001f, clip.name + ": no pasa de 0,5");
            }
            Assert.AreNotSame(BodegaSounds.Note(0), BodegaSounds.Note(1), "cada escotilla tiene su nota");
            Assert.AreNotSame(BodegaSounds.Open(), BodegaSounds.Close(), "abrir (siseo agudo) y cerrar (golpe grave) son dos sonidos");
            Assert.AreNotSame(BodegaSounds.Thud(), BodegaSounds.Perfect(), "el error y el pedido perfecto no suenan igual");
            Assert.AreEqual(BodegaMotion.SpinSeconds + 0.1f, BodegaSounds.Ratchet().length, 0.02f, "la matraca dura lo que el giro");
        }
    }

    public class BodegaVisibilityTests
    {
        [Test]
        public void EveryPieceTheGameDraws_IsOnAndOpaque_AndEveryOrderFitsTheViews()
        {
            // lección de Punta (3-oct): una pieza creada APAGADA, o sin imagen, deja un cuadrado blanco o nada sobre el cielo oscuro. Además se arma un pedido de cada etapa para ver que cabe en los pools.
            var go = new GameObject("BodegaAudit");
            try
            {
                var controller = go.AddComponent<BodegaGameController>();
                // solo se arma la interfaz (Awake también estiliza el panel de resultado, y eso usa Destroy, que en modo edición no se puede)
                var build = typeof(BodegaGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                var problems = controller.AuditVisibility();
                Assert.IsEmpty(problems, "Piezas que no se verían: " + string.Join(" | ", problems));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static string Read(string file) => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Bodega", file));

        [Test]
        public void TheController_UsesOnlyTheGameClock_SoThePauseStopsEverything()
        {
            foreach (var file in new[] { "BodegaGameController.cs", "BodegaGameController.Build.cs", "BodegaGameController.Scene.cs", "BodegaMotion.cs" })
            {
                string text = Read(file);
                foreach (var banned in new[] { "Time.time", "Time.deltaTime", "Time.unscaledTime", "Time.unscaledDeltaTime", "WaitForSeconds", "WaitForSecondsRealtime", "Time.realtimeSinceStartup" })
                    Assert.IsFalse(text.Contains(banned), file + ": usa «" + banned + "» en vez de GameClock");
            }
        }

        [Test]
        public void TheMotionIsNeverAFixedSpeed_EverythingFollowsASpring_AndTheRobotNeverReactsToThePerformance()
        {
            string scene = Read("BodegaGameController.Scene.cs");
            StringAssert.Contains("BodegaMotion.Spring(ref r.X", scene);
            StringAssert.Contains("BodegaMotion.Spring(ref r.Ang", scene);
            StringAssert.Contains("BodegaMotion.Spring(ref r.Ant", scene);
            StringAssert.Contains("BodegaMotion.Spring(ref r.Sw", scene);
            StringAssert.Contains("BodegaMotion.Spring(ref v.Open", scene);
            // regla de patentes (Akili): el robot no pone caras felices o tristes, no salta ni niega con la cabeza según cómo se juega
            string all = Read("BodegaGameController.cs") + scene + Read("BodegaGameController.Build.cs");
            foreach (var banned in new[] { "mood", "Mood", "happy", "Happy", "emotion", "Emotion", "feliz", "triste" })
                StringAssert.DoesNotContain(banned, all.Replace("ShakeAt", ""), "el robot no reacciona al desempeño: «" + banned + "»");
        }

        [Test]
        public void ThePauseAndHowTo_AreWired_AndTheGameIsRegistered()
        {
            string main = Read("BodegaGameController.cs");
            StringAssert.Contains("override bool HowToReady", main);
            StringAssert.Contains("override void HowToSuspend", main);
            StringAssert.Contains("override void HowToResume", main);
            StringAssert.Contains("HowToClock.Shift(_endsAt", main, "«Cómo se juega» y las tarjetas no gastan tiempo del Reto");
            StringAssert.Contains("PollTutorialSkip", main);
            StringAssert.Contains("RunTutorialIfNeeded", main);
            StringAssert.Contains("BuildTutorial", main);
            string entry = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Bootstrap", "GameEntryPoint.cs"));
            StringAssert.Contains("BodegaGameController.GameId", entry);
        }

        [Test]
        public void TheGuidedRound_NeverTouchesTheScoreTheDdaTheRecordOrTheCounts()
        {
            string main = Read("BodegaGameController.cs");
            int a = main.IndexOf("// <guided>"), b = main.IndexOf("// </guided>");
            Assert.Greater(a, 0);
            Assert.Greater(b, a);
            string region = main.Substring(a, b - a);
            foreach (var token in new[] { "_dda", "_points", "_tally", "_ordersDone", "_errs", "_bestRecord", "Register(", "RecordFound(", "RecordMiss(", "PlayerPrefs", "IntroSeen(", "MarkIntroSeen(" })
                StringAssert.DoesNotContain(token, region, "la ronda guiada no puede tocar «" + token + "»");
            StringAssert.Contains("GuidedScript", region);
            // el conteo se salta mientras se guía
            StringAssert.Contains("if (!_guided) RecordFound", main);
            StringAssert.Contains("if (_guided) return;", main);
        }
    }
}
