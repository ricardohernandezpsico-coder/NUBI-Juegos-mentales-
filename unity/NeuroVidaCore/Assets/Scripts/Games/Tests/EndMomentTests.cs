using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// UN solo final (Tarea 126, <c>auditoria-fable/pulido-juegos.md</c> §2): el «momento del objeto» (≈1,0 s: la escena apagada por un velo y el emblema del juego con su movimiento) y la cortina con el emblema (≈1,2 s); con «quitar animaciones» el momento
    /// es un cuadro fijo. Y el cielo único: los tonos de <see cref="NeuroStyle"/> son los del fondo de la app, y los textos claros se leen sobre el más claro de los tres.
    /// </summary>
    public class EndMomentTests
    {
        private const float Dt = 0.05f;
        private readonly List<GameObject> _made = new List<GameObject>();

        /// <summary>Los 17 juegos de la app (los retirados no tienen emblema).</summary>
        private static readonly string[] Games =
        {
            "parejas", "secuencia", "bodega", "correo", "stroop", "piloto", "freno", "satelites", "radar", "calculo", "engranajes", "acoplamiento", "aterrizaje", "anagramas", "meteoros", "disparate", "intrusa"
        };

        [SetUp]
        public void SetUp()
        {
            GameFeel.ReduceMotion = true;
            GameClock.SimulatedDeltaTime = Dt;
            PlasticArt.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            GameFeel.ReduceMotion = false;
            GameClock.SimulatedDeltaTime = 0f;
            foreach (var go in _made) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _made.Clear();
        }

        private RectTransform NewArea()
        {
            var go = new GameObject("Area");
            _made.Add(go);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1080f, 1920f);
            return rect;
        }

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

        private FinishCurtain Curtain(string gameId)
        {
            var c = FinishCurtain.Create(NewArea(), gameId);
            _made.Add(c.gameObject);
            return c;
        }

        // ------------------------------------------------------------------ emblema y movimiento de cada juego

        [Test]
        public void Los_17_juegos_de_la_app_tienen_emblema_y_movimiento()
        {
            foreach (var id in Games)
            {
                Assert.IsNotNull(FinishCurtain.EmblemSprite(id), "falta el emblema de «" + id + "» en el atlas «emblemas» (tools/juegos/empaqueta.py emblemas)");
                Assert.IsTrue(Enum.IsDefined(typeof(FinishCurtain.MomentKind), FinishCurtain.KindOf(id)), id);
            }
            Assert.IsNull(FinishCurtain.EmblemSprite("bitacora"), "un juego retirado no tiene emblema: queda la cortina de siempre");
            Assert.IsNull(FinishCurtain.EmblemSprite(null));
        }

        [Test]
        public void Los_cinco_movimientos_se_usan()
        {
            var kinds = new HashSet<FinishCurtain.MomentKind>();
            foreach (var id in Games) kinds.Add(FinishCurtain.KindOf(id));
            Assert.AreEqual(5, kinds.Count, "se asienta, sube, se mece, sella o aparece: no todos los juegos entran igual");
        }

        [Test]
        public void El_momento_termina_en_reposo_en_cada_movimiento_y_arranca_chico()
        {
            foreach (var id in new[] { "bodega", "piloto", "engranajes", "correo", "parejas" })
            {
                var c = Curtain(id);
                var end = c.EmblemPoseAt(FinishCurtain.MomentSeconds, out float scale, out float rot);
                Assert.AreEqual(Vector2.zero, end, "«" + id + "»: queda en su sitio");
                Assert.AreEqual(1f, scale, 1e-3f, "«" + id + "»: a su tamaño");
                Assert.AreEqual(0f, rot, 1e-2f, "«" + id + "»: derecho");
                c.EmblemPoseAt(0.1f, out float early, out _);
                if (c.Kind != FinishCurtain.MomentKind.Stamp) Assert.Less(early, 1f, "«" + id + "»: entra desde más chico");
                else Assert.Greater(early, 1f, "«" + id + "»: el sello entra grande y se planta");
            }
        }

        // ------------------------------------------------------------------ el tiempo y «quitar animaciones»

        [Test]
        public void Con_quitar_animaciones_el_momento_es_un_cuadro_fijo_y_dura_lo_mismo()
        {
            var c = Curtain("radar");
            var emblem = c.transform.Find("Emblem");
            var fx = c.transform.Find("Fx");
            Assert.IsNotNull(emblem);
            Assert.IsTrue(c.HasEmblem);
            float elapsed = 0f; bool moving = false;
            int frames = Run(c.Play(null), () =>
            {
                elapsed += Dt;
                if (elapsed <= FinishCurtain.MomentSeconds - Dt)
                    if (emblem.localScale != Vector3.one || emblem.localRotation != Quaternion.identity || emblem.GetComponent<RectTransform>().anchoredPosition != Vector2.zero) moving = true;
            });
            Assert.IsFalse(moving, "el momento es un cuadro fijo: el emblema no se mueve");
            Assert.AreEqual(0, fx.childCount, "sin chispas");
            Assert.AreEqual(FinishCurtain.MomentSeconds + FinishCurtain.CurtainSeconds, frames * Dt, 0.2f, "momento + cortina ≈ 2,2 s");
            Assert.AreEqual(1f, c.GetComponent<CanvasGroup>().alpha, 1e-4f);
            Assert.AreEqual(1f, c.transform.Find("SkyGroup").GetComponent<CanvasGroup>().alpha, 1e-3f, "al final el cielo cubre todo");
            Assert.AreEqual(Vector3.one, c.transform.Find("Title").localScale, "«¡Listo!» a su tamaño");
        }

        [Test]
        public void Sin_emblema_no_hay_momento_y_queda_la_cortina_de_siempre()
        {
            var c = Curtain(null);
            Assert.IsFalse(c.HasEmblem);
            Assert.IsNull(c.transform.Find("Emblem"));
            int frames = Run(c.Play(null));
            Assert.AreEqual(FinishCurtain.CurtainSeconds, frames * Dt, 0.15f);
        }

        [Test]
        public void El_velo_tapa_los_toques_desde_el_primer_cuadro()
        {
            var c = Curtain("stroop");
            var veil = c.transform.Find("Veil").GetComponent<Image>();
            Assert.IsTrue(veil.raycastTarget, "el juego no recibe toques mientras suena el final");
            Assert.IsNotNull(c.GetComponent<GraphicRaycaster>());
            Assert.IsFalse(c.transform.Find("SkyGroup").GetComponent<CanvasGroup>().blocksRaycasts, "el cielo no estorba: el velo ya los tapa");
        }

        // ------------------------------------------------------------------ cielo único

        private static float Luminance(Color c)
        {
            float R(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * R(c.r) + 0.7152f * R(c.g) + 0.0722f * R(c.b);
        }

        private static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        [Test]
        public void El_cielo_de_Unity_es_el_de_la_app()
        {
            Assert.AreEqual(NeuroStyle.Hex(0x07071F), NeuroStyle.NightTop);
            Assert.AreEqual(NeuroStyle.Hex(0x100F3A), NeuroStyle.NightMid);
            Assert.AreEqual(NeuroStyle.Hex(0x1C1B52), NeuroStyle.NightBottom);
            var sky = NeuroStyle.NightGradient();
            var tex = sky.texture;
            Assert.AreEqual(NeuroStyle.NightBottom.r, tex.GetPixel(0, 0).r, 0.01f, "abajo, el tono más claro (el horizonte)");
            Assert.AreEqual(NeuroStyle.NightTop.r, tex.GetPixel(0, tex.height - 1).r, 0.01f, "arriba, el más oscuro");
        }

        [Test]
        public void Los_textos_claros_de_los_juegos_se_leen_sobre_el_cielo_nuevo()
        {
            // El peor caso es el tono más claro de los tres (el de abajo). Los colores de texto que los juegos ponen directo sobre el cielo: crema, sus transparencias de siempre y los colores de la paleta.
            var worst = NeuroStyle.NightBottom;
            var texts = new Dictionary<string, Color>
            {
                { "crema", NeuroStyle.Cream }, { "crema 85 %", Color.Lerp(worst, NeuroStyle.Cream, 0.85f) }, { "crema 65 %", Color.Lerp(worst, NeuroStyle.Cream, 0.65f) },
                { "sol", NeuroStyle.Sun }, { "celeste", NeuroStyle.Sky }, { "lima", NeuroStyle.Lime }, { "uva", NeuroStyle.Grape }, { "coral", NeuroStyle.Coral },
            };
            foreach (var kv in texts) Assert.GreaterOrEqual(Contrast(kv.Value, worst), 4.5f, "«" + kv.Key + "» sobre el cielo más claro (#1C1B52): contraste ≥ 4,5:1");
            // y el velo del momento deja la escena apagada: la tinta de los textos oscuros ya no importa, pero el emblema (pastel) se ve sobre el cielo
            Assert.GreaterOrEqual(Contrast(NeuroStyle.Hex(0xF7C58E), NeuroStyle.NightTop), 4.5f, "la ficha de Atención sobre lo más oscuro");
        }
    }
}
