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
        public void Toast_AvoidsAKeepOutZone_AndSaysSoWhenThereIsNoRoom()
        {
            var area = NewArea(1080f, 2200f);
            var toast = new Toast(area, NewHost(), 3f);
            toast.SetBelowHud();
            var zoneGo = new GameObject("zona", typeof(RectTransform));
            _made.Add(zoneGo);
            var zone = (RectTransform)zoneGo.transform;
            zone.SetParent(area, false);
            zone.anchorMin = zone.anchorMax = new Vector2(0.5f, 1f);
            zone.pivot = new Vector2(0.5f, 1f);
            zone.sizeDelta = new Vector2(1080f, 800f);
            zone.anchoredPosition = new Vector2(0f, -250f);                   // de 250 a 1050 desde arriba
            toast.KeepOut(zone);
            toast.Show("Aviso", "Un subtítulo corto", Color.white, 1f);        // la primera vuelta de su corrutina lo coloca
            Assert.GreaterOrEqual(toast.PlacedTopU, 1050f - 1f, "el aviso queda DEBAJO de la zona prohibida");
            Assert.AreEqual(0f, toast.PlacedOverlap, 0.01f, "y sin chocar con nada");
            zone.sizeDelta = new Vector2(1080f, 2100f);
            zone.anchoredPosition = new Vector2(0f, -120f);                   // ya no queda franja libre
            toast.Show("Aviso", "Un subtítulo corto", Color.white, 1f);
            Assert.Greater(toast.PlacedOverlap, 0f, "sin franja libre lo dice (el aviso espera «entre ensayos»)");
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
            Assert.GreaterOrEqual(games, 10, "se revisaron los juegos con aviso");
            Assert.IsEmpty(missing, "estos juegos crean un Toast y no lo ponen debajo del marcador (SetBelowHud): " + string.Join(", ", missing));
        }

        // ------------------------------------------------------------------ el chip de «seguidos» y las zonas del juego

        private RectTransform NewZone(RectTransform area, float topU, float heightU, float widthU = 1000f)
        {
            var go = new GameObject("zona", typeof(RectTransform));
            _made.Add(go);
            var zone = (RectTransform)go.transform;
            zone.SetParent(area, false);
            zone.anchorMin = zone.anchorMax = new Vector2(0.5f, 1f);
            zone.pivot = new Vector2(0.5f, 1f);
            zone.sizeDelta = new Vector2(widthU, heightU);
            zone.anchoredPosition = new Vector2(0f, -topU);
            return zone;
        }

        [Test]
        public void StreakChip_NeverCrossesARegisteredZone_AndSitsInTheNearestFreeStripBelowIt()
        {
            // la tarjeta del pedido de Bodega: de 200 a 380 desde arriba, casi todo el ancho. Sin registrarla, el chip caería en 226 (Height + 36), encima de ella.
            var area = NewArea();
            var plain = new GameHud(area, "Un título", 48f, NewHost());
            plain.SetStreak(4);
            var zoneForPlain = NewZone(area, 200f, 180f);
            Assert.Greater(ToastPlacement.Overlap(ToastPlacement.LocalRectOf(area, plain.StreakRect), ToastPlacement.LocalRectOf(area, zoneForPlain)), 0f,
                "(control) sin registrar la zona el chip SÍ la cruza: la prueba mide algo");

            var area2 = NewArea();
            var hud = new GameHud(area2, "Un título", 48f, NewHost());
            var zone = NewZone(area2, 200f, 180f);
            hud.AvoidForStreak(zone);
            hud.AvoidForStreak(zone);                                                                              // registrar dos veces no la duplica
            Assert.AreEqual(1, hud.StreakAvoidZones.Count);
            for (int streak = 2; streak <= 12; streak++)
            {
                hud.SetStreak(streak);
                var chip = ToastPlacement.LocalRectOf(area2, hud.StreakRect);
                var z = ToastPlacement.LocalRectOf(area2, zone);
                Assert.AreEqual(0f, ToastPlacement.Overlap(chip, z), 0.01f, "×" + streak + ": el chip cruza la zona registrada (chip " + chip + ", zona " + z + ")");
                Assert.GreaterOrEqual(chip.yMin, z.yMax - 0.01f, "×" + streak + ": el chip va DEBAJO de la tarjeta, en la franja libre más cercana");
                Assert.LessOrEqual(chip.yMin - z.yMax, 60f, "×" + streak + ": y pegado a ella, no lejos");
            }

            // una zona apagada (la tarjeta entre pedidos) cuenta igual
            zone.gameObject.SetActive(false);
            hud.SetStreak(13);
            Assert.AreEqual(0f, ToastPlacement.Overlap(ToastPlacement.LocalRectOf(area2, hud.StreakRect), ToastPlacement.LocalRectOf(area2, zone)), 0.01f, "una zona apagada se evita igual");

            // y varias zonas: el chip no cruza ninguna
            var area3 = NewArea();
            var hud3 = new GameHud(area3, "Un título", 48f, NewHost());
            var a = NewZone(area3, 200f, 120f); var b = NewZone(area3, 340f, 150f);
            hud3.AvoidForStreak(a); hud3.AvoidForStreak(b);
            hud3.SetStreak(5);
            var chip3 = ToastPlacement.LocalRectOf(area3, hud3.StreakRect);
            Assert.AreEqual(0f, ToastPlacement.Overlap(chip3, ToastPlacement.LocalRectOf(area3, a)) + ToastPlacement.Overlap(chip3, ToastPlacement.LocalRectOf(area3, b)), 0.01f, "con dos zonas, el chip no cruza ninguna");
        }

        [Test]
        public void InfoChip_PicksTheFirstVersionThatFitsNextToTheTitle()
        {
            var hud = new GameHud(NewArea(), "Acoplamiento", 48f, NewHost());
            hud.SetInfoFit("Módulo 3 de 24 · 8 a bordo");
            Assert.AreEqual("Módulo 3 de 24 · 8 a bordo", hud.InfoText, "una sola versión: es la que se pone");
            hud.SetInfoFit("3 · 8", "Módulo 3 · 8 a bordo");
            Assert.AreEqual("3 · 8", hud.InfoText, "la primera que cabe");
            string absurd = new string('M', 120);                                                     // no cabe en ninguna fila de 1080 unidades
            hud.SetInfoFit(absurd, "Módulo 3 · 8 a bordo");
            Assert.AreEqual("Módulo 3 · 8 a bordo", hud.InfoText, "si la más completa no cabe junto al título, pasa a la siguiente");
            hud.SetInfoFit(absurd, absurd + "N");
            Assert.AreEqual(absurd + "N", hud.InfoText, "si ninguna cabe, la última");
        }

        [Test]
        public void StreakChip_WithoutZones_KeepsItsPlaceBelowTheHud()
        {
            var area = NewArea();
            var hud = new GameHud(area, "Un título", 48f, NewHost());
            hud.SetStreak(3);
            var chip = ToastPlacement.LocalRectOf(area, hud.StreakRect);
            Assert.AreEqual(GameHud.Height + 36f, chip.yMin, 0.01f, "sin zonas registradas el chip queda donde siempre: debajo del marcador, a la derecha");
            Assert.AreEqual(0, hud.StreakAvoidZones.Count);
        }

        // ------------------------------------------------------------------ letra de 14 dp

        [Test]
        public void GameHud_NoTextIsSmallerThan14Dp_AndTheChipsNotSmallerThan16Dp()
        {
            var area = NewArea();
            var hud = new GameHud(area, "Un título cualquiera", 48f, NewHost());
            hud.SetLevel(1);
            hud.SetInfo("1 de 40");
            hud.SetPrompt("Una consigna cualquiera");
            hud.SetPhase("Una fase");
            hud.SetStreak(3);
            int checkedTexts = 0, chips = 0;
            var all = new List<Text>(hud.Rect.GetComponentsInChildren<Text>(true));
            all.AddRange(area.Find("StreakChip").GetComponentsInChildren<Text>(true));         // el chip de «seguidos» es hermano del marcador
            foreach (var t in all)
            {
                int smallest = t.resizeTextForBestFit ? t.resizeTextMinSize : t.fontSize;
                Assert.GreaterOrEqual(smallest, 42, t.name + ": el marcador no baja de 14 dp (42 unidades)");
                // el avance, «×N seguidos», la consigna y su fase van a 16 dp o más (48 unidades; Tarea 123, letra de 16) y el título a 18 dp (Tarea 127)
                string path = t.transform.parent != null ? t.transform.parent.name + "/" + t.name : t.name;
                if (path == "InfoChip/Text" || path == "StreakChip/Text" || path == "Prompt/Text" || path == "Prompt/Phase")
                {
                    Assert.GreaterOrEqual(smallest, 48, path + ": el marcador no baja de 16 dp (48 unidades)");
                    chips++;
                }
                checkedTexts++;
            }
            Assert.AreEqual(54, hud.Rect.Find("Title").GetComponent<Text>().fontSize, "el título va a 18 dp (54 unidades)");
            Assert.GreaterOrEqual(checkedTexts, 5, "título, avance, consigna, fase y seguidos");
            Assert.AreEqual(4, chips, "el avance, la consigna, su fase y «seguidos»");
        }

        private static readonly Regex MakeTextSize = new Regex(@"MakeText\(\s*[^,;]+,\s*(?:""[^""]*""|[A-Za-z_\.\+ ""]+?),\s*(\d+)\s*,");
        private static readonly Regex BestFitMin = new Regex(@"BestFit\(\s*[^,;]+,\s*(\d+)\s*\)");
        private static readonly Regex BestFitMax = new Regex(@"BestFit\(\s*[^,;]+,\s*Mathf\.Max\(\s*(\d+)");
        private static readonly Regex CenteredSize = new Regex(@"CenteredText\(\s*[^,;]+,\s*""[^""]*"",\s*(\d+)\s*,");
        private static readonly Regex ResultSize = new Regex(@"AddResultText\(\s*""[^""]*"",\s*(\d+)\s*,");
        /// <summary>Los rótulos de escena (<c>MakeLabel</c> / <c>MakeSceneLabel</c> en dp, Tarea 129), el último parámetro de <c>BuildPill</c> y los mínimos «<c>RoundToInt(NN f * UnitsPerDp)</c>» del ajuste automático: nada informativo bajo 16 dp.</summary>
        private static readonly Regex LabelDp = new Regex(@"Make(?:Scene)?Label\([^,()]+,\s*[^,()]+(?:\([^()]*\))?[^,()]*,\s*(\d+(?:\.\d+)?)f,");
        private static readonly Regex PillDp = new Regex(@"BuildPill\([^;]*,\s*(\d+(?:\.\d+)?)f\)");
        private static readonly Regex FitDp = new Regex(@"RoundToInt\((\d+(?:\.\d+)?)f \* (?:UnitsPerDp|_s)\)");

        /// <summary>
        /// Lo que ningún juego dibuja a menos de 16 dp (48 unidades del lienzo de 1080; Tarea 128: «nada informativo bajo 16 dp»): se mira el CÓDIGO (el smoke solo ve lo que sale en pantalla durante su corrida).
        /// Excepciones, cada una con su razón: Bitácora de Misión, Rumbo a Casa y Cosecha de palabras (retirados de la app: el código se conserva pero no se juegan) y el panel de resultado de Unity (<c>AddResultText</c>: desde la Tarea 126 Unity no dibuja ningún resultado y esos textos se construyen sin usarse; siguen en 42).
        /// </summary>
        [Test]
        public void NoGameDrawsAnInformativeTextBelow16Dp_InItsSources()
        {
            var root = Path.Combine(Application.dataPath, "Scripts", "Games");
            var found = new List<string>();
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(root.Length + 1).Replace('\\', '/');
                if (rel.Contains("/Tests/") || rel.StartsWith("Bitacora/") || rel.StartsWith("Rumbo/") || rel.StartsWith("Cosecha/")) continue;
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("//") || trimmed.StartsWith("*") || trimmed.StartsWith("///")) continue;
                    if (rel == "Shared/NubiCoach.cs" && line.Contains("_dbgText")) continue;                  // el rótulo de depuración del foco: solo en builds de prueba
                    foreach (var rx in new[] { MakeTextSize, BestFitMin, BestFitMax, CenteredSize, ResultSize })
                    {
                        int min = rx == ResultSize ? 42 : 48;                                   // el panel de resultado de Unity ya no se dibuja (Tarea 126): se queda en 14 dp
                        foreach (Match m in rx.Matches(line))
                            if (int.Parse(m.Groups[1].Value) < min) found.Add(rel + ":" + (i + 1) + " " + trimmed);
                    }
                    foreach (var rx in new[] { LabelDp, PillDp, FitDp })                                    // Tarea 129: también los rótulos de escena y los mínimos del ajuste automático, en dp
                        foreach (Match m in rx.Matches(line))
                            if (float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) < 16f && !(rx == FitDp && !line.Contains("fontSize") && !line.Contains("resizeTextMinSize") && !line.Contains("BestFit")))
                                found.Add(rel + ":" + (i + 1) + " " + trimmed);
                }
            }
            Assert.IsEmpty(found, "letra bajo 16 dp (48 unidades): se parte en dos renglones o se acorta el texto, no se achica:\n" + string.Join("\n", found));
        }

        // ------------------------------------------------------------------ el vocabulario de lo que se dibuja (Tarea 154)

        /// <summary>Lo que SÍ va en mayúscula en pantalla: es el estímulo del juego, no un título. Tinta o Palabra (las dos orillas y los nombres de los colores), Freno de Emergencia (la señal «ALTO») y la ficha «CASA» del tutorial de En la punta de la lengua (las fichas son letras en mayúscula, como las de la partida).</summary>
        private static readonly HashSet<string> StimulusCaps = new HashSet<string> { "TINTA", "PALABRA", "ROJO", "AZUL", "AMARILLO", "BLANCO", "ALTO", "CASA" };

        /// <summary>Archivos con mayúsculas que NO se leen como texto: el banco de palabras y las letras de En la punta de la lengua (estímulos), los identificadores de la configuración de edad, las guardas del tutorial y la versión de Android.</summary>
        private static readonly string[] CapsFilesOutOfScope = { "Anagramas/PuntaBank.cs", "Anagramas/PuntaContract.cs", "DdaUserProfileConfig.cs", "Shared/TutorialGuards.cs", "Shared/GameFeel.cs" };

        private static readonly Regex StringLiteral = new Regex("(?<![\\\\@])\"((?:[^\"\\\\\\r\\n]|\\\\.)*)\"");
        private static readonly Regex Interpolation = new Regex(@"\{[^{}]*\}");
        private static readonly Regex CapsWord = new Regex(@"(?<![A-Za-zÁÉÍÓÚÜÑáéíóúüñ])[A-ZÁÉÍÓÚÜÑ]{2,}(?![A-Za-zÁÉÍÓÚÜÑáéíóúüñ])");
        private static readonly Regex BanishedWord = new Regex(@"\b(racha|rachas|nivel|niveles|reto|precisi[oó]n|sesi[oó]n|cerebro|entrenamiento|cognitiv\w*)\b|camino de hoy", RegexOptions.IgnoreCase);
        private static readonly Regex ToUpperCall = new Regex(@"\.ToUpper(?:Invariant)?\(\)");

        /// <summary>
        /// Cada texto entre comillas del CÓDIGO de los juegos que se dibuja (Tarea 154): sin comentarios, sin lo que solo existe en el Editor (<c>#if UNITY_EDITOR</c>), sin bitácoras, excepciones ni mensajes de las auditorías del smoke (<c>problems.Add</c>, <c>Check(</c>…).
        /// Los juegos retirados (Bitácora, Rumbo, Cosecha) quedan fuera: el código se conserva pero no se juegan. Devuelve archivo, renglón, texto sin las llaves de interpolación y el renglón entero.
        /// </summary>
        private static List<(string rel, int line, string text, string code)> DrawnStringLiterals()
        {
            var root = Path.Combine(Application.dataPath, "Scripts", "Games");
            var found = new List<(string, int, string, string)>();
            string[] diagnostics = { "Debug.Log", "LogDiagnostic", "throw ", "Assert.", "Expect(", "problems.Add(", "Check(", "Bare(", "string tag =", "tag +", "nameof(", "Application.", "Environment." };
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(root.Length + 1).Replace('\\', '/');
                if (rel.StartsWith("Tests/") || rel.Contains("/Tests/") || rel.Contains("/Editor/") || rel.StartsWith("Bitacora/") || rel.StartsWith("Rumbo/") || rel.StartsWith("Cosecha/")) continue;
                var lines = File.ReadAllLines(file);
                bool editorOnly = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (trimmed.StartsWith("#if UNITY_EDITOR")) { editorOnly = true; continue; }
                    if (trimmed.StartsWith("#else") || trimmed.StartsWith("#elif") || trimmed.StartsWith("#endif")) { editorOnly = false; continue; }
                    if (editorOnly || trimmed.StartsWith("//") || trimmed.StartsWith("*") || trimmed.StartsWith("/*")) continue;
                    string code = lines[i];
                    int cut = code.IndexOf("//", System.StringComparison.Ordinal);
                    if (cut >= 0 && StringLiteral.Matches(code.Substring(0, cut)).Count == StringLiteral.Matches(code).Count) code = code.Substring(0, cut);
                    bool diagnostic = false;
                    foreach (var d in diagnostics) if (code.Contains(d)) { diagnostic = true; break; }
                    if (diagnostic) continue;
                    foreach (Match m in StringLiteral.Matches(code))
                        found.Add((rel, i + 1, Interpolation.Replace(m.Groups[1].Value, ""), trimmed));
                }
            }
            return found;
        }

        /// <summary>
        /// Sin mayúsculas sostenidas (<c>DESIGN.md</c> §1; Tarea 154): ninguna palabra de 4 letras o más ni dos palabras seguidas en mayúscula en lo que se dibuja, salvo el estímulo de Tinta o Palabra, la señal «ALTO» de Freno y la ficha «CASA» (<see cref="StimulusCaps"/>).
        /// La fuerza de un título la da el color y el peso, no las mayúsculas. Tampoco un <c>.ToUpper()</c> sobre un texto.
        /// </summary>
        [Test]
        public void NoGameDrawsSustainedCapitals_InItsSources()
        {
            var found = new List<string>();
            foreach (var (rel, line, text, code) in DrawnStringLiterals())
            {
                if (System.Array.IndexOf(CapsFilesOutOfScope, rel) >= 0) continue;
                var words = new List<string>();
                foreach (Match m in CapsWord.Matches(text)) if (!StimulusCaps.Contains(m.Value)) words.Add(m.Value);
                bool longWord = words.Exists(w => w.Length >= 4);
                if (longWord || words.Count >= 2) found.Add(rel + ":" + line + " «" + text + "»");
                if (rel != "DdaUserProfileConfig.cs" && ToUpperCall.IsMatch(code)) found.Add(rel + ":" + line + " pasa un texto a mayúsculas: " + code);
            }
            Assert.IsEmpty(found, "mayúsculas sostenidas en lo que se dibuja (se escribe en caja normal; solo el estímulo del juego va en mayúscula):\n" + string.Join("\n", found));
        }

        /// <summary>
        /// Las palabras que la app ya no dice (Tarea 154, vocabulario de <c>DESIGN.md</c> §1 y Tarea 150): «racha» (en los juegos es «seguidos»; la racha es la de días), «nivel» (no se ve), «Reto» y «Precisión» (son «Con reloj» y «Sin reloj»), «sesión» (es «ruta»), «camino de hoy»,
        /// «cerebro», «entrenamiento» y «cognitivo». Se mira lo que se dibuja, no los identificadores (<c>PrecisionRounds</c>) ni las bitácoras del smoke.
        /// </summary>
        [Test]
        public void NoGameDrawsTheBanishedWords_InItsSources()
        {
            var found = new List<string>();
            foreach (var (rel, line, text, _) in DrawnStringLiterals())
            {
                var m = BanishedWord.Match(text);
                if (m.Success) found.Add(rel + ":" + line + " «" + text + "» dice «" + m.Value + "»");
            }
            Assert.IsEmpty(found, "palabras desterradas en lo que se dibuja:\n" + string.Join("\n", found));
        }

        private static readonly Regex GluedPercent = new Regex(@"(^|\d)%");
        private static readonly Regex AbbreviatedNumber = new Regex(@"[nN]\.º");

        /// <summary>
        /// Sin signos ni abreviaturas en lo que se dibuja (Tarea 161; regla de Ricardo «sin palabras sueltas», la misma que la app desde la Tarea 160): nada de «×» (la excepción es el «×» de la cuenta de Carga exacta: es el signo de multiplicar de un operador, no un texto),
        /// nada de «n.º» (se dice «número») y un «%» nunca pegado a la cifra: «40 %», no «40%» (tampoco el «%» suelto de una concatenación, <c>p + "%"</c>). Los juegos retirados, los <c>Debug.Log</c> y lo del Editor quedan fuera.
        /// </summary>
        [Test]
        public void NoGameDrawsSignsOrGluedPercents_InItsSources()
        {
            var found = new List<string>();
            foreach (var (rel, line, text, _) in DrawnStringLiterals())
            {
                if (text.Contains("×") && !(rel == "Calculo/CalculoContract.cs" && text == "×")) found.Add(rel + ":" + line + " «" + text + "» dice «×»");
                if (AbbreviatedNumber.IsMatch(text)) found.Add(rel + ":" + line + " «" + text + "» dice «n.º»");
                if (GluedPercent.IsMatch(text)) found.Add(rel + ":" + line + " «" + text + "» trae el «%» pegado a la cifra");
            }
            Assert.IsEmpty(found, "signos y abreviaturas en lo que se dibuja («3 seguidos», «número 3», «40 %»):\n" + string.Join("\n", found));
        }

        [Test]
        public void TheSignsGuard_CatchesWhatItShould_AndLetsThroughWhatIsAllowed()
        {
            foreach (var bad in new[] { "×3 seguidos", "Puntos × 1,5", "Cohete n.º 3", "40%", "%", "100%" })
                Assert.IsTrue(bad.Contains("×") || AbbreviatedNumber.IsMatch(bad) || GluedPercent.IsMatch(bad), "la guardia no atrapa «" + bad + "»");
            foreach (var good in new[] { "3 seguidos", "50 % más de puntos", "Cohete número 3", "40 %", "0 %", "a 5 % del blanco" })
                Assert.IsFalse(AbbreviatedNumber.IsMatch(good) || GluedPercent.IsMatch(good), "la guardia atrapa «" + good + "» sin motivo");
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
