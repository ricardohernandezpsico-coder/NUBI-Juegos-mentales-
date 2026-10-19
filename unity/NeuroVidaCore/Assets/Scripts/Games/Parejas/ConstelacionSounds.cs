using System.Collections.Generic;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// «Madera cálida» en Constelaciones (Fase C; spec `auditoria-fable/spec-sonido-CDE.md` §2.3; cableada el 19-oct, Tarea 135): cada grupo suena SU nota (11, la 11.ª una octava arriba: <see cref="ConstelacionContract.NoteHz"/>) con kalimba (<c>WoodCore.PlayNote</c>: al abrir una luz y, 3 dB más baja, en «falta la tercera»);
    /// dar vuelta una luz <c>Tap</c> (por el altavoz del juego), cerrarla <c>Unselect</c>, «no son iguales» <c>Knock</c>, grupo encontrado de memoria <c>Correct(racha)</c> (el par de kalimbas sube con la racha) y a la primera vista <c>Correct 1</c> 3 dB más bajo, cielo completo <c>Perfect</c>,
    /// empieza el cielo <c>Hum</c> y la tarjeta «NUEVO» <c>New</c>. <b>El destello (<c>Spark</c>) se QUITÓ</b>: Ricardo lo decidió de oído (18-oct); el par de <c>Correct</c> ya sube con la racha y dos sonidos por acierto es lo que la paleta evita.
    /// <para><b>La pista («Las luces hermanas brillan», 380 ms después de errar) suena la nota del PROPIO GRUPO</b> (Tarea 137, 19-oct): Ricardo eligió de oído la opción (a) de <c>docs/previews/sonido-escucha-pista.html</c>. A él el <c>Hint</c> de la paleta no le sonó a ayuda; la nota de la hermana
    /// (la misma que sonó cuando la vio) sí avisa sin decir nada más: kalimba, suave (<see cref="HintGainDb"/>), UNA por cada luz hermana en el momento en que brilla (<see cref="ConstelacionContract.HintCues"/>). Es la misma receta de nota de siempre, ya pedida con <c>RequestNote</c>: no se calcula nada nuevo.</para>
    /// </summary>
    public static class ConstelacionSounds
    {
        /// <summary>Cuánto más suave que la nota normal suena la pista: −6 dB (un aviso, no un acierto). Ricardo no pidió bajarla más.</summary>
        public const float HintGainDb = -6f;

        /// <summary>Suena la nota del grupo [group] como pista (kalimba, <see cref="HintGainDb"/>): la llama el juego una vez por cada luz hermana cuando esa luz brilla.</summary>
        public static bool PlayHint(int group) => WoodCore.PlayNote(ConstelacionContract.NoteHz(group), WoodVoice.Kalimba, gainDb: HintGainDb);

        /// <summary>Lo que Constelaciones pide antes de la cuenta regresiva: las 11 notas (suenan al abrir la primera luz y como pista), después el zumbido, cerrar, «no son iguales», el cielo completo y «NUEVO». Tap y Correct 1 a 7 ya van con el núcleo.</summary>
        public static void Request()
        {
            for (int g = 0; g < 11; g++) WoodCore.RequestNote(ConstelacionContract.NoteHz(g), WoodVoice.Kalimba);
            WoodCore.Request(WoodSfx.Hum);
            WoodCore.Request(WoodSfx.Unselect);
            WoodCore.Request(WoodSfx.Knock);
            WoodCore.Request(WoodSfx.Perfect);
            WoodCore.Request(WoodSfx.New);
        }

        /// <summary>Los sonidos PROPIOS de Constelaciones: ninguno (las notas de los grupos y la pista salen de la paleta y de <c>RequestNote</c>). La prueba de niveles recorre esta lista.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All() { yield break; }
    }
}
