using System.Collections.Generic;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Correo
{
    /// <summary>Los sonidos PROPIOS de La estación de correo en «Madera cálida» (Fase C): lo que no es de la paleta ni una nota de buzón.</summary>
    public enum MailSfx { Horn, Sack, Radio }

    /// <summary>
    /// «Madera cálida» en La estación de correo (Fase C; spec `auditoria-fable/spec-sonido-CDE.md` §2.5): cada buzón suena SU nota (do5, mi5, sol5, do6: <see cref="MailContract.PlanetNotes"/>, kalimba); el brillo de la racha <c>Spark</c> junto a la nota (pareja permitida), el escalón de racha <c>Streak</c>,
    /// la carta que cae o el buzón equivocado <c>Wrong</c>, «esa carta no» <c>Knock</c>, el encargo cumplido <c>Perfect</c>, mirar el reloj <c>Tick</c>, empieza el día <c>Hum</c>, la hoja de la mañana <c>Notice</c>, el cierre del día <c>LevelUp</c>, «NUEVO» <c>New</c>; y TRES propios con las piezas de <see cref="WoodKit"/>:
    /// la bocina del faro (<see cref="MailSfx.Horn"/>: 8 LU más baja que hoy, de fondo bajo la fanfarria <c>Perfect</c>; decisión 5 de §8, Ricardo la aprueba de oído), «¡llega un saco!» (<see cref="MailSfx.Sack"/>) y la radio (<see cref="MailSfx.Radio"/>). Esta parte de la clase solo dice las recetas y qué pide el juego
    /// ANTES de la cuenta regresiva. Cableada el 19-oct (Tarea 135): el controlador ya no suena campanas propias; Ricardo escuchó el faro (fanfarria y la bocina 8 LU más abajo) y quedó como está.
    /// </summary>
    public static class MailSounds
    {
        /// <summary>La receta de un sonido propio: el nivel (pico) y la sala de cada una son los de la spec; la familia declara a qué banda de sonoridad pertenece.</summary>
        public static WoodSounds.Recipe MakeRecipe(MailSfx sfx)
        {
            switch (sfx)
            {
                case MailSfx.Horn:                                                 // la bocina del faro: tres notas graves de marimba (re3, la3, re4), dos golpes por nota para que «dure»; de fondo
                {
                    var x = Blank(2.0);
                    double[] hz = { 146.83, 220, 293.66 };
                    for (int i = 0; i < hz.Length; i++)
                        for (int h = 0; h < 2; h++) Add(x, WoodKit.Marimba(hz[i], 0.45), i * 0.05 + h * 0.30, h == 0 ? 0.9 - i * 0.2 : 0.5);
                    return new WoodSounds.Recipe(x, 0.16, 0.25, WoodTier.Textura);
                }
                case MailSfx.Sack:                                                 // «¡llega un saco!»: un siseo que sube, un golpe grave y dos kalimbas
                {
                    var x = Blank(1.2);
                    Add(x, WoodKit.NoiseSweep(0.45, 400, 1400, FilterType.BandPass, 0.8, 0.01, 0.15, 61), 0, 0.8);
                    Add(x, WoodKit.Thump(0.25, 120, 70, 0.08), 0.05, 0.7);
                    Add(x, WoodKit.Kalimba(659.25, 0.25), 0.15, 0.6);
                    Add(x, WoodKit.Kalimba(880, 0.25), 0.25, 0.6);
                    return new WoodSounds.Recipe(x, 0.20, 0.18, WoodTier.Respuesta);
                }
                default:                                                           // Radio: estática granulada y dos marimbas cortas
                {
                    var x = Blank(1.0);
                    Add(x, WoodKit.StaticNoise(0.35, 1500, 0.7, 3, 0.003), 0, 0.5);
                    Add(x, WoodKit.Marimba(880, 0.07), 0, 0.8);
                    Add(x, WoodKit.Marimba(1174.66, 0.07), 0.14, 0.8);
                    return new WoodSounds.Recipe(x, 0.18, 0.12, WoodTier.Respuesta);
                }
            }
        }

        /// <summary>La clave de un sonido propio («correo.horn»).</summary>
        public static string Key(MailSfx sfx) => "correo." + sfx.ToString().ToLowerInvariant();

        /// <summary>Ninguno de los propios de Correo es un micro sin sala: todos se guardan en estéreo.</summary>
        public static bool Mono(MailSfx sfx) => false;

        /// <summary>Los sonidos propios con su clave y su receta: los lee la prueba de niveles y la página de escucha.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All()
        {
            foreach (MailSfx sfx in System.Enum.GetValues(typeof(MailSfx))) yield return (Key(sfx), MakeRecipe(sfx), Mono(sfx));
        }

        /// <summary>Lo que Correo pide antes de la cuenta regresiva, en el orden en que suena: las 4 notas de los buzones, el zumbido del día, el destello de racha (1 a 8), «esa carta no», el escalón de racha, la radio, el saco, la fanfarria con la bocina y «NUEVO». Wrong, Tick, LevelUp y Notice ya van con el núcleo.</summary>
        public static void Request()
        {
            for (int b = 0; b < MailContract.PlanetNotes.Length; b++) WoodCore.RequestNote(MailContract.PlanetNotes[b], WoodVoice.Kalimba);
            WoodCore.Request(WoodSfx.Hum);
            for (int n = 1; n <= 8; n++) WoodCore.Request(WoodSfx.Spark, n);
            WoodCore.Request(WoodSfx.Knock);
            WoodCore.Request(WoodSfx.Streak);
            RequestOwn(MailSfx.Radio);
            RequestOwn(MailSfx.Sack);
            WoodCore.Request(WoodSfx.Perfect);
            RequestOwn(MailSfx.Horn);
            WoodCore.Request(WoodSfx.New);
        }

        private static void RequestOwn(MailSfx sfx) => WoodCore.Request(Key(sfx), () => MakeRecipe(sfx), Mono(sfx));

        /// <summary>Suena un sonido propio (si no estaba listo no suena y queda contado).</summary>
        public static void Play(MailSfx sfx, float gainDb = 0f) => WoodCore.Play(Key(sfx), gainDb);
    }
}
