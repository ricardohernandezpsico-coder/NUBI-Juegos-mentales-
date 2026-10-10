using System;

namespace NeuroVida.Games.Aterrizaje
{
    /// <summary>Un rectángulo en dp (x a la derecha, y hacia abajo): una zona que los avisos no pueden tapar (la nave, la regla, la bandera).</summary>
    public readonly struct LandBox
    {
        public readonly float X0, Y0, X1, Y1;

        public LandBox(float x0, float y0, float x1, float y1)
        {
            X0 = Math.Min(x0, x1);
            Y0 = Math.Min(y0, y1);
            X1 = Math.Max(x0, x1);
            Y1 = Math.Max(y0, y1);
        }

        public bool Intersects(LandBox o) => X0 < o.X1 && X1 > o.X0 && Y0 < o.Y1 && Y1 > o.Y0;
        public float Width => X1 - X0;
        public float Height => Y1 - Y0;
    }

    /// <summary>
    /// Dónde va cada cosa en la pantalla de Aterrizaje Lunar (renovación del 10-oct, docs/diseno-aterrizaje.md §3): dp lógicos de un campo de 360 de ancho, origen arriba a la izquierda. Lógica pura, probada en varias formas de teléfono. La referencia es una pantalla de 360 × 780 dp (20:9):
    /// el marcador común (63 dp), la misión («Aterriza en» y el número en grande, y 96-138), la nave que arranca en y 196 y se posa 50 dp sobre la regla, la regla de x 36 a x 324 (y 604, alto 20, con sus extremos en letra de 20 debajo), la instrucción (y 748), los avisos en el cielo vacío (y 300 y 340) y la Tierra arriba a la derecha.
    /// Todo lo de ABAJO (la regla, la luna, las cordilleras, la instrucción) va anclado al borde de abajo: en una pantalla más baja (16:9, o con el tutorial, que reserva 96 dp abajo) la nave baja menos y en una más alta cae más; los avisos suben para no tocar nunca la bandera.
    /// </summary>
    public readonly struct LandingPlan
    {
        public const float Width = 360f;
        /// <summary>Lo que ocupa el marcador común de arriba (190 unidades de 1080 → 63,3 dp).</summary>
        public const float HudBottom = 190f / 3f;
        public const float TutorialControls = 96f;
        public const float ReferenceHeight = 780f;

        // la regla (en x; el largo lo fija el boceto: de 36 a 324)
        public const float RulerX0 = 36f, RulerX1 = 324f, RulerH = 20f;
        /// <summary>La regla, medida desde el borde de abajo en la referencia (780 − 604).</summary>
        public const float BelowRuler = 176f;
        /// <summary>Lo más arriba que puede quedar la regla (pantallas muy bajas).</summary>
        public const float MinRulerY = 380f;
        /// <summary>Dónde arranca la nave (su centro) y cuánto por encima de la regla se posa (su centro).</summary>
        public const float StartY = 196f, RestAboveRuler = 50f;
        /// <summary>El dedo mueve la nave desde cualquier parte del cielo: de y 160 hacia abajo.</summary>
        public const float TouchTopY = 160f;

        // la nave, ya con su ×1,25 (centro en el medio de la cabina/base): hasta la antena de la cabina arriba y hasta las patas abajo
        public const float LanderScale = 1.25f, LanderTop = 30f, LanderFoot = 32f, LanderHalfWidth = 40f;
        /// <summary>El lado del dibujo de la nave (72 dp del boceto × 1,25).</summary>
        public const float LanderSide = 90f;

        // la misión
        public const float MissionLabelY = 96f, MissionNumberY = 138f, MissionNumberSize = 52f;

        // el alto de la bandera y de su etiqueta
        public const float FlagHeight = 80f, FlagLabelRise = 22f, FlagLabelHalfH = 14f, FlagLabelHalfW = 56f;

        // el aviso (píldora de 34 dp)
        public const float NoticeH = 34f, NoticeMargin = 28f;

        public readonly float Height, EffectiveHeight, RulerY, RestY, HintY, NoticeBigY, NoticeSmallY, EarthX, EarthY, EarthR;

        public LandingPlan(float height, bool guided)
        {
            Height = height;
            float eff = height - (guided ? TutorialControls : 0f);
            EffectiveHeight = eff;
            RulerY = Math.Max(MinRulerY, eff - BelowRuler);
            RestY = RulerY - RestAboveRuler;
            HintY = height - (ReferenceHeight - 748f);
            // los avisos: el de las dianas y las cúpulas (grande) arriba y el de «Justo» y «Cerca» abajo, SIEMPRE encima de la etiqueta de la bandera
            float flagTop = RulerY - FlagHeight - FlagLabelRise - FlagLabelHalfH;
            NoticeSmallY = Math.Min(340f, flagTop - 6f - NoticeH * 0.5f);
            NoticeBigY = NoticeSmallY - 40f;
            EarthX = 318f;
            EarthR = 58f;
            EarthY = Math.Min(300f, RulerY - 190f);
        }

        /// <summary>El x (dp) de la fracción <paramref name="f"/> (0 a 1) de la regla.</summary>
        public static float FracX(float f) => RulerX0 + Math.Max(0f, Math.Min(1f, f)) * (RulerX1 - RulerX0);

        /// <summary>La fracción de la regla que le toca al x (dp), sin pasarse de los extremos.</summary>
        public static float FracOf(float x) => Math.Max(0f, Math.Min(1f, (x - RulerX0) / (RulerX1 - RulerX0)));

        /// <summary>La regla con sus marcas y los números de los extremos (que cuelgan debajo).</summary>
        public LandBox RulerBox => new LandBox(RulerX0 - 8f, RulerY - 6f, RulerX1 + 8f, RulerY + RulerH + 36f);

        /// <summary>La nave posada con su centro en <paramref name="x"/>.</summary>
        public LandBox LanderBox(float x) => new LandBox(x - LanderHalfWidth, RestY - LanderTop, x + LanderHalfWidth, RestY + LanderFoot);

        /// <summary>La bandera plantada en <paramref name="x"/>: el asta, el gallardete y su etiqueta (que no se sale de la pantalla).</summary>
        public LandBox FlagBox(float x)
        {
            float cx = Math.Max(10f + FlagLabelHalfW, Math.Min(Width - 10f - FlagLabelHalfW, x));
            return new LandBox(Math.Min(x - 6f, cx - FlagLabelHalfW), RulerY - FlagHeight - FlagLabelRise - FlagLabelHalfH, Math.Max(x + 30f, cx + FlagLabelHalfW), RulerY);
        }

        /// <summary>Un aviso de ancho <paramref name="width"/> (dp), centrado, en la fila grande o en la chica.</summary>
        public LandBox NoticeBox(bool big, float width)
        {
            float cy = big ? NoticeBigY : NoticeSmallY;
            return new LandBox(Width * 0.5f - width * 0.5f, cy - NoticeH * 0.5f, Width * 0.5f + width * 0.5f, cy + NoticeH * 0.5f);
        }

        /// <summary>Lo más ancho que puede ser un aviso.</summary>
        public const float NoticeMaxWidth = Width - 2f * NoticeMargin;

        /// <summary>El número de la misión (arriba, bajo el marcador): lo que los avisos tampoco pisan.</summary>
        public LandBox MissionBox => new LandBox(30f, MissionLabelY - 14f, Width - 30f, MissionNumberY + 30f);
    }
}
