using System;

namespace NeuroVida.Contracts
{
    /// <summary>
    /// Contrato JSON Nativo -> Unity para "Secuencia Lumínica" (Fase 0/1 del roadmap de
    /// migración, ver NeuroVida/CLAUDE.md). Fijado desde el día 1 a propósito: si no
    /// cambia entre fases, lo que se construya ahora sigue sirviendo cuando se agregue
    /// iOS/backend más adelante.
    ///
    /// Compatible con <c>UnityEngine.JsonUtility.FromJson</c> (solo campos públicos,
    /// sin colecciones genéricas anidadas) — mismo criterio simple que usa el ejemplo de
    /// arquitectura que compartió Ricardo.
    /// </summary>
    [Serializable]
    public class SequenceInitConfig
    {
        public string user_id;
        /// <summary>Id de texto tal cual <c>GameRegistry</c> del lado Kotlin (p. ej.
        /// "secuencia") -- NO un id numérico. El documento de arquitectura de referencia
        /// usaba un int genérico; se corrigió acá para no necesitar una tabla de
        /// traducción entre Unity y la app real.</summary>
        public string game_id;
        public SequenceConfigDetails config;
    }

    [Serializable]
    public class SequenceConfigDetails
    {
        /// <summary>Nivel elegido (1-5) — ancla el punto de partida del DDA, ver
        /// <c>SequenceGameContainer.seedSpan/seedIsiMs</c> en la versión Kotlin.</summary>
        public int level;
        /// <summary>Maestría acumulada entre sesiones (masteryStreak), mismo rol que
        /// <c>baseIntensity</c> en el Container Kotlin.</summary>
        public int base_intensity;
        /// <summary>Modo Reto (con reloj) vs. Precisión (sin reloj).</summary>
        public bool timed;
        /// <summary>"SENIOR" / "ADULT" / "UNDER_18" — ver <see cref="DdaUserProfileConfig"/>.
        /// Nunca la edad exacta: es el mismo rango mínimo necesario que ya captura el
        /// onboarding de la app nativa (dato de salud/personal reducido a lo imprescindible).</summary>
        public string age_band;
        public bool sound_enabled;
        /// <summary>Vibración (Ajustes > Vibración de la app). Por defecto activada: si el JSON no la trae (app
        /// vieja), <c>JsonUtility</c> conserva este valor inicial.</summary>
        public bool haptics_enabled = true;
        /// <summary>Rating guardado del DDA común (0..1) si la app tiene uno para este juego.</summary>
        public bool has_dda_rating;
        public float dda_rating;
        /// <summary>Evaluación inicial "Tu punto de partida" (onboarding): partida corta que busca el nivel de la
        /// persona. <c>assessment_step</c> de <c>assessment_total</c> (1..3) se muestra en la cuenta regresiva.
        /// Ver <c>Games/Shared/Assessment.cs</c>.</summary>
        public bool assessment;
        public int assessment_step;
        public int assessment_total;
        /// <summary>Cómo eligió jugar la persona (docs/dificultad-y-avance.md): "" o "A_TU_MEDIDA" = ajuste normal;
        /// "SUAVE" = con techo; "DESAFIO" / "EXPERTO" = con piso. Los límites vienen calculados por la app sobre el
        /// rating normalizado (0..1); -1 = sin límite. Ver <c>AdaptiveDifficulty.ConfigureMode</c>.</summary>
        /// <summary>El teléfono tiene "quitar animaciones": sin adornos que se mueven solos (ver GameFeel.ReduceMotion).</summary>
        public bool reduce_motion;
        /// <summary>La persona nunca jugó este juego (la app lo sabe por el historial): el juego abre con su tutorial guiado
        /// (<c>Games/Shared/GuidedTutorial</c>). Hoy lo usa Rastro de luz; los demás juegos lo ignoran.</summary>
        public bool show_tutorial;
        /// <summary>Solo «En la punta de la lengua» (id <c>anagramas</c>): las palabras que Nubi tuvo que mostrar en partidas anteriores («las azules»), separadas por «;»
        /// y bien escritas («búho;faro»). La app las guarda (son progreso: van en el respaldo) y las manda aquí para que vuelvan en otra partida.</summary>
        public string punta_pending = "";
        /// <summary>Solo «Engranajes» (id <c>engranajes</c>): las luces del cohete que la persona lleva (0..9) y los cohetes que ya despegaron. La app los guarda (son progreso: van en el respaldo),
        /// los manda aquí y Unity devuelve los nuevos al terminar (<c>engr_lights</c>, <c>engr_orbit</c> en la telemetría).</summary>
        public int engr_lights;
        public int engr_orbit;
        public string play_mode = "";
        public float mode_floor = -1f;
        public float mode_ceiling = -1f;

        /// <summary>Solo Bitácora de Misión: "" = partida completa (transmisión, patrulla e informe); "encode" = solo la
        /// transmisión (al empezar la sesión diaria); "recall" = solo el informe (al terminarla). Ver BitacoraContract.</summary>
        public string memory_phase = "";
        /// <summary>Solo Bitácora: semilla de la misión (la transmisión la elige; el informe recibe la misma).</summary>
        public int memory_seed;
        /// <summary>Solo Bitácora (informe): nivel con que se armó la misión en la transmisión.</summary>
        public int memory_level;
        /// <summary>Solo Bitácora (informe): segundos desde la transmisión.</summary>
        public int memory_elapsed_s;
    }
}
