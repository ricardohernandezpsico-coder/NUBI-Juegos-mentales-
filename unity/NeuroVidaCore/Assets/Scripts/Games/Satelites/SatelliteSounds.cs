using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Satelites
{
    /// <summary>
    /// «Madera cálida» en «Satélites: enciende tu planeta» (Fase D; spec `auditoria-fable/spec-sonido-CDE.md` §3.2; cableada el 19-oct, Tarea 138). Lo que lleva información conserva su altura y su momento y solo cambia el timbre: al presentar los satélites con mensaje suena UNA campana por cada uno, cada
    /// <see cref="SatelliteContract.CueBellGap"/> = 0,12 s, con la nota <see cref="CueHz"/> en MARIMBA (la cantidad es lo que hay que recordar; la misma caída de siempre, 0,3 s). Marcar un satélite es <c>Select</c> (kalimba, el mismo evento de Rescate; la altura sube con cada marca y es la de hoy: <see cref="MarkStep"/>),
    /// quitar la marca <c>Unselect</c>, «ya están todos» <c>Knock</c>, acertar todo <c>Perfect</c>, «faltó alguno» <c>Wrong</c> (nunca un castigo), una luz que se enciende <c>Spark</c> (la misma escalera de cinco notas que sube y vuelve a empezar: <see cref="LightStep"/>), el aviso de sorpresa <c>New</c> y el «clac» al detenerse
    /// <c>Clack</c>. Todo es de la paleta: Satélites no tiene sonidos propios. <b>El zumbido en bucle se queda</b> (dos senos graves a 0,12, un segundo exacto, aprobado: es de su <c>_humSource</c>, como el motor de Piloto y el propulsor de Aterrizaje). Sigue plano durante el seguimiento: no hay respuesta ni sonido mientras giran.
    /// </summary>
    public static class SatelliteSounds
    {
        private const int Rate = 44100;
        private static AudioClip _hum;

        /// <summary>La escala pentatónica de do mayor desde do5 (523 Hz): la de los otros juegos. Las alturas de hoy NO cambian.</summary>
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.51f, 1567.98f, 1760f };

        /// <summary>La frecuencia de la campana de la presentación del satélite número [index] (0 a 4): la de hoy.</summary>
        public static float CueHz(int index) => Penta[2 + Mathf.Clamp(index, 0, 4)];

        /// <summary>Suena la campana de la presentación del satélite [index] (marimba, caída de 0,3 s como hoy).</summary>
        public static bool PlayCue(int index) => WoodCore.PlayNote(CueHz(index), WoodVoice.Marimba);

        /// <summary>El escalón de <c>Select</c> de la marca número [n] (1 a 7): la altura de hoy (<c>Penta[n + 1]</c>: mi5 con la primera). La paleta sube un escalón por marca.</summary>
        public static int MarkStep(int n) => Mathf.Clamp(n, 1, 7) + 1;

        /// <summary>Suena la marca número [n] (kalimba, el <c>Select</c> de Rescate).</summary>
        public static bool PlayMark(int n) => WoodCore.Play(WoodSfx.Select, MarkStep(n));

        /// <summary>El escalón de <c>Spark</c> de la luz que se enciende cuando ya hay [lightsShown]: la escalera de cinco notas que sube y vuelve a empezar (la de hoy, un escalón arriba: la paleta empieza en el do6).</summary>
        public static int LightStep(int lightsShown) => 1 + Mathf.Abs(lightsShown) % 5;

        /// <summary>Suena la luz que se enciende (kalimba aguda; destello de la paleta).</summary>
        public static bool PlayLight(int lightsShown) => WoodCore.Play(WoodSfx.Spark, LightStep(lightsShown));

        /// <summary>El zumbido del seguimiento: dos senos graves (110 y 165 Hz) de un segundo exacto (ciclos enteros: se repite sin chasquido). Es el bucle aprobado: lo suena el <c>_humSource</c> del juego, no <c>WoodCore</c>.</summary>
        public static AudioClip Hum()
        {
            if (_hum == null)
            {
                var data = new float[Rate];
                for (int i = 0; i < Rate; i++)
                {
                    float t = i / (float)Rate;
                    data[i] = 0.5f * (0.6f * Mathf.Sin(2f * Mathf.PI * 110f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * 165f * t));
                }
                _hum = AudioClip.Create("hum", Rate, 1, Rate, false);
                _hum.SetData(data, 0);
            }
            return _hum;
        }

        /// <summary>Lo que Satélites pide antes de la cuenta regresiva, en el orden en que suena: las 5 campanas de la presentación (marimba), el «clac», las marcas (<c>Select</c>) y quitarlas, «ya están todos», acertar todo, las luces, «faltó alguno» y el aviso de sorpresa. <c>Wrong</c> ya va con el núcleo.</summary>
        public static void Request()
        {
            for (int i = 0; i < 5; i++) WoodCore.RequestNote(CueHz(i), WoodVoice.Marimba);
            WoodCore.Request(WoodSfx.Clack);
            for (int n = 1; n <= 7; n++) WoodCore.Request(WoodSfx.Select, MarkStep(n));
            WoodCore.Request(WoodSfx.Unselect);
            WoodCore.Request(WoodSfx.Knock);
            WoodCore.Request(WoodSfx.Perfect);
            for (int k = 1; k <= 5; k++) WoodCore.Request(WoodSfx.Spark, k);
            WoodCore.Request(WoodSfx.New);
        }

        /// <summary>Los sonidos PROPIOS de Satélites: ninguno (todo es de la paleta; el zumbido es el bucle aprobado, fuera de <c>WoodCore</c>). La prueba de niveles recorre esta lista.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All() { yield break; }
    }
}
