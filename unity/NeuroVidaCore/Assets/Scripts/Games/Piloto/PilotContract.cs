using System;

namespace NeuroVida.Games.Piloto
{
    /// <summary>Una señal que aparece en el espacio durante el vuelo: tocarla solo si es la de la misión.</summary>
    public readonly struct PilotSignal
    {
        /// <summary>Índice en <see cref="PilotContract.Shapes"/>.</summary>
        public readonly int Shape;
        /// <summary>Variante de color del ícono (0..2, ver <c>SymbolSprite</c>; la 3 es un tono análogo de la 0 y no se
        /// usa: dos señales de la misma forma deben diferenciarse bien por color).</summary>
        public readonly int Variant;
        public readonly bool IsTarget;
        /// <summary>Posición normalizada dentro de la zona de señales (0..1; x = ancho, y = alto).</summary>
        public readonly float X, Y;
        public readonly bool Peripheral;

        public PilotSignal(int shape, int variant, bool isTarget, float x, float y, bool peripheral)
        {
            Shape = shape;
            Variant = variant;
            IsTarget = isTarget;
            X = x;
            Y = y;
            Peripheral = peripheral;
        }
    }

    /// <summary>
    /// Reglas puras de "Piloto Estelar": entrenamiento de MULTITAREA inspirado en NeuroRacer (Anguera et al., Nature
    /// 2013), con señales periféricas breves como en el entrenamiento de campo visual útil (UFOV, ensayo ACTIVE:
    /// Ball et al., JAMA 2002). Dos tareas a la vez, cada una con su propia dificultad adaptativa:
    /// <list type="bullet">
    /// <item><b>Pilotaje</b> (seguimiento visomotor continuo): mantener la nave dentro de una ruta que serpentea. La
    /// dificultad sube la velocidad, estrecha la ruta y la hace más sinuosa.</item>
    /// <item><b>Señales</b> (go/no-go): tocar solo las señales de la misión (forma + color) e ignorar el resto. La
    /// dificultad acorta el tiempo de cada señal, las hace más seguidas, agrega distractores parecidos (misma forma,
    /// otro color) y desde el nivel 5 las lleva a la periferia.</item>
    /// </list>
    /// El vuelo empieza con <see cref="AutopilotSeconds"/> de piloto automático (solo señales): esa parte mide la
    /// tarea de señales sola y permite calcular el <see cref="MultitaskCost"/> (cuánto empeora al hacer las dos a la
    /// vez), la medida propia de NeuroRacer. Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class PilotContract
    {
        public const string GameId = "piloto";
        public const int MaxLevel = 9;

        /// <summary>Reto: vuelo de 90 s (15 de piloto automático + 75 a los mandos).</summary>
        public const int FlightSeconds = 90;
        public const int AutopilotSeconds = 15;
        /// <summary>Precisión (sin reloj): el vuelo termina tras esta cantidad de señales a los mandos.</summary>
        public const int PrecisionSignals = 24;

        /// <summary>Ventana de pilotaje: cada tanto se evalúa si la nave estuvo en la ruta (un "ensayo" del DDA).</summary>
        public const float DriveWindowSeconds = 1.5f;
        /// <summary>Fracción de la ventana dentro de la ruta para contarla como acierto.</summary>
        public const float DriveWindowPass = 0.85f;

        /// <summary>Proporción de señales que son de la misión.</summary>
        public const float TargetChance = 0.4f;

        /// <summary>Íconos que se usan como señales (índices de <c>ShapeKind</c> de Parejas: se eligen los que se
        /// distinguen bien por silueta aun en un vistazo): estrella, cometa, planeta, luna, cohete, platillo.</summary>
        public static readonly int[] Shapes = { 3, 2, 0, 4, 1, 5 };

        // ------------------------------------------------------------------ pilotaje

        /// <summary>Velocidad de avance de la ruta (unidades de lienzo por segundo).</summary>
        public static float ScrollSpeed(int level, bool precision)
        {
            float v = 360f + 48f * (Clamp(level) - 1);
            return precision ? v * 0.8f : v;
        }

        /// <summary>Medio ancho de la ruta como fracción del ancho de pantalla: 0.30 (fácil) → 0.15.</summary>
        public static float LaneHalfWidth(int level) => 0.30f - 0.15f * (Clamp(level) - 1) / (MaxLevel - 1);

        /// <summary>Cuánto se aleja la ruta del centro (fracción del ancho): 0.10 → 0.30.</summary>
        public static float Curviness(int level) => 0.10f + 0.20f * (Clamp(level) - 1) / (MaxLevel - 1);

        /// <summary>
        /// Centro de la ruta (0..1 del ancho) a la distancia recorrida <paramref name="d"/> (en alturas de pantalla),
        /// con amplitud <paramref name="amp"/>. Suma de dos senos de frecuencias no múltiplos: no se repite a la vista.
        /// La amplitud la guarda cada tramo de la ruta al crearse, así un cambio de nivel no deforma lo ya visible.
        /// </summary>
        public static float CenterAt(float d, float amp, float halfWidth)
        {
            float wave = 0.62f * (float)Math.Sin(d * 2.1f) + 0.38f * (float)Math.Sin(d * 3.7f + 1.3f);
            float x = 0.5f + amp * wave;
            // La ruta nunca se sale de la pantalla.
            float min = halfWidth + 0.04f, max = 1f - halfWidth - 0.04f;
            return Math.Max(min, Math.Min(max, x));
        }

        /// <summary>¿La nave (x normalizada) está dentro de la ruta?</summary>
        public static bool InLane(float shipX, float center, float halfWidth) => Math.Abs(shipX - center) <= halfWidth;

        // ------------------------------------------------------------------ señales

        /// <summary>Segundos entre una señal y la siguiente: 2.2 → 1.1.</summary>
        public static float SignalGap(int level, bool precision)
        {
            float g = 2.2f - 1.1f * (Clamp(level) - 1) / (MaxLevel - 1);
            return precision ? g * 1.15f : g;
        }

        /// <summary>Cuánto dura visible cada señal (ms): 1600 → 650.</summary>
        public static int ExposureMs(int level, bool precision)
        {
            int ms = 1600 - (int)Math.Round(950f * (Clamp(level) - 1) / (MaxLevel - 1));
            return precision ? (int)(ms * 1.3f) : ms;
        }

        /// <summary>Probabilidad de que un distractor tenga LA MISMA forma que la misión (otro color): obliga a mirar
        /// forma y color juntos. 0 en los niveles 1-2.</summary>
        public static float LookalikeChance(int level)
        {
            int l = Clamp(level);
            return l <= 2 ? 0f : l <= 4 ? 0.3f : 0.55f;
        }

        /// <summary>Probabilidad de que la señal aparezca en la periferia (desde el nivel 5).</summary>
        public static float PeripheralChance(int level)
        {
            int l = Clamp(level);
            return l < 5 ? 0f : Math.Min(0.7f, 0.25f + 0.1f * (l - 5));
        }

        /// <summary>La misión de un vuelo: forma y variante de color a atrapar.</summary>
        public const int Variants = 3;

        public static (int shape, int variant) PickMission(Random rng) =>
            (rng.Next(Shapes.Length), rng.Next(Variants));

        public static PilotSignal NextSignal(int level, int missionShape, int missionVariant, Random rng)
        {
            bool target = rng.NextDouble() < TargetChance;
            int shape = missionShape, variant = missionVariant;
            if (!target)
            {
                if (rng.NextDouble() < LookalikeChance(level))
                {
                    // Misma forma, otro color.
                    variant = (missionVariant + 1 + rng.Next(Variants - 1)) % Variants;
                }
                else
                {
                    shape = (missionShape + 1 + rng.Next(Shapes.Length - 1)) % Shapes.Length;
                    variant = rng.Next(Variants);
                }
            }
            bool peripheral = rng.NextDouble() < PeripheralChance(level);
            float x = peripheral
                ? (rng.NextDouble() < 0.5 ? 0.08f + 0.10f * (float)rng.NextDouble() : 0.82f + 0.10f * (float)rng.NextDouble())
                : 0.30f + 0.40f * (float)rng.NextDouble();
            float y = 0.15f + 0.75f * (float)rng.NextDouble();
            return new PilotSignal(shape, variant, target, x, y, peripheral);
        }

        // ------------------------------------------------------------------ puntaje

        /// <summary>Puntos por señal atrapada, con bono de racha y el multiplicador de "hiperimpulso".</summary>
        public static int PointsForCatch(int streak, bool boost) => (100 + 20 * Math.Min(Math.Max(streak - 1, 0), 10)) * (boost ? 2 : 1);

        /// <summary>
        /// Precisión en la tarea de señales: aciertos (atrapadas) menos falsas alarmas, sobre su total ("reconocimiento
        /// corregido" Pr = H − FA, Snodgrass y Corwin, 1988). 0..1; null si faltan señales de alguno de los dos tipos.
        /// </summary>
        public static float? SignalAccuracy(int hits, int targets, int falseAlarms, int nonTargets)
        {
            if (targets <= 0 || nonTargets <= 0) return null;
            float h = (float)hits / targets, fa = (float)falseAlarms / nonTargets;
            return Math.Max(0f, Math.Min(1f, h - fa));
        }

        /// <summary>Costo de multitarea (%): cuánto baja la precisión en señales al pilotar al mismo tiempo, respecto de
        /// hacerlas solas (piloto automático). 0 = sin costo. -1 = sin datos suficientes.</summary>
        public static int MultitaskCost(float? single, float? dual)
        {
            if (single == null || dual == null || single.Value < 0.2f) return -1;
            float cost = (single.Value - dual.Value) / single.Value;
            return (int)Math.Round(Math.Max(0f, Math.Min(1f, cost)) * 100f);
        }

        /// <summary>Puntaje 0-100 del vuelo: señales (55%) y tiempo dentro de la ruta (45%).</summary>
        public static int Score(float signalAccuracy, float inLaneFraction) =>
            Math.Max(0, Math.Min(100, (int)Math.Round((0.55f * Clamp01(signalAccuracy) + 0.45f * Clamp01(inLaneFraction)) * 100f)));

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
        private static float Clamp01(float v) => Math.Max(0f, Math.Min(1f, v));
    }
}
