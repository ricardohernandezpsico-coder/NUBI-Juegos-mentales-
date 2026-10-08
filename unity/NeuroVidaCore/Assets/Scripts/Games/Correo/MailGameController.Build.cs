using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Correo
{
    public sealed partial class MailGameController
    {
        private const int LetterPool = 12, FloatPool = 4, SparkPool = 80, RollerPool = 16, BriefRows = 6, RecapRows = 6, EndRows = 6, IntroPool = 6;

        private static readonly Color Gold = new Color(255f / 255f, 201f / 255f, 74f / 255f);
        private static readonly Color GoldSoft = new Color(255f / 255f, 231f / 255f, 168f / 255f);
        private static readonly Color Cyan = new Color(127f / 255f, 216f / 255f, 255f / 255f);
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Violet = new Color(183f / 255f, 155f / 255f, 255f / 255f);
        private static readonly Color Mint = new Color(159f / 255f, 245f / 255f, 214f / 255f);
        private static readonly Color Lime = new Color(166f / 255f, 227f / 255f, 107f / 255f);
        private static readonly Color Salmon = new Color(255f / 255f, 180f / 255f, 160f / 255f);
        private static readonly Color PanelFill = new Color(20f / 255f, 27f / 255f, 58f / 255f);
        private static readonly Color TextColor = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color Soft = new Color(214f / 255f, 209f / 255f, 242f / 255f);
        private static readonly Color Dim = new Color(143f / 255f, 138f / 255f, 192f / 255f);
        private static readonly Color InkEdge = new Color(26f / 255f, 18f / 255f, 64f / 255f);
        private static readonly Color Night = new Color(2f / 255f, 3f / 255f, 15f / 255f);
        private static readonly Color Deep = new Color(8f / 255f, 10f / 255f, 34f / 255f);
        private static readonly Color BoxFill = new Color(35f / 255f, 40f / 255f, 80f / 255f);
        private static readonly Color BoxMouth = new Color(11f / 255f, 10f / 255f, 38f / 255f);
        private static readonly Color Metal = new Color(58f / 255f, 51f / 255f, 82f / 255f);
        private static readonly Color MetalDoor = new Color(90f / 255f, 82f / 255f, 128f / 255f);
        private static readonly Color[] PlanetGlow =
        {
            new Color(255f / 255f, 138f / 255f, 107f / 255f), new Color(127f / 255f, 216f / 255f, 255f / 255f),
            new Color(166f / 255f, 227f / 255f, 107f / 255f), new Color(183f / 255f, 155f / 255f, 255f / 255f),
        };

        /// <summary>Una carta de la cinta (o en vuelo): la imagen con su sello, el brillo del sello dorado y el movimiento (resorte en la cinta, arco al buzón o a la caja fuerte, caída si se atrasa).</summary>
        private sealed class LetterView
        {
            public RectTransform Root;
            public Image Img, Glow;
            public MailLetter L;
            public bool InUse, Flying, Falling;
            public float X, Vx, Y;
            public float FromX, FromY, ToX, ToY, At, Dur;
            public int Into, Box;
            public float Bounce = -10f;
            public float Scale = 1f;
        }

        private sealed class BoxView
        {
            public RectTransform Root;
            public Image Shadow, Rim, Body, Mouth, Planet, Glow;
            public Text Label;
            public float ShakeAt = -10f, PulseAt = -10f;
        }

        private sealed class FloatView
        {
            public RectTransform Root;
            public Image Bg;
            public Text Text;
            public float At = -10f, Dur = 1.1f, Rise = 18f;
            public Vector2 Pos;
            public Color Color;
        }

        private sealed class Spark { public Image Img; public Vector2 Pos, Vel; public float Age, Life, Size; public bool Alive; public Color Color; }

        private sealed class RowView
        {
            public RectTransform Root;
            public Image Card, CardRim, Icon, Pill, Check;
            public Text A, B, PillText;
        }

        // las capas, de atrás hacia adelante
        private RectTransform _lettersLayer;       // las cartas de la cinta: UN contenedor propio dentro de la capa de la estación, DESPUÉS de la cinta y su marco (SetSiblingIndex ordena entre hermanos: si las cartas fueran hijas directas de la capa, el orden las mandaba debajo de la banda de la cinta)
        private RectTransform _safe, _play, _showBackLayer, _stageLayer, _flyLayer, _showFrontLayer, _bannerLayer, _peekLayer, _floatLayer, _sparkLayer, _briefLayer, _recapLayer, _introLayer, _endLayer;
        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();
        private readonly List<KeyValuePair<Image, float>> _radii = new List<KeyValuePair<Image, float>>();

        // la estación
        private Text _hdrCards, _hint, _clockLabel, _clockQ, _comboText, _waitText;
        private Image _clockShadow, _clockRim, _clockFill, _clockCover, _comboPill, _waitPill;
        private Image _beltBand, _railTop, _railBottom, _frame, _frameGlow, _shine;
        private readonly Image[] _rollers = new Image[RollerPool];
        private readonly LetterView[] _lv = new LetterView[LetterPool];
        private readonly BoxView[] _boxes = new BoxView[4];
        // la caja fuerte y el faro
        private RectTransform _safeRoot, _beaconRoot;
        private Image _safeShadow, _safeRim, _safeBody, _safeDoor, _safeDoorRim, _dial, _handle, _handleRim, _safeGlow;
        private Text _safeLabel;
        private Image _bShadow, _bRim, _bBody, _tower, _lamp, _lampGlow, _beaconGlow;
        private WedgeGraphic _lampBeamA, _lampBeamB;
        private Text _beaconLabel;
        // el momento del faro
        private Image _wash, _flareA, _flareB, _ship, _flame, _flameGlow, _shipSack, _sackGlow;
        private WedgeGraphic _halo, _beam;
        // la barra del día (el reloj destapado)
        private CanvasGroup _peekGroup;
        private Image _peekBg, _peekRim, _peekTrack, _peekFill, _peekStar, _peekStarGlow;
        private Text _peekTitle;
        private readonly Image[] _peekTicks = new Image[4];
        private readonly Text[] _peekWords = new Text[4];
        // los avisos (saco y radio)
        private CanvasGroup _rushGroup, _radioGroup;
        private Image _rushBg, _rushRim, _rushSack, _radioBg, _radioRim;
        private Text _rushT, _rushS, _radioTag, _radioText;
        private readonly FloatView[] _floats = new FloatView[FloatPool];
        private readonly List<Spark> _sparks = new List<Spark>();
        // hoja de la mañana, resumen, «NUEVO» y pantalla final
        private Image _briefDim, _recapDim, _briefBtn, _briefBtnRim, _recapBtn, _recapBtnRim, _routineCard, _routineCardRim;
        private Text _briefTag, _briefTitle, _briefNote1, _briefNote2, _briefBtnLabel, _routineT1, _routineT2;
        private readonly RowView[] _briefRows = new RowView[BriefRows];
        private Text _recapTag, _recapTitle, _recapLine1, _recapLine2, _recapRecord, _recapUp, _recapBtnLabel;
        private readonly RowView[] _recapRows = new RowView[RecapRows];
        private Image _dim, _introCard, _introRim, _endDim, _endCard, _endCardRim;
        private Text _introTag, _introBody, _introTap;
        private CanvasGroup _introGroup;
        private readonly List<Image> _introImgs = new List<Image>();
        private readonly List<Text> _introLabels = new List<Text>();
        private Image _introBox;
        private Text _endTitle, _endHead, _endPct, _endSub, _endTip, _endNote;
        private readonly Text[] _endLabels = new Text[EndRows], _endValues = new Text[EndRows];
        private RectTransform _timerTrackUnused;

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("MailCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.CieloDeCristal);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, MailContract.TitleRun, MarginU, this, withStreak: false);

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _showBackLayer = Layer(_play, "ShowBack");
            _stageLayer = Layer(_play, "Stage");
            _flyLayer = Layer(_play, "Flying");
            _showFrontLayer = Layer(_play, "ShowFront");
            _bannerLayer = Layer(_play, "Banners");
            _peekLayer = Layer(_play, "Peek");
            _floatLayer = Layer(_play, "Floats");
            _sparkLayer = Layer(_play, "Sparks");
            _briefLayer = Layer(_play, "Brief");
            _recapLayer = Layer(_play, "Recap");
            _introLayer = Layer(_play, "Intro");
            _endLayer = Layer(_play, "End");
            BuildShowBack();
            BuildStage();
            BuildFlying();
            BuildShowFront();
            BuildBanners();
            BuildPeek();
            BuildFx();
            BuildBrief();
            BuildRecap();
            BuildIntro();
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
            ShowOnly(null);
        }

        // ------------------------------------------------------------------ el estilo de cada pieza

        private void BuildShowBack()
        {
            _wash = MakeImage(_showBackLayer, "Wash", RadialGlowSprite.Get());
            _wash.color = new Color(255f / 255f, 226f / 255f, 150f / 255f, 0f);
            _halo = NewWedge(_showBackLayer, "Halo");
            _beam = NewWedge(_showBackLayer, "Beam");
            _wash.gameObject.SetActive(false);
            _halo.gameObject.SetActive(false);
            _beam.gameObject.SetActive(false);
        }

        private static WedgeGraphic NewWedge(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = Vector2.zero;
            var g = go.AddComponent<WedgeGraphic>();
            g.raycastTarget = false;
            return g;
        }

        private void BuildStage()
        {
            var s = _stageLayer;
            _hdrCards = MakeLabel(s, "Cards", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleLeft);
            _hdrCards.horizontalOverflow = HorizontalWrapMode.Overflow;
            // la cinta
            _beltBand = MakeImage(s, "Belt", RoundedRectSprite.Get(24));
            _beltBand.color = new Color(20f / 255f, 27f / 255f, 58f / 255f);
            _railTop = MakeImage(s, "RailTop", RoundedRectSprite.Get(24));
            _railTop.color = new Color(35f / 255f, 43f / 255f, 87f / 255f);
            _railBottom = MakeImage(s, "RailBottom", RoundedRectSprite.Get(24));
            _railBottom.color = new Color(35f / 255f, 43f / 255f, 87f / 255f);
            for (int i = 0; i < _rollers.Length; i++)
            {
                _rollers[i] = MakeImage(s, "Roller" + i, RoundedRectSprite.Get(24));
                _rollers[i].color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.12f);
            }
            _frameGlow = MakeImage(s, "FrameGlow", RadialGlowSprite.Get());
            _frameGlow.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
            _frame = MakeImage(s, "Frame", null);
            _frame.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f);
            // las cartas (se mueven cada cuadro: ver Scene) van en su propio contenedor, que se dibuja encima de la cinta, los rieles, los rodillos y el marco
            _lettersLayer = Layer(s, "Letters");
            for (int i = 0; i < _lv.Length; i++) _lv[i] = BuildLetter(_lettersLayer, i);
            _shine = MakeImage(s, "Shine", RadialGlowSprite.Get());
            _shine.color = new Color(1f, 236f / 255f, 170f / 255f, 0f);
            _comboPill = Pill(s, "ComboPill", Gold, out _comboText);
            _waitPill = Pill(s, "WaitPill", Gold, out _waitText);
            // los buzones
            for (int i = 0; i < _boxes.Length; i++) _boxes[i] = BuildBox(s, i);
            BuildSafe(s);
            BuildBeacon(s);
            BuildClock(s);
            _hint = MakeLabel(s, "Hint", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _hint.horizontalOverflow = HorizontalWrapMode.Overflow;
            _hint.text = MailContract.PlayHint;
            _stageLayer.gameObject.SetActive(false);
        }

        private Image Pill(Transform parent, string name, Color fill, out Text text)
        {
            var bg = Rr(parent, name, fill, 13f);
            text = MakeLabel(bg.transform, name + "Text", 15f, UiFonts.Bold, InkEdge, TextAnchor.MiddleCenter);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            bg.gameObject.SetActive(false);
            return bg;
        }

        private LetterView BuildLetter(Transform parent, int i)
        {
            var v = new LetterView();
            var go = new GameObject("Letter" + i);
            go.transform.SetParent(parent, false);
            v.Root = go.AddComponent<RectTransform>();
            v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
            v.Glow = MakeImage(v.Root, "Glow", RadialGlowSprite.Get());
            v.Glow.color = new Color(1f, 214f / 255f, 120f / 255f, 0f);
            v.Img = MakeImage(v.Root, "Img", null);
            go.SetActive(false);
            return v;
        }

        private BoxView BuildBox(Transform parent, int i)
        {
            var b = new BoxView();
            var go = new GameObject("Box" + i);
            go.transform.SetParent(parent, false);
            b.Root = go.AddComponent<RectTransform>();
            b.Root.anchorMin = b.Root.anchorMax = b.Root.pivot = new Vector2(0.5f, 0.5f);
            b.Glow = MakeImage(b.Root, "Glow", RadialGlowSprite.Get());
            b.Glow.color = new Color(PlanetGlow[i].r, PlanetGlow[i].g, PlanetGlow[i].b, 0f);
            b.Shadow = Rr(b.Root, "Shadow", new Color(0f, 0f, 0f, 0.35f), 18f);
            b.Rim = Rr(b.Root, "Rim", InkEdge, 19.5f);
            b.Body = Rr(b.Root, "Body", BoxFill, 18f);
            b.Mouth = Rr(b.Root, "Mouth", BoxMouth, 6f);
            b.Planet = MakeImage(b.Root, "Planet", null);
            b.Label = MakeLabel(b.Root, "Label", 14f, UiFonts.Regular, Soft, TextAnchor.MiddleCenter);
            b.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
            b.Label.text = MailContract.PlanetNames[i];
            go.SetActive(false);
            return b;
        }

        private void BuildSafe(Transform parent)
        {
            var go = new GameObject("Safe");
            go.transform.SetParent(parent, false);
            _safeRoot = go.AddComponent<RectTransform>();
            _safeRoot.anchorMin = _safeRoot.anchorMax = _safeRoot.pivot = new Vector2(0.5f, 0.5f);
            _safeGlow = MakeImage(_safeRoot, "Glow", RadialGlowSprite.Get());
            _safeGlow.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
            _safeShadow = Rr(_safeRoot, "Shadow", new Color(0f, 0f, 0f, 0.35f), 20f);
            _safeRim = Rr(_safeRoot, "Rim", InkEdge, 21.5f);
            _safeBody = Rr(_safeRoot, "Body", Metal, 20f);
            _safeDoorRim = Rr(_safeRoot, "DoorRim", InkEdge, 13f);
            _safeDoor = Rr(_safeRoot, "Door", MetalDoor, 11.5f);
            _dial = MakeImage(_safeRoot, "Dial", null);
            _handleRim = Rr(_safeRoot, "HandleRim", InkEdge, 5f);
            _handle = Rr(_safeRoot, "Handle", Gold, 3.5f);
            _safeLabel = MakeLabel(_safeRoot, "Label", 17f, UiFonts.Bold, TextColor, TextAnchor.MiddleLeft);
            _safeLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _safeLabel.lineSpacing = 1.05f;
            _safeLabel.text = MailContract.SafeLabelA + "\n" + MailContract.SafeLabelB;
        }

        private void BuildBeacon(Transform parent)
        {
            var go = new GameObject("Beacon");
            go.transform.SetParent(parent, false);
            _beaconRoot = go.AddComponent<RectTransform>();
            _beaconRoot.anchorMin = _beaconRoot.anchorMax = _beaconRoot.pivot = new Vector2(0.5f, 0.5f);
            _beaconGlow = MakeImage(_beaconRoot, "Glow", RadialGlowSprite.Get());
            _beaconGlow.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
            _bShadow = Rr(_beaconRoot, "Shadow", new Color(0f, 0f, 0f, 0.35f), 20f);
            _bRim = Rr(_beaconRoot, "Rim", InkEdge, 21.5f);
            _bBody = Rr(_beaconRoot, "Body", Metal, 20f);
            _lampBeamA = NewWedge(_beaconRoot, "BeamA");
            _lampBeamB = NewWedge(_beaconRoot, "BeamB");
            _tower = MakeImage(_beaconRoot, "Tower", null);
            _lampGlow = MakeImage(_beaconRoot, "LampGlow", RadialGlowSprite.Get());
            _lampGlow.color = new Color(1f, 226f / 255f, 122f / 255f, 0f);
            _lamp = MakeImage(_beaconRoot, "Lamp", null);
            _beaconLabel = MakeLabel(_beaconRoot, "Label", 17f, UiFonts.Bold, TextColor, TextAnchor.MiddleLeft);
            _beaconLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _beaconLabel.text = MailContract.BeaconLabel;
        }

        private void BuildClock(Transform parent)
        {
            _clockShadow = MakeImage(parent, "ClockShadow", DiscSprite.Get());
            _clockShadow.color = new Color(0f, 0f, 0f, 0.35f);
            _clockRim = MakeImage(parent, "ClockRim", DiscSprite.Get());
            _clockRim.color = InkEdge;
            _clockFill = MakeImage(parent, "ClockFill", DiscSprite.Get());
            _clockFill.color = new Color(43f / 255f, 49f / 255f, 112f / 255f);
            _clockCover = MakeImage(parent, "ClockCover", DiscSprite.Get());
            _clockCover.color = Violet;
            _clockQ = MakeLabel(parent, "ClockQ", 24f, UiFonts.Bold, InkEdge, TextAnchor.MiddleCenter);
            _clockQ.horizontalOverflow = HorizontalWrapMode.Overflow;
            _clockQ.text = "?";
            _clockLabel = MakeLabel(parent, "ClockLabel", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _clockLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _clockLabel.text = MailContract.ClockLabel;
        }

        private void BuildFlying()
        {
            // las cartas en vuelo usan las mismas vistas (el pool es único): se pasan de capa al volar
        }

        private void BuildShowFront()
        {
            _flareA = MakeImage(_showFrontLayer, "FlareA", RadialGlowSprite.Get());
            _flareB = MakeImage(_showFrontLayer, "FlareB", RadialGlowSprite.Get());
            _ship = MakeImage(_showFrontLayer, "Ship", null);
            _flameGlow = MakeImage(_ship.transform, "FlameGlow", RadialGlowSprite.Get());
            _flame = MakeImage(_ship.transform, "Flame", null);
            _flame.rectTransform.pivot = new Vector2(1f, 0.5f);
            _sackGlow = MakeImage(_showFrontLayer, "SackGlow", RadialGlowSprite.Get());
            _shipSack = MakeImage(_showFrontLayer, "Sack", null);
            _flareA.color = new Color(1f, 236f / 255f, 170f / 255f, 0f);
            _flareB.color = new Color(1f, 1f, 235f / 255f, 0f);
            _flameGlow.color = new Color(1f, 180f / 255f, 110f / 255f, 0.8f);
            _sackGlow.color = new Color(Gold.r, Gold.g, Gold.b, 0.7f);
            foreach (var i in new[] { _flareA, _flareB, _ship, _sackGlow, _shipSack }) i.gameObject.SetActive(false);
        }

        private void BuildBanners()
        {
            var rushGo = new GameObject("RushBanner");
            rushGo.transform.SetParent(_bannerLayer, false);
            var rr = rushGo.AddComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(0.5f, 0.5f);
            _rushGroup = rushGo.AddComponent<CanvasGroup>();
            _rushGroup.blocksRaycasts = false;
            _rushRim = Rr(rr, "Rim", Gold, 17f);
            _rushBg = Rr(rr, "Bg", new Color(Deep.r, Deep.g, Deep.b, 0.95f), 16f);
            _rushSack = MakeImage(rr, "Sack", null);
            _rushT = MakeLabel(rr, "Title", 19f, UiFonts.Bold, Gold, TextAnchor.MiddleLeft);
            _rushS = MakeLabel(rr, "Sub", 14f, UiFonts.Regular, Soft, TextAnchor.MiddleLeft);
            foreach (var t in new[] { _rushT, _rushS }) t.horizontalOverflow = HorizontalWrapMode.Overflow;
            _rushT.text = MailContract.RushTitle;
            _rushS.text = MailContract.RushSub;
            rushGo.SetActive(false);

            var radioGo = new GameObject("RadioBanner");
            radioGo.transform.SetParent(_bannerLayer, false);
            var rd = radioGo.AddComponent<RectTransform>();
            rd.anchorMin = rd.anchorMax = rd.pivot = new Vector2(0.5f, 0.5f);
            _radioGroup = radioGo.AddComponent<CanvasGroup>();
            _radioGroup.blocksRaycasts = false;
            _radioRim = Rr(rd, "Rim", Violet, 17f);
            _radioBg = Rr(rd, "Bg", new Color(Deep.r, Deep.g, Deep.b, 0.96f), 16f);
            _radioTag = MakeLabel(rd, "Tag", 15f, UiFonts.Bold, Violet, TextAnchor.MiddleCenter);
            _radioText = MakeLabel(rd, "Text", 15f, UiFonts.Bold, TextColor, TextAnchor.MiddleCenter);
            _radioTag.horizontalOverflow = HorizontalWrapMode.Overflow;
            _radioTag.text = MailContract.RadioTag;
            radioGo.SetActive(false);
        }

        private void BuildPeek()
        {
            var go = new GameObject("PeekPanel");
            go.transform.SetParent(_peekLayer, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            _peekGroup = go.AddComponent<CanvasGroup>();
            _peekGroup.blocksRaycasts = false;
            _peekRim = Rr(r, "Rim", new Color(Violet.r, Violet.g, Violet.b, 0.6f), 21f);
            _peekBg = Rr(r, "Bg", new Color(Deep.r, Deep.g, Deep.b, 0.95f), 20f);
            _peekTitle = MakeLabel(r, "Title", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _peekTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            _peekTitle.text = MailContract.ClockPanel;
            _peekTrack = Rr(r, "Track", new Color(43f / 255f, 49f / 255f, 112f / 255f), 6f);
            _peekFill = MakeImage(r, "Fill", null);
            _peekFill.type = Image.Type.Filled;
            _peekFill.fillMethod = Image.FillMethod.Horizontal;
            for (int i = 0; i < 4; i++)
            {
                _peekTicks[i] = Rr(r, "Tick" + i, new Color(TextColor.r, TextColor.g, TextColor.b, 0.5f), 1f);
                _peekWords[i] = MakeLabel(r, "Word" + i, 14f, UiFonts.Regular, TextColor, TextAnchor.MiddleCenter);
                _peekWords[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                _peekWords[i].text = MailContract.MomentWord(MailContract.AllMoments[i]);
            }
            _peekStarGlow = MakeImage(r, "StarGlow", RadialGlowSprite.Get());
            _peekStarGlow.color = new Color(1f, 226f / 255f, 122f / 255f, 1f);
            _peekStar = MakeImage(r, "Star", DiscSprite.Get());
            _peekStar.color = new Color(1f, 244f / 255f, 194f / 255f, 1f);
            go.SetActive(false);
        }

        private void BuildFx()
        {
            for (int i = 0; i < SparkPool; i++)
            {
                var img = MakeImage(_sparkLayer, "Spark", DiscSprite.Get(), false);
                _sparks.Add(new Spark { Img = img });
            }
            for (int i = 0; i < _floats.Length; i++)
            {
                var f = new FloatView();
                var go = new GameObject("Float" + i);
                go.transform.SetParent(_floatLayer, false);
                f.Root = go.AddComponent<RectTransform>();
                f.Root.anchorMin = f.Root.anchorMax = f.Root.pivot = new Vector2(0.5f, 0.5f);
                f.Bg = Rr(f.Root, "Bg", new Color(Deep.r, Deep.g, Deep.b, 0.92f), 15f);
                f.Text = MakeLabel(f.Root, "Text", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
                f.Text.horizontalOverflow = HorizontalWrapMode.Overflow;
                go.SetActive(false);
                _floats[i] = f;
            }
        }

        private RowView BuildRow(Transform parent, string name, bool brief)
        {
            var r = new RowView();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            r.Root = go.AddComponent<RectTransform>();
            r.Root.anchorMin = r.Root.anchorMax = r.Root.pivot = new Vector2(0.5f, 0.5f);
            r.CardRim = Rr(r.Root, "Rim", new Color(142f / 255f, 131f / 255f, 216f / 255f, 0.35f), 19f);
            r.Card = Rr(r.Root, "Card", PanelFill, 18f);
            r.Icon = MakeImage(r.Root, "Icon", null);
            r.Check = MakeImage(r.Root, "Check", null);
            r.A = MakeLabel(r.Root, "A", brief ? 15f : 16f, brief ? UiFonts.Regular : UiFonts.Bold, brief ? Soft : TextColor, TextAnchor.MiddleLeft);
            r.B = MakeLabel(r.Root, "B", brief ? 17f : 14f, brief ? UiFonts.Bold : UiFonts.Regular, brief ? Color.white : Lavender, TextAnchor.MiddleLeft);
            foreach (var t in new[] { r.A, r.B }) t.horizontalOverflow = HorizontalWrapMode.Overflow;
            r.Pill = Rr(r.Root, "Pill", Violet, 9f);
            r.PillText = MakeLabel(r.Pill.transform, "PillText", 14f, UiFonts.Bold, InkEdge, TextAnchor.MiddleCenter);
            r.PillText.horizontalOverflow = HorizontalWrapMode.Overflow;
            r.PillText.text = MailContract.RoutineTag;
            go.SetActive(false);
            return r;
        }

        private Image BigButton(Transform parent, string name, Color color, out Image rim, out Text label)
        {
            rim = Rr(parent, name + "Rim", InkEdge, 31f);
            var bg = Rr(parent, name, color, 28f);
            label = MakeLabel(bg.transform, name + "Label", 20f, UiFonts.Bold, InkEdge, TextAnchor.MiddleCenter);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            return bg;
        }

        private void BuildBrief()
        {
            _briefDim = MakeImage(_briefLayer, "Dim", DiscSprite.Get());
            Stretch(_briefDim.rectTransform);
            _briefDim.color = new Color(Night.r, Night.g, Night.b, 0.94f);
            _briefDim.raycastTarget = false;
            _briefTag = MakeLabel(_briefLayer, "Tag", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _briefTitle = MakeLabel(_briefLayer, "Title", 28f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _briefTitle.text = MailContract.BriefTitle;
            for (int i = 0; i < _briefRows.Length; i++) _briefRows[i] = BuildRow(_briefLayer, "Row" + i, true);
            _routineCardRim = Rr(_briefLayer, "RoutineRim", new Color(Violet.r, Violet.g, Violet.b, 0.6f), 19f);
            _routineCard = Rr(_briefLayer, "RoutineCard", PanelFill, 18f);
            _routineT1 = MakeLabel(_briefLayer, "RoutineT1", 16f, UiFonts.Bold, Violet, TextAnchor.MiddleCenter);
            _routineT2 = MakeLabel(_briefLayer, "RoutineT2", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _routineT1.text = MailContract.RoutineTitle;
            _routineT2.text = MailContract.RoutineSub;
            _briefNote1 = MakeLabel(_briefLayer, "Note1", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _briefNote2 = MakeLabel(_briefLayer, "Note2", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _briefNote1.text = MailContract.BriefNote1;
            _briefNote2.text = MailContract.BriefNote2;
            foreach (var t in new[] { _briefTag, _briefTitle, _routineT1, _routineT2, _briefNote1, _briefNote2 }) t.horizontalOverflow = HorizontalWrapMode.Overflow;
            _briefBtn = BigButton(_briefLayer, "BriefBtn", Lime, out _briefBtnRim, out _briefBtnLabel);
            _briefBtnLabel.text = MailContract.BriefStart;
            _briefLayer.gameObject.SetActive(false);
        }

        private void BuildRecap()
        {
            _recapDim = MakeImage(_recapLayer, "Dim", DiscSprite.Get());
            Stretch(_recapDim.rectTransform);
            _recapDim.color = new Color(Night.r, Night.g, Night.b, 0.94f);
            _recapDim.raycastTarget = false;
            _recapTag = MakeLabel(_recapLayer, "Tag", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _recapTitle = MakeLabel(_recapLayer, "Title", 26f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            for (int i = 0; i < _recapRows.Length; i++) _recapRows[i] = BuildRow(_recapLayer, "Row" + i, false);
            _recapLine1 = MakeLabel(_recapLayer, "Line1", 15f, UiFonts.Regular, Soft, TextAnchor.MiddleCenter);
            _recapLine2 = MakeLabel(_recapLayer, "Line2", 15f, UiFonts.Regular, Soft, TextAnchor.MiddleCenter);
            _recapRecord = MakeLabel(_recapLayer, "Record", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _recapUp = MakeLabel(_recapLayer, "Up", 15f, UiFonts.Bold, Mint, TextAnchor.MiddleCenter);
            foreach (var t in new[] { _recapTag, _recapTitle, _recapLine1, _recapLine2, _recapRecord, _recapUp }) t.horizontalOverflow = HorizontalWrapMode.Overflow;
            _recapBtn = BigButton(_recapLayer, "RecapBtn", Cyan, out _recapBtnRim, out _recapBtnLabel);
            _recapLayer.gameObject.SetActive(false);
        }

        private void BuildIntro()
        {
            _dim = MakeImage(_introLayer, "Dim", DiscSprite.Get());
            Stretch(_dim.rectTransform);
            _dim.color = new Color(Night.r, Night.g, Night.b, 0.84f);
            _dim.raycastTarget = false;
            _introRim = Rr(_introLayer, "CardRim", Gold, 28f);
            _introCard = Rr(_introLayer, "Card", PanelFill, 26f);
            for (int i = 0; i < IntroPool; i++) _introImgs.Add(MakeImage(_introLayer, "Art" + i, null, false));
            _introBox = Rr(_introLayer, "ArtBox", new Color(Deep.r, Deep.g, Deep.b, 0.96f), 14f);
            for (int i = 0; i < 3; i++) _introLabels.Add(MakeLabel(_introLayer, "ArtLabel" + i, 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter));
            foreach (var t in _introLabels) { t.horizontalOverflow = HorizontalWrapMode.Overflow; t.gameObject.SetActive(false); }
            _introTag = MakeLabel(_introLayer, "Tag", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _introBody = MakeLabel(_introLayer, "Body", 17f, UiFonts.Bold, Color.white, TextAnchor.UpperCenter);
            _introTap = MakeLabel(_introLayer, "Tap", 16f, UiFonts.Bold, Cyan, TextAnchor.MiddleCenter);
            _introTap.text = MailContract.TapToContinue;
            foreach (var t in new[] { _introTag, _introTap }) { t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; }
            _introBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _introBody.verticalOverflow = VerticalWrapMode.Overflow;
            _introBody.supportRichText = true;
            _introBody.lineSpacing = 1.15f;
            _introGroup = _introLayer.gameObject.AddComponent<CanvasGroup>();
            _introGroup.blocksRaycasts = false;
            _introLayer.gameObject.SetActive(false);
        }

        private void BuildEnd()
        {
            _endDim = MakeImage(_endLayer, "Dim", DiscSprite.Get());
            Stretch(_endDim.rectTransform);
            _endDim.color = new Color(3f / 255f, 4f / 255f, 22f / 255f, 0.95f);
            _endDim.raycastTarget = false;
            _endTitle = MakeLabel(_endLayer, "Title", 30f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _endCardRim = Rr(_endLayer, "CardRim", new Color(Gold.r, Gold.g, Gold.b, 0.6f), 23f);
            _endCard = Rr(_endLayer, "Card", PanelFill, 22f);
            _endHead = MakeLabel(_endLayer, "Head", 15f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _endPct = MakeLabel(_endLayer, "Pct", 42f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _endSub = MakeLabel(_endLayer, "Sub", 14f, UiFonts.Regular, Soft, TextAnchor.MiddleCenter);
            for (int i = 0; i < EndRows; i++)
            {
                _endLabels[i] = MakeLabel(_endLayer, "Label" + i, 15f, UiFonts.Regular, Lavender, TextAnchor.MiddleLeft);
                _endValues[i] = MakeLabel(_endLayer, "Value" + i, 17f, UiFonts.Bold, Color.white, TextAnchor.MiddleRight);
            }
            _endTip = MakeLabel(_endLayer, "Tip", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _endNote = MakeLabel(_endLayer, "Note", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            foreach (var t in new[] { _endTitle, _endHead, _endPct, _endSub, _endTip, _endNote }) t.horizontalOverflow = HorizontalWrapMode.Overflow;
            foreach (var t in _endLabels) t.horizontalOverflow = HorizontalWrapMode.Overflow;
            foreach (var t in _endValues) t.horizontalOverflow = HorizontalWrapMode.Overflow;
            _endHead.text = "Tu memoria para lo pendiente";
            _endTitle.text = "¡Turno terminado!";
            _endNote.text = "Medida de esta partida. No es un diagnóstico.";
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
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
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
            _frame.sprite = MailSprites.DashFrame();
            _dial.sprite = MailSprites.Dial();
            _tower.sprite = MailSprites.Tower();
            _lamp.sprite = MailSprites.Lamp(false);
            _ship.sprite = MailSprites.Ship();
            _flame.sprite = MailSprites.Flame();
            _shipSack.sprite = MailSprites.Sack();
            _rushSack.sprite = MailSprites.Sack();
            _peekFill.sprite = MailSprites.DayBar();
            for (int i = 0; i < _boxes.Length; i++) _boxes[i].Planet.sprite = MailSprites.Planet(i);
            foreach (var v in _lv) v.Img.sprite = MailSprites.Letter(0, MailCue.None);
        }

        // ------------------------------------------------------------------ disposición

        private Vector2 P(Vector2 l) => new Vector2((l.x - MailLayout.W * 0.5f) * _s, _playH * 0.5f - l.y * _s);
        private Vector2 P(float x, float y) => P(new Vector2(x, y));
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + MailLayout.W * 0.5f, (_playH * 0.5f - u.y) / _s);

        private void SetRect(RectTransform r, float cx, float cy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = P(cx, cy);
        }

        /// <summary>Un hijo de un panel posicionado por su DESPLAZAMIENTO (dp) desde el centro del padre (x a la derecha, y hacia abajo).</summary>
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
            _s = _playW / MailLayout.W;
            _logicalH = _playH / _s;
            _lay = MailLayout.Compute(_logicalH - (_guided ? TutorialControlsDp : 0f));
            foreach (var kv in _fonts)
            {
                int px = Mathf.RoundToInt(kv.Value * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            foreach (var kv in _radii) SetRadius(kv.Key, kv.Value * _s);
            LayoutStage();
            LayoutOverlays();
        }

        private void LayoutStage()
        {
            var m = _lay;
            SetRect(_hdrCards.rectTransform, 16f + 120f, m.HeaderY1, 240f, 22f);
            // la cinta
            SetRect(_beltBand.rectTransform, MailLayout.W / 2f, m.BeltY, MailLayout.W + 20f, MailLayout.BeltBandH);
            SetRect(_railTop.rectTransform, MailLayout.W / 2f, m.BeltY - 45f, MailLayout.W, 6f);
            SetRect(_railBottom.rectTransform, MailLayout.W / 2f, m.BeltY + 44f, MailLayout.W, 8f);
            SetRect(_frame.rectTransform, MailLayout.BeltXA, m.BeltY, MailSprites.FrameBoxW, MailSprites.FrameBoxH);
            SetRect(_frameGlow.rectTransform, MailLayout.BeltXA, m.BeltY, 180f, 140f);
            // los buzones (se reubican al armar el día: ver BindDay)
            // la caja fuerte y el faro
            LayoutButton(_safeRoot, _safeGlow, _safeShadow, _safeRim, _safeBody, m.Safe);
            LayoutButton(_beaconRoot, _beaconGlow, _bShadow, _bRim, _bBody, m.Beacon);
            SetChild(_safeDoorRim.rectTransform, -m.Safe.W / 2f + 10f + 31f, 0f, 64f, m.Safe.H - 18f);
            SetChild(_safeDoor.rectTransform, -m.Safe.W / 2f + 10f + 31f, 0f, 62f, m.Safe.H - 20f);
            float wx = -m.Safe.W / 2f + 41f, wy = -8f;
            SetChild(_dial.rectTransform, wx, wy, MailSprites.DialBox, MailSprites.DialBox);
            SetChild(_handleRim.rectTransform, wx, wy + 8f + 20f - 4f, 30f, 9f);
            SetChild(_handle.rectTransform, wx, wy + 8f + 20f - 4f, 28f, 7f);
            SetChild(_safeLabel.rectTransform, -m.Safe.W / 2f + 82f + 36f, 0f, 76f, 46f);
            // el faro: la torre y su lámpara
            float fx = -m.Beacon.W / 2f + 40f, fy = 4f;
            SetChild(_tower.rectTransform, fx, fy, MailSprites.TowerBoxW, MailSprites.TowerBoxH);
            SetChild(_lamp.rectTransform, fx, fy - 20f, MailSprites.LampBox, MailSprites.LampBox);
            SetChild(_lampGlow.rectTransform, fx, fy - 20f, 52f, 52f);
            _lampBeamA.rectTransform.anchoredPosition = new Vector2(fx * _s, -(fy - 20f) * _s);
            _lampBeamB.rectTransform.anchoredPosition = new Vector2(fx * _s, -(fy - 20f) * _s);
            SetChild(_beaconLabel.rectTransform, -m.Beacon.W / 2f + 70f + 40f, 0f, 80f, 24f);
            // el reloj
            SetRect(_clockShadow.rectTransform, m.ClockCx + 2f, m.ClockCy + 4f, MailLayout.ClockR * 2f, MailLayout.ClockR * 2f);
            SetRect(_clockRim.rectTransform, m.ClockCx, m.ClockCy, MailLayout.ClockR * 2f + 3f, MailLayout.ClockR * 2f + 3f);
            SetRect(_clockFill.rectTransform, m.ClockCx, m.ClockCy, MailLayout.ClockR * 2f - 3f, MailLayout.ClockR * 2f - 3f);
            SetRect(_clockCover.rectTransform, m.ClockCx, m.ClockCy, MailLayout.ClockR * 2f - 8f, MailLayout.ClockR * 2f - 8f);
            SetRect(_clockQ.rectTransform, m.ClockCx, m.ClockCy + 1f, 40f, 34f);
            SetRect(_clockLabel.rectTransform, m.ClockCx, m.LabelY, 60f, 22f);
            SetRect(_hint.rectTransform, MailLayout.W / 2f, m.HintY, 300f, 22f);
            LayoutBoxes();
            // la barra del día (el reloj destapado)
            var pr = _peekGroup.GetComponent<RectTransform>();
            SetRect(pr, MailLayout.W / 2f, m.ClockCy - 34f + 47f, 320f, 94f);
            SetChild(_peekRim.rectTransform, 0f, 0f, 323f, 97f);
            SetChild(_peekBg.rectTransform, 0f, 0f, 320f, 94f);
            SetChild(_peekTitle.rectTransform, 0f, -29f, 200f, 22f);
            SetChild(_peekTrack.rectTransform, 0f, -1f, 272f, 12f);
            SetChild(_peekFill.rectTransform, 0f, -1f, 272f, 12f);
            for (int i = 0; i < 4; i++)
            {
                float tx = -136f + 272f * MailContract.MomentCenter(MailContract.AllMoments[i]);
                SetChild(_peekTicks[i].rectTransform, tx + 0f, 12.5f - 1f, 2f, 5f);
                SetChild(_peekWords[i].rectTransform, tx, 27f - 1f, 70f, 22f);
            }
            // los avisos
            var rr = _rushGroup.GetComponent<RectTransform>();
            SetRect(rr, m.Banner.Cx, m.Banner.Cy, m.Banner.W, m.Banner.H);
            SetChild(_rushRim.rectTransform, 0f, 0f, m.Banner.W + 3f, m.Banner.H + 3f);
            SetChild(_rushBg.rectTransform, 0f, 0f, m.Banner.W, m.Banner.H);
            SetChild(_rushSack.rectTransform, -m.Banner.W / 2f + 34f, 0f, 44f, 44f);
            SetChild(_rushT.rectTransform, -m.Banner.W / 2f + 62f + 100f, -10f, 200f, 26f);
            SetChild(_rushS.rectTransform, -m.Banner.W / 2f + 62f + 100f, 14f, 200f, 22f);
            var rd = _radioGroup.GetComponent<RectTransform>();
            SetRect(rd, m.Banner.Cx, m.Banner.Cy, m.Banner.W, m.Banner.H);
            SetChild(_radioRim.rectTransform, 0f, 0f, m.Banner.W + 3f, m.Banner.H + 3f);
            SetChild(_radioBg.rectTransform, 0f, 0f, m.Banner.W, m.Banner.H);
            SetChild(_radioTag.rectTransform, 0f, -m.Banner.H / 2f + 13f, 120f, 22f);
            SetChild(_radioText.rectTransform, 0f, 8f, m.Banner.W - 28f, 40f);
            foreach (var f in _floats) f.Text.fontSize = Mathf.RoundToInt(15f * _s);
            SetRect(_wash.rectTransform, m.Beacon.X + 40f, m.Beacon.Y + m.Beacon.H / 2f - 16f, 1240f, 1240f);
            float lx = m.Beacon.X + 40f, ly = m.Beacon.Y + m.Beacon.H / 2f - 16f;
            _halo.rectTransform.anchoredPosition = P(lx, ly);
            _beam.rectTransform.anchoredPosition = P(lx, ly);
            SetRect(_flareA.rectTransform, lx, ly, 92f, 92f);
            SetRect(_flareB.rectTransform, lx, ly, 36f, 36f);
        }

        private void LayoutButton(RectTransform root, Image glow, Image shadow, Image rim, Image body, MailLayout.Rect2 r)
        {
            SetRect(root, r.Cx, r.Cy, r.W, r.H);
            SetChild(glow.rectTransform, 0f, 0f, r.W + 48f, r.H + 48f);
            SetChild(shadow.rectTransform, 3f, 6f, r.W, r.H);
            SetChild(rim.rectTransform, 0f, 0f, r.W + 3f, r.H + 3f);
            SetChild(body.rectTransform, 0f, 0f, r.W, r.H);
        }

        /// <summary>Los buzones del día: de 2 a 4, con su ancho (150, 102 o 78 dp), su planeta (radio 28 o menos) y su nombre.</summary>
        private void LayoutBoxes()
        {
            int n = _day != null ? _day.Stage.Boxes : (_boxCount > 0 ? _boxCount : 3);
            for (int i = 0; i < _boxes.Length; i++)
            {
                var b = _boxes[i];
                bool on = i < n;
                b.Root.gameObject.SetActive(on && _stageLayer.gameObject.activeSelf);
                if (!on) continue;
                var r = MailLayout.BoxRect(i, n, _lay.BoxY0);
                SetRect(b.Root, r.Cx, r.Cy, r.W, r.H);
                SetChild(b.Glow.rectTransform, 0f, 0f, r.W + 40f, r.H + 40f);
                SetChild(b.Shadow.rectTransform, 3f, 6f, r.W, r.H);
                SetChild(b.Rim.rectTransform, 0f, 0f, r.W + 3f, r.H + 3f);
                SetChild(b.Body.rectTransform, 0f, 0f, r.W, r.H);
                SetChild(b.Mouth.rectTransform, 0f, -r.H / 2f + 18f, r.W - 24f, 12f);
                float R = Mathf.Min(28f, r.W * 0.3f);
                float planetDy = 60f - r.H / 2f;
                SetChild(b.Planet.rectTransform, 0f, planetDy, MailSprites.PlanetBox * R / MailSprites.PlanetR, MailSprites.PlanetBox * R / MailSprites.PlanetR);
                SetChild(b.Label.rectTransform, 0f, r.H / 2f - 14f, r.W, 22f);
            }
        }

        private int _boxCount;

        /// <summary>Lo que ocupan, abajo, el rótulo «Práctica: no cuenta» y «Saltar tutorial» (dp).</summary>
        private const float TutorialControlsDp = 96f;
    }
}
