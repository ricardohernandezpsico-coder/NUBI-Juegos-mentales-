using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Nubi maestra: la nebulosa de arcilla (nube esponjosa lavanda con contorno fino morado, ojos grandes con dos brillos y rubor) junto a una pizarra con una
    /// ruta de puntos, señalándola con un puntero. Es el arte de la tarjeta de entrada del tutorial guiado (<see cref="GuidedTutorial"/>), y se hornea
    /// por código con el mismo pincel SDF de todo el arte del juego (sin assets): réplica en Unity de <c>pose_pizarra</c> de <c>tools/previews/nubi_guia.py</c>
    /// (la nube es la de <c>nubi_suave.py</c>). Nubi explica y acompaña: nunca evalúa ni pone cara triste.
    /// </summary>
    public static class NubiTeacherSprite
    {
        private const int SizePx = 320;
        private static Sprite _sprite;

        public static Sprite Get() => _sprite != null ? _sprite : _sprite = ToSprite(Render(SizePx), SizePx, 100f);

        // Unidades del dibujo original (la nube mide ~74 de ancho). El lienzo cubre 100 × 100 unidades, centrado en (0, -1) del dibujo (y hacia abajo).
        private const float Units = 100f;
        private static readonly Color Line = new Color32(74, 56, 150, 255);
        private static readonly Color Base = new Color32(172, 168, 248, 255);
        private static readonly Color Light = new Color32(232, 230, 255, 255);
        private static readonly Color Shade = new Color32(118, 108, 214, 255);
        private static readonly Color TintC = new Color32(140, 210, 255, 255);
        private static readonly Color TintV = new Color32(196, 160, 255, 255);
        private static readonly Color Eye = new Color32(32, 26, 78, 255);
        private static readonly Color EyeIris = new Color32(84, 96, 200, 255);
        private static readonly Color Blush = new Color32(255, 140, 170, 255);
        private static readonly Color Board = new Color32(40, 72, 96, 255);
        private static readonly Color Chalk = new Color32(240, 240, 240, 255);

        // (x, y, radio) de los copos de la nube, con y hacia abajo como en el dibujo original
        private static readonly float[] Puffs =
        {
            -24, 6, 11, -19, -8, 12, -7, -17, 13, 8, -17, 13, 20, -8, 12, 25, 6, 11, 16, 16, 12, 0, 19, 13, -16, 16, 12
        };

        private static float SMin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return b * (1f - h) + a * h - k * h * (1f - h);
        }

        /// <summary>Distancia con signo a la nube (centro en el origen, y hacia abajo, unidades del dibujo).</summary>
        private static float CloudSdf(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + (y - 2f) * (y - 2f)) - 22f;
            for (int i = 0; i < Puffs.Length; i += 3)
            {
                float dx = x - Puffs[i], dy = y - Puffs[i + 1];
                d = SMin(d, Mathf.Sqrt(dx * dx + dy * dy) - Puffs[i + 2], 1.6f);
            }
            return d;
        }

        /// <summary>Píxeles del dibujo (fila 0 = abajo). Separado para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(int size)
        {
            var pixels = new Color32[size * size];
            float aa = Units / size * 1.3f;            // ~1,3 px en unidades
            const float nx = -16f, ny = 8f, nk = 0.82f; // Nubi: posición y escala dentro de la pose
            const float bx0 = -4f, by0 = -40f, bx1 = 46f, by1 = -4f; // pizarra
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    // píxel → unidades del dibujo (y hacia abajo)
                    float x = ((px + 0.5f) / size - 0.5f) * Units;
                    float y = -(((py + 0.5f) / size - 0.5f) * Units) - 1f;
                    var p = new Px();

                    // ---- pizarra: sombra dura, marco y fondo
                    float board = RoundBox(x, y, (bx0 + bx1) * 0.5f, (by0 + by1) * 0.5f, (bx1 - bx0) * 0.5f, (by1 - by0) * 0.5f, 3f);
                    float boardShadow = RoundBox(x, y - 2f, (bx0 + bx1) * 0.5f, (by0 + by1) * 0.5f, (bx1 - bx0) * 0.5f, (by1 - by0) * 0.5f, 3f);
                    p.Over(Line, Cover(boardShadow, aa));
                    p.Over(Line, Cover(board, aa));
                    p.Over(Board, Cover(board + 1.4f, aa));
                    // dibujo en la pizarra: dos planetas y una ruta de puntos
                    p.Over(Chalk, Cover(Mathf.Abs(Circle(x, y, bx0 + 10f, by1 - 10f, 4f)) - 0.55f, aa));
                    p.Over(new Color(1f, 0.79f, 0.24f), Cover(Mathf.Abs(Circle(x, y, bx1 - 10f, by0 + 10f, 4f)) - 0.55f, aa));
                    float[] route = { bx0 + 14f, by1 - 10f, bx0 + 24f, by1 - 20f, bx1 - 20f, by1 - 12f, bx1 - 14f, by0 + 13f };
                    for (int s = 0; s < 3; s++)
                        for (int k = 0; k < 4; k++)
                        {
                            float t = k / 4f;
                            float qx = Mathf.Lerp(route[s * 2], route[s * 2 + 2], t), qy = Mathf.Lerp(route[s * 2 + 1], route[s * 2 + 3], t);
                            p.Over(Chalk, Cover(Circle(x, y, qx, qy, 0.9f), aa));
                        }

                    // ---- puntero: del brazo de Nubi al punto de la ruta
                    float hx = 6f, hy = 2f, tx = bx0 + 25f, ty = by1 - 20f;
                    p.Over(Line, Cover(Capsule(x, y, hx, hy, tx, ty, 1.55f), aa));
                    p.Over(new Color32(210, 160, 100, 255), Cover(Capsule(x, y, hx, hy, tx, ty, 0.85f), aa));

                    // ---- Nubi (centro en nx, ny; escala nk)
                    float cx = (x - nx) / nk, cy = (y - ny) / nk;
                    float d = CloudSdf(cx, cy);
                    float aaC = aa / nk;
                    // halo suave
                    p.Over(new Color(150f / 255f, 140f / 255f, 1f, 0.22f), Mathf.Clamp01(1f - (d - 1f) / 9f) * (d > 0f ? 1f : 0f));
                    p.Over(Line, Cover(d - 1.7f, aaC));
                    if (d < 3f)
                    {
                        float t = Mathf.Clamp01((cy + 0.35f * cx + 20f) / 44f);
                        Color col = t < 0.45f ? Color.Lerp(Light, Base, t / 0.45f) : Color.Lerp(Base, Shade, (t - 0.45f) / 0.55f);
                        float inside = Cover(d, aaC);
                        p.Over(col, inside);
                        // toques de color: celeste abajo a la izquierda, violeta a la derecha (nebulosa)
                        p.Over(WithAlpha(TintC, 0.4f * Mathf.Clamp01(1f - Dist(cx, cy, -14f, 12f) / 12f)), inside);
                        p.Over(WithAlpha(TintV, 0.32f * Mathf.Clamp01(1f - Dist(cx, cy, 15f, 4f) / 11f)), inside);
                        // luz de cada copo (arriba a la izquierda)
                        float lit = 0f;
                        for (int i = 0; i < Puffs.Length; i += 3)
                        {
                            float r = Puffs[i + 2];
                            lit = Mathf.Max(lit, Mathf.Pow(Mathf.Clamp01(1f - Dist(cx, cy, Puffs[i] - 0.3f * r, Puffs[i + 1] - 0.38f * r) / (0.62f * r)), 1.5f));
                        }
                        p.Over(new Color(1f, 1f, 1f, 0.5f * lit), inside);
                        // cara: ojos grandes con dos brillos, boca y rubor
                        float ey = 2f;
                        foreach (float sx in new[] { -1f, 1f })
                        {
                            float ex = sx * 10.5f;
                            p.Over(Eye, Cover(Ellipse(cx, cy, ex, ey, 5f, 6.1f), aaC) * inside);
                            p.Over(WithAlpha(EyeIris, 0.6f), Cover(Ellipse(cx, cy, ex, ey + 3.2f, 4f, 2.8f), aaC) * Cover(Ellipse(cx, cy, ex, ey, 5f, 6.1f), aaC) * inside);
                            p.Over(Color.white, Cover(Circle(cx, cy, ex - 1.6f, ey - 2.4f, 2.2f), aaC) * inside);
                            p.Over(Color.white, Cover(Circle(cx, cy, ex + 1.75f, ey + 2.6f, 0.9f), aaC) * inside);
                            p.Over(WithAlpha(Blush, 0.45f), Cover(Ellipse(cx, cy, sx * 17f, 8f, 4.2f, 2.6f), aaC * 2f) * inside);
                        }
                        // sonrisa: arco chico bajo los ojos
                        float mouth = Mathf.Abs(Circle(cx, cy, 0f, 8.2f, 3.2f)) - 0.7f;
                        if (cy > 8.4f) p.Over(Eye, Cover(mouth, aaC) * inside);
                    }

                    // ---- mano que sostiene el puntero
                    float hand = Circle(x, y, hx, hy, 4.8f);
                    p.Over(Line, Cover(hand - 1.6f, aa));
                    p.Over(Base, Cover(hand, aa));
                    p.Over(WithAlpha(Light, 0.9f), Cover(Circle(x, y, hx - 1.4f, hy - 1.6f, 2.6f), aa) * Cover(hand, aa));

                    pixels[py * size + px] = p.ToColor32();
                }
            }
            return pixels;
        }

        private static float Dist(float x, float y, float cx, float cy)
        {
            float dx = x - cx, dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
