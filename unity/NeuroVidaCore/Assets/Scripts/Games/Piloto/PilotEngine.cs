using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Piloto
{
    /// <summary>
    /// El motor «Cohete» de Piloto Estelar (Tarea 66, 10-oct; docs/diseno-piloto.md §8): TRES capas en bucle (crucero, rápido e hiperimpulso; las muestras salen de <see cref="PilotSounds.MakeEngineLoop"/>) que suenan a la vez en tres <c>AudioSource</c> que arrancan en
    /// puntos distintos del bucle (0, 1,3 y 2,7 s) para que sus fases no coincidan. Según la velocidad normalizada <c>k</c> (0 = la del nivel 1, 1 = la del nivel máximo con hiperimpulso) se mezclan crucero y rápido con POTENCIA CONSTANTE (<c>cos</c> y <c>sin</c>), el hiperimpulso suma
    /// el chorro encima y las tres capas suben el tono juntas. Todo se acerca al valor nuevo de a poco (constante de 0,25 s; el chorro, 0,12 s al entrar y 0,35 s al salir): nada salta. Es el mismo reparto del laboratorio del motor (docs/previews/motor-laboratorio.html).
    /// Es un FONDO: el nivel general (<see cref="MasterLevel"/>) lo deja claramente por debajo de las campanas de atrapar y del aviso de misión nueva. Respeta el sonido apagado (no se enciende), la pausa del menú (calla todo) y la congelación de Nubi (baja al 30 %, <see cref="GameClock.LoopVolume"/>).
    /// La parte de las cuentas (<see cref="TargetMix"/>, <see cref="Approach"/>) es pura y tiene pruebas.
    /// </summary>
    public sealed class PilotEngine
    {
        /// <summary>
        /// El nivel general del motor (se multiplica por el volumen de cada capa). El laboratorio empezaba en 0,7; medido en el juego (<c>PilotEngineTests</c>) con 0,7 el motor llegaba a la fuerza del aviso de misión nueva (RMS 0,067 contra 0,069), y un fondo no puede competir con
        /// ellos: con 0,35 queda a −6 dB de la fuerza (RMS) del aviso de misión y a −10 dB de la campana de atrapar, incluso en hiperimpulso. Es UN solo número: si en el teléfono se oye muy bajo o muy alto, se cambia acá.
        /// </summary>
        public const float MasterLevel = 0.35f;

        /// <summary>Cada capa arranca en un punto distinto de su bucle de 4 s (crucero, rápido, hiperimpulso).</summary>
        public static readonly float[] StartSeconds = { 0f, 1.3f, 2.7f };

        /// <summary>El tono (velocidad de reproducción) y la mezcla hacia los que va el motor.</summary>
        public struct Mix
        {
            public float Cruise, Fast, Boost, Pitch;
        }

        /// <summary>
        /// Lo que pide el laboratorio para la velocidad normalizada <paramref name="k"/> (0 a 1): crucero 0,55·cos(k·π/2), rápido 0,55·sin(k·π/2), hiperimpulso 0,5 (si está en marcha) y tono 0,96 + 0,10·k (+0,04 en hiperimpulso). Antes del nivel general.
        /// </summary>
        public static Mix TargetMix(float k, bool hyper)
        {
            k = Mathf.Clamp01(k);
            return new Mix
            {
                Cruise = 0.55f * Mathf.Cos(k * Mathf.PI * 0.5f),
                Fast = 0.55f * Mathf.Sin(k * Mathf.PI * 0.5f),
                Boost = hyper ? 0.5f : 0f,
                Pitch = 0.96f + 0.10f * k + (hyper ? 0.04f : 0f),
            };
        }

        /// <summary>Un paso del acercamiento exponencial de <paramref name="current"/> a <paramref name="target"/> con constante de tiempo <paramref name="tau"/> (lo que hace <c>setTargetAtTime</c> en el laboratorio).</summary>
        public static float Approach(float current, float target, float tau, float dt) =>
            target + (current - target) * Mathf.Exp(-Mathf.Max(0f, dt) / Mathf.Max(1e-4f, tau));

        public const float TauMix = 0.25f, TauBoostIn = 0.12f, TauBoostOut = 0.35f;

        private readonly AudioSource[] _sources = new AudioSource[3];
        private readonly float[] _gain = new float[3];
        private float _pitch = 0.96f, _freeze = 1f;

        /// <summary>Crea las tres fuentes de audio (en bucle, sin sonar todavía) en <paramref name="host"/>.</summary>
        public PilotEngine(GameObject host)
        {
            for (int i = 0; i < _sources.Length; i++)
            {
                var s = host.AddComponent<AudioSource>();
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0f;
                _sources[i] = s;
            }
        }

        public bool Playing => _sources[0] != null && _sources[0].isPlaying;

        /// <summary>Enciende las tres capas (cada una desde su punto del bucle) con el tono de la velocidad <paramref name="k"/>; el volumen sube de a poco.</summary>
        public void Start(float k, bool hyper)
        {
            var layers = new[] { PilotSounds.EngineLayer.Cruise, PilotSounds.EngineLayer.Fast, PilotSounds.EngineLayer.Boost };
            _pitch = TargetMix(k, hyper).Pitch;
            _freeze = 1f;
            for (int i = 0; i < _sources.Length; i++)
            {
                var s = _sources[i];
                _gain[i] = 0f;
                s.clip = PilotSounds.EngineLoop(layers[i]);
                s.volume = 0f;
                s.pitch = _pitch;
                s.mute = false;
                s.timeSamples = Mathf.Clamp((int)(StartSeconds[i] * s.clip.frequency), 0, s.clip.samples - 1);
                s.Play();
            }
        }

        public void Stop()
        {
            foreach (var s in _sources) if (s != null && s.isPlaying) s.Stop();
        }

        /// <summary>
        /// Un cuadro: la mezcla y el tono se acercan a lo que pide la velocidad. <paramref name="dt"/> es el tiempo REAL del cuadro (el motor sigue acercándose aunque Nubi congele el juego); <paramref name="loopVolume"/> es <see cref="GameClock.LoopVolume"/> (1, o 0,3 con Nubi
        /// congelando el juego) y <paramref name="silenced"/> es la pausa del menú.
        /// </summary>
        public void Tick(float k, bool hyper, float dt, float loopVolume, bool silenced)
        {
            if (!Playing) return;
            var m = TargetMix(k, hyper);
            _pitch = Approach(_pitch, m.Pitch, TauMix, dt);
            _gain[0] = Approach(_gain[0], m.Cruise, TauMix, dt);
            _gain[1] = Approach(_gain[1], m.Fast, TauMix, dt);
            _gain[2] = Approach(_gain[2], m.Boost, hyper ? TauBoostIn : TauBoostOut, dt);
            _freeze = Mathf.MoveTowards(_freeze, loopVolume, Mathf.Max(0f, dt) * 3f);
            for (int i = 0; i < _sources.Length; i++)
            {
                var s = _sources[i];
                s.pitch = _pitch;
                s.mute = silenced;
                s.volume = Mathf.Clamp01(_gain[i] * MasterLevel * _freeze);
            }
        }

        /// <summary>El volumen que hoy suena en cada capa (para las pruebas y el diagnóstico).</summary>
        public float VolumeOf(int layer) => _sources[layer].volume;

        public bool MutedNow => _sources[0].mute;
    }
}
