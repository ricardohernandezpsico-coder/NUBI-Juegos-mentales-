using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Bodega
{
    public sealed partial class BodegaGameController
    {
        private const int HatchPool = 10, RivetPool = 11, TrayPool = 7, SparkPool = 60, FlyerPool = 7;

        private static readonly Color Cyan = new Color(127f / 255f, 216f / 255f, 255f / 255f);
        private static readonly Color Gold = new Color(255f / 255f, 201f / 255f, 74f / 255f);
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Mint = new Color(159f / 255f, 245f / 255f, 214f / 255f);
        private static readonly Color WarnText = new Color(255f / 255f, 214f / 255f, 160f / 255f);
        private static readonly Color PanelFill = new Color(20f / 255f, 27f / 255f, 58f / 255f);
        private static readonly Color TrayFill = new Color(20f / 255f, 27f / 255f, 58f / 255f);
        private static readonly Color ButtonText = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color BeamSoft = new Color(127f / 255f, 216f / 255f, 255f / 255f, 0.22f);
        private static readonly Color BeamCore = new Color(191f / 255f, 233f / 255f, 255f / 255f, 0.85f);
        private static readonly Color HullRivet = new Color(44f / 255f, 53f / 255f, 102f / 255f);
        private static readonly Color ClosedFill = new Color(35f / 255f, 43f / 255f, 87f / 255f);

        /// <summary>Una escotilla: marco, interior (con máscara redonda), el objeto, «vacía», las dos hojas de la puerta y su junta, y el aro que tapa el borde de la máscara.</summary>
        private sealed class HatchView
        {
            public RectTransform Root, Leaves;
            public Image Glow, Frame, Interior, Obj, Rim, LeafA, LeafB, Seam;
            public Text Empty;
            public int Kind = -1;                      // el interior que lleva puesto: 0 vacío, 1 con algo, 2 cerrada
            // el estado del juego
            public int Obj_ = -1;                      // lo que de verdad hay adentro (-1 vacía)
            public float Open, Vo, Target;             // apertura 0..1 (puede pasarse apenas) y su resorte
            public float ShowAt = -1f, GlowAt = -10f, ShakeAt = -10f, PressAt = -10f, RevealAt = -10f;
        }

        private sealed class TrayView
        {
            public RectTransform Root;
            public Image Border, Inner, Obj, Check;
        }

        private sealed class FlyerView
        {
            public Image Glow, Obj;
            public bool On, First, Out;
            public int Slot, ObjId;
            public Vector2 From;
            public float At;
        }

        private sealed class Spark { public Image Img; public Vector2 Pos, Vel; public float Age, Life, Size; public bool Alive; public Color Color; }

        // las capas y las piezas
        private RectTransform _safe, _play, _boardRoot, _cardLayer, _trayLayer, _toastLayer, _fxLayer, _introLayer, _hatchLayer;
        private RectTransform _timerTrack, _timerHead;
        private Image _timerFill;
        private Image _hull, _beamSoft, _beamCore, _crateFlight, _crateFlightGlow, _crateCarried, _crateCarriedGlow, _rope, _robotShadow, _robotBody, _visor, _eyeL, _eyeR, _antStalk, _antBall, _antGlow;
        private RectTransform _robotRoot, _antRoot;
        private readonly Image[] _rivets = new Image[RivetPool];
        /// <summary>La esclusa de carga (posición 0 del anillo): un componente aparte; por ella entra la carga y sale lo encontrado, gira con la bodega y destella al entrar o salir algo.</summary>
        private sealed class AirlockView
        {
            public RectTransform Root;
            public Image Glow, Seal, Body, Peek;
        }

        private AirlockView _lock;
        private Image _flyObj, _flyGlow;               // el objeto que viaja de la esclusa a su escotilla (dentro de la bodega, encima del robot)

        private readonly Image[] _beamSparks = new Image[3];
        private readonly HatchView[] _hv = new HatchView[HatchPool];
        private readonly TrayView[] _tray = new TrayView[TrayPool];
        private readonly FlyerView[] _fly = new FlyerView[FlyerPool];
        private readonly List<Spark> _sparks = new List<Spark>();
        private Text _trayLabel, _recordText;
        // la tarjeta de arriba
        private Image _cardRim, _cardBg, _cardDisc, _cardObj;
        private Text _cardA, _cardB, _cardT, _cardS;
        private CanvasGroup _cardGroup;
        // el aviso de abajo
        private RectTransform _toastRoot;
        private Image _toastRim, _toastBg;
        private Text _toastTitle, _toastSub;
        private CanvasGroup _toastGroup;
        // la tarjeta «NUEVO»
        private Image _dim, _introCard, _introRim, _introFrame, _introInterior, _introObj, _introDoor;
        private Text _introTitle, _introBody, _introTap;
        private CanvasGroup _introGroup;

        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();
        private readonly List<KeyValuePair<Text, float>> _boardFonts = new List<KeyValuePair<Text, float>>();

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("BodegaCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.CieloDeCristal);           // el cielo quieto de Rastro de luz: aquí lo que se mueve es la bodega

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Bodega de carga", MarginU, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _cardLayer = Layer(_play, "Card");
            _boardRoot = Layer(_play, "Board");
            _trayLayer = Layer(_play, "Tray");
            _toastLayer = Layer(_play, "Toast");
            _fxLayer = Layer(_play, "Fx");
            _introLayer = Layer(_play, "Intro");
            BuildCard();
            BuildBoard();
            BuildTray();
            BuildToast();
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

        // ------------------------------------------------------------------ la tarjeta de arriba, el carro, el aviso, los efectos

        private void BuildCard()
        {
            _cardGroup = _cardLayer.gameObject.AddComponent<CanvasGroup>();
            _cardGroup.blocksRaycasts = false;
            _cardRim = MakeImage(_cardLayer, "Rim", RoundedRectSprite.Get(24));
            _cardBg = MakeImage(_cardLayer, "Bg", RoundedRectSprite.Get(24));
            _cardBg.color = PanelFill;
            _cardDisc = MakeImage(_cardLayer, "Disc", DiscSprite.Get());
            _cardDisc.color = ClosedFill;
            _cardObj = MakeImage(_cardLayer, "Obj", null);
            _cardA = MakeLabel(_cardLayer, "LineA", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleLeft);
            _cardB = MakeLabel(_cardLayer, "LineB", 20f, UiFonts.Bold, Color.white, TextAnchor.MiddleLeft);
            foreach (var t in new[] { _cardA, _cardB }) { t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; }
            _cardT = MakeLabel(_cardLayer, "Title", 19f, UiFonts.Bold, ButtonText, TextAnchor.MiddleCenter);
            _cardS = MakeLabel(_cardLayer, "Sub", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _cardB.horizontalOverflow = HorizontalWrapMode.Wrap;
            BestFit(_cardB, Mathf.RoundToInt(14f * UnitsPerDp));
            BestFit(_cardT, Mathf.RoundToInt(14f * UnitsPerDp));
            _cardT.horizontalOverflow = HorizontalWrapMode.Wrap;
            _cardS.horizontalOverflow = HorizontalWrapMode.Wrap;
            _cardLayer.gameObject.SetActive(false);
        }

        private void BuildTray()
        {
            _trayLabel = MakeLabel(_trayLayer, "Label", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleLeft);
            _recordText = MakeLabel(_trayLayer, "Record", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleRight);
            foreach (var t in new[] { _trayLabel, _recordText }) { t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; }
            _trayLabel.text = BodegaContract.TrayLabel;
            for (int i = 0; i < _tray.Length; i++)
            {
                var v = new TrayView();
                var go = new GameObject("Slot" + i);
                go.transform.SetParent(_trayLayer, false);
                v.Root = go.AddComponent<RectTransform>();
                v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
                v.Border = MakeImage(v.Root, "Border", DiscSprite.Get());
                v.Inner = MakeImage(v.Root, "Inner", DiscSprite.Get());
                v.Inner.color = TrayFill;
                v.Obj = MakeImage(v.Root, "Obj", null, false);
                v.Check = MakeImage(v.Root, "Check", null, false);
                v.Check.color = Mint;
                go.SetActive(false);
                _tray[i] = v;
            }
            _trayLayer.gameObject.SetActive(false);
        }

        private void BuildToast()
        {
            var go = new GameObject("Say");
            go.transform.SetParent(_toastLayer, false);
            _toastRoot = go.AddComponent<RectTransform>();
            _toastRoot.anchorMin = _toastRoot.anchorMax = _toastRoot.pivot = new Vector2(0.5f, 0.5f);
            _toastGroup = go.AddComponent<CanvasGroup>();
            _toastGroup.blocksRaycasts = false;
            _toastRim = MakeImage(_toastRoot, "Rim", RoundedRectSprite.Get(24));
            _toastBg = MakeImage(_toastRoot, "Bg", RoundedRectSprite.Get(24));
            _toastBg.color = new Color(8f / 255f, 10f / 255f, 34f / 255f, 0.96f);
            _toastTitle = MakeLabel(_toastRoot, "Title", 16f, UiFonts.Bold, Mint, TextAnchor.MiddleCenter);
            _toastSub = MakeLabel(_toastRoot, "Sub", 14f, UiFonts.Regular, Gold, TextAnchor.MiddleCenter);
            _toastTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            _toastSub.horizontalOverflow = HorizontalWrapMode.Wrap;
            _toastTitle.verticalOverflow = _toastSub.verticalOverflow = VerticalWrapMode.Overflow;
            go.SetActive(false);
        }

        private void BuildFx()
        {
            for (int i = 0; i < SparkPool; i++)
            {
                var img = MakeImage(_fxLayer, "Spark", DiscSprite.Get(), false);
                _sparks.Add(new Spark { Img = img });
            }
            for (int i = 0; i < _fly.Length; i++)
            {
                var f = new FlyerView();
                f.Glow = MakeImage(_fxLayer, "FlyGlow" + i, RadialGlowSprite.Get(), false);
                f.Obj = MakeImage(_fxLayer, "FlyObj" + i, null, false);
                _fly[i] = f;
            }
        }

        private void BuildIntro()
        {
            _dim = MakeImage(_introLayer, "Dim", DiscSprite.Get());
            Stretch(_dim.rectTransform);
            _dim.color = new Color(2f / 255f, 3f / 255f, 15f / 255f, 0.78f);
            _dim.raycastTarget = false;
            _introRim = MakeImage(_introLayer, "CardRim", RoundedRectSprite.Get(24));
            _introRim.color = Gold;
            _introCard = MakeImage(_introLayer, "Card", RoundedRectSprite.Get(24));
            _introCard.color = PanelFill;
            _introFrame = MakeImage(_introLayer, "Frame", null);
            _introInterior = MakeImage(_introLayer, "Interior", null);
            _introObj = MakeImage(_introLayer, "Obj", null);
            _introDoor = MakeImage(_introLayer, "Door", DiscSprite.Get());
            _introDoor.color = Door;
            _introTitle = MakeLabel(_introLayer, "Title", 16f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _introBody = MakeLabel(_introLayer, "Body", 16f, UiFonts.Bold, Color.white, TextAnchor.UpperCenter);
            _introTap = MakeLabel(_introLayer, "Tap", 16f, UiFonts.Bold, Cyan, TextAnchor.MiddleCenter);
            _introTap.text = "Toca para seguir";
            foreach (var t in new[] { _introTitle, _introTap }) { t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; }
            _introBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _introBody.verticalOverflow = VerticalWrapMode.Overflow;
            _introBody.supportRichText = true;
            _introBody.lineSpacing = 1.15f;
            _introGroup = _introLayer.gameObject.AddComponent<CanvasGroup>();
            _introGroup.blocksRaycasts = false;
            _introLayer.gameObject.SetActive(false);
        }

        private static readonly Color Door = new Color(201f / 255f, 193f / 255f, 1f);

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
            AddResultText("Detail", 46, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            var extra = AddResultText("Extra", 42, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            extra.horizontalOverflow = HorizontalWrapMode.Wrap;
            go.SetActive(false);
        }

        // ------------------------------------------------------------------ la bodega (coordenadas del boceto; 1 dp = Su unidades)

        /// <summary>De un punto del boceto (x 0..360, y hacia abajo, centro de la bodega en (180, 335)) a unidades de la bodega (y hacia arriba).</summary>
        private static Vector2 B(float bx, float by) => new Vector2((bx - BodegaLayout.CenterX) * Su, -(by - BodegaLayout.CenterY) * Su);

        private static Image BoardImage(Transform parent, string name, Sprite sprite, float wDp, float hDp, bool active = true)
        {
            var img = MakeImage(parent, name, sprite, active);
            img.rectTransform.sizeDelta = new Vector2(wDp * Su, hDp * Su);
            return img;
        }

        private void BuildBoard()
        {
            var br = _boardRoot;
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = Vector2.zero;
            float hullBox = 2f * (BodegaLayout.HullR + 3f);
            _hull = BoardImage(br, "Hull", null, hullBox, hullBox);
            for (int i = 0; i < _rivets.Length; i++)
            {
                _rivets[i] = BoardImage(br, "Rivet" + i, DiscSprite.Get(), 6.4f, 6.4f);
                _rivets[i].color = HullRivet;
            }
            BuildAirlock(br);

            _hatchLayer = Layer(br, "Hatches");
            _hatchLayer.anchorMin = _hatchLayer.anchorMax = _hatchLayer.pivot = new Vector2(0.5f, 0.5f);
            _hatchLayer.sizeDelta = Vector2.zero;
            for (int i = 0; i < _hv.Length; i++) _hv[i] = BuildHatch(_hatchLayer, i);

            _crateFlightGlow = BoardImage(br, "CrateFlightGlow", RadialGlowSprite.Get(), 56f, 56f, false);
            _crateFlightGlow.color = new Color(1f, 214f / 255f, 120f / 255f, 0.6f);
            _crateFlight = BoardImage(br, "CrateFlight", null, 38f, 32f, false);

            _beamSoft = MakeImage(br, "BeamSoft", RoundedRectSprite.Get(24), false);
            _beamCore = MakeImage(br, "BeamCore", RoundedRectSprite.Get(24), false);
            _beamSoft.color = BeamSoft;
            _beamCore.color = BeamCore;
            for (int i = 0; i < _beamSparks.Length; i++)
            {
                _beamSparks[i] = BoardImage(br, "BeamSpark" + i, DiscSprite.Get(), 4f, 4f, false);
                _beamSparks[i].color = Color.white;
            }

            _rope = MakeImage(br, "Rope", RoundedRectSprite.Get(24), false);
            _rope.color = new Color(191f / 255f, 233f / 255f, 255f / 255f, 0.6f);
            _crateCarriedGlow = BoardImage(br, "CrateGlow", RadialGlowSprite.Get(), 52f, 52f, false);
            _crateCarriedGlow.color = new Color(1f, 214f / 255f, 120f / 255f, 0.55f);
            _crateCarried = BoardImage(br, "Crate", null, 38f, 32f, false);

            _robotShadow = BoardImage(br, "RobotShadow", DiscSprite.Get(), 44f, 10f);
            _robotShadow.color = new Color(0f, 0f, 0f, 0.35f);
            var rr = new GameObject("Robot");
            rr.transform.SetParent(br, false);
            _robotRoot = rr.AddComponent<RectTransform>();
            _robotRoot.anchorMin = _robotRoot.anchorMax = _robotRoot.pivot = new Vector2(0.5f, 0.5f);
            _robotRoot.sizeDelta = Vector2.zero;
            float body = 2f * (BodegaSprites.RobotR + 3.5f);
            _robotBody = BoardImage(_robotRoot, "Body", null, body, body);
            _visor = BoardImage(_robotRoot, "Visor", DiscSprite.Get(), 30f, 18f);
            _visor.color = new Color(22f / 255f, 58f / 255f, 92f / 255f);
            _eyeL = BoardImage(_robotRoot, "EyeL", DiscSprite.Get(), 6f, 6.4f);
            _eyeR = BoardImage(_robotRoot, "EyeR", DiscSprite.Get(), 6f, 6.4f);
            _eyeL.color = _eyeR.color = Cyan;
            var ant = new GameObject("Antenna");
            ant.transform.SetParent(_robotRoot, false);
            _antRoot = ant.AddComponent<RectTransform>();
            _antRoot.anchorMin = _antRoot.anchorMax = _antRoot.pivot = new Vector2(0.5f, 0.5f);
            _antRoot.sizeDelta = Vector2.zero;
            _antRoot.anchoredPosition = new Vector2(0f, 24f * Su);
            _antStalk = BoardImage(_antRoot, "Stalk", RoundedRectSprite.Get(24), 2.4f, 10f);
            _antStalk.rectTransform.pivot = new Vector2(0.5f, 0f);
            _antStalk.rectTransform.anchoredPosition = new Vector2(0f, -1f * Su);
            _antStalk.type = Image.Type.Sliced;
            SetRadius(_antStalk, 1.2f * Su);
            _antStalk.color = NeuroStyle.Ink;
            _antGlow = BoardImage(_antRoot, "Glow", RadialGlowSprite.Get(), 18f, 18f);
            _antGlow.rectTransform.anchoredPosition = new Vector2(0f, 11f * Su);
            _antGlow.color = new Color(Gold.r, Gold.g, Gold.b, 0.5f);
            _antBall = BoardImage(_antRoot, "Ball", DiscSprite.Get(), 8f, 8f);
            _antBall.rectTransform.anchoredPosition = new Vector2(0f, 11f * Su);
            _antBall.color = Gold;
            _flyGlow = BoardImage(br, "FlyGlow", RadialGlowSprite.Get(), 52f, 52f, false);
            _flyGlow.color = new Color(1f, 214f / 255f, 120f / 255f, 0.7f);
            _flyObj = BoardImage(br, "FlyObj", null, 34f, 34f, false);

            br.gameObject.SetActive(false);
        }

        private void BuildAirlock(Transform br)
        {
            var v = new AirlockView();
            var go = new GameObject("Airlock");
            go.transform.SetParent(br, false);
            v.Root = go.AddComponent<RectTransform>();
            v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
            v.Root.sizeDelta = Vector2.zero;
            float glow = 2f * (BodegaLayout.HatchR + 26f);
            v.Glow = BoardImage(v.Root, "Flash", RadialGlowSprite.Get(), glow, glow);
            v.Glow.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
            float seal = 2f * (BodegaLayout.HatchR + 13f);
            v.Seal = BoardImage(v.Root, "Seal", null, seal, seal);
            float box = 2f * (BodegaSprites.WindowR + 2f);
            v.Body = BoardImage(v.Root, "Glass", null, box, box);
            v.Peek = BoardImage(v.Root, "Peek", null, 34f, 34f, false);
            _lock = v;
        }

        private HatchView BuildHatch(Transform parent, int index)
        {
            var v = new HatchView();
            var go = new GameObject("Hatch" + index);
            go.transform.SetParent(parent, false);
            v.Root = go.AddComponent<RectTransform>();
            v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
            v.Root.sizeDelta = Vector2.zero;
            v.Glow = BoardImage(v.Root, "Glow", RadialGlowSprite.Get(), 2f * (BodegaLayout.HatchR + 24f), 2f * (BodegaLayout.HatchR + 24f));
            v.Glow.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
            float fr = 2f * (BodegaSprites.FrameR + 2.5f);
            v.Frame = BoardImage(v.Root, "Frame", null, fr, fr);
            float ir = 2f * (BodegaSprites.HatchR + 1f);
            v.Interior = BoardImage(v.Root, "Interior", null, ir, ir);
            var mask = v.Interior.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            v.Obj = BoardImage(v.Interior.transform, "Obj", null, 40f, 40f);
            v.Empty = MakeSceneLabel(v.Interior.transform, "Empty", 14f, UiFonts.Regular, new Color(171f / 255f, 165f / 255f, 210f / 255f, 0.85f), TextAnchor.MiddleCenter);
            v.Empty.text = BodegaContract.EmptyLabel;
            var lv = new GameObject("Leaves");
            lv.transform.SetParent(v.Interior.transform, false);
            v.Leaves = lv.AddComponent<RectTransform>();
            v.Leaves.anchorMin = v.Leaves.anchorMax = v.Leaves.pivot = new Vector2(0.5f, 0.5f);
            v.Leaves.sizeDelta = Vector2.zero;
            float lf = 2f * (BodegaSprites.HatchR + 1.5f);
            v.LeafA = BoardImage(v.Leaves, "LeafA", null, lf, lf);
            v.LeafB = BoardImage(v.Leaves, "LeafB", null, lf, lf);
            v.LeafB.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            v.Seam = BoardImage(v.Leaves, "Seam", RoundedRectSprite.Get(24), 2f * BodegaLayout.HatchR, 3f);
            v.Seam.color = new Color(1f, 231f / 255f, 168f / 255f, 0f);
            v.Rim = BoardImage(v.Root, "Rim", null, fr, fr);
            go.SetActive(false);
            return v;
        }

        /// <summary>Pone los sprites horneados y enciende la escena.</summary>
        private void AssignSprites()
        {
            _hull.sprite = BodegaSprites.Hull();
            _lock.Body.sprite = BodegaSprites.Airlock();
            _lock.Seal.sprite = BodegaSprites.SealRing();
            foreach (var v in _hv)
            {
                v.Frame.sprite = BodegaSprites.HatchFrame();
                v.Rim.sprite = BodegaSprites.HatchRim();
                v.LeafA.sprite = v.LeafB.sprite = BodegaSprites.DoorLeaf();
                SetKind(v, 2);
            }
            _robotBody.sprite = BodegaSprites.RobotBody();
            _crateFlight.sprite = _crateCarried.sprite = BodegaSprites.Crate();
            foreach (var t in _tray) t.Check.sprite = BodegaSprites.Check();
            _introFrame.sprite = BodegaSprites.HatchFrame();
            _introInterior.sprite = BodegaSprites.HatchInterior(1);
            _introFrame.rectTransform.sizeDelta = new Vector2(2f * (BodegaSprites.FrameR + 2.5f), 2f * (BodegaSprites.FrameR + 2.5f));
            _boardRoot.gameObject.SetActive(true);
        }


        private static void SetKind(HatchView v, int kind)
        {
            if (v.Kind == kind) return;
            v.Kind = kind;
            v.Interior.sprite = BodegaSprites.HatchInterior(kind);
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

        /// <summary>Un texto de la bodega: su tamaño en pantalla es el de <paramref name="dp"/> aunque la bodega se achique (se compensa en <see cref="Layout"/>).</summary>
        private Text MakeSceneLabel(Transform parent, string name, float dp, Font font, Color color, TextAnchor anchor)
        {
            var t = MakeText(parent, name, Mathf.RoundToInt(dp * Su), anchor, color, 0f, 0f);
            t.font = font;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(60f * Su, 20f * Su);
            _boardFonts.Add(new KeyValuePair<Text, float>(t, dp));
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

        // ------------------------------------------------------------------ disposición

        private Vector2 P(Vector2 l) => new Vector2((l.x - BodegaLayout.W * 0.5f) * _s, _playH * 0.5f - l.y * _s);
        private Vector2 P(float x, float y) => P(new Vector2(x, y));
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + BodegaLayout.W * 0.5f, (_playH * 0.5f - u.y) / _s);

        /// <summary>De un punto de la bodega (coordenadas del boceto) a dp lógicos.</summary>
        private Vector2 L(float bx, float by)
        {
            BodegaLayout.BoardToLogical(_lay, bx, by, out float lx, out float ly);
            return new Vector2(lx, ly);
        }

        private void SetRect(RectTransform r, float cx, float cy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = P(cx, cy);
        }

        /// <summary>Coloca una pieza respecto del centro de su padre (dp; y hacia abajo).</summary>
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
            _s = _playW / BodegaLayout.W;
            _logicalH = _playH / _s;
            _lay = BodegaLayout.Compute(_logicalH);
            foreach (var kv in _fonts)
            {
                int px = Mathf.RoundToInt(kv.Value * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            float k = _lay.Scale;
            _boardRoot.localScale = Vector3.one * (_s / Su * k);
            _boardRoot.anchoredPosition = P(BodegaLayout.W / 2f, _lay.BoardCenterY);
            foreach (var kv in _boardFonts) kv.Key.fontSize = Mathf.RoundToInt(kv.Value * Su / k);

            // la tarjeta de arriba
            float cw = BodegaLayout.W - 20f, cy = _lay.CardTop + BodegaLayout.CardH / 2f;
            SetRect(_cardRim.rectTransform, BodegaLayout.W / 2f, cy, cw + 3f, BodegaLayout.CardH + 3f);
            SetRect(_cardBg.rectTransform, BodegaLayout.W / 2f, cy, cw, BodegaLayout.CardH);
            SetRadius(_cardRim, 19.5f * _s);
            SetRadius(_cardBg, 18f * _s);
            LayoutCardTexts();

            // el carro de reparto
            SetRect(_trayLabel.rectTransform, 14f + 70f, _lay.TrayLabelY, 150f, BodegaLayout.TrayLabelH);
            SetRect(_recordText.rectTransform, BodegaLayout.W - 14f - 80f, _lay.TrayLabelY, 160f, BodegaLayout.TrayLabelH);
            LayoutTray();

            // el aviso de abajo
            float tw = BodegaLayout.W - 20f, th = BodegaLayout.ToastH;
            SetRect(_toastRoot, BodegaLayout.W / 2f, _lay.ToastTop + th / 2f, tw, th);
            SetChild(_toastRim.rectTransform, 0f, 0f, tw + 3f, th + 3f);
            SetChild(_toastBg.rectTransform, 0f, 0f, tw, th);
            SetRadius(_toastRim, 15.5f * _s);
            SetRadius(_toastBg, 14f * _s);
            LayoutToast(true);
        }

        private bool _cardWide;

        /// <summary>Los textos de la tarjeta: con el dibujo del objeto (a la izquierda) o centrados.</summary>
        private void LayoutCardTexts()
        {
            float cw = BodegaLayout.W - 20f, top = _lay.CardTop, h = BodegaLayout.CardH;
            if (_cardWide)
            {
                SetRect(_cardDisc.rectTransform, 10f + 32f, top + h / 2f, 48f, 48f);
                SetRect(_cardObj.rectTransform, 10f + 32f, top + h / 2f, 40f, 40f);
                SetRect(_cardA.rectTransform, 10f + 72f + (cw - 72f - 12f) / 2f, top + 19f, cw - 72f - 12f, 20f);
                SetRect(_cardB.rectTransform, 10f + 72f + (cw - 72f - 12f) / 2f, top + 44f, cw - 72f - 12f, 28f);
            }
            SetRect(_cardT.rectTransform, BodegaLayout.W / 2f, top + 22f, cw - 16f, 28f);
            SetRect(_cardS.rectTransform, BodegaLayout.W / 2f, top + 47f, cw - 16f, 22f);
        }

        private void LayoutTray()
        {
            int n = Mathf.Max(1, _order != null ? _order.Objects : 2);
            for (int i = 0; i < _tray.Length; i++)
            {
                var v = _tray[i];
                bool on = i < n;
                v.Root.gameObject.SetActive(on && _trayLayer.gameObject.activeSelf);
                if (!on) continue;
                float x = BodegaLayout.TrayX(i, n);
                SetRect(v.Root, x, _lay.TrayCenterY, BodegaLayout.TrayCircle, BodegaLayout.TrayCircle);
                SetChild(v.Border.rectTransform, 0f, 0f, BodegaLayout.TrayCircle, BodegaLayout.TrayCircle);
                SetChild(v.Inner.rectTransform, 0f, 0f, BodegaLayout.TrayCircle - 4f, BodegaLayout.TrayCircle - 4f);
                SetChild(v.Obj.rectTransform, 0f, 0f, 26f, 26f);
                SetChild(v.Check.rectTransform, 11f, -14f, 12f, 10f);
            }
        }

        private bool _toastHasSub = true;

        private void LayoutToast(bool force)
        {
            float tw = BodegaLayout.W - 20f, th = BodegaLayout.ToastH;
            bool hasSub = _toastSubFull.Length > 0;
            if (!force && hasSub == _toastHasSub) return;
            _toastHasSub = hasSub;
            if (hasSub)
            {
                SetChild(_toastTitle.rectTransform, 0f, -th * 0.5f + 20f, tw - 20f, 22f);
                SetChild(_toastSub.rectTransform, 0f, th * 0.5f - 19f, tw - 20f, 26f);
            }
            else SetChild(_toastTitle.rectTransform, 0f, 0f, tw - 20f, th - 10f);
            _toastSub.gameObject.SetActive(hasSub);
        }

        /// <summary>La tarjeta «NUEVO»: el título, la escotilla que abre y cierra, el texto (cada línea puede partirse en dos si no cabe en 14 dp o más) y «Toca para seguir». Crece con el texto.</summary>
        private void LayoutIntro(string[] lines)
        {
            float cw = BodegaLayout.W - 36f, cx = BodegaLayout.W / 2f;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) sb.Append(lines[i]);
                else sb.Append(i == 1 ? "\n<color=#D6D1F2>" : "\n").Append(lines[i]);
            }
            if (lines.Length > 1) sb.Append("</color>");
            _introBody.text = sb.ToString();
            _introBody.rectTransform.sizeDelta = new Vector2((cw - 28f) * _s, 400f * _s);
            float bodyH = _introBody.preferredHeight / _s;
            float ch = 128f + bodyH + 40f;
            float top = Mathf.Min(_logicalH * 0.5f - ch / 2f, _logicalH - ch - 12f);
            top = Mathf.Max(top, 12f);
            SetRect(_introRim.rectTransform, cx, top + ch / 2f, cw + 4f, ch + 4f);
            SetRect(_introCard.rectTransform, cx, top + ch / 2f, cw, ch);
            SetRadius(_introRim, 28f * _s);
            SetRadius(_introCard, 26f * _s);
            SetRect(_introTitle.rectTransform, cx, top + 26f, 220f, 24f);
            SetRect(_introBody.rectTransform, cx, top + 120f + bodyH / 2f, cw - 28f, bodyH + 4f);
            SetRect(_introTap.rectTransform, cx, top + ch - 24f, 200f, 24f);
            float iy = top + 76f;
            SetRect(_introFrame.rectTransform, cx, iy, 2f * (BodegaSprites.FrameR + 2.5f), 2f * (BodegaSprites.FrameR + 2.5f));
            SetRect(_introInterior.rectTransform, cx, iy, 2f * (BodegaSprites.HatchR + 1f), 2f * (BodegaSprites.HatchR + 1f));
            _introIllustration = new Vector2(cx, iy);
        }

        private Vector2 _introIllustration;
    }
}
