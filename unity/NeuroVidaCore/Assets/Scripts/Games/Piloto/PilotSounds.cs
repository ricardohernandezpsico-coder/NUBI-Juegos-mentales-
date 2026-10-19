using System;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Piloto
{
    /// <summary>Los sonidos PROPIOS de Piloto Estelar en «Madera cálida» (Fase D): el aviso de misión nueva.</summary>
    public enum PilotSfx { Mission }

    /// <summary>
    /// «Madera cálida» en «Piloto Estelar: la ruta de las balizas» (Fase D; spec `auditoria-fable/spec-sonido-CDE.md` §3.3; cableada el 19-oct, Tarea 138). Lo que lleva información conserva su altura y su momento y solo cambia el timbre: una nota de MARIMBA para cada baliza que se pasa DENTRO de la ruta
    /// (<see cref="BeaconHz"/>, la escala de hoy) y un golpe hueco (<c>Knock</c>: claramente distinto de las marimbas) para la que se pasa FUERA; el «blip» de las señales es el MISMO para todas, por construcción (<see cref="PlayBlip"/>: la misma señal de 880 Hz a −6 dB siempre: no delata cuál es la de la misión);
    /// atrapar una señal de la misión es <c>Correct(racha)</c> (el par sube con la racha, como en toda la app), el toque equivocado <c>Wrong</c>, el arco de un sector <c>SweepUp</c> y el hiperimpulso <c>Streak</c>. La misión que cambia es el único PROPIO (<see cref="PilotSfx.Mission"/>): dos marimbas
    /// (mi6 y si6) separadas exactamente <see cref="PilotContract.PulseBeatSeconds"/> = 0,4 s, una por cada latido de la tarjeta, en UN solo clip (así el 0,4 s queda exacto, sin depender del cuadro). <b>El motor «Cohete» se queda tal cual</b> (tres capas en bucle sin costura, de 4 s: crucero, rápido e hiperimpulso;
    /// la velocidad las mezcla y les sube el tono: ver <see cref="PilotEngine"/>; aprobado: no se toca).
    /// </summary>
    public static class PilotSounds
    {
        private const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>La escala pentatónica de do mayor desde do5: la de los otros juegos. Las alturas de hoy NO cambian.</summary>
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.51f, 1567.98f, 1760f };

        // ------------------------------------------------------------------ el motor «Cohete» (Tarea 66)

        /// <summary>Las tres capas del motor: crucero (rugido profundo), rápido (más abierto, con chasquidos raros) y hiperimpulso (un chorro de aire que se suma encima).</summary>
        public enum EngineLayer { Cruise, Fast, Boost }

        /// <summary>El bucle de 4 s de una capa del motor: se calcula UNA vez (en la precarga) y queda guardado. Mono, 44.100 Hz, 176.400 muestras (0,7 MB).</summary>
        public static AudioClip EngineLoop(EngineLayer layer) => Get("engine-" + layer, () =>
        {
            var data = MakeEngineLoop(layer);
            var clip = AudioClip.Create("engine-" + layer, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        });

        private const double LoopSeconds = 4.0, CrossSeconds = 2.0;

        /// <summary>
        /// Las muestras de una capa del motor «Cohete»: PORT 1 a 1 de <c>ENGINES.cohete</c> del laboratorio del motor (docs/previews/motor-laboratorio.html; mismas semillas, filtros, niveles y bucle sin costura), con <see cref="SoundKit"/>. Una prueba lo compara con muestras de referencia
        /// generadas con node (tools/sonido/referencia-motor.js). Crucero: ruido café por un paso bajo de 280 Hz con turbulencia a 3 Hz y un seno de 41,25 Hz (pico 0,46). Rápido: paso bajo de 650 Hz, turbulencia a 5 Hz, seno de 55 Hz y chasquidos raros filtrados a 1800 Hz (pico 0,50).
        /// Hiperimpulso: ruido de banda de 2000 Hz con turbulencia a 7 Hz (pico 0,32). Todos los tonos son múltiplos de 0,25 Hz: vuelven a la misma fase cada 4 s.
        /// </summary>
        public static float[] MakeEngineLoop(EngineLayer layer)
        {
            switch (layer)
            {
                case EngineLayer.Cruise:
                {
                    var lp = new SoundKit.Biquad(SoundKit.FilterType.LowPass, 280.0, 0.6);
                    var brown = Brown(11);
                    var am = SmoothRand(12, 3.0);
                    return LoopOf(t => lp.Run(brown()) * (0.75 + 0.5 * am()) + 0.35 * Math.Sin(SoundKit.Tau * 41.25 * t), 0.46);
                }
                case EngineLayer.Fast:
                {
                    var lp = new SoundKit.Biquad(SoundKit.FilterType.LowPass, 650.0, 0.6);
                    var bp = new SoundKit.Biquad(SoundKit.FilterType.BandPass, 1800.0, 1.2);
                    var brown = Brown(13);
                    var r = new SoundKit.Rng(14);
                    var am = SmoothRand(15, 5.0);
                    double crack = 0.0;
                    return LoopOf(t =>
                    {
                        if (r.Next() < 0.0009) crack = 1.0;
                        crack *= 0.992;
                        double body = lp.Run(brown()) * (0.7 + 0.6 * am());
                        return body + 0.3 * Math.Sin(SoundKit.Tau * 55.0 * t) + 0.25 * bp.Run((r.Next() * 2.0 - 1.0) * crack);
                    }, 0.5);
                }
                default:
                {
                    var bp = new SoundKit.Biquad(SoundKit.FilterType.BandPass, 2000.0, 0.5);
                    var r = new SoundKit.Rng(16);
                    var am = SmoothRand(17, 7.0);
                    return LoopOf(t => bp.Run(r.Next() * 2.0 - 1.0) * (0.6 + 0.8 * am()), 0.32);
                }
            }
        }

        /// <summary>Ruido café (el ruido blanco integrado con un poco de fuga): grave y suave.</summary>
        private static Func<double> Brown(uint seed)
        {
            var r = new SoundKit.Rng(seed);
            double b = 0.0;
            return () =>
            {
                b = (b + 0.02 * (r.Next() * 2.0 - 1.0)) / 1.02;
                return b * 3.5;
            };
        }

        /// <summary>Un valor al azar que se mueve despacio (cerca de <paramref name="hz"/> veces por segundo): la turbulencia.</summary>
        private static Func<double> SmoothRand(uint seed, double hz)
        {
            var r = new SoundKit.Rng(seed);
            double v = 0.5, a = 1.0 - Math.Exp(-SoundKit.Tau * hz / Rate);
            return () =>
            {
                v += a * (r.Next() - v);
                return v;
            };
        }

        /// <summary>Bucle sin costura (mono): se calculan 6 s y los 2 últimos se funden sobre los 2 primeros → 4 s exactos; el pico queda en <paramref name="level"/>.</summary>
        private static float[] LoopOf(Func<double, double> fn, double level)
        {
            var x = SoundKit.Render(LoopSeconds + CrossSeconds, fn);
            int n = (int)(LoopSeconds * Rate), f = (int)(CrossSeconds * Rate);
            var output = new float[n];
            Array.Copy(x, output, n);
            for (int i = 0; i < f; i++)
            {
                double k = i / (double)f;
                output[i] = (float)(x[i] * k + x[i + n] * (1.0 - k));
            }
            double peak = 1e-9;
            for (int i = 0; i < n; i++) peak = Math.Max(peak, Math.Abs((double)output[i]));
            double g = level / peak;
            for (int i = 0; i < n; i++) output[i] = (float)(output[i] * g);
            return output;
        }

        private static AudioClip Get(string key, Func<AudioClip> build)
        {
            if (!Cache.TryGetValue(key, out var clip) || clip == null)
            {
                clip = build();
                Cache[key] = clip;
            }
            return clip;
        }

        /// <summary>Calcula las tres capas del motor durante la cuenta regresiva, de a una por cuadro (así nada se traba al arrancar el vuelo).</summary>
        public static System.Collections.IEnumerator PrewarmEngine()
        {
            EngineLoop(EngineLayer.Cruise);
            yield return null;
            EngineLoop(EngineLayer.Fast);
            yield return null;
            EngineLoop(EngineLayer.Boost);
        }

        /// <summary>Cuántos clips del motor hay calculados (las pruebas).</summary>
        public static int CachedCount => Cache.Count;

        // ------------------------------------------------------------------ lo que suena en «Madera cálida»

        /// <summary>La frecuencia de la nota de la baliza número [index] que se pasa dentro de la ruta (4 notas, mi5 a la5…: las de hoy).</summary>
        public static float BeaconHz(int index) => Penta[1 + Mathf.Abs(index) % 4];

        /// <summary>Suena la nota de una baliza dentro de la ruta (marimba, la altura de hoy).</summary>
        public static bool PlayBeacon(int index) => WoodCore.PlayNote(BeaconHz(index), WoodVoice.Marimba);

        /// <summary>El blip de una señal que aparece: una señal corta de 880 Hz, IGUAL para todas (no delata cuál es de la misión), −6 dB.</summary>
        public const double BlipHz = 880.0, BlipSeconds = 0.12;

        /// <summary>Suena el blip (el mismo clip para todas las señales: <c>PlayCue</c> con los mismos argumentos da siempre el mismo sonido).</summary>
        public static bool PlayBlip() => WoodCore.PlayCue(BlipHz, BlipSeconds, gainDb: -6f);

        /// <summary>La receta de un sonido propio: la misión que cambia (dos marimbas que suben, mi6 y si6, separadas por <see cref="PilotContract.PulseBeatSeconds"/>: una por latido de la tarjeta). Nivel 0,26, sala 0,18, familia «hito».</summary>
        public static WoodSounds.Recipe MakeRecipe(PilotSfx sfx)
        {
            var x = Blank(1.6);
            Add(x, WoodKit.Marimba(659.25, 0.3));
            Add(x, WoodKit.Marimba(987.77, 0.3), PilotContract.PulseBeatSeconds);
            return new WoodSounds.Recipe(x, 0.26, 0.18, WoodTier.Hito);
        }

        /// <summary>La clave de un sonido propio («piloto.mission»).</summary>
        public static string Key(PilotSfx sfx) => "piloto." + sfx.ToString().ToLowerInvariant();

        /// <summary>Estéreo (con su sala).</summary>
        public static bool Mono(PilotSfx sfx) => false;

        /// <summary>Los sonidos propios con su clave y su receta: los lee la prueba de niveles y la página de escucha.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All()
        {
            foreach (PilotSfx sfx in Enum.GetValues(typeof(PilotSfx))) yield return (Key(sfx), MakeRecipe(sfx), Mono(sfx));
        }

        /// <summary>Suena el aviso de misión nueva.</summary>
        public static bool PlayMission() => WoodCore.Play(Key(PilotSfx.Mission));

        /// <summary>Lo que Piloto pide antes de la cuenta regresiva, en el orden en que suena: las 4 notas de las balizas, el blip de las señales, el golpe de la baliza fuera, la misión, el soplo del sector y el hiperimpulso. <c>Correct 1 a 7</c> y <c>Wrong</c> ya van con el núcleo.</summary>
        public static void Request()
        {
            for (int i = 0; i < 4; i++) WoodCore.RequestNote(BeaconHz(i), WoodVoice.Marimba);
            WoodCore.RequestCue(BlipHz, BlipSeconds);
            WoodCore.Request(WoodSfx.Knock);
            foreach (PilotSfx sfx in Enum.GetValues(typeof(PilotSfx))) { var s = sfx; WoodCore.Request(Key(s), () => MakeRecipe(s), Mono(s)); }
            WoodCore.Request(WoodSfx.SweepUp);
            WoodCore.Request(WoodSfx.Streak);
        }
    }
}
