using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// Lo que se vio mal en las capturas reales del 8-oct y NO debe volver (Tarea 54): los avisos tapaban el marcador de arriba, la letra bajaba de 14 dp, la fila de puntos de avance se salía de la pantalla y las esquinas del foco
    /// del tutorial quedaban a la vista en un aviso. Aparte, el smoke mide cada texto visible a 14 dp exactos y que nada tape el HUD (<c>HeadlessPlaymodeSmokeTest</c>); estas pruebas cubren lo que el smoke no alcanza a ver.
    /// </summary>
    public class HudRulesTests
    {
        private sealed class Host : MonoBehaviour { }

        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _made) if (go != null) Object.DestroyImmediate(go);
            _made.Clear();
        }

        private RectTransform NewArea(float w = 1080f, float h = 2200f)
        {
            var go = new GameObject("Area");
            _made.Add(go);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private Host NewHost()
        {
            var go = new GameObject("Host");
            _made.Add(go);
            return go.AddComponent<Host>();
        }

        // ------------------------------------------------------------------ avisos debajo del marcador

        [Test]
        public void Toast_SetBelowHud_PutsTheNoticeBelowTheHud()
        {
            var toast = new Toast(NewArea(), NewHost(), 3f);
            toast.SetBelowHud();
            Assert.GreaterOrEqual(toast.TopOffsetU, GameHud.Height + Toast.BelowHudGap - 0.01f, "el aviso queda debajo del marcador, no encima del título");
            toast.SetBelowHud(60f);
            Assert.AreEqual(GameHud.Height + Toast.BelowHudGap + 60f, toast.TopOffsetU, 0.01f, "el extra baja el aviso todavía más");
        }

        [Test]
        public void EveryGameWithAToast_PutsItBelowTheHud()
        {
            var root = Path.Combine(Application.dataPath, "Scripts", "Games");
            var missing = new List<string>();
            int games = 0;
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(root.Length + 1).Replace('\\', '/');
                if (rel.Contains("/Tests/") || rel.StartsWith("Shared/")) continue;
                string text = File.ReadAllText(file);
                if (!text.Contains("new Toast(")) continue;
                games++;
                if (!text.Contains("_toast.SetBelowHud(")) missing.Add(rel);
            }
            Assert.Greater(games, 10, "se revisaron los juegos con aviso");
            Assert.IsEmpty(missing, "estos juegos crean un Toast y no lo ponen debajo del marcador (SetBelowHud): " + string.Join(", ", missing));
        }

        // ------------------------------------------------------------------ letra de 14 dp

        [Test]
        public void GameHud_NoTextIsSmallerThan14Dp()
        {
            var hud = new GameHud(NewArea(), "Un título cualquiera", 48f, NewHost());
            hud.SetLevel(1);
            hud.SetInfo("1 de 40");
            int checkedTexts = 0;
            foreach (var t in hud.Rect.GetComponentsInChildren<Text>(true))
            {
                int smallest = t.resizeTextForBestFit ? t.resizeTextMinSize : t.fontSize;
                Assert.GreaterOrEqual(smallest, 42, t.name + ": el marcador no baja de 14 dp (42 unidades)");
                checkedTexts++;
            }
            Assert.GreaterOrEqual(checkedTexts, 4, "título, nivel, avance y racha");
        }

        private static readonly Regex MakeTextSize = new Regex(@"MakeText\(\s*[^,;]+,\s*(?:""[^""]*""|[A-Za-z_\.\+ ""]+?),\s*(\d+)\s*,");
        private static readonly Regex BestFitMin = new Regex(@"BestFit\(\s*[^,;]+,\s*(\d+)\s*\)");
        private static readonly Regex CenteredSize = new Regex(@"CenteredText\(\s*[^,;]+,\s*""[^""]*"",\s*(\d+)\s*,");
        private static readonly Regex ResultSize = new Regex(@"AddResultText\(\s*""[^""]*"",\s*(\d+)\s*,");

        /// <summary>Lo que ningún juego dibuja a menos de 14 dp (42 unidades del lienzo de 1080): se mira el CÓDIGO (el smoke solo ve lo que sale en pantalla durante su corrida). Retirada: Bitácora de Misión.</summary>
        [Test]
        public void NoGameDrawsATextBelow14Dp_InItsSources()
        {
            var root = Path.Combine(Application.dataPath, "Scripts", "Games");
            var found = new List<string>();
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(root.Length + 1).Replace('\\', '/');
                if (rel.Contains("/Tests/") || rel.StartsWith("Bitacora/")) continue;
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("//") || trimmed.StartsWith("*") || trimmed.StartsWith("///")) continue;
                    if (rel == "Shared/NubiCoach.cs" && line.Contains("_dbgText")) continue;                  // el rótulo de depuración del foco: solo en builds de prueba
                    foreach (var rx in new[] { MakeTextSize, BestFitMin, CenteredSize, ResultSize })
                        foreach (Match m in rx.Matches(line))
                            if (int.Parse(m.Groups[1].Value) < 42) found.Add(rel + ":" + (i + 1) + " " + trimmed);
                }
            }
            Assert.IsEmpty(found, "letra bajo 14 dp (42 unidades): se parte en dos renglones o se acorta el texto, no se achica:\n" + string.Join("\n", found));
        }

        // ------------------------------------------------------------------ puntos de avance

        [Test]
        public void ProgressDots_AlwaysFitTheScreenWith16DpOfMargin()
        {
            foreach (int count in new[] { 5, 8, 16, 24, 40 })
            {
                var dots = new ProgressDots(NewArea(), NewHost(), 3f, count);
                _made.Add(dots.Rect.gameObject);
                var layout = dots.Rect.GetComponent<HorizontalLayoutGroup>();
                float total = layout.spacing * (count - 1);
                foreach (RectTransform d in dots.Rect) total += d.sizeDelta.x;
                Assert.LessOrEqual(total, ProgressDots.ReferenceWidthU - 2f * ProgressDots.MarginDp * 3f + 0.5f, count + " puntos: la fila cabe con 16 dp de margen a cada lado");
                Assert.LessOrEqual(dots.Rect.sizeDelta.x, ProgressDots.ReferenceWidthU - 2f * ProgressDots.MarginDp * 3f + 0.01f);
            }
        }

        // ------------------------------------------------------------------ el foco del tutorial no deja esquinas oscuras en un aviso

        [Test]
        public void CoachNotice_AfterAFocus_LeavesNoCornerOrVeilPieceVisible()
        {
            GameClock.Reset();
            var area = NewArea(1000f, 2000f);
            var coach = NubiCoach.Create(area, p => false, () => false);
            _made.Add(coach.gameObject);
            var watch = coach.Watch(() => new Rect(-100f, -100f, 200f, 200f), "Mira", () => false, 5f, circle: true);
            watch.MoveNext();                                    // abre el foco: velo, esquinas y aro
            int cornersShown = 0;
            foreach (Transform c in coach.transform) if (c.name.StartsWith("Corner") && c.gameObject.activeSelf) cornersShown++;
            Assert.Greater(cornersShown, 0, "el foco redondea el hueco con esquinas oscuras");

            var notice = coach.Notice("¡Eso es!");
            notice.MoveNext();                                   // el aviso NO lleva velo: nada del foco anterior puede quedar
            foreach (Transform c in coach.transform)
            {
                bool focusPiece = c.name.StartsWith("Corner") || c.name.StartsWith("Veil") || c.name == "Frame" || c.name == "Ring";
                Assert.IsFalse(focusPiece && c.gameObject.activeSelf, c.name + " sigue a la vista en un aviso (cuadrado oscuro alrededor de lo último iluminado)");
            }
            coach.Hide();
            GameClock.Reset();
        }
    }
}
