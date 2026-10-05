using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion;

namespace NeuroVida.Games.Engranajes
{
    /// <summary>La interfaz de «Engranajes»: se arma una vez por código (marcador, cabecera con las luces del cohete, pregunta, sala de máquinas con el cohete, botones, aviso, tarjeta «NUEVO» y
    /// despegue) y se coloca en <see cref="Layout"/> según la pantalla. Todo lo de la escena (sala + cohete) vive en unidades del boceto (3 por dp) bajo <c>_sceneRoot</c>, que se escala entero.</summary>
    public sealed partial class EngranajesGameController
    {
        private const int GearPool = 40, LinePool = 64, PulsePool = 24, SparkPool = 70, DashPool = 64, PillPool = 4, ArrowPool = GearPool;

        // colores del boceto aprobado
        private static readonly Color Cyan = new Color(127f / 255f, 216f / 255f, 255f / 255f);
        private static readonly Color Gold = new Color(255f / 255f, 201f / 255f, 74f / 255f);
        private static readonly Color GoldSoft = new Color(255f / 255f, 224f / 255f, 138f / 255f);
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Mint = new Color(159f / 255f, 245f / 255f, 214f / 255f);
        private static readonly Color WarnText = new Color(255f / 255f, 214f / 255f, 160f / 255f);
        private static readonly Color WrongFill = new Color(255f / 255f, 180f / 255f, 140f / 255f);
        private static readonly Color ButtonFill = new Color(35f / 255f, 43f / 255f, 87f / 255f);
        private static readonly Color ButtonText = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color LightOff = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color WindowOff = new Color(58f / 255f, 65f / 255f, 112f / 255f);
        private static readonly Color PillIdle = new Color(244f / 255f, 241f / 255f, 255f / 255f, 0.88f);
        private static readonly Color BeltDark = new Color(42f / 255f, 35f / 255f, 80f / 255f);
        private static readonly Color BeltLight = new Color(142f / 255f, 131f / 255f, 216f / 255f);
        private static readonly Color ShaftDark = new Color(59f / 255f, 52f / 255f, 112f / 255f);
        private static readonly Color ShaftLight = new Color(107f / 255f, 95f / 255f, 184f / 255f);
        private static readonly Color Coral = new Color(255f / 255f, 140f / 255f, 107f / 255f);

        private sealed class GearView
        {
            public RectTransform Root;
            public Image Glow, Support, Shadow, Body, Hub;
            public float Tip;
        }

        private sealed class LineView { public RectTransform Rt; public Image Img; }

        private sealed class PillView
        {
            public RectTransform Root;
            public Image Bg;
            public Text Label;
        }

        private sealed class ButtonView
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Shadow, Rim, Bg, Icon;
            public Text Label;
            public Rect Rect;          // lógico (dp)
            public ButtonDef Def;
        }

        private sealed class Spark { public Image Img; public Vector2 Pos, Vel; public float Age, Life, Size; public bool Alive; public Color Color; }

        // ------------------------------------------------------------------ piezas

        private RectTransform _safe, _play, _stripLayer, _sceneRoot, _rocketRoot, _machineLayer, _uiLayer, _fxLayer, _introLayer, _launchLayer, _questionBar;
        private RectTransform _timerTrack, _timerHead, _motorArrowRect;
        private Image _timerFill, _questionBg;
        private CanvasGroup _machineGroup, _questionGroup, _sayGroup, _introGroup;
        private Text _rocketTitle, _orbitText, _questionText, _sayTitle, _saySub, _introTitle, _introLine1, _introLine2, _launchTitle, _launchLine1, _launchLine2, _slotMark;
        private Image _room, _hull, _slotRing, _targetRing, _motorArrow, _sayBg, _sayRim, _dim, _flyerGlow, _flyerDot, _flame, _gateGlow, _stripGlow;
        private Image _introCard, _introRim;
        private readonly Image[] _windows = new Image[EngranajesContract.RocketLights];
        private readonly Image[] _windowGlows = new Image[EngranajesContract.RocketLights];
        private readonly Image[] _lights = new Image[EngranajesContract.RocketLights];
        private readonly RectTransform[] _partRoots = new RectTransform[4];
        private readonly CanvasGroup[] _partGroups = new CanvasGroup[4];
        private Image _dish, _fan, _door, _cargoFrame, _cargoBox, _cargoPlate, _turbineFlame;
        private RectTransform _doorRt, _cargoLiftRt;
        private readonly GearView[] _gears = new GearView[GearPool];
        private GearView _slotGear;
        private readonly LineView[] _lines = new LineView[LinePool];
        private int _lineCount;
        private readonly LineView[] _slotBelt = new LineView[4];
        private readonly PillView[] _beltPills = new PillView[PillPool];
        private readonly PillView[] _stationPills = new PillView[4];
        private PillView _motorPill;
        private readonly Image[] _pulses = new Image[PulsePool];
        private readonly Image[] _dashes = new Image[DashPool];
        private readonly Image[] _arrows = new Image[ArrowPool];
        private readonly ButtonView[] _buttons = new ButtonView[3];
        private int _buttonCount;
        private readonly List<Spark> _sparks = new List<Spark>();
        private GearView _introA, _introB;
        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();
        private readonly List<KeyValuePair<Text, float>> _sceneFonts = new List<KeyValuePair<Text, float>>();
        private float _questionDp = 18f;
        private int _launchNumber;

        // lo que dice el aviso de abajo
        private string _sayTitleFull = "", _saySubFull = "";
        private float _sayAt = -10f;
        private bool _sayGood;
        // la luz que vuela desde la pieza hasta la cabecera
        private bool _flyerOn;
        private Vector2 _flyerFrom;
        private float _flyerAt;
        private float _appearAt = -10f;

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("EngranajesCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.CieloDeCristal);           // el cielo quieto de Rastro de luz: aquí lo que se mueve es la máquina

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Engranajes", MarginU, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _stripLayer = Layer(_play, "Strip");
            _questionBar = Layer(_play, "Question");
            _sceneRoot = Layer(_play, "Scene");
            _uiLayer = Layer(_play, "Ui");
            _fxLayer = Layer(_play, "Fx");
            _launchLayer = Layer(_play, "Launch");
            _introLayer = Layer(_play, "Intro");
            BuildStrip();
            BuildQuestion();
            BuildScene();
            BuildButtons();
            BuildSay();
            BuildFx();
            BuildLaunch();
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

        /// <summary>«Cohete n.º N», las 10 luces y «N cohetes en órbita» (como la cabecera del boceto).</summary>
        private void BuildStrip()
        {
            _stripGlow = MakeImage(_stripLayer, "LightGlow", RadialGlowSprite.Get());
            _stripGlow.color = Gold;
            _stripGlow.gameObject.SetActive(false);
            for (int k = 0; k < _lights.Length; k++) _lights[k] = MakeImage(_stripLayer, "Light" + k, DiscSprite.Get());
            _rocketTitle = MakeLabel(_stripLayer, "Title", 17f, UiFonts.Bold, ButtonText, TextAnchor.MiddleLeft);
            _orbitText = MakeLabel(_stripLayer, "Orbit", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleLeft);
            _rocketTitle.horizontalOverflow = _orbitText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _rocketTitle.text = "Cohete n.º 1";
        }

        private void BuildQuestion()
        {
            _questionBg = MakeImage(_questionBar, "Bg", RoundedRectSprite.Get(48));
            _questionBg.type = Image.Type.Sliced;
            _questionBg.color = new Color(20f / 255f, 27f / 255f, 58f / 255f);
            _questionGroup = _questionBar.gameObject.AddComponent<CanvasGroup>();
            _questionGroup.blocksRaycasts = false;
            _questionText = MakeLabel(_questionBar, "Text", 18f, UiFonts.Bold, ButtonText, TextAnchor.MiddleCenter);
            _fonts.RemoveAt(_fonts.Count - 1);                      // el tamaño lo fija ApplyQuestion (18 dp en una línea, 16 en dos)
            _questionText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _questionText.verticalOverflow = VerticalWrapMode.Overflow;
            _questionBar.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ la escena: sala + cohete + máquina (coordenadas del boceto)

        private static Vector2 B(float bx, float by) => new Vector2((bx - 180f) * Su, -(by - 280f) * Su);

        private static Image SceneImage(Transform parent, string name, Sprite sprite, float bx, float by, float wDp, float hDp, bool active = true)
        {
            var img = MakeImage(parent, name, sprite, active);
            img.rectTransform.anchoredPosition = B(bx, by);
            img.rectTransform.sizeDelta = new Vector2(wDp * Su, hDp * Su);
            return img;
        }

        private static RectTransform SceneNode(Transform parent, string name, float bx, float by)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = B(bx, by);
            r.sizeDelta = Vector2.zero;
            return r;
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

        private void BuildScene()
        {
            var sr = _sceneRoot;
            sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0.5f);
            sr.sizeDelta = Vector2.zero;
            _room = SceneImage(sr, "Room", null, 129f, 288f, 242f, 348f);

            // el cohete (todo junto: despega entero)
            _rocketRoot = SceneNode(sr, "Rocket", 180f, 280f);
            _rocketRoot.anchoredPosition = Vector2.zero;
            _hull = SceneImage(_rocketRoot, "Hull", null, 306f, 267f, 116f, 358f);
            for (int k = 0; k < _windows.Length; k++)
            {
                float y = 154f + k * ((428f - 142f - 90f) / (EngranajesContract.RocketLights - 1));
                _windowGlows[k] = SceneImage(_rocketRoot, "WindowGlow" + k, RadialGlowSprite.Get(), 338f, y, 20f, 20f);
                _windowGlows[k].color = new Color(1f, 201f / 255f, 74f / 255f, 0.6f);
                _windows[k] = SceneImage(_rocketRoot, "Window" + k, DiscSprite.Get(), 338f, y, 8.4f, 8.4f);
            }
            BuildParts();
            _targetRing = SceneImage(_rocketRoot, "TargetRing", null, 306f, 98f, 64f, 64f, false);
            _targetRing.color = Gold;
            _flame = SceneImage(_rocketRoot, "Flame", RadialGlowSprite.Get(), 306f, 452f, 72f, 90f, false);
            _flame.color = new Color(1f, 180f / 255f, 90f / 255f, 0.95f);

            // la máquina
            var ml = new GameObject("Machine");
            ml.transform.SetParent(sr, false);
            _machineLayer = ml.AddComponent<RectTransform>();
            Stretch(_machineLayer);
            _machineGroup = ml.AddComponent<CanvasGroup>();
            _machineGroup.blocksRaycasts = false;
            for (int i = 0; i < LinePool; i++)
            {
                var img = MakeImage(_machineLayer, "Line" + i, null, false);
                _lines[i] = new LineView { Rt = img.rectTransform, Img = img };
            }
            for (int i = 0; i < _slotBelt.Length; i++)
            {
                var img = MakeImage(_machineLayer, "SlotBelt" + i, null, false);
                _slotBelt[i] = new LineView { Rt = img.rectTransform, Img = img };
            }
            for (int i = 0; i < GearPool; i++) _gears[i] = BuildGear(_machineLayer, "Gear" + i);
            _slotGear = BuildGear(_machineLayer, "SlotGear");
            _slotRing = MakeImage(_machineLayer, "SlotRing", null, false);
            _slotRing.color = Gold;
            _slotMark = MakeSceneLabel(_machineLayer, "SlotMark", 22f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _slotMark.text = "?";
            _slotMark.gameObject.SetActive(false);
            for (int i = 0; i < PillPool; i++) _beltPills[i] = BuildPill(_machineLayer, "BeltPill" + i, 72f, 22f, new Color(35f / 255f, 43f / 255f, 87f / 255f), ButtonText, 14f);
            _motorArrow = MakeImage(_machineLayer, "MotorArrow", null, false);
            _motorArrow.color = Cyan;
            _motorArrowRect = _motorArrow.rectTransform;
            for (int i = 0; i < PulsePool; i++)
            {
                _pulses[i] = SceneImage(_machineLayer, "Pulse" + i, RadialGlowSprite.Get(), 0f, 0f, 32f, 32f, false);
                _pulses[i].color = new Color(1f, 214f / 255f, 120f / 255f, 1f);
            }
            for (int i = 0; i < DashPool; i++)
            {
                var d = MakeImage(_machineLayer, "Dash" + i, null, false);
                d.color = Coral;
                _dashes[i] = d;
            }
            for (int i = 0; i < _stationPills.Length; i++) _stationPills[i] = BuildPill(_machineLayer, "Label" + (Station)i, 80f, 22f, PillIdle, NeuroStyle.Ink, 14f);
            _motorPill = BuildPill(_machineLayer, "MotorLabel", 60f, 22f, new Color(22f / 255f, 58f / 255f, 92f / 255f), new Color(191f / 255f, 233f / 255f, 1f), 14f);
            _motorPill.Label.text = "MOTOR";
            for (int i = 0; i < ArrowPool; i++)
            {
                var a = MakeImage(_machineLayer, "Arrow" + i, null, false);
                _arrows[i] = a;
            }
            _machineLayer.gameObject.SetActive(false);
            _sceneRoot.gameObject.SetActive(false);        // hasta que se horneen los sprites
        }

        private void BuildParts()
        {
            // antena: mástil y radar (gira con su engranaje)
            var ant = SceneNode(_rocketRoot, "Antena", 306f, 98f);
            _partRoots[(int)Station.Antena] = ant;
            _partGroups[(int)Station.Antena] = ant.gameObject.AddComponent<CanvasGroup>();
            var mast = SceneImage(ant, "Mast", RoundedRectSprite.Get(8), 0f, 0f, 6f, 14f);
            mast.rectTransform.anchoredPosition = new Vector2(0f, -7f * Su);
            mast.type = Image.Type.Sliced;
            mast.color = new Color(74f / 255f, 80f / 255f, 128f / 255f);
            _dish = MakeImage(ant, "Dish", null);
            _dish.rectTransform.sizeDelta = new Vector2(48f * Su, 24f * Su);

            // compuerta: el hueco y el portón que sube y baja
            var gate = SceneNode(_rocketRoot, "Compuerta", 312f, 250f);
            _partRoots[(int)Station.Compuerta] = gate;
            _partGroups[(int)Station.Compuerta] = gate.gameObject.AddComponent<CanvasGroup>();
            var hole = MakeImage(gate, "Hole", RoundedRectSprite.Get(24));
            hole.type = Image.Type.Sliced;
            SetPpuMultiplier(hole, 24f / (6f * Su));
            hole.color = new Color(30f / 255f, 37f / 255f, 80f / 255f);
            hole.rectTransform.anchoredPosition = new Vector2(4f * Su, 6f * Su);
            hole.rectTransform.sizeDelta = new Vector2(40f * Su, 48f * Su);
            _gateGlow = MakeImage(gate, "Glow", RadialGlowSprite.Get());
            _gateGlow.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
            _gateGlow.rectTransform.anchoredPosition = new Vector2(4f * Su, 5f * Su);
            _gateGlow.rectTransform.sizeDelta = new Vector2(60f * Su, 70f * Su);
            _door = MakeImage(gate, "Door", null);
            _door.rectTransform.sizeDelta = new Vector2(44f * Su, 52f * Su);
            _doorRt = _door.rectTransform;
            _doorRt.anchoredPosition = new Vector2(4f * Su, 6f * Su);

            // carga: el elevador con su caja
            var cargo = SceneNode(_rocketRoot, "Carga", 312f, 316f);
            _partRoots[(int)Station.Carga] = cargo;
            _partGroups[(int)Station.Carga] = cargo.gameObject.AddComponent<CanvasGroup>();
            var frame = MakeImage(cargo, "Frame", null);
            frame.rectTransform.sizeDelta = new Vector2(48f * Su, 68f * Su);
            frame.rectTransform.anchoredPosition = new Vector2(4f * Su, 0f);
            _cargoFrame = frame;
            var lift = SceneNode(cargo, "Lift", 316f, 318f);
            lift.anchoredPosition = new Vector2(4f * Su, -2f * Su);
            _cargoLiftRt = lift;
            _cargoPlate = SceneImage(lift, "Plate", null, 316f, 325f, 40f, 10f);
            _cargoPlate.rectTransform.anchoredPosition = new Vector2(0f, -7f * Su);
            _cargoBox = SceneImage(lift, "Box", null, 316f, 312f, 32f, 24f);
            _cargoBox.rectTransform.anchoredPosition = new Vector2(0f, 6f * Su);

            // turbina: ventilador y llama
            var turbine = SceneNode(_rocketRoot, "Turbina", 306f, 442f);
            _partRoots[(int)Station.Turbina] = turbine;
            _partGroups[(int)Station.Turbina] = turbine.gameObject.AddComponent<CanvasGroup>();
            _turbineFlame = MakeImage(turbine, "Flame", RadialGlowSprite.Get(), false);
            _turbineFlame.color = new Color(Coral.r, Coral.g, Coral.b, 0f);
            _turbineFlame.rectTransform.anchoredPosition = new Vector2(0f, -23f * Su);
            _turbineFlame.rectTransform.sizeDelta = new Vector2(48f * Su, 46f * Su);
            _fan = MakeImage(turbine, "Fan", null);
            _fan.rectTransform.sizeDelta = new Vector2(40f * Su, 40f * Su);
        }

        private GearView BuildGear(Transform parent, string name)
        {
            var v = new GearView();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            v.Root = go.AddComponent<RectTransform>();
            v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
            v.Glow = MakeImage(v.Root, "Glow", RadialGlowSprite.Get());
            v.Glow.color = new Color(1f, 214f / 255f, 120f / 255f, 0f);
            v.Support = MakeImage(v.Root, "Support", DiscSprite.Get());
            v.Support.color = new Color(11f / 255f, 16f / 255f, 48f / 255f);
            v.Shadow = MakeImage(v.Root, "Shadow", null);
            v.Shadow.color = new Color(0f, 0f, 0f, 0.45f);
            v.Body = MakeImage(v.Root, "Body", null);
            v.Hub = MakeImage(v.Root, "Hub", null);
            go.SetActive(false);
            return v;
        }

        private PillView BuildPill(Transform parent, string name, float wDp, float hDp, Color fill, Color text, float fontDp)
        {
            var p = new PillView();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            p.Root = go.AddComponent<RectTransform>();
            p.Root.anchorMin = p.Root.anchorMax = p.Root.pivot = new Vector2(0.5f, 0.5f);
            p.Root.sizeDelta = new Vector2(wDp * Su, hDp * Su);
            p.Bg = go.AddComponent<Image>();
            p.Bg.sprite = RoundedRectSprite.Get(24);
            p.Bg.color = fill;
            p.Bg.raycastTarget = false;
            SetRadius(p.Bg, hDp * Su / 2f);
            p.Label = MakeSceneLabel(p.Root, "Text", fontDp, UiFonts.Bold, text, TextAnchor.MiddleCenter);
            p.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
            p.Label.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(p.Label.rectTransform);
            go.SetActive(false);
            return p;
        }

        // ------------------------------------------------------------------ botones, aviso, efectos, despegue y tarjeta «NUEVO»

        private void BuildButtons()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                var b = new ButtonView();
                var go = new GameObject("Button" + i);
                go.transform.SetParent(_uiLayer, false);
                b.Root = go.AddComponent<RectTransform>();
                b.Root.anchorMin = b.Root.anchorMax = b.Root.pivot = new Vector2(0.5f, 0.5f);
                b.Group = go.AddComponent<CanvasGroup>();
                b.Group.blocksRaycasts = false;
                b.Shadow = MakeImage(b.Root, "Shadow", RoundedRectSprite.Get(24));
                b.Shadow.color = new Color(0f, 0f, 0f, 0.45f);
                b.Rim = MakeImage(b.Root, "Rim", RoundedRectSprite.Get(24));
                b.Bg = MakeImage(b.Root, "Bg", RoundedRectSprite.Get(24));
                b.Icon = MakeImage(b.Root, "Icon", null);
                b.Label = MakeLabel(b.Root, "Label", 17f, UiFonts.Bold, ButtonText, TextAnchor.MiddleCenter);
                _fonts.RemoveAt(_fonts.Count - 1);                  // el tamaño lo fija LayoutButtons (15 o 17 dp según el ancho)
                b.Label.horizontalOverflow = HorizontalWrapMode.Wrap;
                b.Label.verticalOverflow = VerticalWrapMode.Overflow;
                go.SetActive(false);
                _buttons[i] = b;
            }
        }

        private void BuildSay()
        {
            var go = new GameObject("Say");
            go.transform.SetParent(_uiLayer, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            _sayGroup = go.AddComponent<CanvasGroup>();
            _sayGroup.blocksRaycasts = false;
            _sayRim = MakeImage(r, "Rim", RoundedRectSprite.Get(24));
            _sayBg = MakeImage(r, "Bg", RoundedRectSprite.Get(24));
            _sayBg.color = new Color(8f / 255f, 10f / 255f, 34f / 255f, 0.95f);
            _sayTitle = MakeLabel(r, "Title", 16f, UiFonts.Bold, Mint, TextAnchor.MiddleCenter);
            _saySub = MakeLabel(r, "Sub", 14f, UiFonts.Regular, Gold, TextAnchor.MiddleCenter);
            _sayTitle.horizontalOverflow = _saySub.horizontalOverflow = HorizontalWrapMode.Wrap;
            _sayTitle.verticalOverflow = _saySub.verticalOverflow = VerticalWrapMode.Overflow;
            go.SetActive(false);
            _sayRoot = r;
        }

        private RectTransform _sayRoot;

        private void BuildFx()
        {
            for (int i = 0; i < SparkPool; i++)
            {
                var img = MakeImage(_fxLayer, "Spark", DiscSprite.Get(), false);
                _sparks.Add(new Spark { Img = img });
            }
            _flyerGlow = MakeImage(_fxLayer, "FlyerGlow", RadialGlowSprite.Get(), false);
            _flyerGlow.color = Gold;
            _flyerDot = MakeImage(_fxLayer, "FlyerDot", DiscSprite.Get(), false);
            _flyerDot.color = GoldSoft;
        }

        private void BuildLaunch()
        {
            _launchTitle = MakeLabel(_launchLayer, "Title", 30f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _launchLine1 = MakeLabel(_launchLayer, "Line1", 17f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _launchLine2 = MakeLabel(_launchLayer, "Line2", 17f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            foreach (var t in new[] { _launchTitle, _launchLine1, _launchLine2 }) t.horizontalOverflow = HorizontalWrapMode.Overflow;
            _launchLayer.gameObject.SetActive(false);
        }

        private void BuildIntro()
        {
            _dim = MakeImage(_introLayer, "Dim", DiscSprite.Get());
            Stretch(_dim.rectTransform);
            _dim.color = new Color(2f / 255f, 3f / 255f, 15f / 255f, 0.74f);
            _dim.raycastTarget = false;
            _introRim = MakeImage(_introLayer, "CardRim", RoundedRectSprite.Get(24));
            _introRim.color = Gold;
            _introCard = MakeImage(_introLayer, "Card", RoundedRectSprite.Get(24));
            _introCard.color = new Color(20f / 255f, 27f / 255f, 58f / 255f);
            _introTitle = MakeLabel(_introLayer, "Title", 16f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _introLine1 = MakeLabel(_introLayer, "Line1", 17f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _introLine2 = MakeLabel(_introLayer, "Line2", 16f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            var tap = MakeLabel(_introLayer, "Tap", 16f, UiFonts.Bold, Cyan, TextAnchor.MiddleCenter);
            tap.text = "Toca para seguir";
            _introTap = tap;
            foreach (var t in new[] { _introTitle, _introLine1, _introLine2, tap }) { t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; }
            _introA = BuildGear(_introLayer, "IntroGearA");
            _introB = BuildGear(_introLayer, "IntroGearB");
            _introGroup = _introLayer.gameObject.AddComponent<CanvasGroup>();
            _introGroup.blocksRaycasts = false;
            _introLayer.gameObject.SetActive(false);
        }

        private Text _introTap;

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
            var extra = AddResultText("Extra", 40, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            extra.horizontalOverflow = HorizontalWrapMode.Wrap;
            go.SetActive(false);
        }

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

        /// <summary>Un texto de la escena: su tamaño en pantalla es el de <paramref name="dp"/> aunque la escena se achique (se compensa en <see cref="Layout"/>).</summary>
        private Text MakeSceneLabel(Transform parent, string name, float dp, Font font, Color color, TextAnchor anchor)
        {
            var t = MakeText(parent, name, Mathf.RoundToInt(dp * Su), anchor, color, 0f, 0f);
            t.font = font;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            _sceneFonts.Add(new KeyValuePair<Text, float>(t, dp));
            return t;
        }

        /// <summary>Pone los sprites horneados (los que no llevan la máquina) y enciende la escena.</summary>
        private void AssignSprites()
        {
            _room.sprite = EngranajesSprites.Room();
            _hull.sprite = EngranajesSprites.Hull();
            _dish.sprite = EngranajesSprites.Dish();
            _fan.sprite = EngranajesSprites.Fan();
            _door.sprite = EngranajesSprites.Door();
            _cargoFrame.sprite = EngranajesSprites.CargoFrame();
            _cargoPlate.sprite = EngranajesSprites.CargoPlate();
            _cargoBox.sprite = EngranajesSprites.CargoBox();
            _targetRing.sprite = EngranajesSprites.TargetRing();
            _slotRing.sprite = EngranajesSprites.DashedRing();
            for (int i = 0; i < _lines.Length; i++) { _lines[i].Img.sprite = RoundedRectSprite.Get(24); SetRadius(_lines[i].Img, 3f); }
            for (int i = 0; i < _slotBelt.Length; i++) { _slotBelt[i].Img.sprite = RoundedRectSprite.Get(24); SetRadius(_slotBelt[i].Img, 3f); }
            for (int i = 0; i < _dashes.Length; i++) { _dashes[i].sprite = RoundedRectSprite.Get(24); SetRadius(_dashes[i], 1.5f); }
            foreach (var g in _gears) PrepareGearSprites(g);
            PrepareGearSprites(_slotGear);
            PrepareGearSprites(_introA);
            PrepareGearSprites(_introB);
            _sceneRoot.gameObject.SetActive(true);
        }

        private static void PrepareGearSprites(GearView v)
        {
            v.Hub.sprite = EngranajesSprites.Hub();
        }

        // ------------------------------------------------------------------ disposición

        private Vector2 P(Vector2 l) => new Vector2((l.x - EngranajesLayout.W * 0.5f) * _s, _playH * 0.5f - l.y * _s);
        private Vector2 P(float x, float y) => P(new Vector2(x, y));
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + EngranajesLayout.W * 0.5f, (_playH * 0.5f - u.y) / _s);
        /// <summary>De un punto del boceto (dentro de la escena) a dp lógicos.</summary>
        private Vector2 L(float bx, float by) => EngranajesLayout.SceneToLogical(_lay, bx, by);

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
            _s = _playW / EngranajesLayout.W;
            _logicalH = _playH / _s;
            _lay = EngranajesLayout.Compute(_logicalH);
            foreach (var kv in _fonts)
            {
                int px = Mathf.RoundToInt(kv.Value * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            float k = _lay.SceneScale;
            _sceneRoot.localScale = Vector3.one * (_s / Su * k);
            _sceneRoot.anchoredPosition = P(EngranajesLayout.W / 2f + EngranajesLayout.SceneShiftX, _lay.SceneCenterLogicalY);
            foreach (var kv in _sceneFonts) kv.Key.fontSize = Mathf.RoundToInt(kv.Value * Su / k);
            ApplyLabelFonts();

            // cabecera: «Cohete n.º N», las 10 luces (x = 150 + 19 k) y los cohetes en órbita
            float row1 = _lay.StripTop + 14f, row2 = _lay.StripTop + 34f;
            SetRect(_rocketTitle.rectTransform, 14f + 65f, row1, 130f, 24f);
            SetRect(_orbitText.rectTransform, 14f + 100f, row2, 200f, 20f);
            for (int i = 0; i < _lights.Length; i++) SetRect(_lights[i].rectTransform, 150f + i * 19f, row1, 12f, 12f);
            SetRect(_stripGlow.rectTransform, 150f, row1, 36f, 36f);

            // pregunta
            SetRect(_questionBg.rectTransform, EngranajesLayout.W / 2f, _lay.QuestionTop + EngranajesLayout.QuestionH / 2f, EngranajesLayout.W - 16f, EngranajesLayout.QuestionH);
            SetRadius(_questionBg, 14f * _s);
            SetRect(_questionText.rectTransform, EngranajesLayout.W / 2f, _lay.QuestionTop + EngranajesLayout.QuestionH / 2f + 1f, EngranajesLayout.W - 36f, EngranajesLayout.QuestionH);
            ApplyQuestionFont();

            LayoutButtons();
            LayoutSay();

            // tarjeta «NUEVO»
            float cy = _logicalH * 0.48f, cw = EngranajesLayout.W - 44f, ch = 206f;
            SetRect(_introRim.rectTransform, EngranajesLayout.W / 2f, cy, cw + 4f, ch + 4f);
            SetRect(_introCard.rectTransform, EngranajesLayout.W / 2f, cy, cw, ch);
            SetRadius(_introRim, 28f * _s);
            SetRadius(_introCard, 26f * _s);
            SetRect(_introTitle.rectTransform, EngranajesLayout.W / 2f, cy - 74f, 200f, 24f);
            SetRect(_introLine1.rectTransform, EngranajesLayout.W / 2f, cy + 30f, cw - 20f, 26f);
            SetRect(_introLine2.rectTransform, EngranajesLayout.W / 2f, cy + 52f, cw - 20f, 24f);
            SetRect(_introTap.rectTransform, EngranajesLayout.W / 2f, cy + 86f, 200f, 24f);
            LayoutIntroGear(_introA, EngranajesLayout.W / 2f - 24f, cy - 22f, EngranajesSprites.GearSize.Big, EngranajesSprites.Palette.Main);
            LayoutIntroGear(_introB, EngranajesLayout.W / 2f + 26f, cy - 22f, EngranajesSprites.GearSize.Small, EngranajesSprites.Palette.Station);

            // despegue: el texto va sobre la sala vacía (a la izquierda del cohete)
            var lp = L(130f, 170f);
            SetRect(_launchTitle.rectTransform, lp.x, lp.y, 240f, 40f);
            SetRect(_launchLine1.rectTransform, lp.x, L(130f, 204f).y, 240f, 24f);
            SetRect(_launchLine2.rectTransform, lp.x, L(130f, 226f).y, 240f, 24f);
        }

        private void LayoutIntroGear(GearView v, float lx, float ly, EngranajesSprites.GearSize size, EngranajesSprites.Palette palette)
        {
            float tip = EngranajesSprites.TipOf(size), box = EngranajesSprites.GearBox(size);
            v.Tip = tip;
            v.Root.anchoredPosition = P(lx, ly);
            v.Root.sizeDelta = Vector2.zero;
            v.Body.sprite = EngranajesSprites.Gear(size, palette);
            v.Shadow.sprite = EngranajesSprites.Silhouette(size);
            SizeGearParts(v, box, tip, _s);
            v.Root.gameObject.SetActive(false);
        }

        /// <summary>Tamaño de las capas de un engranaje (en unidades; <paramref name="u"/> = unidades por dp) y la sombra corrida 4 dp hacia abajo.</summary>
        private static void SizeGearParts(GearView v, float boxDp, float tip, float u)
        {
            v.Body.rectTransform.sizeDelta = new Vector2(boxDp * u, boxDp * u);
            v.Shadow.rectTransform.sizeDelta = new Vector2(boxDp * u, boxDp * u);
            v.Shadow.rectTransform.anchoredPosition = new Vector2(0f, -4f * u);
            v.Support.rectTransform.sizeDelta = Vector2.one * (tip * u);
            v.Hub.rectTransform.sizeDelta = Vector2.one * (tip * 0.5f * u);
            v.Glow.rectTransform.sizeDelta = Vector2.one * (tip * 3f * u);
        }

        private void LayoutButtons()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                var b = _buttons[i];
                if (i >= _buttonCount) { b.Root.gameObject.SetActive(false); continue; }
                b.Root.gameObject.SetActive(true);
                var rect = EngranajesLayout.ButtonRect(_lay, i, _buttonCount);
                b.Rect = rect;
                float w = rect.width, h = rect.height;
                SetRect(b.Root, rect.center.x, rect.center.y, w, h);
                SetChild(b.Shadow.rectTransform, 0f, 6f, w, h);
                SetChild(b.Rim.rectTransform, 0f, 0f, w + 3f, h + 3f);
                SetChild(b.Bg.rectTransform, 0f, 0f, w, h);
                SetRadius(b.Shadow, 22f * _s);
                SetRadius(b.Rim, 23.5f * _s);
                SetRadius(b.Bg, 22f * _s);
                SetChild(b.Icon.rectTransform, 0f, -h * 0.5f + h * 0.36f, 56f, 56f);
                var l = b.Label;
                l.fontSize = Mathf.RoundToInt((w < 110f ? 15f : 17f) * _s);
                l.rectTransform.sizeDelta = new Vector2((w - 8f) * _s, 40f * _s);
                l.rectTransform.anchoredPosition = new Vector2(0f, -(h * 0.5f - 24f) * _s);
            }
        }

        private void LayoutSay()
        {
            float w = EngranajesLayout.W - 20f, h = _lay.SayH;
            SetRect(_sayRoot, EngranajesLayout.W / 2f, _lay.SayTop + h / 2f, w, h);
            SetChild(_sayRim.rectTransform, 0f, 0f, w + 3f, h + 3f);
            SetChild(_sayBg.rectTransform, 0f, 0f, w, h);
            SetRadius(_sayRim, 15.5f * _s);
            SetRadius(_sayBg, 14f * _s);
            SetChild(_sayTitle.rectTransform, 0f, -h * 0.5f + h * 0.36f, w - 20f, h * 0.56f);
            SetChild(_saySub.rectTransform, 0f, h * 0.5f - h * 0.2f, w - 20f, h * 0.36f);
        }

        /// <summary>Coloca una pieza respecto del centro de su padre (dp; y hacia abajo).</summary>
        private void SetChild(RectTransform r, float dx, float dy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = new Vector2(dx * _s, -dy * _s);
        }

        private void ApplyQuestionFont() => _questionText.fontSize = Mathf.RoundToInt(_questionDp * _s);
    }
}
