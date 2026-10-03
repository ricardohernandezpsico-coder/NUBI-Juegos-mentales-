using NeuroVida.Contracts;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Evaluación inicial "Tu punto de partida" (onboarding de la app, al estilo del Fit Test de Lumosity o la
    /// evaluación de Peak/Elevate): 3 juegos cortos que buscan el nivel de la persona para empezar cada juego a su
    /// medida. La app la pide con <c>config.assessment</c>; acá queda el estado de la partida actual:
    /// - La cuenta regresiva dice "Punto de partida · 2 de 3" en vez de "Prepárate".
    /// - El DDA común calibra más rápido (<see cref="AdaptiveDifficulty.FastCalibration"/>).
    /// - Secuencia Lumínica también usa el DDA común (parte del medio de la escala, 3 vidas y 12 secuencias como máximo).
    /// Lo fija <c>GameEntryPoint</c> con cada config (las partidas normales lo dejan apagado).
    /// </summary>
    public static class Assessment
    {
        public static bool Active { get; private set; }
        public static int Step { get; private set; }
        public static int Total { get; private set; }

        public static void Configure(SequenceConfigDetails config)
        {
            Active = config != null && config.assessment;
            Step = Active ? config.assessment_step : 0;
            Total = Active ? config.assessment_total : 0;
            AdaptiveDifficulty.FastCalibration = Active;
        }

        /// <summary>Nivel donde parte la versión corta del inicio (con <see cref="Active"/>) en los juegos con escalera propia: suave en mayores (1) y un poco más arriba en
        /// el resto (<paramref name="adultLevel"/>, por defecto 3). Con la calibración rápida, el rating final llega a la medida de la persona.</summary>
        public static float SoftStartLevel(AgeBand age, float adultLevel = 3f) => age == AgeBand.Senior ? 1f : adultLevel;

        /// <summary>Subtítulo de la cuenta regresiva: el paso de la evaluación, o <paramref name="normal"/>.</summary>
        public static string Subtitle(string normal) =>
            !Active ? normal : Total > 0 ? $"Punto de partida · {Step} de {Total}" : "Punto de partida";
    }
}
