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
            _coach = NubiCoach.Create(_go.GetComponent<UnityEngine.RectTransform>(), p => false, () => false);
        }

        [TearDown]
        public void TearDown()
        {
            _coach.Hide();
            GameClock.Reset();
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
    }
}
