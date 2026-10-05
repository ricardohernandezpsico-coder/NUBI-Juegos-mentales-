using NUnit.Framework;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.Piloto;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>Símbolos neutros (regla de Ricardo, 4-oct-2026; docs/simbolos-neutros.md): los ocho glifos de los planetas-puerto se distinguen de a pares aun en tamaño chico,
    /// y las formas nuevas de Parejas y Piloto existen y se dibujan.</summary>
    public class NeutralSymbolsTests
    {
        private const int Grid = 20;

        private static bool[] Mask(int glyph)
        {
            var m = new bool[Grid * Grid];
            for (int j = 0; j < Grid; j++)
                for (int i = 0; i < Grid; i++)
                {
                    float x = (i + 0.5f) / Grid * 2f - 1f, y = (j + 0.5f) / Grid * 2f - 1f;
                    m[j * Grid + i] = PortSprites.Glyph(glyph, x * 0.3f, y * 0.3f, 0.3f) < 0f;
                }
            return m;
        }

        [Test]
        public void TheEightPortGlyphs_AreDifferentFromEachOther_EvenSmall()
        {
            var masks = new bool[8][];
            for (int g = 0; g < 8; g++)
            {
                masks[g] = Mask(g);
                int filled = 0;
                foreach (bool b in masks[g]) if (b) filled++;
                Assert.Greater(filled, 20, "el glifo " + g + " se dibuja");
            }
            for (int a = 0; a < 8; a++)
                for (int b = a + 1; b < 8; b++)
                {
                    int diff = 0;
                    for (int k = 0; k < masks[a].Length; k++) if (masks[a][k] != masks[b][k]) diff++;
                    Assert.GreaterOrEqual(diff, 24, "los glifos " + a + " y " + b + " se confunden (solo " + diff + " casillas distintas de " + masks[a].Length + ")");
                }
        }

        [Test]
        public void ThePorts_AreDrawnForAllEightColors()
        {
            for (int c = 0; c < 8; c++) Assert.IsNotNull(PortSprites.Port(c), "puerto " + c);
        }

        [Test]
        public void TheNewCardShapes_AreDrawn_AndTheOldOnesAreGone()
        {
            foreach (var kind in new[] { ShapeKind.Galaxy, ShapeKind.FullMoon, ShapeKind.Hexagon, ShapeKind.Drop })
                for (int v = 0; v < SymbolSprite.VariantCount; v++)
                    Assert.IsNotNull(SymbolSprite.Get(kind, v), kind + " " + v);
            foreach (string name in System.Enum.GetNames(typeof(ShapeKind)))
                Assert.IsFalse(name == "Star" || name == "Moon" || name == "Crescent" || name == "Cross", "la forma «" + name + "» ya no existe");
        }

        [Test]
        public void ThePilotSignals_AreSixDifferentShapes_WithoutAStarOrACrescent()
        {
            Assert.AreEqual(6, PilotContract.Shapes.Length);
            CollectionAssert.AllItemsAreUnique(PilotContract.Shapes);
            Assert.Contains((int)ShapeKind.Hexagon, PilotContract.Shapes);
            Assert.Contains((int)ShapeKind.Drop, PilotContract.Shapes);
            foreach (int s in PilotContract.Shapes)
            {
                string name = ((ShapeKind)s).ToString();
                Assert.IsFalse(name == "Star" || name == "Moon", "una señal de Piloto es «" + name + "»");
            }
        }
    }
}
