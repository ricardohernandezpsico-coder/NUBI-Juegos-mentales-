using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El tono de un aviso de juego (<see cref="GameHud.Announce"/>, Tarea 179). Cada tono lleva una FORMA además del color (acierto y error nunca solo por color): lima con la flecha hacia arriba para subir un paso, lima con el visto para lo logrado, sol con el visto SOLO para un hito
    /// (una racha de 5 o 10, un límite nuevo: «un solo sol a la vez»), lila con el reloj para el ritmo («Con calma»), coral con la equis para la nota de error y crema con un punto para lo que solo informa.
    /// </summary>
    public enum AnnounceTone { Step, Done, Milestone, Pace, Error, Info }

    /// <summary>La losa, el color y el glifo de cada <see cref="AnnounceTone"/>: lo que la fila de consigna se pone mientras habla.</summary>
    public static class AnnounceStyle
    {
        public static readonly AnnounceTone[] All = { AnnounceTone.Step, AnnounceTone.Done, AnnounceTone.Milestone, AnnounceTone.Pace, AnnounceTone.Error, AnnounceTone.Info };

        /// <summary>La losa del kit con que se tiñe la fila.</summary>
        public static SlabTone Slab(AnnounceTone tone)
        {
            switch (tone)
            {
                case AnnounceTone.Step: case AnnounceTone.Done: return SlabTone.Lime;
                case AnnounceTone.Milestone: return SlabTone.Sun;
                case AnnounceTone.Pace: return SlabTone.Lilac;
                case AnnounceTone.Error: return SlabTone.Coral;
                default: return SlabTone.Cream;
            }
        }

        /// <summary>El color del cuerpo de esa losa (contra él se mide que la letra en tinta se lea: ≥ 4,5:1).</summary>
        public static Color Fill(AnnounceTone tone)
        {
            switch (tone)
            {
                case AnnounceTone.Step: case AnnounceTone.Done: return NeuroStyle.Lime;
                case AnnounceTone.Milestone: return new Color32(0xFF, 0xCB, 0x45, 0xFF);
                case AnnounceTone.Pace: return NeuroStyle.Grape;
                case AnnounceTone.Error: return NeuroStyle.Coral;
                default: return NeuroStyle.Cream;
            }
        }

        /// <summary>El nombre de la forma del glifo: flecha, visto, reloj, equis o punto (para las pruebas y los registros).</summary>
        public static string Shape(AnnounceTone tone)
        {
            switch (tone)
            {
                case AnnounceTone.Step: return "flecha";
                case AnnounceTone.Done: case AnnounceTone.Milestone: return "visto";
                case AnnounceTone.Pace: return "reloj";
                case AnnounceTone.Error: return "equis";
                default: return "punto";
            }
        }

        private const int SizePx = 128;
        private const float Zoom = 1.1f;
        private const float Aa = 0.035f;
        private static readonly Sprite[] Cache = new Sprite[6];

        /// <summary>El glifo del tono (blanco con alfa: se tiñe con tinta sobre la losa de la fila).</summary>
        public static Sprite Glyph(AnnounceTone tone)
        {
            int i = (int)tone;
            return Cache[i] != null ? Cache[i] : Cache[i] = ToSprite(Render(tone, SizePx), SizePx, SizePx);
        }

        /// <summary>Píxeles del glifo (fila 0 = abajo). Sin borde ni sombra: la losa que lo lleva ya tiene su volumen.</summary>
        public static Color32[] Render(AnnounceTone tone, int size)
        {
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float x = ((px + 0.5f) / size * 2f - 1f) * Zoom;
                    float y = ((py + 0.5f) / size * 2f - 1f) * Zoom;
                    pixels[py * size + px] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Cover(Distance(tone, x, y), Aa) * 255f));
                }
            return pixels;
        }

        private static float Distance(AnnounceTone tone, float x, float y)
        {
            switch (tone)
            {
                case AnnounceTone.Step:                                                    // la flecha hacia arriba: un tallo y dos alas
                    return Mathf.Min(Capsule(x, y, 0f, -0.55f, 0f, 0.55f, 0.15f),
                        Mathf.Min(Capsule(x, y, -0.5f, 0.05f, 0f, 0.58f, 0.15f), Capsule(x, y, 0.5f, 0.05f, 0f, 0.58f, 0.15f)));
                case AnnounceTone.Done:
                case AnnounceTone.Milestone:                                               // el visto
                    return Mathf.Min(Capsule(x, y, -0.55f, 0.0f, -0.18f, -0.4f, 0.15f), Capsule(x, y, -0.18f, -0.4f, 0.58f, 0.42f, 0.15f));
                case AnnounceTone.Pace:                                                    // el reloj: un aro y dos manecillas
                    return Mathf.Min(Mathf.Abs(Circle(x, y, 0f, 0f, 0.72f)) - 0.11f,
                        Mathf.Min(Capsule(x, y, 0f, 0f, 0f, 0.44f, 0.1f), Capsule(x, y, 0f, 0f, 0.32f, 0f, 0.1f)));
                case AnnounceTone.Error:                                                   // la equis (diagonal, nunca una cruz)
                    return Mathf.Min(Capsule(x, y, -0.45f, -0.45f, 0.45f, 0.45f, 0.15f), Capsule(x, y, -0.45f, 0.45f, 0.45f, -0.45f, 0.15f));
                default:                                                                   // el punto
                    return Circle(x, y, 0f, 0f, 0.36f);
            }
        }
    }
}
