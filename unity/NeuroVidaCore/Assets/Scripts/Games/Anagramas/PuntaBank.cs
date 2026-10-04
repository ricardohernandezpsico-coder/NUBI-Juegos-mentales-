using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Anagramas
{
    /// <summary>Una palabra del banco de «En la punta de la lengua» (Resources/Lexico/punta_banco.json, hecho por tools/punta/banco.py).</summary>
    public sealed class PuntaWord
    {
        /// <summary>La palabra bien escrita, en minúsculas y con tilde («búho»): es la que se muestra al final.</summary>
        public readonly string Word;
        /// <summary>Las fichas: mayúsculas, sin tilde, con Ñ («BUHO»).</summary>
        public readonly string Tiles;
        /// <summary>La definición propia (≤ 90 caracteres).</summary>
        public readonly string Clue;
        /// <summary>Nivel 1-5 (frecuencia de uso y largo).</summary>
        public readonly int Level;
        public readonly string Category;

        public PuntaWord(string word, string tiles, string clue, int level, string category)
        {
            Word = word;
            Tiles = tiles;
            Clue = clue;
            Level = level;
            Category = category ?? "";
        }
    }

    [Serializable]
    public class PuntaWordDto
    {
        public string p, l, d, c;
        public int n, b;
        public float z;
    }

    [Serializable]
    public class PuntaBankDto
    {
        public int version;
        public PuntaWordDto[] palabras;
    }

    /// <summary>El banco de palabras con definición. Lee Resources/Lexico/punta_banco.json; sin el archivo, un banco mínimo (que no se caiga).</summary>
    public sealed class PuntaBank
    {
        public const string Path = "Lexico/punta_banco";

        private readonly List<PuntaWord> _all = new List<PuntaWord>();
        private readonly List<PuntaWord>[] _byLevel = new List<PuntaWord>[PuntaContract.MaxLevel + 1];
        private readonly Dictionary<string, PuntaWord> _byWord = new Dictionary<string, PuntaWord>();

        public IReadOnlyList<PuntaWord> All => _all;
        public int Count => _all.Count;

        public static PuntaBank Load()
        {
            var asset = Resources.Load<TextAsset>(Path);
            if (asset == null)
            {
                Debug.LogError("[Punta] falta Resources/" + Path + ".json: se usa un banco mínimo.");
                return Fallback();
            }
            try { return FromJson(asset.text); }
            catch (Exception e)
            {
                Debug.LogError("[Punta] banco ilegible: " + e.Message);
                return Fallback();
            }
        }

        public static PuntaBank FromJson(string json)
        {
            var dto = JsonUtility.FromJson<PuntaBankDto>(json);
            if (dto == null || dto.palabras == null || dto.palabras.Length == 0) throw new FormatException("sin palabras");
            var words = new List<PuntaWord>();
            foreach (var w in dto.palabras)
            {
                if (w == null || string.IsNullOrEmpty(w.p) || string.IsNullOrEmpty(w.l) || string.IsNullOrEmpty(w.d)) continue;
                if (w.l.Length != w.p.Length || w.n < 1 || w.n > PuntaContract.MaxLevel) continue;
                words.Add(new PuntaWord(w.p, w.l, w.d, w.n, w.c));
            }
            return new PuntaBank(words);
        }

        public PuntaBank(IEnumerable<PuntaWord> words)
        {
            for (int i = 0; i < _byLevel.Length; i++) _byLevel[i] = new List<PuntaWord>();
            foreach (var w in words)
            {
                if (_byWord.ContainsKey(w.Word)) continue;
                _all.Add(w);
                _byLevel[w.Level].Add(w);
                _byWord[w.Word] = w;
            }
        }

        public IReadOnlyList<PuntaWord> OfLevel(int level) => _byLevel[Math.Max(1, Math.Min(PuntaContract.MaxLevel, level))];

        /// <summary>La palabra por su escritura («búho»), o null si no está.</summary>
        public PuntaWord Find(string word) => word != null && _byWord.TryGetValue(word, out var w) ? w : null;

        public static PuntaBank Fallback() => new PuntaBank(new[]
        {
            new PuntaWord("reloj", "RELOJ", "Lo miras para saber qué hora es.", 1, "objeto"),
            new PuntaWord("faro", "FARO", "Torre junto al mar con una luz que guía a los barcos de noche.", 2, "lugar"),
            new PuntaWord("búho", "BUHO", "Ave nocturna de ojos enormes y plumas suaves que hace «uhu».", 2, "animal"),
            new PuntaWord("colmena", "COLMENA", "Hogar de las abejas, donde guardan su miel.", 3, "naturaleza"),
            new PuntaWord("almohada", "ALMOHADA", "Donde apoyas la cabeza para dormir.", 4, "objeto"),
            new PuntaWord("nostalgia", "NOSTALGIA", "Tristeza dulce al recordar tiempos pasados.", 5, "emoción"),
        });
    }
}
