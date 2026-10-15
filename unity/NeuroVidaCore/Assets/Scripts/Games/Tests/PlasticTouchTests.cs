using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// <see cref="PlasticTouch"/> (regla del 14-oct: todo objeto de plasticina que se toca tiene su estado presionado): al aceptar un toque la losa pasa a su imagen presionada (con su huella) mientras el dedo siga encima y al menos 80 ms, el texto baja con ella,
    /// al soltar vuelve con un rebote chico (solo con movimiento) y cambiar el color de la losa conserva la presión. Sin el atlas no hay losa y no hay nada que mostrar. El tiempo se pasa a mano (en EditMode el reloj del juego no avanza).
    /// </summary>
    public sealed class PlasticTouchTests
    {
        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; GameFeel.ReduceMotion = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; GameFeel.ReduceMotion = false; }

        private static void Refresh(PlasticTouch t, float now) =>
            typeof(PlasticTouch).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(t, new object[] { now });

        private static void SetField(PlasticTouch t, string name, object value) =>
            typeof(PlasticTouch).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(t, value);

        private static GameObject Host(out Image image, out RectTransform label)
        {
            var go = new GameObject("TouchAudit", typeof(RectTransform), typeof(Image));
            image = go.GetComponent<Image>();
            var l = new GameObject("Label", typeof(RectTransform));
            l.transform.SetParent(go.transform, false);
            label = (RectTransform)l.transform;
            return go;
        }

        [Test]
        public void Press_ShowsThePressedSlabAndTheLabelDrops_AndAfterTheMinimumItGoesBack()
        {
            var go = Host(out var img, out var label);
            try
            {
                var slab = PlasticSlab.TryStyle(img, SlabSize.Button, SlabTone.Cream);
                Assert.IsNotNull(slab, "falta la losa crema del kit");
                var touch = PlasticTouch.On(go, slab, SlabSize.Button, SlabTone.Cream, label);
                Assert.IsFalse(touch.Down);
                Assert.AreEqual(PlasticSlab.Sprite(SlabSize.Button, SlabTone.Cream, false), slab.sprite);

                touch.Press();
                Assert.IsTrue(touch.Down, "al aceptar el toque se ve presionada");
                Assert.AreEqual(PlasticSlab.Sprite(SlabSize.Button, SlabTone.Cream, true), slab.sprite, "la losa presionada del kit");
                Assert.AreNotEqual(PlasticSlab.Sprite(SlabSize.Button, SlabTone.Cream, false), slab.sprite);
                Assert.Less(label.anchoredPosition.y, -0.5f, "el texto baja con la losa");
                Assert.Greater(touch.Drop, 0.5f);

                float later = GameClock.Time + PlasticTouch.MinSeconds + 0.05f;       // sin dedo y pasado el mínimo (80 ms): vuelve
                Refresh(touch, later);
                Assert.IsFalse(touch.Down);
                Assert.AreEqual(PlasticSlab.Sprite(SlabSize.Button, SlabTone.Cream, false), slab.sprite, "vuelve la losa de siempre");
                Assert.AreEqual(0f, label.anchoredPosition.y, 0.01f, "el texto vuelve a su lugar");
                Assert.AreEqual(0f, touch.Drop);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void AToneChange_KeepsThePressedLook_AndAQuickTapIsStillSeen()
        {
            var go = Host(out var img, out var label);
            try
            {
                var slab = PlasticSlab.TryStyle(img, SlabSize.Button, SlabTone.Cream);
                var touch = PlasticTouch.On(go, slab, SlabSize.Button, SlabTone.Cream, label);
                touch.Press();
                touch.SetTone(SlabTone.Lime);                                           // el juego pinta la opción de lima al acertar: sigue presionada un instante
                Assert.AreEqual(PlasticSlab.Sprite(SlabSize.Button, SlabTone.Lime, true), slab.sprite, "la losa lima presionada");
                Refresh(touch, GameClock.Time + 1f);
                Assert.AreEqual(PlasticSlab.Sprite(SlabSize.Button, SlabTone.Lime, false), slab.sprite, "y vuelve lima");
                Assert.AreEqual(0f, label.anchoredPosition.y, 0.01f, "el texto no queda corrido por el cambio de color");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void TheBounce_OnlyWithMotion_AndStateOnlyModeReportsDownAndDrop()
        {
            var go = Host(out var img, out _);
            try
            {
                var touch = PlasticTouch.On(go, SlabSize.Button, SlabTone.Night);       // modo 2: solo lleva la cuenta (el juego redibuja la losa en cada cuadro)
                Assert.IsFalse(touch.Down);
                touch.Press();
                Assert.IsTrue(touch.Down);
                Assert.Greater(touch.Drop, 0f, "cuánto baja el texto");
                Refresh(touch, GameClock.Time + 1f);
                Assert.IsFalse(touch.Down);
                Assert.AreEqual(0f, touch.Drop);

                SetField(touch, "_upAt", GameClock.Time - 0.05f);                        // soltó hace 50 ms: el rebote chico
                GameFeel.ReduceMotion = false;
                Assert.AreNotEqual(0f, touch.Bounce, "con movimiento hay un rebote chico de escala al soltar");
                Assert.Less(Mathf.Abs(touch.Bounce), 0.1f, "chico");
                GameFeel.ReduceMotion = true;
                Assert.AreEqual(0f, touch.Bounce, "con «quitar animaciones» solo cambia la imagen: sin rebote");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void WithoutThePlasticAtlas_ThereIsNoSlab_SoThereIsNothingToPress()
        {
            PlasticArt.Disabled = true;
            var go = Host(out var img, out var label);
            try
            {
                var slab = PlasticSlab.TryStyle(img, SlabSize.Button, SlabTone.Cream);
                Assert.IsNull(slab);
                Assert.IsNull(PlasticTouch.On(go, slab, SlabSize.Button, SlabTone.Cream, label), "sin losa el juego deja su respuesta de siempre");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void TheKitSlabsTheGamesTouch_AllHaveTheirPressedImage()
        {
            // las losas que los juegos tocan: crema, lima, coral, cielo, sol, noche y la apagada de las herramientas de Carga exacta (md); las chicas y la lila también
            foreach (var tone in new[] { SlabTone.Cream, SlabTone.Lime, SlabTone.Coral, SlabTone.Sky, SlabTone.Sun, SlabTone.Night, SlabTone.Dim })
            {
                var normal = PlasticSlab.Sprite(SlabSize.Button, tone, false);
                var pressed = PlasticSlab.Sprite(SlabSize.Button, tone, true);
                Assert.IsNotNull(normal, "falta la losa md " + tone);
                Assert.AreNotEqual(normal, pressed, "falta la imagen presionada de la losa md " + tone);
            }
        }
    }
}
