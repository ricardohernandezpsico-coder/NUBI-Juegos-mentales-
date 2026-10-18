using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Cierre de partida común a los juegos. UN solo final (Tarea 126): Unity no muestra ningún resultado; hay un «momento del objeto» con el emblema del juego (<see cref="FinishCurtain"/>), la cortina
    /// «¡Listo!» con las estrellas acelerando a hiperespacio (espejo del "¡Ya!" de la cuenta regresiva), suena el final común (<see cref="GameFeel.Finish"/>) y la app se abre sola en su pantalla de resultado,
    /// que arranca directo con el final con sentido.
    /// El botón "Continuar" queda como respaldo: aparece solo si la vuelta automática no ocurrió (Editor, pruebas o un error del puente), sobre la escena donde terminó el juego.
    /// </summary>
    public sealed class ExitButton
    {
        private readonly GameObject _root;
        private readonly RectTransform _rect;
        private readonly MonoBehaviour _runner;
        private readonly string _gameId;

        /// <param name="gameId">El id del juego (<c>radar</c>…): de él salen el emblema y el movimiento del momento del objeto. Null = solo la cortina de siempre.</param>
        public ExitButton(Transform parent, MonoBehaviour runner, float unitsPerDp, string gameId = null)
        {
            _runner = runner;
            _gameId = gameId;
            _root = new GameObject("ExitButton");
            _root.transform.SetParent(parent, false);
            _rect = _root.AddComponent<RectTransform>();
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(560f, 132f);
            _rect.anchoredPosition = new Vector2(0f, 50f + 66f);

            var img = _root.AddComponent<Image>();
            // acción principal en sol con texto tinta, como el botón "Entrenar" de la app: losa de plasticina (la de la app) si existe; si no, el arcilla de siempre
            var slab = PlasticSlab.TryStyle(img, SlabSize.Button, SlabTone.Sun, hit: true);
            if (slab == null)
            {
                img.sprite = RoundedRectSprite.Get(64);
                img.type = Image.Type.Sliced;
                img.color = NeuroStyle.Sun;
                NeuroStyle.ClayFrame(img, 6f, 14f);
            }

            var button = _root.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(NativeBridge.CloseGameScreen);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(_root.transform, false);
            var tr = textGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = UiFonts.Bold;
            text.fontSize = 64;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = NeuroStyle.Ink;
            text.raycastTarget = false;
            text.text = "Continuar";

            if (slab != null) _root.AddComponent<PlasticPress>().Init(slab, SlabSize.Button, SlabTone.Sun, tr);
            else _root.AddComponent<PressScale>();
            _root.SetActive(false);
        }

        public void Hide() => _root.SetActive(false);

        /// <summary>Fin de partida: cierre breve y vuelta automática a la app (ver resumen de la clase).</summary>
        public void Show()
        {
            _root.SetActive(false);
            _runner.StartCoroutine(FinishAndReturn());
        }

        /// <summary>Muestra el botón de respaldo (sin cierre ni vuelta automática).</summary>
        public void ShowButton()
        {
            _root.SetActive(true);
            _root.transform.SetAsLastSibling(); // por encima de paneles y efectos
            _runner.StartCoroutine(PopIn());
        }

        private IEnumerator FinishAndReturn()
        {
            GameFeel.Finish();
            var curtain = FinishCurtain.Create(_runner.transform, _gameId);
            // Reloj real (no GameClock): la partida ya terminó y este cierre no debe congelarse.
            yield return curtain.Play(_runner);
            NativeBridge.CloseGameScreen(); // la app se abre en su pantalla de resultado; Unity recarga la escena

            // Si seguimos acá (Editor, pruebas o el puente falló), se deja ver la escena donde terminó el juego y "Continuar".
            float waited = 0f;
            while (waited < 1.2f) { waited += Time.unscaledDeltaTime; yield return null; }
            if (curtain != null) Object.Destroy(curtain.gameObject);
            ShowButton();
        }

        private IEnumerator PopIn()
        {
            float t = 0f;
            const float delay = 0.3f; // aparece un momento después de retirarse la cortina
            _rect.localScale = Vector3.zero;
            while (t < delay) { t += GameClock.DeltaTime; yield return null; }
            t = 0f;
            const float seconds = 0.3f;
            if (!Motion.Decorative) { _rect.localScale = Vector3.one; yield break; } // sin rebote: aparece ya a su tamaño
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                _rect.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            _rect.localScale = Vector3.one;
        }
    }
}
