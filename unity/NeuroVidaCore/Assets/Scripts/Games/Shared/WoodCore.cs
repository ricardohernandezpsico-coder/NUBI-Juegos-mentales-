using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El NÚCLEO COMÚN de «Madera cálida» (Tarea 120, Fase B) y los sonidos «EXTRA» de cada juego (paso 0 de las Fases C a E, Tarea 133): guarda los clips de la paleta (<see cref="WoodSounds"/>) que usan TODOS los juegos (acierto, error, nivel, tic, fin, cuenta regresiva, «¡Ya!», pausa, aviso y toque del kit), los calcula UNA vez y de a poco, y los suena por dos altavoces: el del juego (se calla con la pausa) y el de la interfaz
    /// (<c>ignoreListenerPause</c>: el toque de un botón de la pausa y los sonidos de abrir y seguir tienen que oírse con el audio congelado). Lo usa <see cref="GameFeel"/> y, desde las Fases C a E, cada juego para sus notas, sus variantes de la paleta y sus sonidos propios.
    /// <para><b>Cuándo se calcula.</b> <see cref="Warm"/> corre al empezar la partida (<c>GameEntryPoint</c>, cuando llega la configuración): calcula de golpe lo que tiene que sonar DURANTE la cuenta regresiva (<see cref="EarlySet"/>: 3, 2, 1 y «¡Ya!», unos 0,2 s) y deja el resto del núcleo (<see cref="CoreSet"/>) en una cola que un
    /// controlador oculto avanza unos milisegundos por cuadro (<see cref="FrameBudgetMs"/>; 8 ms mientras corre la cuenta regresiva: <see cref="CountdownRunning"/>), mientras corre la cuenta regresiva. Si un sonido del núcleo se pide antes de estar listo NO se calcula en ese momento (sería un tirón en pleno juego): no suena y queda contado en <see cref="Skipped"/>. Unity queda vivo entre partidas y los clips son estáticos:
    /// el núcleo se calcula <b>una sola vez</b> (<see cref="BakeCount"/>) y se conserva; con «Efectos de sonido» apagado no se calcula ni suena nada.</para>
    /// <para><b>Lo extra de un juego.</b> El juego pide en su construcción, ANTES de la cuenta regresiva y en el orden en que suena cada cosa (lo que suena primero, primero): <see cref="Request(WoodSfx,int)"/> (una variante de la paleta fuera del núcleo), <see cref="RequestNote"/> (una nota con información),
    /// <see cref="RequestCue"/> (una señal corta) y <see cref="Request(string,Func{WoodSounds.Recipe},bool)"/> (un sonido PROPIO). Se encolan DETRÁS del núcleo y los hornea el mismo controlador oculto con el mismo presupuesto. Política si NO están listos al sonar: una variante de la paleta o un propio no suena y suma
    /// <see cref="Skipped"/> (nunca un tirón por un adorno); una nota o una señal SÍ se calcula en el momento (son la información del juego: mejor un tirón de unos 10 a 30 ms una vez que el silencio) y suma <see cref="LateBakes"/>. El smoke exige <see cref="Skipped"/> = 0 y <see cref="LateBakes"/> = 0 en toda partida con sonido.
    /// Lo extra es de UN juego a la vez: <see cref="Warm"/> con el id del juego suelta lo del juego anterior (<see cref="ReleaseExtras"/>); el núcleo sigue estático.</para>
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

        /// <summary>Cuántos milisegundos por cuadro puede gastar el calculo del núcleo y de lo extra (para que ningún cuadro se trabe).</summary>
        public const float FrameBudgetMs = 5f;

        /// <summary>El presupuesto mientras corre la cuenta regresiva (la pantalla de la cuenta es liviana): 8 ms por cuadro.</summary>
        public const float CountdownBudgetMs = 8f;

        /// <summary>true mientras corre la cuenta regresiva (lo avisa <c>CountdownScreen</c>): el calculo usa <see cref="CountdownBudgetMs"/>; al empezar la partida vuelve a <see cref="FrameBudgetMs"/>.</summary>
        public static bool CountdownRunning { get; set; }

        /// <summary>El presupuesto por cuadro de ahora.</summary>
        public static float CurrentBudgetMs => CountdownRunning ? CountdownBudgetMs : FrameBudgetMs;

        /// <summary>La racha más alta con su propio escalón (de la 7 en adelante suena igual).</summary>
        public const int MaxCorrect = 7;

        /// <summary>Las únicas ganancias con que se puede sonar algo (dB): así todo sigue parejo y el techo de −6 dBFS no cambia (nunca más fuerte que la receta).</summary>
        public static readonly float[] AllowedGainsDb = { 0f, -3f, -6f, -9f };

        /// <summary>Un trabajo de la cola: un evento del núcleo (<see cref="Make"/> null) o algo extra con su clave y su receta.</summary>
        private sealed class Job
        {
            public WoodSfx Sfx;
            public int Arg;
            public string Key;
            public Func<WoodSounds.Recipe> Make;
            public bool Mono;
            public bool Extra => Make != null;
        }

        private static readonly Dictionary<int, AudioClip> Clips = new Dictionary<int, AudioClip>();             // el núcleo común (estático)
        private static readonly Dictionary<string, AudioClip> Extras = new Dictionary<string, AudioClip>();      // lo extra del juego de ahora (variantes de la paleta, notas, señales y propios)
        private static readonly Queue<Job> Pending = new Queue<Job>();
        private static WoodSounds.BakeJob _job;
        private static Job _jobItem;
        private static string _extrasGame;
        private static GameObject _go;
        private static AudioSource _main, _ui;

        // ---- contadores (para las pruebas y el smoke)

        /// <summary>Cuántos clips se calcularon desde que arrancó el proceso (o desde el último <see cref="Reset"/>).</summary>
        public static int BakeCount { get; private set; }

        /// <summary>Cuántos sonidos se mandaron a un altavoz.</summary>
        public static int PlayCount { get; private set; }

        /// <summary>Cuántas veces se pidió un sonido del núcleo, una variante de la paleta o un propio y todavía no estaba listo (no suena; no debería pasar si la cuenta regresiva alcanzó).</summary>
        public static int Skipped { get; private set; }

        /// <summary>Cuántas notas o señales con información se tuvieron que calcular EN EL MOMENTO de sonar porque el juego no las pidió antes o no alcanzaron a calcularse (suenan igual, con un tirón de unos 10 a 30 ms). El smoke exige 0.</summary>
        public static int LateBakes { get; private set; }

        /// <summary>Cuántos «finales» sonaron (la partida tiene que tener exactamente UNO).</summary>
        public static int FinishPlays { get; private set; }

        /// <summary>El último momento (reloj real) en que sonó algo por el altavoz del juego: lo usa el aviso para no sonar encima de otro sonido (<see cref="NoticeUnlessCovered"/>).</summary>
        public static float LastMainAt { get; private set; } = -100f;

        /// <summary>Para las pruebas: ¿el calculo ya terminó (no queda nada en la cola ni a medio hacer)?</summary>
        public static bool Idle => _job == null && Pending.Count == 0;

        /// <summary>Los bytes que ocupan los clips del núcleo guardados (muestras de 32 bits, todos los canales).</summary>
        public static long StoredBytes()
        {
            long total = 0;
            foreach (var c in Clips.Values) if (c != null) total += (long)c.samples * c.channels * 4;
            return total;
        }

        /// <summary>Los bytes que ocupa lo EXTRA del juego de ahora (techo medido en las pruebas: 10 MB por juego).</summary>
        public static long ExtraBytes()
        {
            long total = 0;
            foreach (var c in Extras.Values) if (c != null) total += (long)c.samples * c.channels * 4;
            return total;
        }

        public static int ClipCount => Clips.Count;

        /// <summary>Cuántos clips extra hay guardados (del juego de ahora).</summary>
        public static int ExtraCount => Extras.Count;

        private static int KeyOf(WoodSfx sfx, int arg) => ((int)sfx << 8) | (arg & 0xFF);

        /// <summary>La variante de un evento que se pide con <paramref name="arg"/>: la racha de 1 a <see cref="MaxCorrect"/>, la cuenta de 1 a 3, la marca de 0 a 9 y el destello de 1 a 8; los demás tienen una sola.</summary>
        public static int Normalize(WoodSfx sfx, int arg)
        {
            switch (sfx)
            {
                case WoodSfx.Correct: return Mathf.Clamp(arg, 1, MaxCorrect);
                case WoodSfx.Count: return Mathf.Clamp(arg, 1, 3);
                case WoodSfx.Select: return Mathf.Clamp(arg, 0, 9);
                case WoodSfx.Spark: return Mathf.Clamp(arg, 1, 8);
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

        /// <summary>El clip ya calculado de un evento (null si todavía no): del núcleo, o de lo extra si el juego lo pidió con <see cref="Request(WoodSfx,int)"/>.</summary>
        public static AudioClip Get(WoodSfx sfx, int arg = 0)
        {
            arg = Normalize(sfx, arg);
            if (Clips.TryGetValue(KeyOf(sfx, arg), out var c)) return c;
            return Extras.TryGetValue(WoodSounds.KeyOf(sfx, arg), out c) ? c : null;
        }

        /// <summary>El clip extra ya calculado con esa clave (null si todavía no).</summary>
        public static AudioClip GetExtra(string key) => Extras.TryGetValue(key, out var c) ? c : null;

        // ------------------------------------------------------------------ claves

        /// <summary>La clave de una nota con información («note.kalimba.392.00.0.30»).</summary>
        public static string NoteKey(double hz, WoodVoice voice, double tau = 0.3) =>
            "note." + voice.ToString().ToLowerInvariant() + "." + hz.ToString("0.00", CultureInfo.InvariantCulture) + "." + tau.ToString("0.00", CultureInfo.InvariantCulture);

        /// <summary>La clave de una señal corta («cue.784.00.0.060»).</summary>
        public static string CueKey(double hz, double seconds) => "cue." + hz.ToString("0.00", CultureInfo.InvariantCulture) + "." + seconds.ToString("0.000", CultureInfo.InvariantCulture);

        // ------------------------------------------------------------------ calcular

        /// <summary>
        /// Prepara el sonido de la partida: calcula de golpe lo de la cuenta regresiva (<see cref="EarlySet"/>), deja el resto del núcleo en la cola y, en el juego, crea el controlador que la avanza un poco por cuadro. Si ya está todo calculado no hace nada (el núcleo se calcula UNA vez).
        /// Con los efectos de sonido apagados no calcula ni crea nada. <paramref name="gameId"/> (el juego que va a correr): si cambió respecto del anterior, suelta lo extra del anterior (<see cref="ReleaseExtras"/>); el mismo juego conserva lo suyo.
        /// </summary>
        public static void Warm(string gameId = null)
        {
            CountdownRunning = false;                                              // una partida nueva: la cuenta regresiva todavía no empezó
            if (gameId != null)
            {
                if (_extrasGame != null && _extrasGame != gameId) ReleaseExtras();
                _extrasGame = gameId;
            }
            if (!GameFeel.SoundOn) return;
            foreach (var (sfx, arg) in EarlySet) if (!Has(sfx, arg)) Bake(sfx, arg);
            foreach (var (sfx, arg) in CoreSet) if (!Has(sfx, arg) && !Queued(sfx, arg)) Pending.Enqueue(new Job { Sfx = sfx, Arg = arg });
            if (!Idle) EnsureObject();
        }

        private static bool Queued(WoodSfx sfx, int arg)
        {
            if (_jobItem != null && !_jobItem.Extra && _jobItem.Sfx == sfx && _jobItem.Arg == arg) return true;
            foreach (var p in Pending) if (!p.Extra && p.Sfx == sfx && p.Arg == arg) return true;
            return false;
        }

        private static bool Queued(string key)
        {
            if (_jobItem != null && _jobItem.Key == key) return true;
            foreach (var p in Pending) if (p.Key == key) return true;
            return false;
        }

        private static void Bake(WoodSfx sfx, int arg)
        {
            var job = new WoodSounds.BakeJob(sfx, arg);
            while (!job.Step(int.MaxValue)) { }
            Clips[KeyOf(sfx, arg)] = job.Clip;
            BakeCount++;
        }

        // ------------------------------------------------------------------ pedir lo extra (ANTES de la cuenta regresiva, en el orden en que suena)

        private static void Enqueue(string key, Func<WoodSounds.Recipe> make, bool mono)
        {
            if (!GameFeel.SoundOn) return;                                         // con el sonido apagado no se calcula nada
            if (Extras.ContainsKey(key) || Queued(key)) return;
            Pending.Enqueue(new Job { Key = key, Make = make, Mono = mono });
            EnsureObject();
        }

        /// <summary>Una variante de la paleta que NO es del núcleo (marca 0 a 9, destello 1 a 8, ronda perfecta, ayuda, nuevo, racha, quitar, soplos, zumbido, golpe…): se calcula de a poco detrás del núcleo. Una del núcleo no hace nada (ya va).</summary>
        public static void Request(WoodSfx sfx, int arg = 0)
        {
            arg = Normalize(sfx, arg);
            if (InCore(sfx, arg)) return;
            Enqueue(WoodSounds.KeyOf(sfx, arg), () => WoodSounds.MakeRecipe(sfx, arg), WoodSounds.IsMono(sfx));
        }

        /// <summary>Una nota con información del juego (la altura de un lucero, un grupo, una escotilla…): <see cref="WoodSounds.MakeNote"/> con su timbre y su caída <paramref name="tau"/>.</summary>
        public static void RequestNote(double hz, WoodVoice voice, double tau = 0.3) =>
            Enqueue(NoteKey(hz, voice, tau), () => WoodSounds.MakeNote(hz, voice, tau), false);

        /// <summary>Una señal corta con altura (el tono de la regla, la señal de salida): <see cref="WoodSounds.MakeCue"/>, que se guarda en MONO (casi sin sala).</summary>
        public static void RequestCue(double hz, double seconds) =>
            Enqueue(CueKey(hz, seconds), () => WoodSounds.MakeCue(hz, seconds), true);

        /// <summary>Un sonido PROPIO del juego («bodega.matraca»): su receta se arma con <see cref="WoodKit"/> y declara su familia; <paramref name="mono"/> en los micro sin sala.</summary>
        public static void Request(string key, Func<WoodSounds.Recipe> make, bool mono = false) => Enqueue(key, make, mono);

        /// <summary>
        /// Suelta (<c>DestroyImmediate</c>) todo lo que no es del núcleo y lo extra que todavía esperaba en la cola; el núcleo conserva sus clips (la misma referencia). <see cref="Warm"/> lo llama cuando cambia el juego: lo extra es de UN juego a la vez (≈ 15 MB residentes en vez de hasta 100).
        /// </summary>
        public static void ReleaseExtras()
        {
            foreach (var c in Extras.Values) if (c != null) UnityEngine.Object.DestroyImmediate(c);
            Extras.Clear();
            if (_jobItem != null && _jobItem.Extra) { _job = null; _jobItem = null; }
            if (Pending.Count > 0)
            {
                var keep = new List<Job>();
                foreach (var p in Pending) if (!p.Extra) keep.Add(p);
                Pending.Clear();
                foreach (var p in keep) Pending.Enqueue(p);
            }
        }

        /// <summary>
        /// Avanza el calculo hasta <paramref name="budgetMs"/> milisegundos (la síntesis de un clip, un pedazo de su sala o su cierre cada vez), primero el núcleo y después lo extra, en el orden en que se pidió. Lo llama el controlador oculto en cada cuadro; las pruebas lo llaman con un presupuesto grande para terminar de una vez.
        /// </summary>
        public static void Step(float budgetMs)
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                if (_job == null)
                {
                    while (Pending.Count > 0 && Done(Pending.Peek())) Pending.Dequeue();
                    if (Pending.Count == 0) return;
                    _jobItem = Pending.Dequeue();
                    _job = _jobItem.Extra ? new WoodSounds.BakeJob(_jobItem.Make, _jobItem.Key, _jobItem.Mono) : new WoodSounds.BakeJob(_jobItem.Sfx, _jobItem.Arg);
                }
                if (_job.Step())
                {
                    if (_jobItem.Extra) Extras[_jobItem.Key] = _job.Clip; else Clips[KeyOf(_jobItem.Sfx, _jobItem.Arg)] = _job.Clip;
                    BakeCount++;
                    _job = null;
                    _jobItem = null;
                }
                if (watch.Elapsed.TotalMilliseconds >= budgetMs) return;
            }
        }

        private static bool Done(Job j) => j.Extra ? Extras.ContainsKey(j.Key) : Has(j.Sfx, j.Arg);

        // ------------------------------------------------------------------ sonar

        /// <summary>Una ganancia permitida (0, −3, −6 o −9 dB) o <see cref="ArgumentException"/>: así todo sigue parejo y nunca suena más fuerte que su receta.</summary>
        private static float Gain(float gainDb)
        {
            foreach (var g in AllowedGainsDb) if (Mathf.Approximately(g, gainDb)) return Mathf.Pow(10f, g / 20f);
            throw new ArgumentException("gainDb solo puede ser 0, -3, -6 o -9 (nunca más fuerte que la receta): " + gainDb, nameof(gainDb));
        }

        /// <summary>
        /// Suena un evento. <paramref name="ui"/> true = por el altavoz de la interfaz (se oye aunque el audio esté en pausa: el toque de un botón del menú, abrir y seguir la pausa). Devuelve true si salió un sonido. No suena con los efectos apagados; si el clip todavía no está listo, tampoco (y suma <see cref="Skipped"/>:
        /// nunca se calcula en pleno juego). <paramref name="gainDb"/>: una atenuación OPCIONAL, solo 0, −3, −6 o −9 dB (otro valor: <see cref="ArgumentException"/>).
        /// </summary>
        public static bool Play(WoodSfx sfx, int arg = 0, bool ui = false, float gainDb = 0f)
        {
            float gain = Gain(gainDb);
            if (!GameFeel.SoundOn) return false;
            arg = Normalize(sfx, arg);
            AudioClip clip;
            if (!Clips.TryGetValue(KeyOf(sfx, arg), out clip) || clip == null)
            {
                if (!Extras.TryGetValue(WoodSounds.KeyOf(sfx, arg), out clip) || clip == null) { Skipped++; return false; }
            }
            return Send(clip, gain, ui);
        }

        /// <summary>Una nota con información. Si el juego no la pidió antes o no alcanzó a calcularse, se calcula en el momento (son la información del juego: la altura que se oye importa más que un tirón de unos 10 a 30 ms) y suma <see cref="LateBakes"/>.</summary>
        public static bool PlayNote(double hz, WoodVoice voice, double tau = 0.3, float gainDb = 0f) =>
            PlayInformation(NoteKey(hz, voice, tau), () => WoodSounds.MakeNote(hz, voice, tau), false, gainDb);

        /// <summary>Una señal corta con altura (como <see cref="PlayNote"/>: se calcula en el momento si falta y suma <see cref="LateBakes"/>).</summary>
        public static bool PlayCue(double hz, double seconds, float gainDb = 0f) =>
            PlayInformation(CueKey(hz, seconds), () => WoodSounds.MakeCue(hz, seconds), true, gainDb);

        private static bool PlayInformation(string key, Func<WoodSounds.Recipe> make, bool mono, float gainDb)
        {
            float gain = Gain(gainDb);
            if (!GameFeel.SoundOn) return false;
            if (!Extras.TryGetValue(key, out var clip) || clip == null)
            {
                var job = new WoodSounds.BakeJob(make, key, mono);
                while (!job.Step(int.MaxValue)) { }
                clip = Extras[key] = job.Clip;
                BakeCount++;
                LateBakes++;
            }
            return Send(clip, gain, false);
        }

        /// <summary>Un sonido PROPIO del juego (pedido con <see cref="Request(string,Func{WoodSounds.Recipe},bool)"/>): si todavía no está listo no suena y suma <see cref="Skipped"/>.</summary>
        public static bool Play(string key, float gainDb = 0f)
        {
            float gain = Gain(gainDb);
            if (!GameFeel.SoundOn) return false;
            if (!Extras.TryGetValue(key, out var clip) || clip == null) { Skipped++; return false; }
            return Send(clip, gain, false);
        }

        private static bool Send(AudioClip clip, float gain, bool ui)
        {
            var source = Source(ui);
            if (source == null) return false;
            source.PlayOneShot(clip, WoodKit.PlayVolume * gain);
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

        /// <summary>Pone en cero lo que se cuenta por partida (sonidos, omitidos, cálculos tardíos y finales): el smoke lo llama al empezar cada partida. NO suelta los clips (el núcleo se conserva entre partidas).</summary>
        public static void ResetCounters() => PlayCount = Skipped = LateBakes = FinishPlays = 0;

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

        /// <summary>Estado limpio (SOLO para las pruebas): suelta los clips (también los extras) y los contadores y destruye los altavoces.</summary>
        public static void Reset()
        {
            foreach (var c in Clips.Values) if (c != null) UnityEngine.Object.DestroyImmediate(c);
            Clips.Clear();
            foreach (var c in Extras.Values) if (c != null) UnityEngine.Object.DestroyImmediate(c);
            Extras.Clear();
            Pending.Clear();
            _job = null;
            _jobItem = null;
            _extrasGame = null;
            CountdownRunning = false;
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            _go = null;
            _main = _ui = null;
            BakeCount = PlayCount = Skipped = LateBakes = FinishPlays = 0;
            LastMainAt = -100f;
            GameFeel.ResetTap();
        }
    }
}
