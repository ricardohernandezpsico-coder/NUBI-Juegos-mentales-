using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace NeuroVida.Games.Disparate
{
    /// <summary>Forma del archivo Resources/Frases/disparate_es.json (lo escribe tools/frases/disparate.py). Compatible con
    /// <see cref="JsonUtility"/>: arreglos de objetos, sin diccionarios.</summary>
    [Serializable]
    public class SentenceData
    {
        public int version;
        public string idioma;
        public string fuente;
        public SentenceItem[] frases;
    }

    /// <summary>De dónde saca frases el <see cref="DisparateDirector"/> (separado para probarlo con listas chicas).</summary>
    public interface ISentenceSource
    {
        /// <summary>Una frase de ese tipo y respuesta que no esté en [used] ni en [avoid]; si no queda, relaja primero [avoid] y luego [used].
        /// [maxWords] &gt; 0 pide frases de hasta tantas palabras (si no hay, relaja).</summary>
        SentenceItem Pick(int type, bool truth, Random rng, HashSet<string> used, HashSet<string> avoid, int maxWords = 0);
    }

    /// <summary>El banco de frases del juego, por tipo y respuesta.</summary>
    public sealed class SentenceBank : ISentenceSource
    {
        public const string ResourcePath = "Frases/disparate_es";

        private readonly List<SentenceItem>[,] _by = new List<SentenceItem>[7, 2];
        public int Count { get; private set; }

        /// <summary>Carga el banco de Resources; si falta o no se puede leer, usa uno mínimo para que el juego no se caiga.</summary>
        public static SentenceBank Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset != null)
            {
                try { return FromJson(asset.text); }
                catch (Exception e) { Debug.LogError("[Disparate] frases ilegibles: " + e.Message); }
            }
            else Debug.LogError("[Disparate] falta Resources/" + ResourcePath + ".json: se usa un banco mínimo.");
            return Fallback();
        }

        public static SentenceBank FromJson(string json)
        {
            var data = JsonUtility.FromJson<SentenceData>(json);
            if (data == null || data.frases == null || data.frases.Length == 0) throw new FormatException("sin frases");
            return new SentenceBank(data.frases);
        }

        public SentenceBank(IEnumerable<SentenceItem> items)
        {
            for (int t = 0; t < 7; t++) for (int k = 0; k < 2; k++) _by[t, k] = new List<SentenceItem>();
            foreach (var s in items)
            {
                if (s == null || string.IsNullOrEmpty(s.f) || s.t < 1 || s.t > 6 || string.IsNullOrEmpty(s.i)) continue;
                _by[s.t, s.v ? 1 : 0].Add(s);
                Count++;
            }
        }

        public int CountOf(int type, bool truth) => _by[type, truth ? 1 : 0].Count;

        public SentenceItem Pick(int type, bool truth, Random rng, HashSet<string> used, HashSet<string> avoid, int maxWords = 0)
        {
            type = Math.Max(1, Math.Min(6, type));
            var list = _by[type, truth ? 1 : 0];
            if (list.Count == 0) list = _by[type, truth ? 0 : 1];     // sin banco de ese tipo y respuesta: algo hay que mostrar
            if (list.Count == 0) return null;
            var s = TryPick(list, rng, used, avoid, maxWords) ?? TryPick(list, rng, used, null, maxWords)
                    ?? TryPick(list, rng, used, avoid, 0) ?? TryPick(list, rng, used, null, 0) ?? TryPick(list, rng, null, null, 0);
            return s;
        }

        private static SentenceItem TryPick(List<SentenceItem> list, Random rng, HashSet<string> used, HashSet<string> avoid, int maxWords)
        {
            // el primer candidato desde un punto al azar: una pasada, sin armar listas nuevas
            int n = list.Count, start = rng.Next(n);
            for (int k = 0; k < n; k++)
            {
                var s = list[(start + k) % n];
                if (used != null && used.Contains(s.i)) continue;
                if (avoid != null && avoid.Contains(s.i)) continue;
                if (maxWords > 0 && s.n > maxWords) continue;
                return s;
            }
            return null;
        }

        /// <summary>Banco mínimo: dos frases por tipo y respuesta (solo para que el juego no se caiga sin el archivo).</summary>
        public static SentenceBank Fallback()
        {
            var items = new List<SentenceItem>();
            int id = 0;
            void Add(int t, bool v, string f, string c, string r) =>
                items.Add(new SentenceItem { f = f, v = v, t = t, c = c, n = f.Split(' ').Length, r = r, i = "fb" + (id++) });
            Add(1, true, "Los peces nadan", "", ""); Add(1, true, "Las vacas mugen", "", "");
            Add(1, false, "Las sillas ríen", "evidente", "Las sillas no ríen"); Add(1, false, "Los pingüinos vuelan", "sutil", "Los pingüinos no vuelan");
            Add(2, true, "El sol calienta la tierra", "", ""); Add(2, true, "Los gatos tienen bigotes", "", "");
            Add(2, false, "El hielo es caliente", "sutil", "El hielo es frío"); Add(2, false, "Las vacas leen libros", "evidente", "Las vacas no leen libros");
            Add(3, true, "Los gatos no vuelan", "", ""); Add(3, true, "Las sillas no lloran", "", "");
            Add(3, false, "Los peces no nadan", "sutil", "Los peces sí nadan"); Add(3, false, "El sol no es caliente", "sutil", "El sol sí es caliente");
            Add(4, true, "Los delfines, que tienen aletas, nadan", "", ""); Add(4, true, "El hielo, que es frío, flota en el agua", "", "");
            Add(4, false, "Los peces, que nadan, tienen plumas", "evidente", "Los peces, que nadan, no tienen plumas"); Add(4, false, "Las sillas, que sirven para sentarse, ríen", "evidente", "Las sillas, que sirven para sentarse, no ríen");
            Add(5, true, "Todos los peces nadan", "", ""); Add(5, true, "Ningún pez ladra", "", "");
            Add(5, false, "Todos los animales vuelan", "sutil", "Solo algunos animales vuelan"); Add(5, false, "Ningún animal vuela", "sutil", "Algunos animales vuelan");
            Add(6, true, "Una hormiga es más pequeña que un elefante", "", ""); Add(6, true, "Un minuto dura menos que una hora", "", "");
            Add(6, false, "Un día dura más que un año", "evidente", "Un año dura más que un día"); Add(6, false, "Tres es mayor que siete", "evidente", "Siete es mayor que tres");
            return new SentenceBank(items);
        }
    }
}
