using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Las letras listas ANTES del primer ensayo (Tarea 190d, 29-oct). El texto de Unity (<c>Text</c>) dibuja con una fuente dinámica: la primera vez que se muestra una letra en un tamaño, Unity la rasteriza y, si el atlas se llena, lo agranda y lo reconstruye
    /// (<c>Font.textureRebuilt</c>), todo en el hilo principal y en el cuadro en que la letra aparece. La sonda del teléfono midió en Tinta o Palabra un cuadro de 83 ms con 4 reconstrucciones justo al mostrar el primer ensayo (las palabras de 190 unidades).
    /// <para>Aquí se rasterizan esas letras DURANTE la carga, detrás de la pantalla de la app: <see cref="Plan"/> junta, por fuente y tamaño en pixeles, las letras de lo que el tablero ya muestra (cada <c>Text</c> con su texto), todo el alfabeto en el renglón de avisos del marcador
    /// (cualquier aviso de cualquier juego usa ese tamaño: «¡Un paso más!», los errores…) y lo que cada juego declara en <c>GameControllerBase.GlyphExtras</c> (las palabras y colores de sus ensayos, que todavía no se muestran). <see cref="Run"/> las pide con un presupuesto por cuadro.</para>
    /// <para>Lo que NO se puede listar de antemano (el texto viene de datos): las palabras de Lluvia de meteoros y de En la punta de la lengua, las frases de ¿Verdad o disparate? y los nombres de La estrella intrusa. Sus letras se rasterizan al primer uso, como antes.</para>
    /// </summary>
    public static class GlyphWarm
    {
        /// <summary>Las letras, cifras y signos con que se escribe en español en la app: lo que puede decir un aviso del marcador.</summary>
        public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz\u00c1\u00c9\u00cd\u00d3\u00da\u00dc\u00d1\u00e1\u00e9\u00ed\u00f3\u00fa\u00fc\u00f1\u00a1!\u00bf?0123456789.,:;\u00b7-+%()\u00ab\u00bb/";

        /// <summary>Lo que puede gastar por cuadro mientras la pantalla de carga tapa el juego.</summary>
        public const float BudgetMs = 4f;

        /// <summary>Un grupo: una fuente, un estilo y un tamaño en pixeles (el de verdad: tamaño por escala del lienzo) con las letras que hay que tener rasterizadas en él.</summary>
        public sealed class Group
        {
            public Font Font;
            public int Pixels;
            public FontStyle Style;
            public Text Sample;
            public int Size;                           // el tamaño de la letra en unidades del lienzo (el del texto de muestra, o el que pide el juego)
            public readonly SortedSet<char> Chars = new SortedSet<char>();
            public string Letters()
            {
                var sb = new StringBuilder(Chars.Count);
                foreach (var c in Chars) sb.Append(c);
                return sb.ToString();
            }
        }

        private static readonly TextGenerator Generator = new TextGenerator(256);

        /// <summary>Cuántas veces pidió letras la última vez (para las pruebas y la sonda).</summary>
        public static int LastGroupCount { get; private set; }

        /// <summary>El tamaño en pixeles con que Unity rasteriza este texto: su tamaño (o el que dejó el ajuste automático) por la escala del lienzo.</summary>
        public static int PixelSize(Text t, int fontSize = 0)
        {
            if (t == null) return 0;
            int size = fontSize > 0 ? fontSize : UnitSize(t);
            return Mathf.RoundToInt(size * Mathf.Max(0.0001f, t.pixelsPerUnit));
        }

        /// <summary>El tamaño del texto en unidades del lienzo: el que dejó el ajuste automático, o su tamaño.</summary>
        private static int UnitSize(Text t) => t.resizeTextForBestFit && t.cachedTextGenerator.fontSizeUsedForBestFit > 0 ? t.cachedTextGenerator.fontSizeUsedForBestFit : t.fontSize;

        /// <summary>Los renglones donde puede salir cualquier mensaje: la consigna y los chips del marcador (<c>GameHud</c>: «Hud/Prompt/Text», «InfoChip», «StreakChip») y la píldora de mensajes de los juegos (<c>PhasePill</c>: «¡Bien!», las explicaciones).</summary>
        public static bool CarriesAnyMessage(Text t)
        {
            if (t == null || t.name != "Text" || t.transform.parent == null) return false;
            string parent = t.transform.parent.name;
            return parent == "Prompt" || parent == "PhasePill" || parent == "InfoChip" || parent == "StreakChip";
        }

        /// <summary>Las letras que hay que tener listas, por fuente y tamaño. Puro: no pide nada.</summary>
        public static List<Group> Plan(IEnumerable<Text> texts, IEnumerable<(Text sample, string chars, int fontSize)> extras)
        {
            var groups = new Dictionary<(Font, int, FontStyle), Group>();
            void Add(Text t, string chars, int fontSize = 0)
            {
                if (t == null || t.font == null || string.IsNullOrEmpty(chars)) return;
                int px = PixelSize(t, fontSize);
                if (px <= 0) return;
                var key = (t.font, px, t.fontStyle);
                if (!groups.TryGetValue(key, out var g)) groups[key] = g = new Group { Font = t.font, Pixels = px, Style = t.fontStyle, Sample = t, Size = fontSize > 0 ? fontSize : UnitSize(t) };
                foreach (char c in chars) if (c > ' ' && c != '<' && c != '>') g.Chars.Add(c);                  // sin espacios, saltos de línea ni las marcas de color
            }
            if (texts != null)
                foreach (var t in texts)
                {
                    if (t == null) continue;
                    Add(t, t.text);
                    if (CarriesAnyMessage(t)) Add(t, Alphabet);
                }
            if (extras != null) foreach (var (t, chars, size) in extras) Add(t, chars, size);
            return new List<Group>(groups.Values);
        }

        /// <summary>Rasteriza las letras del grupo con el mismo camino que usa el <c>Text</c> al dibujar (un <c>TextGenerator</c> propio: no toca los del texto).</summary>
        private static void Request(Group g)
        {
            var s = g.Sample.GetGenerationSettings(new Vector2(100000f, 100000f));
            s.resizeTextForBestFit = false;
            s.fontSize = g.Size;
            s.horizontalOverflow = HorizontalWrapMode.Overflow;
            s.verticalOverflow = VerticalWrapMode.Overflow;
            s.richText = false;
            Generator.Populate(g.Letters(), s);
        }

        /// <summary>
        /// Un texto que el juego va a poner en <paramref name="sample"/> (con su caja y su ajuste automático): se genera TAL CUAL, con la misma búsqueda de tamaño que hará el texto de verdad, así que rasteriza los mismos tamaños que visitará
        /// (la palabra de Tinta o Palabra se ajusta entre 80 y 190 unidades, y cada palabra visita tamaños distintos). <paramref name="fontSize"/> &gt; 0 = el tamaño que el juego le pondrá.
        /// </summary>
        private static void RequestWord(Text sample, string text, int fontSize)
        {
            var s = sample.GetGenerationSettings(sample.rectTransform.rect.size);
            if (fontSize > 0) { s.fontSize = fontSize; s.resizeTextMaxSize = fontSize; }
            s.richText = false;
            Generator.Populate(text, s);
        }

        /// <summary>Pide las letras, de a un grupo y con presupuesto por cuadro. Lo gastado va a la sonda de entrada como «letras».</summary>
        public static IEnumerator Run(IEnumerable<Text> texts, IEnumerable<(Text sample, string chars, int fontSize)> extras, IEnumerable<(Text sample, string text, int fontSize)> words = null)
        {
#if UNITY_EDITOR
            if (System.Environment.GetEnvironmentVariable("NUBI_GLYPH_WARM") == "0") yield break;               // solo para comparar la medición con y sin (Tarea 190d)
#endif
            var plan = Plan(texts, extras);
            LastGroupCount = plan.Count;
            var clock = new Stopwatch();
            double frame = 0.0;
            foreach (var g in plan)
            {
                clock.Restart();
                Request(g);
                double ms = clock.Elapsed.TotalMilliseconds;
                EntryProbe.Cost("letras", ms);
                frame += ms;
                if (frame >= BudgetMs) { frame = 0.0; yield return null; }
            }
            if (words == null) yield break;
            foreach (var (sample, text, size) in words)
            {
                if (sample == null || sample.font == null || string.IsNullOrEmpty(text)) continue;
                clock.Restart();
                RequestWord(sample, text, size);
                double ms = clock.Elapsed.TotalMilliseconds;
                EntryProbe.Cost("letras", ms);
                frame += ms;
                if (frame >= BudgetMs) { frame = 0.0; yield return null; }
            }
        }
    }
}
