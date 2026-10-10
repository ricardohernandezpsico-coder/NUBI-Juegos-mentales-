using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Radar
{
    /// <summary>El rayo tractor: un cono de luz de la cápsula a la nave (más claro en la nave). Los cuatro puntos van en unidades del lienzo, relativos al centro del rectángulo.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RadarBeamGraphic : MaskableGraphic
    {
        private Vector2 _a0, _a1, _b1, _b0;
        private Color _near, _far;

        public void Set(Vector2 a0, Vector2 a1, Vector2 b1, Vector2 b0, Color near, Color far)
        {
            _a0 = a0; _a1 = a1; _b1 = b1; _b0 = b0;
            _near = near; _far = far;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            vh.AddVert(_a0, _near, Vector2.zero);
            vh.AddVert(_a1, _near, Vector2.zero);
            vh.AddVert(_b1, _far, Vector2.zero);
            vh.AddVert(_b0, _far, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }
    }

    public sealed partial class RadarGameController
    {
        private const int CapsulePool = 6, RockPool = 2, EchoPool = 6, DustPool = 30, DebrisPool = 5, SmokePool = 4, NebulaPool = 4, WindowCount = 10, BeamPool = 4, SparksPerBeam = 5, EndCapsPool = 48, ConfettiPool = 60, TripLines = 7, DockSparks = 6, EndRows = 5;

        private static readonly Color Gold = new Color(255f / 255f, 201f / 255f, 74f / 255f);
        private static readonly Color Cyan = new Color(127f / 255f, 216f / 255f, 255f / 255f);
        private static readonly Color Lime = new Color(166f / 255f, 227f / 255f, 107f / 255f);
        private static readonly Color Coral = new Color(255f / 255f, 138f / 255f, 107f / 255f);
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Dim = new Color(143f / 255f, 138f / 255f, 192f / 255f);
        private static readonly Color Soft = new Color(214f / 255f, 209f / 255f, 242f / 255f);
        private static readonly Color TextColor = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color Ink = new Color(26f / 255f, 18f / 255f, 64f / 255f);
        private static readonly Color PanelFill = new Color(31f / 255f, 27f / 255f, 78f / 255f);
        private static readonly Color ButtonLive = new Color(42f / 255f, 37f / 255f, 102f / 255f);
        private static readonly Color ButtonDim = new Color(28f / 255f, 26f / 255f, 72f / 255f);
        private static readonly Color ButtonHit = new Color(61f / 255f, 106f / 255f, 44f / 255f);
        private static readonly Color ButtonWrong = new Color(107f / 255f, 46f / 255f, 58f / 255f);
        private static readonly Color WindowEmpty = new Color(197f / 255f, 192f / 255f, 230f / 255f);

        /// <summary>Un botón del tablero: su raíz (el área de toque y el hueco del tutorial), el fondo de arcilla, el aro, la forma, el nombre y la marca de la revelación.</summary>
        private sealed class BtnView
        {
            public RectTransform Root;
            public Image Bg, Ring, Emblem, Badge;
            public Text Label;
            public CanvasGroup Group;
        }

        private RectTransform _safe, _play, _backLayer, _radarLayer, _shipLayer, _fxLayer, _boardLayer, _hudLayer, _endLayer, _radarZone, _shipZone;
        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();
        private readonly List<KeyValuePair<Image, float>> _radii = new List<KeyValuePair<Image, float>>();

        // fondo (v4): nebulosas, planeta lejano, la estación accidentada (baliza que parpadea y humo), restos que flotan y polvo
        private Image _planet, _station, _stationGlow, _stationDot;
        private readonly Image[] _nebulae = new Image[NebulaPool], _smoke = new Image[SmokePool], _debris = new Image[DebrisPool], _dust = new Image[DustPool];
        private readonly Vector2[] _dustPos = new Vector2[DustPool], _debrisPos = new Vector2[DebrisPool];
        private readonly float[] _dustSpeed = new float[DustPool], _dustSize = new float[DustPool];
        private readonly float[] _debrisSpeed = new float[DebrisPool], _debrisAngle = new float[DebrisPool], _debrisSpin = new float[DebrisPool], _debrisSize = new float[DebrisPool];

        // radar
        private Image _scope, _sweep, _beamLine, _sweepHead, _mask, _axis, _flashFill, _flashGlow, _flashWave;
        private readonly Image[] _echoes = new Image[EchoPool];
        private readonly float[] _echoAt = new float[EchoPool], _echoAngle = new float[EchoPool], _echoRadius = new float[EchoPool];
        private readonly Image[] _capImgs = new Image[CapsulePool], _capRings = new Image[CapsulePool], _rockImgs = new Image[RockPool];

        // nave protagonista, rayos y cápsulas que vuelan (la nave es un solo objeto centrado en sí mismo: salta y viaja entera)
        private RectTransform _shipRoot;
        private Image _shipHull, _dockRing;
        private Text _shipCount;
        private readonly Image[] _windows = new Image[WindowCount], _flames = new Image[2], _flying = new Image[BeamPool], _sparks = new Image[BeamPool * SparksPerBeam], _dockSparks = new Image[DockSparks], _tripLines = new Image[TripLines];
        private readonly RadarBeamGraphic[] _beams = new RadarBeamGraphic[BeamPool];

        // tablero
        private readonly BtnView[] _buttons = new BtnView[RadarContract.TypeCount];
        private readonly BtnView _go = new BtnView();

        // marcador de la derecha y franja de mensajes
        private Image _hudIcon, _streakBg, _msgPill, _timerTrack, _timerFill;
        private Text _hudCount, _streakText, _msgA, _msgB;

        // pantalla final
        private Image _endCard, _endRecordBg;
        private readonly Image[] _endCaps = new Image[EndCapsPool], _confetti = new Image[ConfettiPool];
        private Text _endTag, _endTitle, _endNote1, _endNote2, _endNote3, _endRecord;
        private readonly Text[] _endLabel = new Text[EndRows], _endValue = new Text[EndRows];

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("RadarCanvas");
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
            var world = GameWorld.RadarStation;                                         // el cielo base; las nebulosas de la v4 las pone el juego (BuildBack)
            world.NebulaA = Color.clear;
            world.NebulaB = Color.clear;
            WorldBackdrop.Build(bgRect, world);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, RadarContract.Title, MarginU, this, withStreak: false, rightReserve: 170f);

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _backLayer = Layer(_play, "Back");
            _radarLayer = Layer(_play, "Radar");
            _shipLayer = Layer(_play, "Ship");
            _fxLayer = Layer(_play, "Fx");
            _boardLayer = Layer(_play, "Board");
            _hudLayer = Layer(_play, "HudExtra");
            _endLayer = Layer(_play, "End");
            BuildBack();
            BuildRadar();
            BuildShip();
            BuildFx();
            BuildBoard();
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

        private void BuildBack()
        {
            // nebulosas fijas (degradados radiales): verde agua abajo a la izquierda, uva arriba a la derecha, azul al centro y coral abajo a la derecha
            var neb = new[] { new Color(60f / 255f, 200f / 255f, 190f / 255f, 0.13f), new Color(183f / 255f, 100f / 255f, 1f, 0.15f), new Color(80f / 255f, 120f / 255f, 1f, 0.07f), new Color(1f, 138f / 255f, 107f / 255f, 0.06f) };
            for (int i = 0; i < NebulaPool; i++) { _nebulae[i] = MakeImage(_backLayer, "Nebula" + i, RadialGlowSprite.Get()); _nebulae[i].color = neb[i]; }
            _planet = MakeImage(_backLayer, "Planet", null);
            _planet.color = new Color(1f, 1f, 1f, 0.7f);
            _station = MakeImage(_backLayer, "Station", null);
            _stationGlow = MakeImage(_backLayer, "StationGlow", RadialGlowSprite.Get());
            _stationGlow.color = new Color(1f, 138f / 255f, 107f / 255f, 0.8f);
            _stationDot = MakeImage(_backLayer, "StationLight", DiscSprite.Get());
            for (int i = 0; i < SmokePool; i++) { _smoke[i] = MakeImage(_backLayer, "Smoke" + i, DiscSprite.Get()); _smoke[i].color = new Color(170f / 255f, 165f / 255f, 210f / 255f, 0f); }
            // los restos y el polvo salen de las mismas semillas del boceto (rng 9 y rng 5)
            var rd = new SoundKit.Rng(9);
            for (int i = 0; i < DebrisPool; i++)
            {
                _debris[i] = MakeImage(_backLayer, "Debris" + i, null);
                _debrisPos[i] = new Vector2((float)rd.Next() * RadarPlan.Width, 120f + (float)rd.Next() * (RadarPlan.TallHeight - 300f));
                _debrisSpeed[i] = 3f + (float)rd.Next() * 5f;
                _debrisAngle[i] = (float)rd.Next() * 6f;
                _debrisSpin[i] = ((float)rd.Next() - 0.5f) * 0.6f;
                _debrisSize[i] = 4f + (float)rd.Next() * 5f;
            }
            var rs = new SoundKit.Rng(5);
            for (int i = 0; i < DustPool; i++)
            {
                _dust[i] = MakeImage(_backLayer, "Dust" + i, DiscSprite.Get());
                _dust[i].color = new Color(214f / 255f, 209f / 255f, 242f / 255f, 0.35f);
                _dustPos[i] = new Vector2((float)rs.Next() * RadarPlan.Width, (float)rs.Next() * RadarPlan.TallHeight);
                _dustSpeed[i] = 4f + (float)rs.Next() * 8f;
                _dustSize[i] = (0.6f + (float)rs.Next() * 1.4f) * 2f;
            }
        }

        private void BuildRadar()
        {
            var zone = new GameObject("RadarZone");
            zone.transform.SetParent(_radarLayer, false);
            _radarZone = zone.AddComponent<RectTransform>();
            _radarZone.anchorMin = _radarZone.anchorMax = _radarZone.pivot = new Vector2(0.5f, 0.5f);
            _scope = MakeImage(_radarLayer, "Scope", null);
            for (int i = 0; i < EchoPool; i++)
            {
                _echoes[i] = MakeImage(_radarLayer, "Echo" + i, DiscSprite.Get());
                _echoes[i].color = new Color(Cyan.r, Cyan.g, Cyan.b, 0f);
                _echoes[i].gameObject.SetActive(false);
            }
            _sweep = MakeImage(_radarLayer, "Sweep", null);
            _sweep.color = new Color(120f / 255f, 240f / 255f, 220f / 255f, 1f);                       // la estela (el sprite lleva el 40 %)
            _beamLine = MakeImage(_radarLayer, "BeamLine", null);
            _beamLine.rectTransform.pivot = new Vector2(0f, 0.5f);
            _beamLine.color = new Color(205f / 255f, 1f, 240f / 255f, 0.95f);                          // la línea del haz
            _sweepHead = MakeImage(_radarLayer, "SweepHead", RadialGlowSprite.Get());
            _sweepHead.color = new Color(170f / 255f, 1f, 235f / 255f, 0.9f);
            _flashFill = MakeImage(_radarLayer, "FlashFill", DiscSprite.Get());
            _flashFill.color = new Color(190f / 255f, 1f, 240f / 255f, 0f);
            _flashGlow = MakeImage(_radarLayer, "FlashGlow", RadialGlowSprite.Get());
            _flashGlow.color = new Color(225f / 255f, 245f / 255f, 1f, 0f);
            _flashWave = MakeImage(_radarLayer, "FlashWave", RingSprite.Get());
            _flashWave.color = new Color(225f / 255f, 245f / 255f, 1f, 0f);
            _axis = MakeImage(_radarLayer, "Axis", null);
            for (int i = 0; i < RockPool; i++) { _rockImgs[i] = MakeImage(_radarLayer, "Rock" + i, null); _rockImgs[i].gameObject.SetActive(false); }
            for (int i = 0; i < CapsulePool; i++)
            {
                _capRings[i] = MakeImage(_radarLayer, "CapRing" + i, null);
                _capRings[i].color = Coral;
                _capRings[i].gameObject.SetActive(false);
                _capImgs[i] = MakeImage(_radarLayer, "Capsule" + i, null);
                _capImgs[i].gameObject.SetActive(false);
            }
            _mask = MakeImage(_radarLayer, "Mask", null);
            _mask.gameObject.SetActive(false);
        }

        private void BuildShip()
        {
            var zone = new GameObject("ShipZone");
            zone.transform.SetParent(_shipLayer, false);
            _shipZone = zone.AddComponent<RectTransform>();
            _shipZone.anchorMin = _shipZone.anchorMax = _shipZone.pivot = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < TripLines; i++)
            {
                _tripLines[i] = MakeImage(_shipLayer, "TripLine" + i, null);
                _tripLines[i].color = new Color(205f / 255f, 240f / 255f, 1f, 0f);
                _tripLines[i].gameObject.SetActive(false);
            }
            var root = new GameObject("ShipRoot");
            root.transform.SetParent(_shipLayer, false);
            _shipRoot = root.AddComponent<RectTransform>();
            _shipRoot.anchorMin = _shipRoot.anchorMax = _shipRoot.pivot = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < _flames.Length; i++)
            {
                _flames[i] = MakeImage(_shipRoot, "Flame" + i, RadialGlowSprite.Get());
                _flames[i].color = new Color(1f, 170f / 255f, 90f / 255f, 0.85f);
            }
            _shipHull = MakeImage(_shipRoot, "Hull", null);
            for (int i = 0; i < WindowCount; i++) _windows[i] = MakeImage(_shipRoot, "Window" + i, null);
            _dockRing = MakeImage(_shipRoot, "DockRing", RingSprite.Get());
            _dockRing.color = new Color(1f, 1f, 1f, 0f);
            for (int i = 0; i < DockSparks; i++)
            {
                _dockSparks[i] = MakeImage(_shipRoot, "DockSpark" + i, DiscSprite.Get());
                _dockSparks[i].color = new Color(1f, 236f / 255f, 160f / 255f, 0f);
            }
            _shipCount = MakeLabel(_shipLayer, "ShipCount", 14f, UiFonts.Bold, new Color(214f / 255f, 209f / 255f, 242f / 255f), TextAnchor.MiddleCenter);
            _shipCount.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void BuildFx()
        {
            for (int i = 0; i < BeamPool; i++)
            {
                var go = new GameObject("Beam" + i, typeof(RectTransform), typeof(CanvasRenderer));
                go.transform.SetParent(_fxLayer, false);
                var r = (RectTransform)go.transform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = Vector2.zero;
                _beams[i] = go.AddComponent<RadarBeamGraphic>();
                _beams[i].raycastTarget = false;
                go.SetActive(false);
                _flying[i] = MakeImage(_fxLayer, "Flying" + i, null);
                _flying[i].gameObject.SetActive(false);
                for (int k = 0; k < SparksPerBeam; k++)
                {
                    var sp = MakeImage(_fxLayer, "Spark" + i + "_" + k, DiscSprite.Get());
                    sp.color = new Color(232f / 255f, 1f, 208f / 255f, 0.9f);
                    sp.gameObject.SetActive(false);
                    _sparks[i * SparksPerBeam + k] = sp;
                }
            }
        }

        private void BuildBoard()
        {
            for (int i = 0; i < _buttons.Length; i++) _buttons[i] = BuildButton(_boardLayer, "Cell" + i, RadarContract.TypeNames[i], i);
            BuildGo();
        }

        private BtnView BuildButton(Transform parent, string name, string label, int type)
        {
            var v = new BtnView();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            v.Root = go.AddComponent<RectTransform>();
            v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
            v.Group = go.AddComponent<CanvasGroup>();
            v.Group.blocksRaycasts = false;
            v.Bg = MakeImage(v.Root, "Bg", null);
            v.Ring = MakeImage(v.Root, "Ring", null);
            v.Ring.gameObject.SetActive(false);
            v.Emblem = MakeImage(v.Root, "Emblem", null);
            v.Label = MakeLabel(v.Root, "Label", 14f, UiFonts.Bold, new Color(241f / 255f, 238f / 255f, 1f), TextAnchor.MiddleCenter);
            v.Label.text = label;
            v.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
            v.Badge = MakeImage(v.Root, "Badge", null);
            v.Badge.gameObject.SetActive(false);
            return v;
        }

        private void BuildGo()
        {
            var go = new GameObject("Go");
            go.transform.SetParent(_boardLayer, false);
            _go.Root = go.AddComponent<RectTransform>();
            _go.Root.anchorMin = _go.Root.anchorMax = _go.Root.pivot = new Vector2(0.5f, 0.5f);
            _go.Bg = MakeImage(_go.Root, "Bg", null);
            _go.Label = MakeLabel(_go.Root, "Label", 20f, UiFonts.Bold, Ink, TextAnchor.MiddleCenter);
            _go.Label.text = RadarContract.RescueLabel;
            _go.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void BuildHud()
        {
            _hudIcon = MakeImage(_hudLayer, "RescuedIcon", null);
            _hudCount = MakeLabel(_hudLayer, "RescuedCount", 22f, UiFonts.Bold, TextColor, TextAnchor.MiddleRight);
            _hudCount.horizontalOverflow = HorizontalWrapMode.Overflow;
            _streakBg = Rr(_hudLayer, "StreakBg", Gold, 13f);
            _streakText = MakeLabel(_hudLayer, "StreakText", 14f, UiFonts.Bold, Ink, TextAnchor.MiddleCenter);
            _streakText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _timerTrack = Rr(_hudLayer, "TimerTrack", new Color(Lavender.r, Lavender.g, Lavender.b, 0.25f), 2.5f);
            _timerFill = Rr(_hudLayer, "TimerFill", Lime, 2.5f);
            _timerFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _msgPill = Rr(_hudLayer, "MessagePill", new Color(PanelFill.r, PanelFill.g, PanelFill.b, 0.96f), 17f);
            _msgA = MakeLabel(_hudLayer, "MessageA", 18f, UiFonts.Bold, TextColor, TextAnchor.MiddleCenter);
            _msgB = MakeLabel(_hudLayer, "MessageB", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _msgA.horizontalOverflow = _msgB.horizontalOverflow = HorizontalWrapMode.Wrap;
            BestFit(_msgA, 14 * (int)UnitsPerDp);
            _msgPill.gameObject.SetActive(false);
        }

        private void BuildEnd()
        {
            _endTag = MakeLabel(_endLayer, "Tag", 15f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _endTitle = MakeLabel(_endLayer, "Title", 27f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            for (int i = 0; i < EndCapsPool; i++) { _endCaps[i] = MakeImage(_endLayer, "EndCap" + i, null); _endCaps[i].gameObject.SetActive(false); }
            _endCard = Rr(_endLayer, "Card", new Color(34f / 255f, 32f / 255f, 90f / 255f, 0.97f), 22f);
            for (int i = 0; i < _endLabel.Length; i++)
            {
                _endLabel[i] = MakeLabel(_endLayer, "Label" + i, 15f, UiFonts.Regular, Soft, TextAnchor.MiddleLeft);
                _endValue[i] = MakeLabel(_endLayer, "Value" + i, 17f, UiFonts.Bold, Color.white, TextAnchor.MiddleRight);
                _endLabel[i].horizontalOverflow = _endValue[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            _endNote1 = MakeLabel(_endLayer, "Note1", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _endNote2 = MakeLabel(_endLayer, "Note2", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _endNote3 = MakeLabel(_endLayer, "Note3", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _endRecordBg = Rr(_endLayer, "RecordBg", Gold, 15f);
            _endRecord = MakeLabel(_endLayer, "Record", 15f, UiFonts.Bold, Ink, TextAnchor.MiddleCenter);
            _endTag.text = RadarContract.EndTag;
            _endNote1.text = RadarContract.EndNote1;
            _endNote2.text = RadarContract.EndNote2;
            _endNote3.text = RadarContract.EndNote3;
            _endLabel[0].text = RadarContract.EndCapture;
            _endLabel[1].text = RadarContract.EndShortest;
            _endLabel[2].text = RadarContract.EndPerfect;
            _endLabel[3].text = RadarContract.EndStreak;
            _endLabel[4].text = RadarContract.EndTrips;
            foreach (var t in new[] { _endTag, _endTitle, _endNote1, _endNote2, _endNote3, _endRecord }) t.horizontalOverflow = HorizontalWrapMode.Wrap;
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

        /// <summary>Pone los sprites horneados (una vez, al terminar de hornear).</summary>
        private void AssignSprites()
        {
            _planet.sprite = RadarSprites.Planet();
            _station.sprite = RadarSprites.Station();
            foreach (var d in _debris) d.sprite = RadarSprites.Debris();
            _scope.sprite = RadarSprites.Scope();
            _sweep.sprite = RadarSprites.Sweep();
            _axis.sprite = RadarSprites.Axis();
            _shipHull.sprite = RadarSprites.Ship();
            foreach (var w in _windows) w.sprite = RadarSprites.Window();
            foreach (var r in _rockImgs) r.sprite = RadarSprites.Rock();
            foreach (var r in _capRings) r.sprite = RadarSprites.DashedRing();
            for (int i = 0; i < _buttons.Length; i++)
            {
                var type = RadarContract.BoardOrder[i];
                _buttons[i].Bg.sprite = RadarSprites.Button();
                _buttons[i].Emblem.sprite = RadarSprites.Emblem(type);
                }
            _go.Bg.sprite = RadarSprites.GoButton();
            _hudIcon.sprite = RadarSprites.Emblem(CapsuleType.Circle);
        }

        // ------------------------------------------------------------------ disposición (dp lógicos de un campo de 360 de ancho; 1 dp = _s unidades)

        private float _s = 3f, _playW = 1080f, _playH = 1920f, _logicalH = 640f;
        private RadarPlan _plan = new RadarPlan(640f, false);

        private Vector2 P(float x, float y) => new Vector2((x - RadarPlan.Width * 0.5f) * _s, _playH * 0.5f - y * _s);
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + RadarPlan.Width * 0.5f, (_playH * 0.5f - u.y) / _s);

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
            _s = _playW / RadarPlan.Width;
            _logicalH = _playH / _s;
            _plan = new RadarPlan(_logicalH, _guided);
            foreach (var kv in _fonts)
            {
                int px = Mathf.RoundToInt(kv.Value * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            foreach (var kv in _radii) SetRadius(kv.Key, kv.Value * _s);
            LayoutBack();
            LayoutRadar();
            LayoutShip();
            LayoutBoard();
            LayoutHud();
            LayoutEnd();
        }

        private void LayoutBack()
        {
            // todo lo que acompaña al radar (el planeta y la estación) va con él y a su escala; las nebulosas, con las proporciones de la pantalla
            float rk = _plan.GlassR / 146f, cx = _plan.RadarCx, cy = _plan.RadarCy;
            var nebPos = new[] { new Vector2(30f / 360f, 650f / 780f), new Vector2(340f / 360f, 70f / 780f), new Vector2(190f / 360f, 300f / 780f), new Vector2(1f, 560f / 780f) };
            var nebR = new[] { 320f, 260f, 230f, 200f };
            for (int i = 0; i < NebulaPool; i++) SetRect(_nebulae[i].rectTransform, nebPos[i].x * RadarPlan.Width, nebPos[i].y * _logicalH, nebR[i] * 2f, nebR[i] * 2f);
            SetRect(_planet.rectTransform, cx - 150f * rk, cy - 114f * rk, 46f * 2f * 1.6f * rk, 46f * 2f * 1.6f * rk);
            float sx = cx + 148f * rk, sy = cy - 94f * rk;
            SetRect(_station.rectTransform, sx, sy, RadarSprites.StationSide * rk, RadarSprites.StationSide * rk);
            _station.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 0.35f * Mathf.Rad2Deg);
            SetRect(_stationGlow.rectTransform, sx, sy, 24f * rk, 24f * rk);
            SetRect(_stationDot.rectTransform, sx, sy, 8f * rk, 8f * rk);
        }

        private void LayoutRadar()
        {
            _geo = RadarGeometry.For(_plan.GlassR);
            float cx = _plan.RadarCx, cy = _plan.RadarCy, side = _plan.BezelR * RadarSprites.ScopeSideInBezelRadii, k = _plan.Scale;
            SetRect(_radarZone, cx, cy, _plan.BezelR * 2f, _plan.BezelR * 2f);
            SetRect(_scope.rectTransform, cx, cy, side, side);
            SetRect(_mask.rectTransform, cx, cy, side, side);
            SetRect(_sweep.rectTransform, cx, cy, _plan.GlassR * 2f, _plan.GlassR * 2f);
            _beamLine.rectTransform.sizeDelta = new Vector2(_plan.GlassR * _s, 2.8f * k * _s);
            _beamLine.rectTransform.anchoredPosition = P(cx, cy);
            SetRect(_flashFill.rectTransform, cx, cy, (_plan.GlassR - 2f * k) * 2f, (_plan.GlassR - 2f * k) * 2f);
            SetRect(_flashGlow.rectTransform, cx, cy, _plan.GlassR * 2.6f, _plan.GlassR * 2.6f);
            SetRect(_flashWave.rectTransform, cx, cy, 40f * k, 40f * k);
            SetRect(_sweepHead.rectTransform, cx, cy, 20f * k, 20f * k);
            SetRect(_axis.rectTransform, cx, cy, 15f * k, 15f * k);
            foreach (var e in _echoes) e.rectTransform.sizeDelta = new Vector2(5f * k * _s, 5f * k * _s);
            float cs = _geo.CapsuleSize * RadarSprites.CapsuleSideInSizes;
            foreach (var c in _capImgs) c.rectTransform.sizeDelta = new Vector2(cs * _s, cs * _s);
            foreach (var r in _capRings) r.rectTransform.sizeDelta = new Vector2(80f * k * _s, 80f * k * _s);
            float rs = 23f * RadarSprites.RockSideInRadii * k;
            foreach (var r in _rockImgs) r.rectTransform.sizeDelta = new Vector2(rs * _s, rs * _s);
            PlaceRoundObjects();
        }

        private void LayoutShip()
        {
            float ss = _plan.ShipScale, cx = _plan.RadarCx, y = _plan.ShipY;
            SetRect(_shipZone, cx, y, 228f * ss, 84f * ss);
            SetRect(_shipRoot, cx, y, 0f, 0f);                                              // la nave (todo lo suyo, centrado en sí misma): salta y viaja entera
            SetChild(_shipHull.rectTransform, 0f, 0f, RadarSprites.ShipSide * ss, RadarSprites.ShipSide * ss);
            for (int i = 0; i < WindowCount; i++) SetChild(_windows[i].rectTransform, (-81f + i * 18f) * ss, -2f * ss, RadarSprites.WindowSide * ss, RadarSprites.WindowSide * ss);
            float fs = _geo.CapsuleSize * RadarSprites.CapsuleSideInSizes;
            foreach (var f in _flying) f.rectTransform.sizeDelta = new Vector2(fs * _s, fs * _s);
            foreach (var sp in _sparks) sp.rectTransform.sizeDelta = new Vector2(4.4f * ss * _s, 4.4f * ss * _s);
            foreach (var d in _dockSparks) d.rectTransform.sizeDelta = new Vector2(4f * ss * _s, 4f * ss * _s);
            SetRect(_shipCount.rectTransform, cx, _plan.ShipCountY, 180f, 22f);
            _shipCount.gameObject.SetActive(_plan.ShowShipCount && !_hideShipCount);
        }

        private void LayoutBoard()
        {
            float c = _plan.CellW / 104f;                                                    // la escala del botón (1 con los 104 × 74 dp de la referencia)
            for (int i = 0; i < _buttons.Length; i++)
            {
                var (cx, cy) = _plan.CellCenter(i);
                var v = _buttons[i];
                SetRect(v.Root, cx, cy, _plan.CellW, _plan.CellH);
                SetChild(v.Bg.rectTransform, 0f, 0f, RadarSprites.ButtonSide * c, RadarSprites.ButtonSide * c);
                SetChild(v.Ring.rectTransform, 0f, 0f, RadarSprites.ButtonSide * c, RadarSprites.ButtonSide * c);
                float es = 18f * 2f * RadarSprites.CapsuleSideInSizes * c;                     // forma de 18 (unos 36 dp)
                SetChild(v.Emblem.rectTransform, 0f, -11f * c, es, es);
                SetChild(v.Label.rectTransform, 0f, 24f * c, _plan.CellW - 8f, 20f);
                SetChild(v.Badge.rectTransform, _plan.CellW * 0.5f - 12f * c, -_plan.CellH * 0.5f + 10f * c, 22f * c, 22f * c);
            }
            SetRect(_go.Root, _plan.GoCx, _plan.GoCy, _plan.GoW, _plan.GoH);
            float gs = RadarSprites.GoSide * (_plan.GoW / 172f);
            SetChild(_go.Bg.rectTransform, 0f, 0f, gs, gs);
            SetChild(_go.Label.rectTransform, 0f, 1f, _plan.GoW - 20f, 28f);
        }

        private void LayoutHud()
        {
            SetRect(_hudIcon.rectTransform, RadarPlan.Width - 26f, 26f, 26f, 26f);
            SetRect(_hudCount.rectTransform, RadarPlan.Width - 44f - 36f, 26f, 72f, 28f);
            SetRect(_streakBg.rectTransform, RadarPlan.Width - 16f - 56f, 53f, 112f, 26f);
            SetRect(_streakText.rectTransform, RadarPlan.Width - 16f - 56f, 53f, 112f, 22f);
            SetRect(_msgA.rectTransform, RadarPlan.Width * 0.5f, RadarPlan.StripLineA, RadarPlan.Width - 28f, 26f);
            SetRect(_msgB.rectTransform, RadarPlan.Width * 0.5f, RadarPlan.StripLineB, RadarPlan.Width - 28f, 20f);
            SetRect(_msgPill.rectTransform, RadarPlan.Width * 0.5f, 94f, RadarPlan.Width - 24f, 34f);
            SetRect(_timerTrack.rectTransform, RadarPlan.Width * 0.5f, 71f, RadarPlan.Width - 24f, 5f);
            SetTimer(_timerFraction);
        }

        private float _timerFraction = 1f;

        /// <summary>La barra de tiempo del Reto (llena = todo el tiempo; baja de verde a coral).</summary>
        private void SetTimer(float fraction)
        {
            _timerFraction = Mathf.Clamp01(fraction);
            var r = _timerFill.rectTransform;
            r.sizeDelta = new Vector2(Mathf.Max(0.01f, (RadarPlan.Width - 24f) * _timerFraction) * _s, 5f * _s);
            r.anchoredPosition = P(12f, 71f);
            _timerFill.color = _timerFraction > 0.5f ? Color.Lerp(Gold, Lime, (_timerFraction - 0.5f) * 2f) : Color.Lerp(Coral, Gold, _timerFraction * 2f);
        }

        private void LayoutEnd()
        {
            float cx = RadarPlan.Width * 0.5f;
            SetRect(_endTag.rectTransform, cx, RadarPlan.HudBottom + 12f, 300f, 20f);               // bajo el marcador (63 dp): nunca lo tapa
            SetRect(_endTitle.rectTransform, cx, RadarPlan.HudBottom + 39f, 330f, 34f);
            PlaceEndCaps();
            int rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Min(_seats.Count, EndCapsPool) / 10f));
            float y0 = Mathf.Min(150f + rows * 32f, 290f);                                      // hasta 48 caben sin tocar la tarjeta
            SetRect(_endCard.rectTransform, cx, y0 + 86f, RadarPlan.Width - 44f, 172f);
            for (int i = 0; i < _endLabel.Length; i++)
            {
                SetRect(_endLabel[i].rectTransform, 40f + 110f, y0 + 22f + i * 32f, 220f, 24f);
                SetRect(_endValue[i].rectTransform, RadarPlan.Width - 40f - 70f, y0 + 22f + i * 32f, 140f, 24f);
            }
            SetRect(_endNote1.rectTransform, cx, y0 + 194f, 336f, 22f);
            SetRect(_endNote2.rectTransform, cx, y0 + 214f, 336f, 22f);
            SetRect(_endNote3.rectTransform, cx, y0 + 234f, 336f, 22f);
            SetRect(_endRecordBg.rectTransform, cx, y0 + 270f, 190f, 30f);
            SetRect(_endRecord.rectTransform, cx, y0 + 270f, 190f, 24f);
        }

        /// <summary>Lo que ocupan, abajo, el rótulo «Práctica: no cuenta» y «Saltar tutorial» (dp).</summary>
        private const float TutorialControlsDp = RadarPlan.TutorialControls;
    }
}
