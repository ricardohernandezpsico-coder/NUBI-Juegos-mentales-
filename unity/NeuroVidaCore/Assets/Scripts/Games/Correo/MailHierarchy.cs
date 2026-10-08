using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Correo
{
    /// <summary>
    /// El orden en que UGUI dibuja dos piezas: de atrás hacia adelante, siguiendo el orden de los hermanos (<see cref="Transform.GetSiblingIndex"/>) desde el primer ancestro que comparten.
    /// <c>SetSiblingIndex</c> ordena SOLO entre hermanos: una carta que es hija directa de la capa de la estación y se manda al índice 0 queda debajo de la banda de la cinta (el error del 8-oct).
    /// </summary>
    public static class MailHierarchy
    {
        /// <summary>true si <paramref name="a"/> se dibuja DESPUÉS (encima) de <paramref name="b"/>. Un ancestro se dibuja antes que sus descendientes. Piezas sin ancestro común: false.</summary>
        public static bool DrawnAfter(Transform a, Transform b)
        {
            if (a == null || b == null || a == b) return false;
            var pa = PathFromRoot(a);
            var pb = PathFromRoot(b);
            int n = Mathf.Min(pa.Count, pb.Count);
            for (int i = 0; i < n; i++)
            {
                if (pa[i] == pb[i]) continue;
                if (i == 0) return false;                                    // otra raíz
                return pa[i].GetSiblingIndex() > pb[i].GetSiblingIndex();
            }
            return pa.Count > pb.Count;                                      // b es ancestro de a: a se dibuja después
        }

        private static List<Transform> PathFromRoot(Transform t)
        {
            var path = new List<Transform>();
            for (var c = t; c != null; c = c.parent) path.Add(c);
            path.Reverse();
            return path;
        }
    }
}
