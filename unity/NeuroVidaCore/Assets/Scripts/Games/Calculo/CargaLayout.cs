using System;
using UnityEngine;

namespace NeuroVida.Games.Calculo
{
    /// <summary>
    /// Disposición de «Carga exacta» en dp lógicos (360 de ancho; y desde arriba del área de juego, que incluye el marcador). Es la del boceto aprobado
    /// (docs/previews/carga-exacta-boceto.html: reactor arriba, celdas al medio, operaciones y botones abajo) pero ESTIRADA a la altura de cada teléfono: el aire que
    /// sobra se reparte entre el reactor, las celdas y los botones. Pura, con pruebas. Las celdas miden ≥ 44 dp y los botones ≥ 52 dp.
    /// </summary>
    public static class CargaLayout
    {
        public const float W = 360f;
        /// <summary>Lo que ocupa el marcador común de los juegos (GameHud, 190 unidades = 63 dp) más un respiro.</summary>
        public const float HudDp = 66f;
        public const float SideMargin = 14f;
        /// <summary>Radio del reactor entero (aro con luces) y de su núcleo.</summary>
        public const float RingR = 84f;
        public const float CoreR = 72f;
        public const float CellH = 74f;
        public const float OpW = 64f, OpH = 58f, OpGap = 12f;
        public const float ToolW = 150f, ToolH = 52f, ToolGap = 12f;

        public struct Metrics
        {
            public float DotsY, ReactorY, SayY, CellsY, GuideY, OpsY, ToolsY;
        }

        /// <summary>Lo mínimo que necesita la pantalla, sin aire.</summary>
        public const float MinHeight = HudDp + 22f + 2f * RingR + 44f + CellH + 28f + OpH + 16f + ToolH + 28f;

        public static Metrics Compute(float height)
        {
            var m = new Metrics();
            float free = Math.Max(0f, height - MinHeight);
            m.DotsY = HudDp + 11f;
            float y = HudDp + 22f + 0.2f * free;
            m.ReactorY = y + RingR;
            y += 2f * RingR;
            m.SayY = y + 22f;
            y += 44f + 0.3f * free;
            m.CellsY = y + CellH / 2f;
            y += CellH;
            m.GuideY = y + 14f;
            y += 28f + 0.1f * free;
            m.OpsY = y + OpH / 2f;
            y += OpH + 16f + 0.1f * free;
            m.ToolsY = y + ToolH / 2f;
            return m;
        }

        public static float CellGap(int n) => n >= 5 ? 8f : 12f;

        public static float CellW(int n)
        {
            n = Math.Max(1, n);
            return Math.Min(62f, (W - 2f * SideMargin - (n - 1) * CellGap(n)) / n);
        }

        /// <summary>El centro de la celda <paramref name="k"/> de <paramref name="n"/> (centradas).</summary>
        public static Vector2 CellPos(Metrics m, int k, int n)
        {
            float w = CellW(n), gap = CellGap(n);
            float total = n * w + (n - 1) * gap;
            return new Vector2((W - total) / 2f + k * (w + gap) + w / 2f, m.CellsY);
        }

        public static Vector2 OpPos(Metrics m, int k, int n)
        {
            float total = n * OpW + (n - 1) * OpGap;
            return new Vector2((W - total) / 2f + k * (OpW + OpGap) + OpW / 2f, m.OpsY);
        }

        /// <summary>0 = «Deshacer», 1 = «Empezar de nuevo».</summary>
        public static Vector2 ToolPos(Metrics m, int k)
        {
            float total = 2f * ToolW + ToolGap;
            return new Vector2((W - total) / 2f + k * (ToolW + ToolGap) + ToolW / 2f, m.ToolsY);
        }
    }
}
