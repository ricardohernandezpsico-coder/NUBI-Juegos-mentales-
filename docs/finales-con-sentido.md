# Finales con sentido: los 18 juegos (Etapa 3)

**Aprobado por Ricardo el 10-oct-2026, sin cambios.** Boceto: https://claude.ai/artifact/GrXqcMScgaY3rVG7L8Qrox

## Orden del final, para todos los juegos

Es la plantilla de Aterrizaje Lunar (Tarea 70; componente `ui/components/MeaningfulResult.kt`):

1. **Lo que hiciste**, en grande: la medida propia que cada juego ya muestra, más UN dato tuyo, en una o dos líneas cortas.
   - El resto del detalle que hoy muestra cada juego (barras, listas, desgloses) NO se borra. Va detrás de un botón **«Ver el detalle de tu partida»**, de 56 dp o más, cerrado por defecto.
   - Ricardo pidió menos información a la vista: «es demasiada información… lo más importante es lo que la persona hizo».
2. **Tu avance**, solo contigo:
   - tres cuadros: «Hoy», «Tu promedio» y «Tu mejor», con la misma medida y la misma unidad que el juego ya muestra;
   - de las partidas ANTERIORES comparables (`StarMeasures`, como en Aterrizaje), respetando si la medida es mejor hacia arriba o hacia abajo;
   - una frase:
     - «Juega otra vez para ver tu avance.» (sin partidas anteriores)
     - «¡Tu mejor partida hasta ahora!»
     - «Hoy te fue mejor que tu promedio.»
     - «Un poco por debajo de tu promedio: es normal que varíe.»
     - «Igual que tu promedio: vas parejo.»
   - La tolerancia para «parejo» se define por medida y se documenta.
   - Si una medida no se puede mostrar honestamente en número, se muestra con sus palabras. Por ejemplo, Freno usa zonas (ágil, firme, pausado), nunca milisegundos.
3. **Truco para la próxima:**
   - Si el juego ya tiene un truco CONDICIONAL que aplica a esta partida (por ejemplo `DosOrillas`, `Mail`, `Trail`, `Reading`, `Carga`), se muestra ese.
   - Si no, el truco general de la tabla.
4. **Abajo, en chico y sin recuadro:**
   - «¿Por qué importa?»
   - «Pusiste en juego » + «Lo que pusiste en juego» (con la primera letra en minúscula), seguido del texto de «Por qué importa»;
   - **Si «Lo que pusiste en juego» empieza con un verbo en infinitivo** (Recordar, Frenar, Seguir, Captar, Atender, Hacer, Imaginar, Girar, Encontrar, Reconocer, Comprender, Sacar, Ordenar), se arma como «Pusiste en juego lo que usas para » + el texto en minúscula («Pusiste en juego lo que usas para recordar dónde guardaste cada cosa.»); si empieza con un sustantivo («La memoria de lugar…») queda «Pusiste en juego la memoria de lugar…». Regla en `FinalesConSentido.Copy.whyText`, con prueba.
   - «Fuente: <cita corta>»;
   - «Medida de esta partida. No es un diagnóstico.»

Además:
- Se quita del final lo que quede repetido, como en Aterrizaje.
- Nunca percentiles, comparación con otras personas ni promesas de salud.

## Los 18

| # | Juego (id) | Lo que pusiste en juego | Truco general | Por qué importa | Fuente (cita corta · DOI) |
|---|---|---|---|---|---|
| 1 | Constelaciones (`parejas`) | La memoria de lugar: recordar dónde quedó cada cosa. | Da vuelta primero una luz nueva: te muestra más del cielo. | Recordar qué va con qué, como una cosa y su lugar, es la parte de la memoria que más atención pide con los años. | Old y Naveh-Benjamin, 2008 · 10.1037/0882-7974.23.1.104 |
| 2 | Rastro de luz (`secuencia`) | La memoria de trabajo: sostener un orden en la cabeza mientras lo usas. | Junta las luces de a dos o tres, como cuando dictas un número de teléfono. (Los trucos por modo de `Trail` siguen cuando aplican.) | La usamos para comprender, razonar y aprender: guarda por un rato lo que necesitamos mientras lo usamos. | Baddeley, 2003 · 10.1038/nrn1201 |
| 3 | Bodega de carga (`bodega`) | Recordar dónde guardaste cada cosa. | Imagina cada objeto dentro de su escotilla y dile su nombre en voz baja. | Recordar dónde están las cosas es esencial en el día a día, y une tres piezas: la cosa, el lugar y el lazo entre las dos. | Postma, Kessels y van Asselen, 2008 · 10.1016/j.neubiorev.2008.05.001 |
| 4 | La estación de correo (`correo`) | La memoria para lo pendiente: acordarte de hacer algo en el momento justo. | Antes de empezar, dilo en voz baja: «cuando vea…, haré…». (Los `Mail.TIP_*` siguen cuando aplican.) | Acordarse de lo que hay que hacer más tarde es clave para manejar el propio día con independencia. | Hering y otros, 2014 · 10.1007/s00426-014-0566-4 |
| 5 | Tinta o Palabra (`stroop`) | Frenar la lectura automática y cambiar de regla cuando cambia la orilla. | Mira primero de qué orilla llega y recién después la palabra. | Frenar lo automático y adaptarse cuando cambian las reglas son parte de lo que nos permite pensar antes de actuar. | Diamond, 2013 · 10.1146/annurev-psych-113011-143750 |
| 6 | Piloto Estelar (`piloto`) | Atender dos cosas a la vez: llevar la nave y atrapar las señales de tu misión. | Al cruzar cada arco, repite por dentro cuál es la señal de tu misión. | En el día a día solemos coordinar dos cosas que vemos a la vez, como caminar mientras miramos alrededor. Con los años, esa coordinación pide más atención. | Beurskens y Bock, 2012 · 10.1155/2012/131608 |
| 7 | Freno de Emergencia (`freno`) | Frenar a tiempo una acción que ya empezaste. | Lanza sin esperar el ALTO y frena solo si aparece: esperarlo te vuelve más lento. | Detener a tiempo lo que ya no conviene nos permite adaptarnos cuando las cosas cambian de golpe. | Verbruggen y Logan, 2008 · 10.1016/j.tics.2008.07.005 |
| 8 | Satélites (`satelites`) | Seguir con la vista varias cosas que se mueven a la vez. | Une con la mirada los satélites que sigues en una figura, como un triángulo, y sigue la figura. | Seguir varias cosas en movimiento es una capacidad limitada para todos: solo podemos seguir unas pocas a la vez. | Trick, Perl y Sethi, 2005 · 10.1093/geronb/60.2.p102 |
| 9 | Rescate relámpago (`radar`) | Captar de un vistazo qué hay, aunque haya cosas que distraen. | Mira el centro del radar y, apenas se apague, nombra por dentro las formas que viste. | En personas mayores, procesar rápido lo que se ve entre otras cosas se relaciona con las tareas visuales del día a día. | Owsley, 2013 · 10.1016/j.visres.2012.11.014 |
| 10 | Carga exacta (`calculo`) | Hacer cuentas con flexibilidad para llegar a un número. | Mira primero si multiplicar dos celdas te deja cerca de la carga; después ajusta sumando o restando. (Es el de `Carga`, que sigue según el nivel.) | Manejar números con soltura se relaciona con entender riesgos y tomar decisiones con información. | Reyna y otros, 2009 · 10.1037/a0017327 |
| 11 | Engranajes (`engranajes`) | Imaginar paso a paso cómo funciona una máquina antes de tocarla. | Antes de cambiar algo, mira qué piezas quedan después: lo que tocas antes de una rama mueve todo lo que sigue. | Simular en la mente cómo se mueve algo es una forma de razonar que usamos con aparatos y mecanismos, aunque no sepamos cómo se llaman sus partes. | Hegarty, 2004 · 10.1016/j.tics.2004.04.001 |
| 12 | Acoplamiento (`acoplamiento`) | Girar formas en la mente para compararlas. | Elige una parte que destaque, como una punta o un codo, y gira solo esa en tu mente. | Girar formas en la mente es una habilidad espacial. Estas habilidades se relacionan con aprender matemáticas, ciencias y técnica. | Uttal y otros, 2013 · 10.1037/a0028446 |
| 13 | Aterrizaje Lunar (`aterrizaje`) | YA HECHO (Tareas 70 a 73): no se toca. | — | — | Schley y Peters, 2014 |
| 14 | En la punta de la lengua (`anagramas`) | Encontrar una palabra que conoces y que no sale. | Si una no sale, piensa en cómo empieza o en otra parecida: suele destrabarla. | Tener una palabra «en la punta de la lengua» es normal, y se vuelve más frecuente con los años. | Shafto y otros, 2007 · 10.1162/jocn.2007.19.12.2060 |
| 15 | Lluvia de meteoros (`meteoros`) | Reconocer al instante qué palabras existen. | Si dudas, mira el centro de la palabra: ahí suelen esconderse las letras cambiadas. | El vocabulario suele mantenerse, e incluso crecer, con los años. | Verhaeghen, 2003 · 10.1037/0882-7974.18.2.332 |
| 16 | ¿Verdad o disparate? (`disparate`) | Comprender frases rápido y decidir si son ciertas. | Con un «no», lee la frase sin él y después dale la vuelta. (Los trucos por tipo de `Reading` siguen cuando aplican.) | Las frases con «no» piden más esfuerzo que las afirmativas, a todas las personas. Entenderlas bien es parte de leer con atención. | Xiang, Kramer y Nordmeyer, 2020 · 10.1037/xlm0000851 |
| 17 | Cosecha de palabras (`cosecha`) | Sacar palabras de la memoria con rapidez. | Cuando una palabra funciona, busca sus parientes; si se agotan, salta a otra letra inicial. | Para encontrar palabras usamos dos caminos: agrupar parecidas y saltar a otro grupo cuando se agotan. | Troyer, Moscovitch y Winocur, 1997 · 10.1037/0894-4105.11.1.138 |
| 18 | La estrella intrusa (`intrusa`) | Ordenar significados sin dejarte llevar por lo que suele ir junto. | Antes de tocar, pregúntate qué TIPO de cosa es cada una. | Ordenamos lo que sabemos de dos maneras: por tipo de cosa (perro, oso) y por lo que suele ir junto (perro, correa). | Mirman, Landrigan y Britt, 2017 · 10.1037/bul0000092 |

Las fuentes se verificaron en PubMed el 10-oct-2026, con el título, el DOI y el resumen de cada una.

---

## Cómo quedó hecho (Tarea 77)

**Código:** `data/FinalesConSentido.kt` (la tabla de textos de los 17 juegos y la lógica de «Tu avance»), `games/FinalModels.kt` (arma el final de cada juego desde la partida y las medidas guardadas; lógica pura con pruebas) y `ui/components/MeaningfulResult.kt` (el componente, con el botón «Ver el detalle de tu partida»). `GameResultScreen` solo dibuja; la fila común «aciertos · etapa · modo» deja solo etapa y modo en los juegos que ya lo usan (`FinalModels.usesMeaningful`), y el bloque de consejo aparte no sale (el truco va dentro del final).

### Tu avance: de dónde sale cada cuadro y qué cuenta como «parejo»

- **Hoy** es la medida que el juego guarda de esta partida (`StarMeasures`); **Tu promedio** y **Tu mejor** salen de las partidas ANTERIORES de ese juego (hasta las últimas 10; el mismo reloj y un nivel parecido: `StarMeasures.comparable`, como en Aterrizaje). «Tu mejor» incluye hoy. La partida de hoy no se cuenta dos veces aunque ya esté guardada.
- La frase: sin partidas anteriores (o sin medida hoy), «Juega otra vez para ver tu avance.»; mejor que tu mejor anterior (por poco que sea), «¡Tu mejor partida hasta ahora!»; si no, comparada con el promedio: más de la tolerancia por encima, «Hoy te fue mejor que tu promedio.»; más de la tolerancia por debajo, «Un poco por debajo de tu promedio: es normal que varíe.»; dentro de la tolerancia, «Igual que tu promedio: vas parejo.».
- **Tolerancia de «parejo» por medida** (la misma unidad de la medida):

| Juego | Medida (unidad) | Mejor es | Tolerancia | Por qué |
|---|---|---|---|---|
| Constelaciones | memoria de lugar (%) | hacia arriba | 5 puntos | de 8 a 20 oportunidades por partida: una sola pesa de 5 a 12 puntos |
| Bodega de carga | objetos al primer intento (%) | hacia arriba | 5 puntos | de 8 a 15 objetos por partida |
| La estación de correo | encargos cumplidos (%) | hacia arriba | 5 puntos | de 8 a 12 encargos por partida |
| Rastro de luz | luces seguidas (número entero) | hacia arriba | media luz | las luces son enteras; el promedio se muestra redondeado |

- La unidad de los cuadros es la de la medida guardada. **Bodega** muestra el recuadro principal como «9 de 12 objetos» (lo que ya mostraba) pero sus tres cuadros van en **%** («75 %»), porque cada partida trae un total de objetos distinto y los números sueltos no se pueden comparar.

### Qué dato tuyo va a la vista y qué va al detalle

«Lo que hiciste» = la medida propia que el juego ya mostraba, en grande, más UN dato. Si en esta partida se superó un récord, ese es el dato; si no, el primero que haya de esta lista (el resto va al detalle):

| Juego | Medida a la vista | UN dato tuyo |
|---|---|---|
| Constelaciones | «78 %» y «7 de 9 veces fuiste directo a una pareja que ya habías visto» | la racha de memoria más larga |
| Rastro de luz | «5 luces» y «las más largas que repetiste bien en el rastro simple» (sin rastro completo, la frase sin culpa de siempre) | un modo nuevo desbloqueado; si no, la línea del modo que más costó |
| Bodega de carga | «9 de 12 objetos» al primer intento | tu bodega más grande de hoy; si no, tu racha |
| La estación de correo | «73 %» y «8 de 11 encargos cumplidos» | las miradas al reloj cerca de la hora; si no, los cancelados, si no, las cartas bien puestas |

El **detalle** (barras «Por modo», discos por evento y por hora, etapa más alta, racha, ritmo, récord, nota de que lo encontrado por suerte no cuenta, explicación de la memoria para lo pendiente…) NO se borró: va detrás de «Ver el detalle de tu partida» (botón de 56 dp o más, cerrado por defecto), sin repetir lo que ya está en el recuadro principal. Si no queda nada que mostrar, el botón no sale.

### El truco

El condicional del juego si aplica a esta partida (`Constelaciones.tip`, el modo que más costó en `Trail`, `Bodega.tip`, `Mail.tip`); si no, el general de la tabla. Los trucos condicionales ya existentes no cambiaron.

### Capturas y pruebas

`FinalesConSentidoTest` (la tabla y «Tu avance»), `FinalModelsTest` (el final de cada juego) y `Grupo1FinalesScreenshotTest` (capturas Roborazzi `*-final-sentido-*.png` de los cuatro finales y del detalle abierto; el botón mide 56 dp o más; la fila común ya no repite los aciertos).

---

## Grupo 2 (Atención): Tinta o Palabra, Piloto Estelar, Freno de Emergencia, Satélites y Rescate relámpago

**Tolerancia de «parejo» por medida** (en la unidad de la medida; frase y cuadros como en el grupo 1):

| Juego | Medida (unidad) | Mejor es | Tolerancia | Por qué |
|---|---|---|---|---|
| Tinta o Palabra | cuánto te frenó la palabra (ms; se dice en palabras: «casi nada», «+0,4 s») | hacia abajo | 0,1 s (100 ms) | con pocos aciertos de cada tipo la diferencia varía 0,1 s o más, y en pantalla solo se ve en décimas de segundo |
| Piloto Estelar | tus señales a los mandos (%) | hacia arriba | 5 puntos | de 8 a 25 señales de la misión por partida |
| Freno de Emergencia | tu freno (ms; se dice en ZONAS: ágil, firme, pausado) | hacia abajo | 20 ms | una estimación sola trae pocos altos y varía unos 20 ms; la frase mira los milisegundos, pero NUNCA se muestran |
| Satélites | cuántos seguiste de verdad a la vez (con un decimal) | hacia arriba | 0,3 | una partida varía de 0,3 a 0,6 de una a otra |
| Rescate relámpago | tu vistazo (ms) | hacia abajo | 20 ms | la duración de destello varía unos 20 ms entre partidas parecidas |

**Una medida que no se guardaba: Tinta o Palabra.** El juego ya calculaba «cuánto te frenó la palabra» pero no la guardaba, así que no había con qué comparar. Se guarda ahora como una medida estrella más (`stroop`, en `star_measures`, que ya va en el respaldo; menos es mejor; dependiente del nivel) y la ficha del juego y Hoy la muestran en palabras. Las partidas anteriores a este cambio no tienen punto: el primer final dice «Juega otra vez para ver tu avance».

**UN dato tuyo** (si hay récord nuevo hoy, ese; si no):

| Juego | Medida a la vista | UN dato tuyo |
|---|---|---|
| Tinta o Palabra | «+0,4 s» y la frase de siempre («cuánto más tardaste…») | «Cambiar de orilla te costó: +0,2 s» (desde el nivel 3) |
| Piloto Estelar | «58 %» «de las señales de tu misión, a los mandos» | «Señales de tu misión: 14 de 18» |
| Freno de Emergencia | «zona firme» y «promedio de tus últimas 5 partidas» (o «primera lectura») | «Frenaste 6 de 8 altos» |
| Satélites | «2,6 a la vez» y «cuántos seguiste de verdad al mismo tiempo…» | «Encendiste 12 luces» (o las rondas perfectas) |
| Rescate relámpago | «170 ms» y «el destello más breve con el que rescatas casi todas las cápsulas» | «23 cápsulas a salvo» |

**Detrás de «Ver el detalle de tu partida»** (sin repetir lo que ya está arriba): Tinta, la explicación del costo de cambio; Piloto, cómo leer la medida, la ruta, los toques equivocados, el nivel, la racha y los puntos; Freno, el velocímetro de tres zonas, el récord de frenado y cómo leerlo; Satélites, el planeta con sus luces, las rondas perfectas, el récord, la velocidad superada y cómo leerlo; Rescate, las cápsulas a salvo en fila, las rondas perfectas, el destello más corto, los viajes, el récord, el total, «Tu captura» con sus casilleros y cómo leer el vistazo.

**El truco:** el condicional si aplica (Tinta: `DosOrillas.tips`; Piloto: `Piloto.advice`; Freno: «Lanza apenas se encienda la luz, sin esperar al ALTO: así la medida funciona.» cuando no hubo estimación) y si no el general de la tabla.
