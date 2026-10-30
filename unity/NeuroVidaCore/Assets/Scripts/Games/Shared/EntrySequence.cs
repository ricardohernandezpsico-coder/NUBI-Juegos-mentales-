using System.Collections.Generic;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// SOLO EN EL EDITOR (Tarea 191): el orden en que pasan las cosas de la entrada, para que el smoke lo vigile: «reglas» (el repaso de Freno), «tarjeta» (cada tarjeta «NUEVO»), «ya» (el «¡Ya!»), «primer ensayo» y «hoja» (la hoja de la mañana de Correo, que es juego).
    /// Regla: las tarjetas y las reglas van ANTES del «¡Ya!»; el «¡Ya!» es uno solo y va justo antes del primer ensayo; la hoja de la mañana va DESPUÉS. En el teléfono no existe (las llamadas no hacen nada).
    /// </summary>
    public static class EntrySequence
    {
#if UNITY_EDITOR
        public static readonly List<string> Events = new List<string>();
        public static void Note(string what) => Events.Add(what);
        public static void Reset() => Events.Clear();

        /// <summary>Los problemas del orden de la partida que acaba de pasar (vacío = bien). Pura: la prueba y el smoke la usan.</summary>
        public static List<string> Problems(IList<string> events)
        {
            var problems = new List<string>();
            int ya = -1, count = 0;
            for (int i = 0; i < events.Count; i++) if (events[i] == "ya") { if (ya < 0) ya = i; count++; }
            if (ya < 0) return problems;                    // la corrida terminó antes del «¡Ya!» (un tutorial, un arranque corto): no hay nada que ordenar
            if (count != 1) problems.Add("el «¡Ya!» salió " + count + " veces (tiene que ser UNO por partida)");
            int first = events.IndexOf("primer ensayo");
            bool endedInsideTheGo = first < 0 && ya == events.Count - 1;           // la corrida terminó mientras sonaba el «¡Ya!» (un tutorial corto): todavía no llegó el primer ensayo
            if (!endedInsideTheGo && first != ya + 1) problems.Add("entre el «¡Ya!» y el primer ensayo hay algo más: " + (first < 0 ? "no hay primer ensayo" : events[ya + 1]));
            for (int i = ya + 1; i < events.Count; i++)
                if (events[i] == "reglas") problems.Add("el repaso de reglas salió después del «¡Ya!»");
            for (int i = 0; i < ya; i++)
                if (events[i] == "hoja") problems.Add("la hoja de la mañana salió antes del «¡Ya!»: es juego, va después");
            return problems;
        }
#else
        public static void Note(string what) { }
        public static void Reset() { }
#endif
    }
}
