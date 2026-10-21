using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// Nubi viva en los juegos (Tarea 148, P1 y P3): flota y parpadea en reposo, con la sombra fija en el suelo; el tamaño de Nubi en los tutoriales es de 120 dp (preferido) a 60 dp (mínimo).
    /// La lógica es la de <c>NubiIdle.kt</c> en la app. Lo que se ve está en la hoja <c>docs/previews/tarea-148.png</c> (capturas reales).
    /// </summary>
    public class NubiIdleTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            GameFeel.ReduceMotion = false;
            foreach (var go in _made) if (go != null) Object.DestroyImmediate(go);
            _made.Clear();
        }

        // ------------------------------------------------------------------ la lógica pura

        [Test]
        public void Floats6DpUpAnd6DpDown_InA4Point5SecondSine()
        {
            Assert.AreEqual(0f, NubiIdle.FloatDpAt(0f), 1e-4f, "empieza en reposo");
            Assert.AreEqual(6f, NubiIdle.FloatDpAt(1.125f), 1e-3f, "a un cuarto del período, arriba");
            Assert.AreEqual(0f, NubiIdle.FloatDpAt(2.25f), 1e-3f, "a la mitad, de vuelta");
            Assert.AreEqual(-6f, NubiIdle.FloatDpAt(3.375f), 1e-3f, "a tres cuartos, abajo");
            Assert.AreEqual(NubiIdle.FloatDpAt(0.7f), NubiIdle.FloatDpAt(0.7f + 4.5f), 1e-3f, "y el período se repite");
            for (float t = 0f; t < 9f; t += 0.05f) Assert.LessOrEqual(Mathf.Abs(NubiIdle.FloatDpAt(t)), 6.001f, "nunca pasa de ±6 dp (t=" + t + ")");
        }

        [Test]
        public void TheShadowShrinksTo85PercentAndFadesTo70PercentAtTheHighPoint()
        {
            Assert.AreEqual(0f, NubiIdle.Height01(-6f), 1e-4f);
            Assert.AreEqual(1f, NubiIdle.Height01(6f), 1e-4f);
            Assert.AreEqual(1f, NubiIdle.ShadowSize(0f), 1e-4f);
            Assert.AreEqual(0.85f, NubiIdle.ShadowSize(1f), 1e-4f);
            Assert.AreEqual(1f, NubiIdle.ShadowAlpha(0f), 1e-4f);
            Assert.AreEqual(0.7f, NubiIdle.ShadowAlpha(1f), 1e-4f);
        }

        [Test]
        public void OneBlinkInFiveIsDouble_AndTheWaitsAreBetween4And7Seconds()
        {
            for (int n = 1; n <= 20; n++) Assert.AreEqual(n % 5 == 0, NubiIdle.IsDouble(n), "parpadeo " + n);
            Assert.IsFalse(NubiIdle.IsDouble(0));
            var rng = new System.Random(148);
            float min = 99f, max = 0f;
            for (int i = 0; i < 500; i++) { float w = NubiIdle.WaitSeconds(rng); min = Mathf.Min(min, w); max = Mathf.Max(max, w); }
            Assert.GreaterOrEqual(min, NubiIdle.MinWaitSeconds);
            Assert.LessOrEqual(max, NubiIdle.MaxWaitSeconds);
            Assert.Greater(max - min, 2f, "usa el rango, no una sola espera");
        }

        [Test]
        public void TheBlinkIsA120MsFade_ClosesFullyAndOpensAgain()
        {
            var idle = new NubiIdle(7, 0f);
            float peak = 0f, firstClosedAt = -1f, openedAgainAt = -1f;
            for (float t = 0f; t < 12f; t += 1f / 120f)
            {
                idle.Tick(t);
                Assert.That(idle.Closed, Is.InRange(0f, 1f));
                peak = Mathf.Max(peak, idle.Closed);
                if (firstClosedAt < 0f && idle.Closed > 0f) firstClosedAt = t;
                if (firstClosedAt >= 0f && openedAgainAt < 0f && t > firstClosedAt + 0.01f && idle.Closed <= 0f) openedAgainAt = t;
                if (openedAgainAt >= 0f) break;
            }
            Assert.GreaterOrEqual(firstClosedAt, NubiIdle.MinWaitSeconds - 0.02f, "no parpadea antes de 4 s");
            Assert.LessOrEqual(firstClosedAt, NubiIdle.MaxWaitSeconds + 0.02f, "ni después de 7 s");
            Assert.AreEqual(1f, peak, 0.1f, "los ojos llegan a cerrarse del todo (a 120 cuadros por segundo)");
            Assert.AreEqual(2f * NubiIdle.BlinkSeconds, openedAgainAt - firstClosedAt, 0.05f, "120 ms para cerrar y 120 para abrir");
        }

        [Test]
        public void EveryFifthBlinkIsADoubleOne_AndTheGapsBetweenBlinksStayInRange()
        {
            var idle = new NubiIdle(11, 0f);
            var starts = new List<float>();
            float prev = 0f;
            for (float t = 0f; t < 90f && starts.Count < 12; t += 1f / 120f)
            {
                idle.Tick(t);
                if (prev <= 0f && idle.Closed > 0f) starts.Add(t);
                prev = idle.Closed;
            }
            Assert.GreaterOrEqual(starts.Count, 10, "en 90 s hay muchos parpadeos");
            // un parpadeo doble deja DOS arranques seguidos a menos de medio segundo; solo en el 5.º, el 10.º…
            int pairs = 0;
            for (int i = 1; i < starts.Count; i++)
            {
                float gap = starts[i] - starts[i - 1];
                if (gap < 0.5f) pairs++;
                else Assert.That(gap, Is.InRange(NubiIdle.MinWaitSeconds + 2f * NubiIdle.BlinkSeconds - 0.05f, NubiIdle.MaxWaitSeconds + 2f * NubiIdle.BlinkSeconds + 2f * NubiIdle.BlinkSeconds + NubiIdle.DoubleGapSeconds + 0.05f), "espera entre parpadeos");
            }
            Assert.GreaterOrEqual(pairs, 1, "el 5.º parpadeo es doble");
            Assert.LessOrEqual(pairs, 2, "y no es doble cualquiera");
        }

        // ------------------------------------------------------------------ la figura (con el atlas «nubi»)

        private NubiFigure NewFigure(float boxWidth)
        {
            var go = new GameObject("Holder", typeof(RectTransform));
            _made.Add(go);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(boxWidth, boxWidth * NubiFigure.BoxRatio);
            var holder = go.AddComponent<Image>();
            var figure = NubiFigure.Attach(holder);
            Assert.IsNotNull(figure, "con el atlas «nubi» hay figura");
            figure.Restart(0f);         // el reloj de la prueba parte de 0 (en el Editor Time.unscaledTime no)
            return figure;
        }

        [Test]
        public void TheAtlasHasTheOpenEyesTheClosedEyesAndTheShadowAsSeparateImages()
        {
            Assert.IsTrue(NubiFigure.Available, "nubi_3d, nubi_3d_descansa y nubi_sombra en el atlas «nubi»");
            var open = NubiTeacherSprite.Get().rect; var closed = NubiTeacherSprite.GetClosed().rect; var shadow = NubiTeacherSprite.GetShadow().rect;
            Assert.AreEqual(open.size, closed.size, "los ojos cerrados miden lo mismo que los abiertos: el parpadeo es un fundido sin saltos");
            Assert.Greater(open.width, open.height, "la Nubi 3D es más ancha que alta");
            Assert.AreEqual(NubiFigure.ShadowWidthFactor, shadow.width / open.width, 0.01f, "la sombra mide 1,29 veces el ancho del cuerpo");
            Assert.AreEqual(5f, shadow.width / shadow.height, 0.05f, "y es una elipse baja (0,2 de alto sobre su ancho)");
        }

        [Test]
        public void TheFigureFloatsAndBlinksButTheShadowStaysOnTheGround()
        {
            var figure = NewFigure(360f);
            figure.Step(3.375f);        // el punto más bajo
            Assert.AreEqual(-6f, figure.FloatDp, 1e-3f);
            float bodyLow = figure.Body.anchoredPosition.y, shadowY = figure.Shadow.anchoredPosition.y, shadowLow = figure.Shadow.sizeDelta.x, alphaLow = figure.ShadowImage.color.a;
            figure.Step(1.125f);        // el punto más alto
            Assert.AreEqual(6f, figure.FloatDp, 1e-3f);
            Assert.AreEqual(12f * 3f, figure.Body.anchoredPosition.y - bodyLow, 0.01f, "sube 12 dp (de -6 a +6) = 36 unidades");
            Assert.AreEqual(shadowY, figure.Shadow.anchoredPosition.y, 1e-3f, "la sombra no sube con Nubi: está en el suelo");
            Assert.AreEqual(0.85f, figure.Shadow.sizeDelta.x / shadowLow, 0.01f, "pero se achica al 85 % cuando Nubi está arriba");
            Assert.AreEqual(0.7f, figure.ShadowImage.color.a / alphaLow, 0.01f, "y se aclara al 70 % de su opacidad");
            Assert.Less(figure.EyesClosedImage.color.a, 0.01f, "con los ojos abiertos la imagen cerrada es invisible");
        }

        [Test]
        public void TheFigureBlinksByFadingTheClosedEyesImageOverTheBody()
        {
            var figure = NewFigure(360f);
            float peak = 0f;
            for (float t = 0f; t < 20f; t += 1f / 60f) { figure.Step(t); peak = Mathf.Max(peak, figure.EyesClosedImage.color.a); }
            Assert.Greater(peak, 0.9f, "en 20 s parpadea: la imagen de ojos cerrados llega a verse");
        }

        [Test]
        public void WithReduceMotionNubiStaysStillWithHerEyesOpen()
        {
            var figure = NewFigure(360f);
            GameFeel.ReduceMotion = true;
            float y = float.NaN;
            for (float t = 0f; t < 20f; t += 1f / 30f)
            {
                figure.Step(t);
                Assert.AreEqual(0f, figure.FloatDp, 0f, "quieta");
                Assert.AreEqual(0f, figure.EyesClosedImage.color.a, 0f, "con los ojos abiertos (t=" + t + ")");
                if (float.IsNaN(y)) y = figure.Body.anchoredPosition.y; else Assert.AreEqual(y, figure.Body.anchoredPosition.y, 1e-4f, "no se mueve");
            }
        }

        [Test]
        public void TheFigureBoxIsTheBodyPlusRoomForTheShadow()
        {
            var figure = NewFigure(360f);
            figure.Step(0f);
            var body = figure.Body.sizeDelta;
            Assert.AreEqual(360f, body.x, 0.01f, "el cuerpo mide el ancho de la caja");
            float box = 360f * NubiFigure.BoxRatio;
            Assert.AreEqual(body.y + NubiFigure.ShadowRoom * 360f, box, 0.5f, "alto de la caja = cuerpo + aire de la sombra");
            float bodyBottom = figure.Body.anchoredPosition.y - body.y / 2f;
            Assert.Less(figure.Shadow.anchoredPosition.y, bodyBottom, "el centro de la sombra cae bajo la base del cuerpo");
            Assert.Greater(figure.Shadow.anchoredPosition.y, -box / 2f, "pero dentro de la caja (solo su borde suave se sale)");
        }

        // ------------------------------------------------------------------ el tamaño de Nubi en los tutoriales (P3)

        [Test]
        public void TheCoachNubiIs120DpAtMostAnd60DpAtLeast()
        {
            const float unitsPerDp = 3f;
            Assert.AreEqual(120f, CoachLayout.NubiSizes[0] / unitsPerDp, 0.01f, "preferido: 120 dp");
            Assert.AreEqual(60f, CoachLayout.NubiSizes[CoachLayout.NubiSizes.Length - 1] / unitsPerDp, 0.01f, "mínimo: 60 dp (la cara se lee hasta unos 56 dp)");
            for (int i = 1; i < CoachLayout.NubiSizes.Length; i++) Assert.Less(CoachLayout.NubiSizes[i], CoachLayout.NubiSizes[i - 1], "de mayor a menor");
        }

        [Test]
        public void TheCoachPlacementUsesTheRealHeightOfTheWideNubi()
        {
            var screen = new Rect(-540f, -960f, 1080f, 1920f);
            var square = CoachLayout.Place(screen, null, new List<Rect>(), new List<Rect>(), "Mira esto");
            var wide = CoachLayout.Place(screen, null, new List<Rect>(), new List<Rect>(), "Mira esto", null, null, 0.76f);
            Assert.AreEqual(square.NubiSize, square.Nubi.height, 0.01f, "la Nubi plana es cuadrada");
            Assert.AreEqual(wide.NubiSize * 0.76f, wide.Nubi.height, 0.01f, "la 3D con su sombra mide 0,76 de su ancho");
            Assert.AreEqual(CoachLayout.NubiSizes[0], wide.NubiSize, 0.01f, "sin nada que tapar, el tamaño preferido");
        }
    }
}
