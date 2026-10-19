using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NeuroVida.Games.Anagramas;
using NeuroVida.Games.Bodega;
using NeuroVida.Games.Calculo;
using NeuroVida.Games.Correo;
using NeuroVida.Games.Freno;
using NeuroVida.Games.Intrusa;
using NeuroVida.Games.Meteoros;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.Piloto;
using NeuroVida.Games.Satelites;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;
using NeuroVida.Games.Stroop;
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
        private static readonly string[] Migrated = { "Calculo", "Anagramas", "Parejas", "Bodega", "Correo", "Secuencia", "Satelites", "Piloto", "Intrusa", "Stroop", "Meteoros", "Freno" };

        /// <summary>Los bucles aprobados que se quedan con su propio altavoz (el zumbido de Satélites, el motor de Piloto y el propulsor de Aterrizaje).</summary>
        private static readonly string[] LoopFiles = { "PilotSounds.cs", "SatelliteSounds.cs", "SatelliteGameController.cs", "SatelliteGameController.Build.cs", "SatelliteGameController.Scene.cs", "PilotGameController.cs", "PilotGameController.Scene.cs", "PilotEngine.cs", "LandingGameController.Scene.cs" };

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
                if (System.Array.IndexOf(LoopFiles, Path.GetFileName(file)) >= 0 || Path.GetFileName(file) == "HarmonicTone.cs") continue;        // HarmonicTone.cs vive en la carpeta de Rastro pero es el generador de tonos de GameControllerBase.PlayTone: la sirena de Freno, que se queda
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
                if (Path.GetFileName(file) == "BrakeGameController.cs") { TheBrakeSirenIsTheOnlyLooseTone(file); continue; }
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                    if (!lines[i].TrimStart().StartsWith("//") && lines[i].Contains("PlayTone("))
                        Assert.Fail("«" + lines[i].Trim() + "» en " + Where(file, i) + ": un tono suelto; los juegos cableados suenan la paleta o una receta propia (WoodCore)");
            }
        }

        /// <summary>En Freno solo quedan los TRES <c>PlayTone</c> de la sirena de «¡ALTO!» (988, 740 y 988 Hz), y dentro de <c>ShowStop</c> y <c>SirenTail</c>: la salida, el «¡Más rápido!» y el acierto ya no son tonos sueltos.</summary>
        private static void TheBrakeSirenIsTheOnlyLooseTone(string file)
        {
            var lines = File.ReadAllLines(file);
            int shown = -1, tail = -1, tailEnd = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("private void ShowStop()")) shown = i;
                if (lines[i].Contains("private IEnumerator SirenTail()")) tail = i;
            }
            Assert.IsTrue(shown >= 0 && tail > shown, "ShowStop y SirenTail existen en BrakeGameController.cs");
            for (int i = tail + 1; i < lines.Length; i++) if (lines[i].Contains("private ")) { tailEnd = i; break; }
            int count = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("//") || !lines[i].Contains("PlayTone(")) continue;
                count++;
                Assert.IsTrue(i > shown && i < tailEnd, "«" + lines[i].Trim() + "» en " + Where(file, i) + ": solo la sirena (ShowStop y SirenTail) suena con PlayTone en Freno");
            }
            Assert.AreEqual(3, count, "la sirena son tres tonos (988, 740 y 988 Hz)");
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

        /// <summary>Las clases de sonidos de las Fases C y D (Carga, Punta, Constelaciones, Bodega, Correo, Rastro, Satélites, Piloto e Intrusa) dicen sus recetas propias y qué piden antes de la cuenta: <c>All()</c> y <c>Request()</c>.</summary>
        [Test]
        public void ThePhaseCAndDSoundClasses_ListTheirRecipesAndWhatTheyRequest()
        {
            string dir = Path.Combine(Application.dataPath, "Scripts", "Games");
            foreach (var rel in new[] { "Calculo/CargaSounds.cs", "Anagramas/PuntaSounds.cs", "Parejas/ConstelacionSounds.cs", "Bodega/BodegaSounds.cs", "Correo/MailSounds.cs", "Secuencia/RastroSounds.cs", "Satelites/SatelliteSounds.cs", "Piloto/PilotSounds.cs", "Intrusa/IntrusaSounds.cs" })
            {
                string text = File.ReadAllText(Path.Combine(dir, rel));
                Assert.IsTrue(text.Contains("All()") && text.Contains("static void Request()"), rel + ": expone All() y Request()");
            }
        }

        /// <summary>Para cada juego cableado: qué pide por adelantado (<c>Request</c>) y su id (el que recibe <c>WoodCore.Warm</c>). Cada tarea de cableado suma su fila.</summary>
        private static readonly Dictionary<string, (string Id, System.Action Request)> Requests = new Dictionary<string, (string, System.Action)>
        {
            { "Calculo", ("calculo", CargaSounds.Request) },
            { "Anagramas", ("anagramas", PuntaSounds.Request) },
            { "Parejas", ("parejas", ConstelacionSounds.Request) },
            { "Bodega", ("bodega", BodegaSounds.Request) },
            { "Correo", ("correo", MailSounds.Request) },
            { "Secuencia", ("secuencia", RastroSounds.Request) },
            { "Satelites", ("satelites", SatelliteSounds.Request) },
            { "Piloto", ("piloto", PilotSounds.Request) },
            { "Intrusa", ("intrusa", IntrusaSounds.Request) },
            { "Stroop", ("stroop", StroopSounds.Request) },
            { "Meteoros", ("meteoros", MeteorSounds.Request) },
            { "Freno", ("freno", BrakeSounds.Request) },
        };

        /// <summary>Lo que el controlador de un juego cableado le pide a la paleta (<c>WoodCore.Play(WoodSfx.X[, n])</c>): (sonido, variante o -1 si el argumento es una expresión que recorre todas), con el archivo y la línea.</summary>
        private static IEnumerable<(WoodSfx Sfx, int Arg, string At)> PaletteCalls(string file)
        {
            var text = File.ReadAllText(file); var head = new Regex(@"WoodCore\.Play\(\s*WoodSfx\.(\w+)");
            foreach (Match m in head.Matches(text))
            {
                int open = text.IndexOf('(', m.Index); int depth = 0, end = open, i = open;
                for (; i < text.Length; i++) { if (text[i] == '(') depth++; else if (text[i] == ')' && --depth == 0) { end = i; break; } }
                var args = new List<string>(); int d2 = 0, start = open + 1;
                for (int j = open + 1; j < end; j++) { char c = text[j]; if (c == '(') d2++; else if (c == ')') d2--; else if (c == ',' && d2 == 0) { args.Add(text.Substring(start, j - start).Trim()); start = j + 1; } }
                args.Add(text.Substring(start, end - start).Trim());
                var sfx = (WoodSfx)System.Enum.Parse(typeof(WoodSfx), m.Groups[1].Value);
                int line = text.Substring(0, m.Index).Count(ch => ch == '\n') + 1; string at = Path.GetFileName(file) + ":" + line;
                if (args.Count < 2 || args[1].Contains(":")) { yield return (sfx, 0, at); continue; }                  // sin variante o con argumentos con nombre (gainDb:, ui:)
                if (int.TryParse(args[1], out int n)) yield return (sfx, n, at); else yield return (sfx, -1, at);
            }
        }

        /// <summary>Todo lo que un juego cableado le suena a la paleta está listo después de <c>Warm</c> + el <c>Request</c> de su clase de sonidos: o va con el núcleo o lo pidió por adelantado (si no, sonaría tarde o no sonaría: <c>Skipped</c>).</summary>
        [Test]
        public void EverythingAWiredGameSoundsFromThePalette_IsAskedForBeforeTheCountdown()
        {
            bool soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true;
            try
            {
                foreach (var kv in Requests)
                {
                    WoodCore.Reset(); GameFeel.Warm(kv.Value.Id); kv.Value.Request();
                    int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000, "el cálculo no termina"); }
                    foreach (var file in MigratedControllers().Where(f => f.Replace('\\', '/').Contains("/" + kv.Key + "/")))
                        foreach (var (sfx, arg, at) in PaletteCalls(file))
                        {
                            var variants = arg >= 0 ? new[] { arg } : WoodSounds.Variants(sfx);
                            foreach (int v in variants) Assert.IsNotNull(WoodCore.Get(sfx, v), at + ": suena " + sfx + (v != 0 ? " " + v : "") + " y " + kv.Key + " no lo pide por adelantado (ni va con el núcleo): se saltaría");
                        }
                }
            }
            finally { WoodCore.Reset(); GameFeel.SoundOn = soundWas; }
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
