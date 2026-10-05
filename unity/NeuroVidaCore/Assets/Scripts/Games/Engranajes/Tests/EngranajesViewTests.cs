using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Engranajes;

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
                Assert.Greater(m.QuestionTop, m.StripTop + EngranajesLayout.StripH - 0.01f, where + ": la pregunta va bajo la cabecera");
                float sceneTop = m.SceneCenterLogicalY - EngranajesLayout.SceneHeight * m.SceneScale / 2f;
                float sceneBottom = m.SceneCenterLogicalY + EngranajesLayout.SceneHeight * m.SceneScale / 2f;
                Assert.GreaterOrEqual(sceneTop, m.QuestionTop + EngranajesLayout.QuestionH, where + ": la escena va bajo la pregunta");
                Assert.LessOrEqual(sceneBottom, m.ButtonsTop, where + ": los botones van bajo la escena");
                Assert.LessOrEqual(m.ButtonsTop + m.ButtonH, m.SayTop, where + ": el aviso va bajo los botones");
                Assert.LessOrEqual(m.SayTop + m.SayH, h + 0.01f, where + ": el aviso cabe en la pantalla");
                Assert.GreaterOrEqual(m.ButtonH, 52f, where + ": los botones miden al menos 52 dp");
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
                var left = EngranajesLayout.SceneToLogical(m, 8f, 280f);       // el borde izquierdo de la sala
                var right = EngranajesLayout.SceneToLogical(m, 361f, 280f);    // la punta de la aleta derecha
                Assert.GreaterOrEqual(left.x, 8f, "alto " + h + ": con margen para que la flecha del motor no se corte");
                Assert.LessOrEqual(right.x, EngranajesLayout.W - 8f, "alto " + h);
            }
        }

        [Test]
        public void ThreeButtons_FitSideBySide_AndTwoAreWider()
        {
            var m = EngranajesLayout.Compute(740f);
            for (int n = 2; n <= 3; n++)
            {
                for (int k = 0; k < n; k++)
                {
                    var r = EngranajesLayout.ButtonRect(m, k, n);
                    Assert.GreaterOrEqual(r.x, 0f);
                    Assert.LessOrEqual(r.xMax, EngranajesLayout.W);
                    Assert.GreaterOrEqual(r.width, 95f, n + " botones: cada uno ≥ 95 dp");
                    if (k > 0) Assert.Greater(r.x, EngranajesLayout.ButtonRect(m, k - 1, n).xMax, "no se pisan");
                }
            }
            Assert.Greater(EngranajesLayout.ButtonRect(m, 0, 2).width, EngranajesLayout.ButtonRect(m, 0, 3).width);
        }

        [Test]
        public void TheQuestion_BreaksNearTheMiddle_WithoutALoneWord()
        {
            string one = EngranajesLayout.BreakQuestion("¿Cómo girará la antena?", out bool two);
            Assert.IsFalse(two);
            Assert.AreEqual("¿Cómo girará la antena?", one);
            string longQ = EngranajesLayout.BreakQuestion("¿La turbina gira más rápido o más lento que el motor?", out two);
            Assert.IsTrue(two);
            var lines = longQ.Split('\n');
            Assert.AreEqual(2, lines.Length);
            Assert.Less(Math.Abs(lines[0].Length - lines[1].Length), 12, "las dos líneas quedan parejas: " + longQ);
            foreach (var line in lines) Assert.Greater(line.Split(' ').Length, 2, "no queda una palabra suelta: " + line);
            // todas las preguntas del juego caben en una o dos líneas de ≤ 36 caracteres
            var rng = new System.Random(5);
            for (int level = 1; level <= 12; level++)
            {
                var q = EngranajesLayout.BreakQuestion(EngranajesContract.QuestionText(EngranajesContract.Generate(level, rng)), out two);
                foreach (var line in q.Split('\n')) Assert.LessOrEqual(line.Length, 36, "nivel " + level + ": " + line);
            }
        }
    }

    public class EngranajesArtTests
    {
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
            foreach (var sprite in new[] { EngranajesSprites.Room(), EngranajesSprites.Hull(), EngranajesSprites.Dish(), EngranajesSprites.Fan(), EngranajesSprites.Door(), EngranajesSprites.CargoFrame(),
                EngranajesSprites.CargoPlate(), EngranajesSprites.CargoBox(), EngranajesSprites.Hub(), EngranajesSprites.TargetRing(), EngranajesSprites.DashedRing() })
            {
                Assert.IsNotNull(sprite);
                float filled = 0f;
                var px = sprite.texture.GetPixels32();
                foreach (var c in px) if (c.a > 20) filled++;
                Assert.Greater(filled / px.Length, 0.01f, "el sprite " + sprite.name + " no puede estar vacío");
            }
            foreach (ButtonIcon ic in Enum.GetValues(typeof(ButtonIcon)))
            {
                var icon = EngranajesSprites.Icon(ic);
                int filled = 0;
                foreach (var c in icon.texture.GetPixels32()) if (c.a > 128) filled++;
                Assert.Greater(filled, 150, "el ícono " + ic + " se dibuja");
            }
            // la flecha en un sentido y en el otro son espejos
            var cw = EngranajesSprites.Arrow(40f, 3.5f, true);
            var ccw = EngranajesSprites.Arrow(40f, 3.5f, false);
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

        [Test]
        public void EverySound_IsSynthesized_WithAudibleButNotLoudSamples()
        {
            var warm = EngranajesSounds.Prewarm();
            while (warm.MoveNext()) { }
            var clips = new[]
            {
                EngranajesSounds.Step(0), EngranajesSounds.Step(5), EngranajesSounds.Step(9), EngranajesSounds.MotorStart(), EngranajesSounds.Antenna(), EngranajesSounds.Turbine(),
                EngranajesSounds.Rack(), EngranajesSounds.Thud(), EngranajesSounds.Jam(), EngranajesSounds.Place(), EngranajesSounds.Press(), EngranajesSounds.Chime(),
                EngranajesSounds.Success(), EngranajesSounds.Light(0), EngranajesSounds.Light(9), EngranajesSounds.Launch(), EngranajesSounds.Finale()
            };
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
            // cada pieza del cohete suena distinto
            Assert.AreNotSame(EngranajesSounds.Antenna(), EngranajesSounds.Turbine());
            Assert.AreNotSame(EngranajesSounds.Turbine(), EngranajesSounds.Rack());
            Assert.Greater(EngranajesSounds.Launch().length, 3f);
        }
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
