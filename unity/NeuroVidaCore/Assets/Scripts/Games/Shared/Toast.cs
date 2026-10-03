using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Aviso flotante no bloqueante ("Nivel 4 · ¡Bien hecho!") que baja desde arriba,
    /// se sostiene un momento y sube desvaneciéndose. Reemplaza a las pantallas completas
    /// entre niveles (Ricardo, 23-sep: "van apareciendo pantallazos entre los niveles, falta
    /// fluidez"): el tablero sigue visible y el juego no se detiene por esto.
    /// </summary>
    public sealed class Toast
    {
        private readonly MonoBehaviour _runner;
        private readonly RectTransform _rect;
        private readonly CanvasGroup _group;
        private readonly Image _bg;
        private readonly Image _dot;
        private readonly Text _title;
        private readonly Text _subtitle;
        private readonly float _u;
        private Vector2 _basePosition;
        private Coroutine _anim;

        public Toast(Transform parent, MonoBehaviour runner, float unitsPerDp)
        {
            _runner = runner;
            _u = unitsPerDp;

            var go = new GameObject("Toast");
            go.transform.SetParent(parent, false);
            _rect = go.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.5f, 1f);
            _rect.anchorMax = new Vector2(0.5f, 1f);
            _rect.pivot = new Vector2(0.5f, 1f);
            _rect.sizeDelta = new Vector2(300f * _u, 84f * _u);
            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _bg = go.AddComponent<Image>();
            _bg.sprite = RoundedRectSprite.Get(64);
            _bg.type = Image.Type.Sliced;
            _bg.raycastTarget = false;
            NeuroStyle.ClayFrame(_bg, 4f, 8f);

            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(go.transform, false);
            var dotRect = dotGo.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0f, 0.5f);
            dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(30f * _u, 30f * _u);
            dotRect.anchoredPosition = new Vector2(44f * _u, 0f);
            _dot = dotGo.AddComponent<Image>();
            _dot.sprite = DiscSprite.Get();
            _dot.raycastTarget = false;

            _title = NewText("Title", go.transform, 24, new Vector2(0f, 0.5f), new Vector2(1f, 1f));
            _subtitle = NewText("Subtitle", go.transform, 17, new Vector2(0f, 0f), new Vector2(1f, 0.5f));
            _subtitle.color = new Color(1f, 1f, 1f, 0.78f);

            go.SetActive(false);
        }

        private Text NewText(string name, Transform parent, int fontDp, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(76f * _u, 0f);
            rect.offsetMax = new Vector2(-20f * _u, 0f);
            var text = go.AddComponent<Text>();
            text.font = UiFonts.Bold;
            text.fontSize = Mathf.RoundToInt(fontDp * _u);
            text.alignment = anchorMin.y > 0.4f ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>Lo esconde de una vez (si el juego detiene todas sus corrutinas, como al abrir «Cómo se juega», el aviso no queda a medias).</summary>
        public void Hide()
        {
            _anim = null;
            _group.alpha = 0f;
            _rect.gameObject.SetActive(false);
        }

        /// <summary>Posición (respecto del borde superior del contenedor) donde aparece.</summary>
        public void SetTopOffset(float topOffsetU) => _basePosition = new Vector2(0f, -topOffsetU);

        public void Show(string title, string subtitle, Color accent, float holdSeconds = 1.2f)
        {
            bool hasSubtitle = !string.IsNullOrEmpty(subtitle);
            _title.text = title;
            _subtitle.text = subtitle;
            _subtitle.gameObject.SetActive(hasSubtitle);
            _dot.color = accent;
            _bg.color = new Color(
                Mathf.Lerp(0.06f, accent.r, 0.28f),
                Mathf.Lerp(0.09f, accent.g, 0.28f),
                Mathf.Lerp(0.16f, accent.b, 0.28f),
                0.95f);

            FitSize(hasSubtitle);

            _rect.gameObject.SetActive(true);
            if (_anim != null) _runner.StopCoroutine(_anim);
            _anim = _runner.StartCoroutine(Run(holdSeconds));
        }

        /// <summary>Ancho y alto del recuadro a la medida del texto de este aviso, no un tamaño fijo: con textos
        /// cortos ("Con calma") queda compacto y con textos largos crece hasta un máximo cómodo; si ni el máximo
        /// alcanza, el texto pasa a los renglones que necesite y el recuadro crece con ellos (Ricardo, 28-sep: "las
        /// frases sobrepasan el recuadro". Antes solo se sumaba un renglón: en el teléfono el máximo es angosto y un
        /// texto largo ocupaba dos o tres, que quedaban fuera).</summary>
        private void FitSize(bool hasSubtitle)
        {
            const float iconAndPad = 96f; // dp: 76 a la izquierda (punto) + 20 a la derecha
            const float minWidthDp = 300f;
            const float padDp = 16f, titleLineDp = 30f, subtitleLineDp = 22f;
            // Deja siempre un margen a cada lado del contenedor real (la zona seguridad del juego), no un ancho fijo:
            // así el aviso nunca toca el borde ni se corta en pantallas angostas.
            var parentRect = _rect.parent as RectTransform;
            float parentWidthU = parentRect != null && parentRect.rect.width > 0f ? parentRect.rect.width : 900f * _u;
            float maxWidthDp = Mathf.Max(minWidthDp, parentWidthU / _u - 32f);

            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _subtitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            float titleWDp = _title.preferredWidth / _u;
            float subtitleWDp = hasSubtitle ? _subtitle.preferredWidth / _u : 0f;

            float widthDp = Mathf.Clamp(Mathf.Max(titleWDp, subtitleWDp) + iconAndPad, minWidthDp, maxWidthDp);
            float textDp = widthDp - iconAndPad;
            // Renglones: se cuentan con un 8% de holgura (el corte de palabras deja huecos al final de cada renglón).
            int titleLines = Mathf.Max(1, Mathf.CeilToInt(titleWDp * 1.08f / textDp - 0.0001f));
            int subtitleLines = hasSubtitle ? Mathf.Max(1, Mathf.CeilToInt(subtitleWDp * 1.08f / textDp - 0.0001f)) : 0;
            _title.horizontalOverflow = titleLines > 1 ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            _subtitle.horizontalOverflow = subtitleLines > 1 ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;

            float titleH = titleLines * titleLineDp, subtitleH = subtitleLines * subtitleLineDp;
            float heightDp = Mathf.Max(84f, padDp * 2f + titleH + subtitleH);
            _rect.sizeDelta = new Vector2(widthDp * _u, heightDp * _u);

            // Cada texto en su franja (arriba el título, debajo el subtítulo), centrados en el alto del recuadro.
            float top = (heightDp - titleH - subtitleH) * 0.5f;
            Place(_title, top, titleH);
            if (hasSubtitle) Place(_subtitle, top + titleH, subtitleH);
            _title.alignment = TextAnchor.MiddleLeft;
            _subtitle.alignment = TextAnchor.MiddleLeft;
        }

        private void Place(Text text, float topDp, float heightDp)
        {
            var r = text.rectTransform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.offsetMin = new Vector2(76f * _u, -(topDp + heightDp) * _u);
            r.offsetMax = new Vector2(-20f * _u, -topDp * _u);
        }

        private IEnumerator Run(float hold)
        {
            if (!Motion.Decorative)
            {
                // "Quitar animaciones": sin caída ni rebote; solo aparece y se va con un fundido de opacidad.
                _rect.anchoredPosition = _basePosition;
                yield return Motion.Fade(_group, 0f, 1f);
                yield return new WaitForSecondsRealtime(hold);
                yield return Motion.Fade(_group, 1f, 0f);
                _rect.gameObject.SetActive(false);
                yield break;
            }

            const float inSeconds = 0.30f;
            const float outSeconds = 0.30f;
            float drop = 46f * _u;

            float elapsed = 0f;
            while (elapsed < inSeconds)
            {
                elapsed += GameClock.DeltaTime;
                float t = Mathf.Clamp01(elapsed / inSeconds);
                _group.alpha = Mathf.Clamp01(t * 1.6f);
                _rect.anchoredPosition = _basePosition + new Vector2(0f, (1f - UiFx.EaseOutBack(t)) * drop);
                yield return null;
            }
            _group.alpha = 1f;
            _rect.anchoredPosition = _basePosition;

            yield return new WaitForSecondsRealtime(hold);

            elapsed = 0f;
            while (elapsed < outSeconds)
            {
                elapsed += GameClock.DeltaTime;
                float t = Mathf.Clamp01(elapsed / outSeconds);
                _group.alpha = 1f - t;
                _rect.anchoredPosition = _basePosition + new Vector2(0f, t * drop * 0.6f);
                yield return null;
            }
            _group.alpha = 0f;
            _rect.gameObject.SetActive(false);
        }
    }
}
