using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Stroop
{
    /// <summary>De qué orilla llega la palabra: la de la TINTA (izquierda, se toca el COLOR con que está escrita) o la de la PALABRA (derecha, se toca lo que DICE).</summary>
    public enum StroopRule { Ink, Word }

    public readonly struct StroopTrial
    {
        /// <summary>Índice (en <see cref="StroopContract.Colors"/>) de la palabra escrita.</summary>
        public readonly int WordIndex;
        /// <summary>Índice del color de la tinta.</summary>
        public readonly int InkIndex;
        public readonly StroopRule Rule;
        /// <summary>true si la orilla es otra que la de la palabra anterior (un cambio de regla). La primera palabra nunca lo es.</summary>
        public readonly bool Switched;

        public StroopTrial(int wordIndex, int inkIndex, StroopRule rule, bool switched = false)
        {
            WordIndex = wordIndex;
            InkIndex = inkIndex;
            Rule = rule;
            Switched = switched;
        }

        /// <summary>La respuesta correcta depende de la orilla de la que llegó.</summary>
        public int CorrectIndex => Rule == StroopRule.Ink ? InkIndex : WordIndex;

        /// <summary>true si la palabra CHOCA con su tinta (dice un color y está escrita con otro): es lo que hace falta para medir la interferencia.</summary>
        public bool Clash => WordIndex != InkIndex;
    }

    /// <summary>Una respuesta ya dada (solo lo que hace falta para las medidas del final).</summary>
    public readonly struct StroopResponse
    {
        public readonly bool Correct;
        public readonly float Ms;
        public readonly bool Clash;
        public readonly bool Switched;

        public StroopResponse(bool correct, float ms, bool clash, bool switched)
        {
            Correct = correct;
            Ms = ms;
            Clash = clash;
            Switched = switched;
        }
    }

    /// <summary>Qué pide un nivel del DDA común (docs/diseno-tinta-o-palabra.md §5).</summary>
    public readonly struct StroopLevelSpec
    {
        public readonly bool BothRules;
        /// <summary>true = la regla cambia por tramos de 4 a 6 palabras (nivel 3); false con <see cref="BothRules"/> = al azar en cada palabra.</summary>
        public readonly bool Blocks;
        /// <summary>Probabilidad de cambiar de orilla en cada palabra (solo con las dos reglas al azar).</summary>
        public readonly float SwitchChance;
        /// <summary>Fracción de palabras que CHOCAN con su tinta (el resto coincide: hacen falta para medir la interferencia).</summary>
        public readonly float ClashShare;
        /// <summary>Lo que tarda la tarjeta en llegar a su lugar (ms), antes del ajuste por edad.</summary>
        public readonly int ArrivalMs;

        public StroopLevelSpec(bool bothRules, bool blocks, float switchChance, float clashShare, int arrivalMs)
        {
            BothRules = bothRules;
            Blocks = blocks;
            SwitchChance = switchChance;
            ClashShare = clashShare;
            ArrivalMs = arrivalMs;
        }
    }

    /// <summary>
    /// Reglas puras de «Tinta o Palabra» (modelo «Dos orillas», aprobado el 3-oct; ficha: <c>docs/diseno-tinta-o-palabra.md</c>): 4 tintas para todos (paleta
    /// verificada para daltonismo), 5 niveles del DDA común (qué reglas aparecen, cuántas palabras chocan y cuánto tarda en llegar la tarjeta), la generación
    /// de cada palabra y las dos medidas del final (interferencia y costo de cambio). Sin dependencias de UI: se prueba con NUnit.
    /// </summary>
    public static class StroopContract
    {
        public const string GameId = "stroop";
        public const int MaxLevel = 5;
        /// <summary>Modo Precisión (sin reloj): palabras por partida.</summary>
        public const int TotalTrials = 16;
        /// <summary>Duración de la ronda del modo Reto (sin límite de palabras).</summary>
        public const int EndlessSeconds = 60;
        /// <summary>Palabras que se consideran "ritmo completo" en una ronda de 60 s.</summary>
        public const int EndlessTargetTrials = 24;
        /// <summary>Con menos de estos aciertos de cada tipo no se calcula una medida (interferencia, costo de cambio).</summary>
        public const int MinPerKind = 3;
        /// <summary>Las primeras palabras de cada partida llegan siempre por la orilla de la TINTA (primero se aprende una regla).</summary>
        public const int OpeningInkTrials = 3;
        /// <summary>Mayores: la tarjeta llega 1,3 veces más despacio.</summary>
        public const float SeniorArrivalFactor = 1.3f;

        public static readonly string[] Names = { "ROJO", "AZUL", "AMARILLO", "BLANCO" };
        /// <summary>El nombre en minúscula, para las frases de «La tinta era azul» / «La palabra decía rojo».</summary>
        public static readonly string[] LowerNames = { "rojo", "azul", "amarillo", "blanco" };

        /// <summary>Las 4 tintas. Verificadas con <c>python tools/paleta_daltonismo.py C93C3C 4A86E8 F2CC1D F4F4F4 --fondo 141B36</c> (regla: diferencia ≥ 20 en visión
        /// típica, protanopía y deuteranopía). Se quitaron VERDE y MORADO: con deuteranopía el azul y el morado quedaban a 0,9 (no se distinguen).</summary>
        public static readonly Color[] Colors =
        {
            new Color(0xC9 / 255f, 0x3C / 255f, 0x3C / 255f),
            new Color(0x4A / 255f, 0x86 / 255f, 0xE8 / 255f),
            new Color(0xF2 / 255f, 0xCC / 255f, 0x1D / 255f),
            new Color(0xF4 / 255f, 0xF4 / 255f, 0xF4 / 255f),
        };

        /// <summary>La tarjeta de la palabra: #141B36 (los contrastes de la paleta se midieron sobre este fondo).</summary>
        public static readonly Color CardFill = new Color(0x14 / 255f, 0x1B / 255f, 0x36 / 255f);
        /// <summary>La orilla de la TINTA (violeta) y la de la PALABRA (turquesa): no son tintas de respuesta, siempre van con ícono y texto.</summary>
        public static readonly Color InkShore = new Color(0x7C / 255f, 0x5C / 255f, 0xE0 / 255f);
        public static readonly Color WordShore = new Color(0x15 / 255f, 0x9A / 255f, 0x8C / 255f);

        // ------------------------------------------------------------------ tutorial guiado

        /// <summary>
        /// La ronda guiada (docs/diseno-tinta-o-palabra.md §7): 4 palabras, TODAS que chocan (para que se vea por qué manda la orilla): 1) desde la orilla de la TINTA
        /// (ROJO escrito en azul → se toca AZUL), 2) desde la de la PALABRA (AMARILLO escrito en rojo → AMARILLO), y 3 y 4) lo mismo sin aro de ayuda. No cuenta para nada.
        /// </summary>
        public static readonly StroopTrial[] GuidedTrials =
        {
            new StroopTrial(0, 1, StroopRule.Ink),
            new StroopTrial(2, 0, StroopRule.Word, true),
            new StroopTrial(3, 2, StroopRule.Ink, true),
            new StroopTrial(1, 3, StroopRule.Word, true),
        };

        /// <summary>De las palabras guiadas, cuántas llevan el aro de ayuda sobre el botón correcto (las dos primeras).</summary>
        public const int GuidedHinted = 2;
        /// <summary>La tarjeta de la ronda guiada llega despacio (más que el nivel 1): hay tiempo de sobra para mirar de qué orilla viene.</summary>
        public const float GuidedArrivalSeconds = 0.7f;
        /// <summary>Segundos que se espera tras un error (para leer «La tinta era azul») y tras un acierto de la ronda guiada.</summary>
        public const float ErrorPauseSeconds = 1.5f;
        public const float GuidedErrorPauseSeconds = 1.6f;
        public const float GuidedOkPauseSeconds = 0.9f;

        // ------------------------------------------------------------------ niveles

        // 1: solo TINTA, 60 % chocan, 360 ms · 2: solo TINTA, 75 %, 320 · 3: las dos por tramos de 4-6, 75 %, 300 · 4: las dos al azar (~40 % de cambios), 80 %, 280 ·
        // 5: las dos al azar (~50 % de cambios), 85 %, 240.
        private static readonly StroopLevelSpec[] Levels =
        {
            new StroopLevelSpec(false, false, 0f, 0.60f, 360),
            new StroopLevelSpec(false, false, 0f, 0.75f, 320),
            new StroopLevelSpec(true, true, 0f, 0.75f, 300),
            new StroopLevelSpec(true, false, 0.40f, 0.80f, 280),
            new StroopLevelSpec(true, false, 0.50f, 0.85f, 240),
        };

        public static StroopLevelSpec Spec(int level) => Levels[Math.Max(1, Math.Min(MaxLevel, level)) - 1];

        /// <summary>Segundos que tarda la tarjeta en llegar de su orilla a su lugar (no se acepta respuesta hasta que llega; el tiempo de respuesta se mide desde ahí).</summary>
        public static float ArrivalSeconds(int level, bool senior) => Spec(level).ArrivalMs / 1000f * (senior ? SeniorArrivalFactor : 1f);

        // ------------------------------------------------------------------ generación

        /// <summary>
        /// Arma las palabras de una partida una por una: qué orilla toca, y si la palabra choca o coincide con su tinta. Guarda lo que necesita del pasado (la regla anterior y cuánto
        /// lleva el tramo). La orilla se decide por nivel: 1-2 solo TINTA; 3 por tramos de 4 a 6 palabras; 4 y 5 al azar en cada palabra. Las primeras
        /// <see cref="OpeningInkTrials"/> de la partida son de TINTA, y el primer cambio a PALABRA nunca llega antes.
        /// </summary>
        public sealed class Sequencer
        {
            private readonly System.Random _rng;
            private int _index;
            private StroopRule _previous = StroopRule.Ink;
            private int _runLeft;

            public Sequencer(System.Random rng) { _rng = rng; }

            /// <summary>Palabras que ya salieron.</summary>
            public int Count => _index;

            public StroopTrial Next(int level)
            {
                var spec = Spec(level);
                var rule = StroopRule.Ink;
                if (spec.BothRules && _index >= OpeningInkTrials)
                {
                    if (spec.Blocks)
                    {
                        if (_runLeft <= 0)
                        {
                            // un tramo nuevo de la otra regla (el primer tramo de PALABRA arranca recién pasadas las de apertura)
                            rule = _index == OpeningInkTrials ? StroopRule.Word : (_previous == StroopRule.Ink ? StroopRule.Word : StroopRule.Ink);
                            _runLeft = 4 + _rng.Next(3);
                        }
                        else rule = _previous;
                    }
                    else
                    {
                        rule = _rng.NextDouble() < spec.SwitchChance ? (_previous == StroopRule.Ink ? StroopRule.Word : StroopRule.Ink) : _previous;
                    }
                }
                if (spec.Blocks && _runLeft > 0) _runLeft--;
                bool switched = _index > 0 && rule != _previous;
                int word = _rng.Next(Names.Length);
                int ink = word;
                if (_rng.NextDouble() < spec.ClashShare)
                {
                    ink = _rng.Next(Names.Length - 1);
                    if (ink >= word) ink++;
                }
                _previous = rule;
                _index++;
                return new StroopTrial(word, ink, rule, switched);
            }
        }

        // ------------------------------------------------------------------ medidas

        /// <summary>
        /// «Cuánto te frenó la palabra» (interferencia de Stroop, MacLeod 1991): promedio de tiempo de los aciertos con palabra que CHOCA menos el de los que COINCIDEN, en ms
        /// (0 si salió negativo: respondiste igual de rápido o más). −1 si hay menos de <see cref="MinPerKind"/> aciertos de cada tipo.
        /// </summary>
        public static int InterferenceMs(IReadOnlyList<StroopResponse> responses)
        {
            return Difference(responses, r => r.Clash, r => !r.Clash);
        }

        /// <summary>
        /// «Cambiar de orilla te costó» (costo de cambio, Monsell 2003): promedio de tiempo de los aciertos TRAS un cambio de orilla menos el de los que REPITEN orilla, en ms (0 si
        /// salió negativo). −1 si hay menos de <see cref="MinPerKind"/> aciertos de cada tipo (en los niveles 1 y 2 no hay cambios, así que siempre −1).
        /// </summary>
        public static int SwitchCostMs(IReadOnlyList<StroopResponse> responses)
        {
            return Difference(responses, r => r.Switched, r => !r.Switched);
        }

        private static int Difference(IReadOnlyList<StroopResponse> responses, Func<StroopResponse, bool> a, Func<StroopResponse, bool> b)
        {
            double sumA = 0, sumB = 0;
            int nA = 0, nB = 0;
            if (responses != null)
                foreach (var r in responses)
                {
                    if (!r.Correct) continue;
                    if (a(r)) { sumA += r.Ms; nA++; }
                    else if (b(r)) { sumB += r.Ms; nB++; }
                }
            if (nA < MinPerKind || nB < MinPerKind) return -1;
            return Math.Max(0, (int)Math.Round(sumA / nA - sumB / nB));
        }

        // ------------------------------------------------------------------ puntaje

        /// <summary>Puntaje 0-100 del modo sin límite: precisión x ritmo (llegar a
        /// <see cref="EndlessTargetTrials"/> palabras vale el 100% del ritmo). Así ni responder
        /// al azar muy rápido ni acertar todo muy despacio da el máximo.</summary>
        public static int EndlessScore(int correct, int total)
        {
            if (total <= 0) return 0;
            float accuracy = (float)correct / total;
            float pace = Math.Min(1f, (float)total / EndlessTargetTrials);
            return Math.Max(0, Math.Min(100, (int)Math.Round(accuracy * pace * 100f)));
        }

        public static int Score(int correct, int total) =>
            total <= 0 ? 0 : Math.Max(0, Math.Min(100, correct * 100 / total));

        // ------------------------------------------------------------------ textos

        /// <summary>«La tinta era azul» / «La palabra decía rojo»: lo que explica Nubi al fallar, sin culpa.</summary>
        public static string Explain(StroopTrial trial) =>
            trial.Rule == StroopRule.Ink ? "La tinta era " + LowerNames[trial.InkIndex] : "La palabra decía " + LowerNames[trial.WordIndex];

        /// <summary>«Responde: TINTA» / «Responde: PALABRA» (la cinta fija de arriba).</summary>
        public static string Ribbon(StroopRule rule) => rule == StroopRule.Ink ? "Responde: TINTA" : "Responde: PALABRA";

        /// <summary>Qué se toca con cada orilla, en una frase.</summary>
        public static string RuleLine(StroopRule rule) =>
            rule == StroopRule.Ink ? "Toca el color con que está escrita" : "Toca lo que DICE la palabra, no su color";
    }
}
