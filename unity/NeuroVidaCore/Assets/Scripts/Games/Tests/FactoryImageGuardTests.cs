using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// La guardia de la «imagen de fábrica» (Tarea 178) distingue lo que se ve con los valores con que nace un objeto de la interfaz (100 × 100, blanco, en el centro) de lo que el código ya acomodó: sin esta prueba la guardia del smoke podría no detectar NUNCA nada y dar un «todo bien» falso.
    /// </summary>
    public class FactoryImageGuardTests
    {
        private GameObject _root;

        private Image Make(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            return go.AddComponent<Image>();
        }

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("FactoryGuardRoot", typeof(RectTransform), typeof(Canvas));
            _root.GetComponent<RectTransform>().sizeDelta = new Vector2(1080f, 2400f);        // la raíz NO es de fábrica: lo que se prueba es cada hijo
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void An_image_just_created_is_a_factory_image()
        {
            var img = Make("Recien");
            Assert.IsTrue(FactoryImageGuard.IsFactory(img), "100 × 100, blanca, en el centro: es de fábrica");
            var hits = FactoryImageGuard.Scan();
            Assert.IsNotNull(hits);
            Assert.IsTrue(hits.Exists(h => h.Contains("FactoryGuardRoot/Recien")), "la ruta dice quién es: " + string.Join(", ", hits));
        }

        [Test]
        public void Setting_the_size_the_position_the_color_or_the_anchors_makes_it_a_normal_image()
        {
            var a = Make("ConTamano"); a.rectTransform.sizeDelta = new Vector2(42f, 42f);
            var b = Make("ConPosicion"); b.rectTransform.anchoredPosition = new Vector2(10f, -5f);
            var c = Make("ConColor"); c.color = new Color(0.9f, 0.7f, 0.1f, 1f);
            var d = Make("Estirada"); d.rectTransform.anchorMin = Vector2.zero; d.rectTransform.anchorMax = Vector2.one;
            var e = Make("Transparente"); e.color = new Color(1f, 1f, 1f, 0f);
            foreach (var img in new[] { a, b, c, d, e }) Assert.IsFalse(FactoryImageGuard.IsFactory(img), img.name);
        }

        [Test]
        public void The_real_pill_hierarchy_is_caught_by_the_stretched_child_rule_and_not_by_the_midpoint_one()
        {
            // CONTROL: la jerarquía de la píldora de Tinta o Palabra ANTES del arreglo: un padre de 100 × 100 sin acomodar pero YA POSICIONADO (SetPosition), con una losa estirada y un punto visibles
            var pill = Make("Pildora"); pill.color = new Color(1f, 1f, 1f, 0f);
            pill.rectTransform.anchoredPosition = new Vector2(0f, -163.7f);
            var slab = new GameObject("Losa", typeof(RectTransform)).AddComponent<Image>();
            slab.transform.SetParent(pill.transform, false);
            slab.rectTransform.anchorMin = Vector2.zero; slab.rectTransform.anchorMax = Vector2.one; slab.rectTransform.sizeDelta = new Vector2(58f, 69f);
            var r = FactoryImageGuard.Classify(slab);
            Assert.AreNotEqual(FactoryImageGuard.Rule.None, r & FactoryImageGuard.Rule.StretchedChild, "la losa estirada sobre un padre de 100 × 100 se ve");
            Assert.AreEqual(FactoryImageGuard.Rule.None, r & FactoryImageGuard.Rule.Midpoint, "el padre ya tiene posición: el punto medio (estado COMPLETO) no lo ve");
            Assert.AreEqual(FactoryImageGuard.Rule.None, r & FactoryImageGuard.Rule.Strict, "y la estricta tampoco: no es blanca de fábrica ella misma");
            pill.rectTransform.sizeDelta = new Vector2(420f, 64f);
            Assert.AreEqual(FactoryImageGuard.Rule.None, FactoryImageGuard.Classify(slab), "con el padre acomodado, nada");
        }

        [Test]
        public void A_positioned_container_with_children_placed_by_hand_is_not_a_stretched_child()
        {
            // los contenedores de 100 × 100 a propósito (la nave de Aterrizaje, la franja de Piloto…) tienen hijos con anclas propias NO estiradas: no son el error
            var holder = Make("Contenedor"); holder.color = new Color(1f, 1f, 1f, 0f); holder.rectTransform.anchoredPosition = new Vector2(120f, 300f);
            var child = new GameObject("Cuerpo", typeof(RectTransform)).AddComponent<Image>();
            child.transform.SetParent(holder.transform, false);
            child.rectTransform.sizeDelta = new Vector2(64f, 64f); child.color = Color.red;
            Assert.AreEqual(FactoryImageGuard.Rule.None, FactoryImageGuard.Classify(child));
        }

        [Test]
        public void An_inactive_image_or_one_in_an_invisible_group_is_not_seen()
        {
            var off = Make("Apagada"); off.gameObject.SetActive(false);
            Assert.IsFalse(FactoryImageGuard.IsFactory(off));
            var group = _root.AddComponent<CanvasGroup>();
            var hidden = Make("EnGrupoInvisible");
            group.alpha = 0f;
            Assert.IsFalse(FactoryImageGuard.IsFactory(hidden), "un grupo con alfa 0 no se ve: no es el disco");
        }
    }
}
