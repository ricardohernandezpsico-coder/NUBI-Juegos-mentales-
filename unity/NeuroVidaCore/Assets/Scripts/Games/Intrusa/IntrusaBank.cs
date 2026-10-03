using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace NeuroVida.Games.Intrusa
{
    /// <summary>Los grupos (Resources/Lexico/intrusa_es.json) y las figuras (Resources/Lexico/intrusa_figuras.json) del juego.</summary>
    public sealed class IntrusaBank : IGroupSource
    {
        public const string GroupsPath = "Lexico/intrusa_es";
        public const string FiguresPath = "Lexico/intrusa_figuras";

        private readonly List<IntrusaGroup>[] _byType = new List<IntrusaGroup>[7];
        private readonly Dictionary<string, List<IntrusaGroup>> _byRule = new Dictionary<string, List<IntrusaGroup>>();
        private readonly Dictionary<string, FigureShape> _figures = new Dictionary<string, FigureShape>();
        public int Count { get; private set; }

        public static IntrusaBank Load()
        {
            string groups = null, figures = null;
            var g = Resources.Load<TextAsset>(GroupsPath);
            if (g != null) groups = g.text; else Debug.LogError("[Intrusa] falta Resources/" + GroupsPath + ".json: se usa un grupo mínimo.");
            var f = Resources.Load<TextAsset>(FiguresPath);
            if (f != null) figures = f.text; else Debug.LogError("[Intrusa] falta Resources/" + FiguresPath + ".json: se usa una figura mínima.");
            try { if (groups != null) return FromJson(groups, figures); }
            catch (Exception e) { Debug.LogError("[Intrusa] banco ilegible: " + e.Message); }
            return Fallback();
        }

        public static IntrusaBank FromJson(string groupsJson, string figuresJson)
        {
            var data = JsonUtility.FromJson<IntrusaData>(groupsJson);
            if (data == null || data.grupos == null || data.grupos.Length == 0) throw new FormatException("sin grupos");
            FigureShape[] shapes = null;
            if (!string.IsNullOrEmpty(figuresJson))
            {
                var fd = JsonUtility.FromJson<FigureData>(figuresJson);
                if (fd != null) shapes = fd.figuras;
            }
            return new IntrusaBank(data.grupos, shapes);
        }

        public IntrusaBank(IEnumerable<IntrusaGroup> groups, IEnumerable<FigureShape> shapes)
        {
            for (int i = 0; i < _byType.Length; i++) _byType[i] = new List<IntrusaGroup>();
            if (shapes != null)
                foreach (var s in shapes)
                    if (s != null && !string.IsNullOrEmpty(s.regla) && Valid(s)) _figures[s.regla] = s;
            foreach (var g in groups)
            {
                if (g == null || string.IsNullOrEmpty(g.i) || g.p == null || g.p.Length != 4 || string.IsNullOrEmpty(g.x) ||
                    g.o == null || g.o.Length != 3 || g.t < 1 || g.t > 6 || string.IsNullOrEmpty(g.k)) continue;
                if (!_figures.ContainsKey(g.k)) continue;    // sin figura no hay ronda
                _byType[g.t].Add(g);
                if (!_byRule.TryGetValue(g.k, out var list)) _byRule[g.k] = list = new List<IntrusaGroup>();
                list.Add(g);
                Count++;
            }
        }

        public static bool Valid(FigureShape s) =>
            s.puntos != null && s.puntos.Length >= 14 && s.puntos.Length % 2 == 0 && s.aristas != null && s.aristas.Length >= 2 && s.aristas.Length % 2 == 0 &&
            s.anclas != null && s.anclas.Length == 4 && s.huecos != null && s.huecos.Length >= 4 && s.huecos.Length % 2 == 0 && !string.IsNullOrEmpty(s.nombre);

        public IReadOnlyList<IntrusaGroup> OfType(int type) => _byType[Math.Max(1, Math.Min(6, type))];

        public IReadOnlyList<IntrusaGroup> OfRule(string key) =>
            _byRule.TryGetValue(key ?? "", out var l) ? (IReadOnlyList<IntrusaGroup>)l : new IntrusaGroup[0];

        public FigureShape FigureOf(string key) => key != null && _figures.TryGetValue(key, out var s) ? s : null;

        public IEnumerable<string> RuleKeys => _byRule.Keys;
        public int FigureCount => _figures.Count;

        /// <summary>Un grupo y una figura mínimos (solo para que el juego no se caiga sin los archivos).</summary>
        public static IntrusaBank Fallback()
        {
            var shape = new FigureShape
            {
                regla = "fruta", nombre = "La Manzana",
                puntos = new[] { .5f, .24f, .3f, .14f, .12f, .3f, .08f, .6f, .24f, .88f, .5f, .78f, .76f, .88f, .92f, .6f, .88f, .3f, .7f, .14f },
                aristas = new[] { 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 0 },
                anclas = new[] { 2, 4, 6, 8 },
                huecos = new[] { .08f, .08f, .92f, .92f },
                contorno = new FigureOutline[0], detalles = new FigureDetail[0]
            };
            var group = new IntrusaGroup
            {
                i = "fallback", t = 1, p = new[] { "manzana", "pera", "uva", "melón" }, x = "martillo", n = "Frutas",
                o = new[] { "Frutas", "Herramientas", "Animales" }, c = 0, a = "", e = "Todas son frutas; el martillo no.", r = "", k = "fruta"
            };
            return new IntrusaBank(new[] { group }, new[] { shape });
        }
    }
}
