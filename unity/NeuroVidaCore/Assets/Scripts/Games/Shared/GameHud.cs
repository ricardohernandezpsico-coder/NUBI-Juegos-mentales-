using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia; // RoundedRectSprite
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Marcador común (la franja de arriba) de los juegos, con una jerarquía clara de DOS filas (Tarea 127, <c>auditoria-fable/pulido-juegos.md</c> §1.1 y §1.2):
    /// - <b>Fila 1</b>: el título del juego a 18 dp a la izquierda y el AVANCE de la partida a la derecha («3 de 12», «1.250 pts»), en un chip crema de 16 dp. «Nivel N» ya no se ve durante la partida (queda en el resultado y en la marca de depuración de la cuenta
    ///   regresiva): era el nivel interno del ajuste de dificultad, que no se compara entre juegos.
    /// - <b>Fila 2</b>: la fila de CONSIGNA, una losa «noche» del kit de 16-17 dp con lo que hay que hacer («Lanza el cohete que se enciende», «Responde: TINTA»), un ícono opcional a la izquierda y una FASE opcional a la derecha («Mira el rastro» → «Repítelo»).
    ///   Un solo lugar, siempre el mismo (reconocer, no recordar): los juegos que la ponían flotando, al pie o en un panel la mueven aquí. Una sola línea: el texto se achica hasta 16 dp y no se parte (los textos son cortos a propósito).
    /// - Los aciertos seguidos ya no son una píldora grande siempre a la vista (casi siempre decía «0»): un chip «×4 seguidos» que aparece SOLO desde 2, justo debajo del marcador a la derecha, rebota al crecer y en cada múltiplo de 5 suelta destellos.
    /// - Los AVISOS del juego («¡Un paso más!», «Con calma», «Era su espejo»…) los dice la misma fila de consigna (<see cref="Announce"/>, Tarea 179): se tiñe con el tono, cambia su ícono, dice el aviso en una línea y vuelve sola; no hay carteles flotando.
    /// Subir de nivel hace saltar el chip de avance (con una chispa redonda de sol). Mismo alto que antes (190): no mueve nada de abajo.
    /// </summary>
    public sealed class GameHud
    {
        public const float Height = 190f;
        private const float ChipH = 72f;          // los chips llevan letra de 16 dp (48 unidades): Tarea 123 (antes 14 dp y 64)
        private const float ChipPad = 26f;
        private const float TitleTop = 8f, TitleH = 70f;
        private const float PromptTop = 90f, PromptH = 94f;
        /// <summary>Tamaño de la letra de la consigna: 17 dp (51 unidades); se achica hasta 16 dp (48) para no partirse.</summary>
        public const int PromptSize = 51, PromptMin = 48;
        /// <summary>El chip de «seguidos» aparece desde esta cantidad.</summary>
        public const int StreakFrom = 2;

        public RectTransform Rect { get; }
        /// <summary>La fila de consigna (la losa de la fila 2).</summary>
        public RectTransform PromptRect { get; }
        /// <summary>El chip de avance de la fila 1 (Rastro de luz hace volar hacia él las luces recordadas).</summary>
        public RectTransform InfoRect => _infoChip;

        private readonly MonoBehaviour _runner;
        private readonly float _margin;
        private readonly RectTransform _infoChip, _streakChip;
        private readonly Text _titleText, _infoText, _streakText, _promptText, _phaseText;
        private readonly Image _infoBg, _promptBg, _promptIcon;
        /// <summary>Las losas de plasticina de las pastillas (null = el arcilla de siempre): avance, consigna y seguidos.</summary>
        private readonly Image _infoSlab, _promptSlab, _streakSlab;
        private int _level = -1;
        private int _shownPoints = -1;
        private int _targetPoints;
        private int _streak;
        private Coroutine _countUp;

        public GameHud(RectTransform parent, string title, float marginU, MonoBehaviour runner, bool withStreak = true, float rightReserve = 0f)
        {
            _runner = runner;
            _margin = marginU;

            var go = new GameObject("Hud");
            go.transform.SetParent(parent, false);
            Rect = go.AddComponent<RectTransform>();
            Rect.anchorMin = new Vector2(0f, 1f);
            Rect.anchorMax = new Vector2(1f, 1f);
            Rect.pivot = new Vector2(0.5f, 1f);
            Rect.sizeDelta = new Vector2(0f, Height);
            Rect.anchoredPosition = Vector2.zero;
            go.AddComponent<AnnounceTicker>().Hud = this;                    // el reloj de los avisos vive en el marcador, no en el juego: si el juego detiene sus corrutinas, la fila igual vuelve a su consigna

            // fila 1: el título a 18 dp (54 unidades); se achica hasta 16 dp solo si el chip de avance le deja poco lugar (el título más largo, «En la punta de la lengua»)
            _titleText = MakeText(Rect, "Title", 54, TextAnchor.MiddleLeft, Color.white, 3f, 0.45f);
            PlaceTopText(_titleText, marginU, marginU + 160f, -TitleTop, TitleH);
            BestFit(_titleText, 48);
            _titleText.text = title;

            _infoChip = Chip("InfoChip", SlabTone.Cream, out _infoBg, out _infoSlab, out _infoText);
            _infoChip.gameObject.SetActive(false);

            // fila 2: la consigna (losa «noche» del kit con el texto crema; sin plasticina, la superficie azul noche de siempre)
            var pgo = new GameObject("Prompt");
            pgo.transform.SetParent(Rect, false);
            PromptRect = pgo.AddComponent<RectTransform>();
            PromptRect.anchorMin = new Vector2(0f, 1f);
            PromptRect.anchorMax = new Vector2(1f, 1f);
            PromptRect.pivot = new Vector2(0.5f, 1f);
            PromptRect.offsetMin = new Vector2(marginU, -(PromptTop + PromptH));
            PromptRect.offsetMax = new Vector2(-marginU, -PromptTop);
            _promptBg = pgo.AddComponent<Image>();
            _promptSlab = PlasticSlab.TryStyle(_promptBg, SlabSize.Chip, SlabTone.Night);
            if (_promptSlab == null)
            {
                _promptBg.sprite = RoundedRectSprite.Get(64);
                _promptBg.type = Image.Type.Sliced;
                _promptBg.color = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.9f);
                _promptBg.raycastTarget = false;
                NeuroStyle.ClayFrame(_promptBg, 3f, 6f);
            }
            var igo = new GameObject("Icon");
            igo.transform.SetParent(PromptRect, false);
            var ir = igo.AddComponent<RectTransform>();
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0f, 0.5f);
            ir.sizeDelta = new Vector2(60f, 60f);
            ir.anchoredPosition = new Vector2(26f, 0f);
            _promptIcon = igo.AddComponent<Image>();
            _promptIcon.raycastTarget = false;
            _promptIcon.preserveAspect = true;
            igo.SetActive(false);
            _promptText = MakeText(PromptRect, "Text", PromptSize, TextAnchor.MiddleLeft, NeuroStyle.Cream, 0f, 0f);
            _promptText.supportRichText = true;
            BestFit(_promptText, PromptMin);
            _phaseText = MakeText(PromptRect, "Phase", PromptSize, TextAnchor.MiddleRight, NeuroStyle.Sun, 0f, 0f);
            _phaseText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _phaseText.verticalOverflow = VerticalWrapMode.Overflow;
            _phaseText.gameObject.SetActive(false);
            PromptRect.gameObject.SetActive(false);                      // sin consigna no hay fila: los juegos que todavía la tienen en otro lugar no ven una losa vacía
            LayoutPrompt();

            if (withStreak)
            {
                // el chip de «seguidos»: debajo del marcador, a la derecha, solo desde 2; es hermano del marcador (no hijo) para dibujarse encima del juego cuando aparece
                var sgo = new GameObject("StreakChip");
                sgo.transform.SetParent(parent, false);
                _streakChip = sgo.AddComponent<RectTransform>();
                _streakChip.anchorMin = _streakChip.anchorMax = _streakChip.pivot = new Vector2(1f, 1f);
                _streakChip.sizeDelta = new Vector2(300f, ChipH);
                _streakChip.anchoredPosition = new Vector2(-marginU, -(Height + 36f));             // debajo de la franja fina que muchos juegos ponen pegada al marcador (el viaje de Piloto, el reloj del Reto)
                var bg = sgo.AddComponent<Image>();
                _streakSlab = PlasticSlab.TryStyle(bg, SlabSize.Chip, SlabTone.Sun);
                if (_streakSlab == null)
                {
                    bg.sprite = RoundedRectSprite.Get(64);
                    bg.type = Image.Type.Sliced;
                    bg.color = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.92f);
                    bg.raycastTarget = false;
                    NeuroStyle.ClayFrame(bg, 3f, 6f);
                }
                _streakText = MakeText(_streakChip, "Text", 48, TextAnchor.MiddleCenter, _streakSlab != null ? NeuroStyle.Ink : NeuroStyle.Sun, 0f, 0f);       // 16 dp (48 unidades)
                sgo.SetActive(false);
            }
        }

        /// <summary>Una pastilla: losa chica de plasticina del color [tone] con el texto en tinta o, sin plasticina, el chip oscuro de siempre con el texto de color.</summary>
        private RectTransform Chip(string name, SlabTone tone, out Image bg, out Image slab, out Text text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Rect, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(160f, ChipH);
            rect.anchoredPosition = new Vector2(-_margin, -(TitleTop + 1f));
            bg = go.AddComponent<Image>();
            slab = PlasticSlab.TryStyle(bg, SlabSize.Chip, tone);
            if (slab == null)
            {
                bg.sprite = RoundedRectSprite.Get(64);
                bg.type = Image.Type.Sliced;
                bg.color = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.9f);
                bg.raycastTarget = false;
                NeuroStyle.ClayFrame(bg, 3f, 6f);
            }
            text = MakeText(rect, "Text", 48, TextAnchor.MiddleCenter, slab != null ? NeuroStyle.Ink : NeuroStyle.Sun, 0f, 0f);       // 16 dp (48 unidades): los chips del marcador no bajan de ahí (HudRulesTests y el smoke)
            return rect;
        }

        /// <summary>Ajusta el ancho del chip de avance a su texto y le deja el resto de la fila 1 al título.</summary>
        private void LayoutChips()
        {
            float w = 0f;
            if (_infoChip.gameObject.activeSelf)
            {
                w = Mathf.Ceil(_infoText.preferredWidth) + ChipPad * 2f;
                _infoChip.sizeDelta = new Vector2(w, ChipH);
                _infoChip.anchoredPosition = new Vector2(-_margin, -(TitleTop + 1f));
            }
            PlaceTopText(_titleText, _margin, _margin + (w > 0f ? w + 20f : 0f), -TitleTop, TitleH);
        }

        /// <summary>Reparte la fila de consigna: el ícono (si hay), la fase a la derecha (si hay) y el texto en el resto.</summary>
        private void LayoutPrompt()
        {
            float left = _promptIcon.gameObject.activeSelf ? 26f + 60f + 14f : 28f;
            float right = 24f;
            if (_phaseText.gameObject.activeSelf)
            {
                var pr = _phaseText.rectTransform;
                float w = Mathf.Ceil(_phaseText.preferredWidth);
                pr.anchorMin = new Vector2(1f, 0f);
                pr.anchorMax = new Vector2(1f, 1f);
                pr.pivot = new Vector2(1f, 0.5f);
                pr.sizeDelta = new Vector2(w + 4f, 0f);
                pr.anchoredPosition = new Vector2(-24f, 0f);
                right = 24f + w + 28f;
            }
            var tr = _promptText.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(left, 4f);
            tr.offsetMax = new Vector2(-right, -4f);
        }

        // ------------------------------------------------------------------ API

        /// <summary>
        /// El nivel del ajuste de dificultad. Ya NO se dibuja (Tarea 127): solo se recuerda para saber cuándo SUBE, y entonces el chip de avance salta y suelta una chispa de sol (el aviso «Nivel 2» y el sonido los pone cada juego).
        /// </summary>
        public void SetLevel(int level)
        {
            bool up = _level >= 0 && level > _level;
            _level = level;
            if (up) _runner.StartCoroutine(LevelUpGlow());
        }

        /// <summary>Puntos del modo Reto: el número cuenta hacia arriba en ~0,3 s.</summary>
        public void SetPoints(int points)
        {
            _infoChip.gameObject.SetActive(true);
            _targetPoints = points;
            if (_shownPoints < 0 || points < _shownPoints || !Motion.Decorative) // sin animaciones: el número final de una vez
            {
                if (_countUp != null) { _runner.StopCoroutine(_countUp); _countUp = null; }
                _shownPoints = points;
                _infoText.text = FormatPoints(points);
                LayoutChips();
                return;
            }
            if (_countUp != null) _runner.StopCoroutine(_countUp);
            _countUp = _runner.StartCoroutine(CountUp());
        }

        /// <summary>Texto libre del chip de avance ("3 de 12", "Tesoros 2/5").</summary>
        public void SetInfo(string text)
        {
            _infoChip.gameObject.SetActive(true);
            _shownPoints = -1;
            _infoText.text = text;
            LayoutChips();
        }

        /// <summary>
        /// Como <see cref="SetInfo"/> con varias versiones del texto, de la más completa a la más corta: pone la primera cuyo chip deja al título su ancho a la letra más chica (16 dp); si ninguna cabe, la última. Acoplamiento (Tarea 144):
        /// «Módulo 3 de 24 · 8 a bordo» → «3 de 24 · 8 a bordo» → «Módulo 3 · 8 a bordo». Nada se parte ni se corta: el título y el chip comparten la fila 1.
        /// </summary>
        public void SetInfoFit(params string[] candidates)
        {
            if (candidates == null || candidates.Length == 0) return;
            string chosen = candidates[candidates.Length - 1];
            foreach (var c in candidates) if (!string.IsNullOrEmpty(c) && InfoFits(c)) { chosen = c; break; }
            SetInfo(chosen);
        }

        /// <summary>¿Cabe el chip de avance con <paramref name="text"/> sin quitarle al título más de lo que necesita a 16 dp (la letra más chica que permite)?</summary>
        private bool InfoFits(string text)
        {
            bool best = _titleText.resizeTextForBestFit;
            int size = _titleText.fontSize;
            var wrap = _titleText.horizontalOverflow;
            _titleText.resizeTextForBestFit = false;
            _titleText.fontSize = 48;
            _titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            float titleW = Mathf.Ceil(_titleText.preferredWidth);
            _titleText.horizontalOverflow = wrap;
            _titleText.fontSize = size;
            _titleText.resizeTextForBestFit = best;
            string old = _infoText.text;
            _infoText.text = text;
            float chipW = Mathf.Ceil(_infoText.preferredWidth) + ChipPad * 2f;
            _infoText.text = old;
            return titleW + 20f + chipW <= 1080f - 2f * _margin;
        }

        /// <summary>El texto que muestra el chip de avance ahora (para las pruebas).</summary>
        public string InfoText => _infoText.text;

        /// <summary>
        /// La fila de consigna (fila 2): <paramref name="text"/> a la izquierda (admite <c>&lt;color&gt;</c>: la regla de Tinta va con el color de su orilla Y su palabra) y un <paramref name="icon"/> opcional (la gota o el libro: la forma además del color).
        /// <paramref name="sizeU"/> = tamaño de la letra en unidades (51 = 17 dp por defecto; 54 = 18 dp para Tinta). Una sola línea.
        /// </summary>
        public void SetPrompt(string text, Sprite icon = null, int sizeU = PromptSize)
        {
            if (_announcing)
            {
                // la consigna manda (Tarea 179): lo mismo que ya dice no interrumpe al aviso; una consigna distinta lo termina y ocupa su lugar
                bool hasI = icon != null && !string.IsNullOrEmpty(text);
                if ((text ?? "") == _snapText && (hasI ? icon : null) == (_snapIconOn ? _snapIcon : null)) return;
                EndAnnounce();
            }
            if (_promptText.fontSize != sizeU) _promptText.fontSize = sizeU;
            _promptText.resizeTextMaxSize = sizeU;
            _promptText.text = text ?? "";
            bool hasIcon = icon != null && !string.IsNullOrEmpty(text);
            _promptIcon.gameObject.SetActive(hasIcon);
            if (hasIcon) _promptIcon.sprite = icon;
            RefreshRow();
            LayoutPrompt();
        }

        /// <summary>La fila se ve si hay consigna o fase: sin ninguna de las dos no hay una losa vacía (los juegos que todavía tienen su consigna en otro lugar).</summary>
        private void RefreshRow() => PromptRect.gameObject.SetActive(!string.IsNullOrEmpty(_promptText.text) || _phaseText.gameObject.activeSelf);

        /// <summary>
        /// Como <see cref="SetPrompt"/> con varias versiones del texto, de la más completa a la más corta: pone la primera que cabe en UNA línea a 16 dp (si ninguna cabe, la última). Piloto Estelar: «Sector 1 de 3 · misión: triángulo con franja» →
        /// «Misión: triángulo con franja» → «triángulo con franja». Con rich text (<c>&lt;color&gt;</c>) se mide lo que se ve, sin las etiquetas.
        /// </summary>
        public void SetPromptFit(Sprite icon, params string[] candidates)
        {
            if (candidates == null || candidates.Length == 0) { SetPrompt(null); return; }
            string chosen = candidates[candidates.Length - 1];
            foreach (var c in candidates) if (!string.IsNullOrEmpty(c) && FitsOneLine(c, icon != null)) { chosen = c; break; }
            SetPrompt(chosen, icon);
        }

        /// <summary>¿Cabe <paramref name="text"/> en una línea de la fila de consigna a 16 dp (la letra más chica que permite)? Con el ícono y la fase que haya puestos.</summary>
        public bool FitsOneLine(string text, bool withIcon) => LineFits(text, withIcon, _phaseText.gameObject.activeSelf);

        private bool LineFits(string text, bool withIcon, bool withPhase)
        {
            bool best = _promptText.resizeTextForBestFit;
            int size = _promptText.fontSize;
            _promptText.resizeTextForBestFit = false;
            _promptText.fontSize = PromptMin;
            _promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            string old = _promptText.text;
            _promptText.text = text;
            float width = _promptText.preferredWidth;
            _promptText.text = old;
            _promptText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _promptText.fontSize = size;
            _promptText.resizeTextForBestFit = best;
            float left = withIcon ? 26f + 60f + 14f : 28f;
            float right = 24f + (withPhase ? Mathf.Ceil(_phaseText.preferredWidth) + 28f : 0f);
            return width <= 1080f - 2f * _margin - left - right;
        }

        /// <summary>Solo el ícono de la consigna (la regla de Tinta cambia de color y de ícono con cada palabra).</summary>
        public void SetPromptIcon(Sprite icon, Color color)
        {
            if (_announcing)
            {
                if (icon == (_snapIconOn ? _snapIcon : null) && (icon == null || color == _snapIconColor)) return;
                EndAnnounce();
            }
            _promptIcon.gameObject.SetActive(icon != null);
            if (icon != null) { _promptIcon.sprite = icon; _promptIcon.color = color; }
            LayoutPrompt();
        }

        /// <summary>La fase de la consigna, a la derecha de la misma fila («Mira el rastro» → «Repítelo»). Vacío = sin fase.</summary>
        public void SetPhase(string text)
        {
            if (_announcing)
            {
                // la fase (el reloj del Contrarreloj, «40 rescatadas», lo que dice Satélites) es información de apoyo, no la consigna: se guarda y vuelve con la fila; no termina el aviso
                _snapPhaseOn = !string.IsNullOrEmpty(text);
                _snapPhase = text ?? "";
                return;
            }
            bool show = !string.IsNullOrEmpty(text);
            _phaseText.gameObject.SetActive(show);
            if (show) _phaseText.text = text;
            RefreshRow();
            LayoutPrompt();
        }

        /// <summary>La consigna salta un poco y se enciende un momento (un sector nuevo, una misión nueva): sin «quitar animaciones», solo el color.</summary>
        public void PulsePrompt()
        {
            if (!_announcing) _runner.StartCoroutine(PromptPulse());                                   // mientras la fila habla, el aviso ya es el destello
        }

        /// <summary>El color del cuerpo de la fila de consigna (la losa «noche» del kit): contra él se mide que lo que va en color se lea (≥ 4,5:1). Medido sobre una captura real.</summary>
        public static readonly Color RowBody = new Color32(0x2F, 0x27, 0x5F, 0xFF);

        /// <summary>La consigna (mientras la fila habla, la de siempre: la que vuelve, no la del aviso).</summary>
        public string PromptText => _announcing ? _snapText : _promptText.text;
        /// <summary>El chip de «seguidos» (null en los juegos sin racha): su caja, para que la guardia del smoke mida que no tapa nada.</summary>
        public RectTransform StreakRect => _streakChip;
        public bool HasStreakChip => _streakChip != null;

        private float _streakFirst = Height + 36f;

        /// <summary>
        /// Desde dónde (unidades bajo el borde de arriba del contenedor) se empieza a buscar el lugar del chip «×N seguidos»: por omisión <c>Height + 36</c>, debajo de la franja fina que muchos juegos ponen pegada al marcador. Un juego con una pieza grande justo debajo (la tarjeta
        /// de Punta empieza en 312) lo sube para que el chip quepa antes de ella (Tarea 144).
        /// </summary>
        public void SetStreakTop(float topU) => _streakFirst = Mathf.Max(Height + 8f, topU);

        private readonly System.Collections.Generic.List<RectTransform> _avoidForStreak = new System.Collections.Generic.List<RectTransform>();

        /// <summary>
        /// Una zona propia del juego que el chip «×N seguidos» NUNCA cruza (Tarea 144, ítem 6 de la revisión del 19-oct), aunque en ese momento no tenga nada dibujado o esté a medio fundido: la tarjeta del pedido de Bodega, donde cae «¡Pedido perfecto!».
        /// Es como <see cref="Toast.KeepOut"/> pero para el chip: el chip se coloca en la franja libre más cercana de debajo del marcador, de preferencia justo debajo de la zona. Sin esto el chip se medía contra lo que estaba dibujado al aparecer
        /// y la tarjeta, que entra con un fundido, todavía no contaba. Hay que registrar el RECTÁNGULO de la pieza (no una capa que ocupe toda el área).
        /// </summary>
        public void AvoidForStreak(RectTransform zone)
        {
            if (zone != null && !_avoidForStreak.Contains(zone)) _avoidForStreak.Add(zone);
        }

        /// <summary>Las zonas que el juego declaró con <see cref="AvoidForStreak"/> (para la guardia del smoke y las pruebas).</summary>
        public System.Collections.Generic.IReadOnlyList<RectTransform> StreakAvoidZones => _avoidForStreak;
        public string PhaseText => _announcing ? (_snapPhaseOn ? _snapPhase : "") : _phaseText.gameObject.activeSelf ? _phaseText.text : "";
        public bool StreakVisible => _streakChip != null && _streakChip.gameObject.activeSelf;
        public string StreakText => _streakText != null ? _streakText.text : "";

        public void SetStreak(int streak) => SetStreak(streak, animate: true);

        private void SetStreak(int streak, bool animate)
        {
            if (_streakChip == null) return;
            bool grew = streak > _streak;
            _streak = streak;
            bool show = streak >= StreakFrom;
            bool wasShown = _streakChip.gameObject.activeSelf;
            _streakChip.gameObject.SetActive(show);
            if (!show) return;
            _streakText.text = streak + " seguidos";
            _streakChip.sizeDelta = new Vector2(Mathf.Ceil(_streakText.preferredWidth) + ChipPad * 2f, ChipH);
            if (!wasShown) _streakChip.SetAsLastSibling();                  // al aparecer se dibuja encima del juego (la zona de debajo del marcador, a la derecha)
            if (!wasShown || grew) PlaceStreakChip();
            if (!animate || !grew) return;
            bool milestone = streak % 5 == 0;
            _runner.StartCoroutine(BounceRect(_streakChip, milestone ? 0.7f : 0.85f, milestone ? 0.42f : 0.3f));       // «seguidos» rebota al crecer (Tarea 126); con «quitar animaciones» solo cambia el número
            if (milestone && Motion.Decorative)
            {
                var fx = (RectTransform)_streakChip.parent;
                Vector2 c = LocalIn(fx, _streakChip) + new Vector2(-_streakChip.rect.width * 0.5f, -_streakChip.rect.height * 0.5f);
                _runner.StartCoroutine(UiFx.SparkBurst(fx, c, NeuroStyle.Sun, 12, 180f, 30f, 0.55f));
            }
        }

        /// <summary>
        /// El chip de «seguidos» no debe tapar nada del juego (Tarea 127): busca su lugar como los avisos (<see cref="ToastPlacement"/>): de abajo del marcador hacia abajo, el primer lugar a la derecha donde no pisa ningún texto, botón ni estímulo (menos de 800 unidades cuadradas
        /// de choque); si no hay, el de menos choque. La guardia del smoke (<c>Seguidos*</c>) mide lo mismo en los juegos con racha.
        /// Desde la Tarea 144 las zonas que el juego registró con <see cref="AvoidForStreak"/> mandan sobre todo lo demás: un lugar que las pisa nunca gana a uno que no, y los lugares que se prueban incluyen «justo debajo de cada zona» (la franja libre más cercana).
        /// </summary>
        private void PlaceStreakChip()
        {
            var space = _streakChip.parent as RectTransform;
            if (space == null) return;
            float first = _streakFirst;
            var tops = new System.Collections.Generic.List<float>();
            var zones = new System.Collections.Generic.List<Rect>();
            // en orden de preferencia: justo debajo de cada zona registrada (12 unidades; si ahí no cabe sin pisar otra cosa, 4 y, por último, pegado), y después de arriba hacia abajo cada 44
            foreach (float gap in StreakZoneGaps)
                foreach (var z in _avoidForStreak)
                {
                    if (z == null) continue;
                    var zr = ToastPlacement.LocalRectOf(space, z);
                    if (gap == StreakZoneGaps[0]) zones.Add(zr);
                    if (zr.yMax + gap >= first) tops.Add(zr.yMax + gap);                      // la franja libre más cercana: justo debajo de la zona
                }
            for (float y = first; y <= first + 700f; y += 44f) tops.Add(y);
            float best = float.MaxValue, bestY = first;
            foreach (float y in tops)
            {
                _streakChip.anchoredPosition = new Vector2(-_margin, -y);
                var mine = ToastPlacement.LocalRectOf(space, _streakChip);
                float overlap = 0f, inZone = 0f;
                foreach (var box in ToastPlacement.Collect(space, _streakChip)) overlap += ToastPlacement.Overlap(mine, box);
                foreach (var zr in zones) inZone += ToastPlacement.Overlap(mine, zr);
                float score = inZone * 1000f + overlap;                                                  // pisar una zona registrada pesa mucho más que cualquier otro choque
                if (score < best) { best = score; bestY = y; }
                if (inZone <= 0f && overlap < 800f) break;
            }

            _streakChip.anchoredPosition = new Vector2(-_margin, -bestY);
#if UNITY_EDITOR
            if (best >= 800f)
            {
                // sin franja libre (lo que lo tapa, para entender al smoke: Tarea 144)
                var names = new System.Collections.Generic.List<string>();
                var boxes = ToastPlacement.Collect(space, _streakChip, names);
                var mine = ToastPlacement.LocalRectOf(space, _streakChip);
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < boxes.Count; i++) { float o = ToastPlacement.Overlap(mine, boxes[i]); if (o > 0f) sb.Append(names[i]).Append(" (").Append(Mathf.RoundToInt(o)).Append(" u²) "); }
                Debug.Log("[SmokeTest] seguidos: sin franja libre, el chip queda en " + Mathf.RoundToInt(bestY) + " (peor choque " + Mathf.RoundToInt(best) + "): " + sb);
            }
#endif
        }

        /// <summary>Aire entre una zona registrada con <see cref="AvoidForStreak"/> y el chip que se coloca justo debajo de ella (unidades), de lo preferido a lo mínimo.</summary>
        private static readonly float[] StreakZoneGaps = { 12f, 4f, 0f };

        // ------------------------------------------------------------------ los avisos: la fila habla (Tarea 179)

        /// <summary>Cuánto se ve un aviso de una línea (segundos): lo que dura hoy un aviso corto; con subtítulo, <see cref="SubtitleSeconds"/>.</summary>
        public const float LineSeconds = 1.2f;
        /// <summary>Cuánto se ve la segunda parte (el subtítulo) de un aviso en dos tiempos, o la línea con subtítulo.</summary>
        public const float SubtitleSeconds = 2f;
        /// <summary>Lo que dura la entrada y la vuelta de la consigna (con «quitar animaciones» no hay transición, pero se leen los mismos segundos).</summary>
        public const float AnnounceFade = 0.2f;
        /// <summary>Lo que separa el aviso de su subtítulo cuando caben en una línea.</summary>
        public const string SubtitleJoin = " · ";

        private bool _announcing;
        private AnnounceTone _annTone, _annSubTone;          // _annTone = el de la parte que se ve ahora
        private string[] _annBeats;
        private float[] _annHolds;
        private int _annIndex;
        private float _annT, _annFadeBack;
        // lo que la fila decía antes de hablar (vuelve sola)
        private string _snapText = "", _snapPhase = "";
        private int _snapSize = PromptSize;
        private Sprite _snapIcon;
        private Color _snapIconColor = Color.white;
        private bool _snapIconOn, _snapPhaseOn;

#if UNITY_EDITOR
        /// <summary>SOLO EN EL EDITOR (el smoke lo lee y lo vacía): los avisos mostrados y los que un aviso nuevo pisó antes de que se leyera su primera línea («texto → texto nuevo (tono)»).</summary>
        public static int AnnouncesShown;
        public static readonly System.Collections.Generic.List<string> AnnouncesCutShort = new System.Collections.Generic.List<string>();
#endif

        /// <summary>¿La fila está hablando (un aviso a la vista)?</summary>
        public bool Announcing => _announcing;
        /// <summary>El texto que dice la fila ahora mismo (el aviso, o la consigna si no habla). Para las pruebas y el smoke.</summary>
        public string RowText => _promptText.text;
        /// <summary>El tono del aviso en curso (solo válido mientras <see cref="Announcing"/>).</summary>
        public AnnounceTone AnnounceToneNow => _annTone;
        /// <summary>El ícono que lleva la fila ahora mismo (el glifo del tono mientras habla).</summary>
        public Sprite RowIcon => _promptIcon.gameObject.activeSelf ? _promptIcon.sprite : null;
        /// <summary>La fila de consigna: el texto, para medir su letra y su contraste.</summary>
        public Text RowTextComponent => _promptText;

        /// <summary>
        /// El aviso de un juego (Tarea 179, «La fila habla»): la fila de consigna, que ya está en los 17 juegos, se tiñe con el tono (lima, sol, lila, coral o crema), cambia su ícono por la forma del tono y dice el aviso en UNA línea con la letra en tinta; después vuelve sola a su consigna.
        /// Ningún aviso flota ni tapa nada, y hay un solo lugar donde mirar. Un aviso nuevo reemplaza al anterior (nunca dos a la vez). <paramref name="seconds"/> es cuánto se ve (0 = por omisión <see cref="LineSeconds"/>, o <see cref="SubtitleSeconds"/> con subtítulo).
        /// Si «texto · subtítulo» no cabe en una línea (a 16 dp), se dicen en DOS tiempos en la misma fila: el texto y después el subtítulo (<paramref name="seconds"/> es entonces el total mínimo). Si el juego cambia su consigna mientras la fila habla, la consigna manda y el aviso termina.
        /// <paramref name="subtitleTone"/>: el tono del subtítulo cuando se dice en su propio tiempo (por omisión, el mismo): la pista de Engranajes es ayuda, no un segundo error, y va en crema con el punto.
        /// <paramref name="sound"/>: false = este aviso es silencioso (el juego ya suena su propio sonido con él; <see cref="WoodCore.NoticeUnlessCovered"/>).
        /// </summary>
        public void Announce(string text, AnnounceTone tone, float seconds = 0f, string subtitle = null, bool sound = true, AnnounceTone? subtitleTone = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            var beats = AnnounceBeats(text, subtitle);
            var holds = new float[beats.Length];
            if (beats.Length == 1) holds[0] = seconds > 0f ? seconds : string.IsNullOrEmpty(subtitle) ? LineSeconds : SubtitleSeconds;
            else { holds[0] = LineSeconds; holds[1] = Mathf.Max(SubtitleSeconds, seconds - LineSeconds); }

            if (!_announcing) Snapshot();
#if UNITY_EDITOR
            else if (_annIndex == 0 && _annT < LineSeconds) AnnouncesCutShort.Add(_annBeats[0] + " → " + beats[0] + " (" + _annTone + " → " + tone + ")");
            AnnouncesShown++;
#endif
            _announcing = true;
            _annTone = tone;
            _annSubTone = subtitleTone ?? tone;
            _annBeats = beats;
            _annHolds = holds;
            _annIndex = 0;
            _annT = 0f;
            _annFadeBack = 0f;
            ShowBeat();
            if (sound && GameFeel.SoundOn) _runner.StartCoroutine(NoticeNextFrame(Time.realtimeSinceStartup));
        }

        /// <summary>
        /// Las partes en que se dice un aviso: «texto · subtítulo» en una sola línea si cabe a 16 dp junto al ícono; si no, el texto y después el subtítulo (dos tiempos). Sin subtítulo, solo el texto.
        /// </summary>
        public string[] AnnounceBeats(string text, string subtitle)
        {
            if (string.IsNullOrEmpty(subtitle)) return new[] { text };
            string joined = text + SubtitleJoin + subtitle;
            return LineFits(joined, true, false) ? new[] { joined } : new[] { text, subtitle };
        }

        /// <summary>¿Cabe <paramref name="beat"/> en la fila, en una línea a 16 dp, con el ícono del tono y sin la fase (como se dice un aviso)?</summary>
        public bool AnnounceFits(string beat) => LineFits(beat, true, false);

        /// <summary>Termina el aviso de una vez y la fila vuelve a su consigna (al abrir la pausa o «Cómo se juega», o al empezar otra partida).</summary>
        public void CancelAnnounce()
        {
            if (_announcing) EndAnnounce(fade: false);
            _annFadeBack = 0f;
            SetRowAlpha(1f);
        }

        /// <summary>El reloj del aviso (lo llama el marcador cada cuadro; las pruebas lo llaman a mano con el tiempo que quieran).</summary>
        public void AdvanceAnnounce(float dt)
        {
            if (_annFadeBack > 0f)
            {
                _annFadeBack -= dt;
                SetRowAlpha(_annFadeBack <= 0f ? 1f : 1f - _annFadeBack / AnnounceFade);
            }
            if (!_announcing) return;
            _annT += dt;
            float fade = Motion.Decorative ? AnnounceFade : 0f;
            SetRowAlpha(fade > 0f ? Mathf.Clamp01(_annT / fade) : 1f);
            PromptRect.localScale = Vector3.one * (fade > 0f && _annT < fade ? Mathf.Lerp(1.05f, 1f, UiFx.EaseOutCubic(_annT / fade)) : 1f);
            if (_annT < _annHolds[_annIndex] + fade) return;
            if (_annIndex + 1 < _annBeats.Length)
            {
                _annIndex++;
                _annT = 0f;
                _annTone = _annSubTone;
                ShowBeat();
                return;
            }
            EndAnnounce(fade: true);
        }

        private void Snapshot()
        {
            _snapText = _promptText.text ?? "";
            _snapSize = _promptText.fontSize;
            _snapIconOn = _promptIcon.gameObject.activeSelf;
            _snapIcon = _promptIcon.sprite;
            _snapIconColor = _promptIcon.color;
            _snapPhaseOn = _phaseText.gameObject.activeSelf;
            _snapPhase = _phaseText.text ?? "";
        }

        /// <summary>Pone la parte actual del aviso en la fila: el tono, el glifo, el texto en tinta; la fase a la derecha se oculta mientras habla.</summary>
        private void ShowBeat()
        {
            _promptText.fontSize = PromptSize;
            _promptText.resizeTextMaxSize = PromptSize;
            _promptText.text = _annBeats[_annIndex];
            _promptText.color = NeuroStyle.Ink;
            _promptIcon.gameObject.SetActive(true);
            _promptIcon.sprite = AnnounceStyle.Glyph(_annTone);
            _promptIcon.color = NeuroStyle.Ink;
            _phaseText.gameObject.SetActive(false);
            if (_promptSlab != null) PlasticSlab.Set(_promptSlab, SlabSize.Chip, AnnounceStyle.Slab(_annTone));
            else _promptBg.color = Color.Lerp(NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.9f), AnnounceStyle.Fill(_annTone), 0.6f);
            PromptRect.gameObject.SetActive(true);
            LayoutPrompt();
            SetRowAlpha(Motion.Decorative ? 0f : 1f);
            PromptRect.localScale = Vector3.one * (Motion.Decorative ? 1.05f : 1f);
        }

        /// <summary>La fila vuelve a lo que decía antes: su texto, su ícono, su fase y su losa «noche».</summary>
        private void EndAnnounce(bool fade = false)
        {
            _announcing = false;
            _promptText.fontSize = _snapSize;
            _promptText.resizeTextMaxSize = _snapSize;
            _promptText.text = _snapText;
            _promptText.color = NeuroStyle.Cream;
            _promptIcon.gameObject.SetActive(_snapIconOn);
            _promptIcon.sprite = _snapIcon;
            _promptIcon.color = _snapIconColor;
            _phaseText.gameObject.SetActive(_snapPhaseOn);
            _phaseText.text = _snapPhase;
            if (_promptSlab != null) PlasticSlab.Set(_promptSlab, SlabSize.Chip, SlabTone.Night);
            else _promptBg.color = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.9f);
            PromptRect.localScale = Vector3.one;
            RefreshRow();
            LayoutPrompt();
            _annFadeBack = fade && Motion.Decorative ? AnnounceFade : 0f;
            SetRowAlpha(_annFadeBack > 0f ? 0f : 1f);
        }

        /// <summary>La opacidad de lo que dice la fila (el texto, el ícono y la fase): la entrada del aviso y la vuelta de la consigna se hacen con ella.</summary>
        private void SetRowAlpha(float a)
        {
            var c = _promptText.color; c.a = a; _promptText.color = c;
            c = _promptIcon.color; c.a = a; _promptIcon.color = c;
            c = _phaseText.color; c.a = a; _phaseText.color = c;
        }

        /// <summary>Un cuadro después de mostrarse: si en ese rato no sonó nada más por el altavoz del juego, suena el aviso (así da lo mismo si el juego llama a su sonido antes o después del aviso).</summary>
        private static IEnumerator NoticeNextFrame(float shownAt)
        {
            yield return null;
            WoodCore.NoticeUnlessCovered(shownAt);
        }

        // ------------------------------------------------------------------ animaciones

        private IEnumerator CountUp()
        {
            int from = _shownPoints;
            float t = 0f;
            const float seconds = 0.3f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                _shownPoints = Mathf.RoundToInt(Mathf.Lerp(from, _targetPoints, k));
                _infoText.text = FormatPoints(_shownPoints);
                LayoutChips();
                yield return null;
            }
            _shownPoints = _targetPoints;
            _infoText.text = FormatPoints(_shownPoints);
            LayoutChips();
            _countUp = null;
        }

        private IEnumerator LevelUpGlow()
        {
            _runner.StartCoroutine(PopRect(_infoChip, 1.25f, 0.32f)); // sin animaciones: no hace nada (ver UiKit.PopRect)
            if (Motion.Decorative)
            {
                // y una chispa redonda de sol en el chip (Tarea 126): el salto se ve aunque el chip esté chico
                var fx = Rect;
                Vector2 c = LocalIn(fx, _infoChip) + new Vector2(-_infoChip.rect.width * 0.5f, -_infoChip.rect.height * 0.5f);
                _runner.StartCoroutine(UiFx.SparkBurst(fx, c, NeuroStyle.Sun, 10, 150f, 26f, 0.5f));
            }
            if (_infoSlab != null)
            {
                // plasticina: la pastilla pasa un momento de crema a lima (con «quitar animaciones» también: es un cambio de color, no movimiento) y vuelve
                PlasticSlab.Set(_infoSlab, SlabSize.Chip, SlabTone.Lime);
                float hold = Motion.Decorative ? 0.6f : 0.45f, waited = 0f;
                while (waited < hold) { waited += GameClock.DeltaTime; yield return null; }
                PlasticSlab.Set(_infoSlab, SlabSize.Chip, SlabTone.Cream);
                yield break;
            }
            var baseColor = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.9f);
            if (!Motion.Decorative)
            {
                // Sin salto ni brillo que va y viene: el chip queda resaltado un momento (estático) y vuelve con un fundido.
                _infoBg.color = Color.Lerp(baseColor, NeuroStyle.Sun, 0.85f);
                _infoText.color = NeuroStyle.Ink;
                float held = 0f;
                while (held < 0.45f) { held += GameClock.DeltaTime; yield return null; }
                _runner.StartCoroutine(Motion.ColorTo(_infoText, NeuroStyle.Sun));
                yield return Motion.ColorTo(_infoBg, baseColor);
                yield break;
            }
            float t = 0f;
            const float seconds = 0.6f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                // Sube rápido al sol y vuelve más despacio (la salida no compite con lo que sigue).
                float glow = k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) / 0.8f;
                _infoBg.color = Color.Lerp(baseColor, NeuroStyle.Sun, glow * 0.85f);
                _infoText.color = Color.Lerp(NeuroStyle.Sun, NeuroStyle.Ink, glow);
                yield return null;
            }
            _infoBg.color = baseColor;
            _infoText.color = NeuroStyle.Sun;
        }

        private IEnumerator PromptPulse()
        {
            _runner.StartCoroutine(PopRect(PromptRect, 1.06f, 0.3f));
            if (_promptSlab != null)
            {
                PlasticSlab.Set(_promptSlab, SlabSize.Chip, SlabTone.Sky);
                float hold = Motion.Decorative ? 0.7f : 0.5f, waited = 0f;
                while (waited < hold) { waited += GameClock.DeltaTime; yield return null; }
                if (!_announcing) PlasticSlab.Set(_promptSlab, SlabSize.Chip, SlabTone.Night);          // si la fila habla, su tono manda
                yield break;
            }
            var baseColor = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.9f);
            _promptBg.color = Color.Lerp(baseColor, NeuroStyle.Sky, 0.35f);
            float held = 0f;
            while (held < 0.5f) { held += GameClock.DeltaTime; yield return null; }
            if (!_announcing) _promptBg.color = baseColor;
        }

        /// <summary>"1.250 pts" (separador de miles con punto, como se escribe en español).</summary>
        // Sin CultureInfo("es-ES"): en IL2CPP los datos de cultura pueden venir recortados y lanzar excepción.
        private static string FormatPoints(int points) =>
            points.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.') + " pts";
    }

    /// <summary>El reloj de los avisos (Tarea 179): vive en el objeto del marcador y avanza con el tiempo del juego (se detiene con la pausa). Así, aunque un juego detenga todas sus corrutinas, la fila vuelve sola a su consigna.</summary>
    internal sealed class AnnounceTicker : MonoBehaviour
    {
        public GameHud Hud;

        private void Update() => Hud?.AdvanceAnnounce(GameClock.DeltaTime);
    }
}
