using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia; // RoundedRectSprite
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Tutorial guiado COMÚN de los juegos (docs/diseno-inicio.md, «Tutorial dentro del juego»; docs/diseno-rastro-de-luz.md §5): el mismo para quien abre
    /// un juego por primera vez que para quien lo hace dentro del inicio. Rastro de luz es el primero que lo usa; los otros 22 lo usarán igual:
    /// <list type="number">
    /// <item><b>Tarjeta de entrada</b> (<see cref="Intro"/>): Nubi maestra (<see cref="NubiTeacherSprite"/>), el nombre del juego, la meta en una frase,
    /// «Probar una ronda» y «Saltar tutorial».</item>
    /// <item><b>Ronda guiada</b> que arma cada juego (<see cref="GameControllerBase.GuidedRound"/>): mientras dura, <see cref="BeginPractice"/> pone
    /// el rótulo «Práctica: no cuenta» y el botón «Saltar tutorial» siempre visible; <see cref="Say"/> escribe lo que Nubi dice («Casi: esa no era…»).</item>
    /// </list>
    /// La ronda guiada NO cuenta: ni puntos, ni DDA, ni avance, ni rachas, y no se guarda como partida: el juego la juega sin pasarla por su motor.
    /// Se maneja con toques directos (sin <c>Button</c>), igual que el resto de los controles de Unity. Con «quitar animaciones» solo hay fundidos.
    /// </summary>
    public sealed class GuidedTutorial
    {
        private const float Dp = 3f;

        private readonly RectTransform _intro, _startRect, _skipIntroRect, _practiceRoot, _skipRect;
        private readonly CanvasGroup _introGroup;
        private readonly Text _title, _line, _caption;
        private readonly Text _skipPracticeLabel;
        private readonly Image _nubi;

        /// <summary>true si la persona tocó «Saltar tutorial» (en la tarjeta o durante la ronda guiada).</summary>
        public bool Skipped { get; private set; }
        /// <summary>true mientras la ronda guiada está en curso (el rótulo y el botón de saltar están a la vista).</summary>
        public bool Practicing { get; private set; }

        /// <param name="parent">El área segura del juego (el tutorial se dibuja encima de todo lo demás).</param>
        /// <param name="practiceTopU">Distancia (unidades del lienzo) desde arriba a la que va el rótulo «Práctica: no cuenta»: debajo del marcador de cada juego.</param>
        public GuidedTutorial(RectTransform parent, float practiceTopU)
        {
            // ---- tarjeta de entrada
            var introGo = new GameObject("TutorialIntro");
            introGo.transform.SetParent(parent, false);
            _intro = introGo.AddComponent<RectTransform>();
            Stretch(_intro);
            _introGroup = introGo.AddComponent<CanvasGroup>();
            var dim = introGo.AddComponent<Image>();
            dim.color = new Color(0.008f, 0.012f, 0.06f, 0.78f);
            dim.raycastTarget = false;

            var card = Panel(_intro, "Card", new Vector2(930f, 1240f), Vector2.zero, NeuroStyle.Surface, 7f, 18f);
            var nubi = new GameObject("Nubi");
            nubi.transform.SetParent(card, false);
            var nr = nubi.AddComponent<RectTransform>();
            nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0.5f);
            nr.sizeDelta = new Vector2(620f, 620f);
            nr.anchoredPosition = new Vector2(0f, 300f);
            _nubi = nubi.AddComponent<Image>();      // el dibujo se hornea al abrir la tarjeta por primera vez (no al armar el juego)
            _nubi.raycastTarget = false;

            _title = Label(card, "Title", 72, new Vector2(0f, -70f), new Vector2(840f, 110f), NeuroStyle.Sun, TextAnchor.MiddleCenter);
            BestFit(_title, 48);
            _line = Label(card, "Line", 54, new Vector2(0f, -240f), new Vector2(800f, 220f), Color.white, TextAnchor.MiddleCenter);
            BestFit(_line, 48);                      // ajuste de línea; si no cabe, achica hasta 16 sp: nunca se corta

            _startRect = Button(card, "Start", new Vector2(0f, -430f), new Vector2(720f, 150f), NeuroStyle.Sun, NeuroStyle.Ink, out var startLabel);
            _startRect.GetComponentInChildren<Text>().text = "Probar una ronda";
            BestFit(_startRect.GetComponentInChildren<Text>(), 48);
            _skipIntroRect = Panel(card, "SkipIntro", new Vector2(640f, 150f), new Vector2(0f, -560f), new Color(0f, 0f, 0f, 0f), 0f, 0f);
            var skipText = Label(_skipIntroRect, "Label", 54, Vector2.zero, new Vector2(640f, 150f), new Color(0.851f, 0.831f, 0.961f, 1f), TextAnchor.MiddleCenter);
            skipText.text = "Saltar tutorial";
            BestFit(skipText, 45);
            _intro.gameObject.SetActive(false);

            // ---- durante la ronda guiada: rótulo arriba, mensaje de Nubi y «Saltar tutorial» abajo
            var pgo = new GameObject("TutorialPractice");
            pgo.transform.SetParent(parent, false);
            _practiceRoot = pgo.AddComponent<RectTransform>();
            Stretch(_practiceRoot);

            var badge = Panel(_practiceRoot, "Badge", new Vector2(640f, 96f), Vector2.zero, NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.92f), 4f, 8f);
            badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1f);
            badge.anchoredPosition = new Vector2(0f, -practiceTopU - 48f);
            var badgeText = Label(badge, "Label", 48, Vector2.zero, new Vector2(620f, 96f), NeuroStyle.Grape, TextAnchor.MiddleCenter);
            badgeText.text = "Práctica: no cuenta";
            BestFit(badgeText, 45);

            _caption = Label(_practiceRoot, "Caption", 60, Vector2.zero, new Vector2(960f, 200f), Color.white, TextAnchor.MiddleCenter);
            _caption.rectTransform.anchorMin = _caption.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _caption.rectTransform.anchoredPosition = new Vector2(0f, 330f);
            BestFit(_caption, 48);
            NeuroStyle.ClayText(_caption, 3f, 4f);

            _skipRect = Panel(_practiceRoot, "Skip", new Vector2(640f, 132f), Vector2.zero, NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.92f), 4f, 8f);
            _skipRect.anchorMin = _skipRect.anchorMax = new Vector2(0.5f, 0f);
            _skipRect.anchoredPosition = new Vector2(0f, 12f * Dp + 66f);
            _skipPracticeLabel = Label(_skipRect, "Label", 54, Vector2.zero, new Vector2(620f, 132f), new Color(0.851f, 0.831f, 0.961f, 1f), TextAnchor.MiddleCenter);
            _skipPracticeLabel.text = "Saltar tutorial";
            BestFit(_skipPracticeLabel, 45);
            _practiceRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ tarjeta de entrada

        /// <summary>Muestra la tarjeta de entrada y espera: «Probar una ronda» sigue; «Saltar tutorial» deja <see cref="Skipped"/> en true.</summary>
        public IEnumerator Intro(string title, string line, string startLabel = "Probar una ronda")
        {
            Skipped = false;
            _title.text = title;
            _line.text = line;
            _startRect.GetComponentInChildren<Text>().text = startLabel;
            if (_nubi.sprite == null) _nubi.sprite = NubiTeacherSprite.Get();
            _intro.gameObject.SetActive(true);
            yield return Motion.Fade(_introGroup, 0f, 1f, Motion.Decorative ? 0.25f : Motion.FadeSeconds);
            bool wait = true;
            float guard = 0f; // un toque que ya venía de antes (la cuenta de «Listo») no cuenta
            while (wait)
            {
                guard += GameClock.RealDeltaTime;
                if (guard > 0.25f && TryPress(out Vector2 pos))
                {
                    if (Hit(_skipIntroRect, pos)) { Skipped = true; wait = false; }
                    else if (Hit(_startRect, pos)) wait = false;
                }
                yield return null;
            }
            yield return Motion.Fade(_introGroup, 1f, 0f, Motion.FadeSeconds);
            _intro.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ ronda guiada

        /// <summary>Empieza la ronda guiada: rótulo «Práctica: no cuenta» y botón «Saltar tutorial» a la vista.</summary>
        public void BeginPractice()
        {
            Practicing = true;
            _caption.text = "";
            _practiceRoot.gameObject.SetActive(true);
        }

        /// <summary>Lo que dice Nubi durante la práctica (arriba del botón de saltar). Vacío = nada.</summary>
        public void Say(string text) => _caption.text = text;

        public void EndPractice()
        {
            Practicing = false;
            _caption.text = "";
            _practiceRoot.gameObject.SetActive(false);
        }

        /// <summary>true si <paramref name="screenPos"/> cae en «Saltar tutorial» durante la ronda guiada (y entonces deja <see cref="Skipped"/> en true).
        /// El controlador lo consulta con cada toque mientras <see cref="Practicing"/>.</summary>
        public bool TrySkip(Vector2 screenPos)
        {
            if (!Practicing || !Hit(_skipRect, screenPos)) return false;
            Skipped = true;
            return true;
        }

        /// <summary>Cierra todo (tarjeta y ronda guiada).</summary>
        public void Hide()
        {
            Practicing = false;
            _intro.gameObject.SetActive(false);
            _practiceRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ toques

        /// <summary>El toque nuevo de este cuadro, en píxeles de pantalla (dedo o ratón).</summary>
        public static bool TryPress(out Vector2 pos)
        {
            pos = Vector2.zero;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase != TouchPhase.Began) return false;
                pos = t.position;
                return true;
            }
            if (!Input.GetMouseButtonDown(0)) return false;
            pos = Input.mousePosition;
            return true;
        }

        private static bool Hit(RectTransform rect, Vector2 screenPos) => RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);

        // ------------------------------------------------------------------ construcción

        private static RectTransform Panel(RectTransform parent, string name, Vector2 size, Vector2 pos, Color color, float border, float depth)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = size;
            r.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            if (border > 0f) NeuroStyle.ClayFrame(img, border, depth);
            return r;
        }

        private static RectTransform Button(RectTransform parent, string name, Vector2 pos, Vector2 size, Color fill, Color textColor, out Text label)
        {
            var r = Panel(parent, name, size, pos, fill, 6f, 14f);
            label = Label(r, "Label", 62, Vector2.zero, size, textColor, TextAnchor.MiddleCenter);
            return r;
        }

        private static Text Label(RectTransform parent, string name, int fontPx, Vector2 pos, Vector2 size, Color color, TextAnchor anchor)
        {
            var t = MakeText(parent, name, fontPx, anchor, color, 0f, 0f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = size;
            r.anchoredPosition = pos;
            return t;
        }
    }
}
