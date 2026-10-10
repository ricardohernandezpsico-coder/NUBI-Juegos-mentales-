using System;
using System.Collections.Generic;
using System.Globalization;

namespace NeuroVida.Games.Aterrizaje
{
    /// <summary>Un aterrizaje: la regla (de <see cref="Min"/> a <see cref="Max"/>) y el número donde hay que posarse.</summary>
    public sealed class LandingTrial
    {
        public float Min, Max, Target;
        /// <summary>Cómo se muestra el objetivo ("37", "3/4", "0,6", "45%", "250 + 130").</summary>
        public string Label;
        public string MinLabel, MaxLabel;
        /// <summary>Marca a la mitad de la regla (ayuda en los niveles bajos).</summary>
        public bool MidTick;
        public int Level;
        /// <summary>La regla es de números enteros o de porcentajes: la distancia al blanco se dice en unidades («a 3 del blanco»); en fracciones y decimales se dice en % de la regla («a 3 % del blanco»).</summary>
        public bool Unit;
        /// <summary>Lo que dice la bandera: el número (en las sumas, el RESULTADO, para que se vea contra qué se aterrizó). Si es null, es <see cref="Label"/>.</summary>
        public string FlagLabel;

        public string FlagText => FlagLabel ?? Label;

        /// <summary>Dónde está el objetivo en la regla (0 = extremo izquierdo, 1 = derecho).</summary>
        public float TargetFraction => (Target - Min) / (Max - Min);
    }

    /// <summary>
    /// Reglas puras de "Aterrizaje Lunar": ESTIMACIÓN EN LA LÍNEA NUMÉRICA (Siegler y Opfer, 2003; Siegler y Ramani,
    /// 2008; meta-análisis de Schneider et al., 2018: se relaciona con el rendimiento en matemáticas). Se da un número y
    /// hay que posar la nave en su lugar sobre una regla que solo tiene marcados los extremos. La dificultad amplía la
    /// escala (0-10 → 0-1000), quita la marca del medio, pasa a fracciones, decimales y porcentajes, a reglas que no
    /// empiezan en 0, a sumas que hay que resolver antes de aterrizar y a números negativos.
    /// Renovación «la misma tarea, más grande, con más vida y un final con sentido» (10-oct, docs/diseno-aterrizaje.md): las REGLAS no cambian; se suman las cúpulas de tu base (una cada 5 aterrizajes justos), los tiempos del resultado y los textos de los avisos.
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class LandingContract
    {
        public const string GameId = "aterrizaje";
        public const int MaxLevel = 12;

        /// <summary>Reto: 2 minutos de aterrizajes.</summary>
        public const int RetoSeconds = 120;
        /// <summary>Precisión (sin reloj): cantidad de aterrizajes.</summary>
        public const int PrecisionTrials = 15;

        /// <summary>Versión corta del inicio («Tu punto de partida», con <c>Assessment</c> activo): 8 aterrizajes fijos (≈ 70 s), sin reloj y con la bajada del nivel.</summary>
        public const int AssessmentTrials = 8;

        /// <summary>Aterrizajes de la partida sin reloj: 8 en la versión corta, 15 en Precisión.</summary>
        public static int TotalTrials(bool assessment) => assessment ? AssessmentTrials : PrecisionTrials;

        /// <summary>Paso del DDA (≈ 18 aterrizajes por partida: pasos medianos).</summary>
        public const float StepUp = 0.3f;

        /// <summary>El motor común. La versión corta parte suave (nivel 1 en mayores, 3 en el resto: regla de 0 a 100 con marca del medio). Sin tiempo de reacción: cuenta la precisión.</summary>
        public static NeuroVida.Games.AdaptiveDifficulty CreateEngine(NeuroVida.Contracts.SequenceConfigDetails config)
        {
            var age = NeuroVida.Games.DdaUserProfileConfig.ParseAgeBand(config.age_band);
            float start = config.assessment ? NeuroVida.Games.Shared.Assessment.SoftStartLevel(age) : NeuroVida.Games.AdaptiveDifficulty.StartRating(config, MaxLevel);
            return new NeuroVida.Games.AdaptiveDifficulty(MaxLevel, age, start, StepUp, useReaction: false);
        }

        // ------------------------------------------------------------------ ronda guiada del tutorial

        /// <summary>Las dos zonas guía de la ronda guiada, como fracción del largo de la regla a cada lado del lugar justo: ancha (±12 %) y luego más chica (±6 %).</summary>
        public static readonly float[] GuidedZones = { 0.12f, 0.06f };

        /// <summary>Un aterrizaje fácil para el tutorial: regla de 0 a 10 con la marca del medio, y un número entre el 2 y el 8 que no sea el 5 ni el <paramref name="avoid"/>.</summary>
        public static LandingTrial GuidedTrial(Random rng, int avoid = -1)
        {
            int v;
            do { v = 2 + rng.Next(7); } while (v == 5 || v == avoid);
            return new LandingTrial
            {
                Level = 1, Min = 0, Max = 10, Target = v, Label = v.ToString(CultureInfo.InvariantCulture),
                MinLabel = "0", MaxLabel = "10", MidTick = true, Unit = true
            };
        }

        /// <summary>Error (fracción del largo de la regla) que todavía cuenta como buen aterrizaje.</summary>
        public const float HitError = 0.05f;
        /// <summary>Error de una "diana": justo en el blanco.</summary>
        public const float BullseyeError = 0.012f;

        // Formato fijo (sin depender de las culturas del teléfono): coma decimal como en español.
        private static string Dec(double v, string format) => v.ToString(format, CultureInfo.InvariantCulture).Replace('.', ',');

        /// <summary>Segundos que tarda en bajar sola (soltar el dedo la hace bajar rápido): 6 → 3,5; Precisión 10.</summary>
        public static float DescentSeconds(int level, bool precision) =>
            precision ? 10f : 6f - 2.5f * (Clamp(level) - 1) / (MaxLevel - 1);

        /// <summary>Qué trae cada nivel (para el aviso al subir).</summary>
        public static string LevelNews(int level)
        {
            switch (Clamp(level))
            {
                case 2: return "Regla de 0 a 20";
                case 3: return "Regla de 0 a 100";
                case 4: return "Sin marca del medio";
                case 5: return "Regla de 0 a 1000";
                case 6: return "0 a 1000 sin ayuda";
                case 7: return "Llegan las fracciones";
                case 8: return "Decimales y porcentajes";
                case 9: return "La regla no empieza en 0";
                case 10: return "Resuelve la suma y aterriza";
                case 11: return "Fracciones de 0 a 2";
                case 12: return "Números negativos";
                default: return "";
            }
        }

        public static LandingTrial NextTrial(int level, Random rng)
        {
            int l = Clamp(level);
            var t = new LandingTrial { Level = l };
            switch (l)
            {
                case 1: Integers(t, 0, 10, true, rng); break;
                case 2: Integers(t, 0, 20, true, rng); break;
                case 3: Integers(t, 0, 100, true, rng); break;
                case 4: Integers(t, 0, 100, false, rng); break;
                case 5: Integers(t, 0, 1000, true, rng); break;
                case 6: Integers(t, 0, 1000, false, rng); break;
                case 7: Fraction(t, 1, 10, rng); break;
                case 8:
                    if (rng.NextDouble() < 0.5) Decimal(t, rng);
                    else Percent(t, rng);
                    break;
                case 9:
                {
                    int start = 100 * (1 + rng.Next(5));               // 100..500
                    Integers(t, start, start + 500, false, rng);
                    break;
                }
                case 10: Sum(t, rng); break;
                case 11: Fraction(t, 2, 8, rng); break;
                default: Integers(t, -50, 50, false, rng); break;
            }
            t.FlagLabel = t.FlagLabel ?? t.Label;
            return t;
        }

        /// <summary>Entero al azar, lejos de los extremos (entre el 4% y el 96% de la regla).</summary>
        private static void Integers(LandingTrial t, int min, int max, bool mid, Random rng)
        {
            int span = max - min;
            int lo = min + Math.Max(1, (int)Math.Ceiling(span * 0.04)), hi = max - Math.Max(1, (int)Math.Ceiling(span * 0.04));
            int v = lo + rng.Next(hi - lo + 1);
            // Evita justo el medio cuando hay marca del medio (sería regalado).
            if (mid && v * 2 == min + max) v += rng.NextDouble() < 0.5 ? 1 : -1;
            t.Min = min;
            t.Max = max;
            t.Target = v;
            t.Label = v.ToString(CultureInfo.InvariantCulture);
            t.MinLabel = min.ToString(CultureInfo.InvariantCulture);
            t.MaxLabel = max.ToString(CultureInfo.InvariantCulture);
            t.MidTick = mid;
            t.Unit = true;
        }

        /// <summary>Fracción propia (o hasta 2 si <paramref name="upTo"/> = 2) con denominador 2..<paramref name="maxDen"/>.</summary>
        private static void Fraction(LandingTrial t, int upTo, int maxDen, Random rng)
        {
            int den, num;
            do
            {
                den = 2 + rng.Next(maxDen - 1);
                num = 1 + rng.Next(den * upTo - 1);
            } while (Gcd(num, den) != 1 || num == den);
            t.Min = 0;
            t.Max = upTo;
            t.Target = (float)num / den;
            t.Label = num + "/" + den;
            t.MinLabel = "0";
            t.MaxLabel = upTo.ToString(CultureInfo.InvariantCulture);
            t.MidTick = false;
        }

        private static void Decimal(LandingTrial t, Random rng)
        {
            // Mitad décimas (0,1..0,9), mitad centésimas (0,05..0,95).
            int hundredths = rng.NextDouble() < 0.5 ? 10 * (1 + rng.Next(9)) : 5 + rng.Next(91);
            t.Min = 0;
            t.Max = 1;
            t.Target = hundredths / 100f;
            t.Label = Dec(hundredths / 100.0, "0.0#");
            t.MinLabel = "0";
            t.MaxLabel = "1";
            t.MidTick = false;
        }

        private static void Percent(LandingTrial t, Random rng)
        {
            int p = 5 + rng.Next(91);
            t.Min = 0;
            t.Max = 100;
            t.Target = p;
            t.Label = p + "%";
            t.MinLabel = "0%";
            t.MaxLabel = "100%";
            t.MidTick = false;
            t.Unit = true;
        }

        private static void Sum(LandingTrial t, Random rng)
        {
            int a = 10 * (5 + rng.Next(50));   // 50..540
            int b = 10 * (3 + rng.Next(38));   // 30..400
            t.Min = 0;
            t.Max = 1000;
            t.Target = a + b;
            t.Label = a + " + " + b;
            t.FlagLabel = (a + b).ToString(CultureInfo.InvariantCulture);          // la bandera dice el RESULTADO de la suma
            t.MinLabel = "0";
            t.MaxLabel = "1000";
            t.MidTick = false;
            t.Unit = true;
        }

        private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

        // ------------------------------------------------------------------ puntaje y medidas

        /// <summary>Error como fracción del largo de la regla (0 = justo en el blanco).</summary>
        public static float Error(LandingTrial t, float landedValue) => Math.Abs(landedValue - t.Target) / (t.Max - t.Min);

        /// <summary>Valor de la regla en la fracción <paramref name="f"/> (0..1) de su largo.</summary>
        public static float ValueAt(LandingTrial t, float f) => t.Min + Math.Max(0f, Math.Min(1f, f)) * (t.Max - t.Min);

        public static bool IsHit(float error) => error <= HitError;
        public static bool IsBullseye(float error) => error <= BullseyeError;

        /// <summary>Puntos: más cuanto más cerca (0 desde un 12% de error), bono de diana, nivel y racha.</summary>
        public static int Points(float error, int level, int streak)
        {
            float closeness = Math.Max(0f, 1f - error / 0.12f);
            int pts = (int)Math.Round(120f * closeness);
            if (IsBullseye(error)) pts += 80;
            if (IsHit(error)) pts += 10 * (Clamp(level) - 1) + 15 * Math.Min(Math.Max(streak - 1, 0), 10);
            return pts;
        }

        /// <summary>"Tu estimación": distancia media al blanco en % del largo de la regla. -1 sin aterrizajes.</summary>
        public static float MeanErrorPct(IReadOnlyList<float> errors)
        {
            if (errors == null || errors.Count == 0) return -1f;
            double s = 0;
            foreach (float e in errors) s += e;
            return (float)(s / errors.Count * 100.0);
        }

        /// <summary>Puntaje 0-100: precisión (70%; 0% de error = 1, 15% = 0) y nivel más alto alcanzado (30%).</summary>
        public static int Score(float meanErrorPct, int peakLevel)
        {
            float acc = meanErrorPct < 0f ? 0f : Math.Max(0f, Math.Min(1f, 1f - meanErrorPct / 15f));
            float lv = (float)(Clamp(peakLevel) - 1) / (MaxLevel - 1);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.7f * acc + 0.3f * lv) * 100f)));
        }

        /// <summary>Diferencia en las unidades de la regla, para mostrar ("7", "0,3", "0,05"): con una regla corta lleva decimales, para que un aterrizaje justo nunca diga «0» (con 5 % de error en una regla de 0 a 10 hay medio punto).</summary>
        public static string DistanceLabel(LandingTrial t, float landedValue)
        {
            float d = Math.Abs(landedValue - t.Target);
            float span = t.Max - t.Min;
            if (span <= 2f) return Dec(d, "0.00");
            if (span <= 25f) return Dec(d, "0.0");
            return Math.Round(d).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>«a 3 del blanco» (reglas de números enteros y porcentajes: en unidades) o «a 3 % del blanco» (fracciones y decimales: en % de la regla; nunca menos de 1).</summary>
        public static string DistanceText(LandingTrial t, float landedValue)
        {
            if (t.Unit) return "a " + DistanceLabel(t, landedValue) + " del blanco";
            int pct = Math.Max(1, (int)Math.Round(Error(t, landedValue) * 100f));
            return "a " + pct + " % del blanco";
        }

        // ------------------------------------------------------------------ cúpulas de tu base

        /// <summary>Cada cuántos aterrizajes justos se arma una cúpula de tu base.</summary>
        public const int DomeEvery = 5;

        /// <summary>Las cúpulas que se armaron con <paramref name="hits"/> aterrizajes justos.</summary>
        public static int Domes(int hits) => Math.Max(0, hits) / DomeEvery;

        /// <summary>Cuántos aterrizajes justos van hacia la próxima cúpula (0 a 4: los puntos del marcador).</summary>
        public static int ToNextDome(int hits) => Math.Max(0, hits) % DomeEvery;

        /// <summary>¿El aterrizaje justo número <paramref name="hitsAfter"/> arma una cúpula?</summary>
        public static bool EarnsDome(int hitsAfter) => hitsAfter > 0 && hitsAfter % DomeEvery == 0;

        // ------------------------------------------------------------------ los tiempos del resultado (ms desde que la nave toca la regla)

        /// <summary>Soltar: la nave cae en 320 ms.</summary>
        public const int DropMs = 320;
        /// <summary>El tramo entre la nave y el lugar justo se dibuja entre los 300 y los 600 ms; la bandera sube entre los 380 y los 800 ms (80 dp); el aviso sale a los 620 ms y el de la cúpula 900 ms después; la siguiente nave llega a los 2300 ms.</summary>
        public const int LineDelayMs = 300, LineMs = 300, FlagDelayMs = 380, FlagMs = 420, NoticeDelayMs = 620, DomeNoticeDelayMs = 900, ResultHoldMs = 2300;
        /// <summary>Alto de la bandera (dp).</summary>
        public const float FlagHeight = 80f;

        // ------------------------------------------------------------------ textos (docs/diseno-aterrizaje.md §3 y §6)

        public const string Title = "Aterrizaje Lunar", MissionLabel = "Aterriza en", CountdownSub = "Prepárate";
        public const string Hint = "Arrastra para mover la nave · suelta para aterrizar";
        /// <summary>La instrucción se ve en los primeros aterrizajes.</summary>
        public const int HintTrials = 3;
        public const string BullNotice = "¡Diana lunar!", DomeNotice = "¡Nueva cúpula en tu base!";

        public static string RoundChip(int n, int total) => total > 0 ? "Aterrizaje " + Math.Min(n, total) + " de " + total : "Aterrizaje " + n;
        public static string StreakChip(int streak) => "Racha ×" + streak;

        /// <summary>Cómo salió el aterrizaje.</summary>
        public enum Outcome { Far, Hit, Bull }

        public static Outcome OutcomeOf(float error) => IsBullseye(error) ? Outcome.Bull : IsHit(error) ? Outcome.Hit : Outcome.Far;

        /// <summary>El aviso de después de posarse (va en el cielo vacío): «¡Diana lunar!», «¡Justo! a 3 del blanco» (desde la racha 3, con «· Racha ×N») o «Cerca: a 3 del blanco».</summary>
        public static string Notice(LandingTrial t, float landedValue, int streak)
        {
            float err = Error(t, landedValue);
            if (IsBullseye(err)) return BullNotice;
            string d = DistanceText(t, landedValue);
            if (IsHit(err)) return streak >= 3 ? "¡Justo! " + d + " · Racha ×" + streak : "¡Justo! " + d;
            return "Cerca: " + d;
        }

        // ------------------------------------------------------------------ el final dentro del juego (el de la app va en GameResultScreen)

        public const string EndTag = "¡MISIÓN CUMPLIDA!", EndBoxTitle = "Tu distancia promedio al lugar justo";

        public static string EndTitle(int hits, int total) => (hits == 1 ? "1 aterrizaje justo" : hits + " aterrizajes justos") + " de " + total;

        /// <summary>El error medio como número entero de la cifra grande: redondeado y nunca menos de 1 (el mismo de la app: <c>Aterrizaje.estimate</c>).</summary>
        private static int EndN(float meanErrorPct) => Math.Max(1, (int)Math.Round(meanErrorPct));

        /// <summary>«11 % de la regla» (el error medio redondeado, nunca menos de 1; con espacio antes del %, igual que la app); «—» sin aterrizajes.</summary>
        public static string EndEstimate(float meanErrorPct) => meanErrorPct < 0f ? "—" : EndN(meanErrorPct) + " % de la regla";

        /// <summary>La línea simple bajo la cifra, con la unidad puesta en algo que se imagina: «Como quedar a 11 en una regla de 0 a 100.»; vacía sin aterrizajes (igual que la app).</summary>
        public static string EndAnalogy(float meanErrorPct) => meanErrorPct < 0f ? "" : "Como quedar a " + EndN(meanErrorPct) + " en una regla de 0 a 100.";

        /// <summary>«2 dianas lunares · racha mayor ×4 · 1 cúpula» (en singular cuando corresponde; sin racha mayor: «—»).</summary>
        public static string EndSummary(int bulls, int bestStreak, int domes) =>
            (bulls == 1 ? "1 diana lunar" : bulls + " dianas lunares") + " · racha mayor " + (bestStreak >= 2 ? "×" + bestStreak : "—") + " · " + (domes == 1 ? "1 cúpula" : domes + " cúpulas");

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
