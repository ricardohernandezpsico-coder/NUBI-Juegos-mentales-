using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace NeuroVida.Games.Meteoros
{
    [Serializable] public class LexWord { public string p; public int b; }
    [Serializable] public class LexDecoy { public string p; public string t; public string de; public int b; }

    /// <summary>Forma del archivo Resources/Lexico/meteoros_es.json (lo escribe tools/lexico/meteoros.py). Compatible con
    /// <see cref="JsonUtility"/>: arreglos de objetos, sin diccionarios.</summary>
    [Serializable]
    public class LexData
    {
        public int version;
        public string idioma;
        public string fuente;
        public LexWord[] palabras;
        public LexDecoy[] inventadas;
    }

    /// <summary>De dónde saca palabras e inventadas el <see cref="MeteorDirector"/> (separado para probarlo con listas chicas).</summary>
    public interface ILexicon
    {
        /// <summary>Una palabra real de las bandas [minBand, maxBand] y de ese largo (si no hay, relaja el largo y luego la banda).</summary>
        bool PickWord(int minBand, int maxBand, int minLen, int maxLen, Random rng, out string word, out int band);

        /// <summary>Una inventada de alguno de los tipos dados (si no hay del largo pedido, relaja el largo y luego el tipo).</summary>
        bool PickDecoy(DecoyKind[] kinds, int minLen, int maxLen, Random rng, out string word, out DecoyKind kind, out int band);
    }

    /// <summary>
    /// El léxico del juego, por idioma (hoy español): palabras con su banda de prevalencia (1 = la conoce casi todo el
    /// mundo … 6 = rara) e inventadas de tres tipos. No repite una palabra dentro de la partida mientras queden otras.
    /// </summary>
    public sealed class MeteorLexicon : ILexicon
    {
        public const string ResourcePath = "Lexico/meteoros_es";

        private readonly List<LexWord> _words = new List<LexWord>();
        private readonly List<LexDecoy>[] _decoys = { new List<LexDecoy>(), new List<LexDecoy>(), new List<LexDecoy>() };
        private readonly HashSet<string> _used = new HashSet<string>();

        public int WordCount => _words.Count;
        public int DecoyCount => _decoys[0].Count + _decoys[1].Count + _decoys[2].Count;

        /// <summary>Carga el léxico de Resources; si falta o no se puede leer, usa uno mínimo para que el juego no se caiga.</summary>
        public static MeteorLexicon Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset != null)
            {
                try { return FromJson(asset.text); }
                catch (Exception e) { Debug.LogError("[Meteoros] léxico ilegible: " + e.Message); }
            }
            else Debug.LogError("[Meteoros] falta Resources/" + ResourcePath + ".json: se usa un léxico mínimo.");
            return Fallback();
        }

        public static MeteorLexicon FromJson(string json)
        {
            var data = JsonUtility.FromJson<LexData>(json);
            if (data == null || data.palabras == null || data.palabras.Length == 0) throw new FormatException("sin palabras");
            return new MeteorLexicon(data);
        }

        public MeteorLexicon(LexData data)
        {
            foreach (var w in data.palabras)
                if (w != null && !string.IsNullOrEmpty(w.p) && w.b >= 1 && w.b <= 6) _words.Add(w);
            if (data.inventadas != null)
                foreach (var d in data.inventadas)
                {
                    if (d == null || string.IsNullOrEmpty(d.p)) continue;
                    int k = KindIndex(d.t);
                    if (k >= 0) _decoys[k].Add(d);
                }
        }

        private static int KindIndex(string t)
        {
            switch (t)
            {
                case "obvia": return (int)DecoyKind.Obvious;
                case "letra": return (int)DecoyKind.OneLetter;
                case "traspuesta": return (int)DecoyKind.Transposed;
                default: return -1;
            }
        }

        /// <summary>Léxico mínimo escrito a mano (30 palabras y 18 inventadas), solo por si falta el archivo.</summary>
        public static MeteorLexicon Fallback()
        {
            var words = new[]
            {
                "luna", "casa", "mesa", "libro", "ventana", "camino", "ciudad", "familia",
                "jardín", "puerta", "tiempo", "cuerpo", "música", "viaje", "montaña", "tormenta",
                "cántaro", "umbral", "alba", "ábaco", "rumbo", "bóveda", "ocaso", "sendero",
                "efímero", "inefable", "ínfimo", "albacea", "solsticio", "proscenio"
            };
            var bands = new[] { 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6 };
            var data = new LexData { version = 1, idioma = "es", fuente = "mínimo", palabras = new LexWord[words.Length] };
            for (int i = 0; i < words.Length; i++) data.palabras[i] = new LexWord { p = words[i], b = bands[i] };
            var decoys = new List<LexDecoy>();
            foreach (var s in new[] { "bolpa", "tinelo", "ramusa", "dopero", "calito", "mufano" })
                decoys.Add(new LexDecoy { p = s, t = "obvia", de = "", b = 0 });
            foreach (var pair in new[] { new[] { "ventano", "ventana" }, new[] { "camina", "camino" }, new[] { "ciudid", "ciudad" }, new[] { "jarsín", "jardín" } })
                decoys.Add(new LexDecoy { p = pair[0], t = "letra", de = pair[1], b = 1 });
            foreach (var pair in new[] { new[] { "tormenat", "tormenta" }, new[] { "mtoña", "montaña" }, new[] { "sendreo", "sendero" }, new[] { "famlila", "familia" } })
                decoys.Add(new LexDecoy { p = pair[0], t = "traspuesta", de = pair[1], b = 3 });
            data.inventadas = decoys.ToArray();
            return new MeteorLexicon(data);
        }

        // ------------------------------------------------------------------ elegir

        public bool PickWord(int minBand, int maxBand, int minLen, int maxLen, Random rng, out string word, out int band)
        {
            word = null;
            band = 0;
            if (_words.Count == 0) return false;
            var pool = new List<LexWord>();
            for (int widen = 0; widen <= 5 && pool.Count == 0; widen++)
            {
                int lo = Math.Max(1, minBand - widen), hi = Math.Min(6, maxBand + widen);
                foreach (var w in _words)
                    if (w.b >= lo && w.b <= hi && w.p.Length >= minLen && w.p.Length <= maxLen) pool.Add(w);
                if (pool.Count == 0)
                    foreach (var w in _words)
                        if (w.b >= lo && w.b <= hi) pool.Add(w);
            }
            if (pool.Count == 0) pool.AddRange(_words);
            var fresh = pool.FindAll(w => !_used.Contains(w.p));
            if (fresh.Count == 0)
            {
                foreach (var w in pool) _used.Remove(w.p);
                fresh = pool;
            }
            var pick = fresh[rng.Next(fresh.Count)];
            _used.Add(pick.p);
            word = pick.p;
            band = pick.b;
            return true;
        }

        public bool PickDecoy(DecoyKind[] kinds, int minLen, int maxLen, Random rng, out string word, out DecoyKind kind, out int band)
        {
            word = null;
            kind = DecoyKind.Obvious;
            band = 0;
            var order = new List<DecoyKind>(kinds);
            // al final, cualquier otro tipo, por si el pedido no tiene nada
            foreach (DecoyKind k in Enum.GetValues(typeof(DecoyKind)))
                if (!order.Contains(k)) order.Add(k);
            var first = kinds[rng.Next(kinds.Length)];
            order.Remove(first);
            order.Insert(0, first);
            foreach (bool strictLen in new[] { true, false })
            {
                foreach (var k in order)
                {
                    var list = _decoys[(int)k];
                    var pool = list.FindAll(d => !strictLen || (d.p.Length >= minLen && d.p.Length <= maxLen));
                    if (pool.Count == 0) continue;
                    var fresh = pool.FindAll(d => !_used.Contains(d.p));
                    if (fresh.Count == 0)
                    {
                        foreach (var d in pool) _used.Remove(d.p);
                        fresh = pool;
                    }
                    var pick = fresh[rng.Next(fresh.Count)];
                    _used.Add(pick.p);
                    word = pick.p;
                    kind = k;
                    band = pick.b;
                    return true;
                }
            }
            return false;
        }
    }
}
