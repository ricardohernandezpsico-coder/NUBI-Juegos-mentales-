using NUnit.Framework;
using NeuroVida.Games.Acoplamiento;
using NeuroVida.Games.Radar;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// Ningún botón que se toca baja de 48 dp, ni en la práctica del tutorial (Tarea 128; la 127 los había dejado en 46 en la pantalla más baja). Las 3 formas de pantalla de la revisión son 1080 × 1920, 2160 y 2400 (640, 720 y 800 dp de alto).
    /// El smoke mide además las losas de verdad con <c>PlasticTouch</c> (guardia «botones tocables ≥ 48 dp»).
    /// </summary>
    public class TouchTargetTests
    {
        private static readonly float[] Shapes = { 640f, 720f, 800f };

        [Test]
        public void Acoplamiento_ButtonsAreAtLeast48Dp_InTheThreeShapes_AlsoDuringThePractice()
        {
            foreach (float h in Shapes)
                foreach (bool guided in new[] { false, true })
                {
                    var p = new DockingPlan(h, guided);
                    Assert.GreaterOrEqual(p.ButtonH, 48f - 1e-3f, "Encaja/Espejo (alto " + h + ", práctica " + guided + ")");
                    Assert.GreaterOrEqual(p.ButtonW, 48f - 1e-3f, "Encaja/Espejo, ancho (alto " + h + ", práctica " + guided + ")");
                }
        }

        [Test]
        public void Rescate_CapsulesAndTheGoButtonAreAtLeast48Dp_InTheThreeShapes_AlsoDuringThePractice()
        {
            foreach (float h in Shapes)
                foreach (bool guided in new[] { false, true })
                {
                    var p = new RadarPlan(h, guided);
                    string at = " (alto " + h + ", práctica " + guided + ")";
                    Assert.GreaterOrEqual(p.CellH, RadarPlan.MinTouchDp - 1e-3f, "las cápsulas del tablero" + at);
                    Assert.GreaterOrEqual(p.CellW, RadarPlan.MinTouchDp - 1e-3f, "las cápsulas del tablero, ancho" + at);
                    Assert.GreaterOrEqual(p.GoH, RadarPlan.MinTouchDp - 1e-3f, "«¡Rescatar!»" + at);
                    Assert.LessOrEqual(p.GoBox.Y1, h - (guided ? RadarPlan.TutorialControls : 0f) + 0.01f, "todo entra" + at);
                }
        }
    }
}
