using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>Un sprite del atlas de plasticina (<c>manifest.json</c> de cada grupo; lo escribe <c>tools/juegos/empaqueta.py</c>). Todo en píxeles del atlas. <c>y</c> cuenta desde ABAJO (como el Rect de Unity).</summary>
    [Serializable]
    public sealed class PlasticEntry
    {
        public string n;
        public int x, y, w, h;
        public float ppu = 100f, px = 0.5f, py = 0.5f;
        /// <summary>Nine-slice de las losas: izquierda, abajo, derecha, arriba (el orden de <see cref="Sprite.border"/>). Vacío = no se estira por partes.</summary>
        public int[] borde;
        /// <summary>De las losas: lo que sobresale la imagen alrededor del cuerpo (donde cae la sombra): izquierda, arriba, derecha, abajo.</summary>
        public int[] margen;
        /// <summary>De las losas: cuánto baja el texto al presionar (píxeles = unidades del lienzo, a 3 px por dp).</summary>
        public float baja;
    }

    [Serializable] public sealed class PlasticMeasure { public string n; public float v; }

    [Serializable] public sealed class PlasticWeight { public int png_bytes, crudo_rgba32, deflate_rgba32, astc6x6; }

    [Serializable]
    public sealed class PlasticManifest
    {
        public int version;
        public string grupo;
        public int ancho, alto;
        public PlasticEntry[] sprites;
        public PlasticMeasure[] medidas;
        public PlasticWeight peso;
    }

    /// <summary>
    /// Un grupo de plasticina: UN atlas (una textura) y su manifiesto. Corta cada sprite del atlas con las medidas del manifiesto (rectángulo, pivote, nine-slice) y los guarda:
    /// <c>Resources/Plasticina/&lt;grupo&gt;/atlas.png</c> + <c>manifest.json</c>.
    /// </summary>
    public sealed class PlasticGroup
    {
        public readonly string Name;
        public readonly PlasticManifest Manifest;
        private readonly Texture2D _atlas;
        private readonly Dictionary<string, PlasticEntry> _entries = new Dictionary<string, PlasticEntry>();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        internal PlasticGroup(string name, Texture2D atlas, PlasticManifest manifest)
        {
            Name = name;
            _atlas = atlas;
            Manifest = manifest;
            if (manifest.sprites != null) foreach (var e in manifest.sprites) _entries[e.n] = e;
        }

        public Texture2D Atlas => _atlas;
        public IEnumerable<string> Names => _entries.Keys;
        public bool Has(string sprite) => _entries.ContainsKey(sprite);
        public PlasticEntry Entry(string sprite) => _entries.TryGetValue(sprite, out var e) ? e : null;

        /// <summary>El sprite [sprite] (o null si el grupo no lo tiene). Se corta una sola vez y se guarda.</summary>
        public Sprite Get(string sprite)
        {
            if (_sprites.TryGetValue(sprite, out var cached) && cached != null) return cached;
            if (!_entries.TryGetValue(sprite, out var e)) return null;
            var border = e.borde != null && e.borde.Length == 4 ? new Vector4(e.borde[0], e.borde[1], e.borde[2], e.borde[3]) : Vector4.zero;
            var s = Sprite.Create(_atlas, new Rect(e.x, e.y, e.w, e.h), new Vector2(e.px, e.py), e.ppu, 0, SpriteMeshType.FullRect, border);
            s.name = Name + "/" + sprite;
            _sprites[sprite] = s;
            return s;
        }

        internal void Release()
        {
            foreach (var s in _sprites.Values) if (s != null) UnityEngine.Object.DestroyImmediate(s);
            _sprites.Clear();
        }

        /// <summary>Una medida que dejó el script de Blender en el manifiesto (por ejemplo, dónde caen los asientos de la nave), o [fallback] si no está.</summary>
        public float Measure(string name, float fallback)
        {
            if (Manifest.medidas != null) foreach (var m in Manifest.medidas) if (m.n == name) return m.v;
            return fallback;
        }
    }

    /// <summary>
    /// Entrega el arte de PLASTICINA (imágenes renderizadas en Blender, ver <c>tools/juegos/README.md</c>) si existe y deja que cada juego caiga a su arte procedural (<see cref="ClayRaster"/>) si no:
    /// así se migra un juego sin romper los demás, y un APK sin las imágenes sigue funcionando. Cada grupo (<c>comun</c>, <c>nubi</c>, <c>radar</c>…) es un atlas.
    /// </summary>
    public static class PlasticArt
    {
        public const string Folder = "Plasticina/";

        private static readonly Dictionary<string, PlasticGroup> Groups = new Dictionary<string, PlasticGroup>();
        private static readonly HashSet<string> Missing = new HashSet<string>();

        /// <summary>true = ignora la plasticina (pruebas: comparar contra el arte procedural, o el juego de antes).</summary>
        public static bool Disabled;

        /// <summary>El grupo, o null si no existe (o si <see cref="Disabled"/>).</summary>
        public static PlasticGroup Group(string name)
        {
            if (Disabled) return null;
            if (Groups.TryGetValue(name, out var g) && g != null && g.Atlas != null) return g;
            if (Missing.Contains(name)) return null;
            var atlas = Resources.Load<Texture2D>(Folder + name + "/atlas");
            var json = Resources.Load<TextAsset>(Folder + name + "/manifest");
            if (atlas == null || json == null)
            {
                Missing.Add(name);
                return null;
            }
            PlasticManifest manifest;
            try { manifest = JsonUtility.FromJson<PlasticManifest>(json.text); }
            catch (Exception) { manifest = null; }
            if (manifest == null || manifest.sprites == null)
            {
                Missing.Add(name);
                return null;
            }
            g = new PlasticGroup(name, atlas, manifest);
            Groups[name] = g;
            return g;
        }

        /// <summary>El sprite de plasticina, o null si no existe: el que llama usa entonces el suyo.</summary>
        public static Sprite Get(string group, string sprite) => Group(group)?.Get(sprite);

        public static bool Has(string group, string sprite) => Group(group)?.Has(sprite) ?? false;

        public static float Measure(string group, string name, float fallback) => Group(group)?.Measure(name, fallback) ?? fallback;

        /// <summary>Olvida lo cargado (las pruebas que cambian <see cref="Disabled"/> o quieren cargar desde cero).</summary>
        public static void Reset()
        {
            foreach (var g in Groups.Values) g?.Release();
            Groups.Clear();
            Missing.Clear();
        }
    }
}
