using System;

namespace NeuroVida.Contracts
{
    /// <summary>
    /// Contrato JSON Unity -> Nativo al terminar "Tinta o Palabra" (Stroop). El contrato de
    /// ENTRADA reusa <see cref="SequenceInitConfig"/> como el resto de los juegos.
    /// </summary>
    [Serializable]
    public class StroopTelemetry
    {
        public string user_id;
        public string game_id;
        public StroopSessionMetrics session_metrics;
    }

    [Serializable]
    public class StroopSessionMetrics
    {
        public int correct_trials;
        public int total_trials;
        public int calculated_score;
        /// <summary>Tiempo medio de respuesta de los ensayos respondidos, en milisegundos.</summary>
        public int average_response_time_ms;
        public int level;
        public bool timed;
        /// <summary>Rating final del DDA común normalizado 0..1 (0 = lo más fácil de la escalera del juego).
        /// Para que la app lo guarde entre sesiones (hoy solo se registra).</summary>
        public float end_rating;
        /// <summary>Nivel más alto alcanzado en la partida.</summary>
        public int peak_level;
        /// <summary>Solo Piloto Estelar: costo de multitarea en % (cuánto baja la precisión en señales al pilotar a la
        /// vez). -1 = no aplica o sin datos.</summary>
        public int multitask_cost = -1;
        /// <summary>Solo Radar: "tu vistazo" en ms (duración de destello en la que se asentó la escalera). -1 = no aplica.</summary>
        public int glance_ms = -1;
        /// <summary>Solo Radar: aciertos de ubicación por dirección (8, 0 = arriba y en sentido horario).</summary>
        public int[] sector_hits;
        /// <summary>Solo Radar: ensayos por dirección (8).</summary>
        public int[] sector_trials;
        /// <summary>Solo Satélites: "tu seguimiento", cuántos se siguen de verdad a la vez (descontando la suerte). -1 = no aplica.</summary>
        public float tracking_capacity = -1f;
        /// <summary>Solo Satélites: cuántos había que seguir por ronda, en promedio (el techo de "tu seguimiento"). -1 = no aplica.</summary>
        public float tracking_targets = -1f;
        /// <summary>Solo Bitácora de Misión: fase jugada ("" completa, "encode" transmisión, "recall" informe).</summary>
        public string mem_phase = "";
        /// <summary>Solo Bitácora: semilla y nivel de la misión, paradas, aprendidas en el primer repaso (y cuáles, en bits),
        /// recordadas en el informe (y cuáles), hallazgos elegidos que no estaban, paradas de la ruta en su lugar y
        /// segundos entre la transmisión y el informe. -1 = no aplica.</summary>
        public int mem_seed = -1;
        public int mem_level = -1;
        public int mem_items = -1;
        public int mem_learned = -1;
        public int mem_learned_mask = -1;
        public int mem_recalled = -1;
        public int mem_recalled_mask = -1;
        public int mem_intrusions = -1;
        public int mem_order_ok = -1;
        public int mem_delay_s = -1;
        /// <summary>Solo Satélites: velocidad del nivel más alto superado completo, como múltiplo de la del nivel 1. -1 = ninguno.</summary>
        public float tracking_speed = -1f;
        /// <summary>Solo Freno de Emergencia: "tu freno" (tiempo de frenado, SSRT, ms). -1 = sin estimación confiable.</summary>
        public int brake_ms = -1;
        /// <summary>Solo Freno de Emergencia: altos frenados y altos totales.</summary>
        public int stops_ok;
        public int stops_total;
        /// <summary>Solo Freno de Emergencia: el alto más tardío que se frenó (ms). -1 = ninguno.</summary>
        public int brake_best_ssd_ms = -1;
        /// <summary>Solo Aterrizaje Lunar: error medio en % del largo de la regla. -1 = no aplica.</summary>
        public float numline_error_pct = -1f;
        /// <summary>Solo Aterrizaje Lunar: en cada aterrizaje, dónde estaba el blanco y dónde se posó (0..1 de la regla).</summary>
        public float[] numline_true;
        public float[] numline_given;
        /// <summary>Solo Aterrizaje Lunar: dianas (justo en el blanco).</summary>
        public int numline_bullseyes;
        /// <summary>Solo Acoplamiento: "tu giro mental" (grados por segundo). -1 = sin medida.</summary>
        public int rotation_speed_dps = -1;
        /// <summary>Solo Acoplamiento: tiempo medio de los aciertos a 0°, 45°, 90°, 135° y 180° (-1 = sin datos).</summary>
        public int[] rotation_curve_ms;
        /// <summary>Solo Tráfico Estelar: "tu anticipación" (mediana, ms, de cuánto antes se preparan los desvíos). -1 = sin medida.</summary>
        public int traffic_lead_ms = -1;
        /// <summary>Solo Tráfico Estelar: % de desvíos preparados con tiempo (≥ 1 s). -1 = sin medida.</summary>
        public int traffic_proactive_pct = -1;
        /// <summary>Solo Tráfico Estelar: más cápsulas en viaje a la vez.</summary>
        public int traffic_peak_pods;
        /// <summary>Solo Rumbo a Casa: "tu brújula interna", a qué distancia de casa quedó la nave en promedio (% de la
        /// distancia que había hasta la base). -1 = no aplica.</summary>
        public float homing_error_pct = -1f;
        /// <summary>Solo Rumbo a Casa: dónde quedó cada vuelta en el marco de la vuelta justa, en fracciones de la
        /// distancia a casa (la base en along = 1, lateral = 0; lateral + = a la derecha).</summary>
        public float[] homing_along;
        public float[] homing_lateral;
        /// <summary>Solo Rumbo a Casa: 1 si ese viaje tenía faro.</summary>
        public int[] homing_beacon;
        /// <summary>Solo Rumbo a Casa: llegadas perfectas.</summary>
        public int homing_perfect;
        /// <summary>Solo Correo Estelar: encargos por lugar (planetas entregados de los que pasaron), planetas tocados que no
        /// eran del encargo (y cuántos de color parecido), encargos por hora (avisos a tiempo de las horas que hubo; período
        /// en segundos, 0 = sin radio), avisos a destiempo, miradas al reloj (y cuántas justo antes de la hora), % en la ruta y
        /// sobres. -1 = no aplica.</summary>
        public int mail_event_hits = -1;
        public int mail_event_total = -1;
        public int mail_commissions = -1;
        public int mail_lure_commissions = -1;
        public int mail_radio_hits = -1;
        public int mail_radio_total = -1;
        public int mail_radio_offtime = -1;
        public int mail_radio_period_s = -1;
        public int mail_clock_checks = -1;
        public int mail_clock_late = -1;
        public int mail_lane_pct = -1;
        public int mail_envelopes = -1;
        public int mail_envelopes_total = -1;
        /// <summary>Solo Correo Estelar: asteroides chocados y asteroides que pasaron junto a la nave (esquivados + chocados).</summary>
        public int mail_asteroid_hits = -1;
        public int mail_asteroids = -1;
        /// <summary>Correo Estelar: % del vuelo con el escudo entero (cuidado de la nave).</summary>
        public int mail_hull_intact_pct = -1;
        /// <summary>Correo Estelar: reparaciones de emergencia (veces que se quedó sin escudo).</summary>
        public int mail_emergencies = -1;
    }
}
