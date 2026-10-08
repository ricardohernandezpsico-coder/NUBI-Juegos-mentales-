using System;
using System.Collections.Generic;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Correo
{
    /// <summary>Los cuatro momentos del día (el centro de la ventana de un encargo por hora).</summary>
    public enum MailMoment { Manana, Mediodia, Tarde, Noche }

    /// <summary>Las tarjetas «NUEVO» (una sola vez por instalación cada una): la estación, la hora, lo de todos los días, lo que cancela la radio y el lazo.</summary>
    public enum MailIntro { None, Estacion, Hora, Rutina, Cancela, Lazo }

    /// <summary>Una etapa de la escalera (docs/diseno-correo-estacion.md §4): cuántos buzones hay, cada cuántos segundos llega una carta, cuántas cartas señal con sello dorado y con lazo trae cada día, los encargos por hora,
    /// si el primero se repite todos los días, si la radio puede cancelar uno, qué tan ancha es la ventana de la hora (fracción del día) y qué tarjeta «NUEVO» trae.</summary>
    public readonly struct MailStage
    {
        public readonly int Boxes, Gold, Lazo;
        public readonly float Every, Win;
        public readonly MailMoment[] Times;
        public readonly bool Routine, Cancel;
        public readonly MailIntro Intro;

        public MailStage(int boxes, float every, int gold, int lazo, MailMoment[] times, bool routine, bool cancel, float win, MailIntro intro)
        {
            Boxes = boxes;
            Every = every;
            Gold = gold;
            Lazo = lazo;
            Times = times ?? new MailMoment[0];
            Routine = routine;
            Cancel = cancel;
            Win = win;
            Intro = intro;
        }
    }

    /// <summary>
    /// «La estación de correo» (id <c>correo</c>; docs/diseno-correo-estacion.md, boceto docs/previews/correo-estacion-boceto.html versión 2): las reglas puras de las etapas, los encargos, el plan del día y las medidas.
    /// Las cartas llegan por una cinta y se tocan en el buzón del planeta de su sello (la tarea de fondo); los encargos del día se dan en la mañana y NO se ven durante el día: por evento («si llega una carta con sello
    /// dorado o con lazo, a la caja fuerte») y por hora («al mediodía, enciende el faro», con el reloj tapado). Memoria prospectiva (Tse et al., 2022; Rose et al., 2015; Henry et al., 2021; Peper y Ball, 2023).
    /// Sin conducción, sin pedidos con componentes ni tiempos de entrega, sin vías ni desvíos (docs §8: patentes de Akili/UCSD y de Lumos Labs).
    /// </summary>
    public static class MailContract
    {
        public const string GameId = "correo";

        /// <summary>Etapas de la escalera (el rating del motor común va de 1 a este número).</summary>
        public const int MaxLevel = 10;
        /// <summary>Un día dura 50 s; una partida son 4 días; el día de práctica del tutorial dura 30 s.</summary>
        public const float DaySeconds = 50f, PracticeSeconds = 30f;
        public const int Days = 4;
        /// <summary>Paso de subida del motor común por encargo cumplido (cada encargo es un ensayo; con el objetivo 0,80 cada encargo fallado baja 4 veces eso).</summary>
        public const float DdaStepUp = 0.3f;

        // ------------------------------------------------------------------ tiempos (docs §2, §5 y §11)

        /// <summary>El reloj tapado se destapa 1,6 s al tocarlo; la radio dura 3,2 s; el faro encendido a tiempo dura 3,4 s.</summary>
        public const float PeekSeconds = 1.6f, RadioSeconds = 3.2f, ShowSeconds = 3.4f;
        /// <summary>Un saco trae 5 cartas seguidas, una cada 0,85 s; mientras dura la cinta aguanta 7.</summary>
        public const int RushLetters = 5;
        public const float RushEvery = 0.85f, RushStartDelay = 0.6f;
        public const int BeltCapacity = 3, RushBeltCapacity = 7;
        /// <summary>Una carta de la cinta se puede tocar cuando ya llegó a su lugar (a 40 dp o menos de él).</summary>
        public const float ReadyDistance = 40f;
        /// <summary>El 30 % del día antes de la ventana cuenta como «cerca de la hora» para mirar el reloj.</summary>
        public const float NearHourBefore = 0.30f;
        /// <summary>Lo que se avanza o retrocede cada día (docs §4): todos los encargos y esta clasificación sube; menos de la mitad de los encargos baja.</summary>
        public const float UpAccuracy = 0.85f, DownShare = 0.5f;
        /// <summary>Racha: cada 10 cartas bien puestas seguidas es un escalón (×10, ×20, ×30 o más).</summary>
        public const int ComboStep = 10;

        // ------------------------------------------------------------------ los momentos del día (docs §4)

        public static float MomentCenter(MailMoment m)
        {
            switch (m)
            {
                case MailMoment.Manana: return 0.26f;
                case MailMoment.Mediodia: return 0.50f;
                case MailMoment.Tarde: return 0.70f;
                default: return 0.88f;
            }
        }

        /// <summary>«a media mañana», «al mediodía», «a media tarde», «al anochecer»: cómo se dice el momento en una frase.</summary>
        public static string MomentPhrase(MailMoment m)
        {
            switch (m)
            {
                case MailMoment.Manana: return "a media mañana";
                case MailMoment.Mediodia: return "al mediodía";
                case MailMoment.Tarde: return "a media tarde";
                default: return "al anochecer";
            }
        }

        /// <summary>La palabra corta de la barra del día.</summary>
        public static string MomentWord(MailMoment m)
        {
            switch (m)
            {
                case MailMoment.Manana: return "mañana";
                case MailMoment.Mediodia: return "mediodía";
                case MailMoment.Tarde: return "tarde";
                default: return "noche";
            }
        }

        public static readonly MailMoment[] AllMoments = { MailMoment.Manana, MailMoment.Mediodia, MailMoment.Tarde, MailMoment.Noche };

        // ------------------------------------------------------------------ las etapas (docs §4)

        private static MailMoment[] T(params MailMoment[] m) => m;
        private const MailMoment Ma = MailMoment.Manana, Me = MailMoment.Mediodia, Ta = MailMoment.Tarde, No = MailMoment.Noche;

        private static readonly MailStage[] Stages =
        {
            new MailStage(2, 2.6f, 2, 0, T(), false, false, 0.12f, MailIntro.Estacion),
            new MailStage(3, 2.4f, 2, 0, T(Me), false, false, 0.12f, MailIntro.Hora),
            new MailStage(3, 2.3f, 2, 0, T(Me), true, false, 0.12f, MailIntro.Rutina),
            new MailStage(3, 2.1f, 2, 0, T(Ta), true, true, 0.12f, MailIntro.Cancela),
            new MailStage(4, 2.1f, 0, 2, T(Me), true, false, 0.12f, MailIntro.Lazo),
            new MailStage(4, 2.0f, 1, 2, T(Ta), true, true, 0.12f, MailIntro.None),
            new MailStage(4, 1.9f, 0, 2, T(Ma, Ta), true, false, 0.10f, MailIntro.None),
            new MailStage(4, 1.8f, 1, 2, T(Ma, Ta), true, true, 0.10f, MailIntro.None),
            new MailStage(4, 1.7f, 0, 3, T(Me, No), true, true, 0.09f, MailIntro.None),
            new MailStage(4, 1.6f, 1, 3, T(Ma, Ta, No), true, true, 0.08f, MailIntro.None),
        };

        /// <summary>La etapa <paramref name="level"/> (1..10; fuera de rango se acota).</summary>
        public static MailStage Stage(int level) => Stages[Math.Max(1, Math.Min(MaxLevel, level)) - 1];

        /// <summary>El grupo de etapas (1..5) de un nivel: 2 etapas por grupo.</summary>
        public static int GroupOf(int level) => (Math.Max(1, Math.Min(MaxLevel, level)) - 1) / 2 + 1;
        public const int GroupCount = 5;

        /// <summary>Cuántos sacos caen en un día: 1 hasta la etapa 4 y 2 desde la 5.</summary>
        public static int RushCount(int level) => level >= 5 ? 2 : 1;

        /// <summary>Las tarjetas «NUEVO» que trae la etapa (y las anteriores que la persona nunca vio, por si empieza más arriba), en orden de aparición.</summary>
        public static IEnumerable<MailIntro> IntrosFor(int level)
        {
            int l = Math.Max(1, Math.Min(MaxLevel, level));
            for (int i = 1; i <= l; i++)
            {
                var intro = Stages[i - 1].Intro;
                if (intro != MailIntro.None) yield return intro;
            }
        }

        // ------------------------------------------------------------------ los buzones: un planeta con su figura neutra (docs §5)

        public const int PlanetCount = 4;
        public static readonly string[] PlanetNames = { "Coralia", "Celesta", "Lima", "Uva" };
        /// <summary>La figura de cada planeta: círculo, triángulo, cuadrado y gota (sin estrellas de puntas, cruces ni medias lunas).</summary>
        public static readonly string[] PlanetShapes = { "círculo", "triángulo", "cuadrado", "gota" };
        /// <summary>La nota de cada buzón (do, mi, sol y do alto).</summary>
        public static readonly float[] PlanetNotes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f, 1567.98f, 1760f };

        // ------------------------------------------------------------------ motor común

        /// <summary>El motor común: escalera 1..10, parte del rating guardado (o de la etapa pedida desde las herramientas de prueba), usa el perfil de edad y respeta el techo y el piso del modo. Cada ENCARGO es un
        /// ensayo de memoria prospectiva; la clasificación de las cartas es la tarea de fondo y no mueve el rating. Sin tiempo de reacción: mirar con calma no se penaliza.</summary>
        public static AdaptiveDifficulty CreateEngine(SequenceConfigDetails config) =>
            new AdaptiveDifficulty(MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.age_band), StartRating(config), DdaStepUp, useReaction: false);

        public static float StartRating(SequenceConfigDetails config) =>
            config.mail_stage > 0 ? Math.Max(1f, Math.Min(MaxLevel, config.mail_stage)) : AdaptiveDifficulty.StartRating(config, MaxLevel);

        /// <summary>La etapa con que empieza la partida (el nivel real del rating de partida, 1..10).</summary>
        public static int StartLevel(AdaptiveDifficulty dda) => Math.Max(1, Math.Min(MaxLevel, dda.Level));

        /// <summary>Lo que pasa al terminar un día (docs §4): con todos los encargos cumplidos y al menos 85 % de las cartas bien puestas sube una etapa; con menos de la mitad de los encargos baja una; si no, se queda.
        /// <paramref name="floor"/> y <paramref name="ceiling"/> son los límites del modo (Suave, Desafío, Experto).</summary>
        public static int Advance(int level, int ok, int all, float accuracy, int floor = 1, int ceiling = MaxLevel)
        {
            int next = level;
            if (all > 0)
            {
                if (ok == all && accuracy >= UpAccuracy) next = level + 1;
                else if ((float)ok / all < DownShare) next = level - 1;
            }
            floor = Math.Max(1, Math.Min(MaxLevel, floor));
            ceiling = Math.Max(floor, Math.Min(MaxLevel, ceiling));
            return Math.Max(floor, Math.Min(ceiling, next));
        }

        /// <summary>Si el día subió (para el «Mañana: un poco más difícil» del resumen).</summary>
        public static bool DayWasUp(int ok, int all, float accuracy) => all > 0 && ok == all && accuracy >= UpAccuracy;

        // ------------------------------------------------------------------ medidas (docs §7)

        /// <summary>«Tu memoria para lo pendiente»: encargos cumplidos sobre encargos (los cancelados que no hiciste cuentan como cumplidos). null sin encargos.</summary>
        public static int? MemoryPercent(int ok, int all) => all <= 0 ? (int?)null : (int)Math.Round(100.0 * Math.Max(0, Math.Min(ok, all)) / all);

        /// <summary>El puntaje de la partida (0..100): la memoria para lo pendiente; sin encargos que medir, 100.</summary>
        public static int Score(int ok, int all) => MemoryPercent(ok, all) ?? 100;

        /// <summary>El récord «cartas en un día perfecto»: nunca baja.</summary>
        public static int NewRecord(int previous, int perfectDayLetters) => Math.Max(Math.Max(0, previous), Math.Max(0, perfectDayLetters));

        /// <summary>Telemetría de salida de la partida (<c>-1</c> = sin dato; reemplaza a los <c>mail_*</c> del vuelo).</summary>
        public static StroopSessionMetrics BuildMetrics(AdaptiveDifficulty dda, MailTally t, int bestRecord, bool newRecord, int maxLevelReached, SequenceConfigDetails config)
        {
            int all = t.MeasureAll, ok = t.MeasureOk;
            return new StroopSessionMetrics
            {
                correct_trials = ok,
                total_trials = all,
                calculated_score = Score(ok, all),
                average_response_time_ms = 0,
                level = config.level,
                timed = config.timed,
                end_rating = dda.RatingNormalized,
                peak_level = Math.Max(dda.PeakLevel, maxLevelReached),
                mode_trials = dda.ScoredTrials,
                mode_hits = dda.ScoredCorrect,
                mail_ev_hits = t.EventsOk,
                mail_ev_total = t.Events,
                mail_time_hits = t.TimesOk,
                mail_time_total = t.Times,
                mail_cancels = t.Cancels,
                mail_commissions = t.Commissions,
                mail_early = t.Early,
                mail_peeks = t.Peeks,
                mail_peeks_good = t.GoodPeeks,
                mail_right = t.Right,
                mail_sorted = t.Sorted,
                mail_best_combo = t.BestCombo,
                mail_days_perfect = t.PerfectDays,
                mail_group = GroupOf(maxLevelReached),
                mail_best = bestRecord,
                mail_new = newRecord ? 1 : 0,
            };
        }

        // ------------------------------------------------------------------ textos (todos de 14 dp o más)

        public const string TitleRun = "La estación de correo";
        public const string CountdownSub = "Clasifica las cartas y cumple los encargos";
        public const string BriefTitle = "Encargos de hoy";
        public const string BriefStart = "Empezar el día";
        public const string BriefNote1 = "Durante el día no los verás.";
        public const string BriefNote2 = "Dilo en voz baja: «cuando vea…, haré…»";
        public const string RoutineTag = "TODOS LOS DÍAS";
        public const string RoutineTitle = "Y lo de todos los días";
        public const string RoutineSub = "(ya no se anota: acuérdate tú)";
        public const string PlayHint = "Toca el buzón del sello";
        public const string TapToContinue = "Toca para seguir";
        public const string ClockLabel = "reloj";
        public const string ClockPanel = "Hora del día";
        public const string RadioTag = "RADIO";
        public const string RushTitle = "¡Llega un saco!";
        public const string RushSub = "5 cartas seguidas: ¡rápido!";
        public const string SafeLabelA = "Caja", SafeLabelB = "fuerte", BeaconLabel = "Faro";

        public const string FloatWrongBox = "Otro buzón";
        public const string FloatCueDone = "¡Encargo cumplido!";
        public const string FloatGoldMissed = "Era con sello dorado: iba a la caja fuerte";
        public const string FloatLazoMissed = "Tenía lazo: iba a la caja fuerte";
        public const string FloatNotSafe = "Esa carta no va a la caja fuerte";
        public const string FloatBeaconOn = "¡Faro encendido a tiempo!";
        public const string FloatTooEarly = "Aún no es la hora";
        public const string FloatNotNow = "Ahora no toca el faro";
        public const string FloatCancelled = "¡Estaba cancelado!";
        public const string FloatShipArrived = "¡La nave del correo llegó!";
        public static string FloatLateWindow(MailMoment m) => "Se pasó la hora: " + MomentPhrase(m);
        public static string FloatCombo(int combo)
        {
            int tier = combo / ComboStep;
            return tier <= 1 ? "¡Racha ×" + combo + "!" : tier == 2 ? "¡Imparable! ×" + combo : "¡Maestro del correo! ×" + combo;
        }
        public static string RadioText(MailMoment m) => "hoy NO hace falta encender el faro " + MomentPhrase(m);

        public static string DayTitle(int day) => "DÍA " + day + " DE " + Days;
        public static string DayHud(int day) => "Día " + day + " de " + Days;
        public static string CartasLine(int right, int late) => "Cartas: " + right + (late > 0 ? " · atrasadas: " + late : "");
        public static string RecapTag(int day) => "FIN DEL DÍA " + day;
        public static string RecapTitle(int ok, int all) => ok == all ? "¡Todos los encargos!" : "Encargos: " + ok + " de " + all;
        public static string RecapCards(int right, int total, int bestCombo) => "Cartas bien puestas: " + right + " de " + total + " · racha mayor ×" + bestCombo;
        public static string RecapClock(int peeks, int good) => "Reloj: lo miraste " + peeks + (peeks == 1 ? " vez" : " veces") + ", " + good + " cerca de la hora";
        public const string RecapRecord = "¡Nuevo récord de cartas en un día perfecto!";
        public const string RecapUp = "Mañana: un poco más difícil", RecapSame = "Mañana: mismo ritmo";
        public static string RecapNext(int day) => day >= Days ? "Ver resultado" : "Siguiente día";

        public static string CueLineA(MailCue cue) => cue == MailCue.Gold ? "Si llega una carta con sello dorado," : "Si llega una carta con lazo,";
        public const string CueLineB = "guárdala en la caja fuerte";
        public static string TimeLineA(MailMoment m)
        {
            string p = MomentPhrase(m);
            return char.ToUpperInvariant(p[0]) + p.Substring(1) + ",";
        }
        public const string TimeLineB = "enciende el faro";

        public static string ItemGold = "Sello dorado: caja fuerte", ItemLazo = "Carta con lazo: caja fuerte";
        public static string ItemTime(MailMoment m, bool routine) => "Faro " + MomentPhrase(m) + (routine ? " · de todos los días" : "");
        public static string ItemCancelled(MailMoment m) => "Faro " + MomentPhrase(m) + " (cancelado)";
        public const string DetHit = "a tiempo: la nave del correo llegó", DetMiss = "se pasó la hora: la nave no llegó";
        public const string DetCommission = "lo hiciste igual", DetNoCommission = "no lo hiciste: bien";
        public static string DetCount(int ok, int all) => ok + " de " + all;

        public static string IntroTag(MailIntro intro) => intro == MailIntro.Estacion ? "CORREO ESTELAR" : "NUEVO";

        public static string[] IntroLines(MailIntro intro)
        {
            switch (intro)
            {
                case MailIntro.Estacion: return new[] { "Toca el buzón del sello de cada carta.", "Y cumple los encargos del día:", "nadie te los va a recordar." };
                case MailIntro.Hora: return new[] { "Encargos con hora.", "El faro guía la nave del correo:", "enciéndelo a la hora justa.", "El reloj va tapado: tócalo para mirarla." };
                case MailIntro.Rutina: return new[] { "Lo de todos los días.", "Un encargo que se repite cada día", "y que ya no se vuelve a anotar." };
                case MailIntro.Cancela: return new[] { "A veces la radio cancela un encargo.", "Si lo cancela,", "acuérdate de NO hacerlo." };
                case MailIntro.Lazo: return new[] { "Ahora la señal es un lazo,", "no el sello.", "Mira la carta entera." };
                default: return new string[0];
            }
        }

        public static readonly string[] Tips =
        {
            "Truco: repite «cuando vea un lazo, caja fuerte»",
            "Truco: mira el reloj cuando se acerque la hora",
            "Truco: imagínate haciendo el encargo",
        };

        /// <summary>El consejo del final: rota con el día y los encargos por evento (docs §7).</summary>
        public static string Tip(int salt) => Tips[((salt % Tips.Length) + Tips.Length) % Tips.Length];
    }

    /// <summary>Lo que se acumula en toda la partida (suma de los días): la medida final (docs §7).</summary>
    public sealed class MailTally
    {
        public int Days, PerfectDays;
        public int Events, EventsOk, Times, TimesOk, Cancels, Commissions, Early;
        public int Peeks, GoodPeeks;
        public int Right, Sorted, Late, BestCombo;

        /// <summary>Encargos medidos y cumplidos: por evento, por hora y los cancelados (los que NO se hicieron cuentan como cumplidos).</summary>
        public int MeasureAll => Events + Times + Cancels;
        public int MeasureOk => EventsOk + TimesOk + (Cancels - Commissions);

        public void Add(MailDayStat d)
        {
            Days++;
            if (d.Perfect) PerfectDays++;
            Events += d.Events; EventsOk += d.EventsOk;
            Times += d.TimesAll; TimesOk += d.TimesOk;
            Cancels += d.Cancels; Commissions += d.Commissions;
            Early += d.Early;
            Peeks += d.Peeks; GoodPeeks += d.GoodPeeks;
            Right += d.Right; Sorted += d.Sorted; Late += d.Late;
            BestCombo = Math.Max(BestCombo, d.BestCombo);
        }

        public int? Percent => MailContract.MemoryPercent(MeasureOk, MeasureAll);
    }
}
