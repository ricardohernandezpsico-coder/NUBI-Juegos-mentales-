using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NeuroVida.Games.Acoplamiento;
using NeuroVida.Games.Disparate;
using NeuroVida.Games.Radar;
using NeuroVida.Games.Shared;
using NeuroVida.Games.Stroop;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// Los ROBOTS POR EDAD del Contrarreloj (Tarea 204; diseño de Fable en <c>auditoria-fable/referencia-estimada.md</c> §2.2 y §2.3, decisión de Ricardo del 1-nov): una tercera clase de robot sobre el andamio de <see cref="TimeTrialSimulation"/> —la MISMA pista real, con la semilla del día, los generadores sembrados,
    /// el banco de frases real y las constantes de los contratos— cuyo comportamiento sale de estudios por edad y no de dos supuestos fijos como «azar» y «cuidado». Cada robot es una PERSONA con tres números propios sorteados de su franja (lentitud, exactitud en lo difícil y objetos de un vistazo) y después juega la pista
    /// con vaivén ensayo a ensayo. De cada persona se guarda su mejor marca de la semana (lo que la página compara); de muchas personas sale la tabla de la <b>referencia estimada</b>: tramos por juego y franja, que la app lee de un JSON dentro del APK. Nada de esto son personas reales: la página lo dice siempre.
    /// Lo que los estudios fijan es <i>cuánto cambia con la edad</i> (la forma y la distancia entre franjas, <see cref="Slowness"/> y compañía); la ESCALA absoluta en el teléfono —cuánto tarda una persona joven en tocar— no sale de ningún estudio y es el único número que se va a calibrar: <see cref="ReferenceScales"/>.
    /// Sin UnityEngine y con SplitMix64 (<see cref="TimeTrial.TrackRandom"/>, no <c>System.Random</c>): la misma semilla da SIEMPRE la misma tabla, también entre versiones de Unity.
    /// </summary>
    public static class AgeReference
    {
        /// <summary>La versión del MODELO (los parámetros y las reglas de abajo): si cambian, se sube y el JSON la lleva.</summary>
        public const int ModelVersion = 1;

        // ------------------------------------------------------------------ las franjas (sin la de 13 a 17 años: no se compara; `Features.COMPARE_MINORS`)

        /// <summary>Las claves de las franjas, como se llaman en la app (<c>AgeRange</c> de Kotlin, de 18 a 29 hasta 75 o más).</summary>
        public static readonly string[] RangeKeys = { "R18_29", "R30_44", "R45_54", "R55_64", "R65_74", "R75_PLUS" };
        /// <summary>Cómo se dicen, para el informe.</summary>
        public static readonly string[] RangeLabels = { "18 a 29", "30 a 44", "45 a 54", "55 a 64", "65 a 74", "75 o más" };

        // §2.2 de Fable. Son INTERPOLACIONES entre estudios de laboratorio (Brinley, Cerella 1985, Myerson 1990, Salthouse 1996, Der y Deary 2006, Hultsch 2002, Ratcliff 2010, Van der Elst 2006, Troyer 2006, Brockmole y Logie 2013, Habekost 2013), no cifras publicadas por franja.
        /// <summary>Factor de lentitud (mediana, relativo a 18 a 29).</summary>
        public static readonly double[] Slowness = { 1.00, 1.06, 1.14, 1.24, 1.40, 1.62 };
        /// <summary>Dispersión ENTRE personas del factor de lentitud (sigma lognormal): crece con la media y con la edad.</summary>
        public static readonly double[] Dispersion = { 0.18, 0.19, 0.20, 0.22, 0.25, 0.28 };
        /// <summary>Exactitud media en el ensayo difícil (el SD entre personas es <see cref="AccuracySd"/>).</summary>
        public static readonly double[] Accuracy = { 0.95, 0.95, 0.94, 0.93, 0.91, 0.89 };
        public const double AccuracySd = 0.04;
        /// <summary>Objetos de un vistazo (media; SD <see cref="ObjectsSd"/>): solo importa en Rescate relámpago.</summary>
        public static readonly double[] Objects = { 3.6, 3.4, 3.2, 3.0, 2.7, 2.4 };
        public const double ObjectsSd = 0.6;
        /// <summary>De 65 años en adelante el controlador de Tinta o Palabra deja llegar la tarjeta 1,3 veces más despacio (<see cref="StroopContract.SeniorArrivalFactor"/>), también en el Contrarreloj.</summary>
        public static readonly bool[] Senior = { false, false, false, false, true, true };
        /// <summary>El vaivén de una persona de un ensayo a otro (sigma lognormal), el mismo del robot de «cuidado».</summary>
        public const double Within = 0.25;

        // ------------------------------------------------------------------ las medianas de una persona joven (escala 1,0): §2.3 de Fable

        /// <summary>Tinta o Palabra: respuesta en la palabra que choca (s); coincide = ×0,85; +0,25 s cuando la regla acaba de cambiar.</summary>
        public const double StroopResponse = 1.25, StroopCongruentFactor = 0.85, StroopSwitchExtra = 0.25;
        /// <summary>¿Verdad o disparate?: leer = 0,7 s + 0,30 s por palabra.</summary>
        public const double DisparateBase = 0.7, DisparatePerWord = 0.30;
        /// <summary>Rescate relámpago: respuesta (s) + 0,25 s por cápsula; τ de encodificación 120 ms; una roca se marca de más con 12 %; lo no visto se adivina con 35 %.</summary>
        public const double RadarResponse = 2.2, RadarPerCapsule = 0.25, RadarTauMs = 120, RadarRockMistake = 0.12, RadarGuess = 0.35;
        /// <summary>Acoplamiento: decidir = 1,3 s + ángulo / 150 (+0,3 s si es espejo); exactitud 0,98 − 0,0004 × ángulo en jóvenes.</summary>
        public const double DockingBase = 1.3, DockingDegPerSecond = 150, DockingMirrorExtra = 0.3, DockingAccuracyBonus = 0.03, DockingAccuracyPerDeg = 0.0004;

        /// <summary>Cuánto resta de exactitud cada tipo de frase (1 a 6: corta, con complemento, negación, con pausa, con cuantificador, comparación).</summary>
        private static readonly double[] DisparateHard = { 0.0, 0.01, 0.03, 0.02, 0.02, 0.03 };

        // ------------------------------------------------------------------ una persona

        /// <summary>Una persona simulada: sus tres números, ya sorteados de su franja. <see cref="Slowness"/> ya lleva la escala del juego.</summary>
        public readonly struct Person
        {
            public readonly double Slowness, Accuracy, Objects;
            public readonly bool Senior;

            public Person(double slowness, double accuracy, double objects, bool senior)
            {
                Slowness = slowness;
                Accuracy = accuracy;
                Objects = objects;
                Senior = senior;
            }

            /// <summary>El tiempo de respuesta de un ensayo: lognormal alrededor de la mediana de la persona (la mediana del juego × su lentitud).</summary>
            public double Rt(Random rng, double median) => LogN(rng, median * Slowness, Within);
        }

        /// <summary>Sortea una persona de la franja <paramref name="range"/> (0 a 5): lentitud lognormal (× <paramref name="scale"/>), exactitud y objetos de un vistazo normales con tope.</summary>
        public static Person Draw(int range, double scale, Random rng) => new Person(
            LogN(rng, Slowness[range], Dispersion[range]) * scale,
            Clip(Gauss(rng) * AccuracySd + Accuracy[range], 0.6, 0.995),
            Clip(Gauss(rng) * ObjectsSd + Objects[range], 1.2, 4.5),
            Senior[range]);

        // ------------------------------------------------------------------ jugar la pista real

        /// <summary>Una partida de 120 s de <paramref name="person"/> sobre la pista REAL de <paramref name="game"/> del día <paramref name="day"/> (aaaammdd): la MARCA (aciertos menos errores, con piso en 0). <paramref name="rng"/> es el vaivén de la persona ese día.</summary>
        public static double Play(string game, Person person, int day, Random rng, SentenceBank bank)
        {
            switch (game)
            {
                case "stroop": return PlayStroop(person, day, rng);
                case "disparate": return PlayDisparate(person, day, rng, bank);
                case "radar": return PlayRadar(person, day, rng);
                case "acoplamiento": return PlayDocking(person, day, rng);
                default: throw new ArgumentException("juego sin Contrarreloj: " + game);
            }
        }

        private static double PlayStroop(Person p, int day, Random rng)
        {
            var run = new TimeTrial.Run("stroop", day);
            var seq = new StroopContract.Sequencer(new Random(0));
            double t = 0;
            while (true)
            {
                int level = run.Level;
                var trial = seq.Next(level, run.Rng());
                double median = StroopResponse * (trial.Clash ? 1.0 : StroopCongruentFactor) + (trial.Switched ? StroopSwitchExtra : 0.0);
                double answerAt = t + StroopContract.ArrivalSeconds(level, p.Senior) + p.Rt(rng, median);
                if (answerAt > TimeTrial.Seconds) break;
                double acc = p.Accuracy + (trial.Clash ? 0.0 : 0.04) - (trial.Switched ? 0.04 : 0.0);
                bool correct = rng.NextDouble() < Clip(acc, 0.5, 0.995);
                run.Register(correct, (float)(TimeTrial.Seconds - answerAt));
                t = answerAt + (correct ? StroopContract.RetoSlideOutSeconds : TimeTrial.ErrorFeedbackSeconds + StroopContract.FadeOutSeconds);
                if (t >= TimeTrial.Seconds) break;
            }
            return run.Score;
        }

        private static double PlayDisparate(Person p, int day, Random rng, SentenceBank bank)
        {
            var run = new TimeTrial.Run("disparate", day);
            var director = new DisparateDirector(bank, new Random(0)) { NoBursts = true };
            double t = 0;
            while (true)
            {
                var spec = director.Next(run.Level, false, false, run.Rng());
                int words = spec.S.n;
                int type = Math.Max(1, Math.Min(DisparateHard.Length, spec.S.t));
                double read = p.Rt(rng, DisparateBase + DisparatePerWord * words);
                double answerAt = t + TimeTrialSimulation.DisparateArrive + Math.Max(TimeTrial.MinThinkSeconds("disparate", words), read);
                if (answerAt > TimeTrial.Seconds) break;
                bool correct = rng.NextDouble() < Clip(p.Accuracy - DisparateHard[type - 1], 0.5, 0.995);
                run.Register(correct, (float)(TimeTrial.Seconds - answerAt));
                t = answerAt + (correct ? TimeTrialSimulation.DisparateFarewell : TimeTrial.ErrorFeedbackSeconds + TimeTrialSimulation.DisparateFarewell);
                if (t >= TimeTrial.Seconds) break;
            }
            return run.Score;
        }

        private static double PlayRadar(Person p, int day, Random rng)
        {
            var run = new TimeTrial.Run("radar", day);
            double t = 0;
            while (true)
            {
                int level = run.Level;
                var round = RadarContract.NextRound(level, run.Rng());
                double watch = RadarContract.WatchMinSeconds + (RadarContract.WatchMaxSeconds - RadarContract.WatchMinSeconds) * run.Rng(1).NextDouble();
                int n = round.Count, rocks = round.Rocks.Length;
                double ms = round.ExposureMs;
                double answerAt = t + watch + ms / 1000.0 + RadarContract.MaskMs / 1000.0 + p.Rt(rng, RadarResponse) + RadarPerCapsule * n;
                if (answerAt > TimeTrial.Seconds) break;
                // los objetos que VIO de verdad: capacidad × (1 − e^(−destello / τ)), con τ más largo cuanto más lenta la persona; lo que no vio lo adivina
                double seen = Math.Min(n, p.Objects * (1.0 - Math.Exp(-ms / (RadarTauMs * p.Slowness))));
                int good = 0;
                for (int i = 0; i < n; i++) if (rng.NextDouble() < seen / n) good++;
                int extras = 0;
                for (int i = 0; i < rocks; i++) if (rng.NextDouble() < RadarRockMistake * p.Slowness) extras++;
                int guess = n - good;
                for (int i = 0; i < guess; i++) if (rng.NextDouble() < RadarGuess) good++;
                for (int i = 0; i < guess; i++) if (rng.NextDouble() < RadarGuess) extras++;
                bool success = RadarContract.Success(n, good, extras);
                run.Register(success, (float)(TimeTrial.Seconds - answerAt));
                t = answerAt + RadarContract.RevealSeconds;
                if (t >= TimeTrial.Seconds) break;
            }
            return run.Score;
        }

        private static double PlayDocking(Person p, int day, Random rng)
        {
            var run = new TimeTrial.Run("acoplamiento", day);
            double t = 0;
            double minThink = TimeTrial.MinThinkSeconds("acoplamiento");
            while (true)
            {
                int level = run.Level;
                var trial = DockingContract.NextTrial(level, run.Rng());
                double decideAt = t + DockingContract.ArriveMs / 1000.0;
                double deadline = DockingContract.DeadlineMs(level, false) / 1000.0;
                double angle = trial.Disparity;
                double resp = Math.Max(minThink, p.Rt(rng, DockingBase + angle / DockingDegPerSecond + (trial.Mirrored ? DockingMirrorExtra : 0.0)));
                bool timedOut = resp >= deadline;
                double answerAt = decideAt + (timedOut ? deadline : resp);
                if (answerAt > TimeTrial.Seconds) break;
                double acc = p.Accuracy + DockingAccuracyBonus - DockingAccuracyPerDeg * angle;
                bool correct = !timedOut && rng.NextDouble() < Clip(acc, 0.5, 0.995);
                run.Register(correct, (float)(TimeTrial.Seconds - answerAt));
                t = answerAt + (correct ? DockingContract.OkRevealSeconds(trial.Mirrored) : DockingContract.MissRevealSeconds(trial.Mirrored));
                if (t >= TimeTrial.Seconds) break;
            }
            return run.Score;
        }

        // ------------------------------------------------------------------ la tabla

        /// <summary>
        /// El ancho del tramo de cada juego (decisión de Ricardo, 1-nov): de a 1 acierto en Rescate relámpago, de a 2 en Acoplamiento y de a 5 en Tinta o Palabra y ¿Verdad o disparate?. Cuántos tramos hay lo dice <see cref="BinCount"/>; el último junta «lo de ahí en adelante».
        /// </summary>
        public static int BinWidth(string game)
        {
            switch (game)
            {
                case "radar": return 1;
                case "acoplamiento": return 2;
                default: return 5;
            }
        }

        /// <summary>
        /// Cuántos tramos tiene la tabla de un juego (Opus, 1-nov): 16 en Tinta o Palabra (0 a 79), ¿Verdad o disparate? (0 a 79) y Acoplamiento (0 a 31); **24 en Rescate relámpago** (de a 1: 0 a 23), porque con 16 (0 a 15) el 21 al 29 % de los jóvenes se juntaba en el último tramo («15 o más»). El JSON lo dice (<c>tramos</c>) y la app lo lee de ahí.
        /// </summary>
        public static int BinCount(string game) => game == "radar" ? 24 : 16;

        /// <summary>Los días de pista de una semana: cada día tiene su propia pista (otra semilla), así que una persona que juega 3 veces juega 3 pistas distintas.</summary>
        public const int WeekDays = 7;

        /// <summary>
        /// Con qué se arma la tabla: cuántas personas por franja, cuántas PARTIDAS juega cada una en la semana (<see cref="Played"/>: de ellas sale su mejor marca; son días DISTINTOS de los <see cref="WeekDays"/>, sorteados por persona con la semilla fija), desde qué día (aaaammdd empieza la semana), con qué semilla y con qué escala.
        /// Cuántas partidas juega una persona por semana es una perilla que se mezcla con la escala: por defecto 3 (las personas de Ricardo juegan 3 Contrarrelojes por juego); 7 = todos los días.
        /// </summary>
        public readonly struct Options
        {
            public readonly int People, Played, FirstDay;
            public readonly ulong Seed;
            /// <summary>Multiplica la lentitud de todas las personas del juego; 1,0 = las medianas de la §2.3 de Fable. Si es 0 o menos se usa <see cref="ReferenceScales.For"/>.</summary>
            public readonly double Scale;

            public Options(int people, int played, int firstDay, ulong seed, double scale = 0.0)
            {
                People = people;
                Played = Math.Max(1, Math.Min(WeekDays, played));
                FirstDay = firstDay;
                Seed = seed;
                Scale = scale;
            }
        }

        /// <summary>Lo de siempre: 2.000 personas por franja, 3 partidas por semana (3 días distintos de la semana del 1 de noviembre de 2026) y la semilla 20261101.</summary>
        public static Options Default => new Options(2000, 3, 20261101, 20261101UL);

        /// <summary>Los días (índices 0 a <see cref="WeekDays"/> − 1, de menor a mayor) en que juega una persona: <paramref name="played"/> DISTINTOS, sorteados con su semilla (Fisher-Yates parcial con SplitMix64: la misma persona juega siempre los mismos días).</summary>
        public static int[] PickDays(ulong personSeed, int played)
        {
            played = Math.Max(1, Math.Min(WeekDays, played));
            var idx = new int[WeekDays];
            for (int i = 0; i < WeekDays; i++) idx[i] = i;
            var rng = new TimeTrial.TrackRandom(Mix(personSeed ^ 0xA24BAED4963EE407UL));
            for (int i = 0; i < played; i++)
            {
                int j = i + rng.Next(WeekDays - i);
                int tmp = idx[i]; idx[i] = idx[j]; idx[j] = tmp;
            }
            var picked = new int[played];
            Array.Copy(idx, picked, played);
            Array.Sort(picked);
            return picked;
        }

        /// <summary>El resultado para UNA franja de UN juego.</summary>
        public sealed class RangeResult
        {
            public string Key, Label;
            public int People;
            public int[] Bins;
            public double Mean;
            public int P25, P50, P75, P90;
        }

        /// <summary>La tabla de UN juego: todas sus franjas.</summary>
        public sealed class Table
        {
            public string Game;
            public int TrackVersion, BinWidth, BinCount, Played, FirstDay;
            public int PeoplePerRange;
            public ulong Seed;
            public double Scale;
            public RangeResult[] Ranges;
        }

        /// <summary>Los siete días (o los que sean) de una semana desde <paramref name="firstDay"/> (aaaammdd), pasando de mes si hace falta.</summary>
        public static int[] DayKeys(int firstDay, int days)
        {
            var first = new DateTime(firstDay / 10000, firstDay / 100 % 100, firstDay % 100);
            var keys = new int[days];
            for (int i = 0; i < days; i++)
            {
                var d = first.AddDays(i);
                keys[i] = d.Year * 10000 + d.Month * 100 + d.Day;
            }
            return keys;
        }

        /// <summary>Arma la tabla de <paramref name="game"/>: por cada franja, <see cref="Options.People"/> personas; cada una juega <see cref="Options.Played"/> días DISTINTOS de la semana (la pista real de cada día) y se queda con su mejor marca.</summary>
        public static Table Build(string game, SentenceBank bank, Options options)
        {
            double scale = options.Scale > 0 ? options.Scale : ReferenceScales.For(game);
            int width = BinWidth(game);
            var week = DayKeys(options.FirstDay, WeekDays);
            var table = new Table
            {
                Game = game, TrackVersion = TimeTrial.TrackVersion(game), BinWidth = width, BinCount = BinCount(game), Played = options.Played, FirstDay = options.FirstDay,
                PeoplePerRange = options.People, Seed = options.Seed, Scale = scale, Ranges = new RangeResult[RangeKeys.Length],
            };
            for (int f = 0; f < RangeKeys.Length; f++)
            {
                var best = new int[options.People];
                for (int i = 0; i < options.People; i++)
                {
                    ulong personSeed = Mix(options.Seed ^ Hash(game) ^ ((ulong)(f + 1) << 48) ^ ((ulong)(uint)i * 0xD1B54A32D192ED03UL));
                    var person = Draw(f, scale, new TimeTrial.TrackRandom(personSeed));
                    double top = 0;
                    foreach (int d in PickDays(personSeed, options.Played))
                    {
                        var rng = new TimeTrial.TrackRandom(Mix(personSeed ^ ((ulong)(uint)week[d] * 0x8CB92BA72F3D8DD7UL)));
                        top = Math.Max(top, Play(game, person, week[d], rng, bank));
                    }
                    best[i] = (int)top;
                }
                table.Ranges[f] = Summarize(f, best, width, table.BinCount);
            }
            return table;
        }

        private static RangeResult Summarize(int range, int[] marks, int width, int binCount)
        {
            var bins = new int[binCount];
            double sum = 0;
            foreach (int m in marks)
            {
                bins[Math.Min(binCount - 1, Math.Max(0, m) / width)]++;
                sum += m;
            }
            var sorted = (int[])marks.Clone();
            Array.Sort(sorted);
            int Q(double q) => sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)(q * sorted.Length))];
            return new RangeResult { Key = RangeKeys[range], Label = RangeLabels[range], People = marks.Length, Bins = bins, Mean = marks.Length == 0 ? 0 : sum / marks.Length, P25 = Q(0.25), P50 = Q(0.50), P75 = Q(0.75), P90 = Q(0.90) };
        }

        // ------------------------------------------------------------------ el JSON (lo que lee la app: `app/src/main/assets/referencia/<juego>_v<version>.json`)

        /// <summary>De dónde salen los parámetros del modelo (lo escribe el JSON junto a la versión del modelo).</summary>
        public const string Source = "auditoria-fable/referencia-estimada.md §2.2";

        /// <summary>
        /// El JSON de una tabla, a mano y con cultura invariante. Lleva el ancho del tramo, la versión de la pista, la cuenta de robots, las partidas por semana, la escala, las semillas, la versión del modelo y de dónde sale; por franja, los 16 tramos, la media y p25/p50/p75/p90. <paramref name="date"/> es solo metadata (cuándo se generó): no entra en
        /// ninguna comparación. Los robots NO son personas: el archivo se llama «referencia» y dice <c>"estimada": true</c>.
        /// </summary>
        public static string ToJson(Table t, string date)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"estimada\": true,\n");
            sb.Append("  \"modelo\": ").Append(ModelVersion.ToString(inv)).Append(",\n");
            sb.Append("  \"fuente_modelo\": \"").Append(Source).Append("\",\n");
            sb.Append("  \"juego\": \"").Append(t.Game).Append("\",\n");
            sb.Append("  \"version_pista\": ").Append(t.TrackVersion.ToString(inv)).Append(",\n");
            sb.Append("  \"ancho_tramo\": ").Append(t.BinWidth.ToString(inv)).Append(",\n");
            sb.Append("  \"tramos\": ").Append(t.BinCount.ToString(inv)).Append(",\n");
            sb.Append("  \"robots_por_franja\": ").Append(t.PeoplePerRange.ToString(inv)).Append(",\n");
            sb.Append("  \"partidas_por_semana\": ").Append(t.Played.ToString(inv)).Append(",\n");
            sb.Append("  \"dias_de_pista\": ").Append(WeekDays.ToString(inv)).Append(",\n");
            sb.Append("  \"primer_dia\": ").Append(t.FirstDay.ToString(inv)).Append(",\n");
            sb.Append("  \"semilla\": ").Append(t.Seed.ToString(inv)).Append(",\n");
            sb.Append("  \"escala\": ").Append(t.Scale.ToString("0.####", inv)).Append(",\n");
            sb.Append("  \"generado\": \"").Append(date).Append("\",\n");
            sb.Append("  \"franjas\": {\n");
            for (int i = 0; i < t.Ranges.Length; i++)
            {
                var r = t.Ranges[i];
                sb.Append("    \"").Append(r.Key).Append("\": { \"robots\": ").Append(r.People.ToString(inv));
                sb.Append(", \"tramos\": [").Append(string.Join(", ", Array.ConvertAll(r.Bins, b => b.ToString(inv)))).Append("]");
                sb.Append(", \"media\": ").Append(r.Mean.ToString("0.00", inv));
                sb.Append(", \"p25\": ").Append(r.P25.ToString(inv)).Append(", \"p50\": ").Append(r.P50.ToString(inv)).Append(", \"p75\": ").Append(r.P75.ToString(inv)).Append(", \"p90\": ").Append(r.P90.ToString(inv)).Append(" }");
                sb.Append(i < t.Ranges.Length - 1 ? ",\n" : "\n");
            }
            sb.Append("  }\n}\n");
            return sb.ToString();
        }

        /// <summary>El nombre del archivo de una tabla: <c>stroop_v2.json</c>.</summary>
        public static string FileName(Table t) => t.Game + "_v" + t.TrackVersion.ToString(CultureInfo.InvariantCulture) + ".json";

        /// <summary>El informe corto de una tabla para leer de un vistazo: mitad y cuartiles por franja.</summary>
        public static string Describe(Table t)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append(t.Game).Append(" v").Append(t.TrackVersion.ToString(inv)).Append(" · tramos de ").Append(t.BinWidth.ToString(inv)).Append(" · escala ").Append(t.Scale.ToString("0.###", inv)).Append(" · ").Append(t.PeoplePerRange.ToString(inv)).Append(" robots por franja × ").Append(t.Played.ToString(inv)).Append(" partidas por semana\n");
            foreach (var r in t.Ranges)
                sb.Append("  ").Append(r.Label.PadRight(9)).Append(" p25 ").Append(r.P25.ToString(inv).PadLeft(3)).Append(" · mitad ").Append(r.P50.ToString(inv).PadLeft(3)).Append(" · p75 ").Append(r.P75.ToString(inv).PadLeft(3)).Append(" · p90 ").Append(r.P90.ToString(inv).PadLeft(3)).Append(" · media ").Append(r.Mean.ToString("0.0", inv)).Append("\n");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ azar

        private static double Gauss(Random r)
        {
            double u1 = 1.0 - r.NextDouble(), u2 = r.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        private static double LogN(Random r, double median, double sigma) => median * Math.Exp(sigma * Gauss(r));
        private static double Clip(double x, double lo, double hi) => Math.Max(lo, Math.Min(hi, x));

        /// <summary>SplitMix64 (la misma vuelta que <c>TimeTrial.Mix</c>, que no es pública).</summary>
        private static ulong Mix(ulong x)
        {
            unchecked
            {
                x += 0x9E3779B97F4A7C15UL;
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }

        /// <summary>FNV-1a del id del juego: así dos juegos no comparten el mismo flujo de azar.</summary>
        private static ulong Hash(string s)
        {
            unchecked
            {
                ulong h = 14695981039346656037UL;
                foreach (char c in s ?? "") { h ^= c; h *= 1099511628211UL; }
                return h;
            }
        }
    }

    /// <summary>
    /// LA ESCALA de cada juego: el único número que se calibra (Tarea 204; Fable §3.4). Multiplica la lentitud de TODAS las personas simuladas de ese juego: 1,0 = las medianas de laboratorio de <see cref="AgeReference"/> (Tinta o Palabra 1,25 s, ¿Verdad o disparate? 0,30 s por palabra,
    /// Rescate relámpago 2,2 s, Acoplamiento 1,3 s) y 1,3 = lo mismo con la respuesta de un teléfono 30 % más lenta. Lo que los estudios fijan es la distancia entre franjas; cuánto tarda en tocar una persona joven en ESTE teléfono no lo fija nadie. Se ajusta antes de lanzar con la mejor marca de Ricardo
    /// y de sus 5 personas (y, con servidor, con las primeras 50 personas reales); se cambia AQUÍ y se regenera la tabla (<c>bash tools/verificar-todo.sh --referencia</c>). El valor usado queda escrito en el JSON.
    /// </summary>
    public static class ReferenceScales
    {
        public const double Stroop = 1.0, Disparate = 1.0, Radar = 1.0, Docking = 1.0;

        public static double For(string game)
        {
            switch (game)
            {
                case "stroop": return Stroop;
                case "disparate": return Disparate;
                case "radar": return Radar;
                case "acoplamiento": return Docking;
                default: throw new ArgumentException("juego sin Contrarreloj: " + game);
            }
        }
    }
}
