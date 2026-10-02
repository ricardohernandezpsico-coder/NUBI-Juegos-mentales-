using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace NeuroVida.Games.Cosecha
{
    /// <summary>De dónde saca rondas el juego (separado para probarlo con listas chicas).</summary>
    public interface IRoundSource
    {
        /// <summary>Una ronda del nivel que no esté en [avoid] ni en [used]; si no queda, relaja primero [avoid], luego [used]. En mayores se
        /// prefieren las rondas con más palabras comunes (la mitad de arriba).</summary>
        HarvestRound Pick(int level, bool senior, Random rng, HashSet<string> used, HashSet<string> avoid);
    }

    /// <summary>Las rondas del juego, por nivel (Resources/Lexico/cosecha_es.json; lo escribe tools/cosecha/rondas.py).</summary>
    public sealed class CosechaBank : IRoundSource
    {
        public const string ResourcePath = "Lexico/cosecha_es";

        private readonly List<HarvestRound>[] _byLevel = new List<HarvestRound>[CosechaContract.MaxLevel + 1];
        public int Count { get; private set; }

        /// <summary>Carga el banco de Resources; si falta o no se puede leer, usa una ronda mínima para que el juego no se caiga.</summary>
        public static CosechaBank Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset != null)
            {
                try { return FromJson(asset.text); }
                catch (Exception e) { Debug.LogError("[Cosecha] rondas ilegibles: " + e.Message); }
            }
            else Debug.LogError("[Cosecha] falta Resources/" + ResourcePath + ".json: se usa una ronda mínima.");
            return Fallback();
        }

        public static CosechaBank FromJson(string json)
        {
            var data = JsonUtility.FromJson<HarvestData>(json);
            if (data == null || data.rondas == null || data.rondas.Length == 0) throw new FormatException("sin rondas");
            return new CosechaBank(data.rondas);
        }

        public CosechaBank(IEnumerable<HarvestRound> rounds)
        {
            for (int i = 0; i < _byLevel.Length; i++) _byLevel[i] = new List<HarvestRound>();
            foreach (var r in rounds)
            {
                if (r == null || string.IsNullOrEmpty(r.id) || string.IsNullOrEmpty(r.letras) || r.letras.Length != CosechaContract.LetterCount ||
                    r.palabras == null || r.palabras.Length == 0) continue;
                _byLevel[CosechaContract.ClampLevel(r.nivel)].Add(r);
                Count++;
            }
        }

        public int CountOf(int level) => _byLevel[CosechaContract.ClampLevel(level)].Count;

        public HarvestRound Pick(int level, bool senior, Random rng, HashSet<string> used, HashSet<string> avoid)
        {
            level = CosechaContract.ClampLevel(level);
            var list = _byLevel[level];
            // un nivel sin rondas: el más cercano que tenga
            for (int d = 1; list.Count == 0 && d < CosechaContract.MaxLevel; d++)
            {
                if (level - d >= 1 && _byLevel[level - d].Count > 0) list = _byLevel[level - d];
                else if (level + d <= CosechaContract.MaxLevel && _byLevel[level + d].Count > 0) list = _byLevel[level + d];
            }
            if (list.Count == 0) return null;
            if (senior)
            {
                // mayores: la mitad de las rondas del nivel con más palabras comunes
                var sorted = new List<HarvestRound>(list);
                sorted.Sort((a, b) => CosechaContract.CommonTotal(b).CompareTo(CosechaContract.CommonTotal(a)));
                list = sorted.GetRange(0, Math.Max(1, (sorted.Count + 1) / 2));
            }
            return TryPick(list, rng, used, avoid) ?? TryPick(list, rng, used, null) ?? TryPick(list, rng, null, null);
        }

        private static HarvestRound TryPick(List<HarvestRound> list, Random rng, HashSet<string> used, HashSet<string> avoid)
        {
            int n = list.Count, start = rng.Next(n);
            for (int k = 0; k < n; k++)
            {
                var r = list[(start + k) % n];
                if (used != null && used.Contains(r.id)) continue;
                if (avoid != null && avoid.Contains(r.id)) continue;
                return r;
            }
            return null;
        }

        /// <summary>Una ronda mínima (solo para que el juego no se caiga sin el archivo): ASOCIAR y sus palabras más comunes.</summary>
        public static CosechaBank Fallback()
        {
            string[] common = { "casi", "cría", "rica", "cara", "ora", "cosa", "río", "arca", "iras", "así", "roca", "rosa", "casa", "saca", "caro",
                                "rico", "arco", "oír", "caso", "risa", "aras", "aro", "orca", "rosca", "sacar", "casar" };
            var words = new List<HarvestWord> { new HarvestWord { p = "asociar", b = 2, comun = true, estrella = true } };
            foreach (var w in common) words.Add(new HarvestWord { p = w, b = 1, comun = true });
            var round = new HarvestRound { id = "fallback", nivel = 1, letras = "iraoasc", palabras = words.ToArray() };
            return new CosechaBank(new[] { round });
        }
    }
}
