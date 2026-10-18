namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Los nombres de los juegos tal como están en el catálogo de la app (<c>GameRegistry</c>, <c>model/Models.kt</c>): UN solo nombre por juego en la app, el marcador, la cuenta regresiva y el tutorial (Tarea 127; Correo decía «Correo Estelar» en la app y «La estación de correo»
    /// en Unity). Esta lista es la que vigilan las pruebas: <c>GameTitlesTests</c> (Unity: los títulos de los controladores y de los contratos están aquí) y <c>GameTitlesUnityTest</c> (la app: esta lista es igual a <c>GameRegistry</c>).
    /// Los juegos retirados de la app (Rumbo, Cosecha, Bitácora) no están. «La estación de correo» queda solo como descripción en los documentos.
    /// </summary>
    public static class GameTitles
    {
        /// <summary>(id del juego, nombre del catálogo). Una entrada por línea: la prueba de la app lee este archivo.</summary>
        public static readonly (string id, string title)[] All =
        {
            ("parejas", "Constelaciones"),
            ("secuencia", "Rastro de luz"),
            ("bodega", "Bodega de carga"),
            ("correo", "Correo Estelar"),
            ("stroop", "Tinta o Palabra"),
            ("piloto", "Piloto Estelar"),
            ("freno", "Freno de Emergencia"),
            ("satelites", "Satélites"),
            ("radar", "Rescate relámpago"),
            ("calculo", "Carga exacta"),
            ("engranajes", "Engranajes"),
            ("acoplamiento", "Acoplamiento"),
            ("aterrizaje", "Aterrizaje Lunar"),
            ("anagramas", "En la punta de la lengua"),
            ("meteoros", "Lluvia de meteoros"),
            ("disparate", "¿Verdad o disparate?"),
            ("intrusa", "La estrella intrusa"),
        };

        /// <summary>El nombre del catálogo de un juego, o null si no es uno de los 17 de la app.</summary>
        public static string Of(string id)
        {
            foreach (var entry in All) if (entry.id == id) return entry.title;
            return null;
        }
    }
}
