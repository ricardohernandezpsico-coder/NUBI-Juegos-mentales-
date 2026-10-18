using System;
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
    /// el rótulo «Práctica (sin puntos)» y el botón «Saltar tutorial» siempre visible; <see cref="Say"/> escribe lo que Nubi dice («Casi: esa no era…»).</item>
    /// </list>
    /// La ronda guiada NO cuenta: ni puntos, ni DDA, ni avance, ni rachas, y no se guarda como partida: el juego la juega sin pasarla por su motor.
    /// Se maneja con toques directos (sin <c>Button</c>), igual que el resto de los controles de Unity. Con «quitar animaciones» solo hay fundidos.
    /// </summary>
    public sealed class GuidedTutorial
    {
        private const float Dp = 3f;
        /// <summary>La fila fija de los controles de la práctica (Tarea 127, §1.7): SIEMPRE arriba, debajo del marcador, en los 17 juegos: «Práctica (sin puntos)» a la izquierda y «Saltar tutorial» a la derecha (es lo que menos se toca: no necesita el pulgar).</summary>
        public const float RowMarginU = 48f, BadgeW = 560f, BadgeH = 96f, SkipW = 410f, SkipH = 132f, RowGapU = 8f;
        /// <summary>Durante la ronda guiada, lo que baja el contenido de un juego que arranca justo bajo el marcador (en dp lógicos de 360 de ancho) para no quedar bajo la fila fija: la fila llega a unos 113 dp y el marcador deja 66, así que 48 dp. Lo usan los juegos con layout propio (<c>topInset</c>).</summary>
        public const float RowInsetDp = 48f, RowInsetU = RowInsetDp * Dp;
        /// <summary>Donde termina la fila fija del tutorial (dp desde arriba): la fila parte 10 unidades bajo el marcador (<c>GameHud.Height + 10</c>, lo que pasan los 17 juegos), deja <see cref="RowGapU"/> y mide <see cref="SkipH"/>.</summary>
        public const float RowBottomDp = (GameHud.Height + 10f + RowGapU + SkipH) / Dp;

        private readonly RectTransform _intro, _startRect, _skipIntroRect, _practiceRoot, _skipRect, _badge;
        private readonly CanvasGroup _introGroup;
        private readonly Text _title, _line, _caption;
        private readonly Text _skipPracticeLabel;
        private readonly Image _nubi;

#if UNITY_EDITOR
        /// <summary>SOLO EN EL EDITOR (smoke test): la tarjeta de entrada sigue sola al segundo, como si se hubiera tocado «Probar una ronda». Así el arranque de
        /// prueba llega hasta la ronda guiada de cada juego. En el teléfono no existe.</summary>
        public static bool EditorAutoContinue;
        /// <summary>SOLO EN EL EDITOR (smoke de «Engranajes»): la partida real (sin tutorial) se juega sola, para que el arranque de prueba pase por todo el recorrido de una máquina (armar, cambiar,
        /// arrancar, veredicto, luz del cohete). En el teléfono no existe.</summary>
        public static bool EditorAutoPlayGame;
        /// <summary>SOLO EN EL EDITOR (smoke): un toque «de verdad» en <see cref="EditorPressPos"/> (píxeles de pantalla) que ven TODOS los que leen toques durante el cuadro <see cref="EditorPressFrame"/>,
        /// como un dedo. Sirve para comprobar que lo que un paso del tutorial ilumina se puede tocar. En el teléfono no existe.</summary>
        public static Vector2 EditorPressPos;
        public static int EditorPressFrame = -1;
#endif

        /// <summary>SOLO EN EL EDITOR (smoke de «Engranajes»): true cuando la partida real lleva más de 1,5 s esperando la jugada y el arranque de prueba pide jugarla sola. En el teléfono siempre da false.</summary>
        public static bool AutoPlayGame(float waitedSeconds)
        {
#if UNITY_EDITOR
            return EditorAutoPlayGame && waitedSeconds > 1.5f;
#else
            return false;
#endif
        }

        /// <summary>SOLO EN EL EDITOR (smoke): true cuando el juego lleva más de 2 s esperando la jugada de la persona en la ronda guiada; entonces el controlador la hace solo, para que el
        /// arranque de prueba pase por todos los pasos del tutorial (y se pueda revisar cada uno). En el teléfono siempre da false.</summary>
        public static bool AutoPlay(float waitedSeconds)
        {
#if UNITY_EDITOR
            return EditorAutoContinue && waitedSeconds > 2f;
#else
            return false;
#endif
        }

        /// <summary>«Nubi entrenadora»: el foco que ilumina SOLO lo que hay que tocar y la deja hablar en un globo (ver <see cref="NubiCoach"/>).</summary>
        public NubiCoach Coach { get; }

        /// <summary>true si la persona tocó «Saltar tutorial» (en la tarjeta o durante la ronda guiada).</summary>
        public bool Skipped { get; private set; }
        /// <summary>true mientras la ronda guiada está en curso (el rótulo y el botón de saltar están a la vista).</summary>
        public bool Practicing { get; private set; }

        /// <param name="parent">El área segura del juego (el tutorial se dibuja encima de todo lo demás).</param>
        /// <param name="practiceTopU">Distancia (unidades del lienzo) desde arriba a la que va el rótulo «Práctica (sin puntos)»: debajo del marcador de cada juego.</param>
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

            // plasticina: la tarjeta es una lámina crema (la del kit de la app) con el texto en tinta; sin ella, la superficie azul noche de siempre
            bool plastic = PlasticSlab.Available(SlabSize.Panel, SlabTone.Cream);
            var card = Panel(_intro, "Card", new Vector2(930f, 1240f), Vector2.zero, NeuroStyle.Surface, 7f, 18f, SlabSize.Panel, SlabTone.Cream);
            var nubi = new GameObject("Nubi");
            nubi.transform.SetParent(card, false);
            var nr = nubi.AddComponent<RectTransform>();
            nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0.5f);
            nr.sizeDelta = new Vector2(620f, 620f);
            nr.anchoredPosition = new Vector2(0f, 300f);
            _nubi = nubi.AddComponent<Image>();      // el dibujo se hornea al abrir la tarjeta por primera vez (no al armar el juego)
            _nubi.raycastTarget = false;

            _title = Label(card, "Title", 72, new Vector2(0f, -70f), new Vector2(840f, 110f), plastic ? NeuroStyle.Ink : NeuroStyle.Sun, TextAnchor.MiddleCenter);
            BestFit(_title, 48);
            _line = Label(card, "Line", 54, new Vector2(0f, -240f), new Vector2(800f, 220f), plastic ? NeuroStyle.Ink : Color.white, TextAnchor.MiddleCenter);
            BestFit(_line, 48);                      // ajuste de línea; si no cabe, achica hasta 16 sp: nunca se corta

            _startRect = Button(card, "Start", new Vector2(0f, -430f), new Vector2(720f, 150f), NeuroStyle.Sun, NeuroStyle.Ink, out var startLabel, SlabTone.Sun);
            _startRect.GetComponentInChildren<Text>().text = "Probar una ronda";
            BestFit(_startRect.GetComponentInChildren<Text>(), 48);
            _skipIntroRect = Panel(card, "SkipIntro", new Vector2(640f, 150f), new Vector2(0f, -560f), new Color(0f, 0f, 0f, 0f), 0f, 0f);
            var skipText = Label(_skipIntroRect, "Label", 54, Vector2.zero, new Vector2(640f, 150f), plastic ? NeuroStyle.InkSoft : new Color(0.851f, 0.831f, 0.961f, 1f), TextAnchor.MiddleCenter);
            skipText.text = "Saltar tutorial";
            BestFit(skipText, 48);
            _intro.gameObject.SetActive(false);

            // ---- «Nubi entrenadora»: el foco (velo con hueco + Nubi y su globo). Va debajo del rótulo y de «Saltar tutorial», que siguen a la vista.
            Coach = NubiCoach.Create(parent, p => Practicing && Hit(_skipRect, p), () => Skipped);

            // ---- durante la ronda guiada: rótulo arriba, mensaje de Nubi y «Saltar tutorial» abajo
            var pgo = new GameObject("TutorialPractice");
            pgo.transform.SetParent(parent, false);
            _practiceRoot = pgo.AddComponent<RectTransform>();
            Stretch(_practiceRoot);

            var badge = Panel(_practiceRoot, "Badge", new Vector2(BadgeW, BadgeH), Vector2.zero, NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.92f), 4f, 8f, SlabSize.Chip, SlabTone.Cream);
            _badge = badge;
            var badgeText = Label(badge, "Label", 48, Vector2.zero, new Vector2(BadgeW - 20f, BadgeH), plastic ? NeuroStyle.Ink : NeuroStyle.Grape, TextAnchor.MiddleCenter);       // 15 dp: «Práctica (sin puntos)» es más larga que el «Práctica: no cuenta» de antes y a 16 dp rozaba un texto del juego (Correo, forma de 1920)
            badgeText.text = "Práctica (sin puntos)";
            BestFit(badgeText, 48);

            _caption = Label(_practiceRoot, "Caption", 60, Vector2.zero, new Vector2(960f, 200f), Color.white, TextAnchor.MiddleCenter);
            _caption.rectTransform.anchorMin = _caption.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _caption.rectTransform.anchoredPosition = new Vector2(0f, 330f);
            BestFit(_caption, 48);
            NeuroStyle.ClayText(_caption, 3f, 4f);

            _skipRect = Panel(_practiceRoot, "Skip", new Vector2(SkipW, SkipH), Vector2.zero, NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.92f), 4f, 8f, SlabSize.Button, SlabTone.Cream);
            _skipPracticeLabel = Label(_skipRect, "Label", 48, Vector2.zero, new Vector2(SkipW - 20f, SkipH), plastic ? NeuroStyle.Ink : new Color(0.851f, 0.831f, 0.961f, 1f), TextAnchor.MiddleCenter);
            _skipPracticeLabel.text = "Saltar tutorial";
            BestFit(_skipPracticeLabel, 48);
            PlaceRow(practiceTopU);
            Coach.ControlSkip = _skipRect;       // Nubi y su globo no los tapan, estén donde estén
            Coach.ControlBadge = _badge;
            _practiceRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// La fila fija de los controles (Tarea 127): «Práctica (sin puntos)» arriba a la izquierda y «Saltar tutorial» arriba a la derecha, en UNA fila debajo del marcador (<paramref name="practiceTopU"/> unidades desde arriba), igual en los 17 juegos.
        /// Antes cambiaban de lugar según el juego (abajo en unos, arriba en otros, partidos en Rastro).
        /// </summary>
        private void PlaceRow(float practiceTopU)
        {
            float y = -(practiceTopU + RowGapU + SkipH * 0.5f);
            _badge.anchorMin = _badge.anchorMax = new Vector2(0f, 1f);
            _badge.pivot = new Vector2(0f, 0.5f);
            _badge.anchoredPosition = new Vector2(RowMarginU, y);
            _skipRect.anchorMin = _skipRect.anchorMax = new Vector2(1f, 1f);
            _skipRect.pivot = new Vector2(1f, 0.5f);
            _skipRect.anchoredPosition = new Vector2(-RowMarginU, y);
        }

        /// <summary>
        /// Acomoda lo de la ronda guiada: los controles van SIEMPRE en la fila fija de arriba (los parámetros <paramref name="skipAtTop"/>, <paramref name="badgeAtBottom"/> y <paramref name="skipFromBottomU"/> quedaron de cuando cada juego los ponía donde cabían y ya no hacen nada)
        /// y el mensaje de Nubi va a <paramref name="captionFromBottomU"/> unidades del borde de abajo (-1 = donde está por defecto).
        /// </summary>
        public void PlaceControls(float practiceTopU, bool skipAtTop, float captionFromBottomU, bool badgeAtBottom = false, float skipFromBottomU = -1f)
        {
            PlaceRow(practiceTopU);
            if (captionFromBottomU >= 0f) _caption.rectTransform.anchoredPosition = new Vector2(0f, captionFromBottomU);
        }

        // ------------------------------------------------------------------ tarjeta de entrada

        /// <summary>Muestra la tarjeta de entrada y espera: «Probar una ronda» sigue; «Saltar tutorial» deja <see cref="Skipped"/> en true.</summary>
        public IEnumerator Intro(string title, string line, string startLabel = "Probar una ronda")
        {
            Skipped = false;
            _title.text = title;
            _line.text = line;
            _startRect.GetComponentInChildren<Text>().text = startLabel;
            if (_nubi.sprite == null) { _nubi.sprite = NubiTeacherSprite.Get(); _nubi.preserveAspect = true; }       // la Nubi 3D no es cuadrada
            _intro.gameObject.SetActive(true);
            yield return Motion.Fade(_introGroup, 0f, 1f, Motion.Decorative ? 0.25f : Motion.FadeSeconds);
            bool wait = true;
            float guard = 0f; // un toque que ya venía de antes (la cuenta de «Listo») no cuenta
            while (wait)
            {
                guard += GameClock.RealDeltaTime;
#if UNITY_EDITOR
                if (EditorAutoContinue && guard > 1f) break;
#endif
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

        /// <summary>Empieza la ronda guiada: rótulo «Práctica (sin puntos)» y botón «Saltar tutorial» a la vista.</summary>
        public void BeginPractice()
        {
            Practicing = true;
            _caption.text = "";
            _practiceRoot.gameObject.SetActive(true);
        }

        /// <summary>Lo que dice Nubi durante la práctica (arriba del botón de saltar). Vacío = nada.</summary>
        public void Say(string text) => _caption.text = text;

        /// <summary>
        /// «Aprender haciendo» (Ricardo, 3-oct): Nubi dice <paramref name="text"/> y se QUEDA puesto hasta que la persona toque la pantalla (en cualquier parte menos «Saltar tutorial»).
        /// Nada avanza por tiempo. Debajo del texto va <paramref name="cue"/> («Toca para seguir», «Toca para empezar»…); al tocar, la explicación se queda sin la invitación.
        /// Sin <paramref name="text"/> solo espera el toque (el que llama ya dijo lo suyo). Un toque que ya venía de antes no cuenta (0,3 s), ni uno mientras el juego está en pausa.
        /// En el Editor, con <see cref="EditorAutoContinue"/> (el smoke), sigue solo a los 1,2 s: así los arranques de prueba llegan hasta el final de la ronda guiada.
        /// </summary>
        public IEnumerator WaitForContinue(string text = null, string cue = "Toca para seguir")
        {
            if (text != null) Say(text + "\n<color=#FFC93C>" + cue + "</color>");
            float waited = 0f;
            while (!Skipped)
            {
                waited += GameClock.RealDeltaTime;
#if UNITY_EDITOR
                if (EditorAutoContinue && waited > 1.2f) break;
#endif
                if (waited > 0.3f && !GameClock.Paused && TryPress(out Vector2 pos) && !Hit(_skipRect, pos)) break;
                yield return null;
            }
            if (text != null) Say(text);
        }

        public void EndPractice()
        {
            Practicing = false;
            Coach.Hide();
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
            Coach.Hide();
            _intro.gameObject.SetActive(false);
            _practiceRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ toques

        /// <summary>El toque nuevo de este cuadro, en píxeles de pantalla (dedo o ratón).</summary>
        public static bool TryPress(out Vector2 pos)
        {
            pos = Vector2.zero;
#if UNITY_EDITOR
            if (EditorPressFrame == Time.frameCount) { pos = EditorPressPos; return true; }
#endif
            if (Input.touchCount > 0)
            {
                // cualquier dedo que acaba de apoyarse (un dedo o la palma que ya estaban en la pantalla no esconden el toque nuevo)
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase != TouchPhase.Began) continue;
                    pos = t.position;
                    return true;
                }
                return false;
            }
            if (!Input.GetMouseButtonDown(0)) return false;
            pos = Input.mousePosition;
            return true;
        }

        private static bool Hit(RectTransform rect, Vector2 screenPos) => RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);

        // ------------------------------------------------------------------ aro de ayuda

        /// <summary>El aro sol punteado que marca «aquí» en la ronda guiada: creado e inactivo; el juego lo coloca, lo muestra con SetActive y lo hace girar con
        /// <see cref="SpinHint"/>.</summary>
        public static Image CreateHintRing(Transform parent, float sizeU)
        {
            var go = new GameObject("HintRing");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(sizeU, sizeU);
            var img = go.AddComponent<Image>();
            img.sprite = RastroSprites.DashedRing();
            img.color = NeuroStyle.Sun;
            img.raycastTarget = false;
            go.SetActive(false);
            return img;
        }

        /// <summary>Hace girar despacio el aro de ayuda (quieto con «quitar animaciones»).</summary>
        public static void SpinHint(Image ring)
        {
            if (ring == null || !ring.gameObject.activeSelf) return;
            ring.rectTransform.localEulerAngles = new Vector3(0f, 0f, Motion.Decorative ? -GameClock.Time * 25f : 0f);
        }

        // ------------------------------------------------------------------ construcción

        private static RectTransform Panel(RectTransform parent, string name, Vector2 size, Vector2 pos, Color color, float border, float depth, SlabSize? slabSize = null, SlabTone slabTone = SlabTone.Cream)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = size;
            r.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            if (slabSize.HasValue && PlasticSlab.TryStyle(img, slabSize.Value, slabTone) != null) return r;     // la losa de plasticina; sin ella, lo de siempre
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            if (border > 0f) NeuroStyle.ClayFrame(img, border, depth);
            return r;
        }

        private static RectTransform Button(RectTransform parent, string name, Vector2 pos, Vector2 size, Color fill, Color textColor, out Text label, SlabTone slabTone = SlabTone.Sun)
        {
            var r = Panel(parent, name, size, pos, fill, 6f, 14f, SlabSize.Button, slabTone);
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

    /// <summary>
    /// El guion de una ronda guiada (puro, con pruebas): qué paso toca, cuántos van, cuántos errores hubo y si se saltó. Un error repite el MISMO paso (con la
    /// regla explicada, sin culpa); un acierto pasa al siguiente; «Saltar tutorial» termina de una vez y lleva a la partida. NO tiene puntos, rachas ni DDA: la
    /// ronda guiada no cuenta.
    /// </summary>
    public sealed class GuidedScript
    {
        public int Count { get; }
        public int Index { get; private set; }
        public int Failures { get; private set; }
        public bool Skipped { get; private set; }
        public GuidedScript(int steps) { Count = steps; }

        /// <summary>true si ya no queda nada por jugar (se acabaron los pasos o se saltó).</summary>
        public bool Finished => Skipped || Index >= Count;
        public void Success() { if (!Finished) Index++; }
        public void Failure() { if (!Finished) Failures++; }
        public void Skip() { Skipped = true; }
    }

    /// <summary>
    /// El reloj de «Cómo se juega» desde la pausa (puro, con pruebas): el tutorial corre con el reloj de juego andando, y al terminar se sabe cuánto duró
    /// (<see cref="Finish"/>) para correr hacia adelante cada ancla de tiempo del juego (el final del Reto, el inicio del reloj) con <see cref="Shift"/>: así el
    /// tiempo que quedaba antes de abrir «Cómo se juega» es el mismo que queda después.
    /// </summary>
    public readonly struct HowToClock
    {
        private readonly float _startedAt;
        public HowToClock(float gameTime) { _startedAt = gameTime; }

        /// <summary>Segundos que duró el tutorial (nunca negativo).</summary>
        public float Finish(float gameTime) => Math.Max(0f, gameTime - _startedAt);

        /// <summary>Corre un ancla de tiempo (un instante del reloj de juego) hacia adelante lo que duró el tutorial.</summary>
        public static float Shift(float anchor, float spentSeconds) => anchor + spentSeconds;
    }
}

