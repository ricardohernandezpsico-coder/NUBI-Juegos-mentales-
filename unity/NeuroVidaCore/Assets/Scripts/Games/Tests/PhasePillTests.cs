using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// La píldora de mensajes de Tinta o Palabra nace OCULTA (Tarea 178): hasta su primer mensaje no tiene texto, color ni tamaño propio (un objeto de la interfaz recién creado mide 100 × 100), y si se veía era un DISCO crema bajo la tarjeta durante todo el primer ensayo: el «círculo en blanco» que Ricardo vio.
    /// Esta prueba cubre lo que la guardia de pantalla no puede ver: lo que se ve es un HIJO (la losa y el punto) de un padre sin acomodar.
    /// </summary>
    public class PhasePillTests
    {
        private sealed class Runner : MonoBehaviour { }

        private GameObject _host;
        private PhasePill _pill;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("PillHost", typeof(RectTransform), typeof(Canvas));
            _pill = new PhasePill(_host.transform, _host.AddComponent<Runner>(), 3f, 22);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_host);

        [Test]
        public void ItIsBornHidden_SoItNeverShowsItsRawSlabAsADisc()
        {
            var group = _pill.Rect.GetComponent<CanvasGroup>();
            Assert.IsNotNull(group);
            Assert.AreEqual(0f, group.alpha, "hasta su primer mensaje no se ve");
        }

        [Test]
        public void ItIsBornWithItsMinimumSize_NotTheFactory100x100()
        {
            Assert.AreNotEqual(new Vector2(100f, 100f), _pill.Rect.sizeDelta);
            Assert.AreEqual(new Vector2(22f * 3f * 7f, 22f * 3f * 2.15f), _pill.Rect.sizeDelta, "el mínimo de Set: siete letras de ancho y dos letras y un poco de alto");
        }

        [Test]
        public void ClearHidesItAgain_SoANewMatchDoesNotShowTheLastMessage()
        {
            var group = _pill.Rect.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            _pill.Rect.localScale = Vector3.one * 0.8f;
            _pill.Clear();
            Assert.AreEqual(0f, group.alpha);
            Assert.AreEqual(Vector3.one, _pill.Rect.localScale);
        }
    }
}
