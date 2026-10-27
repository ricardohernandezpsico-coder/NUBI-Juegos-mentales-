using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// Los avisos de los juegos los dice la fila de consigna del marcador (<see cref="GameHud.Announce"/>, Tarea 179, «La fila habla»): se tiñe con el tono, cambia su ícono por la forma del tono, dice el aviso en UNA línea con la letra en tinta y vuelve sola a su consigna.
    /// Ningún aviso flota ni tapa nada, nunca hay dos a la vez, y si el juego cambia la consigna mientras habla, la consigna manda. Aquí también se mide que todos los textos de aviso de los juegos caben en una línea a 16 dp.
    /// </summary>
    public class HudAnnounceTests
    {
        private sealed class Host : MonoBehaviour { }

        private readonly List<GameObject> _made = new List<GameObject>();
        private bool _sound;

        [SetUp]
        public void SetUp()
        {
            GameClock.Reset();
            GameFeel.ReduceMotion = false;
            _sound = GameFeel.SoundOn;
            GameFeel.SoundOn = false;                      // el aviso pide su sonido al terminar el cuadro: en la prueba no suena nada
#if UNITY_EDITOR
            GameHud.AnnouncesCutShort.Clear();
            GameHud.AnnouncesShown = 0;
#endif
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _made) if (go != null) Object.DestroyImmediate(go);
            _made.Clear();
            GameFeel.ReduceMotion = false;
            GameFeel.SoundOn = _sound;
            GameClock.Reset();
        }

        private GameHud NewHud()
        {
            var area = new GameObject("Area");
            _made.Add(area);
            var rect = area.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1080f, 2200f);
            var host = new GameObject("Host");
            _made.Add(host);
            return new GameHud(rect, "Un título", 60f, host.AddComponent<Host>());
        }

        private static float Luminance(Color c)
        {
            float L(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * L(c.r) + 0.7152f * L(c.g) + 0.0722f * L(c.b);
        }

        private static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        // ------------------------------------------------------------------ la fila habla y vuelve sola

        [Test]
        public void TheRowSpeaksWithTheTonesIconAndInkText_AndReturnsToItsPromptByItself()
        {
            var hud = NewHud();
            hud.SetPrompt("Lanza el cohete que se enciende");
            hud.Announce("¡Un paso más!", AnnounceTone.Step);
            Assert.IsTrue(hud.Announcing);
            Assert.AreEqual("¡Un paso más!", hud.RowText);
            Assert.AreEqual("Lanza el cohete que se enciende", hud.PromptText, "la consigna que vuelve sigue siendo la de siempre");
            Assert.AreSame(AnnounceStyle.Glyph(AnnounceTone.Step), hud.RowIcon, "la forma del tono");
            Assert.AreEqual(AnnounceTone.Step, hud.AnnounceToneNow);
            var c = hud.RowTextComponent.color;
            Assert.AreEqual(NeuroStyle.Ink.r, c.r, 0.001f, "la letra va en tinta sobre el tono");
            Assert.AreEqual(NeuroStyle.Ink.g, c.g, 0.001f);

            hud.AdvanceAnnounce(0.1f);
            Assert.IsTrue(hud.Announcing, "sigue a la vista");
            hud.AdvanceAnnounce(GameHud.LineSeconds + GameHud.AnnounceFade);
            Assert.IsFalse(hud.Announcing, "vuelve sola");
            Assert.AreEqual("Lanza el cohete que se enciende", hud.RowText);
            Assert.IsNull(hud.RowIcon, "sin su ícono: el de la consigna (ninguno)");
            Assert.AreEqual(NeuroStyle.Cream.r, hud.RowTextComponent.color.r, 0.001f, "la letra crema de la consigna");
            hud.AdvanceAnnounce(GameHud.AnnounceFade + 0.01f);
            Assert.AreEqual(1f, hud.RowTextComponent.color.a, 0.001f, "la consigna vuelve con un fundido y queda del todo visible");
        }

        [Test]
        public void ARowWithoutAPromptShowsOnlyWhileItSpeaks()
        {
            var hud = NewHud();
            Assert.IsFalse(hud.PromptRect.gameObject.activeSelf, "sin consigna no hay fila");
            hud.Announce("¡Nuevo límite!", AnnounceTone.Milestone);
            Assert.IsTrue(hud.PromptRect.gameObject.activeSelf);
            hud.AdvanceAnnounce(10f);
            Assert.IsFalse(hud.PromptRect.gameObject.activeSelf, "y se vuelve a ir");
        }

        [Test]
        public void TheIconAndThePhaseComeBackAfterTheAnnouncement()
        {
            var hud = NewHud();
            var icon = AnnounceStyle.Glyph(AnnounceTone.Info);         // cualquier sprite sirve de «ícono de la consigna»
            hud.SetPrompt("Mira el rastro", icon);
            hud.SetPhase("Repítelo");
            hud.Announce("Con calma", AnnounceTone.Pace);
            Assert.AreSame(AnnounceStyle.Glyph(AnnounceTone.Pace), hud.RowIcon);
            Assert.AreEqual("Repítelo", hud.PhaseText, "la fase se esconde mientras habla pero no se pierde");
            hud.AdvanceAnnounce(10f);
            Assert.AreSame(icon, hud.RowIcon);
            Assert.AreEqual("Repítelo", hud.PhaseText);
            Assert.AreEqual("Mira el rastro", hud.RowText);
        }

        [Test]
        public void NeverTwoAtOnce_ANewOneReplacesTheOldOne()
        {
            var hud = NewHud();
            hud.SetPrompt("Consigna");
            hud.Announce("¡Un paso más!", AnnounceTone.Step);
            hud.AdvanceAnnounce(0.5f);
            hud.Announce("Con calma", AnnounceTone.Pace);
            Assert.AreEqual("Con calma", hud.RowText);
            Assert.AreEqual(AnnounceTone.Pace, hud.AnnounceToneNow);
            Assert.AreEqual("Consigna", hud.PromptText, "lo que vuelve es la consigna, no el aviso pisado");
            hud.AdvanceAnnounce(10f);
            Assert.AreEqual("Consigna", hud.RowText);
#if UNITY_EDITOR
            Assert.AreEqual(1, GameHud.AnnouncesCutShort.Count, "el smoke cuenta los avisos pisados antes de leerse");
#endif
        }

        [Test]
        public void ThePromptWins_ADifferentPromptEndsTheAnnouncement_TheSameOneDoesNot()
        {
            var hud = NewHud();
            hud.SetPrompt("Responde: TINTA");
            hud.Announce("¡Un paso más!", AnnounceTone.Step);
            hud.SetPrompt("Responde: TINTA");                          // el juego la repite en cada ensayo: no interrumpe
            Assert.IsTrue(hud.Announcing);
            hud.SetPrompt("Responde: PALABRA");                        // cambió la regla: manda la consigna
            Assert.IsFalse(hud.Announcing);
            Assert.AreEqual("Responde: PALABRA", hud.RowText);
            Assert.AreEqual(1f, hud.RowTextComponent.color.a, 0.001f, "la consigna nueva se ve de una vez");
        }

        [Test]
        public void APhaseUpdate_DoesNotEndTheAnnouncement_ItComesBackWithTheRow()
        {
            var hud = NewHud();
            hud.SetPrompt("Responde: TINTA");
            hud.SetPhase("1:54");
            hud.Announce("La tinta era azul", AnnounceTone.Error);
            hud.SetPhase("1:53");                                       // el reloj del Contrarreloj cambia cada segundo
            Assert.IsTrue(hud.Announcing, "el reloj no interrumpe el aviso");
            Assert.AreEqual("La tinta era azul", hud.RowText);
            hud.AdvanceAnnounce(10f);
            Assert.AreEqual("1:53", hud.PhaseText, "y al volver la fila lleva el tiempo de ahora, no el de antes");
        }

        [Test]
        public void CancelAnnounce_BringsBackThePromptAtOnce()
        {
            var hud = NewHud();
            hud.SetPrompt("Consigna");
            hud.Announce("¡Ráfaga!", AnnounceTone.Info);
            hud.CancelAnnounce();
            Assert.IsFalse(hud.Announcing);
            Assert.AreEqual("Consigna", hud.RowText);
            Assert.AreEqual(1f, hud.RowTextComponent.color.a, 0.001f);
        }

        // ------------------------------------------------------------------ tiempos y «quitar animaciones»

        [Test]
        public void ASubtitleWhichFitsGoesInTheSameLine_AndLastsTwoSeconds()
        {
            var hud = NewHud();
            hud.Announce("¡Un paso más!", AnnounceTone.Step, 0f, "Más difícil");
            Assert.AreEqual("¡Un paso más! · Más difícil", hud.RowText);
            hud.AdvanceAnnounce(GameHud.SubtitleSeconds - 0.1f);
            Assert.IsTrue(hud.Announcing, "2 s a la vista con subtítulo");
            hud.AdvanceAnnounce(0.1f + GameHud.AnnounceFade + 0.01f);
            Assert.IsFalse(hud.Announcing);
        }

        [Test]
        public void ATextWhichDoesNotFitInOneLineIsSaidInTwoBeatsInTheSameRow()
        {
            var hud = NewHud();
            const string sub = "Una frase de explicación que de ninguna manera cabe junto a su título en una sola línea";
            Assert.AreEqual(2, hud.AnnounceBeats("La turbina giró al revés", sub).Length);
            Assert.AreEqual(1, hud.AnnounceBeats("Con calma", "Más fácil").Length);
            hud.Announce("La turbina giró al revés", AnnounceTone.Error, 3.2f, sub);
            Assert.AreEqual("La turbina giró al revés", hud.RowText, "primero el texto");
            hud.AdvanceAnnounce(GameHud.LineSeconds + GameHud.AnnounceFade + 0.01f);
            Assert.IsTrue(hud.Announcing);
            Assert.AreEqual(sub, hud.RowText, "después el subtítulo, en la misma fila y con el mismo tono");
            Assert.AreEqual(AnnounceTone.Error, hud.AnnounceToneNow);
            Assert.AreSame(AnnounceStyle.Glyph(AnnounceTone.Error), hud.RowIcon);
            hud.AdvanceAnnounce(2f - 0.1f);
            Assert.IsTrue(hud.Announcing, "el subtítulo dura lo que dura la pista: 2 s (3,2 s en total, lo que duraba la franja de Engranajes)");
            hud.AdvanceAnnounce(0.1f + GameHud.AnnounceFade + 0.01f);
            Assert.IsFalse(hud.Announcing);
        }

        [Test]
        public void TheSecondBeatCanHaveItsOwnTone_TheHelpOfEngranajesIsInfoNotASecondError()
        {
            var hud = NewHud();
            const string sub = "Una pista que de ninguna manera cabe junto a su título en una sola línea";
            hud.Announce("La turbina giró al revés", AnnounceTone.Error, 3.2f, sub, subtitleTone: AnnounceTone.Info);
            Assert.AreEqual(AnnounceTone.Error, hud.AnnounceToneNow, "lo que falló, en coral");
            Assert.AreSame(AnnounceStyle.Glyph(AnnounceTone.Error), hud.RowIcon);
            hud.AdvanceAnnounce(GameHud.LineSeconds + GameHud.AnnounceFade + 0.01f);
            Assert.AreEqual(sub, hud.RowText);
            Assert.AreEqual(AnnounceTone.Info, hud.AnnounceToneNow, "la pista, en crema con el punto: es ayuda");
            Assert.AreSame(AnnounceStyle.Glyph(AnnounceTone.Info), hud.RowIcon, "y con el glifo de su tono");
        }

        [Test]
        public void ReducedMotion_ChangesTheTextWithoutTransition_ButWithTheSameReadingTime()
        {
            GameFeel.ReduceMotion = true;
            var hud = NewHud();
            hud.SetPrompt("Consigna");
            hud.Announce("¡Un paso más!", AnnounceTone.Step);
            Assert.AreEqual(1f, hud.RowTextComponent.color.a, 0.001f, "aparece de una vez, sin fundido");
            Assert.AreEqual(Vector3.one, hud.PromptRect.localScale, "y sin rebote");
            hud.AdvanceAnnounce(GameHud.LineSeconds - 0.05f);
            Assert.IsTrue(hud.Announcing, "se lee el mismo tiempo");
            hud.AdvanceAnnounce(0.06f);
            Assert.IsFalse(hud.Announcing);
            Assert.AreEqual(1f, hud.RowTextComponent.color.a, 0.001f, "y la consigna vuelve de una vez");
        }

        [Test]
        public void AnExplicitDurationIsKept_TheContrarrelojErrorCanBeShort()
        {
            var hud = NewHud();
            hud.Announce("La tinta era azul", AnnounceTone.Error, 0.8f);
            hud.AdvanceAnnounce(0.8f + GameHud.AnnounceFade + 0.01f);
            Assert.IsFalse(hud.Announcing);
        }

        // ------------------------------------------------------------------ los tonos

        [Test]
        public void EveryToneHasItsOwnShape_ItsKitSlab_AndInkReadsOnIt()
        {
            var seen = new HashSet<string>();
            foreach (var tone in AnnounceStyle.All)
            {
                Assert.IsNotNull(AnnounceStyle.Glyph(tone), tone.ToString());
                Assert.GreaterOrEqual(Contrast(NeuroStyle.Ink, AnnounceStyle.Fill(tone)), 4.5f, "la letra en tinta sobre " + tone + " se lee (≥ 4,5:1)");
                Assert.IsTrue(PlasticSlab.Available(SlabSize.Chip, AnnounceStyle.Slab(tone)) || !PlasticArtPresent(), "la losa de " + tone + " existe en el kit");
                seen.Add(AnnounceStyle.Shape(tone));
            }
            Assert.AreEqual(5, seen.Count, "flecha, visto, reloj, equis y punto: cinco formas");
            Assert.AreNotEqual(AnnounceStyle.Slab(AnnounceTone.Step), AnnounceStyle.Slab(AnnounceTone.Milestone), "lima es subir un paso; sol es SOLO un hito");
            Assert.AreEqual(AnnounceStyle.Shape(AnnounceTone.Done), AnnounceStyle.Shape(AnnounceTone.Milestone));
            Assert.AreNotEqual(AnnounceStyle.Slab(AnnounceTone.Info), SlabTone.Night, "la crema no se confunde con la fila en reposo (noche)");
        }

        private static bool PlasticArtPresent() => PlasticArt.Group(PlasticSlab.Group) != null;

        [Test]
        public void TheGlyphsAreDifferentPictures_NotJustDifferentColors()
        {
            var pics = new Dictionary<string, Color32[]>();
            foreach (var tone in new[] { AnnounceTone.Step, AnnounceTone.Done, AnnounceTone.Pace, AnnounceTone.Error, AnnounceTone.Info })
                pics[AnnounceStyle.Shape(tone)] = AnnounceStyle.Render(tone, 64);
            var names = new List<string>(pics.Keys);
            for (int i = 0; i < names.Count; i++)
                for (int j = i + 1; j < names.Count; j++)
                {
                    int diff = 0;
                    for (int k = 0; k < pics[names[i]].Length; k++) if (Mathf.Abs(pics[names[i]][k].a - pics[names[j]][k].a) > 100) diff++;
                    Assert.Greater(diff, 150, names[i] + " y " + names[j] + " se ven casi iguales");
                }
        }

        // ------------------------------------------------------------------ ningún juego tiene un sistema propio

        private static string GamesDir => Path.Combine(Application.dataPath, "Scripts", "Games");

        private static IEnumerable<(string Rel, string Text)> GameSources()
        {
            foreach (var file in Directory.GetFiles(GamesDir, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(GamesDir.Length + 1).Replace('\\', '/');
                if (rel.Contains("/Tests/") || rel.StartsWith("Tests/") || rel.StartsWith("Shared/")) continue;
                yield return (rel, File.ReadAllText(file));
            }
        }

        [Test]
        public void NoGameKeepsAFloatingToast_TheyAllSpeakThroughTheHudRow()
        {
            int games = 0;
            var offenders = new List<string>();
            foreach (var (rel, text) in GameSources())
            {
                if (text.Contains("new Toast(") || Regex.IsMatch(text, @"\b_toast\b")) offenders.Add(rel);
                if (Regex.IsMatch(text, @"_hud\??\.Announce\(")) games++;
            }
            Assert.IsEmpty(offenders, "estos juegos todavía tienen su propio aviso flotante: " + string.Join(", ", offenders));
            Assert.GreaterOrEqual(games, 8, "los juegos con aviso hablan por la fila de consigna");
            Assert.IsFalse(File.Exists(Path.Combine(GamesDir, "Shared", "Toast.cs")), "el aviso flotante se retiró");
        }

        [Test]
        public void NoPointsTextFloatsInTheContrarreloj()
        {
            // el Contrarreloj no tiene puntos: ningún «+N» (Tarea 179). Los juegos con Contrarreloj que sacan «+N» de puntos en la partida de siempre lo hacen solo con `_tt == null`.
            foreach (var game in new[] { "Disparate/DisparateGameController.cs", "Stroop/StroopGameController.cs", "Radar/RadarGameController.cs", "Acoplamiento/DockingGameController.cs" })
            {
                string path = Path.Combine(GamesDir, game);
                if (!File.Exists(path)) continue;
                foreach (var line in File.ReadAllLines(path))
                    if (Regex.IsMatch(line, @"FloatText\(.*""\+""") && !line.TrimStart().StartsWith("//"))
                        StringAssert.Contains("_tt == null", line, game + ": un «+N» de puntos sale solo fuera del Contrarreloj");
            }
        }

        // ------------------------------------------------------------------ los textos caben

        /// <summary>Bitácora, Cosecha y Rumbo están retirados de la app (siguen en Unity y en el smoke): sus textos no se miden.</summary>
        private static bool Retired(string rel) => rel.StartsWith("Bitacora/") || rel.StartsWith("Cosecha/") || rel.StartsWith("Rumbo/");

        private static IEnumerable<(string Text, string Sub)> AllAnnouncementCopy()
        {
            // los textos escritos tal cual en cada llamado: Announce("texto", AnnounceTone.X[, segundos][, subtitle: "…"])
            var call = new Regex(@"\.Announce\(\s*""((?:[^""\\]|\\.)*)""\s*,\s*AnnounceTone\.\w+(?:\s*,\s*[\d.]+f)?(?:\s*,\s*subtitle:\s*""((?:[^""\\]|\\.)*)"")?");
            foreach (var (rel, text) in GameSources())
            {
                if (Retired(rel)) continue;
                foreach (Match m in call.Matches(text))
                    yield return (m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : null);
            }
            // y los que arma cada juego con su contrato: el paso que sube, el aviso de Acoplamiento, la pista de Engranajes…
            for (int level = 1; level <= 16; level++)
            {
                yield return ("¡Un paso más!", Disparate.DisparateContract.LevelNews(level));
                yield return ("¡Un paso más!", Intrusa.IntrusaContract.LevelNews(level));
                yield return ("¡Un paso más!", Meteoros.MeteorContract.LevelNews(level));
            }
            foreach (var mirrored in new[] { true, false })
                foreach (var answer in new[] { Acoplamiento.DockingContract.AnswerFits, Acoplamiento.DockingContract.AnswerMirror, Acoplamiento.DockingContract.AnswerTimeout })
                    foreach (var streak in new[] { 0, 3, 12 })
                        yield return (Acoplamiento.DockingContract.Notice(mirrored, answer, streak), null);
            yield return (Acoplamiento.DockingContract.RingComplete, null);
            foreach (var color in Stroop.StroopContract.InkNames) yield return ("La tinta era " + color, null);
            foreach (var color in Stroop.StroopContract.LowerNames) yield return ("La palabra decía " + color, null);
            // Engranajes: lo que falló (cada pieza y cada sentido), la pista («Truco…»), el toque bloqueado y el acierto; cada uno se dice solo (en el segundo tiempo)
            foreach (var r in new[] { new Engranajes.PartResult(Engranajes.Station.Carga, Engranajes.Act.Up, false), new Engranajes.PartResult(Engranajes.Station.Carga, Engranajes.Act.Down, false),
                                      new Engranajes.PartResult(Engranajes.Station.Compuerta, Engranajes.Act.Open, false), new Engranajes.PartResult(Engranajes.Station.Compuerta, Engranajes.Act.Close, false),
                                      new Engranajes.PartResult(Engranajes.Station.Antena, Engranajes.Act.Cw, false), new Engranajes.PartResult(Engranajes.Station.Turbina, Engranajes.Act.Ccw, false) })
                yield return (Engranajes.EngranajesContract.FailText(new[] { r }), null);
            yield return (Engranajes.EngranajesContract.FailText(new[] { new Engranajes.PartResult(Engranajes.Station.Antena, Engranajes.Act.Cw, false), new Engranajes.PartResult(Engranajes.Station.Carga, Engranajes.Act.Up, false),
                                                                       new Engranajes.PartResult(Engranajes.Station.Compuerta, Engranajes.Act.Open, false), new Engranajes.PartResult(Engranajes.Station.Turbina, Engranajes.Act.Ccw, false) }), null);
            foreach (var st in new[] { Engranajes.Station.Antena, Engranajes.Station.Compuerta, Engranajes.Station.Carga, Engranajes.Station.Turbina }) yield return ("Tu cambio movió " + Engranajes.EngranajesContract.StationName(st), null);
            yield return ("En dorado, lo que había que cambiar", null);
            yield return ("Truco: el de al lado gira al revés", null);
            yield return (Engranajes.EngranajesContract.BlockedTitle(1), Engranajes.EngranajesContract.BlockedHint(new[] { Engranajes.EngranajesContract.MotorId }, 1));
            yield return (Engranajes.EngranajesContract.BlockedTitle(2), Engranajes.EngranajesContract.BlockedHint(new[] { 2, Engranajes.EngranajesContract.MotorId }, 2));
            yield return (Engranajes.EngranajesContract.BlockedTitle(1), Engranajes.EngranajesContract.BlockedHint(new[] { 2 }, 1));
            yield return ("¡Bien visto! Ya estaba lista", null);
            foreach (var ok in new[] { "¡Cohete listo!", "¡Arreglado!", "¡Todo en orden!" }) yield return (ok, null);
        }

        [Test]
        public void EveryAnnouncementTextFitsOneLineAt16dp_EitherJoinedOrInItsTwoBeats()
        {
            var hud = NewHud();
            var tooLong = new List<string>();
            var twoBeats = new List<string>();
            int total = 0;
            foreach (var (text, sub) in AllAnnouncementCopy())
            {
                if (string.IsNullOrEmpty(text)) continue;
                total++;
                var beats = hud.AnnounceBeats(text, sub);
                if (beats.Length == 2) twoBeats.Add(text + " · " + sub);
                foreach (var beat in beats)
                    if (!hud.AnnounceFits(beat)) tooLong.Add("«" + beat + "» (" + beat.Length + " letras)");
            }
            TestContext.WriteLine("avisos medidos: " + total + "; en dos tiempos: " + twoBeats.Count + "\n" + string.Join("\n", twoBeats));
            Assert.GreaterOrEqual(total, 40, "se encontraron los textos de aviso de los juegos: " + total);
            Assert.IsEmpty(tooLong, "estos avisos no caben en una línea a 16 dp (la fila no se parte en dos renglones): " + string.Join("; ", tooLong));
        }

        [Test]
        public void NoAnnouncementUsesTheWordsTheAppDoesNotSay()
        {
            var banned = new Regex(@"\b(cerebro|cognitiv\w*|entrenamiento|racha|Reto|Desafío|nivel(?:es)?)\b", RegexOptions.IgnoreCase);
            foreach (var (text, sub) in AllAnnouncementCopy())
            {
                if (text.Contains("Subes de nivel")) continue;                                     // Cosecha y Rumbo están retirados de la app
                Assert.IsFalse(banned.IsMatch(text), "«" + text + "» dice una palabra que la app no usa");
                if (sub != null && !sub.Contains("de nivel")) Assert.IsFalse(banned.IsMatch(sub), "«" + sub + "» dice una palabra que la app no usa");
            }
        }
    }
}
