using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Smoke test headless: abre la escena de prueba del piloto, entra en Play, deja
    /// correr unos segundos reales (suficiente para countdown + primera presentación de
    /// secuencia) y reporta si hubo errores/excepciones en consola antes de salir. No
    /// simula toques de usuario -- solo confirma que el arranque real (parseo de JSON,
    /// seed del DDA, construcción de UI, síntesis de audio) no explota.
    ///
    /// Invocar con:
    /// Unity.exe -batchmode -nographics -projectPath &lt;path&gt;
    ///   -executeMethod NeuroVida.Bridge.EditorTools.HeadlessPlaymodeSmokeTest.Run
    /// (sin -quit -- este script llama a EditorApplication.Exit cuando termina).
    /// </summary>
    public static class HeadlessPlaymodeSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/SecuenciaPilotoTest.unity";
        private static float RunSeconds = 6f;

        private static int _errorCount;
        private static double _enteredPlayAt;
        private static bool _isRunning;
        private static bool _previousDomainReloadDisabled;
        private static EnterPlayModeOptions _previousOptions;

        /// <summary>Con la variable de entorno <c>NUBI_REDUCE_MOTION=1</c> el smoke corre con "quitar animaciones" activo
        /// (la config del juego lleva <c>reduce_motion = true</c>, como cuando el teléfono tiene la escala de animación en 0).</summary>
        private static bool ReduceMotionRequested => Environment.GetEnvironmentVariable("NUBI_REDUCE_MOTION") == "1";

        /// <summary>Smoke de «Rastro de luz» (id <c>secuencia</c>, el juego por defecto de la escena): cuenta regresiva y la primera muestra de la chispa.</summary>
        public static void Run() => RunGame(null, 10f);

        /// <summary>Rastro de luz con su tutorial guiado (<c>show_tutorial</c>): la tarjeta de Nubi maestra queda esperando un toque que el smoke no da.</summary>
        public static void RunTutorial() => RunGame(null, 12f, tutorial: true);

        /// <summary>Los tutoriales guiados de los otros tres juegos del inicio (la tarjeta de Nubi sigue sola y se juega el principio de la ronda guiada).</summary>
        public static void RunTutorialFreno() => RunGame("freno", 12f, tutorial: true);
        public static void RunTutorialAterrizaje() => RunGame("aterrizaje", 12f, tutorial: true);
        public static void RunTutorialMeteoros() => RunGame("meteoros", 12f, tutorial: true);
        /// <summary>Tinta o Palabra («Dos orillas») con su tutorial guiado (sin versión corta: no está en el inicio).</summary>
        public static void RunTutorialStroop() => RunGame("stroop", 12f, tutorial: true);
        /// <summary>«En la punta de la lengua» (id anagramas) con su tutorial guiado: la tarjeta de Nubi y la primera palabra, donde Nubi toca «¡La tengo!» y arma la palabra.</summary>
        public static void RunTutorialAnagramas() => RunGame("anagramas", 12f, tutorial: true);

        /// <summary>«Carga exacta» (id calculo) con su tutorial guiado: la tarjeta de Nubi y la carga fácil (3 celdas, solo sumas) con tres focos: celda, «+» y otra celda.</summary>
        public static void RunTutorialCalculo() => RunGame("calculo", 12f, tutorial: true);

        /// <summary>«Engranajes» (id engranajes) con su tutorial guiado: la tarjeta de Nubi y una máquina fácil con tres focos (la flecha del motor, la pieza de la pregunta y el botón correcto).</summary>
        public static void RunTutorialEngranajes() => RunGame("engranajes", 12f, tutorial: true);

        /// <summary>«Bodega de carga» (id bodega) con su tutorial guiado: la tarjeta de Nubi, la carga que entra por la esclusa y el robot la guarda, el toque en la escotilla pedida y un error de muestra.</summary>
        public static void RunTutorialBodega() => RunGame("bodega", 12f, tutorial: true);

        /// <summary>La versión corta del inicio («Tu punto de partida»: <c>assessment</c>) de Freno, Aterrizaje y Meteoros.</summary>
        public static void RunCortoFreno() => RunGame("freno", 10f, assessment: true);
        public static void RunCortoAterrizaje() => RunGame("aterrizaje", 10f, assessment: true);
        public static void RunCortoMeteoros() => RunGame("meteoros", 10f, assessment: true);

        /// <summary>Mismo smoke test pero con "Tinta o Palabra" (Stroop) como juego.</summary>
        public static void RunStroop() => RunGame("stroop", 9f);

        /// <summary>Mismo smoke test pero con «En la punta de la lengua» (id anagramas) como juego.</summary>
        public static void RunAnagramas() => RunGame("anagramas", 9f);

        /// <summary>Mismo smoke test pero con «Carga exacta» (id calculo) como juego.</summary>
        public static void RunCalculo() => RunGame("calculo", 9f);

        /// <summary>Mismo smoke test pero con «Engranajes» (id engranajes) como juego.</summary>
        public static void RunEngranajes() => RunGame("engranajes", 24f);

        /// <summary>Mismo smoke test pero con «Bodega de carga» (id bodega) como juego (se juega sola: pedidos pares acertados, impares con un error primero).</summary>
        public static void RunBodega() => RunGame("bodega", 30f);

        /// <summary>Mismo smoke test pero con Piloto Estelar como juego.</summary>
        public static void RunPiloto() => RunGame("piloto", 9f);

        /// <summary>Mismo smoke test pero con Radar como juego.</summary>
        public static void RunRadar() => RunGame("radar", 9f);

        /// <summary>Mismo smoke test pero con Satélites como juego.</summary>
        public static void RunSatelites() => RunGame("satelites", 9f);

        /// <summary>Mismo smoke test pero con Freno de Emergencia como juego.</summary>
        public static void RunFreno() => RunGame("freno", 9f);

        /// <summary>Mismo smoke test pero con Lluvia de meteoros como juego.</summary>
        public static void RunMeteoros() => RunGame("meteoros", 9f);

        /// <summary>Mismo smoke test pero con ¿Verdad o disparate? como juego.</summary>
        public static void RunDisparate() => RunGame("disparate", 9f);

        /// <summary>Mismo smoke test pero con Cosecha de palabras como juego.</summary>
        public static void RunCosecha() => RunGame("cosecha", 9f);

        /// <summary>Mismo smoke test pero con La estrella intrusa como juego.</summary>
        public static void RunIntrusa() => RunGame("intrusa", 9f);

        /// <summary>Mismo smoke test pero con Aterrizaje Lunar como juego.</summary>
        public static void RunAterrizaje() => RunGame("aterrizaje", 9f);

        /// <summary>Mismo smoke test pero con Acoplamiento como juego.</summary>
        public static void RunAcoplamiento() => RunGame("acoplamiento", 9f);


        /// <summary>Mismo smoke test pero con Bitácora de Misión como juego.</summary>
        public static void RunBitacora() => RunGame("bitacora", 9f);

        /// <summary>Mismo smoke test pero con Rumbo a Casa como juego.</summary>
        public static void RunRumbo() => RunGame("rumbo", 9f);

        /// <summary>Mismo smoke test pero con Correo Estelar como juego.</summary>
        public static void RunCorreo() => RunGame("correo", 9f);

        /// <summary>Mismo smoke test pero con Parejas Ocultas como juego.</summary>
        public static void RunParejas() => RunGame("parejas", 9f);

        // ------------------------------------------------------------------ encadenado en UN solo proceso de Unity

        /// <summary>Nombre de cada juego (el sufijo de su método Run*) con su id y los segundos de partida del smoke.</summary>
        private static readonly (string Name, string Id, float Seconds)[] Catalog =
        {
            ("Run", null, 10f), ("Tutorial", "secuencia", 12f),
            ("TutorialFreno", "freno", 12f), ("TutorialAterrizaje", "aterrizaje", 12f), ("TutorialMeteoros", "meteoros", 12f), ("TutorialStroop", "stroop", 12f), ("TutorialAnagramas", "anagramas", 12f), ("TutorialCalculo", "calculo", 12f), ("TutorialEngranajes", "engranajes", 12f), ("TutorialBodega", "bodega", 12f),
            ("CortoFreno", "freno", 10f), ("CortoAterrizaje", "aterrizaje", 10f), ("CortoMeteoros", "meteoros", 10f), ("Stroop", "stroop", 9f), 
            ("Calculo", "calculo", 9f), ("Engranajes", "engranajes", 24f), ("Bodega", "bodega", 30f), ("Anagramas", "anagramas", 9f),
            ("Parejas", "parejas", 9f), ("Piloto", "piloto", 9f), ("Radar", "radar", 9f), ("Satelites", "satelites", 9f),
            ("Freno", "freno", 9f), ("Aterrizaje", "aterrizaje", 9f), ("Acoplamiento", "acoplamiento", 9f),
            ("Bitacora", "bitacora", 9f), ("Rumbo", "rumbo", 9f), ("Correo", "correo", 9f), ("Meteoros", "meteoros", 9f),
            ("Disparate", "disparate", 9f), ("Cosecha", "cosecha", 9f), ("Intrusa", "intrusa", 9f),
        };

        private static readonly Queue<(string Name, string Id, float Seconds)> _queue = new Queue<(string, string, float)>();
        private static readonly Queue<int> _heights = new Queue<int>();       // alto de pantalla de cada corrida de _queue (0 = el de la ventana del Editor)
        private static (string Name, string Id, float Seconds) _current;
        private static int _currentHeight;
        private static int _listOk, _listTotal;
        private static bool _listRunning, _listPlaying, _listAnyFailed;

        /// <summary>
        /// Smoke de VARIOS juegos en un solo proceso (abrir Unity 23 veces es lo que más tarda). La lista sale de la variable de entorno
        /// NUBI_SMOKE_GAMES o del argumento <c>-smokeGames</c> (nombres separados por comas: Meteoros,Intrusa; vacía = los 23). Cada juego
        /// entra a Play, corre su tiempo, sale, y entra el siguiente (sin domain reload, igual que el smoke de uno solo).
        /// </summary>
        public static void RunList()
        {
            var raw = Environment.GetEnvironmentVariable("NUBI_SMOKE_GAMES");
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++) if (args[i] == "-smokeGames") raw = args[i + 1];

            _queue.Clear();
            _heights.Clear();
            if (string.IsNullOrWhiteSpace(raw))
            {
                foreach (var g in Catalog) Enqueue(g);
            }
            else
            {
                foreach (var part in raw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = part.Trim();
                    var found = false;
                    foreach (var g in Catalog)
                    {
                        if (!string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                        Enqueue(g);
                        found = true;
                        break;
                    }
                    if (!found)
                    {
                        Debug.Log($"[SmokeTest] FALLÓ -- juego desconocido en la lista: {name}");
                        EditorApplication.Exit(2);
                        return;
                    }
                }
            }

            _listTotal = _queue.Count;
            _listOk = 0;
            _listAnyFailed = false;
            EditorSceneManager.OpenScene(ScenePath);
            _previousDomainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled;
            _previousOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Application.logMessageReceived += OnLog;
            _listRunning = true;
            _listPlaying = false;
            EditorApplication.update += OnUpdateList;
            StartNextInList();
        }

        /// <summary>Alto de las tres formas de pantalla (ancho 1080) con que se revisa el tutorial de cada juego: 20:9 (el teléfono de Ricardo), 18:9 y 16:9.</summary>
        private static readonly int[] AuditHeights = { 2400, 2160, 1920 };

        /// <summary>Los tutoriales guiados corren en las tres formas de pantalla: <see cref="NeuroVida.Games.Shared.NubiCoach"/> registra cada paso y se comprueba que Nubi y su globo no tapen nada.</summary>
        private static void Enqueue((string Name, string Id, float Seconds) g)
        {
            if (g.Name.StartsWith("Tutorial"))
            {
                var only = Environment.GetEnvironmentVariable("NUBI_COACH_HEIGHTS");      // p. ej. «2160,1920»: repite solo esas formas
                foreach (var h in AuditHeights)
                {
                    if (!string.IsNullOrEmpty(only) && Array.IndexOf(only.Split(','), h.ToString()) < 0) continue;
                    _queue.Enqueue((g.Name, g.Id, g.Seconds + CoachAuditExtraSeconds)); _heights.Enqueue(h);
                }
                return;
            }
            _queue.Enqueue(g);
            _heights.Enqueue(0);
        }

        /// <summary>Más tiempo para que la ronda guiada llegue a sus últimos pasos (cada foco de «tocar» sigue solo a los 1,2 s en el Editor).</summary>
        private const float CoachAuditExtraSeconds = 22f;

        private static void StartNextInList()
        {
            _current = _queue.Dequeue();
            _currentHeight = _heights.Dequeue();
            _screenLogged = false;
            NeuroVida.Games.Shared.NubiCoach.AuditEnabled = _current.Name.StartsWith("Tutorial");
            NeuroVida.Games.Shared.NubiCoach.AuditGame = _current.Id ?? "secuencia";
            NeuroVida.Games.Shared.NubiCoach.AuditSteps.Clear();
            _auditCanvases.Clear();
            _errorCount = 0;
            _enteredPlayAt = 0;
            _pauseStage = 0;
            _listPlaying = true;
            EditorPlaytestBootstrap.GameIdOverride = _current.Id;
            EditorPlaytestBootstrap.ReduceMotionOverride = ReduceMotionRequested;
            // «Tutorial*» = con la tarjeta de Nubi (sigue sola en el Editor) y la ronda guiada; «Corto*» = la versión corta del inicio (assessment)
            bool tutorial = _current.Name.StartsWith("Tutorial");
            EditorPlaytestBootstrap.ShowTutorialOverride = tutorial;
            EditorPlaytestBootstrap.AssessmentOverride = _current.Name.StartsWith("Corto");
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoContinue = tutorial;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = _current.Name == "Engranajes" || _current.Name == "Bodega";      // la partida de Engranajes se juega sola (máquinas pares: la solución; impares: sin tocar)
            EditorApplication.EnterPlaymode();
        }

        // ------------------------------------------------------------------ la pausa de cada juego responde

        private static int _pauseStage;
        private static double _pauseAt;

        /// <summary>
        /// Después de los segundos de partida, abre la pausa del juego y comprueba que SE PUEDE TOCAR: que haya un EventSystem y que, en el centro de cada botón («Continuar»,
        /// «Reiniciar», «Cómo se juega» si está y «Salir»), lo primero que recibe el toque sea ese botón (así también se atrapa algo del juego que tape la pausa). Después toca
        /// «Continuar» y comprueba que la pausa se cierra y el reloj de juego sigue. Devuelve true cuando terminó (cada llamada es un paso, una por cuadro).
        /// </summary>
        private static bool PauseCheckDone()
        {
            double now = EditorApplication.timeSinceStartup;
            switch (_pauseStage)
            {
                case 0:
                {
                    var entry = UnityEngine.Object.FindObjectOfType<NeuroVida.Bridge.GameEntryPoint>();
                    if (entry == null) { PauseFail("no hay GameEntryPoint para abrir la pausa"); _pauseStage = 9; return true; }
                    var show = typeof(NeuroVida.Bridge.GameEntryPoint).GetMethod("ShowPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    show.Invoke(entry, null);
                    _pauseAt = now;
                    _pauseStage = 1;
                    return false;
                }
                case 1:
                {
                    if (now - _pauseAt < 0.6) return false;
                    var menu = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.PauseMenu>();
                    if (menu == null || !menu.IsShown) { PauseFail("la pausa no quedó visible"); _pauseStage = 9; return true; }
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    if (es == null) { PauseFail("no hay EventSystem: ningún botón de la pausa responde"); _pauseStage = 9; return true; }
                    UnityEngine.GameObject continueButton = null;
                    foreach (var kv in menu.VisibleButtons())
                    {
                        var screen = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, kv.Value.TransformPoint(kv.Value.rect.center));
                        // la ventana de juego del Editor es mucho más ancha que un teléfono: con cuatro botones el último puede quedar fuera de ella (en un teléfono entra de sobra)
                        if (screen.x < 0f || screen.y < 0f || screen.x > UnityEngine.Screen.width || screen.y > UnityEngine.Screen.height)
                        {
                            Debug.Log("[SmokeTest] aviso: «" + kv.Key + "» queda fuera de la ventana del Editor (" + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + "): no se prueba");
                            continue;
                        }
                        var data = new UnityEngine.EventSystems.PointerEventData(es) { position = screen };
                        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                        es.RaycastAll(data, hits);
                        if (hits.Count == 0 || !hits[0].gameObject.transform.IsChildOf(kv.Value))
                            PauseFail("el toque en el centro de «" + kv.Key + "» no llega al botón (primero recibe: " + (hits.Count == 0 ? "nada" : hits[0].gameObject.name) + ")");
                        else if (kv.Key.Contains("Continuar")) continueButton = hits[0].gameObject;
                    }
                    if (continueButton != null)
                    {
                        var click = new UnityEngine.EventSystems.PointerEventData(es);
                        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(continueButton, click, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                    }
                    _pauseAt = now;
                    _pauseStage = 2;
                    return false;
                }
                case 2:
                {
                    if (now - _pauseAt < 0.3) return false;
                    var menu = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.PauseMenu>();
                    if (menu != null && menu.IsShown) PauseFail("«Continuar» no cerró la pausa");
                    if (NeuroVida.Games.Shared.GameClock.Paused) PauseFail("«Continuar» no reanudó el reloj de juego");
                    _pauseStage = 9;
                    return true;
                }
                default:
                    return true;
            }
        }

        private static bool CoachAuditReachedEnd()
        {
            foreach (var st in NeuroVida.Games.Shared.NubiCoach.AuditSteps) if (st.Text == NeuroVida.Games.Shared.CoachTexts.Ready) return true;
            return false;
        }

        private static bool _screenLogged;
        private static readonly HashSet<int> _auditCanvases = new HashSet<int>();

        /// <summary>
        /// La ventana del Editor sin gráficos mide 640x480 y no se puede cambiar: para revisar el tutorial en otras formas de pantalla, el lienzo de cada juego (de pantalla completa, con su
        /// <c>CanvasScaler</c> de 1080 de ancho) se pasa a «espacio del mundo» con 1080 x <paramref name="height"/> unidades, que es justo el área que vería el juego en ese teléfono. Se hace en cuanto aparece,
        /// antes de que el juego calcule su disposición.
        /// </summary>
        private static void ForceAuditCanvasSize(int height)
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.GetComponent<UnityEngine.UI.CanvasScaler>() == null) continue;
                if (!_auditCanvases.Add(canvas.GetInstanceID())) continue;
                canvas.GetComponent<UnityEngine.UI.CanvasScaler>().enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                var rt = (RectTransform)canvas.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(1080f, height);
                rt.localScale = Vector3.one;
                rt.position = Vector3.zero;
            }
        }

        /// <summary>
        /// Los pasos del tutorial que registró <see cref="NeuroVida.Games.Shared.NubiCoach"/>: cada uno debe tener a Nubi y su globo SIN tapar el hueco, las zonas protegidas ni ningún texto del juego, con el texto en
        /// 3 líneas o menos y la letra en su tamaño. Deja el registro en <c>unity/test-results/coach-audit/&lt;juego&gt;-&lt;ancho&gt;x&lt;alto&gt;.json</c> (de ahí salen las láminas de revisión).
        /// </summary>
        private static void CheckCoachAudit()
        {
            if (!NeuroVida.Games.Shared.NubiCoach.AuditEnabled) return;
            NeuroVida.Games.Shared.NubiCoach.AuditEnabled = false;
            var steps = NeuroVida.Games.Shared.NubiCoach.AuditSteps;
            var game = NeuroVida.Games.Shared.NubiCoach.AuditGame;
            var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "test-results", "coach-audit"));
            System.IO.Directory.CreateDirectory(dir);
            var wrapper = new CoachAuditFile { Game = game, Width = Screen.width, Height = Screen.height, Steps = steps.ToArray() };
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, $"{game}-{_currentHeight}.json"), JsonUtility.ToJson(wrapper, true));
            Debug.Log($"[SmokeTest] tutorial de {game} a {Screen.width}x{Screen.height}: {steps.Count} paso(s) registrados");
            bool strict = true;
            if (steps.Count == 0 && strict) { _errorCount++; Debug.Log($"[SmokeTest] Error capturado: el tutorial de {game} no registró ningún paso"); }
            foreach (var st in steps)
            {
                string where = $"{game} {_currentHeight}, paso {st.Index} ({st.Kind}) «{st.Text}»";
                if (!st.Clean) { if (strict) _errorCount++; Debug.Log($"[SmokeTest] Error capturado: Nubi o el globo tapan algo ({Mathf.RoundToInt(st.Overlap)} u²{(st.TooLong ? ", el texto no cabe en 3 líneas" : "")}): {where}"); }
                if (st.ControlClash > 20f) { if (strict) _errorCount++; Debug.Log($"[SmokeTest] Error capturado: «Saltar tutorial» o el rótulo de práctica quedan sobre algo del juego ({Mathf.RoundToInt(st.ControlClash)} u²): {where}"); }
                if (st.ActualLines > NeuroVida.Games.Shared.CoachLayout.MaxLines) { if (strict) _errorCount++; Debug.Log($"[SmokeTest] Error capturado: el texto se dibuja en {st.ActualLines} líneas: {where}"); }
                if (st.ActualLines > st.Lines) { if (strict) _errorCount++; Debug.Log($"[SmokeTest] Error capturado: el globo se calculó para {st.Lines} línea(s) y el texto se dibuja en {st.ActualLines} (se saldría del globo): {where}"); }
            }
        }

        [Serializable]
        private sealed class CoachAuditFile { public string Game; public int Width, Height; public NeuroVida.Games.Shared.CoachStepReport[] Steps; }

        private static void PauseFail(string what)
        {
            _errorCount++;
            Debug.Log("[SmokeTest] Error capturado: pausa de " + (_current.Name ?? "este juego") + ": " + what);
        }

        private static void OnUpdateList()
        {
            if (!_listRunning) return;
            if (_listPlaying)
            {
                if (!EditorApplication.isPlaying) return; // todavía entrando a Play
                if (_currentHeight > 0) ForceAuditCanvasSize(_currentHeight);
                if (!_screenLogged) { _screenLogged = true; Debug.Log($"[SmokeTest] pantalla {Screen.width}x{Screen.height}" + (_currentHeight > 0 ? $", lienzo de revisión 1080x{_currentHeight}" : "")); }
                if (_enteredPlayAt == 0) _enteredPlayAt = EditorApplication.timeSinceStartup;
                double played = EditorApplication.timeSinceStartup - _enteredPlayAt;
                // la revisión del tutorial espera a que la ronda guiada llegue a su último aviso («¡Listo! Ahora va en serio»), con tope de 100 s
                if (NeuroVida.Games.Shared.NubiCoach.AuditEnabled) { if (played < 8 || (played < 100 && !CoachAuditReachedEnd())) return; }
                else if (played < _current.Seconds) return;
                if (_currentHeight == 0 && !PauseCheckDone()) return;  // abre la pausa y comprueba que responde (no en el lienzo de revisión: ahí no hay dónde tocar)

                _listPlaying = false;
                CheckCoachAudit();
                EditorApplication.ExitPlaymode();
                CheckReduceMotionArrived();
                var ok = _errorCount == 0;
                if (ok) _listOk++; else _listAnyFailed = true;
                Debug.Log(ok
                    ? $"[SmokeTest] {_current.Name}: OK"
                    : $"[SmokeTest] {_current.Name}: FALLÓ -- {_errorCount} error(es)/excepción(es) durante la partida.");
                return;
            }

            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return; // todavía saliendo de Play
            if (_queue.Count > 0) { StartNextInList(); return; }

            _listRunning = false;
            EditorApplication.update -= OnUpdateList;
            Application.logMessageReceived -= OnLog;
            RestoreDomainReloadSettings();
            Debug.Log($"[SmokeTest] RunList: {_listOk}/{_listTotal} " + (_listAnyFailed ? "FALLÓ" : "OK"));
            EditorApplication.Exit(_listAnyFailed ? 1 : 0);
        }

        private static void RunGame(string gameId, float seconds, bool tutorial = false, bool assessment = false)
        {
            RunSeconds = seconds;
            _pauseStage = 0;
            _current = (gameId ?? "juego", gameId, seconds);
            EditorPlaytestBootstrap.ShowTutorialOverride = tutorial;
            EditorPlaytestBootstrap.AssessmentOverride = assessment;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoContinue = tutorial;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = (gameId == "engranajes" || gameId == "bodega") && !tutorial;
            EditorPlaytestBootstrap.GameIdOverride = gameId;
            EditorPlaytestBootstrap.ReduceMotionOverride = ReduceMotionRequested;
            EditorSceneManager.OpenScene(ScenePath);

            // Entrar a Play dispara por defecto un domain reload -- eso borra estado
            // estático y desuscribe cualquier evento registrado antes de llamar
            // EnterPlaymode(), dejando este smoke test colgado para siempre (bug real
            // encontrado en la primera corrida: el proceso de Unity quedaba girando en
            // vacío sin nunca llamar EditorApplication.Exit). Se desactiva acá, solo
            // para esta corrida, y se restaura al final.
            _previousDomainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled;
            _previousOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            Application.logMessageReceived += OnLog;
            EditorApplication.update += OnUpdate;
            _isRunning = true;
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Con <c>NUBI_REDUCE_MOTION=1</c>, comprueba que la bandera llegó de verdad al juego (si no, el smoke no probaría nada).</summary>
        private static void CheckReduceMotionArrived()
        {
            if (!ReduceMotionRequested || NeuroVida.Games.Shared.GameFeel.ReduceMotion) return;
            _errorCount++;
            Debug.Log("[SmokeTest] Error capturado: NUBI_REDUCE_MOTION=1 pero GameFeel.ReduceMotion quedó en false");
        }

        private static void RestoreDomainReloadSettings()
        {
            EditorSettings.enterPlayModeOptionsEnabled = _previousDomainReloadDisabled;
            EditorSettings.enterPlayModeOptions = _previousOptions;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                _errorCount++;
                Debug.Log($"[SmokeTest] Error capturado: {condition}\n{stackTrace}");
            }
        }

        private static void OnUpdate()
        {
            if (!_isRunning) return;
            if (!EditorApplication.isPlaying) return; // todavía terminando el domain reload de entrar a Play

            if (_enteredPlayAt == 0) _enteredPlayAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - _enteredPlayAt < RunSeconds) return;
            if (!PauseCheckDone()) return;  // abre la pausa y comprueba que responde

            _isRunning = false;
            CheckReduceMotionArrived();
            EditorApplication.update -= OnUpdate;
            Application.logMessageReceived -= OnLog;
            EditorApplication.ExitPlaymode();
            RestoreDomainReloadSettings();

            Debug.Log(_errorCount == 0
                ? "[SmokeTest] OK -- sin errores durante los primeros segundos de partida."
                : $"[SmokeTest] FALLÓ -- {_errorCount} error(es)/excepción(es) durante la partida.");

            EditorApplication.Exit(_errorCount == 0 ? 0 : 1);
        }
    }
}
