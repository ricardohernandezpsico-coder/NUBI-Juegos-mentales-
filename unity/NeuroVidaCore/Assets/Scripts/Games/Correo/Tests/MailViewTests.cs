using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Correo.Tests
{
    /// <summary>La disposición de «La estación de correo» en la pantalla (docs §5): todo cabe, nada se encima y ningún toque mide menos de 56 dp.</summary>
    public class MailLayoutTests
    {
        private static readonly float[] Heights = { 592f, 600f, 640f, 700f, 760f, 800f, 900f };

        [Test]
        public void EverythingFits_AndNothingOverlaps_FromFiveHundredNinetyTwoDpUp()
        {
            foreach (float h in Heights)
            {
                var m = MailLayout.Compute(h);
                string at = " (alto " + h + ")";
                Assert.GreaterOrEqual(m.ClockCy - MailLayout.ClockR, MailLayout.HudDp, "el reloj va debajo del marcador" + at);
                Assert.LessOrEqual(m.ClockCx + MailLayout.ClockR, MailLayout.W, "el reloj cabe a lo ancho" + at);
                Assert.LessOrEqual(m.Banner.Y + m.Banner.H, m.BeltY - MailLayout.BeltBandH / 2f, "la franja de avisos va arriba de la cinta" + at);
                Assert.GreaterOrEqual(m.Banner.Y, MailLayout.HudDp, "y debajo del marcador" + at);
                Assert.LessOrEqual(m.BeltY + MailLayout.BeltBandH / 2f, m.BoxY0 - 20f, "aire entre la cinta y los buzones" + at);
                Assert.LessOrEqual(m.BoxY0 + MailLayout.BoxH, m.BtnY0 - 20f, "aire entre los buzones y los botones" + at);
                Assert.LessOrEqual(m.BtnY0 + MailLayout.BtnH, m.HintY - 14f, "la frase de abajo no toca los botones" + at);
                Assert.LessOrEqual(m.Bottom, h, "todo cabe en el alto" + at);
            }
        }

        [Test]
        public void EveryTouchTargetIsAtLeastFiftySixDp_AndTheBoxesAreCenteredWithoutTouchingEachOther()
        {
            var m = MailLayout.Compute(640f);
            Assert.GreaterOrEqual(m.Safe.W, 56f); Assert.GreaterOrEqual(m.Safe.H, 56f);
            Assert.GreaterOrEqual(m.Beacon.W, 56f); Assert.GreaterOrEqual(m.Beacon.H, 56f);
            Assert.GreaterOrEqual(2f * MailLayout.ClockR + 20f, 56f, "el reloj (con el margen de toque) mide más de 56 dp");
            Assert.Less(m.Safe.X + m.Safe.W, m.Beacon.X, "la caja fuerte y el faro no se tocan");
            Assert.LessOrEqual(m.Beacon.X + m.Beacon.W, MailLayout.W);
            for (int n = 2; n <= 4; n++)
            {
                var rects = Enumerable.Range(0, n).Select(i => MailLayout.BoxRect(i, n, m.BoxY0)).ToArray();
                foreach (var r in rects)
                {
                    Assert.GreaterOrEqual(r.W, 56f, n + " buzones: cada uno de 56 dp o más");
                    Assert.GreaterOrEqual(r.H, 56f);
                    Assert.GreaterOrEqual(r.X, 0f);
                    Assert.LessOrEqual(r.X + r.W, MailLayout.W);
                }
                for (int i = 1; i < n; i++) Assert.GreaterOrEqual(rects[i].X - (rects[i - 1].X + rects[i - 1].W), 10f, "10 dp de aire entre buzones");
                Assert.AreEqual(MailLayout.W / 2f, (rects[0].X + rects[n - 1].X + rects[n - 1].W) / 2f, 0.01f, "centrados");
            }
            Assert.AreEqual(150f, MailLayout.BoxRect(0, 2, 0f).W);
            Assert.AreEqual(102f, MailLayout.BoxRect(0, 3, 0f).W);
            Assert.AreEqual(78f, MailLayout.BoxRect(0, 4, 0f).W);
        }

        [Test]
        public void TheBeltSlots_FrontLetterWaitsAt150_TheRestFollowBehind()
        {
            Assert.AreEqual(150f, MailLayout.SlotX(0));
            Assert.AreEqual(260f, MailLayout.SlotX(1));
            Assert.AreEqual(330f, MailLayout.SlotX(2));
            Assert.Greater(MailLayout.SlotX(1) - MailLayout.SlotX(0), MailLayout.LetterW * 0.9f, "la segunda no tapa a la primera");
        }
    }

    /// <summary>El movimiento de la estación: el resorte de la cinta, el momento del faro y la nave (docs §11).</summary>
    public class MailMotionTests
    {
        [Test]
        public void TheBeltSpring_ArrivesAtItsSlot_WithoutWildOvershoot()
        {
            float x = 420f, v = 0f, max = 0f;
            for (int i = 0; i < 180; i++) { MailMotion.Spring(ref x, ref v, 150f, 1f / 60f, MailMotion.BeltFreq, MailMotion.BeltZeta); max = Mathf.Max(max, 150f - x); }
            Assert.Less(Mathf.Abs(x - 150f), 0.5f);
            Assert.Less(max, 25f, "se pasa poco");
            // y entra a los 40 dp de su lugar (se puede tocar) en menos de un segundo y medio
            x = 420f; v = 0f;
            int frames = 0;
            while (Mathf.Abs(x - 150f) > MailContract.ReadyDistance && frames < 600) { MailMotion.Spring(ref x, ref v, 150f, 1f / 60f, MailMotion.BeltFreq, MailMotion.BeltZeta); frames++; }
            Assert.Less(frames / 60f, 1.5f, "una carta nueva espera menos de 1,5 s antes de poder tocarse");
        }

        [Test]
        public void TheSpringUsesTheRealDt_WithNoJumps_WhenTheFrameIsLong()
        {
            float x = 300f, v = 0f;
            MailMotion.Spring(ref x, ref v, 150f, 5f, 2.2f, 0.82f);          // un cuadro de 5 s se acota a 0,05
            Assert.Greater(x, 250f, "un cuadro muy largo no hace saltar la carta (se acota a 0,05 s)");
            Assert.AreEqual(0.05f, MailMotion.ClampDt(5f));
            Assert.AreEqual(0f, MailMotion.ClampDt(-1f));
        }

        [Test]
        public void TheBeaconShow_LastsThreePointFourSeconds_WithTheLightFadingInAndOut()
        {
            Assert.AreEqual(3.4f, MailMotion.ShowSeconds);
            Assert.AreEqual(0f, MailMotion.ShowEnvelope(0f), 1e-5f);
            Assert.AreEqual(1f, MailMotion.ShowEnvelope(0.25f), 1e-5f, "sube en 0,25 s");
            Assert.AreEqual(1f, MailMotion.ShowEnvelope(1.5f), 1e-5f);
            Assert.AreEqual(1f, MailMotion.ShowEnvelope(2.6f), 1e-5f);
            Assert.Less(MailMotion.ShowEnvelope(3.0f), 0.6f, "baja en los últimos 0,8 s");
            Assert.AreEqual(0f, MailMotion.ShowEnvelope(3.4f), 1e-5f);
            Assert.AreEqual(0f, MailMotion.ShowEnvelope(5f));
        }

        [Test]
        public void TheShip_EntersFromTheLeftFollowingTheLight_StopsOverTheBelt_AndLeavesByTheRight_AndNeverBeforeTheBeaconIsLit()
        {
            float x, y;
            MailMotion.ShipAt(0.2f, false, 116f, out x, out y);
            Assert.Less(x, 0f, "al principio está fuera de la pantalla, a la izquierda");
            MailMotion.ShipAt(1.1f, false, 116f, out x, out y);
            Assert.That(x, Is.InRange(0f, 180f));
            MailMotion.ShipAt(2.0f, false, 116f, out x, out y);
            Assert.AreEqual(180f, x, 0.01f, "se detiene en el centro");
            Assert.AreEqual(146f, y, 3f, "sobre la cinta");
            MailMotion.ShipAt(3.3f, false, 116f, out x, out y);
            Assert.Greater(x, 330f, "se va por la derecha");
            MailMotion.ShipAt(1.0f, true, 116f, out x, out y);
            Assert.AreEqual(180f, x, "con «quitar animaciones» queda quieta sobre la cinta");
            Assert.AreEqual(146f, y, 0.01f);
            MailMotion.ShipAt(3.3f, true, 116f, out x, out y);
            Assert.AreEqual(180f, x);
            // el saco cae desde 1,8 s en 0,4 s
            Assert.AreEqual(0f, MailMotion.SackDrop(1.0f));
            Assert.AreEqual(0f, MailMotion.SackDrop(1.8f));
            Assert.AreEqual(0.5f, MailMotion.SackDrop(2.0f), 1e-5f);
            Assert.AreEqual(1f, MailMotion.SackDrop(2.2f));
            // el haz barre de lado a lado; sin animaciones queda hacia arriba
            Assert.AreEqual(-Mathf.PI / 2f, MailMotion.BeamAngle(1.234f, true), 1e-5f);
            float lo = float.MaxValue, hi = float.MinValue;
            for (float t = 0f; t < 3.4f; t += 0.02f) { float a = MailMotion.BeamAngle(t, false); lo = Mathf.Min(lo, a); hi = Mathf.Max(hi, a); }
            Assert.Greater(hi - lo, 1.8f, "barre de lado a lado");
        }

        [Test]
        public void TheEasings_AreMonotonic_AndClampedToZeroAndOne()
        {
            Assert.AreEqual(0f, MailMotion.EaseOut(-1f));
            Assert.AreEqual(1f, MailMotion.EaseOut(2f));
            Assert.AreEqual(0.5f, MailMotion.EaseInOut(0.5f), 1e-5f);
            float prev = 0f;
            for (float t = 0f; t <= 1f; t += 0.05f) { float e = MailMotion.EaseInOut(t); Assert.GreaterOrEqual(e, prev - 1e-6f); prev = e; }
            Assert.AreEqual(1f, MailMotion.EaseBack(1f), 1e-5f);
        }
    }

    /// <summary>El arte y el sonido de la estación: todo se hornea, se ve y suena (y ninguno trae símbolos que no van: docs/simbolos-neutros.md).</summary>
    public class MailArtTests
    {
        private static Sprite Bake(Func<Sprite> f)
        {
            var s = f();
            Assert.IsNotNull(s);
            Assert.IsNotNull(s.texture);
            return s;
        }

        private static (int opaque, int total, int cornerAlpha) Stats(Sprite s)
        {
            var px = s.texture.GetPixels32();
            int opaque = px.Count(p => p.a > 200);
            int w = s.texture.width, h = s.texture.height;
            int corner = Mathf.Max(px[0].a, Mathf.Max(px[w - 1].a, Mathf.Max(px[(h - 1) * w].a, px[h * w - 1].a)));
            return (opaque, px.Length, corner);
        }

        [Test]
        public void EveryLetter_IsBaked_HasAPaperAndATransparentCorner_AndTheTypesDiffer()
        {
            var seen = new HashSet<string>();
            foreach (MailCue cue in new[] { MailCue.None, MailCue.Gold, MailCue.Lazo })
                for (int pl = 0; pl < MailContract.PlanetCount; pl++)
                {
                    var s = Bake(() => MailSprites.Letter(pl, cue));
                    Assert.AreEqual(Mathf.CeilToInt(MailSprites.LetterBoxW * MailSprites.LetterPpd), s.texture.width);
                    var (opaque, total, corner) = Stats(s);
                    Assert.Greater(opaque / (float)total, 0.5f, "la carta ocupa la mayor parte de su caja (" + pl + "/" + cue + ")");
                    Assert.AreEqual(0, corner, "las esquinas son transparentes");
                    Assert.IsTrue(seen.Add(string.Join(",", s.texture.GetPixels32().Where((p, i) => i % 211 == 0).Select(p => p.r + ":" + p.g + ":" + p.b + ":" + p.a))), "dos cartas distintas no se ven iguales (" + pl + "/" + cue + ")");
                }
            Assert.AreSame(MailSprites.Letter(2, MailCue.Lazo), MailSprites.Letter(2, MailCue.Lazo), "se hornean una sola vez");
        }

        [Test]
        public void ThePlanets_AreFourDistinctFigures_AndTheOtherPiecesAreBaked()
        {
            var hashes = new HashSet<string>();
            for (int i = 0; i < 4; i++)
            {
                var s = Bake(() => MailSprites.Planet(i));
                var (opaque, total, corner) = Stats(s);
                Assert.Greater(opaque, total * 0.3f, "el planeta " + i + " ocupa su caja");
                Assert.AreEqual(0, corner);
                Assert.IsTrue(hashes.Add(string.Join(",", s.texture.GetPixels32().Where((p, k) => k % 97 == 0).Select(p => p.r + ":" + p.g + ":" + p.b))), "los planetas se distinguen por color Y por figura");
            }
            foreach (var s in new[] { MailSprites.Dial(), MailSprites.Tower(), MailSprites.Lamp(false), MailSprites.Lamp(true), MailSprites.Ship(), MailSprites.Flame(), MailSprites.Sack(), MailSprites.DashFrame(), MailSprites.CheckOk(), MailSprites.CheckNo(), MailSprites.DayBar() })
            {
                Assert.IsNotNull(s);
                var (opaque, total, corner) = Stats(s);
                Assert.Greater(opaque, 20, "no está vacío: " + s.name);
                Assert.AreNotSame(MailSprites.Lamp(false), MailSprites.Lamp(true));
            }
            var off = MailSprites.Lamp(false).texture.GetPixel(MailSprites.Lamp(false).texture.width / 2, MailSprites.Lamp(false).texture.height / 2);
            var on = MailSprites.Lamp(true).texture.GetPixel(MailSprites.Lamp(true).texture.width / 2, MailSprites.Lamp(true).texture.height / 2);
            Assert.Greater(on.r + on.g, off.r + off.g, "encendida es más clara que apagada");
            Assert.AreNotEqual(MailSprites.CheckOk().texture.GetPixel(64, 64), MailSprites.CheckNo().texture.GetPixel(64, 64), "✓ y raya no son lo mismo (no solo por color: son formas distintas)");
        }

        [Test]
        public void TheDialHasMarks_NotASpokedWheel_AndTheArtUsesNoPointedStarsCrescentsOrCrosses()
        {
            string src = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Correo", "MailSprites.cs"));
            foreach (var banned in new[] { "Star(", "Crescent(", "spoke", "Spoke", "rayos de la rueda" })
                StringAssert.DoesNotContain(banned, src, "símbolos que no van (docs/simbolos-neutros.md y la rueda de 3 rayos parece el símbolo de la paz)");
            StringAssert.Contains("ocho marcas", src);
            // el dial tiene 8 marcas doradas: se cuentan los puntos dorados a su radio
            var dial = MailSprites.Dial();
            int goldPixels = dial.texture.GetPixels32().Count(p => p.a > 200 && p.r > 200 && p.g > 150 && p.b < 120);
            Assert.Greater(goldPixels, 40);
        }

        [Test]
        public void EverySound_IsAudible_NeverLouderThanHalf_AndTheMailboxesSoundDifferent()
        {
            var clips = new List<AudioClip>();
            for (int i = 0; i < 4; i++) clips.Add(MailSounds.BoxNote(i));
            clips.AddRange(new[] { MailSounds.Thud(), MailSounds.Soft(), MailSounds.Tick(), MailSounds.Fanfare(), MailSounds.Radio(), MailSounds.Horn(), MailSounds.Sack(), MailSounds.Hum(), MailSounds.BriefBell(), MailSounds.Chime(), MailSounds.DayEnd(), MailSounds.Finale() });
            for (int tier = 1; tier <= 3; tier++) clips.Add(MailSounds.Tier(tier));
            for (int c = 0; c < 10; c++) clips.Add(MailSounds.ComboBell(c));
            foreach (var clip in clips)
            {
                var data = new float[clip.samples];
                clip.GetData(data, 0);
                float peak = data.Max(Mathf.Abs);
                Assert.Greater(peak, 0.03f, clip.name + ": se oye");
                Assert.LessOrEqual(peak, 0.5001f, clip.name + ": no pasa de 0,5");
            }
            Assert.AreNotSame(MailSounds.BoxNote(0), MailSounds.BoxNote(1), "cada buzón suena distinto");
            Assert.AreNotSame(MailSounds.Fanfare(), MailSounds.Soft(), "acertar y equivocarse no suenan igual");
            Assert.AreSame(MailSounds.Horn(), MailSounds.Horn(), "se sintetiza una sola vez");
            Assert.GreaterOrEqual(MailSounds.Horn().length, 1.9f, "la bocina de tres notas dura más que un golpe");
        }
    }

    /// <summary>La vista de la estación: todo lo que se dibuja se ve, el reloj es el del juego y las reglas de patentes (docs §8).</summary>
    public class MailVisibilityTests
    {
        private static string Read(string file) => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Correo", file));

        private static readonly string[] ControllerFiles =
        {
            "MailGameController.cs", "MailGameController.Build.cs", "MailGameController.Scene.cs", "MailGameController.Guided.cs", "MailMotion.cs", "MailDay.cs"
        };

        [Test]
        public void EveryPieceTheGameDraws_IsOnAndOpaque_EveryStageFitsTheViews_AndNoTextIsUnderFourteenDp()
        {
            var go = new GameObject("MailAudit");
            try
            {
                var controller = go.AddComponent<MailGameController>();
                var build = typeof(MailGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
            foreach (var file in ControllerFiles)
            {
                string text = Read(file);
                foreach (var banned in new[] { "Time.time", "Time.deltaTime", "Time.unscaledTime", "Time.unscaledDeltaTime", "WaitForSeconds", "WaitForSecondsRealtime", "Time.realtimeSinceStartup" })
                    Assert.IsFalse(text.Contains(banned), file + ": usa «" + banned + "» en vez de GameClock");
            }
        }

        [Test]
        public void NoFacesReactToThePerformance_AndTheFlightIsGone_NoPilotingNoRouteNoAsteroidsNoShieldNoPorts()
        {
            string all = string.Concat(ControllerFiles.Select(Read)) + Read("MailContract.cs") + Read("MailSprites.cs") + Read("MailSounds.cs");
            // regla de patentes (Akili): nada de caras felices o tristes según cómo se juega
            foreach (var banned in new[] { "mood", "Mood", "happy", "Happy", "emotion", "Emotion", "feliz", "triste" })
                StringAssert.DoesNotContain(banned, all, "no hay caras que reaccionen al desempeño: «" + banned + "»");
            // lo que se fue con el vuelo (docs §10 y §8: nunca vuelve una nave que se conduce)
            foreach (var gone in new[] { "PilotContract", "PilotShipSprite", "ShipShield", "Asteroid", "asteroid", "PortSprites", "Rigidbody", "Input.acceleration", "gyro", "Accelerometer", "ShipDamage", "RadioJudgement" })
                StringAssert.DoesNotContain(gone, all, "se fue con el vuelo: «" + gone + "»");
            // patentes de Lumos Labs (docs §8): sin vías ni desvíos, sin pedidos con componentes ni tiempos de entrega
            foreach (var banned in new[] { "Order", "Recipe", "Ingredient", "Switch(", "Junction", "Rail(" })
                StringAssert.DoesNotContain(banned, all, "regla de patentes: «" + banned + "»");
        }

        [Test]
        public void TheShipOnlyAppearsAfterTheBeaconIsLit_NeverBeforeBecauseThatWouldBeAHintOfTheHour()
        {
            string scene = Read("MailGameController.Scene.cs");
            StringAssert.Contains("bool on = _showAt > -50f && t >= 0f && t < MailMotion.ShowSeconds;", scene, "la nave solo existe dentro del momento del faro");
            StringAssert.Contains("_showAt = Now;", scene);
            // y solo se arranca desde DoBeacon con el resultado BeaconHit
            int hit = scene.IndexOf("case MailTap.BeaconHit:", StringComparison.Ordinal);
            int show = scene.IndexOf("_showAt = Now;", StringComparison.Ordinal);
            Assert.Greater(hit, 0);
            Assert.Greater(show, hit, "_showAt se fija dentro del caso BeaconHit");
            Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(scene, "_showAt = Now;").Count, "ningún otro lugar enciende la nave");
        }

        [Test]
        public void TheBeaconAndTheSafe_AreAlwaysOnScreen_SoTheyAreNoReminderOfTheTodos()
        {
            // la caja fuerte y el faro no se esconden ni se muestran según los encargos del día: nadie los enciende ni apaga por su cuenta
            string all = Read("MailGameController.cs") + Read("MailGameController.Scene.cs") + Read("MailGameController.Build.cs");
            StringAssert.DoesNotContain("_safeRoot.gameObject.SetActive", all);
            StringAssert.DoesNotContain("_beaconRoot.gameObject.SetActive", all);
            // la hoja de la mañana es lo único que dice los encargos, y solo en su pantalla
            Assert.AreEqual("Durante el día no los verás.", MailContract.BriefNote1);
        }

        [Test]
        public void ThePauseAndHowTo_AreWired_AndTheGameIsRegistered()
        {
            string main = Read("MailGameController.cs");
            StringAssert.Contains("override bool HowToReady", main);
            StringAssert.Contains("override void HowToSuspend", main);
            StringAssert.Contains("override void HowToResume", main);
            StringAssert.Contains("PollTutorialSkip", main);
            StringAssert.Contains("RunTutorialIfNeeded", main);
            StringAssert.Contains("BuildTutorial", main);
            string entry = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Bootstrap", "GameEntryPoint.cs"));
            StringAssert.Contains("MailGameController.GameId", entry);
            Assert.AreEqual("correo", MailGameController.GameId, "el id no cambia: el historial se conserva");
            StringAssert.Contains("MailContract.CreateEngine", main);
            StringAssert.Contains("_dda.Register(it.Ok)", main, "cada encargo es un ensayo del motor común");
        }

        [Test]
        public void TheGuidedPractice_DoesNotTouchTheScoreTheDdaTheRecordOrTheSavedCards()
        {
            string g = Read("MailGameController.Guided.cs");
            int a = g.IndexOf("// <guided>", StringComparison.Ordinal), b = g.IndexOf("// </guided>", StringComparison.Ordinal);
            Assert.Greater(a, 0);
            string region = g.Substring(a, b - a);
            foreach (var t in new[] { "_dda", "_run", "_recordBroken", "_bestRecord", "Register(", "Complete(", "PlayerPrefs", "FinishGame(" })
                StringAssert.DoesNotContain(t, region);
            StringAssert.Contains("MailDay.Create(2, 1, ref routine, _rng, MailContract.PracticeSeconds)", region, "un día de práctica de 30 s, en la etapa 2");
        }
    }
}
