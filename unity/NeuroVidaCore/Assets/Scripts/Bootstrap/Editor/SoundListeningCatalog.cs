using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using NeuroVida.Games.Acoplamiento;
using NeuroVida.Games.Anagramas;
using NeuroVida.Games.Aterrizaje;
using NeuroVida.Games.Bodega;
using NeuroVida.Games.Calculo;
using NeuroVida.Games.Correo;
using NeuroVida.Games.Disparate;
using NeuroVida.Games.Engranajes;
using NeuroVida.Games.Intrusa;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.Piloto;
using NeuroVida.Games.Radar;
using NeuroVida.Games.Satelites;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// LOS EVENTOS de la página de escucha (Tarea 119): una fila por cada sonido que hoy suena en un juego o en lo común (qué pasa, cómo suena hoy, dónde se llama: archivo:línea al 17-oct, a qué sonido de la paleta de <see cref="WoodSounds"/> se mapea y si lleva información del juego).
    /// Los sonidos de «hoy» salen de las MISMAS clases que suenan en el juego; los de Madera, de <see cref="WoodSounds"/>. Banderas: <c>info</c> = la altura, el orden o el momento de la nota es información del juego; <c>señal</c> = es parte del estímulo o de la instrucción; <c>tiempo</c> = va sincronizado con algo (no puede crecer su latencia);
    /// <c>bucle</c> = suena en bucle debajo del juego.
    /// </summary>
    public static partial class SoundListeningExporter
    {
        private const string Common = "comun";

        /// <summary>Las familias de notas con información (para medir qué tan afinada queda cada una al cambiar de timbre).</summary>
        internal sealed class Family
        {
            public string Id, Title;
            public double[] Hz;
            public Func<int, Clip> Hoy;
            public readonly string[] WoodKeys = new string[2];
        }

        internal static readonly List<Family> Families = new List<Family>();

        private static void RegisterAll()
        {
            Families.Clear();
            RegisterCommon();
            RegisterPalette();
            RegisterRastro();
            RegisterParejas();
            RegisterPista();
            RegisterBodega();
            RegisterCorreo();
            RegisterStroop();
            RegisterPiloto();
            RegisterFreno();
            RegisterSatelites();
            RegisterRadar();
            RegisterCarga();
            RegisterEngranajes();
            RegisterAcoplamiento();
            RegisterAterrizaje();
            RegisterPunta();
            RegisterMeteoros();
            RegisterDisparate();
            RegisterIntrusa();
            RegisterSignalFamily();
        }

        // ------------------------------------------------------------------ ayudas

        private static Ev E(string group, string id, string title, string today, string sites, string hoy, float hoyGain, string wood, string map, string flag = "", string note = "")
        {
            var e = new Ev { Group = group, Id = group + "." + id, Title = title, Today = today, Sites = sites, HoyKey = hoy, HoyGain = hoyGain, WoodKey = wood, Map = map, Flag = flag, Note = note, Phase = PhaseOf(group) };
            Events.Add(e);
            return e;
        }

        /// <summary>La fase del plan en que se migra cada juego (spec `auditoria-fable/spec-sonido-CDE.md` §7): «B» lo común (ya suena), «C» Carga, Punta, Constelaciones, Bodega y Correo; «D» Rastro, Satélites, Piloto e Intrusa; «E» Tinta, Meteoros, Freno, Disparate y Engranajes. Rescate, Acoplamiento y Aterrizaje ya están en Madera: sin fase.</summary>
        internal static string PhaseOf(string group)
        {
            switch (group)
            {
                case Common: return "B";
                case "calculo": case "anagramas": case "parejas": case "bodega": case "correo": return "C";
                case "secuencia": case "satelites": case "piloto": case "intrusa": return "D";
                case "stroop": case "meteoros": case "freno": case "disparate": case "engranajes": return "E";
                case "pista": return "P";                                    // la mini página de la pista de Constelaciones (Tarea 135): tres opciones para que Ricardo elija de oído
                default: return "";
            }
        }

        /// <summary>El volumen con que suena la Madera de un evento que el juego atenúa (−3, −6 o −9 dB): así el botón suena como va a sonar.</summary>
        private static float Atten(double db) => WoodSounds.PlayVolume * (float)Math.Pow(10.0, db / 20.0);

        /// <summary>Un sonido que se mezcla con otro (la fanfarria y la bocina a la vez): cada uno con su volumen de hoy, sumados en un solo archivo.</summary>
        private static Clip Mix(params (Clip Clip, double Gain)[] parts)
        {
            var clips = new List<Clip>();
            var starts = new List<double>();
            foreach (var (clip, gain) in parts) { clips.Add(Math.Abs(gain - 1.0) < 1e-9 ? clip : Scaled(clip, gain)); starts.Add(0.0); }
            return Sequence(clips, starts);
        }

        /// <summary>Un evento de un juego que YA suena en Madera cálida (Rescate, Acoplamiento, Aterrizaje): lo de hoy ES Madera.</summary>
        private static Ev EW(string group, string id, string title, string today, string sites, string hoy, float hoyGain, string map, string flag = "", string note = "")
        {
            var e = E(group, id, title, today, sites, hoy, hoyGain, null, map, flag, note);
            e.AlreadyWood = true;
            return e;
        }

        /// <summary>La clave de un sonido de la paleta (lo registra si falta).</summary>
        private static string Wk(WoodSfx sfx, int arg = 0)
        {
            string k = "wood." + WoodSounds.KeyOf(sfx, arg);
            if (!Factories.ContainsKey(k)) W(k, sfx, arg);
            return k;
        }

        /// <summary>Una escalera de variantes de la paleta (la racha 1 a 8, la cuenta 3-2-1…) en un solo archivo, una cada <paramref name="spacing"/> s.</summary>
        private static string WoodLadder(WoodSfx sfx, int[] args, double spacing)
        {
            string k = "wood.ladder." + sfx.ToString().ToLowerInvariant();
            int count = Math.Min(args.Length, LadderSteps);
            if (!Factories.ContainsKey(k)) Raw(k, () => Scale(count, spacing, i => Trimmed(FromWood("", sfx, args[i]), 1.0)));
            return k;
        }

        /// <summary>Cuántos escalones se oyen de una escalera de la paleta o de hoy, y cuántas notas de una escala de una familia (en la página se oyen unos pocos para que pese poco; las medidas son de todos).</summary>
        private const int LadderSteps = 5, ScaleNotes = 5;
        private const double ScaleSpacing = 0.35, NoteSeconds = 0.8;

        /// <summary>Lo común de HOY (antes de la Fase B): las recetas congeladas de <c>GameFeel</c> (ver <see cref="GameFeelLegacy"/>); desde la Fase B el juego suena la paleta.</summary>
        private static float[] GameFeelPentatonic() => GameFeelLegacy.Pentatonic;

        /// <summary>Una familia de notas con información: la escala de hoy, la de Madera con kalimba y la de Madera con marimba (las mismas alturas, en el mismo orden y espaciado). Con <paramref name="hoy"/> = null (los juegos ya cableados) la escala de hoy sale congelada del 17-oct.</summary>
        private static void Family_(string id, string title, double[] hz, Func<int, AudioClip> hoy, double spacing = 0.42, double tau = 0.3)
        {
            var fam = new Family { Id = id, Title = title, Hz = hz, Hoy = hoy == null ? null : (Func<int, Clip>)(i => FromClip(hoy(i))) };
            Families.Add(fam);
            int n = Math.Min(hz.Length, ScaleNotes);
            if (hoy == null) Frozen("fam." + id + ".hoy");              // el juego ya suena Madera y su clase de campanas se borró: «hoy» sale CONGELADO del 17-oct (el archivo y las medidas de cada nota)
            else Raw("fam." + id + ".hoy", () => Scale(n, ScaleSpacing, i => Trimmed(fam.Hoy(i), NoteSeconds)));
            // la escala de Madera depende solo de las alturas: las familias con las mismas notas comparten el archivo
            string sig = "";
            for (int i = 0; i < n; i++) sig += (i > 0 ? "-" : "") + Math.Round(hz[i]);
            foreach (var voice in new[] { WoodVoice.Kalimba, WoodVoice.Marimba })
            {
                var v = voice;
                string key = "fam.madera." + v.ToString().ToLowerInvariant() + (Math.Abs(tau - 0.3) > 1e-9 ? ".tau" + tau.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : "") + "." + sig;      // otra caída = otro archivo
                if (!Factories.ContainsKey(key)) Raw(key, () => Scale(n, ScaleSpacing, i => Trimmed(FromRecipe(WoodSounds.MakeNote(hz[i], v, tau)), NoteSeconds)));
                fam.WoodKeys[v == WoodVoice.Kalimba ? 0 : 1] = key;
            }
        }

        private static Ev FamilyEvent(string group, string id, string title, string today, string sites, string famId, float gain, string map, string note = "", bool marimba = false)
        {
            Family fam = Families.Find(f => f.Id == famId);
            // «Madera» es la voz del juego (kalimba en la Fase C, marimba en la D) y el tercer botón, la otra para comparar
            var e = E(group, id, title, today, sites, "fam." + famId + ".hoy", gain, fam.WoodKeys[marimba ? 1 : 0], map, "info", note);
            e.WoodAltKey = fam.WoodKeys[marimba ? 0 : 1];
            if (marimba) e.WoodAltLabel = "Madera (kalimba)";
            return e;
        }

        private static double[] Hz(Func<int, double> f, int count)
        {
            var r = new double[count];
            for (int i = 0; i < count; i++) r[i] = f(i);
            return r;
        }

        // ------------------------------------------------------------------ lo común

        private static void RegisterCommon()
        {
            var pent = GameFeelPentatonic();
            Raw("gamefeel.racha", () => Scale(LadderSteps, 0.55, i => Trimmed(FromClip(GameFeelLegacy.Marimba(pent[Math.Min(i, pent.Length - 1)], 0.26f)), 1.0)));
            H("gamefeel.wrong", () => GameFeelLegacy.Wrong());
            H("gamefeel.levelup", () => GameFeelLegacy.LevelUp());
            H("gamefeel.tick", () => GameFeelLegacy.Tick());
            H("gamefeel.finish", () => GameFeelLegacy.Finish());

            const string g = Common;
            E(g, "acierto", "Acertar (la nota sube con la racha: 1 a 8)", "GameFeel: «pling» de marimba (seno + parcial ×4, τ 0,11 s) por la pentatónica do5 a mi6, mono, sin sala; pico ≈ 0,5", "GameFeel.cs:41 Correct(racha). Lo llaman Tinta o Palabra (StroopGameController.cs:287), Freno (BrakeGameController.cs:313, :358), Lluvia de meteoros (MeteorGameController.cs:492), ¿Verdad o disparate? (DisparateGameController.cs:586) y las prácticas de Aterrizaje (LandingGameController.Guided.cs:51), Meteoros (:1231) y Tinta (:1162)",
                "gamefeel.racha", 0.55f, WoodLadder(WoodSfx.Correct, WoodSounds.Variants(WoodSfx.Correct), 0.7f), "Correct(racha): kalimba de 2 notas que suben");
            E(g, "error", "Equivocarse", "GameFeel: «bu-bum» de seno grave (220 y 185 Hz con tercer armónico), mono, sin sala; vibración doble", "GameFeel.cs:49 Wrong(). Tinta (StroopGameController.cs:307), Freno (BrakeGameController.cs:331, :379), Meteoros (MeteorGameController.cs:499), Disparate (DisparateGameController.cs:621) y las prácticas de Aterrizaje (.Guided.cs:57) y Meteoros (:1239)",
                "gamefeel.wrong", 0.5f, Wk(WoodSfx.Wrong), "Wrong: dos marimbas graves que bajan (la de Rescate)");
            E(g, "nivel", "Subir de nivel", "GameFeel: arpegio de marimba do5-mi5-sol5-do6 cada 65 ms, mono, sin sala; vibración firme", "GameFeel.cs:56 LevelUp(). Tinta (StroopGameController.cs:212), Freno (BrakeGameController.cs:288, :323, :368), Meteoros (MeteorGameController.cs:187), Disparate (:292 «¡Ráfaga!», :794), En la punta de la lengua (PuntaGameController.cs:351), La estrella intrusa (IntrusaGameController.cs:1055)",
                "gamefeel.levelup", 0.5f, Wk(WoodSfx.LevelUp), "LevelUp: el mismo arpegio, de kalimba");
            E(g, "tic", "Tic de los últimos 5 segundos", "GameFeel: golpecito de madera (1250 Hz + 2500 Hz, τ 12 ms), mono, sin sala; un tic por segundo", "GameFeel.cs:63 Tick(). Acoplamiento (DockingGameController.cs:476), Punta (:1041), Aterrizaje (:473), Bodega (:828), Carga exacta (:788), Engranajes (:635), Freno (:421), Intrusa (:1171), Meteoros (:466), Constelaciones (:539), Rescate (RadarGameController.cs:469), Satélites (:433), Rastro (:717), Tinta (:364), Disparate (:927)",
                "gamefeel.tick", 0.45f, Wk(WoodSfx.Tick), "Tick: bloque de madera");
            E(g, "fin", "Fin de la partida («¡Listo!»)", "GameFeel: acorde de campana do5-mi5-sol5-do6 (τ 0,42 s) con brillo a 2093 Hz, 1,1 s, mono, sin sala; vibración firme", "ExitButton.cs:87 (GameFeel.Finish) al empezar el cierre, en los 17 juegos",
                "gamefeel.finish", 0.55f, Wk(WoodSfx.Finish), "Finish: el final de Rescate (marimba y kalimba larga)", "",
                "HOY SUENAN DOS FINALES SEGUIDOS: el Finale propio de cada juego (PlayClip al terminar la partida) y este, que agrega ExitButton al abrir el cierre. En la Fase B queda UNO.");
            E(g, "cuenta", "Cuenta regresiva 3 · 2 · 1", "Silencio (solo se ven los números)", "CountdownScreen.cs:186 Play(), en los 17 juegos", null, 1f, WoodLadder(WoodSfx.Count, WoodSounds.Variants(WoodSfx.Count), 0.7f), "Count(3), Count(2), Count(1): marimba do, re, mi", "", "NUEVO: hoy no suena nada.");
            E(g, "ya", "«¡Ya!»", "Silencio", "CountdownScreen.cs:321 y :447 (con «quitar animaciones»)", null, 1f, Wk(WoodSfx.Go), "Go: dos marimbas y una kalimba aguda", "", "NUEVO: hoy no suena nada.");
            E(g, "pausa-abrir", "Abrir la pausa", "Silencio (el audio se congela con AudioListener.pause)", "PauseMenu.cs:187 Show()", null, 1f, Wk(WoodSfx.PauseIn), "PauseIn: dos marimbas suaves que bajan", "", "NUEVO. Ojo: la pausa congela el audio, así que este sonido tiene que sonar ANTES de congelarlo (o con ignoreListenerPause).");
            E(g, "pausa-seguir", "Seguir jugando", "Silencio", "PauseMenu.cs (botón «Seguir»)", null, 1f, Wk(WoodSfx.PauseOut), "PauseOut: las mismas dos, que suben", "", "NUEVO.");
            E(g, "aviso", "Aviso en pantalla (Toast, «¡Ráfaga!», «Nivel 2»…)", "Silencio (algunos juegos suman su propio sonido)", "Toast.cs:178 Show()", null, 1f, Wk(WoodSfx.Notice), "Notice: una marimba sola", "", "NUEVO.");
            E(g, "boton", "Botón o ficha presionada (kit de plasticina)", "Silencio en el kit; cada juego pone el suyo (campanita, siseo, golpe…)", "PlasticTouch.cs:86 Press(); PlasticSlab.cs:214 PlasticPress (pointer down)", null, 1f, Wk(WoodSfx.Tap), "Tap: marimba apagada de 60 ms (la de Acoplamiento)", "", "NUEVO en el kit; aprobado ya en Acoplamiento. Ricardo notó que los toques cortos y más fuertes «no se entendían»: queda el más suave.");
        }

        // ------------------------------------------------------------------ eventos nuevos de la paleta (hoy cada juego tiene el suyo)

        private static void RegisterPalette()
        {
            const string g = Common;
            E(g, "p-elegir", "Elegir o marcar (la nota sube con la cantidad)", "Cada juego el suyo: campanitas (Carga exacta, Satélites, Engranajes), kalimba (Rescate)", "ver cada juego", null, 1f, WoodLadder(WoodSfx.Select, WoodSounds.Variants(WoodSfx.Select), 0.55f), "Select(k): kalimba do5, re5, mi5… (la marca de Rescate)");
            E(g, "p-quitar", "Quitar o deshacer", "Cada juego el suyo: soplo que baja, golpecito, campanita apagada", "ver cada juego", null, 1f, Wk(WoodSfx.Unselect), "Unselect: kalimba apagada (la de Rescate)");
            E(g, "p-nosepuede", "«Eso no se puede» / sin efecto", "Cada juego el suyo: golpe sordo, tono grave suave, madera sorda", "ver cada juego", null, 1f, Wk(WoodSfx.Knock), "Knock: UN toc de madera grave y hueco");
            E(g, "p-ayuda", "Ayuda de Nubi", "Cada juego el suyo: dos campanas o soplo y campana", "ver cada juego", null, 1f, Wk(WoodSfx.Hint), "Hint: soplo que sube y kalimba grave");
            E(g, "p-nuevo", "«NUEVO», sorpresa, algo que se desbloquea", "Dos campanas (mi y la) copiadas en 7 juegos", "ver cada juego", null, 1f, Wk(WoodSfx.New), "New: dos kalimbas agudas que suben");
            E(g, "p-chispa", "Destello de racha (sube con cada acierto seguido)", "Una campanita aguda por juego (Bodega, Constelaciones, Correo, Satélites)", "ver cada juego", null, 1f, WoodLadder(WoodSfx.Spark, WoodSounds.Variants(WoodSfx.Spark), 0.55f), "Spark(n): kalimba aguda que sube");
            E(g, "p-hito", "Hito de racha (cada 5, 10, 20…)", "Correo: tres campanas por escalón (×10, ×20, ×30); los demás, nada", "MailGameController.Scene.cs:183", null, 1f, Wk(WoodSfx.Streak), "Streak: arpegio corto de kalimba");
            E(g, "p-perfecta", "Ronda perfecta / pedido perfecto / diana", "Cada juego el suyo: arpegio de 4 campanas o do-mi-sol-do", "ver cada juego", null, 1f, Wk(WoodSfx.Perfect), "Perfect: la de Rescate (marimba do-mi-sol-do y dos kalimbas)");
            E(g, "p-record", "Récord personal", "Silencio (solo se ve)", "—", null, 1f, Wk(WoodSfx.Record), "Record: escalera de kalimba y acorde de marimba", "", "NUEVO.");
            E(g, "p-soplo-sube", "Textura: soplo que sube (algo aparece o se abre)", "Cada juego el suyo: barridos de ruido", "ver cada juego", null, 1f, Wk(WoodSfx.SweepUp), "SweepUp: ruido de banda 500 → 2200 Hz");
            E(g, "p-soplo-baja", "Textura: soplo que baja (algo se suelta o vuelve)", "Cada juego el suyo: barridos de ruido", "ver cada juego", null, 1f, Wk(WoodSfx.SweepDown), "SweepDown: el de Aterrizaje (paso bajo 1400 → 300 Hz)");
            E(g, "p-zumbido", "Textura: zumbido que arranca", "Bodega, Correo, Constelaciones, Engranajes: senos o triángulos graves", "ver cada juego", null, 1f, Wk(WoodSfx.Hum), "Hum: dos marimbas graves (196 y 294 Hz)");
            E(g, "p-golpe", "Textura: golpe seco de un objeto", "Satélites «clac», Bodega y Engranajes «clank»", "ver cada juego", null, 1f, Wk(WoodSfx.Clack), "Clack: el golpe de Aterrizaje al posarse");
        }

        // ------------------------------------------------------------------ Rastro de luz

        private static void RegisterRastro()
        {
            const string g = "secuencia";
            // «hoy» sale CONGELADO del 17-oct (Tarea 138): la clase de campanas de Rastro se borró al cablear el juego
            Family_("rastro", "Rastro de luz: las 9 notas de los luceros", Hz(i => RastroSounds.OrbHz(i), RastroBoard.Notes.Length), null, 0.42, RastroSounds.OrbTau);
            foreach (var k in new[] { "chord", "wrong", "unlock", "air", "cue0", "cue1", "cue2", "cue3" }) Frozen("rastro." + k);
            // el aviso de cada modo: la MISMA receta que va al juego (marimba de caída 0,2 con las alturas y los instantes de hoy)
            foreach (var r in RastroSounds.All()) { var recipe = r.Recipe; Raw(r.Key + ".madera", () => FromRecipe(recipe)); }

            FamilyEvent(g, "orbes", "Cada lucero suena SU nota (9 luceros, 392 a 1175 Hz)", "Campana: seno + parciales ×2,76 y ×5,4, ataque 8 ms, cae hasta el silencio en 1,3 s; mono, sin sala; pico ≈ 0,3",
                "RastroGameController.cs: Arrive (cuando la chispa llega al lucero) y la entrada del jugador (RastroSounds.PlayOrb)", "rastro",
                1f, "PlayNote(Notes[orb], Marimba, τ 0,2): la MISMA altura de siempre",
                "El camino se aprende con el oído y la vista: la altura, el orden y el momento de cada nota no cambian; solo el timbre. La caída es de 0,2 s (la campana de hoy cae 20 dB en 450 ms): con la marimba de 0,3 s las notas de un camino rápido se encimarían más que hoy. Suena igual cuando la chispa llega y cuando el jugador toca (un solo nivel).", marimba: true);
            E(g, "acierto", "Ronda lograda: do-mi-sol-do", "Campana: cuatro notas escalonadas 70 ms, 2 s", "RastroGameController.cs: Win y la ronda guiada del tutorial", "rastro.chord", 1f, WoodLadder(WoodSfx.Correct, WoodSounds.Variants(WoodSfx.Correct), 0.7), "Correct(largo del camino − 2, de 1 a 7): el par de kalimbas sube con el largo del camino",
                "", "DECIDIDO (Ricardo, 19-oct): la ronda lograda es una respuesta, no un hito en cada ronda (sale cada 6-10 s): suena como el acierto de toda la app y sube con el largo del camino; el hito (Perfect) no.");
            E(g, "error", "Error: campana grave", "Campana de 196 Hz, 0,7 s", "RastroGameController.cs: Wrong (y el error del tutorial)", "rastro.wrong", 1f, Wk(WoodSfx.Wrong), "Wrong");
            E(g, "nuevo", "«¡NUEVO!»: modo desbloqueado", "Dos campanas (do6 y mi6), 1,7 s", "RastroGameController.cs: ModeScreen (el cartel de un modo nuevo)", "rastro.unlock", 1f, Wk(WoodSfx.New), "New");
            for (int i = 0; i < 4; i++)
            {
                string[] names = { "Rastro (una campana)", "Al revés (dos que bajan)", "El cielo gira (tres: sube, baja)", "En marcha (tres que suben rápido)" };
                var e = E(g, "modo" + i, "Aviso de modo: " + names[i], "Campanas de la misma familia, cortas", "RastroGameController.cs: ConfusionMessage y ModeScreen (RastroSounds.PlayCue)", "rastro.cue" + i, 0.8f, "rastro.cue" + i + ".madera", "Propio rastro.cue" + i + ": marimbas de τ 0,2 con las mismas notas y el mismo ritmo", "info",
                    "Es la INSTRUCCIÓN del modo: el patrón de notas (cuántas, hacia dónde van) tiene que seguir distinguiéndose. Mismo nivel en los dos sitios donde suena (antes 0,8 y 1,0).");
                e.WoodGain = WoodSounds.PlayVolume;
            }
            E(g, "aire", "El cielo gira: soplo de aire", "Ruido por un filtro de banda 900 → 2400 Hz, 0,5 s", "RastroGameController.cs: Turn", "rastro.air", 1f, Wk(WoodSfx.SweepUp), "SweepUp", "",
                "El soplo baja 12 LU respecto de hoy (hoy era de los más fuertes por ser un ruido). Si se quiere más presente: un propio con NoiseSweep(0.5, 900, 2400) a nivel 0,18.");
            E(g, "tic", "Tic del reloj", "Común (GameFeel.Tick)", "RastroGameController.cs: UpdateClock", "gamefeel.tick", 0.45f, Wk(WoodSfx.Tick), "Tick");
        }

        // ------------------------------------------------------------------ Constelaciones

        private static void RegisterParejas()
        {
            const string g = "parejas";
            Family_("parejas", "Constelaciones: la nota de cada grupo (11)", Hz(i => ConstelacionContract.NoteHz(i), 11), null);
            // «hoy» sale CONGELADO del 17-oct: la clase de campanas de Constelaciones se borró al cablear el juego
            foreach (var k in new[] { "hum", "boardend", "flipup", "flipdown", "soft", "foundmem", "foundnew", "spark", "finale", "chime" }) Frozen("parejas." + k);

            FamilyEvent(g, "notas", "Cada grupo de luces suena SU nota (11: la 11.ª, una octava arriba)", "Campana de la pentatónica (τ 0,22 s, 0,9 s), mono, sin sala; pico ≈ 0,3",
                "ConstelacionGameController.cs:250 (al abrir una luz, vol 0,35), :266 (al fallar, vol 0,3; con Madera 3 dB más baja)", "parejas", 0.35f, "PlayNote(NoteHz(g), Kalimba): 11 notas",
                "Las dos luces de una pareja suenan igual: es una ayuda para recordar con el oído. La altura de cada grupo no cambia.");
            E(g, "voltear", "Dar vuelta una luz", "Siseo agudo que sube (2200 → 3400 Hz), 0,12 s", "ConstelacionGameController.cs:249 (vol 0,5)", "parejas.flipup", 0.5f, Wk(WoodSfx.Tap), "Tap: marimba apagada de 60 ms (por el altavoz del juego); suena a la vez que la nota del grupo (pareja permitida)");
            E(g, "cerrar", "Cerrar una luz", "Siseo corto que baja, 0,1 s", "ConstelacionGameController.cs:246, :455 (vol 0,5)", "parejas.flipdown", 0.5f, Wk(WoodSfx.Unselect), "Unselect");
            E(g, "noigual", "«No son iguales»", "Seno grave y corto que baja 330 → 247 Hz, 0,32 s", "ConstelacionGameController.cs:272 (vol 0,5)", "parejas.soft", 0.5f, Wk(WoodSfx.Knock), "Knock");
            E(g, "grupo-memoria", "Encontrar un grupo de memoria (la nota sube con la racha)", "Dos campanas (nota del grupo y su quinta), 1,6 s", "ConstelacionGameController.cs:292 (vol 0,8)", "parejas.foundmem", 0.8f, WoodLadder(WoodSfx.Correct, WoodSounds.Variants(WoodSfx.Correct), 0.7f), "Correct(racha 1 a 7): el par de kalimbas sube con la racha");
            E(g, "grupo-vista", "Encontrar un grupo a la primera vista (suerte)", "Dos campanas (nota y tercera), más calladas, 1,4 s", "ConstelacionGameController.cs:299 (vol 0,7)", "parejas.foundnew", 0.7f, Wk(WoodSfx.Correct, 1), "Correct(1), 3 dB más bajo (no sube: no fue memoria)").WoodGain = Atten(-3.0);
            var racha = E(g, "racha", "Destello de racha de memoria (1 a 8): SE QUITA", "Campanita aguda que sube por la escala, junto al acierto", "ConstelacionGameController.cs:293 (vol 0,6)", "parejas.spark", 0.6f, null, "se quita: el par de Correct ya sube con la racha", "", "DECIDIDO por Ricardo de oído (18-oct): hoy suenan los dos a la vez (el acierto y el destello); con Madera queda solo el acierto, que ya sube con la racha. Si prefieres conservar los dos, el destello suena 90 ms después.");
            racha.NoWood = "SE QUITÓ (decisión de Ricardo, 18-oct): con Madera el par de Correct ya sube con la racha, y dos sonidos por acierto es lo que la paleta evita.";
            E(g, "cielo", "Un cielo completo", "Arpegio de cuatro campanas, 1,8 s", "ConstelacionGameController.cs:223 (vol 0,8)", "parejas.boardend", 0.8f, Wk(WoodSfx.Perfect), "Perfect");
            E(g, "zumbido", "Empieza el cielo: zumbido de fondo", "Dos senos graves (196 y 294 Hz), 1,7 s", "ConstelacionGameController.cs:204 (vol 0,5)", "parejas.hum", 0.5f, Wk(WoodSfx.Hum), "Hum");
            E(g, "nuevo", "«NUEVO»", "Dos campanas (mi y la)", "ConstelacionGameController.cs:406 (vol 0,7)", "parejas.chime", 0.7f, Wk(WoodSfx.New), "New");
            var hermanas = E(g, "hermanas", "Las luces hermanas brillan (la pista, 380 ms después de errar)", "Dos campanas (mi y la), muy suaves", "ConstelacionGameController.cs:460 (vol 0,25)", "parejas.chime", 0.25f, "pista.a", "PlayNote(NoteHz(grupo), Kalimba, gainDb −6): una vez por cada luz hermana, cuando brilla", "",
                "ELEGIDO por Ricardo (19-oct, Tarea 137): el «Hint» de la paleta no le sonó a ayuda y de las tres opciones de docs/previews/sonido-escucha-pista.html eligió la (a): cada luz hermana suena la nota de SU grupo en kalimba, suave (−6 dB), en el momento en que brilla (con dos hermanas, dos notas 0,16 s aparte).");
            hermanas.WoodGain = Atten(-6.0);
            E(g, "final", "Fin de la partida", "Tres campanas que suben, 2 s, con eco", "ConstelacionGameController.cs:358", "parejas.finale", 0.8f, Wk(WoodSfx.Finish), "Finish (el común; se quita el doble final)");
        }

        // ------------------------------------------------------------------ La pista de Constelaciones: tres opciones para elegir (Tarea 135)

        /// <summary>
        /// «Las luces hermanas brillan»: a Ricardo el <c>Hint</c> de la paleta no le sonó a ayuda (18-oct) y eligió de oído la opción (a) (19-oct, Tarea 137: cableada con <see cref="ConstelacionSounds.PlayHint"/>); la página queda como registro. Tres opciones, cada una con su botón «Hoy»:
        /// (a) la NOTA DEL PROPIO GRUPO en kalimba, suave (−6 dB respecto de la nota normal), una vez por cada luz hermana en el momento en que brilla (la recomendada: la pareja «se oye» y ayuda a recordar); (b) la misma nota en marimba, suave;
        /// (c) las dos campanitas de hoy (mi y la) en kalimba, suaves. Los ejemplos suenan con dos hermanas (dos notas, una cada 0,16 s; con una pareja es una sola, con un trío son dos).
        /// </summary>
        private static void RegisterPista()
        {
            const string g = "pista";
            double hz = ConstelacionContract.NoteHz(3);                                    // el ejemplo: el grupo de la 4.ª nota (sol5)
            Raw("pista.a", () => Scale(2, 0.16, i => FromRecipe(WoodSounds.MakeNote(hz, WoodVoice.Kalimba))));
            Raw("pista.b", () => Scale(2, 0.16, i => FromRecipe(WoodSounds.MakeNote(hz, WoodVoice.Marimba))));
            Raw("pista.c", () => Scale(2, 0.09, i => FromRecipe(WoodSounds.MakeNote(i == 0 ? 659.25 : 880.0, WoodVoice.Kalimba))));
            const string today = "Hoy: dos campanitas (mi y la), muy suaves, las mismas para todos los grupos";
            const string sites = "ConstelacionGameController.cs: la pista, unos 0,4 s después de errar (ConstelacionSounds.PlayHint)";
            var a = E(g, "a", "Opción (a) · la nota del propio grupo, en kalimba, suave", today, sites, "parejas.chime", 0.25f, "pista.a", "PlayNote(NoteHz(grupo), Kalimba, gainDb −6): una vez por cada luz hermana, cuando brilla",
                "elegida", "LA ELEGIDA por Ricardo (19-oct), ya en el juego: la hermana suena con la nota de SU grupo (la misma que sonó al abrirla); así la pareja «se oye» y ayuda a recordar con el oído. El ejemplo suena con dos hermanas.");
            a.WoodGain = Atten(-6.0); a.WoodLabel = "Opción (a)";
            var b = E(g, "b", "Opción (b) · la misma nota del grupo, en marimba, suave", today, sites, "parejas.chime", 0.25f, "pista.b", "PlayNote(NoteHz(grupo), Marimba, gainDb −6): una vez por cada luz hermana, cuando brilla",
                "", "La misma idea con marimba: más seca y con más cuerpo que la kalimba. El ejemplo suena con dos hermanas.");
            b.WoodGain = Atten(-6.0); b.WoodLabel = "Opción (b)";
            var c = E(g, "c", "Opción (c) · las dos campanitas de hoy (mi y la), en kalimba, suaves", today, sites, "parejas.chime", 0.25f, "pista.c", "Dos kalimbas, mi (659 Hz) y la (880 Hz), 90 ms entre una y otra: el mismo gesto de hoy con el timbre de Madera",
                "", "Igual de suave que hoy y con la misma melodía para todos los grupos (no dice cuál es la pareja, solo avisa).");
            c.WoodGain = Atten(-6.0); c.WoodLabel = "Opción (c)";
        }

        // ------------------------------------------------------------------ Bodega de carga

        private static void RegisterBodega()
        {
            const string g = "bodega";
            Family_("bodega", "Bodega de carga: la nota de cada escotilla (12)", Hz(i => BodegaSounds.NoteHz(i), 12), null);
            // «hoy» sale CONGELADO del 17-oct: la clase de campanas de Bodega se borró al cablear el juego
            foreach (var k in new[] { "whoosh", "hum", "arrive", "beam", "open", "close", "clank", "ratchet", "ping", "thud", "spark", "perfect", "finale", "chime" }) Frozen("bodega." + k);
            // los CINCO sonidos propios de Bodega en Madera: las mismas recetas que van al juego (BodegaSounds.MakeRecipe)
            foreach (var (key, recipe, mono) in BodegaSounds.All()) { var r = recipe; Raw(key + ".madera", () => FromRecipe(r)); }

            FamilyEvent(g, "notas", "Cada escotilla suena SU nota (12; la 11.ª y siguientes, una octava arriba)", "Campana de la pentatónica (τ 0,42 s, 1,3 s), mono, sin sala; pico 0,3",
                "BodegaGameController.cs:221 (al guardar, vol 0,9), :409 (al abrir, vol 0,9)", "bodega", 0.9f, "PlayNote(NoteHz(h), Kalimba): 12 notas",
                "Con la vista y con el oído se recuerda dónde está cada cosa: la altura de cada escotilla no cambia.");
            E(g, "zumbido", "El robot arranca: zumbido grave", "Triángulo 65 → 90 Hz, 0,85 s", "BodegaGameController.cs:189 (vol 0,5)", "bodega.hum", 0.5f, Wk(WoodSfx.Hum), "Hum");
            E(g, "llega", "Llega la carga por la esclusa", "Soplido que baja y campanita aguda, 0,6 s", "BodegaGameController.cs:202 (vol 0,6)", "bodega.arrive", 0.6f, "bodega.arrive.madera", "PROPIO bodega.arrive: un soplido que baja y una kalimba aguda (sol6)", "", "NUEVO: sonido propio de Bodega.");
            E(g, "haz", "El haz del robot («piu» 420 → 880 Hz)", "Seno que sube, 0,34 s", "BodegaGameController.cs:213, :253, :283 (vol 0,6)", "bodega.beam", 0.6f, "bodega.beam.madera", "PROPIO bodega.beam: tres marimbas que suben (una kalimba no puede deslizarse)", "", "NUEVO: sonido propio de Bodega.");
            E(g, "abrir", "Abrir una puerta", "Siseo agudo que baja, 0,2 s", "BodegaGameController.cs:216, :384, :407, :688 (vol 0,6)", "bodega.open", 0.6f, "bodega.open.madera", "PROPIO bodega.open (mono): un siseo corto que sube", "", "NUEVO: sonido propio de Bodega.");
            E(g, "cerrar", "Cerrar una puerta", "Golpe grave y suave (ruido 700 → 250 Hz), 0,2 s", "BodegaGameController.cs:227, :390, :421, :688 (vol 0,5)", "bodega.close", 0.5f, Wk(WoodSfx.Clack), "Clack, 6 dB más bajo").WoodGain = Atten(-6.0);
            E(g, "caja", "Tomar o dejar la caja («clank»)", "Golpe metálico corto y campanita aguda, 0,4 s", "BodegaGameController.cs:259, :289 (vol 0,6)", "bodega.clank", 0.6f, Wk(WoodSfx.Clack), "Clack");
            E(g, "volar", "El robot vuela con la caja", "Soplido de ruido 300 → 1500 Hz, 1,1 s", "BodegaGameController.cs:263 (vol 0,6); BodegaGameController.Scene.cs:607 (vol 0,5)", "bodega.whoosh", 0.6f, "bodega.fly.madera", "PROPIO bodega.fly: un soplido de 1 s de 300 a 1500 Hz (el SweepUp de la paleta dura 0,5 s: corto para el vuelo)", "", "NUEVO: sonido propio de Bodega.");
            E(g, "matraca", "La bodega gira: matraca de engranes", "Un clic cada 110 ms durante 1,5 s y zumbido grave", "BodegaGameController.cs:308 (vol 0,6)", "bodega.ratchet", 0.6f, "bodega.ratchet.madera", "PROPIO bodega.ratchet: 14 bloques de madera, uno cada 110 ms, y dos marimbas graves", "tiempo", "NUEVO: sonido propio de Bodega; dura lo que el giro (1,5 s). Hoy suena −19 LUFS; con Madera −32 (a propósito: era fuerte por accidente).");
            E(g, "pedido", "La pregunta de un pedido", "Campanita (mi agudo), 0,6 s", "BodegaGameController.cs:354, :394 (vol 0,6)", "bodega.ping", 0.6f, Wk(WoodSfx.Notice), "Notice");
            E(g, "error", "Error: golpe sordo", "Seno grave que baja 220 → 130 Hz, 0,4 s", "BodegaGameController.cs:386 (vol 0,7)", "bodega.thud", 0.7f, Wk(WoodSfx.Wrong), "Wrong");
            E(g, "racha", "Destello de racha (1 a 8), junto a la nota", "Campanita aguda que sube de tono", "BodegaGameController.cs:412 (vol 0,6)", "bodega.spark", 0.6f, WoodLadder(WoodSfx.Spark, WoodSounds.Variants(WoodSfx.Spark), 0.55f), "Spark(n): pareja permitida (la nota es la información, el destello es la racha)");
            E(g, "perfecto", "Pedido perfecto", "Arpegio de cuatro campanas, 1,8 s", "BodegaGameController.cs:471 (vol 0,8)", "bodega.perfect", 0.8f, Wk(WoodSfx.Perfect), "Perfect");
            E(g, "nuevo", "«NUEVO»", "Dos campanas (mi y la)", "BodegaGameController.cs:552 (vol 0,7)", "bodega.chime", 0.7f, Wk(WoodSfx.New), "New");
            E(g, "final", "Fin de la partida", "Tres campanas que suben, 2 s, con eco", "BodegaGameController.cs:486", "bodega.finale", 0.8f, Wk(WoodSfx.Finish), "Finish (el común)");
        }

        // ------------------------------------------------------------------ La estación de correo

        private static void RegisterCorreo()
        {
            const string g = "correo";
            Family_("correo", "Correo: la nota de cada buzón (4)", Hz(i => MailContract.PlanetNotes[i], 4), null);
            // «hoy» sale CONGELADO del 17-oct: la clase de campanas de Correo se borró al cablear el juego
            foreach (var k in new[] { "combo", "thud", "soft", "tick", "fanfare", "radio", "horn", "sack", "hum", "brief", "chime", "dayend", "finale", "tier" }) Frozen("correo." + k);
            // los TRES sonidos propios de Correo en Madera: las mismas recetas que van al juego (MailSounds.MakeRecipe)
            foreach (var (key, recipe, mono) in MailSounds.All()) { var r = recipe; Raw(key + ".madera", () => FromRecipe(r)); }
            // el faro: hoy suenan a la vez la fanfarria (vol 0,8) y la bocina (vol 0,6); con Madera, la fanfarria (Perfect) y la bocina de fondo, 8 LU más baja
            Frozen("correo.faro.hoy");
            Raw("correo.faro.madera", () => Mix((FromWood("", WoodSfx.Perfect, 0), 1.0), (FromRecipe(MailSounds.MakeRecipe(MailSfx.Horn)), 1.0)));

            FamilyEvent(g, "buzones", "Cada buzón suena SU nota (do5, mi5, sol5, do6)", "Campana suave (τ 0,2 s, 0,7 s), mono, sin sala; pico 0,3",
                "MailGameController.Scene.cs:158 (al entregar, vol 0,5)", "correo", 0.5f, "PlayNote(PlanetNotes[b], Kalimba): 4 notas", "La nota identifica el buzón (planeta): no cambia la altura.");
            E(g, "combo", "Brillo de la racha (desde ×3), junto a la nota", "Campanita aguda que sube por la escala", "MailGameController.Scene.cs:159 (vol 0,35)", "correo.combo", 0.35f, WoodLadder(WoodSfx.Spark, WoodSounds.Variants(WoodSfx.Spark), 0.55f), "Spark(n): pareja permitida (la nota es la información, el brillo la racha)");
            E(g, "escalon", "Escalón de racha (×10, ×20, ×30)", "Dos a cuatro campanas, 1,4 s", "MailGameController.Scene.cs:183 (vol 0,7), :553 (vol 0,5, demostración; con Madera 3 dB más baja)", "correo.tier", 0.7f, Wk(WoodSfx.Streak), "Streak (uno solo para los tres escalones)");
            E(g, "error", "Carta atrasada que cae / buzón equivocado / no va en la caja fuerte", "Golpe sordo (220 → 130 Hz), 0,34 s", "MailGameController.Scene.cs:165, :209, :240, :272 (vol 0,5 a 0,6)", "correo.thud", 0.6f, Wk(WoodSfx.Wrong), "Wrong");
            E(g, "esanoo", "«Esa carta no» / «aún no es la hora» / ventana cerrada", "Tono suave que baja 392 → 330 Hz, 0,28 s", "MailGameController.Scene.cs:175, :212, :244, :248, :287 (vol 0,4 a 0,5)", "correo.soft", 0.5f, Wk(WoodSfx.Knock), "Knock");
            E(g, "cumplido", "Encargo cumplido (caja fuerte)", "Fanfarria de tres campanas (sol, si, re agudos), 1,6 s", "MailGameController.Scene.cs:202 (vol 0,8)", "correo.fanfare", 0.8f, Wk(WoodSfx.Perfect), "Perfect");
            E(g, "faro", "Se enciende el faro: fanfarria Y bocina a la vez", "La fanfarria (vol 0,8) y la bocina grave (vol 0,6) suenan juntas", "MailGameController.Scene.cs:230-231", "correo.faro.hoy", 1f, "correo.faro.madera", "Perfect + la bocina propia de fondo (8 LU más baja)", "", "DECISIÓN 5 para que la apruebes de oído: hoy son dos hitos encimados; con Madera manda la fanfarria y la bocina queda de fondo. Otras opciones: solo la bocina (sin premio) o solo la fanfarria.");
            E(g, "bocina", "La nave del correo: bocina grave de tres notas", "Tres triángulos (146, 220, 293 Hz), 2,1 s", "MailGameController.Scene.cs:231 (vol 0,6)", "correo.horn", 0.6f, "correo.horn.madera", "PROPIO correo.horn: tres marimbas graves, dos golpes por nota para que «dure»", "", "NUEVO: sonido propio de Correo; hoy −18 LUFS, con Madera −26 (la bocina ya no manda sobre la fanfarria).");
            E(g, "tic", "Mirar el reloj: tic", "Ruido y campanita, 0,4 s", "MailGameController.Scene.cs:259 (vol 0,5)", "correo.tick", 0.5f, Wk(WoodSfx.Tick), "Tick");
            E(g, "saco", "«¡Llega un saco!»", "Siseo que sube, golpe y dos campanitas, 0,9 s", "MailGameController.Scene.cs:277 (vol 0,7)", "correo.sack", 0.7f, "correo.sack.madera", "PROPIO correo.sack: un siseo que sube, un golpe grave y dos kalimbas", "", "NUEVO: sonido propio de Correo.");
            E(g, "radio", "La radio", "Dos tonos y un siseo, 0,7 s", "MailGameController.Scene.cs:282 (vol 0,7)", "correo.radio", 0.7f, "correo.radio.madera", "PROPIO correo.radio: estática granulada y dos marimbas cortas", "", "NUEVO: sonido propio de Correo.");
            E(g, "dia", "Empieza el día: zumbido (sol y re graves)", "Dos senos, 1,6 s", "MailGameController.cs:183 (vol 0,5)", "correo.hum", 0.5f, Wk(WoodSfx.Hum), "Hum");
            E(g, "hoja", "La hoja de la mañana", "Campana (sol), 0,9 s", "MailGameController.cs:225 (vol 0,7)", "correo.brief", 0.7f, Wk(WoodSfx.Notice), "Notice");
            E(g, "cierre-dia", "Cierre del día: tres campanas que suben", "Tres campanas, 1,6 s, con eco", "MailGameController.cs:198 (vol 0,8)", "correo.dayend", 0.8f, Wk(WoodSfx.LevelUp), "LevelUp");
            E(g, "nuevo", "«NUEVO»", "Dos campanas (mi y la)", "MailGameController.cs:296 (vol 0,7)", "correo.chime", 0.7f, Wk(WoodSfx.New), "New");
            E(g, "final", "Fin de la partida", "Dos campanas lentas (sol y do agudos), 2 s", "MailGameController.cs:262", "correo.finale", 0.8f, Wk(WoodSfx.Finish), "Finish (el común)");
        }

        // ------------------------------------------------------------------ Tinta o Palabra

        private static void RegisterStroop()
        {
            const string g = "stroop";
            H("stroop.regla", () => HarmonicTone.Build(392f, 0.14f, 0.1f));
            H("stroop.tinta", () => HarmonicTone.Build(330f, 0.1f, 0.1f));
            H("stroop.palabra", () => HarmonicTone.Build(440f, 0.1f, 0.1f));
            H("stroop.listo", () => HarmonicTone.Build(523f, 0.4f, 0.08f));
            Raw("stroop.regla.madera", () => FromRecipe(WoodSounds.MakeCue(392, 0.14)));
            Raw("stroop.tinta.madera", () => FromRecipe(WoodSounds.MakeCue(330, 0.1)));
            Raw("stroop.palabra.madera", () => FromRecipe(WoodSounds.MakeCue(440, 0.1)));
            E(g, "regla", "Aparece la tarjeta de la regla", "PlayTone: 392 Hz, 0,14 s (seno + 2.º y 3.er armónico; ataque 15 ms; amplitud 0,1)", "StroopGameController.cs:377", "stroop.regla", 1f, "stroop.regla.madera", "Cue(392): marimba seca de ~0,1 s", "señal",
                "Avisa que hay una regla nueva. Misma altura; el inicio no se atrasa.");
            E(g, "cambia-tinta", "La regla pasa a «el color de la tinta»", "PlayTone: 330 Hz, 0,1 s", "StroopGameController.cs:1034", "stroop.tinta", 1f, "stroop.tinta.madera", "Cue(330)", "señal", "La ALTURA dice qué regla vale: 330 Hz = tinta, 440 Hz = palabra. No se cambia.");
            E(g, "cambia-palabra", "La regla pasa a «lo que dice la palabra»", "PlayTone: 440 Hz, 0,1 s", "StroopGameController.cs:1034", "stroop.palabra", 1f, "stroop.palabra.madera", "Cue(440)", "señal");
            E(g, "listo", "Práctica: «¡Listo!»", "PlayTone: 523 Hz, 0,4 s", "StroopGameController.cs:1182", "stroop.listo", 1f, Wk(WoodSfx.Notice), "Notice");
            E(g, "comun", "Acierto, error, subir de nivel y tic", "Comunes (GameFeel)", "StroopGameController.cs:287, :307, :212, :364", "gamefeel.wrong", 0.5f, Wk(WoodSfx.Wrong), "Wrong (ver «Lo común»)");
        }

        // ------------------------------------------------------------------ Piloto Estelar

        private static void RegisterPiloto()
        {
            const string g = "piloto";
            Family_("piloto-balizas", "Piloto: las 4 balizas dentro de la ruta", Hz(i => PilotSounds.Penta[1 + i], 4), i => PilotSounds.Beacon(i), 0.45);
            Family_("piloto-atrapar", "Piloto: atrapar una señal (la racha sube la nota)", Hz(i => PilotSounds.Penta[3 + i], 5), i => PilotSounds.Catch(i), 0.5);
            H("piloto.buzz", () => PilotSounds.Buzz());
            H("piloto.blip", () => PilotSounds.Blip());
            H("piloto.thud", () => PilotSounds.Thud());
            H("piloto.hyper", () => PilotSounds.Hyper());
            H("piloto.whoosh", () => PilotSounds.Whoosh());
            H("piloto.mission", () => PilotSounds.MissionChange());
            H("piloto.finale", () => PilotSounds.Finale());
            Raw("piloto.blip.madera", () => FromRecipe(WoodSounds.MakeCue(880, 0.12)));
            Raw("piloto.mission.madera", () => Sequence(new[] { FromRecipe(WoodSounds.MakeNote(659.25, WoodVoice.Kalimba)), FromRecipe(WoodSounds.MakeNote(987.77, WoodVoice.Kalimba)) }, new[] { 0.0, PilotContract.PulseBeatSeconds }));

            FamilyEvent(g, "balizas", "Baliza dentro de la ruta: una nota de la escala", "Campana corta (τ 0,1 s, 0,3 s), mono; pico ≈ 0,3", "PilotGameController.cs:268 (vol 0,6)", "piloto-balizas", 0.6f, "Note(hz): kalimba (o marimba)", "Dentro de la ruta suena una nota y fuera suena el zumbido: no es solo el color.");
            E(g, "zumbido", "Baliza FUERA de la ruta: zumbido grave", "Dos senos (130 y 138 Hz) con ruido, 0,22 s", "PilotGameController.cs:274 (vol 0,7)", "piloto.buzz", 0.7f, Wk(WoodSfx.Knock), "Knock", "info", "Tiene que seguir siendo CLARAMENTE distinto de las notas de las balizas buenas.");
            E(g, "blip", "Aparece una señal (el MISMO blip para todas)", "Seno 880 Hz, τ 35 ms, 0,12 s", "PilotGameController.cs:378 (vol 0,5)", "piloto.blip", 0.5f, "piloto.blip.madera", "Cue(880)", "señal", "Por diseño es IGUAL para todas las señales (no delata cuál es la de la misión). Debe seguir idéntico entre sí.");
            FamilyEvent(g, "atrapar", "Atrapar la señal de la misión (la racha sube la nota)", "Campana de la escala (τ 0,25 s, 0,8 s)", "PilotGameController.cs:447 (vol 0,8)", "piloto-atrapar", 0.8f, "Correct(racha) o Note(hz)", "");
            E(g, "error", "Toque equivocado", "Golpe sordo y corto (220 → 130 Hz), 0,3 s", "PilotGameController.cs:460 (vol 0,7)", "piloto.thud", 0.7f, Wk(WoodSfx.Wrong), "Wrong");
            E(g, "sector", "Cruzas el arco de un sector", "Soplido que sube 300 → 2400 Hz, 0,8 s", "PilotGameController.cs:559 (vol 0,8); PilotGameController.Guided.cs:210", "piloto.whoosh", 0.8f, Wk(WoodSfx.SweepUp), "SweepUp");
            E(g, "mision", "La misión cambió: dos notas que suben (con el latido de la tarjeta)", "Dos triángulos (mi6 y si6) a 0,4 s uno del otro, 1 s", "PilotGameController.Scene.cs:71 (vol 0,85)", "piloto.mission", 0.85f, "piloto.mission.madera", "Note(hz) × 2 a 0,4 s", "tiempo", "Va sincronizado con los dos latidos de la tarjeta: el segundo suena exactamente 0,4 s después del primero. No se mueve.");
            E(g, "hiper", "Hiperimpulso", "Destello de ruido que se abre y cuatro campanas que suben, 1,6 s", "PilotGameController.cs:484 (vol 0,8)", "piloto.hyper", 0.8f, Wk(WoodSfx.Streak), "Streak");
            E(g, "final", "Llegada al puerto: fin del vuelo", "Dos campanas lentas, 2 s", "PilotGameController.cs:604 (vol 0,8)", "piloto.finale", 0.8f, Wk(WoodSfx.Finish), "Finish");
            E(g, "motor", "El motor «Cohete» (bucle de 4 s en tres capas)", "Ruido café filtrado con turbulencia; ya aprobado por Ricardo (10-oct)", "PilotEngine.cs (PilotGameController.Scene.cs:465 Tick)", null, 1f, null, "SE QUEDA como está (Ricardo lo aprobó)", "bucle");
        }

        // ------------------------------------------------------------------ Freno de emergencia

        private static void RegisterFreno()
        {
            const string g = "freno";
            H("freno.salida", () => HarmonicTone.Build(784f, 0.06f, 0.06f));
            H("freno.rugido", () => HarmonicTone.Build(196f, 0.25f, 0.05f));
            H("freno.lento", () => HarmonicTone.Build(262f, 0.12f, 0.05f));
            H("freno.resoplido", () => HarmonicTone.Build(147f, 0.3f, 0.05f));
            Raw("freno.sirena", () => Sequence(new[] { FromClip(HarmonicTone.Build(988f, 0.09f, 0.08f)), FromClip(HarmonicTone.Build(740f, 0.09f, 0.08f)), FromClip(HarmonicTone.Build(988f, 0.09f, 0.07f)) }, new[] { 0.0, 0.1, 0.2 }));
            Raw("freno.salida.madera", () => FromRecipe(WoodSounds.MakeCue(784, 0.06)));
            Raw("freno.sirena.madera", () => Sequence(new[] { FromRecipe(WoodSounds.MakeCue(988, 0.09)), FromRecipe(WoodSounds.MakeCue(740, 0.09)), FromRecipe(WoodSounds.MakeCue(988, 0.09)) }, new[] { 0.0, 0.1, 0.2 }));
            E(g, "salida", "Se enciende un cohete: SEÑAL de salida", "PlayTone: 784 Hz, 60 ms (seno + 2.º y 3.er armónico; amplitud 0,06)", "BrakeGameController.cs:236, :1183 (práctica)", "freno.salida", 1f, "freno.salida.madera", "Cue(784)", "señal",
                "Es el ESTÍMULO: el tiempo de reacción se mide desde que se enciende la luz y suena este tono. Misma altura, misma duración y el inicio igual de inmediato (nada de ataques lentos).");
            E(g, "sirena", "¡ALTO!: la sirena de dos tonos (988, 740, 988 Hz)", "Tres tonos de 90 ms cada 100 ms", "BrakeGameController.cs:444-453", "freno.sirena", 1f, "freno.sirena.madera", "Cue × 3 (mismas alturas y ritmo)", "señal",
                "Es la señal de PARAR: tiene que seguir sonando a ALARMA y no a otra cosa. Se mantiene el patrón de tres tonos; cuidar que no se confunda con la señal de salida.");
            E(g, "rugido", "Despegue acertado: rugido grave (con el acierto común)", "PlayTone: 196 Hz, 0,25 s", "BrakeGameController.cs:315", "freno.rugido", 1f, null, "propio (Fase C): se evalúa quitarlo (ya suena el Acierto común)");
            E(g, "lento", "«¡Más rápido!»", "PlayTone: 262 Hz, 0,12 s", "BrakeGameController.cs:344", "freno.lento", 1f, Wk(WoodSfx.Knock), "Knock");
            E(g, "resoplido", "Freno perfecto: resoplido (con el acierto común)", "PlayTone: 147 Hz, 0,3 s", "BrakeGameController.cs:359", "freno.resoplido", 1f, null, "propio (Fase C): se evalúa quitarlo (ya suena el Acierto común)");
            E(g, "listo", "Práctica: «¡Listo!»", "PlayTone: 523 Hz, 0,4 s", "BrakeGameController.cs:1255", "stroop.listo", 1f, Wk(WoodSfx.Notice), "Notice");
            E(g, "comun", "Acierto, error, subir de nivel y tic", "Comunes (GameFeel)", "BrakeGameController.cs:313/358, :331/379, :288/323/368, :421", "gamefeel.wrong", 0.5f, Wk(WoodSfx.Wrong), "Wrong (ver «Lo común»)");
        }

        // ------------------------------------------------------------------ Satélites

        private static void RegisterSatelites()
        {
            const string g = "satelites";
            // «hoy» sale CONGELADO del 17-oct (Tarea 138): la clase de campanas de Satélites ya no existe (queda solo el zumbido en bucle)
            Family_("satelites-campanas", "Satélites: las campanas de la presentación (una por satélite)", Hz(i => SatelliteSounds.CueHz(i), 5), null, 0.3);
            Family_("satelites-marcas", "Satélites: marcar (cada marca sube)", Hz(i => SatelliteSounds.Penta[SatelliteSounds.MarkStep(i + 1)], 7), null, 0.4);
            Family_("satelites-luces", "Satélites: una luz que se enciende", Hz(i => SatelliteSounds.Penta[4 + i], 5), null, 0.45);
            foreach (var k in new[] { "stop", "unmark", "blocked", "perfect", "partial", "chime", "finale" }) Frozen("satelites." + k);
            H("satelites.hum", () => SatelliteSounds.Hum());                // el bucle aprobado: sigue siendo el clip del juego

            FamilyEvent(g, "campanas", "Se presentan los satélites con mensaje: una campana cada 0,12 s", "Campana de la escala (τ 0,3 s, 1 s)", "SatelliteGameController.cs: DoCue (SatelliteSounds.PlayCue, cada SatelliteContract.CueBellGap); SatelliteGameController.Guided.cs: RingCues", "satelites-campanas", 0.7f, "PlayNote(CueHz(i), Marimba): la altura de hoy, τ 0,3 s como hoy",
                "La CANTIDAD de campanas es la información (cuántos satélites hay que seguir) y cada una cae a los 0,12 s: orden y momento no se mueven (el 0,12 es SatelliteContract.CueBellGap y el juego y el tutorial lo usan). En el tutorial antes sonaban todas de golpe; ahora una por una, como en el juego.", marimba: true);
            E(g, "zumbido", "Los satélites giran: zumbido en bucle", "Dos senos (110 y 165 Hz), bucle exacto de 1 s, volumen 0,12", "SatelliteGameController.cs: StartHum/StopHum (AudioSource en bucle)", "satelites.hum", 0.12f, null, "SE QUEDA (bucle grave y suave: como el motor de Piloto y el propulsor de Aterrizaje)", "bucle");
            E(g, "clac", "Se detienen: «clac»", "Seno 260 → 150 Hz y chasquido, 0,3 s", "SatelliteGameController.cs: DoTrack (al terminar el seguimiento)", "satelites.stop", 0.8f, Wk(WoodSfx.Clack), "Clack", "tiempo", "Marca el momento en que el seguimiento termina y empieza la respuesta.");
            E(g, "marcar", "Marcar un satélite (cada marca sube)", "Campana de la escala (τ 0,12 s, 0,5 s)", "SatelliteGameController.cs: Toggle (SatelliteSounds.PlayMark)", "fam.satelites-marcas.hoy", 0.8f, WoodLadder(WoodSfx.Select, new[] { 2, 3, 4, 5, 6, 7, 8 }, 0.7), "Select(n + 1): kalimba, el mismo evento de Rescate; la altura de cada marca es la de hoy (mi5 con la primera)", "info",
                "DECIDIDO (Ricardo, 19-oct): un evento, un sonido (el de Rescate). Las marcas las pone el dedo (≥ 0,3 s entre una y otra) y la kalimba suena más de un segundo: no se encima. Si en el teléfono se oyera arrastrada, la alternativa es PlayNote(Penta[n + 1], Marimba).");
            E(g, "quitar", "Quitar una marca", "Seno que baja 440 → 330 Hz, 0,2 s", "SatelliteGameController.cs: Toggle", "satelites.unmark", 0.7f, Wk(WoodSfx.Unselect), "Unselect");
            E(g, "bloqueado", "Ya están todos marcados (un toque que no cuenta)", "Seno 300 Hz, 0,2 s", "SatelliteGameController.cs: Toggle", "satelites.blocked", 0.6f, Wk(WoodSfx.Knock), "Knock", "",
                "Sube ≈ 10 LU respecto de hoy (hoy casi no se oía): es a propósito.");
            E(g, "perfecto", "Acertar todo: arpegio", "Cuatro campanas que suben, 1,5 s", "SatelliteGameController.cs: Evaluate", "satelites.perfect", 0.8f, Wk(WoodSfx.Perfect), "Perfect");
            E(g, "falto", "Faltó alguno: tono suave que baja", "Seno 392 → 330 Hz, 0,5 s", "SatelliteGameController.cs: Evaluate", "satelites.partial", 0.7f, Wk(WoodSfx.Wrong), "Wrong");
            E(g, "luz", "Una luz se enciende en el planeta", "Campanita aguda que sube por la escala (τ 0,22 s)", "SatelliteGameController.Scene.cs: LightUp (SatelliteSounds.PlayLight)", "fam.satelites-luces.hoy", 0.7f, WoodLadder(WoodSfx.Spark, new[] { 1, 2, 3, 4, 5 }, 0.55), "Spark(1 + luces % 5): la misma escalera de cinco notas que sube y vuelve a empezar (un escalón arriba: la paleta empieza en el do6)", "info",
                "DECISIÓN DE SONNET: la spec decía Spark(luces + 1) con tope en 8; hoy la escalera es de cinco notas que se repite, así que se conserva el ciclo (si no, a partir de la 8.ª luz sonaría siempre la misma).");
            E(g, "nuevo", "Aviso de sorpresa", "Dos campanas (mi y la)", "SatelliteGameController.cs: ShowSurprise", "satelites.chime", 0.7f, Wk(WoodSfx.New), "New");
            E(g, "final", "Fin de la partida", "Dos campanas lentas (sol y do agudos), 2 s", "ExitButton (GameFeel.Finish)", "satelites.finale", 0.8f, Wk(WoodSfx.Finish), "Finish");
        }

        // ------------------------------------------------------------------ Rescate relámpago (ya en Madera cálida)

        private static void RegisterRadar()
        {
            const string g = "radar";
            string[] ids = { "ping", "flash", "static", "unmark", "beam", "perfect", "miss", "rain", "finale", "trip" };
            RadarSfx[] sfx = { RadarSfx.Ping, RadarSfx.Flash, RadarSfx.Static, RadarSfx.Unmark, RadarSfx.Beam, RadarSfx.Perfect, RadarSfx.Miss, RadarSfx.Rain, RadarSfx.Finale, RadarSfx.Trip };
            for (int i = 0; i < ids.Length; i++) { var s = sfx[i]; H("radar." + ids[i], () => RadarSounds.Get(s)); }
            Raw("radar.mark", () => Scale(4, 0.45, i => FromClip(RadarSounds.Get(RadarSfx.Mark, i + 1))));
            Raw("radar.dock", () => Scale(4, 0.45, i => FromClip(RadarSounds.Get(RadarSfx.Dock, i))));
            EW(g, "ping", "Una vuelta del haz del radar", "Bloque de madera 1250 Hz + sala", "RadarGameController.Scene.cs:328 (vol 0,8)", "radar.ping", 0.8f, "= Tick");
            EW(g, "flash", "El relámpago (qué cápsulas viste)", "Ruido que se cierra y golpe grave", "RadarGameController.cs:230", "radar.flash", 0.8f, "propio");
            EW(g, "static", "La estática", "Sonajero suave", "RadarGameController.cs:259", "radar.static", 0.8f, "propio");
            EW(g, "mark", "Marcar la 1.ª, 2.ª, 3.ª y 4.ª cápsula", "Kalimba do5, re5, mi5, sol5", "RadarGameController.cs:507 (vol 0,8)", "radar.mark", 0.8f, "= Select(k)");
            EW(g, "unmark", "Desmarcar", "Kalimba apagada (392 Hz)", "RadarGameController.cs:502", "radar.unmark", 0.8f, "= Unselect");
            EW(g, "beam", "El rayo tractor", "Marimba que sube", "RadarGameController.Scene.cs:481", "radar.beam", 0.8f, "propio");
            EW(g, "dock", "Entra una cápsula en la nave", "Marimba y bloque de madera (cuatro alturas)", "RadarGameController.Scene.cs:531", "radar.dock", 0.8f, "propio");
            EW(g, "perfect", "Ronda perfecta", "Marimba do-mi-sol-do y dos kalimbas", "RadarGameController.cs:334", "radar.perfect", 0.8f, "= Perfect");
            EW(g, "miss", "Ronda con un error", "Dos marimbas graves que bajan", "RadarGameController.cs:340", "radar.miss", 0.8f, "= Wrong");
            EW(g, "rain", "Lluvia de cápsulas", "Ocho kalimbas que bajan", "RadarGameController.cs:214", "radar.rain", 0.8f, "propio");
            EW(g, "trip", "Viaje a la estación", "Rayo rápido y una kalimba do6", "RadarGameController.cs:366", "radar.trip", 0.8f, "propio");
            EW(g, "finale", "Fin de la partida", "Acorde de marimba y kalimba larga", "RadarGameController.cs:395", "radar.finale", 0.8f, "= Finish");
        }

        // ------------------------------------------------------------------ Carga exacta

        private static void RegisterCarga()
        {
            const string g = "calculo";
            // «hoy» de las cinco clases de la Fase C: lo que suena con el juego tal como está; los clips sueltos salen CONGELADOS del 17-oct (nada se renderiza), lo que se mezcla o es una escala sale del código de hoy
            Frozen("calculo.select");
            Frozen("calculo.operation");
            Frozen("calculo.merge");
            Frozen("calculo.thud");
            Frozen("calculo.back");
            Frozen("calculo.hint");
            Frozen("calculo.arpeggio");
            Frozen("calculo.finale");
            E(g, "celda", "Elegir una celda", "Campanita (la nota 3 de la escala), 0,5 s", "CalculoGameController.cs:535, :541 (vol 0,7)", "calculo.select", 0.7f, Wk(WoodSfx.Select, 2), "Select(2): kalimba mi5, la misma altura");
            E(g, "operacion", "Elegir una operación", "Campanita (la nota 6), 0,5 s", "CalculoGameController.cs:556 (vol 0,7)", "calculo.operation", 0.7f, Wk(WoodSfx.Select, 5), "Select(5): kalimba do6, la misma altura");
            E(g, "juntar", "Juntar dos celdas (la nota sube con cada paso)", "Campana de la escala (τ 0,22 s) y un soplo de luz", "CalculoGameController.cs:579 (vol 0,8)", "calculo.merge", 0.8f, WoodLadder(WoodSfx.Correct, WoodSounds.Variants(WoodSfx.Correct), 0.7f), "Correct(paso 1 a 7): el par de kalimbas sube con el paso", "info", "La altura sube con cada paso (cuánto avanzó la cuenta).");
            E(g, "imposible", "Una jugada imposible", "Golpe grave y blando, 0,34 s", "CalculoGameController.cs:566 (vol 0,8)", "calculo.thud", 0.8f, Wk(WoodSfx.Knock), "Knock");
            E(g, "quedo", "«Quedó X: deshaz y prueba otro camino»", "Golpe grave y blando, 0,34 s", "CalculoGameController.cs:597 (vol 0,6)", "calculo.thud", 0.6f, Wk(WoodSfx.Knock), "Knock, 3 dB más bajo").WoodGain = Atten(-3.0);
            E(g, "deshacer", "Deshacer o empezar de nuevo", "Soplo corto que baja, 0,3 s", "CalculoGameController.cs:627, :650 (vol 0,7)", "calculo.back", 0.7f, Wk(WoodSfx.Unselect), "Unselect");
            E(g, "armado", "«Empezar de nuevo» armado (primer toque)", "Soplo corto que baja, 0,3 s", "CalculoGameController.cs:640 (vol 0,4)", "calculo.back", 0.4f, Wk(WoodSfx.Unselect), "Unselect, 6 dB más bajo").WoodGain = Atten(-6.0);
            E(g, "pista", "La pista de Nubi", "Dos campanas (mi y la), 1,1 s", "CalculoGameController.cs:671 (vol 0,8)", "calculo.hint", 0.8f, Wk(WoodSfx.Hint), "Hint: soplo que sube y kalimba grave");
            E(g, "exacta", "La carga está exacta: arpegio", "Cuatro campanas que suben, 2 s, con eco", "CalculoGameController.cs:607 (vol 0,9)", "calculo.arpeggio", 0.9f, Wk(WoodSfx.Perfect), "Perfect", "", "Hoy es el sonido más fuerte de toda la app (−11 LUFS): con Madera queda a −19 LUFS (8 LU más bajo), a propósito.");
            E(g, "final", "Fin de la partida", "Ya no suena (el final lo suena ExitButton, uno por partida)", "CalculoGameController.cs:343", "calculo.finale", 0.8f, Wk(WoodSfx.Finish), "Finish (el común)");
        }

        // ------------------------------------------------------------------ Engranajes

        private static void RegisterEngranajes()
        {
            const string g = "engranajes";
            Family_("engranajes-pasos", "Engranajes: la nota de cada paso de la cascada", Hz(i => EngranajesSounds.Penta[Math.Min(9, i + 1)], 10), i => EngranajesSounds.Step(i), 0.45);
            Family_("engranajes-luces", "Engranajes: la luz del cohete que se enciende", Hz(i => EngranajesSounds.Penta[i], 10), i => EngranajesSounds.Light(i), 0.45);
            H("engranajes.motor", () => EngranajesSounds.MotorStart());
            H("engranajes.antenna", () => EngranajesSounds.Antenna());
            H("engranajes.turbine", () => EngranajesSounds.Turbine());
            H("engranajes.rack", () => EngranajesSounds.Rack());
            H("engranajes.thud", () => EngranajesSounds.Thud());
            H("engranajes.clank", () => EngranajesSounds.Clank());
            H("engranajes.press", () => EngranajesSounds.Press());
            H("engranajes.chime", () => EngranajesSounds.Chime());
            H("engranajes.success", () => EngranajesSounds.Success());
            H("engranajes.launch", () => EngranajesSounds.Launch());
            H("engranajes.finale", () => EngranajesSounds.Finale());

            E(g, "arrancar", "Tocar «Arrancar»", "Campanita (la nota 3), 0,4 s", "EngranajesGameController.cs:590 (vol 0,6)", "engranajes.press", 0.6f, Wk(WoodSfx.Tap), "Tap");
            E(g, "motor", "El motor arranca: zumbido grave", "Triángulo 70 → 95 Hz, 0,7 s", "EngranajesGameController.cs:184 (vol 0,6)", "engranajes.motor", 0.6f, Wk(WoodSfx.Hum), "Hum");
            FamilyEvent(g, "pasos", "Cada paso de la cascada: clic de dientes y una nota que sube", "Chasquido de 50 ms y campanita de la escala", "EngranajesGameController.cs:361 (vol 0,9)", "engranajes-pasos", 0.9f, "Select(k)", "La altura sube con la profundidad del paso.");
            E(g, "clank", "Un cambio (tocar el motor o una correa, o deshacer): «clank» de llave", "Golpe metálico corto y campanita aguda", "EngranajesGameController.cs:604 (vol 0,7)", "engranajes.clank", 0.7f, Wk(WoodSfx.Clack), "Clack");
            E(g, "cartel", "Un cartel que no cumplió / una jugada que no era", "Golpe grave y blando, 0,4 s", "EngranajesGameController.cs:369 (vol 0,8), :613 (vol 0,4)", "engranajes.thud", 0.8f, Wk(WoodSfx.Knock), "Knock");
            E(g, "antena", "La antena: silbido de viento y campana aguda", "Ruido 600 → 2600 Hz y campana, 1,2 s", "EngranajesGameController.cs:404 (vol 0,8)", "engranajes.antenna", 0.8f, null, "propio (Fase C)");
            E(g, "turbina", "La turbina: rugido que sube", "Ruido 180 → 900 Hz con retumbo, 1,3 s", "EngranajesGameController.cs:405 (vol 0,8)", "engranajes.turbine", 0.8f, null, "propio (Fase C)");
            E(g, "compuerta", "La compuerta y el elevador: motor eléctrico y golpe seco", "Diente de sierra suave 90 → 140 Hz y golpe, 1,1 s", "EngranajesGameController.cs:406 (vol 0,8)", "engranajes.rack", 0.8f, null, "propio (Fase C)");
            E(g, "acierto", "Acierto de la máquina: tres campanas que suben", "Tres campanas, 2,2 s, con eco", "EngranajesGameController.cs:376 (vol 0,8)", "engranajes.success", 0.8f, Wk(WoodSfx.Perfect), "Perfect");
            FamilyEvent(g, "luz", "La luz del cohete que se enciende (la nota sube con cada luz)", "Campanita de la escala (τ 0,22 s)", "EngranajesGameController.Scene.cs:808 (vol 0,8)", "engranajes-luces", 0.8f, "Spark(n)");
            E(g, "despegue", "El despegue: rugido que sube y cinco campanas", "3,8 s con eco", "EngranajesGameController.cs:422 (vol 0,9)", "engranajes.launch", 0.9f, null, "propio (Fase C): rugido de ruido + Record");
            E(g, "nuevo", "«NUEVO»", "Dos campanas (mi y la)", "EngranajesGameController.cs:315 (vol 0,7)", "engranajes.chime", 0.7f, Wk(WoodSfx.New), "New");
            E(g, "final", "Fin de la partida", "Tres campanas que suben, 2 s", "EngranajesGameController.cs:238 (vol 0,8)", "engranajes.finale", 0.8f, Wk(WoodSfx.Finish), "Finish");
        }

        // ------------------------------------------------------------------ Acoplamiento y Aterrizaje (ya en Madera cálida)

        private static void RegisterAcoplamiento()
        {
            const string g = "acoplamiento";
            foreach (DockSfx s in Enum.GetValues(typeof(DockSfx)))
            {
                var sfx = s;
                if (sfx == DockSfx.Slot) Raw("acoplamiento.slot", () => Scale(LadderSteps, 0.55, i => Trimmed(FromClip(DockingSounds.Get(DockSfx.Slot, i + 1)), 1.0)));
                else H("acoplamiento." + sfx.ToString().ToLowerInvariant(), () => DockingSounds.Get(sfx));
            }
            EW(g, "arrive", "El módulo se acerca", "Soplo de paso bajo 900 → 300 Hz", "DockingGameController.cs:217", "acoplamiento.arrive", 0.8f, "≈ SweepDown");
            EW(g, "tap", "Tocar «Encaja» o «Espejo»", "Marimba apagada de 60 ms", "DockingGameController.cs:503", "acoplamiento.tap", 0.8f, "= Tap");
            EW(g, "flip", "El módulo se da vuelta (espejo)", "Dos kalimbas que bajan", "DockingGameController.cs:298", "acoplamiento.flip", 0.8f, "propio");
            EW(g, "dock", "Encaja: golpe hueco con marimba", "Thump + marimba + bloque de madera", "DockingGameController.cs:304", "acoplamiento.dock", 0.8f, "propio");
            EW(g, "slot", "Se enciende una luz (la nota sube con la racha)", "Kalimba cuya nota sube", "DockingGameController.cs:350", "acoplamiento.slot", 0.8f, "≈ Correct(racha)");
            EW(g, "ring", "Anillo completo", "Arpegio de marimba y kalimba", "DockingGameController.cs:356", "acoplamiento.ring", 0.8f, "= Perfect");
            EW(g, "miss", "No era", "Dos marimbas graves que bajan", "DockingGameController.cs:312", "acoplamiento.miss", 0.8f, "= Wrong");
            EW(g, "finale", "Fin de la partida", "Acorde de marimba y kalimba larga", "DockingGameController.cs:390", "acoplamiento.finale", 0.8f, "= Finish");
        }

        private static void RegisterAterrizaje()
        {
            const string g = "aterrizaje";
            foreach (LandSfx s in Enum.GetValues(typeof(LandSfx))) { var sfx = s; H("aterrizaje." + sfx.ToString().ToLowerInvariant(), () => LandingSounds.Get(sfx)); }
            EW(g, "release", "Soltar la nave", "Soplo de paso bajo 1400 → 300 Hz", "LandingGameController.cs:259", "aterrizaje.release", 0.8f, "= SweepDown");
            EW(g, "touch", "Posarse", "Golpe grave suave y bloque de madera", "LandingGameController.cs:291", "aterrizaje.touch", 0.8f, "= Clack");
            EW(g, "flag", "Sube la bandera", "Kalimba (sol5)", "LandingGameController.cs:325; LandingGameController.Guided.cs:69", "aterrizaje.flag", 0.8f, "propio");
            EW(g, "hit", "Aterrizaje justo", "Dos marimbas (mi5 y la5)", "LandingGameController.cs:352", "aterrizaje.hit", 0.8f, "≈ Correct");
            EW(g, "bull", "Diana", "Arpegio de marimba y kalimba", "LandingGameController.cs:352", "aterrizaje.bull", 0.8f, "= Perfect");
            EW(g, "miss", "Quedó lejos", "Dos marimbas graves que bajan", "LandingGameController.cs:352", "aterrizaje.miss", 0.8f, "= Wrong");
            EW(g, "dome", "Se arma una cúpula", "Acorde de marimba y kalimba", "LandingGameController.cs:361", "aterrizaje.dome", 0.8f, "≈ Streak / LevelUp");
            EW(g, "finale", "Fin de la partida", "Acorde de marimba y kalimba larga", "LandingGameController.cs:389", "aterrizaje.finale", 0.8f, "= Finish");
            E(g, "propulsor", "El propulsor mientras baja (bucle de 4 s)", "Ruido café pasado por un paso bajo de 420 Hz; ya aprobado", "LandingGameController.cs:173; LandingGameController.Scene.cs:416", null, 1f, null, "SE QUEDA como está", "bucle");
            E(g, "practica", "La práctica usa los sonidos viejos", "GameFeel.Correct y Wrong (marimba y «bu-bum»)", "LandingGameController.Guided.cs:51, :57", "gamefeel.wrong", 0.5f, Wk(WoodSfx.Wrong), "Wrong", "", "Hoy la práctica del tutorial suena distinto del juego; la Fase B lo deja igual.");
        }

        // ------------------------------------------------------------------ En la punta de la lengua

        private static void RegisterPunta()
        {
            const string g = "anagramas";
            Family_("punta", "En la punta de la lengua: la nota de cada casilla (10)", Hz(i => PuntaSounds.Penta[i], 10), null, 0.4);
            Frozen("punta.appear");
            Frozen("punta.back");
            Frozen("punta.thud");
            Frozen("punta.help");
            Frozen("punta.chord");
            Frozen("punta.chime");
            Frozen("punta.finale");
            // la cascada del final: la palabra salió y cada letra se enciende con la nota siguiente, cada 0,09 s (hoy y Madera, con la caída de siempre y con una caída más corta por si se oye espesa)
            Frozen("punta.cascada.hoy");
            Raw("punta.cascada.madera", () => Scale(7, 0.09, i => FromRecipe(WoodSounds.MakeNote(PuntaSounds.Penta[i + 1], WoodVoice.Kalimba))));
            Raw("punta.cascada.madera.tau02", () => Scale(7, 0.09, i => FromRecipe(WoodSounds.MakeNote(PuntaSounds.Penta[i + 1], WoodVoice.Kalimba, 0.2))));
            FamilyEvent(g, "notas", "Cada ficha que se coloca suena la nota de su casilla", "Campana de la pentatónica (τ 0,22 s, 1,2 s)", "PuntaGameController.cs:514 (vol 0,8)", "punta", 0.8f, "PlayNote(Penta[k], Kalimba): 10 notas, do5 a la6",
                "La casilla k suena la nota k: el orden en que se arma la palabra es información (la altura no cambia).");
            var cascada = E(g, "cascada", "La palabra salió: cada letra se enciende con la nota siguiente (cada 0,09 s)", "Las mismas notas, una cada 0,09 s (hasta 7)", "PuntaGameController.cs:684 desde LightUp (:662, k · 0,09 s)", "punta.cascada.hoy", 0.8f, "punta.cascada.madera", "Las mismas 10 notas de kalimba, el mismo retardo k · 0,09 s (no se toca LightUp)", "info",
                "Si se oye espesa, la alternativa es pedir las notas de la cascada con una caída de 0,2 s (el botón verde). Con el acorde final encima (Perfect) es la misma escala y la misma sala.");
            cascada.WoodAltKey = "punta.cascada.madera.tau02";
            cascada.WoodAltLabel = "Madera (caída más corta)";
            E(g, "aparecen", "Aparecen las fichas", "Barrido de ruido que sube 600 → 2200 Hz, 0,55 s", "PuntaGameController.cs:463 (vol 0,6)", "punta.appear", 0.6f, Wk(WoodSfx.SweepUp), "SweepUp");
            E(g, "volver", "Una ficha vuelve al banco", "Soplo corto y bajo, 0,2 s", "PuntaGameController.cs:508, :523 (vol 0,5)", "punta.back", 0.5f, Wk(WoodSfx.Unselect), "Unselect");
            E(g, "casi", "«Casi»: golpe grave y blando", "Seno 220 → 130 Hz, 0,34 s", "PuntaGameController.cs:543 (vol 0,8)", "punta.thud", 0.8f, Wk(WoodSfx.Wrong), "Wrong");
            E(g, "ayuda", "Una ayuda: soplo que sube y campana grave", "Ruido 300 → 1800 Hz y campana de 392 Hz, 1 s", "PuntaGameController.cs:591 (vol 0,7)", "punta.help", 0.7f, Wk(WoodSfx.Hint), "Hint");
            E(g, "palabra", "La palabra salió: acorde que cierra", "Tres campanas que suben, 1,6 s", "PuntaGameController.cs:691 (vol 0,8)", "punta.chord", 0.8f, Wk(WoodSfx.Perfect), "Perfect");
            E(g, "luz", "El lucero llega al cielo", "Dos campanitas, 1,1 s", "PuntaGameController.cs:1436, :1466 (vol 0,5)", "punta.chime", 0.5f, Wk(WoodSfx.New), "New, 3 dB más bajo").WoodGain = Atten(-3.0);
            E(g, "final", "Fin de la partida", "Tres campanas que suben, lentas", "PuntaGameController.cs:713", "punta.finale", 0.8f, Wk(WoodSfx.Finish), "Finish (el común)");
        }

        // ------------------------------------------------------------------ Lluvia de meteoros

        private static void RegisterMeteoros()
        {
            const string g = "meteoros";
            H("meteoros.destruir", () => HarmonicTone.Build(330f, 0.12f, 0.03f));
            Raw("meteoros.destruir.madera", () => FromRecipe(WoodSounds.MakeCue(330, 0.12)));
            E(g, "destruir", "Se destruye un meteoro", "PlayTone: 330 Hz, 0,12 s, amplitud 0,03 (se suma al acierto común)", "MeteorGameController.cs:520, :1250", "meteoros.destruir", 1f, "meteoros.destruir.madera", "Cue(330)", "", "Es retroalimentación, no información: se evalúa en la Fase C si se queda o se quita (ya suena el Acierto común).");
            E(g, "listo", "Práctica: «¡Listo!»", "PlayTone: 523 Hz, 0,4 s", "MeteorGameController.cs:1212", "stroop.listo", 1f, Wk(WoodSfx.Notice), "Notice");
            E(g, "comun", "Acierto, error, subir de nivel y tic", "Comunes (GameFeel)", "MeteorGameController.cs:492, :499, :187, :466", "gamefeel.wrong", 0.5f, Wk(WoodSfx.Wrong), "Wrong (ver «Lo común»)");
        }

        // ------------------------------------------------------------------ ¿Verdad o disparate?

        private static void RegisterDisparate()
        {
            const string g = "disparate";
            H("disparate.tune", () => DisparateSounds.Tune());
            H("disparate.static", () => DisparateSounds.StaticClick());
            H("disparate.lost", () => DisparateSounds.Lost());
            H("disparate.wave", () => DisparateSounds.WaveRing());
            H("disparate.aurora", () => DisparateSounds.Aurora());
            H("disparate.unclear", () => DisparateSounds.Unclear());
            E(g, "sintonia", "Llega la frase: sintonía", "Barrido de ruido que sube y se aclara, 0,3 s", "DisparateGameController.cs:310 (vol 0,5)", "disparate.tune", 0.5f, Wk(WoodSfx.SweepUp), "SweepUp");
            E(g, "acierto", "Acierto: onda que sale de la antena (junto al acierto común)", "Soplo que sube y campanita, 0,45 s", "DisparateGameController.cs:587 (vol 0,45)", "disparate.wave", 0.45f, null, "propio: se suma al Acierto común; en la Fase C queda solo el Acierto");
            E(g, "error", "Error: chasquido de estática (junto al error común)", "Chasquido y golpe grave suave, 0,22 s", "DisparateGameController.cs:622 (vol 0,45)", "disparate.static", 0.45f, null, "propio: se suma al Error común; en la Fase C queda solo el Error");
            E(g, "perdida", "Se perdió la señal", "Ruido que se apaga y nota grave, 0,7 s", "DisparateGameController.cs:668 (vol 0,5)", "disparate.lost", 0.5f, Wk(WoodSfx.Wrong), "Wrong");
            E(g, "aurora", "Transmisión perfecta (10 seguidas)", "Cascada de campanas altas, 1,8 s", "DisparateGameController.cs:795 (vol 0,55)", "disparate.aurora", 0.55f, Wk(WoodSfx.Streak), "Streak");
            E(g, "aviso", "Gracias por avisar (frase poco clara)", "Dos marimbas suaves que suben", "DisparateGameController.cs:520 (vol 0,5)", "disparate.unclear", 0.5f, Wk(WoodSfx.Notice), "Notice");
            E(g, "comun", "Acierto, error, subir de nivel, tic y fin", "Comunes (GameFeel); no tiene Finale propio", "DisparateGameController.cs:586, :621, :292/:794, :927", "gamefeel.wrong", 0.5f, Wk(WoodSfx.Wrong), "Wrong (ver «Lo común»)");
        }

        // ------------------------------------------------------------------ La estrella intrusa

        private static void RegisterIntrusa()
        {
            const string g = "intrusa";
            Family_("intrusa-suave", "Intrusa: la chispa traza la figura (estrella sin palabra)", Hz(i => PuntaSounds.Penta[i], 9), i => IntrusaSounds.Note(i, false), 0.4);
            Family_("intrusa-fuerte", "Intrusa: la chispa traza la figura (estrella con palabra: más fuerte)", Hz(i => PuntaSounds.Penta[i], 9), i => IntrusaSounds.Note(i, true), 0.4);
            H("intrusa.chord", () => IntrusaSounds.Chord());
            H("intrusa.fall", () => IntrusaSounds.Fall());
            H("intrusa.glass", () => IntrusaSounds.Glass());
            H("intrusa.engrave", () => IntrusaSounds.Engrave());
            H("intrusa.named", () => IntrusaSounds.Named());
            H("intrusa.pulse", () => IntrusaSounds.Pulse());
            H("intrusa.dull", () => IntrusaSounds.Dull());
            FamilyEvent(g, "suave", "La chispa pasa por una estrella sin palabra: nota que sube", "Marimba suave (pico 0,3) y brillo; 0,4 s", "IntrusaGameController.cs:885 (vol 0,55)", "intrusa-suave", 0.55f, "Note(hz)", "");
            FamilyEvent(g, "fuerte", "La chispa pasa por una estrella CON palabra: nota más fuerte y brillante", "Marimba fuerte (pico 0,5) con campana una octava arriba; 0,55 s", "IntrusaGameController.cs:885 (vol 0,55)", "intrusa-fuerte", 0.55f, "Note(hz)", "Lo fuerte avisa que esa estrella lleva palabra: se mantiene la diferencia entre fuerte y suave.");
            E(g, "acorde", "Se cierra la figura: acorde de cuatro notas", "Cuatro campanas do-mi-sol-do con brillo, 1,3 s", "IntrusaGameController.cs:832, :871 (vol 0,35 a 0,65)", "intrusa.chord", 0.6f, Wk(WoodSfx.Perfect), "Perfect");
            E(g, "cae", "La estrella fugaz cae", "Soplo brillante que baja y campanita, 0,95 s", "IntrusaGameController.cs:758 (vol 0,55)", "intrusa.fall", 0.55f, Wk(WoodSfx.SweepDown), "SweepDown");
            E(g, "error", "Error: cristal suave y apagado", "Dos campanas graves que bajan, 0,6 s", "IntrusaGameController.cs:486 (vol 0,5)", "intrusa.glass", 0.5f, Wk(WoodSfx.Wrong), "Wrong");
            E(g, "grabado", "El grabado: barrido cálido que sube", "Ruido y seno 700 → 2500 Hz, 1,2 s", "IntrusaGameController.cs:982 (vol 0,4)", "intrusa.engrave", 0.4f, Wk(WoodSfx.SweepUp), "SweepUp");
            E(g, "une", "«¿Qué las une?» acertada", "Tres campanas que suben, 1 s", "IntrusaGameController.cs:438 (vol 0,6)", "intrusa.named", 0.6f, Wk(WoodSfx.Correct, 3), "Correct");
            E(g, "latido", "Latido de las cuatro estrellas mientras se piensa", "Golpecito grave suave, 0,35 s", "IntrusaGameController.cs:397 (vol 0,5)", "intrusa.pulse", 0.5f, Wk(WoodSfx.Tap), "Tap");
            E(g, "noera", "La opción no era: golpecito de madera sordo", "Seno 180 Hz, 0,25 s", "IntrusaGameController.cs:437 (vol 0,5)", "intrusa.dull", 0.5f, Wk(WoodSfx.Knock), "Knock");
            E(g, "comun", "Subir de nivel, tic y fin", "Comunes (GameFeel); no tiene Finale propio", "IntrusaGameController.cs:1055, :1171", "gamefeel.levelup", 0.5f, Wk(WoodSfx.LevelUp), "LevelUp (ver «Lo común»)");
        }

        // ------------------------------------------------------------------ las señales cortas (para medir su afinación)

        private static void RegisterSignalFamily()
        {
            var hz = new[] { 147.0, 196.0, 262.0, 330.0, 392.0, 440.0, 740.0, 784.0, 880.0, 988.0 };
            var fam = new Family { Id = "senales", Title = "Señales cortas (Tinta o Palabra, Freno, Piloto, Meteoros): tono armónico de 0,1 s de hoy contra marimba seca de 0,1 s", Hz = hz, Hoy = i => FromClip(HarmonicTone.Build((float)hz[i], 0.1f, 0.1f)) };
            Families.Add(fam);
        }
    }
}
