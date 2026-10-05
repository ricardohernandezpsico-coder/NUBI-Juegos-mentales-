# Engranajes — juego nuevo de Razonamiento (aprobado por Ricardo el 4-oct)

Juego nuevo, id `engranajes`. Ocupa el lugar de Tráfico Estelar (retirado el 4-oct por su parecido con «Train of
Thought» de Lumosity) y deja Razonamiento en 4 juegos: Carga exacta, Aterrizaje, Acoplamiento y Engranajes. La app queda
en 19.

- **Boceto aprobado:** `docs/previews/engranajes-boceto.html` (https://claude.ai/artifact/RyRcvJcKCfEANMoAXz31YE, versión
  3, «escena»).
- **Ricardo:** «perfecto, mucho mejor, me gusta, vamos allá». Lo ve como posible **juego bandera** de la app: pidió máxima
  calidad visual, sonido y movimiento, y que **todo tenga sentido**.
- **Versiones descartadas:**
  - Ramas sueltas que no llevan a nada: «engranajes que no cumplen ningún sentido».
  - Cadenas en zigzag sin motivo.
  - Textos chicos o cortados.
  - Un cohete que despegaba incompleto.
- **Antecedentes:** antes se descartaron dos juegos de deducción con información oculta (regla escondida y código secreto)
  por frustrantes. Este tiene todo a la vista.

## Estado (5-oct): hecho, a probar en el teléfono

Implementado en `Games/Engranajes/` (contrato puro y 37 pruebas de Unity entre contrato, disposición, arte y vista; controlador en tres archivos; sonidos y arte propios) y en la app (`data/Engranajes.kt`, registro, ícono,
medida, pantalla final, cohete guardado en preferencias `engranajes_rocket` que van en el respaldo). Lámina de la pantalla compuesta con los sprites reales: [`docs/previews/engranajes.png`](previews/engranajes.png)
(`tools/art-preview/engranajes.py`). **Diferencias con el boceto** (todas a propósito):

- El motor NO gira antes de responder (en el boceto sí, y sus dientes atravesaban los del vecino quieto): gira su flecha alrededor, y las piezas se ven quietas hasta que arranca.
- Al arrancar, todos los engranajes giran JUNTOS con una aceleración suave (así los dientes encajan siempre; en el boceto cada uno arrancaba a su turno y por un momento se pisaban). La «cascada» se ve en el pulso de luz,
  el destello de cada engranaje al llegarle la fuerza, el clic y la nota de cada paso (cada 0,15 s; 0,21 s en mayores) y la reacción de cada pieza (la llama de la turbina y las barras llegan con la fuerza).
- El hueco de «Arma tú» ya viene con las fases de los dientes calculadas (en el boceto los engranajes de después del hueco tenían los dientes sueltos hasta elegir la pieza).
- La escena (sala + cohete) se achica pareja en pantallas bajas y nunca pasa de 0,96 para que la flecha del motor y la aleta no se corten; los textos de la escena no bajan de 14 dp aunque se achique.
- El aviso de abajo admite dos líneas y el truco (en el boceto un texto largo se cortaba).
- Las tarjetas «NUEVO» se muestran UNA vez por instalación (preferencias de Unity), no en cada partida; «Cómo se juega» de la pausa repite el tutorial.
- Con «quitar animaciones» el aro de la pieza preguntada queda quieto (en el boceto desaparecía y la pregunta solo se distinguía por el color).

## 1. La idea

A la izquierda, la **sala de máquinas** con el motor. A la derecha, **el cohete** con cuatro piezas rotuladas: Antena (punta),
Compuerta, Carga (elevador) y Turbina (cola). Cada cadena de engranajes lleva la fuerza del motor a una pieza. La persona mira
la máquina y decide qué hará **la pieza que brilla** cuando arranque el motor. Al responder, la máquina arranca en cascada y se
ve por qué.

## 2. Reglas físicas (contrato puro, con pruebas)

- Dos engranajes que se tocan giran en sentido contrario.
- Una correa recta mantiene el sentido; una correa cruzada lo invierte.
- La velocidad va según el tamaño: uno chico gira más rápido que uno grande (razón de radios primitivos). Los intermedios no
  cambian la razón entre el motor y la pieza.
- Tres engranajes que se tocan en triángulo **se traban**: la máquina no gira.
- Barra dentada a la derecha de su engranaje: si este gira como el reloj, la barra baja.
  - Carga: sube o baja.
  - Compuerta: si la barra sube, se abre.
- El motor gira como el reloj o al revés, al azar, y su flecha lo muestra.

## 3. La máquina (generación)

- **Cuadrícula de 4 columnas por 6 filas** en la sala, más la columna del cohete, donde van los engranajes de las piezas.
  - Casillas alternadas grandes (15 dientes) y chicas (10 dientes), como un tablero de ajedrez. Así los vecinos engranan
    justo y los de la diagonal nunca se tocan.
  - Paso 50 u, radio primitivo 30/20, punta 33/23, diente de 6 u.
  - Filas de las piezas: antena 0, compuerta 2, carga 3, turbina 5. Compuerta y carga nunca van activas a la vez, porque
    son vecinas.
- **Caminos limpios:**
  - Búsqueda en profundidad que prefiere avanzar derecho mientras se acerca y doblar hacia el destino.
  - Se generan varias y se elige la de menos dobleces (puntaje: dobleces × 3 + largo).
  - Cada casilla nueva solo puede tocar a la anterior.
  - A la columna del cohete se entra solo desde la izquierda.
- **Ramas con propósito:** desde un engranaje del camino sale otro camino que llega a **otra pieza del cohete**, que también
  funciona al arrancar. Nunca hay ramas que terminen en la nada.
- **Correas y hueco:** en un tramo recto de tres, se quita el del medio.
  - Correa: recta o cruzada.
  - Hueco (etapa «Arma tú»): las dos piezas posibles son «engranaje», que deja el mismo sentido, y «correa cruzada», que
    lo invierte. Siempre dan resultados distintos.
- **Trampa:** un engranaje chico fuera de la cuadrícula que toca a dos vecinos del camino, formando un triángulo.
- **Fases de los dientes:** se calculan para que cada diente quede frente al hueco del vecino; las velocidades exactas
  mantienen el encaje al girar.

## 4. Etapas (12 niveles del DDA común)

| Niveles | Pregunta | Piezas | Extras |
|---|---|---|---|
| 1-2 | ¿Cómo girará la antena / turbina? | 1 | Camino corto; luego motor lejos |
| 3-4 | Igual | 2 (una rama) | Correa recta o cruzada |
| 5-6 | ¿La carga sube o baja? / ¿La compuerta se abre o se cierra? | 1-2 | |
| 7-8 | ¿Gira más rápido o más lento que el motor? (antena = grande, turbina = chica) | 1-2 | Correa |
| 9 | Giro, con posible traba (3 botones) | 2 | Trampa al 45 % |
| 10 | Arma tú: ¿qué pieza hace SUBIR la carga? | 1 | Hueco |
| 11 | Carga o compuerta | 3 | 2 ramas y una correa |
| 12 | Arma tú: ¿qué pieza ABRE la compuerta? | 2 | Hueco, rama y correa |

- **Dificultad:** `AdaptiveDifficulty` común (objetivo 0,80; 0,85 en mayores). En el boceto se probó: 2 aciertos seguidos
  suben de etapa, un error mantiene la etapa con otra máquina parecida, y 2 errores seguidos bajan una.
- **Mayores:** más tiempo de cascada para mirar.
- **Precisión:** 10 máquinas. **Reto:** 120 s.
- **Tarjeta «NUEVO»:** la primera vez que aparece algo nuevo (ramas, correas, carga, compuerta, velocidad, traba, arma)
  aparece una tarjeta con dos engranajes animados que espera un toque. Los textos están en el boceto (`INTROS`).

## 5. Presentación (como el boceto)

- **Cabecera:** «Cohete n.º N», 10 luces de progreso y «N cohetes en órbita».
- **Pregunta:** en una barra, a 18 dp, que pasa a dos líneas cortando cerca del medio si es larga.
- **Sala de máquinas:**
  - placa metálica con tornillos y rayas tenues;
  - engranajes de arcilla: motor celeste, camino lila, piezas doradas, trampa coral;
  - soporte oscuro del eje, rayos y brillo;
  - «MOTOR» en una etiqueta sobre el propio motor;
  - flecha del motor con su sentido;
  - correas con su etiqueta «recta» o «cruzada»;
  - hueco punteado con «?» que late.
- **El cohete:**
  - casco degradado, aletas rojas y tobera;
  - **10 ventanillas** que se encienden con cada acierto;
  - radar de la antena que gira con su eje;
  - compuerta con portón que sube y baja, y luz cuando se abre;
  - elevador con caja;
  - turbina de 4 aspas, con llama cuando gira;
  - ejes visibles de cada engranaje dorado a su pieza;
  - **rótulos en etiquetas claras**, la pieza preguntada en dorado y con un aro que late.
- **Al responder:**
  - la cascada avanza un paso cada 150 ms;
  - cada engranaje acelera suave (0,3 s);
  - un **pulso de luz** recorre cada unión;
  - cada paso suena con un clic de dientes y una nota pentatónica;
  - cada pieza tiene su sonido: zumbido de la antena, rugido de la turbina, motor eléctrico de la compuerta o carga con su
    golpe al parar;
  - si hay traba: forcejeo del motor, golpe y triángulo coral.
- **Acierto:** acorde, chispas en la pieza y una luz que vuela a la cabecera.
- **Error:** explicación en una línea y el **truco de Nubi**: flechas que alternan el sentido a lo largo del camino a la
  pieza. Si la pregunta es de velocidad: «compara el tamaño del motor con el de la pieza».
- **Botones:** grandes, con ícono y texto, que se hunden al tocarlos.

## 6. El cohete (progreso que se guarda)

- 10 luces por cohete, que **se guardan entre partidas** en preferencias que se respaldan.
- Cuando se juntan las 10, **el cohete de la escena despega** en ese momento: tiembla, llama y humo, «¡Despegue! Tu cohete
  n.º N va a la órbita». Después empieza uno nuevo y la partida sigue.
- **Nunca despega incompleto.** Al final de la partida: «Faltan N luces: tu cohete espera en el hangar».

## 7. Medidas al final

- Máquinas acertadas.
- Etapa más alta (de 5 grupos: giro, ramas y correas, movimiento, velocidad, trampas y armar).
- Luces del cohete y cohetes en órbita.
- Ritmo (segundos por máquina).
- Nota común «Medida de esta partida… No es un diagnóstico».
- Consejo en `ResultAdvice`: «Sigue el camino desde el motor diciendo "al revés, al derecho…" en cada engranaje».

## 8. Tutorial (Nubi entrenadora) y movimiento reducido

- **Tutorial con `NubiCoach`** (≤ 30 s), sobre una máquina fácil:
  1. Mirar la flecha del motor: «El motor gira así».
  2. Mirar la pieza que brilla: «Esta es la pieza de la pregunta».
  3. Tocar el botón correcto: «Cada engranaje que toca gira al revés».
- **Movimiento reducido:**
  - **Se quita lo decorativo:** pulso, chispas, aceleración, temblor y llamas.
  - **Se mantiene:** el resultado se muestra en una posición fija (flechas de sentido, barra arriba o abajo) y los tiempos
    se conservan (`Motion.Hold`).

## 9. Originalidad

- Los acertijos de «¿hacia dónde gira el engranaje?» son clásicos y de uso libre.
- No encontramos un juego de engranajes en Lumosity, Peak ni Elevate (búsqueda del 4-oct).
- Nombre, arte, escena del cohete y sonidos propios.
- Va a la lista del abogado como juego nuevo.
