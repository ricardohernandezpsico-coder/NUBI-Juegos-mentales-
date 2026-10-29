using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// El «¡Ya!» de entrada (Tarea 190): dura 0,8 s, entra y sale en espejo, con «quitar animaciones» es un fundido de 150 ms sin rebote con la misma duración total, no se toca, no tapa el marcador y su letra pasa el mínimo.
    /// Las pruebas EditMode no tienen cuadros reales: fijan <see cref="GameClock.SimulatedDeltaTime"/> y avanzan la corrutina a mano.
    /// </summary>
    public class GoCueTests
    {
        private const float Dt = 0.05f;
        private readonly List<GameObject> _made = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            GameFeel.ReduceMotion = false;
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

        private RectTransform NewArea(float w = 1080f, float h = 1920f)
        {
            var go = new GameObject("Area");
            _made.Add(go);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private static int Run(IEnumerator root, Action perFrame = null, int maxFrames = 2000)
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
                if (frames > maxFrames) Assert.Fail("La corrutina no termina.");
            }
            return frames;
        }

        [Test]
        public void Dura_ocho_decimas_y_termina_escondido()
        {
            Assert.AreEqual(0.8f, GoCue.Seconds, 1e-6f);
            var cue = new GoCue(NewArea(), 3f);
            Assert.IsFalse(cue.Showing, "antes de empezar no se ve");
            bool visible = false;
            int frames = Run(cue.Play(), () => visible |= cue.Showing);
            Assert.IsTrue(visible, "se vio durante la corrida");
            Assert.IsFalse(cue.Showing, "al terminar ya no está");
            Assert.AreEqual(GoCue.Seconds, frames * Dt, Dt + 1e-4f, "dura 0,8 s de reloj de juego");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Entra_y_sale_en_espejo(bool reduceMotion)
        {
            foreach (float t in new[] { 0.02f, 0.05f, 0.1f, 0.15f, 0.2f })
            {
                GoCue.Pose(t, reduceMotion, out float a1, out float s1);
                GoCue.Pose(GoCue.Seconds - t, reduceMotion, out float a2, out float s2);
                Assert.AreEqual(a1, a2, 1e-4f, "la opacidad de la salida es la de la entrada al revés, t = " + t);
                Assert.AreEqual(s1, s2, 1e-4f, "la escala de la salida es la de la entrada al revés, t = " + t);
            }
            GoCue.Pose(0f, reduceMotion, out float a0, out _);
            GoCue.Pose(GoCue.Seconds, reduceMotion, out float aEnd, out _);
            Assert.AreEqual(0f, a0, 1e-4f, "empieza invisible");
            Assert.AreEqual(0f, aEnd, 1e-4f, "termina invisible");
            GoCue.Pose(GoCue.Seconds / 2f, reduceMotion, out float aMid, out float sMid);
            Assert.AreEqual(1f, aMid, 1e-4f, "a la mitad se ve entera");
            Assert.AreEqual(1f, sMid, 1e-4f, "y en su tamaño");
        }

        [Test]
        public void Con_quitar_animaciones_es_un_fundido_de_150_ms_sin_rebote_y_dura_lo_mismo()
        {
            GameFeel.ReduceMotion = true;
            GoCue.Pose(0.075f, true, out float aHalf, out float sHalf);
            Assert.AreEqual(0.5f, aHalf, 1e-3f, "a los 75 ms el fundido va por la mitad");
            Assert.AreEqual(1f, sHalf, 1e-6f, "sin rebote ni cambio de tamaño");
            GoCue.Pose(0.15f, true, out float aFull, out _);
            Assert.AreEqual(1f, aFull, 1e-3f, "a los 150 ms ya se ve entera");
            var cue = new GoCue(NewArea(), 3f);
            float maxScaleDelta = 0f;
            int frames = Run(cue.Play(), () => maxScaleDelta = Mathf.Max(maxScaleDelta, Mathf.Abs(cue.Slab.localScale.x - 1f)));
            Assert.AreEqual(0f, maxScaleDelta, 1e-6f, "la losa nunca cambia de tamaño");
            Assert.AreEqual(GoCue.Seconds, frames * Dt, Dt + 1e-4f, "la misma duración total");
        }

        [Test]
        public void No_se_toca_no_tapa_el_marcador_y_su_letra_pasa_el_minimo()
        {
            var area = NewArea();
            var cue = new GoCue(area, 3f);
            var root = area.Find("GoCue");
            Assert.IsNotNull(root);
            var group = root.GetComponent<CanvasGroup>();
            Assert.IsFalse(group.blocksRaycasts, "los toques pasan");
            Assert.IsFalse(group.interactable);
            foreach (var g in root.GetComponentsInChildren<Graphic>(true)) Assert.IsFalse(g.raycastTarget, g.name + " no recibe toques");
            var text = root.GetComponentInChildren<Text>(true);
            Assert.AreEqual("¡Ya!", text.text);
            Assert.GreaterOrEqual(text.fontSize, 48, "nada informativo bajo 16 dp (48 unidades)");
            // el lienzo de 1080 × 1920 con la losa al centro: queda lejos del marcador y la fila de consigna (el cuarto de arriba)
            float top = 0.5f * 1920f + GoCue.SlabUnits.y / 2f;
            Assert.Less(top, 0.75f * 1920f, "la losa no llega al marcador ni a la fila de consigna");
            Assert.LessOrEqual(GoCue.SlabUnits.x, 1080f * 0.6f, "no es una pantalla: es una losa");
        }
    }
}
