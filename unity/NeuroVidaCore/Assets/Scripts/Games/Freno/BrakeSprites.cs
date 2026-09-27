using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Freno
{
    /// <summary>
    /// Arte procedural de Freno de Emergencia, con el pincel de arcilla común (<see cref="ClayRaster"/>):
    /// <list type="bullet">
    /// <item><see cref="StopSign"/>: señal de ALTO, octágono coral con aro crema (el texto "ALTO" lo pone un Text
    /// encima). Forma + texto + sonido: nunca solo color.</item>
    /// <item><see cref="LaunchPad"/>: plataforma de lanzamiento con franjas de precaución y una baliza apagada.</item>
    /// <item><see cref="LaunchButton"/>: botón de arcilla de cada carril, con una flecha hacia arriba.</item>
    /// </list>
    /// Colores horneados: <c>Image.color</c> en blanco (salvo para atenuar).
    /// </summary>
    public static class BrakeSprites
    {
        private static Sprite _stop, _pad, _button, _buttonLit;

        public static Sprite StopSign() => _stop != null ? _stop : _stop = ToSprite(RenderStopSign(256), 256, 256);
        public static Sprite LaunchPad() => _pad != null ? _pad : _pad = ToSprite(RenderLaunchPad(256), 256, 256);
        public static Sprite LaunchButton(bool lit)
        {
            if (lit) return _buttonLit != null ? _buttonLit : _buttonLit = ToSprite(RenderButton(256, true), 256, 256);
            return _button != null ? _button : _button = ToSprite(RenderButton(256, false), 256, 256);
        }

        // ------------------------------------------------------------------ alto

        private static readonly float[] Octagon = OctagonPoints(0.82f);

        private static float[] OctagonPoints(float r)
        {
            var pts = new float[16];
            for (int i = 0; i < 8; i++)
            {
                float a = Mathf.PI / 8f + i * Mathf.PI / 4f;
                pts[i * 2] = r * Mathf.Cos(a);
                pts[i * 2 + 1] = r * Mathf.Sin(a);
            }
            return pts;
        }

        private static readonly float[] OctagonInner = OctagonPoints(0.68f);

        private static float StopBody(float x, float y) => Polygon(x, y, Octagon);

        public static Color32[] RenderStopSign(int size) => RenderClay(size, 1.1f, 0.07f, 0.09f, 0.02f, StopBody, (ref Px p, float x, float y) =>
        {
            float body = StopBody(x, y);
            p.Over(Coral, Cover(body, 0.02f));
            float inner = Polygon(x, y, OctagonInner);
            p.Over(Cream, Cover(Mathf.Abs(inner) - 0.035f, 0.02f));
            p.Over(new Color(1f, 1f, 1f, 0.35f), Cover(Ellipse(x, y, -0.3f, 0.5f, 0.22f, 0.07f), 0.02f) * Cover(body + 0.05f, 0.02f));
        });

        // ------------------------------------------------------------------ plataforma

        private static float PadBody(float x, float y) =>
            Mathf.Min(RoundBox(x, y, 0f, -0.62f, 0.9f, 0.16f, 0.08f),                 // losa
                Mathf.Min(RoundBox(x, y, -0.62f, -0.2f, 0.07f, 0.3f, 0.04f),            // torre izquierda
                    RoundBox(x, y, 0.62f, -0.2f, 0.07f, 0.3f, 0.04f)));                 // torre derecha

        public static Color32[] RenderLaunchPad(int size) => RenderClay(size, 1.05f, 0.05f, 0.07f, 0.02f, PadBody, (ref Px p, float x, float y) =>
        {
            var slab = Hex(0x9C94D6);
            float s = RoundBox(x, y, 0f, -0.62f, 0.9f, 0.16f, 0.08f);
            p.Over(slab, Cover(s, 0.02f));
            // Franjas de precaución (sol / tinta) en la cara de la losa.
            if (s < 0f && y < -0.58f)
            {
                float stripe = Mathf.Repeat((x - y) * 4.5f, 1f);
                p.Over(stripe < 0.5f ? Sun : Ink, Cover(s + 0.06f, 0.02f) * (y < -0.64f ? 1f : 0f));
            }
            p.Over(new Color(1f, 1f, 1f, 0.3f), Cover(RoundBox(x, y, -0.3f, -0.51f, 0.4f, 0.025f, 0.02f), 0.02f));
            float towers = Mathf.Min(RoundBox(x, y, -0.62f, -0.2f, 0.07f, 0.3f, 0.04f), RoundBox(x, y, 0.62f, -0.2f, 0.07f, 0.3f, 0.04f));
            p.Over(Shade(slab, 0.85f), Cover(towers, 0.02f));
            // Travesaños de las torres.
            for (int i = 0; i < 3; i++)
            {
                float ty = -0.36f + i * 0.14f;
                float bar = Mathf.Min(Capsule(x, y, -0.66f, ty, -0.58f, ty + 0.09f, 0.012f), Capsule(x, y, 0.66f, ty, 0.58f, ty + 0.09f, 0.012f));
                p.Over(Ink, Cover(bar, 0.015f) * Cover(towers, 0.02f));
            }
        });

        // ------------------------------------------------------------------ botón

        private static float ButtonBody(float x, float y) => Circle(x, y, 0f, 0f, 0.8f);

        public static Color32[] RenderButton(int size, bool lit) => RenderClay(size, 1.1f, 0.08f, 0.12f, 0.025f, ButtonBody, (ref Px p, float x, float y) =>
        {
            float disc = ButtonBody(x, y);
            p.Over(lit ? Lime : Hex(0x2A3590), Cover(disc, 0.025f));
            p.Over(new Color(1f, 1f, 1f, lit ? 0.45f : 0.25f), Cover(Ellipse(x, y, -0.28f, 0.44f, 0.24f, 0.08f), 0.025f) * Cover(disc + 0.04f, 0.025f));
            // Flecha hacia arriba (lanzar).
            float arrow = Mathf.Min(Capsule(x, y, 0f, -0.36f, 0f, 0.3f, 0.09f),
                Mathf.Min(Capsule(x, y, -0.27f, 0.04f, 0f, 0.34f, 0.09f), Capsule(x, y, 0.27f, 0.04f, 0f, 0.34f, 0.09f)));
            p.Over(lit ? Ink : Cream, Cover(arrow, 0.025f));
        });
    }
}
