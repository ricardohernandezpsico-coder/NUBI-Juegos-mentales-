using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Tipografía compartida por los juegos Unity: <b>Fredoka</b>, la misma de la app (sello "noche + arcilla",
    /// 25-sep; antes era Outfit). Redondeada y amable, se lee bien en números grandes y a cualquier edad.
    /// Licencia OFL (<c>Assets/Resources/Fonts/Fredoka-OFL.txt</c>). Los .ttf son instancias estáticas
    /// (Bold 700 y SemiBold 600) generadas de la fuente variable de la app (<c>res/font/fredoka.ttf</c>, que por
    /// defecto es Light): el Text legacy de Unity no elige peso en fuentes variables. Sin la fuente presente,
    /// cae a la legacy para no romper nada.
    ///
    /// Se usa siempre la variante ya diseñada con su peso: NO combinar con <c>FontStyle.Bold</c> (Unity la
    /// "engordaría" otra vez sintéticamente).
    /// </summary>
    public static class UiFonts
    {
        private static Font _bold;
        private static Font _regular;
        private static Font _word;
        private static Font _name;

        public static Font Bold => _bold != null ? _bold : (_bold = Load("Fonts/Fredoka-Bold"));
        public static Font Regular => _regular != null ? _regular : (_regular = Load("Fonts/Fredoka-SemiBold"));

        /// <summary>Atkinson Hyperlegible Bold (Braille Institute of America, SIL OFL 1.1; <c>Fonts/Atkinson-OFL.txt</c>): SOLO
        /// para las palabras de Lluvia de meteoros, donde hay que distinguir "a" de "o" y "i" de "l" sin dudar. Sin el archivo,
        /// cae a Fredoka Bold.</summary>
        public static Font Word => _word != null ? _word : (_word = LoadOr("Fonts/AtkinsonHyperlegible-Bold", Bold));

        /// <summary>Fraunces Italic Medium (SIL OFL 1.1; <c>Fonts/Fraunces-OFL.txt</c>): SOLO el nombre del grabado de La estrella intrusa
        /// («La Manzana»). Sin el archivo, cae a Fredoka SemiBold.</summary>
        public static Font Name => _name != null ? _name : (_name = LoadOr("Fonts/Fraunces-Italic", Regular));

        private static Font LoadOr(string path, Font fallback)
        {
            var font = Resources.Load<Font>(path);
            return font != null ? font : fallback;
        }

        private static Font Load(string path)
        {
            var font = Resources.Load<Font>(path);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>Sombra suave debajo del texto (mejor lectura que el contorno negro duro
        /// de las primeras pasadas; se atenúa junto con el alfa del texto).</summary>
        public static void AddSoftShadow(GameObject textGo, float distance = 3f, float alpha = 0.38f)
        {
            var shadow = textGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(0f, -distance);
            shadow.useGraphicAlpha = true;
        }
    }
}
