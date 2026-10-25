using System;
using System.Collections.Generic;
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
        /// <summary>Cuánto aire sobrante se reparte (dp): lo que crecen los botones (nada: Ricardo, 25-oct, «para este juego no requiere que sean tan grandes»: se quedan en 88 dp), lo que baja la mesa y lo que se separan los botones de la mesa; el resto queda bajo el aviso.
        /// La escena no crece con la altura: su ancho ya llega a 10 dp de cada borde de la pantalla (<see cref="MaxScale"/>).</summary>
        public const float MaxButtonGrow = 0f, MaxTopShift = 44f, MaxGap = 72f;

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
            float topShift = Math.Min(MaxTopShift, free * 0.30f);   // la mesa baja un poco de la cabecera (en un teléfono de 800 dp, 44 dp: sigue ~20 dp más arriba que antes de la Tarea 164)
            float gap = Math.Min(MaxGap, free * 0.45f);             // y los botones se separan de la mesa: quedan más a mano del pulgar
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

        // ------------------------------------------------------------------ el rótulo «Motor» (Tarea 166)

        /// <summary>El rótulo «Motor» (boceto, dp): la losa chica del kit, de 66 × 30. Antes colgaba 16 dp bajo el centro del motor y le tapaba media rueda.</summary>
        public const float MotorPillW = 66f, MotorPillH = 30f;
        /// <summary>Lo que el rótulo deja libre del círculo del motor (sus dientes llegan a la punta), de otras ruedas y del borde de la ventanilla (dp del boceto).</summary>
        public const float MotorPillClearMotor = 4f, MotorPillClearGear = 3f, MotorPillClearWindow = 4f;

        private static float DistToRect(float px, float py, float cx, float cy, float hw, float hh)
        {
            float dx = Math.Max(Math.Abs(px - cx) - hw, 0f), dy = Math.Max(Math.Abs(py - cy) - hh, 0f);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Dónde va el rótulo «Motor» de esta máquina (centro, en coordenadas del boceto): FUERA del círculo del motor (la consigna dice «Toca el motor» y el rótulo le tapaba media rueda), DENTRO de la ventanilla y, si hay lugar, sin tocar otra rueda, una barra dentada,
        /// una correa ni un eje. Como el motor cambia de lugar en cada máquina, se busca en una rejilla de 2 dp el lugar más cercano al motor (mejor debajo que arriba: ahí cae la flecha del giro) que cumpla; si ninguno está del todo libre se toma el que menos choca, pero
        /// nunca sobre el motor. <paramref name="touches"/> = cuántos dp de otras piezas pisa (0 = libre).
        /// </summary>
        public static Vector2 MotorPillCenter(Machine m, out float touches)
        {
            float hw = MotorPillW / 2f, hh = MotorPillH / 2f;
            var g0 = m.Gears[0];
            float wl = WindowCx - WindowW / 2f + MotorPillClearWindow + hw, wr = WindowCx + WindowW / 2f - MotorPillClearWindow - hw;
            float wt = WindowCy - WindowH / 2f + MotorPillClearWindow + hh, wb = WindowCy + WindowH / 2f - MotorPillClearWindow - hh;
            var best = new Vector2(g0.X, g0.Y + g0.Tip + MotorPillClearMotor + hh);
            float bestScore = float.MaxValue; touches = float.MaxValue; bool found = false;
            for (float py = wt; py <= wb + 0.01f; py += 2f)
                for (float px = wl; px <= wr + 0.01f; px += 2f)
                {
                    if (DistToRect(g0.X, g0.Y, px, py, hw, hh) < g0.Tip + MotorPillClearMotor) continue;          // nunca sobre el motor
                    float over = 0f;
                    for (int i = 1; i < m.Gears.Count; i++)
                    {
                        var g = m.Gears[i];
                        over += Math.Max(0f, g.Tip + MotorPillClearGear - DistToRect(g.X, g.Y, px, py, hw, hh));
                        if (m.Stations.TryGetValue(Station.Compuerta, out int gc) && gc == i || m.Stations.TryGetValue(Station.Carga, out int gk) && gk == i)    // la barra dentada, a la derecha de su engranaje (20 × 84)
                            over += Math.Max(0f, Math.Min(px + hw, g.X + g.Pitch + 17.5f) - Math.Max(px - hw, g.X + g.Pitch - 2.5f)) > 0f && Math.Abs(py - g.Y) < hh + 42f ? 8f : 0f;
                        if (g.HasStation && (g.Station == Station.Antena || g.Station == Station.Turbina))                // el eje hasta la pieza, en horizontal a la altura del engranaje
                            over += px + hw > g.X && Math.Abs(py - g.Y) < hh + 4f ? 6f : 0f;
                    }
                    foreach (var l in m.Links)                                                                          // las correas: la tira entre las dos poleas
                    {
                        if (l.Type != LinkType.Belt) continue;
                        var a = m.Gears[l.A]; var b = m.Gears[l.B];
                        for (int k = 0; k <= 8; k++)
                            if (DistToRect(Mathf.Lerp(a.X, b.X, k / 8f), Mathf.Lerp(a.Y, b.Y, k / 8f), px, py, hw, hh) < 8f) { over += 5f; break; }
                    }
                    float dist = Mathf.Sqrt((px - g0.X) * (px - g0.X) + (py - g0.Y) * (py - g0.Y));
                    float score = over * 400f + dist + (py < g0.Y ? 12f : 0f);
                    if (score < bestScore) { bestScore = score; best = new Vector2(px, py); touches = over; found = true; }
                }
            if (!found) touches = float.MaxValue;
            return best;
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
