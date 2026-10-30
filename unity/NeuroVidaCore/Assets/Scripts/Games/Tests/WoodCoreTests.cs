using System;
using System.Collections.Generic;
using System.Threading;
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

        /// <summary>Espera a que los hilos de trabajo terminen y el hilo principal cree todos los clips (<see cref="WoodCore.Drain"/>).</summary>
        private static int Drain() => WoodCore.Drain();

        private static GameObject Speakers() => GameObject.Find("WoodCore");

        [Test]
        public void Warm_CalculatesNothingOnTheMainThread_TheGoSoundGoesFirst_AndEverythingIsThereWhenItIsReady()
        {
            Assert.AreEqual(1, WoodCore.EarlySet.Length, "la cuenta regresiva se quitó (Tarea 190): solo queda el «¡Ya!»");
            Assert.AreEqual((WoodSfx.Go, 0), WoodCore.EarlySet[0]);
            GameFeel.Warm();
            Assert.AreEqual(0, WoodCore.BakeCount, "Warm no calcula ni crea nada: todo queda en la cola para los hilos de trabajo");
            Assert.IsFalse(WoodCore.Idle);
            Assert.IsFalse(WoodCore.Ready, "todavía no se puede mostrar el tablero");
            Assert.IsNull(WoodCore.Get(WoodSfx.Finish), "el final todavía no");
            Drain();
            Assert.IsTrue(WoodCore.Ready);
            Assert.AreEqual(WoodCore.EarlySet.Length + WoodCore.CoreSet.Length, WoodCore.BakeCount);
            foreach (var (sfx, arg) in WoodCore.EarlySet) Assert.IsNotNull(WoodCore.Get(sfx, arg), "el «¡Ya!»: " + sfx + arg);
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
            GameFeel.Go(); GameFeel.PauseIn(); GameFeel.PauseOut();
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

        // ------------------------------------------------------------------ lo extra de cada juego (paso 0 de las Fases C a E, Tarea 133)

        private static WoodSounds.Recipe OwnRecipe() => new WoodSounds.Recipe(WoodKit.Woodblock(1000), 0.1, 0.1, WoodTier.Micro);

        [Test]
        public void TheRequestedExtras_AreBakedBehindTheCore_AndAllOfThemAreThereWhenReady()
        {
            GameFeel.Warm();
            WoodCore.Request(WoodSfx.Select, 3);
            WoodCore.RequestNote(392.0, WoodVoice.Kalimba, 0.2);
            WoodCore.RequestCue(784.0, 0.06);
            WoodCore.Request("test.propio", OwnRecipe, true);
            Assert.IsFalse(WoodCore.Idle);
            Assert.IsNull(WoodCore.Get(WoodSfx.Select, 3), "pedir no calcula nada: lo calculan los hilos de trabajo");
            Drain();
            Assert.IsNotNull(WoodCore.Get(WoodSfx.Select, 3));
            Assert.IsNotNull(WoodCore.GetExtra(WoodCore.NoteKey(392.0, WoodVoice.Kalimba, 0.2)));
            Assert.IsNotNull(WoodCore.GetExtra(WoodCore.CueKey(784.0, 0.06)));
            Assert.IsNotNull(WoodCore.GetExtra("test.propio"));
            Assert.AreEqual(4, WoodCore.ExtraCount);
            Assert.AreEqual(WoodCore.EarlySet.Length + WoodCore.CoreSet.Length + 4, WoodCore.BakeCount);
            Assert.IsTrue(WoodCore.Play(WoodSfx.Select, 3) && WoodCore.Play("test.propio") && WoodCore.PlayNote(392.0, WoodVoice.Kalimba, 0.2) && WoodCore.PlayCue(784.0, 0.06), "ya listos, suenan");
            Assert.AreEqual(0, WoodCore.Skipped);
            Assert.AreEqual(0, WoodCore.LateBakes, "pedidos antes: nada se calcula tarde");
        }

        private static int _recipeThread = -1;

        [Test]
        public void TheSamplesAreCalculatedOnAWorkerThread_NotOnTheMainOne()
        {
            int main = Thread.CurrentThread.ManagedThreadId;
            _recipeThread = -1;
            GameFeel.Warm();
            WoodCore.Request("test.hilo", () => { _recipeThread = Thread.CurrentThread.ManagedThreadId; return OwnRecipe(); }, true);
            Drain();
            Assert.AreNotEqual(-1, _recipeThread, "la receta se calculó");
            Assert.AreNotEqual(main, _recipeThread, "la receta corrió en un hilo de trabajo, no en el principal");
            Assert.IsNotNull(WoodCore.GetExtra("test.hilo"));
        }

        [Test]
        public void TheMainThreadSpendsAtMostTheBudgetCreatingClips_AndTheWorkersKeepGoing()
        {
            GameFeel.Warm();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            WoodCore.Step(WoodCore.FrameBudgetMs);                                                 // un «cuadro»: reparte el trabajo y crea lo que ya terminó (nada, recién empezó)
            Assert.Less(watch.Elapsed.TotalMilliseconds, 50.0, "un cuadro de reparto no calcula nada: no se queda esperando a los hilos");
            Assert.IsFalse(WoodCore.Idle, "los hilos siguen calculando");
            Drain();
            Assert.IsTrue(WoodCore.Idle);
        }

        [Test]
        public void TheSamplesFromTheWorkerAreIdenticalToTheDirectRenderAndTheClipsHoldThem()
        {
            var cases = new List<(WoodSfx Sfx, int Arg)> { (WoodSfx.Go, 0), (WoodSfx.Correct, 1), (WoodSfx.Correct, 7), (WoodSfx.Wrong, 0), (WoodSfx.Finish, 0), (WoodSfx.Tap, 0), (WoodSfx.Tick, 0), (WoodSfx.LevelUp, 0), (WoodSfx.Notice, 0), (WoodSfx.PauseIn, 0) };
            foreach (var (sfx, arg) in cases)
            {
                WoodSounds.RenderForClip(sfx, arg, out var wantL, out var wantR);
                float[] gotL = null, gotR = null;
                var worker = new Thread(() => new WoodSounds.BakeJob(sfx, arg).ComputeSamples(out gotL, out gotR));
                worker.Start();
                worker.Join();
                CollectionAssert.AreEqual(wantL, gotL, sfx + "" + arg + ": el canal izquierdo sale idéntico desde un hilo");
                if (wantR == null) Assert.IsNull(gotR, sfx + "" + arg + ": mono"); else CollectionAssert.AreEqual(wantR, gotR, sfx + "" + arg + ": el canal derecho sale idéntico desde un hilo");
            }
            // y el clip que crea el hilo principal guarda esas mismas muestras
            GameFeel.Warm();
            Drain();
            foreach (var (sfx, arg) in new[] { (WoodSfx.Correct, 1), (WoodSfx.Finish, 0), (WoodSfx.Tap, 0) })
            {
                WoodSounds.RenderForClip(sfx, arg, out var l, out var r);
                var clip = WoodCore.Get(sfx, arg);
                Assert.IsNotNull(clip);
                int channels = r == null ? 1 : 2;
                Assert.AreEqual(channels, clip.channels);
                Assert.AreEqual(l.Length, clip.samples);
                var data = new float[l.Length * channels];
                clip.GetData(data, 0);
                for (int i = 0; i < l.Length; i += 97)
                {
                    Assert.AreEqual(l[i], data[i * channels], 1e-6f, sfx + "" + arg + " muestra " + i);
                    if (r != null) Assert.AreEqual(r[i], data[i * 2 + 1], 1e-6f, sfx + "" + arg + " muestra " + i + " (derecha)");
                }
            }
        }

        [Test]
        public void ReleasingTheExtrasWhileTheyAreBeingCalculated_DiscardsTheResult()
        {
            GameFeel.Warm("juegoA");
            WoodCore.Request("test.a", OwnRecipe, true);
            WoodCore.Step(0f);                                                                     // reparte a los hilos
            GameFeel.Warm("juegoB");                                                               // cambia de juego: suelta lo extra del anterior
            Drain();
            Assert.IsNull(WoodCore.GetExtra("test.a"), "lo que un hilo calculaba del juego anterior no se guarda");
            Assert.AreEqual(0, WoodCore.ExtraCount);
            Assert.IsTrue(WoodCore.Idle);
        }

        [Test]
        public void ResetWhileSomethingIsBeingCalculated_LeavesNoStrayClips()
        {
            GameFeel.Warm();
            WoodCore.Step(0f);
            WoodCore.Reset();
            Thread.Sleep(300);                                                                     // los hilos terminan lo que ya tenían en la mano
            WoodCore.Step(1000f);
            Assert.AreEqual(0, WoodCore.ClipCount, "nada de la generación anterior vuelve a aparecer");
            Assert.AreEqual(0, WoodCore.BakeCount);
            Assert.IsTrue(WoodCore.Idle);
        }

        [Test]
        public void AnExtraThatIsNotReady_IsSkipped_NeverBakedInTheMiddleOfTheGame()
        {
            GameFeel.Warm();
            Drain();
            int baked = WoodCore.BakeCount;
            Assert.IsFalse(WoodCore.Play("nadie.lo.pidio"), "un propio que no se pidió no suena");
            Assert.IsFalse(WoodCore.Play(WoodSfx.Select, 3), "una variante de la paleta que no se pidió tampoco");
            Assert.AreEqual(2, WoodCore.Skipped);
            Assert.AreEqual(baked, WoodCore.BakeCount, "y no se calcula en ese momento (sería un tirón por un adorno)");
            Assert.AreEqual(0, WoodCore.PlayCount);
            WoodCore.Request(WoodSfx.Select, 3);                                                   // pedirlo y esperar: suena
            Drain();
            Assert.IsTrue(WoodCore.Play(WoodSfx.Select, 3));
            WoodCore.Request(WoodSfx.Correct, 2);                                                  // una del núcleo no se pide dos veces
            Assert.IsTrue(WoodCore.Idle);
        }

        [Test]
        public void ANoteOrACueThatIsNotReady_SoundsAnyway_AndCountsALateBake()
        {
            GameFeel.Warm();
            Drain();
            int baked = WoodCore.BakeCount;
            Assert.IsTrue(WoodCore.PlayNote(659.25, WoodVoice.Kalimba), "la información no se pierde: suena");
            Assert.AreEqual(1, WoodCore.LateBakes);
            Assert.AreEqual(baked + 1, WoodCore.BakeCount, "se calculó en el momento");
            Assert.IsTrue(WoodCore.PlayNote(659.25, WoodVoice.Kalimba));
            Assert.AreEqual(1, WoodCore.LateBakes, "la segunda ya estaba lista");
            WoodCore.RequestNote(783.99, WoodVoice.Marimba);
            Drain();
            Assert.IsTrue(WoodCore.PlayNote(783.99, WoodVoice.Marimba));
            Assert.AreEqual(1, WoodCore.LateBakes, "una nota pedida antes no se calcula tarde");
            Assert.IsTrue(WoodCore.PlayCue(784.0, 0.1));
            Assert.AreEqual(2, WoodCore.LateBakes, "una señal que nadie pidió también cuenta");
            Assert.AreEqual(0, WoodCore.Skipped);
            WoodCore.ResetCounters();
            Assert.AreEqual(0, WoodCore.LateBakes, "el smoke pone en cero lo que se cuenta por partida");
        }

        [Test]
        public void ADestroyedExtra_IsRequestedAndBakedAgain_NotTakenAsPresent()
        {
            // Unity destruye los objetos creados en Play al salir de él cuando la recarga de dominio está apagada (la cadena del smoke): el diccionario guarda el clip «nulo» y un pedido nuevo tenía que recalcularlo
            // (si se daba por presente, la nota se calculaba tarde y los demás extras se omitían durante toda la partida siguiente del mismo juego).
            GameFeel.Warm("juego-a");
            Drain();
            WoodCore.RequestNote(523.25, WoodVoice.Kalimba);
            WoodCore.Request(WoodSfx.Spark, 4);
            WoodCore.Request("test.propio", OwnRecipe, true);
            Drain();
            var key = WoodCore.NoteKey(523.25, WoodVoice.Kalimba);
            Assert.IsNotNull(WoodCore.GetExtra(key));
            foreach (var k in new[] { key, WoodSounds.KeyOf(WoodSfx.Spark, 4), "test.propio" }) UnityEngine.Object.DestroyImmediate(WoodCore.GetExtra(k));
            Assert.IsTrue(WoodCore.GetExtra(key) == null, "el clip quedó destruido (nulo de Unity)");
            WoodCore.RequestNote(523.25, WoodVoice.Kalimba);
            WoodCore.Request(WoodSfx.Spark, 4);
            WoodCore.Request("test.propio", OwnRecipe, true);
            Assert.IsFalse(WoodCore.Idle, "los tres se volvieron a poner en la cola");
            Drain();
            Assert.IsNotNull(WoodCore.GetExtra(key), "la nota se calculó de nuevo");
            Assert.IsNotNull(WoodCore.Get(WoodSfx.Spark, 4), "y el destello");
            Assert.IsNotNull(WoodCore.GetExtra("test.propio"), "y el propio");
            WoodCore.ResetCounters();
            Assert.IsTrue(WoodCore.PlayNote(523.25, WoodVoice.Kalimba));
            Assert.AreEqual(0, WoodCore.LateBakes, "ya no se calcula tarde");
            Assert.AreEqual(3, WoodCore.ExtraCount, "sin entradas repetidas");
        }

        [Test]
        public void ReleaseExtras_ReleasesOnlyWhatIsExtra_TheCoreKeepsItsClips()
        {
            GameFeel.Warm("juego-a");
            Drain();
            var finish = WoodCore.Get(WoodSfx.Finish);
            int core = WoodCore.ClipCount;
            long coreBytes = WoodCore.StoredBytes();
            WoodCore.Request(WoodSfx.Spark, 4);
            WoodCore.RequestNote(523.25, WoodVoice.Kalimba);
            WoodCore.Request("test.propio", OwnRecipe, true);
            Drain();
            Assert.AreEqual(3, WoodCore.ExtraCount);
            Assert.AreEqual(coreBytes, WoodCore.StoredBytes(), "lo extra no cuenta como núcleo");
            WoodCore.Request(WoodSfx.Spark, 5);                                                    // en la cola cuando se suelta
            WoodCore.ReleaseExtras();
            Assert.AreEqual(0, WoodCore.ExtraCount);
            Assert.AreEqual(0L, WoodCore.ExtraBytes());
            Assert.IsTrue(WoodCore.Idle, "lo extra que esperaba en la cola también se descarta");
            Assert.AreEqual(core, WoodCore.ClipCount);
            Assert.AreSame(finish, WoodCore.Get(WoodSfx.Finish), "el núcleo conserva sus clips (la misma referencia)");
            Assert.IsNull(WoodCore.Get(WoodSfx.Spark, 4));
            // y al cambiar de juego, Warm con el id del juego nuevo suelta lo del anterior; con el mismo juego lo conserva
            WoodCore.Request(WoodSfx.Spark, 4); Drain();
            GameFeel.Warm("juego-a");
            Assert.AreEqual(1, WoodCore.ExtraCount, "el mismo juego conserva lo suyo");
            GameFeel.Warm("juego-b");
            Assert.AreEqual(0, WoodCore.ExtraCount, "otro juego: se suelta lo del anterior");
            Assert.AreSame(finish, WoodCore.Get(WoodSfx.Finish));
        }

        [Test]
        public void TheGain_IsOnlyZeroMinus3Minus6OrMinus9_NeverLouderThanTheRecipe()
        {
            GameFeel.Warm();
            Drain();
            WoodCore.Request("test.propio", OwnRecipe, true);
            WoodCore.RequestNote(523.25, WoodVoice.Kalimba);
            Drain();
            foreach (float ok in new[] { 0f, -3f, -6f, -9f })
            {
                Assert.IsTrue(WoodCore.Play(WoodSfx.Correct, 1, false, ok), "gainDb " + ok);
                Assert.IsTrue(WoodCore.Play("test.propio", ok));
                Assert.IsTrue(WoodCore.PlayNote(523.25, WoodVoice.Kalimba, 0.3, ok));
            }
            foreach (float bad in new[] { 3f, 1f, -1f, -4.5f, -12f })
            {
                Assert.Throws<ArgumentException>(() => WoodCore.Play(WoodSfx.Correct, 1, false, bad), "gainDb " + bad);
                Assert.Throws<ArgumentException>(() => WoodCore.Play("test.propio", bad));
                Assert.Throws<ArgumentException>(() => WoodCore.PlayNote(523.25, WoodVoice.Kalimba, 0.3, bad));
                Assert.Throws<ArgumentException>(() => WoodCore.PlayCue(784.0, 0.1, bad));
            }
        }

        [Test]
        public void ExtraBytes_CountsOnlyWhatIsExtra()
        {
            GameFeel.Warm();
            Drain();
            Assert.AreEqual(0L, WoodCore.ExtraBytes());
            WoodCore.Request(WoodSfx.Perfect);
            WoodCore.RequestNote(392.0, WoodVoice.Kalimba);
            Drain();
            Assert.Greater(WoodCore.ExtraBytes(), 0L);
            Assert.AreEqual(WoodCore.ExtraBytes(), Bytes(WoodCore.Get(WoodSfx.Perfect)) + Bytes(WoodCore.GetExtra(WoodCore.NoteKey(392.0, WoodVoice.Kalimba))));
            Assert.LessOrEqual(WoodCore.StoredBytes(), 6L * 1024 * 1024, "el núcleo sigue pesando lo mismo");
        }

        private static long Bytes(AudioClip c) => (long)c.samples * c.channels * 4;

        [Test]
        public void WithTheSoundOff_RequestingExtras_DoesNothing_Mudo()
        {
            GameFeel.SoundOn = false;
            GameFeel.Warm("juego-a");
            WoodCore.Request(WoodSfx.Select, 3);
            WoodCore.RequestNote(392.0, WoodVoice.Kalimba);
            WoodCore.RequestCue(784.0, 0.1);
            WoodCore.Request("test.propio", OwnRecipe, true);
            Assert.IsTrue(WoodCore.Idle, "no se encola nada");
            Assert.IsFalse(WoodCore.Play(WoodSfx.Select, 3) || WoodCore.Play("test.propio") || WoodCore.PlayNote(392.0, WoodVoice.Kalimba) || WoodCore.PlayCue(784.0, 0.1));
            Assert.AreEqual(0, WoodCore.BakeCount);
            Assert.AreEqual(0, WoodCore.LateBakes);
            Assert.AreEqual(0, WoodCore.ExtraCount);
            Assert.IsNull(Speakers(), "ni siquiera se crean los altavoces");
        }

        [Test]
        public void TheBudget_IsTwoMillisecondsForTheMainThread_AndThirtyWhileTheLoadingScreenCovers()
        {
            Assert.AreEqual(2f, WoodCore.FrameBudgetMs);
            Assert.AreEqual(WoodCore.FrameBudgetMs, WoodCore.CurrentBudgetMs);
            WoodCore.Loading = true;
            Assert.AreEqual(WoodCore.LoadingBudgetMs, WoodCore.CurrentBudgetMs);
            Assert.AreEqual(30f, WoodCore.LoadingBudgetMs);
            GameFeel.Warm();                                                                       // una partida nueva empieza antes de que su tablero espere
            Assert.IsFalse(WoodCore.Loading);
            Assert.AreEqual(2f, WoodCore.CurrentBudgetMs);
        }

        [Test]
        public void TheStreak_IsClampedToItsVariants()
        {
            Assert.AreEqual(1, WoodCore.Normalize(WoodSfx.Correct, 0));
            Assert.AreEqual(WoodCore.MaxCorrect, WoodCore.Normalize(WoodSfx.Correct, 40));
            Assert.AreEqual(0, WoodCore.Normalize(WoodSfx.Finish, 5));
        }
    }
}
