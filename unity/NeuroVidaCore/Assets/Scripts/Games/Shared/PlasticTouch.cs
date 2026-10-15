using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El estado PRESIONADO de las losas de plasticina en los juegos que leen <c>Input</c> directo (no tienen <c>Button</c>, así que <see cref="PlasticPress"/> no sirve). Regla del 14-oct: todo objeto de plasticina que se toca tiene su
    /// imagen presionada (más baja, más oscura, con la huella de un dedo; las losas del kit ya la traen).
    /// <para>Se llama a <see cref="Press"/> cuando el juego ACEPTA el toque (su propia lectura del dedo decide cuál objeto se tocó): la losa cambia a su imagen presionada mientras el dedo sigue sobre el rectángulo y al menos
    /// <see cref="MinSeconds"/> (un toque rápido también se ve), y al soltar vuelve con un rebote chico de escala. Con «quitar animaciones» solo cambia la imagen (sin rebote). La vibración y los sonidos no cambian: eso lo sigue haciendo el juego.</para>
    /// <para>Dos modos. (1) <see cref="On(GameObject, Image, SlabSize, SlabTone, RectTransform[])"/>: el componente es dueño de la losa (la del <see cref="PlasticSlab.TryStyle"/>) y la cambia solo; <paramref name="movers"/> (el texto, el ícono) bajan lo que baja la losa.
    /// Si el juego cambia el color de la losa, lo hace con <see cref="SetTone"/>. (2) <see cref="On(GameObject, SlabSize, SlabTone)"/>: solo lleva la cuenta; el juego, que ya redibuja la losa en cada cuadro (elegida, lograda…), lee <see cref="Down"/> y
    /// <see cref="Drop"/> al elegir su imagen y su texto.</para>
    /// </summary>
    public sealed class PlasticTouch : MonoBehaviour
    {
        /// <summary>Lo mínimo que se ve presionada la losa (segundos de juego): un toque rápido también se tiene que ver.</summary>
        public const float MinSeconds = 0.08f;

        /// <summary>Lo que dura el rebote chico de escala al soltar (solo con movimiento).</summary>
        public const float BounceSeconds = 0.22f;

        private RectTransform _rect;
        private Image _slab;
        private SlabSize _size;
        private SlabTone _tone;
        private readonly List<RectTransform> _moved = new List<RectTransform>();
        private float _until = -10f, _upAt = -10f;
        private bool _held, _shown;
        private float _appliedDrop;

        /// <summary>true mientras la imagen presionada tiene que verse (con el dedo encima, y <see cref="MinSeconds"/> más).</summary>
        public bool Down => _shown;

        /// <summary>Cuánto baja lo que va encima (el texto) mientras está presionada, en unidades del lienzo; 0 si no.</summary>
        public float Drop => _shown ? PlasticSlab.PressDrop(_size, _tone) : 0f;

        /// <summary>El rebote chico de escala al soltar (0 si no toca o con «quitar animaciones»): se suma a 1.</summary>
        public float Bounce
        {
            get
            {
                if (_shown || !Motion.Decorative) return 0f;
                float t = GameClock.Time - _upAt;
                if (t < 0f || t >= BounceSeconds) return 0f;
                float u = t / BounceSeconds;
                return 0.07f * Mathf.Sin(u * Mathf.PI * 2f) * (1f - u);                 // un resorte que se amortigua: sube un poco y se asienta
            }
        }

        /// <summary>Modo 1: el componente cambia la losa [slab] (la que dejó <see cref="PlasticSlab.TryStyle"/>) y baja a [movers]. Devuelve null si no hay plasticina ([slab] null): el juego deja su respuesta de siempre.</summary>
        public static PlasticTouch On(GameObject host, Image slab, SlabSize size, SlabTone tone, params RectTransform[] movers)
        {
            if (slab == null) return null;
            var t = host.GetComponent<PlasticTouch>() ?? host.AddComponent<PlasticTouch>();
            t._rect = (RectTransform)host.transform;
            t._slab = slab;
            t._size = size;
            t._tone = tone;
            t._moved.Clear();
            t._moved.AddRange(movers);
            return t;
        }

        /// <summary>Modo 2: solo lleva la cuenta (el juego lee <see cref="Down"/> y <see cref="Drop"/>). [size] y [tone] son los de la losa que dibuja, para saber cuánto baja el texto.</summary>
        public static PlasticTouch On(GameObject host, SlabSize size, SlabTone tone)
        {
            var t = host.GetComponent<PlasticTouch>() ?? host.AddComponent<PlasticTouch>();
            t._rect = (RectTransform)host.transform;
            t._slab = null;
            t._size = size;
            t._tone = tone;
            return t;
        }

        /// <summary>Cambia el color de la losa (modo 1: la imagen cambia conservando si está presionada; modo 2: solo cuánto baja el texto).</summary>
        public void SetTone(SlabTone tone)
        {
            if (tone == _tone) return;
            _tone = tone;
            if (_slab != null) Apply();
        }

        /// <summary>El juego aceptó un toque en este objeto: se ve presionado mientras el dedo siga encima y al menos <see cref="MinSeconds"/>.</summary>
        public void Press()
        {
            _held = true;
            _until = GameClock.Time + MinSeconds;
            Refresh(GameClock.Time);
        }

        private void Update() => Refresh(GameClock.Time);

        private void Refresh(float now)
        {
            if (_held && !PointerOn()) _held = false;
            bool show = _held || now < _until;
            if (show != _shown)
            {
                _shown = show;
                if (!show) _upAt = now;
                if (_slab != null) Apply();
            }
            if (_slab != null)
            {
                float b = Bounce;
                _slab.rectTransform.localScale = b == 0f ? Vector3.one : Vector3.one * (1f + b);
            }
        }

        private void Apply()
        {
            PlasticSlab.Set(_slab, _size, _tone, _shown);
            float drop = Drop;
            float delta = drop - _appliedDrop;                                          // lo que baja lo de encima cambia solo con la presión
            if (delta != 0f) foreach (var r in _moved) if (r != null) r.anchoredPosition += new Vector2(0f, -delta);
            _appliedDrop = drop;
        }

        private bool PointerOn()
        {
            if (_rect == null) return false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled && RectTransformUtility.RectangleContainsScreenPoint(_rect, t.position, null)) return true;
            }
            return Input.touchCount == 0 && Input.GetMouseButton(0) && RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition, null);
        }

        private void OnDisable()
        {
            _held = false;
            _until = -10f;
            if (_shown)
            {
                _shown = false;
                if (_slab != null) Apply();
            }
            if (_slab != null) _slab.rectTransform.localScale = Vector3.one;
        }
    }
}
