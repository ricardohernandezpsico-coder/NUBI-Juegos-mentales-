using System;

namespace NeuroVida.Games.Piloto
{
    /// <summary>
    /// Dónde va cada cosa en la pantalla de Piloto Estelar (dp lógicos de un campo de 360 de ancho, con el origen arriba a la izquierda; lógica pura, probada en tres formas de teléfono): el marcador común (63 dp), la barra del viaje, la tarjeta de misión, el aviso de
    /// sector, el cielo donde nacen las señales, la nave, y abajo la franja del dedo (≥ 100 dp de alto). En la ronda guiada el tutorial usa los 96 dp de abajo («Práctica: no cuenta» y «Saltar tutorial»): la franja sube para no quedar debajo de ellos.
    /// </summary>
    public readonly struct PilotPlan
    {
        public const float Width = PilotContract.FieldWidth;
        /// <summary>Alto del marcador común (190 unidades del lienzo = 63,3 dp).</summary>
        public const float HudBottom = 190f / 3f;
        public const float TutorialControls = 96f;
        /// <summary>La franja del dedo mide al menos esto (dp).</summary>
        public const float StripMinHeight = 100f;
        /// <summary>Alto del aviso de MISIÓN NUEVA (dp): el título, la forma dibujada a ≥ 48 dp con su nombre y el sector. Va bajo la tarjeta de misión, sobre el cielo de señales (el cielo no se achica: solo es zona prohibida mientras el aviso está a la vista).</summary>
        public const float NoticeHeight = 112f;
        /// <summary>El diámetro (dp) de la forma dibujada en el aviso de misión nueva: al menos 48.</summary>
        public const float NoticeShapeDp = 52f;

        public readonly float Height;
        public readonly float JourneyY, MissionTop, MissionBottom, BannerTop, BannerBottom, NoticeBottom;
        public readonly float SkyTop, SkyBottom, ShipY, StripTop, StripVisualTop, StripBottom;
        /// <summary>Hasta dónde se dibuja la ruta hacia arriba (debajo de la tarjeta de misión y del aviso).</summary>
        public readonly float RouteTop;

        public PilotPlan(float height, bool guided)
        {
            Height = height;
            float inset = guided ? TutorialControls : 0f;
            JourneyY = HudBottom + 7f;
            MissionTop = HudBottom + 15f;
            MissionBottom = MissionTop + 36f;
            BannerTop = MissionBottom + 8f;
            BannerBottom = BannerTop + 52f;
            NoticeBottom = BannerTop + NoticeHeight;
            StripBottom = height - inset - 10f;
            StripTop = height - inset - 120f;
            StripVisualTop = StripTop + 8f;
            ShipY = StripTop - 68f;
            SkyTop = BannerBottom - 24f;
            SkyBottom = ShipY - 72f;
            RouteTop = MissionBottom + 4f;
        }

        /// <summary>El aviso corto (hiperimpulso; donde sale mientras está a la vista): la zona donde NO nacen señales.</summary>
        public Box BannerBox => new Box(40f, BannerTop, Width - 40f, BannerBottom);

        /// <summary>El aviso de misión nueva (sector): mismo lugar de arriba que <see cref="BannerBox"/> pero más alto y un poco más ancho; mientras está a la vista o en espera, ahí no nacen señales ni salen textos flotantes.</summary>
        public Box NoticeBox => new Box(30f, BannerTop, Width - 30f, NoticeBottom);

        /// <summary>El cielo donde nacen las señales (arriba del avance de la nave).</summary>
        public Box SkyBox => new Box(0f, SkyTop - PilotContract.SignalRingSize * 0.5f, Width, SkyBottom + PilotContract.SignalRingSize * 0.5f);
    }
}
