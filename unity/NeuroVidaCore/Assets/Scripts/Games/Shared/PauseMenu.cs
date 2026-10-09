using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia; // RoundedRectSprite
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Menú de pausa de los 9 juegos (Atrás a mitad de partida, o la app pasa a segundo plano): congela la partida
    /// (<see cref="GameClock"/>) y ofrece Continuar / Reiniciar / Salir. "Salir" vuelve a la app DEJANDO la partida
    /// en pausa: si después se vuelve a abrir ese mismo juego, continúa donde quedó (antes Atrás la abandonaba y
    /// empezaba de cero). Va en un canvas propio encima del juego; su animación usa el reloj real.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private RectTransform _panel;
        private CanvasGroup _group;
        private Action _onResume, _onRestart, _onExit, _onHowTo;
        private Func<bool> _canHowTo;
        private RectTransform _title, _subtitle, _btnContinue, _btnRestart, _btnHowTo, _btnExit;

        public bool IsShown => gameObject.activeSelf;

        private static PauseMenu s_shown;

        /// <summary>
        /// true mientras algún menú de pausa esté a la vista (el foco de Nubi entrenadora no debe tomar esos toques ni reanudar el juego). Se calcula del menú VIVO, no es una bandera que alguien tenga que acordarse de apagar:
        /// «Salir» deja el menú a la vista «para cuando se vuelva a abrir el juego» y el juego siguiente carga una escena nueva que lo destruye, pero la bandera estática de antes se quedaba en true para siempre y el foco de Nubi
        /// ignoraba TODOS los toques (Engranajes y Carga exacta en el teléfono de Ricardo, 9-oct, Tarea 58; el registro traía el paso pero ningún toque).
        /// </summary>
        public static bool Open => s_shown != null && s_shown.gameObject.activeInHierarchy;

        private void OnDestroy()
        {
            if (s_shown == this) s_shown = null;
        }

        /// <summary>Los botones que se ven ahora (nombre y rect), para las pruebas que tocan el centro de cada uno.</summary>
        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, RectTransform>> VisibleButtons()
        {
            var list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, RectTransform>>();
            foreach (var b in new[] { _btnContinue, _btnRestart, _btnHowTo, _btnExit })
                if (b != null && b.gameObject.activeInHierarchy) list.Add(new System.Collections.Generic.KeyValuePair<string, RectTransform>(b.name, b));
            return list;
        }

        /// <param name="onHowTo">«Cómo se juega»: la tarjeta de Nubi y la ronda guiada, y después se vuelve a la partida (el juego se encarga).</param>
        /// <param name="canHowTo">¿Se ofrece «Cómo se juega» ahora? (el juego tiene tutorial y está en marcha). Se pregunta cada vez que se abre el menú.</param>
        public static PauseMenu Create(Transform parent, Action onResume, Action onRestart, Action onExit, Action onHowTo = null, Func<bool> canHowTo = null)
        {
            EventSystemGuard.Ensure(); // sin EventSystem ningún botón de la pausa responde
            var root = new GameObject("PauseMenu");
            root.transform.SetParent(parent, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // encima del juego y de su cuenta regresiva
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            root.AddComponent<GraphicRaycaster>();

            var menu = root.AddComponent<PauseMenu>();
            menu._onResume = onResume;
            menu._onRestart = onRestart;
            menu._onExit = onExit;
            menu._onHowTo = onHowTo;
            menu._canHowTo = canHowTo;
            menu.Build(root.GetComponent<RectTransform>());
            root.SetActive(false);
            return menu;
        }

        private void Build(RectTransform root)
        {
            _group = gameObject.AddComponent<CanvasGroup>();

            // Velo oscuro: tapa el juego y bloquea sus toques mientras está en pausa.
            var scrim = new GameObject("Scrim");
            scrim.transform.SetParent(root, false);
            var scrimRect = scrim.AddComponent<RectTransform>();
            Stretch(scrimRect);
            scrim.AddComponent<Image>().color = NeuroStyle.WithAlpha(NeuroStyle.NightBottom, 0.78f);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(root, false);
            _panel = panelGo.AddComponent<RectTransform>();
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(820f, 940f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = RoundedRectSprite.Get(64);
            panelImg.type = Image.Type.Sliced;
            panelImg.color = NeuroStyle.Surface;
            NeuroStyle.ClayFrame(panelImg, 7f, 18f);

            var title = MakeText(_panel, "Title", 110, TextAnchor.MiddleCenter, NeuroStyle.Sun, 0f, 0f);
            _title = title.rectTransform;
            title.text = "Pausa";
            NeuroStyle.ClayText(title, 6f, 12f);

            var sub = MakeText(_panel, "Subtitle", 46, TextAnchor.MiddleCenter, NeuroStyle.Hex(0xB4BFEA), 0f, 0f);
            _subtitle = sub.rectTransform;
            sub.text = "Tu partida te espera";

            _btnContinue = AddButton("Continuar", NeuroStyle.Sun, () =>
            {
                Hide();
                GameClock.Resume();
                _onResume?.Invoke();
            });
            _btnRestart = AddButton("Reiniciar", NeuroStyle.Sky, () =>
            {
                Hide();
                _onRestart?.Invoke();
            });
            // «Cómo se juega» (solo en los juegos con tutorial): el reloj lo retoma el juego, no el menú
            _btnHowTo = AddButton("Cómo se juega", NeuroStyle.Grape, () =>
            {
                Hide();
                _onHowTo?.Invoke();
            });
            _btnExit = AddButton("Salir", NeuroStyle.Cream, () => _onExit?.Invoke());
            Relayout(false);
        }

        /// <summary>Acomoda el panel con tres botones o con cuatro (si se ofrece «Cómo se juega»).</summary>
        private void Relayout(bool withHowTo)
        {
            float h = withHowTo ? 1130f : 940f;
            _panel.sizeDelta = new Vector2(820f, h);
            float top = h * 0.5f;
            PlaceCentered(_title, new Vector2(0f, top - 140f), new Vector2(760f, 150f));
            PlaceCentered(_subtitle, new Vector2(0f, top - 255f), new Vector2(760f, 70f));
            float y = top - 415f;
            foreach (var btn in withHowTo ? new[] { _btnContinue, _btnRestart, _btnHowTo, _btnExit } : new[] { _btnContinue, _btnRestart, _btnExit })
            {
                PlaceCentered(btn, new Vector2(0f, y), new Vector2(640f, 150f)); // ~50 dp de alto: mayor que el mínimo de 48 dp
                y -= 190f;
            }
            _btnHowTo.gameObject.SetActive(withHowTo);
        }

        private RectTransform AddButton(string label, Color fill, Action onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(_panel, false);
            var rect = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = fill;
            NeuroStyle.ClayFrame(img, 6f, 14f);
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick());
            go.AddComponent<PressScale>();

            var text = MakeText(go.transform, "Label", 60, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            text.text = label;
            BestFit(text, 48);
            return rect;
        }

        private static void PlaceCentered(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
        }

        /// <summary>Pausa la partida y muestra el menú (con una entrada breve).</summary>
        public void Show()
        {
            GameClock.Pause();
            s_shown = this;
            if (IsShown) return;
            Relayout(_onHowTo != null && _canHowTo != null && _canHowTo());
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            StartCoroutine(PopIn());
        }

#if UNITY_EDITOR
        /// <summary>
        /// SOLO EN EL EDITOR (capturas de pantalla): termina de golpe la entrada del menú (fundido y rebote). Mientras el guion de capturas espera, el juego avanza muy pocos cuadros y la entrada, que cuenta el tiempo por cuadro, salía a medio aparecer
        /// (desvaída y con los botones sin leerse). En el teléfono no existe.
        /// </summary>
        public void SettleForCapture()
        {
            if (!IsShown) return;
            StopAllCoroutines();
            _group.alpha = 1f;
            _panel.localScale = Vector3.one;
        }
#endif

        /// <summary>Oculta el menú (no reanuda: eso lo decide quien llama).</summary>
        public void Hide()
        {
            if (s_shown == this) s_shown = null;
            gameObject.SetActive(false);
        }

        /// <summary>Atrás con el menú abierto = Continuar.</summary>
        public void ResumeFromBack()
        {
            Hide();
            GameClock.Resume();
            _onResume?.Invoke();
        }

        private IEnumerator PopIn()
        {
            float t = 0f;
            const float seconds = 0.22f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime; // reloj real: el de juego está congelado
                float k = Mathf.Clamp01(t / seconds);
                _group.alpha = k;
                _panel.localScale = Vector3.one * (Motion.Decorative ? Mathf.LerpUnclamped(0.85f, 1f, UiFx.EaseOutBack(k)) : 1f); // sin animaciones: solo fundido
                yield return null;
            }
            _group.alpha = 1f;
            _panel.localScale = Vector3.one;
        }
    }
}
