using System;
using NUnit.Framework;

namespace NeuroVida.Games.Anagramas.Tests
{
    public class BubbleFieldTests
    {
        // Área de juego típica: 960 x 720 unidades (3 unidades = 1 dp); 11 letras de 56 dp.
        private static BubbleField Make(int n = 11, float diameterDp = 56f, float gapDp = 8f, float speedDp = 70f, int seed = 3)
            => new BubbleField(n, 960f, 720f, diameterDp * 1.5f, BubbleField.MinDiameterDp * 1.5f, gapDp * 3f, speedDp * 3f, new Random(seed));

        [Test]
        public void Bubbles_NeverTouch_NeverLeave_AndNeverStop_ForAMinute()
        {
            foreach (int seed in new[] { 1, 2, 3, 4, 5 })
            foreach (bool senior in new[] { false, true })
            {
                // Perfil de adulto (56 dp, 8 de separación) y de mayor (64 dp, 12 de separación): mismas garantías.
                var f = senior ? Make(diameterDp: 64f, gapDp: 12f, speedDp: 42f, seed: seed) : Make(seed: seed);
                Assert.GreaterOrEqual(f.MinClearance(), f.Gap - 0.5f, "al empezar");
                for (int step = 0; step < 60 * 60; step++)
                {
                    f.Step(1f / 60f);
                    Assert.GreaterOrEqual(f.MinClearance(), f.Gap - 1f, $"se tocaron (semilla {seed}, paso {step})");
                    for (int i = 0; i < f.Count; i++)
                    {
                        Assert.That(f.X[i], Is.InRange(f.Radius, f.Width - f.Radius));
                        Assert.That(f.Y[i], Is.InRange(f.Radius, f.Height - f.Radius));
                        float v = (float)Math.Sqrt(f.Vx[i] * f.Vx[i] + f.Vy[i] * f.Vy[i]);
                        Assert.AreEqual(f.Speed, v, 0.01f, "cada burbuja mantiene su rapidez");
                    }
                }
            }
        }

        [Test]
        public void ALetterThatGoesToItsSlot_StopsColliding_AndComesBackToAFreeSpot()
        {
            var f = Make();
            f.Deactivate(0);
            float x = f.X[0];
            for (int s = 0; s < 120; s++) f.Step(1f / 60f);
            Assert.AreEqual(x, f.X[0], 1e-4f, "la que está en su casilla no se mueve");
            f.Reactivate(0, new Random(9));
            Assert.GreaterOrEqual(f.MinClearance(), f.Gap - 0.5f, "vuelve a un lugar libre");
        }

        [Test]
        public void Spec_OnlyFromLevel5_GentlerAndBiggerForSeniors_AndNeverUnder48dp()
        {
            Assert.IsNull(BubbleField.SpecFor(4, AgeBand.Adult));
            var adult5 = BubbleField.SpecFor(5, AgeBand.Adult).Value;
            var adult7 = BubbleField.SpecFor(7, AgeBand.Adult).Value;
            var senior7 = BubbleField.SpecFor(7, AgeBand.Senior).Value;
            Assert.Less(adult5.SpeedDp, adult7.SpeedDp, "el movimiento entra de a poco");
            Assert.Less(senior7.SpeedDp, adult7.SpeedDp);
            Assert.Greater(senior7.DiameterDp, adult7.DiameterDp);
            Assert.GreaterOrEqual(senior7.GapDp, 8f);
            // Un área chica achica las burbujas, pero nunca bajo 48 dp.
            float r = BubbleField.FitRadius(11, 600f, 400f, 32f * 3f, 24f * 3f, 24f);
            Assert.GreaterOrEqual(r, 24f * 3f);
        }
    }
}
