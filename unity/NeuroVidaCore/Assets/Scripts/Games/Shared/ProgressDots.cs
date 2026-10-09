using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>Fila de puntos de avance (uno por ensayo): gris pendiente, verde acierto,
    /// rojo error, con un pequeño "pop" al marcarse. Reemplaza a las barras de progreso.</summary>
    public sealed class ProgressDots
    {
        public static readonly Color Pending = new Color(1f, 1f, 1f, 0.18f);
        public static readonly Color Current = new Color(1f, 1f, 1f, 0.55f);
        public static readonly Color Good = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        public static readonly Color Bad = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);

        /// <summary>Ancho del lienzo de los juegos (unidades) y margen mínimo a cada lado (dp).</summary>
        public const float ReferenceWidthU = 1080f, MarginDp = 16f;

        private readonly MonoBehaviour _runner;
        private readonly RectTransform _rect;
        private readonly List<Image> _dots = new List<Image>();

        public RectTransform Rect => _rect;

        public ProgressDots(Transform parent, MonoBehaviour runner, float unitsPerDp, int count, float dotDp = 15f)
        {
            _runner = runner;
            var go = new GameObject("ProgressDots");
            go.transform.SetParent(parent, false);
            _rect = go.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.5f, 1f);
            _rect.anchorMax = new Vector2(0.5f, 1f);
            _rect.pivot = new Vector2(0.5f, 1f);
            // La fila cabe SIEMPRE en el ancho del juego con 16 dp (48 unidades) de margen a cada lado: primero se junta la separación (hasta 0,3 del punto) y, si aún no entra, se achica el punto (8-oct: con 16 puntos se salía por la derecha).
            float size = dotDp * unitsPerDp;
            float maxW = ReferenceWidthU - 2f * MarginDp * unitsPerDp;
            float gap = size * 0.75f;
            if (count > 1 && count * size + (count - 1) * gap > maxW) gap = Mathf.Max(size * 0.3f, (maxW - count * size) / (count - 1));
            if (count * size + Mathf.Max(0, count - 1) * gap > maxW)
            {
                float k = maxW / (count * size + Mathf.Max(0, count - 1) * gap);
                size *= k;
                gap *= k;
            }
            _rect.sizeDelta = new Vector2(maxW, Mathf.Max(dotDp * 1.6f * unitsPerDp, size * 1.6f));
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = gap;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < count; i++)
            {
                var dotGo = new GameObject("Dot_" + i);
                dotGo.transform.SetParent(_rect, false);
                var r = dotGo.AddComponent<RectTransform>();
                r.sizeDelta = new Vector2(size, size);
                var image = dotGo.AddComponent<Image>();
                image.sprite = DiscSprite.Get();
                image.raycastTarget = false;
                image.color = Pending;
                _dots.Add(image);
            }
        }

        public void Reset()
        {
            foreach (var d in _dots) d.color = Pending;
        }

        public void MarkCurrent(int index)
        {
            if (index >= 0 && index < _dots.Count) _dots[index].color = Current;
        }

        public void Mark(int index, bool correct)
        {
            if (index < 0 || index >= _dots.Count) return;
            var target = correct ? Good : Bad;
            if (!Motion.Decorative)
            {
                // Sin "pop": el punto pasa a su color final con un fundido corto (verde/rojo no es lo único: ver juego).
                _runner.StartCoroutine(Motion.ColorTo(_dots[index], target));
                return;
            }
            _dots[index].color = target;
            _runner.StartCoroutine(Pop(_dots[index].rectTransform));
        }

        private static IEnumerator Pop(RectTransform rect)
        {
            const float seconds = 0.26f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (rect == null) yield break;
                elapsed += GameClock.DeltaTime;
                rect.localScale = Vector3.one * Mathf.Lerp(1.7f, 1f, UiFx.EaseOutCubic(Mathf.Clamp01(elapsed / seconds)));
                yield return null;
            }
            if (rect != null) rect.localScale = Vector3.one;
        }
    }
}
