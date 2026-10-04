using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RingSprite / RadialGlowSprite
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// «Nubi entrenadora» (Ricardo, 4-oct): el tutorial de todos los juegos. La pantalla se oscurece con un velo gris y SOLO se ilumina lo que hay que tocar (un hueco redondeado, o
    /// circular, con un aro celeste que late); Nubi entra deslizándose por un costado y habla en un globo claro de 1-2 líneas que nunca tapa el hueco. Tres formas:
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

        private const float NubiSize = 300f;          // unidades del lienzo (100 dp)
        private const float BubbleHeight = 200f;
        private const float Margin = 30f;
        private const float HudClear = 330f;          // lo que ocupan el marcador de arriba y el rótulo «Práctica: no cuenta»
        private const float BottomClear = 190f;       // lo que ocupa «Saltar tutorial» abajo
        private const float InsistSeconds = 5f;
        private const float HolePad = 12f;

        private static readonly Color VeilColor = new Color(0.03f, 0.04f, 0.11f, 0.72f);
        private static readonly Color BubbleColor = new Color(1f, 0.965f, 0.9f, 1f);

        private RectTransform _root, _group, _bubble;
        private CanvasGroup _rootGroup, _groupGroup;
        private readonly Image[] _strips = new Image[4];
        private readonly Image[] _corners = new Image[4];
        private Image _frame, _ring, _nubi, _finger;
        private Text _text;
        private Func<Vector2, bool> _isSkip;
        private Func<bool> _aborted;

        private Kind _kind = Kind.None;
        private Func<Rect> _target;
        private Func<bool> _done;
        private bool _circle, _freeze, _frozen, _left;
        private float _waited, _noticeSeconds, _maxSeconds, _shownAt, _lastInsist, _popAt = -10f;
        private Rect _hole;

        /// <summary>true mientras hay un foco (Tocar, Mirar o un aviso) a la vista.</summary>
        public bool Active => _kind != Kind.None;

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

            for (int i = 0; i < 4; i++) _strips[i] = Piece("Veil" + i, null);
            for (int i = 0; i < 4; i++)
            {
                _corners[i] = Piece("Corner" + i, CoachSprites.Corner());
                _corners[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, i == 0 ? 0f : i == 1 ? -90f : i == 2 ? 180f : 90f);   // arriba-izq, arriba-der, abajo-der, abajo-izq
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
            nr.sizeDelta = new Vector2(NubiSize, NubiSize);
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
            _text = MakeText(_bubble, "Text", 54, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            _text.rectTransform.offsetMin = new Vector2(26f, 14f);
            _text.rectTransform.offsetMax = new Vector2(-26f, -14f);
            BestFit(_text, 45);

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

        private bool InsideHole(Vector2 screenPos)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPos, null, out var local)) return false;
            return _hole.Contains(local);
        }

        /// <summary>true si un toque en <paramref name="screenPos"/> NO debe llegar al juego: hay un foco de Tocar y el toque cae fuera del hueco (y no es «Saltar tutorial»).</summary>
        public bool Blocks(Vector2 screenPos) => _kind == Kind.Touch && !InsideHole(screenPos) && !(_isSkip != null && _isSkip(screenPos));

        // ------------------------------------------------------------------ las tres formas

        /// <summary>Foco de TOCAR: congela el juego (si <paramref name="freeze"/>) y espera el toque dentro del hueco; ese toque llega al juego. Devuelve cuando se cierra.</summary>
        public IEnumerator Touch(Func<Rect> target, string text, bool circle = false, bool freeze = true)
        {
            Begin(Kind.Touch, target, text, circle, freeze, null, 0f);
            while (_kind != Kind.None) { if (_aborted != null && _aborted()) { Close(); break; } yield return null; }
        }

        /// <summary>Foco de MIRAR: sin congelar, sobre algo que se mueve; se cierra cuando <paramref name="done"/> da true (o a los <paramref name="maxSeconds"/>).</summary>
        public IEnumerator Watch(Func<Rect> target, string text, Func<bool> done, float maxSeconds = 12f, bool circle = false)
        {
            Begin(Kind.Watch, target, text, circle, false, done, maxSeconds);
            while (_kind != Kind.None) { if (_aborted != null && _aborted()) { Close(); break; } yield return null; }
        }

        /// <summary>Aviso breve: el globo de Nubi sin velo, que se va solo.</summary>
        public IEnumerator Notice(string text, float seconds = 1.8f)
        {
            Begin(Kind.Notice, null, text, false, false, null, seconds);
            while (_kind != Kind.None) { if (_aborted != null && _aborted()) { Close(); break; } yield return null; }
        }

        /// <summary>Cierra lo que haya (y reanuda el juego si estaba congelado).</summary>
        public void Hide() => Close();

        private void Begin(Kind kind, Func<Rect> target, string text, bool circle, bool freeze, Func<bool> done, float seconds)
        {
            Close();
            _kind = kind;
            _target = target;
            _circle = circle;
            _freeze = freeze;
            _done = done;
            _maxSeconds = kind == Kind.Watch ? seconds : 0f;
            _noticeSeconds = kind == Kind.Notice ? seconds : 0f;
            _waited = 0f;
            _shownAt = Time.unscaledTime;
            _lastInsist = 0f;
            _popAt = -10f;
            _text.text = text;
            if (_nubi.sprite == null) _nubi.sprite = NubiTeacherSprite.Get();
            _root.gameObject.SetActive(true);
            bool veil = kind != Kind.Notice;
            foreach (var s in _strips) { s.gameObject.SetActive(veil); s.raycastTarget = kind == Kind.Touch; }
            foreach (var c in _corners) c.gameObject.SetActive(veil);
            _frame.gameObject.SetActive(veil && !circle);
            _ring.gameObject.SetActive(veil && circle);
            _finger.gameObject.SetActive(false);
            if (target != null) _hole = Inflate(target());
            PlaceNubi();
            if (kind == Kind.Touch && freeze && !PauseMenu.Open)
            {
                GameClock.Pause();
                _frozen = true;
            }
            UpdateLayout(0f);
        }

        private void Close()
        {
            if (_kind == Kind.None) { if (_frozen) Unfreeze(); return; }
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
            if (_target != null) _hole = Inflate(_target());
            UpdateLayout(dt);

            switch (_kind)
            {
                case Kind.Touch:
                    // si la pausa del menú se cerró mientras el foco seguía abierto, el juego se vuelve a congelar
                    if (_freeze && _frozen && !GameClock.Paused && !PauseMenu.Open) GameClock.Pause();
                    if (!PauseMenu.Open && GuidedTutorial.TryPress(out Vector2 pos) && _waited > 0.15f && !(_isSkip != null && _isSkip(pos)) && InsideHole(pos))
                    {
                        Close();     // el juego recibe este mismo toque (este componente corre antes que él)
                        return;
                    }
#if UNITY_EDITOR
                    if (GuidedTutorial.EditorAutoContinue && _waited > 1.2f) { Close(); return; }
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

        private Rect Inflate(Rect r)
        {
            var pr = _root.rect;
            var o = Rect.MinMaxRect(r.xMin - HolePad, r.yMin - HolePad, r.xMax + HolePad, r.yMax + HolePad);
            if (_circle)
            {
                float d = Mathf.Max(o.width, o.height);
                o = new Rect(o.center - new Vector2(d, d) / 2f, new Vector2(d, d));
            }
            return Rect.MinMaxRect(Mathf.Max(o.xMin, pr.xMin), Mathf.Max(o.yMin, pr.yMin), Mathf.Min(o.xMax, pr.xMax), Mathf.Min(o.yMax, pr.yMax));
        }

        private static void Box(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.sizeDelta = new Vector2(Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
            rt.anchoredPosition = new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f);
        }

        private void UpdateLayout(float dt)
        {
            bool decorative = Motion.Decorative;
            float since = Time.unscaledTime - _shownAt;
            float fade = Mathf.Clamp01(since / (decorative ? 0.22f : Motion.FadeSeconds));
            _rootGroup.alpha = fade;

            if (_kind != Kind.Notice)
            {
                var pr = _root.rect;
                var h = _hole;
                float r = _circle ? Mathf.Min(h.width, h.height) / 2f : Mathf.Min(54f, Mathf.Min(h.width, h.height) / 2f);
                Box(_strips[0].rectTransform, pr.xMin, h.yMax, pr.xMax, pr.yMax);     // arriba
                Box(_strips[1].rectTransform, pr.xMin, pr.yMin, pr.xMax, h.yMin);     // abajo
                Box(_strips[2].rectTransform, pr.xMin, h.yMin, h.xMin, h.yMax);       // izquierda
                Box(_strips[3].rectTransform, h.xMax, h.yMin, pr.xMax, h.yMax);       // derecha
                // esquinas que redondean el hueco (un cuarto de círculo oscuro en cada una)
                _corners[0].rectTransform.sizeDelta = _corners[1].rectTransform.sizeDelta = _corners[2].rectTransform.sizeDelta = _corners[3].rectTransform.sizeDelta = new Vector2(r, r);
                _corners[0].rectTransform.anchoredPosition = new Vector2(h.xMin + r / 2f, h.yMax - r / 2f);
                _corners[1].rectTransform.anchoredPosition = new Vector2(h.xMax - r / 2f, h.yMax - r / 2f);
                _corners[2].rectTransform.anchoredPosition = new Vector2(h.xMax - r / 2f, h.yMin + r / 2f);
                _corners[3].rectTransform.anchoredPosition = new Vector2(h.xMin + r / 2f, h.yMin + r / 2f);
                // aro celeste que late
                float pulse = decorative ? 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 5.2f) : 1f;
                float a = decorative ? 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 5.2f) : 1f;
                if (_circle)
                {
                    float d = Mathf.Max(h.width, h.height) / 0.96f;
                    _ring.rectTransform.sizeDelta = new Vector2(d, d) * pulse;
                    _ring.rectTransform.anchoredPosition = h.center;
                    _ring.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, a);
                }
                else
                {
                    _frame.rectTransform.sizeDelta = new Vector2(h.width + 14f, h.height + 14f) * pulse;
                    _frame.rectTransform.anchoredPosition = h.center;
                    SetFrameScale(32f / Mathf.Max(8f, r));
                    _frame.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, a);
                }
                if (_finger.gameObject.activeSelf)
                {
                    float bob = decorative ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)) : 0.5f;
                    _finger.rectTransform.anchoredPosition = h.center + new Vector2(0f, -h.height * 0.15f - 150f * (1f - bob));
                }
            }

            // Nubi: desliza desde su costado (o aparece con un fundido) y el globo salta cuando insiste
            float slide = decorative ? 1f - UiFx.EaseOutCubic(Mathf.Clamp01(since / 0.32f)) : 0f;
            _groupGroup.alpha = decorative ? 1f : fade;
            _group.anchoredPosition = _groupPos + new Vector2((_left ? -1f : 1f) * slide * 520f, 0f);
            float pop = decorative ? Mathf.Lerp(1.1f, 1f, UiFx.EaseOutCubic(Mathf.Clamp01((Time.unscaledTime - _popAt) / 0.3f))) : 1f;
            _bubble.localScale = Vector3.one * (Time.unscaledTime - _popAt < 0.3f ? pop : 1f);
        }

        private static readonly System.Reflection.PropertyInfo PixelsMultiplier = typeof(Image).GetProperty("pixelsPerUnitMultiplier");

        /// <summary>Hace que la esquina del marco mida lo mismo que la del hueco (la propiedad existe en Unity 6; por reflexión para compilar también contra el UI viejo de la verificación en la nube).</summary>
        private void SetFrameScale(float multiplier)
        {
            if (PixelsMultiplier != null) PixelsMultiplier.SetValue(_frame, multiplier);
        }

        // ------------------------------------------------------------------ dónde va Nubi

        private Vector2 _groupPos;

        /// <summary>Nubi y su globo van abajo, arriba o al medio, del lado más libre: nunca sobre el hueco.</summary>
        private void PlaceNubi()
        {
            var pr = _root.rect;
            float bubbleW = Mathf.Min(pr.width - NubiSize - 3f * Margin, 720f);
            float groupW = NubiSize + bubbleW + 20f;
            float groupH = Mathf.Max(NubiSize, BubbleHeight);
            bool hasHole = _kind != Kind.Notice && _target != null;
            _left = hasHole ? _hole.center.x > pr.center.x : false;       // Nubi del lado contrario al hueco

            float cx = _left ? pr.xMin + Margin + groupW / 2f : pr.xMax - Margin - groupW / 2f;
            float[] ys = { pr.yMin + BottomClear + groupH / 2f, pr.yMax - HudClear - groupH / 2f, pr.center.y };
            int best = 0;
            float bestOverlap = float.MaxValue;
            for (int i = 0; i < ys.Length; i++)
            {
                var g = new Rect(cx - groupW / 2f, ys[i] - groupH / 2f, groupW, groupH);
                float o = hasHole ? Overlap(g, Rect.MinMaxRect(_hole.xMin - 24f, _hole.yMin - 24f, _hole.xMax + 24f, _hole.yMax + 24f)) : 0f;
                if (o < bestOverlap) { bestOverlap = o; best = i; }
                if (o <= 0f) break;
            }
            _groupPos = new Vector2(cx, ys[best]);
            _group.sizeDelta = new Vector2(groupW, groupH);
            // adentro del grupo: Nubi en el borde y el globo hacia el centro de la pantalla
            float nubiX = _left ? -groupW / 2f + NubiSize / 2f : groupW / 2f - NubiSize / 2f;
            float bubbleX = _left ? groupW / 2f - bubbleW / 2f : -groupW / 2f + bubbleW / 2f;
            _nubi.rectTransform.anchoredPosition = new Vector2(nubiX, 0f);
            _nubi.rectTransform.localScale = new Vector3(_left ? 1f : -1f, 1f, 1f);
            _bubble.sizeDelta = new Vector2(bubbleW, BubbleHeight);
            _bubble.anchoredPosition = new Vector2(bubbleX, 0f);
            _text.fontSize = 54;
            _text.resizeTextMaxSize = 54;
        }

        private static float Overlap(Rect a, Rect b)
        {
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return w > 0f && h > 0f ? w * h : 0f;
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
