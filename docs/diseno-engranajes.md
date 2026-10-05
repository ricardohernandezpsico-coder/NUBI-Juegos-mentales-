# Engranajes: «Taller de reparación» (Razonamiento; rediseño aprobado por Ricardo el 5-oct)

- **Id del juego:** `engranajes`, el mismo de antes. Razonamiento sigue con 4 juegos (Carga exacta, Aterrizaje, Acoplamiento,
  Engranajes) y la app con 19.
- **Boceto aprobado:** `docs/previews/engranajes-taller-boceto.html` (https://claude.ai/artifact/PDoX1VC4YR58jvxv4pTZn8).
  Con `?etapa=N` se entra directo a una etapa.

## Por qué se rehízo

Ricardo probó la primera versión en su teléfono (5-oct) y la rechazó. En ella se miraba la máquina y se elegía qué haría
la pieza que brilla:

- **Las ramas eran adorno:** la pregunta era por una sola pieza, así que la rama a la otra «a nivel cognitivo no genera
  ninguna dificultad». La rama no entraba en el razonamiento.
- **Lo ve como juego bandera:** pidió repensarlo para que supere lo que hay en el mercado. Se le dieron 3 propuestas
  escritas:
  - A, leer toda la máquina;
  - B, arreglarla;
  - C, cambio en marcha.
- **Eligió la B** y aprobó el boceto: «bien, vamos ahora con la propuesta B».

Lo que se conserva de la versión anterior:

- la física;
- el arte de arcilla;
- los sonidos;
- la sala de máquinas;
- el cohete con sus 4 piezas y sus 10 luces, que se guardan entre partidas;
- la cascada al arrancar;
- las medidas y el registro en la app.

Lo que se quita:

- los botones de respuesta («Como el reloj», «Al revés», «Se traba»…);
- la trampa del triángulo;
- la etapa «Arma tú»;
- las preguntas de velocidad.

**Regla de oro (con prueba):** todo engranaje en pantalla está en el camino del motor a alguna pieza que tiene cartel. Si
quitas cualquiera, el resultado cambia. No hay ramas sueltas, piezas sin cartel ni nada decorativo en la sala de máquinas.

## 1. La idea

La máquina que mueve las piezas del cohete viene **mal armada**.

- Cada pieza activa lleva un **cartel** con lo que debe hacer:
  - Antena o Turbina: «reloj» o «al revés», con su flecha;
  - Carga: «sube» o «baja»;
  - Compuerta: «se abre» o «se cierra».
- La persona tiene **una llave**, que vale **un cambio**, y lo hace tocando la máquina:
  - **el motor**: su flecha se da vuelta;
  - **una correa**: se cruza o se descruza frente a ti.
- Después toca **Arrancar**. La fuerza recorre la máquina y cada cartel se pone verde si la pieza cumplió o coral si no.

Lo que hace pensar es que **un cambio antes de una bifurcación cambia todas las piezas que siguen, y uno dentro de una rama
cambia solo esa pieza**. Por ejemplo, si solo falla la turbina y tocas el motor, arreglas la turbina pero rompes la antena.
Así las ramas son el problema y no un adorno.

**Entrena:** planificar, razonar causa y efecto, prever consecuencias antes de actuar y seguir varias cadenas a la vez.

**Sin información oculta** (regla de Ricardo): todo se deduce mirando. La máquina, los carteles y la flecha del motor
están a la vista.

## 2. Reglas físicas (contrato puro)

- **Engranajes:** dos que se tocan giran en sentido contrario.
- **Correas:** la recta mantiene el sentido y la cruzada lo invierte.
- **Barras dentadas:** van a la derecha del engranaje de la Carga y de la Compuerta. Si el engranaje gira como el reloj, la
  barra baja: la carga baja y la compuerta se cierra. Si gira al revés, la carga sube y la compuerta se abre.
- **Motor:** gira como el reloj o al revés, y su flecha lo muestra.

## 3. La máquina (generación, como `logic.js` del boceto)

**Cuadrícula:**

- 4 columnas de sala más la columna del cohete, y 6 filas.
- Casillas alternadas grandes (15 dientes, radio primitivo 30, punta 33) y chicas (10 dientes, 20 y 23), con paso 50.
- Filas de las piezas: Antena 0, Compuerta 2, Carga 3, Turbina 5.
- **Compuerta y Carga nunca van juntas**, porque sus filas son vecinas.

**Árbol limpio** (`build(targets, j, r0)`):

- **Tronco:** el motor en (0, r0) y el tronco horizontal hasta la columna j (1 o 2).
- **Bifurcación:** desde la bifurcación (j, r0) sale una vertical por la columna j hacia arriba y hacia abajo.
- **Recorrido:** en la fila de cada pieza dobla a la derecha hasta la columna 4, donde está el engranaje de la pieza.
- **Pieza en la misma fila:** si una pieza está en la fila r0, el tronco sigue derecho hasta ella.
- **Piezas del mismo lado:** comparten la vertical, lo que da una bifurcación dentro de otra.
- **Una sola pieza:** j puede ser 1, 2 o 3.

**Correas:**

- Salen de un tramo recto de tres engranajes cuyo engranaje del medio tiene solo 2 vecinos y no es el motor ni una pieza.
  Ese engranaje se quita y los extremos quedan unidos por una correa, recta o cruzada al azar.
- Las ternas elegidas no comparten casillas.
- Con 2 o más piezas, cada correa debe mover un **conjunto distinto de piezas** aguas abajo. Así cada correa es una
  decisión distinta.

**Interruptores:**

- El motor, que mueve todas las piezas.
- Cada correa, que mueve las piezas que siguen después de ella. Es una máscara de bits sobre las piezas.

**El problema:**

1. **W** es el conjunto de piezas que viene fallando, elegido según la etapa (tabla del punto 4).
2. **El mínimo de cambios** se calcula como el XOR de las máscaras con 0, 1 o 2 interruptores.
3. **Los carteles** se arman a partir de lo que hace hoy cada pieza: las piezas de W llevan el cartel contrario.

**Al arrancar se juzga el resultado, no el camino.** Cualquier combinación de cambios que deje todas las piezas
cumpliendo cuenta como acierto, aunque no sea la solución guardada.

**Solución guardada:** una solución mínima, que se muestra si la persona se equivoca.

**Pruebas del boceto:** 6.000 máquinas, 500 por etapa: ninguna toca a otra sin estar unida, no hay callejones, la solución
funciona siempre, «sin cambios falla» salvo cuando W está vacío, y el mínimo de cambios coincide con la etapa.

## 4. Etapas (12 niveles del DDA común)

| Etapa | Piezas | Correas | Llaves | Qué falla | Tarjeta «NUEVO» |
|---|---|---|---|---|---|
| 1 | 1 (antena o turbina) | 0 | 1 | la pieza (se arregla con el motor) | taller |
| 2 | 1 | 1 | 1 | la pieza (motor o correa) | correas |
| 3 | 2 (antena + turbina) | 2 | 1 | **una sola** pieza: hay que tocar su rama | ramas |
| 4 | 2 | 2 | 1 | cualquier grupo que se arregle con 1 cambio | |
| 5 | 2 (antena o turbina + compuerta o carga) | 2 | 1 | ídem | barras |
| 6 | 2 con barra | 2 | 1 | ídem; 25 % ya está bien | bien |
| 7 | 3 (antena, compuerta o carga, turbina) | 3 | 1 | ídem | |
| 8 | 3 | 3 | 1 | ídem; 20 % ya está bien | |
| 9 | 2 | 2 | 2 | hacen falta **justo 2** cambios | dos |
| 10 | 3 | 3 | 2 | justo 2 | |
| 11 | 3 | 3 | 2 | 1 o 2; 15 % ya está bien | |
| 12 | 3 | 4 | 2 | justo 2 | |

En «cualquier grupo», el grupo «todas fallan», que se arregla con el motor, sale como mucho el 20 % de las veces.

**Grupos de etapa para la pantalla final (5):**

1. motor y correas (etapas 1-2);
2. ramas (3-4);
3. carga y compuerta (5-6);
4. tres piezas (7-8);
5. dos llaves (9-12).

**Dificultad:**

- `AdaptiveDifficulty` común, con objetivo 0,80 (0,85 en mayores).
- En el boceto: 2 aciertos seguidos suben una etapa y 2 errores seguidos bajan una.
- **Precisión:** 10 máquinas. **Reto:** 120 s.

## 5. Cómo se toca

- **El motor:** se toca el engranaje o su rótulo «MOTOR». La flecha se da vuelta con un giro de 0,3 s y queda dorada.
- **Una correa:** se toca la correa misma, a 26 u o menos de su línea. Se cruza o se descruza con una animación de 0,32 s y
  queda dorada.
- **Llaves:**
  - Tocar algo ya cambiado lo deshace.
  - Con todas las llaves usadas, tocar otra cosa **mueve** la llave: se deshace el cambio más antiguo y se hace el nuevo.
    No hay mensaje de error.
- **Contador de cambios**, abajo a la izquierda: «1 cambio» o «2 cambios», con llaves inglesas que se llenan de dorado al
  usarlas.
- **Arrancar:** botón grande con un triángulo de «play», abajo a la derecha. Está siempre activo, también sin cambios,
  porque a veces la máquina ya está bien.
- **Pista de las primeras máquinas:** los primeros 2,6 s, lo que se puede tocar late suave. En la etapa 1 solo late el
  motor.
- **Consigna**, arriba en dos líneas:
  - primera: «Que cada pieza cumpla su cartel»;
  - segunda, según la etapa:
    - «Toca el motor para cambiar su giro»;
    - «Un cambio: el motor o una correa»;
    - «Un cambio, o ninguno si ya está bien»;
    - «Hasta dos cambios».

## 6. Presentación

- **Base:** como la versión anterior (sala de máquinas, cohete, cabecera con «Cohete n.º N» y 10 luces). El cohete está un
  poco más abajo (punta en y 104), para que el radar no pise la consigna.
- **Correas:** una **polea** oscura en el eje de cada engranaje de los extremos, y la correa que las abraza, dibujada
  **encima** de los engranajes.
  - Recta: un óvalo. Cruzada: un ocho.
  - Así no hacen falta los rótulos «recta» y «cruzada», y hay menos texto en pantalla.
- **Carteles en el cohete**, solo en las piezas activas:
  - Una píldora oscura con borde dorado.
  - Línea 1: el nombre de la pieza.
  - Línea 2: el ícono (flecha de giro, o triángulo ▲ o ▼) y la palabra.
  - Las piezas inactivas se ven atenuadas y sin cartel.
- **Barras dentadas** visibles a la derecha del engranaje de la Carga y de la Compuerta. Se mueven con la pieza.

## 7. Al arrancar

**La cascada:**

- Un paso cada 150 ms (más lento en mayores), con un pulso de luz por cada unión, un clic y una nota pentatónica por paso,
  y el sonido propio de cada pieza.
- Se mantiene la decisión de la versión anterior: todos los engranajes arrancan juntos para que los dientes no se pisen,
  y la cascada se ve en el pulso.

**Veredicto en cada cartel**, al llegar la fuerza:

- Verde menta con ✓ si la pieza cumplió.
- Coral con un temblor corto si no cumplió, más un golpe sordo.

**Acierto:**

- Acorde, chispas en las piezas y una luz que vuela a la cabecera.
- El aviso dice «¡Cohete listo!», «¡Arreglado!» o «¡Todo en orden!». Si ya estaba bien y no se cambió nada: «¡Bien visto!
  Ya estaba lista».

**Error (lo que enseña):**

- En **celeste**, el brillo de los engranajes que movió tu cambio: todo lo que sigue después de él.
- En **dorado latiendo**, lo que había que tocar: la solución mínima.
- **Aviso de dos líneas:**
  1. Lo que falló, una de estas frases:
     - «La turbina giró al revés de su cartel»;
     - «La carga bajó en vez de subir»;
     - «La compuerta se cerró en vez de abrirse»;
     - «Fallaron la antena y la turbina».
  2. La pista, una de estas frases:
     - si tu cambio rompió una pieza que estaba bien: «Tu cambio también movió la antena»;
     - si no cambiaste nada: «Brilla en dorado lo que había que cambiar»;
     - en otro caso: «Truco: cada engranaje que toca gira al revés».
- La siguiente máquina llega a los 4,8 s del veredicto si hubo error, y a los 1,9 s si fue acierto.

## 8. El cohete (sin cambios)

- Junta **10 luces**, que se guardan entre partidas en las preferencias `engranajes_rocket`, que van en el respaldo.
- **Despega solo cuando está completo**, en ese momento, y la partida sigue.
- Si la partida termina antes: «Faltan N luces: tu cohete espera en el hangar».

## 9. Pantalla final y medidas

- Máquinas arregladas, de 10.
- Etapa más alta, de 5 grupos.
- Luces del cohete.
- Cohetes en órbita.
- Ritmo, en segundos por máquina.
- La nota común «Medida de esta partida… No es un diagnóstico».
- Medida estrella en `StarMeasures`: «Tus máquinas arregladas en Engranajes» (el % de máquinas arregladas).
- Consejo en `ResultAdvice`: «Antes de cambiar algo, mira qué piezas quedan después: lo que tocas antes de una rama mueve
  todo lo que sigue».

## 10. Tarjetas «NUEVO» (una vez por instalación)

| Tarjeta | Texto |
|---|---|
| taller | «La máquina del cohete viene mal armada.» / «Cada pieza tiene un cartel con lo que debe hacer.» / «Toca el motor para cambiar su giro y arranca.» |
| correas | «Toca una correa para cruzarla o descruzarla.» / «Recta: mismo giro.» / «Cruzada (en X): giro contrario.» |
| ramas | «Ahora la fuerza se reparte en ramas.» / «Un cambio antes de la rama cambia todo;» / «uno dentro de la rama, solo esa pieza.» |
| barras | «Carga y compuerta van con una barra dentada.» / «Si su engranaje gira como el reloj, la barra baja:» / «la carga baja y la compuerta se cierra.» |
| bien | «A veces la máquina ya está bien.» / «Si todo cumple su cartel, no cambies nada:» / «solo arranca.» |
| dos | «Ahora tienes dos llaves.» / «Puedes hacer hasta dos cambios.» |

## 11. Tutorial (NubiCoach, ≤ 30 s, se aprende haciendo)

Sobre una máquina de la etapa 1 en la que la antena viene fallando:

1. **Cartel (Watch o Notice).** El hueco es el cartel de la antena y la zona protegida es la antena. Nubi dice: «El cartel
   dice qué debe hacer la antena».
2. **Motor (Touch).** El hueco es el motor y su flecha. Nubi dice: «Toca el motor para cambiar su giro». El toque es el
   cambio real.
3. **Arrancar (Touch).** El hueco es el botón Arrancar y la zona protegida es el cartel. Nubi dice: «Arranca y mira la
   antena». Arranca de verdad y el cartel se pone verde.

Después: «¡Listo! Ahora va en serio».

## 12. Movimiento reducido

- **Se quita:** pulso de luz, chispas, aceleración, temblor, llamas y latidos.
- **Se mantiene:** la flecha y la correa cambian de estado al instante, el resultado queda en posición fija (barra arriba o
  abajo, carteles con veredicto) y los tiempos se conservan (`Motion.Hold`).

## 13. Originalidad

- Los acertijos de giro de engranajes son clásicos y de uso libre.
- Los puzles de engranajes existen como género en tiendas de apps, pero no encontramos uno en Lumosity, Peak ni Elevate.
- Lo propio es arreglar con un número fijo de cambios y la idea de «aguas abajo» de una bifurcación, junto con el cohete,
  los carteles, el arte y los sonidos.
- Va a la lista del abogado como juego nuevo.
