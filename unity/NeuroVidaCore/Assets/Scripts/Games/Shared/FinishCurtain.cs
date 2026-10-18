using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;    // RadialGlowSprite
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El ÚNICO final de la partida (Tarea 126, <c>auditoria-fable/pulido-juegos.md</c> §2). Antes se veían tres: el panel de resultado de cada juego (con tabla y gráfico) que la cortina tapaba en 0,22 s, la cortina «¡Listo!» y la pantalla de la app que volvía a
    /// empezar con puntos y estrellas. Ahora Unity no dibuja ningún resultado: hay un «momento del objeto» y la cortina, y la app abre directo con el final con sentido.
    ///
    /// 1. <b>El momento del objeto</b> (≈1,0 s, <see cref="MomentSeconds"/>): la escena queda donde terminó, apagada por un velo de noche, y el EMBLEMA del juego (la ficha de plasticina con su objeto, la misma de la app) entra al centro con su movimiento
    ///    (<see cref="MomentKind"/>: se asienta, sube, se mece, sella o simplemente aparece) y unas chispas redondas de sol. Con «quitar animaciones» es un cuadro fijo: el velo y el emblema quietos durante el mismo tiempo.
    /// 2. <b>La cortina</b> (≈1,2 s, <see cref="CurtainSeconds"/>): el cielo (los tres tonos del fondo de la app) cubre el juego, el emblema sube un poco y debajo aparece «¡Listo!» con «Veamos cómo te fue»; las estrellas aceleran a hiperespacio.
    ///
    /// Un solo Canvas por encima del juego y por debajo del menú de pausa; el velo tapa los toques desde el primer cuadro (no hay que esperar al cielo). Sin emblema (juego sin ficha: pruebas, juegos retirados) no hay momento: queda la cortina de siempre.
    /// </summary>
    public sealed class FinishCurtain : MonoBehaviour
    {
        public const float MomentSeconds = 1.0f;
        public const float CurtainSeconds = 1.2f;
        private const float EmblemMoment = 540f, EmblemCurtain = 440f;                 // lado de la ficha (unidades del lienzo de 1080)
        private static readonly Vector2 MomentAnchor = new Vector2(0.5f, 0.55f), CurtainAnchor = new Vector2(0.5f, 0.64f);
        private const float VeilAlpha = 0.58f;

        /// <summary>Cómo entra el emblema en el momento del objeto.</summary>
        public enum MomentKind { Pop, Settle, Rise, Rock, Stamp }

        /// <summary>El movimiento de cada juego: lo que su objeto «hace» al terminar (la caja se asienta, el cohete sube, el engranaje se mece, el sobre se sella…).</summary>
        public static MomentKind KindOf(string gameId)
        {
            switch (gameId)
            {
                case "bodega": case "aterrizaje": case "acoplamiento": return MomentKind.Settle;        // la caja baja a su sitio, la nave se posa, la pieza encaja
                case "piloto": case "radar": return MomentKind.Rise;                                     // el cohete despega, la cápsula sube a la nave
                case "engranajes": case "satelites": case "meteoros": return MomentKind.Rock;            // el engranaje se acomoda, el satélite y el meteoro se mecen
                case "correo": case "freno": case "stroop": case "calculo": return MomentKind.Stamp;     // el sello cae, la señal se planta, la tinta se asienta, el reactor se carga
                default: return MomentKind.Pop;                                                          // constelaciones, rastro de luz, punta de la lengua, disparate, intrusa
            }
        }

        /// <summary>El emblema del juego (la ficha con su objeto) del atlas «emblemas», o null si no hay.</summary>
        public static Sprite EmblemSprite(string gameId) => string.IsNullOrEmpty(gameId) ? null : PlasticArt.Get("emblemas", "emb_" + gameId);

        private MomentKind _kind;
        private CanvasGroup _group;               // todo el lienzo (queda en 1; las pruebas lo leen)
        private CanvasGroup _skyGroup;            // el cielo y las estrellas: entran después del momento
        private Image _veil;
        private RectTransform _emblem, _glow;
        private Image _glowImg;
        private RectTransform _title;
        private RectTransform _plaque;            // la placa de plasticina detrás de «¡Listo!» (null = el texto con contorno de siempre)
        private CanvasGroup _plaqueGroup;
        private CanvasGroup _subGroup;
        private StarfieldFx _stars;
        private RectTransform _fx;
        private bool _hasEmblem;

        public static FinishCurtain Create(Transform parent, string gameId = null)
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
            curtain._kind = KindOf(gameId);
            curtain.Build(root.GetComponent<RectTransform>(), EmblemSprite(gameId));
            return curtain;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private void Build(RectTransform root, Sprite emblem)
        {
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;

            // El velo: apaga la escena que quedó debajo y, como recibe toques, la deja quieta desde el primer cuadro.
            var veilRect = Child(root, "Veil");
            Stretch(veilRect);
            _veil = veilRect.gameObject.AddComponent<Image>();
            _veil.color = NeuroStyle.WithAlpha(NeuroStyle.NightTop, 0f);
            _veil.raycastTarget = true;

            // El cielo y las estrellas: entran con la cortina.
            var skyRoot = Child(root, "SkyGroup");
            Stretch(skyRoot);
            _skyGroup = skyRoot.gameObject.AddComponent<CanvasGroup>();
            _skyGroup.alpha = 0f;
            _skyGroup.blocksRaycasts = false;
            var sky = Child(skyRoot, "Sky");
            Stretch(sky);
            var skyImg = sky.gameObject.AddComponent<Image>();
            skyImg.sprite = NeuroStyle.NightGradient();
            skyImg.color = Color.white;
            skyImg.raycastTarget = false;

            var starsRect = Child(skyRoot, "Stars");
            Stretch(starsRect);
            _stars = starsRect.gameObject.AddComponent<StarfieldFx>();
            _stars.VanishingPoint = new Vector2(0.5f, 0.55f);
            _stars.Build(starsRect, 70, 10f, 29);

            // Un resplandor suave de sol detrás del emblema y el emblema mismo.
            _hasEmblem = emblem != null;
            if (_hasEmblem)
            {
                _glow = Child(root, "EmblemGlow");
                _glow.anchorMin = _glow.anchorMax = MomentAnchor;
                _glow.sizeDelta = new Vector2(EmblemMoment * 1.5f, EmblemMoment * 1.5f);
                _glow.anchoredPosition = Vector2.zero;
                _glowImg = _glow.gameObject.AddComponent<Image>();
                _glowImg.sprite = RadialGlowSprite.Get();
                _glowImg.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0f);
                _glowImg.raycastTarget = false;

                _emblem = Child(root, "Emblem");
                _emblem.anchorMin = _emblem.anchorMax = MomentAnchor;
                _emblem.sizeDelta = new Vector2(EmblemMoment, EmblemMoment);
                _emblem.anchoredPosition = Vector2.zero;
                var img = _emblem.gameObject.AddComponent<Image>();
                img.sprite = emblem;
                img.preserveAspect = true;
                img.raycastTarget = false;
                _emblem.localScale = Vector3.zero;
            }

            _fx = Child(root, "Fx");
            _fx.anchorMin = _fx.anchorMax = MomentAnchor;
            _fx.sizeDelta = Vector2.zero;

            // «¡Listo!»: una placa de plasticina sol (la losa del kit) con el texto en tinta encima; sin plasticina, el texto sol con contorno tinta de siempre. Va debajo del emblema (o, sin emblema, donde estaba).
            float titleY = _hasEmblem ? 0.37f : 0.55f;
            if (PlasticSlab.Available(SlabSize.Button, SlabTone.Sun))
            {
                var plaqueRect = Child(root, "TitlePlaque");
                plaqueRect.anchorMin = plaqueRect.anchorMax = new Vector2(0.5f, titleY);
                plaqueRect.sizeDelta = _hasEmblem ? new Vector2(700f, 220f) : new Vector2(780f, 270f);
                plaqueRect.anchoredPosition = Vector2.zero;
                var plaqueImg = plaqueRect.gameObject.AddComponent<Image>();
                plaqueImg.raycastTarget = false;
                PlasticSlab.TryStyle(plaqueImg, SlabSize.Button, SlabTone.Sun);
                _plaque = plaqueRect;
                _plaqueGroup = plaqueRect.gameObject.AddComponent<CanvasGroup>();
            }

            var title = MakeText(root, "Title", _hasEmblem ? 140 : 170, TextAnchor.MiddleCenter, _plaque != null ? NeuroStyle.Ink : NeuroStyle.Sun, 0f, 0f);
            _title = title.rectTransform;
            _title.anchorMin = _title.anchorMax = new Vector2(0.5f, titleY);
            _title.sizeDelta = new Vector2(1000f, 240f);
            _title.anchoredPosition = Vector2.zero;
            title.font = UiFonts.Bold;
            title.text = "¡Listo!";
            if (_plaque == null) NeuroStyle.ClayText(title, 8f, 16f);

            var sub = MakeText(root, "Sub", 54, TextAnchor.MiddleCenter, NeuroStyle.Cream, 0f, 0f);
            var subRect = sub.rectTransform;
            subRect.anchorMin = subRect.anchorMax = new Vector2(0.5f, titleY);
            subRect.sizeDelta = new Vector2(1000f, 90f);
            subRect.anchoredPosition = new Vector2(0f, _hasEmblem ? -150f : -170f);
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

        /// <summary>Anima el final completo (momento ≈1,0 s + cortina ≈1,2 s) con el reloj real.</summary>
        public IEnumerator Play(MonoBehaviour runner)
        {
            if (_hasEmblem) yield return PlayMoment(runner);
            yield return PlayCurtain(runner);
        }

        // ------------------------------------------------------------------ 1. el momento del objeto

        private IEnumerator PlayMoment(MonoBehaviour runner)
        {
            if (!Motion.Decorative)
            {
                // Un cuadro fijo: el velo y el emblema quietos, con el mismo tiempo (el final dura lo mismo).
                _veil.color = NeuroStyle.WithAlpha(NeuroStyle.NightTop, VeilAlpha);
                _glowImg.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.22f);
                _emblem.localScale = Vector3.one;
                float held = 0f;
                while (held < MomentSeconds) { held += GameClock.RealDeltaTime; yield return null; }
                yield break;
            }
            runner.StartCoroutine(UiFx.SparkBurst(_fx, Vector2.zero, NeuroStyle.Sun, 18, 380f, 46f, 0.85f));
            float t = 0f;
            while (t < MomentSeconds)
            {
                t += GameClock.RealDeltaTime;
                float k = Mathf.Clamp01(t / MomentSeconds);
                _veil.color = NeuroStyle.WithAlpha(NeuroStyle.NightTop, VeilAlpha * Mathf.Clamp01(t / 0.25f));
                _glowImg.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.26f * Mathf.Clamp01(t / 0.3f) * (1f - 0.35f * k));
                ApplyEmblem(_kind, t);
                yield return null;
            }
            ApplyEmblem(_kind, MomentSeconds);
        }

        /// <summary>La pose del emblema a los <paramref name="t"/> segundos del momento (pura: la prueba la lee sin esperar).</summary>
        private void ApplyEmblem(MomentKind kind, float t)
        {
            float pop = UiFx.EaseOutBack(Mathf.Clamp01(t / 0.45f));                 // 0 → 1 con rebote
            float scale = Mathf.LerpUnclamped(0.55f, 1f, pop);
            float y = 0f, rot = 0f;
            switch (kind)
            {
                case MomentKind.Settle:                                              // baja desde arriba y se asienta
                    y = (1f - UiFx.EaseOutCubic(Mathf.Clamp01(t / 0.5f))) * 180f;
                    break;
                case MomentKind.Rise:                                                // sube desde abajo (el cohete despega, la cápsula sube)
                    y = -(1f - UiFx.EaseOutCubic(Mathf.Clamp01(t / 0.5f))) * 200f;
                    break;
                case MomentKind.Rock:                                                // se mece y se acomoda
                    rot = Mathf.Sin(Mathf.Clamp01(t / 0.9f) * Mathf.PI * 3f) * 7f * (1f - Mathf.Clamp01(t / 0.9f));
                    break;
                case MomentKind.Stamp:                                               // cae como un sello: grande y se planta
                    scale = Mathf.LerpUnclamped(1.45f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(t / 0.35f)));
                    break;
            }
            _emblem.anchoredPosition = new Vector2(0f, y);
            _emblem.localScale = Vector3.one * scale;
            _emblem.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        // ------------------------------------------------------------------ 2. la cortina

        private IEnumerator PlayCurtain(MonoBehaviour runner)
        {
            if (!Motion.Decorative)
            {
                // Sin rebote, chispas ni hiperespacio: el cielo, "¡Listo!" y el subtítulo aparecen con un fundido de opacidad.
                // La duración total (≈1,2 s) queda igual: es el aviso de que viene el resultado.
                var titleText = _title.GetComponent<Text>();
                SetTitle(1f, 0f);
                if (_hasEmblem) PlaceEmblemForCurtain(1f);
                float elapsed = 0f;
                while (elapsed < CurtainSeconds)
                {
                    elapsed += GameClock.RealDeltaTime;
                    _skyGroup.alpha = Mathf.Clamp01(elapsed / Motion.FadeSeconds);
                    var tc = titleText.color;
                    tc.a = Mathf.Clamp01((elapsed - 0.05f) / Motion.FadeSeconds);
                    titleText.color = tc;
                    if (_plaque != null) _plaqueGroup.alpha = tc.a;
                    _subGroup.alpha = Mathf.Clamp01((elapsed - 0.35f) / Motion.FadeSeconds);
                    yield return null;
                }
                _skyGroup.alpha = 1f;
                yield break;
            }
            if (!_hasEmblem) runner.StartCoroutine(UiFx.SparkBurst(_fx, Vector2.zero, NeuroStyle.Sun, 22, 520f, 60f, 0.9f));
            float t = 0f;
            while (t < CurtainSeconds)
            {
                t += GameClock.RealDeltaTime;
                // Cielo: entra rápido (0,22 s) para tapar el juego.
                _skyGroup.alpha = Mathf.Clamp01(t / 0.22f);
                if (_hasEmblem) PlaceEmblemForCurtain(UiFx.EaseOutCubic(Mathf.Clamp01(t / 0.35f)));
                // "¡Listo!": rebote de 0,4 s, empieza con el cielo.
                float k = Mathf.Clamp01((t - 0.05f) / 0.4f);
                SetTitle(Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(k)), 1f);
                // Subtítulo: después del título.
                _subGroup.alpha = Mathf.Clamp01((t - 0.35f) / 0.25f);
                // Estrellas: aceleran hasta hiperespacio en la segunda mitad (el viaje "hacia" el resultado).
                _stars.Warp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.45f) / 0.7f));
                yield return null;
            }
            _skyGroup.alpha = 1f;
        }

        /// <summary>El emblema pasa de su lugar del momento (centro, grande) al de la cortina (más arriba y más chico): <paramref name="k"/> 0..1. El velo y el resplandor se retiran con el cielo.</summary>
        private void PlaceEmblemForCurtain(float k)
        {
            var anchor = Vector2.Lerp(MomentAnchor, CurtainAnchor, k);
            float side = Mathf.Lerp(EmblemMoment, EmblemCurtain, k);
            _emblem.anchorMin = _emblem.anchorMax = anchor;
            _emblem.anchoredPosition = Vector2.zero;
            _emblem.sizeDelta = new Vector2(side, side);
            _emblem.localScale = Vector3.one;
            _emblem.localRotation = Quaternion.identity;
            _glow.anchorMin = _glow.anchorMax = anchor;
            _glow.sizeDelta = new Vector2(side * 1.5f, side * 1.5f);
            _veil.color = NeuroStyle.WithAlpha(NeuroStyle.NightTop, VeilAlpha * (1f - k));
        }

        /// <summary>Para las pruebas: la pose del emblema a los <paramref name="seconds"/> del momento, sin esperar ni dibujar.</summary>
        public Vector2 EmblemPoseAt(float seconds, out float scale, out float rotation)
        {
            ApplyEmblem(_kind, seconds);
            scale = _emblem.localScale.x;
            rotation = _emblem.localEulerAngles.z > 180f ? _emblem.localEulerAngles.z - 360f : _emblem.localEulerAngles.z;
            return _emblem.anchoredPosition;
        }

        public bool HasEmblem => _hasEmblem;
        public MomentKind Kind => _kind;
    }
}
