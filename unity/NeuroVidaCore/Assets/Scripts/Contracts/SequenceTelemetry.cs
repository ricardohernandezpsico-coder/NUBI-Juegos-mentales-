using System;

namespace NeuroVida.Contracts
{
    /// <summary>
    /// Contrato JSON Unity -> Nativo al terminar la partida de "Secuencia Lumínica".
    /// El lado nativo (Kotlin) escribe esto en la misma tabla Room de siempre
    /// (<c>GameProgressEntity</c>/<c>NeuroVidaRepository.recordGameResult</c>) — Unity no
    /// sabe nada de Room, solo entrega el resultado, igual que hoy hace
    /// <c>SequenceGameContainer.endSession</c> hacia <c>onFinish</c>.
    /// </summary>
    [Serializable]
    public class SequenceTelemetry
    {
        public string user_id;
        public string game_id;
        public SequenceSessionMetrics session_metrics;
    }

    [Serializable]
    public class SequenceSessionMetrics
    {
        public int correct_rounds;
        public int total_rounds;
        public int calculated_score;
        /// <summary>Tiempo de reacción promedio (ms) de toda la partida — alimenta el
        /// bono de velocidad, mismo criterio que <c>endSession</c> en Kotlin.</summary>
        public double average_response_time_ms;
        /// <summary>Span alcanzado en la última ronda jugada — útil para depurar el DDA
        /// sin tener que reconstruir la partida entera desde el log.</summary>
        public int final_span_length;
        /// <summary>Eco de <c>SequenceInitConfig.config.level</c>/<c>.timed</c> — mismo
        /// criterio que <c>NeuroVidaViewModel.finishActiveGame</c> (usa
        /// <c>ActiveGameSession.level/.timed</c>, los valores con que se lanzó la
        /// partida, no un recálculo). Cierra los dos TODO de <c>NativeReceiver.kt</c> del
        /// lado Kotlin, que hasta ahora no tenía cómo saberlos.</summary>
        public int level;
        public bool timed;
        /// <summary>Nivel más alto alcanzado (1..16): se mantiene por compatibilidad; la app usa <see cref="end_rating"/>
        /// y solo cae a este si falta (versiones viejas de Unity).</summary>
        public int peak_level;
        /// <summary>Rating final del DDA común normalizado 0..1 (0 = lo más fácil de los 16 niveles). Lo guarda la app.</summary>
        public float end_rating;
        /// <summary>Secuencias y aciertos DESPUÉS del calentamiento, para decidir si un Desafío o un Experto se superó.</summary>
        public int mode_trials;
        public int mode_hits;
    }
}
