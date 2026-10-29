using UnityEngine;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>Mueve las burbujas de luz de fondo de los cielos de los juegos (<c>WorldBackdrop</c>; antes vivía en la cuenta regresiva) (suben
    /// despacio con un leve balanceo y reaparecen abajo al salir por arriba).</summary>
    public sealed class BokehAmbient : MonoBehaviour
    {
        private RectTransform _area;
        private RectTransform[] _bubbles;
        private Vector2[] _norm;      // posición normalizada (0..1)
        private float[] _speed;       // fracción de pantalla por segundo
        private float[] _phase;

        public void Build(RectTransform area, int count)
        {
            _area = area;
            _bubbles = new RectTransform[count];
            _norm = new Vector2[count];
            _homeNorm = new Vector2[count];
            _speed = new float[count];
            _phase = new float[count];
            var rng = new System.Random(21);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Bubble");
                go.transform.SetParent(area, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                float size = 120f + (float)rng.NextDouble() * 300f;
                rect.sizeDelta = new Vector2(size, size);
                var img = go.AddComponent<UnityEngine.UI.Image>();
                img.sprite = RadialGlowSprite.Get();
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, 0.07f + (float)rng.NextDouble() * 0.09f);
                _bubbles[i] = rect;
                _norm[i] = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                _homeNorm[i] = _norm[i];
                _speed[i] = 0.03f + (float)rng.NextDouble() * 0.05f;
                _phase[i] = (float)rng.NextDouble() * Mathf.PI * 2f;
            }
        }

        private Vector2 _restSize = new Vector2(-1f, -1f);
        private Vector2[] _homeNorm;

        private void Update() => Step(GameClock.DeltaTime);

        /// <summary>Un cuadro de <paramref name="dt"/> segundos (público para las pruebas EditMode, que no tienen cuadros).
        /// Con "quitar animaciones" las burbujas quedan quietas en su lugar de casa (se dibujan una vez).</summary>
        public void Step(float dt)
        {
            if (_bubbles == null) return;
            var size = _area.rect.size;
            if (!Motion.Decorative)
            {
                if (size == _restSize) return;
                _restSize = size;
                for (int i = 0; i < _bubbles.Length; i++)
                {
                    _norm[i] = _homeNorm[i];
                    _bubbles[i].anchoredPosition = new Vector2(_homeNorm[i].x * size.x, _homeNorm[i].y * size.y);
                }
                return;
            }
            _restSize = new Vector2(-1f, -1f);
            for (int i = 0; i < _bubbles.Length; i++)
            {
                _norm[i].y += _speed[i] * dt;
                if (_norm[i].y > 1.15f) _norm[i].y = -0.15f;
                _phase[i] += dt * 0.6f;
                float sway = Mathf.Sin(_phase[i]) * 0.02f;
                _bubbles[i].anchoredPosition = new Vector2((_norm[i].x + sway) * size.x, _norm[i].y * size.y);
            }
        }
    }
}
