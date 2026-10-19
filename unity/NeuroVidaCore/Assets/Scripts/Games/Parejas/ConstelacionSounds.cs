using System;
using System.Collections.Generic;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// «Madera cálida» en Constelaciones (Fase C; spec `auditoria-fable/spec-sonido-CDE.md` §2.3; cableada el 19-oct, Tarea 135): cada grupo suena SU nota (11, la 11.ª una octava arriba: <see cref="ConstelacionContract.NoteHz"/>) con kalimba (<c>WoodCore.PlayNote</c>: al abrir una luz y, 3 dB más baja, en «falta la tercera»);
    /// dar vuelta una luz <c>Tap</c> (por el altavoz del juego), cerrarla <c>Unselect</c>, «no son iguales» <c>Knock</c>, grupo encontrado de memoria <c>Correct(racha)</c> (el par de kalimbas sube con la racha) y a la primera vista <c>Correct 1</c> 3 dB más bajo, cielo completo <c>Perfect</c>,
    /// empieza el cielo <c>Hum</c> y la tarjeta «NUEVO» <c>New</c>. <b>El destello (<c>Spark</c>) se QUITÓ</b>: Ricardo lo decidió de oído (18-oct); el par de <c>Correct</c> ya sube con la racha y dos sonidos por acierto es lo que la paleta evita.
    /// <para><b>La pista («Las luces hermanas brillan», 850 ms después de errar) se queda como HOY</b> (las dos campanitas mi y la, muy suaves): a Ricardo el <c>Hint</c> de la paleta no le sonó a ayuda. La receta de abajo reproduce ese sonido por código (mismas campanas, mismo volumen) hasta que él elija de oído entre las tres opciones
    /// de <c>docs/previews/sonido-escucha-pista.html</c>; entonces esta receta se reemplaza por la elegida y se borra.</para>
    /// </summary>
    public static class ConstelacionSounds
    {
        /// <summary>La clave de la pista «como hoy» (un sonido propio temporal, con su receta).</summary>
        public const string HintKey = "parejas.hint";

        /// <summary>
        /// La pista como suena hoy: dos campanitas, mi (659,25 Hz, τ 0,2 s) y la (880 Hz, τ 0,26 s, 90 ms después), secas y en mono, a 0,16 y reproducidas con volumen 0,25. La receta lleva el nivel que da ESE volumen
        /// con el volumen maestro y el de reproducción de la paleta (<see cref="WoodKit.MasterGain"/> y <see cref="WoodKit.PlayVolume"/>), para que no suene más fuerte ni más suave que antes.
        /// </summary>
        public static WoodSounds.Recipe HintRecipe()
        {
            int n = (int)Math.Ceiling(1.2 * SoundKit.Rate);
            var x = new float[n];
            double peak = 0.0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)SoundKit.Rate;
                double v = 0.16 * (HintBell(659.25, t, 0.2) + HintBell(880.0, t - 0.09, 0.26));
                x[i] = (float)v;
                peak = Math.Max(peak, Math.Abs(v));
            }
            double level = peak * 0.25 / (WoodKit.MasterGain * WoodKit.PlayVolume);             // hoy: el clip sonaba a 0,25 de su volumen
            return new WoodSounds.Recipe(x, level, 0.0, WoodTier.Micro);
        }

        /// <summary>Una campanita de las de antes (seno con dos parciales inarmónicos y una caída exponencial): solo para la pista «como hoy».</summary>
        private static double HintBell(double hz, double t, double tau)
        {
            if (t < 0.0) return 0.0;
            double env = (t < 0.01 ? t / 0.01 : 1.0) * Math.Exp(-Math.Max(0.0, t - 0.01) / tau);
            double w = 2.0 * Math.PI * t;
            return env * (Math.Sin(w * hz) + 0.16 * Math.Sin(w * hz * 2.76) + 0.05 * Math.Sin(w * hz * 5.4));
        }

        /// <summary>Suena la pista de las luces hermanas (como hoy).</summary>
        public static void PlayHint() => WoodCore.Play(HintKey);

        /// <summary>Lo que Constelaciones pide antes de la cuenta regresiva: las 11 notas (suenan al abrir la primera luz), después el zumbido, cerrar, «no son iguales», la pista, el cielo completo y «NUEVO». Tap y Correct 1 a 7 ya van con el núcleo.</summary>
        public static void Request()
        {
            for (int g = 0; g < 11; g++) WoodCore.RequestNote(ConstelacionContract.NoteHz(g), WoodVoice.Kalimba);
            WoodCore.Request(WoodSfx.Hum);
            WoodCore.Request(WoodSfx.Unselect);
            WoodCore.Request(WoodSfx.Knock);
            WoodCore.Request(HintKey, HintRecipe, true);
            WoodCore.Request(WoodSfx.Perfect);
            WoodCore.Request(WoodSfx.New);
        }

        /// <summary>Los sonidos PROPIOS de Constelaciones: solo la pista «como hoy» (temporal, hasta que Ricardo elija): la prueba de niveles recorre esta lista.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All() { yield return (HintKey, HintRecipe(), true); }
    }
}
