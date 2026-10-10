using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Acoplamiento
{
    public sealed partial class DockingGameController
    {
        private const int Rings = DockingPlan.VisibleRings, SlotsPerRing = DockingContract.Slots, ConfettiPool = 50, CurveBars = 5, EndRows = 4, ChevronCount = 4;

        private static readonly Color Gold = new Color(255f / 255f, 201f / 255f, 74f / 255f);
        private static readonly Color Cyan = new Color(127f / 255f, 216f / 255f, 255f / 255f);
        private static readonly Color Lime = new Color(166f / 255f, 227f / 255f, 107f / 255f);
        private static readonly Color Coral = new Color(255f / 255f, 138f / 255f, 107f / 255f);
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Dim = new Color(143f / 255f, 138f / 255f, 192f / 255f);
        private static readonly Color Soft = new Color(214f / 255f, 209f / 255f, 242f / 255f);
        private static readonly Color TextColor = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color Ink = new Color(26f / 255f, 18f / 255f, 64f / 255f);
        private static readonly Color Shadow = new Color(8f / 255f, 6f / 255f, 30f / 255f, 0.55f);
        private static readonly Color NoticeFill = new Color(31f / 255f, 27f / 255f, 78f / 255f);
        private static readonly Color TowerColor = new Color(142f / 255f, 134f / 255f, 216f / 255f);
        private static readonly Color RingViolet = new Color(91f / 255f, 79f / 255f, 192f / 255f);
        private static readonly Color PortEdgeIdle = new Color(120f / 255f, 240f / 255f, 220f / 255f, 0.65f);
        private static readonly Color CardFill = new Color(34f / 255f, 32f / 255f, 90f / 255f, 0.97f);

        private RectTransform _safe, _play, _backLayer, _stationLayer, _portLayer, _moduleLayer, _buttonLayer, _hudLayer, _fxLayer, _endLayer;
        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();
        private readonly List<KeyValuePair<Image, float>> _radii = new List<KeyValuePair<Image, float>>();

        // estación: la torre central y, por cada anillo visible, sus 8 casilleros (4 detrás del anillo y 4 delante), el anillo (tinta + color) y el aro blanco del último módulo
        private Image _towerShadow, _towerInk, _tower, _slotFlash;
        private RectTransform _stationZone;
        private readonly Image[] _ringInk = new Image[Rings], _ringColor = new Image[Rings];
        private readonly Image[,] _slots = new Image[Rings, SlotsPerRing], _slotDots = new Image[Rings, SlotsPerRing];

        // puerto
        private Image _portPanel, _portEdge, _portHole;
        private Text _portLabel;
        private readonly Image[] _chevrons = new Image[ChevronCount];

        // módulo: la raíz (posición, tamaño, alfa), la sombra, el cuerpo (con máscara: gira y se da vuelta) y la luz fija (recortada por la silueta; contra-girada para no girar)
        private RectTransform _moduleRoot, _bodyRect, _shadowRect, _lightRect, _spotRect;
        private CanvasGroup _moduleGroup;
        private Image _moduleBody, _moduleShadow, _lightImage, _spotImage;

        // botones
        private sealed class BtnView
        {
            public RectTransform Root;
            public Image Bg, Icon, Badge;
            public Text Label;
            public CanvasGroup Group;
        }

        private readonly BtnView[] _buttons = new BtnView[2];

        // barra de tiempo, pista y avisos
        private Image _fuelBorder, _fuelTrack, _fuelFill, _noticeShadow, _noticeBorder, _noticePill;
        private Text _fuelLabel, _hint, _noticeText;

        // marcador de la derecha
        private Image _hudIcon, _hudIconInk, _streakBg;
        private Text _hudCount, _streakText;

        // pantalla final
        private Image _endShade, _endCard, _endRecordBg;
        private Text _endTag, _endTitle, _endCurveTitle, _endNote1, _endNote2, _endRecord;
        private readonly Text[] _endLabel = new Text[EndRows], _endValue = new Text[EndRows], _curveValue = new Text[CurveBars], _curveAngle = new Text[CurveBars];
        private readonly Image[] _curveBarInk = new Image[CurveBars], _curveBar = new Image[CurveBars], _confetti = new Image[ConfettiPool];

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("DockingCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            Stretch(bgRect);
            WorldBackdrop.Build(bgRect, GameWorld.DockingBay);                              // el cielo quieto con su planeta: nada gira en el fondo (el giro es la tarea)

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, DockingContract.Title, MarginU, this, withStreak: false, rightReserve: 170f);

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _backLayer = Layer(_play, "Back");
            _stationLayer = Layer(_play, "Station");
            _portLayer = Layer(_play, "Port");
            _moduleLayer = Layer(_play, "Module");
            _buttonLayer = Layer(_play, "Buttons");
            _hudLayer = Layer(_play, "HudExtra");
            _fxLayer = Layer(_play, "Fx");
            _endLayer = Layer(_play, "End");
            BuildStation();
            BuildPort();
            BuildModule();
            BuildButtons();
            BuildFuelAndNotice();
            BuildHud();
            BuildEnd();
            BuildHiddenResult();

            _exit = new ExitButton(_safe, this, UnitsPerDp);

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
            SetUpTutorial();
            ResetViews();
        }

        private void BuildStation()
        {
            var zone = new GameObject("StationZone");
            zone.transform.SetParent(_stationLayer, false);
            _stationZone = zone.AddComponent<RectTransform>();
            _stationZone.anchorMin = _stationZone.anchorMax = _stationZone.pivot = new Vector2(0.5f, 0.5f);
            _towerShadow = Rr(_stationLayer, "TowerShadow", Shadow, 14f);
            _towerInk = Rr(_stationLayer, "TowerInk", Ink, 17f);
            _tower = Rr(_stationLayer, "Tower", TowerColor, 14f);
            for (int r = 0; r < Rings; r++)
            {
                // primero los casilleros de ATRÁS, después el anillo y por último los de ADELANTE: así los módulos pasan por detrás y por delante del aro
                for (int k = 0; k < SlotsPerRing; k++) if (!IsFront(k)) MakeSlot(r, k);
                _ringInk[r] = MakeImage(_stationLayer, "RingInk" + r, null);
                _ringInk[r].color = Ink;
                _ringColor[r] = MakeImage(_stationLayer, "RingColor" + r, null);
                for (int k = 0; k < SlotsPerRing; k++) if (IsFront(k)) MakeSlot(r, k);
            }
            _slotFlash = MakeImage(_stationLayer, "SlotFlash", RingSprite.Get());
            _slotFlash.color = new Color(1f, 1f, 1f, 0f);
            _slotFlash.gameObject.SetActive(false);
        }

        private static bool IsFront(int slot) => DockingPlan.SlotPoint(0f, slot).depth >= 0f;

        private void MakeSlot(int ring, int k)
        {
            _slots[ring, k] = MakeImage(_stationLayer, "Slot" + ring + "_" + k, null);
            _slotDots[ring, k] = MakeImage(_stationLayer, "SlotDot" + ring + "_" + k, DiscSprite.Get());
            _slotDots[ring, k].color = new Color(1f, 246f / 255f, 200f / 255f, 1f);
        }

        private void BuildPort()
        {
            _portPanel = MakeImage(_portLayer, "PortPanel", null);
            _portLabel = MakeLabel(_portLayer, "PortLabel", 14f, UiFonts.Bold, new Color(214f / 255f, 209f / 255f, 242f / 255f), TextAnchor.MiddleCenter);
            _portLabel.text = DockingContract.PortLabel;
            _portLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            for (int i = 0; i < ChevronCount; i++) _chevrons[i] = MakeImage(_portLayer, "Chevron" + i, null);
            _portEdge = MakeImage(_portLayer, "PortEdge", null);
            _portHole = MakeImage(_portLayer, "PortHole", null);
            _portHole.color = DockingSprites.HoleColor;
            _portEdge.gameObject.SetActive(false);
            _portHole.gameObject.SetActive(false);
        }

        private void BuildModule()
        {
            var root = new GameObject("ModuleRoot", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(_moduleLayer, false);
            _moduleRoot = (RectTransform)root.transform;
            _moduleRoot.anchorMin = _moduleRoot.anchorMax = _moduleRoot.pivot = new Vector2(0.5f, 0.5f);
            _moduleGroup = root.GetComponent<CanvasGroup>();
            _moduleGroup.blocksRaycasts = false;
            _moduleShadow = MakeImage(_moduleRoot, "Shadow", null);
            _shadowRect = _moduleShadow.rectTransform;
            _moduleShadow.color = Shadow;
            _moduleBody = MakeImage(_moduleRoot, "Body", null);
            _bodyRect = _moduleBody.rectTransform;
            // la luz va DENTRO del cuerpo y recortada por su silueta (Mask); como el cuerpo gira, la luz gira al revés: queda fija en la pantalla
            var mask = _moduleBody.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            _lightImage = MakeImage(_bodyRect, "Light", null);
            _lightRect = _lightImage.rectTransform;
            _spotImage = MakeImage(_bodyRect, "Spot", null);
            _spotRect = _spotImage.rectTransform;
            _spotImage.color = new Color(1f, 1f, 1f, 0.3f);
            _moduleRoot.gameObject.SetActive(false);
        }

        private void BuildButtons()
        {
            _buttons[0] = BuildButton("Fits", DockingContract.FitsLabel);
            _buttons[1] = BuildButton("Mirror", DockingContract.MirrorLabel);
        }

        private BtnView BuildButton(string name, string label)
        {
            var v = new BtnView();
            var go = new GameObject(name);
            go.transform.SetParent(_buttonLayer, false);
            v.Root = go.AddComponent<RectTransform>();
            v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
            v.Group = go.AddComponent<CanvasGroup>();
            v.Group.blocksRaycasts = false;
            v.Bg = MakeImage(v.Root, "Bg", null);
            v.Icon = MakeImage(v.Root, "Icon", null);
            v.Label = MakeLabel(v.Root, "Label", 22f, UiFonts.Bold, Ink, TextAnchor.MiddleLeft);
            v.Label.text = label;
            v.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
            v.Badge = MakeImage(v.Root, "Badge", null);
            v.Badge.gameObject.SetActive(false);
            return v;
        }

        private void BuildFuelAndNotice()
        {
            _fuelBorder = Rr(_hudLayer, "FuelBorder", Ink, 8f);
            _fuelTrack = Rr(_hudLayer, "FuelTrack", new Color(8f / 255f, 6f / 255f, 30f / 255f, 0.6f), 6f);
            _fuelFill = Rr(_hudLayer, "FuelFill", Gold, 4f);
            _fuelFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _fuelLabel = MakeLabel(_hudLayer, "FuelLabel", 14f, UiFonts.Bold, Soft, TextAnchor.MiddleRight);
            _fuelLabel.text = DockingContract.TimeLabel;
            _fuelLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _hint = MakeLabel(_hudLayer, "Hint", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _hint.text = DockingContract.Hint;
            _hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            // el aviso: una píldora oscura con borde de tinta y sombra dura, en la franja de y 246-278
            _noticeShadow = Rr(_hudLayer, "NoticeShadow", Shadow, 16f);
            _noticeBorder = Rr(_hudLayer, "NoticeBorder", Ink, 19f);
            _noticePill = Rr(_hudLayer, "NoticePill", NoticeFill, 16f);
            _noticeText = MakeLabel(_hudLayer, "NoticeText", 17f, UiFonts.Bold, Lime, TextAnchor.MiddleCenter);
            _noticeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            SetNoticeVisible(false);
        }

        private void BuildHud()
        {
            _hudIconInk = Rr(_hudLayer, "ScoreIconInk", Ink, 5f);                          // el módulo chico del marcador: celeste con borde de tinta
            _hudIcon = Rr(_hudLayer, "ScoreIcon", DockingSprites.ModuleColor, 3.5f);
            _hudCount = MakeLabel(_hudLayer, "ScoreCount", 22f, UiFonts.Bold, TextColor, TextAnchor.MiddleRight);
            _hudCount.horizontalOverflow = HorizontalWrapMode.Overflow;
            _streakBg = Rr(_hudLayer, "StreakBg", Gold, 13f);
            _streakText = MakeLabel(_hudLayer, "StreakText", 14f, UiFonts.Bold, Ink, TextAnchor.MiddleCenter);
            _streakText.horizontalOverflow = HorizontalWrapMode.Overflow;
            // la barra de tiempo del Reto (los 120 s), fina y bajo el marcador
            _timerTrack = Rr(_hudLayer, "TimerTrack", new Color(Lavender.r, Lavender.g, Lavender.b, 0.25f), 2.5f);
            _timerFill = Rr(_hudLayer, "TimerFill", Lime, 2.5f);
            _timerFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        private void BuildEnd()
        {
            _endShade = MakeImage(_endLayer, "Shade", null);
            _endShade.color = new Color(4f / 255f, 5f / 255f, 26f / 255f, 0.55f);
            _endTag = MakeLabel(_endLayer, "Tag", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _endTitle = MakeLabel(_endLayer, "Title", 26f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _endCard = Rr(_endLayer, "Card", CardFill, 22f);
            string[] labels = { DockingContract.EndHits, DockingContract.EndSpeed, DockingContract.EndRings, DockingContract.EndStreak };
            for (int i = 0; i < EndRows; i++)
            {
                _endLabel[i] = MakeLabel(_endLayer, "Label" + i, 15f, UiFonts.Regular, Soft, TextAnchor.MiddleLeft);
                _endValue[i] = MakeLabel(_endLayer, "Value" + i, 17f, UiFonts.Bold, Color.white, TextAnchor.MiddleRight);
                _endLabel[i].horizontalOverflow = _endValue[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                _endLabel[i].text = labels[i];
            }
            _endCurveTitle = MakeLabel(_endLayer, "CurveTitle", 15f, UiFonts.Bold, Soft, TextAnchor.MiddleCenter);
            _endCurveTitle.text = DockingContract.EndCurve;
            for (int i = 0; i < CurveBars; i++)
            {
                _curveBarInk[i] = Rr(_endLayer, "BarInk" + i, Ink, 10f);
                _curveBar[i] = Rr(_endLayer, "Bar" + i, DockingSprites.GrapeColor, 8f);
                _curveValue[i] = MakeLabel(_endLayer, "BarValue" + i, 14f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
                _curveAngle[i] = MakeLabel(_endLayer, "BarAngle" + i, 14f, UiFonts.Bold, Soft, TextAnchor.MiddleCenter);
                _curveValue[i].horizontalOverflow = _curveAngle[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                _curveAngle[i].text = DockingContract.CurveBins[i] + "°";
            }
            _endNote1 = MakeLabel(_endLayer, "Note1", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _endNote2 = MakeLabel(_endLayer, "Note2", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _endNote1.text = DockingContract.EndNote1;
            _endNote2.text = DockingContract.EndNote2;
            _endNote1.horizontalOverflow = _endNote2.horizontalOverflow = HorizontalWrapMode.Wrap;
            _endRecordBg = Rr(_endLayer, "RecordBg", Gold, 15f);
            _endRecord = MakeLabel(_endLayer, "Record", 15f, UiFonts.Bold, Ink, TextAnchor.MiddleCenter);
            _endTag.text = DockingContract.EndTag;
            foreach (var t in new[] { _endTag, _endTitle, _endCurveTitle, _endRecord }) t.horizontalOverflow = HorizontalWrapMode.Wrap;
            for (int i = 0; i < ConfettiPool; i++)
            {
                _confetti[i] = MakeImage(_endLayer, "Confetti" + i, DiscSprite.Get());
                _confetti[i].gameObject.SetActive(false);
            }
            _endLayer.gameObject.SetActive(false);
        }

        /// <summary>El panel de resultado de la clase base (no se muestra: la pantalla final es la de <see cref="BuildEnd"/>; la base pide que exista).</summary>
        private void BuildHiddenResult()
        {
            var go = new GameObject("Result");
            go.transform.SetParent(_safe, false);
            _resultRoot = go.AddComponent<RectTransform>();
            _resultRoot.sizeDelta = new Vector2(10f, 10f);
            var score = new GameObject("Score");
            score.transform.SetParent(go.transform, false);
            score.AddComponent<RectTransform>();
            score.AddComponent<Text>();
            go.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            go.SetActive(false);
        }

        // ------------------------------------------------------------------ piezas sueltas

        private static RectTransform Layer(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            Stretch(r);
            return r;
        }

        /// <summary>Una imagen centrada en su padre. Nace ENCENDIDA.</summary>
        private static Image MakeImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Un rectángulo redondeado (9-slice) con el radio en dp: el radio en unidades se aplica en <see cref="Layout"/> (depende de la escala de la pantalla).</summary>
        private Image Rr(Transform parent, string name, Color color, float radiusDp)
        {
            var img = MakeImage(parent, name, RoundedRectSprite.Get(24));
            img.color = color;
            img.type = Image.Type.Sliced;
            _radii.Add(new KeyValuePair<Image, float>(img, radiusDp));
            return img;
        }

        /// <summary>Un texto centrado en su propio rect; el tamaño en dp se aplica en <see cref="Layout"/> (depende de la escala de la pantalla).</summary>
        private Text MakeLabel(Transform parent, string name, float dp, Font font, Color color, TextAnchor anchor)
        {
            var t = MakeText(parent, name, Mathf.RoundToInt(dp * UnitsPerDp), anchor, color, 0f, 0f);
            t.font = font;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            _fonts.Add(new KeyValuePair<Text, float>(t, dp));
            return t;
        }

        private static void SetRadius(Image img, float radiusUnits)
        {
            img.type = Image.Type.Sliced;
            SetPpuMultiplier(img, 24f / Mathf.Max(0.05f, radiusUnits));
        }

        // Image.pixelsPerUnitMultiplier existe en el UGUI de Unity 6 pero no en el UI de referencia de tools/unity-compile-check: se llama por reflexión (una sola búsqueda)
        private static readonly System.Reflection.PropertyInfo PpuProperty = typeof(Image).GetProperty("pixelsPerUnitMultiplier");

        private static void SetPpuMultiplier(Image img, float m)
        {
            if (PpuProperty != null) PpuProperty.SetValue(img, m, null);
        }

        // ------------------------------------------------------------------ disposición (dp lógicos de un campo de 360 de ancho; 1 dp = _s unidades)

        private float _s = 3f, _playW = 1080f, _playH = 1920f, _logicalH = 780f;
        private DockingPlan _plan = new DockingPlan(780f, false);

        private Vector2 P(float x, float y) => new Vector2((x - DockingPlan.Width * 0.5f) * _s, _playH * 0.5f - y * _s);
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + DockingPlan.Width * 0.5f, (_playH * 0.5f - u.y) / _s);

        private void SetRect(RectTransform r, float cx, float cy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = P(cx, cy);
        }

        /// <summary>Un hijo posicionado por su DESPLAZAMIENTO (dp) desde el centro del padre (x a la derecha, y hacia abajo).</summary>
        private void SetChild(RectTransform r, float dx, float dy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = new Vector2(dx * _s, -dy * _s);
        }

        private void Layout()
        {
            Canvas.ForceUpdateCanvases();
            _playW = _play.rect.width;
            _playH = _play.rect.height;
            _s = _playW / DockingPlan.Width;
            _logicalH = _playH / _s;
            _plan = new DockingPlan(_logicalH, _guided);
            foreach (var kv in _fonts)
            {
                float dp = kv.Value;
                if (kv.Key == _buttons[0].Label || kv.Key == _buttons[1].Label) dp = Mathf.Max(14f, dp * _plan.K);          // el texto de los botones se achica con ellos, pero nunca bajo 14 dp
                int px = Mathf.RoundToInt(dp * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            foreach (var kv in _radii) SetRadius(kv.Key, kv.Value * _s);
            AssignSprites();
            LayoutStation();
            LayoutPort();
            LayoutModule();
            LayoutButtons();
            LayoutFuelAndNotice();
            LayoutHud();
            LayoutEnd();
            RefreshStation();
            ApplyModulePose();
        }

        /// <summary>Pone los sprites horneados (los fijos; los de la pieza se ponen en cada módulo).</summary>
        private void AssignSprites()
        {
            if (!_baked) return;
            _portPanel.sprite = DockingSprites.PortPanel();
            for (int i = 0; i < ChevronCount; i++) _chevrons[i].sprite = DockingSprites.Chevron();
            _lightImage.sprite = DockingSprites.LightGradient();
            _spotImage.sprite = DockingSprites.LightSpot();
            _buttons[0].Bg.sprite = DockingSprites.FitButton();
            _buttons[0].Icon.sprite = DockingSprites.FitIcon();
            _buttons[1].Bg.sprite = DockingSprites.MirrorButton();
            _buttons[1].Icon.sprite = DockingSprites.MirrorIcon();
            for (int r = 0; r < Rings; r++)
            {
                _ringInk[r].sprite = DockingSprites.EllipseInk();
                _ringColor[r].sprite = DockingSprites.EllipseColor();
            }
        }

        private void LayoutStation()
        {
            float w = DockingSprites.RingSpriteW, h = DockingSprites.RingSpriteH;
            for (int r = 0; r < Rings; r++)
            {
                float y = DockingPlan.RingY(r);
                SetRect(_ringInk[r].rectTransform, DockingPlan.StationCx, y, w, h);
                SetRect(_ringColor[r].rectTransform, DockingPlan.StationCx, y, w, h);
            }
            SetRect(_slotFlash.rectTransform, DockingPlan.StationCx, DockingPlan.StationBase, 24f, 24f);
            SetRect(_stationZone, DockingPlan.StationCx, 183f, 276f, 110f);                            // lo que ilumina el tutorial al hablar de la estación (la torre y el primer anillo; no llega al marcador)
        }

        private void LayoutPort()
        {
            float cx = _plan.PortCx, cy = _plan.PortCy, k = _plan.K;
            SetRect(_portPanel.rectTransform, cx, cy, DockingSprites.PanelSide * k, DockingSprites.PanelSide * k);
            SetRect(_portLabel.rectTransform, cx, cy - _plan.PortH * 0.5f + 16f * k, 100f, 20f);
            for (int i = 0; i < ChevronCount; i++)
            {
                float lx = cx - _plan.PortW * 0.5f + 22f * k + i * ((_plan.PortW - 44f * k) / 3f);
                SetRect(_chevrons[i].rectTransform, lx, cy + _plan.PortH * 0.5f - 11f * k, DockingSprites.ChevronSide * k, DockingSprites.ChevronSide * k);
            }
            float side = DockingSprites.PieceSideCells * _plan.Block;
            SetRect(_portEdge.rectTransform, cx, cy + 6f * k, side, side);
            SetRect(_portHole.rectTransform, cx, cy + 6f * k, side, side);
        }

        private void LayoutModule()
        {
            float side = DockingSprites.PieceSideCells * _plan.Block;
            _moduleRoot.sizeDelta = Vector2.zero;
            _bodyRect.sizeDelta = _shadowRect.sizeDelta = new Vector2(side * _s, side * _s);
            _shadowRect.anchoredPosition = new Vector2(0f, -6f * _plan.K * _s);                         // la sombra dura cae siempre hacia abajo (la raíz no gira)
            // la luz: un cuadrado del tamaño de la circunferencia que envuelve la pieza (como en el boceto), centrado y fijo
            float rad = ModuleRadius();
            _lightRect.sizeDelta = new Vector2(rad * 2f * _s, rad * 2f * _s);
            _spotRect.sizeDelta = new Vector2(rad * 1.1f * _s, rad * 0.48f * _s);
            _spotRect.anchoredPosition = new Vector2(-rad * 0.35f * _s, rad * 0.5f * _s);
        }

        /// <summary>El radio de la circunferencia que envuelve al módulo en curso (dp, a la escala de la pantalla): lo usan la luz y el brillo.</summary>
        private float ModuleRadius()
        {
            if (_trial == null) return 2.83f * _plan.Block;
            DockingSprites.PieceCenter(_trial.Shape, out _, out _, out int w, out int h);
            return Mathf.Sqrt(w * w + h * h) * _plan.Block * 0.5f;
        }

        private void LayoutButtons()
        {
            for (int i = 0; i < 2; i++)
            {
                var (cx, cy) = _plan.ButtonCenter(i);
                var v = _buttons[i];
                SetRect(v.Root, cx, cy, _plan.ButtonW, _plan.ButtonH);
                float bs = DockingSprites.ButtonSide * _plan.K;
                SetChild(v.Bg.rectTransform, 0f, 0f, bs, bs);
                SetChild(v.Icon.rectTransform, -_plan.ButtonW * 0.5f + 34f * _plan.K, 0f, 36f * _plan.K, 36f * _plan.K);
                SetChild(v.Label.rectTransform, -_plan.ButtonW * 0.5f + 58f * _plan.K + 45f, 1f, 90f, 30f);
                SetChild(v.Badge.rectTransform, _plan.ButtonW * 0.5f - 12f * _plan.K, -_plan.ButtonH * 0.5f + 10f * _plan.K, 26f * _plan.K, 26f * _plan.K);
            }
        }

        private void LayoutFuelAndNotice()
        {
            float cx = DockingPlan.Width * 0.5f;
            SetRect(_fuelBorder.rectTransform, cx, _plan.FuelCy, _plan.FuelW + 4.8f * _plan.K, _plan.FuelH + 4.8f * _plan.K);
            SetRect(_fuelTrack.rectTransform, cx, _plan.FuelCy, _plan.FuelW, _plan.FuelH);
            SetRect(_fuelLabel.rectTransform, cx - _plan.FuelW * 0.5f - 8f - 30f, _plan.FuelCy, 60f, 20f);
            SetFuel(_fuelFraction);
            SetRect(_hint.rectTransform, cx, _plan.HintCy, DockingPlan.Width - 24f, 36f);
            LayoutNotice();
        }

        private void LayoutHud()
        {
            SetRect(_hudIconInk.rectTransform, DockingPlan.Width - 26f, 26f, 27f, 21f);
            SetRect(_hudIcon.rectTransform, DockingPlan.Width - 26f, 26f, 22f, 16f);
            SetRect(_hudCount.rectTransform, DockingPlan.Width - 44f - 36f, 26f, 72f, 28f);
            SetRect(_streakBg.rectTransform, DockingPlan.Width - 16f - 56f, 53f, 112f, 26f);
            SetRect(_streakText.rectTransform, DockingPlan.Width - 16f - 56f, 53f, 112f, 22f);
            SetRect(_timerTrack.rectTransform, DockingPlan.Width * 0.5f, 71f, DockingPlan.Width - 24f, 5f);
            SetTimer(_timerFraction);
        }

        /// <summary>Lo que va bajo la franja de arriba en la pantalla final se aprieta parejo en pantallas bajas (el botón «Continuar» ocupa los últimos 65 dp). Las dos notas y el cartel del récord van a distancias fijas bajo la curva (no se aprietan: son texto), así que lo que se aprieta es lo de arriba (de 1 a 0,6).</summary>
        private float EndK() => Mathf.Clamp((_logicalH - 65f - 250f - 4f - EndFixedBelowCurve) / 332f, 0.6f, 1f);

        /// <summary>Lo que ocupan, bajo la base de la curva, los ángulos, las dos notas y el cartel del récord (dp).</summary>
        private const float EndFixedBelowCurve = 100f;

        private float EndY(float refY) => 250f + (refY - 250f) * EndK();

        private void LayoutEnd()
        {
            float cx = DockingPlan.Width * 0.5f;
            SetRect(_endShade.rectTransform, cx, 250f + (_logicalH - 250f) * 0.5f, DockingPlan.Width, _logicalH - 250f);
            SetRect(_endTag.rectTransform, cx, 34f, 300f, 22f);
            SetRect(_endTitle.rectTransform, cx, 64f, 330f, 36f);
            float top = EndY(262f), bottom = EndY(422f);
            SetRect(_endCard.rectTransform, cx, (top + bottom) * 0.5f, DockingPlan.Width - 44f, bottom - top);
            float rowH = (bottom - top - 16f) / EndRows;
            for (int i = 0; i < EndRows; i++)
            {
                float y = top + 8f + rowH * (i + 0.5f);
                SetRect(_endLabel[i].rectTransform, 40f + 110f, y, 220f, 24f);
                SetRect(_endValue[i].rectTransform, DockingPlan.Width - 40f - 80f, y, 160f, 24f);
            }
            SetRect(_endCurveTitle.rectTransform, cx, EndY(462f), 260f, 24f);
            float baseY = EndY(582f), maxH = 80f * Mathf.Clamp((EndY(582f) - EndY(482f)) / 100f, 0.6f, 1f);
            for (int i = 0; i < CurveBars; i++)
            {
                float x = 56f + i * 62f;
                SetRect(_curveAngle[i].rectTransform, x, baseY + 16f, 56f, 20f);
            }
            _endBarMaxH = maxH;
            float noteY = baseY + 38f;                                                      // las notas y el cartel, a distancias fijas bajo la curva: el cartel nunca pisa la nota
            SetRect(_endNote1.rectTransform, cx, noteY, 340f, 22f);
            SetRect(_endNote2.rectTransform, cx, noteY + 19f, 340f, 22f);
            float pillY = noteY + 19f + 8.5f + 6f + 14f;
            SetRect(_endRecordBg.rectTransform, cx, pillY, 190f, 28f);
            SetRect(_endRecord.rectTransform, cx, pillY, 190f, 24f);
            PlaceCurve();
        }

        /// <summary>Lo que ocupa, abajo, el rótulo «Práctica: no cuenta» y «Saltar tutorial» (dp).</summary>
        private const float TutorialControlsDp = DockingPlan.TutorialControls;
    }
}
