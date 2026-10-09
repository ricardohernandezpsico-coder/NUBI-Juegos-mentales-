using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// La colocación de «Nubi entrenadora» (Games/Shared/CoachLayout.cs): el globo crece con el texto (máximo 3 líneas, letra 54), Nubi y el globo no tapan el hueco ni las zonas
    /// protegidas ni los textos del juego en las tres proporciones de pantalla (20:9 como el teléfono de Ricardo, 18:9 y 16:9), y el velo cubre todo menos lo iluminado.
    /// </summary>
    public class CoachLayoutTests
    {
        private static readonly (string Name, float Height)[] Screens = { ("20:9", 2400f), ("18:9", 2160f), ("16:9", 1920f) };

        private static Rect Screen(float h) => new Rect(-540f, -h / 2f, 1080f, h);

        // ------------------------------------------------------------------ el texto

        [Test]
        public void TheBubbleKeepsTheBaseLetterSize()
        {
            Assert.AreEqual(54, CoachLayout.FontSize);
            Assert.AreEqual(3, CoachLayout.MaxLines);
        }

        [Test]
        public void TheBubbleGrowsWithTheLines()
        {
            var one = CoachText.Measure("Toca la celda", 600f);
            var three = CoachText.Measure("Toca la celda que se enciende y luego toca el botón de la operación que quieres", 600f);
            Assert.AreEqual(1, one.Lines);
            Assert.GreaterOrEqual(three.Lines, 3);
            Assert.Greater(three.Height, one.Height * 2f, "tres líneas miden más del doble que una");
            var placeOne = CoachLayout.Place(Screen(2400f), null, new List<Rect>(), new List<Rect>(), "Toca la celda");
            var placeThree = CoachLayout.Place(Screen(2400f), null, new List<Rect>(), new List<Rect>(), "Toca la celda que se enciende y luego toca el botón de la operación que quieres");
            Assert.Greater(placeThree.Bubble.height, placeOne.Bubble.height, "el globo de más líneas es más alto");
        }

        [Test]
        public void ATextThatDoesNotFitInThreeLines_IsFlagged_NotShrunk()
        {
            string longText = "Esta es una frase demasiado larga para un globo que solo puede tener tres líneas de letra grande, así que debe acortarse";
            var p = CoachLayout.Place(Screen(2400f), null, new List<Rect>(), new List<Rect>(), longText);
            Assert.IsTrue(p.TooLong, "se marca para acortar el texto");
            Assert.IsFalse(p.Clean);
        }

        [Test]
        public void EveryTutorialText_FitsInThreeLinesAtTheBaseLetterSize()
        {
            int n = 0;
            foreach (var (game, step, text) in CoachTexts.All())
            {
                n++;
                var m = CoachText.Measure(text, 690f - 2f * CoachLayout.PadX);
                Assert.LessOrEqual(m.Lines, CoachLayout.MaxLines, $"{game} · {step}: «{text}» ocupa {m.Lines} líneas: hay que acortar el texto (la letra no se achica)");
                foreach (var (name, h) in Screens)
                {
                    var p = CoachLayout.Place(Screen(h), null, new List<Rect>(), new List<Rect>(), text);
                    Assert.IsFalse(p.TooLong, $"{game} · {step} en {name}");
                    Assert.LessOrEqual(p.Lines, CoachLayout.MaxLines);
                }
            }
            Assert.Greater(n, 40, "la lista de textos del tutorial está completa");
        }

        /// <summary>El vocabulario que no va en nada visible (el mismo de <c>VisibleTextsGuardTest</c> de la app): ni promesas de salud ni «cognitivo», «entrenamiento», «cerebro» o percentiles.</summary>
        private static readonly string[] BannedWords =
            { "cognitiv", "entren", "estimulaci", "bienestar", "percentil", "cerebro", "neurona", "mejora tu", "fortalece", "previene" };

        [Test]
        public void TheTutorialTexts_AreWrittenToTheRules()
        {
            foreach (var (game, step, text) in CoachTexts.All())
            {
                foreach (var word in BannedWords)
                    Assert.IsFalse(text.ToLowerInvariant().Contains(word), $"{game} · {step}: «{text}» usa «{word}» (vocabulario que no va: juego, partida, camino, tu avance, áreas, mente activa)");
                Assert.IsFalse(text.EndsWith("."), $"{game} · {step}: sin punto final en el globo");
                int sentences = text.Split(new[] { ". " }, System.StringSplitOptions.RemoveEmptyEntries).Length;
                Assert.LessOrEqual(sentences, 2, $"{game} · {step}: una idea por globo");
                Assert.LessOrEqual(text.Length, 62, $"{game} · {step}: «{text}» es muy largo");
            }
        }

        // ------------------------------------------------------------------ el lugar

        [Test]
        public void NubiAndTheBubble_NeverCoverTheHoleOrTheProtectedZones_InThreeScreenShapes()
        {
            string[] texts = { "Toca la celda", "Si ya la sabes, toca ¡La tengo!", "Arrastra la nave hasta el 40% y suelta" };
            int cases = 0;
            foreach (var (name, h) in Screens)
            {
                var screen = Screen(h);
                // huecos de varios tamaños en una cuadrícula de posiciones, con una zona protegida (la pregunta) arriba del hueco o abajo
                foreach (float fx in new[] { -0.3f, 0f, 0.3f })
                    foreach (float fy in new[] { -0.28f, -0.1f, 0.05f, 0.2f })
                        foreach (var size in new[] { new Vector2(240f, 240f), new Vector2(900f, 300f) })
                            foreach (var text in texts)
                            {
                                var hole = new Rect(fx * screen.width - size.x / 2f, fy * screen.height - size.y / 2f, size.x, size.y);
                                var question = new Rect(-420f, screen.yMax - 520f, 840f, 160f);        // la pregunta, bajo el marcador
                                var hard = new List<Rect> { CoachLayout.Grow(hole, 24f), CoachLayout.Grow(question, 20f) };
                                var p = CoachLayout.Place(screen, hole.center, hard, new List<Rect>(), text);
                                cases++;
                                string where = $"{name} hueco {hole} texto «{text}»";
                                Assert.IsTrue(p.Clean, "no hay lugar limpio: " + where + " solapa " + p.Overlap);
                                foreach (var z in new[] { hole, question })
                                {
                                    Assert.AreEqual(0f, CoachLayout.Overlap(p.Nubi, z), 0.5f, "Nubi tapa: " + where);
                                    Assert.AreEqual(0f, CoachLayout.Overlap(p.Bubble, z), 0.5f, "el globo tapa: " + where);
                                }
                                Assert.LessOrEqual(p.Lines, CoachLayout.MaxLines, where);
                                Assert.GreaterOrEqual(p.Bubble.xMin, screen.xMin, where);
                                Assert.LessOrEqual(p.Bubble.xMax, screen.xMax, where);
                                Assert.GreaterOrEqual(p.Nubi.xMin, screen.xMin, where);
                                Assert.LessOrEqual(p.Nubi.xMax, screen.xMax, where);
                            }
            }
            Assert.Greater(cases, 100);
        }

        [Test]
        public void NubiAndTheBubble_NeverCoverGameText()
        {
            var screen = Screen(2400f);
            var hole = new Rect(-120f, -100f, 240f, 240f);
            // letras del juego repartidas por una buena parte de la pantalla
            var soft = new List<Rect>();
            for (int i = 0; i < 6; i++) soft.Add(new Rect(-500f + i * 170f, 300f, 130f, 130f));
            for (int i = 0; i < 6; i++) soft.Add(new Rect(-500f + i * 170f, -600f, 130f, 130f));
            var hard = new List<Rect> { CoachLayout.Grow(hole, 24f) };
            var p = CoachLayout.Place(screen, hole.center, hard, soft, "Toca las letras en orden");
            Assert.IsTrue(p.Clean);
            foreach (var s in soft)
            {
                Assert.AreEqual(0f, CoachLayout.Overlap(p.Nubi, s), 0.5f);
                Assert.AreEqual(0f, CoachLayout.Overlap(p.Bubble, s), 0.5f);
            }
        }

        [Test]
        public void WhenNothingFits_TheLeastCoveringPlaceWins_AndItIsNotClean()
        {
            var screen = Screen(2400f);
            var hard = new List<Rect> { screen };          // un hueco que ocupa toda la pantalla: no hay dónde
            var p = CoachLayout.Place(screen, Vector2.zero, hard, new List<Rect>(), "Toca aquí");
            Assert.IsNotNull(p);
            Assert.IsFalse(p.Clean);
            Assert.Greater(p.Overlap, 0f);
        }

        [Test]
        public void NubiGoesOnTheSideOppositeTheHole_WhenThereIsRoom()
        {
            var screen = Screen(2400f);
            var holeRight = new Rect(250f, -200f, 240f, 240f);
            var p = CoachLayout.Place(screen, holeRight.center, new List<Rect> { CoachLayout.Grow(holeRight, 24f) }, new List<Rect>(), "Toca aquí");
            Assert.IsTrue(p.Left, "el hueco está a la derecha: Nubi a la izquierda");
        }

        // ------------------------------------------------------------------ el velo

        [Test]
        public void TheVeil_CoversEverythingExceptTheLitZones_WithoutOverlappingItself()
        {
            var screen = Screen(2400f);
            var lit = new List<Rect> { new Rect(-200f, -200f, 400f, 300f), new Rect(-400f, 500f, 800f, 150f), new Rect(100f, -50f, 300f, 200f) };   // la 3.ª pisa a la 1.ª
            var veil = CoachLayout.Subtract(screen, lit);
            float veilArea = veil.Sum(r => r.width * r.height);
            // área iluminada = unión de los rectángulos
            float union = 400f * 300f + 800f * 150f + (300f * 200f - CoachLayout.Overlap(lit[0], lit[2]));
            Assert.AreEqual(screen.width * screen.height - union, veilArea, 2f);
            for (int i = 0; i < veil.Count; i++)
            {
                foreach (var l in lit) Assert.AreEqual(0f, CoachLayout.Overlap(veil[i], l), 0.5f, "el velo no cubre lo iluminado");
                for (int j = i + 1; j < veil.Count; j++) Assert.AreEqual(0f, CoachLayout.Overlap(veil[i], veil[j]), 0.5f, "las piezas del velo no se pisan");
            }
            Assert.LessOrEqual(veil.Count, 24, "caben en las piezas del velo");
        }

        [Test]
        public void TheVeil_WithOneHole_IsTheClassicFourStrips()
        {
            var veil = CoachLayout.Subtract(Screen(2400f), new List<Rect> { new Rect(-100f, -100f, 200f, 200f) });
            Assert.AreEqual(4, veil.Count);
        }

        [Test]
        public void TheVeil_WithNoZones_IsTheWholeScreen()
        {
            var screen = Screen(2400f);
            var veil = CoachLayout.Subtract(screen, new List<Rect>());
            Assert.AreEqual(1, veil.Count);
            Assert.AreEqual(screen.width * screen.height, veil[0].width * veil[0].height, 1f);
        }
    }
}
