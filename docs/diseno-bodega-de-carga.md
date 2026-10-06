# Bodega de carga: juego nuevo de Memoria (aprobado por Ricardo el 5-oct)

Juego nuevo, id `bodega`, área Memoria. Ocupa el lugar de **Bitácora de Misión**, que Ricardo retiró el 5-oct («lo he jugado
unas 20 veces y no me convence; a mi hermana y a un familiar tampoco les gustó»). Con él, Memoria vuelve a 5 juegos y la app
a 19.

- **Boceto aprobado:** `docs/previews/bodega-de-carga-boceto.html` (https://claude.ai/artifact/C6ERowzeHeR8zJzFtwLbfN,
  versión 3: «movimiento con flow» y sin caras que reaccionen). Con `?etapa=N` se entra directo a una etapa.
- **Ricardo, sobre la versión 1:** «me gustó bastante la idea, cómo se ve, la velocidad es bien adecuada… el movimiento
  del robot está demasiado robotizado, con movimientos muy rígidos; igual cuando las monedas dan vueltas [las puertas].
  Hay que darle un flow, que la dinámica sea más suave pero con el mismo estilo. Todo lo demás aprobado».
- **La versión 2 ya trae ese flow** (sección 6). Es la referencia.
- **Por qué este y no otros:**
  - «¿Qué cambió?» se descartó por parecerse a «Tidal Treasures» de Lumosity.
  - «Últimas señales» se descartó por parecerse a Rastro de luz.
  - Bitácora no enganchaba: demasiadas fases y el resultado llegaba minutos después.
  - Este tiene rondas cortas, respuesta inmediata y movimiento visible.

## 1. La idea

La bodega de la nave es un **módulo redondo**: escotillas alrededor, una **ventanilla** con un planeta (borde dorado) y un
**robot** en el centro.

1. **El robot guarda:** el robot se desliza hacia una escotilla y le lanza un haz de luz. La escotilla se abre, el objeto
   aparece (rebota un poco y flota) mientras arriba se lee su nombre, suena **la nota propia de esa escotilla** y la
   escotilla se cierra.
2. **El robot cambia cajas de lugar** (etapa 5 en adelante): vuela a una escotilla, saca la caja **cerrada** con su haz,
   la lleva colgando (se mece) por el anillo y la deja en otra escotilla vacía. Hay que recordar qué había adentro.
3. **La bodega gira** (etapa 8 en adelante): toma un pequeño impulso hacia atrás, gira 2 o 3 posiciones y se asienta
   suave. La ventanilla gira con ella y sirve de referencia. El robot la sigue con la mirada.
4. **Los pedidos:** «¿Dónde está el farol?», con el objeto dibujado en la tarjeta de arriba. Se toca la escotilla:
   - **Correcta:** se abre, suena su nota, y el objeto vuela girando al **carro de reparto** de abajo, donde aterriza con
     un rebote.
   - **Equivocada:** se abre igual y **muestra qué había ahí** (otro objeto, o «vacía»), tiembla y suena un golpe suave.
     Después la correcta brilla en dorado y se abre sola. **Siempre se aprende dónde estaba.**
   - **El robot no reacciona al desempeño:** no pone caras felices ni tristes ni niega con la cabeza. Es la regla de
     patentes de Akili (`CLAUDE.md`). Su expresión es siempre la misma; solo trabaja y mira.

**Qué entrena:** memoria de ubicación de objetos, es decir, saber qué cosa quedó dónde. Además actualiza la memoria cuando
algo se mueve y obliga a reorientarse cuando la bodega gira.

**Sin información oculta** (regla de Ricardo): todo pasa a la vista. Es memoria, no deducción.

## 2. Respaldo científico (PubMed, revisado el 5-oct)

- **El test PAL de la batería CANTAB** usa el mismo paradigma: se abren cajas y luego hay que recordar dónde estaba cada
  figura.
  - Separó Alzheimer de controles con un 92 % de sensibilidad y un 86 % de especificidad, y se correlacionó con el MoCA
    con r = 0,8 (Hicks et al., 2020, [DOI](https://doi.org/10.1017/S1041610220003841)).
  - Funciona en línea y sin supervisión: con 14.528 personas, rinde peor con la edad, con más quejas de memoria y con
    deterioro leve informado por la persona (Ashford et al., 2024, [DOI](https://doi.org/10.14283/jpad.2023.117)).
  - En 16.683 personas, la queja subjetiva de memoria predijo cuánto bajaba el rendimiento en esta tarea con el tiempo
    (Kang et al., 2025, [DOI](https://doi.org/10.1186/s13195-024-01641-2)).
  - Su baja con la edad se relaciona con el volumen del lóbulo temporal medial (Wearn et al., 2021,
    [DOI](https://doi.org/10.1016/j.neuroimage.2021.118214)).
- **Hay normas por edad** para el aprendizaje de ubicaciones (m-LLT, de 50 a 89 años) (St-Hilaire et al., 2025,
  [DOI](https://doi.org/10.1093/arclin/acaf009)).
- **Aprender sin errores ayuda a los mayores.** En una cómoda virtual con objetos cotidianos, los adultos mayores
  recordaron mejor cuando no se equivocaban al aprender (Scheper et al., 2020,
  [DOI](https://doi.org/10.1007/s40520-020-01603-2)). Pasó lo mismo en Alzheimer inicial (Scheper et al., 2023,
  [DOI](https://doi.org/10.1111/jnp.12330)). De ahí salen dos decisiones: empezar fácil, y que tras un error se abra la
  correcta.
- **Un punto de referencia facilita reorientarse** después de un giro (Janzen et al., 2020,
  [DOI](https://doi.org/10.3389/fnhum.2020.00121)). De ahí sale la ventanilla.
- **La práctica de memoria mejora la memoria en mayores, con efecto moderado:**
  - el entrenamiento con estrategias da g ≈ 0,41, y se mantiene (g ≈ 0,42) meses después (Chen et al., 2022,
    [DOI](https://doi.org/10.1037/pag0000712); Verhaeghen et al., 1992,
    [DOI](https://doi.org/10.1037//0882-7974.7.2.242));
  - entrenar memoria de trabajo en personas de 75 a 85 años transfirió a tareas cotidianas y a la representación espacial
    (Borella et al., 2019, [DOI](https://doi.org/10.1016/j.jagp.2019.01.210)).
- **Lo que NO se dice en la app:** que previene o detecta Alzheimer. Nada de nombres de enfermedades. Va la nota común «No
  es un diagnóstico».

## 3. Etapas (12 niveles del DDA común)

| Etapa | Escotillas | Objetos | Extras | Tarjeta «NUEVO» |
|---|---|---|---|---|
| 1 | 6 | 2 | | bodega (la presentación) |
| 2 | 6 | 3 | | |
| 3 | 8 | 3 | | |
| 4 | 8 | 4 | | |
| 5 | 8 | 4 | 1 caja cambia de lugar | mueve |
| 6 | 8 | 5 | | |
| 7 | 10 | 5 | 1 caja cambia | |
| 8 | 8 | 4 | la bodega gira | gira |
| 9 | 10 | 5 | gira | |
| 10 | 10 | 5 | 1 caja + gira | |
| 11 | 10 | 6 | 1 caja + gira | |
| 12 | 10 | 7 | 2 cajas + gira | |

- **Partida:** 6 pedidos en Precisión. En Reto, 120 s y los pedidos que alcancen.
- **Pedido:** se piden todos los objetos guardados, uno por vez y en orden al azar.
- **Dificultad:**
  - `AdaptiveDifficulty` común, con objetivo 0,80 (0,85 en mayores).
  - Lo que se mide por pedido es el porcentaje encontrado al primer intento.
  - En el boceto, un pedido perfecto sube una etapa y dos errores o más bajan una.
- **Mayores:** tiempos ×1,35 (`slow()` en el boceto), para mirar cada objeto y seguir la caja.
- **Giro:** de 2 o 3 posiciones, en cualquier sentido. Nunca de 4 o más.
- **Escotilla destino de la caja:** siempre una vacía.

**Grupos de etapa para la pantalla final (5):**

1. pocos objetos (etapas 1-2);
2. más escotillas (3-4);
3. cajas que se mueven (5-7);
4. la bodega gira (8-9);
5. todo junto (10-12).

## 4. Pantalla

- **Arriba:**
  - «Pedido N de 6»;
  - a la derecha, la **racha** («Racha ×3», píldora dorada que respira) desde 2 aciertos seguidos al primer intento, o
    «Récord: N objetos» si no hay racha.
- **Tarjeta de 2 líneas** según la fase:
  - «Mira dónde guarda cada cosa» / «N objetos»;
  - «El robot guarda» / el nombre del objeto, con su dibujo;
  - «El robot cambia una caja de lugar» / «Síguela con la vista»;
  - «¡La bodega gira!» / «Fíjate dónde queda la ventanilla»;
  - «Pedido» / «¿Dónde está el farol?», con borde dorado;
  - «¡Pedido perfecto!» o «¡Pedido completo!» / «N de M al primer intento · ¡nuevo récord!».
- **La bodega:**
  - anillo de radio 125 u con centro en (180, 335), escotillas de radio 27 y marco de 5;
  - posición 0 = la ventanilla;
  - remaches del anillo que giran con la bodega.
- **Carro de reparto**, abajo: un círculo por objeto del pedido.
  - Al llegar un objeto aterriza con un rebote.
  - Borde menta y ✓ si fue al primer intento; borde gris si no.
- **Aviso de abajo** al terminar un pedido imperfecto: «Casi perfecto» o «Pedido completo», más un truco que rota:
  - «imagina el objeto dentro de su escotilla»;
  - «dile su nombre en voz baja al guardarlo»;
  - «escucha la nota de cada escotilla».
- **Objetos:** 12 siluetas fáciles de nombrar, con color propio: llave, campana, farol, manzana, hongo, taza, paraguas,
  libro, gema, reloj de arena, pluma y bellota.
  - Se puede reusar el arte de Bitácora (`BitacoraSprites`), pero **sin la estrella de mar** (regla de símbolos), **sin
    copa** (parece un cáliz) y **sin ancla**.
  - Siempre con el nombre escrito en la tarjeta.

## 5. Sonido

- **Cada escotilla tiene su nota** (pentatónica de la app, una octava arriba desde la 11): al guardar, la bodega suena
  como una melodía, y al abrir la correcta vuelve su nota. Es la mecánica propia del juego: se recuerda con la vista y con
  el oído.
- **Puertas:** abrir es un siseo agudo y cerrar un golpe grave suave.
- **Robot:** zumbido al empezar, un «piu» ascendente con cada haz, y un clank al tomar o dejar la caja.
- **Movimientos:** soplido de vuelo con la caja, y matraca de engranes (un clic cada 110 ms) mientras la bodega gira.
- **Respuestas:**
  - acierto: la nota de la escotilla, más un destello que sube de tono con la racha;
  - error: golpe sordo;
  - pedido perfecto: arpegio.

## 6. Movimiento con flow (pedido de Ricardo; NO volver a lo rígido)

Todo lo que se mueve sigue a su objetivo con un **resorte amortiguado**, nunca en línea recta con velocidad fija. En el
boceto: `spring(x, v, objetivo, dt, frecuencia, amortiguación)`, integrado cada cuadro con el `dt` real (tope 0,05 s).

- **Robot:**
  - **Se desliza** hacia la escotilla en la que trabaja (queda a 52 u del borde del anillo) y vuelve al centro. Es un
    resorte de 1,5 Hz con amortiguación 0,8.
  - **Se inclina** según su velocidad (máximo 0,22 rad) y **mira** hacia donde trabaja: el visor se corre hacia ese
    lado, como si girara en 3D. El giro es un resorte de 2,2 Hz con amortiguación 0,75.
  - **Antena con retraso:** se queda atrás al moverse y rebota (resorte de 3,2 Hz, amortiguación 0,3).
  - **Flota** con dos ondas que no coinciden (520 y 1370 ms), así que nunca se ve mecánico. Su sombra queda siempre
    horizontal.
  - **Se aplasta un poco** al lanzar el haz (12 %, vuelve solo). Parpadea cada ~3,6 s.
- **Haz de luz:** crece con suavidad (0,26 s), late, lleva 3 chispas que viajan por él y **se apaga** en 0,22 s, sin
  cortarse de golpe.
- **Puertas:** son **dos hojas que se abren hacia los lados** (la junta va en dirección al centro), **no** una moneda que
  gira.
  - Abrir: resorte de 2,6 Hz con amortiguación 0,55, que se pasa apenas y vuelve.
  - Cerrar: amortiguación 0,85, que cierra firme.
  - La junta brilla un instante al empezar a abrir.
- **Objeto:** aparece con un rebote (`easeBack`, 0,42 s, hasta 1,08) y flota 1,5 u mientras se ve. Vuela al carro girando
  un poco en arco y aterriza con rebote.
- **Caja que cambia de lugar:**
  1. sale de la escotilla con el haz y llega a la garra del robot;
  2. cuelga de una línea de luz y **se mece** con la inercia (resorte de 1,3 Hz, amortiguación 0,22);
  3. el robot vuela **por el anillo**, por el camino más corto, y la deja entrar en la escotilla nueva.
- **Giro de la bodega:** pequeño impulso atrás (3,5 % en el primer 12 %), luego giro con un asentarse suave al final
  (`easeSpin` del boceto). Dura 1,5 s.

**Movimiento reducido:**

- **Se quita:** resortes, flotación, chispas, inclinación, mecerse y temblor. Todo llega a su lugar al instante.
- **Se mantiene:** las escotillas se abren y cierran, la caja se ve en su escotilla nueva y la bodega aparece ya girada,
  con los tiempos de mirada intactos (`Motion.Hold`). Sin estos tiempos la tarea cambiaría.

## 7. Medidas al final (juego estrella)

- **Encontrados al primer intento:** «N de M», más el porcentaje. Es la misma medida que la de primer intento del test
  PAL.
- **Tu racha más larga.**
- **Tu bodega más grande hoy:** el pedido más grande sin errores.
- **Tu récord:** la bodega más grande de siempre, guardada entre partidas en preferencias que **van en el respaldo**.
- **Etapa más alta**, de 5 grupos.
- **Consejo** en `ResultAdvice`: «Imagina cada objeto dentro de su escotilla y dile su nombre en voz baja».
- **Nota común:** «Medida de esta partida… No es un diagnóstico».
- Se agrega su justificación a `docs/medidas-juegos-estrella.md`, con las referencias de la sección 2.

## 8. Tutorial (NubiCoach, ≤ 30 s, se aprende haciendo)

Sobre un pedido de la etapa 1 (2 objetos), con el sistema de zonas `keep`:

1. **Notice.** El hueco es la bodega mientras el robot guarda; la zona protegida es la tarjeta. Nubi dice: «Mira dónde
   guarda cada cosa».
2. **Touch.** El hueco es la escotilla correcta; la zona protegida es la tarjeta del pedido. Nubi dice: «¿Dónde está la
   llave? Toca su escotilla». Es un toque real.
3. **Notice.** Nubi dice: «Si te equivocas, se abre la correcta: así aprendes dónde estaba».

Después: «¡Listo! Ahora va en serio».

## 9. Originalidad y riesgos

- **Lumosity «Memory Matrix»:** recordar qué casillas de una cuadrícula se encienden. Acá no hay cuadrícula; hay objetos
  distintos en un anillo que gira.
- **Lumosity «Memory Serves»:** llevar la cuenta de maletas. Es otra tarea.
- **Lumosity «Tidal Treasures»:** detectar el objeto nuevo. Es otra tarea; es la propuesta C, que se descartó.
- **El test PAL de CANTAB** (Cambridge Cognition) usa el mismo paradigma clásico de ubicación de objetos, que es de uso
  libre en la investigación. Nombre, arte, sonidos, la bodega que gira, la caja que se mueve y la nota de cada escotilla
  son propios. **Va a la lista del abogado.**
- **Símbolos neutros:** sin estrellas con puntas, medias lunas ni cruces.
