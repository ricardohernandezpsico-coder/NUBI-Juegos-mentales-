using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

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

        /// <summary>La última línea escrita (para las pruebas).</summary>
        public static string LastLine => _lastLine;

        /// <summary>Empieza a medir esta partida (lo llama <c>GameEntryPoint.InitializeGameConfig</c>). Sin bandera de depuración (ni <c>NUBI_PROBE=1</c> en el Editor) apaga todo.</summary>
        public static void Begin(bool debugBuild, string gameId)
        {
            bool env = Application.isEditor && Environment.GetEnvironmentVariable("NUBI_PROBE") == "1";
            Enabled = (debugBuild || env) && Application.isPlaying;
            if (!Enabled)
            {
                if (_driver != null) UnityEngine.Object.Destroy(_driver.gameObject);
                _driver = null;
                return;
            }
            _game = gameId ?? "";
            _beganAt = Time.realtimeSinceStartup;
            _shownAt = -1f;
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
            _costsAtShown = 0.0;
            foreach (var kv in Costs) _costsAtShown += kv.Value;
        }

        private static void Frame(float periodMs)
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
                Hitches.Add(string.Format(CultureInfo.InvariantCulture, "[sonda-entrada-cuadro] juego={0} a los {1:F2} s de «listo»: cuadro de {2:F0} ms", _game, since, periodMs));
        }

        private static double Percentile(List<float> sorted, double p)
        {
            if (sorted.Count == 0) return 0.0;
            int i = Math.Min(sorted.Count - 1, (int)Math.Ceiling(p * sorted.Count) - 1);
            return sorted[Math.Max(0, i)];
        }

        private static void Finish()
        {
            if (!Enabled) return;
            Enabled = false;                                                      // una sola línea por partida
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
            _lastLine = sb.ToString();
            Debug.Log(_lastLine);
            foreach (var h in Hitches) Debug.Log(h);
        }

        private sealed class Driver : MonoBehaviour
        {
            private void Update()
            {
                if (!Enabled) return;
                Frame(Time.unscaledDeltaTime * 1000f);
            }
        }
    }
}
