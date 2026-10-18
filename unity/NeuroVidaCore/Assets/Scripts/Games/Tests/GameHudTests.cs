using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// El marcador común de dos filas y los controles fijos del tutorial (Tarea 127, <c>auditoria-fable/pulido-juegos.md</c> §1.1, §1.2 y §1.7): fila 1 con el título a 18 dp y el avance a la derecha, «Nivel N» fuera de la vista, el chip «×N seguidos» solo desde 2,
    /// la fila de consigna con su fase, y «Práctica (sin puntos)» a la izquierda y «Saltar tutorial» a la derecha en una fila de arriba, igual en los 17 juegos.
    /// </summary>
    public class GameHudTests
    {
        private sealed class Host : MonoBehaviour { }

        private readonly List<GameObject> _made = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            GameClock.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _made) if (go != null) Object.DestroyImmediate(go);
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

        private Host NewHost()
        {
            var go = new GameObject("Host");
            _made.Add(go);
            return go.AddComponent<Host>();
        }

        [Test]
        public void El_nivel_ya_no_se_ve_en_la_partida()
        {
            var hud = new GameHud(NewArea(), "Un juego", 48f, NewHost());
            hud.SetLevel(1);
            hud.SetLevel(2);
            hud.SetInfo("3 de 12");
            foreach (var t in hud.Rect.GetComponentsInChildren<Text>(true)) StringAssert.DoesNotContain("Nivel", t.text, t.name + ": «Nivel N» no se ve durante la partida");
            Assert.IsNull(hud.Rect.Find("LevelChip"), "ya no hay chip de nivel");
        }

        [Test]
        public void Fila_1_titulo_a_18_dp_y_el_avance_a_la_derecha_en_un_chip()
        {
            var hud = new GameHud(NewArea(), "Un juego", 60f, NewHost());
            hud.SetInfo("3 de 12");
            var title = hud.Rect.Find("Title").GetComponent<Text>();
            Assert.AreEqual(54, title.fontSize, "el título va a 18 dp (54 unidades)");
            var chip = (RectTransform)hud.Rect.Find("InfoChip");
            Assert.AreEqual(new Vector2(1f, 1f), chip.anchorMin, "el avance va arriba a la derecha");
            Assert.AreEqual("3 de 12", chip.Find("Text").GetComponent<Text>().text);
            Assert.AreEqual(48, chip.Find("Text").GetComponent<Text>().fontSize, "el chip va a 16 dp (48 unidades)");
            Assert.AreEqual(GameHud.Height, 190f, "el marcador mide lo mismo que antes: no mueve nada de abajo");
        }

        [Test]
        public void El_chip_de_seguidos_aparece_solo_desde_2()
        {
            var area = NewArea();
            var hud = new GameHud(area, "Un juego", 48f, NewHost());
            Assert.IsFalse(hud.StreakVisible, "sin racha no hay chip (antes una píldora grande que casi siempre decía «0»)");
            hud.SetStreak(1);
            Assert.IsFalse(hud.StreakVisible, "con 1 acierto todavía no");
            hud.SetStreak(2);
            Assert.IsTrue(hud.StreakVisible);
            Assert.AreEqual("×2 seguidos", hud.StreakText);
            hud.SetStreak(7);
            Assert.AreEqual("×7 seguidos", hud.StreakText);
            hud.SetStreak(0);
            Assert.IsFalse(hud.StreakVisible, "al fallar, el chip se va");
            Assert.AreEqual(GameHud.StreakFrom, 2);
            var withoutStreak = new GameHud(NewArea(), "Otro juego", 48f, NewHost(), withStreak: false);
            withoutStreak.SetStreak(5);
            Assert.IsFalse(withoutStreak.StreakVisible, "los juegos sin racha no la muestran");
        }

        [Test]
        public void La_fila_de_consigna_se_ve_solo_con_consigna_o_fase()
        {
            var hud = new GameHud(NewArea(), "Un juego", 48f, NewHost());
            Assert.IsFalse(hud.PromptRect.gameObject.activeSelf, "sin consigna no hay una losa vacía");
            hud.SetPrompt("Lanza el cohete que se enciende");
            Assert.IsTrue(hud.PromptRect.gameObject.activeSelf);
            Assert.AreEqual("Lanza el cohete que se enciende", hud.PromptText);
            hud.SetPhase("Mira el rastro");
            Assert.AreEqual("Mira el rastro", hud.PhaseText);
            hud.SetPrompt(null);
            Assert.IsTrue(hud.PromptRect.gameObject.activeSelf, "con fase, la fila sigue");
            hud.SetPhase("");
            Assert.IsFalse(hud.PromptRect.gameObject.activeSelf);
        }

        [Test]
        public void La_consigna_elige_la_version_que_cabe_en_una_linea()
        {
            var hud = new GameHud(NewArea(), "Un juego", 48f, NewHost());
            string longer = "Sector 1 de 3 · misión: triángulo con franja de color y con estrellas";
            hud.SetPromptFit(null, longer, "Misión: triángulo con franja", "triángulo");
            Assert.AreNotEqual(longer, hud.PromptText, "la versión larguísima no cabe en una línea a 16 dp");
            Assert.IsTrue(hud.FitsOneLine(hud.PromptText, false), "la elegida cabe");
            hud.SetPromptFit(null, "Mira el rastro", "Mira");
            Assert.AreEqual("Mira el rastro", hud.PromptText, "si la primera cabe, esa");
        }

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
        public void La_palabra_de_la_regla_de_Tinta_se_lee_sobre_la_fila_de_consigna()
        {
            // «Responde: TINTA / PALABRA»: la palabra con el color de su orilla (nunca solo color: lleva también la palabra y el ícono) y ≥ 4,5:1 sobre el cuerpo de la losa «noche» de la fila
            foreach (var rule in new[] { Games.Stroop.StroopRule.Ink, Games.Stroop.StroopRule.Word })
                Assert.GreaterOrEqual(Contrast(Games.Stroop.StroopContract.RuleLight(rule), GameHud.RowBody), 4.5f, rule + ": la palabra de la regla se lee sobre la fila");
            Assert.GreaterOrEqual(Contrast(NeuroStyle.Cream, GameHud.RowBody), 4.5f, "el texto de la consigna (crema) también");
            Assert.GreaterOrEqual(Contrast(NeuroStyle.Sun, GameHud.RowBody), 4.5f, "y la fase (sol)");
        }

        // ------------------------------------------------------------------ la pausa

        [Test]
        public void Reiniciar_pide_confirmacion_en_una_linea_y_el_subtitulo_va_a_16_dp()
        {
            int restarts = 0;
            var pause = PauseMenu.Create(NewArea(), () => { }, () => restarts++, () => { });
            _made.Add(pause.gameObject);
            pause.Show();
            var sub = pause.transform.Find("Panel/Subtitle").GetComponent<Text>();
            Assert.AreEqual("Tu partida te espera", sub.text);
            Assert.AreEqual(48, sub.fontSize, "«Tu partida te espera» va a 16 dp (48 unidades)");
            pause.transform.Find("Panel/Btn_Reiniciar").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(pause.Confirming, "«Reiniciar» no tira la partida de una vez: pregunta");
            Assert.AreEqual(0, restarts);
            Assert.AreEqual("¿Empezar de nuevo esta partida?", sub.text, "la pregunta va en UNA línea");
            Assert.IsFalse(pause.transform.Find("Panel/Btn_Continuar").gameObject.activeSelf, "mientras pregunta, solo «Sí» y «No»");
            Assert.IsTrue(pause.transform.Find("Panel/Btn_No, seguir jugando").gameObject.activeSelf);
            pause.ResumeFromBack();
            Assert.IsFalse(pause.Confirming, "Atrás en la pregunta vuelve al menú (no sigue la partida)");
            Assert.IsTrue(pause.IsShown);
            Assert.AreEqual("Tu partida te espera", sub.text);
            pause.transform.Find("Panel/Btn_Reiniciar").GetComponent<Button>().onClick.Invoke();
            pause.transform.Find("Panel/Btn_No, seguir jugando").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(pause.Confirming);
            Assert.AreEqual(0, restarts, "«No, seguir jugando» no reinicia");
            pause.transform.Find("Panel/Btn_Reiniciar").GetComponent<Button>().onClick.Invoke();
            pause.transform.Find("Panel/Btn_Sí, empezar de nuevo").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(1, restarts, "«Sí, empezar de nuevo» reinicia");
            Assert.IsFalse(pause.IsShown);
        }

        // ------------------------------------------------------------------ los controles del tutorial

        [Test]
        public void El_tutorial_pone_Practica_a_la_izquierda_y_Saltar_a_la_derecha_en_una_fila_de_arriba()
        {
            var area = NewArea();
            var tutorial = new GuidedTutorial(area, GameHud.Height + 10f);
            _made.Add(tutorial.Coach.gameObject);
            var root = area.Find("TutorialPractice");
            Assert.IsNotNull(root);
            var badge = (RectTransform)root.Find("Badge");
            var skip = (RectTransform)root.Find("Skip");
            Assert.AreEqual(new Vector2(0f, 1f), badge.anchorMin, "«Práctica (sin puntos)» arriba a la izquierda");
            Assert.AreEqual(new Vector2(1f, 1f), skip.anchorMin, "«Saltar tutorial» arriba a la derecha");
            Assert.AreEqual(badge.anchoredPosition.y, skip.anchoredPosition.y, 0.01f, "los dos en la MISMA fila");
            Assert.Less(badge.anchoredPosition.y, -GameHud.Height, "debajo del marcador");
            float badgeRight = badge.anchoredPosition.x + badge.sizeDelta.x;
            float skipLeft = 1080f + skip.anchoredPosition.x - skip.sizeDelta.x;
            Assert.Less(badgeRight, skipLeft, "no se pisan");
            Assert.GreaterOrEqual(skip.sizeDelta.y, 132f, "«Saltar tutorial» sigue siendo un blanco de 44 dp (132 unidades)");
            StringAssert.Contains("Práctica", badge.Find("Label").GetComponent<Text>().text);
            // PlaceControls (lo que llama cada juego) NO la mueve: ya no hay «abajo» ni «a media altura»
            var y = skip.anchoredPosition;
            tutorial.PlaceControls(GameHud.Height + 10f, skipAtTop: false, captionFromBottomU: 300f, badgeAtBottom: true, skipFromBottomU: 500f);
            Assert.AreEqual(y, skip.anchoredPosition, "los controles quedan en la fila fija, pida lo que pida el juego");
        }
    }
}
