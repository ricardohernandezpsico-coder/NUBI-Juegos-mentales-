using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Momento de cierre de la partida (≈1,2 s): el cielo nocturno tapa el juego, las estrellas aceleran hasta
    /// hiperespacio y aparece "¡Listo!" en arcilla sol con una lluvia de destellos; debajo, "Veamos cómo te fue",
    /// que anuncia lo que viene (la pantalla de resultado de la app). Canvas propio por encima del juego y por
    /// debajo del menú de pausa. Tiempos: entrada con rebote (spring) y sin salida propia: la app aparece encima.
    /// </summary>
    public sealed class FinishCurtain : MonoBehaviour
    {
        private CanvasGroup _group;
        private RectTransform _title;
        private RectTransform _plaque;          // la placa de plasticina detrás de «¡Listo!» (null = el texto con contorno de siempre)
        private CanvasGroup _plaqueGroup;
        private CanvasGroup _subGroup;
        private StarfieldFx _stars;
        private RectTransform _fx;

        public static FinishCurtain Create(Transform parent)
        {
            var root = new GameObject("FinishCurtain");
            root.transform.SetParent(parent, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450; // sobre el juego y su cuenta regresiva; bajo el menú de pausa (500)
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            root.AddComponent<GraphicRaycaster>(); // bloquea toques al juego durante el cierre

            var curtain = root.AddComponent<FinishCurtain>();
            curtain.Build(root.GetComponent<RectTransform>());
            return curtain;
        }

        private void Build(RectTransform root)
        {
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            var sky = new GameObject("Sky");
            sky.transform.SetParent(root, false);
            var skyRect = sky.AddComponent<RectTransform>();
            Stretch(skyRect);
            var skyImg = sky.AddComponent<Image>();
            skyImg.sprite = NeuroStyle.NightGradient();
            skyImg.color = Color.white;

            var starsGo = new GameObject("Stars");
            starsGo.transform.SetParent(root, false);
            var starsRect = starsGo.AddComponent<RectTransform>();
            Stretch(starsRect);
            _stars = starsGo.AddComponent<StarfieldFx>();
            _stars.VanishingPoint = new Vector2(0.5f, 0.55f);
            _stars.Build(starsRect, 70, 10f, 29);

            var fxGo = new GameObject("Fx");
            fxGo.transform.SetParent(root, false);
            _fx = fxGo.AddComponent<RectTransform>();
            _fx.anchorMin = _fx.anchorMax = new Vector2(0.5f, 0.55f);
            _fx.sizeDelta = Vector2.zero;

            // «¡Listo!»: una placa de plasticina sol (la losa del kit) con el texto en tinta encima; sin plasticina, el texto sol con contorno tinta de siempre
            if (PlasticSlab.Available(SlabSize.Button, SlabTone.Sun))
            {
                var plaqueGo = new GameObject("TitlePlaque");
                plaqueGo.transform.SetParent(root, false);
                var plaqueRect = plaqueGo.AddComponent<RectTransform>();
                plaqueRect.anchorMin = plaqueRect.anchorMax = new Vector2(0.5f, 0.55f);
                plaqueRect.sizeDelta = new Vector2(780f, 270f);
                plaqueRect.anchoredPosition = Vector2.zero;
                var plaqueImg = plaqueGo.AddComponent<Image>();
                PlasticSlab.TryStyle(plaqueImg, SlabSize.Button, SlabTone.Sun);
                _plaque = plaqueRect;
                _plaqueGroup = plaqueGo.AddComponent<CanvasGroup>();
            }

            var title = MakeText(root, "Title", 170, TextAnchor.MiddleCenter, _plaque != null ? NeuroStyle.Ink : NeuroStyle.Sun, 0f, 0f);
            _title = title.rectTransform;
            _title.anchorMin = _title.anchorMax = new Vector2(0.5f, 0.55f);
            _title.sizeDelta = new Vector2(1000f, 240f);
            _title.anchoredPosition = Vector2.zero;
            title.font = UiFonts.Bold;
            title.text = "¡Listo!";
            if (_plaque == null) NeuroStyle.ClayText(title, 8f, 16f);

            var sub = MakeText(root, "Sub", 54, TextAnchor.MiddleCenter, NeuroStyle.Cream, 0f, 0f);
            var subRect = sub.rectTransform;
            subRect.anchorMin = subRect.anchorMax = new Vector2(0.5f, 0.55f);
            subRect.sizeDelta = new Vector2(1000f, 90f);
            subRect.anchoredPosition = new Vector2(0f, -170f);
            sub.text = "Veamos cómo te fue";
            _subGroup = sub.gameObject.AddComponent<CanvasGroup>();
            _subGroup.alpha = 0f;

            _title.localScale = Vector3.zero;
            if (_plaque != null) _plaque.localScale = Vector3.zero;
        }

        /// <summary>El título y, si hay, su placa: escala y opacidad juntas.</summary>
        private void SetTitle(float scale, float alpha)
        {
            _title.localScale = Vector3.one * scale;
            if (_plaque == null) return;
            _plaque.localScale = Vector3.one * scale;
            _plaqueGroup.alpha = alpha;
        }

        /// <summary>Anima el cierre completo (≈1,2 s) con el reloj real.</summary>
        public IEnumerator Play(MonoBehaviour runner)
        {
            if (!Motion.Decorative)
            {
                // Sin rebote, chispas ni hiperespacio: el cielo, "¡Listo!" y el subtítulo aparecen con un fundido de opacidad.
                // La duración total (≈1,2 s) queda igual: es el aviso de que viene el resultado.
                var titleText = _title.GetComponent<Text>();
                SetTitle(1f, 0f);
                float elapsed = 0f;
                const float calmTotal = 1.2f;
                while (elapsed < calmTotal)
                {
                    elapsed += GameClock.RealDeltaTime;
                    _group.alpha = Mathf.Clamp01(elapsed / Motion.FadeSeconds);
                    var tc = titleText.color;
                    tc.a = Mathf.Clamp01((elapsed - 0.05f) / Motion.FadeSeconds);
                    titleText.color = tc;
                    if (_plaque != null) _plaqueGroup.alpha = tc.a;
                    _subGroup.alpha = Mathf.Clamp01((elapsed - 0.35f) / Motion.FadeSeconds);
                    yield return null;
                }
                yield break;
            }
            runner.StartCoroutine(UiFx.SparkBurst(_fx, Vector2.zero, NeuroStyle.Sun, 22, 520f, 60f, 0.9f));
            float t = 0f;
            const float total = 1.2f;
            while (t < total)
            {
                t += GameClock.RealDeltaTime;
                // Cielo: entra rápido (0,22 s) para tapar el juego.
                _group.alpha = Mathf.Clamp01(t / 0.22f);
                // "¡Listo!": rebote de 0,4 s, empieza con el cielo.
                float k = Mathf.Clamp01((t - 0.05f) / 0.4f);
                SetTitle(Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(k)), 1f);
                // Subtítulo: después del título.
                _subGroup.alpha = Mathf.Clamp01((t - 0.35f) / 0.25f);
                // Estrellas: aceleran hasta hiperespacio en la segunda mitad (el viaje "hacia" el resultado).
                _stars.Warp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.45f) / 0.7f));
                yield return null;
            }
        }
    }
}
