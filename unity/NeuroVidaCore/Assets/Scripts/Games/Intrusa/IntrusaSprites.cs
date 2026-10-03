using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Intrusa
{
    /// <summary>
    /// El grabado de una lámina del atlas, rasterizado UNA vez por ronda (no se dibuja cuadro a cuadro): dos texturas del mismo tamaño,
    /// una con el contorno dorado (se revela de a poco con un relleno radial) y otra con el sombreado de líneas diagonales, recortado
    /// al contorno, más los detalles (ojo, punto, línea). Dibujo 100% propio de las figuras de tools/intrusa; sin láminas históricas.
    /// </summary>
    public sealed class Engraving
    {
        public Texture2D Outline, Shade;
        /// <summary>Centro y tamaño de las texturas en unidades lógicas (dp de una pantalla de 360 de ancho).</summary>
        public Vector2 Center, Size;

        private Sprite _outlineSprite, _shadeSprite;

        public Sprite OutlineSprite() => _outlineSprite != null ? _outlineSprite : (_outlineSprite = Make(Outline));
        public Sprite ShadeSprite() => _shadeSprite != null ? _shadeSprite : (_shadeSprite = Make(Shade));

        private static Sprite Make(Texture2D t) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);

        /// <summary>Libera las texturas y los sprites de la ronda (se hacen de nuevo en cada ronda).</summary>
        public void Release()
        {
            if (_outlineSprite != null) UnityEngine.Object.Destroy(_outlineSprite);
            if (_shadeSprite != null) UnityEngine.Object.Destroy(_shadeSprite);
            if (Outline != null) UnityEngine.Object.Destroy(Outline);
            if (Shade != null) UnityEngine.Object.Destroy(Shade);
            Outline = Shade = null;
            _outlineSprite = _shadeSprite = null;
        }
    }

    public static class IntrusaSprites
    {
        /// <summary>Oro del grabado (#E9C77B) y su alfa de contorno, grosor y sombreado, como en el boceto del Atlas celeste.</summary>
        public static readonly Color Gold = new Color(233f / 255f, 199f / 255f, 123f / 255f, 1f);
        public const float OutlineAlpha = 0.55f, OutlineWidth = 1.2f, HatchAlpha = 0.13f, HatchSpacing = 6f;
        public const float PxPerUnit = 1.5f;

        private static Sprite _line, _flare;

        /// <summary>Barra horizontal de borde suave (perfil de alfa a lo ancho): una línea de luz se arma estirándola y girándola.</summary>
        public static Sprite Line()
        {
            if (_line != null) return _line;
            const int w = 8, h = 32;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                float a = Mathf.Clamp01(1f - v * v * v);      // casi plano al centro, se suaviza hacia el borde
                for (int x = 0; x < w; x++) px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply();
            _line = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            return _line;
        }

        /// <summary>Destello de cuatro puntas para las estrellas-palabra (más fino que SparkleSprite: casi una cruz de luz).</summary>
        public static Sprite Flare()
        {
            if (_flare != null) return _flare;
            const int n = 96;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float fx = ((x + 0.5f) / n * 2f - 1f), fy = ((y + 0.5f) / n * 2f - 1f);
                    float ax = Mathf.Abs(fx), ay = Mathf.Abs(fy);
                    float cross = Mathf.Max(Mathf.Exp(-ax * 22f) * Mathf.Clamp01(1f - ay), Mathf.Exp(-ay * 22f) * Mathf.Clamp01(1f - ax));
                    float core = Mathf.Exp(-(fx * fx + fy * fy) * 40f);
                    float a = Mathf.Clamp01(cross * 0.85f + core);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply();
            _flare = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return _flare;
        }

        // ------------------------------------------------------------------ el grabado

        /// <summary>Rasteriza el grabado de [f] con el reflejo y el giro de esta ronda. Pensado para correr en un solo cuadro (unos ms);
        /// <see cref="BakeCoroutine"/> lo reparte entre cuadros si hace falta.</summary>
        public static Engraving Bake(FigureShape f, bool mirror, float rotDeg)
        {
            var e = new Engraving();
            var it = BakeCoroutine(f, mirror, rotDeg, e, 1 << 20);
            while (it.MoveNext()) { }
            return e;
        }

        public static IEnumerator<object> BakeCoroutine(FigureShape f, bool mirror, float rotDeg, Engraving result, int rowsPerFrame = 120)
        {
            // 1) trazos en pantalla lógica
            var polys = new List<List<Vector2>>();
            if (f.contorno != null)
                foreach (var c in f.contorno)
                {
                    if (c == null) continue;
                    if (c.suave && c.puntos != null && c.puntos.Length >= 6) polys.Add(Smooth(c.puntos, mirror, rotDeg));
                    else if (c.elipse != null && c.elipse.Length == 4) polys.Add(EllipsePoly(c.elipse, mirror, rotDeg));
                }
            var details = new List<FigureDetail>();
            if (f.detalles != null) foreach (var d in f.detalles) if (d != null && d.v != null) details.Add(d);

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in polys) foreach (var q in p) { minX = Mathf.Min(minX, q.x); maxX = Mathf.Max(maxX, q.x); minY = Mathf.Min(minY, q.y); maxY = Mathf.Max(maxY, q.y); }
            if (polys.Count == 0) { minX = minY = IntrusaLayout.BoxX; maxX = IntrusaLayout.BoxX + 10f; maxY = IntrusaLayout.BoxY + 10f; }
            const float margin = 8f;
            minX -= margin; minY -= margin; maxX += margin; maxY += margin;
            int w = Mathf.Max(8, Mathf.CeilToInt((maxX - minX) * PxPerUnit)), h = Mathf.Max(8, Mathf.CeilToInt((maxY - minY) * PxPerUnit));
            result.Center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            result.Size = new Vector2(maxX - minX, maxY - minY);

            // al espacio de píxeles (fila 0 = abajo, así que se invierte y)
            Func<Vector2, Vector2> toPx = q => new Vector2((q.x - minX) * PxPerUnit, (maxY - q.y) * PxPerUnit);
            var pxPolys = new List<List<Vector2>>();
            foreach (var p in polys)
            {
                var l = new List<Vector2>(p.Count);
                foreach (var q in p) l.Add(toPx(q));
                pxPolys.Add(l);
            }

            // 2) contorno: cápsulas de [OutlineWidth] dp, el alfa más alto de cada píxel
            var outline = new float[w * h];
            float hw = OutlineWidth * PxPerUnit * 0.5f;
            foreach (var poly in pxPolys)
                for (int i = 0; i < poly.Count; i++) Stroke(outline, w, h, poly[i], poly[(i + 1) % poly.Count], hw, 1f);
            yield return null;

            // 3) sombreado: líneas diagonales cada [HatchSpacing] dp, solo adentro de algún contorno (barrido por filas)
            var shade = new float[w * h];
            float spacing = HatchSpacing * PxPerUnit * Mathf.Sqrt(2f);
            var xs = new List<float>();
            for (int y = 0; y < h; y++)
            {
                float cy = y + 0.5f;
                foreach (var poly in pxPolys)
                {
                    xs.Clear();
                    for (int i = 0; i < poly.Count; i++)
                    {
                        var a = poly[i]; var b = poly[(i + 1) % poly.Count];
                        if ((a.y <= cy && b.y > cy) || (b.y <= cy && a.y > cy))
                            xs.Add(a.x + (cy - a.y) / (b.y - a.y) * (b.x - a.x));
                    }
                    xs.Sort();
                    for (int k = 0; k + 1 < xs.Count; k += 2)
                    {
                        int x0 = Mathf.Max(0, Mathf.CeilToInt(xs[k] - 0.5f)), x1 = Mathf.Min(w - 1, Mathf.FloorToInt(xs[k + 1] - 0.5f));
                        for (int x = x0; x <= x1; x++)
                        {
                            float t = Mathf.Repeat(x + y, spacing);
                            float d = Mathf.Min(t, spacing - t) / 1.4142f;
                            float cov = Mathf.Clamp01(0.95f - d);
                            if (cov > shade[y * w + x]) shade[y * w + x] = cov;
                        }
                    }
                }
                if (y % rowsPerFrame == rowsPerFrame - 1) yield return null;
            }

            // 4) detalles, en la misma textura del sombreado (se funden juntos)
            var det = new float[w * h];
            foreach (var d in details)
            {
                if (d.tipo == "ojo" && d.v.Length >= 2)
                {
                    var c = toPx(IntrusaLayout.ToScreen(new Vec2(d.v[0], d.v[1]), mirror, rotDeg).ToV2());
                    Disc(det, w, h, c, 2.3f * PxPerUnit, 0f, 1f);
                }
                else if (d.tipo == "punto" && d.v.Length >= 2)
                {
                    var c = toPx(IntrusaLayout.ToScreen(new Vec2(d.v[0], d.v[1]), mirror, rotDeg).ToV2());
                    Disc(det, w, h, c, 3.4f * PxPerUnit, 1.1f * PxPerUnit, 0.9f);
                }
                else if (d.tipo == "linea" && d.v.Length >= 4)
                {
                    var a = toPx(IntrusaLayout.ToScreen(new Vec2(d.v[0], d.v[1]), mirror, rotDeg).ToV2());
                    var b = toPx(IntrusaLayout.ToScreen(new Vec2(d.v[2], d.v[3]), mirror, rotDeg).ToV2());
                    Stroke(det, w, h, a, b, OutlineWidth * PxPerUnit * 0.5f, 0.8f);
                }
            }
            yield return null;

            result.Outline = ToTexture(outline, null, w, h, OutlineAlpha, 0f);
            result.Shade = ToTexture(shade, det, w, h, HatchAlpha, 0.8f);
        }

        private static Texture2D ToTexture(float[] a, float[] extra, int w, int h, float alpha, float extraAlpha)
        {
            var px = new Color32[w * h];
            byte r = (byte)(Gold.r * 255f), g = (byte)(Gold.g * 255f), b = (byte)(Gold.b * 255f);
            for (int i = 0; i < px.Length; i++)
            {
                float v = a[i] * alpha;
                if (extra != null) v = Mathf.Max(v, extra[i] * extraAlpha);
                px[i] = new Color32(r, g, b, (byte)(Mathf.Clamp01(v) * 255f));
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ trazos

        private static void Stroke(float[] buf, int w, int h, Vector2 a, Vector2 b, float halfWidth, float strength)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - halfWidth - 2f)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + halfWidth + 2f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - halfWidth - 2f)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + halfWidth + 2f));
            Vector2 ab = b - a;
            float len2 = Mathf.Max(1e-6f, ab.sqrMagnitude);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float d = (p - (a + ab * t)).magnitude;
                    float cov = Mathf.Clamp01(halfWidth + 0.75f - d) * strength;
                    int i = y * w + x;
                    if (cov > buf[i]) buf[i] = cov;
                }
        }

        private static void Disc(float[] buf, int w, int h, Vector2 c, float radius, float ringWidth, float strength)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(c.x - radius - 2f)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(c.x + radius + 2f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(c.y - radius - 2f)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(c.y + radius + 2f));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - c).magnitude;
                    float cov = ringWidth <= 0f ? Mathf.Clamp01(radius + 0.75f - d) : Mathf.Clamp01(ringWidth * 0.5f + 0.75f - Mathf.Abs(d - radius));
                    int i = y * w + x;
                    cov *= strength;
                    if (cov > buf[i]) buf[i] = cov;
                }
        }

        // ------------------------------------------------------------------ contornos

        /// <summary>Contorno cerrado suave (Catmull-Rom por los puntos, 8 pasos por tramo) en pantalla lógica.</summary>
        public static List<Vector2> Smooth(float[] flat, bool mirror, float rotDeg)
        {
            int n = flat.Length / 2;
            var pts = new Vector2[n];
            for (int i = 0; i < n; i++) pts[i] = IntrusaLayout.ToScreen(new Vec2(flat[i * 2], flat[i * 2 + 1]), mirror, rotDeg).ToV2();
            var res = new List<Vector2>(n * 8);
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = pts[(i - 1 + n) % n], p1 = pts[i], p2 = pts[(i + 1) % n], p3 = pts[(i + 2) % n];
                for (int k = 0; k < 8; k++)
                {
                    float t = k / 8f, t2 = t * t, t3 = t2 * t;
                    res.Add(0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            return res;
        }

        public static List<Vector2> EllipsePoly(float[] e, bool mirror, float rotDeg)
        {
            const int n = 48;
            var res = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                res.Add(IntrusaLayout.ToScreen(new Vec2(e[0] + e[2] * Mathf.Cos(a), e[1] + e[3] * Mathf.Sin(a)), mirror, rotDeg).ToV2());
            }
            return res;
        }

        private static Vector2 ToV2(this Vec2 v) => new Vector2(v.X, v.Y);
    }
}
