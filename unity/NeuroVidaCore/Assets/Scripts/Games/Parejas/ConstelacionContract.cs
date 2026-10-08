using System;
using System.Collections.Generic;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Parejas
{
    /// <summary>Las tarjetas «NUEVO» (una sola vez por instalación cada una): el cielo, los gemelos, los tríos, la mezcla y el cielo grande.</summary>
    public enum ConIntro { None, Cielo, Gemelos, Trios, Mezcla, Grande }

    /// <summary>Una etapa de la escalera (docs/diseno-constelaciones.md §4): cuántos grupos de objetos iguales hay, de cuántas luces es cada grupo (2 = pareja, 3 = trío), cuántos gemelos (cada gemelo son DOS grupos
    /// distintos: el objeto y su versión parecida), cuántos de los grupos que no son gemelos vienen de a tres (la mezcla) y qué tarjeta «NUEVO» trae.</summary>
    public readonly struct ConStage
    {
        public readonly int Groups, Size, Twins, Trios;
        public readonly ConIntro Intro;

        public ConStage(int groups, int size, int twins = 0, int trios = 0, ConIntro intro = ConIntro.None)
        {
            Groups = groups;
            Size = size;
            Twins = twins;
            Trios = trios;
            Intro = intro;
        }

        /// <summary>Cuántas luces tiene el cielo: cada grupo aporta su tamaño y los tríos de la mezcla aportan una más.</summary>
        public int Lights => Groups * Size + Trios;

        public bool Mixed => Trios > 0;
        public bool HasTwins => Twins > 0;
    }

    /// <summary>
    /// «Constelaciones» (id <c>parejas</c>, antes «Parejas Ocultas»; docs/diseno-constelaciones.md): las reglas puras. 18 etapas en 6 grupos de 3, una partida de 6 cielos, el motor común del DDA con cada
    /// OPORTUNIDAD DE MEMORIA como un ensayo, la medida «memoria de lugar» y todos los textos. Sin dependencias de la pantalla: el cielo (<see cref="ConstelacionSky"/>), el reparto de las luces
    /// (<see cref="ConstelacionLayout"/>) y las líneas (<see cref="ConstelacionRoute"/>) también son puros y tienen pruebas.
    /// </summary>
    public static class ConstelacionContract
    {
        public const string GameId = "parejas";

        /// <summary>Etapas de la escalera (el rating del motor común va de 1 a este número).</summary>
        public const int MaxLevel = 18;
        /// <summary>Cielos por partida en Precisión (en Reto se juega hasta que se acabe el tiempo).</summary>
        public const int Boards = 6;
        /// <summary>Etapas por grupo y grupos para la pantalla final («Etapa más alta: 3 de 6»).</summary>
        public const int StagesPerGroup = 3, GroupCount = 6;
        /// <summary>Duración del Reto, en segundos (se juegan cielos hasta que se acabe).</summary>
        public const float RetoSeconds = 180f;

        /// <summary>Paso de subida del motor común por oportunidad acertada: con el objetivo 0,80 cada «se te escapó» baja 4 veces eso. Un cielo con 0 o 1 oportunidades falladas sube o se queda y con 3 o más baja
        /// (lo que pide el diseño); un cielo sin oportunidades no mueve nada.</summary>
        public const float DdaStepUp = 0.2f;

        // ------------------------------------------------------------------ tiempos y medidas de toque (docs §3 y §5)

        /// <summary>Las dos luces abiertas que no coinciden se cierran solas a los 850 ms (o al instante si se toca otra).</summary>
        public const float CloseAfterMs = 850f;
        /// <summary>Tras un «se te escapó», a los 380 ms la compañera ya vista brilla 1,1 s (sin darse vuelta).</summary>
        public const float HintDelayMs = 380f, HintMs = 1100f;
        /// <summary>Una luz recién aparecida se puede tocar apenas termina de crecer un poco (150 ms): nada más la bloquea.</summary>
        public const float TapableAfterMs = 150f;
        /// <summary>Las luces aparecen escalonadas: 45 ms entre una y otra, cada una con 380 ms de escala.</summary>
        public const float AppearStepMs = 45f, AppearMs = 380f;

        // ------------------------------------------------------------------ las etapas

        private static readonly ConStage[] Stages =
        {
            // 1. parejas
            new ConStage(3, 2, intro: ConIntro.Cielo), new ConStage(4, 2), new ConStage(5, 2),
            // 2. cielo más lleno
            new ConStage(6, 2), new ConStage(7, 2), new ConStage(8, 2),
            // 3. gemelos
            new ConStage(6, 2, twins: 1, intro: ConIntro.Gemelos), new ConStage(7, 2, twins: 1), new ConStage(8, 2, twins: 2),
            // 4. tríos
            new ConStage(4, 3, intro: ConIntro.Trios), new ConStage(5, 3), new ConStage(5, 3, twins: 1),
            // 5. parejas y tríos juntos
            new ConStage(6, 2, trios: 2, intro: ConIntro.Mezcla), new ConStage(7, 2, twins: 1, trios: 2), new ConStage(8, 2, twins: 1, trios: 3),
            // 6. cielo grande
            new ConStage(10, 2, twins: 2, intro: ConIntro.Grande), new ConStage(11, 2, twins: 3), new ConStage(11, 2, twins: 3, trios: 2),
        };

        /// <summary>La etapa <paramref name="level"/> (1..18; fuera de rango se acota).</summary>
        public static ConStage Stage(int level) => Stages[Math.Max(1, Math.Min(MaxLevel, level)) - 1];

        /// <summary>El grupo de etapas (1..6) de un nivel: 3 etapas por grupo.</summary>
        public static int GroupOf(int level) => (Math.Max(1, Math.Min(MaxLevel, level)) - 1) / StagesPerGroup + 1;

        /// <summary>Las tarjetas «NUEVO» que trae la etapa (la de la etapa y las anteriores de la escalera que la persona nunca vio, por si empieza más arriba), de la más nueva a la más vieja no: en orden de aparición.</summary>
        public static IEnumerable<ConIntro> IntrosFor(int level)
        {
            int l = Math.Max(1, Math.Min(MaxLevel, level));
            for (int i = 1; i <= l; i++)
            {
                var intro = Stages[i - 1].Intro;
                if (intro != ConIntro.None) yield return intro;
            }
        }

        // ------------------------------------------------------------------ los objetos del espacio (docs §5)

        public const int ObjectKinds = 12;
        public static readonly string[] ObjectNames = { "planeta", "cohete", "cometa", "ovni", "cristal", "casco", "luna", "satélite", "telescopio", "asteroide", "antena", "brújula" };
        /// <summary>Los que tienen gemelo: planeta (sin anillo), cohete (con llama grande), ovni (con patas) y casco (con antena).</summary>
        public static readonly int[] TwinKinds = { 0, 1, 3, 5 };
        public static readonly string[] TwinNotes = { "sin anillo", "con llama grande", "con patas", "con antena" };

        public static bool HasTwin(int kind) => Array.IndexOf(TwinKinds, kind) >= 0;

        /// <summary>«el planeta», «el cohete»…: cómo se nombra un objeto (para lectores de pantalla).</summary>
        public static string ArticleName(int kind, int variant)
        {
            string[] art = { "el", "el", "el", "el", "el", "el", "la", "el", "el", "el", "la", "la" };
            string name = art[kind] + " " + ObjectNames[kind];
            int t = Array.IndexOf(TwinKinds, kind);
            return variant == 1 && t >= 0 ? name + " " + TwinNotes[t] : name;
        }

        // ------------------------------------------------------------------ sonido: la nota de cada grupo

        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f, 1567.98f, 1760f };

        /// <summary>La nota (índice de la pentatónica) del grupo <paramref name="group"/>: del 0 al 9 sube por la escala y del 10 en adelante repite una octava arriba.</summary>
        public static int NoteIndex(int group) => group % 10;
        public static int NoteOctave(int group) => group >= 10 ? 2 : 1;
        public static float NoteHz(int group) => Penta[NoteIndex(group)] * (NoteOctave(group) == 2 ? 2f : 1f);

        // ------------------------------------------------------------------ motor común

        /// <summary>El motor común: escalera 1..18, parte del rating guardado o del nivel elegido (o de la etapa pedida desde las herramientas de prueba), usa el perfil de edad y respeta el techo y el piso del modo.
        /// Sin tiempo de reacción: mirar con calma no se penaliza (la memoria no es una carrera).</summary>
        public static AdaptiveDifficulty CreateEngine(SequenceConfigDetails config) =>
            new AdaptiveDifficulty(MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.age_band), StartRating(config), DdaStepUp, useReaction: false);

        public static float StartRating(SequenceConfigDetails config) =>
            config.con_stage > 0 ? Math.Max(1f, Math.Min(MaxLevel, config.con_stage)) : AdaptiveDifficulty.StartRating(config, MaxLevel);

        // ------------------------------------------------------------------ puntaje y medidas (docs §7)

        /// <summary>Memoria de lugar, 0..100: aciertos de memoria sobre oportunidades. Sin oportunidades (el cielo se resolvió sin que hiciera falta recordar) no es una mala partida: 100.</summary>
        public static int Score(int hits, int opportunities) =>
            opportunities <= 0 ? 100 : Math.Max(0, Math.Min(100, (int)Math.Round(100.0 * Math.Max(0, Math.Min(hits, opportunities)) / opportunities)));

        /// <summary>El récord «mejor racha»: nunca baja.</summary>
        public static int NewRecord(int previousBest, int bestStreakToday) => Math.Max(Math.Max(0, previousBest), Math.Max(0, bestStreakToday));

        /// <summary>Telemetría de salida de la partida (el contrato es <see cref="CardsTelemetry"/>; <c>-1</c> = sin dato).</summary>
        public static CardsSessionMetrics BuildMetrics(AdaptiveDifficulty dda, ConTally tally, int bestRecord, bool newRecord, int score, SequenceConfigDetails config) => new CardsSessionMetrics
        {
            matched_pairs = tally.Groups,
            attempts = tally.Turns,
            calculated_score = score,
            level = config.level,
            timed = config.timed,
            end_rating = dda.RatingNormalized,
            peak_level = dda.PeakLevel,
            mode_trials = dda.ScoredTrials,
            mode_hits = dda.ScoredCorrect,
            con_hits = tally.Hits,
            con_opps = tally.Opportunities,
            con_groups = tally.Groups,
            con_mem_groups = tally.MemGroups,
            con_best_streak = tally.BestStreak,
            con_useless = tally.Useless,
            con_turns = tally.Turns,
            con_group = tally.Boards > 0 ? GroupOf(tally.PeakLevel) : -1,
            con_best = bestRecord,
            con_new = newRecord ? 1 : 0,
        };

        // ------------------------------------------------------------------ textos (todos de 14 dp o más)

        public const string CardDoneTitle = "¡Constelación completa!";
        public const string LuckyLine = "Todas a la primera vista: ¡qué suerte!";
        public const string MissTitle = "Se te escapó: brilla dónde estaba";
        public const string MissSub = "Ya la habías visto: la próxima vez, directo";
        public const string MemoryLabel = "De memoria";
        public const string MemoryNone = "aún nada que recordar";
        public const string FloatMemory = "¡De memoria!", FloatFound = "¡Encontrada!", FloatTrio = "¡Trío!", FloatThird = "¡Falta la tercera!";
        public const string Streak = "Racha de memoria";
        public const string TapToStart = "Toca para empezar";
        public const string GoldLegend = "Línea dorada: la recordaste";

        public static string SkyOf(int board, int boards) => boards > 0 ? "Cielo " + board + " de " + boards : "Cielo " + board;

        public static string BestStreakLine(int best) => best > 0 ? "Mejor racha: " + best : "";

        public static string MemoryCount(int hits, int opps) => opps > 0 ? hits + " de " + opps : MemoryNone;

        public static string DoneLine(int hits, int opps, bool recordStreak) =>
            opps > 0 ? hits + " de " + opps + " de memoria" + (recordStreak ? " · ¡racha récord!" : "") : LuckyLine;

        /// <summary>«2 parejas», «1 trío», «2 parejas y 1 trío»: lo que falta del cielo.</summary>
        public static string Missing(int pairs, int trios)
        {
            var parts = new List<string>();
            if (pairs > 0) parts.Add(pairs + (pairs == 1 ? " pareja" : " parejas"));
            if (trios > 0) parts.Add(trios + (trios == 1 ? " trío" : " tríos"));
            return string.Join(" y ", parts);
        }

        public static string CardTitle(ConStage st) =>
            st.HasTwins ? (st.Mixed ? "Gemelos, parejas y tríos" : "Ojo con los gemelos")
            : st.Mixed ? "Parejas y tríos"
            : st.Size == 3 ? "Busca tres iguales"
            : "Busca las parejas";

        public static string CardSub(ConStage st, int pairsLeft, int triosLeft)
        {
            string left = Missing(pairsLeft, triosLeft);
            return st.HasTwins && !st.Mixed ? "Solo se unen los idénticos · faltan " + left : "Faltan " + left;
        }

        private static readonly string[] Tips =
        {
            "Truco: da vuelta primero una luz nueva",
            "Truco: di su nombre en voz baja al verla",
            "Truco: fíjate en qué parte del cielo está"
        };

        public static IEnumerable<string> AllTips() => Tips;

        /// <summary>El consejo del final: rota con el cielo; el primero es el de la medida («da vuelta primero una luz nueva»).</summary>
        public static string Tip(int board) => Tips[((board % Tips.Length) + Tips.Length) % Tips.Length];

        public static string IntroTag(ConIntro intro) => intro == ConIntro.Cielo ? "CONSTELACIONES" : "NUEVO";

        public static string[] IntroLines(ConIntro intro)
        {
            switch (intro)
            {
                case ConIntro.Cielo: return new[] { "Cada luz esconde un objeto.", "Da vuelta dos: si son iguales,", "quedan unidas con luz." };
                case ConIntro.Gemelos: return new[] { "¡Llegan gemelos parecidos!", "Mira bien el detalle:", "solo se unen los idénticos." };
                case ConIntro.Trios: return new[] { "Ahora son tríos.", "Da vuelta tres iguales", "para unirlos." };
                case ConIntro.Mezcla: return new[] { "Parejas y tríos juntos.", "Si dos iguales quedan abiertas,", "busca la tercera." };
                case ConIntro.Grande: return new[] { "¡Cielo grande!", "Más luces que nunca:", "recórrelo por partes." };
                default: return new string[0];
            }
        }
    }

    /// <summary>Las cuentas de la partida entera (todos los cielos): lo que se manda al terminar. Pura, con pruebas.</summary>
    public sealed class ConTally
    {
        public int Boards, Turns, Hits, Opportunities, Groups, MemGroups, Useless, BestStreak, PeakLevel = 1;

        /// <summary>Suma lo ocurrido en un cielo ya terminado (o cortado por el tiempo del Reto).</summary>
        public void AddBoard(ConstelacionSky sky, int level)
        {
            Boards++;
            Turns += sky.Turns;
            Hits += sky.Hits;
            Opportunities += sky.OpportunityCount;
            Groups += sky.GroupsFound;
            MemGroups += sky.MemoryGroups;
            Useless += sky.Useless;
            BestStreak = Math.Max(BestStreak, sky.BestStreak);
            PeakLevel = Math.Max(PeakLevel, level);
        }

        public int Percent => Opportunities <= 0 ? -1 : (int)Math.Round(100.0 * Hits / Opportunities);
    }
}
