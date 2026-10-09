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
        /// <summary>Ensayos y aciertos DESPUÉS del calentamiento del DDA: la app decide con ellos si un Desafío o un
        /// Experto se superó (Skill.passed). 0 = no aplica.</summary>
        public int mode_trials;
        public int mode_hits;
        /// <summary>Solo Tinta o Palabra («Dos orillas»): «Cuánto te frenó la palabra», en ms: promedio de tiempo de los aciertos con palabra que CHOCA menos el de los que COINCIDEN
        /// (interferencia de Stroop). -1 con menos de 3 aciertos de cada tipo o en otro juego.</summary>
        public int interference_ms = -1;
        /// <summary>Solo Tinta o Palabra: «Cambiar de orilla te costó», en ms: promedio de tiempo de los aciertos tras un cambio de orilla menos el de los que repiten orilla (costo de cambio).
        /// -1 con menos de 3 aciertos de cada tipo (en los niveles 1 y 2 no hay cambios) o en otro juego.</summary>
        public int switch_cost_ms = -1;
        /// <summary>Solo «Rescate relámpago» (id radar): "tu vistazo" en ms (media geométrica de las duraciones REALES del destello de las rondas normales desde la 4.ª; -1 con menos de 5 rondas) y con cuántas cápsulas a la vez (-1 = sin dato).</summary>
        public int glance_ms = -1;
        public float glance_load = -1f;
        /// <summary>Solo Rescate relámpago: "tu captura", cuántas cápsulas se nombran bien de un vistazo (de 4; promedio de aciertos − 2 × elegidas que no estaban en las lluvias, mínimo 0; -1 con menos de 2 lluvias).</summary>
        public float capture = -1f;
        /// <summary>Solo Rescate relámpago (renovado el 9-oct; docs/diseno-rescate.md §6): cápsulas rescatadas en la partida, rondas perfectas, rondas jugadas (con lluvias), racha mayor de rondas perfectas, el destello más corto (ms reales) con que se resolvió una ronda normal perfecta (-1 = ninguna), el récord de cápsulas rescatadas (el guardado o el de esta partida, el mayor) y 1 si esta partida lo superó. -1 = no aplica.</summary>
        public int resc_rescued = -1;
        public int resc_perfect = -1;
        public int resc_rounds = -1;
        public int resc_best_streak = -1;
        public int resc_shortest_ms = -1;
        public int resc_best = -1;
        public int resc_new;
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
        // ---- «La estación de correo» (id correo desde el 8-oct; docs/diseno-correo-estacion.md §7). -1 = no aplica / sin dato (reemplaza a los mail_* del vuelo, que ya no existen).
        /// <summary>Encargos por evento (cartas señal con sello dorado o lazo que se alcanzaron a resolver) y los que fueron a la caja fuerte.</summary>
        public int mail_ev_hits = -1;
        public int mail_ev_total = -1;
        /// <summary>Encargos por hora (faro) que no fueron cancelados, y los encendidos a tiempo.</summary>
        public int mail_time_hits = -1;
        public int mail_time_total = -1;
        /// <summary>Encargos cancelados por la radio y cuántas veces se hizo igual (error de comisión); veces que se encendió el faro antes de hora.</summary>
        public int mail_cancels = -1;
        public int mail_commissions = -1;
        public int mail_early = -1;
        /// <summary>Miradas al reloj y cuántas fueron cerca de la hora (en el 30 % del día antes de la ventana o dentro de ella).</summary>
        public int mail_peeks = -1;
        public int mail_peeks_good = -1;
        /// <summary>Cartas bien puestas, cartas clasificadas (se tocó un buzón), mejor racha de cartas del día y días con todos los encargos cumplidos.</summary>
        public int mail_right = -1;
        public int mail_sorted = -1;
        public int mail_best_combo = -1;
        public int mail_days_perfect = -1;
        // Cartas señal que se escaparon, por tipo (sello dorado / lazo): el consejo nombra la que de verdad se perdió (docs §12).
        public int mail_gold_missed = -1;
        public int mail_lazo_missed = -1;
        /// <summary>Grupo de etapas más alto (1..5), el récord «cartas en un día perfecto» (el guardado o el de esta partida, el mayor) y 1 si esta partida lo superó.</summary>
        public int mail_group = -1;
        public int mail_best = -1;
        public int mail_new;
        // ---- «Piloto Estelar: la ruta de las balizas» (renovado el 9-oct; docs/diseno-piloto.md §9). TODO se mide con las dos tareas juntas: no hay medida de una tarea sola. -1 = no aplica / sin dato.
        /// <summary>% del vuelo con la nave dentro de la ruta; señales de la misión atrapadas y resueltas (atrapadas + las que se fueron); toques equivocados.</summary>
        public int pil_lane_pct = -1;
        public int pil_hits = -1;
        public int pil_targets = -1;
        public int pil_false = -1;
        /// <summary>«Tus señales a los mandos»: (aciertos − toques equivocados) / señales de la misión, en % (-1 con menos de 8 señales de la misión).</summary>
        public int pil_signal_pct = -1;
        /// <summary>Nivel asentado de cada motor (1..9): señales y pilotaje; mejor racha de señales bien resueltas, puntos y veces que se entró en hiperimpulso.</summary>
        public int pil_signal_level = -1;
        public int pil_drive_level = -1;
        public int pil_best_streak = -1;
        public int pil_points = -1;
        public int pil_hyper = -1;
        // ---- «Satélites: enciende tu planeta» (renovado el 9-oct; docs/diseno-satelites.md §8). -1 = no aplica / sin dato. Siguen los tracking_capacity / tracking_targets / tracking_speed de siempre.
        /// <summary>Luces encendidas en la partida (mensajes entregados), rondas perfectas (todos los k), racha mayor de rondas perfectas, el récord de luces (el guardado o el de esta partida, el mayor) y 1 si esta partida lo superó.</summary>
        public int sat_lights = -1;
        public int sat_perfect = -1;
        public int sat_best_streak = -1;
        public int sat_best = -1;
        public int sat_new;
        /// <summary>Solo Lluvia de meteoros: palabras reales vistas y tocadas por banda de prevalencia (6: de la común a la
        /// rara). Sin datos = listas vacías.</summary>
        public int[] lex_band_seen;
        public int[] lex_band_hits;
        /// <summary>Solo Lluvia de meteoros: inventadas vistas y tocadas (falsas alarmas) por tipo (3: obvia, una letra
        /// cambiada, letras traspuestas).</summary>
        public int[] lex_fa_seen;
        public int[] lex_fa_hits;
        /// <summary>Solo Lluvia de meteoros: mediana del tiempo de toque (ms) en palabras comunes (bandas 1-2) y raras
        /// (bandas 5-6). -1 = pocas muestras.</summary>
        public int lex_rt_common_ms = -1;
        public int lex_rt_rare_ms = -1;
        /// <summary>Solo Lluvia de meteoros: palabras raras acertadas, separadas por coma ("" = ninguna o no aplica).</summary>
        public string lex_rare_words = "";
        /// <summary>Solo ¿Verdad o disparate?: palabras por minuto leyendo y decidiendo (mediana en los aciertos; -1 con menos
        /// de 10 aciertos).</summary>
        public int sv_wpm = -1;
        /// <summary>Solo ¿Verdad o disparate?: por tipo de frase (6: corta, con complemento, negación, con pausa, todos/algunos/
        /// ningún, comparación): tiempo medio de los aciertos en ms (-1 con menos de 4), aciertos y frases vistas.</summary>
        public int[] sv_rt_type;
        public int[] sv_hits_type;
        public int[] sv_seen_type;
        /// <summary>Solo ¿Verdad o disparate?: disparates evidentes y sutiles vistos y bien respondidos.</summary>
        public int sv_evident_hits = -1;
        public int sv_evident_seen = -1;
        public int sv_subtle_hits = -1;
        public int sv_subtle_seen = -1;
        /// <summary>Solo ¿Verdad o disparate?: la mejor racha de la partida.</summary>
        public int sv_best_streak = -1;
        /// <summary>Solo ¿Verdad o disparate?: ids de las frases que se marcaron como "no está clara", separados por coma.</summary>
        public string sv_unclear = "";
        /// <summary>Solo Cosecha de palabras: palabras encontradas en las 3 cosechas (sin las ocultas) y, de las comunes, cuántas se
        /// encontraron y cuántas había.</summary>
        public int harv_words = -1;
        public int harv_common_found = -1;
        public int harv_common_total = -1;
        /// <summary>Solo Cosecha de palabras: % de palabras en racimo frente a salto (-1 con menos de 10 palabras).</summary>
        public int harv_cluster_pct = -1;
        /// <summary>Solo Cosecha de palabras: palabras en los primeros y en los últimos 20 s de cada cosecha (promedio de las cosechas).</summary>
        public int harv_first20 = -1;
        public int harv_last20 = -1;
        /// <summary>Solo Cosecha de palabras: la palabra estrella encontrada ("" = ninguna), la más larga o rara y hasta 5 comunes que
        /// faltaron (separadas por coma), y cuántas pistas se usaron.</summary>
        public string harv_star = "";
        public string harv_best = "";
        public string harv_missed = "";
        public int harv_hints = -1;

        /// <summary>Solo La estrella intrusa: por tipo de grupo (6: amplia, vecina, uso, material/lugar/parte, trampa, regla + trampa) rondas
        /// vistas y aciertos (se tocó la intrusa a la primera).</summary>
        public int[] intr_seen_type;
        public int[] intr_hits_type;
        /// <summary>Solo La estrella intrusa: mediana del tiempo hasta el toque en los aciertos (ms, -1 con menos de 4) y la mejor racha.</summary>
        public int intr_rt_ms = -1;
        public int intr_best_streak = -1;
        /// <summary>Solo La estrella intrusa: «¿Qué las une?» vistas y acertadas (-1 = no aplica).</summary>
        public int intr_bonus_seen = -1;
        public int intr_bonus_hits = -1;
        /// <summary>Solo La estrella intrusa: claves de reglas separadas por ';': láminas nuevas del atlas ganadas en la partida, reglas
        /// falladas (por repasar), reglas repasadas y dominadas, y láminas ganadas «con nombre propio» (acertó «¿Qué las une?»).</summary>
        public string intr_new_plates = "";
        public string intr_review_new = "";
        public string intr_review_done = "";
        public string intr_named = "";

        /// <summary>Solo «En la punta de la lengua»: palabras encontradas solas (lucero dorado), con 1-2 ayudas (plateado), con las letras justas (cobre) y mostradas por Nubi
        /// (azul). -1 = no aplica.</summary>
        public int punta_solo = -1;
        public int punta_pista = -1;
        public int punta_letras = -1;
        public int punta_vista = -1;
        /// <summary>Solo «En la punta de la lengua»: tiempo medio, en ms, desde que la definición terminó de escribirse hasta «¡La tengo!» en las que salieron solas. -1 = ninguna.</summary>
        public int punta_ms = -1;
        /// <summary>Solo «En la punta de la lengua»: cada palabra jugada con su lucero, en orden («reloj:o;búho:a»; o oro, p plata, c cobre, a azul).</summary>
        public string punta_words = "";
        /// <summary>Solo «En la punta de la lengua»: las palabras que Nubi mostró (vuelven otro día) y las azules pendientes que esta partida encontró sola o con 1-2 ayudas
        /// (dejan de esperar), separadas por «;».</summary>
        public string punta_blue = "";
        public string punta_cleared = "";

        /// <summary>Solo «Carga exacta» (id <c>calculo</c>): cargas logradas SIN pista, logradas con pista de Nubi y logradas por un «camino corto» (los pasos del camino más corto,
        /// sin pista). -1 = no aplica.</summary>
        public int carga_alone = -1;
        public int carga_hinted = -1;
        public int carga_short = -1;
        /// <summary>Solo «Carga exacta»: tiempo medio, en ms, de las cargas logradas sin pista. -1 = ninguna.</summary>
        public int carga_ms = -1;

        /// <summary>Solo «Engranajes» (id <c>engranajes</c>): la etapa más alta que se jugó (1..5: giro, ramas y correas, movimiento, velocidad, trampas y armar), las luces del cohete con que
        /// termina la partida (0..9), los cohetes en órbita, cuántos despegaron en esta partida y el tiempo medio por máquina en ms. -1 = no aplica.</summary>
        public int engr_etapa = -1;
        public int engr_lights = -1;
        public int engr_orbit = -1;
        public int engr_launches = -1;
        public int engr_ms = -1;

        /// <summary>Solo «Bodega de carga» (id <c>bodega</c>; los objetos encontrados al primer intento van en <c>correct_trials</c> y los encontrados en <c>total_trials</c>): el grupo de etapa más alto
        /// (1..5: pocos objetos, más escotillas, cajas que se mueven, la bodega gira, todo junto), la racha más larga, el pedido más grande sin errores de esta partida, el récord (el mayor de todos,
        /// que la app guarda) y el tiempo medio por objeto en ms. -1 = no aplica.</summary>
        public int bod_group = -1;
        public int bod_best_streak = -1;
        public int bod_biggest = -1;
        public int bod_best = -1;
        public int bod_ms = -1;
        /// <summary>1 si en esta partida se superó el récord (pedido perfecto más grande que el guardado), 0 si no; -1 = no aplica.</summary>
        public int bod_new = -1;
    }
}
