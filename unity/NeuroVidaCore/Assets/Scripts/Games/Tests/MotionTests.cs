using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// "Quitar animaciones" (docs/movimiento-reducido.md): con <see cref="GameFeel.ReduceMotion"/> en true, los componentes comunes
    /// no crean ni mueven adornos. Las pruebas EditMode no tienen cuadros reales, así que fijan <see cref="GameClock.SimulatedDeltaTime"/>
    /// y avanzan las corrutinas a mano. ReduceMotion es estático: el TearDown lo deja en false para no contaminar otras pruebas.
    /// </summary>
    public class MotionTests
    {
        private const float Dt = 0.05f;
        private readonly List<GameObject> _made = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            GameFeel.ReduceMotion = true;
            GameClock.SimulatedDeltaTime = Dt;
        }

        [TearDown]
        public void TearDown()
        {
            GameFeel.ReduceMotion = false;
            GameClock.SimulatedDeltaTime = 0f;
            foreach (var go in _made) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _made.Clear();
        }

        // ------------------------------------------------------------------ utilidades

        private RectTransform NewArea(string name = "Area", float w = 1080f, float h = 1920f)
        {
            var go = new GameObject(name);
            _made.Add(go);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        /// <summary>Avanza una corrutina hasta el final (entiende las corrutinas anidadas, como Unity). Llama a
        /// <paramref name="perFrame"/> después de cada cuadro. Falla si no termina en <paramref name="maxFrames"/>.</summary>
        private static int Run(IEnumerator root, Action perFrame = null, int maxFrames = 5000)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            int frames = 0;
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                if (!top.MoveNext()) { stack.Pop(); continue; }
                if (top.Current is IEnumerator inner) { stack.Push(inner); continue; }
                frames++;
                perFrame?.Invoke();
                if (frames > maxFrames) Assert.Fail("La corrutina no termina (más de " + maxFrames + " cuadros).");
            }
            return frames;
        }

        /// <summary>Foto de posición, giro, tamaño, escala y color de TODO lo que cuelga de <paramref name="root"/>.</summary>
        private static string Snapshot(Transform root)
        {
            var sb = new StringBuilder();
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                sb.Append(rt.name).Append(rt.anchoredPosition.ToString("F3")).Append(rt.sizeDelta.ToString("F3"))
                  .Append(rt.localRotation.ToString("F4")).Append(rt.localScale.ToString("F3"));
                var g = rt.GetComponent<Graphic>();
                if (g != null) sb.Append(g.color.ToString("F4"));
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private static void StepAll(Transform root, float dt)
        {
            foreach (var a in root.GetComponentsInChildren<WorldAmbient>(true)) a.Step(dt);
            foreach (var a in root.GetComponentsInChildren<CountdownAmbient>(true)) a.Step(dt);
            foreach (var s in root.GetComponentsInChildren<StarfieldFx>(true)) s.Step(dt);
        }

        private static GameWorld BusyWorld() => new GameWorld
        {
            Name = "Prueba movimiento", Stars = 12, Bokeh = 3,
            Moons = new[] { new Vector3(0.7f, 0.8f, 120f) }, MoonTints = new[] { Color.white },
            Asteroids = new[] { new Vector3(0.1f, 0.5f, 100f), new Vector3(0.9f, 0.3f, 80f) },
            SurfaceHeight = 0.15f, HasDome = true,
            OrbitRings = 2, Constellations = 2, MeteorEverySeconds = 0.4f, FloatingGlyphs = "ABC",
        };

        // ------------------------------------------------------------------ Motion

        [Test]
        public void Decorative_sigue_la_bandera_de_GameFeel()
        {
            GameFeel.ReduceMotion = true;
            Assert.IsFalse(Motion.Decorative);
            GameFeel.ReduceMotion = false;
            Assert.IsTrue(Motion.Decorative);
        }

        [Test]
        public void ScaleTo_con_ReduceMotion_deja_la_escala_final_de_una_vez()
        {
            var rect = NewArea();
            rect.localScale = Vector3.zero;
            Run(Motion.ScaleTo(rect, 0.5f, 1f, 0.3f, UiFx.EaseOutBack), () => Assert.AreEqual(Vector3.one, rect.localScale));
            Assert.AreEqual(Vector3.one, rect.localScale);
        }

        [Test]
        public void Fade_de_CanvasGroup_y_ColorTo_terminan_en_el_valor_final()
        {
            var rect = NewArea();
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            Run(Motion.Fade(group, 0f, 1f));
            Assert.AreEqual(1f, group.alpha, 1e-4f);

            var img = rect.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 0f, 0f, 1f);
            Run(Motion.ColorTo(img, new Color(0f, 1f, 0f, 0.5f)));
            Assert.AreEqual(new Color(0f, 1f, 0f, 0.5f), img.color);
        }

        [Test]
        public void PopRect_y_PopIn_no_animan_y_dejan_la_escala_en_uno()
        {
            var rect = NewArea();
            rect.localScale = Vector3.zero;
            Run(UiKit.PopRect(rect, 1.7f, 0.3f), () => Assert.AreEqual(Vector3.one, rect.localScale));
            Assert.AreEqual(Vector3.one, rect.localScale);

            rect.localScale = Vector3.zero;
            Run(UiKit.PopIn(rect, 0.3f), () => Assert.AreEqual(Vector3.one, rect.localScale));
            Assert.AreEqual(Vector3.one, rect.localScale);
        }

        // ------------------------------------------------------------------ misma duración (regla 6)

        [Test]
        public void PopRect_PopIn_y_Shake_duran_lo_mismo_con_y_sin_ReduceMotion()
        {
            var rect = NewArea();
            int Frames(Func<IEnumerator> make) { rect.localScale = Vector3.one; return Run(make()); }

            GameFeel.ReduceMotion = true;
            int popRectCalm = Frames(() => UiKit.PopRect(rect, 1.2f, 0.3f));
            int popInCalm = Frames(() => UiKit.PopIn(rect, 0.25f));
            int shakeCalm = Frames(() => UiFx.Shake(20f, 0.4f, rect));
            GameFeel.ReduceMotion = false;
            Assert.AreEqual(Frames(() => UiKit.PopRect(rect, 1.2f, 0.3f)), popRectCalm, "PopRect");
            Assert.AreEqual(Frames(() => UiKit.PopIn(rect, 0.25f)), popInCalm, "PopIn");
            Assert.AreEqual(Frames(() => UiFx.Shake(20f, 0.4f, rect)), shakeCalm, "Shake");
            Assert.Greater(popRectCalm, 3);
        }

        [Test]
        public void Las_rafagas_con_ReduceMotion_esperan_su_duracion_sin_crear_nada()
        {
            var parent = NewArea();
            // Dt = 0,05: N cuadros = ceil(segundos / 0,05) (con un cuadro de margen por la suma de flotantes).
            int spark = Run(UiFx.SparkBurst(parent, Vector2.zero, Color.white, 12, 180f, 30f, 0.55f));
            int ring = Run(UiFx.RingBurst(parent, Vector2.zero, Color.white, 10f, 100f, 0.5f));
            Assert.That(spark * Dt, Is.InRange(0.55f, 0.55f + 2 * Dt), "SparkBurst");
            Assert.That(ring * Dt, Is.InRange(0.5f, 0.5f + 2 * Dt), "RingBurst");
            Assert.AreEqual(0, parent.childCount);
        }

        [Test]
        public void ScaleTo_con_ReduceMotion_espera_su_duracion_con_la_escala_final()
        {
            var rect = NewArea();
            rect.localScale = Vector3.zero;
            int frames = Run(Motion.ScaleTo(rect, 0.5f, 1f, 0.3f, UiFx.EaseOutBack));
            Assert.That(frames * Dt, Is.InRange(0.3f, 0.3f + 2 * Dt));
            Assert.AreEqual(Vector3.one, rect.localScale);
        }

        // ------------------------------------------------------------------ UiFx

        [Test]
        public void SparkBurst_y_RingBurst_no_crean_hijos()
        {
            var parent = NewArea();
            Run(UiFx.SparkBurst(parent, Vector2.zero, Color.white, 12, 180f, 30f), () => Assert.AreEqual(0, parent.childCount));
            Run(UiFx.RingBurst(parent, Vector2.zero, Color.white, 10f, 100f), () => Assert.AreEqual(0, parent.childCount));
            Assert.AreEqual(0, parent.childCount);
        }

        [Test]
        public void Shake_no_mueve_nada()
        {
            var a = NewArea("A");
            var b = NewArea("B");
            a.anchoredPosition = new Vector2(10f, 20f);
            b.anchoredPosition = new Vector2(-5f, 7f);
            Run(UiFx.Shake(30f, 0.4f, a, b), () =>
            {
                Assert.AreEqual(new Vector2(10f, 20f), a.anchoredPosition);
                Assert.AreEqual(new Vector2(-5f, 7f), b.anchoredPosition);
            });
            Assert.AreEqual(new Vector2(10f, 20f), a.anchoredPosition);
            Assert.AreEqual(new Vector2(-5f, 7f), b.anchoredPosition);
        }

        // ------------------------------------------------------------------ fondos

        [Test]
        public void WorldBackdrop_en_reposo_no_cambia_posiciones_ni_alfa_en_muchos_cuadros()
        {
            var area = NewArea();
            WorldBackdrop.Build(area, BusyWorld());
            StepAll(area, Dt);
            var first = Snapshot(area);
            for (int i = 0; i < 240; i++) StepAll(area, Dt);
            Assert.AreEqual(first, Snapshot(area), "con ReduceMotion el fondo debe quedar quieto");
        }

        [Test]
        public void WorldBackdrop_cielo_quieto_con_titileo_tambien_queda_quieto()
        {
            var area = NewArea();
            WorldBackdrop.Build(area, GameWorld.CieloProfundo);
            StepAll(area, Dt);
            var first = Snapshot(area);
            for (int i = 0; i < 240; i++) StepAll(area, Dt);
            Assert.AreEqual(first, Snapshot(area));
        }

        [Test]
        public void WorldBackdrop_en_reposo_no_tiene_estrellas_fugaces_visibles()
        {
            var area = NewArea();
            WorldBackdrop.Build(area, BusyWorld());
            for (int i = 0; i < 120; i++)
            {
                StepAll(area, Dt);
                foreach (var img in area.GetComponentsInChildren<Image>(true))
                    if (img.name == "Meteor") Assert.AreEqual(0f, img.color.a, "estrella fugaz visible en el cuadro " + i);
            }
        }

        [Test]
        public void Control_sin_ReduceMotion_el_fondo_SI_se_mueve()
        {
            // Si esto fallara, las pruebas de "queda quieto" no probarían nada (el fondo estaría quieto de todos modos).
            GameFeel.ReduceMotion = false;
            var area = NewArea();
            WorldBackdrop.Build(area, BusyWorld());
            StepAll(area, Dt);
            var first = Snapshot(area);
            for (int i = 0; i < 60; i++) StepAll(area, Dt);
            Assert.AreNotEqual(first, Snapshot(area));
        }

        [Test]
        public void StarfieldFx_en_reposo_ignora_Warp_y_no_cambia()
        {
            var area = NewArea();
            var stars = area.gameObject.AddComponent<StarfieldFx>();
            stars.Build(area, 30, 10f);
            stars.Step(Dt);
            var first = Snapshot(area);
            stars.Warp = 1f;
            for (int i = 0; i < 240; i++) stars.Step(Dt);
            Assert.AreEqual(first, Snapshot(area));
        }

        // ------------------------------------------------------------------ cuenta regresiva y cierre

        [Test]
        public void Countdown_con_ReduceMotion_termina_sin_chispas_orbita_onda_ni_rebote()
        {
            var parent = NewArea();
            var screen = new CountdownScreen(parent, 1f);
            int reveals = 0;
            var root = parent.Find("CountdownScreen");
            Assert.IsNotNull(root);

            int frames = Run(screen.Play("Nivel 1", "Mira bien", () => reveals++), () =>
            {
                foreach (var img in root.GetComponentsInChildren<Image>(true))
                {
                    if (img.name == "Sparkle" || img.name == "OrbitStar" || img.name == "Ripple" || img.name == "Burst")
                        Assert.AreEqual(0f, img.color.a, img.name + " no debe verse");
                }
                foreach (var t in root.GetComponentsInChildren<Text>(true))
                    if (t.name == "Number" || t.name == "NumberOut") Assert.AreEqual(Vector3.one, t.rectTransform.localScale, t.name + " sin rebote");
            });

            Assert.Greater(frames, 10);
            Assert.AreEqual(1, reveals, "el juego se revela una sola vez");
            Assert.IsFalse(root.gameObject.activeSelf, "la cuenta regresiva termina y se esconde");
        }

        [Test]
        public void Countdown_el_subtitulo_largo_ocupa_el_ancho_de_la_pantalla_con_margen_y_pasa_a_otra_linea()
        {
            // el subtítulo de «Bodega de carga» se veía cortado: era una sola línea más ancha que la pantalla
            var parent = NewArea("Pantalla", 1080f, 2400f);
            new CountdownScreen(parent, 3f);
            var sub = parent.Find("CountdownScreen/Subtitle").GetComponent<Text>();
            Assert.AreEqual(HorizontalWrapMode.Wrap, sub.horizontalOverflow, "sin ajuste de línea se sale de la pantalla");
            float w = sub.rectTransform.rect.width;
            Assert.LessOrEqual(w, 1080f - 2f * 28f * 3f + 0.5f, "con un margen de 28 dp a cada lado");
            Assert.Greater(w, 700f, "y no queda angosto");
            var small = NewArea("Chica", 720f, 1280f);       // otra pantalla: el ancho sigue a la pantalla, no a una cifra fija
            new CountdownScreen(small, 2f);
            Assert.AreEqual(720f - 2f * 28f * 2f, small.Find("CountdownScreen/Subtitle").GetComponent<Text>().rectTransform.rect.width, 0.5f);
        }

        [Test]
        public void FinishCurtain_con_ReduceMotion_termina_sin_chispas_ni_rebote()
        {
            var parent = NewArea();
            var curtain = FinishCurtain.Create(parent);
            _made.Add(curtain.gameObject);
            var fx = curtain.transform.Find("Fx");
            var title = curtain.transform.Find("Title");
            Assert.IsNotNull(fx);
            Assert.IsNotNull(title);

            Run(curtain.Play(null), () =>
            {
                Assert.AreEqual(0, fx.childCount, "sin chispas");
                Assert.AreEqual(Vector3.one, title.localScale, "sin rebote del título");
            });
            Assert.AreEqual(1f, curtain.GetComponent<CanvasGroup>().alpha, 1e-4f);
        }

        // ------------------------------------------------------------------ toque

        [Test]
        public void PressScale_con_ReduceMotion_oscurece_en_vez_de_escalar_y_restaura()
        {
            var rect = NewArea("Boton", 200f, 100f);
            var img = rect.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var press = rect.gameObject.AddComponent<PressScale>();
            // Awake no corre en EditMode para un componente normal: se llama a mano.
            typeof(PressScale).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(press, null);

            press.OnPointerDown(null);
            Assert.AreEqual(Vector3.one, rect.localScale, "no escala");
            Assert.Less(img.color.r, 1f, "se oscurece");
            Assert.AreEqual(1f, img.color.a, 1e-4f, "el alfa no cambia");

            press.OnPointerUp(null);
            Assert.AreEqual(Color.white, img.color, "vuelve a su color");
        }
    }
}
