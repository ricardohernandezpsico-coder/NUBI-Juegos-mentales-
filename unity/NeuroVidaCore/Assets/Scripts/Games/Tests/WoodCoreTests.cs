using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// El núcleo común de «Madera cálida» (Games/Shared/WoodCore.cs + GameFeel, Tarea 120, Fase B): lo de la cuenta regresiva está listo ANTES de que empiece, el resto se calcula de a poco, el núcleo se calcula UNA vez y se conserva entre partidas, con el sonido apagado no se calcula ni suena nada («Mudo»),
    /// el primer sonido no calcula nada (no hay tirón en pleno juego), lo de la interfaz sale por el altavoz que ignora la pausa y el aviso no se suma a otro sonido.
    /// </summary>
    public class WoodCoreTests
    {
        private bool _soundWas;

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

        private static int Drain()
        {
            int steps = 0;
            while (!WoodCore.Idle) { WoodCore.Step(1f); steps++; if (steps > 100000) Assert.Fail("el calculo del núcleo no termina"); }
            return steps;
        }

        private static GameObject Speakers() => GameObject.Find("WoodCore");

        [Test]
        public void TheCountdownSounds_AreReadyBeforeTheCountdown_AndTheRestIsBakedInSlices()
        {
            GameFeel.Warm();
            foreach (var (sfx, arg) in WoodCore.EarlySet) Assert.IsNotNull(WoodCore.Get(sfx, arg), "3, 2, 1 y «¡Ya!» ya están calculados al terminar Warm: " + sfx + arg);
            Assert.AreEqual(WoodCore.EarlySet.Length, WoodCore.BakeCount);
            Assert.IsFalse(WoodCore.Idle, "el resto del núcleo queda en la cola");
            Assert.IsNull(WoodCore.Get(WoodSfx.Finish), "el final todavía no");
            int steps = Drain();
            Assert.Greater(steps, WoodCore.CoreSet.Length * 2, "se calcula de a poco (varios pasos por clip), no de golpe");
            Assert.AreEqual(WoodCore.EarlySet.Length + WoodCore.CoreSet.Length, WoodCore.BakeCount);
            foreach (var (sfx, arg) in WoodCore.CoreSet) Assert.IsNotNull(WoodCore.Get(sfx, arg), sfx + "" + arg);
        }

        [Test]
        public void TheCoreIsBakedOnce_AndKeptBetweenGames()
        {
            GameFeel.Warm();
            Drain();
            int baked = WoodCore.BakeCount;
            var finish = WoodCore.Get(WoodSfx.Finish);
            GameFeel.Warm();                                                   // la partida siguiente
            Assert.IsTrue(WoodCore.Idle, "ya está todo: no queda nada por calcular");
            Drain();
            Assert.AreEqual(baked, WoodCore.BakeCount, "no se calcula nada de nuevo");
            Assert.AreSame(finish, WoodCore.Get(WoodSfx.Finish), "es el mismo clip");
            WoodCore.ResetCounters();
            Assert.AreEqual(baked, WoodCore.BakeCount, "poner en cero lo que se cuenta por partida no suelta los clips");
            Assert.IsNotNull(WoodCore.Get(WoodSfx.Finish));
        }

        [Test]
        public void WithTheSoundOff_NothingIsBakedPlayedOrCreated_Mudo()
        {
            GameFeel.SoundOn = false;
            GameFeel.Warm();
            GameFeel.Correct(1); GameFeel.Correct(9); GameFeel.Wrong(); GameFeel.LevelUp(); GameFeel.Tick(); GameFeel.Finish(); GameFeel.Notice(); GameFeel.Tap();
            GameFeel.Count(3); GameFeel.Count(2); GameFeel.Count(1); GameFeel.Go(); GameFeel.PauseIn(); GameFeel.PauseOut();
            Assert.IsFalse(WoodCore.NoticeUnlessCovered(Time.realtimeSinceStartup + 10f));
            Assert.AreEqual(0, WoodCore.BakeCount, "no se calcula nada");
            Assert.AreEqual(0, WoodCore.PlayCount, "no suena nada");
            Assert.AreEqual(0, WoodCore.ClipCount);
            Assert.AreEqual(0, WoodCore.FinishPlays);
            Assert.IsNull(Speakers(), "ni siquiera se crean los altavoces");
        }

        [Test]
        public void TheFirstSound_BakesNothing_AndIsFast()
        {
            GameFeel.Warm();
            Drain();
            int baked = WoodCore.BakeCount;
            var clock = Stopwatch.StartNew();
            GameFeel.Correct(1);
            clock.Stop();
            Assert.AreEqual(baked, WoodCore.BakeCount, "sonar no calcula nada");
            Assert.AreEqual(1, WoodCore.PlayCount);
            Assert.Less(clock.Elapsed.TotalMilliseconds, 25.0, "el primer sonido (con la creación de los altavoces) sale al instante: " + clock.Elapsed.TotalMilliseconds.ToString("0.0") + " ms");
            clock.Restart();
            GameFeel.Wrong();
            clock.Stop();
            Assert.Less(clock.Elapsed.TotalMilliseconds, 5.0, "los siguientes, todavía más");
        }

        [Test]
        public void ASoundOfTheCoreAskedTooEarly_IsSkipped_NeverBakedInTheMiddleOfTheGame()
        {
            GameFeel.Warm();                                                   // solo lo de la cuenta regresiva
            int baked = WoodCore.BakeCount;
            Assert.IsFalse(WoodCore.Play(WoodSfx.Finish), "el final todavía no está listo");
            Assert.AreEqual(baked, WoodCore.BakeCount, "no se calcula en ese momento (sería un tirón)");
            Assert.AreEqual(1, WoodCore.Skipped);
            Assert.AreEqual(0, WoodCore.PlayCount);
            Drain();
            Assert.IsTrue(WoodCore.Play(WoodSfx.Finish), "ya listo, suena");
        }

        [Test]
        public void TheCoreMemory_IsKnown_AndTheSpeakersAreTheRightOnes()
        {
            GameFeel.Warm();
            Drain();
            Assert.AreEqual(WoodCore.EarlySet.Length + WoodCore.CoreSet.Length, WoodCore.ClipCount);
            Assert.LessOrEqual(WoodCore.StoredBytes(), 6L * 1024 * 1024, "el núcleo común que carga todo juego pesa menos de 6 MB: " + (WoodCore.StoredBytes() / 1048576.0).ToString("0.0") + " MB");
            GameFeel.Tap();
            GameFeel.Correct(1);
            var go = Speakers();
            Assert.IsNotNull(go);
            var sources = go.GetComponents<AudioSource>();
            Assert.AreEqual(2, sources.Length);
            Assert.IsFalse(sources[0].ignoreListenerPause, "el altavoz del juego se calla con la pausa");
            Assert.IsTrue(sources[1].ignoreListenerPause, "el de la interfaz (toque, abrir y seguir la pausa) suena con el audio congelado");
        }

        [Test]
        public void TheTap_IsRateLimited_SoQuickTappingIsNotAMachineGun()
        {
            GameFeel.Warm();
            Drain();
            GameFeel.Tap();
            GameFeel.Tap();
            GameFeel.Tap();
            Assert.AreEqual(1, WoodCore.PlayCount, "tres toques seguidos: suena uno");
        }

        [Test]
        public void TheNotice_DoesNotAddUpToAnotherSound_ButSoundsAlone()
        {
            GameFeel.Warm();
            Drain();
            float shown = Time.realtimeSinceStartup;
            GameFeel.LevelUp();                                                // el juego suena «subir de nivel» junto al aviso
            Assert.IsFalse(WoodCore.NoticeUnlessCovered(shown), "el aviso no se suma al sonido del juego");
            Assert.AreEqual(1, WoodCore.PlayCount);
            Assert.IsTrue(WoodCore.NoticeUnlessCovered(Time.realtimeSinceStartup + 1f), "sin otro sonido cerca, el aviso suena solo");
            Assert.AreEqual(2, WoodCore.PlayCount);
        }

        [Test]
        public void TheFinalSound_IsCounted_SoTheSmokeCanDemandExactlyOne()
        {
            GameFeel.Warm();
            Drain();
            GameFeel.Finish();
            Assert.AreEqual(1, WoodCore.FinishPlays);
            GameFeel.Finish();
            Assert.AreEqual(2, WoodCore.FinishPlays, "cada final suena y se cuenta: el smoke falla si en una partida hay más de uno");
        }

        [Test]
        public void TheStreakAndTheCount_AreClampedToTheirVariants()
        {
            Assert.AreEqual(1, WoodCore.Normalize(WoodSfx.Correct, 0));
            Assert.AreEqual(WoodCore.MaxCorrect, WoodCore.Normalize(WoodSfx.Correct, 40));
            Assert.AreEqual(3, WoodCore.Normalize(WoodSfx.Count, 9));
            Assert.AreEqual(0, WoodCore.Normalize(WoodSfx.Finish, 5));
        }
    }
}
