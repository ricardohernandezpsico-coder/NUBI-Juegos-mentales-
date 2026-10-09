using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Parejas
{
    public sealed partial class ConstelacionGameController
    {
        private const int LightPool = 24, LinkPool = 16, SparkPool = 60, FloatPool = 4, DotPool = 30, IntroPool = 20;

        private static readonly Color Gold = new Color(255f / 255f, 201f / 255f, 74f / 255f);
        private static readonly Color GoldSoft = new Color(255f / 255f, 231f / 255f, 168f / 255f);
        private static readonly Color Cyan = new Color(127f / 255f, 216f / 255f, 255f / 255f);
        private static readonly Color CyanSoft = new Color(191f / 255f, 233f / 255f, 255f / 255f);
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Mint = new Color(159f / 255f, 245f / 255f, 214f / 255f);
        private static readonly Color PanelFill = new Color(20f / 255f, 27f / 255f, 58f / 255f);
        private static readonly Color TextColor = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color Muted = new Color(110f / 255f, 106f / 255f, 154f / 255f);
        private static readonly Color InkEdge = new Color(26f / 255f, 18f / 255f, 64f / 255f);

        /// <summary>Una luz: el aro de pareja hecha, el brillo de pista, y el cuerpo con su objeto (que se voltea con un resorte).</summary>
        private sealed class LightView
        {
            public RectTransform Root, Flip;
            public Image Hint, HintRing, Ring, Body, Obj;
            public float F, Vf;
            public float HintAt = -10f, PressAt = -10f;
            public int Kind = -1, Variant = -1;
            public bool Up;
            public ConLine RingKind;
        }

        /// <summary>Una línea entre dos luces: el brillo, el borde oscuro y el trazo (tres mallas), y la chispa de la punta mientras se traza.</summary>
        private sealed class LinkView
        {
            public LineGraphic Glow, Border, Core;
            public Image Spark;
            public ConLink Link;
            public bool Settled, WasGlowing;
            public float LastB = -1f, LastRa = -1f, LastRb = -1f;
        }

        private sealed class FloatView
        {
            public RectTransform Root;
            public Image Bg;
            public Text Text;
            public float At = -10f;
            public Vector2 Pos;
            public Color Color;
        }

        private sealed class DotView
        {
            public RectTransform Root;
            public Image Border, Fill, Ring;
        }

        private sealed class Spark { public Image Img; public Vector2 Pos, Vel; public float Age, Life, Size; public bool Alive; public Color Color; }

        // las capas y las piezas
        private RectTransform _safe, _play, _boardRoot, _lightLayer, _linkLayer, _cardLayer, _rowLayer, _rowRect, _fxLayer, _introLayer;
        private RectTransform _timerTrack, _timerHead;
        private Image _timerFill;
        private readonly LightView[] _lv = new LightView[LightPool];
        private readonly List<LinkView> _links = new List<LinkView>();
        private readonly Dictionary<ConLink, float> _linkGlowAt = new Dictionary<ConLink, float>();
        private readonly LineGraphic[] _thread = new LineGraphic[2];
        private readonly FloatView[] _floats = new FloatView[FloatPool];
        private readonly DotView[] _dots = new DotView[DotPool];
        private readonly List<Spark> _sparks = new List<Spark>();
        // la tarjeta de arriba y la fila de abajo
        private Image _cardRim, _cardBg;
        private Text _cardT, _cardS, _rowLabel, _rowCount;
        private CanvasGroup _cardGroup;
        // la tarjeta «NUEVO»
        private Image _dim, _introCard, _introRim;
        private Text _introTag, _introBody, _introTap;
        private CanvasGroup _introGroup;
        private readonly List<Image> _introImgs = new List<Image>();
        private readonly List<Text> _introLabels = new List<Text>();
        private LineGraphic _introLine, _introLine2;

        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("ConstelacionCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.CieloDeCristal);           // el cielo nocturno quieto de Rastro de luz: aquí lo que se mueve son las luces

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Constelaciones", MarginU, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _cardLayer = Layer(_play, "Card");
            _boardRoot = Layer(_play, "Sky");
            _rowLayer = Layer(_play, "Row");
            _fxLayer = Layer(_play, "Fx");
            _introLayer = Layer(_play, "Intro");
            BuildCard();
            BuildSky();
            BuildRow();
            BuildFx();
            BuildIntro();

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
            SetUpTutorial();
        }

        private void BuildTimer()
        {
            var go = new GameObject("TimeTrack");
            go.transform.SetParent(_safe, false);
            _timerTrack = go.AddComponent<RectTransform>();
            _timerTrack.anchorMin = _timerTrack.anchorMax = new Vector2(0.5f, 1f);
            _timerTrack.pivot = new Vector2(0.5f, 0.5f);
            _timerTrack.sizeDelta = new Vector2(960f, 8f);
            _timerTrack.anchoredPosition = new Vector2(0f, -(GameHud.Height + 6f));
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(4);
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.1f);
            img.raycastTarget = false;
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            var fr = fillGo.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            _timerFill = fillGo.AddComponent<Image>();
            _timerFill.sprite = RoundedRectSprite.Get(4);
            _timerFill.type = Image.Type.Sliced;
            _timerFill.color = NeuroStyle.WithAlpha(Cyan, 0.8f);
            _timerFill.raycastTarget = false;
            var head = MakeImage(go.transform, "Head", RadialGlowSprite.Get());
            _timerHead = head.rectTransform;
            _timerHead.anchorMin = _timerHead.anchorMax = new Vector2(1f, 0.5f);
            _timerHead.sizeDelta = new Vector2(70f, 70f);
            head.color = NeuroStyle.WithAlpha(Cyan, 0.7f);
        }

        // ------------------------------------------------------------------ la tarjeta de arriba y la fila de abajo

        private void BuildCard()
        {
            _cardGroup = _cardLayer.gameObject.AddComponent<CanvasGroup>();
            _cardGroup.blocksRaycasts = false;
            _cardRim = MakeImage(_cardLayer, "Rim", RoundedRectSprite.Get(24));
            _cardBg = MakeImage(_cardLayer, "Bg", RoundedRectSprite.Get(24));
            _cardBg.color = PanelFill;
            _cardT = MakeLabel(_cardLayer, "Title", 18f, UiFonts.Bold, TextColor, TextAnchor.MiddleCenter);
            _cardS = MakeLabel(_cardLayer, "Sub", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            BestFit(_cardT, Mathf.RoundToInt(14f * UnitsPerDp));
            BestFit(_cardS, Mathf.RoundToInt(14f * UnitsPerDp));
            _cardT.horizontalOverflow = HorizontalWrapMode.Wrap;
            _cardS.horizontalOverflow = HorizontalWrapMode.Wrap;
            _cardLayer.gameObject.SetActive(false);
        }

        private void BuildRow()
        {
            var rr = new GameObject("RowZone");
            rr.transform.SetParent(_rowLayer, false);
            _rowRect = rr.AddComponent<RectTransform>();
            _rowRect.anchorMin = _rowRect.anchorMax = _rowRect.pivot = new Vector2(0.5f, 0.5f);
            _rowLabel = MakeLabel(_rowLayer, "Label", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleLeft);
            _rowCount = MakeLabel(_rowLayer, "Count", 15f, UiFonts.Bold, TextColor, TextAnchor.MiddleRight);
            foreach (var t in new[] { _rowLabel, _rowCount }) { t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; }
            _rowLabel.text = ConstelacionContract.MemoryLabel;
            for (int i = 0; i < _dots.Length; i++)
            {
                var d = new DotView();
                var go = new GameObject("Dot" + i);
                go.transform.SetParent(_rowLayer, false);
                d.Root = go.AddComponent<RectTransform>();
                d.Root.anchorMin = d.Root.anchorMax = d.Root.pivot = new Vector2(0.5f, 0.5f);
                d.Border = MakeImage(d.Root, "Border", DiscSprite.Get());
                d.Border.color = InkEdge;
                d.Fill = MakeImage(d.Root, "Fill", DiscSprite.Get());
                d.Fill.color = Gold;
                d.Ring = MakeImage(d.Root, "Ring", RingSprite.Get());
                d.Ring.color = Lavender;
                go.SetActive(false);
                _dots[i] = d;
            }
            _rowLayer.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ el cielo: las luces, las líneas y el hilo del trío

        private void BuildSky()
        {
            var br = _boardRoot;
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = Vector2.zero;
            _lightLayer = Layer(br, "Lights");
            _lightLayer.anchorMin = _lightLayer.anchorMax = _lightLayer.pivot = new Vector2(0.5f, 0.5f);
            _lightLayer.sizeDelta = Vector2.zero;
            for (int i = 0; i < _lv.Length; i++) _lv[i] = BuildLight(_lightLayer, i);
            // las líneas van SOBRE todas las luces (con borde oscuro) y nacen en el borde de las suyas
            _linkLayer = Layer(br, "Links");
            _linkLayer.anchorMin = _linkLayer.anchorMax = _linkLayer.pivot = new Vector2(0.5f, 0.5f);
            _linkLayer.sizeDelta = Vector2.zero;
            for (int i = 0; i < LinkPool; i++) _links.Add(BuildLink(_linkLayer, i));
            for (int i = 0; i < _thread.Length; i++) _thread[i] = NewLine(_linkLayer, "Thread" + i);
            br.gameObject.SetActive(false);
        }

        private LightView BuildLight(Transform parent, int i)
        {
            var v = new LightView();
            var go = new GameObject("Light" + i);
            go.transform.SetParent(parent, false);
            v.Root = go.AddComponent<RectTransform>();
            v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
            v.Root.sizeDelta = Vector2.zero;
            v.Hint = MakeImage(v.Root, "Hint", RadialGlowSprite.Get());
            v.Hint.color = new Color(GoldSoft.r, GoldSoft.g, GoldSoft.b, 0f);
            v.HintRing = MakeImage(v.Root, "HintRing", null);
            v.HintRing.color = new Color(GoldSoft.r, GoldSoft.g, GoldSoft.b, 0f);
            v.Ring = MakeImage(v.Root, "Ring", null, false);
            var flip = new GameObject("Flip");
            flip.transform.SetParent(v.Root, false);
            v.Flip = flip.AddComponent<RectTransform>();
            v.Flip.anchorMin = v.Flip.anchorMax = v.Flip.pivot = new Vector2(0.5f, 0.5f);
            v.Flip.sizeDelta = Vector2.zero;
            v.Body = MakeImage(v.Flip, "Body", null);
            v.Obj = MakeImage(v.Flip, "Obj", null, false);
            go.SetActive(false);
            return v;
        }

        private LinkView BuildLink(Transform parent, int i)
        {
            var v = new LinkView();
            v.Glow = NewLine(parent, "Glow" + i);
            v.Border = NewLine(parent, "Border" + i);
            v.Core = NewLine(parent, "Core" + i);
            v.Spark = MakeImage(parent, "Spark" + i, RadialGlowSprite.Get(), false);
            return v;
        }

        private static LineGraphic NewLine(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = Vector2.zero;
            var g = go.AddComponent<LineGraphic>();
            g.raycastTarget = false;
            go.SetActive(false);
            return g;
        }

        private void BuildFx()
        {
            for (int i = 0; i < SparkPool; i++)
            {
                var img = MakeImage(_fxLayer, "Spark", DiscSprite.Get(), false);
                _sparks.Add(new Spark { Img = img });
            }
            for (int i = 0; i < _floats.Length; i++)
            {
                var f = new FloatView();
                var go = new GameObject("Float" + i);
                go.transform.SetParent(_fxLayer, false);
                f.Root = go.AddComponent<RectTransform>();
                f.Root.anchorMin = f.Root.anchorMax = f.Root.pivot = new Vector2(0.5f, 0.5f);
                f.Bg = MakeImage(f.Root, "Bg", RoundedRectSprite.Get(24));
                f.Bg.color = new Color(8f / 255f, 10f / 255f, 34f / 255f, 0.88f);
                f.Text = MakeLabel(f.Root, "Text", 16f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
                f.Text.horizontalOverflow = HorizontalWrapMode.Overflow;
                go.SetActive(false);
                _floats[i] = f;
            }
        }

        // ------------------------------------------------------------------ la tarjeta «NUEVO»

        private void BuildIntro()
        {
            _dim = MakeImage(_introLayer, "Dim", DiscSprite.Get());
            Stretch(_dim.rectTransform);
            _dim.color = new Color(2f / 255f, 3f / 255f, 15f / 255f, 0.8f);
            _dim.raycastTarget = false;
            _introRim = MakeImage(_introLayer, "CardRim", RoundedRectSprite.Get(24));
            _introRim.color = Gold;
            _introCard = MakeImage(_introLayer, "Card", RoundedRectSprite.Get(24));
            _introCard.color = PanelFill;
            for (int i = 0; i < IntroPool; i++) _introImgs.Add(MakeImage(_introLayer, "Art" + i, null, false));
            _introLine = NewLine(_introLayer, "ArtLine");
            _introLine2 = NewLine(_introLayer, "ArtLine2");
            for (int i = 0; i < 3; i++) _introLabels.Add(MakeLabel(_introLayer, "ArtLabel" + i, 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter));
            foreach (var t in _introLabels) { t.horizontalOverflow = HorizontalWrapMode.Overflow; t.gameObject.SetActive(false); }
            _introTag = MakeLabel(_introLayer, "Tag", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _introBody = MakeLabel(_introLayer, "Body", 17f, UiFonts.Bold, Color.white, TextAnchor.UpperCenter);
            _introTap = MakeLabel(_introLayer, "Tap", 16f, UiFonts.Bold, Cyan, TextAnchor.MiddleCenter);
            _introTap.text = ConstelacionContract.TapToStart;
            foreach (var t in new[] { _introTag, _introTap }) { t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; }
            _introBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _introBody.verticalOverflow = VerticalWrapMode.Overflow;
            _introBody.supportRichText = true;
            _introBody.lineSpacing = 1.15f;
            _introGroup = _introLayer.gameObject.AddComponent<CanvasGroup>();
            _introGroup.blocksRaycasts = false;
            _introLayer.gameObject.SetActive(false);
        }

        private void BuildResultPanel()
        {
            var go = new GameObject("Result");
            go.transform.SetParent(_safe, false);
            _resultRoot = go.AddComponent<RectTransform>();
            _resultRoot.anchorMin = _resultRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _resultRoot.pivot = new Vector2(0.5f, 0.5f);
            _resultRoot.sizeDelta = new Vector2(880f, 760f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.08f, 0.12f, 0.22f, 0.96f);

            AddResultText("Title", 76, new Vector2(0f, 250f), Color.white);
            AddResultText("Score", 260, new Vector2(0f, 60f), Color.white);
            var detail = AddResultText("Detail", 44, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            detail.rectTransform.sizeDelta = new Vector2(820f, 130f);
            var extra = AddResultText("Extra", 42, new Vector2(0f, -270f), new Color(1f, 1f, 1f, 0.65f));
            extra.horizontalOverflow = HorizontalWrapMode.Wrap;
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

        /// <summary>Una imagen centrada en su padre. Nace ENCENDIDA (<paramref name="active"/>): las piezas que se prenden y apagan a propósito lo dicen aparte.</summary>
        private static Image MakeImage(Transform parent, string name, Sprite sprite, bool active = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            go.SetActive(active);
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
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            _fonts.Add(new KeyValuePair<Text, float>(t, dp));
            return t;
        }

        /// <summary>Una cápsula/pastilla: la imagen redondeada con el radio en unidades (el 9-slice del sprite se ajusta con el multiplicador).</summary>
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

        /// <summary>Pone los sprites horneados y enciende la escena.</summary>
        private void AssignSprites()
        {
            var dormant = ConstelacionSprites.Dormant();
            foreach (var v in _lv)
            {
                v.Body.sprite = dormant;
                v.HintRing.sprite = ConstelacionSprites.RingMemory();
            }
            _boardRoot.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------ disposición

        private Vector2 P(Vector2 l) => new Vector2((l.x - ConstelacionLayout.W * 0.5f) * _s, _playH * 0.5f - l.y * _s);
        private Vector2 P(float x, float y) => P(new Vector2(x, y));
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + ConstelacionLayout.W * 0.5f, (_playH * 0.5f - u.y) / _s);

        /// <summary>De un punto del cielo (dp del cielo, y hacia abajo) a dp lógicos de pantalla.</summary>
        private Vector2 SkyToLogical(float x, float y) =>
            new Vector2((ConstelacionLayout.W - ConstelacionLayout.SkyW * _lay.Scale) * 0.5f + x * _lay.Scale, _lay.SkyTop + y * _lay.Scale);

        private Vector2 LogicalToSky(Vector2 l) =>
            new Vector2((l.x - (ConstelacionLayout.W - ConstelacionLayout.SkyW * _lay.Scale) * 0.5f) / _lay.Scale, (l.y - _lay.SkyTop) / _lay.Scale);

        /// <summary>De un punto del cielo a unidades locales del cielo (centrado; y hacia arriba).</summary>
        private static Vector2 SkyLocal(float x, float y, float h) => new Vector2((x - ConstelacionLayout.SkyW * 0.5f) * Su, -(y - h * 0.5f) * Su);

        private void SetRect(RectTransform r, float cx, float cy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = P(cx, cy);
        }

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
            _s = _playW / ConstelacionLayout.W;
            _logicalH = _playH / _s;
            // en la ronda guiada «Práctica: no cuenta» y «Saltar tutorial» ocupan el borde de abajo: el cielo y la fila «De memoria» suben para no quedar debajo de ellos
            _lay = ConstelacionLayout.Compute(_logicalH - (_guided ? TutorialControlsDp : 0f));
            foreach (var kv in _fonts)
            {
                int px = Mathf.RoundToInt(kv.Value * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            // el cielo entero se escala parejo (hasta 0,88 en pantallas bajas)
            float skyH = _sky != null ? _sky.Placement.Height : _lay.SkyH;
            var center = SkyToLogical(ConstelacionLayout.SkyW * 0.5f, skyH * 0.5f);
            _boardRoot.localScale = Vector3.one * (_s / Su * _lay.Scale);
            _boardRoot.anchoredPosition = P(center);
            // la tarjeta de arriba
            float cw = ConstelacionLayout.W - 20f, cy = _lay.CardTop + ConstelacionLayout.CardH / 2f;
            SetRect(_cardRim.rectTransform, ConstelacionLayout.W / 2f, cy, cw + 3f, ConstelacionLayout.CardH + 3f);
            SetRect(_cardBg.rectTransform, ConstelacionLayout.W / 2f, cy, cw, ConstelacionLayout.CardH);
            SetRadius(_cardRim, 19.5f * _s);
            SetRadius(_cardBg, 18f * _s);
            SetRect(_cardT.rectTransform, ConstelacionLayout.W / 2f, _lay.CardTop + 22f, cw - 16f, 28f);
            SetRect(_cardS.rectTransform, ConstelacionLayout.W / 2f, _lay.CardTop + 46f, cw - 16f, 24f);
            LayoutRow();
            foreach (var f in _floats) f.Text.fontSize = Mathf.RoundToInt(16f * _s);
        }

        /// <summary>La fila «De memoria» de abajo: el rótulo, «X de Y» y los puntos (llenos dorados = acierto, aros vacíos = se te escapó).</summary>
        private void LayoutRow()
        {
            float top = _lay.RowTop;
            SetRect(_rowRect, ConstelacionLayout.W / 2f, top + ConstelacionLayout.RowH / 2f, ConstelacionLayout.W - 20f, ConstelacionLayout.RowH);
            SetRect(_rowLabel.rectTransform, 16f + 60f, top + 10f, 120f, 22f);
            SetRect(_rowCount.rectTransform, ConstelacionLayout.W - 16f - 80f, top + 10f, 160f, 22f);
            LayoutDots();
        }

        private const int DotsPerRow = 15;
        /// <summary>Lo que ocupan, abajo, el rótulo «Práctica: no cuenta» y «Saltar tutorial» (dp).</summary>
        private const float TutorialControlsDp = 96f;

        private void LayoutDots()
        {
            int n = _sky != null ? _sky.Opps.Count : 0;
            for (int i = 0; i < _dots.Length; i++)
            {
                var d = _dots[i];
                bool on = i < n && _rowLayer.gameObject.activeSelf;
                if (d.Root.gameObject.activeSelf != on) d.Root.gameObject.SetActive(on);
                if (!on) continue;
                bool hit = _sky.Opps[i];
                float x = 16f + 8f + (i % DotsPerRow) * 22f, y = _lay.RowTop + 32f + (i / DotsPerRow) * 20f;
                SetRect(d.Root, x, y, 16f, 16f);
                d.Border.gameObject.SetActive(hit);
                d.Fill.gameObject.SetActive(hit);
                d.Ring.gameObject.SetActive(!hit);
                SetChild(d.Border.rectTransform, 0f, 0f, 15f, 15f);
                SetChild(d.Fill.rectTransform, 0f, 0f, 11f, 11f);
                SetChild(d.Ring.rectTransform, 0f, 0f, 14f, 14f);
            }
        }

        /// <summary>La tarjeta «NUEVO»: el título, la ilustración de la regla, el texto (cada línea puede partirse en dos si no cabe en 14 dp o más) y «Toca para empezar». Crece con el texto.</summary>
        private void LayoutIntro(string[] lines)
        {
            float cw = ConstelacionLayout.W - 36f, cx = ConstelacionLayout.W / 2f;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) sb.Append(lines[i]);
                else sb.Append(i == 1 ? "\n<color=#D6D1F2>" : "\n").Append(lines[i]);
            }
            if (lines.Length > 1) sb.Append("</color>");
            if (_introKind == ConIntro.Cielo) sb.Append("\n<color=#FFC94A>").Append(ConstelacionContract.GoldLegend).Append("</color>");
            _introBody.text = sb.ToString();
            _introBody.rectTransform.sizeDelta = new Vector2((cw - 28f) * _s, 400f * _s);
            float bodyH = _introBody.preferredHeight / _s;
            float ch = 150f + bodyH + 44f;
            float top = Mathf.Min(_logicalH * 0.5f - ch / 2f, _logicalH - ch - 12f);
            top = Mathf.Max(top, 12f);
            SetRect(_introRim.rectTransform, cx, top + ch / 2f, cw + 4f, ch + 4f);
            SetRect(_introCard.rectTransform, cx, top + ch / 2f, cw, ch);
            SetRadius(_introRim, 28f * _s);
            SetRadius(_introCard, 26f * _s);
            SetRect(_introTag.rectTransform, cx, top + 26f, 220f, 24f);
            SetRect(_introBody.rectTransform, cx, top + 150f + bodyH / 2f, cw - 28f, bodyH + 4f);
            SetRect(_introTap.rectTransform, cx, top + ch - 24f, 200f, 24f);
            LayoutIntroArt(cx, top + 88f);
        }
    }
}
