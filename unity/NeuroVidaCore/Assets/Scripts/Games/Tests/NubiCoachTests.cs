using NUnit.Framework;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>«Nubi entrenadora» (Games/Shared/NubiCoach.cs): el foco de «tocar» congela el juego y lo reanuda al cerrarse, «mirar» y el aviso no lo congelan, y cerrar de más no rompe nada.</summary>
    public class NubiCoachTests
    {
        private UnityEngine.GameObject _go;
        private NubiCoach _coach;

        [SetUp]
        public void SetUp()
        {
            GameClock.Reset();
            _go = new UnityEngine.GameObject("CoachParent", typeof(UnityEngine.RectTransform));
            _go.GetComponent<UnityEngine.RectTransform>().sizeDelta = new UnityEngine.Vector2(1000f, 2000f);
            _coach = NubiCoach.Create(_go.GetComponent<UnityEngine.RectTransform>(), p => false, () => false);
        }

        [TearDown]
        public void TearDown()
        {
            _coach.Hide();
            GameClock.Reset();
            GameClock.SimulatedDeltaTime = 0f;
            GuidedTutorial.EditorPressFrame = -1;
            NubiCoach.AuditEnabled = false;
            NubiCoach.AuditSteps.Clear();
            UnityEngine.Object.DestroyImmediate(_go);
        }

        private static UnityEngine.Rect SomeHole() => new UnityEngine.Rect(-100f, -100f, 200f, 200f);

        [Test]
        public void TouchFocus_FreezesTheGameClock_AndHideResumesIt()
        {
            Assert.IsFalse(_coach.Active);
            var touch = _coach.Touch(SomeHole, "Toca aquí");
            touch.MoveNext();                                    // abre el foco
            Assert.IsTrue(_coach.Active);
            Assert.IsTrue(GameClock.Paused, "el juego se congela mientras el foco espera el toque");
            _coach.Hide();
            Assert.IsFalse(_coach.Active);
            Assert.IsFalse(GameClock.Paused, "al cerrarse el foco el juego sigue");
        }

        // ------------------------------------------------------------------ el audio no se corta (Tarea 64)

        [Test]
        public void TouchFocus_FreezesTheClock_ButDoesNotSilenceTheAudio()
        {
            _coach.Touch(SomeHole, "Toca aquí").MoveNext();
            Assert.IsTrue(GameClock.Paused, "el reloj se detiene");
            Assert.IsFalse(UnityEngine.AudioListener.pause, "pero el audio sigue: un sonido que estaba sonando termina completo");
            Assert.IsFalse(GameClock.AudioSilenced);
            Assert.AreEqual(GameClock.FrozenLoopVolume, GameClock.LoopVolume, 1e-5f, "los sonidos en bucle bajan al 30 %, no se cortan");
            _coach.Hide();
            Assert.IsFalse(UnityEngine.AudioListener.pause, "al soltar todo queda como estaba");
            Assert.AreEqual(1f, GameClock.LoopVolume, 1e-5f, "los bucles vuelven a su volumen");
            Assert.AreEqual(1f, UnityEngine.Time.timeScale, 1e-5f);
        }

        [Test]
        public void TheMenuPause_SilencesAllTheAudio_AsBefore_AndResumeGivesEverythingBack()
        {
            GameClock.Pause();
            Assert.IsTrue(GameClock.Paused);
            Assert.IsTrue(UnityEngine.AudioListener.pause, "la pausa del menú calla todo");
            Assert.IsTrue(GameClock.AudioSilenced);
            Assert.AreEqual(1f, GameClock.LoopVolume, 1e-5f, "con el audio callado no se baja nada: al reanudar vuelven a su volumen");
            GameClock.Resume();
            Assert.IsFalse(GameClock.Paused);
            Assert.IsFalse(UnityEngine.AudioListener.pause);
            Assert.IsFalse(GameClock.AudioSilenced);
            Assert.AreEqual(1f, UnityEngine.Time.timeScale, 1e-5f);
        }

        [Test]
        public void AMenuPauseOverANubiFreeze_SilencesTheAudio_AndEverythingComesBackWhenItEnds()
        {
            _coach.Touch(SomeHole, "Toca aquí").MoveNext();
            Assert.IsFalse(UnityEngine.AudioListener.pause, "Nubi congela sin callar");
            GameClock.Pause();                                       // llega la pausa del menú encima
            Assert.IsTrue(UnityEngine.AudioListener.pause, "ahora sí se calla todo");
            Assert.IsTrue(GameClock.AudioSilenced);
            GameClock.Resume();                                      // el menú se cierra
            Assert.IsFalse(UnityEngine.AudioListener.pause);
            Assert.IsFalse(GameClock.Paused);
            _coach.Hide();
            Assert.IsFalse(UnityEngine.AudioListener.pause);
            Assert.IsFalse(GameClock.Paused);
            Assert.AreEqual(1f, UnityEngine.Time.timeScale, 1e-5f);
        }

        [Test]
        public void ResetLeavesTheAudioAndTheLoopsClean()
        {
            GameClock.Pause(silenceAudio: false);
            GameClock.Reset();
            Assert.IsFalse(GameClock.Paused);
            Assert.IsFalse(UnityEngine.AudioListener.pause);
            Assert.AreEqual(1f, GameClock.LoopVolume, 1e-5f);
        }

        [Test]
        public void WatchFocusAndNotice_NeverFreezeTheGame()
        {
            var watch = _coach.Watch(SomeHole, "Mira", () => false, 5f);
            watch.MoveNext();
            Assert.IsTrue(_coach.Active);
            Assert.IsFalse(GameClock.Paused, "mirar no congela");
            _coach.Hide();
            var notice = _coach.Notice("¡Eso es!");
            notice.MoveNext();
            Assert.IsTrue(_coach.Active);
            Assert.IsFalse(GameClock.Paused, "el aviso breve no congela");
            _coach.Hide();
        }

        [Test]
        public void ClosingTwiceOrOpeningOverAnotherFocus_LeavesTheClockRight()
        {
            _coach.Hide();
            _coach.Hide();
            Assert.IsFalse(GameClock.Paused);
            _coach.Touch(SomeHole, "uno").MoveNext();
            _coach.Touch(SomeHole, "dos").MoveNext();            // un foco nuevo reemplaza al anterior
            Assert.IsTrue(GameClock.Paused);
            _coach.Hide();
            Assert.IsFalse(GameClock.Paused, "no quedan dos pausas apiladas");
        }

        [Test]
        public void WithoutAFocus_NoTouchIsBlocked()
        {
            Assert.IsFalse(_coach.Blocks(new UnityEngine.Vector2(10f, 10f)));
        }

        [Test]
        public void TheSprites_ExistAndTheFrameIsNineSliced()
        {
            Assert.IsNotNull(CoachSprites.Corner());
            var frame = CoachSprites.Frame();
            Assert.IsNotNull(frame);
            Assert.Greater(frame.border.x, 0f, "el marco se estira con cortes");
        }

        // ------------------------------------------------------------------ red de seguridad: ningún paso de Tocar deja a nadie atrapado (tarea 42, 6-oct)

        private static void TickOf(NubiCoach coach) =>
            typeof(NubiCoach).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(coach, null);

        private void PressAt(float x, float y)
        {
            GuidedTutorial.EditorPressPos = new UnityEngine.Vector2(x, y);
            GuidedTutorial.EditorPressFrame = UnityEngine.Time.frameCount;
            TickOf(_coach);
        }

        private void OpenTouchStep()
        {
            GameClock.SimulatedDeltaTime = 0.2f;                  // cada cuadro suma 0,2 s de espera (el toque vale pasados 0,15 s)
            _coach.Touch(SomeHole, "Toca aquí").MoveNext();
            TickOf(_coach);
        }

        [Test]
        public void ATouchInsideTheHole_ClosesTheStep_AndTheGameReceivesIt()
        {
            OpenTouchStep();
            PressAt(0f, 0f);
            Assert.IsFalse(_coach.Active);
            Assert.IsFalse(_coach.Blocks(new UnityEngine.Vector2(0f, 0f)), "el juego recibe ese toque");
        }

        [Test]
        public void ATouchNearTheHoleEdge_Within40dp_CountsAsTheRequestedTouch()
        {
            OpenTouchStep();
            PressAt(200f, 0f);                                    // el hueco llega a x = 112 (con su margen): 88 unidades fuera, menos de 120 (40 dp)
            Assert.IsFalse(_coach.Active);
            Assert.IsFalse(_coach.Blocks(new UnityEngine.Vector2(200f, 0f)), "un toque casi encima del hueco también llega al juego");
        }

        [Test]
        public void TheFirstTouchFarFromTheHole_IsIgnored_AndTheSecondAdvancesWithoutReachingTheGame()
        {
            OpenTouchStep();
            PressAt(450f, 800f);
            Assert.IsTrue(_coach.Active, "un toque lejos del hueco, una sola vez, no avanza");
            PressAt(-400f, -700f);
            Assert.IsFalse(_coach.Active, "el segundo toque fuera avanza: nadie queda atrapado");
            Assert.IsTrue(_coach.Blocks(new UnityEngine.Vector2(-400f, -700f)), "ese toque no llega al juego (no pidió nada de eso)");
        }

        [Test]
        public void After10Seconds_AnyTouchAdvancesTheStep()
        {
            GameClock.SimulatedDeltaTime = 11f;
            _coach.Touch(SomeHole, "Toca aquí").MoveNext();
            TickOf(_coach);                                       // pasan 11 s sin un toque válido
            PressAt(450f, 800f);
            Assert.IsFalse(_coach.Active);
        }

        [Test]
        public void TheSkipButtonTouch_NeverAdvancesTheStep()
        {
            var go = new UnityEngine.GameObject("CoachParent2", typeof(UnityEngine.RectTransform));
            go.GetComponent<UnityEngine.RectTransform>().sizeDelta = new UnityEngine.Vector2(1000f, 2000f);
            var coach = NubiCoach.Create(go.GetComponent<UnityEngine.RectTransform>(), p => true, () => false);       // todo toque cae en «Saltar tutorial»
            GameClock.SimulatedDeltaTime = 0.2f;
            coach.Touch(SomeHole, "Toca aquí").MoveNext();
            TickOf(coach);
            GuidedTutorial.EditorPressPos = UnityEngine.Vector2.zero;
            GuidedTutorial.EditorPressFrame = UnityEngine.Time.frameCount;
            TickOf(coach);
            Assert.IsTrue(coach.Active, "los toques en «Saltar tutorial» los maneja el juego, no avanzan el foco");
            coach.Hide();
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TheHelpFinger_RestsOutsideTheHole_NeverOnItsContent()
        {
            NubiCoach.AuditEnabled = true;
            NubiCoach.AuditSteps.Clear();
            var tall = new UnityEngine.Rect(-400f, -300f, 800f, 600f);          // una tarjeta grande con texto adentro
            _coach.Touch(() => tall, "Toca la tarjeta").MoveNext();
            _coach.Hide();
            var st = NubiCoach.AuditSteps[NubiCoach.AuditSteps.Count - 1];
            Assert.IsTrue(st.HasHole);
            Assert.AreEqual(0f, CoachLayout.Overlap(st.Finger, st.Hole), 0.5f, "el dedo no pisa el hueco (antes caía en el centro y tapaba las letras)");
            Assert.Greater(st.Finger.height, 100f);
        }
    }
}
