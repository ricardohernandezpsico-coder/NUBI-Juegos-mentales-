using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Anagramas;
using NeuroVida.Games.Bodega;
using NeuroVida.Games.Calculo;
using NeuroVida.Games.Correo;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// Los sonidos de cada juego en «Madera cálida» contra su FAMILIA (paso 0 de las Fases C a E, Tarea 133; spec `auditoria-fable/spec-sonido-CDE.md` §1.4): para cada juego de la lista, cada receta de su <c>All()</c> es finita, tiene el pico en su nivel, termina en 0, empieza enseguida y suena DENTRO de la banda de sonoridad
    /// de su familia (las de <c>TheLevelsFollowTheTiers_SoEverythingSoundsEven</c>; las texturas, según su objeto), y lo que el juego pide antes de la cuenta regresiva (<c>Request()</c>) pesa menos de 10 MB y se calcula sin omitir ni calcular tarde nada. La lista crece con cada fase: Fase C hoy (Carga, Punta, Constelaciones, Bodega y Correo).
    /// </summary>
    public class GameSoundsTierTests
    {
        private bool _soundWas;

        private sealed class GameSounds
        {
            public string Id;
            public Func<IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)>> All;
            public Action Request;
        }

        private static readonly GameSounds[] Games =
        {
            new GameSounds { Id = "calculo", All = CargaSounds.All, Request = CargaSounds.Request },
            new GameSounds { Id = "anagramas", All = PuntaSounds.All, Request = PuntaSounds.Request },
            new GameSounds { Id = "parejas", All = ConstelacionSounds.All, Request = ConstelacionSounds.Request },
            new GameSounds { Id = "bodega", All = BodegaSounds.All, Request = BodegaSounds.Request },
            new GameSounds { Id = "correo", All = MailSounds.All, Request = MailSounds.Request },
            new GameSounds { Id = "secuencia", All = RastroSounds.All, Request = RastroSounds.Request },
        };

        [SetUp]
        public void SetUp()
        {
            _soundWas = GameFeel.SoundOn;
            GameFeel.SoundOn = true;
            WoodCore.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            WoodCore.Reset();
            GameFeel.SoundOn = _soundWas;
        }

        private static double Lufs(float[] l, float[] r) => SoundMeter.MomentaryLufs(l, r) + SoundMeter.ToDb(WoodSounds.PlayVolume);

        [Test]
        public void EveryOwnSound_IsFinite_WithThePeakAtItsLevel_AndEndsInSilence()
        {
            foreach (var g in Games)
                foreach (var (key, recipe, mono) in g.All())
                {
                    WoodSounds.Render(recipe, out var l, out var r);
                    Assert.Greater(l.Length, 2000, key);
                    for (int i = 0; i < l.Length; i++) Assert.IsFalse(float.IsNaN(l[i]) || float.IsInfinity(l[i]) || float.IsNaN(r[i]) || float.IsInfinity(r[i]), key + " tiene un valor no finito");
                    Assert.AreEqual(recipe.Level, SoundMeter.Peak(l, r), 0.005, key + ": el pico es el nivel de la receta");
                    Assert.LessOrEqual(SoundMeter.Peak(l, r), 0.41, key + ": nada pasa de 0,40 (el techo de −6 dBFS reproducido no cambia)");
                    Assert.AreEqual(0f, l[l.Length - 1], 1e-6f, key + ": termina en 0 (sin clic de corte)");
                    Assert.AreEqual(0f, r[r.Length - 1], 1e-6f, key);
                }
        }

        [Test]
        public void EveryOwnSound_StartsRightAway_ThoseThatStartWithABreathWithinThirtyMilliseconds()
        {
            foreach (var g in Games)
                foreach (var (key, recipe, mono) in g.All())
                {
                    WoodSounds.Render(recipe, out var l, out var r);
                    double onset = SoundMeter.OnsetMs(l, r, 0.1);
                    Assert.LessOrEqual(onset, 30.0, key + ": se oye a los " + onset.ToString("0.0") + " ms");
                }
        }

        [Test]
        public void EveryOwnSound_SoundsInsideTheBandOfItsFamily()
        {
            foreach (var g in Games)
                foreach (var (key, recipe, mono) in g.All())
                {
                    WoodSounds.Render(recipe, out var l, out var r);
                    double lufs = Lufs(l, r);
                    string name = key + " [" + recipe.Tier + "] " + lufs.ToString("0.0") + " LUFS";
                    switch (recipe.Tier)
                    {
                        case WoodTier.Micro: Assert.LessOrEqual(lufs, -27.0, name + ": los micro son los más suaves"); break;
                        case WoodTier.Respuesta: Assert.That(lufs, Is.InRange(-30.0, -19.0), name); break;
                        case WoodTier.Hito: Assert.That(lufs, Is.InRange(-22.0, -17.0), name); break;
                        case WoodTier.Cierre: Assert.That(lufs, Is.InRange(-20.0, -15.0), name); break;
                        default: Assert.That(lufs, Is.InRange(-40.0, -18.0), name + ": una textura va al nivel de su objeto"); break;
                    }
                }
        }

        [Test]
        public void EveryGame_RequestsBeforeTheCountdown_AndItsExtrasWeighLessThanTenMegabytes_NothingSkippedNorLate()
        {
            foreach (var g in Games)
            {
                WoodCore.Reset();
                GameFeel.SoundOn = true;
                GameFeel.Warm(g.Id);
                g.Request();
                int steps = 0;
                while (!WoodCore.Idle) { WoodCore.Step(1f); if (++steps > 200000) Assert.Fail(g.Id + ": el calculo no termina"); }
                Assert.Greater(WoodCore.ExtraCount, 0, g.Id + ": pide algo");
                Assert.LessOrEqual(WoodCore.ExtraBytes(), 10L * 1024 * 1024, g.Id + ": lo extra pesa " + (WoodCore.ExtraBytes() / 1048576.0).ToString("0.0") + " MB (techo 10 MB)");
                foreach (var (key, recipe, mono) in g.All()) Assert.IsNotNull(WoodCore.GetExtra(key), g.Id + ": el propio «" + key + "» se pidió y se calculó");
                Assert.AreEqual(0, WoodCore.Skipped, g.Id);
                Assert.AreEqual(0, WoodCore.LateBakes, g.Id);
                foreach (var (key, recipe, mono) in g.All()) Assert.IsTrue(WoodCore.Play(key), g.Id + ": «" + key + "» suena");
                Assert.AreEqual(0, WoodCore.Skipped, g.Id + ": lo pedido suena");
                WoodCore.ReleaseExtras();
                Assert.AreEqual(0L, WoodCore.ExtraBytes(), g.Id + ": al cambiar de juego se suelta");
            }
        }

        [Test]
        public void TheOwnSoundsOfThePhase_AreTheOnesOfTheSpec()
        {
            // Fase C: Bodega 5 propios, Correo 3 (Carga, Punta y Constelaciones no tienen: la pista de Constelaciones es la nota del grupo, opción (a) que eligió Ricardo, Tarea 137)
            CollectionAssert.AreEquivalent(new[] { "bodega.arrive", "bodega.beam", "bodega.open", "bodega.fly", "bodega.ratchet" }, Keys(BodegaSounds.All()));
            CollectionAssert.AreEquivalent(new[] { "correo.horn", "correo.sack", "correo.radio" }, Keys(MailSounds.All()));
            foreach (var g in new[] { Games[0], Games[1], Games[2] }) CollectionAssert.IsEmpty(Keys(g.All()), g.Id);
            // Fase D (Tarea 138): Rastro 4 (el aviso de cada modo)
            CollectionAssert.AreEquivalent(new[] { "rastro.cue0", "rastro.cue1", "rastro.cue2", "rastro.cue3" }, Keys(RastroSounds.All()));
            // los que van en mono son los micro sin sala: solo «abrir una puerta»
            foreach (var (key, recipe, mono) in BodegaSounds.All()) Assert.AreEqual(key == "bodega.open", mono, key);
        }

        private static List<string> Keys(IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> all)
        {
            var keys = new List<string>();
            foreach (var (key, recipe, mono) in all) keys.Add(key);
            return keys;
        }

        [Test]
        public void TheRatchetOfTheBodega_Has14ClicksEvery110Ms_AndLastsTheSpin()
        {
            var recipe = BodegaSounds.MakeRecipe(BodegaSfx.Ratchet);
            // las muestras SECAS de la receta (antes de la sala): ahí cada golpe tiene su inicio limpio; las dos marimbas graves de abajo (a 0 y a 750 ms) suman sus propios inicios
            var onsets = SoundMeter.OnsetsMs(recipe.X, null, -30.0, 15.0);
            for (int i = 0; i < 14; i++)
            {
                bool found = false;
                foreach (var o in onsets) if (Math.Abs(o - i * 110.0) <= 3.0) found = true;
                Assert.IsTrue(found, "el clic " + i + " entra a los " + (i * 110) + " ms (inicios: " + string.Join(", ", onsets) + ")");
            }
            foreach (var o in onsets)
            {
                bool isClick = o > 1500.0;                                                           // más allá del último clic (a 1430 ms) solo queda el corte de la marimba de abajo (a los 1,6 s)
                for (int i = 0; i < 14; i++) if (Math.Abs(o - i * 110.0) <= 3.0) isClick = true;
                Assert.IsTrue(isClick, "no hay otros inicios que los 14 clics: " + string.Join(", ", onsets));
            }
            Assert.GreaterOrEqual(recipe.X.Length / (double)SoundKit.Rate, BodegaMotion.SpinSeconds + 0.3 - 0.01, "dura lo que el giro (SpinSeconds + 0,3 s)");
        }

        [Test]
        public void TheHornOfTheMail_Lasts2Seconds_WithItsThreeOnsetsAt0_50And100Ms()
        {
            var recipe = MailSounds.MakeRecipe(MailSfx.Horn);
            Assert.GreaterOrEqual(recipe.X.Length / (double)SoundKit.Rate, 1.9);
            var onsets = SoundMeter.OnsetsMs(recipe.X, null, -25.0, 20.0);                          // las muestras secas de la receta: tres notas a 0, 50 y 100 ms y sus segundos golpes 300 ms después
            string all = string.Join(", ", onsets);
            // el detector no es perfecto con notas graves que se montan (puede sumar un inicio de más entre dos golpes): los inicios de la receta tienen que estar ENTRE los que encuentra
            foreach (double expected in new[] { 0.0, 50.0, 100.0, 300.0, 350.0, 400.0 })
            {
                bool found = false;
                foreach (var o in onsets) if (Math.Abs(o - expected) <= 4.0) found = true;
                Assert.IsTrue(found, "falta el inicio de los " + expected + " ms (la bocina son tres notas a 0, 50 y 100 ms, con un segundo golpe 300 ms después para que «dure»): " + all);
            }
        }

        [Test]
        public void TheOnsetsMeter_FindsEveryHitOfASequence()
        {
            var x = SoundKit.Blank(1.0);
            for (int i = 0; i < 5; i++) SoundKit.Add(x, WoodKit.Woodblock(1000), 0.1 + i * 0.15, 1.0);
            var onsets = SoundMeter.OnsetsMs(x, null, -30.0, 40.0);
            Assert.AreEqual(5, onsets.Count, string.Join(", ", onsets));
            for (int i = 0; i < 5; i++) Assert.AreEqual(100.0 + i * 150.0, onsets[i], 3.0);
            Assert.AreEqual(0, SoundMeter.OnsetsMs(new float[1000]).Count, "el silencio no tiene inicios");
        }
    }
}
