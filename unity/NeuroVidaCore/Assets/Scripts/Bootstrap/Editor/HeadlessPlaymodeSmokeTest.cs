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
        public static void RunTutorial()
        {
            EditorPlaytestBootstrap.ShowTutorialOverride = true;
            RunGame(null, 8f);
        }

        /// <summary>Mismo smoke test pero con "Tinta o Palabra" (Stroop) como juego.</summary>
        public static void RunStroop() => RunGame("stroop", 9f);

        /// <summary>Mismo smoke test pero con Anagramas como juego.</summary>
        public static void RunAnagramas() => RunGame("anagramas", 9f);

        /// <summary>Mismo smoke test pero con Cálculo Sereno como juego.</summary>
        public static void RunCalculo() => RunGame("calculo", 9f);

        /// <summary>Mismo smoke test pero con Detective de Series como juego.</summary>
        public static void RunSeries() => RunGame("series", 9f);

        /// <summary>Mismo smoke test pero con Ruta del Tesoro como juego.</summary>
        public static void RunRutaTesoro() => RunGame("rutatesoro", 9f);

        /// <summary>Mismo smoke test pero con Cambio de Chip como juego.</summary>
        public static void RunCambioChip() => RunGame("cambiochip", 9f);

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

        /// <summary>Mismo smoke test pero con Tráfico Estelar como juego.</summary>
        public static void RunTrafico() => RunGame("trafico", 9f);

        /// <summary>Mismo smoke test pero con Bitácora de Misión como juego.</summary>
        public static void RunBitacora() => RunGame("bitacora", 9f);

        /// <summary>Mismo smoke test pero con Rumbo a Casa como juego.</summary>
        public static void RunRumbo() => RunGame("rumbo", 9f);

        /// <summary>Mismo smoke test pero con Correo Estelar como juego.</summary>
        public static void RunCorreo() => RunGame("correo", 9f);

        /// <summary>Mismo smoke test pero con Comparación Instantánea como juego.</summary>
        public static void RunComparacion() => RunGame("comparacion", 9f);

        /// <summary>Mismo smoke test pero con Parejas Ocultas como juego.</summary>
        public static void RunParejas() => RunGame("parejas", 9f);

        // ------------------------------------------------------------------ encadenado en UN solo proceso de Unity

        /// <summary>Nombre de cada juego (el sufijo de su método Run*) con su id y los segundos de partida del smoke.</summary>
        private static readonly (string Name, string Id, float Seconds)[] Catalog =
        {
            ("Run", null, 10f), ("Tutorial", "secuencia", 8f), ("Stroop", "stroop", 9f), ("Comparacion", "comparacion", 9f), ("CambioChip", "cambiochip", 9f),
            ("RutaTesoro", "rutatesoro", 9f), ("Series", "series", 9f), ("Calculo", "calculo", 9f), ("Anagramas", "anagramas", 9f),
            ("Parejas", "parejas", 9f), ("Piloto", "piloto", 9f), ("Radar", "radar", 9f), ("Satelites", "satelites", 9f),
            ("Freno", "freno", 9f), ("Aterrizaje", "aterrizaje", 9f), ("Acoplamiento", "acoplamiento", 9f), ("Trafico", "trafico", 9f),
            ("Bitacora", "bitacora", 9f), ("Rumbo", "rumbo", 9f), ("Correo", "correo", 9f), ("Meteoros", "meteoros", 9f),
            ("Disparate", "disparate", 9f), ("Cosecha", "cosecha", 9f), ("Intrusa", "intrusa", 9f),
        };

        private static readonly Queue<(string Name, string Id, float Seconds)> _queue = new Queue<(string, string, float)>();
        private static (string Name, string Id, float Seconds) _current;
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
            if (string.IsNullOrWhiteSpace(raw))
            {
                foreach (var g in Catalog) _queue.Enqueue(g);
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
                        _queue.Enqueue(g);
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

        private static void StartNextInList()
        {
            _current = _queue.Dequeue();
            _errorCount = 0;
            _enteredPlayAt = 0;
            _listPlaying = true;
            EditorPlaytestBootstrap.GameIdOverride = _current.Id;
            EditorPlaytestBootstrap.ReduceMotionOverride = ReduceMotionRequested;
            EditorPlaytestBootstrap.ShowTutorialOverride = _current.Name == "Tutorial";
            EditorApplication.EnterPlaymode();
        }

        private static void OnUpdateList()
        {
            if (!_listRunning) return;
            if (_listPlaying)
            {
                if (!EditorApplication.isPlaying) return; // todavía entrando a Play
                if (_enteredPlayAt == 0) _enteredPlayAt = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - _enteredPlayAt < _current.Seconds) return;

                _listPlaying = false;
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

        private static void RunGame(string gameId, float seconds)
        {
            RunSeconds = seconds;
            if (gameId != null) EditorPlaytestBootstrap.ShowTutorialOverride = false;
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
