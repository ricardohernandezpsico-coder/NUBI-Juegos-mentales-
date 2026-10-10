using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Aterrizaje
{
    public sealed partial class LandingGameController
    {
        private const int StarCount = 26, GuideDots = 40, DustCount = 14, ConfettiPool = 50, ProgressDots = LandingContract.DomeEvery;

        private static readonly Color Gold = LandingSprites.Gold;
        private static readonly Color Cyan = LandingSprites.Cyan;
        private static readonly Color Lime = LandingSprites.LimeColor;
        private static readonly Color Coral = LandingSprites.CoralColor;
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Dim = new Color(143f / 255f, 138f / 255f, 192f / 255f);
        private static readonly Color Soft = new Color(214f / 255f, 209f / 255f, 242f / 255f);
        private static readonly Color TextColor = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color Ink = new Color(26f / 255f, 18f / 255f, 64f / 255f);
        private static readonly Color Shadow = new Color(8f / 255f, 6f / 255f, 30f / 255f, 0.55f);
        private static readonly Color NoticeFill = LandingSprites.NoticeFill;
        private static readonly Color CardFill = new Color(34f / 255f, 32f / 255f, 90f / 255f, 0.97f);

        private RectTransform _safe, _play, _worldLayer, _rulerLayer, _guideLayer, _landerLayer, _resultLayer, _fxLayer, _noticeLayer, _hudLayer, _endLayer;
        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();
        private readonly List<KeyValuePair<Image, float>> _radii = new List<KeyValuePair<Image, float>>();

        // el mundo
        private Image _nebula, _earthGlow, _earth, _satellite, _shoot, _rangeFar, _rangeNear, _moon;
        private readonly Image[] _stars = new Image[StarCount];
        private readonly float[] _starX = new float[StarCount], _starY = new float[StarCount], _starPhase = new float[StarCount], _starSize = new float[StarCount];

        // la regla
        private Image _rulerShadow, _rulerInk, _rulerFill, _rulerShine, _tickL, _tickR, _tickMid;
        private Text _minLabel, _maxLabel;
        private RectTransform _lineZone;

        // la misión
        private Text _missionSmall, _missionNumber;

        // la guía y la nave
        private readonly Image[] _guideDot = new Image[GuideDots];
        private Image _guideArrow;
        private RectTransform _landerRoot;
        private Image _landerBody, _flame, _lightL, _lightR;

        // el resultado: el tramo, la insignia y la bandera
        private Image _gapLine, _badge, _pole, _pennant, _flagShadow, _flagInk, _flagFill;
        private Text _flagText;
        private readonly Image[] _dust = new Image[DustCount];
        private readonly Vector2[] _dustVel = new Vector2[DustCount];
        private readonly float[] _dustLife = new float[DustCount], _dustDx = new float[DustCount];

        // los avisos: A (lo que salió) y B (la cúpula nueva)
        private sealed class NoticeView
        {
            public Image Shadow, Border, Pill;
            public Text Text;
            public string Message = "";
            public Color Color;
            public bool Big;
            public float At = -1f, Life;
            public float Width;
            public bool Live;
        }

        private readonly NoticeView[] _notices = new NoticeView[2];

        // el marcador de la derecha, la instrucción y la barra del Reto
        private Image _hudDome, _streakBg, _timerTrack, _timerFill;
        private readonly Image[] _hudDot = new Image[ProgressDots];
        private Text _hudCount, _streakText, _hint;

        // la pantalla final
        private Image _endShade, _endCard;
        private Text _endTag, _endTitle, _endBoxTitle, _endEstimate, _endUnit, _endSummary;
        private readonly Image[] _confetti = new Image[ConfettiPool];

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("LandingCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.LunarBase);                               // el cielo de la app, sin estrellas ni perspectiva: lo que se ve en el cielo lo arma este juego

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, LandingContract.Title, MarginU, this, withStreak: false, rightReserve: 170f);

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _worldLayer = Layer(_play, "World");
            _rulerLayer = Layer(_play, "RulerLayer");
            var missionLayer = Layer(_play, "Mission");
            _guideLayer = Layer(_play, "Guide");
            _landerLayer = Layer(_play, "LanderLayer");
            _resultLayer = Layer(_play, "ResultLayer");                                     // la bandera va ENCIMA de la nave: el blanco siempre se ve
            _fxLayer = Layer(_play, "Fx");
            _noticeLayer = Layer(_play, "Notices");
            _hudLayer = Layer(_play, "HudExtra");
            _endLayer = Layer(_play, "End");
            BuildWorld();
            BuildRuler();
            BuildMission(missionLayer);
            BuildGuide();
            BuildLander();
            BuildResult();
            BuildFx();
            BuildNotices();
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
            _hoverSource = gameObject.AddComponent<AudioSource>();
            _hoverSource.playOnAwake = false;
            _hoverSource.loop = true;
            _hoverSource.volume = 0f;
            SetUpTutorial();
            ResetViews();
        }

        private void BuildWorld()
        {
            _nebula = MakeImage(_worldLayer, "Nebula", null);
            // las estrellas que titilan: 26, con las posiciones del boceto (semilla 77)
            var rng = new SoundKit.Rng(77);
            for (int i = 0; i < StarCount; i++)
            {
                _starX[i] = (float)rng.Next() * LandingPlan.Width;
                _starY[i] = (float)rng.Next();                                              // 0..1: se reparte entre y 170 y la regla en Layout
                _starPhase[i] = (float)rng.Next() * 6f;
                _starSize[i] = 0.8f + (float)rng.Next() * 1.4f;
                _stars[i] = MakeImage(_worldLayer, "Star" + i, DiscSprite.Get());
            }
            _shoot = MakeImage(_worldLayer, "ShootingStar", null);
            _earthGlow = MakeImage(_worldLayer, "EarthGlow", RadialGlowSprite.Get());
            _earthGlow.color = new Color(127f / 255f, 216f / 255f, 1f, 0.32f);
            _earth = MakeImage(_worldLayer, "Earth", null);
            _satellite = MakeImage(_worldLayer, "Satellite", null);
            _rangeFar = MakeImage(_worldLayer, "RangeFar", null);
            _rangeNear = MakeImage(_worldLayer, "RangeNear", null);
            _moon = MakeImage(_worldLayer, "Moon", null);
        }

        private void BuildRuler()
        {
            _rulerShadow = Rr(_rulerLayer, "RulerShadow", Shadow, 12f);
            _rulerInk = Rr(_rulerLayer, "RulerInk", Ink, 13.4f);
            _rulerFill = Rr(_rulerLayer, "RulerFill", LandingSprites.RulerColor, 10f);
            _rulerShine = Rr(_rulerLayer, "RulerShine", new Color(1f, 1f, 1f, 0.38f), 3f);
            _tickL = Rr(_rulerLayer, "EndTickL", Ink, 1.7f);
            _tickR = Rr(_rulerLayer, "EndTickR", Ink, 1.7f);
            _tickMid = Rr(_rulerLayer, "MidTick", Ink, 1.3f);
            _minLabel = MakeLabel(_rulerLayer, "Min", 20f, UiFonts.Bold, Color.white, TextAnchor.MiddleLeft);
            _maxLabel = MakeLabel(_rulerLayer, "Max", 20f, UiFonts.Bold, Color.white, TextAnchor.MiddleRight);
            foreach (var t in new[] { _minLabel, _maxLabel })
            {
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                NeuroStyle.ClayText(t, 3.5f, 5f);                                           // contorno de tinta: se lee sobre la luna lavanda
            }
            var zone = new GameObject("LineZone", typeof(RectTransform));
            zone.transform.SetParent(_rulerLayer, false);
            _lineZone = (RectTransform)zone.transform;
            _lineZone.anchorMin = _lineZone.anchorMax = _lineZone.pivot = new Vector2(0.5f, 0.5f);
        }

        private void BuildMission(Transform parent)
        {
            _missionSmall = MakeLabel(parent, "MissionSmall", 16f, UiFonts.Regular, Soft, TextAnchor.MiddleCenter);
            _missionSmall.text = LandingContract.MissionLabel;
            _missionSmall.horizontalOverflow = HorizontalWrapMode.Overflow;
            _missionNumber = MakeLabel(parent, "Mission", LandingPlan.MissionNumberSize, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _missionNumber.resizeTextForBestFit = true;                                      // se achica si no cabe (una suma larga), pero nunca bajo 28 dp
        }

        private void BuildGuide()
        {
            // la zona guía de la ronda guiada: una franja sol sobre el lugar justo (detrás de la nave)
            _zone = Rr(_guideLayer, "GuideZone", new Color(Gold.r, Gold.g, Gold.b, 0.32f), 12f);
            _zone.gameObject.SetActive(false);
            for (int i = 0; i < GuideDots; i++)
            {
                _guideDot[i] = MakeImage(_guideLayer, "Dot" + i, DiscSprite.Get());
                _guideDot[i].color = new Color(Lime.r, Lime.g, Lime.b, 0.8f);
            }
            _guideArrow = MakeImage(_guideLayer, "Arrow", null);
        }

        private void BuildLander()
        {
            var root = new GameObject("Lander", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(_landerLayer, false);
            _landerRoot = (RectTransform)root.transform;
            _landerRoot.anchorMin = _landerRoot.anchorMax = _landerRoot.pivot = new Vector2(0.5f, 0.5f);
            _landerGroup = root.GetComponent<CanvasGroup>();
            _landerGroup.blocksRaycasts = false;
            _flame = MakeImage(_landerRoot, "Flame", RadialGlowSprite.Get());
            _flame.color = new Color(1f, 170f / 255f, 90f / 255f, 0.9f);
            _landerBody = MakeImage(_landerRoot, "Body", null);
            _lightL = MakeImage(_landerRoot, "LightL", RadialGlowSprite.Get());
            _lightL.color = Coral;
            _lightR = MakeImage(_landerRoot, "LightR", RadialGlowSprite.Get());
            _lightR.color = Lime;
            _landerRoot.gameObject.SetActive(false);
        }

        private CanvasGroup _landerGroup;

        private void BuildResult()
        {
            _gapLine = Rr(_resultLayer, "GapLine", Lime, 3f);
            _badge = MakeImage(_resultLayer, "Badge", null);
            _pole = Rr(_resultLayer, "Pole", Ink, 1.5f);
            _pennant = MakeImage(_resultLayer, "Pennant", null);
            _flagShadow = Rr(_resultLayer, "FlagShadow", Shadow, 14f);
            _flagInk = Rr(_resultLayer, "FlagInk", Ink, 15.5f);
            _flagFill = Rr(_resultLayer, "FlagFill", NoticeFill, 14f);
            _flagText = MakeLabel(_resultLayer, "Label", 18f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _flagText.horizontalOverflow = HorizontalWrapMode.Overflow;
            SetResultVisible(false);
        }

        private void BuildFx()
        {
            for (int i = 0; i < DustCount; i++)
            {
                _dust[i] = MakeImage(_fxLayer, "Dust" + i, DiscSprite.Get());
                _dust[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < ConfettiPool; i++)
            {
                _confetti[i] = MakeImage(_fxLayer, "Confetti" + i, DiscSprite.Get());
                _confetti[i].gameObject.SetActive(false);
            }
        }

        private void BuildNotices()
        {
            for (int i = 0; i < _notices.Length; i++)
            {
                var n = new NoticeView();
                n.Shadow = Rr(_noticeLayer, "NoticeShadow" + i, Shadow, 20f);
                n.Border = Rr(_noticeLayer, "NoticeBorder" + i, Ink, 20f);
                n.Pill = Rr(_noticeLayer, "NoticePill" + i, NoticeFill, 17f);
                n.Text = MakeLabel(_noticeLayer, "NoticeText" + i, 17f, UiFonts.Bold, Lime, TextAnchor.MiddleCenter);
                n.Text.horizontalOverflow = HorizontalWrapMode.Overflow;
                _notices[i] = n;
                SetNoticeVisible(n, false);
            }
        }

        private void BuildHud()
        {
            _hudDome = MakeImage(_hudLayer, "Dome", null);
            _hudCount = MakeLabel(_hudLayer, "DomeCount", 20f, UiFonts.Bold, TextColor, TextAnchor.MiddleRight);
            _hudCount.horizontalOverflow = HorizontalWrapMode.Overflow;
            for (int i = 0; i < ProgressDots; i++) _hudDot[i] = MakeImage(_hudLayer, "ProgressDot" + i, DiscSprite.Get());
            _streakBg = Rr(_hudLayer, "StreakBg", Gold, 13f);
            _streakText = MakeLabel(_hudLayer, "StreakText", 14f, UiFonts.Bold, Ink, TextAnchor.MiddleCenter);
            _streakText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _hint = MakeLabel(_hudLayer, "Hint", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _hint.text = LandingContract.Hint;
            _hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            // la barra del Reto (los 120 s), fina y bajo el marcador
            _timerTrack = Rr(_hudLayer, "TimerTrack", new Color(Lavender.r, Lavender.g, Lavender.b, 0.25f), 2.5f);
            _timerFill = Rr(_hudLayer, "TimerFill", Lime, 2.5f);
            _timerFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        private void BuildEnd()
        {
            _endShade = MakeImage(_endLayer, "Shade", null);
            _endShade.color = new Color(4f / 255f, 5f / 255f, 26f / 255f, 0.62f);
            _endTag = MakeLabel(_endLayer, "Tag", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _endTag.text = LandingContract.EndTag;
            _endTitle = MakeLabel(_endLayer, "Title", 25f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _endCard = Rr(_endLayer, "Card", CardFill, 22f);
            _endBoxTitle = MakeLabel(_endLayer, "BoxTitle", 15f, UiFonts.Bold, Soft, TextAnchor.MiddleCenter);
            _endBoxTitle.text = LandingContract.EndBoxTitle;
            _endEstimate = MakeLabel(_endLayer, "Estimate", 34f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _endUnit = MakeLabel(_endLayer, "Unit", 16f, UiFonts.Regular, Color.white, TextAnchor.MiddleCenter);
            _endUnit.text = LandingContract.EndBoxUnit;
            _endSummary = MakeLabel(_endLayer, "Summary", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            foreach (var t in new[] { _endTag, _endTitle, _endBoxTitle, _endEstimate, _endUnit, _endSummary }) t.horizontalOverflow = HorizontalWrapMode.Wrap;
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
        private LandingPlan _plan = new LandingPlan(780f, false);

        private Vector2 P(float x, float y) => new Vector2((x - LandingPlan.Width * 0.5f) * _s, _playH * 0.5f - y * _s);
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + LandingPlan.Width * 0.5f, (_playH * 0.5f - u.y) / _s);

        private void SetRect(RectTransform r, float cx, float cy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = P(cx, cy);
        }

        private void Layout()
        {
            Canvas.ForceUpdateCanvases();
            _playW = _play.rect.width;
            _playH = _play.rect.height;
            _s = _playW / LandingPlan.Width;
            _logicalH = _playH / _s;
            _plan = new LandingPlan(_logicalH, _guided);
            foreach (var kv in _fonts)
            {
                int px = Mathf.RoundToInt(kv.Value * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit)
                {
                    kv.Key.resizeTextMaxSize = px;
                    kv.Key.resizeTextMinSize = Mathf.RoundToInt(28f * _s);
                }
            }
            foreach (var kv in _radii) SetRadius(kv.Key, kv.Value * _s);
            AssignSprites();
            LayoutWorld();
            LayoutRuler();
            LayoutMission();
            LayoutHud();
            LayoutEnd();
            RefreshHudExtras();
            Animate(Now, 0f);
        }

        /// <summary>Pone los sprites horneados.</summary>
        private void AssignSprites()
        {
            if (!_baked) return;
            _nebula.sprite = LandingSprites.Nebula();
            _earth.sprite = LandingSprites.Earth();
            _satellite.sprite = LandingSprites.Satellite();
            _shoot.sprite = LandingSprites.ShootingStar();
            _rangeFar.sprite = LandingSprites.Range(true);
            _rangeNear.sprite = LandingSprites.Range(false);
            _moon.sprite = LandingSprites.Moon();
            _landerBody.sprite = LandingSprites.Lander();
            _pennant.sprite = LandingSprites.Pennant();
            _guideArrow.sprite = LandingSprites.Arrow();
            _hudDome.sprite = LandingSprites.Dome(_shownDomes > 0);
        }

        private void LayoutWorld()
        {
            SetRect(_nebula.rectTransform, LandingPlan.Width * 0.5f, 300f, LandingPlan.Width, 600f);
            float lo = 170f, hi = Mathf.Max(lo + 40f, _plan.RulerY - 54f);
            for (int i = 0; i < StarCount; i++)
            {
                float d = _starSize[i] * 2f;
                SetRect(_stars[i].rectTransform, _starX[i], lo + _starY[i] * (hi - lo), d, d);
            }
            SetRect(_earthGlow.rectTransform, _plan.EarthX, _plan.EarthY, _plan.EarthR * 3.6f, _plan.EarthR * 3.6f);
            SetRect(_earth.rectTransform, _plan.EarthX, _plan.EarthY, LandingSprites.EarthSide, LandingSprites.EarthSide);
            SetRect(_satellite.rectTransform, 0f, 236f, LandingSprites.SatelliteW, LandingSprites.SatelliteH);
            _satellite.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 0.2f * Mathf.Rad2Deg);       // -0,2 rad en el boceto (y hacia abajo): se inclina hacia abajo a la derecha
            // las cordilleras: la base va sobre la regla (la de atrás 4 dp más arriba); su x lo mueve Animate
            SetRect(_rangeFar.rectTransform, LandingPlan.Width, _plan.RulerY - 4f - LandingSprites.RangeH * 0.5f, LandingSprites.RangeW, LandingSprites.RangeH);
            SetRect(_rangeNear.rectTransform, LandingPlan.Width, _plan.RulerY - LandingSprites.RangeH * 0.5f, LandingSprites.RangeW, LandingSprites.RangeH);
            SetRect(_moon.rectTransform, LandingPlan.Width * 0.5f, _plan.RulerY - 16f + LandingSprites.MoonH * 0.5f, LandingSprites.MoonW, LandingSprites.MoonH);
        }

        private void LayoutRuler()
        {
            float cx = LandingPlan.Width * 0.5f, y = _plan.RulerY + LandingPlan.RulerH * 0.5f;
            float w = LandingPlan.RulerX1 - LandingPlan.RulerX0 + 16f, h = LandingPlan.RulerH;
            SetRect(_rulerShadow.rectTransform, cx, y + 5f, w + 6.8f, h + 6.8f);
            SetRect(_rulerInk.rectTransform, cx, y, w + 6.8f, h + 6.8f);
            SetRect(_rulerFill.rectTransform, cx, y, w, h);
            SetRect(_rulerShine.rectTransform, cx, y - h * 0.25f, w - 14f, 4f);
            SetRect(_tickL.rectTransform, LandingPlan.RulerX0, y, 3.4f, h + 12f);
            SetRect(_tickR.rectTransform, LandingPlan.RulerX1, y, 3.4f, h + 12f);
            SetRect(_tickMid.rectTransform, cx, y, 2.6f, h - 6f);
            float ly = _plan.RulerY + LandingPlan.RulerH + 24f;
            SetRect(_minLabel.rectTransform, LandingPlan.RulerX0 - 8f + 60f, ly, 120f, 30f);          // alineado a la izquierda: empieza en x0 - 8
            SetRect(_maxLabel.rectTransform, LandingPlan.RulerX1 + 8f - 60f, ly, 120f, 30f);          // alineado a la derecha: termina en x1 + 8
            SetRect(_lineZone, cx, _plan.RulerY + 8f, 330f, 120f);
        }

        private void LayoutMission()
        {
            SetRect(_missionSmall.rectTransform, LandingPlan.Width * 0.5f, LandingPlan.MissionLabelY, 240f, 22f);
            SetRect(_missionNumber.rectTransform, LandingPlan.Width * 0.5f, LandingPlan.MissionNumberY, 240f, 66f);
        }

        private void LayoutHud()
        {
            // la cúpula de tu base, su número y los 5 puntos hacia la próxima (arriba a la derecha: nada de esto queda cerca de la regla)
            SetRect(_hudDome.rectTransform, LandingPlan.Width - 30f, 20f, LandingSprites.DomeW * 1.1f, LandingSprites.DomeH * 1.1f);
            SetRect(_hudCount.rectTransform, LandingPlan.Width - 48f - 30f, 22f, 60f, 26f);
            for (int i = 0; i < ProgressDots; i++) SetRect(_hudDot[i].rectTransform, LandingPlan.Width - 70f + i * 11f, 46f, 7.2f, 7.2f);
            SetRect(_streakBg.rectTransform, LandingPlan.Width - 16f - 56f, 91f, 112f, 26f);                 // bajo los puntos y la barra del Reto
            SetRect(_streakText.rectTransform, LandingPlan.Width - 16f - 56f, 91.5f, 112f, 22f);
            SetRect(_hint.rectTransform, LandingPlan.Width * 0.5f, _plan.HintY, 336f, 22f);
            SetRect(_timerTrack.rectTransform, LandingPlan.Width * 0.5f, 71f, LandingPlan.Width - 24f, 5f);
        }

        private void LayoutEnd()
        {
            float cx = LandingPlan.Width * 0.5f;
            SetRect(_endShade.rectTransform, cx, _logicalH * 0.5f, LandingPlan.Width, _logicalH);
            SetRect(_endTag.rectTransform, cx, 34f, 300f, 22f);
            SetRect(_endTitle.rectTransform, cx, 64f, 330f, 36f);
            SetRect(_endCard.rectTransform, cx, 92f + 78f, LandingPlan.Width - 36f, 156f);
            SetRect(_endBoxTitle.rectTransform, cx, 92f + 22f, 300f, 22f);
            SetRect(_endEstimate.rectTransform, cx, 92f + 56f, 300f, 44f);
            SetRect(_endUnit.rectTransform, cx, 92f + 84f, 300f, 24f);
            SetRect(_endSummary.rectTransform, cx, 92f + 126f, 300f, 22f);
        }
    }
}
