using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Capturas de pantalla REALES de un juego (no una composición aparte): corre el juego en Play con el lienzo en forma de teléfono (1080x2400, el mismo truco del smoke), un guion lo lleva por los momentos que se
    /// fotografían, y una cámara ortográfica dibuja el lienzo a una textura que se guarda como PNG. NECESITA gráficos: Unity SIN <c>-nographics</c> y una tarjeta de video (no corre en el CI de GitHub ni en una sesión en la nube).
    /// Se usa desde <c>bash tools/verificar-todo.sh --capturas Correo</c>, que después arma la hoja de contacto (tools/capturas/hoja.py). Hoy solo Correo tiene guion.
    /// Variables de entorno: <c>NUBI_SHOTS_GAMES</c> (hoy solo «Correo») y <c>NUBI_SHOTS_DIR</c> (carpeta de salida).
    /// </summary>
    public static class ScreenshotRunner
    {
        private const string ScenePath = "Assets/Scenes/SecuenciaPilotoTest.unity";
        private const int Width = 1080, Height = 2400;
        private const double TimeoutSeconds = 150.0;

        private static string _dir;
        private static int _count;
        private static bool _scriptStarted, _failed;
        private static double _startedAt;
        private static bool _previousDomainReloadDisabled;
        private static EnterPlayModeOptions _previousOptions;

        public static void Run()
        {
            var game = (Environment.GetEnvironmentVariable("NUBI_SHOTS_GAMES") ?? "Correo").Trim();
            _dir = Environment.GetEnvironmentVariable("NUBI_SHOTS_DIR");
            if (string.IsNullOrEmpty(_dir)) _dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "test-results", "capturas", game.ToLowerInvariant()));
            if (!string.Equals(game, "Correo", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("[Capturas] FALLÓ -- " + game + " no tiene guion de capturas (hoy solo Correo)");
                EditorApplication.Exit(2);
                return;
            }
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Debug.Log("[Capturas] FALLÓ -- Unity corre sin gráficos (-nographics): no se puede dibujar. Corre sin ese argumento y con una tarjeta de video.");
                EditorApplication.Exit(2);
                return;
            }
            Directory.CreateDirectory(_dir);
            foreach (var old in Directory.GetFiles(_dir, "*.png")) File.Delete(old);

            EditorSceneManager.OpenScene(ScenePath);
            _previousDomainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled;
            _previousOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            EditorPlaytestBootstrap.GameIdOverride = "correo";
            EditorPlaytestBootstrap.ShowTutorialOverride = false;
            EditorPlaytestBootstrap.AssessmentOverride = false;
            EditorPlaytestBootstrap.ReduceMotionOverride = false;
            EditorPlaytestBootstrap.MailStageOverride = 5;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoContinue = false;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = false;
            NeuroVida.Games.Correo.MailGameController.EditorShotMode = true;
            PlayerPrefs.DeleteKey("mail_intros");                       // para que las tarjetas «NUEVO» salgan aunque ya se hayan visto
            HeadlessPlaymodeSmokeTest.ResetAuditCanvases();
            _scriptStarted = _failed = false;
            _count = 0;
            _startedAt = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += OnUpdate;
            EditorApplication.EnterPlaymode();
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            _failed = true;
            Debug.Log("[Capturas] Error capturado: " + condition + "\n" + stackTrace);
        }

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying) return;
            HeadlessPlaymodeSmokeTest.ForceAuditCanvasSize(Height);
            var controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Correo.MailGameController>();
            if (controller != null && !_scriptStarted)
            {
                _scriptStarted = true;
                controller.StartCoroutine(controller.EditorShotScript(Shot, PauseShot));
            }
            bool timedOut = EditorApplication.timeSinceStartup - _startedAt > TimeoutSeconds;
            if (controller != null && !controller.EditorShotFinished && !timedOut) return;
            if (controller == null && !timedOut) return;
            Application.logMessageReceived -= OnLog;
            EditorApplication.update -= OnUpdate;
            NeuroVida.Games.Correo.MailGameController.EditorShotMode = false;
            EditorApplication.ExitPlaymode();
            EditorSettings.enterPlayModeOptionsEnabled = _previousDomainReloadDisabled;
            EditorSettings.enterPlayModeOptions = _previousOptions;
            bool ok = !timedOut && !_failed && controller != null && controller.EditorShotFinished;
            Debug.Log(ok ? "[Capturas] OK -- " + _count + " capturas en " + _dir : "[Capturas] FALLÓ -- " + (timedOut ? "el guion no terminó a tiempo (" + _count + " capturas)" : "errores durante el guion (" + _count + " capturas)"));
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Abre la pausa del juego (como el smoke), saca la foto «pausa» y la cierra con «Continuar» (si no, el reloj de juego se queda detenido).</summary>
        private static System.Collections.IEnumerator PauseShot()
        {
            var entry = UnityEngine.Object.FindObjectOfType<GameEntryPoint>();
            if (entry == null) yield break;
            typeof(GameEntryPoint).GetMethod("ShowPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(entry, null);
            for (float t = 0f; t < 0.9f; t += Time.unscaledDeltaTime) yield return null;
            Shot("pausa");
            var menu = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.PauseMenu>();
            if (menu != null && menu.IsShown)
                foreach (var kv in menu.VisibleButtons())
                    if (kv.Key.Contains("Continuar"))
                    {
                        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(kv.Value.gameObject, new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current), UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                        break;
                    }
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) yield return null;
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
        }
    }
}
