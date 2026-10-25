using System;
using UnityEngine;

namespace NeuroVida.Games.Engranajes
{
    /// <summary>
    /// Disposición de «Engranajes» en dp lógicos (360 de ancho; y desde arriba del área de juego, que incluye el marcador). Es la del boceto aprobado
    /// (docs/previews/engranajes-taller-boceto.html: cabecera con las luces del cohete, consigna, sala de máquinas con el cohete a la derecha y, abajo, la cuenta de cambios y «Arrancar»)
    /// adaptada a la altura de cada teléfono: la escena (sala + cohete, de 384 dp de alto en el boceto) se ACHICA, entera y pareja, si la pantalla es baja, y si sobra aire
    /// se reparte en los espacios y en la altura de la barra de abajo. Pura, con pruebas. Ningún texto baja de 14 dp y la barra de abajo mide ≥ 52 dp.
    /// </summary>
    public static class EngranajesLayout
    {
        public const float W = 360f;
        /// <summary>La cabecera del cohete (Tarea 161): «Cohete número 12» pide 160 dp; las 10 luces empiezan a la derecha del título y se separan 16 dp (antes: título de 130 dp, luces desde 150 cada 19).</summary>
        public const float TitleW = 160f, LightsX0 = 184f, LightsStep = 16f;
        /// <summary>Lo que ocupa el marcador común de los juegos (GameHud, 190 unidades = 63 dp) más un respiro.</summary>
        public const float HudDp = 66f;
        public const float SideMargin = 14f;

        // la escena, en coordenadas del boceto (x 0..360, y hacia abajo): la MESA de taller (Tarea 164: una losa crema gruesa que lleva la sala, 8..250 × 114..462, como ventanilla noche, y el cohete, con aletas hasta 360 y carteles hasta 369), de 84 a 476 de alto
        public const float SceneTop = 84f, SceneBottom = 476f, SceneCenterY = 280f;
        public const float SceneHeight = SceneBottom - SceneTop;
        /// <summary>La mesa (de -8 a 377, centrada en 184,5) se corre 4,5 dp a la izquierda para quedar con el mismo margen a cada lado.</summary>
        public const float TableLeft = -8f, TableRight = 377f;
        public const float SceneShiftX = -4.5f;
        /// <summary>La ventanilla noche (la sala de siempre: el mismo tamaño y lugar que el panel de antes; la máquina no se mueve ni un píxel dentro de ella).</summary>
        public const float WindowCx = 129f, WindowCy = 288f, WindowW = 242f, WindowH = 348f;
        /// <summary>La mesa empieza 16 dp bajo el texto de «Cohete número N» (que termina hacia 92 dp) y el aviso va 10 dp bajo los botones.</summary>
        public const float TableTopDp = HudDp + 2f + StripH - 2f, SayGap = 10f;
        /// <summary>Cuánto aire sobrante se reparte (dp): lo que crecen los botones, lo que baja la mesa y lo que se separan los botones de la mesa; el resto queda bajo el aviso.</summary>
        public const float MaxButtonGrow = 26f, MaxTopShift = 36f, MaxGap = 64f;

        /// <summary>QuestionH = 0: la consigna ya no es una barra bajo la cabecera sino la fila de consigna del marcador común (Tarea 127); su alto lo aprovecha la escena.</summary>
        public const float StripH = 42f, QuestionH = 0f, MinScale = 0.55f;
        /// <summary>La escena nunca pasa de 0,88: así la flecha del motor (que sale de la sala por la izquierda) y la aleta del cohete quedan adentro de la pantalla y, sobre todo, el cartel de la turbina y de la antena (que cuelga a la derecha
        /// del cohete, hasta 369 en el boceto) queda a 16 dp o más del borde (8-oct: con 0,96 llegaba a 3 dp del borde y el marco del tutorial se cortaba).</summary>
        public const float MaxScale = 0.88f;
        /// <summary>El borde derecho del cartel más ancho (boceto) y el margen mínimo que debe dejar a la derecha (dp).</summary>
        public const float CartelRight = 369f, CartelMargin = 16f;

        public struct Metrics
        {
            public float StripTop, QuestionTop, SceneCenterLogicalY, SceneScale, ButtonsTop, ButtonH, SayTop, SayH, Bottom;
        }

        /// <summary>La altura que pide la pantalla con la escena a tamaño completo y los botones y el aviso más bajos que se permiten.</summary>
        public static float FullHeight(float buttonH, float sayH) => TableTopDp + SceneHeight + 8f + buttonH + SayGap + sayH + 6f;

        public static Metrics Compute(float height)
        {
            var m = new Metrics();
            float buttonH = 88f, sayH = 84f;
            // si ni así cabe a escala 0,6, el aviso y los botones se bajan al mínimo
            float fixedTop = TableTopDp;
            float fixedBottom = 8f + buttonH + SayGap + sayH + 6f;
            float avail = height - fixedTop - fixedBottom;
            float scale = Math.Min(MaxScale, avail / SceneHeight);
            if (scale < MinScale)
            {
                buttonH = 80f;
                sayH = 72f;
                fixedBottom = 8f + buttonH + SayGap + sayH + 6f;
                avail = height - fixedTop - fixedBottom;
                scale = Math.Max(MinScale, Math.Min(MaxScale, avail / SceneHeight));
            }
            float sceneH = SceneHeight * scale;
            float free = Math.Max(0f, avail - sceneH);              // aire que sobra (Tarea 164): en los botones (más altos), un poco sobre la mesa, entre la mesa y los botones y bajo el aviso; así no queda un tramo de cielo vacío de ~190 dp bajo los botones
            float growButtons = Math.Min(MaxButtonGrow, free * 0.3f);
            buttonH += growButtons;
            free -= growButtons;
            m.StripTop = HudDp + 2f;
            m.QuestionTop = m.StripTop + StripH + 4f;
            float topShift = Math.Min(MaxTopShift, free * 0.25f);   // la mesa baja un poco de la cabecera (en un teléfono de 800 dp, ~31 dp: sigue ~40 dp más arriba que antes de la Tarea 164)
            float gap = Math.Min(MaxGap, free * 0.35f);             // y los botones se separan de la mesa: quedan más a mano del pulgar
            float sceneTop = TableTopDp + topShift;
            m.SceneScale = scale;
            m.SceneCenterLogicalY = sceneTop + sceneH / 2f;
            m.ButtonH = buttonH;
            m.ButtonsTop = sceneTop + sceneH + 8f + gap;
            m.SayTop = m.ButtonsTop + buttonH + SayGap;
            m.SayH = sayH;
            m.Bottom = m.SayTop + sayH;
            return m;
        }

        /// <summary>De un punto del boceto (dentro de la escena) a dp lógicos.</summary>
        public static Vector2 SceneToLogical(Metrics m, float bx, float by) =>
            new Vector2(W / 2f + SceneShiftX + (bx - W / 2f) * m.SceneScale, m.SceneCenterLogicalY + (by - SceneCenterY) * m.SceneScale);

        /// <summary>De un punto en dp lógicos a un punto de la escena (coordenadas del boceto): lo contrario de <see cref="SceneToLogical"/> (para saber qué se tocó).</summary>
        public static Vector2 LogicalToScene(Metrics m, float lx, float ly) =>
            new Vector2(W / 2f + (lx - (W / 2f + SceneShiftX)) / m.SceneScale, SceneCenterY + (ly - m.SceneCenterLogicalY) / m.SceneScale);

        /// <summary>La casilla de la cuenta de cambios («1 cambio» con su llave), abajo a la izquierda (dp lógicos).</summary>
        public static Rect CounterRect(Metrics m) => new Rect(SideMargin, m.ButtonsTop, 104f, m.ButtonH);

        /// <summary>El botón grande «Arrancar», abajo a la derecha (dp lógicos).</summary>
        public static Rect StartRect(Metrics m)
        {
            float x = SideMargin + 104f + 12f;
            return new Rect(x, m.ButtonsTop, W - SideMargin - x, m.ButtonH);
        }
    }
}
