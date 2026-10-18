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
    ///    termina la cuenta regresiva), a los 8, 20 y 40 s de juego, la pausa abierta y, si la partida llega a su fin, el momento del objeto, la cortina «¡Listo!» y la escena final con «Continuar»; después UNA toma con «quitar animaciones» y, en los juegos con tutorial, el paso 1 y el paso 3 del tutorial.
    ///    No se cambia la lógica de ningún juego: solo se mira. Los errores de consola de cada juego quedan en <c>errores.txt</c> de su carpeta y un resumen con los tiempos en <c>_resumen.txt</c>.
    /// Variable <c>NUBI_SHOTS_DIR</c>: carpeta raíz de salida (por defecto unity/test-results/capturas); cada juego va en su subcarpeta (por su id).
    /// </summary>
    public static class ScreenshotRunner
    {
        private const string ScenePath = "Assets/Scenes/SecuenciaPilotoTest.unity";
        private const int Width = 1080;
        /// <summary>El alto del lienzo de las capturas: 2400 (20:9) casi siempre; Rescate relámpago saca además unas tomas en 1920 (16:9).</summary>
        private static int Height = 2400;
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
            "secuencia", "freno", "aterrizaje", "meteoros", "stroop", "anagramas", "calculo", "engranajes", "bodega", "parejas", "correo", "cosecha", "disparate", "intrusa", "satelites", "piloto", "radar", "acoplamiento",
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

        /// <summary>Modo «juice» (Tarea 130, NUBI_SHOTS_JUICE=1): en vez de la partida entera, cada juego (Carga, Tinta, Punta, Rastro) saca «antes · durante · después» de su efecto nuevo, con el juego REAL (la jugada se hace por reflexión).</summary>
        private static bool _juice;

        public static void Run()
        {
            _juice = Environment.GetEnvironmentVariable("NUBI_SHOTS_JUICE") == "1";
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
            NeuroVida.Games.Radar.RadarGameController.EditorShotMode = false;
            NeuroVida.Games.Acoplamiento.DockingGameController.EditorShotMode = false;
            NeuroVida.Games.Aterrizaje.LandingGameController.EditorShotMode = false;
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

                if (_juice) yield return JuiceShots(id);
                else if (id == "correo") yield return CorreoScript();
                else if (id == "satelites") yield return SatelitesScript();
                else if (id == "piloto") yield return PilotoScript();
                else if (id == "radar") { yield return RadarScript(); yield return RadarScript16(); }
                else if (id == "acoplamiento") { yield return AcoplamientoScript(); yield return AcoplamientoScript16(); }
                else if (id == "aterrizaje") { yield return AterrizajeScript(); yield return AterrizajeScript16(); }
                else yield return PlayShots(id);
                if (!_juice && id != "correo" && id != "satelites" && id != "piloto" && id != "radar" && id != "acoplamiento" && id != "aterrizaje") yield return ReduceMotionShot(id);          // los guiones de Correo, Satélites, Piloto, Rescate y Acoplamiento ya traen su toma con «quitar animaciones»
                if (!_juice && TutorialGames.Contains(id)) yield return TutorialShots(id);
                if (!_juice && (id == "acoplamiento" || id == "radar")) yield return TutorialShots(id, 1920, "-16x9");          // la práctica en la pantalla más baja: ahí la fila fija de arriba aprieta más (Tarea 128)
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
            EditorPlaytestBootstrap.RescStageOverride = 0;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoContinue = tutorial;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = !tutorial && AutoPlayGames.Contains(id);
            NeuroVida.Games.Shared.NubiCoach.AuditEnabled = tutorial;
            NeuroVida.Games.Shared.NubiCoach.AuditGame = id;
            NeuroVida.Games.Shared.NubiCoach.AuditSteps.Clear();
            NeuroVida.Games.Correo.MailGameController.EditorShotMode = false;
            NeuroVida.Games.Satelites.SatelliteGameController.EditorShotMode = false;
            NeuroVida.Games.Piloto.PilotGameController.EditorShotMode = false;
            NeuroVida.Games.Radar.RadarGameController.EditorShotMode = false;
            NeuroVida.Games.Acoplamiento.DockingGameController.EditorShotMode = false;
            NeuroVida.Games.Aterrizaje.LandingGameController.EditorShotMode = false;
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

        /// <summary>La partida real: primera pantalla jugable, 8, 20 y 40 s de juego, la pausa, y el momento del objeto, la cortina «¡Listo!» y la escena final si la partida llega a su fin.</summary>
        // ------------------------------------------------------------------ juice (Tarea 130): antes · durante · después

        private const System.Reflection.BindingFlags AnyInstance = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

        private static object Member(object o, string name)
        {
            if (o == null) return null;
            for (var t = o.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(name, AnyInstance | System.Reflection.BindingFlags.DeclaredOnly);
                if (f != null) return f.GetValue(o);
                var p = t.GetProperty(name, AnyInstance | System.Reflection.BindingFlags.DeclaredOnly);
                if (p != null) return p.GetValue(o);
            }
            return null;
        }

        private static object Call(object o, string name, params object[] args)
        {
            for (var t = o.GetType(); t != null; t = t.BaseType)
                foreach (var m in t.GetMethods(AnyInstance | System.Reflection.BindingFlags.DeclaredOnly))
                    if (m.Name == name && m.GetParameters().Length == args.Length) return m.Invoke(o, args);
            GameNotes.Add("no encontré el método " + name);
            return null;
        }

        private static MonoBehaviour FindController(string typeName)
        {
            foreach (var mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>()) if (mb != null && mb.GetType().Name == typeName) return mb;
            return null;
        }

        /// <summary>El efecto nuevo de cada juego, con el juego real: la jugada se hace por reflexión y se saca una toma antes, durante (~0,12 s) y después.</summary>
        private static IEnumerator JuiceShots(string id)
        {
            string typeName = id == "calculo" ? "CalculoGameController" : id == "stroop" ? "StroopGameController" : id == "anagramas" ? "PuntaGameController" : "RastroGameController";
            Configure(id, tutorial: false, reduceMotion: false);
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            yield return EnterPlay();
            MonoBehaviour c = null;
            yield return Wait.Until(() => (c = FindController(typeName)) != null, 40);
            if (c == null) { GameNotes.Add("no apareció " + typeName); yield return ExitPlay(); yield break; }
            yield return WaitPlayable();

            if (id == "calculo")
            {
                yield return Wait.Until(() => Convert.ToString(Member(c, "_phase")) == "Play", 20);
                yield return Wait.For(2.0);
                // la fusión para la lámina: la suma más alta que todavía no llega a la carga Y mejora la mejor celda en 3 luces o más (así el aro sube de «antes» a «después»; la solución, de un solo paso en el nivel 1, lo llenaría de una vez).
                // Si esta carga no tiene una así, se resuelve (para que venga otra) y se vuelve a mirar, hasta 12 veces.
                int ida = -1, idb = -1, target = 0;
                for (int attempt = 0; attempt < 12 && ida < 0; attempt++)
                {
                    var puzzle = Member(c, "_puzzle");
                    target = (int)Member(puzzle, "Target");
                    var cells = (System.Collections.IList)Member(Member(c, "_board"), "Cells");
                    int bestCell = 0;
                    for (int i = 0; i < cells.Count; i++) { int v = (int)Member(cells[i], "Value"); if (v <= target && v > bestCell) bestCell = v; }
                    int bestSum = bestCell;
                    for (int i = 0; i < cells.Count; i++)
                        for (int j = i + 1; j < cells.Count; j++)
                        {
                            int sum = (int)Member(cells[i], "Value") + (int)Member(cells[j], "Value");
                            if (sum < target && sum > bestSum) { bestSum = sum; ida = (int)Member(cells[i], "Id"); idb = (int)Member(cells[j], "Id"); }
                        }
                    if (ida >= 0 && Mathf.RoundToInt(12f * bestSum / target) - Mathf.RoundToInt(12f * bestCell / target) < 3) ida = idb = -1;     // el aro tiene que SUBIR a la vista: al menos 3 luces más
                    if (ida >= 0) break;
                    // sin mejora posible: resolver esta carga y esperar la siguiente
                    var sol = (System.Collections.IList)Member(puzzle, "Solution");
                    var st = sol[0];
                    int sa = (int)Member(st, "A"), sb = (int)Member(st, "B");
                    int sia = -1, sib = -1;
                    foreach (var cell in cells)
                    {
                        int v = (int)Member(cell, "Value"), cid = (int)Member(cell, "Id");
                        if (sia < 0 && v == sa) sia = cid; else if (sib < 0 && v == sb) sib = cid;
                    }
                    if (sia < 0 || sib < 0) break;
                    Call(c, "Merge", sia, Member(st, "Op"), sib);
                    yield return Wait.Until(() => !ReferenceEquals(Member(c, "_puzzle"), puzzle) && Convert.ToString(Member(c, "_phase")) == "Play", 20);
                    yield return Wait.For(2.0);
                }
                Shot("1-antes");
                if (ida < 0 || idb < 0) { GameNotes.Add("no encontré una fusión que mejore el aro sin resolver la carga"); }
                else
                {
                    Call(c, "Merge", ida, NeuroVida.Games.Calculo.CargaOp.Add, idb);
                    yield return Wait.For(0.12);
                    Shot("2-durante");
                    yield return Wait.For(2.0);
                    Shot("3-despues");
                }
            }
            else if (id == "stroop")
            {
                yield return Wait.Until(() => Member(c, "_acceptInput") is bool ok && ok, 20);
                yield return Wait.For(0.5);
                Shot("1-antes");
                Call(c, "OnColorTapped", (int)Member(Member(c, "_trial"), "CorrectIndex"));
                yield return Wait.For(0.14);
                Shot("2-durante");
                yield return Wait.Until(() => Member(c, "_acceptInput") is bool ok && ok, 10);
                yield return Wait.For(0.6);
                int correct = (int)Member(Member(c, "_trial"), "CorrectIndex");
                Call(c, "OnColorTapped", (correct + 1) % 4);
                yield return Wait.For(0.5);
                Shot("3-despues");
            }
            else if (id == "anagramas")
            {
                yield return Wait.Until(() => Convert.ToString(Member(c, "_phase")) == "Think", 30);
                yield return Wait.For(2.8);
                Shot("1-antes");
                Call(c, "ShowTiles", true);
                yield return Wait.For(0.14);
                Shot("2-durante");
                yield return Wait.For(0.9);
                Shot("3-despues");
            }
            else
            {
                yield return Wait.Until(() => Convert.ToString(Member(c, "_phase")) == "Input", 90);
                yield return Wait.For(0.5);
                Shot("1-antes");
                var fixedList = (System.Collections.IList)Member(c, "_fixed");
                var linkType = fixedList.GetType().GetGenericArguments()[0];
                object Link(int a, int b, float born)
                {
                    object l = Activator.CreateInstance(linkType);
                    linkType.GetField("A").SetValue(l, a);
                    linkType.GetField("B").SetValue(l, b);
                    linkType.GetField("Born").SetValue(l, born);
                    return l;
                }
                float now = NeuroVida.Games.Shared.GameClock.Time;
                fixedList.Add(Link(0, 1, now - 1f));
                fixedList.Add(Link(1, 2, now - 1f));
                fixedList.Add(Link(2, 3, NeuroVida.Games.Shared.GameClock.Time));
                // el juego arma la malla del hilo en su siguiente cuadro: la toma «durante» sale en cuanto hay tramos (el tercero, recién nacido, a medio tender)
                var fixedMesh = Member(c, "_fixedMesh");
                yield return Wait.Until(() => (int)Member(fixedMesh, "Count") > 0, 5);
                Shot("2-durante");
                yield return Wait.For(0.8);
                Shot("3-despues");
            }
            yield return ExitPlay();
        }

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
            else yield return ForcedEndShots(id);
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

        /// <summary>
        /// UN solo final (Tarea 126): cuando aparece el cierre, una toma del «momento del objeto» (la escena apagada y el emblema del juego en su sitio), otra con la cortina «¡Listo!» ya armada y, cuando la cortina se va sin que la app se abra
        /// (Editor y pruebas), una con la escena donde terminó el juego y el «Continuar» de respaldo. Unity ya no dibuja ningún resultado: ese vive en la app.
        /// </summary>
        private static IEnumerator EndShots()
        {
            yield return Wait.For(0.8);
            if (CurtainUp()) Shot("momento");
            yield return Wait.For(1.0);
            if (CurtainUp()) Shot("listo");
            yield return Wait.Until(() => !CurtainUp(), 12);
            yield return Wait.For(1.8);
            Shot("final");
        }

        /// <summary>
        /// Los juegos que no llegan a su fin en los 40 s de la toma (la mayoría): se arma el MISMO cierre que usa <c>ExitButton</c> (el momento del objeto y la cortina «¡Listo!», con el emblema del juego) sobre la partida tal como va,
        /// y se sacan las tomas «momento» y «listo». Así se ve el final real de los 17 juegos sin jugarlos enteros; la escena de abajo es la de los 40 s, no la del último ensayo.
        /// </summary>
        private static IEnumerator ForcedEndShots(string id)
        {
            MonoBehaviour runner = null;
            foreach (var mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
            {
                var ns = mb.GetType().Namespace;
                if (mb.isActiveAndEnabled && ns != null && ns.StartsWith("NeuroVida.Games") && mb.GetType().Name.EndsWith("Controller")) { runner = mb; break; }
            }
            if (runner == null) { GameNotes.Add("sin controlador para armar el cierre forzado"); yield break; }
            var curtain = NeuroVida.Games.Shared.FinishCurtain.Create(runner.transform, id);
            runner.StartCoroutine(curtain.Play(runner));
            yield return Wait.For(0.8);
            Shot("momento");
            yield return Wait.For(1.0);
            Shot("listo");
            yield return Wait.For(0.6);                     // la cortina termina de animarse (la corrutina lee sus grupos hasta el último cuadro) antes de quitarla
            if (curtain != null) UnityEngine.Object.Destroy(curtain.gameObject);
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

        /// <summary>El tutorial guiado con sus pasos andando solos (el smoke): una toma del paso 1 y otra del paso 3, a 0,3 s de que el paso aparece (para que ya se vea asentado; en algunos juegos el paso 1 dura poco). Piloto suma el paso 6, el de «Cruzaste el arco: ¡tu misión cambió!» (Tarea 63), y el 7, el de tocar la señal de la misión nueva (Tarea 64); Rescate suma el paso 4, «Ahora toca ¡Rescatar!», con lo marcado y el botón encendido (Tarea 65).</summary>
        private static IEnumerator TutorialShots(string id, int height = 2400, string suffix = "")
        {
            Height = height;                                                                    // 1920 = la pantalla más baja (16:9): con el tutorial es donde más aprieta la fila fija de arriba (Tarea 128)
            Configure(id, tutorial: true, reduceMotion: false);
            yield return EnterPlay();
            bool shot1 = false, shot3 = false, shotNew = id != "piloto", shotNew2 = id != "piloto", shotRescue = id != "radar", shotDock = id != "acoplamiento", shotMirror = id != "acoplamiento", shotLand = id != "aterrizaje";
            int lastIndex = -1;
            double activeSince = 0, start = Now;
            NeuroVida.Games.Shared.NubiCoach coach = null;
            double lastFind = 0;
            yield return Wait.Until(() =>
            {
                if (coach == null && Now - lastFind > 0.25) { lastFind = Now; coach = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.NubiCoach>(); }
                int index = NeuroVida.Games.Shared.NubiCoach.AuditSteps.Count;
                bool active = coach != null && coach.Active;
                if (!active) { lastIndex = -1; return (shot3 && shotNew && shotNew2 && shotRescue && shotDock && shotMirror && shotLand) || (shot1 && index > (id == "piloto" || id == "acoplamiento" ? 7 : id == "radar" ? 3 : 2)); }
                if (index != lastIndex) { lastIndex = index; activeSince = Now; }
                if (Now - activeSince >= 0.3)
                {
                    if (index == 0 && !shot1) { Shot("tutorial-1" + suffix); shot1 = true; }
                    else if (index == 2 && !shot3) { Shot("tutorial-3" + suffix); shot3 = true; }
                    else if (index == 5 && id == "piloto" && !shotNew) { Shot("tutorial-nueva-mision" + suffix); shotNew = true; }
                    else if (index == 6 && id == "piloto" && !shotNew2) { Shot("tutorial-toca-nueva-mision" + suffix); shotNew2 = true; }
                    else if (index == 3 && id == "acoplamiento" && !shotDock) { Shot("tutorial-se-enciende" + suffix); shotDock = true; }                 // «¡Encaja! Se enciende una luz» con el módulo a ras del hueco y la fila de luces iluminada (Tarea 68; v3 en la 118)
                    else if (index == 6 && id == "acoplamiento" && !shotMirror) { Shot("tutorial-espejo" + suffix); shotMirror = true; }             // «¿Y este?» con el espejo esperando
                    else if (index == 1 && id == "aterrizaje" && !shotLand) { Shot("tutorial-suelta" + suffix); shotLand = true; }                  // «Suelta para aterrizar» con la nave bajando y el hueco de la regla iluminado
                    else if (index == 3 && id == "radar" && !shotRescue) { Shot("tutorial-rescatar" + suffix); shotRescue = true; }          // «Ahora toca ¡Rescatar!» con las dos cápsulas marcadas y el botón encendido (Tarea 65)
                }
                return shot3 && shotNew && shotNew2 && shotRescue && shotDock && shotMirror && shotLand;
            }, 90);
            if (!shotDock || !shotMirror) GameNotes.Add("el tutorial de Acoplamiento no llegó a los pasos de la fila de luces y del espejo (pasos que se cerraron: " + NeuroVida.Games.Shared.NubiCoach.AuditSteps.Count + ")");
            if (!shotNew || !shotNew2) GameNotes.Add("el tutorial de Piloto no llegó a los pasos de la misión nueva (pasos que se cerraron: " + NeuroVida.Games.Shared.NubiCoach.AuditSteps.Count + ")");
            if (!shotRescue) GameNotes.Add("el tutorial de Rescate no llegó al paso «¡Rescatar!» (pasos que se cerraron: " + NeuroVida.Games.Shared.NubiCoach.AuditSteps.Count + ")");
            if (!shot1) GameNotes.Add("el tutorial no mostró el paso 1 a tiempo");
            if (!shot3) GameNotes.Add("el tutorial no llegó a un paso 3 (pasos que se cerraron: " + NeuroVida.Games.Shared.NubiCoach.AuditSteps.Count + ")");
            NeuroVida.Games.Shared.NubiCoach.AuditEnabled = false;
            Height = 2400;
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

        // ------------------------------------------------------------------ Rescate relámpago (su guion)

        private static IEnumerator RadarScript()
        {
            Configure("radar", tutorial: false, reduceMotion: false);
            EditorPlaytestBootstrap.RescStageOverride = 8;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Radar.RadarGameController.EditorShotMode = true;
            yield return EnterPlay();
            NeuroVida.Games.Radar.RadarGameController controller = null;
            yield return Wait.Until(() => (controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Radar.RadarGameController>()) != null, 40);
            if (controller == null) { GameNotes.Add("no apareció el juego de Rescate relámpago"); yield return ExitPlay(); yield break; }
            controller.StartCoroutine(controller.EditorShotScript(Shot, PauseShot));
            yield return Wait.Until(() => controller == null || controller.EditorShotFinished, 240);
            if (controller != null && !controller.EditorShotFinished) GameNotes.Add("el guion de Rescate relámpago no terminó a tiempo");
            NeuroVida.Games.Radar.RadarGameController.EditorShotMode = false;
            yield return ExitPlay();
        }

        /// <summary>Rescate relámpago en 16:9 (1080 × 1920): el layout compacto de la Tarea 62, con unas pocas tomas (nombres «…-16x9»).</summary>
        private static IEnumerator RadarScript16()
        {
            Height = 1920;
            Configure("radar", tutorial: false, reduceMotion: false);
            EditorPlaytestBootstrap.RescStageOverride = 8;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Radar.RadarGameController.EditorShotMode = true;
            yield return EnterPlay();
            NeuroVida.Games.Radar.RadarGameController controller = null;
            yield return Wait.Until(() => (controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Radar.RadarGameController>()) != null, 40);
            if (controller == null) { GameNotes.Add("no apareció el juego de Rescate relámpago (16:9)"); Height = 2400; yield return ExitPlay(); yield break; }
            controller.StartCoroutine(controller.EditorShortShotScript(Shot));
            yield return Wait.Until(() => controller == null || controller.EditorShotFinished, 120);
            if (controller != null && !controller.EditorShotFinished) GameNotes.Add("el guion de Rescate relámpago en 16:9 no terminó a tiempo");
            NeuroVida.Games.Radar.RadarGameController.EditorShotMode = false;
            Height = 2400;
            yield return ExitPlay();
        }

        // ------------------------------------------------------------------ Aterrizaje Lunar (su guion)

        private static IEnumerator AterrizajeScript()
        {
            Configure("aterrizaje", tutorial: false, reduceMotion: false);
            EditorPlaytestBootstrap.TimedOverride = false;                                               // Precisión: la instrucción se ve en los primeros aterrizajes
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Aterrizaje.LandingGameController.EditorShotMode = true;
            yield return EnterPlay();
            NeuroVida.Games.Aterrizaje.LandingGameController controller = null;
            yield return Wait.Until(() => (controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Aterrizaje.LandingGameController>()) != null, 40);
            if (controller == null) { GameNotes.Add("no apareció el juego de Aterrizaje Lunar"); yield return ExitPlay(); yield break; }
            controller.StartCoroutine(controller.EditorShotScript(Shot, PauseShot));
            yield return Wait.Until(() => controller == null || controller.EditorShotFinished, 240);
            if (controller != null && !controller.EditorShotFinished) GameNotes.Add("el guion de Aterrizaje Lunar no terminó a tiempo");
            NeuroVida.Games.Aterrizaje.LandingGameController.EditorShotMode = false;
            yield return ExitPlay();
        }

        /// <summary>Aterrizaje Lunar en 16:9 (1080 × 1920): el layout compacto (la regla va anclada abajo), con unas pocas tomas (nombres «…-16x9»).</summary>
        private static IEnumerator AterrizajeScript16()
        {
            Height = 1920;
            Configure("aterrizaje", tutorial: false, reduceMotion: false);
            EditorPlaytestBootstrap.TimedOverride = false;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Aterrizaje.LandingGameController.EditorShotMode = true;
            yield return EnterPlay();
            NeuroVida.Games.Aterrizaje.LandingGameController controller = null;
            yield return Wait.Until(() => (controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Aterrizaje.LandingGameController>()) != null, 40);
            if (controller == null) { GameNotes.Add("no apareció el juego de Aterrizaje Lunar (16:9)"); Height = 2400; yield return ExitPlay(); yield break; }
            controller.StartCoroutine(controller.EditorShortShotScript(Shot));
            yield return Wait.Until(() => controller == null || controller.EditorShotFinished, 120);
            if (controller != null && !controller.EditorShotFinished) GameNotes.Add("el guion corto de Aterrizaje Lunar en 16:9 no terminó a tiempo");
            NeuroVida.Games.Aterrizaje.LandingGameController.EditorShotMode = false;
            Height = 2400;
            yield return ExitPlay();
        }

        // ------------------------------------------------------------------ Acoplamiento (su guion)

        private static IEnumerator AcoplamientoScript()
        {
            Configure("acoplamiento", tutorial: false, reduceMotion: false);
            EditorPlaytestBootstrap.TimedOverride = true;                                                // en Reto: se ve la barra de «Tiempo»
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Acoplamiento.DockingGameController.EditorShotMode = true;
            yield return EnterPlay();
            NeuroVida.Games.Acoplamiento.DockingGameController controller = null;
            yield return Wait.Until(() => (controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Acoplamiento.DockingGameController>()) != null, 40);
            if (controller == null) { GameNotes.Add("no apareció el juego de Acoplamiento"); yield return ExitPlay(); yield break; }
            controller.StartCoroutine(controller.EditorShotScript(Shot, PauseShot));
            yield return Wait.Until(() => controller == null || controller.EditorShotFinished, 240);
            if (controller != null && !controller.EditorShotFinished) GameNotes.Add("el guion de Acoplamiento no terminó a tiempo");
            NeuroVida.Games.Acoplamiento.DockingGameController.EditorShotMode = false;
            yield return ExitPlay();
        }

        /// <summary>Acoplamiento en 16:9 (1080 × 1920): el layout compacto, con unas pocas tomas (nombres «…-16x9»).</summary>
        private static IEnumerator AcoplamientoScript16()
        {
            Height = 1920;
            Configure("acoplamiento", tutorial: false, reduceMotion: false);
            EditorPlaytestBootstrap.TimedOverride = true;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Acoplamiento.DockingGameController.EditorShotMode = true;
            yield return EnterPlay();
            NeuroVida.Games.Acoplamiento.DockingGameController controller = null;
            yield return Wait.Until(() => (controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Acoplamiento.DockingGameController>()) != null, 40);
            if (controller == null) { GameNotes.Add("no apareció el juego de Acoplamiento (16:9)"); Height = 2400; yield return ExitPlay(); yield break; }
            controller.StartCoroutine(controller.EditorShortShotScript(Shot));
            yield return Wait.Until(() => controller == null || controller.EditorShotFinished, 120);
            if (controller != null && !controller.EditorShotFinished) GameNotes.Add("el guion corto de Acoplamiento en 16:9 no terminó a tiempo");
            NeuroVida.Games.Acoplamiento.DockingGameController.EditorShotMode = false;
            Height = 2400;
            yield return ExitPlay();
        }

        // ------------------------------------------------------------------ piezas

        private static Transform _countdownRoot;

        /// <summary>Espera a que termine la cuenta regresiva (el objeto «CountdownScreen» se apaga) y deja 0,8 s para que la primera pantalla se asiente. Si nunca se ve la cuenta, a los 14 s sigue igual.</summary>
        private static IEnumerator WaitPlayable()
        {
            bool seen = false, shotTaken = false;
            double start = Now, lastFind = 0, seenAt = 0;
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
                if (active)
                {
                    if (!seen) seenAt = Now;
                    seen = true;
                    // la cuenta regresiva (el anillo, la píldora del título y el número) a los ~1,3 s: en el paso «2»
                    if (!shotTaken && Now - seenAt > 1.3) { shotTaken = true; Shot("cuenta"); }
                }
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
            int framesBefore = Time.frameCount;
            yield return Wait.Until(() => Now >= settleAt && (group == null || group.alpha >= 0.999f), 5);
            yield return Wait.For(0.25);
            // Mientras se espera acá el juego avanza muy pocos cuadros (en una corrida, 2 en 5 s) y la entrada del menú cuenta el tiempo por cuadro: sin esto la pausa salía a medio aparecer, desvaída y con los botones sin leerse. Se termina la entrada de golpe.
            Debug.Log("[Capturas] entrada de la pausa: grupo " + (group == null ? "NULO" : "alfa " + group.alpha.ToString("0.00")) + ", cuadros de juego durante la espera " + (Time.frameCount - framesBefore) + "; se termina de golpe");
            if (menu != null) menu.SettleForCapture();
            LogPauseState();
            Shot("pausa");
            // la pregunta de «Reiniciar» («¿Empezar de nuevo esta partida?», Tarea 127): una toma y se cancela con Atrás
            if (menu != null && menu.IsShown)
                foreach (var kv in menu.VisibleButtons())
                    if (kv.Key.Contains("Reiniciar"))
                    {
                        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(kv.Value.gameObject, new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current), UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                        break;
                    }
            if (menu != null && menu.Confirming)
            {
                menu.SettleForCapture();
                yield return Wait.For(0.3);
                Shot("pausa-reiniciar");
                menu.ResumeFromBack();                                                         // Atrás en la pregunta vuelve al menú
            }
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
            if (_juice) Canvas.ForceUpdateCanvases();
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
