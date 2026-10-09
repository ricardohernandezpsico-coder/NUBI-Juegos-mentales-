using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Satelites
{
    /// <summary>
    /// Dónde va cada cosa en la pantalla (medidas en dp lógicos de un campo de 360 de ancho, con el origen arriba a la izquierda; lógica pura, probada en tres formas de teléfono): el marcador común arriba (63 dp), la barra del Reto, las dos líneas
    /// de instrucción, el campo de las órbitas (con el planeta al centro) y, abajo, los discos de rondas y «Luces encendidas». El campo ocupa TODO lo que queda entre la instrucción y el pie, así que en una pantalla alta (20:9) los anillos
    /// usan toda la altura y no queda un tercio de pantalla vacío (<see cref="OrbitLayout"/> lo estira hasta 1,42 × el ancho).
    /// </summary>
    public readonly struct ScreenPlan
    {
        public const float Width = SatelliteContract.FieldWidth;
        /// <summary>Alto del marcador común (190 unidades del lienzo = 63,3 dp).</summary>
        public const float HudBottom = 190f / 3f;
        /// <summary>Alto de lo que el tutorial pone abajo («Práctica: no cuenta» y «Saltar tutorial»).</summary>
        public const float TutorialControls = 96f;
        public const float FooterHeight = 74f;

        public readonly float Height, TimerY, PromptAY, PromptBY, BandTop, BandBottom, Cx, Cy, HalfHeight, DiscsY, LightsY;

        public ScreenPlan(float height, bool guided)
        {
            Height = height;
            float bottom = height - (guided ? TutorialControls : 0f);
            TimerY = HudBottom + 5f;
            PromptAY = HudBottom + 25f;
            PromptBY = HudBottom + 52f;
            BandTop = HudBottom + 70f;
            BandBottom = bottom - (guided ? 8f : FooterHeight + 6f);
            Cx = Width * 0.5f;
            // el anillo de arriba necesita más aire que el de abajo: el sobre de la señal sube 22 dp sobre el satélite
            float top = BandTop + SatelliteContract.SatRadius + SatelliteContract.EnvelopeRise + 4f, bottom2 = BandBottom - SatelliteContract.SatRadius - 4f;
            Cy = (top + bottom2) * 0.5f;
            HalfHeight = Math.Max(120f, (bottom2 - top) * 0.5f);
            DiscsY = bottom - 48f;
            LightsY = bottom - 20f;
        }

        public OrbitLayout Orbits => new OrbitLayout(Width, Cx, Cy, HalfHeight);
    }
}
