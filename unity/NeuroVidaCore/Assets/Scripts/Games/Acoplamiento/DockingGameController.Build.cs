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
        private const int Lights = DockingPlan.Lights, SlotsPerRing = DockingContract.Slots, SculptureRings = DockingPlan.SculptureRings, ConfettiPool = 50, CurveBars = 5, EndRows = 4, ChevronCount = 4;

        /// <summary>El atlas de plasticina de Acoplamiento (los íconos de los botones, los chevrones y las luces de la fila de avance; el resto sale del kit de la app).</summary>
        public const string PlasticGroup = "acoplamiento";

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
        private static readonly Color RingViolet = new Color(140f / 255f, 120f / 255f, 236f / 255f);
        /// <summary>El cordón del hueco: verde agua en reposo, lima al encajar, celeste al «mostrar» (el tubo de plasticina es gris y se tiñe con estos).</summary>
        private static readonly Color PortEdgeIdle = new Color(92f / 255f, 200f / 255f, 194f / 255f);
        private static readonly Color CardFill = new Color(34f / 255f, 32f / 255f, 90f / 255f, 0.97f);

        private RectTransform _safe, _play, _backLayer, _portLayer, _moduleLayer, _buttonLayer, _hudLayer, _fxLayer, _endLayer;
        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();
        private readonly List<KeyValuePair<Image, float>> _radii = new List<KeyValuePair<Image, float>>();

        // la fila de 8 luces de avance (con su halo y la medalla de los anillos) y la zona que ilumina el tutorial al hablar de ella
        private readonly Image[] _lights = new Image[Lights];
        private Image _lightGlow, _medal;
        private Text _medalText;
        private RectTransform _lightsZone, _noticeZone;

        // puerto: el panel (losa «noche» del kit), su rótulo, los chevrones, la cavidad del hueco, el cordón (tinta el estado) y la marca de visto
        private Image _portPanel, _portSlab, _portEdge, _portHole, _portMark;
        private Text _portLabel;
        private readonly Image[] _chevrons = new Image[ChevronCount];

        // módulo: la raíz (posición, tamaño, alfa) y el cuerpo (la pieza horneada: la mostrada, la alineada o la asentada; gira y se da vuelta en la revelación)
        private RectTransform _moduleRoot, _bodyRect;
        private CanvasGroup _moduleGroup;
        private Image _moduleBody;

        // botones: la losa del kit (lima / uva) con su ícono y su texto encima
        private sealed class BtnView
        {
            public RectTransform Root, Content;
            public Image Bg, Slab, Icon, Badge;
            public Text Label;
            public CanvasGroup Group;
            public PlasticTouch Touch;
            public SlabTone Tone;
        }

        private readonly BtnView[] _buttons = new BtnView[2];

        // barra de tiempo, pista y avisos (la losa chica del kit)
        private Image _fuelBorder, _fuelTrack, _fuelFill, _noticeShadow, _noticeBorder, _noticePill, _noticeSlab;
        private Text _fuelLabel, _hint, _noticeText;

        // pantalla final
        private Image _endShade, _endCard, _endRecordBg;
        private Text _endTag, _endTitle, _endCurveTitle, _endNote1, _endNote2, _endRecord;
        private readonly Text[] _endLabel = new Text[EndRows], _endValue = new Text[EndRows], _curveValue = new Text[CurveBars], _curveAngle = new Text[CurveBars];
        private readonly Image[] _curveBarInk = new Image[CurveBars], _curveBar = new Image[CurveBars], _confetti = new Image[ConfettiPool];

        // la escultura de «tu estación» (pantalla final): el núcleo, las pistas de los anillos (atrás y adelante) y las piezas acopladas (3 anillos de 8)
        private RectTransform _sculptBack, _sculptFront;
        private Image _sculptCore;
        private readonly Image[] _trackBack = new Image[SculptureRings], _trackFront = new Image[SculptureRings];
        private readonly Image[,] _sculptPieces = new Image[SculptureRings, SlotsPerRing];

        private static Sprite Art(string name) => PlasticArt.Get(PlasticGroup, name);

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

            _hud = new GameHud(_safe, DockingContract.Title, MarginU, this);                 // el marcador común ENTERO (Tarea 144): el contador va en el chip de avance y «×N seguidos» es el chip común

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _backLayer = Layer(_play, "Back");
            _portLayer = Layer(_play, "Port");
            _moduleLayer = Layer(_play, "Module");
            _buttonLayer = Layer(_play, "Buttons");
            _hudLayer = Layer(_play, "HudExtra");
            _fxLayer = Layer(_play, "Fx");
            _endLayer = Layer(_play, "End");
            BuildLights();
            BuildPort();
            BuildModule();
            BuildButtons();
            BuildFuelAndNotice();
            BuildHud();
            BuildEnd();
            BuildHiddenResult();

            _exit = new ExitButton(_safe, this, UnitsPerDp, GameId);

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

        /// <summary>La fila de 8 luces redondas de plasticina junto al marcador (el avance: cada módulo acoplado enciende una; cada 8, un anillo) y la medalla de los anillos a su derecha.</summary>
        private void BuildLights()
        {
            var zone = new GameObject("LightsZone");
            zone.transform.SetParent(_hudLayer, false);
            _lightsZone = zone.AddComponent<RectTransform>();
            _lightsZone.anchorMin = _lightsZone.anchorMax = _lightsZone.pivot = new Vector2(0.5f, 0.5f);
            var noticeZone = new GameObject("NoticeZone");
            noticeZone.transform.SetParent(_hudLayer, false);
            _noticeZone = noticeZone.AddComponent<RectTransform>();
            _noticeZone.anchorMin = _noticeZone.anchorMax = _noticeZone.pivot = new Vector2(0.5f, 0.5f);
            _hud.AvoidForStreak(_noticeZone);                               // la franja del aviso («¡Anillo completo!» aparece con el 8.º módulo, a la vez que el chip, y entra con un fundido): el chip tampoco la cruza
            _hud.AvoidForStreak(_lightsZone);                               // Tarea 144: las luces de avance miden menos de 80 unidades y la guardia no las ve como estímulo: el chip «×N seguidos» no las cruza nunca
            for (int i = 0; i < Lights; i++) _lights[i] = MakeImage(_hudLayer, "Light" + i, null);
            _lightGlow = MakeImage(_hudLayer, "LightGlow", RadialGlowSprite.Get());
            _lightGlow.color = new Color(1f, 0.82f, 0.34f, 0f);
            _lightGlow.gameObject.SetActive(false);
            _medal = MakeImage(_hudLayer, "Medal", null);
            _medalText = MakeLabel(_hudLayer, "MedalText", 16f, UiFonts.Bold, Ink, TextAnchor.MiddleCenter);
            _medalText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _medal.gameObject.SetActive(false);
            _medalText.gameObject.SetActive(false);
        }

        private void BuildPort()
        {
            _portPanel = MakeImage(_portLayer, "PortPanel", null);
            _portSlab = PlasticSlab.TryStyle(_portPanel, SlabSize.Button, SlabTone.Night);          // el panel «Puerto»: la losa «noche» del kit (la de las casillas de Rescate)
            _portLabel = MakeLabel(_portLayer, "PortLabel", 16f, UiFonts.Bold, new Color(214f / 255f, 209f / 255f, 242f / 255f), TextAnchor.MiddleCenter);
            _portLabel.text = DockingContract.PortLabel;
            _portLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            for (int i = 0; i < ChevronCount; i++) _chevrons[i] = MakeImage(_portLayer, "Chevron" + i, null);
            _portHole = MakeImage(_portLayer, "PortHole", null);                                  // la cavidad (fondo oscuro y pared iluminada)
            _portEdge = MakeImage(_portLayer, "PortEdge", null);                                  // el cordón de plasticina (gris: se tiñe con el estado)
            _portMark = MakeImage(_portLayer, "PortMark", null);                                  // el visto lima del kit al encajar
            _portEdge.gameObject.SetActive(false);
            _portHole.gameObject.SetActive(false);
            _portMark.gameObject.SetActive(false);
        }

        private void BuildModule()
        {
            var root = new GameObject("ModuleRoot", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(_moduleLayer, false);
            _moduleRoot = (RectTransform)root.transform;
            _moduleRoot.anchorMin = _moduleRoot.anchorMax = _moduleRoot.pivot = new Vector2(0.5f, 0.5f);
            _moduleGroup = root.GetComponent<CanvasGroup>();
            _moduleGroup.blocksRaycasts = false;
            _moduleBody = MakeImage(_moduleRoot, "Body", null);                                   // la pieza ya trae su canto, su sombra y su lámpara horneados (DockingArt)
            _bodyRect = _moduleBody.rectTransform;
            _moduleRoot.gameObject.SetActive(false);
        }

        private void BuildButtons()
        {
            _buttons[0] = BuildButton("Fits", DockingContract.FitsLabel, SlabTone.Lime);
            _buttons[1] = BuildButton("Mirror", DockingContract.MirrorLabel, SlabTone.Lilac);
        }

        private BtnView BuildButton(string name, string label, SlabTone tone)
        {
            var v = new BtnView { Tone = tone };
            var go = new GameObject(name);
            go.transform.SetParent(_buttonLayer, false);
            v.Root = go.AddComponent<RectTransform>();
            v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
            v.Group = go.AddComponent<CanvasGroup>();
            v.Group.blocksRaycasts = false;
            v.Bg = MakeImage(v.Root, "Bg", null);                                                // el respaldo de arcilla (sin el kit)
            v.Slab = PlasticSlab.Attach(v.Root, SlabSize.Button, tone);                           // «Encaja» y «Espejo»: la losa lima y la uva del kit TAL CUAL, como VERDAD y DISPARATE
            if (v.Slab != null)
            {
                v.Bg.enabled = false;
                v.Touch = PlasticTouch.On(go, SlabSize.Button, tone);                              // el estado presionado y el rebote
            }
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(v.Root, false);
            v.Content = (RectTransform)content.transform;
            v.Content.anchorMin = v.Content.anchorMax = v.Content.pivot = new Vector2(0.5f, 0.5f);
            v.Content.sizeDelta = Vector2.zero;
            v.Icon = MakeImage(v.Content, "Icon", null);
            v.Label = MakeLabel(v.Content, "Label", 22f, UiFonts.Bold, Ink, TextAnchor.MiddleLeft);
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
            _fuelLabel = MakeLabel(_hudLayer, "FuelLabel", 16f, UiFonts.Bold, Soft, TextAnchor.MiddleRight);
            _fuelLabel.text = DockingContract.TimeLabel;
            _fuelLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _hint = MakeLabel(_hudLayer, "Hint", 16f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _hint.text = DockingContract.Hint;
            _hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            // el aviso: la losa chica del kit (dorada si acertó, crema si no) con el texto liso de tinta, en la franja de y 118-150 (sin el kit, la píldora oscura de siempre)
            _noticeShadow = Rr(_hudLayer, "NoticeShadow", Shadow, 16f);
            _noticeBorder = Rr(_hudLayer, "NoticeBorder", Ink, 19f);
            _noticePill = Rr(_hudLayer, "NoticePill", NoticeFill, 16f);
            _noticeSlab = PlasticSlab.Available(SlabSize.Chip, SlabTone.Sun) && PlasticSlab.Available(SlabSize.Chip, SlabTone.Cream) ? PlasticSlab.Attach(_noticePill.rectTransform, SlabSize.Chip, SlabTone.Sun) : null;
            if (_noticeSlab != null)
            {
                _noticePill.enabled = false;
                _noticeShadow.enabled = false;
                _noticeBorder.enabled = false;
            }
            _noticeText = MakeLabel(_hudLayer, "NoticeText", 17f, UiFonts.Bold, Lime, TextAnchor.MiddleCenter);
            _noticeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            SetNoticeVisible(false);
        }

        private void BuildHud()
        {
            // la barra de tiempo del Reto (los 120 s), fina y bajo el marcador
            _timerTrack = Rr(_hudLayer, "TimerTrack", new Color(Lavender.r, Lavender.g, Lavender.b, 0.25f), 2.5f);
            _timerFill = Rr(_hudLayer, "TimerFill", Lime, 2.5f);
            _timerFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        private void BuildEnd()
        {
            _endShade = MakeImage(_endLayer, "Shade", null);
            _endShade.color = new Color(4f / 255f, 5f / 255f, 26f / 255f, 0.55f);
            BuildSculpture();
            _endTag = MakeLabel(_endLayer, "Tag", 16f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _endTitle = MakeLabel(_endLayer, "Title", 26f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _endCard = Rr(_endLayer, "Card", CardFill, 22f);
            string[] labels = { DockingContract.EndHits, DockingContract.EndSpeed, DockingContract.EndRings, DockingContract.EndStreak };
            for (int i = 0; i < EndRows; i++)
            {
                _endLabel[i] = MakeLabel(_endLayer, "Label" + i, 16f, UiFonts.Regular, Soft, TextAnchor.MiddleLeft);
                _endValue[i] = MakeLabel(_endLayer, "Value" + i, 17f, UiFonts.Bold, Color.white, TextAnchor.MiddleRight);
                _endLabel[i].horizontalOverflow = _endValue[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                _endLabel[i].text = labels[i];
            }
            _endCurveTitle = MakeLabel(_endLayer, "CurveTitle", 16f, UiFonts.Bold, Soft, TextAnchor.MiddleCenter);
            _endCurveTitle.text = DockingContract.EndCurve;
            for (int i = 0; i < CurveBars; i++)
            {
                _curveBarInk[i] = Rr(_endLayer, "BarInk" + i, Ink, 10f);
                _curveBar[i] = Rr(_endLayer, "Bar" + i, DockingSprites.GrapeColor, 8f);
                _curveValue[i] = MakeLabel(_endLayer, "BarValue" + i, 16f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
                _curveAngle[i] = MakeLabel(_endLayer, "BarAngle" + i, 16f, UiFonts.Bold, Soft, TextAnchor.MiddleCenter);
                _curveValue[i].horizontalOverflow = _curveAngle[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                _curveAngle[i].text = DockingContract.CurveBins[i] + "°";
            }
            _endNote1 = MakeLabel(_endLayer, "Note1", 16f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _endNote2 = MakeLabel(_endLayer, "Note2", 16f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _endNote1.text = DockingContract.EndNote1;
            _endNote2.text = DockingContract.EndNote2;
            _endNote1.horizontalOverflow = _endNote2.horizontalOverflow = HorizontalWrapMode.Wrap;
            _endRecordBg = Rr(_endLayer, "RecordBg", Gold, 15f);
            _endRecord = MakeLabel(_endLayer, "Record", 16f, UiFonts.Bold, Ink, TextAnchor.MiddleCenter);
            _endTag.text = DockingContract.EndTag;
            foreach (var t in new[] { _endTag, _endTitle, _endCurveTitle, _endRecord }) t.horizontalOverflow = HorizontalWrapMode.Wrap;
            for (int i = 0; i < ConfettiPool; i++)
            {
                _confetti[i] = MakeImage(_endLayer, "Confetti" + i, DiscSprite.Get());
                _confetti[i].gameObject.SetActive(false);
            }
            _endLayer.gameObject.SetActive(false);
        }

        /// <summary>«Tu estación» como una escultura de plasticina: un núcleo y, a su alrededor, hasta 3 anillos de 8 piezas (las que acopló la persona); atrás del núcleo van las pistas y las piezas de atrás, adelante las demás. Se arma al final (<see cref="ShowSculpture"/>).</summary>
        private void BuildSculpture()
        {
            _sculptBack = Layer(_endLayer, "SculptureBack");
            _sculptCore = MakeImage(_endLayer, "SculptureCore", null);
            _sculptFront = Layer(_endLayer, "SculptureFront");
            for (int r = 0; r < SculptureRings; r++)
            {
                _trackBack[r] = MakeImage(_sculptBack, "TrackBack" + r, null);
                _trackFront[r] = MakeImage(_sculptFront, "TrackFront" + r, null);
            }
            for (int r = 0; r < SculptureRings; r++)
                for (int k = 0; k < SlotsPerRing; k++)
                    _sculptPieces[r, k] = MakeImage(DockingPlan.IsFront(k) ? _sculptFront : _sculptBack, "Piece" + r + "_" + k, null);
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
                if (kv.Key == _buttons[0].Label || kv.Key == _buttons[1].Label) dp = Mathf.Max(16f, dp * _plan.K);          // el texto de los botones se achica con ellos, pero nunca bajo 16 dp (Tarea 129; antes 14)
                int px = Mathf.RoundToInt(dp * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            foreach (var kv in _radii) SetRadius(kv.Key, kv.Value * _s);
            AssignSprites();
            LayoutLights();
            LayoutPort();
            LayoutModule();
            LayoutButtons();
            LayoutFuelAndNotice();
            LayoutHud();
            LayoutEnd();
            RefreshLights(Now);
            ApplyModulePose();
        }

        /// <summary>Pone los sprites fijos: los del atlas de plasticina y, si no está, los de arcilla de siempre (la pieza y el hueco se ponen en cada módulo).</summary>
        private void AssignSprites()
        {
            if (!_baked) return;
            if (_portSlab == null) _portPanel.sprite = DockingSprites.PortPanel();
            for (int i = 0; i < 2; i++)
            {
                var v = _buttons[i];
                if (v.Slab == null) v.Bg.sprite = i == 0 ? DockingSprites.FitButton() : DockingSprites.MirrorButton();
                v.Icon.sprite = Art(i == 0 ? "ico_encaja" : "ico_espejo") ?? (i == 0 ? DockingSprites.FitIcon() : DockingSprites.MirrorIcon());
            }
            if (_noticeSlab == null) { _noticePill.sprite = _noticeBorder.sprite = _noticeShadow.sprite = RoundedRectSprite.Get(24); }
            _medal.sprite = Art("luz_lima") ?? DiscSprite.Get();
        }

        /// <summary>El sprite de un estado de una luz de la fila (apagada, oro, lima): el del atlas o, sin él, un disco que se tiñe (devuelve también si hay que teñirlo).</summary>
        private static Sprite LightSprite(int state, out Color tint)
        {
            string name = state == 2 ? "luz_lima" : state == 1 ? "luz_oro" : "luz_apagada";
            var s = Art(name);
            if (s != null) { tint = Color.white; return s; }
            tint = state == 2 ? Lime : state == 1 ? Gold : new Color(42f / 255f, 37f / 255f, 102f / 255f);
            return DiscSprite.Get();
        }

        private void LayoutLights()
        {
            bool art = Art("luz_oro") != null;
            for (int i = 0; i < Lights; i++) SetRect(_lights[i].rectTransform, DockingPlan.LightX(i), _plan.LightsY, art ? DockingPlan.LightSprite : DockingPlan.LightSprite * 0.73f, art ? DockingPlan.LightSprite : DockingPlan.LightSprite * 0.73f);
            SetRect(_medal.rectTransform, DockingPlan.Width * 0.5f + DockingPlan.MedalDx, _plan.LightsY, art ? DockingPlan.MedalSprite : DockingPlan.MedalSprite * 0.73f, art ? DockingPlan.MedalSprite : DockingPlan.MedalSprite * 0.73f);
            SetRect(_medalText.rectTransform, DockingPlan.Width * 0.5f + DockingPlan.MedalDx, _plan.LightsY - 0.5f, 34f, 22f);
            SetRect(_lightsZone, DockingPlan.Width * 0.5f + (DockingPlan.MedalDx + DockingPlan.MedalSprite * 0.5f - 4f * DockingPlan.LightsStep) * 0.5f, _plan.LightsY, 4f * DockingPlan.LightsStep + DockingPlan.MedalDx + DockingPlan.MedalSprite * 0.5f + 10f, 36f);                // lo que ilumina el tutorial al hablar de la fila (las 8 luces y la medalla)
        }

        private void LayoutPort()
        {
            float cx = _plan.PortCx, cy = _plan.PortCy, k = _plan.K, fb = _plan.Block / 26f;
            SetRect(_portPanel.rectTransform, cx, cy, _portSlab != null ? _plan.PortW : DockingSprites.PanelSide * fb, _portSlab != null ? _plan.PortH : DockingSprites.PanelSide * fb);
            SetRect(_portLabel.rectTransform, cx, cy - _plan.PortH * 0.5f + 17f * k, 110f, 22f);
            float chev = 24f * k;
            for (int i = 0; i < ChevronCount; i++) SetRect(_chevrons[i].rectTransform, cx + (i - 1.5f) * 26f * k, cy + _plan.PortH * 0.5f - 15f * k, chev, chev);
            float side = DockingArt.AlignedFrameBlocks * _plan.Block;
            SetRect(_portHole.rectTransform, cx, _plan.HoleCy, side, side);
            SetRect(_portEdge.rectTransform, cx, _plan.HoleCy, side, side);
            float mk = 26f * k;
            SetRect(_portMark.rectTransform, cx + _plan.PortW * 0.5f - 22f * k, cy - _plan.PortH * 0.5f + 22f * k, mk, mk);
        }

        private void LayoutModule()
        {
            _moduleRoot.sizeDelta = Vector2.zero;
            float side = (_poseAligned ? DockingArt.AlignedFrameBlocks : _dispSideBlocks) * _plan.Block;
            _bodyRect.sizeDelta = new Vector2(side * _s, side * _s);
        }

        /// <summary>El radio de la circunferencia que envuelve al módulo en curso (dp, a la escala de la pantalla): lo usa la zona que ilumina el tutorial.</summary>
        private float ModuleRadius()
        {
            if (_trial == null) return 2.83f * _plan.Block;
            DockingArt.PieceCenter(_trial.Shape, out _, out _, out int w, out int h);
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
                float ico = 54f * _plan.K;
                SetChild(v.Icon.rectTransform, -_plan.ButtonW * 0.5f + 2f * _plan.K + ico * 0.5f, 0f, ico, ico);
                SetChild(v.Label.rectTransform, -_plan.ButtonW * 0.5f + 62f * _plan.K + 45f, 1f, 90f, 30f);
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
            SetRect(_timerTrack.rectTransform, DockingPlan.Width * 0.5f, 71f, DockingPlan.Width - 24f, 5f);
            SetRect(_noticeZone, DockingPlan.Width * 0.5f, _plan.StripCy, DockingPlan.Width - 56f, 38f);
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
            LayoutSculpture();
        }

        /// <summary>Lo que ocupa, abajo, el rótulo «Práctica: no cuenta» y «Saltar tutorial» (dp).</summary>
        private const float TutorialControlsDp = DockingPlan.TutorialControls;
    }
}
