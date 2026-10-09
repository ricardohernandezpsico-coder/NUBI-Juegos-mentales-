using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Capturas de pantalla REALES de los juegos (no una composición aparte): cada juego corre en Play con el lienzo en forma de teléfono (1080x2400, el mismo truco del smoke) y una cámara ortográfica dibuja el lienzo a una textura
    /// que se guarda como PNG. NECESITA gráficos: Unity SIN <c>-nographics</c> y una tarjeta de video (no corre en el CI de GitHub ni en una sesión en la nube). Se usa desde <c>bash tools/verificar-todo.sh --capturas &lt;Juego|todos&gt;</c>,
    /// que después arma las hojas de contacto (tools/capturas/hoja.py).
    ///
    /// Dos modos (variable de entorno <c>NUBI_SHOTS_GAMES</c>: nombres separados por comas, o «todos»):
    ///  - <b>Correo</b> sigue con su guion (<c>MailGameController.EditorShotScript</c>): lleva la partida por sus momentos clave (docs/previews/correo-estacion.png).
    ///  - <b>Por tiempo</b> (cualquier otro juego, sin guion por juego, 4-oct → 8-oct «versión barata»): el piloto automático del smoke juega donde lo hay (Engranajes, Bodega, Constelaciones) y se fotografía la primera pantalla jugable (cuando
    ///    termina la cuenta regresiva), a los 8, 20 y 40 s de juego, la pausa abierta y, si la partida llega a su fin, la cortina «¡Listo!» y la pantalla final; después UNA toma con «quitar animaciones» y, en los juegos con tutorial, el paso 1 y el paso 3 del tutorial.
    ///    No se cambia la lógica de ningún juego: solo se mira. Los errores de consola de cada juego quedan en <c>errores.txt</c> de su carpeta y un resumen con los tiempos en <c>_resumen.txt</c>.
    /// Variable <c>NUBI_SHOTS_DIR</c>: carpeta raíz de salida (por defecto unity/test-results/capturas); cada juego va en su subcarpeta (por su id).
    /// </summary>
    public static class ScreenshotRunner
    {
        private const string ScenePath = "Assets/Scenes/SecuenciaPilotoTest.unity";
        private const int Width = 1080, Height = 2400;
        private const double WholeRunTimeoutSeconds = 3.0 * 3600.0;

        /// <summary>Los 19 juegos en el orden de las áreas (nombre de la hoja de ruta / id).</summary>
        private static readonly (string Name, string Id)[] AllGames =
        {
            ("Parejas", "parejas"), ("Secuencia", "secuencia"), ("Bodega", "bodega"), ("Rumbo", "rumbo"), ("Correo", "correo"),
            ("Stroop", "stroop"), ("Piloto", "piloto"), ("Freno", "freno"), ("Satelites", "satelites"), ("Radar", "radar"),
            ("Acoplamiento", "acoplamiento"), ("Calculo", "calculo"), ("Aterrizaje", "aterrizaje"), ("Engranajes", "engranajes"),
            ("Anagramas", "anagramas"), ("Meteoros", "meteoros"), ("Disparate", "disparate"), ("Cosecha", "cosecha"), ("Intrusa", "intrusa"),
        };

        /// <summary>Los juegos con tutorial guiado (los de <c>UnityGameLauncher.TUTORIAL_GAMES</c> de la app).</summary>
        private static readonly HashSet<string> TutorialGames = new HashSet<string>
        {
            "secuencia", "freno", "aterrizaje", "meteoros", "stroop", "anagramas", "calculo", "engranajes", "bodega", "parejas", "correo", "cosecha", "disparate", "intrusa", "satelites", "piloto",
        };

        /// <summary>Los juegos que el smoke juega solos con <c>GuidedTutorial.EditorAutoPlayGame</c> (el único «piloto automático» que hay de la partida real).</summary>
        private static readonly HashSet<string> AutoPlayGames = new HashSet<string> { "engranajes", "bodega", "parejas", "correo" };

        private static string _root, _dir, _gameId;
        private static int _count;
        private static bool _failed, _finished;
        private static double _startedAt;
        private static bool _previousDomainReloadDisabled;
        private static EnterPlayModeOptions _previousOptions;

        // el avance del guion (ver OnUpdate)
        private static readonly Stack<IEnumerator> Stack = new Stack<IEnumerator>();
        private static double _waitUntilTime, _condDeadline;
        private static Func<bool> _cond;

        // por juego
        private static readonly List<string> GameErrors = new List<string>();
        private static readonly List<string> GameNotes = new List<string>();
        private static readonly StringBuilder Summary = new StringBuilder();
        private static int _totalShots, _gamesDone, _gamesWithProblems;

        private static double Now => EditorApplication.timeSinceStartup;

        /// <summary>Una espera del guion: unos segundos, o hasta que se cumpla una condición (con tope de tiempo: si se agota, el guion sigue igual).</summary>
        private sealed class Wait
        {
            public double Seconds;
            public Func<bool> Cond;
            public double Timeout;
            public static Wait For(double seconds) => new Wait { Seconds = seconds };
            public static Wait Until(Func<bool> cond, double timeout) => new Wait { Cond = cond, Timeout = timeout };
        }

        public static void Run()
        {
            var raw = (Environment.GetEnvironmentVariable("NUBI_SHOTS_GAMES") ?? "Correo").Trim();
            _root = Environment.GetEnvironmentVariable("NUBI_SHOTS_DIR");
            if (string.IsNullOrEmpty(_root)) _root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "test-results", "capturas"));
            var ids = Resolve(raw);
            if (ids.Count == 0)
            {
                Debug.Log("[Capturas] FALLÓ -- no reconozco «" + raw + "» (usa: todos, o nombres como Correo, Radar, Freno, ...)");
                EditorApplication.Exit(2);
                return;
            }
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Debug.Log("[Capturas] FALLÓ -- Unity corre sin gráficos (-nographics): no se puede dibujar. Corre sin ese argumento y con una tarjeta de video.");
                EditorApplication.Exit(2);
                return;
            }
            Directory.CreateDirectory(_root);

            EditorSceneManager.OpenScene(ScenePath);
            _previousDomainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled;
            _previousOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            _failed = _finished = false;
            _totalShots = _gamesDone = _gamesWithProblems = 0;
            Summary.Clear();
            Stack.Clear();
            Stack.Push(Script(ids));
            _cond = null;
            _waitUntilTime = 0;
            _startedAt = Now;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += OnUpdate;
        }

        /// <summary>«todos», o nombres/ids separados por comas → ids de juego (en el orden pedido).</summary>
        private static List<string> Resolve(string raw)
        {
            var ids = new List<string>();
            if (string.Equals(raw, "todos", StringComparison.OrdinalIgnoreCase)) { foreach (var g in AllGames) ids.Add(g.Id); return ids; }
            foreach (var part in raw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                foreach (var g in AllGames)
                    if (string.Equals(g.Name, part, StringComparison.OrdinalIgnoreCase) || string.Equals(g.Id, part, StringComparison.OrdinalIgnoreCase) || (g.Id == "secuencia" && string.Equals(part, "Rastro", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!ids.Contains(g.Id)) ids.Add(g.Id);
                        break;
                    }
            return ids;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            var line = condition.Replace("\r", " ").Replace("\n", " ");
            if (line.Length > 300) line = line.Substring(0, 300) + "…";
            if (!GameErrors.Contains(line) && GameErrors.Count < 40) GameErrors.Add(line);
            Debug.Log("[Capturas] Error de consola (" + (_gameId ?? "-") + "): " + condition + "\n" + stackTrace);
        }

        // ------------------------------------------------------------------ el avance del guion

        /// <summary>Hace avanzar el guion un paso por cuadro del Editor. Un paso puede esperar segundos o una condición (<see cref="Wait"/>), o traer otro guion anidado (<c>yield return OtroGuion()</c>).</summary>
        private static void OnUpdate()
        {
            if (_finished) return;
            if (EditorApplication.isPlaying) HeadlessPlaymodeSmokeTest.ForceAuditCanvasSize(Height);
            double now = Now;
            if (now - _startedAt > WholeRunTimeoutSeconds) { _failed = true; Debug.Log("[Capturas] se acabó el tiempo total de la corrida"); Finish(); return; }
            if (_waitUntilTime > now) return;
            if (_cond != null)
            {
                bool done;
                try { done = _cond(); }
                catch (Exception e) { Debug.Log("[Capturas] aviso: la espera falló (" + e.Message + ")"); done = true; }
                if (!done && now < _condDeadline) return;
                _cond = null;
            }
            for (int guard = 0; guard < 64 && Stack.Count > 0; guard++)
            {
                var top = Stack.Peek();
                bool moved;
                try { moved = top.MoveNext(); }
                catch (Exception e)
                {
                    Debug.Log("[Capturas] aviso: excepción en el guion de " + (_gameId ?? "-") + ": " + e);
                    GameNotes.Add("excepción en el guion: " + e.Message);
                    if (Stack.Count <= 1) { _failed = true; Stack.Clear(); break; }
                    Stack.Pop();           // se abandona esa parte y el guion de arriba sigue con lo que viene (el siguiente juego)
                    continue;
                }
                if (!moved) { Stack.Pop(); continue; }
                var y = top.Current;
                if (y is IEnumerator nested) { Stack.Push(nested); continue; }
                if (y is Wait w)
                {
                    _waitUntilTime = now + w.Seconds;
                    _cond = w.Cond;
                    _condDeadline = now + w.Timeout;
                    return;
                }
                return;                    // null: al siguiente cuadro
            }
            if (Stack.Count == 0) Finish();
        }

        private static void Finish()
        {
            if (_finished) return;
            _finished = true;
            Application.logMessageReceived -= OnLog;
            EditorApplication.update -= OnUpdate;
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
            EditorSettings.enterPlayModeOptionsEnabled = _previousDomainReloadDisabled;
            EditorSettings.enterPlayModeOptions = _previousOptions;
            NeuroVida.Games.Shared.NubiCoach.AuditEnabled = false;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoContinue = false;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Correo.MailGameController.EditorShotMode = false;
            NeuroVida.Games.Satelites.SatelliteGameController.EditorShotMode = false;
            NeuroVida.Games.Piloto.PilotGameController.EditorShotMode = false;
            double total = Now - _startedAt;
            Summary.Insert(0, "total\t" + Mathf.RoundToInt((float)total) + " s\t" + _totalShots + " capturas\t" + _gamesDone + " juegos\n");
            try { File.WriteAllText(Path.Combine(_root, "_resumen.txt"), Summary.ToString(), new UTF8Encoding(false)); } catch (Exception) { /* el resumen es un extra */ }
            bool ok = !_failed && _gamesDone > 0 && _totalShots > 0;
            Debug.Log(ok
                ? "[Capturas] OK -- " + _totalShots + " capturas de " + _gamesDone + " juego(s) en " + _root + " (" + Mathf.RoundToInt((float)(total / 60.0)) + " min" + (_gamesWithProblems > 0 ? "; " + _gamesWithProblems + " juego(s) con avisos, ver _resumen.txt" : "") + ")"
                : "[Capturas] FALLÓ -- " + (_failed ? "el guion falló o no terminó a tiempo" : "no se sacó ninguna captura") + " (" + _totalShots + " capturas, " + _gamesDone + " juego(s))");
            EditorApplication.Exit(ok ? 0 : 1);
        }

        // ------------------------------------------------------------------ el guion de toda la corrida

        private static IEnumerator Script(List<string> ids)
        {
            foreach (var id in ids)
            {
                _gameId = id;
                _dir = Path.Combine(_root, id);
                Directory.CreateDirectory(_dir);
                foreach (var old in Directory.GetFiles(_dir)) File.Delete(old);
                _count = 0;
                GameErrors.Clear();
                GameNotes.Clear();
                double t0 = Now;
                Debug.Log("[Capturas] ---- " + id);

                if (id == "correo") yield return CorreoScript();
                else if (id == "satelites") yield return SatelitesScript();
                else if (id == "piloto") yield return PilotoScript();
                else yield return PlayShots(id);
                if (id != "correo" && id != "satelites" && id != "piloto") yield return ReduceMotionShot(id);          // los guiones de Correo, Satélites y Piloto ya traen su toma con «quitar animaciones»
                if (TutorialGames.Contains(id)) yield return TutorialShots(id);
                yield return Stopped();

                int shots = _count;
                _totalShots += shots;
                if (shots > 0) _gamesDone++;
                bool problems = GameErrors.Count > 0 || GameNotes.Count > 0 || shots == 0;
                if (problems) _gamesWithProblems++;
                var report = new StringBuilder();
                foreach (var n in GameNotes) report.AppendLine("nota: " + n);
                foreach (var e in GameErrors) report.AppendLine("error de consola: " + e);
                File.WriteAllText(Path.Combine(_dir, "errores.txt"), report.Length == 0 ? "(sin errores de consola ni notas)\n" : report.ToString(), new UTF8Encoding(false));
                Summary.AppendLine(id + "\t" + Mathf.RoundToInt((float)(Now - t0)) + " s\t" + shots + " capturas\t" + GameErrors.Count + " errores de consola\t" + GameNotes.Count + " notas");
                Debug.Log("[Capturas] " + id + ": " + shots + " capturas en " + Mathf.RoundToInt((float)(Now - t0)) + " s");
            }
            _gameId = null;
        }

        /// <summary>El estado de cada juego queda limpio entre corridas (el Editor no recarga el dominio: lo estático se hereda).</summary>
        private static void Configure(string id, bool tutorial, bool reduceMotion)
        {
            EditorPlaytestBootstrap.GameIdOverride = id;
            EditorPlaytestBootstrap.ShowTutorialOverride = tutorial;
            EditorPlaytestBootstrap.AssessmentOverride = false;
            EditorPlaytestBootstrap.ReduceMotionOverride = reduceMotion;
            EditorPlaytestBootstrap.MailStageOverride = 0;
            EditorPlaytestBootstrap.SatStageOverride = 0;
            EditorPlaytestBootstrap.SatSurpriseOverride = 0;
            EditorPlaytestBootstrap.PilStageOverride = 0;
            EditorPlaytestBootstrap.PilSectorOverride = 0;
            EditorPlaytestBootstrap.TimedOverride = false;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoContinue = tutorial;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = !tutorial && AutoPlayGames.Contains(id);
            NeuroVida.Games.Shared.NubiCoach.AuditEnabled = tutorial;
            NeuroVida.Games.Shared.NubiCoach.AuditGame = id;
            NeuroVida.Games.Shared.NubiCoach.AuditSteps.Clear();
            NeuroVida.Games.Correo.MailGameController.EditorShotMode = false;
            NeuroVida.Games.Satelites.SatelliteGameController.EditorShotMode = false;
            NeuroVida.Games.Piloto.PilotGameController.EditorShotMode = false;
            HeadlessPlaymodeSmokeTest.ResetAuditCanvases();
        }

        private static IEnumerator Stopped()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) yield break;
            EditorApplication.ExitPlaymode();
            yield return Wait.Until(() => !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode, 40);
            yield return Wait.For(0.2);
        }

        private static IEnumerator EnterPlay()
        {
            yield return Stopped();
            NeuroVida.Games.Shared.GameClock.Reset();
            EditorApplication.EnterPlaymode();
            yield return Wait.Until(() => EditorApplication.isPlaying, 60);
            yield return Wait.For(0.3);
        }

        private static IEnumerator ExitPlay()
        {
            if (NeuroVida.Games.Shared.GameClock.Paused) NeuroVida.Games.Shared.GameClock.Resume();      // si se saliera con la pausa abierta, el reloj de juego quedaría detenido para el juego siguiente
            yield return Stopped();
            NeuroVida.Games.Shared.GameClock.Reset();
        }

        // ------------------------------------------------------------------ por tiempo

        /// <summary>La partida real: primera pantalla jugable, 8, 20 y 40 s de juego, la pausa, y la cortina «¡Listo!» y la pantalla final si la partida llega a su fin.</summary>
        private static IEnumerator PlayShots(string id)
        {
            Configure(id, tutorial: false, reduceMotion: false);
            yield return EnterPlay();
            yield return WaitPlayable();
            double playable = Now;
            Shot("primera");
            bool ended = false;
            foreach (var step in new[] { ("t08", 8.0), ("t20", 20.0), ("pausa", 21.0), ("aviso", 30.0), ("t40", 40.0) })
            {
                double at = playable + step.Item2;
                yield return Wait.Until(() => Now >= at || CurtainUp(), step.Item2 + 30);
                if (CurtainUp()) { ended = true; break; }
                // la pausa va a los 21 s, a mitad de partida: más tarde un juego corto ya terminó (Bodega acaba sus 6 pedidos hacia los 40 s) y su pausa ya no ofrece «Cómo se juega»
                if (step.Item1 == "pausa") yield return PauseShot();
                else if (step.Item1 == "aviso") yield return ToastShot();
                else Shot(step.Item1);
            }
            if (ended || CurtainUp()) yield return EndShots();
            if (_count < 4) GameNotes.Add("solo " + _count + " toma(s) de la partida (¿terminó muy pronto o no arrancó?)");
            yield return ExitPlay();
        }

        /// <summary>
        /// Muestra un aviso de prueba (de dos renglones) con el <c>Toast</c> del juego y saca la toma «aviso» cuando ya se ve del todo: para revisar a ojo que no tapa el título, el estímulo ni los botones (Tarea 55). Los juegos sin aviso
        /// (Constelaciones, Rastro de luz, Bodega, Engranajes, Correo) no sacan esta toma. Si el aviso espera una franja libre («entre ensayos») y no aparece en 5 s, la toma sale igual con lo que haya.
        /// </summary>
        private static IEnumerator ToastShot()
        {
            NeuroVida.Games.Shared.Toast toast = null;
            foreach (var mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
            {
                var field = mb.GetType().GetField("_toast", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null || field.FieldType != typeof(NeuroVida.Games.Shared.Toast)) continue;
                toast = (NeuroVida.Games.Shared.Toast)field.GetValue(mb);
                if (toast != null) break;
            }
            if (toast == null) yield break;
            toast.Show("Aviso de prueba", "Una frase de dos renglones para probar dónde queda el aviso en esta pantalla", NeuroVida.Games.Shared.NeuroStyle.Sun, 7f);
            var group = toast.Rect.GetComponent<CanvasGroup>();
            double limit = Now + 5.5;
            yield return Wait.Until(() => (group != null && group.alpha >= 0.99f) || Now > limit, 6.5);
            yield return Wait.For(0.3);
            Debug.Log("[Capturas] aviso de " + _gameId + ": a " + Mathf.RoundToInt(toast.PlacedTopU) + " u del borde de arriba, choque " + Mathf.RoundToInt(toast.PlacedOverlap));
            Shot("aviso");
        }

        /// <summary>Cuando aparece la cortina «¡Listo!»: una toma con la cortina y otra con la pantalla final (la cortina se va y queda el resultado con «Continuar»).</summary>
        private static IEnumerator EndShots()
        {
            yield return Wait.For(1.1);
            if (CurtainUp()) Shot("listo");
            yield return Wait.Until(() => !CurtainUp(), 12);
            yield return Wait.For(1.8);
            Shot("final");
        }

        /// <summary>UNA toma de la primera pantalla jugable con «quitar animaciones» activo.</summary>
        private static IEnumerator ReduceMotionShot(string id)
        {
            Configure(id, tutorial: false, reduceMotion: true);
            yield return EnterPlay();
            yield return WaitPlayable();
            Shot("sin-animaciones");
            yield return ExitPlay();
        }

        /// <summary>El tutorial guiado con sus pasos andando solos (el smoke): una toma del paso 1 y otra del paso 3, a 0,3 s de que el paso aparece (para que ya se vea asentado; en algunos juegos el paso 1 dura poco).</summary>
        private static IEnumerator TutorialShots(string id)
        {
            Configure(id, tutorial: true, reduceMotion: false);
            yield return EnterPlay();
            bool shot1 = false, shot3 = false;
            int lastIndex = -1;
            double activeSince = 0, start = Now;
            NeuroVida.Games.Shared.NubiCoach coach = null;
            double lastFind = 0;
            yield return Wait.Until(() =>
            {
                if (coach == null && Now - lastFind > 0.25) { lastFind = Now; coach = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.NubiCoach>(); }
                int index = NeuroVida.Games.Shared.NubiCoach.AuditSteps.Count;
                bool active = coach != null && coach.Active;
                if (!active) { lastIndex = -1; return shot3 || (shot1 && index > 2); }
                if (index != lastIndex) { lastIndex = index; activeSince = Now; }
                if (Now - activeSince >= 0.3)
                {
                    if (index == 0 && !shot1) { Shot("tutorial-1"); shot1 = true; }
                    else if (index == 2 && !shot3) { Shot("tutorial-3"); shot3 = true; }
                }
                return shot3;
            }, 90);
            if (!shot1) GameNotes.Add("el tutorial no mostró el paso 1 a tiempo");
            if (!shot3) GameNotes.Add("el tutorial no llegó a un paso 3 (pasos que se cerraron: " + NeuroVida.Games.Shared.NubiCoach.AuditSteps.Count + ")");
            NeuroVida.Games.Shared.NubiCoach.AuditEnabled = false;
            yield return ExitPlay();
        }

        // ------------------------------------------------------------------ Correo (su guion)

        private static IEnumerator CorreoScript()
        {
            Configure("correo", tutorial: false, reduceMotion: false);
            EditorPlaytestBootstrap.MailStageOverride = 5;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Correo.MailGameController.EditorShotMode = true;
            UnityEngine.PlayerPrefs.DeleteKey("mail_intros");                       // para que las tarjetas «NUEVO» salgan aunque ya se hayan visto
            yield return EnterPlay();
            NeuroVida.Games.Correo.MailGameController controller = null;
            yield return Wait.Until(() => (controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Correo.MailGameController>()) != null, 40);
            if (controller == null) { GameNotes.Add("no apareció el juego de Correo"); yield return ExitPlay(); yield break; }
            controller.StartCoroutine(controller.EditorShotScript(Shot, PauseShot));
            yield return Wait.Until(() => controller == null || controller.EditorShotFinished, 150);
            if (controller != null && !controller.EditorShotFinished) GameNotes.Add("el guion de Correo no terminó a tiempo");
            NeuroVida.Games.Correo.MailGameController.EditorShotMode = false;
            yield return ExitPlay();
        }

        // ------------------------------------------------------------------ Satélites (su guion)

        private static IEnumerator SatelitesScript()
        {
            Configure("satelites", tutorial: false, reduceMotion: false);
            EditorPlaytestBootstrap.SatStageOverride = 6;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Satelites.SatelliteGameController.EditorShotMode = true;
            UnityEngine.PlayerPrefs.DeleteKey("sat_intros");                        // para que el aviso «NUEVO» de la nube salga aunque ya se haya visto
            yield return EnterPlay();
            NeuroVida.Games.Satelites.SatelliteGameController controller = null;
            yield return Wait.Until(() => (controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Satelites.SatelliteGameController>()) != null, 40);
            if (controller == null) { GameNotes.Add("no apareció el juego de Satélites"); yield return ExitPlay(); yield break; }
            controller.StartCoroutine(controller.EditorShotScript(Shot, PauseShot));
            yield return Wait.Until(() => controller == null || controller.EditorShotFinished, 200);
            if (controller != null && !controller.EditorShotFinished) GameNotes.Add("el guion de Satélites no terminó a tiempo");
            NeuroVida.Games.Satelites.SatelliteGameController.EditorShotMode = false;
            yield return ExitPlay();
        }

        // ------------------------------------------------------------------ Piloto (su guion)

        private static IEnumerator PilotoScript()
        {
            Configure("piloto", tutorial: false, reduceMotion: false);
            EditorPlaytestBootstrap.PilStageOverride = 6;
            EditorPlaytestBootstrap.TimedOverride = true;                          // el Reto: el vuelo de 90 s en tres sectores
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Piloto.PilotGameController.EditorShotMode = true;
            yield return EnterPlay();
            NeuroVida.Games.Piloto.PilotGameController controller = null;
            yield return Wait.Until(() => (controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Piloto.PilotGameController>()) != null, 40);
            if (controller == null) { GameNotes.Add("no apareció el juego de Piloto"); yield return ExitPlay(); yield break; }
            controller.StartCoroutine(controller.EditorShotScript(Shot, PauseShot));
            yield return Wait.Until(() => controller == null || controller.EditorShotFinished, 240);
            if (controller != null && !controller.EditorShotFinished) GameNotes.Add("el guion de Piloto no terminó a tiempo");
            NeuroVida.Games.Piloto.PilotGameController.EditorShotMode = false;
            yield return ExitPlay();
        }

        // ------------------------------------------------------------------ piezas

        private static Transform _countdownRoot;

        /// <summary>Espera a que termine la cuenta regresiva (el objeto «CountdownScreen» se apaga) y deja 0,8 s para que la primera pantalla se asiente. Si nunca se ve la cuenta, a los 14 s sigue igual.</summary>
        private static IEnumerator WaitPlayable()
        {
            bool seen = false;
            double start = Now, lastFind = 0;
            _countdownRoot = null;
            yield return Wait.Until(() =>
            {
                if (_countdownRoot == null && Now - lastFind > 0.1)
                {
                    lastFind = Now;
                    foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (t.name == "CountdownScreen") { _countdownRoot = t; break; }
                }
                bool active = _countdownRoot != null && _countdownRoot.gameObject.activeInHierarchy;
                if (active) seen = true;
                return (seen && !active) || Now - start > 14.0;
            }, 20);
            if (!seen) GameNotes.Add("no se vio la cuenta regresiva; la 'primera' toma es a los 14 s");
            yield return Wait.For(0.8);
        }

        private static double _curtainChecked;
        private static bool _curtain;

        /// <summary>true mientras la cortina «¡Listo!» (el cierre de la partida) está a la vista.</summary>
        private static bool CurtainUp()
        {
            if (Now - _curtainChecked < 0.2) return _curtain;
            _curtainChecked = Now;
            _curtain = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.FinishCurtain>() != null;
            return _curtain;
        }

        /// <summary>Abre la pausa del juego (como el smoke), saca la foto «pausa» y la cierra con «Continuar» (si no, el reloj de juego se queda detenido).</summary>
        private static IEnumerator PauseShot()
        {
            var entry = UnityEngine.Object.FindObjectOfType<GameEntryPoint>();
            if (entry == null) { GameNotes.Add("no hay GameEntryPoint para abrir la pausa"); yield break; }
            typeof(GameEntryPoint).GetMethod("ShowPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(entry, null);
            var menu = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.PauseMenu>();
            // La entrada del menú (fundido y rebote, 0,22 s con el reloj REAL de los cuadros del juego) tiene que haber TERMINADO: el guion avanza en cada vuelta del Editor (hay varias por cuadro) y no sirve sumar el tiempo de cuadro. Se espera en tiempo real
            // y a que el fundido del menú llegue a 1 (antes la pausa salía a medio aparecer, con el velo y los botones a medias).
            var group = menu != null ? menu.GetComponent<CanvasGroup>() : null;
            double settleAt = Now + 0.6;
            yield return Wait.Until(() => Now >= settleAt && (group == null || group.alpha >= 0.999f), 5);
            yield return Wait.For(0.25);
            LogPauseState();
            Shot("pausa");
            if (menu != null && menu.IsShown)
                foreach (var kv in menu.VisibleButtons())
                    if (kv.Key.Contains("Continuar"))
                    {
                        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(kv.Value.gameObject, new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current), UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                        break;
                    }
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) yield return null;
        }

        /// <summary>Para entender una pausa que sale «apagada»: qué está a la vista a pantalla casi completa (nombre, opacidad y orden de su lienzo) y si la pausa ofrece «Cómo se juega». Solo informa en el log.</summary>
        private static void LogPauseState()
        {
            try
            {
                var g = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.GameControllerBase>();
                Debug.Log("[Capturas] pausa de " + _gameId + ": CanShowHowTo=" + (g != null ? g.CanShowHowTo.ToString() : "sin juego") + ", GameClock.Paused=" + NeuroVida.Games.Shared.GameClock.Paused + ", timeScale=" + Time.timeScale);
                if (g != null)
                {
                    const System.Reflection.BindingFlags all = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
                    var baseType = typeof(NeuroVida.Games.Shared.GameControllerBase);
                    var tutorial = baseType.GetField("_tutorial", all)?.GetValue(g);
                    var running = baseType.GetField("_howToRunning", all)?.GetValue(g);
                    var ready = baseType.GetProperty("HowToReady", all)?.GetValue(g);
                    var phase = g.GetType().GetField("_phase", all)?.GetValue(g);
                    var loopOn = g.GetType().GetField("_loopOn", all)?.GetValue(g);
                    Debug.Log("[Capturas]   _tutorial " + (tutorial == null ? "NULO" : "ok") + ", _howToRunning=" + running + ", HowToReady=" + ready + ", _phase=" + phase + ", _loopOn=" + loopOn);
                }
                foreach (var img in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Graphic>())
                {
                    if (!img.isActiveAndEnabled || img.canvas == null) continue;
                    var rt = img.rectTransform;
                    float a = img.color.a * (img.canvasRenderer != null ? img.canvasRenderer.GetInheritedAlpha() : 1f);
                    if (a < 0.05f || rt.rect.width < 900f || rt.rect.height < 1500f) continue;
                    var path = new System.Text.StringBuilder();
                    for (var t = img.transform; t != null && path.Length < 160; t = t.parent) path.Insert(0, t.name + "/");
                    Debug.Log("[Capturas]   a pantalla casi completa: " + path + " alfa " + a.ToString("0.00") + " canvas orden " + img.canvas.rootCanvas.sortingOrder + " profundidad " + (img.canvasRenderer != null ? img.canvasRenderer.absoluteDepth : -1));
                }
            }
            catch (Exception e) { Debug.Log("[Capturas] LogPauseState: " + e.Message); }
        }

        /// <summary>Dibuja el lienzo (en espacio del mundo, 1080x2400, centrado en el origen) con una cámara ortográfica y lo guarda como PNG numerado.</summary>
        private static void Shot(string name)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var camGo = new GameObject("ShotCam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = Height / 2f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.targetTexture = rt;
            cam.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            _count++;
            var file = Path.Combine(_dir, _count.ToString("00") + "-" + name + ".png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Debug.Log("[Capturas] " + file + " (" + SystemInfo.graphicsDeviceName + ")");
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(camGo);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
