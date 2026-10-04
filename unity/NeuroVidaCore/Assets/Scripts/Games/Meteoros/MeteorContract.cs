using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Meteoros
{
    /// <summary>Tipo de palabra inventada (señuelo): sílabas recombinadas, una letra cambiada o dos letras interiores
    /// traspuestas ("chocloate"). El orden es el de las listas de telemetría (<c>lex_fa_*</c>).</summary>
    public enum DecoyKind { Obvious = 0, OneLetter = 1, Transposed = 2 }

    /// <summary>Qué trae un nivel: bandas de palabras, tipos de señuelo, cuántos meteoros a la vez, caída y largo.</summary>
    public sealed class MeteorLevelSpec
    {
        public int MinBand, MaxBand;
        public DecoyKind[] Decoys;
        public int Concurrent;
        public float FallSeconds;
        public int MinLen, MaxLen;
    }

    /// <summary>Cómo es un meteoro de la partida: su palabra, si existe, y de qué clase es.</summary>
    public sealed class MeteorSpec
    {
        public string Word;
        public bool IsWord;
        /// <summary>Banda de la palabra (1 = la conoce casi todo el mundo, 6 = rara). En una inventada, la de la palabra
        /// de la que sale (0 si es "obvia").</summary>
        public int Band;
        public DecoyKind Decoy;
        public bool Golden;
        public bool Shower;
        /// <summary>Segundos que tarda en caer hasta la atmósfera.</summary>
        public float FallSeconds;
    }

    /// <summary>
    /// Reglas puras de "Lluvia de meteoros" (juego estrella de Lenguaje; ver docs/diseno-lluvia-de-meteoros.md). Caen
    /// meteoros con palabras: se tocan las que existen y se dejan pasar las inventadas. Es la DECISIÓN LÉXICA "ir / no
    /// ir" (Perea, Rosa y Gómez, 2002; Meyer y Schvaneveldt, 1971). La dificultad sube la rareza de las palabras (bandas
    /// de prevalencia, SPALEX; Aguasvivas et al., 2018) y el parecido de los señuelos con palabras reales (letras
    /// traspuestas: Perea y Lupker, 2003). Las medidas del final siguen la idea de LexTALE (Lemhöfer y Broersma, 2012):
    /// reconocidas menos falsas alarmas. Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class MeteorContract
    {
        public const string GameId = "meteoros";
        public const int MaxLevel = 12;

        /// <summary>Reto: 2 minutos de lluvia.</summary>
        public const int RetoSeconds = 120;
        /// <summary>Versión corta del inicio («Tu punto de partida», con <c>Assessment</c> activo): 60 s (el reloj corre como en el Reto), con 2 meteoros a la vez como mínimo.
        /// Salen unas 10 palabras reales y otras tantas inventadas: alcanza para «reconociste N de M».</summary>
        public const int AssessmentSeconds = 60;

        /// <summary>Segundos de la partida con reloj: 60 en la versión corta, 120 en el Reto.</summary>
        public static int RunSeconds(bool assessment) => assessment ? AssessmentSeconds : RetoSeconds;

        /// <summary>Meteoros a la vez en la versión corta: al menos 2 (hay más palabras que decidir en 60 s).</summary>
        public static int AssessmentConcurrent(int level) => Math.Max(2, Spec(level).Concurrent);

        /// <summary>Paso del DDA (≈ 60 decisiones en el Reto: pasos de tamaño común).</summary>
        public const float StepUp = 0.15f;

        /// <summary>El motor común. La versión corta parte suave (nivel 1 en mayores, 3 en el resto: palabras de las bandas 1-2). Sin tiempo de reacción: cuenta acertar, no la prisa.</summary>
        public static NeuroVida.Games.AdaptiveDifficulty CreateEngine(NeuroVida.Contracts.SequenceConfigDetails config)
        {
            var age = NeuroVida.Games.DdaUserProfileConfig.ParseAgeBand(config.age_band);
            float start = config.assessment ? NeuroVida.Games.Shared.Assessment.SoftStartLevel(age) : NeuroVida.Games.AdaptiveDifficulty.StartRating(config, MaxLevel);
            return new NeuroVida.Games.AdaptiveDifficulty(MaxLevel, age, start, StepUp, useReaction: false);
        }

        // ------------------------------------------------------------------ ronda guiada del tutorial

        /// <summary>El guion de la ronda guiada: 2 palabras reales que se tocan y 1 inventada que se deja caer (true = palabra real). No cuenta para nada.</summary>
        public static readonly bool[] GuidedPlan = { true, false };

        /// <summary>Segundos que tarda en caer cada meteoro de la ronda guiada. Antes 11 s (tiempo muerto: la inventada hay que dejarla caer entera); desde la prueba de Ricardo
        /// (3-oct, «al inicio es bastante lenta y genera mucha pausa») 6 s, y 7,5 s en mayores. Cae de a UNO, sin otros que distraigan: alcanza de sobra para leer la palabra.</summary>
        public const float GuidedFallSeconds = 6f;
        public const float GuidedFallSecondsSenior = 7.5f;
        public static float GuidedFall(bool senior) => senior ? GuidedFallSecondsSenior : GuidedFallSeconds;

        /// <summary>Pausa entre un acierto y el siguiente meteoro guiado: apenas se resuelve uno aparece el otro (antes 2,2 s).</summary>
        public const float GuidedGapSeconds = 0.4f;
        /// <summary>Tras un error SÍ se espera más: se alcanza a leer por qué (sin culpa) antes de repetir el mismo paso.</summary>
        public const float GuidedRetryGapSeconds = 1.8f;
        /// <summary>El «¡Así se juega! Ahora sin ayuda» del cierre (antes 1,8 s).</summary>
        public const float GuidedClosingSeconds = 1.2f;

        /// <summary>Segundos de espera SIN que la persona haga nada en una ronda guiada sin errores: las pausas entre pasos, el cierre y la caída entera de la inventada
        /// (que se deja caer). Antes: 3 × 2,2 + 1,8 + 11 = 19,4 s. Ahora: 3 × 0,4 + 1,2 + 6 = 8,4 s (9,9 s en mayores).</summary>
        public static float GuidedIdleSeconds(bool senior)
        {
            int decoys = 0;
            foreach (bool word in GuidedPlan) if (!word) decoys++;
            return GuidedPlan.Length * GuidedGapSeconds + GuidedClosingSeconds + decoys * GuidedFall(senior);
        }

        /// <summary>El primer meteoro de la versión corta sale a más tardar a estos segundos de empezar la partida (después de la cuenta regresiva).</summary>
        public const float AssessmentFirstSpawnSeconds = 0.5f;
        /// <summary>La versión corta mantiene al menos estos meteoros en pantalla desde el inicio: si hay menos, el siguiente sale enseguida.</summary>
        public const int AssessmentMinOnScreen = 2;
        public const float AssessmentRefillGapSeconds = 0.25f;
        /// <summary>Pausa común entre meteoros de la partida.</summary>
        public const float NormalGapSeconds = 1.1f;
        public const float ShowerGapSeconds = 0.45f;

        /// <summary>Segundos mínimos desde el último meteoro lanzado hasta el siguiente. En la versión corta, con menos de 2 en pantalla el siguiente sale casi enseguida
        /// (no hay pausas con el cielo vacío); la partida normal no cambia.</summary>
        public static float SpawnGap(bool shower, bool assessment, int onScreen)
        {
            if (shower) return ShowerGapSeconds;
            if (assessment && onScreen < AssessmentMinOnScreen) return AssessmentRefillGapSeconds;
            return NormalGapSeconds;
        }

        /// <summary>Precisión (sin reloj): meteoros por partida.</summary>
        public const int PrecisionMeteors = 40;
        /// <summary>Nunca más de tantas inventadas seguidas.</summary>
        public const int MaxDecoyRun = 3;
        /// <summary>Cada cuántos meteoros llega la Lluvia de estrellas, y cuántos trae.</summary>
        public const int ShowerEvery = 25;
        public const int ShowerSize = 6;
        /// <summary>Uno de cada tantos meteoros es dorado.</summary>
        public const int GoldenEvery = 12;
        /// <summary>Racha desde la que la estela se enciende (puntos × 1,5).</summary>
        public const int StreakGlow = 8;
        /// <summary>Palabras vistas que hacen falta en una banda para decir algo de ella.</summary>
        public const int MinBandSeen = 6;
        /// <summary>Toques mínimos para estimar el tiempo de reconocimiento (en comunes y en raras).</summary>
        public const int MinRtSamples = 5;

        private static readonly MeteorLevelSpec[] Levels =
        {
            // nivel 1-2: palabras que todos conocen, señuelos obvios, uno a la vez
            new MeteorLevelSpec { MinBand = 1, MaxBand = 1, Decoys = new[] { DecoyKind.Obvious }, Concurrent = 1, FallSeconds = 7f, MinLen = 4, MaxLen = 6 },
            new MeteorLevelSpec { MinBand = 1, MaxBand = 1, Decoys = new[] { DecoyKind.Obvious }, Concurrent = 1, FallSeconds = 7f, MinLen = 4, MaxLen = 6 },
            // nivel 3-4: bandas 1-2, obvias y de una letra, 1 a 2 a la vez
            new MeteorLevelSpec { MinBand = 1, MaxBand = 2, Decoys = new[] { DecoyKind.Obvious, DecoyKind.OneLetter }, Concurrent = 1, FallSeconds = 6f, MinLen = 4, MaxLen = 7 },
            new MeteorLevelSpec { MinBand = 1, MaxBand = 2, Decoys = new[] { DecoyKind.Obvious, DecoyKind.OneLetter }, Concurrent = 2, FallSeconds = 6f, MinLen = 4, MaxLen = 7 },
            // nivel 5-6: bandas 2-3, una letra cambiada, dos a la vez
            new MeteorLevelSpec { MinBand = 2, MaxBand = 3, Decoys = new[] { DecoyKind.OneLetter }, Concurrent = 2, FallSeconds = 5.5f, MinLen = 5, MaxLen = 8 },
            new MeteorLevelSpec { MinBand = 2, MaxBand = 3, Decoys = new[] { DecoyKind.OneLetter }, Concurrent = 2, FallSeconds = 5.5f, MinLen = 5, MaxLen = 8 },
            // nivel 7-8: bandas 3-4, una letra y letras traspuestas, 2 a 3 a la vez
            new MeteorLevelSpec { MinBand = 3, MaxBand = 4, Decoys = new[] { DecoyKind.OneLetter, DecoyKind.Transposed }, Concurrent = 2, FallSeconds = 5f, MinLen = 5, MaxLen = 9 },
            new MeteorLevelSpec { MinBand = 3, MaxBand = 4, Decoys = new[] { DecoyKind.OneLetter, DecoyKind.Transposed }, Concurrent = 3, FallSeconds = 5f, MinLen = 5, MaxLen = 9 },
            // nivel 9-10: bandas 4-5, traspuestas, 3 a la vez
            new MeteorLevelSpec { MinBand = 4, MaxBand = 5, Decoys = new[] { DecoyKind.Transposed }, Concurrent = 3, FallSeconds = 4.5f, MinLen = 6, MaxLen = 10 },
            new MeteorLevelSpec { MinBand = 4, MaxBand = 5, Decoys = new[] { DecoyKind.Transposed }, Concurrent = 3, FallSeconds = 4.5f, MinLen = 6, MaxLen = 10 },
            // nivel 11-12: bandas 5-6 (raras), traspuestas, 3 a la vez
            new MeteorLevelSpec { MinBand = 5, MaxBand = 6, Decoys = new[] { DecoyKind.Transposed }, Concurrent = 3, FallSeconds = 4f, MinLen = 6, MaxLen = 12 },
            new MeteorLevelSpec { MinBand = 5, MaxBand = 6, Decoys = new[] { DecoyKind.Transposed }, Concurrent = 3, FallSeconds = 4f, MinLen = 6, MaxLen = 12 },
        };

        public static MeteorLevelSpec Spec(int level) => Levels[Clamp(level) - 1];

        /// <summary>Qué trae cada nivel (para el aviso al subir).</summary>
        public static string LevelNews(int level)
        {
            switch (Clamp(level))
            {
                case 3: return "Llegan palabras con una letra cambiada";
                case 4: return "Dos meteoros a la vez";
                case 5: return "Palabras menos comunes";
                case 7: return "Letras cambiadas de lugar";
                case 8: return "Tres meteoros a la vez";
                case 9: return "Palabras poco frecuentes";
                case 11: return "Palabras raras";
                default: return "";
            }
        }

        /// <summary>Segundos de caída del nivel, con los ajustes del modo y de la edad: Precisión × 1,5; mayores × 1,25.</summary>
        public static float FallSeconds(int level, bool precision, bool senior) =>
            Spec(level).FallSeconds * (precision ? 1.5f : 1f) * (senior ? 1.25f : 1f);

        /// <summary>Cuántos meteoros a la vez (Precisión: máximo 2).</summary>
        public static int Concurrent(int level, bool precision) => precision ? Math.Min(2, Spec(level).Concurrent) : Spec(level).Concurrent;

        /// <summary>Tamaño de la letra de las palabras, en dp: 26 y, en mayores, 30.</summary>
        public static int WordSizeDp(bool senior) => senior ? 30 : 26;

        /// <summary>Radio mínimo de la zona de toque, en dp (56 de diámetro; 64 en mayores): la roca ya es más grande.</summary>
        public static float MinTouchDp(bool senior) => senior ? 64f : 56f;

        /// <summary>Letra mínima (dp) a la que se achica una palabra larga que no cabe a lo ancho (solo si es necesario).</summary>
        public const int MinWordDp = 22;
        public const float PlaquePadU = 64f;      // relleno horizontal de la placa (unidades, 3 por dp)
        public const float RockWidthK = 1.45f, RockWidthPadU = 60f, RockHeightK = 3.2f;

        /// <summary>Tamaño de la roca (unidades) para una placa: más ancha que la placa y de alto 3,2 placas, así la
        /// placa ocupa ~1/3 de la roca y la roca se ve.</summary>
        public static (float w, float h) RockSize(float plaqueW, float plaqueH) => (plaqueW * RockWidthK + RockWidthPadU, plaqueH * RockHeightK);

        /// <summary>Letra (dp) con que cabe una palabra: [baseDp] si la roca entra en [maxRockW] (unidades); si no, la
        /// letra baja de a 1 dp hasta que entre, sin pasar de <see cref="MinWordDp"/>. [textWAtBase] = ancho del texto a [baseDp].</summary>
        public static int FitWordDp(int baseDp, float textWAtBase, float maxRockW)
        {
            for (int dp = baseDp; dp > MinWordDp; dp--)
            {
                float textW = textWAtBase * dp / baseDp;
                if (RockSize(textW + PlaquePadU, 0f).w <= maxRockW) return dp;
            }
            return Math.Min(baseDp, MinWordDp);
        }

        /// <summary>¿Se pisan dos meteoros? Cajas de semiejes (rx, ry) centradas en (x, y), con [gap] de aire de sobra. Se usa
        /// la caja de la roca y no solo la placa: nada tapa la placa de otro.</summary>
        public static bool Overlaps(float ax, float ay, float arx, float ary, float bx, float by, float brx, float bry, float gap)
        {
            float dx = Math.Abs(ax - bx) - (arx + brx + gap);
            float dy = Math.Abs(ay - by) - (ary + bry + gap);
            return dx < 0f && dy < 0f;
        }

        // ------------------------------------------------------------------ qué sale

        /// <summary>¿El próximo meteoro es una palabra real? 50% cada uno, y nunca más de <see cref="MaxDecoyRun"/> inventadas seguidas.</summary>
        public static bool NextIsWord(int decoyRun, Random rng) => decoyRun >= MaxDecoyRun || rng.NextDouble() < 0.5;

        /// <summary>¿El meteoro número <paramref name="index"/> (desde 0, sin contar la lluvia) es dorado? 1 de cada <see cref="GoldenEvery"/>.</summary>
        public static bool IsGolden(int index) => index >= 0 && (index + 1) % GoldenEvery == 0;

        /// <summary>¿Toca Lluvia de estrellas después del meteoro número <paramref name="spawned"/> (cantidad ya lanzada)? Cada 25.</summary>
        public static bool ShowerDue(int spawned) => spawned > 0 && spawned % ShowerEvery == 0;

        /// <summary>Banda de la palabra dorada: 2 bandas más rara que el nivel (tope 6).</summary>
        public static int GoldenBand(int level) => Math.Min(6, Spec(level).MaxBand + 2);

        // ------------------------------------------------------------------ puntaje

        /// <summary>Puntos de un acierto: tocar una palabra vale más que dejar pasar una inventada (silencioso).</summary>
        public static int Points(bool tappedWord, int level, int streak, bool golden, bool shower)
        {
            float pts = tappedWord ? 100f + 10f * (Clamp(level) - 1) : 25f;
            if (golden) pts *= 2f;
            if (shower) pts *= 2f;
            if (streak >= StreakGlow) pts *= 1.5f;
            return (int)Math.Round(pts);
        }

        /// <summary>Precisión equilibrada 0..1: (palabras tocadas / palabras + inventadas dejadas pasar / inventadas) / 2.</summary>
        public static float BalancedAccuracy(int wordsSeen, int wordsHit, int decoysSeen, int decoysPassed)
        {
            float w = wordsSeen > 0 ? (float)wordsHit / wordsSeen : 0f;
            float d = decoysSeen > 0 ? (float)decoysPassed / decoysSeen : 0f;
            if (wordsSeen == 0 && decoysSeen == 0) return 0f;
            if (wordsSeen == 0) return d;
            if (decoysSeen == 0) return w;
            return (w + d) / 2f;
        }

        /// <summary>Puntaje 0-100: precisión equilibrada (70%) y nivel más alto alcanzado (30%).</summary>
        public static int Score(float balancedAccuracy, int peakLevel)
        {
            float lv = (float)(Clamp(peakLevel) - 1) / (MaxLevel - 1);
            float a = Math.Max(0f, Math.Min(1f, balancedAccuracy));
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.7f * a + 0.3f * lv) * 100f)));
        }

        // ------------------------------------------------------------------ medidas del final

        /// <summary>
        /// "Tu vocabulario": por banda (1 = común … 6 = rara), % de palabras reconocidas MENOS el % de inventadas
        /// tocadas en toda la partida (la falsa alarma: así no se premia tocar todo). Solo bandas con al menos
        /// <see cref="MinBandSeen"/> palabras vistas; si no, -1.
        /// </summary>
        public static int[] BandPercents(int[] seen, int[] hits, int decoysSeen, int decoysTapped)
        {
            var r = new int[6];
            float fa = decoysSeen > 0 ? 100f * decoysTapped / decoysSeen : 0f;
            for (int b = 0; b < 6; b++)
            {
                int s = seen != null && b < seen.Length ? seen[b] : 0;
                int h = hits != null && b < hits.Length ? hits[b] : 0;
                r[b] = s < MinBandSeen ? -1 : (int)Math.Round(Math.Max(0f, 100f * h / s - fa));
            }
            return r;
        }

        /// <summary>Mediana de una lista de tiempos (ms); -1 si hay menos de <paramref name="minSamples"/>.</summary>
        public static int MedianMs(IReadOnlyList<int> values, int minSamples = MinRtSamples)
        {
            if (values == null || values.Count < minSamples || values.Count == 0) return -1;
            var s = new List<int>(values);
            s.Sort();
            int n = s.Count;
            return n % 2 == 1 ? s[n / 2] : (int)Math.Round((s[n / 2 - 1] + s[n / 2]) / 2.0);
        }

        /// <summary>Banda 1-2 = palabras comunes; banda 5-6 = palabras raras (para el tiempo de reconocimiento).</summary>
        public static bool IsCommonBand(int band) => band == 1 || band == 2;
        public static bool IsRareBand(int band) => band == 5 || band == 6;

        /// <summary>Palabras raras (bandas 5-6) acertadas, sin repetir y en orden de aparición, separadas por coma.</summary>
        public static string RareWordsCsv(IEnumerable<string> words)
        {
            var seen = new HashSet<string>();
            var list = new List<string>();
            foreach (var w in words)
                if (!string.IsNullOrEmpty(w) && seen.Add(w)) list.Add(w);
            return string.Join(",", list);
        }

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
