using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El NÚCLEO COMÚN de «Madera cálida» (Tarea 120, Fase B) y los sonidos «EXTRA» de cada juego (paso 0 de las Fases C a E, Tarea 133): guarda los clips de la paleta (<see cref="WoodSounds"/>) que usan TODOS los juegos (acierto, error, nivel, tic, fin, cuenta regresiva, «¡Ya!», pausa, aviso y toque del kit), los calcula UNA vez y de a poco, y los suena por dos altavoces: el del juego (se calla con la pausa) y el de la interfaz
    /// (<c>ignoreListenerPause</c>: el toque de un botón de la pausa y los sonidos de abrir y seguir tienen que oírse con el audio congelado). Lo usa <see cref="GameFeel"/> y, desde las Fases C a E, cada juego para sus notas, sus variantes de la paleta y sus sonidos propios.
    /// <para><b>Cuándo y dónde se calcula (Tarea 190).</b> <see cref="Warm"/> corre al empezar la partida (<c>GameEntryPoint</c>, cuando llega la configuración) y deja TODO en una cola: primero el «¡Ya!» (<see cref="EarlySet"/>), después el resto del núcleo (<see cref="CoreSet"/>) y, detrás, lo extra que pide el juego. Las MUESTRAS se calculan en HILOS DE TRABAJO
    /// (<see cref="WoodSounds.BakeJob.ComputeSamples"/>: pura aritmética, nada de Unity; hasta <see cref="WorkerCount"/> a la vez) y el hilo principal solo hace lo que Unity exige que haga él: <c>AudioClip.Create</c> y <c>SetData</c>, con un tope de <see cref="FrameBudgetMs"/> (2 ms) por cuadro (el controlador oculto <see cref="WoodCoreDriver"/>), o de
    /// <see cref="LoadingBudgetMs"/> mientras la pantalla de carga de la app tapa el juego (<see cref="Loading"/>). El juego muestra su tablero SOLO cuando <see cref="Ready"/> (nada en la cola, nada calculándose, nada por crear): después, ningún cálculo toca el hilo principal. Si un sonido del núcleo se pide antes de estar listo NO se calcula en ese momento
    /// (sería un tirón en pleno juego): no suena y queda contado en <see cref="Skipped"/>. Unity queda vivo entre partidas y los clips son estáticos: el núcleo se calcula <b>una sola vez</b> (<see cref="BakeCount"/>) y se conserva; con «Efectos de sonido» apagado no se calcula ni suena nada.</para>
    /// <para><b>Lo extra de un juego.</b> El juego pide en su construcción, ANTES de la cuenta regresiva y en el orden en que suena cada cosa (lo que suena primero, primero): <see cref="Request(WoodSfx,int)"/> (una variante de la paleta fuera del núcleo), <see cref="RequestNote"/> (una nota con información),
    /// <see cref="RequestCue"/> (una señal corta) y <see cref="Request(string,Func{WoodSounds.Recipe},bool)"/> (un sonido PROPIO). Se encolan DETRÁS del núcleo y los hornea el mismo controlador oculto con el mismo presupuesto. Política si NO están listos al sonar: una variante de la paleta o un propio no suena y suma
    /// <see cref="Skipped"/> (nunca un tirón por un adorno); una nota o una señal SÍ se calcula en el momento (son la información del juego: mejor un tirón de unos 10 a 30 ms una vez que el silencio) y suma <see cref="LateBakes"/>. El smoke exige <see cref="Skipped"/> = 0 y <see cref="LateBakes"/> = 0 en toda partida con sonido.
    /// Lo extra es de UN juego a la vez: <see cref="Warm"/> con el id del juego suelta lo del juego anterior (<see cref="ReleaseExtras"/>); el núcleo sigue estático.</para>
    /// </summary>
    public static class WoodCore
    {
        /// <summary>Lo que tiene que estar listo ANTES de que se vea el tablero: el «¡Ya!» (Tarea 190: la cuenta regresiva 3-2-1 se quitó, así que ya no se calculan los tres números; <see cref="WoodSfx.Count"/> sigue en la paleta pero nadie la suena).</summary>
        public static readonly (WoodSfx Sfx, int Arg)[] EarlySet = { (WoodSfx.Go, 0) };

        /// <summary>El resto del núcleo común, en el orden en que se calcula: primero lo de la interfaz y el acierto de la primera respuesta.</summary>
        public static readonly (WoodSfx Sfx, int Arg)[] CoreSet =
        {
            (WoodSfx.Tap, 0), (WoodSfx.Correct, 1), (WoodSfx.Wrong, 0), (WoodSfx.Notice, 0), (WoodSfx.PauseIn, 0), (WoodSfx.PauseOut, 0), (WoodSfx.Correct, 2), (WoodSfx.Tick, 0), (WoodSfx.Correct, 3), (WoodSfx.LevelUp, 0),
            (WoodSfx.Correct, 4), (WoodSfx.Finish, 0), (WoodSfx.Correct, 5), (WoodSfx.Correct, 6), (WoodSfx.Correct, 7),
        };

        /// <summary>Cuántos milisegundos por cuadro puede gastar el HILO PRINCIPAL en crear los clips que los hilos de trabajo ya calcularon (<c>AudioClip.Create</c> y <c>SetData</c>; Tarea 190: antes eran 5 ms de síntesis).</summary>
        public const float FrameBudgetMs = 2f;

        /// <summary>El presupuesto mientras la pantalla de carga de la app tapa el juego (Tarea 190): nadie ve los cuadros, así que el hilo principal puede crear clips durante más tiempo.</summary>
        public const float LoadingBudgetMs = 30f;

        /// <summary>true mientras el juego espera a mostrar su tablero (<c>GameControllerBase.ShowBoardWhenReady</c>): el presupuesto es <see cref="LoadingBudgetMs"/>.</summary>
        public static bool Loading { get; set; }

        /// <summary>El presupuesto por cuadro de ahora.</summary>
        public static float CurrentBudgetMs => Loading ? LoadingBudgetMs : FrameBudgetMs;

        /// <summary>Cuántos hilos de trabajo calculan muestras a la vez (uno menos que los núcleos del teléfono, entre 1 y 4: el hilo principal y el de Unity siguen libres).</summary>
        public static int WorkerCount => Mathf.Clamp(SystemInfo.processorCount - 1, 1, 4);

        /// <summary>El juego puede mostrar su tablero: no queda sonido por calcular ni por crear (con «Efectos de sonido» apagado, siempre).</summary>
        public static bool Ready => Idle;

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

        /// <summary>Un clip que un hilo de trabajo está calculando (o ya calculó y espera su <c>AudioClip.Create</c> en el hilo principal).</summary>
        private sealed class Work
        {
            public Job Item;
            public WoodSounds.BakeJob Bake;
            public int Generation;
            public volatile bool Cancelled;       // lo extra de un juego que se soltó mientras se calculaba: el resultado se descarta
            public float[] Left, Right;
            public Exception Error;
        }

        private static readonly ConcurrentQueue<Work> Finished = new ConcurrentQueue<Work>();     // lo que los hilos terminaron, para crear el clip en el principal
        private static readonly List<Work> InFlight = new List<Work>();                          // lo que se mandó a calcular y todavía no tiene su clip (solo el hilo principal lo toca)
        private static int _generation;                                                          // sube con Reset: los resultados de una generación anterior se descartan
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

        /// <summary>¿Ya terminó todo (nada en la cola, nada calculándose en un hilo, nada esperando su clip)?</summary>
        public static bool Idle => Pending.Count == 0 && InFlight.Count == 0;

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
        /// Prepara el sonido de la partida: deja en la cola el «¡Ya!» (<see cref="EarlySet"/>) y, detrás, el resto del núcleo (<see cref="CoreSet"/>), y en el juego crea el controlador que reparte el trabajo a los hilos y crea los clips (<see cref="WoodCoreDriver"/>). NO calcula nada en el hilo principal (Tarea 190: antes calculaba de golpe el «¡Ya!» y los tres números, unos 125 a 160 ms en el primer cuadro).
        /// Si ya está todo calculado no hace nada (el núcleo se calcula UNA vez). Con los efectos de sonido apagados no calcula ni crea nada. <paramref name="gameId"/> (el juego que va a correr): si cambió respecto del anterior, suelta lo extra del anterior (<see cref="ReleaseExtras"/>); el mismo juego conserva lo suyo.
        /// </summary>
        public static void Warm(string gameId = null)
        {
            Loading = false;                                                       // una partida nueva: todavía no espera a mostrar su tablero
            if (gameId != null)
            {
                if (_extrasGame != null && _extrasGame != gameId) ReleaseExtras();
                _extrasGame = gameId;
            }
            if (!GameFeel.SoundOn) return;
            SoundKit.PrepareForThreads();                                          // el filtro de media frecuencia se arma acá, una vez, en el hilo principal (los hilos de trabajo solo lo leen)
            foreach (var (sfx, arg) in EarlySet) if (!Has(sfx, arg) && !Queued(sfx, arg)) Pending.Enqueue(new Job { Sfx = sfx, Arg = arg });
            foreach (var (sfx, arg) in CoreSet) if (!Has(sfx, arg) && !Queued(sfx, arg)) Pending.Enqueue(new Job { Sfx = sfx, Arg = arg });
            if (!Idle) EnsureObject();
        }

        private static bool Queued(WoodSfx sfx, int arg)
        {
            foreach (var w in InFlight) if (!w.Item.Extra && w.Item.Sfx == sfx && w.Item.Arg == arg) return true;
            foreach (var p in Pending) if (!p.Extra && p.Sfx == sfx && p.Arg == arg) return true;
            return false;
        }

        private static bool Queued(string key)
        {
            foreach (var w in InFlight) if (!w.Cancelled && w.Item.Key == key) return true;
            foreach (var p in Pending) if (p.Key == key) return true;
            return false;
        }

        // ------------------------------------------------------------------ pedir lo extra (ANTES de la cuenta regresiva, en el orden en que suena)

        private static void Enqueue(string key, Func<WoodSounds.Recipe> make, bool mono)
        {
            if (!GameFeel.SoundOn) return;                                         // con el sonido apagado no se calcula nada
            if (Extras.TryGetValue(key, out var have))
            {
                if (have != null) return;
                Extras.Remove(key);                                                // un clip que Unity destruyó (en el editor, al salir de Play con la recarga de dominio apagada: la cadena del smoke): se vuelve a calcular, no se da por presente
            }
            if (Queued(key)) return;
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
            foreach (var w in InFlight) if (w.Item.Extra) w.Cancelled = true;      // lo que un hilo estaba calculando de ese juego se descarta al terminar
            if (Pending.Count > 0)
            {
                var keep = new List<Job>();
                foreach (var p in Pending) if (!p.Extra) keep.Add(p);
                Pending.Clear();
                foreach (var p in keep) Pending.Enqueue(p);
            }
        }

        /// <summary>
        /// Lo que hace el HILO PRINCIPAL en cada cuadro (Tarea 190): manda a los hilos de trabajo los clips de la cola (hasta <see cref="WorkerCount"/> a la vez, primero el núcleo y después lo extra, en el orden en que se pidió) y crea (<c>AudioClip.Create</c> y <c>SetData</c>) los que ya terminaron,
        /// durante a lo más <paramref name="budgetMs"/> milisegundos (siempre crea al menos uno si hay). Lo llama el controlador oculto en cada cuadro; las pruebas lo llaman con un presupuesto grande para terminar (<see cref="Drain"/>).
        /// </summary>
        public static void Step(float budgetMs)
        {
            Dispatch();
            var watch = Stopwatch.StartNew();
            while (Finished.TryDequeue(out var w))
            {
                Realize(w);
                Dispatch();
                if (watch.Elapsed.TotalMilliseconds >= budgetMs) break;
            }
        }

        /// <summary>Para las pruebas: espera (con el hilo principal) a que TODO termine de calcularse y se cree. Devuelve cuántas veces tuvo que repartir y crear.</summary>
        public static int Drain(int timeoutMs = 120000)
        {
            var watch = Stopwatch.StartNew();
            int rounds = 0;
            while (!Idle)
            {
                Step(1000f);
                rounds++;
                if (!Idle) Thread.Sleep(1);
                if (watch.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("WoodCore no terminó de calcular en " + timeoutMs + " ms");
            }
            return rounds;
        }

        private static void Dispatch()
        {
            int workers = WorkerCount;
            while (InFlight.Count < workers && Pending.Count > 0)
            {
                var item = Pending.Dequeue();
                if (Done(item) || StillInFlight(item)) continue;
                var work = new Work { Item = item, Generation = _generation };
                work.Bake = item.Extra ? new WoodSounds.BakeJob(item.Make, item.Key, item.Mono) : new WoodSounds.BakeJob(item.Sfx, item.Arg);
                InFlight.Add(work);
                ThreadPool.QueueUserWorkItem(Compute, work);
            }
        }

        private static bool StillInFlight(Job item)
        {
            foreach (var w in InFlight)
            {
                if (w.Cancelled) continue;
                if (item.Extra ? (w.Item.Extra && w.Item.Key == item.Key) : (!w.Item.Extra && w.Item.Sfx == item.Sfx && w.Item.Arg == item.Arg)) return true;
            }
            return false;
        }

        /// <summary>El trabajo de un hilo de trabajo: SOLO aritmética (ninguna API de Unity). Un error no se pierde: se guarda y el hilo principal lo informa.</summary>
        private static void Compute(object state)
        {
            var work = (Work)state;
            try
            {
                if (!work.Cancelled && work.Generation == _generation) work.Bake.ComputeSamples(out work.Left, out work.Right);
            }
            catch (Exception e)
            {
                work.Error = e;
            }
            Finished.Enqueue(work);
        }

        /// <summary>El hilo principal crea el clip de lo que un hilo terminó y lo guarda (o lo descarta si era de una generación anterior o de un juego que se soltó).</summary>
        private static void Realize(Work w)
        {
            InFlight.Remove(w);
            if (w.Generation != _generation || w.Cancelled) return;
            if (w.Error != null)
            {
                UnityEngine.Debug.LogError("[WoodCore] no se pudo calcular " + w.Bake.ClipName + ": " + w.Error);
                return;
            }
            var watch = Stopwatch.StartNew();
            var clip = WoodSounds.MakeClip(w.Bake.ClipName, w.Left, w.Right);
            if (w.Item.Extra) Extras[w.Item.Key] = clip; else Clips[KeyOf(w.Item.Sfx, w.Item.Arg)] = clip;
            BakeCount++;
            EntryProbe.Cost("sonido", watch.Elapsed.TotalMilliseconds);
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
            _generation++;                                                         // lo que los hilos todavía calculan se descarta al terminar
            foreach (var w in InFlight) w.Cancelled = true;
            InFlight.Clear();
            _extrasGame = null;
            Loading = false;
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            _go = null;
            _main = _ui = null;
            BakeCount = PlayCount = Skipped = LateBakes = FinishPlays = 0;
            LastMainAt = -100f;
            GameFeel.ResetTap();
        }
    }
}
