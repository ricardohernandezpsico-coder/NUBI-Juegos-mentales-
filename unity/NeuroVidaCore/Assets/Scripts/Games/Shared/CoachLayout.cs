using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>Cuánto ocupa un texto del globo a un ancho dado: líneas y alto (unidades del lienzo).</summary>
    public readonly struct TextMeasure
    {
        public readonly int Lines;
        public readonly float Height;
        public TextMeasure(int lines, float height) { Lines = lines; Height = height; }
    }

    /// <summary>
    /// Mide el texto del globo de Nubi con la misma fuente y tamaño que se dibuja (Fredoka Bold, <see cref="CoachLayout.FontSize"/>): sirve igual en el juego y en las pruebas, sin tener que dibujar nada.
    /// </summary>
    public static class CoachText
    {
        /// <summary>Escalas de lienzo con las que se mide: el teléfono dibuja el texto a 54 × la escala del lienzo (≈0,65 en una pantalla de 720 px de ancho, 1 en una de 1080 y 1,3 en una de 1440) y
        /// la fuente redondea cada letra a píxeles enteros, así que el mismo texto puede partirse en otra línea según la pantalla. Se queda con lo peor (más líneas, más alto) para que el globo
        /// nunca quede más chico que el texto (Ricardo, 6-oct: la segunda línea se salía del globo en su teléfono).</summary>
        public static readonly float[] Scales = { 0.65f, 0.8f, 1f, 1.25f };

        public static TextMeasure Measure(string text, float width, int fontSize = CoachLayout.FontSize)
        {
            if (string.IsNullOrEmpty(text)) return new TextMeasure(0, 0f);
            int lines = 0;
            float height = 0f;
            foreach (float scale in Scales)
            {
                var m = MeasureAt(text, width, fontSize, scale);
                if (m.Lines > lines) lines = m.Lines;
                if (m.Height > height) height = m.Height;
            }
            return new TextMeasure(lines, height);
        }

        /// <summary>Una sola medida, a una escala de lienzo dada (la altura vuelve en unidades del lienzo).</summary>
        public static TextMeasure MeasureAt(string text, float width, int fontSize, float scale)
        {
            if (string.IsNullOrEmpty(text)) return new TextMeasure(0, 0f);
            var gen = new TextGenerator();
            var settings = new TextGenerationSettings
            {
                font = UiFonts.Bold,
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                lineSpacing = 1f,
                richText = true,
                textAnchor = TextAnchor.MiddleCenter,
                alignByGeometry = false,
                scaleFactor = scale,
                color = Color.white,
                resizeTextForBestFit = false,
                updateBounds = false,
                horizontalOverflow = HorizontalWrapMode.Wrap,
                verticalOverflow = VerticalWrapMode.Overflow,
                generationExtents = new Vector2(Mathf.Max(1f, width), 0f),
                pivot = new Vector2(0.5f, 0.5f),
                generateOutOfBounds = false,
            };
            if (!gen.Populate(text, settings)) gen.Populate(text, settings);     // la primera vez la fuente puede estar armando su atlas
            return new TextMeasure(gen.lineCount, gen.GetPreferredHeight(text, settings) / Mathf.Max(0.01f, scale));
        }
    }

    /// <summary>Dónde quedó Nubi y su globo, y si de verdad no tapan nada.</summary>
    public sealed class CoachPlacement
    {
        public bool Left;                     // Nubi en el costado izquierdo (el globo hacia el centro)
        public float NubiSize;
        public Rect Nubi;
        public Rect Bubble;
        public int Lines;
        public float Overlap;                 // área tapada de lo que no se debía tapar (0 = limpio)
        public bool TooLong;                  // el texto no cabe en <see cref="CoachLayout.MaxLines"/> líneas ni en el globo más ancho
        public bool Clean => Overlap <= 0.5f && !TooLong;
    }

    /// <summary>
    /// La colocación de «Nubi entrenadora» (pura, con pruebas): busca dónde poner a Nubi y su globo para que NO tapen el hueco ni las zonas que hay que seguir viendo
    /// (la pregunta, la consigna, el rótulo del que habla el texto, el contador), ni ningún texto del juego, ni el dedo que insiste. Prueba los dos costados, varios
    /// anchos de globo (la altura sale de las líneas del texto: máximo <see cref="MaxLines"/>, la letra no se achica) y muchas alturas; gana la de cero solape que menos
    /// se aleja de lo esperado (abajo, del lado contrario al hueco, globo ancho). Si ninguna queda limpia gana la que menos tapa y el resultado no es <see cref="CoachPlacement.Clean"/>.
    /// </summary>
    public static class CoachLayout
    {
        public const int FontSize = 54;
        public const int MaxLines = 3;
        public const float Margin = 30f;
        public const float HudClear = 330f;       // lo que ocupan el marcador de arriba y el rótulo «Práctica: no cuenta»
        public const float BottomClear = 120f;    // margen de abajo para Nubi y el globo (los controles del tutorial son obstáculos aparte: NubiCoach.ControlSkip y ControlBadge)
        public const float PadX = 26f, PadY = 14f;
        public const float MinBubbleHeight = 112f;
        public const float Gap = 20f;
        public const float MinBubbleWidth = 330f;
        public const float MaxBubbleWidth = 720f;
        public const float Step = 10f;
        public static readonly float[] NubiSizes = { 300f, 240f, 180f, 150f };
        public static readonly float[] WidthFractions = { 1f, 0.84f, 0.7f, 0.58f, 0.48f };

        /// <summary>Las 4 esquinas del rect pedido a <paramref name="r"/> unidades más grande por cada lado.</summary>
        public static Rect Grow(Rect r, float by) => Rect.MinMaxRect(r.xMin - by, r.yMin - by, r.xMax + by, r.yMax + by);

        public static float Overlap(Rect a, Rect b)
        {
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return w > 0f && h > 0f ? w * h : 0f;
        }

        private static float Total(Rect r, IList<Rect> list, float weight)
        {
            float t = 0f;
            for (int i = 0; i < list.Count; i++) t += Overlap(r, list[i]);
            return t * weight;
        }

        /// <param name="screen">El área segura del juego (en el espacio del foco, centrado).</param>
        /// <param name="holeCenter">El centro del hueco (para poner a Nubi del lado contrario) o null si no hay hueco (aviso).</param>
        /// <param name="hard">Lo que NUNCA se debe tapar: el hueco, las zonas protegidas y el dedo (ya con su margen).</param>
        /// <param name="soft">Los textos del juego a la vista (con su margen): tampoco se tapan.</param>
        /// <param name="prefer">Lo que conviene no tapar pero no es un error taparlo (el dedo de ayuda, que solo aparece pasados 5 s y se acomoda del lado donde hay lugar): pesa poco en el costo y no cuenta como solape.</param>
        public static CoachPlacement Place(Rect screen, Vector2? holeCenter, IList<Rect> hard, IList<Rect> soft, string text, Func<string, float, TextMeasure> measure = null, IList<Rect> prefer = null)
        {
            measure = measure ?? ((s, w) => CoachText.Measure(s, w));
            CoachPlacement best = null;
            float bestCost = float.MaxValue;
            foreach (float nubi in NubiSizes)
            {
                float wMax = Mathf.Min(screen.width - nubi - 3f * Margin, MaxBubbleWidth);
                foreach (float frac in WidthFractions)
                {
                    float bw = wMax * frac;
                    if (bw < MinBubbleWidth && frac < 1f) continue;
                    var m = measure(text, bw - 2f * PadX);
                    bool tooLong = m.Lines > MaxLines;
                    float bh = Mathf.Max(MinBubbleHeight, m.Height + 2f * PadY);
                    float gh = Mathf.Max(nubi, bh);
                    float gw = nubi + Gap + bw;
                    float yLo = screen.yMin + BottomClear + gh / 2f;
                    float yHi = screen.yMax - HudClear - gh / 2f;
                    if (yHi < yLo) yLo = yHi = (yLo + yHi) / 2f;
                    for (int side = 0; side < 2; side++)
                    {
                        bool left = side == 0;
                        float gx = left ? screen.xMin + Margin : screen.xMax - Margin - gw;
                        float nx = left ? gx : gx + gw - nubi;
                        float bx = left ? gx + nubi + Gap : gx;
                        for (float cy = yLo; cy <= yHi + 0.01f; cy += Step)
                        {
                            var nr = new Rect(nx, cy - nubi / 2f, nubi, nubi);
                            var br = new Rect(bx, cy - bh / 2f, bw, bh);
                            float ov = Total(nr, hard, 3f) + Total(br, hard, 3f) + Total(nr, soft, 1f) + Total(br, soft, 1f);
                            float cost = ov > 0f ? 1e6f + ov : 0f;
                            if (prefer != null) cost += (Total(nr, prefer, 1f) + Total(br, prefer, 1f)) * 0.02f;
                            if (tooLong) cost += 5e5f;
                            cost += m.Lines * 18f + (wMax - bw) * 0.12f + (nubi < 300f ? (nubi < 240f ? (nubi < 180f ? 140f : 90f) : 40f) : 0f);
                            // lo esperado: abajo mejor que arriba, arriba mejor que al medio; Nubi del lado contrario al hueco
                            float fromBottom = cy - yLo, fromTop = yHi - cy;
                            cost += Mathf.Min(fromBottom * 0.04f, fromTop * 0.04f + 15f);
                            if (holeCenter.HasValue && (holeCenter.Value.x > screen.center.x) != left) cost += 45f;
                            if (cost < bestCost)
                            {
                                bestCost = cost;
                                best = new CoachPlacement { Left = left, NubiSize = nubi, Nubi = nr, Bubble = br, Lines = m.Lines, Overlap = ov, TooLong = tooLong };
                            }
                        }
                    }
                }
            }
            return best;
        }

        /// <summary>El área <paramref name="screen"/> menos las zonas iluminadas (en rectángulos que no se pisan): lo que cubre el velo gris.</summary>
        public static List<Rect> Subtract(Rect screen, IList<Rect> lit)
        {
            var cut = new List<Rect>();
            foreach (var r in lit)
            {
                var c = Rect.MinMaxRect(Mathf.Max(r.xMin, screen.xMin), Mathf.Max(r.yMin, screen.yMin), Mathf.Min(r.xMax, screen.xMax), Mathf.Min(r.yMax, screen.yMax));
                if (c.width > 0.01f && c.height > 0.01f) cut.Add(c);
            }
            var ys = new List<float> { screen.yMin, screen.yMax };
            foreach (var c in cut) { ys.Add(c.yMin); ys.Add(c.yMax); }
            ys.Sort();
            var result = new List<Rect>();
            for (int i = 0; i + 1 < ys.Count; i++)
            {
                float y0 = ys[i], y1 = ys[i + 1];
                if (y1 - y0 < 0.01f) continue;
                var spans = new List<Vector2>();
                foreach (var c in cut) if (c.yMin <= y0 + 0.01f && c.yMax >= y1 - 0.01f) spans.Add(new Vector2(c.xMin, c.xMax));
                spans.Sort((a, b) => a.x.CompareTo(b.x));
                float x = screen.xMin;
                foreach (var s in spans)
                {
                    if (s.x > x + 0.01f) AddSlab(result, x, y0, s.x, y1);
                    x = Mathf.Max(x, s.y);
                }
                if (screen.xMax > x + 0.01f) AddSlab(result, x, y0, screen.xMax, y1);
            }
            return result;
        }

        // une la franja nueva con la de justo debajo si tiene el mismo ancho (menos piezas)
        private static void AddSlab(List<Rect> list, float x0, float y0, float x1, float y1)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var r = list[i];
                if (Mathf.Abs(r.yMax - y0) < 0.01f && Mathf.Abs(r.xMin - x0) < 0.01f && Mathf.Abs(r.xMax - x1) < 0.01f)
                {
                    list[i] = Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, y1);
                    return;
                }
            }
            list.Add(Rect.MinMaxRect(x0, y0, x1, y1));
        }
    }

    /// <summary>
    /// Lo que quedó registrado de un paso del tutorial (para las pruebas, el smoke y las láminas de revisión): las zonas, dónde cayeron Nubi y el globo y cuánto tapan.
    /// </summary>
    [Serializable]
    public sealed class CoachStepReport
    {
        public string Game;
        public int Index;
        public string Kind;
        public string Text;
        public Rect Screen;
        public bool HasHole;
        public Rect Hole;
        public bool Circle;
        public Rect[] Keep = new Rect[0];
        public Rect[] GameText = new Rect[0];
        public Rect Finger;
        public Rect Nubi;
        public Rect Bubble;
        public int Lines;
        public int ActualLines;
        public int FontSize;
        public float Overlap;
        public bool TooLong;
        public bool Clean;
        public Rect[] Controls = new Rect[0];     // «Saltar tutorial» y «Práctica: no cuenta» (los que estaban a la vista en este paso)
        public float ControlClash;     // área que los controles del tutorial («Saltar tutorial», el rótulo) pisan de lo iluminado o de los textos del juego
    }
}
