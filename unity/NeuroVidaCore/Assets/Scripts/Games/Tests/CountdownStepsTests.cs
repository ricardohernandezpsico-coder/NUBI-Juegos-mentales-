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
    /// La cuenta regresiva de 2 o 3 pasos (Tarea 127, <c>auditoria-fable/pulido-juegos.md</c> §1.4): «3 · 2 · 1 · ¡Ya!» (3,1 s) la primera partida del día y la primera vez de un juego, «2 · 1 · ¡Ya!» (2,4 s) en las demás; la app lo decide y lo manda en la config (<c>countdown_steps</c>);
    /// con «quitar animaciones» rige la misma regla. El subtítulo es siempre la consigna del juego.
    /// </summary>
    public class CountdownStepsTests
    {
        private const float Dt = 0.05f;
        private readonly List<GameObject> _made = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            GameClock.Reset();
            GameClock.SimulatedDeltaTime = Dt;
            CountdownScreen.Steps = 3;
        }

        [TearDown]
        public void TearDown()
        {
            CountdownScreen.Steps = 3;
            GameFeel.ReduceMotion = false;
            GameClock.SimulatedDeltaTime = 0f;
            foreach (var go in _made) if (go != null) UnityEngine.Object.DestroyImmediate(go);
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

        /// <summary>Corre la cuenta entera cuadro a cuadro (0,05 s) y devuelve los números que se vieron y cuánto duró.</summary>
        private float Run(int steps, bool calm, List<string> seen)
        {
            GameFeel.ReduceMotion = calm;
            CountdownScreen.Steps = steps;
            var area = NewArea();
            var cd = new CountdownScreen(area, 3f);
            var stack = new Stack<IEnumerator>();
            stack.Push(cd.Play("Un juego", "La consigna", null));
            int frames = 0;
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                if (!top.MoveNext()) { stack.Pop(); continue; }
                if (top.Current is IEnumerator inner) { stack.Push(inner); continue; }
                frames++;
                foreach (var t in area.GetComponentsInChildren<Text>(true))
                    if (t.gameObject.activeInHierarchy && (t.text == "3" || t.text == "2" || t.text == "1" || t.text == "¡Ya!") && !seen.Contains(t.text)) seen.Add(t.text);
                if (frames > 2000) Assert.Fail("la cuenta no termina");
            }
            return frames * Dt;
        }

        [Test]
        public void La_duracion_es_3_1_s_con_3_pasos_y_2_4_s_con_2()
        {
            Assert.AreEqual(3.1f, CountdownScreen.TotalSeconds(3), 1e-3f);
            Assert.AreEqual(2.4f, CountdownScreen.TotalSeconds(2), 1e-3f);
        }

        [Test]
        public void Con_3_pasos_dice_3_2_1_y_Ya()
        {
            var seen = new List<string>();
            float seconds = Run(3, calm: false, seen);
            CollectionAssert.AreEqual(new[] { "3", "2", "1", "¡Ya!" }, seen);
            Assert.AreEqual(0.22f + CountdownScreen.TotalSeconds(3), seconds, 0.3f);
        }

        [Test]
        public void Con_2_pasos_dice_2_1_y_Ya_sin_el_3_y_dura_menos()
        {
            var seen = new List<string>();
            float seconds = Run(2, calm: false, seen);
            CollectionAssert.AreEqual(new[] { "2", "1", "¡Ya!" }, seen);
            Assert.AreEqual(0.22f + CountdownScreen.TotalSeconds(2), seconds, 0.3f);
            Assert.Less(seconds, 2.9f, "la cuenta corta es de unos 2,4 s");
        }

        [Test]
        public void Con_quitar_animaciones_rige_la_misma_regla()
        {
            var three = new List<string>();
            var two = new List<string>();
            float s3 = Run(3, calm: true, three);
            float s2 = Run(2, calm: true, two);
            CollectionAssert.AreEqual(new[] { "3", "2", "1", "¡Ya!" }, three);
            CollectionAssert.AreEqual(new[] { "2", "1", "¡Ya!" }, two);
            Assert.AreEqual(s3 - s2, StepsDifference(), 0.2f, "con «quitar animaciones» también se acorta lo que dura un paso");
        }

        private static float StepsDifference() => CountdownScreen.TotalSeconds(3) - CountdownScreen.TotalSeconds(2);

        [Test]
        public void Ningun_juego_dice_Preparate_en_la_cuenta()
        {
            // el subtítulo de la cuenta es la consigna del juego (Tarea 127): «Prepárate» ya no sale en los 17 juegos de la app
            var root = System.IO.Path.Combine(Application.dataPath, "Scripts", "Games");
            var found = new List<string>();
            foreach (var file in System.IO.Directory.GetFiles(root, "*.cs", System.IO.SearchOption.AllDirectories))
            {
                string rel = file.Substring(root.Length + 1).Replace('\\', '/');
                if (rel.Contains("/Tests/") || rel.StartsWith("Bitacora/") || rel.StartsWith("Rumbo/") || rel.StartsWith("Cosecha/") || rel.StartsWith("Shared/Assessment.cs")) continue;      // los retirados de la app conservan su cuenta
                string text = System.IO.File.ReadAllText(file);
                if (text.Contains("\"Prepárate\"")) found.Add(rel);
            }
            Assert.IsEmpty(found, "estos juegos todavía dicen «Prepárate» en su cuenta regresiva: " + string.Join(", ", found));
        }
    }
}
