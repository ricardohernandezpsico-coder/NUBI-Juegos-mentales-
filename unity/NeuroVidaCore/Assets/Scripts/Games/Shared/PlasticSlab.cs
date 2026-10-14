using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>El radio de las esquinas de la losa (el del kit de la app, <c>ClayKit.kt</c>): chica = chips y casillas (14 dp), botón = botones y tarjetas (22 dp), panel = láminas (30 dp).</summary>
    public enum SlabSize { Chip, Button, Panel }

    /// <summary>
    /// El color de la losa. Los seis primeros son los del kit de la app; <see cref="Night"/>, <see cref="Hit"/>, <see cref="Miss"/> y <see cref="Dim"/> son las casillas OSCURAS de respuesta de los juegos
    /// (para que los símbolos de color resalten): normal, acierto, error y apagada. Cada una tiene su imagen normal y, las del kit y la noche normal, su imagen presionada.
    /// </summary>
    public enum SlabTone { Sun, Cream, Lime, Sky, Coral, Lilac, Night, Hit, Miss, Dim }

    /// <summary>
    /// Las losas de plasticina del kit de la app (<c>tools/isla/isla.py --kit</c>) como nine-slice de Unity: la imagen se dibuja alrededor del cuerpo (el rectángulo del botón) con sus márgenes (donde cae la sombra de contacto), las esquinas
    /// no se estiran y el centro sí (casi liso a propósito). El texto NUNCA va horneado: lo pone el juego encima. Si el atlas <c>comun</c> no existe, todo devuelve null y el que llama usa su arte de siempre.
    /// </summary>
    public static class PlasticSlab
    {
        public const string Group = "comun";
        private const string SlabName = "Slab";

        private static string Letter(SlabSize s) => s == SlabSize.Chip ? "ch" : s == SlabSize.Button ? "md" : "gr";

        private static string ToneName(SlabTone t)
        {
            switch (t)
            {
                case SlabTone.Sun: return "sol";
                case SlabTone.Cream: return "crema";
                case SlabTone.Lime: return "lima";
                case SlabTone.Sky: return "cielo";
                case SlabTone.Coral: return "coral";
                case SlabTone.Lilac: return "lila";
                case SlabTone.Night: return "noche";
                case SlabTone.Hit: return "acierto";
                case SlabTone.Miss: return "error";
                default: return "apagada";
            }
        }

        /// <summary>El nombre del sprite en el atlas («losa_md_sol», «losa_md_sol_p»…).</summary>
        public static string Key(SlabSize size, SlabTone tone, bool pressed) => "losa_" + Letter(size) + "_" + ToneName(tone) + (pressed ? "_p" : "");

        private static readonly Color[] KitColors =
        {
            new Color32(0xFF, 0xCB, 0x45, 0xFF), new Color32(0xF5, 0xEB, 0xDB, 0xFF), new Color32(0x9B, 0xE5, 0x64, 0xFF),
            new Color32(0x4C, 0xC9, 0xF0, 0xFF), new Color32(0xFF, 0x6B, 0x4A, 0xFF), new Color32(0xB8, 0xA4, 0xFF, 0xFF),
        };
        private static readonly SlabTone[] KitTones = { SlabTone.Sun, SlabTone.Cream, SlabTone.Lime, SlabTone.Sky, SlabTone.Coral, SlabTone.Lilac };

        /// <summary>La losa del kit más parecida a un color de acento (las pastillas de estado y los avisos se pintan con su acento: sol, lima, cielo, coral, uva).</summary>
        public static SlabTone Nearest(Color accent)
        {
            int best = 1;
            float bestD = float.MaxValue;
            for (int i = 0; i < KitColors.Length; i++)
            {
                float dr = accent.r - KitColors[i].r, dg = accent.g - KitColors[i].g, db = accent.b - KitColors[i].b;
                float d = dr * dr + dg * dg + db * db;
                if (d < bestD) { bestD = d; best = i; }
            }
            return KitTones[best];
        }

        /// <summary>El tamaño que de verdad existe para ese color (si no está renderizado en [size], se usa el botón, como en la app).</summary>
        public static SlabSize Resolve(SlabSize size, SlabTone tone)
        {
            var g = PlasticArt.Group(Group);
            if (g == null || g.Has(Key(size, tone, false))) return size;
            return SlabSize.Button;
        }

        public static bool Available(SlabSize size, SlabTone tone) => PlasticArt.Group(Group)?.Has(Key(Resolve(size, tone), tone, false)) ?? false;

        public static Sprite Sprite(SlabSize size, SlabTone tone, bool pressed = false)
        {
            var g = PlasticArt.Group(Group);
            if (g == null) return null;
            size = Resolve(size, tone);
            return g.Get(Key(size, tone, pressed)) ?? g.Get(Key(size, tone, false));
        }

        /// <summary>Lo que sobresale la imagen alrededor del cuerpo, en unidades del lienzo (izquierda, arriba, derecha, abajo).</summary>
        public static Vector4 Margins(SlabSize size, SlabTone tone)
        {
            var e = PlasticArt.Group(Group)?.Entry(Key(Resolve(size, tone), tone, false));
            if (e == null || e.margen == null || e.margen.Length != 4) return Vector4.zero;
            return new Vector4(e.margen[0], e.margen[1], e.margen[2], e.margen[3]);
        }

        /// <summary>Cuánto baja el texto cuando la losa está presionada (unidades del lienzo).</summary>
        public static float PressDrop(SlabSize size, SlabTone tone)
        {
            var e = PlasticArt.Group(Group)?.Entry(Key(Resolve(size, tone), tone, false));
            return e != null ? e.baja : 0f;
        }

        /// <summary>
        /// Pone la losa DETRÁS de lo que hay en [host] (su cuerpo es exactamente el rectángulo de [host]; la imagen sobresale por los márgenes) y devuelve su <c>Image</c>, o null si no hay plasticina (el que llama deja su arte de siempre).
        /// El hijo se llama «Slab», va primero (debajo del texto y los íconos) y no recibe toques.
        /// </summary>
        public static Image Attach(RectTransform host, SlabSize size, SlabTone tone)
        {
            var sprite = Sprite(size, tone);
            if (sprite == null) return null;
            return AttachSprite(host, sprite, Margins(size, tone));
        }

        private static Image AttachSprite(RectTransform host, Sprite sprite, Vector4 margins)
        {
            var existing = host.Find(SlabName);
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = new GameObject(SlabName);
                go.transform.SetParent(host, false);
                go.AddComponent<RectTransform>();
                go.AddComponent<Image>().raycastTarget = false;
            }
            go.transform.SetAsFirstSibling();
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-margins.x, -margins.w);
            rt.offsetMax = new Vector2(margins.z, margins.y);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = false;
            return img;
        }

        // ------------------------------------------------------------------ losas con el color EXACTO que pide un juego (viven en el atlas del juego, con el formato y el nine-slice de las del kit)

        /// <summary>true si el atlas [group] trae la losa [sprite] (por ejemplo «losa_md_boton_rojo» de «stroop»).</summary>
        public static bool AvailableCustom(string group, string sprite) => PlasticArt.Group(group)?.Has(sprite) ?? false;

        /// <summary>Lo que sobresale la imagen de una losa del atlas de un juego alrededor del cuerpo (izquierda, arriba, derecha, abajo), en unidades del lienzo.</summary>
        public static Vector4 MarginsCustom(string group, string sprite)
        {
            var e = PlasticArt.Group(group)?.Entry(sprite);
            if (e == null || e.margen == null || e.margen.Length != 4) return Vector4.zero;
            return new Vector4(e.margen[0], e.margen[1], e.margen[2], e.margen[3]);
        }

        /// <summary>Cuánto baja el texto cuando la losa [sprite] del atlas de un juego está presionada (unidades del lienzo).</summary>
        public static float PressDropCustom(string group, string sprite)
        {
            var e = PlasticArt.Group(group)?.Entry(sprite);
            return e != null ? e.baja : 0f;
        }

        /// <summary>Como <see cref="Attach"/>, con una losa del atlas de un juego: la pone DETRÁS de lo que hay en [host]. Null si no existe (el que llama deja su arte de siempre).</summary>
        public static Image AttachCustom(RectTransform host, string group, string sprite)
        {
            var s = PlasticArt.Get(group, sprite);
            if (s == null) return null;
            return AttachSprite(host, s, MarginsCustom(group, sprite));
        }

        /// <summary>Como <see cref="TryStyle"/>, con una losa del atlas de un juego.</summary>
        public static Image TryStyleCustom(Image host, string group, string sprite, bool hit = false)
        {
            var slab = AttachCustom((RectTransform)host.transform, group, sprite);
            if (slab == null) return null;
            host.sprite = null;
            host.type = Image.Type.Simple;
            host.color = new Color(1f, 1f, 1f, 0f);
            host.raycastTarget = hit;
            return slab;
        }

        /// <summary>Cambia la losa de un <see cref="AttachCustom"/> por otra del mismo atlas (el cuerpo y los márgenes se quedan: las de una misma clase miden lo mismo).</summary>
        public static void SetCustom(Image slab, string group, string sprite)
        {
            if (slab == null) return;
            var s = PlasticArt.Get(group, sprite);
            if (s != null) slab.sprite = s;
        }

        /// <summary>Cambia la losa de un <see cref="Attach"/> (color o estado); los márgenes y el nine-slice cambian con ella.</summary>
        public static void Set(Image slab, SlabSize size, SlabTone tone, bool pressed = false)
        {
            if (slab == null) return;
            var s = Sprite(size, tone, pressed);
            if (s != null) slab.sprite = s;
        }

        /// <summary>
        /// Pone la losa de plasticina en lugar del fondo de siempre de un botón o panel: el <c>Image</c> [host] se vuelve INVISIBLE (queda solo como objetivo de los toques si [hit]) y la losa va detrás de lo que haya en su rectángulo.
        /// Devuelve la losa, o null si no hay plasticina: entonces el que llama aplica su estilo de siempre (sprite redondeado y <c>ClayFrame</c>). Llamarla ANTES de ponerle a [host] su estilo viejo.
        /// </summary>
        public static Image TryStyle(Image host, SlabSize size, SlabTone tone, bool hit = false)
        {
            var slab = Attach((RectTransform)host.transform, size, tone);
            if (slab == null) return null;
            host.sprite = null;
            host.type = Image.Type.Simple;
            host.color = new Color(1f, 1f, 1f, 0f);
            host.raycastTarget = hit;
            return slab;
        }
    }

    /// <summary>
    /// El feedback de presión de una losa de plasticina: mientras el dedo está apoyado la losa cambia a su imagen presionada (más baja, más oscura, de sombra más chica) y lo que hay encima (el texto, el ícono) baja un poco, como en la app. Sin escala.
    /// Se pone en el mismo objeto que el <c>Button</c>; busca el hijo «Slab» que dejó <see cref="PlasticSlab.Attach"/>.
    /// </summary>
    public sealed class PlasticPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Image _slab;
        private SlabSize _size;
        private SlabTone _tone;
        private float _drop;
        private bool _down;
        private string _group, _normal, _pressed;                      // con losas del atlas de un juego (InitCustom)
        private readonly System.Collections.Generic.List<RectTransform> _moved = new System.Collections.Generic.List<RectTransform>();

        /// <summary>Le dice qué losa lleva y cuánto baja lo de encima; <paramref name="movers"/> son los hijos que acompañan la presión (el texto, el ícono).</summary>
        public void Init(Image slab, SlabSize size, SlabTone tone, params RectTransform[] movers)
        {
            _slab = slab;
            _size = size;
            _tone = tone;
            _drop = PlasticSlab.PressDrop(size, tone);
            _group = null;
            _moved.Clear();
            _moved.AddRange(movers);
        }

        /// <summary>Lo mismo con las losas de un atlas de un juego (el color exacto de la tinta): [normal] y [pressed] son nombres de sprite de [group].</summary>
        public void InitCustom(Image slab, string group, string normal, string pressed, params RectTransform[] movers)
        {
            _slab = slab;
            _group = group;
            _normal = normal;
            _pressed = pressed;
            _drop = PlasticSlab.PressDropCustom(group, normal);
            _moved.Clear();
            _moved.AddRange(movers);
        }

        public void OnPointerDown(PointerEventData eventData) => SetDown(true);
        public void OnPointerUp(PointerEventData eventData) => SetDown(false);
        public void OnPointerExit(PointerEventData eventData) => SetDown(false);
        private void OnDisable() => SetDown(false);

        private void SetDown(bool down)
        {
            if (_slab == null || down == _down) return;
            _down = down;
            if (_group != null) PlasticSlab.SetCustom(_slab, _group, down ? _pressed : _normal);
            else PlasticSlab.Set(_slab, _size, _tone, down);
            foreach (var r in _moved) if (r != null) r.anchoredPosition += new Vector2(0f, down ? -_drop : _drop);
        }
    }
}
