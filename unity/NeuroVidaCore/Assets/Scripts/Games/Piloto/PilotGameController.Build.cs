using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Piloto
{
    public sealed partial class PilotGameController
    {
        private const int SignalPool = 6, BeaconPool = 14, FloatPool = 4, DotPool = 9;

        private static readonly Color Gold = new Color(255f / 255f, 201f / 255f, 74f / 255f);
        private static readonly Color Cyan = new Color(127f / 255f, 216f / 255f, 255f / 255f);
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Dim = new Color(143f / 255f, 138f / 255f, 192f / 255f);
        private static readonly Color Soft = new Color(214f / 255f, 209f / 255f, 242f / 255f);
        private static readonly Color Good = new Color(166f / 255f, 227f / 255f, 107f / 255f);
        private static readonly Color Bad = new Color(255f / 255f, 138f / 255f, 107f / 255f);
        private static readonly Color TextColor = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color PanelFill = new Color(28f / 255f, 23f / 255f, 80f / 255f);
        private static readonly Color PanelEdge = new Color(58f / 255f, 52f / 255f, 128f / 255f);
        private static readonly Color Pill = new Color(8f / 255f, 10f / 255f, 34f / 255f, 0.92f);

        /// <summary>Una señal en pantalla: su área de toque (80 dp, que también es el hueco del tutorial), el anillo que se vacía, el dibujo y la marca de «toque equivocado».</summary>
        private sealed class SigView
        {
            public RectTransform Root;
            public Image Ring, Body, Mark;
            public bool InUse;
        }

        /// <summary>Un texto flotante («+10», «No era de tu misión», «Se fue»): una píldora oscura que se coloca junto a su señal SIN tapar otra.</summary>
        private sealed class FloatView
        {
            public RectTransform Root;
            public Image Bg;
            public Text Label;
            public bool Active;
            public float At, Seconds = 1f;
            public Box Area;
        }

        private RectTransform _safe, _play, _tintLayer, _routeLayer, _sigLayer, _shipLayer, _stripLayer, _topLayer, _noticeLayer, _fxLayer, _endLayer;
        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();
        private readonly List<KeyValuePair<Image, float>> _radii = new List<KeyValuePair<Image, float>>();

        // cielo y ruta
        private Image _tintA, _tintB;
        private RouteGraphic _routeGraphic;
        private GateGraphic _gateGraphic;
        private readonly Image[] _beaconGlow = new Image[BeaconPool * 2], _beaconDot = new Image[BeaconPool * 2];

        // nave, rayo y franja del dedo
        private RectTransform _shipRoot, _skyZone;
        private Image _shipGlow, _shipBody, _beam, _stripRim, _stripFill, _fingerDot, _fingerRing;
        private Text _stripLabel, _stripHint;
        private readonly Image[] _dots = new Image[DotPool];

        // señales y textos flotantes
        private readonly SigView[] _views = new SigView[SignalPool];
        private readonly FloatView[] _floatViews = new FloatView[FloatPool];

        // arriba: viaje y misión
        private Image _journeyTrack, _journeyFill, _sepA, _sepB, _missionRim, _missionFill, _missionIcon;
        private Text _missionTag, _missionName;

        // el aviso de sector y de hiperimpulso
        private CanvasGroup _bannerGroup;
        private Image _bannerRim, _bannerFill;
        private Text _bannerTag, _bannerTitle;

        // la pantalla final
        private readonly Image[] _endSector = new Image[PilotContract.Sectors], _endLink = new Image[PilotContract.Sectors - 1];
        private readonly Text[] _endSectorName = new Text[PilotContract.Sectors];
        private Image _endPanelRim, _endPanel;
        private Text _endTag, _endTitle, _endNote1, _endNote2;
        private readonly Text[] _endLabel = new Text[6], _endValue = new Text[6];

        // el motor (se repite)
        private AudioSource _engineSource;

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("PilotCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.Hyperspace);
            _stars = bgRect.GetComponentInChildren<StarfieldFx>();

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, PilotContract.Title, MarginU, this);

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _tintLayer = Layer(_play, "SectorTint");
            _routeLayer = Layer(_play, "Route");
            _sigLayer = Layer(_play, "Signals");
            _shipLayer = Layer(_play, "Ship");
            _stripLayer = Layer(_play, "FingerStrip");
            _topLayer = Layer(_play, "Top");
            _noticeLayer = Layer(_play, "Notices");
            _fxLayer = Layer(_play, "Fx");
            _endLayer = Layer(_play, "End");
            BuildTint();
            BuildRoute();
            BuildSignals();
            BuildShip();
            BuildStrip();
            BuildTop();
            BuildNotices();
            BuildEnd();
            BuildHiddenResult();

            _exit = new ExitButton(_safe, this, UnitsPerDp);

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _engineSource = gameObject.AddComponent<AudioSource>();
            _engineSource.loop = true;
            _engineSource.playOnAwake = false;
            _engineSource.volume = 0f;

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
            SetUpTutorial();
            ResetViews();
        }

        private void BuildTint()
        {
            _tintA = MakeImage(_tintLayer, "TintA", RadialGlowSprite.Get());
            _tintB = MakeImage(_tintLayer, "TintB", RadialGlowSprite.Get());
        }

        private void BuildRoute()
        {
            var rg = new GameObject("RouteMesh", typeof(RectTransform), typeof(CanvasRenderer));
            rg.transform.SetParent(_routeLayer, false);
            Stretch((RectTransform)rg.transform);
            _routeGraphic = rg.AddComponent<RouteGraphic>();
            _routeGraphic.raycastTarget = false;
            for (int i = 0; i < _beaconGlow.Length; i++)
            {
                _beaconGlow[i] = MakeImage(_routeLayer, "BeaconGlow" + i, RadialGlowSprite.Get());
                _beaconDot[i] = MakeImage(_routeLayer, "BeaconDot" + i, DiscSprite.Get());
                _beaconGlow[i].gameObject.SetActive(false);
                _beaconDot[i].gameObject.SetActive(false);
            }
            var gg = new GameObject("Gate", typeof(RectTransform), typeof(CanvasRenderer));
            gg.transform.SetParent(_routeLayer, false);
            var gr = (RectTransform)gg.transform;
            gr.anchorMin = gr.anchorMax = gr.pivot = new Vector2(0.5f, 0.5f);
            gr.sizeDelta = Vector2.zero;
            _gateGraphic = gg.AddComponent<GateGraphic>();
            _gateGraphic.raycastTarget = false;
            gg.SetActive(false);
        }

        private void BuildSignals()
        {
            for (int i = 0; i < _views.Length; i++)
            {
                var v = new SigView();
                var go = new GameObject("Signal" + i);
                go.transform.SetParent(_sigLayer, false);
                v.Root = go.AddComponent<RectTransform>();
                v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
                v.Ring = MakeImage(v.Root, "Ring", RingSprite.Get());
                v.Ring.type = Image.Type.Filled;
                v.Ring.fillMethod = Image.FillMethod.Radial360;
                v.Ring.fillOrigin = (int)Image.Origin360.Top;
                v.Ring.fillClockwise = true;
                v.Ring.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.85f);
                v.Body = MakeImage(v.Root, "Body", null);
                v.Mark = MakeImage(v.Root, "Mark", AnswerMarkSprite.Cross());
                v.Mark.gameObject.SetActive(false);
                go.SetActive(false);
                _views[i] = v;
            }
        }

        private void BuildShip()
        {
            var go = new GameObject("ShipRoot");
            go.transform.SetParent(_shipLayer, false);
            _shipRoot = go.AddComponent<RectTransform>();
            _shipRoot.anchorMin = _shipRoot.anchorMax = _shipRoot.pivot = new Vector2(0.5f, 0.5f);
            _shipGlow = MakeImage(_shipRoot, "Glow", RadialGlowSprite.Get());
            _shipBody = MakeImage(_shipRoot, "Body", PilotShipSprite.Get());
            _beam = MakeImage(_shipLayer, "Beam", null);
            _beam.color = new Color(1f, 214f / 255f, 120f / 255f, 0f);
            _beam.gameObject.SetActive(false);
        }

        private void BuildStrip()
        {
            _stripRim = Rr(_stripLayer, "StripRim", new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f), 23f);
            _stripFill = Rr(_stripLayer, "StripFill", new Color(PanelFill.r, PanelFill.g, PanelFill.b, 0.62f), 22f);
            for (int i = 0; i < _dots.Length; i++)
            {
                _dots[i] = MakeImage(_stripLayer, "Dot" + i, DiscSprite.Get());
                _dots[i].color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.4f);
            }
            _fingerRing = MakeImage(_stripLayer, "FingerRing", RingSprite.Get());
            _fingerRing.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.7f);
            _fingerDot = MakeImage(_stripLayer, "FingerDot", DiscSprite.Get());
            _fingerDot.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f);
            _stripLabel = MakeLabel(_stripLayer, "StripLabel", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _stripHint = MakeLabel(_stripLayer, "StripHint", 15f, UiFonts.Bold, Cyan, TextAnchor.MiddleCenter);
            _stripLabel.horizontalOverflow = _stripHint.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void BuildTop()
        {
            _journeyTrack = Rr(_topLayer, "JourneyTrack", new Color(Lavender.r, Lavender.g, Lavender.b, 0.25f), 2.5f);
            _journeyFill = Rr(_topLayer, "JourneyFill", Cyan, 2.5f);
            _sepA = MakeImage(_topLayer, "SepA", null);
            _sepB = MakeImage(_topLayer, "SepB", null);
            _sepA.color = _sepB.color = new Color(26f / 255f, 18f / 255f, 64f / 255f, 1f);
            _missionRim = Rr(_topLayer, "MissionRim", PanelEdge, 19f);
            _missionFill = Rr(_topLayer, "MissionFill", new Color(PanelFill.r, PanelFill.g, PanelFill.b, 0.95f), 17f);
            _missionIcon = MakeImage(_topLayer, "MissionIcon", null);
            _missionIcon.enabled = false;
            _missionTag = MakeLabel(_topLayer, "MissionTag", 14f, UiFonts.Bold, Gold, TextAnchor.MiddleLeft);
            _missionName = MakeLabel(_topLayer, "MissionName", 15f, UiFonts.Bold, TextColor, TextAnchor.MiddleLeft);
            _missionTag.horizontalOverflow = HorizontalWrapMode.Overflow;
            _missionName.horizontalOverflow = HorizontalWrapMode.Overflow;
            _missionName.resizeTextForBestFit = false;
            // el cielo de señales como zona protegida para el globo de Nubi en el tutorial (no se dibuja)
            var sz = new GameObject("SkyZone");
            sz.transform.SetParent(_play, false);
            _skyZone = sz.AddComponent<RectTransform>();
            _skyZone.anchorMin = _skyZone.anchorMax = _skyZone.pivot = new Vector2(0.5f, 0.5f);
        }

        private void BuildNotices()
        {
            _bannerGroup = _noticeLayer.gameObject.AddComponent<CanvasGroup>();
            _bannerGroup.blocksRaycasts = false;
            _bannerGroup.interactable = false;
            _bannerRim = Rr(_noticeLayer, "BannerRim", Gold, 19f);
            _bannerFill = Rr(_noticeLayer, "BannerFill", Pill, 18f);
            _bannerTag = MakeLabel(_noticeLayer, "BannerTag", 14f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _bannerTitle = MakeLabel(_noticeLayer, "BannerTitle", 18f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _bannerTag.horizontalOverflow = _bannerTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            _bannerGroup.alpha = 0f;
            _noticeLayer.gameObject.SetActive(false);
            for (int i = 0; i < _floatViews.Length; i++)
            {
                var f = new FloatView();
                var go = new GameObject("Float" + i);
                go.transform.SetParent(_fxLayer, false);
                f.Root = go.AddComponent<RectTransform>();
                f.Root.anchorMin = f.Root.anchorMax = f.Root.pivot = new Vector2(0.5f, 0.5f);
                f.Bg = Rr(f.Root, "Bg", Pill, 15f);
                f.Label = MakeLabel(f.Root, "Label", 15f, UiFonts.Bold, Good, TextAnchor.MiddleCenter);
                f.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
                go.SetActive(false);
                _floatViews[i] = f;
            }
        }

        private void BuildEnd()
        {
            _endTag = MakeLabel(_endLayer, "Tag", 14f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _endTitle = MakeLabel(_endLayer, "Title", 26f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            for (int i = 0; i < PilotContract.Sectors; i++)
            {
                _endSector[i] = MakeImage(_endLayer, "Sector" + i, DiscSprite.Get());
                _endSectorName[i] = MakeLabel(_endLayer, "SectorName" + i, 14f, UiFonts.Regular, Soft, TextAnchor.MiddleCenter);
                _endSectorName[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            for (int i = 0; i < _endLink.Length; i++)
            {
                _endLink[i] = MakeImage(_endLayer, "Link" + i, null);
                _endLink[i].color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f);
            }
            _endPanelRim = Rr(_endLayer, "PanelRim", PanelEdge, 19f);
            _endPanel = Rr(_endLayer, "Panel", new Color(20f / 255f, 18f / 255f, 58f / 255f, 0.96f), 17f);
            for (int i = 0; i < _endLabel.Length; i++)
            {
                _endLabel[i] = MakeLabel(_endLayer, "Label" + i, 15f, UiFonts.Regular, Soft, TextAnchor.MiddleLeft);
                _endValue[i] = MakeLabel(_endLayer, "Value" + i, 16f, UiFonts.Bold, Color.white, TextAnchor.MiddleRight);
                _endLabel[i].horizontalOverflow = _endValue[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            _endNote1 = MakeLabel(_endLayer, "Note1", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _endNote2 = MakeLabel(_endLayer, "Note2", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _endTag.text = PilotContract.EndTag;
            _endNote1.text = PilotContract.EndNote1;
            _endNote2.text = PilotContract.EndNote2;
            foreach (var t in new[] { _endTag, _endTitle, _endNote1, _endNote2 }) t.horizontalOverflow = HorizontalWrapMode.Wrap;
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

        private float _s = 3f, _playW = 1080f, _playH = 1920f, _logicalH = 640f;
        private PilotPlan _plan = new PilotPlan(640f, false);

        private Vector2 P(float x, float y) => new Vector2((x - PilotPlan.Width * 0.5f) * _s, _playH * 0.5f - y * _s);
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + PilotPlan.Width * 0.5f, (_playH * 0.5f - u.y) / _s);

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
            _s = _playW / PilotPlan.Width;
            _logicalH = _playH / _s;
            _plan = new PilotPlan(_logicalH, _guided);
            foreach (var kv in _fonts)
            {
                int px = Mathf.RoundToInt(kv.Value * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            foreach (var kv in _radii) SetRadius(kv.Key, kv.Value * _s);
            LayoutTint();
            LayoutShip();
            LayoutSignals();
            LayoutStrip();
            LayoutTop();
            LayoutNotices();
            LayoutEnd();
        }

        private void LayoutTint()
        {
            SetRect(_tintA.rectTransform, 60f, 220f, 360f, 360f);
            SetRect(_tintB.rectTransform, 270f, _logicalH * 0.62f, 380f, 380f);
        }

        private void LayoutShip()
        {
            _shipRoot.sizeDelta = Vector2.zero;
            float side = PilotContract.ShipSize * 1.28f;
            SetChild(_shipBody.rectTransform, 0f, 0f, side, side);
            SetChild(_shipGlow.rectTransform, 0f, 8f, PilotContract.ShipSize * 2.1f, PilotContract.ShipSize * 2.1f);
        }

        private void LayoutSignals()
        {
            foreach (var v in _views)
            {
                v.Root.sizeDelta = new Vector2(PilotContract.TouchRadius * 2f * _s, PilotContract.TouchRadius * 2f * _s);
                SetChild(v.Ring.rectTransform, 0f, 0f, PilotContract.SignalRingSize, PilotContract.SignalRingSize);
                float body = PilotContract.SignalSize * PilotSignalSprites.SideInDiameters;
                SetChild(v.Body.rectTransform, 0f, 0f, body, body);
                SetChild(v.Mark.rectTransform, 0f, 0f, 30f, 30f);
            }
        }

        private void LayoutStrip()
        {
            float top = _plan.StripVisualTop, bottom = _plan.StripBottom;
            float cy = (top + bottom) * 0.5f, h = bottom - top;
            SetRect(_stripRim.rectTransform, PilotPlan.Width * 0.5f, cy, 344f, h + 4f);
            SetRect(_stripFill.rectTransform, PilotPlan.Width * 0.5f, cy, 340f, h);
            SetRect(_stripLabel.rectTransform, PilotPlan.Width * 0.5f, top + 22f, 320f, 22f);
            SetRect(_stripHint.rectTransform, PilotPlan.Width * 0.5f, bottom - 14f, 340f, 24f);
            FingerY = top + 56f;
            SetRect(_fingerRing.rectTransform, _steerX, FingerY, 40f, 40f);
            SetRect(_fingerDot.rectTransform, _steerX, FingerY, 32f, 32f);
            foreach (var d in _dots) d.rectTransform.sizeDelta = new Vector2(5f * _s, 5f * _s);
        }

        /// <summary>La altura (dp) del círculo que sigue al dedo en la franja.</summary>
        private float FingerY;

        private void LayoutTop()
        {
            float cx = PilotPlan.Width * 0.5f;
            SetRect(_journeyTrack.rectTransform, cx, _plan.JourneyY, JourneyW, 5f);
            // la barra se llena de izquierda a derecha: el rect de relleno tiene su pivote a la izquierda
            _journeyFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            SetJourney(_journeyProgress);
            SetRect(_sepA.rectTransform, JourneyLeft + JourneyW / 3f, _plan.JourneyY, 2f, 9f);
            SetRect(_sepB.rectTransform, JourneyLeft + JourneyW * 2f / 3f, _plan.JourneyY, 2f, 9f);
            float my = (_plan.MissionTop + _plan.MissionBottom) * 0.5f, mw = PilotPlan.Width - 24f;
            SetRect(_missionRim.rectTransform, cx, my, mw + 3f, 39f);
            SetRect(_missionFill.rectTransform, cx, my, mw, 36f);
            SetRect(_missionIcon.rectTransform, 34f, my, 28f, 28f);
            PlaceMissionTexts();
            var sky = _plan.SkyBox;
            SetRect(_skyZone, PilotPlan.Width * 0.5f, (sky.Y0 + sky.Y1) * 0.5f, sky.X1 - sky.X0, sky.Y1 - sky.Y0);
        }

        private const float JourneyLeft = 12f, JourneyW = PilotPlan.Width - 24f;

        private void SetJourney(float progress)
        {
            _journeyProgress = Mathf.Clamp01(progress);
            var r = _journeyFill.rectTransform;
            r.sizeDelta = new Vector2(Mathf.Max(0.01f, JourneyW * _journeyProgress) * _s, 5f * _s);
            r.anchoredPosition = P(JourneyLeft, _plan.JourneyY);
        }

        private float _journeyProgress;

        private void LayoutNotices()
        {
            var b = _plan.BannerBox;
            float cx = (b.X0 + b.X1) * 0.5f, cy = (b.Y0 + b.Y1) * 0.5f, w = b.X1 - b.X0, h = b.Y1 - b.Y0;
            SetRect(_bannerRim.rectTransform, cx, cy, w + 3f, h + 3f);
            SetRect(_bannerFill.rectTransform, cx, cy, w, h);
            SetRect(_bannerTag.rectTransform, cx, b.Y0 + 15f, w - 16f, 22f);
            SetRect(_bannerTitle.rectTransform, cx, b.Y0 + 37f, w - 16f, 28f);
        }

        private void LayoutEnd()
        {
            float cx = PilotPlan.Width * 0.5f, y = PilotPlan.HudBottom;
            SetRect(_endTag.rectTransform, cx, y + 20f, 300f, 22f);
            SetRect(_endTitle.rectTransform, cx, y + 50f, 330f, 36f);
            // el mapa del viaje: tres sectores unidos por una línea punteada
            float my = y + 108f;
            for (int i = 0; i < PilotContract.Sectors; i++)
            {
                float x = 80f + i * 100f;
                SetRect(_endSector[i].rectTransform, x, my, 36f, 36f);
                SetRect(_endSectorName[i].rectTransform, x, my + 42f, 96f, 40f);
            }
            for (int i = 0; i < _endLink.Length; i++)
                SetRect(_endLink[i].rectTransform, 130f + i * 100f, my, 36f, 3f);
            float py = y + 190f, rowH = 29f, ph = rowH * _endLabel.Length + 18f;
            SetRect(_endPanelRim.rectTransform, cx, py + ph * 0.5f, 318f, ph + 3f);
            SetRect(_endPanel.rectTransform, cx, py + ph * 0.5f, 314f, ph);
            for (int i = 0; i < _endLabel.Length; i++)
            {
                float ry = py + 9f + rowH * (i + 0.5f);
                SetRect(_endLabel[i].rectTransform, 40f + 105f, ry, 210f, 24f);
                SetRect(_endValue[i].rectTransform, 320f - 70f, ry, 140f, 24f);
            }
            SetRect(_endNote1.rectTransform, cx, py + ph + 24f, 336f, 22f);
            SetRect(_endNote2.rectTransform, cx, py + ph + 46f, 336f, 22f);
        }

        /// <summary>Lo que ocupan, abajo, el rótulo «Práctica: no cuenta» y «Saltar tutorial» (dp).</summary>
        private const float TutorialControlsDp = PilotPlan.TutorialControls;
    }
}
