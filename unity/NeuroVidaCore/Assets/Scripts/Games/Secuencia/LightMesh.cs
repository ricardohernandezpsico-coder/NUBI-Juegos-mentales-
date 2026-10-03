using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Una sola malla de UI con muchos tramos de luz: el rastro de la chispa, la cinta del dedo y los tramos acertados de «Rastro de luz».
    /// Cada tramo es un rectángulo con ancho y color propios en cada extremo (el rastro se angosta y se apaga). Una malla = un dibujo por
    /// capa, en vez de cien <c>Image</c> que se mueven por cuadro (60 fps en el Motorola). Las posiciones están en el espacio local del
    /// rectángulo (el mismo de <c>anchoredPosition</c> de un hijo centrado). Sin raycast.
    /// </summary>
    public sealed class LightMesh : MaskableGraphic
    {
        private struct Seg { public Vector2 A, B; public float WA, WB; public Color CA, CB; }
        private readonly List<Seg> _segs = new List<Seg>(128);

        public int Count => _segs.Count;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Clear()
        {
            if (_segs.Count == 0) return;
            _segs.Clear();
            SetVerticesDirty();
        }

        /// <summary>Un tramo de <paramref name="a"/> a <paramref name="b"/> con ancho <paramref name="wa"/>→<paramref name="wb"/> y color <paramref name="ca"/>→<paramref name="cb"/>.</summary>
        public void Add(Vector2 a, Vector2 b, float wa, float wb, Color ca, Color cb)
        {
            if ((b - a).sqrMagnitude < 0.01f) return;
            _segs.Add(new Seg { A = a, B = b, WA = wa, WB = wb, CA = ca, CB = cb });
        }

        public void Add(Vector2 a, Vector2 b, float w, Color c) => Add(a, b, w, w, c, c);

        /// <summary>Termina de armar el cuadro: vuelve a dibujar la malla.</summary>
        public void Commit() => SetVerticesDirty();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int v = 0;
            foreach (var s in _segs)
            {
                Vector2 d = s.B - s.A;
                Vector2 n = new Vector2(-d.y, d.x).normalized;
                Vector2 ha = n * (s.WA * 0.5f), hb = n * (s.WB * 0.5f);
                vh.AddVert(s.A - ha, s.CA, Vector2.zero);
                vh.AddVert(s.A + ha, s.CA, Vector2.zero);
                vh.AddVert(s.B + hb, s.CB, Vector2.zero);
                vh.AddVert(s.B - hb, s.CB, Vector2.zero);
                vh.AddTriangle(v, v + 1, v + 2);
                vh.AddTriangle(v, v + 2, v + 3);
                v += 4;
            }
        }
    }
}
