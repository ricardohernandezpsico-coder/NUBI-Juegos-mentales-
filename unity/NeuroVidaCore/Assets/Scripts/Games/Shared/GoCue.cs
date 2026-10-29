using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El «¡Ya!» (Tarea 190, 28-oct; opción A de <c>auditoria-fable/cuenta-y-tutoriales.md</c>, que eligió Ricardo): lo ÚNICO que queda de la cuenta regresiva. El tablero ya está armado y a la vista (la pantalla de carga de la app lo tapó hasta que
    /// estuvo completo: <see cref="GameControllerBase.ShowBoardWhenReady"/>) y, encima, una losa sol de plasticina —la misma de «¡Listo!» del final, así la entrada y la salida hablan igual— entra, se queda un instante y sale en espejo. Dura <see cref="Seconds"/> (0,8 s) y suena
    /// <see cref="GameFeel.Go"/> (la marimba de salida). No se toca (no es un botón), no tapa el marcador ni la fila de consigna (va al centro del lienzo) y no hay nada más de sol en la pantalla mientras dura. El primer ensayo y el reloj de la partida (el Contrarreloj de 120 s también)
    /// empiezan cuando termina <see cref="Play"/>. Con «quitar animaciones»: la misma duración con un fundido de 150 ms al entrar y al salir, sin rebote.
    /// </summary>
    public sealed class GoCue
    {
        /// <summary>Lo que dura el «¡Ya!» completo (entra, se queda y sale).</summary>
        public const float Seconds = 0.8f;
        private const float InSeconds = 0.22f, OutSeconds = 0.22f, FadeSecondsCalm = 0.15f;
        private const float StartScale = 0.6f;

        /// <summary>La losa mide 520 × 190 unidades del lienzo (≈ 190 × 70 dp con letra normal) y el texto va a 112 unidades (≈ 41 dp): mucho más que el mínimo de 16 dp.</summary>
        public static readonly Vector2 SlabUnits = new Vector2(520f, 190f);
        public const int FontUnits = 112;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly RectTransform _slab;

        public GoCue(Transform parent, float unitsPerDp)
        {
            var rootGo = new GameObject("GoCue");
            rootGo.transform.SetParent(parent, false);
            _root = rootGo.AddComponent<RectTransform>();
            Stretch(_root);
            _group = rootGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;                       // no es un botón: los toques pasan
            _group.interactable = false;

            var slabRect = new GameObject("Plate").AddComponent<RectTransform>();      // «Plate», no «Slab»: sin plasticina no hay losa (DisparatePlasticTests cuenta los objetos «Slab»)
            slabRect.SetParent(_root, false);
            slabRect.anchorMin = slabRect.anchorMax = new Vector2(0.5f, 0.5f);
            slabRect.sizeDelta = SlabUnits;
            slabRect.anchoredPosition = Vector2.zero;
            _slab = slabRect;

            bool plastic = PlasticSlab.Available(SlabSize.Button, SlabTone.Sun);
            if (plastic)
            {
                var img = slabRect.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                PlasticSlab.TryStyle(img, SlabSize.Button, SlabTone.Sun);
            }
            var text = MakeText(slabRect, "Text", FontUnits, TextAnchor.MiddleCenter, plastic ? NeuroStyle.Ink : NeuroStyle.Sun, 0f, 0f);
            text.text = "¡Ya!";
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (!plastic) NeuroStyle.ClayText(text, 8f, 16f);   // sin plasticina: el texto sol con contorno de arcilla de siempre

            _root.gameObject.SetActive(false);
        }

        /// <summary>true mientras el «¡Ya!» está a la vista (para las pruebas y la guardia de imágenes de fábrica).</summary>
        public bool Showing => _root != null && _root.gameObject.activeSelf;

        /// <summary>La losa del «¡Ya!» (las pruebas miden su tamaño y su lugar).</summary>
        public RectTransform Slab => _slab;

        /// <summary>Corre el «¡Ya!» (<see cref="Seconds"/>) con el reloj del juego (la pausa lo detiene) y suena la marimba de salida. Al terminar, la losa ya no está.</summary>
        public IEnumerator Play()
        {
            _root.SetAsLastSibling();                            // sobre el tablero (el marcador y la consigna quedan debajo del centro: no se tapan)
            _group.alpha = 0f;
            _slab.localScale = Vector3.one;
            _root.gameObject.SetActive(true);
            GameFeel.Go();
            float t = 0f;
            bool calm = !Motion.Decorative;
            while (t < Seconds)
            {
                t += GameClock.DeltaTime;
                Pose(t, calm, out float alpha, out float scale);
                _group.alpha = alpha;
                _slab.localScale = Vector3.one * scale;
                yield return null;
            }
            _group.alpha = 0f;
            _root.gameObject.SetActive(false);
        }

        /// <summary>La opacidad y la escala a los <paramref name="t"/> segundos (pura: la prueba la lee sin esperar). Entra y sale en espejo: lo que la entrada hace hacia adelante, la salida lo hace hacia atrás.</summary>
        public static void Pose(float t, bool calm, out float alpha, out float scale)
        {
            float inSeconds = calm ? FadeSecondsCalm : InSeconds, outSeconds = calm ? FadeSecondsCalm : OutSeconds;
            float k;
            if (t < inSeconds) k = Mathf.Clamp01(t / inSeconds);
            else if (t > Seconds - outSeconds) k = Mathf.Clamp01((Seconds - t) / outSeconds);
            else k = 1f;
            if (calm)
            {
                alpha = k;
                scale = 1f;
                return;
            }
            alpha = Mathf.Clamp01(k * 1.8f);                                   // la losa se ve entera un poco antes que su rebote termine
            scale = Mathf.LerpUnclamped(StartScale, 1f, UiFx.EaseOutBack(k));
        }
    }
}
