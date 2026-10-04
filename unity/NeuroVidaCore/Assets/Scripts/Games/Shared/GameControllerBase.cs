using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // HarmonicTone
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Base de los juegos del DDA común (Stroop, Comparación, Ruta del Tesoro, Series, Cálculo,
    /// Anagramas): el estado y los comportamientos que cada controlador tenía copiados idénticos. Cada juego arma
    /// su propia UI en <see cref="BuildUi"/> (se llama una vez desde <see cref="Awake"/>) y debe asignar
    /// <see cref="_flash"/> (destello de pantalla completa) y <see cref="_resultRoot"/> (panel de resultado, con un
    /// hijo "Score" de tipo Text). Secuencia y Parejas tienen estructura propia y no heredan de acá.
    /// </summary>
    public abstract class GameControllerBase : MonoBehaviour
    {
        protected SequenceInitConfig _config;
        protected AudioSource _audioSource;
        protected readonly Dictionary<float, AudioClip> _toneCache = new Dictionary<float, AudioClip>();
        protected Image _flash;
        protected RectTransform _resultRoot;
        /// <summary>Tutorial guiado común (<see cref="GuidedTutorial"/>); null hasta que el juego llama a <see cref="BuildTutorial"/>.</summary>
        protected GuidedTutorial _tutorial;
        private string _tutorialTitle = "", _tutorialGoal = "";
        private bool _howToRunning;

        protected virtual void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            EventSystemGuard.Ensure(); // los botones (pausa, «¡Listo!») no responden sin él
            BuildUi();
            StyleResultPanel();
            gameObject.SetActive(false);
        }

        /// <summary>Panel de resultado con el sello de la app: superficie azul noche con borde tinta y sombra dura,
        /// y el puntaje gigante en sol "de arcilla". Se aplica a los 7 juegos después de que cada uno arma el suyo.</summary>
        private void StyleResultPanel()
        {
            if (_resultRoot == null) return;
            var panel = _resultRoot.GetComponent<Image>();
            if (panel != null)
            {
                panel.color = NeuroStyle.Surface;
                NeuroStyle.ClayFrame(panel, 7f, 18f);
            }
            var score = _resultRoot.Find("Score");
            var scoreText = score != null ? score.GetComponent<Text>() : null;
            if (scoreText != null)
            {
                foreach (var old in scoreText.GetComponents<Shadow>()) Destroy(old);
                scoreText.color = NeuroStyle.Sun;
                NeuroStyle.ClayText(scoreText, 6f, 12f);
            }
        }

        /// <summary>Arma toda la UI del juego por código (llamado una sola vez, en <see cref="Awake"/>).</summary>
        protected abstract void BuildUi();

        // ------------------------------------------------------------------ tutorial guiado (pieza común)

        /// <summary>true si la app pidió el tutorial (la persona nunca jugó este juego: <c>show_tutorial</c> de la config).</summary>
        protected bool TutorialWanted => _tutorial != null && _config != null && _config.config != null && _config.config.show_tutorial;

        /// <summary>Crea el tutorial guiado sobre <paramref name="safe"/> (llamar al final de <see cref="BuildUi"/>, para que quede encima de todo).
        /// <paramref name="practiceTopU"/> = unidades del lienzo desde arriba hasta debajo del marcador del juego; <paramref name="title"/> y
        /// <paramref name="goal"/> son el nombre del juego y su meta en una frase (≤ 2 líneas) para la tarjeta de entrada.</summary>
        protected void BuildTutorial(RectTransform safe, float practiceTopU, string title, string goal, bool skipAtTop = false, float captionFromBottomU = -1f, bool badgeAtBottom = false)
        {
            _tutorial = new GuidedTutorial(safe, practiceTopU);
            _tutorial.PlaceControls(practiceTopU, skipAtTop, captionFromBottomU, badgeAtBottom);
            _tutorialTitle = title;
            _tutorialGoal = goal;
        }

        /// <summary>Para el botón de «saltar»: llamar al principio de <c>Update</c> y salir si devuelve true (el toque era de «Saltar tutorial», no del juego).</summary>
        protected bool PollTutorialSkip() =>
            _tutorial != null && _tutorial.Practicing && GuidedTutorial.TryPress(out var pos) && _tutorial.TrySkip(pos);

        /// <summary>El gancho de «ronda guiada»: cada juego lo reemplaza con SU primera ronda (corta, con ayuda, sin puntos). Debe jugarse SIN pasar por
        /// el DDA, el puntaje ni el guardado, llamar a <see cref="GuidedTutorial.BeginPractice"/> y <see cref="GuidedTutorial.EndPractice"/>, y dejar de
        /// jugar en cuanto <see cref="GuidedTutorial.Skipped"/> sea true. Por defecto no hace nada.</summary>
        protected virtual IEnumerator GuidedRound(GuidedTutorial tutorial) { yield break; }

        /// <summary>Si corresponde, corre el tutorial: tarjeta de entrada con Nubi maestra → <see cref="GuidedRound"/> (salvo «Saltar tutorial»). Se usa
        /// con <c>yield return StartCoroutine(RunTutorialIfNeeded(...))</c> antes de la cuenta regresiva o de la primera ronda real.</summary>
        protected IEnumerator RunTutorialIfNeeded()
        {
            if (!TutorialWanted) yield break;
            yield return StartCoroutine(_tutorial.Intro(_tutorialTitle, _tutorialGoal));
            if (!_tutorial.Skipped) yield return StartCoroutine(GuidedRound(_tutorial));
            _tutorial.Hide();
        }

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        /// <summary>El juego ya está en marcha y puede mostrar «Cómo se juega» (cada juego con tutorial lo reemplaza: ni en la cuenta regresiva ni ya terminado).</summary>
        protected virtual bool HowToReady => false;

        /// <summary>La pausa ofrece «Cómo se juega» si el juego tiene tutorial, está en marcha y no se está mostrando ya.</summary>
        public bool CanShowHowTo => _tutorial != null && !_howToRunning && HowToReady;

        /// <summary>Antes del tutorial: el juego deja quieto lo suyo (fase en reposo, nada a medio caer ni a medio contar). Se llama justo antes de
        /// detener todas sus corrutinas, así que no hay nada que seguir esperando.</summary>
        protected virtual void HowToSuspend() { }

        /// <summary>Después del tutorial: el juego corre <paramref name="spentSeconds"/> más de reloj (lo que duró) y retoma su partida donde estaba;
        /// lo que haga acá tiene que correr el reloj del Reto esa cantidad hacia adelante (para que el tiempo del tutorial no se le descuente) y volver a
        /// arrancar su bucle principal sin contar nada de lo que se vio.</summary>
        protected virtual void HowToResume(float spentSeconds) { }

        /// <summary>«Cómo se juega» de la pausa: la tarjeta de Nubi y la ronda guiada, y después vuelve a la partida donde estaba. Nada de lo hecho ahí
        /// cuenta, y el reloj del Reto no avanza mientras tanto (se corre hacia adelante lo que duró: <see cref="HowToClock"/>).</summary>
        public void ShowHowTo()
        {
            if (!CanShowHowTo) return;
            HowToSuspend();
            StopAllCoroutines();                 // la partida queda quieta; se retoma desde su bucle principal al volver
            _howToRunning = true;
            StartCoroutine(HowToFlow());
        }

        private IEnumerator HowToFlow()
        {
            GameClock.Resume();                  // el tutorial se juega con el reloj andando (la pausa ya lo detuvo)
            var clock = new HowToClock(GameClock.Time);
            yield return StartCoroutine(_tutorial.Intro(_tutorialTitle, _tutorialGoal));
            if (!_tutorial.Skipped) yield return StartCoroutine(GuidedRound(_tutorial));
            _tutorial.Hide();
            float spent = clock.Finish(GameClock.Time);
            _howToRunning = false;
            HowToResume(spent);
        }

        /// <summary>Tono armónico (cacheado por frecuencia y duración); respeta "sonido desactivado" de la app.</summary>
        protected void PlayTone(float hz, float seconds, float volume)
        {
            if (_config != null && _config.config != null && !_config.config.sound_enabled) return;
            float key = Mathf.Round(hz * 10f) + seconds * 100000f;
            if (!_toneCache.TryGetValue(key, out var clip))
            {
                clip = HarmonicTone.Build(hz, seconds, volume);
                _toneCache[key] = clip;
            }
            _audioSource.PlayOneShot(clip);
        }

        /// <summary>Destello de pantalla completa que se desvanece (acierto/error). Con "quitar animaciones" es solo un
        /// tinte suave (alfa ≤ 0,15) que se desvanece en ≤ 0,2 s: acierto y error se siguen viendo por su color y forma.</summary>
        protected IEnumerator Flash(Color color, float maxAlpha, float seconds)
        {
            float fullSeconds = seconds;
            if (!Motion.Decorative)
            {
                maxAlpha = Mathf.Min(maxAlpha, 0.15f);
                seconds = Mathf.Min(seconds, 0.2f);
            }
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _flash.color = new Color(color.r, color.g, color.b, maxAlpha * (1f - k));
                yield return null;
            }
            if (fullSeconds > seconds) yield return Motion.Hold(fullSeconds - seconds); // misma duración total
            _flash.color = new Color(0f, 0f, 0f, 0f);
        }

        /// <summary>Entrada del panel de resultado con rebote y el puntaje contando hasta <paramref name="score"/>.</summary>
        protected IEnumerator AnimateResult(int score)
        {
            var scoreText = _resultRoot.Find("Score").GetComponent<Text>();
            if (!Motion.Decorative)
            {
                // Sin rebote ni cuenta animada: el panel aparece con un fundido y el puntaje ya es el final.
                _resultRoot.localScale = Vector3.one;
                scoreText.text = score.ToString();
                var group = _resultRoot.GetComponent<CanvasGroup>();
                if (group == null) group = _resultRoot.gameObject.AddComponent<CanvasGroup>();
                yield return Motion.Fade(group, 0f, 1f);
                yield return Motion.Hold(0.9f - Motion.FadeSeconds); // misma duración total que la animación (0,9 s)
                yield break;
            }
            float t = 0f;
            const float seconds = 0.9f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _resultRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(k * 2f)));
                scoreText.text = Mathf.RoundToInt(score * UiFx.EaseOutCubic(k)).ToString();
                yield return null;
            }
            scoreText.text = score.ToString();
            _resultRoot.localScale = Vector3.one;
        }

        /// <summary>Texto centrado dentro del panel de resultado.</summary>
        protected Text AddResultText(string name, int size, Vector2 pos, Color color)
        {
            var t = MakeText(_resultRoot, name, size, TextAnchor.MiddleCenter, color, 3f, 0.4f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(820f, size * 1.4f);
            r.anchoredPosition = pos;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }
    }
}
