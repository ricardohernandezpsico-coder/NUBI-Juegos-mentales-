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
        private readonly RectTransform _infoChip, _streakChip, _titleRect;
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

            // fila 1: el título a 18 dp (54 unidades); se achica hasta 16 dp solo si el chip de avance le deja poco lugar (el título más largo, «En la punta de la lengua»)
            _titleText = MakeText(Rect, "Title", 54, TextAnchor.MiddleLeft, Color.white, 3f, 0.45f);
            _titleRect = _titleText.rectTransform;
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
        public bool FitsOneLine(string text, bool withIcon)
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
            float right = 24f + (_phaseText.gameObject.activeSelf ? Mathf.Ceil(_phaseText.preferredWidth) + 28f : 0f);
            return width <= 1080f - 2f * _margin - left - right;
        }

        /// <summary>Solo el ícono de la consigna (la regla de Tinta cambia de color y de ícono con cada palabra).</summary>
        public void SetPromptIcon(Sprite icon, Color color)
        {
            _promptIcon.gameObject.SetActive(icon != null);
            if (icon != null) { _promptIcon.sprite = icon; _promptIcon.color = color; }
            LayoutPrompt();
        }

        /// <summary>La fase de la consigna, a la derecha de la misma fila («Mira el rastro» → «Repítelo»). Vacío = sin fase.</summary>
        public void SetPhase(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            _phaseText.gameObject.SetActive(show);
            if (show) _phaseText.text = text;
            RefreshRow();
            LayoutPrompt();
        }

        /// <summary>La consigna salta un poco y se enciende un momento (un sector nuevo, una misión nueva): sin «quitar animaciones», solo el color.</summary>
        public void PulsePrompt() => _runner.StartCoroutine(PromptPulse());

        /// <summary>El color del cuerpo de la fila de consigna (la losa «noche» del kit): contra él se mide que lo que va en color se lea (≥ 4,5:1). Medido sobre una captura real.</summary>
        public static readonly Color RowBody = new Color32(0x2F, 0x27, 0x5F, 0xFF);

        public string PromptText => _promptText.text;
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
        public string PhaseText => _phaseText.gameObject.activeSelf ? _phaseText.text : "";
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
            _streakText.text = "×" + streak + " seguidos";
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
                PlasticSlab.Set(_promptSlab, SlabSize.Chip, SlabTone.Night);
                yield break;
            }
            var baseColor = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.9f);
            _promptBg.color = Color.Lerp(baseColor, NeuroStyle.Sky, 0.35f);
            float held = 0f;
            while (held < 0.5f) { held += GameClock.DeltaTime; yield return null; }
            _promptBg.color = baseColor;
        }

        /// <summary>"1.250 pts" (separador de miles con punto, como se escribe en español).</summary>
        // Sin CultureInfo("es-ES"): en IL2CPP los datos de cultura pueden venir recortados y lanzar excepción.
        private static string FormatPoints(int points) =>
            points.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.') + " pts";
    }
}
