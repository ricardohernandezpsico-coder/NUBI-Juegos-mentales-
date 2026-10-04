# Revisión de los 19 juegos frente a la competencia (4-oct)

Revisé cada juego que queda tras los retiros del 3-4 oct (Cambio de Chip, Comparación, Detective de Series y Ruta
del Tesoro, todos por parecido con Lumosity o por repetición). Lo comparé con lo que conocemos de Lumosity, Peak, Elevate,
NeuroNation, NeuroTracker, Posit (BrainHQ) y Akili, más una búsqueda web puntual de los dos casos dudosos. **No reemplaza
la revisión del abogado**: Peak y Elevate tienen más de 40 juegos cada uno y no los revisé todos. Va con la lista de
`docs/nombre-marca-y-riesgos.md`.

## Riesgo alto

| Juego | Se parece a | Por qué | Propuesta |
|---|---|---|---|
| **Tráfico Estelar** | Lumosity «Train of Thought», el más popular de Lumosity | Allá trenes de colores salen de un túnel y se tocan los cambios de vía para que lleguen a la estación de su color, con trenes de dos colores desde el nivel 8. Acá las cápsulas de colores salen de una compuerta y se tocan los desvíos para que lleguen al planeta de su color y símbolo. Es la misma mecánica con otro arte. | Rediseñar a fondo o retirar. |

Fuentes: [ficha de Lumosity](https://help.lumosity.com/hc/en-us/articles/360050228814-Train-of-Thought-Instructions),
[artículo de Lumosity](https://medium.com/@lumosity/train-of-thought-a-closer-look-at-lumositys-most-popular-game-88bd201ee5f7).

## Símbolos sensibles (regla de Ricardo: nada de estrellas con puntas, cruces ni medias lunas)

- **Tráfico Estelar** (`TrafficSprites.Glyph`) marca sus planetas-puerto con 8 símbolos: corazón, **estrella de 5 puntas**,
  rombo, triángulo, **media luna**, **cruz**, cuadrado y aro. Esos puertos los reusan **Bitácora de Misión** y **Correo
  Estelar**.
- **`SymbolSprite`** (la pieza común de símbolos) incluye `Star` (estrella de 5 puntas) y `Moon` (media luna). Los usan
  las cartas de **Parejas** (`CardsGameContract`) y, según su lista de formas, quizá las señales de **Piloto**
  (`PilotContract.Shapes`): hay que revisarlo.
- Propuesta: cambiarlos por formas neutras que sigan siendo fáciles de distinguir, por ejemplo hexágono, gota, ola,
  pentágono, anillo doble o semicírculo. Así se respeta la regla y se evita cualquier lectura religiosa o política (la
  media luna con estrella y la cruz, sobre todo).

## Riesgo medio (para el abogado, con prioridad)

| Juego | Se parece a | Comentario |
|---|---|---|
| **Piloto Estelar** | NeuroRacer, la base de EndeavorRx de Akili | Conducir y a la vez responder a señales es el corazón de NeuroRacer. Akili tiene tecnología patentada o con patente en trámite sobre «procesamiento de interferencia», y ya tenemos reglas para no copiar sus perfiles. La mecánica (multitarea) es genérica, pero conviene que el abogado mire sus patentes. [Akili](https://www.akiliinteractive.com/science-and-technology), [Nature 2013](https://www.nature.com/articles/nature12486). |
| **Satélites** | NeuroTracker | El seguimiento de varios objetos es una tarea clásica (Pylyshyn, 1988). La patente de NeuroTracker es en 3D: el nuestro es plano y no usa su nombre (regla vigente). |
| **Rescate relámpago** | UFOV de Posit | Ya se rediseñó por la patente US 8,348,671 (29-sep). Se mantiene en la lista. |

## Riesgo bajo (tareas clásicas o diseños propios)

- **Rastro de luz:** recordar un camino de luces se parece al juguete «Simon» (Hasbro) en que cada luz suena. La tarea
  (Corsi) es clásica y los modos (al revés, el cielo gira, en marcha) son propios. Nunca usar el nombre «Simon».
- **Cosecha de palabras:** diferencias con Wordscapes ya documentadas.
- **Parejas Ocultas:** el juego de memoria de pares es antiquísimo.
- **Tinta o Palabra:** Stroop, de 1935; «Dos orillas» es propio y sin el sí/no de Lumosity.
- **Freno de Emergencia:** tarea de señal de alto, clásica.
- **Acoplamiento:** rotación mental clásica (Shepard y Metzler). No es la «Rotation Matrix» de Lumosity, que gira una
  cuadrícula.
- **Aterrizaje Lunar:** estimación en la línea numérica, clásica.
- **Carga exacta:** idea del «24», de dominio público.
- **Bitácora, Rumbo a Casa y Correo Estelar:** sin equivalente comercial conocido.
- **Lluvia de meteoros, ¿Verdad o disparate?, La estrella intrusa y En la punta de la lengua:** tareas clásicas de
  lenguaje (decisión léxica, verificación de frases, categorías, palabra a partir de su definición), con presentación
  propia.
