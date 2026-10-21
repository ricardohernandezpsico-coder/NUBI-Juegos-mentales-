using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// La cuenta regresiva con el mismo kit que el resto (Tarea 148, P9 de <c>auditoria-fable/propuestas-diseno-visual.md</c>): los numerales 3, 2 y 1 son de plasticina (atlas «comun») teñidos con el color de cada paso; el título es una losa mediana crema;
    /// bajo el anillo va el emblema del juego (el mismo de la cortina del final); y sin atlas todo cae al texto de siempre. Los tiempos no cambian (<see cref="CountdownStepsTests"/>).
    /// </summary>
    public class CountdownPlasticTests
    {
        private const float Dt = 0.05f;
        private readonly List<GameObject> _made = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            PlasticArt.Disabled = false;
            PlasticArt.Reset();
            GameClock.Reset();
            GameClock.SimulatedDeltaTime = Dt;
            CountdownScreen.Steps = 3;
            CountdownScreen.GameId = "";
        }

        [TearDown]
        public void TearDown()
        {
            PlasticArt.Disabled = false;
            PlasticArt.Reset();
            CountdownScreen.Steps = 3;
            CountdownScreen.GameId = "";
            GameFeel.ReduceMotion = false;
            GameClock.SimulatedDeltaTime = 0f;
            foreach (var go in _made) if (go != null) Object.DestroyImmediate(go);
            _made.Clear();
            GameClock.Reset();
        }

        private RectTransform NewArea()
        {
            var go = new GameObject("Area");
            _made.Add(go);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1080f, 2200f);
            return rect;
        }

        /// <summary>Corre la cuenta cuadro a cuadro y llama a <paramref name="perFrame"/> con la pantalla después de cada uno.</summary>
        private void Run(RectTransform area, System.Action<RectTransform> perFrame)
        {
            var cd = new CountdownScreen(area, 3f);
            var stack = new Stack<IEnumerator>();
            stack.Push(cd.Play("Un juego", "La consigna", null));
            int frames = 0;
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                if (!top.MoveNext()) { stack.Pop(); continue; }
                if (top.Current is IEnumerator inner) { stack.Push(inner); continue; }
                perFrame(area);
                if (++frames > 2000) Assert.Fail("la cuenta no termina");
            }
        }

        private static IEnumerable<Image> Numerals(RectTransform area)
        {
            foreach (var img in area.GetComponentsInChildren<Image>(true)) if (img.name == "Numeral" && img.gameObject.activeInHierarchy && img.enabled) yield return img;
        }

        [Test]
        public void TheAtlasHasTheThreeNumerals()
        {
            foreach (var n in new[] { "cuenta_3", "cuenta_2", "cuenta_1" })
            {
                var s = PlasticArt.Get("comun", n);
                Assert.IsNotNull(s, "falta " + n + " en el atlas «comun» (tools/juegos/comun.py --solo numerales; empaqueta.py comun)");
                Assert.AreEqual(s.rect.width, s.rect.height, 0.5f, n + " es cuadrado");
            }
        }

        [Test]
        public void TheNumbersThreeTwoOneArePlasticineTintedWithTheStepColor_AndYaStaysText()
        {
            var seen = new Dictionary<string, Color>();
            bool yaIsText = false;
            Run(NewArea(), area =>
            {
                foreach (var img in Numerals(area))
                    if (img.color.a > 0.95f && img.sprite != null) seen[img.sprite.name] = img.color;
                foreach (var t in area.GetComponentsInChildren<Text>(true))
                    if (t.text == "¡Ya!" && t.enabled && t.gameObject.activeInHierarchy) yaIsText = true;
            });
            CollectionAssert.AreEquivalent(new[] { "comun/cuenta_3", "comun/cuenta_2", "comun/cuenta_1" }, seen.Keys, "los tres numerales de plasticina, ninguno más");
            AssertSameRgb(NeuroStyle.Sky, seen["comun/cuenta_3"], "el 3, celeste");
            AssertSameRgb(NeuroStyle.Grape, seen["comun/cuenta_2"], "el 2, uva");
            AssertSameRgb(NeuroStyle.Coral, seen["comun/cuenta_1"], "el 1, coral");
            Assert.IsTrue(yaIsText, "«¡Ya!» es una palabra: sigue en Fredoka de arcilla");
        }

        private static void AssertSameRgb(Color expected, Color actual, string what)
        {
            Assert.AreEqual(expected.r, actual.r, 0.01f, what + " (rojo)");
            Assert.AreEqual(expected.g, actual.g, 0.01f, what + " (verde)");
            Assert.AreEqual(expected.b, actual.b, 0.01f, what + " (azul)");
        }

        [Test]
        public void WithoutThePlasticAtlasTheNumbersAreTheOldClayText()
        {
            PlasticArt.Disabled = true;
            int numerals = 0;
            var seenText = new List<string>();
            Run(NewArea(), area =>
            {
                numerals += System.Linq.Enumerable.Count(Numerals(area));
                foreach (var t in area.GetComponentsInChildren<Text>(true))
                    if (t.enabled && t.gameObject.activeInHierarchy && (t.text == "3" || t.text == "2" || t.text == "1") && !seenText.Contains(t.text)) seenText.Add(t.text);
            });
            Assert.AreEqual(0, numerals, "sin atlas no hay numerales de plasticina");
            CollectionAssert.AreEqual(new[] { "3", "2", "1" }, seenText, "y la cuenta de siempre sigue completa");
        }

        [Test]
        public void ReduceMotionKeepsTheSameNumerals()
        {
            GameFeel.ReduceMotion = true;
            var seen = new HashSet<string>();
            Run(NewArea(), area => { foreach (var img in Numerals(area)) if (img.color.a > 0.95f) seen.Add(img.sprite.name); });
            CollectionAssert.AreEquivalent(new[] { "comun/cuenta_3", "comun/cuenta_2", "comun/cuenta_1" }, seen);
        }

        [Test]
        public void TheGameEmblemShowsUnderTheRing_AndOnlyWhenThereIsOne()
        {
            CountdownScreen.GameId = "stroop";
            Sprite withGame = null; RectTransform rect = null;
            Run(NewArea(), area =>
            {
                var emb = area.Find("CountdownScreen/Emblem");
                if (emb == null) return;
                var img = emb.GetComponent<Image>();
                if (img.enabled && img.sprite != null) { withGame = img.sprite; rect = (RectTransform)emb; }
            });
            Assert.IsNotNull(withGame, "con un juego, su emblema se ve");
            Assert.AreEqual("emblemas/emb_stroop", withGame.name);
            Assert.Less(rect.anchoredPosition.y, 0f, "debajo del centro: bajo el anillo");
            CountdownScreen.GameId = "";
            bool shown = false;
            Run(NewArea(), area =>
            {
                var emb = area.Find("CountdownScreen/Emblem");
                if (emb != null && emb.GetComponent<Image>().enabled) shown = true;
            });
            Assert.IsFalse(shown, "sin juego (pruebas, juegos sin ficha) no hay emblema");
        }

        [Test]
        public void TheTitleIsAMediumCreamSlab()
        {
            var area = NewArea();
            var cd = new CountdownScreen(area, 3f);
            var chip = area.Find("CountdownScreen/TitleChip");
            Assert.IsNotNull(chip);
            var slab = chip.Find("Slab");
            Assert.IsNotNull(slab, "el título lleva una losa de plasticina (la hija «Slab», como las del marcador y la pausa)");
            var img = slab.GetComponent<Image>();
            Assert.IsNotNull(img.sprite, "con su imagen del atlas");
            Assert.AreEqual(Image.Type.Sliced, img.type, "estirable (nine-slice)");
        }
    }
}
