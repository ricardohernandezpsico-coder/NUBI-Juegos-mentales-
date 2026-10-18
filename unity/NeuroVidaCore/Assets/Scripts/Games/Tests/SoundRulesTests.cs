using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// Las reglas del sonido común que se vigilan leyendo el código de los juegos (Tarea 120, Fase B): UN solo final por partida, nada doble en los avisos y en el toque del kit, y el «¡Listo!» de las prácticas en la paleta. Si alguien las rompe, la prueba dice en qué archivo y en qué línea.
    /// No revisa los juegos retirados (Bitácora, Rumbo a Casa y Cosecha de palabras: la app ya no los lanza).
    /// </summary>
    public class SoundRulesTests
    {
        private static readonly string[] Retired = { "Bitacora", "Cosecha", "Rumbo" };

        /// <summary>Los archivos de los controladores de los juegos (sin las pruebas, sin lo común, sin los sonidos propios ni los juegos retirados).</summary>
        private static IEnumerable<string> Controllers()
        {
            string dir = Path.Combine(Application.dataPath, "Scripts", "Games");
            foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(dir.Length).Replace('\\', '/');
                if (rel.Contains("/Tests/") || rel.Contains("/Shared/") || rel.EndsWith("Sounds.cs") || rel.EndsWith("Contract.cs")) continue;
                bool retired = false;
                foreach (var r in Retired) if (rel.StartsWith("/" + r + "/")) retired = true;
                if (!retired) yield return file;
            }
        }

        private static string Where(string file, int line) => Path.GetFileName(file) + ":" + (line + 1);

        [Test]
        public void NoGamePlaysItsOwnFinalSound_TheFinalIsOneForTheWholeApp()
        {
            var own = new Regex(@"\bPlay(?:Clip)?\([^;]*\bFinale\b");
            foreach (var file in Controllers())
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                    if (!lines[i].TrimStart().StartsWith("//") && own.IsMatch(lines[i]))
                        Assert.Fail("«" + lines[i].Trim() + "» en " + Where(file, i) + ": el final de la partida lo suena ExitButton (GameFeel.Finish), UNO por partida; ningún juego suena uno propio");
            }
        }

        [Test]
        public void AToastThatAlreadySoundsSomethingOfItsOwn_IsSilent()
        {
            var own = new Regex(@"\bPlay(?:Clip|Tone)\(|(?<![\w.])Play\((?:Dock|Land|Radar)Sfx");
            foreach (var file in Controllers())
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!Regex.IsMatch(lines[i], @"_toast\??\.Show\(") || lines[i].Contains("sound: false")) continue;
                    for (int j = System.Math.Max(0, i - 2); j <= System.Math.Min(lines.Length - 1, i + 2); j++)
                        if (j != i && !lines[j].TrimStart().StartsWith("//") && own.IsMatch(lines[j]))
                            Assert.Fail("El aviso de " + Where(file, i) + " suena el aviso de la paleta y el juego ya suena algo propio en " + Where(file, j) + " («" + lines[j].Trim() + "»): ese aviso va con «sound: false»");
                }
            }
        }

        [Test]
        public void AKitButtonThatAnswersWithItsOwnSound_DoesNotAddTheKitTap()
        {
            foreach (var file in Controllers())
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].Contains("AddComponent<PlasticPress>()")) continue;
                    bool silent = lines[i].Contains("PlaysTap = false") || (i + 1 < lines.Length && lines[i + 1].Contains("PlaysTap = false"));
                    Assert.IsTrue(silent, "El botón de " + Where(file, i) + " usa PlasticPress (suena el toque del kit al apoyar el dedo) en un juego: si el juego ya suena su acierto, su error o su nota al tocarlo, lleva «PlaysTap = false»");
                }
            }
        }

        [Test]
        public void TheReadyNoticeOfThePractices_IsThePaletteNotice_NotALooseTone()
        {
            foreach (var file in Controllers())
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                    if (lines[i].Contains("PlayTone(523f, 0.4f, 0.08f)"))
                        Assert.Fail("El «¡Listo!» de la práctica en " + Where(file, i) + " tiene que sonar GameFeel.Notice() (la paleta), no un tono suelto");
            }
        }

        /// <summary>El toque del kit (Tarea 123; Fable §3.5): suena el golpecito de madera Y da un tic háptico, con el ajuste de vibración de la app (`HapticsOn`), suene o no el golpecito.</summary>
        [Test]
        public void TheKitTap_AlsoGivesAHapticTick()
        {
            string text = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Shared", "GameFeel.cs"));
            int tap = text.IndexOf("public static void Tap()", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(tap, 0, "GameFeel.Tap existe");
            int end = text.IndexOf("/// <summary>", tap, System.StringComparison.Ordinal);
            string body = text.Substring(tap, (end > tap ? end : text.Length) - tap);
            Assert.IsTrue(body.Contains("WoodCore.Play(WoodSfx.Tap"), "el toque suena el golpecito de madera");
            Assert.IsTrue(body.Contains("Haptic(HapticKind.Light)"), "y da el tic háptico (Haptic respeta HapticsOn, el ajuste de vibración de la app)");
        }
    }
}
