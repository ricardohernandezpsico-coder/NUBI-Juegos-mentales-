using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using NeuroVida.Bridge;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// La medición de la ENTRADA al juego (Tarea 190, 28-oct): cuánto tarda en estar listo el tablero y qué tan parejos son los cuadros desde que se ve hasta los 5 s de partida (que incluyen el «¡Ya!»). Es la herramienta con que se midió la causa del «pegado» de la cuenta regresiva (Tarea 189) y con que se comprueba que ya no está.
    /// <para><b>Solo con la bandera de depuración</b>: <see cref="Begin"/> no hace nada salvo que la app mande <c>debug_build</c> (<c>BuildConfig.DEBUG</c>) o, en el Editor, la variable de entorno <c>NUBI_PROBE=1</c>. En release no crea ningún objeto, no mide ni escribe nada.</para>
    /// <para>Escribe UNA línea por partida con la etiqueta <c>[sonda-entrada]</c> (en Android va al registro, <c>adb logcat -s Unity</c>): el tiempo hasta «listo» desde que llegó la configuración, los cuadros de la carga (los tapa la pantalla de carga de la app, no se ven) y, aparte, los cuadros VISIBLES (de «listo» a +5 s: cuántos, mediana, p95, máximo y cuántos pasan de 34 ms), más lo que
    /// gastó el hilo principal en crear clips de sonido y texturas de sprites (<see cref="Cost"/>). Si un cuadro visible pasa de 34 ms, su línea <c>[sonda-entrada-cuadro]</c> dice cuál fue y qué hacía.</para>
    /// </summary>
    public static class EntryProbe
    {
        /// <summary>Lo que dura la ventana visible que se mide, desde que se mostró el tablero.</summary>
        public const float VisibleWindowSeconds = 5f;

        /// <summary>El cuadro visible más largo aceptado (ms): arriba de esto se ve como un tirón.</summary>
        public const float HitchMs = 34f;

        public static bool Enabled { get; private set; }

        private static string _game = "";
        private static float _beganAt;
        private static float _shownAt = -1f;
        private static readonly List<float> Loading = new List<float>();
        private static readonly List<float> Visible = new List<float>();
        private static readonly List<string> Hitches = new List<string>();
        private static readonly Dictionary<string, double> Costs = new Dictionary<string, double>();
        private static double _costsAtShown;
        private static Driver _driver;
        private static string _lastLine = "";
        private static bool _debug;                                       // la bandera de depuración de la app (se conserva entre partidas: el reinicio ocurre cuando la medición ya terminó)
        private static float _restartAt = -1f;                            // el toque en «Sí, empezar de nuevo» (reloj real), hasta que la partida siguiente lo consume
        private static float _restartTap = -1f;                           // el toque que pertenece a la partida que se mide ahora
        private static float _restartConfig = -1f;                        // lo que tardó la escena en recargarse hasta que llegó la configuración
        private static long _launchAtMs;                                  // la hora del toque en «Jugar» (reloj del teléfono: la manda la app, que es otro proceso; 0 = no la mandó)
        private static long _configAtMs;                                  // cuándo llegó la configuración a Unity
        private static long _shownAtMs;                                   // cuándo se mostró el tablero («listo»)
        private static string _note = "";                                 // la última marca de la partida («listo», «¡Ya!», «primer ensayo») y cuándo se puso
        private static float _noteAt;
        private static int _fontRebuilds, _fontRebuildsPrev, _collectionsPrev;     // reconstrucciones de la textura de letras y recolecciones de memoria: lo que, aparte del dibujo, alarga un cuadro
        private static bool _touchedPrev;

        private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private static void OnFontRebuilt(Font f) => _fontRebuilds++;

        /// <summary>La última línea escrita (para las pruebas).</summary>
        public static string LastLine => _lastLine;

        /// <summary>Empieza a medir esta partida (lo llama <c>GameEntryPoint.InitializeGameConfig</c>). Sin bandera de depuración (ni <c>NUBI_PROBE=1</c> en el Editor) apaga todo.</summary>
        public static void Begin(bool debugBuild, string gameId, long launchAtMs = 0L)
        {
            if (Enabled && _shownAt >= 0f && Visible.Count > 0) Finish(true);       // la partida anterior se cortó antes de los 5 s (un reinicio, salir): se escribe lo que haya
            _debug = debugBuild;
            bool env = Application.isEditor && Environment.GetEnvironmentVariable("NUBI_PROBE") == "1";
            Enabled = (debugBuild || env) && Application.isPlaying;
            if (!Enabled)
            {
                Font.textureRebuilt -= OnFontRebuilt;
                if (_driver != null) UnityEngine.Object.Destroy(_driver.gameObject);
                _driver = null;
                return;
            }
            _game = gameId ?? "";
            _launchAtMs = launchAtMs;
            _configAtMs = NowMs();
            _shownAtMs = 0L;
            _note = "";
            _fontRebuilds = _fontRebuildsPrev = 0;
            _collectionsPrev = GC.CollectionCount(0);
            _touchedPrev = false;
            Font.textureRebuilt -= OnFontRebuilt;
            Font.textureRebuilt += OnFontRebuilt;
            _beganAt = Time.realtimeSinceStartup;
            _shownAt = -1f;
            _restartTap = _restartAt;                                        // si esta partida nació de un «Reiniciar»: cuánto tardó la escena en llegar hasta acá
            _restartConfig = _restartAt >= 0f ? _beganAt - _restartAt : -1f;
            _restartAt = -1f;
            Loading.Clear();
            Visible.Clear();
            Hitches.Clear();
            Costs.Clear();
            _costsAtShown = 0.0;
            if (_driver == null)
            {
                var go = new GameObject("EntryProbe");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _driver = go.AddComponent<Driver>();
            }
        }

        /// <summary>
        /// «Reiniciar» desde la pausa (lo llama <c>LaunchIntentConfigReader.RestartCurrentGame</c>): anota la hora del toque. La partida que nace de ese reinicio dice cuánto tardó en recargarse la escena y cuánto pasó hasta su «listo» (el tiempo en que se ve el azul de Unity,
        /// porque el reinicio no tiene pantalla de carga). Solo en depuración.
        /// </summary>
        public static void MarkRestart()
        {
            if (!_debug) return;
            _restartAt = Time.realtimeSinceStartup;
        }

        /// <summary>Una marca de la partida («listo», «¡Ya!», «primer ensayo»): un cuadro largo dice cuál fue la última y cuánto hace. Solo con la bandera de depuración.</summary>
        public static void Note(string what)
        {
            if (!Enabled) return;
            _note = what ?? "";
            _noteAt = Time.realtimeSinceStartup;
        }

        /// <summary>La línea de un cuadro largo (pura, para la prueba): a los cuántos segundos del «listo», cuánto duró, la última marca y qué más pasó en ese cuadro (letras reconstruidas, recolecciones de memoria, un toque).</summary>
        public static string FormatHitch(string game, float sinceShown, float periodMs, string note, float noteAgeSeconds, int fontRebuilds, int collections, bool touched)
        {
            var inv = CultureInfo.InvariantCulture;
            string mark = string.IsNullOrEmpty(note) ? "sin marca" : "última marca «" + note + "» hace " + noteAgeSeconds.ToString("F2", inv) + " s";
            return string.Format(inv, "[sonda-entrada-cuadro] juego={0} a los {1:F2} s de «listo»: cuadro de {2:F0} ms | {3} | letras reconstruidas en ese cuadro: {4} | recolecciones de memoria: {5} | toque: {6}",
                game, sinceShown, periodMs, mark, fontRebuilds, collections, touched ? "sí" : "no");
        }

        /// <summary>El reparto de la espera desde el toque en «Jugar» (pura, para la prueba): hasta la llegada de la configuración a Unity y hasta su «listo». Vacío si la app no mandó la hora del toque.</summary>
        public static string HandoffText(long launchAtMs, long configAtMs, long shownAtMs)
        {
            if (launchAtMs <= 0L) return "";
            var inv = CultureInfo.InvariantCulture;
            return string.Format(inv, " | traspaso: del toque en «Jugar» a la configuración en Unity {0:F2} s y al «listo» {1:F2} s", (configAtMs - launchAtMs) / 1000.0, (shownAtMs - launchAtMs) / 1000.0);
        }

        /// <summary>Suma <paramref name="ms"/> al costo de <paramref name="what"/> («sonido», «sprites») en el hilo principal desde que llegó la configuración.</summary>
        public static void Cost(string what, double ms)
        {
            if (!Enabled) return;
            Costs.TryGetValue(what, out var v);
            Costs[what] = v + ms;
        }

        /// <summary>El tablero se mostró (lo llama <see cref="GameReadiness.Report"/>): termina la carga y empieza la ventana visible.</summary>
        public static void Shown()
        {
            if (!Enabled || _shownAt >= 0f) return;
            _shownAt = Time.realtimeSinceStartup;
            _shownAtMs = NowMs();
            _note = "listo";
            _noteAt = _shownAt;
            _costsAtShown = 0.0;
            foreach (var kv in Costs) _costsAtShown += kv.Value;
        }

        private static void Frame(float periodMs, int fontRebuilds, int collections, bool touched)
        {
            if (_shownAt < 0f)
            {
                Loading.Add(periodMs);
                return;
            }
            float since = Time.realtimeSinceStartup - _shownAt;
            if (since > VisibleWindowSeconds) { Finish(); return; }
            Visible.Add(periodMs);
            if (periodMs > HitchMs && Hitches.Count < 20)
                Hitches.Add(FormatHitch(_game, since, periodMs, _note, Time.realtimeSinceStartup - _noteAt, fontRebuilds, collections, touched));
        }

        private static double Percentile(List<float> sorted, double p)
        {
            if (sorted.Count == 0) return 0.0;
            int i = Math.Min(sorted.Count - 1, (int)Math.Ceiling(p * sorted.Count) - 1);
            return sorted[Math.Max(0, i)];
        }

        private static void Finish(bool partial = false)
        {
            if (!Enabled) return;
            Enabled = false;                                                      // una sola línea por partida
            Font.textureRebuilt -= OnFontRebuilt;
            var vis = new List<float>(Visible);
            vis.Sort();
            var load = new List<float>(Loading);
            load.Sort();
            int over = 0;
            foreach (var v in vis) if (v > HitchMs) over++;
            double loadMax = load.Count == 0 ? 0.0 : load[load.Count - 1];
            double listo = _shownAt - _beganAt;
            double costLoad = _costsAtShown, costAll = 0.0;
            foreach (var kv in Costs) costAll += kv.Value;
            var sb = new StringBuilder();
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "[sonda-entrada] juego={0} listo={1:F2}s | carga: {2} cuadros (máx {3:F0} ms; los tapa la pantalla de carga) | visibles (a 5 s de «listo»): {4} cuadros, mediana {5:F1} ms, p95 {6:F1} ms, máx {7:F1} ms, >{8:F0} ms: {9} | hilo principal: ",
                _game, listo, load.Count, loadMax, vis.Count, Percentile(vis, 0.5), Percentile(vis, 0.95), vis.Count == 0 ? 0.0 : vis[vis.Count - 1], HitchMs, over));
            bool first = true;
            foreach (var kv in Costs)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(kv.Key).Append(' ').Append(kv.Value.ToString("F0", CultureInfo.InvariantCulture)).Append(" ms");
            }
            if (first) sb.Append("sin horneo");
            sb.Append(string.Format(CultureInfo.InvariantCulture, " (de ellos, tras «listo»: {0:F0} ms)", costAll - costLoad));
            sb.Append(string.Format(CultureInfo.InvariantCulture, " | núcleos {0}, hilos de horneo {1}, hilos de sonido {2} | ventana {3:F1} s{4}",
                Environment.ProcessorCount, Background.Workers, WoodCore.WorkerCount, Time.realtimeSinceStartup - _shownAt, partial ? " (corta: se salió antes de los 5 s)" : ""));
            if (_restartTap >= 0f)
                sb.Append(string.Format(CultureInfo.InvariantCulture, " | reinicio: la escena tardó {0:F2} s en llegar y desde el toque hasta «listo» pasaron {1:F2} s (el azul de Unity, sin pantalla de carga)", _restartConfig, _shownAt - _restartTap));
            sb.Append(HandoffText(_launchAtMs, _configAtMs, _shownAtMs));
            _lastLine = sb.ToString();
            Debug.Log(_lastLine);
            NativeBridge.AppendProbe(_lastLine);                                  // al archivo de la app (files/sonda-entrada.txt): el logcat del teléfono se rota en minutos
            foreach (var h in Hitches) { Debug.Log(h); NativeBridge.AppendProbe(h); }
        }

        private sealed class Driver : MonoBehaviour
        {
            private void OnApplicationPause(bool paused)
            {
                if (paused && Enabled && _shownAt >= 0f && Visible.Count > 0) Finish(true);       // salió a segundo plano antes de los 5 s
            }

            private void Update()
            {
                if (!Enabled) return;
                int collections = GC.CollectionCount(0), fonts = _fontRebuilds;
                bool touching = Touching();
                Frame(Time.unscaledDeltaTime * 1000f, fonts - _fontRebuildsPrev, collections - _collectionsPrev, touching || _touchedPrev);   // lo ocurrido desde el cuadro anterior: ese intervalo es el del cuadro que se mide
                _fontRebuildsPrev = fonts;
                _collectionsPrev = collections;
                _touchedPrev = touching;
            }

            private static bool Touching()
            {
                try { return Input.touchCount > 0 || Input.GetMouseButton(0); }
                catch (InvalidOperationException) { return false; }                    // sin el sistema de entrada clásico
            }
        }
    }
}
