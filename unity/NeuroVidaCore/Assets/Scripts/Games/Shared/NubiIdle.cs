using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El reposo de Nubi (Tarea 148, P1 de <c>auditoria-fable/propuestas-diseno-visual.md</c> §2.3), igual que en la app (<c>ui/components/NubiIdle.kt</c>): <b>flota</b> y <b>parpadea</b> aunque no pase nada, para que sea alguien que está ahí y no una ilustración pegada.
    /// <list type="bullet">
    /// <item>Flota: sube y baja ±<see cref="FloatDp"/> dp en un seno de <see cref="FloatSeconds"/> s; su sombra (la elipse fija del suelo) se achica y se aclara en el punto alto.</item>
    /// <item>Parpadea: un fundido de <see cref="BlinkSeconds"/> s a la imagen «descansa» (ojos cerrados) y de vuelta, con una espera al azar de 4 a 7 s entre uno y otro y un parpadeo doble cada <see cref="DoubleEvery"/> veces.</item>
    /// </list>
    /// Es decorativo e igual en toda partida, buena o mala: nada reacciona al desempeño (regla de patentes). Con «quitar animaciones» (<see cref="Motion.Decorative"/>) Nubi queda quieta y con los ojos abiertos.
    /// Esta clase es la lógica pura (con pruebas); <see cref="NubiFigure"/> la dibuja.
    /// </summary>
    public sealed class NubiIdle
    {
        public const float FloatDp = 6f;
        public const float FloatSeconds = 4.5f;
        public const float BlinkSeconds = 0.12f;
        public const float MinWaitSeconds = 4f;
        public const float MaxWaitSeconds = 7f;
        /// <summary>Entre los dos parpadeos de uno doble.</summary>
        public const float DoubleGapSeconds = 0.16f;
        public const int DoubleEvery = 5;

        /// <summary>El desplazamiento vertical (dp, positivo = arriba) a los <paramref name="seconds"/> s: un seno de ±<see cref="FloatDp"/> con período <see cref="FloatSeconds"/>.</summary>
        public static float FloatDpAt(float seconds) => FloatDp * Mathf.Sin(2f * Mathf.PI * (seconds % FloatSeconds) / FloatSeconds);

        /// <summary>Cuánto espera antes del próximo parpadeo: entre <see cref="MinWaitSeconds"/> y <see cref="MaxWaitSeconds"/>.</summary>
        public static float WaitSeconds(System.Random rng) => MinWaitSeconds + (float)rng.NextDouble() * (MaxWaitSeconds - MinWaitSeconds);

        /// <summary>¿El parpadeo número <paramref name="n"/> (1, 2, 3…) es doble? Uno de cada <see cref="DoubleEvery"/>.</summary>
        public static bool IsDouble(int n) => n > 0 && n % DoubleEvery == 0;

        /// <summary>De 0 (en el punto más bajo) a 1 (en el más alto): para la sombra.</summary>
        public static float Height01(float floatDp) => Mathf.Clamp01((floatDp + FloatDp) / (2f * FloatDp));

        /// <summary>La sombra en el punto alto: 0,85 de su ancho y 0,7 de su opacidad (como la sombra de la isla con movimiento).</summary>
        public static float ShadowSize(float height01) => 1f - 0.15f * height01;
        public static float ShadowAlpha(float height01) => 1f - 0.3f * height01;

        private readonly System.Random _rng;
        private float _nextAt, _start;
        private bool _blinking, _second;
        private int _count;

        /// <summary>Cuánto están cerrados los ojos: 0 (abiertos) a 1 (cerrados).</summary>
        public float Closed { get; private set; }

        /// <summary>Cuántos parpadeos (de a uno; el doble cuenta como uno) lleva.</summary>
        public int Blinks => _count;

        /// <param name="seed">0 = al azar de verdad; otro número, repetible (las pruebas).</param>
        public NubiIdle(int seed = 0, float now = 0f)
        {
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
            Reset(now);
        }

        /// <summary>Ojos abiertos y la espera hasta el próximo parpadeo empieza de nuevo (al aparecer Nubi y al volver de «quitar animaciones»).</summary>
        public void Reset(float now)
        {
            Closed = 0f;
            _blinking = false;
            _second = false;
            _nextAt = now + WaitSeconds(_rng);
        }

        /// <summary>Un cuadro: <paramref name="now"/> en segundos (siempre crece). Deja en <see cref="Closed"/> cuánto están cerrados los ojos.</summary>
        public void Tick(float now)
        {
            if (!_blinking)
            {
                Closed = 0f;
                if (now < _nextAt) return;
                _count++;
                _blinking = true;
                _start = now;
                _second = IsDouble(_count);
            }
            while (true)
            {
                float p = (now - _start) / BlinkSeconds;           // 0 a 1: cierra; 1 a 2: abre
                if (p < 0f) { Closed = 0f; return; }               // la pausa entre los dos parpadeos del doble
                if (p < 1f) { Closed = p; return; }
                if (p < 2f) { Closed = 2f - p; return; }
                if (_second)
                {
                    _second = false;
                    _start += 2f * BlinkSeconds + DoubleGapSeconds;
                    continue;
                }
                _blinking = false;
                Closed = 0f;
                _nextAt = now + WaitSeconds(_rng);
                return;
            }
        }
    }

    /// <summary>
    /// La Nubi 3D de plasticina viva (Tarea 148): el cuerpo con los ojos abiertos, la misma imagen con los ojos cerrados que se funde encima al parpadear, y UNA sombra fija en el suelo (la de la isla: <c>nubi_sombra</c>; antes iba horneada en <c>nubi_3d</c> y habría flotado con ella).
    /// Se cuelga de un <see cref="Image"/> «portador» (el que ya mide y coloca quien muestra a Nubi: <see cref="NubiCoach"/>, la tarjeta de entrada del tutorial): lo apaga y dibuja adentro. Sin el atlas «nubi» no hace nada y el portador sigue con la Nubi plana de siempre.
    /// El portador mide su ancho (= el del cuerpo) × <see cref="BoxRatio"/> de alto: cuerpo y aire para la sombra.
    /// </summary>
    public sealed class NubiFigure : MonoBehaviour
    {
        /// <summary>Cuánto aire (fracción del ancho del cuerpo) deja la figura debajo del cuerpo para su sombra: lo mismo que <c>NUBI_SHADOW_ROOM</c> de la app.</summary>
        public const float ShadowRoom = 0.1f;
        /// <summary>La elipse de la sombra mide 1,29 veces el ancho del cuerpo (la de la app: radio 0,6 y desenfoque 0,045 por lado).</summary>
        public const float ShadowWidthFactor = 1.29f;
        private const float ShadowBelowBody = 0.042f;       // el centro de la sombra cae 0,042 del ancho por debajo de la base del cuerpo (como en la app)

        private Image _holder, _body, _closed, _shadow;
        private RectTransform _bodyRt, _shadowRt;
        private float _unitsPerDp = 3f;
        private NubiIdle _idle;
        private bool _wasAlive;
        private float _floatDp;

        /// <summary>El alto del portador sobre su ancho: el cuerpo (su proporción) más el aire de la sombra; 1 si no hay Nubi de plasticina (la plana es cuadrada).</summary>
        public static float BoxRatio
        {
            get
            {
                var s = NubiTeacherSprite.IsPlastic ? NubiTeacherSprite.Get() : null;
                return s == null ? 1f : s.rect.height / s.rect.width + ShadowRoom;
            }
        }

        /// <summary>true si hay Nubi de plasticina con sus ojos cerrados y su sombra (el atlas «nubi» completo).</summary>
        public static bool Available => NubiTeacherSprite.IsPlastic && NubiTeacherSprite.GetClosed() != null && NubiTeacherSprite.GetShadow() != null;

        /// <summary>Cuelga la figura del <paramref name="holder"/> (null si no hay atlas: el portador sigue como estaba). Se puede llamar más de una vez.</summary>
        public static NubiFigure Attach(Image holder, float unitsPerDp = 3f)
        {
            if (holder == null || !Available) return null;
            var figure = holder.GetComponent<NubiFigure>();
            if (figure == null) figure = holder.gameObject.AddComponent<NubiFigure>();
            figure.Build(holder, unitsPerDp);
            return figure;
        }

        private static Image Layer(RectTransform parent, string name, Sprite sprite, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            else rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private void Build(Image holder, float unitsPerDp)
        {
            _unitsPerDp = unitsPerDp;
            if (_holder != null) return;
            _holder = holder;
            _holder.enabled = false;                          // el portador deja de dibujar la Nubi plana; lo mide y lo coloca quien lo usa
            var root = _holder.rectTransform;
            _shadow = Layer(root, "Shadow", NubiTeacherSprite.GetShadow(), false);
            _shadowRt = _shadow.rectTransform;
            _body = Layer(root, "Body", NubiTeacherSprite.Get(), false);
            _bodyRt = _body.rectTransform;
            _closed = Layer(_bodyRt, "EyesClosed", NubiTeacherSprite.GetClosed(), true);      // encima del cuerpo, del mismo tamaño: se funde por alfa
            _closed.color = new Color(1f, 1f, 1f, 0f);
            _idle = new NubiIdle(0, Time.unscaledTime);
            _wasAlive = true;
            Layout();
        }

        /// <summary>Cuánto están cerrados los ojos (0 a 1) y cuánto sube Nubi (dp): para las pruebas.</summary>
        public float EyesClosed => _idle == null ? 0f : _idle.Closed;
        public float FloatDp => _floatDp;
        public RectTransform Body => _bodyRt;
        public RectTransform Shadow => _shadowRt;
        public Image EyesClosedImage => _closed;
        public Image ShadowImage => _shadow;

        /// <summary>Vuelve a empezar la espera del parpadeo desde <paramref name="now"/> (las pruebas fijan su propio reloj).</summary>
        public void Restart(float now)
        {
            if (_idle == null) return;
            _idle.Reset(now);
            _wasAlive = true;
        }

        private void LateUpdate() => Step(Time.unscaledTime);

        /// <summary>Un cuadro en el segundo <paramref name="now"/> (público para las pruebas EditMode, que no tienen cuadros).</summary>
        public void Step(float now)
        {
            if (_idle == null) return;
            bool alive = Motion.Decorative;
            if (!alive)
            {
                _floatDp = 0f;
                _closed.color = new Color(1f, 1f, 1f, 0f);
                _wasAlive = false;
                Layout();
                return;
            }
            if (!_wasAlive) { _idle.Reset(now); _wasAlive = true; }
            _floatDp = NubiIdle.FloatDpAt(now);
            _idle.Tick(now);
            _closed.color = new Color(1f, 1f, 1f, _idle.Closed);
            Layout();
        }

        /// <summary>Coloca el cuerpo (con lo que flota), la sombra (fija, más chica y clara cuanto más sube Nubi) y los tamaños, con la caja actual del portador.</summary>
        private void Layout()
        {
            var box = _holder.rectTransform.rect;
            var body = NubiTeacherSprite.Get().rect;
            float bodyW = Mathf.Min(box.width, box.height / BoxRatio);
            float bodyH = bodyW * body.height / body.width;
            float figureH = bodyH + ShadowRoom * bodyW;
            float top = figureH / 2f;
            _bodyRt.sizeDelta = new Vector2(bodyW, bodyH);
            _bodyRt.anchoredPosition = new Vector2(0f, top - bodyH / 2f + _floatDp * _unitsPerDp);
            float h = NubiIdle.Height01(_floatDp);
            var shadow = NubiTeacherSprite.GetShadow().rect;
            float sw = ShadowWidthFactor * bodyW * NubiIdle.ShadowSize(h);
            _shadowRt.sizeDelta = new Vector2(sw, sw * shadow.height / shadow.width);
            _shadowRt.anchoredPosition = new Vector2(bodyW * 0.02f, top - bodyH - ShadowBelowBody * bodyW);
            _shadow.color = new Color(1f, 1f, 1f, NubiIdle.ShadowAlpha(h));
        }
    }
}
