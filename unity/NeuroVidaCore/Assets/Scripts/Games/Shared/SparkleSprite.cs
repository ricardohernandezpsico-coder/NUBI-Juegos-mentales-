using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Destello REDONDO (blanco, se tiñe con <c>Image.color</c>): un núcleo brillante con un halo suave que se apaga hacia el borde. Es el "brillo" del sello nocturno
    /// de Nubi (cuenta regresiva, celebraciones, aciertos). Hasta el 4-oct era una estrella de 4 puntas; la regla de Ricardo es nada de estrellas con puntas.
    /// </summary>
    public static class SparkleSprite
    {
        private static Sprite _cached;

        public static Sprite Get()
        {
            if (_cached != null) return _cached;

            const int size = 128;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = ((x + 0.5f) / size - 0.5f) * 2f; // -1..1
                    float dy = ((y + 0.5f) / size - 0.5f) * 2f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float core = Mathf.Exp(-r * r * 38f);
                    float halo = 0.55f * Mathf.Pow(Mathf.Clamp01(1f - r), 2.4f);
                    float a = Mathf.Clamp01(core + halo);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            _cached = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _cached;
        }
    }
}
