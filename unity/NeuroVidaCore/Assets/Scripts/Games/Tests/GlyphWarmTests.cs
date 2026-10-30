using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>Las letras listas antes del primer ensayo (Tarea 190d): el plan junta las letras por fuente y tamaño, el renglón de avisos del marcador recibe todo el alfabeto y lo que cada juego declara se suma.</summary>
    public class GlyphWarmTests
    {
        private static Text NewText(Transform parent, string name, string text, int size, Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.text = text;
            return t;
        }

        [Test]
        public void ThePlan_GroupsTheLettersByFontAndSize_DropsSpacesAndColorTags_AndAddsTheGamesOwnWords()
        {
            var root = new GameObject("GlyphRoot", typeof(Canvas));
            try
            {
                var bold = UiFonts.Bold;
                var a = NewText(root.transform, "A", "ROJO", 60, bold);
                var b = NewText(root.transform, "B", "<color=#fff>AZUL</color> uno", 60, bold);        // mismo tamaño y fuente que A: el mismo grupo
                var c = NewText(root.transform, "C", "x", 80, bold);                                    // otro tamaño: otro grupo
                var big = NewText(root.transform, "Word", "", 190, bold);
                var groups = GlyphWarm.Plan(new[] { a, b, c, big }, new[] { (big, string.Concat("ROJO", "AZUL", "AMARILLO", "BLANCO"), 0) });
                Assert.AreEqual(3, groups.Count, "tres tamaños distintos: 60, 80 y 190");
                var g60 = groups.Find(g => g.Sample == a);
                StringAssert.Contains("R", g60.Letters());
                StringAssert.Contains("Z", g60.Letters());
                StringAssert.DoesNotContain(" ", g60.Letters(), "los espacios no se piden");
                StringAssert.DoesNotContain("<", g60.Letters(), "las marcas de color no se piden");
                var g190 = groups.Find(g => g.Sample == big);
                foreach (char letter in "ROJAZULMILBNC") StringAssert.Contains(letter.ToString(), g190.Letters(), "las palabras de colores van en el grupo de 190");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void AnExtraWithAnExplicitSize_IsRequestedAtThatSize_NotAtTheSamplesOwn()
        {
            var root = new GameObject("GlyphSized", typeof(Canvas));
            try
            {
                var row = NewText(root.transform, "Text", "Consigna", 51, UiFonts.Regular);
                var plain = GlyphWarm.Plan(new[] { row }, null);
                var sized = GlyphWarm.Plan(new Text[0], new[] { (row, "Responde: TINTA", 60) });
                Assert.AreEqual(1, sized.Count);
                Assert.AreEqual(60, sized[0].Size, "pide el tamaño del juego (la consigna de Tinta va a 60 unidades)");
                Assert.AreNotEqual(plain[0].Pixels, sized[0].Pixels, "y no el de la muestra");
                StringAssert.Contains("T", sized[0].Letters());
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void TheHudPromptRow_GetsTheWholeAlphabet_BecauseAnyAnnouncementUsesItsSize()
        {
            var root = new GameObject("GlyphHud", typeof(Canvas));
            try
            {
                var hud = new GameObject("Hud"); hud.transform.SetParent(root.transform, false);
                var prompt = new GameObject("Prompt"); prompt.transform.SetParent(hud.transform, false);
                var row = NewText(prompt.transform, "Text", "Consigna", 48, UiFonts.Regular);
                var other = NewText(hud.transform, "Text", "Otro", 48, UiFonts.Regular);              // se llama igual pero su padre no es «Prompt»: no cuenta
                var pill = new GameObject("PhasePill"); pill.transform.SetParent(hud.transform, false);
                var pillText = NewText(pill.transform, "Text", "", 72, UiFonts.Bold);                   // la píldora nace vacía: su mensaje llega con la primera respuesta
                Assert.IsTrue(GlyphWarm.CarriesAnyMessage(row));
                Assert.IsTrue(GlyphWarm.CarriesAnyMessage(pillText));
                Assert.IsFalse(GlyphWarm.CarriesAnyMessage(other));
                var pillGroups = GlyphWarm.Plan(new[] { pillText }, null);
                Assert.AreEqual(1, pillGroups.Count, "la píldora vacía igual pide el alfabeto");
                StringAssert.Contains("B", pillGroups[0].Letters());
                var groups = GlyphWarm.Plan(new[] { row }, null);
                Assert.AreEqual(1, groups.Count);
                foreach (char letter in "AaZz\u00e1\u00f1\u00bf!9") StringAssert.Contains(letter.ToString(), groups[0].Letters());
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void TheRunRequestsEveryGroupWithinItsBudget_AndNeverTouchesTheTextsOwnGenerator()
        {
            var root = new GameObject("GlyphRun", typeof(Canvas));
            try
            {
                var t = NewText(root.transform, "T", "Hola", 40, UiFonts.Bold);
                var before = t.cachedTextGenerator.characterCount;
                var routine = GlyphWarm.Run(new[] { t }, null);
                int frames = 0;
                while (routine.MoveNext() && frames < 100) frames++;
                Assert.AreEqual(1, GlyphWarm.LastGroupCount);
                Assert.AreEqual(before, t.cachedTextGenerator.characterCount, "el generador del propio texto no se toca");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
