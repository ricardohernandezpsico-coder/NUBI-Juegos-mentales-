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
        /// <summary>Lo que ocupa el marcador común de los juegos (GameHud, 190 unidades = 63 dp) más un respiro.</summary>
        public const float HudDp = 66f;
        public const float SideMargin = 14f;

        // la escena, en coordenadas del boceto (x 0..360, y hacia abajo): la sala (8..250) y el cohete (aletas hasta 360), de la antena (92) a la llama (470)
        public const float SceneTop = 88f, SceneBottom = 472f, SceneCenterY = 280f;
        public const float SceneHeight = SceneBottom - SceneTop;
        /// <summary>La escena (8..361 de ancho) se corre 4,5 dp a la izquierda para quedar con el mismo margen a cada lado.</summary>
        public const float SceneShiftX = -4.5f;

        public const float StripH = 42f, QuestionH = 48f, MinScale = 0.55f;
        /// <summary>La escena nunca pasa de 0,96: así la flecha del motor (que sale de la sala por la izquierda) y la aleta del cohete quedan ~10 dp adentro de la pantalla y no se cortan.</summary>
        public const float MaxScale = 0.96f;

        public struct Metrics
        {
            public float StripTop, QuestionTop, SceneCenterLogicalY, SceneScale, ButtonsTop, ButtonH, SayTop, SayH, Bottom;
        }

        /// <summary>La altura que pide la pantalla con la escena a tamaño completo y los botones y el aviso más bajos que se permiten.</summary>
        public static float FullHeight(float buttonH, float sayH) => HudDp + 2f + StripH + 4f + QuestionH + 6f + SceneHeight + 8f + buttonH + 6f + sayH + 6f;

        public static Metrics Compute(float height)
        {
            var m = new Metrics();
            float buttonH = 88f, sayH = 84f;
            // si ni así cabe a escala 0,6, el aviso y los botones se bajan al mínimo
            float fixedTop = HudDp + 2f + StripH + 4f + QuestionH + 6f;
            float fixedBottom = 8f + buttonH + 6f + sayH + 6f;
            float avail = height - fixedTop - fixedBottom;
            float scale = Math.Min(MaxScale, avail / SceneHeight);
            if (scale < MinScale)
            {
                buttonH = 80f;
                sayH = 72f;
                fixedBottom = 8f + buttonH + 6f + sayH + 6f;
                avail = height - fixedTop - fixedBottom;
                scale = Math.Max(MinScale, Math.Min(MaxScale, avail / SceneHeight));
            }
            float sceneH = SceneHeight * scale;
            float free = Math.Max(0f, avail - sceneH);              // aire que sobra: arriba y abajo de la escena y en los botones
            float growButtons = Math.Min(14f, free * 0.4f);
            buttonH += growButtons;
            free -= growButtons;
            m.StripTop = HudDp + 2f;
            m.QuestionTop = m.StripTop + StripH + 4f;
            float sceneTop = m.QuestionTop + QuestionH + 6f + free * 0.4f;
            m.SceneScale = scale;
            m.SceneCenterLogicalY = sceneTop + sceneH / 2f;
            m.ButtonH = buttonH;
            m.ButtonsTop = sceneTop + sceneH + 8f + free * 0.3f;
            m.SayTop = m.ButtonsTop + buttonH + 6f;
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
