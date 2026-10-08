using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Parejas.Tests
{
    /// <summary>El «flow» de «Constelaciones»: el volteo con resorte (abrir con un poco de rebote, cerrar más rápido y sin rebote), con el dt real y sin saltos.</summary>
    public class ConstelacionMotionTests
    {
        private static List<(float x, float v)> Run(float target, bool opening, float fps, float seconds, float x0 = 0f)
        {
            var path = new List<(float, float)>();
            float x = x0, v = 0f, dt = 1f / fps;
            for (float t = 0f; t < seconds; t += dt)
            {
                ConstelacionMotion.Flip(ref x, ref v, target, dt);
                path.Add((x, v));
            }
            return path;
        }

        [Test]
        public void OpeningALight_OvershootsJustABit_AndSettlesAtOne()
        {
            var open = Run(1f, true, 60f, 2f);
            float max = open.Max(p => p.x);
            Assert.Greater(max, 1.01f, "abre con un pequeño rebote");
            Assert.Less(max, 1.2f, "pero poco");
            Assert.Less(Math.Abs(open.Last().x - 1f), 0.01f);
        }

        [Test]
        public void ClosingALight_IsFasterThanOpening_AndDoesNotBounce()
        {
            var close = Run(0f, false, 60f, 2f, 1f);
            Assert.Greater(close.Min(p => p.x), -0.05f, "cierra sin rebote");
            Assert.Less(Math.Abs(close.Last().x), 0.01f);
            int Frames(IEnumerable<(float x, float v)> path, Func<float, bool> done) { int i = 0; foreach (var p in path) { if (done(p.x)) return i; i++; } return 999; }
            int closeAt = Frames(close, x => x < 0.1f);
            int openAt = Frames(Run(1f, true, 60f, 2f), x => x > 0.9f);
            Assert.Less(closeAt, openAt + 1, "la salida dura menos que la entrada");
            Assert.AreEqual(2.6f, ConstelacionMotion.OpenFreq);
            Assert.AreEqual(0.72f, ConstelacionMotion.OpenZeta);
            Assert.AreEqual(3.6f, ConstelacionMotion.CloseFreq);
            Assert.AreEqual(0.95f, ConstelacionMotion.CloseZeta);
        }

        [Test]
        public void TheSpring_GivesTheSameResultAtAnyFrameRate_AndIgnoresHugeFrames()
        {
            var a = Run(1f, true, 30f, 2f);
            var b = Run(1f, true, 120f, 2f);
            Assert.Less(Math.Abs(a.Last().x - b.Last().x), 0.05f, "30 y 120 cuadros por segundo llegan casi igual");
            float x = 0f, v = 0f;
            ConstelacionMotion.Spring(ref x, ref v, 100f, 5f, 3.2f, 0.3f);      // un cuadro enorme (el teléfono se trabó) se acota a 0,05 s
            Assert.IsFalse(float.IsNaN(x) || float.IsInfinity(x));
            Assert.AreEqual(0.05f, ConstelacionMotion.ClampDt(5f));
            Assert.AreEqual(0f, ConstelacionMotion.ClampDt(-1f));
        }

        [Test]
        public void TheFlipLooksNarrowInTheMiddle_AndLiftsAbit_AndTheAppearancePopsToAbout108()
        {
            Assert.AreEqual(1f, ConstelacionMotion.FlipWidth(0f), 1e-4f);
            Assert.AreEqual(0.02f, ConstelacionMotion.FlipWidth(0.5f), 1e-3f, "de canto casi no se ve");
            Assert.AreEqual(1f, ConstelacionMotion.FlipWidth(1f), 1e-4f);
            Assert.AreEqual(1.07f, ConstelacionMotion.FlipLift(0.5f), 1e-3f, "se levanta hasta 7 %");
            Assert.AreEqual(1f, ConstelacionMotion.FlipLift(0f), 1e-4f);
            float pop = 0f;
            for (float t = 0f; t <= 1f; t += 0.01f) pop = Mathf.Max(pop, ConstelacionMotion.EaseBack(t));
            Assert.That(pop, Is.InRange(1.05f, 1.15f));
            Assert.AreEqual(0f, ConstelacionMotion.EaseBack(0f), 1e-4f);
            Assert.AreEqual(1f, ConstelacionMotion.EaseBack(1f), 1e-4f);
            Assert.AreEqual(0.94f, ConstelacionMotion.PressScale);
            Assert.AreEqual(0.14f, ConstelacionMotion.PressSeconds);
            Assert.AreEqual(0.38f, ConstelacionMotion.TraceSeconds);
        }
    }

    /// <summary>El arte y el sonido de «Constelaciones»: todo se hornea, las siluetas se distinguen y los gemelos cambian en un detalle grande.</summary>
    public class ConstelacionArtTests
    {
        private static int Filled(Sprite s)
        {
            int n = 0;
            foreach (var c in s.texture.GetPixels32()) if (c.a > 20) n++;
            return n;
        }

        private static float AlphaAt(Sprite s, float xDp, float yDp)
        {
            var tex = s.texture;
            float ppd = s.pixelsPerUnit;
            int px = Mathf.Clamp(Mathf.RoundToInt(tex.width / 2f + xDp * ppd), 0, tex.width - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(tex.height / 2f + yDp * ppd), 0, tex.height - 1);
            return tex.GetPixel(px, py).a;
        }

        /// <summary>Cuántos píxeles distintos hay entre dos sprites del mismo tamaño.</summary>
        private static int Differences(Sprite a, Sprite b)
        {
            var pa = a.texture.GetPixels32();
            var pb = b.texture.GetPixels32();
            int d = 0;
            for (int i = 0; i < pa.Length; i++)
                if (Mathf.Abs(pa[i].a - pb[i].a) > 80 || (pa[i].a > 200 && pb[i].a > 200 && (Mathf.Abs(pa[i].r - pb[i].r) + Mathf.Abs(pa[i].g - pb[i].g) + Mathf.Abs(pa[i].b - pb[i].b)) > 120)) d++;
            return d;
        }

        [Test]
        public void EverySpriteIsBaked_TwelveObjectsFourTwinsTheLightsAndTheRings_NoneEmpty()
        {
            var bake = ConstelacionSprites.Prewarm();
            while (bake.MoveNext()) { }
            var all = new List<Sprite> { ConstelacionSprites.Dormant(), ConstelacionSprites.Open(), ConstelacionSprites.RingMemory(), ConstelacionSprites.RingNew() };
            for (int k = 0; k < 12; k++)
            {
                all.Add(ConstelacionSprites.Object(k, 0));
                if (ConstelacionContract.HasTwin(k)) all.Add(ConstelacionSprites.Object(k, 1));
            }
            Assert.AreEqual(4 + 12 + 4, all.Count);
            foreach (var s in all)
            {
                Assert.IsNotNull(s);
                Assert.Greater(Filled(s), 120, "el sprite " + s.name + " no puede estar vacío");
            }
            Assert.AreSame(ConstelacionSprites.Object(0, 0), ConstelacionSprites.Object(0, 0), "se hornea una sola vez");
            Assert.AreSame(ConstelacionSprites.Object(2, 0), ConstelacionSprites.Object(2, 1), "un objeto sin gemelo no cambia con la variante");
        }

        [Test]
        public void EveryObjectFitsInsideItsLight_AndHasTheLargeSilhouetteOfAnObject()
        {
            // el dibujo ocupa ±22 unidades; mostrado a R × 1,32 / 40 por unidad cabe adentro de la luz de radio R
            for (int k = 0; k < 12; k++)
            {
                var s = ConstelacionSprites.Object(k, 0);
                var px = s.texture.GetPixels32();
                int n = s.texture.width;
                int minX = n, maxX = 0, minY = n, maxY = 0;
                for (int i = 0; i < px.Length; i++)
                    if (px[i].a > 40) { int x = i % n, y = i / n; minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
                float w = (maxX - minX) / s.pixelsPerUnit, h = (maxY - minY) / s.pixelsPerUnit;
                Assert.Greater(Math.Max(w, h), 24f, ConstelacionContract.ObjectNames[k] + ": se ve (silueta grande)");
                Assert.LessOrEqual(Math.Max(w, h), 54f, ConstelacionContract.ObjectNames[k] + ": cabe en la luz (a R × 1,32 / 40 por unidad, 54 unidades ocupan el 90 % de su diámetro)");
            }
        }

        [Test]
        public void TheTwinsDifferFromTheirOriginalsByALargeShapeDetail_NeverByATone()
        {
            // planeta sin anillo, cohete con llama grande, ovni con patas, casco con antena
            foreach (int kind in ConstelacionContract.TwinKinds)
            {
                int d = Differences(ConstelacionSprites.Object(kind, 0), ConstelacionSprites.Object(kind, 1));
                Assert.Greater(d, 700, ConstelacionContract.ObjectNames[kind] + ": el gemelo se distingue por una forma grande (" + d + " píxeles distintos)");
            }
            // el planeta con anillo es más ancho que el planeta sin anillo (forma, no tono)
            var withRing = ConstelacionSprites.Object(0, 0);
            var without = ConstelacionSprites.Object(0, 1);
            Assert.Greater(AlphaAt(withRing, 19.5f, 6f) + AlphaAt(withRing, -19.5f, -6f), 0.8f, "el anillo sale a los costados (inclinado)");
            Assert.Less(AlphaAt(without, 19.5f, 6f) + AlphaAt(without, -19.5f, -6f), 0.1f, "sin anillo no hay nada ahí");
            // el cohete con llama grande llega más abajo
            Assert.Greater(AlphaAt(ConstelacionSprites.Object(1, 1), 0f, -24f), 0.8f, "la llama grande");
            Assert.Less(AlphaAt(ConstelacionSprites.Object(1, 0), 0f, -24f), 0.1f);
        }

        [Test]
        public void TheDoneRings_OneIsContinuousGold_AndTheOtherIsDashedCyan_SoItDoesNotDependOnColorAlone()
        {
            float radius = ConstelacionSprites.BakeR + 5f;
            int onMem = 0, offMem = 0, onNew = 0, offNew = 0;
            for (int i = 0; i < 360; i += 2)
            {
                float a = i * Mathf.Deg2Rad, x = Mathf.Cos(a) * radius, y = Mathf.Sin(a) * radius;
                if (AlphaAt(ConstelacionSprites.RingMemory(), x, y) > 0.4f) onMem++; else offMem++;
                if (AlphaAt(ConstelacionSprites.RingNew(), x, y) > 0.4f) onNew++; else offNew++;
            }
            Assert.AreEqual(0, offMem, "el aro de memoria es continuo");
            Assert.Greater(onNew, 50, "el aro de primera vista tiene trazos");
            Assert.Greater(offNew, 50, "con huecos entre ellos (punteado)");
            var gold = ConstelacionSprites.RingMemory().texture.GetPixel(ConstelacionSprites.RingMemory().texture.width / 2 + Mathf.RoundToInt(radius * 3f), ConstelacionSprites.RingMemory().texture.height / 2);
            Assert.Greater(gold.r, 0.9f);
            Assert.Greater(gold.g, 0.7f);
            Assert.Less(gold.b, 0.4f, "dorado");
        }

        [Test]
        public void TheDormantLightAndTheOpenLightLookDifferent_AndTheDormantOnesAllLookAlike()
        {
            Assert.Greater(Differences(ConstelacionSprites.Dormant(), ConstelacionSprites.Open()), 3000, "una luz dormida y una abierta se distinguen");
            // dormida: oscura con un núcleo claro al medio
            var dormant = ConstelacionSprites.Dormant().texture;
            var center = dormant.GetPixel(dormant.width / 2, dormant.height / 2);
            var edge = dormant.GetPixel(dormant.width / 2 + Mathf.RoundToInt(30f * 3f), dormant.height / 2);
            Assert.Greater(center.b, edge.b + 0.2f, "tiene un núcleo tibio");
            Assert.Less(edge.r + edge.g + edge.b, 1.2f, "la esfera es oscura");
            // abierta: clara
            var open = ConstelacionSprites.Open().texture;
            var oc = open.GetPixel(open.width / 2 - Mathf.RoundToInt(12f * 3f), open.height / 2 + Mathf.RoundToInt(12f * 3f));
            Assert.Greater(oc.r + oc.g + oc.b, 2.2f, "el disco abierto es claro");
        }

        [Test]
        public void NoSymbolOutsideTheRule_NoSunNoPointedStarNoCrescentNoCross()
        {
            string src = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Parejas", "ConstelacionSprites.cs"));
            foreach (var banned in new[] { "Star(", "Crescent(", "Sun(", "Cross(" })
                StringAssert.DoesNotContain(banned, src, "símbolo prohibido (docs/simbolos-neutros.md): " + banned);
            Assert.AreEqual(12, ConstelacionContract.ObjectKinds);
            foreach (var name in ConstelacionContract.ObjectNames) Assert.AreNotEqual("sol", name);
        }

        [Test]
        public void EverySound_IsSynthesized_AudibleButNotLoud()
        {
            var warm = ConstelacionSounds.Prewarm();
            while (warm.MoveNext()) { }
            var clips = new List<AudioClip>
            {
                ConstelacionSounds.FlipUp(), ConstelacionSounds.FlipDown(), ConstelacionSounds.Soft(), ConstelacionSounds.Hum(), ConstelacionSounds.Chime(), ConstelacionSounds.BoardEnd(), ConstelacionSounds.Finale(),
                ConstelacionSounds.Spark(1), ConstelacionSounds.Spark(8)
            };
            for (int g = 0; g < 11; g++) { clips.Add(ConstelacionSounds.Note(g)); clips.Add(ConstelacionSounds.FoundMemory(g)); clips.Add(ConstelacionSounds.FoundNew(g)); }
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
            Assert.AreNotSame(ConstelacionSounds.Note(0), ConstelacionSounds.Note(1), "cada grupo tiene su nota");
            Assert.AreNotSame(ConstelacionSounds.FoundMemory(0), ConstelacionSounds.FoundNew(0), "de memoria suena distinto que a la primera vista");
            Assert.AreNotSame(ConstelacionSounds.Soft(), ConstelacionSounds.BoardEnd(), "el «no son iguales» y el cielo completo no suenan igual");
            Assert.AreNotSame(ConstelacionSounds.Spark(1), ConstelacionSounds.Spark(5), "el destello sube con la racha");
        }
    }

    /// <summary>La vista y la pantalla de «Constelaciones»: todo lo que se dibuja se ve, los pools alcanzan, el reloj es el del juego, y las reglas de patentes.</summary>
    public class ConstelacionVisibilityTests
    {
        [Test]
        public void EveryPieceTheGameDraws_IsOnAndOpaque_AndEverySkyFitsTheViews()
        {
            // lección de Punta (3-oct): una pieza creada APAGADA, o sin imagen, deja un cuadrado blanco o nada sobre el cielo oscuro. Además se arma un cielo de cada etapa para ver que cabe en los pools.
            var go = new GameObject("ConstelacionAudit");
            try
            {
                var controller = go.AddComponent<ConstelacionGameController>();
                // solo se arma la interfaz (Awake también estiliza el panel de resultado, y eso usa Destroy, que en modo edición no se puede)
                var build = typeof(ConstelacionGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                var problems = controller.AuditVisibility();
                Assert.IsEmpty(problems, "Piezas que no se verían: " + string.Join(" | ", problems));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static string Read(string file) => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Parejas", file));

        private static readonly string[] ControllerFiles =
        {
            "ConstelacionGameController.cs", "ConstelacionGameController.Build.cs", "ConstelacionGameController.Scene.cs", "ConstelacionGameController.Guided.cs", "ConstelacionMotion.cs"
        };

        [Test]
        public void TheController_UsesOnlyTheGameClock_SoThePauseStopsEverything()
        {
            foreach (var file in ControllerFiles)
            {
                string text = Read(file);
                foreach (var banned in new[] { "Time.time", "Time.deltaTime", "Time.unscaledTime", "Time.unscaledDeltaTime", "WaitForSeconds", "WaitForSecondsRealtime", "Time.realtimeSinceStartup" })
                    Assert.IsFalse(text.Contains(banned), file + ": usa «" + banned + "» en vez de GameClock");
            }
        }

        [Test]
        public void NoFacesReactToThePerformance_AndTheBoardIsAlwaysCompleteAtOnce_NoPreviewNoLivesNoGridNoSpeedBonus()
        {
            string all = string.Concat(ControllerFiles.Select(Read)) + Read("ConstelacionContract.cs") + Read("ConstelacionSky.cs");
            // regla de patentes (Akili): nada de caras felices o tristes según cómo se juega
            foreach (var banned in new[] { "mood", "Mood", "happy", "Happy", "emotion", "Emotion", "feliz", "triste" })
                StringAssert.DoesNotContain(banned, all, "no hay caras que reaccionen al desempeño: «" + banned + "»");
            // lo que se fue de la versión anterior (docs §10)
            foreach (var gone in new[] { "LivesHud", "SymbolBank", "PreviewExposure", "Distractor", "GridLayoutGroup", "SpeedBonus", "StageFailures", "MismatchesToFail" })
                StringAssert.DoesNotContain(gone, all, "se fue de la versión anterior: «" + gone + "»");
            // el tablero siempre está completo a la vez (patente de Posit): las luces aparecen todas en el mismo cielo, nada se muestra «una por una» como secuencia a recordar
            StringAssert.Contains("AppearStepMs", Read("ConstelacionContract.cs"));
        }

        [Test]
        public void TheLinesDrawOnTopOfAllTheLights_AndNothingBlocksTheTap()
        {
            string build = Read("ConstelacionGameController.Build.cs");
            int lights = build.IndexOf("_lightLayer = Layer(br, \"Lights\")", StringComparison.Ordinal);
            int links = build.IndexOf("_linkLayer = Layer(br, \"Links\")", StringComparison.Ordinal);
            Assert.Greater(lights, 0);
            Assert.Greater(links, lights, "la capa de las líneas va DESPUÉS (encima) de la de las luces");
            string main = Read("ConstelacionGameController.cs");
            // no hay candado global: el toque solo depende de que la luz esté dormida y haya aparecido (ConstelacionSky.CanTap)
            StringAssert.DoesNotContain("_isTurnLocked", main);
            StringAssert.DoesNotContain("_locked", main);
            string sky = Read("ConstelacionSky.cs");
            StringAssert.Contains("ClosePending()", sky);
        }

        [Test]
        public void ThePauseAndHowTo_AreWired_AndTheGameIsRegistered()
        {
            string main = Read("ConstelacionGameController.cs");
            StringAssert.Contains("override bool HowToReady", main);
            StringAssert.Contains("override void HowToSuspend", main);
            StringAssert.Contains("override void HowToResume", main);
            StringAssert.Contains("HowToClock.Shift(_endsAt", main, "«Cómo se juega» y las tarjetas no gastan tiempo del Reto");
            StringAssert.Contains("PollTutorialSkip", main);
            StringAssert.Contains("RunTutorialIfNeeded", main);
            StringAssert.Contains("BuildTutorial", main);
            string entry = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Bootstrap", "GameEntryPoint.cs"));
            StringAssert.Contains("ConstelacionGameController.GameId", entry);
            Assert.AreEqual("parejas", ConstelacionGameController.GameId, "el id no cambia: el historial se conserva");
        }

        [Test]
        public void TheGuidedRound_NeverTouchesTheScoreTheDdaTheRecordOrTheCounts()
        {
            string main = Read("ConstelacionGameController.Guided.cs");
            int a = main.IndexOf("// <guided>"), b = main.IndexOf("// </guided>");
            Assert.Greater(a, 0);
            Assert.Greater(b, a);
            string region = main.Substring(a, b - a);
            foreach (var token in new[] { "_dda", "_tally", "_streak", "_bestStreak", "_bestRecord", "Register(", "PlayerPrefs", "IntroSeen(", "MarkIntroSeen(", "FinishGame(", "EndBoard(" })
                StringAssert.DoesNotContain(token, region, "la ronda guiada no puede tocar «" + token + "»");
            StringAssert.Contains("GuidedScript", region);
            // lo que cuenta se salta mientras se guía
            string ctl = Read("ConstelacionGameController.cs");
            StringAssert.Contains("if (res.Opportunity && !_guided)", ctl);
        }

        [Test]
        public void TheIntroCards_FitInTheScreen_EvenIfEachLineBreaksInTwoAt14dpOrMore()
        {
            float widthPx = (ConstelacionLayout.W - 36f - 28f) * 3f;
            foreach (ConIntro intro in Enum.GetValues(typeof(ConIntro)))
            {
                if (intro == ConIntro.None) continue;
                int lines = 0;
                foreach (var line in ConstelacionContract.IntroLines(intro))
                {
                    var measure = NeuroVida.Games.Shared.CoachText.Measure(line, widthPx, 51);
                    Assert.LessOrEqual(measure.Lines, 2, intro + ": «" + line + "» ocupa " + measure.Lines + " líneas");
                    lines += measure.Lines;
                }
                Assert.LessOrEqual(lines, 6, intro + ": la tarjeta no crece de más");
            }
        }
    }
}
