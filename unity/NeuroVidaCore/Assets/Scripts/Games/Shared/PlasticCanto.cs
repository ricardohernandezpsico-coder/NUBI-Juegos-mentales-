using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El CANTO de la plasticina (el volumen aprobado el 16-oct para Engranajes; ver <c>docs/plasticina-juegos.md</c>): la misma silueta de un dibujo, más oscura, corrida HACIA ABAJO en la pantalla y dibujada DEBAJO de él, como el grosor de una pieza de plasticina.
    /// Es un efecto de malla (<see cref="BaseMeshEffect"/>): duplica la malla del gráfico en [<see cref="Steps"/>] pasos entre 0 y <see cref="Distance"/> (del más oscuro y lejano al más claro y cercano), así que sigue al gráfico solo (posición, tamaño, color de alfa y
    /// grupo de alfa) y no cuesta un objeto más. El corrimiento va en el espacio de la PANTALLA: si el gráfico está girado, el corrimiento se gira al revés (el canto NO gira con la pieza: no delata el giro ni el espejo). La malla solo se rehace cuando el gráfico cambia de tamaño, de sprite o de color;
    /// quien gira un gráfico con canto cambia su ángulo y llama a <see cref="Refresh"/> (o a <c>SetVerticesDirty</c>). Sirve también para dar relieve a un texto (2 pasos de un tono oscuro, ~1 dp).
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class CantoEffect : BaseMeshEffect
    {
        public const int DefaultSteps = 5;

        /// <summary>Cuánto baja el canto en la pantalla (unidades del lienzo del padre del gráfico; 3 por dp en las escenas de Engranajes).</summary>
        public float Distance = 13.5f;
        /// <summary>Cuántas copias de la silueta entre 0 y <see cref="Distance"/> (más pasos = una pared más lisa en las diagonales).</summary>
        public int Steps = DefaultSteps;
        /// <summary>El tono del extremo de abajo (lo más hondo del canto) y el de arriba (junto a la cara); se MULTIPLICA con lo que traiga el dibujo (un sprite claro da casi este color).</summary>
        public Color Deep = new Color(0.30f, 0.22f, 0.52f, 1f);
        public Color Light = new Color(0.42f, 0.32f, 0.70f, 1f);

        private static readonly List<UIVertex> Source = new List<UIVertex>(64);
        private static readonly List<UIVertex> Output = new List<UIVertex>(256);
        private float _lastZ = float.NaN;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || Distance <= 0f || Steps <= 0 || vh.currentVertCount == 0) return;
            Source.Clear();
            vh.GetUIVertexStream(Source);
            int n = Source.Count;
            if (n == 0) return;
            float z = graphic.rectTransform.localEulerAngles.z;
            var down = new Vector3(0f, -1f, 0f);
            if (Mathf.Abs(Mathf.DeltaAngle(0f, z)) > 0.01f) down = Quaternion.Euler(0f, 0f, -z) * down;      // el corrimiento es de la pantalla: se gira al revés que el gráfico
            _lastZ = z;
            Output.Clear();
            for (int i = Steps; i >= 1; i--)                                                                // de lo más hondo a lo más cercano: lo cercano tapa lo hondo
            {
                float f = i / (float)Steps;
                var c = Color.Lerp(Light, Deep, f);
                var offset = down * (Distance * f);
                for (int k = 0; k < n; k++)
                {
                    var v = Source[k];
                    var vc = v.color;
                    v.position += offset;
                    v.color = new Color32((byte)Mathf.RoundToInt(c.r * 255f), (byte)Mathf.RoundToInt(c.g * 255f), (byte)Mathf.RoundToInt(c.b * 255f), (byte)Mathf.RoundToInt(vc.a * c.a));
                    Output.Add(v);
                }
            }
            Output.AddRange(Source);                                                                         // la cara, encima
            vh.Clear();
            vh.AddUIVertexTriangleStream(Output);
        }

        /// <summary>Rehace la malla si el gráfico giró desde la última vez (el corrimiento depende de su ángulo).</summary>
        public void Refresh()
        {
            if (graphic == null) return;
            float z = graphic.rectTransform.localEulerAngles.z;
            if (Mathf.Abs(Mathf.DeltaAngle(_lastZ, z)) > 0.01f || float.IsNaN(_lastZ)) graphic.SetVerticesDirty();
        }

        public void Apply(float distance, int steps, Color deep, Color light)
        {
            Distance = distance;
            Steps = steps;
            Deep = deep;
            Light = light;
            if (graphic != null) graphic.SetVerticesDirty();
        }
    }

    /// <summary>Atajos para ponerle canto (<see cref="CantoEffect"/>) a un gráfico o a un texto.</summary>
    public static class PlasticCanto
    {
        /// <summary>Le pone (o le cambia) el canto al gráfico [g] y lo devuelve (null si [g] es null): [distance] en unidades del lienzo hacia ABAJO en la pantalla, en [steps] pasos de [light] (cerca) a [deep] (hondo).</summary>
        public static CantoEffect Add(Graphic g, float distance, Color deep, Color light, int steps = CantoEffect.DefaultSteps)
        {
            if (g == null) return null;
            var e = g.GetComponent<CantoEffect>();
            if (e == null) e = g.gameObject.AddComponent<CantoEffect>();
            e.Apply(distance, steps, deep, light);
            return e;
        }

        /// <summary>El relieve de un texto o un ícono: una sombra pegada (2 pasos) del tono [shade], [distance] unidades hacia abajo.</summary>
        public static CantoEffect Relief(Graphic g, float distance, Color shade, int steps = 2) => Add(g, distance, shade, shade, steps);

        /// <summary>El tono hondo del canto de un color de la paleta (más oscuro y un poco más saturado) y el claro (junto a la cara).</summary>
        public static void Tones(Color face, out Color deep, out Color light)
        {
            Color.RGBToHSV(face, out float h, out float s, out float v);
            deep = Color.HSVToRGB(h, Mathf.Clamp01(s * 1.1f + 0.05f), v * 0.40f);
            light = Color.HSVToRGB(h, Mathf.Clamp01(s * 1.1f + 0.04f), v * 0.60f);
        }
    }
}
