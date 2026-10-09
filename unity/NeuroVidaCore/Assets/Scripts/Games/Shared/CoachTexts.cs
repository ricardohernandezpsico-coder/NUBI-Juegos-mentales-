using System.Collections.Generic;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Lo que dice Nubi en el tutorial de cada juego, en un solo lugar (revisado el 5-oct: Ricardo probó el tutorial y no quedaba claro). Reglas: UNA idea por globo; primero el verbo de lo que hay que hacer
    /// («Toca…», «Mira…»); nada que dependa de algo que todavía no se vio; y el texto nombra lo que está iluminado con LA MISMA palabra que se lee en pantalla. Cada texto cabe en 3 líneas de letra
    /// grande (<see cref="CoachLayout.MaxLines"/>): una prueba lo comprueba con la lista de <see cref="All"/>. Engranajes (rediseñado el 5-oct como «Taller de reparación») entra con sus tres focos.
    /// </summary>
    public static class CoachTexts
    {
        /// <summary>El aviso con que termina la ronda guiada de todos los juegos.</summary>
        public const string Ready = "¡Listo! Ahora va en serio";

        /// <summary>«En la punta de la lengua» (pantalla: «Nubi capta una definición», «¿Cuál es la palabra?», botones «¡La tengo!» y «Una ayuda»).</summary>
        public static class Punta
        {
            public const string ReadDefinition = "Lee la definición y toca la tarjeta";
            public const string Have = "Si ya sabes la palabra, toca ¡La tengo!";
            public const string Letters = "Toca las letras en orden";
            public const string Help = "Si no te sale, toca Una ayuda";
            public const string FoundAlone = "¡Lucero dorado! La encontraste sola";
            public const string Shown = "No pasa nada: Nubi te la muestra y vuelve otro día";
            public const string FoundWithHelp = "Lucero plateado: la encontraste con ayuda";
            public const string HelpAlso = "Con ayuda también se encuentra";
        }

        /// <summary>Aterrizaje lunar (pantalla: el número grande de la misión, la línea con sus dos extremos, la nave).</summary>
        public static class Aterrizaje
        {
            public static string Drag(string label) => "Arrastra la nave hasta el " + label + " y suelta";
            public static string Again(string label) => "Otra vez: arrastra la nave hasta el " + label;
            public const string Good = "¡Bien! Mientras más cerca del número, mejor";
            public static string Close(string label) => "Casi: la zona amarilla marca el " + label;
        }

        /// <summary>Carga exacta (pantalla: «carga exacta» con el número del reactor, las celdas, los botones de operación, «usa todas las celdas»).</summary>
        public static class Calculo
        {
            public const string Cell = "Toca una celda: juntas llegan a la carga exacta";
            public const string Plus = "Toca + para sumar";
            public const string OtherCell = "Toca otra celda: se suman";
            public const string Done = "¡Carga exacta! Así se juega";
        }

        /// <summary>Freno de emergencia (pantalla: los cohetes con su luz y la señal «ALTO»).</summary>
        public static class Freno
        {
            public const string Launch = "Toca el cohete que se enciende";
            public const string Stop = "Aparece ALTO: no toques nada";
            public const string Missed = "Casi: toca el cohete que se enciende";
            public const string Braked = "¡Frenaste a tiempo!";
            public const string Tapped = "Casi: con ALTO el cohete se queda quieto. Prueba otra vez";
        }

        /// <summary>Lluvia de meteoros (pantalla: las palabras que caen dentro de cada meteoro).</summary>
        public static class Meteoros
        {
            public const string Exists = "Si la palabra existe, tócala";
            public const string LetItFall = "¡Bien! Esa no existía: dejarla caer fue lo correcto";
            public const string MissedWord = "Casi: esa palabra existe. Tócala antes de que llegue abajo";
            public const string MissedFake = "Casi: esa no existe. Las inventadas se dejan caer";
        }

        /// <summary>Rastro de luz (pantalla: los luceros, el contador «luces recordadas»).</summary>
        public static class Rastro
        {
            public const string Watch = "Mira el camino de la chispa";
            public const string Repeat = "Repite el camino: toca las mismas luces";
            public const string Good = "¡Eso! Cada ronda suma una luz";
            public const string Missed = "Casi: esa no era. Mira otra vez el camino";
        }

        /// <summary>Dos orillas, «Tinta o palabra» (pantalla: la cinta «Responde: TINTA / PALABRA», las dos orillas, la palabra y los cuatro botones de color).</summary>
        public static class Stroop
        {
            public const string InkRule = "Responde TINTA: toca el color con que está escrita";
            public const string WordRule = "Responde PALABRA: toca lo que DICE la palabra";
            public static string Good(string explain) => "¡Bien! " + explain;
            public const string MissedInk = "Casi: con TINTA se toca el color de la letra";
            public const string MissedWord = "Casi: con PALABRA se toca lo que DICE";
        }

        /// <summary>Engranajes: Taller de reparación (pantalla: los carteles de las piezas del cohete, el motor con su flecha y el botón «Arrancar»).</summary>
        public static class Engranajes
        {
            public const string Cartel = "El cartel dice qué debe hacer la antena";
            public const string Motor = "Toca el motor para cambiar su giro";
            public const string Start = "Toca Arrancar y mira la antena";
        }

        /// <summary>Bodega de carga (pantalla: la bodega con su esclusa y las escotillas, y la tarjeta de arriba).</summary>
        public static class Bodega
        {
            public const string Begin = "El robot guarda la carga. Toca la bodega y mira";
            public const string Watch = "Fíjate en qué escotilla guarda cada cosa";
            public const string Reveal = "Así quedó la carga. Ahora cierro las escotillas";
            public static string Ask(string objectWithArticle) => "¿Dónde está " + objectWithArticle + "? Toca su escotilla";
            public const string Wrong = "Si fallas, se abre la correcta y aprendes";
        }

        /// <summary>Constelaciones (pantalla: el cielo de luces, la tarjeta de arriba y la fila «De memoria» de abajo).</summary>
        public static class Constelaciones
        {
            public const string First = "Toca una luz para ver qué esconde";
            public const string Second = "Toca otra. Si no son iguales, se cierran";
            public const string Partner = "Esta ya la viste. ¿Dónde estaba su pareja?";
            public const string GoldLine = "Línea dorada: la encontraste de memoria";
            public const string Row = "Aquí se cuenta cuántas veces fuiste directo";
        }

        /// <summary>La estación de correo (pantalla: la hoja de encargos, la cinta con la carta, la caja fuerte, el reloj tapado y el faro).</summary>
        public static class Correo
        {
            public const string First = "Toca el buzón del sello";
            public const string Sheet = "Esto te piden hoy. Durante el día no lo verás";
            public const string Gold = "¡Esta es la del encargo! A la caja fuerte";
            public const string Clock = "Hay un encargo con hora: toca el reloj para mirarla";
            public const string Hour = "Aquí está la hora del día";
            public const string Beacon = "¡Es la hora! Enciende el faro";
            public const string Show = "El faro guía la nave del correo";
        }

        /// <summary>Cosecha de palabras (pantalla: las siete letras que giran, la bandeja, «Sembrar» y «Borrar» abajo y el planeta del huerto). La práctica arma CASA con las letras I R A O A S C.</summary>
        public static class Cosecha
        {
            public const string First = "Forma CASA: toca primero la C";
            public const string Next = "Sigue con la A, la S y otra A";
            public const string Sow = "Toca Sembrar para guardarla";
            public const string Sprout = "¡Brotó! Cada palabra hace crecer tu huerto";
            public const string Erase = "Si te equivocas, Borrar vacía la bandeja";
        }

        /// <summary>¿Verdad o disparate? (pantalla: la placa con la frase, la barra de señal y los botones VERDAD y DISPARATE abajo).</summary>
        public static class Disparate
        {
            public const string Truth = "Si es verdad, toca VERDAD";
            public const string Nonsense = "Si no tiene sentido, toca DISPARATE";
            public const string Rhythm = "Responde rápido, pero lo que cuenta es acertar";
            public const string Unclear = "Si una frase no está clara, mantenla presionada";
        }

        /// <summary>La estrella intrusa (pantalla: cinco estrellas con una palabra, una figura que aparece al contestar y su nombre). La práctica usa cuatro frutas y un zapato.</summary>
        public static class Intrusa
        {
            public const string Look = "Cuatro estrellas comparten algo; una no";
            public const string Tap = "Toca la intrusa";
            public const string Spark = "La chispa une las otras cuatro: son frutas";
            public const string Atlas = "Cada figura que dibujas se guarda en tu atlas";
        }

        /// <summary>Todos los textos (con el peor caso de los que cambian según la jugada) para comprobar que caben en el globo.</summary>
        public static IEnumerable<(string Game, string Step, string Text)> All()
        {
            yield return ("todos", "listo", Ready);

            yield return ("anagramas", "definición", Punta.ReadDefinition);
            yield return ("anagramas", "la tengo", Punta.Have);
            yield return ("anagramas", "letras", Punta.Letters);
            yield return ("anagramas", "ayuda", Punta.Help);
            yield return ("anagramas", "sola", Punta.FoundAlone);
            yield return ("anagramas", "mostrada", Punta.Shown);
            yield return ("anagramas", "con ayuda", Punta.FoundWithHelp);
            yield return ("anagramas", "también", Punta.HelpAlso);

            foreach (var label in new[] { "0", "7", "10", "1/4", "0.75", "100%", "500 + 250" })
            {
                yield return ("aterrizaje", "arrastra " + label, Aterrizaje.Drag(label));
                yield return ("aterrizaje", "otra vez " + label, Aterrizaje.Again(label));
                yield return ("aterrizaje", "casi " + label, Aterrizaje.Close(label));
            }
            yield return ("aterrizaje", "bien", Aterrizaje.Good);

            yield return ("calculo", "celda", Calculo.Cell);
            yield return ("calculo", "más", Calculo.Plus);
            yield return ("calculo", "otra celda", Calculo.OtherCell);
            yield return ("calculo", "listo", Calculo.Done);

            yield return ("freno", "lanzar", Freno.Launch);
            yield return ("freno", "alto", Freno.Stop);
            yield return ("freno", "casi", Freno.Missed);
            yield return ("freno", "frenó", Freno.Braked);
            yield return ("freno", "tocó con alto", Freno.Tapped);

            yield return ("meteoros", "existe", Meteoros.Exists);
            yield return ("meteoros", "dejar caer", Meteoros.LetItFall);
            yield return ("meteoros", "casi palabra", Meteoros.MissedWord);
            yield return ("meteoros", "casi inventada", Meteoros.MissedFake);

            yield return ("secuencia", "mira", Rastro.Watch);
            yield return ("secuencia", "repite", Rastro.Repeat);
            yield return ("secuencia", "bien", Rastro.Good);
            yield return ("secuencia", "casi", Rastro.Missed);

            yield return ("engranajes", "cartel", Engranajes.Cartel);
            yield return ("engranajes", "motor", Engranajes.Motor);
            yield return ("engranajes", "arrancar", Engranajes.Start);

            yield return ("parejas", "primera luz", Constelaciones.First);
            yield return ("parejas", "otra luz", Constelaciones.Second);
            yield return ("parejas", "su pareja", Constelaciones.Partner);
            yield return ("parejas", "línea dorada", Constelaciones.GoldLine);
            yield return ("parejas", "de memoria", Constelaciones.Row);

            yield return ("correo", "buzón", Correo.First);
            yield return ("correo", "hoja", Correo.Sheet);
            yield return ("correo", "caja fuerte", Correo.Gold);
            yield return ("correo", "reloj", Correo.Clock);
            yield return ("correo", "hora", Correo.Hour);
            yield return ("correo", "faro", Correo.Beacon);
            yield return ("correo", "nave", Correo.Show);
            yield return ("bodega", "empezar", Bodega.Begin);
            yield return ("bodega", "esclusa", Bodega.Watch);
            yield return ("bodega", "así quedó", Bodega.Reveal);
            foreach (var name in new[] { "la llave", "la campana", "el farol", "la manzana", "el hongo", "la taza", "el paraguas", "el libro", "la gema", "el reloj de arena", "la pluma", "la bellota" })
                yield return ("bodega", "toca " + name, Bodega.Ask(name));
            yield return ("bodega", "error", Bodega.Wrong);

            yield return ("cosecha", "letras", Cosecha.First);
            yield return ("cosecha", "sigue", Cosecha.Next);
            yield return ("cosecha", "sembrar", Cosecha.Sow);
            yield return ("cosecha", "brotó", Cosecha.Sprout);
            yield return ("cosecha", "borrar", Cosecha.Erase);

            yield return ("disparate", "verdad", Disparate.Truth);
            yield return ("disparate", "disparate", Disparate.Nonsense);
            yield return ("disparate", "ritmo", Disparate.Rhythm);
            yield return ("disparate", "no está clara", Disparate.Unclear);

            yield return ("intrusa", "cuatro comparten", Intrusa.Look);
            yield return ("intrusa", "toca la intrusa", Intrusa.Tap);
            yield return ("intrusa", "chispa", Intrusa.Spark);
            yield return ("intrusa", "atlas", Intrusa.Atlas);

            yield return ("stroop", "tinta", Stroop.InkRule);
            yield return ("stroop", "palabra", Stroop.WordRule);
            yield return ("stroop", "casi tinta", Stroop.MissedInk);
            yield return ("stroop", "casi palabra", Stroop.MissedWord);
            foreach (var word in new[] { "rojo", "azul", "amarillo", "blanco" })
            {
                yield return ("stroop", "bien tinta " + word, Stroop.Good("La tinta era " + word));
                yield return ("stroop", "bien palabra " + word, Stroop.Good("La palabra decía " + word));
            }
        }
    }
}
