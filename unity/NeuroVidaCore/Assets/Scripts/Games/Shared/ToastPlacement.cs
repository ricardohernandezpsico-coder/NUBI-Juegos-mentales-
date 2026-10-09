using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Dónde puede ir un aviso (<see cref="Toast"/>) sin tapar el juego (Tarea 55, 9-oct): lo que NO se tapa nunca es todo texto a la vista, todo lo que se toca, el estímulo del momento (las imágenes grandes con dibujo: la pieza, la señal, la nave, la palabra) y las filas propias del
    /// juego debajo del marcador (el texto con la caja de su fila: el medidor, «Tu estación»). El marcador de arriba tampoco: el aviso nunca sube más arriba de su borde de abajo. El aviso mide el espacio libre cuando se muestra y se pone en la primera franja que no choca con nada; si no hay ninguna,
    /// en la de menos choque. La guardia del smoke usa estas mismas reglas para comprobar que cada juego tiene su franja libre en cada forma de pantalla.
    /// </summary>
    public static class ToastPlacement
    {
        /// <summary>Si alguno de los antepasados de un elemento se llama así, es ambiente (cielo, estrellas, fondo, efectos) y un aviso puede pasar por encima.</summary>
        private static readonly string[] AmbientAncestors =
        {
            "Background", "Backdrop", "Starfield", "Stars", "Sky", "Nebula", "Aurora", "Ambient", "Fx", "Vignette", "Toast", "Hud", "PauseMenu", "NubiCoach", "CountdownScreen", "FinishCurtain", "Scrim", "Veil", "PhasePill",
        };

        /// <summary>Elementos sueltos que son adorno aunque tengan dibujo (brillos, sombras, estelas, destellos).</summary>
        private static readonly string[] AmbientNames =
        {
            "Glow", "Halo", "Shadow", "Rim", "Trail", "Spark", "Star", "Bokeh", "Dim", "Flash", "Aura", "Fog", "Wave", "Ripple", "Burst", "Sheen", "Highlight",
        };

        public const float MinStimulusUnits = 80f;

        /// <summary>
        /// Las cajas protegidas, en el espacio local de <paramref name="space"/> y con la «y» medida DESDE ARRIBA hacia abajo (como se coloca el aviso). <paramref name="skip"/>: objetos (y lo que cuelga de ellos) que no cuentan (el propio aviso).
        /// </summary>
        public static List<Rect> Collect(RectTransform space, Transform skip = null, List<string> names = null)
        {
            var list = new List<Rect>();
            var canvas = space.GetComponentInParent<Canvas>();
            if (canvas == null) return list;
            canvas = canvas.rootCanvas;
            var corners = new Vector3[4];
            float screenW = space.rect.width, screenH = space.rect.height;
            float screenArea = Mathf.Max(1f, screenW * screenH);
            float top = space.rect.yMax;

            foreach (var g in canvas.GetComponentsInChildren<Graphic>(false))
            {
                if (g == null || !g.isActiveAndEnabled || g.canvasRenderer == null || g.canvasRenderer.cull) continue;
                if (skip != null && g.transform.IsChildOf(skip)) continue;
                float alpha = g.color.a * g.canvasRenderer.GetInheritedAlpha();
                if (alpha < 0.3f) continue;
                if (IsAmbient(g.transform)) continue;

                // todo se mide en el espacio LOCAL del contenedor (unidades del lienzo): en el teléfono el lienzo va escalado y el mundo está en píxeles
                Rect local;
                var text = g as Text;
                if (text != null)
                {
                    if (string.IsNullOrWhiteSpace(text.text)) continue;
                    local = ToLocal(space, top, DrawnTextRect(text));
                    // el texto de una FILA (el medidor, «Tu estación», la tarjeta de la definición): se protege toda la caja de su fila si es una pieza (no medio lienzo)
                    var row = text.transform.parent as RectTransform;
                    if (row != null && row.GetComponent<Canvas>() == null)
                    {
                        row.GetWorldCorners(corners);
                        var rl = ToLocal(space, top, Bounds(corners));
                        bool piece = rl.width * rl.height < 0.22f * screenArea && rl.height < 650f;
                        if (piece) local = Union(local, rl);
                    }
                }
                else
                {
                    g.rectTransform.GetWorldCorners(corners);
                    local = ToLocal(space, top, Bounds(corners));
                    float w = local.width, h = local.height;
                    if (g.raycastTarget)
                    {
                        // lo que se toca (botones, fichas, luces); un fondo o un velo de pantalla casi completa no
                        if (w > 0.9f * screenW && h > 0.5f * screenH) continue;
                        if (w < 40f || h < 40f) continue;
                    }
                    else
                    {
                        // el estímulo: una imagen con dibujo, ni chica ni de pantalla completa, sin ser adorno
                        var image = g as Image;
                        if (image == null || image.sprite == null || alpha < 0.45f) continue;
                        if (w < MinStimulusUnits || h < MinStimulusUnits || w > 0.97f * screenW || h > 0.6f * screenH) continue;
                        if (IsAmbientName(g.name)) continue;
                    }
                }
                list.Add(local);
                if (names != null) names.Add(PathOf(g.transform));
            }
            return list;
        }

        /// <summary>Cuánto se pisan dos cajas (unidades cuadradas).</summary>
        public static float Overlap(Rect a, Rect b)
        {
            float ox = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), oy = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return ox > 0f && oy > 0f ? ox * oy : 0f;
        }

        /// <summary>La caja del aviso en el espacio local de <paramref name="space"/> con la «y» desde arriba (para compararla con <see cref="Collect"/>).</summary>
        public static Rect LocalRectOf(RectTransform space, RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return ToLocal(space, space.rect.yMax, Bounds(corners));
        }

        private static string PathOf(Transform t)
        {
            var sb = new System.Text.StringBuilder();
            int n = 0;
            for (var c = t; c != null && n < 5; c = c.parent, n++) sb.Insert(0, c.name + (sb.Length > 0 ? "/" : ""));
            return sb.ToString();
        }

        private static float ScreenWorldArea(Canvas canvas)
        {
            var rt = (RectTransform)canvas.transform;
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            var b = Bounds(c);
            return Mathf.Max(1f, b.width * b.height);
        }

        private static bool IsAmbient(Transform t)
        {
            for (var c = t; c != null; c = c.parent)
                foreach (var n in AmbientAncestors)
                    if (c.name == n || c.name.StartsWith(n)) return true;
            return false;
        }

        private static bool IsAmbientName(string name)
        {
            foreach (var n in AmbientNames) if (name.Contains(n)) return true;
            return false;
        }

        private static Rect Bounds(Vector3[] c)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var p in c) { x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y); }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        private static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

        private static Rect ToLocal(RectTransform space, float top, Rect world)
        {
            var a = space.InverseTransformPoint(new Vector3(world.xMin, world.yMin, 0f));
            var b = space.InverseTransformPoint(new Vector3(world.xMax, world.yMax, 0f));
            float x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x), y0 = Mathf.Min(a.y, b.y), y1 = Mathf.Max(a.y, b.y);
            return Rect.MinMaxRect(x0, top - y1, x1, top - y0);
        }

        /// <summary>Lo que el texto DIBUJA (no toda su caja): el ancho y el alto reales del texto acomodados según su alineación y sin pasar de su caja si se parte en líneas.</summary>
        public static Rect DrawnTextRect(Text t)
        {
            var corners = new Vector3[4];
            t.rectTransform.GetWorldCorners(corners);
            var rect = Bounds(corners);
            var sc = t.transform.lossyScale;
            float w = t.preferredWidth * Mathf.Abs(sc.x), h = t.preferredHeight * Mathf.Abs(sc.y);
            if (t.horizontalOverflow == HorizontalWrapMode.Wrap) w = Mathf.Min(w, rect.width);
            if (t.verticalOverflow == VerticalWrapMode.Truncate) h = Mathf.Min(h, rect.height);
            if (w <= 0f || h <= 0f) return rect;
            float x0, y0;
            switch (t.alignment)
            {
                case TextAnchor.UpperLeft: case TextAnchor.MiddleLeft: case TextAnchor.LowerLeft: x0 = rect.xMin; break;
                case TextAnchor.UpperRight: case TextAnchor.MiddleRight: case TextAnchor.LowerRight: x0 = rect.xMax - w; break;
                default: x0 = rect.center.x - w / 2f; break;
            }
            switch (t.alignment)
            {
                case TextAnchor.UpperLeft: case TextAnchor.UpperCenter: case TextAnchor.UpperRight: y0 = rect.yMax - h; break;
                case TextAnchor.LowerLeft: case TextAnchor.LowerCenter: case TextAnchor.LowerRight: y0 = rect.yMin; break;
                default: y0 = rect.center.y - h / 2f; break;
            }
            return new Rect(x0, y0, w, h);
        }
    }
}
