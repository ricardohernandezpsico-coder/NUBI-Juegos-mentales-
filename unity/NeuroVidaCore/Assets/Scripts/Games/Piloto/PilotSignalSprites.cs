using System.Collections.Generic;
using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Piloto
{
    /// <summary>
    /// Las señales de Piloto Estelar con el pincel de arcilla común (<see cref="NeuroVida.Games.Shared.ClayRaster"/>): cinco formas (hexágono, gota, círculo, cuadrado y triángulo; sin estrellas con puntas, cruces ni medias lunas) con tres detalles (punto, anillo y franja). Cada forma
    /// tiene SIEMPRE el mismo color: las señales parecidas (misma forma, otro detalle) tienen el mismo color y se distinguen por el detalle, no por el color. Se hornean una vez (15 sprites de 128 px) y se reutilizan. Todo plano.
    /// </summary>
    public static class PilotSignalSprites
    {
        private const int SizePx = 128;
        private const float Zoom = 1.18f, Line = 0.075f, Drop = 0.06f, Aa = 0.03f;

        /// <summary>Del diámetro de la señal (dp) al lado del sprite (dp): la forma ocupa 0,92 de los 1,18 de «zoom», así que lado = diámetro × 1,18 / 0,92 ≈ 1,28.</summary>
        public const float SideInDiameters = 1.28f;

        /// <summary>El color fijo de cada forma.</summary>
        public static readonly Color[] ShapeColors =
        {
            Hex(0xA6E36B),      // hexágono
            Hex(0x7FD8FF),      // gota
            Hex(0xFF8A6B),      // círculo
            Hex(0xFFC94A),      // cuadrado
            Hex(0xB79BFF),      // triángulo
        };

        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        public static Sprite Get(SignalShape shape, SignalDetail detail)
        {
            int key = (int)shape * 8 + (int)detail;
            if (!Cache.TryGetValue(key, out var s) || s == null)
            {
                s = ToSprite(Render(shape, detail, SizePx), SizePx, SizePx);
                Cache[key] = s;
            }
            return s;
        }

        /// <summary>Hornea las 15 de a poco (durante la cuenta regresiva).</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            for (int sh = 0; sh < PilotContract.ShapeCount; sh++)
            {
                for (int d = 0; d < PilotContract.DetailCount; d++) Get((SignalShape)sh, (SignalDetail)d);
                yield return null;
            }
        }

        // ------------------------------------------------------------------ formas (y arriba, como en Unity)

        private static readonly float[] HexPts = BuildPolygon(6, 0.9f, Mathf.PI / 6f);
        private static readonly float[] TriPts = { 0f, 0.98f, 0.98f, -0.74f, -0.98f, -0.74f };

        private static float[] BuildPolygon(int n, float r, float start)
        {
            var p = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                float a = start + i * 2f * Mathf.PI / n;
                p[i * 2] = Mathf.Cos(a) * r;
                p[i * 2 + 1] = Mathf.Sin(a) * r;
            }
            return p;
        }

        private static float ShapeSdf(SignalShape shape, float x, float y)
        {
            switch (shape)
            {
                case SignalShape.Hexagon: return Polygon(x, y, HexPts) - 0.05f;
                case SignalShape.Drop: return UnevenCapsule(x, y + 0.34f, 0.66f, 0.07f, 1.3f);
                case SignalShape.Circle: return Circle(x, y, 0f, 0f, 0.92f);
                case SignalShape.Square: return RoundBox(x, y, 0f, 0f, 0.74f, 0.74f, 0.22f);
                default: return Polygon(x, y, TriPts) - 0.08f;
            }
        }

        /// <summary>El centro del detalle dentro de cada forma (y arriba).</summary>
        private static float DetailY(SignalShape shape)
        {
            switch (shape)
            {
                case SignalShape.Drop: return -0.30f;
                case SignalShape.Triangle: return -0.18f;
                default: return 0f;
            }
        }

        public static Color32[] Render(SignalShape shape, SignalDetail detail, int size)
        {
            Color fill = ShapeColors[(int)shape];
            float cy = DetailY(shape);
            return RenderClay(size, Zoom, Line, Drop, Aa, (x, y) => ShapeSdf(shape, x, y), (ref Px p, float x, float y) =>
            {
                float sdf = ShapeSdf(shape, x, y);
                p.Over(fill, Cover(sdf + 0.02f, Aa));
                p.Over(new Color(1f, 1f, 1f, 0.38f), Cover(Ellipse(x, y, -0.3f, 0.42f, 0.2f, 0.09f), Aa) * Cover(sdf + 0.08f, Aa));
                switch (detail)
                {
                    case SignalDetail.Dot:
                        p.Over(Ink, Cover(Circle(x, y, 0f, cy, 0.27f), Aa));
                        break;
                    case SignalDetail.Ring:
                        p.Over(Ink, Cover(Mathf.Abs(Circle(x, y, 0f, cy, 0.42f)) - 0.075f, Aa));
                        break;
                    default:
                        p.Over(Ink, Cover(Mathf.Abs(y - cy) - 0.15f, Aa) * Cover(sdf + 0.03f, Aa));
                        break;
                }
            });
        }
    }
}
