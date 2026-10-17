using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El NÚCLEO COMÚN de «Madera cálida» (Tarea 120, Fase B): guarda los clips de la paleta (<see cref="WoodSounds"/>) que usan TODOS los juegos (acierto, error, nivel, tic, fin, cuenta regresiva, «¡Ya!», pausa, aviso y toque del kit), los calcula UNA vez y de a poco, y los suena por dos altavoces: el del juego (se calla con la pausa) y el de la interfaz
    /// (<c>ignoreListenerPause</c>: el toque de un botón de la pausa y los sonidos de abrir y seguir tienen que oírse con el audio congelado). Lo usa <see cref="GameFeel"/>; los juegos no lo tocan.
    /// <para><b>Cuándo se calcula.</b> <see cref="Warm"/> corre al empezar la partida (<c>GameEntryPoint</c>, cuando llega la configuración): calcula de golpe lo que tiene que sonar DURANTE la cuenta regresiva (<see cref="EarlySet"/>: 3, 2, 1 y «¡Ya!», unos 0,2 s) y deja el resto del núcleo (<see cref="CoreSet"/>) en una cola que un
    /// controlador oculto avanza unos milisegundos por cuadro (<see cref="FrameBudgetMs"/>), mientras corre la cuenta regresiva. Si un sonido del núcleo se pide antes de estar listo NO se calcula en ese momento (sería un tirón en pleno juego): no suena y queda contado en <see cref="Skipped"/>. Unity queda vivo entre partidas y los clips son estáticos:
    /// el núcleo se calcula <b>una sola vez</b> (<see cref="BakeCount"/>) y se conserva; con «Efectos de sonido» apagado no se calcula ni suena nada.</para>
    /// </summary>
    public static class WoodCore
    {
        /// <summary>Lo que tiene que estar listo ANTES de que empiece la cuenta regresiva (suena durante ella): 3, 2, 1 y «¡Ya!».</summary>
        public static readonly (WoodSfx Sfx, int Arg)[] EarlySet = { (WoodSfx.Count, 3), (WoodSfx.Count, 2), (WoodSfx.Count, 1), (WoodSfx.Go, 0) };

        /// <summary>El resto del núcleo común, en el orden en que se calcula: primero lo de la interfaz y el acierto de la primera respuesta.</summary>
        public static readonly (WoodSfx Sfx, int Arg)[] CoreSet =
        {
            (WoodSfx.Tap, 0), (WoodSfx.Correct, 1), (WoodSfx.Wrong, 0), (WoodSfx.Notice, 0), (WoodSfx.PauseIn, 0), (WoodSfx.PauseOut, 0), (WoodSfx.Correct, 2), (WoodSfx.Tick, 0), (WoodSfx.Correct, 3), (WoodSfx.LevelUp, 0),
            (WoodSfx.Correct, 4), (WoodSfx.Finish, 0), (WoodSfx.Correct, 5), (WoodSfx.Correct, 6), (WoodSfx.Correct, 7),
        };

        /// <summary>Cuántos milisegundos por cuadro puede gastar el calculo del núcleo (para que ningún cuadro se trabe).</summary>
        public const float FrameBudgetMs = 5f;

        /// <summary>La racha más alta con su propio escalón (de la 7 en adelante suena igual).</summary>
        public const int MaxCorrect = 7;

        private static readonly Dictionary<int, AudioClip> Clips = new Dictionary<int, AudioClip>();
        private static readonly Queue<(WoodSfx Sfx, int Arg)> Pending = new Queue<(WoodSfx, int)>();
        private static WoodSounds.BakeJob _job;
        private static (WoodSfx Sfx, int Arg) _jobKey;
        private static GameObject _go;
        private static AudioSource _main, _ui;

        // ---- contadores (para las pruebas y el smoke)

        /// <summary>Cuántos clips se calcularon desde que arrancó el proceso (o desde el último <see cref="Reset"/>).</summary>
        public static int BakeCount { get; private set; }

        /// <summary>Cuántos sonidos se mandaron a un altavoz.</summary>
        public static int PlayCount { get; private set; }

        /// <summary>Cuántas veces se pidió un sonido del núcleo y todavía no estaba listo (no suena; no debería pasar si la cuenta regresiva alcanzó).</summary>
        public static int Skipped { get; private set; }

        /// <summary>Cuántos «finales» sonaron (la partida tiene que tener exactamente UNO).</summary>
        public static int FinishPlays { get; private set; }

        /// <summary>El último momento (reloj real) en que sonó algo por el altavoz del juego: lo usa el aviso para no sonar encima de otro sonido (<see cref="NoticeUnlessCovered"/>).</summary>
        public static float LastMainAt { get; private set; } = -100f;

        /// <summary>Para las pruebas: ¿el calculo del núcleo ya terminó (no queda nada en la cola ni a medio hacer)?</summary>
        public static bool Idle => _job == null && Pending.Count == 0;

        /// <summary>Los bytes que ocupan los clips guardados (muestras de 32 bits, todos los canales).</summary>
        public static long StoredBytes()
        {
            long total = 0;
            foreach (var c in Clips.Values) if (c != null) total += (long)c.samples * c.channels * 4;
            return total;
        }

        public static int ClipCount => Clips.Count;

        private static int KeyOf(WoodSfx sfx, int arg) => ((int)sfx << 8) | (arg & 0xFF);

        /// <summary>La variante de un evento que se pide con <paramref name="arg"/>: la racha de 1 a <see cref="MaxCorrect"/>, la cuenta de 1 a 3; los demás tienen una sola.</summary>
        public static int Normalize(WoodSfx sfx, int arg)
        {
            switch (sfx)
            {
                case WoodSfx.Correct: return Mathf.Clamp(arg, 1, MaxCorrect);
                case WoodSfx.Count: return Mathf.Clamp(arg, 1, 3);
                default: return WoodSounds.Variants(sfx).Length > 1 ? arg : 0;
            }
        }

        private static bool Has(WoodSfx sfx, int arg) => Clips.TryGetValue(KeyOf(sfx, arg), out var c) && c != null;

        private static bool InCore(WoodSfx sfx, int arg)
        {
            foreach (var (s, a) in EarlySet) if (s == sfx && a == arg) return true;
            foreach (var (s, a) in CoreSet) if (s == sfx && a == arg) return true;
            return false;
        }

        /// <summary>El clip ya calculado de un evento (null si todavía no).</summary>
        public static AudioClip Get(WoodSfx sfx, int arg = 0) => Clips.TryGetValue(KeyOf(sfx, Normalize(sfx, arg)), out var c) ? c : null;

        // ------------------------------------------------------------------ calcular

        /// <summary>
        /// Prepara el sonido de la partida: calcula de golpe lo de la cuenta regresiva (<see cref="EarlySet"/>), deja el resto del núcleo en la cola y, en el juego, crea el controlador que la avanza un poco por cuadro. Si ya está todo calculado no hace nada (el núcleo se calcula UNA vez).
        /// Con los efectos de sonido apagados no calcula ni crea nada.
        /// </summary>
        public static void Warm()
        {
            if (!GameFeel.SoundOn) return;
            foreach (var (sfx, arg) in EarlySet) if (!Has(sfx, arg)) Bake(sfx, arg);
            foreach (var (sfx, arg) in CoreSet) if (!Has(sfx, arg) && !Queued(sfx, arg)) Pending.Enqueue((sfx, arg));
            if (!Idle) EnsureObject();
        }

        private static bool Queued(WoodSfx sfx, int arg)
        {
            if (_job != null && _jobKey.Sfx == sfx && _jobKey.Arg == arg) return true;
            foreach (var p in Pending) if (p.Sfx == sfx && p.Arg == arg) return true;
            return false;
        }

        private static void Bake(WoodSfx sfx, int arg)
        {
            var job = new WoodSounds.BakeJob(sfx, arg);
            while (!job.Step(int.MaxValue)) { }
            Clips[KeyOf(sfx, arg)] = job.Clip;
            BakeCount++;
        }

        /// <summary>
        /// Avanza el calculo del núcleo hasta <paramref name="budgetMs"/> milisegundos (la síntesis de un clip, un pedazo de su sala o su cierre cada vez). Lo llama el controlador oculto en cada cuadro; las pruebas lo llaman con un presupuesto grande para terminar de una vez.
        /// </summary>
        public static void Step(float budgetMs)
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                if (_job == null)
                {
                    while (Pending.Count > 0 && Has(Pending.Peek().Sfx, Pending.Peek().Arg)) Pending.Dequeue();
                    if (Pending.Count == 0) return;
                    _jobKey = Pending.Dequeue();
                    _job = new WoodSounds.BakeJob(_jobKey.Sfx, _jobKey.Arg);
                }
                if (_job.Step())
                {
                    Clips[KeyOf(_jobKey.Sfx, _jobKey.Arg)] = _job.Clip;
                    BakeCount++;
                    _job = null;
                }
                if (watch.Elapsed.TotalMilliseconds >= budgetMs) return;
            }
        }

        // ------------------------------------------------------------------ sonar

        /// <summary>
        /// Suena un evento. <paramref name="ui"/> true = por el altavoz de la interfaz (se oye aunque el audio esté en pausa: el toque de un botón del menú, abrir y seguir la pausa). Devuelve true si salió un sonido. No suena con los efectos apagados; si el clip del núcleo todavía no está listo, tampoco
        /// (y suma <see cref="Skipped"/>); un evento que no es del núcleo se calcula en el momento (solo pasa en pruebas y en recetas sueltas).
        /// </summary>
        public static bool Play(WoodSfx sfx, int arg = 0, bool ui = false)
        {
            if (!GameFeel.SoundOn) return false;
            arg = Normalize(sfx, arg);
            if (!Has(sfx, arg))
            {
                if (InCore(sfx, arg)) { Skipped++; return false; }
                Bake(sfx, arg);
            }
            var source = Source(ui);
            if (source == null) return false;
            source.PlayOneShot(Clips[KeyOf(sfx, arg)], WoodKit.PlayVolume);
            PlayCount++;
            if (!ui) LastMainAt = Time.realtimeSinceStartup;
            return true;
        }

        /// <summary>
        /// El aviso en pantalla (<see cref="Toast"/>): suena <see cref="WoodSfx.Notice"/> SOLO si desde <paramref name="since"/> (reloj real) no sonó otra cosa por el altavoz del juego: el acierto, el error o «subir de nivel» ya acompañan a casi todos los avisos y no se suman. Lo pide el aviso un cuadro después de
        /// mostrarse (así da lo mismo si el juego llama al sonido antes o después del aviso).
        /// </summary>
        public static bool NoticeUnlessCovered(float since)
        {
            if (LastMainAt >= since - 0.05f) return false;
            return Play(WoodSfx.Notice);
        }

        public static void CountFinish() => FinishPlays++;

        /// <summary>Pone en cero lo que se cuenta por partida (sonidos, omitidos y finales): el smoke lo llama al empezar cada partida. NO suelta los clips (el núcleo se conserva entre partidas).</summary>
        public static void ResetCounters() => PlayCount = Skipped = FinishPlays = 0;

        private static AudioSource Source(bool ui)
        {
            EnsureObject();
            return ui ? _ui : _main;
        }

        private static void EnsureObject()
        {
            if (_go != null && _main != null && _ui != null) return;
            _go = new GameObject("WoodCore");
            _main = _go.AddComponent<AudioSource>();
            _main.playOnAwake = false;
            _main.spatialBlend = 0f;
            _ui = _go.AddComponent<AudioSource>();
            _ui.playOnAwake = false;
            _ui.spatialBlend = 0f;
            _ui.ignoreListenerPause = true;
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(_go);
                _go.AddComponent<WoodCoreDriver>();
            }
        }

        /// <summary>Estado limpio (SOLO para las pruebas): suelta los clips y los contadores y destruye los altavoces.</summary>
        public static void Reset()
        {
            foreach (var c in Clips.Values) if (c != null) UnityEngine.Object.DestroyImmediate(c);
            Clips.Clear();
            Pending.Clear();
            _job = null;
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            _go = null;
            _main = _ui = null;
            BakeCount = PlayCount = Skipped = FinishPlays = 0;
            LastMainAt = -100f;
            GameFeel.ResetTap();
        }
    }
}
