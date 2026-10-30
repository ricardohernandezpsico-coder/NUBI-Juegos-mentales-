using System;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Las marcas de «ya visto» de Unity (Tarea 191): un solo lugar para las preferencias que dicen qué tarjetas «NUEVO» (y otros avisos de una sola vez) la persona ya vio. Cada clave es un entero con un bit por tarjeta (<c>con_intros</c>, <c>bod_intros</c>, <c>mail_intros</c>, <c>engr_intros2</c>,
    /// <c>sat_intros</c>, <c>tinta_cards</c>). Son PROGRESO, no estado pasajero: van en el respaldo y «Borrar datos» las borra (<c>docs/respaldo-y-diagnostico.md</c>).
    /// <para>En el modo de prueba «Jugar como la primera vez» (<see cref="Fresh"/>, solo botones [Debug]) nada se lee ni se guarda en las preferencias: todo se ve como nuevo y las marcas viven en la memoria de esa partida.</para>
    /// </summary>
    public static class SeenMarks
    {
        /// <summary>true = la partida se juega «como la primera vez» (la fija <see cref="Configure"/> con la config de cada partida).</summary>
        public static bool Fresh { get; private set; }

        private static readonly Dictionary<string, int> Memory = new Dictionary<string, int>();

        /// <summary>Borra todas las preferencias de Unity (se cambia en las pruebas: no se toca el registro de quien las corre).</summary>
        public static Action ClearAll = () => { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); };

        public static void Configure(SequenceConfigDetails config)
        {
            Fresh = config != null && config.fresh_start;
            Memory.Clear();
            if (config != null && config.reset_unity_prefs && !Fresh)
            {
                try { ClearAll(); }                         // «Borrar datos» de la app: sin esto, tras borrar vuelve el tutorial pero las tarjetas «NUEVO» no
                catch (Exception) { }
            }
        }

        /// <summary>¿La persona ya vio lo que marca <paramref name="bit"/> de la clave <paramref name="key"/>?</summary>
        public static bool IsSeen(string key, int bit)
        {
            if (Fresh) return Memory.TryGetValue(key, out int m) && (m & bit) != 0;
            try { return (PlayerPrefs.GetInt(key, 0) & bit) != 0; }
            catch (Exception) { return false; }
        }

        /// <summary>Marca como visto lo de <paramref name="bit"/>. Una tarjeta cuenta como vista APENAS se muestra (no al tocarla): si «Cómo se juega» la interrumpe, no vuelve.</summary>
        public static void Mark(string key, int bit)
        {
            if (Fresh) { Memory.TryGetValue(key, out int m); Memory[key] = m | bit; return; }
            try { PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) | bit); PlayerPrefs.Save(); }
            catch (Exception) { }
        }
    }
}
