using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Meteoros
{
    /// <summary>Las tres propuestas de roca de la maqueta <c>docs/previews/meteoros-rocas.png</c>: A = arcilla con volumen,
    /// B = A con el borde delantero al rojo vivo y estela de calor, C = cristal.</summary>
    public enum RockStyle { A, B, C }

    /// <summary>
    /// PROPUESTAS de arte para los meteoros (1-oct, "se ven muy básicas"): NO se usan todavía en el juego; solo las dibuja
    /// la vista previa (<c>tools/art-preview</c>). Mismo pincel que el resto (<see cref="ClayRaster"/>), sin dependencias
    /// de Unity más allá de los tipos de color.
    /// <list type="bullet">
    /// <item>A: un campo de alturas (domo + cráteres con borde levantado + grano) iluminado desde arriba a la izquierda:
    /// sombras internas, canto claro, cráteres con su pared iluminada y grano de arcilla.</item>
    /// <item>B: A y, en el borde que va por delante, una brasa naranja-coral que se ve también fuera del contorno;
    /// <see cref="RenderHeatTrail"/> es la estela de calor con chispas.</item>
    /// <item>C: meteoro de cristal o hielo: facetas planas lila-celeste, un cristal interior más claro y un destello.</item>
    /// </list>
    /// </summary>
    public static class MeteorSpritesV2
    {
        private const float Zoom = 1.08f;
        /// <summary>Zoom del sprite de la opción B (más margen para el resplandor de la brasa).</summary>
        public const float ZoomB = 1.3f;
        public const float ZoomBase = 1.08f;
        private static readonly float Lx = -0.55f, Ly = 0.62f, Lz = 0.56f;

        private static readonly float[][] Phase =
        {
            new[] { 0.3f, 1.9f, 4.1f, 2.2f }, new[] { 2.2f, 0.4f, 1.3f, 5.1f }, new[] { 4.6f, 3.1f, 0.7f, 3.3f }, new[] { 1.1f, 5.2f, 2.6f, 0.9f }
        };

        // (x, y, radio) de los cráteres de cada forma: grandes y en los bordes, para que se vean sin tapar la placa.
        private static readonly float[][] Craters =
        {
            new[] { -0.62f, 0.34f, 0.17f, 0.60f, -0.36f, 0.14f, 0.06f, 0.46f, 0.10f, 0.80f, 0.10f, 0.08f, -0.74f, -0.22f, 0.08f },
            new[] { -0.74f, -0.20f, 0.14f, -0.30f, 0.45f, 0.12f, 0.66f, 0.30f, 0.16f, 0.34f, -0.46f, 0.10f, 0.0f, 0.5f, 0.07f },
            new[] { -0.52f, -0.40f, 0.15f, 0.14f, 0.47f, 0.11f, 0.74f, -0.10f, 0.13f, -0.78f, 0.22f, 0.09f, 0.45f, -0.45f, 0.07f },
            new[] { 0.52f, 0.40f, 0.14f, -0.66f, 0.26f, 0.16f, -0.12f, -0.48f, 0.11f, 0.78f, -0.26f, 0.10f, -0.4f, 0.5f, 0.07f },
        };

        private static float Hash(float x, float y)
        {
            float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        private static float Noise(float x, float y)
        {
            float ix = Mathf.Floor(x), iy = Mathf.Floor(y), fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(ix, iy), b = Hash(ix + 1f, iy), c = Hash(ix, iy + 1f), d = Hash(ix + 1f, iy + 1f);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Grain(float x, float y) => 0.6f * Noise(x * 9f, y * 9f) + 0.3f * Noise(x * 21f, y * 21f) + 0.1f * Noise(x * 47f, y * 47f);

        private static float RockSdf(float x, float y, int shape)
        {
            var ph = Phase[shape];
            float a = Mathf.Atan2(y, x);
            float wobble = 1f + 0.07f * Mathf.Sin(3f * a + ph[0]) + 0.045f * Mathf.Sin(5f * a + ph[1]) + 0.03f * Mathf.Sin(9f * a + ph[2]) + 0.015f * Mathf.Sin(14f * a + ph[3]);
            float d = Mathf.Sqrt(x * x / (0.9f * 0.9f) + y * y / (0.62f * 0.62f));
            return (d - wobble) * 0.5f;
        }

        /// <summary>Altura de la roca (0 en el borde, hasta ~0,55 en el centro) con cráteres hundidos de borde levantado y grano.</summary>
        private static float Height(float x, float y, int shape)
        {
            float d = RockSdf(x, y, shape);
            float h = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-d / 0.42f)) * 0.55f;
            var cr = Craters[shape];
            for (int i = 0; i < cr.Length; i += 3)
            {
                float dd = Circle(x, y, cr[i], cr[i + 1], cr[i + 2]) / cr[i + 2]; // < 0 adentro
                h -= 0.24f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-dd) * 1.7f);
                h += 0.08f * Mathf.Exp(-dd * dd * 16f);
            }
            h += (Grain(x + shape * 3.1f, y) - 0.5f) * 0.08f;
            return h;
        }

        private static Color Mul(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
        private static Color Add(Color c, float k) => new Color(Mathf.Clamp01(c.r + k), Mathf.Clamp01(c.g + k), Mathf.Clamp01(c.b + k), c.a);

        public static Color32[] Render(RockStyle style, int size, int shape, RockTint tint)
        {
            shape = ((shape % 4) + 4) % 4;
            return style == RockStyle.C ? RenderCrystal(size, shape, tint) : RenderClayRock(size, shape, tint, style == RockStyle.B);
        }

        // ------------------------------------------------------------------ A y B: roca de arcilla con volumen

        private static readonly float EmberX = 0.55f, EmberY = -0.83f; // hacia donde cae el meteoro: abajo a la derecha

        private static Color32[] RenderClayRock(int size, int shape, RockTint tint, bool ember)
        {
            var baseColor = MeteorSprites.TintColor(tint);
            const float aa = 0.02f;
            float ll = Mathf.Sqrt(Lx * Lx + Ly * Ly + Lz * Lz);
            float lx = Lx / ll, ly = Ly / ll, lz = Lz / ll;
            // B lleva más margen alrededor (la brasa se ve también fuera del contorno): ZoomB = 1,3 en vez de 1,08.
            return RenderClay(size, ember ? ZoomB : Zoom, 0.05f, 0.06f, aa, (x, y) => RockSdf(x, y, shape), (ref Px p, float x, float y) =>
            {
                float d = RockSdf(x, y, shape);
                float inside = Cover(d, aa);
                const float e = 0.012f;
                float gx = (Height(x + e, y, shape) - Height(x - e, y, shape)) / (2f * e);
                float gy = (Height(x, y + e, shape) - Height(x, y - e, shape)) / (2f * e);
                float nx = -gx * 0.9f, ny = -gy * 0.9f, nz = 1f;
                float nl = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                nx /= nl; ny /= nl; nz /= nl;
                float diff = Mathf.Max(0f, nx * lx + ny * ly + nz * lz);
                float grain = (Grain(x * 1.3f + 7f, y * 1.3f) - 0.5f) * 0.16f;
                // oclusión: más oscuro abajo a la derecha y en el pie de la roca
                float edge = Mathf.Clamp01(1f - (-d) / 0.22f);
                float ao = 1f - 0.22f * edge * Mathf.Clamp01(0.5f - (nx * lx + ny * ly) * 0.8f);
                float k = (0.42f + 0.95f * diff) * ao + grain;
                var c = Mul(baseColor, k);
                c = Add(c, 0.28f * Mathf.Pow(diff, 14f));                               // brillo de la arcilla
                p.Over(c, inside);
                p.Over(WithAlpha(Tint(baseColor, 0.8f), 0.55f), inside * Mathf.Clamp01(edge * (ny * 0.7f - nx * 0.7f)) * 0.8f); // canto claro (arriba a la izquierda)

                if (ember)
                {
                    // brasa en el borde que va por delante
                    float ex = x - 0f, ey = y;
                    float gdx = RockSdf(x + e, y, shape) - RockSdf(x - e, y, shape);
                    float gdy = RockSdf(x, y + e, shape) - RockSdf(x, y - e, shape);
                    float gl = Mathf.Max(0.0001f, Mathf.Sqrt(gdx * gdx + gdy * gdy));
                    float facing = Mathf.Clamp01((gdx / gl) * EmberX + (gdy / gl) * EmberY);
                    facing = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(facing * 1.25f - 0.05f));
                    float rim = Mathf.Clamp01(1f - (-d) / 0.2f);
                    float heat = facing * rim * (0.75f + 0.5f * Grain(x * 2f, y * 2f));
                    var hot = Color.Lerp(Coral, Sun, Mathf.Clamp01(heat * 1.3f - 0.25f));
                    p.Over(WithAlpha(hot, 1f), inside * Mathf.Clamp01(heat * 1.25f));
                    // la brasa también se ve fuera del contorno (resplandor)
                    float outside = 1f - Cover(d - 0.012f, aa * 2f);
                    float edgeFade = Mathf.Clamp01((ZoomB * 0.96f - Mathf.Max(Mathf.Abs(x), Mathf.Abs(y))) / 0.14f); // se apaga antes del borde del sprite
                    float glow = Mathf.Exp(-Mathf.Max(0f, d) * 16f) * facing * 0.6f * edgeFade;
                    p.Over(WithAlpha(Color.Lerp(Orange, Coral, 0.4f), glow), (1f - inside) * outside);
                }
            });
        }

        // ------------------------------------------------------------------ estela de calor (B)

        /// <summary>Estela de calor con chispas: fila 0 = el extremo caliente (pegado a la roca); arriba se enfría y se
        /// apaga. Ancho w, alto h; se estira y se gira como la estela actual.</summary>
        public static Color32[] RenderHeatTrail(int w, int h)
        {
            var px = new Color32[w * h];
            const int sparks = 26;
            for (int py = 0; py < h; py++)
            {
                float t = (py + 0.5f) / h;
                for (int pxl = 0; pxl < w; pxl++)
                {
                    float u = ((pxl + 0.5f) / w) * 2f - 1f;
                    float half = Mathf.Pow(1f - t, 0.75f) * 0.85f + 0.03f;
                    float body = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((Mathf.Abs(u) - half * 0.55f) / (half * 0.55f + 0.001f)));
                    float inten = Mathf.Pow(1f - t, 1.5f);
                    var core = t < 0.3f ? Color.Lerp(Sun, Orange, t / 0.3f) : Color.Lerp(Orange, Coral, Mathf.Clamp01((t - 0.3f) / 0.4f));
                    core = Color.Lerp(core, Grape, Mathf.Clamp01((t - 0.55f) / 0.45f));
                    float wob = 0.85f + 0.3f * Grain(u * 1.5f + 3f, t * 4f);
                    var pxAcc = new Px();
                    pxAcc.Over(WithAlpha(core, 1f), Mathf.Clamp01(body * inten * 1.1f * wob));
                    // chispas: puntos brillantes que se alejan
                    for (int s = 0; s < sparks; s++)
                    {
                        float sy = Hash(s * 1.7f, 3.3f) * 0.92f;
                        float sx = (Hash(s * 2.9f, 8.1f) * 2f - 1f) * (0.15f + sy * 0.9f) * 0.9f;
                        float r = 0.018f + Hash(s * 4.3f, 1.1f) * 0.02f;
                        float dd = Mathf.Sqrt((u - sx) * (u - sx) + ((t - sy) * h / w * 2f) * ((t - sy) * h / w * 2f));
                        float a = Mathf.Clamp01(1f - dd / r) * (1f - sy);
                        if (a > 0f) pxAcc.Over(WithAlpha(Color.Lerp(Sun, Color.white, 0.5f), 1f), a);
                    }
                    px[py * w + pxl] = pxAcc.ToColor32();
                }
            }
            return px;
        }

        // ------------------------------------------------------------------ C: cristal

        private static readonly float[][] CrystalAngles =
        {
            new[] { 0.00f, 0.95f, 1.75f, 2.7f, 3.4f, 4.15f, 5.1f, 5.7f },
            new[] { 0.20f, 0.85f, 1.9f, 2.55f, 3.5f, 4.3f, 4.95f, 5.85f },
            new[] { 0.10f, 1.10f, 1.65f, 2.80f, 3.30f, 4.40f, 5.20f, 5.80f },
            new[] { 0.35f, 1.20f, 2.05f, 2.75f, 3.60f, 4.20f, 5.00f, 5.65f },
        };
        private static readonly float[][] CrystalRadius =
        {
            new[] { 1.00f, 0.88f, 1.02f, 0.86f, 1.00f, 0.90f, 1.04f, 0.90f },
            new[] { 0.92f, 1.04f, 0.88f, 1.00f, 0.90f, 1.02f, 0.86f, 1.00f },
            new[] { 1.02f, 0.90f, 1.00f, 0.86f, 1.04f, 0.92f, 1.00f, 0.88f },
            new[] { 0.90f, 1.00f, 0.88f, 1.04f, 0.92f, 0.98f, 1.02f, 0.88f },
        };

        private static float[] CrystalPoints(int shape, float scale)
        {
            var a = CrystalAngles[shape];
            var r = CrystalRadius[shape];
            var pts = new float[a.Length * 2];
            for (int i = 0; i < a.Length; i++)
            {
                pts[2 * i] = Mathf.Cos(a[i]) * 0.9f * r[i] * scale;
                pts[2 * i + 1] = Mathf.Sin(a[i]) * 0.6f * r[i] * scale;
            }
            return pts;
        }

        private static Color32[] RenderCrystal(int size, int shape, RockTint tint)
        {
            var outer = CrystalPoints(shape, 1f);
            var inner = CrystalPoints(shape, 0.5f);
            var ang = CrystalAngles[shape];
            int n = ang.Length;
            // el cristal mezcla el color elegido con hielo: lila y celeste, nunca un color plano
            var main = tint == RockTint.Sky ? Sky : tint == RockTint.Coral ? Pink : tint == RockTint.Gold ? Sun : Grape;
            var alt = tint == RockTint.Sky ? Grape : Sky;
            const float aa = 0.02f;
            float lightAng = Mathf.Atan2(Ly, Lx);
            return RenderClay(size, Zoom, 0.05f, 0.06f, aa, (x, y) => Polygon(x, y, outer), (ref Px p, float x, float y) =>
            {
                float body = Polygon(x, y, outer);
                float inside = Cover(body, aa);
                float th = Mathf.Atan2(y, x);
                if (th < 0f) th += 2f * Mathf.PI;
                int k = n - 1;
                for (int i = 0; i < n; i++) if (th >= ang[i]) k = i; // los ángulos van de menor a mayor: el último que no pasa de th
                float a0k = ang[k], a1k = k + 1 < n ? ang[k + 1] : ang[0] + 2f * Mathf.PI;
                float mid = (a0k + a1k) * 0.5f;
                float lit = 0.5f + 0.5f * Mathf.Cos(mid - lightAng);                       // cara iluminada: mira a la luz
                var facet = Color.Lerp(main, alt, 0.5f + 0.5f * Mathf.Sin(k * 1.9f + shape));
                facet = Mul(Tint(facet, 0.18f), 0.55f + 0.7f * lit);
                p.Over(facet, inside);

                // cristal interior: más claro, con sus propias facetas al revés (se ve profundidad)
                float ib = Polygon(x, y, inner);
                float litIn = 0.5f - 0.5f * Mathf.Cos(mid - lightAng);
                var ic = Mul(Tint(Color.Lerp(main, alt, 0.35f), 0.55f), 0.7f + 0.55f * litIn);
                p.Over(ic, Cover(ib, aa) * inside);
                p.Over(WithAlpha(Ink, 0.55f), Cover(Mathf.Abs(ib) - 0.012f, aa) * inside);

                // aristas desde el centro hacia cada vértice (líneas claras finas)
                float edges = 9f;
                for (int i = 0; i < n; i++) edges = Mathf.Min(edges, Capsule(x, y, inner[2 * i], inner[2 * i + 1], outer[2 * i], outer[2 * i + 1], 0.008f));
                p.Over(WithAlpha(Color.white, 0.55f), Cover(edges, aa) * inside);
                // brillo y destello
                p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Ellipse(x, y, -0.34f, 0.26f, 0.2f, 0.05f), aa) * inside);
                p.Over(Color.white, Cover(Star(x, y, -0.5f, 0.3f, 4, 0.13f, 3.4f, 0.01f), aa) * inside);
            });
        }
    }
}
