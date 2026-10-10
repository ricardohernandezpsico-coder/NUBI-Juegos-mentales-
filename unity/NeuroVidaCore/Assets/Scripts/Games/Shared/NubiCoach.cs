using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RingSprite / RadialGlowSprite
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// «Nubi entrenadora» (Ricardo, 4-oct): el tutorial de todos los juegos. La pantalla se oscurece con un velo gris y SOLO se ilumina lo que hay que tocar (un hueco redondeado, o
    /// circular, con un aro celeste que late) y las ZONAS PROTEGIDAS que cada paso declara (la pregunta, la consigna, el rótulo del que habla el texto, el contador): esas se ven
    /// claras junto al hueco, sin la sombra. Nubi entra deslizándose por un costado y habla en un globo claro que CRECE con el texto (máximo 3 líneas; la letra no se achica) y
    /// que, junto con Nubi, nunca tapa el hueco, ni las zonas protegidas, ni ningún texto del juego, ni el dedo que insiste (<see cref="CoachLayout"/> busca el lugar). Tres formas:
    /// <list type="bullet">
    /// <item><b><see cref="Touch"/></b>: el juego se CONGELA (<see cref="GameClock"/> en pausa) y el toque DENTRO del hueco es la acción real del juego: cierra el foco, el juego sigue y
    /// recibe ese mismo toque (por eso este componente corre antes que los controladores: <c>DefaultExecutionOrder(-100)</c>). Fuera del hueco no responde (los juegos preguntan
    /// <see cref="Blocks"/> antes de leer un toque; los botones de UI los tapa el velo), salvo «Saltar tutorial». Nada espera en silencio: a los 5 s Nubi insiste (el globo salta y aparece un dedo que
    /// señala el hueco).</item>
    /// <item><b><see cref="Watch"/></b>: el foco sobre algo que se mueve, SIN congelar; se cierra cuando pasa lo que se esperaba.</item>
    /// <item><b><see cref="Notice"/></b>: un globo de Nubi sin velo, ~1,8 s, que se va solo («¡Eso es!»).</item>
    /// </list>
    /// Con «quitar animaciones»: sin deslizamiento, latido, salto ni dedo que se mueve; el velo y el hueco aparecen con un fundido rápido.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class NubiCoach : MonoBehaviour
    {
        private enum Kind { None, Touch, Watch, Notice }

        private const float InsistSeconds = 5f;
        private const float HolePad = 12f;
        private const float KeepPad = 10f;            // lo que se agranda cada zona protegida al iluminarla
        private const float HoleAvoid = 12f;          // margen alrededor del hueco que Nubi y el globo no pisan
        private const float KeepAvoid = 4f;
        private const float TextAvoid = 6f;
        private const int MaxVeil = 24, MaxLit = 4;   // piezas del velo y zonas iluminadas (el hueco + 3 protegidas)
        public const int MaxKeep = MaxLit - 1;

        private static readonly Color VeilColor = new Color(0.03f, 0.04f, 0.11f, 0.72f);
        private static readonly Color BubbleColor = new Color(1f, 0.965f, 0.9f, 1f);

        private RectTransform _root, _group, _bubble;
        private CanvasGroup _rootGroup, _groupGroup;
        private readonly Image[] _veil = new Image[MaxVeil];
        private readonly Image[] _corners = new Image[MaxLit * 4];
        private Image _frame, _ring, _nubi, _finger;
        private Text _text;
        private Func<Vector2, bool> _isSkip;
        private Func<bool> _aborted;

        private Kind _kind = Kind.None;
        private Func<Rect> _target;
        private Func<Rect>[] _keep;
        private Func<bool> _done;
        private bool _circle, _freeze, _frozen, _left;
        private float _waited, _noticeSeconds, _maxSeconds, _shownAt, _lastInsist, _popAt = -10f, _lastGather = -10f, _lastReplace = -10f;
        private Rect _hole;
        private readonly List<Rect> _lit = new List<Rect>();          // [0] = el hueco (si hay) y después las zonas protegidas, ya agrandadas
        private readonly List<Rect> _hard = new List<Rect>();         // lo que nunca se tapa (hueco, zonas, dedo), con su margen
        private readonly List<Rect> _prefer = new List<Rect>();       // lo que conviene no tapar sin que sea un error (el dedo de ayuda)
        private readonly List<Rect> _soft = new List<Rect>();         // los textos del juego a la vista
        private readonly List<Rect> _textRaw = new List<Rect>();      // los mismos, sin margen, sin los del tutorial
        private readonly List<Rect> _controls = new List<Rect>();     // los textos de los controles del tutorial («Práctica: no cuenta», «Saltar tutorial»), sin margen
        private readonly List<Text> _textBuf = new List<Text>();
        private List<Rect> _veilRects = new List<Rect>();
        private CoachPlacement _placement;
        private CoachStepReport _report;
        private bool _warned;
        private int _misses, _swallowFrame = -1;
        private Vector2 _lastTouchLocal;
        private bool _hasTouch;
        private readonly TextGenerator _measureGen = new TextGenerator();

        /// <summary>Los controles del tutorial («Saltar tutorial» y «Práctica: no cuenta»): Nubi y su globo no los tapan (los pone <see cref="GuidedTutorial"/>; cada juego los coloca donde le queda mejor).</summary>
        public RectTransform ControlSkip, ControlBadge;

        // red de seguridad: ningún paso de Tocar puede dejar a alguien sin poder avanzar
        private const float NearSlack = 120f;         // 40 dp: un toque a menos de esto del borde del hueco cuenta como el toque pedido
        private const float StuckSeconds = 10f;       // a los 10 s sin un toque válido, el siguiente toque en cualquier parte avanza
        private const int MissesToAdvance = 2;        // el segundo toque fuera del hueco avanza

        /// <summary>SOLO DEPURACIÓN: dibuja encima los rectángulos del foco (hueco, zonas, globo, Nubi, dedo, «Saltar tutorial») y el último toque, para poder mandar una captura de lo que pasa en el teléfono.</summary>
        public static bool DebugOverlay;
        private RectTransform _dbgRoot;
        private readonly List<Image> _dbgBoxes = new List<Image>();
        private Text _dbgText;

        /// <summary>true mientras hay un foco (Tocar, Mirar o un aviso) a la vista.</summary>
        public bool Active => _kind != Kind.None;

        /// <summary>
        /// true si el último paso de Tocar se cerró por la RED DE SEGURIDAD (el segundo toque fuera del hueco, o un toque pasados 10 s) y no por un toque válido en el hueco: ese toque no llegó al juego. La ronda guiada, entonces, hace
        /// ella misma la jugada que el paso pedía (como si se hubiera tocado bien) y sigue; nunca vuelve a pedir el mismo paso en un bucle (Tarea 58).
        /// </summary>
        public bool ClosedBySafetyNet { get; private set; }

#if UNITY_EDITOR
        /// <summary>
        /// SOLO EN EL EDITOR (smoke de los tutoriales, Tarea 58): con esto encendido, en CADA paso de Tocar el foco da por su cuenta un toque «de verdad» en el centro del hueco (entra por <see cref="GuidedTutorial.TryPress"/>, el mismo camino que un dedo,
        /// no por un atajo) y comprueba que el paso se cierra. Si no se cierra, deja el motivo en <see cref="ProbeFailures"/>. Antes el Editor cerraba los pasos solo, a los 1,2 s, sin tocar: así nunca se vio que en el teléfono los
        /// toques de Engranajes y de Carga exacta no llegaban al foco. En el teléfono no existe.
        /// </summary>
        public static bool EditorProbe;
        public static readonly List<string> ProbeFailures = new List<string>();
        /// <summary>Cuántos pasos de Tocar se cerraron por un toque «de verdad» (el del foco o el de un juego); el smoke exige uno por cada paso de Tocar.</summary>
        public static int RealTouchesAccepted;
        private const float ProbeAfterSeconds = 0.85f, ProbeWaitsSeconds = 0.3f;
        private int _stepSerial, _probeSerial = -1;
        private float _probeAt;
#endif

        // ------------------------------------------------------------------ registro de pasos (pruebas y smoke)

        /// <summary>true = cada paso que se cierra deja su <see cref="CoachStepReport"/> en <see cref="AuditSteps"/> (lo usa el smoke del Editor; en el teléfono no se llena).</summary>
        public static bool AuditEnabled;
        /// <summary>El juego que está corriendo, para rotular el registro.</summary>
        public static string AuditGame = "";
        public static readonly List<CoachStepReport> AuditSteps = new List<CoachStepReport>();

        public static NubiCoach Create(RectTransform parent, Func<Vector2, bool> isSkipPoint, Func<bool> aborted)
        {
            var go = new GameObject("NubiCoach", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var coach = go.AddComponent<NubiCoach>();
            coach._isSkip = isSkipPoint;
            coach._aborted = aborted;
            coach.Build(go.GetComponent<RectTransform>());
            return coach;
        }

        // ------------------------------------------------------------------ construcción

        private void Build(RectTransform root)
        {
            _root = root;
            Stretch(_root);
            _rootGroup = gameObject.AddComponent<CanvasGroup>();
            _rootGroup.blocksRaycasts = false;   // el velo decide por sí mismo quién bloquea

            for (int i = 0; i < MaxVeil; i++) _veil[i] = Piece("Veil" + i, null);
            for (int i = 0; i < _corners.Length; i++)
            {
                _corners[i] = Piece("Corner" + i, CoachSprites.Corner());
                int c = i % 4;
                _corners[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, c == 0 ? 0f : c == 1 ? -90f : c == 2 ? 180f : 90f);   // arriba-izq, arriba-der, abajo-der, abajo-izq
            }
            _frame = Piece("Frame", CoachSprites.Frame());
            _frame.type = Image.Type.Sliced;
            _frame.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.95f);
            _ring = Piece("Ring", RingSprite.Get());
            _ring.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.95f);

            var groupGo = new GameObject("Nubi");
            groupGo.transform.SetParent(_root, false);
            _group = groupGo.AddComponent<RectTransform>();
            _group.anchorMin = _group.anchorMax = _group.pivot = new Vector2(0.5f, 0.5f);
            _groupGroup = groupGo.AddComponent<CanvasGroup>();
            _groupGroup.blocksRaycasts = false;
            var nubiGo = new GameObject("Sprite");
            nubiGo.transform.SetParent(_group, false);
            var nr = nubiGo.AddComponent<RectTransform>();
            nr.anchorMin = nr.anchorMax = nr.pivot = new Vector2(0.5f, 0.5f);
            nr.sizeDelta = new Vector2(CoachLayout.NubiSizes[0], CoachLayout.NubiSizes[0]);
            _nubi = nubiGo.AddComponent<Image>();
            _nubi.raycastTarget = false;
            var bubbleGo = new GameObject("Bubble");
            bubbleGo.transform.SetParent(_group, false);
            _bubble = bubbleGo.AddComponent<RectTransform>();
            _bubble.anchorMin = _bubble.anchorMax = _bubble.pivot = new Vector2(0.5f, 0.5f);
            var bi = bubbleGo.AddComponent<Image>();
            bi.sprite = RoundedRectSprite.Get(40);
            bi.type = Image.Type.Sliced;
            bi.color = BubbleColor;
            bi.raycastTarget = false;
            NeuroStyle.ClayFrame(bi, 5f, 9f);
            _text = MakeText(_bubble, "Text", CoachLayout.FontSize, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            _text.rectTransform.offsetMin = new Vector2(CoachLayout.PadX, CoachLayout.PadY);
            _text.rectTransform.offsetMax = new Vector2(-CoachLayout.PadX, -CoachLayout.PadY);
            // la letra del globo NUNCA se achica: el globo crece con las líneas (máximo 3); si un texto no cabe, se acorta el texto
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.resizeTextForBestFit = false;

            _finger = Piece("Finger", DiscSprite.Get());
            _finger.color = BubbleColor;
            _finger.rectTransform.sizeDelta = new Vector2(78f, 78f);
            var ink = Piece("FingerRim", RingSprite.Get());
            ink.transform.SetParent(_finger.transform, false);
            ink.rectTransform.sizeDelta = new Vector2(80f, 80f);
            ink.color = NeuroStyle.Ink;
            ink.gameObject.SetActive(true);
            _root.gameObject.SetActive(false);
        }

        private Image Piece(string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = sprite == null || name.StartsWith("Corner") ? VeilColor : Color.white;
            img.raycastTarget = false;
            return img;
        }

        // ------------------------------------------------------------------ conversiones

        /// <summary>El rect de <paramref name="rt"/> en el espacio del foco (centrado en el área segura).</summary>
        public Rect RectOf(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            var a = (Vector2)_root.InverseTransformPoint(c[0]);
            var b = (Vector2)_root.InverseTransformPoint(c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>El centro de <paramref name="rt"/> en el espacio del foco.</summary>
        public Vector2 CenterOf(RectTransform rt) => RectOf(rt).center;

        /// <summary>Un rect de <paramref name="size"/> unidades centrado en <paramref name="rt"/> (para marcar algo cuyo propio rect es chico o cero).</summary>
        public Rect AroundOf(RectTransform rt, Vector2 size)
        {
            var c = CenterOf(rt);
            return new Rect(c - size / 2f, size);
        }

        /// <summary>Una zona protegida que sigue a <paramref name="rt"/> (la pregunta, la consigna, un rótulo): se ilumina junto al hueco y Nubi no la tapa.</summary>
        public Func<Rect> Zone(RectTransform rt) => () => RectOf(rt);

        /// <summary>Lo mismo con un rect propio (se pide cada cuadro, por si se mueve).</summary>
        public Func<Rect> Zone(Func<Rect> rect) => rect;

        /// <summary>Un rect que cubre a todos los <paramref name="rts"/> juntos (p. ej. el contador y su rótulo).</summary>
        public Func<Rect> ZoneOf(params RectTransform[] rts) => () =>
        {
            Rect r = default;
            bool first = true;
            foreach (var rt in rts)
            {
                if (rt == null) continue;
                var x = RectOf(rt);
                r = first ? x : Rect.MinMaxRect(Mathf.Min(r.xMin, x.xMin), Mathf.Min(r.yMin, x.yMin), Mathf.Max(r.xMax, x.xMax), Mathf.Max(r.yMax, x.yMax));
                first = false;
            }
            return r;
        };

        /// <summary>Una zona protegida que cubre justo las letras de estos textos (no toda su caja): el número del reactor y su rótulo, el número de la misión. Un texto vacío o apagado no cuenta.</summary>
        public Func<Rect> ZoneOfTexts(params Text[] texts) => () =>
        {
            Rect r = default;
            bool first = true;
            foreach (var t in texts)
            {
                if (t == null || !t.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(t.text)) continue;
                if (!TextRect(t, out var x)) x = RectOf(t.rectTransform);
                r = first ? x : Rect.MinMaxRect(Mathf.Min(r.xMin, x.xMin), Mathf.Min(r.yMin, x.yMin), Mathf.Max(r.xMax, x.xMax), Mathf.Max(r.yMax, x.yMax));
                first = false;
            }
            return r;
        };

        private bool InsideHole(Vector2 screenPos)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPos, null, out var local)) return false;
            return _hole.Contains(local);
        }

        /// <summary>true si un toque en <paramref name="screenPos"/> NO debe llegar al juego: hay un foco de Tocar y el toque cae fuera del hueco (y no es «Saltar tutorial»).</summary>
        public bool Blocks(Vector2 screenPos) => Time.frameCount == _swallowFrame || (_kind == Kind.Touch && !InsideHole(screenPos) && !(_isSkip != null && _isSkip(screenPos)));

        // ------------------------------------------------------------------ las tres formas

        /// <summary>Foco de TOCAR: congela el juego (si <paramref name="freeze"/>) y espera el toque dentro del hueco; ese toque llega al juego. Devuelve cuando se cierra.
        /// <paramref name="keep"/>: las zonas que deben seguir a la vista y claras (hasta <see cref="MaxKeep"/>).</summary>
        public IEnumerator Touch(Func<Rect> target, string text, bool circle = false, bool freeze = true, Func<Rect>[] keep = null)
        {
            Begin(Kind.Touch, target, text, circle, freeze, null, 0f, keep);
            while (_kind != Kind.None) { if (_aborted != null && _aborted()) { Close(); break; } yield return null; }
        }

        /// <summary>Foco de MIRAR: sin congelar, sobre algo que se mueve; se cierra cuando <paramref name="done"/> da true (o a los <paramref name="maxSeconds"/>).</summary>
        public IEnumerator Watch(Func<Rect> target, string text, Func<bool> done, float maxSeconds = 12f, bool circle = false, Func<Rect>[] keep = null)
        {
            Begin(Kind.Watch, target, text, circle, false, done, maxSeconds, keep);
            while (_kind != Kind.None) { if (_aborted != null && _aborted()) { Close(); break; } yield return null; }
        }

        /// <summary>Aviso breve: el globo de Nubi sin velo, que se va solo. <paramref name="keep"/>: lo que el globo no debe tapar.</summary>
        public IEnumerator Notice(string text, float seconds = 1.8f, Func<Rect>[] keep = null)
        {
            Begin(Kind.Notice, null, text, false, false, null, seconds, keep);
            while (_kind != Kind.None) { if (_aborted != null && _aborted()) { Close(); break; } yield return null; }
        }

        /// <summary>Cierra lo que haya (y reanuda el juego si estaba congelado).</summary>
        public void Hide() => Close();

        private void Begin(Kind kind, Func<Rect> target, string text, bool circle, bool freeze, Func<bool> done, float seconds, Func<Rect>[] keep)
        {
            Close();
            _kind = kind;
            _target = target;
            _keep = keep;
            _circle = circle;
            _freeze = freeze;
            _done = done;
            _maxSeconds = kind == Kind.Watch ? seconds : 0f;
            _noticeSeconds = kind == Kind.Notice ? seconds : 0f;
            _waited = 0f;
            _shownAt = Time.unscaledTime;
            _lastInsist = 0f;
            _popAt = -10f;
            _warned = false;
            _lastGather = -10f;
            _lastReplace = -10f;
            _misses = 0;
            _hasTouch = false;
            ClosedBySafetyNet = false;
#if UNITY_EDITOR
            _stepSerial++;
#endif
            _litShown.Clear();
            _text.text = text;
            if (_nubi.sprite == null) _nubi.sprite = NubiTeacherSprite.Get();
            _root.gameObject.SetActive(true);
            bool veil = kind != Kind.Notice;
            foreach (var s in _veil) { s.gameObject.SetActive(false); s.raycastTarget = kind == Kind.Touch; }
            // Las esquinas oscuras que redondean el hueco del foco anterior se apagan TAMBIÉN aquí: un aviso (sin velo) no las vuelve a calcular y quedaban a la vista alrededor de lo último que se iluminó, como un cuadrado oscuro (8-oct, paso 3 de Rastro de luz).
            // Y se olvida el último velo calculado, para que el foco siguiente lo arme de nuevo aunque caiga en el mismo lugar.
            foreach (var c in _corners) c.gameObject.SetActive(false);
            _litShown.Clear();
            _frame.gameObject.SetActive(veil && !circle);
            _ring.gameObject.SetActive(veil && circle);
            _finger.gameObject.SetActive(false);
            if (AuditEnabled)
                _report = new CoachStepReport { Game = AuditGame, Index = AuditSteps.Count, Kind = kind.ToString(), Text = text, FontSize = CoachLayout.FontSize, HasHole = target != null && kind != Kind.Notice, Circle = circle };
            RefreshZones(true);
            Replace(true);
            if (_report != null) FillReport();
            if (kind == Kind.Touch) LogStep(text);
            if (kind == Kind.Touch && freeze && !PauseMenu.Open)
            {
                GameClock.Pause(silenceAudio: false);        // el reloj se detiene pero el audio sigue: un sonido que estaba sonando termina completo (Tarea 64)
                _frozen = true;
            }
            UpdateLayout(0f);
        }

        private void Close()
        {
            if (_kind == Kind.None) { if (_frozen) Unfreeze(); return; }
            Record();
            _kind = Kind.None;
            if (_root != null) _root.gameObject.SetActive(false);
            Unfreeze();
        }

        private void Unfreeze()
        {
            if (!_frozen) return;
            _frozen = false;
            if (!PauseMenu.Open) GameClock.Resume();
        }

        private void OnDisable() => Close();

        // ------------------------------------------------------------------ cada cuadro

        private void Update()
        {
            if (_kind == Kind.None) return;
            float dt = GameClock.RealDeltaTime;
            _waited += dt;
            RefreshZones(false);
            if (PlacementIsStale()) Replace(false);
            UpdateLayout(dt);

            switch (_kind)
            {
                case Kind.Touch:
                    // si la pausa del menú se cerró mientras el foco seguía abierto, el juego se vuelve a congelar
                    if (_freeze && _frozen && !GameClock.Paused && !PauseMenu.Open) GameClock.Pause(silenceAudio: false);
                    if (!PauseMenu.Open && GuidedTutorial.TryPress(out Vector2 pos) && _waited > 0.15f && !(_isSkip != null && _isSkip(pos)))
                    {
                        HandleTouch(pos);
                        if (_kind == Kind.None) return;
                    }
#if UNITY_EDITOR
                    if (EditorProbe)
                    {
                        if (_probeSerial != _stepSerial && _waited >= ProbeAfterSeconds)
                        {
                            _probeSerial = _stepSerial;
                            _probeAt = _waited;
                            // congelar para Nubi detiene el reloj pero NO calla el audio (Tarea 64): si el audio queda en pausa, un sonido que estaba sonando se cortaría a la mitad
                            if (_frozen && !PauseMenu.Open && AudioListener.pause) ProbeFailures.Add($"{AuditGame}: el paso «{_text.text}» congeló el juego Y calló el audio (AudioListener.pause): los sonidos se cortarían");
                            var world = _root.TransformPoint(new Vector3(_hole.center.x, _hole.center.y, 0f));
                            GuidedTutorial.EditorPressPos = RectTransformUtility.WorldToScreenPoint(null, world);
                            GuidedTutorial.EditorPressFrame = Time.frameCount + 1;
                        }
                        else if (_probeSerial == _stepSerial && _waited >= _probeAt + ProbeWaitsSeconds)
                        {
                            // el toque de prueba cayó en el centro del hueco y el paso sigue abierto: un dedo de verdad tampoco lo cerraría
                            ProbeFailures.Add($"{AuditGame}: un toque en el centro del hueco NO cerró el paso «{_text.text}» (PauseMenu.Open={PauseMenu.Open}, hueco {_hole})");
                            _probeSerial = -2;
                        }
                    }
                    if (GuidedTutorial.EditorAutoContinue && _waited > 1.2f && (!EditorProbe || _probeSerial == -2 || _waited > 2.4f)) { Close(); return; }
#endif
                    if (_waited - _lastInsist >= InsistSeconds && _waited >= InsistSeconds)
                    {
                        _lastInsist = _waited;
                        _popAt = Time.unscaledTime;
                        _finger.gameObject.SetActive(true);
                    }
                    break;
                case Kind.Watch:
                    if ((_done != null && _done()) || _waited >= _maxSeconds) Close();
                    break;
                case Kind.Notice:
                    if (_waited >= _noticeSeconds) Close();
                    break;
            }
        }

        /// <summary>Distancia (unidades del lienzo) de un punto al rectángulo del hueco: 0 si está adentro.</summary>
        private float DistanceToHole(Vector2 local)
        {
            float dx = Mathf.Max(Mathf.Max(_hole.xMin - local.x, 0f), local.x - _hole.xMax);
            float dy = Mathf.Max(Mathf.Max(_hole.yMin - local.y, 0f), local.y - _hole.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Un toque en un paso de Tocar. Adentro del hueco (o a menos de 40 dp de su borde) es el toque pedido: cierra el paso y el juego lo recibe. Si no, no se acepta, PERO nadie queda
        /// atrapado: el segundo toque fuera, o cualquier toque pasados 10 s, avanza el paso (ese toque no llega al juego, para que no haga algo que nadie pidió). Cada toque queda en el registro.</summary>
        private void HandleTouch(Vector2 screenPos)
        {
            bool mapped = RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPos, null, out var local);
            _lastTouchLocal = local;
            _hasTouch = true;
            float dist = mapped ? DistanceToHole(local) : float.MaxValue;
            bool accept = false, pass = true;
            string why;
            if (mapped && dist <= 0f) { accept = true; why = "dentro del hueco"; }
            else
            {
                _misses++;
                if (mapped && dist <= NearSlack) { accept = true; why = "cerca del borde del hueco"; }
                else if (_misses >= MissesToAdvance) { accept = true; pass = false; why = "segundo toque fuera: avanza"; }
                else if (_waited >= StuckSeconds) { accept = true; pass = false; why = "pasaron 10 s: avanza"; }
                else why = "fuera del hueco: no se acepta";
            }
            LogTouch(screenPos, local, mapped, dist, accept, why);
#if UNITY_EDITOR
            if (accept && pass && GuidedTutorial.EditorPressFrame == Time.frameCount) RealTouchesAccepted++;
#endif
            if (!accept) return;
            if (!pass) { _swallowFrame = Time.frameCount; ClosedBySafetyNet = true; }
            Close();     // el juego recibe este mismo toque (este componente corre antes que él), salvo el que avanzó por la red de seguridad
        }

        private static string N(float v) => Mathf.RoundToInt(v).ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Al empezar un paso de Tocar: pantalla, zona segura, escala del lienzo y hueco, en el registro de errores del teléfono (Ajustes → «Enviar informe de errores»).</summary>
        private void LogStep(string text)
        {
            var canvas = _root.GetComponentInParent<Canvas>();
            var sa = Screen.safeArea;
            NativeBridge.LogDiagnostic("TUTORIAL", $"{AuditGame} paso «{(text.Length > 40 ? text.Substring(0, 40) : text)}»: pantalla {Screen.width}x{Screen.height}, zona segura {N(sa.x)},{N(sa.y)} {N(sa.width)}x{N(sa.height)}, escala {(canvas != null ? canvas.scaleFactor : 0f):0.00}, raíz {N(_root.rect.width)}x{N(_root.rect.height)}, hueco {N(_hole.xMin)},{N(_hole.yMin)} {N(_hole.width)}x{N(_hole.height)}");
        }

        private void LogTouch(Vector2 screenPos, Vector2 local, bool mapped, float dist, bool accept, string why)
        {
            NativeBridge.LogDiagnostic("TUTORIAL", $"{AuditGame} toque en pantalla {N(screenPos.x)},{N(screenPos.y)} → lienzo {(mapped ? N(local.x) + "," + N(local.y) : "sin convertir")}; hueco {N(_hole.xMin)},{N(_hole.yMin)} {N(_hole.width)}x{N(_hole.height)}; distancia {(mapped ? N(dist) : "?")}; {(accept ? "ACEPTADO" : "no aceptado")} ({why}); toques fuera {_misses}; espera {_waited:0.0} s");
        }

        private Rect Inflate(Rect r)
        {
            var pr = _root.rect;
            var o = Rect.MinMaxRect(r.xMin - HolePad, r.yMin - HolePad, r.xMax + HolePad, r.yMax + HolePad);
            if (_circle)
            {
                float d = Mathf.Max(o.width, o.height);
                o = new Rect(o.center - new Vector2(d, d) / 2f, new Vector2(d, d));
            }
            return Clip(o, pr);
        }

        private static Rect Clip(Rect o, Rect pr) =>
            Rect.MinMaxRect(Mathf.Max(o.xMin, pr.xMin), Mathf.Max(o.yMin, pr.yMin), Mathf.Min(o.xMax, pr.xMax), Mathf.Min(o.yMax, pr.yMax));

        // ------------------------------------------------------------------ zonas

        private const float FingerTravel = 150f, FingerR = 45f, FingerTouch = -45f;       // el dedo reposa justo FUERA del borde del hueco (el disco mide 78) y se aleja hasta 150

        /// <summary>El dedo que insiste va SIEMPRE fuera del hueco, pegado a su borde (antes caía en el centro y tapaba las letras de la tarjeta): abajo si cabe y no tapa un texto del juego, si no arriba.</summary>
        private bool FingerGoesBelow(Rect hole)
        {
            var screen = _root.rect;
            var below = FingerRectAt(hole, true);
            var above = FingerRectAt(hole, false);
            bool fitsBelow = below.yMin >= screen.yMin, fitsAbove = above.yMax <= screen.yMax;
            float overBelow = 0f, overAbove = 0f;
            foreach (var t in _textRaw) { overBelow += CoachLayout.Overlap(below, t); overAbove += CoachLayout.Overlap(above, t); }
            if (_placement != null)
            {
                // y, donde hay lugar, del lado donde no queda sobre Nubi ni el globo
                overBelow += CoachLayout.Overlap(below, _placement.Nubi) + CoachLayout.Overlap(below, _placement.Bubble);
                overAbove += CoachLayout.Overlap(above, _placement.Nubi) + CoachLayout.Overlap(above, _placement.Bubble);
            }
            if (fitsBelow != fitsAbove) return fitsBelow;
            return overBelow <= overAbove;
        }

        /// <summary>Todo lo que el dedo recorre con su vaivén (para que Nubi y el globo no queden sobre él).</summary>
        private static Rect FingerRectAt(Rect hole, bool below) => below
            ? new Rect(hole.center.x - FingerR, hole.yMin + FingerTouch - FingerTravel - FingerR, 2f * FingerR, FingerTravel + 2f * FingerR)
            : new Rect(hole.center.x - FingerR, hole.yMax - FingerTouch - FingerR, 2f * FingerR, FingerTravel + 2f * FingerR);

        private Rect FingerRect(Rect hole) => FingerRectAt(hole, FingerGoesBelow(hole));

        private Vector2 FingerCenter(Rect hole, float bob)
        {
            float away = FingerTravel * (1f - bob);
            return FingerGoesBelow(hole) ? new Vector2(hole.center.x, hole.yMin + FingerTouch - away) : new Vector2(hole.center.x, hole.yMax - FingerTouch + away);
        }

        /// <summary>Vuelve a medir el hueco, las zonas protegidas y (cada medio segundo) los textos del juego a la vista.</summary>
        private void RefreshZones(bool force)
        {
            var pr = _root.rect;
            bool hasHole = _kind != Kind.Notice && _target != null;
            _lit.Clear();
            _hard.Clear();
            _prefer.Clear();
            if (hasHole)
            {
                _hole = Inflate(_target());
                _lit.Add(_hole);
                _hard.Add(CoachLayout.Grow(_hole, HoleAvoid));
                if (_kind == Kind.Touch) _prefer.Add(FingerRect(_hole));
            }
            if (_keep != null)
            {
                int n = 0;
                foreach (var k in _keep)
                {
                    if (k == null) continue;
                    var r = k();
                    if (r.width < 1f || r.height < 1f) continue;
                    if (++n > MaxKeep) { Debug.LogWarning("[Coach] más zonas protegidas de las que caben (" + MaxKeep + "): se ignora la sobrante"); break; }
                    var lit = Clip(CoachLayout.Grow(r, KeepPad), pr);
                    if (_kind != Kind.Notice) _lit.Add(lit);
                    _hard.Add(CoachLayout.Grow(lit, KeepAvoid));
                }
            }
            // «Saltar tutorial» y «Práctica: no cuenta» van donde le queda mejor a cada juego (arriba, abajo o a media altura): Nubi y el globo tampoco los tapan
            foreach (var c in new[] { ControlSkip, ControlBadge })
                if (c != null && c.gameObject.activeInHierarchy) _hard.Add(CoachLayout.Grow(RectOf(c), 8f));
            if (force || Time.unscaledTime - _lastGather >= 0.5f)
            {
                _lastGather = Time.unscaledTime;
                GatherGameText();
            }
            if (_report != null) FillReport();
        }

        /// <summary>Los textos del juego que están a la vista (letras, rótulos, marcador): el globo y Nubi no los tapan. Se mide el texto de verdad dibujado.</summary>
        private void GatherGameText()
        {
            _soft.Clear();
            _textRaw.Clear();
            _controls.Clear();
            var scope = _root.parent as RectTransform;
            if (scope == null) return;
            scope.GetComponentsInChildren(false, _textBuf);
            foreach (var t in _textBuf)
            {
                if (t == null || !t.enabled || t.transform.IsChildOf(_root) || string.IsNullOrWhiteSpace(t.text)) continue;
                if (t.color.a * t.canvasRenderer.GetInheritedAlpha() < 0.08f) continue;
                if (!TextRect(t, out var r)) continue;
                bool control = false;     // los controles del tutorial (rótulo y «Saltar tutorial») viven bajo «TutorialPractice»
                for (var p = t.transform.parent; p != null && p != scope; p = p.parent) if (p.name == "TutorialPractice") { control = true; break; }
                if (control) _controls.Add(r); else _textRaw.Add(r);
                var g = CoachLayout.Grow(r, TextAvoid);
                bool inside = false;      // ya iluminado y protegido por el hueco o una zona
                foreach (var h in _hard) if (h.Contains(g.min) && h.Contains(g.max)) { inside = true; break; }
                if (!inside) _soft.Add(g);
            }
        }

        /// <summary>El rectángulo que de verdad ocupan las letras de <paramref name="t"/> (en el espacio del foco).</summary>
        private bool TextRect(Text t, out Rect rect)
        {
            rect = default;
            var gen = t.cachedTextGenerator;
            int n = gen.vertexCount;
            if (n < 4) return false;
            var verts = gen.verts;
            float inv = 1f / Mathf.Max(0.0001f, t.pixelsPerUnit);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            var tr = t.transform;
            for (int i = 0; i < n; i++)
            {
                var p = (Vector2)_root.InverseTransformPoint(tr.TransformPoint((Vector3)(verts[i].position * inv)));
                if (p.x < x0) x0 = p.x;
                if (p.y < y0) y0 = p.y;
                if (p.x > x1) x1 = p.x;
                if (p.y > y1) y1 = p.y;
            }
            if (x1 <= x0 || y1 <= y0) return false;
            rect = Rect.MinMaxRect(x0, y0, x1, y1);
            return true;
        }

        // ------------------------------------------------------------------ dónde va Nubi

        private Vector2 _groupPos;

        private float PlacementOverlap(CoachPlacement p)
        {
            float o = 0f;
            foreach (var h in _hard) o += 3f * (CoachLayout.Overlap(p.Nubi, h) + CoachLayout.Overlap(p.Bubble, h));
            foreach (var s in _soft) o += CoachLayout.Overlap(p.Nubi, s) + CoachLayout.Overlap(p.Bubble, s);
            return o;
        }

        /// <summary>Si algo se movió y ahora Nubi o el globo lo tapan, se busca otro lugar (como mucho cada 0,3 s: Nubi no baila).</summary>
        private bool PlacementIsStale() =>
            _placement != null && Time.unscaledTime - _lastReplace >= 0.3f && (PlacementOverlap(_placement) > 0.5f || _text.cachedTextGenerator.lineCount > _placement.Lines);

        /// <summary>Nubi y su globo van donde no tapen nada (ver <see cref="CoachLayout.Place"/>): el globo crece con el texto, máximo 3 líneas.</summary>
        private void Replace(bool first)
        {
            _lastReplace = Time.unscaledTime;
            bool hasHole = _kind != Kind.Notice && _target != null;
            var p = CoachLayout.Place(_root.rect, hasHole ? _hole.center : (Vector2?)null, _hard, _soft, _text.text, MeasureLive, _prefer);
            if (p == null) return;
            if (!first && _placement != null && PlacementOverlap(_placement) <= p.Overlap + 1f && _text.cachedTextGenerator.lineCount <= _placement.Lines) return;   // no hay nada mejor: Nubi se queda donde está
            _placement = p;
            _left = p.Left;
            var u = Rect.MinMaxRect(Mathf.Min(p.Nubi.xMin, p.Bubble.xMin), Mathf.Min(p.Nubi.yMin, p.Bubble.yMin), Mathf.Max(p.Nubi.xMax, p.Bubble.xMax), Mathf.Max(p.Nubi.yMax, p.Bubble.yMax));
            _groupPos = u.center;
            _group.sizeDelta = u.size;
            _nubi.rectTransform.sizeDelta = new Vector2(p.NubiSize, p.NubiSize);
            _nubi.rectTransform.anchoredPosition = p.Nubi.center - u.center;
            _nubi.rectTransform.localScale = new Vector3(_left ? 1f : -1f, 1f, 1f);
            _bubble.sizeDelta = p.Bubble.size;
            _bubble.anchoredPosition = p.Bubble.center - u.center;
            _text.fontSize = CoachLayout.FontSize;
            if (!p.Clean && !_warned)
            {
                _warned = true;
                Debug.LogWarning("[Coach] sin lugar limpio para «" + _text.text + "»: " + (p.TooLong ? "no cabe en " + CoachLayout.MaxLines + " líneas; " : "") + "tapa " + Mathf.RoundToInt(p.Overlap) + " u²");
            }
        }

        /// <summary>Mide el texto con el MISMO <c>Text</c> que lo dibuja (su escala de lienzo y su fuente en este teléfono) y se queda con lo peor entre eso y la medida de fábrica
        /// (<see cref="CoachText.Measure"/>, que ya cubre varias escalas): así el globo nunca se calcula más chico que lo que de verdad ocupa el texto.</summary>
        private TextMeasure MeasureLive(string text, float width)
        {
            var offline = CoachText.Measure(text, width);
            if (_text == null || !_text.isActiveAndEnabled || string.IsNullOrEmpty(text)) return offline;
            var settings = _text.GetGenerationSettings(new Vector2(Mathf.Max(1f, width), 0f));
            settings.horizontalOverflow = HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            if (!_measureGen.Populate(text, settings)) _measureGen.Populate(text, settings);
            float ppu = Mathf.Max(0.01f, _text.pixelsPerUnit);
            int lines = _measureGen.lineCount;
            float h = _measureGen.GetPreferredHeight(text, settings) / ppu;
            return new TextMeasure(Mathf.Max(lines, offline.Lines), Mathf.Max(h, offline.Height));
        }

        private void FillReport()
        {
            var r = _report;
            r.Screen = _root.rect;
            r.HasHole = _kind != Kind.Notice && _target != null;
            r.Hole = r.HasHole ? _hole : default;
            var keep = new List<Rect>();
            for (int i = r.HasHole ? 1 : 0; i < _lit.Count; i++) keep.Add(_lit[i]);
            if (_kind == Kind.Notice && _keep != null) foreach (var k in _keep) if (k != null) keep.Add(k());
            r.Keep = keep.ToArray();
            r.GameText = _soft.ToArray();
            r.Finger = r.HasHole && _kind == Kind.Touch ? FingerRect(_hole) : default;
            if (_placement != null)
            {
                r.Nubi = _placement.Nubi;
                r.Bubble = _placement.Bubble;
                r.Lines = _placement.Lines;
                r.Overlap = PlacementOverlap(_placement);
                r.TooLong = _placement.TooLong;
                r.Clean = r.Overlap <= 0.5f && !r.TooLong;
            }
            r.ActualLines = _text.cachedTextGenerator.lineCount;
            // los controles del tutorial («Saltar tutorial», el rótulo) tampoco deben quedar sobre el hueco, las zonas ni los textos del juego
            float clash = 0f;
            foreach (var c in _controls)
            {
                foreach (var l in _lit) clash += CoachLayout.Overlap(c, l);
                foreach (var t in _textRaw) clash += CoachLayout.Overlap(c, t);
            }
            r.ControlClash = clash;
            var ctl = new List<Rect>();
            foreach (var c in new[] { ControlSkip, ControlBadge }) if (c != null && c.gameObject.activeInHierarchy) ctl.Add(RectOf(c));
            r.Controls = ctl.ToArray();
        }

        private void Record()
        {
            if (_report == null) return;
            FillReport();
            AuditSteps.Add(_report);
            _report = null;
        }

        // ------------------------------------------------------------------ dibujo

        private static void Box(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.sizeDelta = new Vector2(Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
            rt.anchoredPosition = new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f);
        }

        private static bool Same(List<Rect> a, List<Rect> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (Mathf.Abs(a[i].xMin - b[i].xMin) > 0.5f || Mathf.Abs(a[i].yMin - b[i].yMin) > 0.5f || Mathf.Abs(a[i].xMax - b[i].xMax) > 0.5f || Mathf.Abs(a[i].yMax - b[i].yMax) > 0.5f) return false;
            return true;
        }

        private readonly List<Rect> _litShown = new List<Rect>();

        private void UpdateLayout(float dt)
        {
            bool decorative = Motion.Decorative;
            float since = Time.unscaledTime - _shownAt;
            float fade = Mathf.Clamp01(since / (decorative ? 0.22f : Motion.FadeSeconds));
            _rootGroup.alpha = fade;

            if (_kind != Kind.Notice)
            {
                var pr = _root.rect;
                // el velo: toda el área menos lo iluminado (el hueco y las zonas protegidas)
                if (!Same(_lit, _litShown))
                {
                    _litShown.Clear();
                    _litShown.AddRange(_lit);
                    _veilRects = CoachLayout.Subtract(pr, _lit);
                    for (int i = 0; i < _veil.Length; i++)
                    {
                        bool on = i < _veilRects.Count;
                        _veil[i].gameObject.SetActive(on);
                        if (on) Box(_veil[i].rectTransform, _veilRects[i].xMin, _veilRects[i].yMin, _veilRects[i].xMax, _veilRects[i].yMax);
                    }
                    if (_veilRects.Count > _veil.Length) Debug.LogWarning("[Coach] el velo necesita " + _veilRects.Count + " piezas y solo hay " + _veil.Length);
                    // esquinas que redondean cada zona iluminada (un cuarto de círculo oscuro en cada una; se omite si cae sobre otra zona iluminada)
                    for (int i = 0; i < MaxLit; i++)
                    {
                        bool has = i < _lit.Count;
                        var h = has ? _lit[i] : default;
                        float r = !has ? 0f : (i == 0 && _target != null) ? (_circle ? Mathf.Min(h.width, h.height) / 2f : Mathf.Min(54f, Mathf.Min(h.width, h.height) / 2f)) : Mathf.Min(24f, Mathf.Min(h.width, h.height) / 2f);
                        for (int c = 0; c < 4; c++)
                        {
                            var img = _corners[i * 4 + c];
                            Vector2 center = !has ? Vector2.zero : new Vector2(c == 0 || c == 3 ? h.xMin + r / 2f : h.xMax - r / 2f, c == 0 || c == 1 ? h.yMax - r / 2f : h.yMin + r / 2f);
                            bool covered = false;
                            if (has) for (int j = 0; j < _lit.Count && !covered; j++)
                                    if (j != i && CoachLayout.Overlap(new Rect(center - new Vector2(r, r) / 2f, new Vector2(r, r)), _lit[j]) > 0.5f) covered = true;
                            img.gameObject.SetActive(has && r > 1f && !covered);
                            img.rectTransform.sizeDelta = new Vector2(r, r);
                            img.rectTransform.anchoredPosition = center;
                        }
                    }
                }
                var hole = _hole;
                float rr = _circle ? Mathf.Min(hole.width, hole.height) / 2f : Mathf.Min(54f, Mathf.Min(hole.width, hole.height) / 2f);
                // aro celeste que late
                float pulse = decorative ? 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 5.2f) : 1f;
                float a = decorative ? 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 5.2f) : 1f;
                if (_circle)
                {
                    float d = Mathf.Max(hole.width, hole.height) / 0.96f;
                    _ring.rectTransform.sizeDelta = new Vector2(d, d) * pulse;
                    _ring.rectTransform.anchoredPosition = hole.center;
                    _ring.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, a);
                }
                else
                {
                    _frame.rectTransform.sizeDelta = new Vector2(hole.width + 14f, hole.height + 14f) * pulse;
                    _frame.rectTransform.anchoredPosition = hole.center;
                    SetFrameScale(32f / Mathf.Max(8f, rr));
                    _frame.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, a);
                }
                if (_finger.gameObject.activeSelf)
                {
                    float bob = decorative ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)) : 0.5f;
                    _finger.rectTransform.anchoredPosition = FingerCenter(hole, bob);
                }
            }

            // Nubi: desliza desde su costado (o aparece con un fundido) y el globo salta cuando insiste
            float slide = decorative ? 1f - UiFx.EaseOutCubic(Mathf.Clamp01(since / 0.32f)) : 0f;
            _groupGroup.alpha = decorative ? 1f : fade;
            _group.anchoredPosition = _groupPos + new Vector2((_left ? -1f : 1f) * slide * 520f, 0f);
            float pop = decorative ? Mathf.Lerp(1.1f, 1f, UiFx.EaseOutCubic(Mathf.Clamp01((Time.unscaledTime - _popAt) / 0.3f))) : 1f;
            _bubble.localScale = Vector3.one * (Time.unscaledTime - _popAt < 0.3f ? pop : 1f);
            if (DebugOverlay) DrawDebug();
            else if (_dbgRoot != null && _dbgRoot.gameObject.activeSelf) _dbgRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ vista de diagnóstico (solo depuración)

        private void Dbg(int i, Rect r, Color c)
        {
            while (_dbgBoxes.Count <= i)
            {
                var go = new GameObject("Box" + _dbgBoxes.Count, typeof(RectTransform));
                go.transform.SetParent(_dbgRoot, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                var im = go.AddComponent<Image>();
                im.raycastTarget = false;
                _dbgBoxes.Add(im);
            }
            var img = _dbgBoxes[i];
            bool on = r.width > 1f && r.height > 1f;
            img.gameObject.SetActive(on);
            if (!on) return;
            img.color = c;
            Box(img.rectTransform, r.xMin, r.yMin, r.xMax, r.yMax);
        }

        /// <summary>Dibuja encima, en colores: el hueco (rojo), las zonas iluminadas (amarillo), el globo (verde), Nubi (azul), el dedo (magenta), «Saltar tutorial» y el rótulo (blanco) y el último toque (celeste).
        /// Arriba va la pantalla, la escala del lienzo y el área del foco. Con una captura de esto se ve por qué un toque no entra.</summary>
        private void DrawDebug()
        {
            if (_dbgRoot == null)
            {
                var go = new GameObject("DebugOverlay", typeof(RectTransform));
                go.transform.SetParent(_root, false);
                _dbgRoot = (RectTransform)go.transform;
                Stretch(_dbgRoot);
                _dbgText = MakeText(_dbgRoot, "Info", 30, TextAnchor.UpperLeft, Color.white, 0f, 0f);
                _dbgText.rectTransform.offsetMin = new Vector2(20f, 20f);
                _dbgText.rectTransform.offsetMax = new Vector2(-20f, -20f);
                _dbgText.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            _dbgRoot.gameObject.SetActive(true);
            _dbgRoot.SetAsLastSibling();
            int n = 0;
            if (_kind != Kind.Notice && _target != null) Dbg(n++, _hole, new Color(1f, 0.1f, 0.1f, 0.30f));
            for (int i = _kind != Kind.Notice && _target != null ? 1 : 0; i < _lit.Count; i++) Dbg(n++, _lit[i], new Color(1f, 0.9f, 0.1f, 0.25f));
            if (_placement != null)
            {
                Dbg(n++, _placement.Bubble, new Color(0.1f, 1f, 0.2f, 0.25f));
                Dbg(n++, _placement.Nubi, new Color(0.2f, 0.4f, 1f, 0.25f));
            }
            if (_kind == Kind.Touch && _finger.gameObject.activeSelf) Dbg(n++, FingerRect(_hole), new Color(1f, 0.1f, 1f, 0.25f));
            if (ControlSkip != null && ControlSkip.gameObject.activeInHierarchy) Dbg(n++, RectOf(ControlSkip), new Color(1f, 1f, 1f, 0.30f));
            if (ControlBadge != null && ControlBadge.gameObject.activeInHierarchy) Dbg(n++, RectOf(ControlBadge), new Color(1f, 1f, 1f, 0.30f));
            if (_hasTouch) Dbg(n++, new Rect(_lastTouchLocal - new Vector2(24f, 24f), new Vector2(48f, 48f)), new Color(0f, 1f, 1f, 0.9f));
            for (int i = n; i < _dbgBoxes.Count; i++) _dbgBoxes[i].gameObject.SetActive(false);
            var canvas = _root.GetComponentInParent<Canvas>();
            var sa = Screen.safeArea;
            _dbgText.text = $"{Screen.width}x{Screen.height} · escala {(canvas != null ? canvas.scaleFactor : 0f):0.00} · foco {N(_root.rect.width)}x{N(_root.rect.height)} · zona segura {N(sa.x)},{N(sa.y)} {N(sa.width)}x{N(sa.height)} · paso {_kind} · toques fuera {_misses} · {_waited:0.0} s";
        }

        private static readonly System.Reflection.PropertyInfo PixelsMultiplier = typeof(Image).GetProperty("pixelsPerUnitMultiplier");

        /// <summary>Hace que la esquina del marco mida lo mismo que la del hueco (la propiedad existe en Unity 6; por reflexión para compilar también contra el UI viejo de la verificación en la nube).</summary>
        private void SetFrameScale(float multiplier)
        {
            if (PixelsMultiplier != null) PixelsMultiplier.SetValue(_frame, multiplier);
        }
    }

    /// <summary>Arte del foco, dibujado por código (sin assets): las esquinas oscuras que redondean el hueco y el marco celeste hueco (de 9 cortes).</summary>
    public static class CoachSprites
    {
        private static Sprite _corner, _frame;

        /// <summary>Un cuadrado oscuro con un cuarto de círculo transparente: dos de sus lados (arriba e izquierda) son la esquina del hueco. Se rota para las otras tres.</summary>
        public static Sprite Corner()
        {
            if (_corner != null) return _corner;
            const int size = 64;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // el centro del arco está en la esquina de abajo a la derecha del cuadrado (fila 0 = abajo)
                    float d = Mathf.Sqrt((x + 0.5f - size) * (x + 0.5f - size) + (y + 0.5f) * (y + 0.5f));
                    float a = Mathf.Clamp01((d - (size - 0.75f)) / 1.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            return _corner = ToSprite(px, size, Vector4.zero);
        }

        /// <summary>Marco hueco de esquinas redondas (radio 32 px, trazo suave): se estira con <see cref="Image.Type.Sliced"/>.</summary>
        public static Sprite Frame()
        {
            if (_frame != null) return _frame;
            const int size = 96;
            const float radius = 32f, half = size / 2f, thick = 5f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half) - (half - radius), dy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float dist = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;   // <0 adentro
                    float inner = Mathf.Clamp01((dist + thick) / 1.5f);       // 0 adentro del trazo → 1
                    float outer = Mathf.Clamp01(1f - (dist - 1f) / 1.5f);
                    float glow = Mathf.Clamp01(1f - Mathf.Max(0f, dist - 1f) / 7f) * 0.35f * (dist > 0f ? 1f : 0f);
                    float a = Mathf.Clamp01(Mathf.Min(inner, outer) + glow);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            return _frame = ToSprite(px, size, new Vector4(36f, 36f, 36f, 36f));
        }

        private static Sprite ToSprite(Color32[] px, int size, Vector4 border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
