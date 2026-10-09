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
            if (_anim != null) { _runner.StopCoroutine(_anim); _anim = null; }
            _group.alpha = 0f;
            _rect.gameObject.SetActive(false);
        }

        /// <summary>
        /// El aviso aparece DEBAJO del marcador de arriba (<see cref="GameHud"/>: título, «Nivel», avance y racha), nunca encima: <paramref name="extraU"/> unidades más abajo todavía si el juego lo pide. Antes se ponía en el borde superior
        /// y tapaba el título y los rótulos (8-oct, revisión de las capturas).
        /// </summary>
        public void SetBelowHud(float extraU = 0f)
        {
            _minTopU = GameHud.Height + BelowHudGap + extraU;
            _basePosition = new Vector2(0f, -_minTopU);
        }

        private float _minTopU = GameHud.Height + BelowHudGap;

        /// <summary>Cuánto baja el aviso desde el borde de arriba de su contenedor donde quedó (unidades).</summary>
        public float PlacedTopU => -_basePosition.y;

        /// <summary>La caja del aviso (para la guardia del smoke y las pruebas).</summary>
        public RectTransform Rect => _rect;

        /// <summary>Cuánto pisa el aviso, donde quedó, a lo que no se debe tapar (0 = franja libre; <see cref="ToastPlacement"/>).</summary>
        public float PlacedOverlap { get; private set; }

        private readonly System.Collections.Generic.List<RectTransform> _keepOut = new System.Collections.Generic.List<RectTransform>();

        /// <summary>
        /// Una zona donde el aviso NUNCA va, aunque en este momento no se vea nada en ella (el campo donde se mueve el estímulo: el cielo de Meteoros y de Aterrizaje, la arena de Satélites, el radar, el lugar de la señal de Freno). Las cajas invisibles y apagadas cuentan.
        /// </summary>
        public void KeepOut(RectTransform zone)
        {
            if (zone != null && !_keepOut.Contains(zone)) _keepOut.Add(zone);
        }

        /// <summary>Las zonas que el juego declaró con <see cref="KeepOut"/> (para la guardia del smoke).</summary>
        public System.Collections.Generic.IReadOnlyList<RectTransform> KeepOutZones => _keepOut;

        /// <summary>Lo más que el aviso espera a que aparezca una franja libre antes de mostrarse donde menos choque (segundos).</summary>
        public const float MaxWaitSeconds = 4f;

        /// <summary>
        /// Busca la primera franja libre debajo del marcador (de arriba hacia abajo, de 12 en 12 unidades) donde el aviso, con el tamaño que ya tiene, no choca con ningún texto, ningún botón, el estímulo del juego (<see cref="ToastPlacement"/>) ni una zona declarada
        /// con <see cref="KeepOut"/>. Devuelve true si la encontró; si no, deja el aviso donde menos choca. Cada juego deja su disposición como está y el aviso se acomoda a ella (Tarea 55: antes caía sobre «Tu estación», la pregunta y la pieza de Acoplamiento y sobre el medidor de Freno).
        /// </summary>
        private bool TryPlace()
        {
            var parent = _rect.parent as RectTransform;
            PlacedOverlap = 0f;
            if (parent == null || parent.rect.height <= 0f) return true;
            var protectedBoxes = ToastPlacement.Collect(parent, _rect);
            foreach (var z in _keepOut) if (z != null) protectedBoxes.Add(ToastPlacement.LocalRectOf(parent, z));
            float h = _rect.sizeDelta.y, w = _rect.sizeDelta.x;
            float cx = parent.rect.center.x;
            float maxTop = parent.rect.height - h - 6f;
            float best = _minTopU, bestScore = float.MaxValue;
            for (float top = _minTopU; top <= Mathf.Max(_minTopU, maxTop); top += 12f)
            {
                var box = new Rect(cx - w * 0.5f, top, w, h);
                float score = 0f;
                foreach (var p in protectedBoxes) score += ToastPlacement.Overlap(box, p);
                if (score < bestScore) { bestScore = score; best = top; }
                if (score <= 1f) break;
            }
            _basePosition = new Vector2(0f, -best);
            PlacedOverlap = bestScore <= 1f ? 0f : bestScore;
            return bestScore <= 1f;
        }

        /// <summary>Aire entre el marcador y el aviso (unidades).</summary>
        public const float BelowHudGap = 14f;

        /// <summary>Cuánto baja el aviso desde el borde de arriba del contenedor (unidades): lo que fijó <see cref="SetBelowHud"/>. Para las pruebas.</summary>
        public float TopOffsetU => -_basePosition.y;

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

            _group.alpha = 0f;
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
            const float padDp = 12f, titleLineDp = 28f, subtitleLineDp = 22f;
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
            float heightDp = Mathf.Max(72f, padDp * 2f + titleH + subtitleH);
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
            // Sin franja libre en este momento (el estímulo ocupa donde cabría), el aviso espera «entre ensayos» a que aparezca una; a los MaxWaitSeconds se muestra donde menos choca.
            float waited = 0f;
            while (!TryPlace() && waited < MaxWaitSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            _rect.anchoredPosition = _basePosition;
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
            float drop = 36f * _u;           // entra subiendo desde abajo (no cae desde arriba: ahí está el marcador)

            float elapsed = 0f;
            while (elapsed < inSeconds)
            {
                elapsed += GameClock.DeltaTime;
                float t = Mathf.Clamp01(elapsed / inSeconds);
                _group.alpha = Mathf.Clamp01(t * 1.6f);
                _rect.anchoredPosition = _basePosition - new Vector2(0f, Mathf.Max(0f, 1f - UiFx.EaseOutBack(t)) * drop);
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
                _rect.anchoredPosition = _basePosition - new Vector2(0f, t * drop * 0.6f);
                yield return null;
            }
            _group.alpha = 0f;
            _rect.gameObject.SetActive(false);
        }
    }
}
