using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Satelites
{
    public sealed partial class SatelliteGameController
    {
        private const int SatPool = 13, FlightPool = 6, RoundSlots = SatelliteContract.PrecisionRounds;

        private static readonly Color Gold = new Color(255f / 255f, 201f / 255f, 74f / 255f);
        private static readonly Color Cyan = new Color(127f / 255f, 216f / 255f, 255f / 255f);
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Dim = new Color(143f / 255f, 138f / 255f, 192f / 255f);
        private static readonly Color Soft = new Color(214f / 255f, 209f / 255f, 242f / 255f);
        private static readonly Color Good = new Color(166f / 255f, 227f / 255f, 107f / 255f);
        private static readonly Color Bad = new Color(255f / 255f, 138f / 255f, 107f / 255f);
        private static readonly Color TextColor = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color PanelFill = new Color(20f / 255f, 18f / 255f, 58f / 255f);
        private static readonly Color InkEdge = new Color(26f / 255f, 18f / 255f, 64f / 255f);

        /// <summary>Un satélite: su raíz de 60 dp (el área de toque y el hueco del tutorial), el resplandor y el aro de la señal, el sobre, el aro celeste de «marcado» y, al revelar, la marca y el aro punteado.</summary>
        private sealed class SatView
        {
            public RectTransform Root;
            public Image Glow, Body, CueRing, Envelope, MarkRing, Badge, Dashed;
        }

        /// <summary>Un sobre que vuela de un satélite al planeta (o, con «quitar animaciones», se queda y se apaga) y, al llegar, enciende una luz.</summary>
        private sealed class Flight
        {
            public RectTransform Root;
            public Image Glow, Envelope;
            public bool Active, Landed;
            public Vector2 From, To;
            public float At, Duration = SatelliteContract.FlySeconds;
            public int Light;
        }

        /// <summary>Una luz del planeta: un resplandor y un punto.</summary>
        private sealed class LightDot
        {
            public RectTransform Root;
            public Image Glow, Core;
            public Vector2 Spot;
            public bool On;
            public float At;
        }

        private RectTransform _safe, _play, _orbitLayer, _planetRoot, _satLayer, _cloudLayer, _fxLayer, _promptLayer, _footerLayer, _surpriseLayer, _endLayer;
        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();
        private readonly List<KeyValuePair<Image, float>> _radii = new List<KeyValuePair<Image, float>>();

        private readonly OrbitLineGraphic[] _orbitLines = new OrbitLineGraphic[3];
        private Image _halo, _planetBody;
        private readonly LightDot[] _lightDots = new LightDot[SatelliteContract.MaxLightDots];
        private readonly SatView[] _sats = new SatView[SatPool];
        private readonly Flight[] _flights = new Flight[FlightPool];
        private Image _cloud;
        private Text _promptA, _promptB, _lightsText;
        private RectTransform _timerBg, _timerFill;
        private Image _timerFillImage;
        private readonly Image[] _discRing = new Image[RoundSlots], _discFill = new Image[RoundSlots];

        // la revelación
        private RectTransform _crossMarker;
        private Image _crossRing, _crossPillBg;
        private Text _crossLabel;

        // el aviso de sorpresa
        private CanvasGroup _surpriseGroup;
        private Image _surpriseDim, _surpriseCard, _surpriseCardRim, _surpriseIcon;
        private Text _surpriseTag, _surpriseName, _surpriseLine;

        // la pantalla final
        private Text _endTag, _endTitle, _endLabel, _endValue, _endDetail, _endNote1, _endNote2, _endRecord;

        // el zumbido del seguimiento (se repite)
        private AudioSource _humSource;

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("SatelliteCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.MissionControl);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, SatelliteContract.Title, MarginU, this);

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _orbitLayer = Layer(_play, "Orbits");
            _planetRoot = Layer(_play, "Planet");
            _satLayer = Layer(_play, "Satellites");
            _cloudLayer = Layer(_play, "Cloud");
            _fxLayer = Layer(_play, "Fx");
            _promptLayer = Layer(_play, "Prompts");
            _footerLayer = Layer(_play, "Footer");
            _surpriseLayer = Layer(_play, "Surprise");
            _endLayer = Layer(_play, "End");
            BuildOrbits();
            BuildPlanet();
            BuildSats();
            BuildCloud();
            BuildFx();
            BuildPrompts();
            BuildFooter();
            BuildTimer();
            BuildSurprise();
            BuildEnd();
            BuildHiddenResult();

            _exit = new ExitButton(_safe, this, UnitsPerDp);

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _humSource = gameObject.AddComponent<AudioSource>();
            _humSource.loop = true;
            _humSource.playOnAwake = false;
            _humSource.volume = 0f;

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
            SetUpTutorial();
            ResetRoundViews();
        }

        private void BuildOrbits()
        {
            for (int i = 0; i < _orbitLines.Length; i++)
            {
                var go = new GameObject("Orbit" + i, typeof(RectTransform), typeof(CanvasRenderer));
                go.transform.SetParent(_orbitLayer, false);
                var r = (RectTransform)go.transform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = Vector2.zero;
                var g = go.AddComponent<OrbitLineGraphic>();
                g.raycastTarget = false;
                _orbitLines[i] = g;
            }
        }

        private void BuildPlanet()
        {
            _planetRoot.anchorMin = _planetRoot.anchorMax = _planetRoot.pivot = new Vector2(0.5f, 0.5f);
            _planetRoot.sizeDelta = Vector2.zero;
            _halo = MakeImage(_planetRoot, "Halo", RadialGlowSprite.Get());
            _halo.color = new Color(1f, 214f / 255f, 120f / 255f, 0f);
            _planetBody = MakeImage(_planetRoot, "Body", null);
            _planetBody.enabled = false;                          // sin sprite sería un cuadrado blanco: se enciende al hornear
            for (int i = 0; i < _lightDots.Length; i++)
            {
                var d = new LightDot();
                var go = new GameObject("Light" + i);
                go.transform.SetParent(_planetRoot, false);
                d.Root = go.AddComponent<RectTransform>();
                d.Root.anchorMin = d.Root.anchorMax = d.Root.pivot = new Vector2(0.5f, 0.5f);
                d.Glow = MakeImage(d.Root, "Glow", RadialGlowSprite.Get());
                d.Glow.color = new Color(1f, 220f / 255f, 140f / 255f, 1f);
                d.Core = MakeImage(d.Root, "Core", DiscSprite.Get());
                d.Core.color = new Color(1f, 231f / 255f, 168f / 255f, 1f);
                go.SetActive(false);
                _lightDots[i] = d;
            }
        }

        private void BuildSats()
        {
            for (int i = 0; i < _sats.Length; i++)
            {
                var v = new SatView();
                var go = new GameObject("Satellite" + i);
                go.transform.SetParent(_satLayer, false);
                v.Root = go.AddComponent<RectTransform>();
                v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
                v.Glow = MakeImage(v.Root, "Glow", RadialGlowSprite.Get());
                v.Glow.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
                v.Body = MakeImage(v.Root, "Body", null);
                v.CueRing = MakeImage(v.Root, "CueRing", RingSprite.Get());
                v.CueRing.color = Gold;
                v.Envelope = MakeImage(v.Root, "Envelope", null);
                v.MarkRing = MakeImage(v.Root, "MarkRing", RingSprite.Get());
                v.MarkRing.color = Cyan;
                v.Dashed = MakeImage(v.Root, "Dashed", null);
                v.Dashed.color = Bad;
                v.Badge = MakeImage(v.Root, "Badge", null);
                go.SetActive(false);
                v.CueRing.gameObject.SetActive(false);
                v.Envelope.gameObject.SetActive(false);
                v.MarkRing.gameObject.SetActive(false);
                v.Dashed.gameObject.SetActive(false);
                v.Badge.gameObject.SetActive(false);
                v.Glow.gameObject.SetActive(false);
                _sats[i] = v;
            }
        }

        private void BuildCloud()
        {
            _cloud = MakeImage(_cloudLayer, "DustCloud", null);
            _cloud.gameObject.SetActive(false);
        }

        private void BuildFx()
        {
            for (int i = 0; i < _flights.Length; i++)
            {
                var f = new Flight();
                var go = new GameObject("Flight" + i);
                go.transform.SetParent(_fxLayer, false);
                f.Root = go.AddComponent<RectTransform>();
                f.Root.anchorMin = f.Root.anchorMax = f.Root.pivot = new Vector2(0.5f, 0.5f);
                f.Glow = MakeImage(f.Root, "Glow", RadialGlowSprite.Get());
                f.Glow.color = new Color(1f, 214f / 255f, 120f / 255f, 0.7f);
                f.Envelope = MakeImage(f.Root, "Envelope", null);
                go.SetActive(false);
                _flights[i] = f;
            }
            // «¿Aquí se cruzaron?»: un aro coral y su rótulo
            var cm = new GameObject("CrossMarker");
            cm.transform.SetParent(_fxLayer, false);
            _crossMarker = cm.AddComponent<RectTransform>();
            _crossMarker.anchorMin = _crossMarker.anchorMax = _crossMarker.pivot = new Vector2(0.5f, 0.5f);
            _crossRing = MakeImage(_crossMarker, "Ring", RingSprite.Get());
            _crossRing.color = Bad;
            _crossPillBg = Rr(_crossMarker, "PillBg", new Color(8f / 255f, 10f / 255f, 34f / 255f, 0.94f), 13f);
            _crossLabel = MakeLabel(_crossMarker, "Label", 15f, UiFonts.Bold, Bad, TextAnchor.MiddleCenter);
            _crossLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _crossLabel.text = SatelliteContract.CrossText;
            cm.SetActive(false);
        }

        private void BuildPrompts()
        {
            _promptA = MakeLabel(_promptLayer, "PromptA", 20f, UiFonts.Bold, TextColor, TextAnchor.MiddleCenter);
            _promptB = MakeLabel(_promptLayer, "PromptB", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _promptA.horizontalOverflow = HorizontalWrapMode.Wrap;
            _promptB.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void BuildFooter()
        {
            for (int i = 0; i < RoundSlots; i++)
            {
                _discRing[i] = MakeImage(_footerLayer, "DiscRing" + i, RingSprite.Get());
                _discFill[i] = MakeImage(_footerLayer, "DiscFill" + i, DiscSprite.Get());
                _discFill[i].gameObject.SetActive(false);
            }
            _lightsText = MakeLabel(_footerLayer, "Lights", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _lightsText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void BuildTimer()
        {
            var bg = new GameObject("TimerBar");
            bg.transform.SetParent(_play, false);
            _timerBg = bg.AddComponent<RectTransform>();
            _timerBg.anchorMin = _timerBg.anchorMax = _timerBg.pivot = new Vector2(0.5f, 0.5f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = RoundedRectSprite.Get(10);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1f, 1f, 1f, 0.12f);
            bgImg.raycastTarget = false;
            var fill = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            _timerFill = fill.AddComponent<RectTransform>();
            _timerFill.anchorMin = Vector2.zero;
            _timerFill.anchorMax = Vector2.one;
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            _timerFillImage = fill.AddComponent<Image>();
            _timerFillImage.sprite = RoundedRectSprite.Get(10);
            _timerFillImage.type = Image.Type.Sliced;
            _timerFillImage.raycastTarget = false;
            _timerFillImage.color = Good;
            bg.SetActive(false);
        }

        private void BuildSurprise()
        {
            _surpriseGroup = _surpriseLayer.gameObject.AddComponent<CanvasGroup>();
            _surpriseGroup.blocksRaycasts = false;
            _surpriseDim = MakeImage(_surpriseLayer, "Dim", null);
            _surpriseDim.color = new Color(4f / 255f, 5f / 255f, 26f / 255f, 0.6f);
            Stretch(_surpriseDim.rectTransform);
            _surpriseCardRim = Rr(_surpriseLayer, "CardRim", Gold, 23.5f);
            _surpriseCard = Rr(_surpriseLayer, "Card", PanelFill, 22f);
            _surpriseIcon = MakeImage(_surpriseLayer, "Icon", null);
            _surpriseTag = MakeLabel(_surpriseLayer, "Tag", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _surpriseName = MakeLabel(_surpriseLayer, "Name", 25f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _surpriseLine = MakeLabel(_surpriseLayer, "Line", 16f, UiFonts.Regular, Soft, TextAnchor.MiddleCenter);
            foreach (var t in new[] { _surpriseTag, _surpriseName }) t.horizontalOverflow = HorizontalWrapMode.Overflow;
            _surpriseLayer.gameObject.SetActive(false);
        }

        private void BuildEnd()
        {
            _endTag = MakeLabel(_endLayer, "Tag", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _endTitle = MakeLabel(_endLayer, "Title", 27f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _endLabel = MakeLabel(_endLayer, "Label", 15f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _endValue = MakeLabel(_endLayer, "Value", 30f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _endDetail = MakeLabel(_endLayer, "Detail", 15f, UiFonts.Regular, Soft, TextAnchor.MiddleCenter);
            _endRecord = MakeLabel(_endLayer, "Record", 15f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _endNote1 = MakeLabel(_endLayer, "Note1", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _endNote2 = MakeLabel(_endLayer, "Note2", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            foreach (var t in new[] { _endTag, _endTitle, _endLabel, _endValue, _endDetail, _endRecord, _endNote1, _endNote2 }) t.horizontalOverflow = HorizontalWrapMode.Wrap;
            _endTag.text = "FIN DE LA PARTIDA";
            _endLabel.text = "Tu seguimiento";
            _endNote1.text = "Descuenta lo que se acierta por suerte.";
            _endNote2.text = "Medida de esta partida. No es un diagnóstico.";
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

        /// <summary>Pone los sprites horneados.</summary>
        private void AssignSprites()
        {
            _planetBody.sprite = SatelliteSprites.Planet();
            _planetBody.enabled = true;
            _cloud.sprite = SatelliteSprites.Cloud();
            _surpriseIcon.sprite = SatelliteSprites.Cloud();
            foreach (var v in _sats)
            {
                v.Body.sprite = SatelliteSprites.Body();
                v.Envelope.sprite = SatelliteSprites.Envelope();
                v.Dashed.sprite = SatelliteSprites.DashedRing();
            }
            foreach (var f in _flights) f.Envelope.sprite = SatelliteSprites.Envelope();
        }

        // ------------------------------------------------------------------ disposición (dp lógicos de un campo de 360 de ancho; 1 dp = _s unidades)

        private float _s = 3f, _playW = 1080f, _playH = 1920f, _logicalH = 640f;
        private ScreenPlan _plan = new ScreenPlan(640f, false);

        private Vector2 P(float x, float y) => new Vector2((x - ScreenPlan.Width * 0.5f) * _s, _playH * 0.5f - y * _s);
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + ScreenPlan.Width * 0.5f, (_playH * 0.5f - u.y) / _s);

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
            _s = _playW / ScreenPlan.Width;
            _logicalH = _playH / _s;
            _plan = new ScreenPlan(_logicalH, _guided);
            foreach (var kv in _fonts)
            {
                int px = Mathf.RoundToInt(kv.Value * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            foreach (var kv in _radii) SetRadius(kv.Key, kv.Value * _s);
            LayoutOrbits();
            LayoutPlanet();
            LayoutSats();
            LayoutTexts();
            LayoutSurprise();
            LayoutEnd();
        }

        private void LayoutOrbits()
        {
            var o = _plan.Orbits;
            for (int i = 0; i < 3; i++)
            {
                _orbitLines[i].rectTransform.anchoredPosition = P(_plan.Cx, _plan.Cy);
                _orbitLines[i].Set(o.Rx[i] * _s, o.Rx[i] * o.RyK * _s, 1.2f * _s, 3f * _s, 7f * _s, new Color(Cyan.r, Cyan.g, Cyan.b, 0.16f));
            }
        }

        private void LayoutPlanet()
        {
            _planetRoot.anchorMin = _planetRoot.anchorMax = new Vector2(0.5f, 0.5f);
            if (!_planetBig) _planetRoot.anchoredPosition = P(_plan.Cx, _plan.Cy);
            float side = SatelliteContract.PlanetRadius * SatelliteSprites.PlanetSpriteSideInRadii;
            SetChild(_planetBody.rectTransform, 0f, 0f, side, side);
            SetChild(_halo.rectTransform, 0f, 0f, SatelliteContract.PlanetRadius * 4.2f, SatelliteContract.PlanetRadius * 4.2f);
            foreach (var d in _lightDots)
            {
                SetChild(d.Root, d.Spot.x, d.Spot.y, 18f, 18f);
                SetChild(d.Glow.rectTransform, 0f, 0f, 18f, 18f);
                SetChild(d.Core.rectTransform, 0f, 0f, 4.4f, 4.4f);
            }
        }

        private void LayoutSats()
        {
            foreach (var v in _sats)
            {
                SetChild(v.Root, 0f, 0f, 60f, 60f);
                // el sprite 'Body' trae el borde, la sombra y los paneles
                float body = SatelliteContract.SatRadius * SatelliteSprites.BodySpriteSideInDiscRadii;
                SetChild(v.Glow.rectTransform, 0f, 0f, 64f, 64f);
                SetChild(v.Body.rectTransform, 0f, 0f, body, body);
                SetChild(v.CueRing.rectTransform, 0f, 0f, (SatelliteContract.SatRadius + 6f) / 0.48f, (SatelliteContract.SatRadius + 6f) / 0.48f);
                SetChild(v.Envelope.rectTransform, 0f, -(SatelliteContract.SatRadius + 14f), 36.7f, 36.7f);
                SetChild(v.MarkRing.rectTransform, 0f, 0f, (SatelliteContract.SatRadius + 7f) / 0.48f, (SatelliteContract.SatRadius + 7f) / 0.48f);
                SetChild(v.Dashed.rectTransform, 0f, 0f, (SatelliteContract.SatRadius + 11f) / 0.47f, (SatelliteContract.SatRadius + 11f) / 0.47f);
                SetChild(v.Badge.rectTransform, 15f, -15f, 22f, 22f);
            }
            foreach (var f in _flights)
            {
                SetChild(f.Glow.rectTransform, 0f, 0f, 32f, 32f);
                SetChild(f.Envelope.rectTransform, 0f, 0f, 36.7f, 36.7f);
                f.Root.sizeDelta = Vector2.zero;
            }
            _cloud.rectTransform.sizeDelta = new Vector2(200f * _s, 200f * _s);
            SetChild(_crossRing.rectTransform, 0f, 0f, 50f, 50f);
        }

        private void LayoutTexts()
        {
            SetRect(_promptA.rectTransform, ScreenPlan.Width * 0.5f, _plan.PromptAY, ScreenPlan.Width - 28f, 30f);
            SetRect(_promptB.rectTransform, ScreenPlan.Width * 0.5f, _plan.PromptBY, ScreenPlan.Width - 28f, 22f);
            // la barra del Reto, justo bajo el marcador
            SetRect(_timerBg, ScreenPlan.Width * 0.5f, _plan.TimerY, ScreenPlan.Width - 40f, 5f);
            // los discos de las rondas y las luces encendidas
            const float gap = 26f;
            float x0 = ScreenPlan.Width * 0.5f - gap * (RoundSlots - 1) * 0.5f;
            for (int i = 0; i < RoundSlots; i++)
            {
                SetRect(_discRing[i].rectTransform, x0 + i * gap, _plan.DiscsY, 20f, 20f);
                SetRect(_discFill[i].rectTransform, x0 + i * gap, _plan.DiscsY, 14f, 14f);
            }
            SetRect(_lightsText.rectTransform, ScreenPlan.Width * 0.5f, _plan.LightsY, 300f, 22f);
        }

        private void LayoutSurprise()
        {
            float cy = _plan.Cy - 30f;
            float w = ScreenPlan.Width - 56f, h = 168f;
            SetRect(_surpriseCardRim.rectTransform, ScreenPlan.Width * 0.5f, cy, w + 5f, h + 5f);
            SetRect(_surpriseCard.rectTransform, ScreenPlan.Width * 0.5f, cy, w, h);
            SetRect(_surpriseTag.rectTransform, ScreenPlan.Width * 0.5f, cy - h * 0.5f + 24f, w - 24f, 22f);
            SetRect(_surpriseName.rectTransform, ScreenPlan.Width * 0.5f, cy - h * 0.5f + 62f, w - 24f, 34f);
            SetRect(_surpriseLine.rectTransform, ScreenPlan.Width * 0.5f, cy + h * 0.5f - 44f, w - 36f, 54f);
            SetRect(_surpriseIcon.rectTransform, -1000f, -1000f, 1f, 1f);       // sin icono por ahora (el aviso es texto)
        }

        private void LayoutEnd()
        {
            float cx = ScreenPlan.Width * 0.5f;
            float H = _logicalH;
            SetRect(_endTag.rectTransform, cx, ScreenPlan.HudBottom + 22f, 300f, 22f);
            SetRect(_endTitle.rectTransform, cx, ScreenPlan.HudBottom + 54f, 330f, 36f);
            // el planeta grande ocupa el medio (lo coloca FinishGame): el bloque de medidas va debajo
            float y0 = Mathf.Min(H - 250f, ScreenPlan.HudBottom + 74f + 168f + 40f);
            SetRect(_endLabel.rectTransform, cx, y0, 300f, 22f);
            SetRect(_endValue.rectTransform, cx, y0 + 34f, 330f, 40f);
            SetRect(_endDetail.rectTransform, cx, y0 + 70f, 330f, 40f);
            SetRect(_endRecord.rectTransform, cx, y0 + 108f, 330f, 22f);
            SetRect(_endNote1.rectTransform, cx, y0 + 138f, 330f, 22f);
            SetRect(_endNote2.rectTransform, cx, y0 + 160f, 330f, 22f);
        }

        /// <summary>Lo que ocupan, abajo, el rótulo «Práctica: no cuenta» y «Saltar tutorial» (dp).</summary>
        private const float TutorialControlsDp = ScreenPlan.TutorialControls;
    }
}
