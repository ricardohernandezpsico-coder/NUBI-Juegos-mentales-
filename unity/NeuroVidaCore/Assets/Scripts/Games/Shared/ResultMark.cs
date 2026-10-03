using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Marca ✓ / ✗ de arcilla junto a un elemento (una opción, una carta): acierto y error se ven por FORMA, no solo por color y no
    /// dependen de una animación (sacudida, rebote) que «quitar animaciones» apaga. Aparece con un pop (que con ReduceMotion queda
    /// quieto: <see cref="UiKit.PopIn"/>), se sostiene y se va. <paramref name="layer"/> debe tener anclas al centro (como los
    /// <c>_fxRect</c> de los juegos), para que <see cref="UiKit.LocalIn"/> dé la posición.
    /// </summary>
    public static class ResultMark
    {
        public static IEnumerator Show(RectTransform layer, RectTransform target, bool ok, float size, float seconds)
        {
            if (layer == null || target == null) yield break;
            var go = new GameObject(ok ? "ResultCheck" : "ResultCross");
            go.transform.SetParent(layer, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = UiKit.LocalIn(layer, target) + new Vector2(target.rect.width * 0.38f, target.rect.height * 0.38f);
            var img = go.AddComponent<Image>();
            img.sprite = ok ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            img.raycastTarget = false;
            const float pop = 0.18f;
            yield return UiKit.PopIn(rt, pop);
            yield return Motion.Hold(Mathf.Max(0f, seconds - pop));
            if (go != null) Object.Destroy(go);
        }
    }
}
