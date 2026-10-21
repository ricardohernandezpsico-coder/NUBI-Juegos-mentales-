using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia; // RoundedRectSprite

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// La losa chica «Seguir» de las tarjetas «Nuevo» de Bodega, Correo, Engranajes y Constelaciones (Tarea 154, B8.4 de la auditoría de coherencia): antes decía «Toca para seguir» en azul sobre crema, que parecía un enlace y no un botón.
    /// Toda la tarjeta se toca (cualquier toque, pasada media décima, sigue), así que la losa no es un objetivo aparte: solo lo dice con la forma de un botón del kit (chica, sol, con la letra en tinta). El texto va DESPUÉS de la losa, encima.
    /// </summary>
    public static class SeguirChip
    {
        public const string Label = "Seguir";

        /// <summary>El tamaño de la losa (dp): el texto de 16 dp cabe de sobra y la losa se ve a distancia de teléfono.</summary>
        public const float WidthDp = 132f, HeightDp = 40f;

        /// <summary>Lo que crece la tarjeta, abajo, para alojar la losa (dp).</summary>
        public const float ExtraDp = 24f;

        /// <summary>Dónde cae el centro de la losa, medido desde el borde de abajo de la tarjeta (dp).</summary>
        public const float FromBottomDp = 32f;

        /// <summary>Crea la losa dentro de <paramref name="parent"/> (antes que su texto, para que quede debajo): la losa chica sol del kit; sin plasticina, un rectángulo redondeado sol.</summary>
        public static Image Create(Transform parent)
        {
            var go = new GameObject("SeguirSlab", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var host = go.GetComponent<Image>();
            if (PlasticSlab.TryStyle(host, SlabSize.Chip, SlabTone.Sun) == null)
            {
                host.sprite = RoundedRectSprite.Get(24);
                host.color = NeuroStyle.Sun;
            }
            host.raycastTarget = false;
            return host;
        }
    }
}
