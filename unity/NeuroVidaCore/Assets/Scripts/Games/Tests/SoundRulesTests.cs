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

        /// <summary>
        /// Los juegos YA CABLEADOS a <c>WoodCore</c> (carpeta de <c>Games/</c>): rigen las reglas (b), (c) y (d). Paso 0 de las Fases C a E (Tarea 133): ninguno todavía; cada tarea de cableado suma su carpeta aquí (Calculo, Anagramas, Parejas, Bodega, Correo en la Fase C…).
        /// </summary>
        private static readonly string[] Migrated = { };

        /// <summary>Los bucles aprobados que se quedan con su propio altavoz (el zumbido de Satélites, el motor de Piloto y el propulsor de Aterrizaje).</summary>
        private static readonly string[] LoopFiles = { "SatelliteGameController.cs", "SatelliteGameController.Scene.cs", "PilotGameController.cs", "PilotGameController.Scene.cs", "PilotEngine.cs", "LandingGameController.Scene.cs" };

        /// <summary>Los controladores de los juegos ya cableados.</summary>
        private static IEnumerable<string> MigratedControllers()
        {
            string dir = Path.Combine(Application.dataPath, "Scripts", "Games");
            foreach (var file in Controllers())
            {
                string rel = file.Substring(dir.Length).Replace('\\', '/');
                foreach (var m in Migrated) if (rel.StartsWith("/" + m + "/")) yield return file;
            }
        }

        private static readonly Regex OwnSound = new Regex(@"WoodCore\.Play|\.Play\(\w+Sfx|PlayNote\(|PlayCue\(");

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
            var own = new Regex(@"\bPlay(?:Clip|Tone)\(|(?<![\w.])Play\((?:Dock|Land|Radar)Sfx|WoodCore\.Play|\.Play\(\w+Sfx|PlayNote\(|PlayCue\(");           // (a) desde el paso 0 cubre también los sonidos de WoodCore
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

        /// <summary>(b) Ningún juego cableado suena por su cuenta: nada de <c>PlayOneShot(</c>, <c>AudioClip.Create(</c> ni <c>AddComponent&lt;AudioSource&gt;</c> (todo va por <c>WoodCore</c>), salvo los tres bucles aprobados (lista explícita de archivos).</summary>
        [Test]
        public void AWiredGame_HasNoSpeakerOfItsOwn_OnlyTheThreeLoopsKeepTheirs()
        {
            var own = new Regex(@"PlayOneShot\(|AudioClip\.Create\(|AddComponent<AudioSource>");
            foreach (var file in MigratedControllers())
            {
                if (System.Array.IndexOf(LoopFiles, Path.GetFileName(file)) >= 0) continue;
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                    if (!lines[i].TrimStart().StartsWith("//") && own.IsMatch(lines[i]))
                        Assert.Fail("«" + lines[i].Trim() + "» en " + Where(file, i) + ": un juego cableado suena por WoodCore (Play / PlayNote / PlayCue), no por un altavoz propio; solo los bucles de Satélites, Piloto y Aterrizaje se quedan con el suyo");
            }
        }

        /// <summary>(c) Ningún <c>PlayTone(</c> en los juegos cableados, salvo la sirena de Freno (se queda por decisión de Ricardo). La regla se activa por juego.</summary>
        [Test]
        public void AWiredGame_PlaysNoLooseTone_OnlyTheBrakeSirenStays()
        {
            foreach (var file in MigratedControllers())
            {
                if (Path.GetFileName(file) == "BrakeGameController.cs") continue;
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                    if (!lines[i].TrimStart().StartsWith("//") && lines[i].Contains("PlayTone("))
                        Assert.Fail("«" + lines[i].Trim() + "» en " + Where(file, i) + ": un tono suelto; los juegos cableados suenan la paleta o una receta propia (WoodCore)");
            }
        }

        /// <summary>(d) Toda clase de sonidos de un juego cableado expone <c>All()</c> (la lista de sus recetas propias) y ya no define <c>Bell(</c> ni <c>Make(</c>: el idioma viejo (campanas copiadas en cada juego) desaparece.</summary>
        [Test]
        public void TheSoundsOfAWiredGame_AreAListOfRecipes_TheOldBellIdiomIsGone()
        {
            string dir = Path.Combine(Application.dataPath, "Scripts", "Games");
            foreach (var m in Migrated)
                foreach (var file in Directory.GetFiles(Path.Combine(dir, m), "*Sounds.cs"))
                {
                    string text = File.ReadAllText(file);
                    Assert.IsTrue(text.Contains("All()"), Path.GetFileName(file) + ": una clase de sonidos de un juego cableado expone All() (sus recetas propias)");
                    Assert.IsFalse(Regex.IsMatch(text, @"static\s+\w[\w<>\[\]]*\s+(?:Bell|Make)\("), Path.GetFileName(file) + ": ya no define Bell( ni Make( (las campanas viejas)");
                }
        }

        /// <summary>Las cinco clases de la Fase C ya dicen sus recetas propias y qué piden antes de la cuenta (aunque ningún controlador las use todavía): <c>All()</c> y <c>Request()</c>.</summary>
        [Test]
        public void ThePhaseCSoundClasses_ListTheirRecipesAndWhatTheyRequest()
        {
            string dir = Path.Combine(Application.dataPath, "Scripts", "Games");
            foreach (var rel in new[] { "Calculo/CargaSounds.Wood.cs", "Anagramas/PuntaSounds.Wood.cs", "Parejas/ConstelacionSounds.Wood.cs", "Bodega/BodegaSounds.Wood.cs", "Correo/MailSounds.Wood.cs" })
            {
                string text = File.ReadAllText(Path.Combine(dir, rel));
                Assert.IsTrue(text.Contains("All()") && text.Contains("static void Request()"), rel + ": expone All() y Request()");
            }
        }

        /// <summary>(e) Dos sonidos por una misma respuesta (un <c>WoodCore.Play</c> / <c>PlayNote</c> en la misma línea o la siguiente de un <c>GameFeel.Correct/Wrong/LevelUp</c>) fallan, salvo las parejas permitidas a propósito (lista explícita; hoy ninguna: las dos de la Fase C, nota + destello en Bodega y Correo y toque + nota en Constelaciones, no usan <c>GameFeel.Correct</c>).</summary>
        [Test]
        public void ARightOrWrongAnswer_DoesNotSoundTwoThings()
        {
            var feel = new Regex(@"GameFeel\.(?:Correct|Wrong|LevelUp)\(");
            var allowed = new HashSet<string>();                              // "Archivo.cs:línea" de las parejas permitidas
            foreach (var file in Controllers())
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("//") || !feel.IsMatch(lines[i])) continue;
                    for (int j = System.Math.Max(0, i - 1); j <= System.Math.Min(lines.Length - 1, i + 1); j++)
                        if (j != i && !lines[j].TrimStart().StartsWith("//") && OwnSound.IsMatch(lines[j]) && !allowed.Contains(Where(file, i)))
                            Assert.Fail("«" + lines[i].Trim() + "» en " + Where(file, i) + " y «" + lines[j].Trim() + "» en " + Where(file, j) + ": dos sonidos por una respuesta; la paleta evita dos sonidos por acierto (si la pareja es a propósito, va en la lista de permitidas de esta prueba)");
                }
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
