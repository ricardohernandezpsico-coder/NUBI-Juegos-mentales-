#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// La guardia de la «imagen de fábrica» (Tarea 178, 26-oct; SOLO del Editor: el smoke, las capturas y las pruebas la usan; no viaja en el APK): ninguna <c>Image</c> se ve con los valores con que nace un objeto de la interfaz. Una <c>Image</c> recién creada (o activada) mide 100 × 100, está en el centro de su padre y es BLANCA; si un efecto,
    /// un aviso o un lucero se hace visible antes de que el código le fije posición, tamaño y color, se ve UN cuadro como un disco (o cuadrado) blanco de ~100 px en el centro: «unas pelotitas blanquitas que aparecen y se apagan» de En la punta de la lengua (Tarea 170) y el «círculo en blanco» que Ricardo vio en Tinta o Palabra (26-oct).
    /// La prueba de lectura de código (<c>ParticleActivationGuardTests</c>) solo puede mirar los métodos que crean y activan su objeto en el mismo cuerpo: casi todos los efectos se construyen al armar la escena (con su posición) y se encienden más tarde desde otro método, y eso no se ve leyendo el texto. Esta mira lo que realmente se ve, en CUALQUIER juego y en
    /// CUALQUIER objeto, durante todas las corridas del smoke; con la ruta en la jerarquía dice quién es.
    /// <para>
    /// TRES REGLAS (<see cref="Rule"/>), porque la píldora de Tinta o Palabra, la causa real, era un HIJO (una losa estirada y un punto) de un padre sin acomodar que sí tenía posición (la de su sitio bajo la tarjeta): la regla estricta de la imagen blanca de fábrica no la ve. Cuál de ellas cuenta como error lo decide <see cref="Active"/>;
    /// con el experimento del smoke (<c>NUBI_GUARD_EXPERIMENT=1</c>) se registran todas por separado y no se cuenta ninguna.
    /// </para>
    /// </summary>
    public static class FactoryImageGuard
    {
        [Flags]
        public enum Rule
        {
            None = 0,
            /// <summary>A, estricta: ELLA misma está con todos los valores de fábrica (100 × 100, anclas al centro, en (0, 0), blanca) y es un disco, halo o destello sin arte con nombre: la clase de la Tarea 170 (chispas, aros y luceros sin acomodar).</summary>
            Strict = 1,
            /// <summary>B, punto medio de Opus: ELLA o su PADRE inmediato están en estado de fábrica COMPLETO (100 × 100, anclas y pivote al centro, posición 0 y escala 1) y se ve. Si el padre tiene posición (como la píldora, que SetPosition dejó en su sitio) no cae.</summary>
            Midpoint = 2,
            /// <summary>C, hijo estirado: la imagen está ESTIRADA sobre su padre (anclas separadas) y el padre tiene el tamaño de fábrica con anclas al centro, en cualquier posición: lo que se ve depende de un padre al que nadie le dio tamaño (la losa de la píldora).</summary>
            StretchedChild = 4,
        }

        /// <summary>Qué reglas cuentan como error (el resto solo se registra con el experimento). Se fija con la evidencia del smoke dirigido (Tarea 178).</summary>
        public static Rule Active = Rule.Strict | Rule.StretchedChild;

        /// <summary>Lo que mide una <c>Image</c> recién creada.</summary>
        private static readonly Vector2 FactorySize = new Vector2(100f, 100f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private static bool CenterAnchored(RectTransform rt) => rt.anchorMin == rt.anchorMax && rt.anchorMin == Center;

        /// <summary>Anclas al centro y 100 × 100, en cualquier posición, pivote y escala.</summary>
        private static bool FactorySized(RectTransform rt) => rt != null && CenterAnchored(rt) && rt.sizeDelta == FactorySize;

        /// <summary>El estado de fábrica COMPLETO: 100 × 100, anclas y pivote al centro, posición 0 y escala 1.</summary>
        private static bool Complete(RectTransform rt) =>
            FactorySized(rt) && rt.pivot == Center && rt.anchoredPosition == Vector2.zero && rt.localScale == Vector3.one;

        private static bool Visible(Image img)
        {
            if (img.color.a * img.canvasRenderer.GetInheritedAlpha() < 0.2f) return false;
            var s = img.rectTransform.lossyScale;
            return Mathf.Abs(s.x) > 0.0001f && Mathf.Abs(s.y) > 0.0001f;
        }

        /// <summary>Qué reglas cumple la imagen ahora (<see cref="Rule.None"/> si ninguna o si no se ve).</summary>
        public static Rule Classify(Image img)
        {
            if (img == null || !img.isActiveAndEnabled || !Visible(img)) return Rule.None;
            var rt = img.rectTransform;
            var parent = rt.parent as RectTransform;
            var r = Rule.None;
            var c = img.color;
            // la imagen de un ARTE con nombre (un sprite del atlas: la nave de Aterrizaje, una losa…) en 100 × 100 y blanca (sin teñir) es el diseño; un disco, un halo o un destello (sin sprite o con uno calculado, sin nombre) en ese estado es el error
            bool generic = img.sprite == null || string.IsNullOrEmpty(img.sprite.name);
            if (generic && FactorySized(rt) && rt.anchoredPosition == Vector2.zero && c.r >= 0.95f && c.g >= 0.95f && c.b >= 0.95f) r |= Rule.Strict;
            if (Complete(rt) || Complete(parent)) r |= Rule.Midpoint;
            if (rt.anchorMin != rt.anchorMax && FactorySized(parent)) r |= Rule.StretchedChild;
            return r;
        }

        /// <summary>¿Cuenta como error ahora? Alguna de las reglas <see cref="Active"/>.</summary>
        public static bool IsFactory(Image img) => (Classify(img) & Active) != Rule.None;

        /// <summary>Las imágenes de fábrica visibles ahora (reglas activas), cada una como «ruta en la jerarquía (sprite)»; null si no hay ninguna.</summary>
        public static List<string> Scan() => Scan(Active);

        /// <summary>Lo mismo con las reglas pedidas; cada ruta lleva las letras de las reglas que cumple («[A+C] ruta (sprite …)»).</summary>
        public static List<string> Scan(Rule rules)
        {
            List<string> hits = null;
            foreach (var img in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var r = Classify(img) & rules;
                if (r == Rule.None) continue;
                if (hits == null) hits = new List<string>();
                hits.Add("[" + Letters(r) + "] " + PathOf(img.transform) + " (sprite " + (img.sprite != null ? img.sprite.name : "ninguno") + ")");
            }
            return hits;
        }

        public static string Letters(Rule r)
        {
            var parts = new List<string>();
            if ((r & Rule.Strict) != 0) parts.Add("A");
            if ((r & Rule.Midpoint) != 0) parts.Add("B");
            if ((r & Rule.StretchedChild) != 0) parts.Add("C");
            return string.Join("+", parts);
        }

        public static string PathOf(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }
    }
}
#endif
