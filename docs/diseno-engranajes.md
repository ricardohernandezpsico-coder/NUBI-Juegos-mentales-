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
- **Llaves** (cambiado el 16-oct, Tarea 114, a pedido de Ricardo: antes, con todas las llaves usadas, tocar otra cosa *movía* la llave en silencio —se deshacía el cambio más antiguo y se hacía el nuevo— y «desconfiguraba todo»):
  - Tocar algo ya cambiado lo deshace.
  - Con todas las llaves usadas, tocar el motor o una correa NO cambiados **no cambia nada** (`EngranajesContract.Touch` → `Blocked`).
  - Sobre lo tocado aparece ~1 s la **moneda coral con la aspa** (la de lo común), con una sacudida corta de 0,3 s (con «quitar animaciones» solo aparece y se apaga).
  - El contador de llaves **late una vez** (el color; el tamaño solo con movimiento), suena el **golpe suave** de «no» (el sordo del cartel que falla, a 0,4) y vibra corto si la vibración está activa.
  - El aviso de abajo (el mismo de «La turbina giró al revés…») nombra lo cambiado: «Ya usaste tu cambio» y, debajo, «Toca el motor otra vez para deshacerlo» o «Toca la correa dorada otra vez para deshacerlo»; con dos llaves, «Ya usaste tus 2 cambios» y «Toca uno de ellos para deshacerlo».
  - **No es un error**: no cuenta como fallo, no toca el DDA ni las medidas y no agrega campos de telemetría. La solución guardada nunca pide más cambios que llaves, así que la regla no deja a nadie sin salida (lo vigila `EveryStageGeneratedWithKeys_CanBeSolvedWithoutNeedingToMoveAKey`).
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

- **Base:** como la versión anterior (sala de máquinas, cohete, cabecera con «Cohete número N» y 10 luces (Tarea 161: «número» y no «n.º»; el título mide 160 dp y las luces empiezan en x = 184, cada 16)). El cohete está un
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
- Consejo: Truco para la próxima, en el final con sentido (`FinalModels`; la marca «Truco: » la lee `ResultAdvice.tipOf`): «Antes de cambiar algo, mira qué piezas quedan después: lo que tocas antes de una rama mueve
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

## 14. Estado de la implementación (5-oct) y desvíos del boceto

Hecho y verificado: contrato puro (`EngranajesContract`, puerto fiel de la lógica del boceto) con sus pruebas, interfaz, sonidos (se sumó el «clank» de cada cambio; el golpe sordo del cartel que falla es el de siempre), tutorial de 3
focos con zonas protegidas, pantalla final, medida y consejo, movimiento reducido. La lámina con el arte real está en `docs/previews/engranajes.png` (réplica, no captura).

Desvíos del boceto, a propósito:

- **Cartel más ancho y pegado a los engranajes.** El boceto lo centra en 314 con 82 de ancho y letra de 13 px; aquí la letra de la escena nunca baja de 14 dp, así que el cartel mide 96 (más si el texto lo pide, y deja lugar al ✓)
  y su borde izquierdo queda donde terminan los dientes de los engranajes de la columna del cohete (273), sin pisarlos y sin pasarse del borde derecho de la pantalla aunque la escena se achique.
- **Tarjetas «NUEVO» con letra de 16 dp.** Con ese tamaño algunas líneas del doc no caben en una sola; cada línea puede partirse en dos y la tarjeta crece con el texto (la prueba comprueba que no pase de 6 líneas).
- **Dificultad:** se usa `AdaptiveDifficulty` común (objetivo 0,80; pasos de 0,5: dos aciertos seguidos suben una etapa), no la regla simple «2 aciertos suben / 2 errores bajan» del boceto jugable.
- **En el tutorial no sale el aviso de abajo** («¡Cohete listo!»…): habla Nubi, y el aviso quedaba bajo «Saltar tutorial».
- **Tarjetas «NUEVO» con clave nueva** (`engr_intros2`): las de la versión anterior eran otras, así que quien ya las vio verá estas una vez.
- **Medida con clave nueva** (`taller`): el historial de la versión anterior no se mezcla (ver `docs/medidas-juegos-estrella.md`).
- **La flecha del motor no gira antes de «Arrancar»** (como el boceto: queda quieta y solo se da vuelta al tocarla); con «quitar animaciones» el cambio es instantáneo.

## 15. Plasticina (Tarea 112, 16-oct): los engranajes, el radar, la turbina, la polea, el portón, la carga, la barra, el casco y las flechas; los dientes mandan

Ricardo: todos los juegos en plasticina. Es SOLO arte: las reglas, el DDA, los tiempos, el giro, los pulsos de las uniones, la llama, el despegue, la transición de la correa y las medidas no cambian, y ningún umbral bajó. Este es uno de los tres juegos de «la mecánica es la silueta» (`plasticina-juegos.md` §13): lo que se lee es cómo **encajan los dientes** y **hacia dónde gira** cada pieza, así que se midió que la plasticina no cambie ninguna de las dos cosas. Técnica y pesos: [`tools/juegos/README.md`](../tools/juegos/README.md). **Fecha del cambio de arte: 16-oct-2026** (marca `estilo 16-oct · plasticina engranajes`; sin `art_version`).

| De plasticina (`tools/juegos/engranajes.py`) | Se queda procedural |
|---|---|
| los **6 engranajes** (grande de 15 dientes y chico de 10, en motor azul, camino lila y pieza dorada) con su aro y sus 5 o 3 rayos, y el **motor presionado** | la **sala**, el **cubo**, el disco oscuro de apoyo y la **sombra** (la silueta que se corre hacia abajo), las **correas** (tiras y medias vueltas: se tiñen y se estiran) |
| el **radar** (elipse celeste con el punto coral en la punta), la **turbina** (5 aspas doradas inclinadas y su cubo), la **polea** y su presionada, el **portón**, el **marco**, la **plataforma** y la **caja** de la carga, la **barra dentada** y el **casco** del cohete | la **llave inglesa**, la llama, el resplandor, las chispas, los rótulos y los textos |
| las **flechas de giro** (grande y chica, a favor y en contra del reloj; la chica del cartel) y los **triángulos** de los carteles y el «play» de «Arrancar» (grises claros: el código los tiñe) | los puntos de las luces, el número de cambios, la consigna y los avisos |
| **«Arrancar»**: una losa celeste del kit (Sky), que se ve presionada al tocarla (con su texto y su ícono abajo); las marcas ✓ y aspa de los carteles son las monedas de lo común | |

### Los dientes mandan (la medida que más importó)

El contorno de cada engranaje es el MISMO trapecio de `EngranajesSprites.GearPolygon` (punta ±0,17 y raíz ±0,32 del paso angular; la punta del grande llega a la raíz del chico: holgura cero). Dos cosas se cuidaron:

1. **El bisel no puede cambiar el contorno.** `pieza` (`plasticina.py`) bisela todo canto de más de 40°, y eso incluye las esquinas VERTICALES de los dientes (74°): la primera versión redondeaba la punta (−0,3 dp de ancho junto a la punta) y rellenaba la raíz (+0,25 dp). Ahora el bisel de la placa de los engranajes solo toma los cantos de arriba y de abajo (90°): el canto blando va hacia ADENTRO y las esquinas quedan vivas. Medido sobre el atlas (`tools/juegos/mide_engranajes.py`, alfa interpolado con precisión de sub-píxel), **contra el trapecio exacto la mayor diferencia es 0,04 dp**; contra el contorno de antes, **0,12 dp** en el ancho del diente y en el del hueco (se pedía ±0,3). Junto a la punta, a media altura y cerca de la raíz del grande: diente de 4,93 / 5,93 / 6,61 dp (el trapecio: 4,91 / 5,96 / 6,60) y hueco de 8,68 / 6,64 / 5,32 dp (8,70 / 6,61 / 5,34).
2. **Lo que se veía de antes.** El engranaje procedural llevaba un borde de tinta de 2,4 dp CENTRADO en el contorno (1,2 dp por dentro y 1,2 por fuera), y como la tinta casi no se distingue de la sala oscura, lo que se veía era el relleno claro: unos dientes 2,5 dp más finos y un hueco 2,5 dp más ancho que el real. La placa de plasticina llena hasta el contorno exacto y no lleva borde de tinta: **el hueco que se ve es el hueco real**, no uno 2,5 dp mayor. Es el único cambio de lo que se ve en los dientes, y va hacia la mecánica (la tinta de antes, en cambio, se pisaba entre vecinos: 47 dp² de silueta con tinta).

**El engrane en reposo** (el par grande-chico a 50 dp con las fases de `MeshPhase`, en la zona de contacto, rejilla de 0,25 dp): los rellenos de plasticina **se pisan 0,31 dp²** (ruido del muestreo; mal encajados, con medio paso de diferencia, se pisan 30,1 dp²) y el hueco que se ve es de **30,1 dp²** contra **101,5 dp²** de hoy (el de hoy era el hueco real más la tinta que no se veía). Los huecos entre dientes son transparentes (alfa 0,000 a 1,5 dp de la punta): detrás va el disco oscuro de apoyo, como hoy.

### Lo que gira no lleva luz ni sombra que gire

Los engranajes, el radar y la turbina giran por código, así que no pueden llevar horneada una luz o una sombra: se renderizaron **vistos exactamente desde arriba, sin suelo, y se promedian cuatro renders con la luz del estudio girada 0°, 90°, 180° y 270°** (`promedia_cuatro_eng` en `empaqueta.py`, como la hoja de la puerta de Bodega). Medido: la turbina varía **0,010** de luminosidad de aspa a aspa (se pide < 0,05) y el radar sin su punto tiene la misma luminosidad media (0,881) a la izquierda, a la derecha, arriba y abajo. La polea no gira (solo la correa), y el portón, el marco y el casco llevan la luz inclinada del kit.

**El sentido del giro se sigue leyendo.** El degradé radial del boceto (de un foco chico arriba a la izquierda, en el color claro de la paleta, hasta la punta, en el oscuro) se aplica al empaquetar como COLOR (no como luz), así que gira con el engranaje; y los **5 rayos** del grande y los **3** del chico siguen ahí. El engranaje no es simétrico: respecto de su espejo cambia **0,08 a 0,10** de luminosidad y respecto de su giro de 180°, **0,10 a 0,12**; los rayos se cuentan exactos (5 y 3) con una hondura de **0,17 a 0,18** de luminosidad (la de hoy: 0,16), en las tres paletas. Las flechas a favor y en contra del reloj son espejos (la silueta difiere **0,09 a 0,12 %**) y se distinguen por la CABEZA: sin espejar, sus siluetas difieren 29 a 46 %, y en escala de grises y a tamaño real (3 px por dp; la flechita, a 4) la cabeza cambia de lado en **114 dp²** (grande y chica) y **51 dp²** (la del cartel).

### Nada de «+»

- **La turbina tenía cuatro aspas a 90°** (una cruz «+», `simbolos-neutros.md`). El **número de aspas no es una pista del juego** (la tarea es el sentido del giro, y las aspas van inclinadas), así que ahora son **cinco, a 72°** (con y sin atlas: `EngranajesSprites.FanBlades`); medido a 12 dp del centro: aspas en 12°, 83°, 156°, 227° y 299° (pasos de 71° a 73°), ningún par enfrentado.
- **La correa cruzada («ocho») se queda**, y el cruce NO es un «+»: cada correa une dos engranajes a dos pasos de la cuadrícula (100 u) y sale del borde de las poleas (r = 14), así que las dos tiras se cruzan a 2·atan(28/100) = **31,3°**, un ocho tendido (la prueba revisa las correas de las 12 etapas).
- **El cartel que falla** solo se ponía coral. Ahora lleva la **aspa en diagonal** de lo común (la moneda coral) y el que cumple, su ✓ (la moneda verde): acierto y error nunca van solo por color. `AnimateCartels` los enciende según el veredicto; sin el atlas de lo común quedan la ✓ blanca de siempre y una aspa blanca nueva, teñidas de menta y de coral.
- **El radar sigue asimétrico**: su punto coral está en x = +18,0 dp, y = 0,0 y no hay nada coral a la izquierda del centro.

### Lo que se toca se ve presionado

Los que se tocan: el **engranaje del motor** (más oscuro, 0,706 → 0,652 de luminosidad, y más plano; el contorno de los dientes no cambia: 0,00 % de silueta distinta; **sin huella de dedo**: un engranaje gira y una huella fuera del centro giraría con él), la **correa**, que se toca por sus extremos (sus dos poleas se ven presionadas: 0,255 → 0,244) y **«Arrancar»** (losa Sky del kit con `PlasticTouch`: el texto y el ícono bajan con ella). Los carteles no se tocan (son rótulos). La imagen presionada dura al menos 80 ms (`PlasticTouch.MinSeconds`) y el estado del juego no la espera: el cambio ya está hecho. El toque guiado del tutorial también la muestra.

### Pruebas y pesos

`EngranajesPlasticTests` (13): cada pieza está en el atlas y mide lo de siempre; los dientes son el trapecio exacto y el ancho de punta y de hueco son los de hoy a ±0,3 dp (en tres radios, grande y chico); el engrane en reposo no se pisa y el hueco que se ve no es mayor que hoy; los rayos (5 y 3) y la asimetría del engranaje; sin luz que gire (turbina, radar, contorno presionado); el radar asimétrico; la turbina de 5 aspas con y sin atlas; el cruce de la correa a 31°; el cartel que falla con su aspa; las flechas espejo y su cabeza en grises; los presionados; y que todo lo que el juego dibuja esté encendido con y sin plasticina. `EngranajesArtTests` (las pruebas de siempre del dibujo procedural, que sigue de respaldo) corren ahora con `PlasticArt.Disabled`. El atlas pesa **0,86 MB** en el APK (0,82 MB con ASTC): 27 imágenes, 512 × 3768 px. Hoja de las piezas: [`previews/plasticina/engranajes-piezas.png`](previews/plasticina/engranajes-piezas.png); capturas: [antes](previews/plasticina/antes-engranajes.png) → [después](previews/plasticina/despues-engranajes.png).

## 16. Volumen (Tarea 115, 16-oct): el canto, la sombra de contacto y el color por rol

Ricardo vio la hoja de propuesta ([`previews/plasticina/propuesta-volumen.png`](previews/plasticina/propuesta-volumen.png)) y aprobó Engranajes tal cual: la plasticina de la Tarea 112 no se notaba a distancia de teléfono. Es SOLO arte: las reglas, el DDA, los tiempos, el giro, los pulsos, la transición de la correa, las medidas y el cambio bloqueado de la Tarea 114 no cambian, y **la silueta de la cara y los dientes tampoco** (`TheTeeth_AreTheExactTrapezoid…` sigue en ±0,3 dp). **Marca: `estilo 16-oct · volumen de engranajes`.**

**Qué se ve ahora**
| Pieza | Volumen |
|---|---|
| los **engranajes** (grande y chico) | **canto** de 4,5 dp (la silueta más oscura corrida HACIA ABAJO en la pantalla, en 5 pasos: no gira con el engranaje), **sombra de contacto** blanda 7 dp más abajo, cara mate con bisel y **color por rol** con un mapa de color (motor azul, camino lila, pieza dorada); el canto y la sombra de TODOS van en dos capas debajo de TODOS los engranajes (el canto de uno no tapa los dientes del vecino) |
| las **correas** (banda oscura y medias vueltas), las **poleas**, los **ejes** y las **barras** | canto de 3 a 3,5 dp (los ejes, 1,4); las poleas, con más bisel y color |
| la **flecha del motor**, las flechitas de los carteles y los triángulos | sombreado de tubo (un «churro»: bordes más oscuros) y la flecha del motor con su canto de 2,2 dp |
| los **carteles** y el rótulo **MOTOR** | placa gruesa (nine-slice con su canto, su sombra y el borde levantado; una por estado: neutra, cumplida, fallida; MOTOR, normal y dorada); las letras y los íconos en relieve; las monedas ✓ y ✗ del kit |
| el **cohete** | casco con cuerpo de cilindro (más claro a la izquierda, más oscuro a la derecha), canto de 6 dp, remaches de bolita por el borde izquierdo; las luces, cada una un domo con su aro; el portón, el marco, la plataforma y la caja con su canto |
| **«Arrancar»** y la **cuenta de cambios** | «Arrancar» con 4 dp más de canto y la letra en relieve; la cuenta de cambios es la losa «noche» del kit teñida de azul oscuro y cada llave va en una moneda con su canto |

**Cómo está hecho.** El arte del atlas pasa por `tools/juegos/engranajes_volumen.py` (se llama desde `empaqueta.py engranajes`, después del render de Blender y del promedio de cuatro luces): la luz del render recorre una rampa de color por rol, y las piezas nuevas (el domo, la moneda de las llaves, las placas) salen del mismo script. En Unity el canto es `Games/Shared/PlasticCanto` (`CantoEffect`, un efecto de malla): duplica la malla del gráfico en N pasos hacia abajo en el espacio de la PANTALLA (si el gráfico está girado, el corrimiento se gira al revés: el canto no delata el giro ni el espejo) y no cuesta un objeto más. Los engranajes llevan su canto y su sombra blanda en las capas `Soft` y `Canto` de la máquina; lo demás lleva el efecto sobre su propio dibujo.

**Decisiones**
1. **El bisel de la cara de lo que GIRA es isótropo** (sale de la distancia al borde y oscurece el borde por parejo), no el de la hoja con la luz arriba a la izquierda: una luz horneada que gira con el engranaje es una luz que gira. La dirección la dan el canto y la sombra, que NO giran.
2. El canto es un **efecto de malla** y no un hijo suelto (lo pidieron «como capa que NO gira, con un desplazamiento fijo de 4,5 dp hacia abajo»: es lo mismo, una copia de la silueta con ese corrimiento en la pantalla, sin los objetos de más).
3. Sin el atlas (o con `PlasticArt.Disabled`) no hay volumen: el juego dibuja lo de siempre (sombra dura incluida).

**Medidas** (sobre el juego integrado, nivel 12, 12 engranajes y 4 correas; `tools/juegos/propuesta_medidas.py engr`)
- **El hueco entre dientes se sigue leyendo con el canto detrás:** razón de contraste diente/hueco, mediana de 12 engranajes, sin canto → con canto: mitad de arriba 3,7 → 3,1 : 1 y mitad de abajo (donde cae el canto) 5,4 → 4,0 : 1; **el 98 % o más del hueco** sigue por encima de 1,5 : 1 en todos. Los colores de los engranajes son más oscuros y más saturados que los de la Tarea 112 (lila pálido), así que el contraste absoluto es menor que en esa época (8,2 / 9,5 : 1 en la hoja de la propuesta: era el color pálido); el canto cuesta ~0,6 : 1.
- **Costo de las capas** (Editor, CPU, nivel 12): rehacer los lienzos con todos los engranajes girando cuesta **0,74 ms por cuadro con canto contra 0,54 sin él** y con el dibujo de la cámara **2,37 contra 1,87 ms**: +0,2 a +0,5 ms por cuadro (≈ 3 % de los 16,7 ms de 60 fps).
- **Peso:** el atlas pasa de 27 a 34 imágenes (512 × 4020 px): **0,99 MB** el PNG, **1,07 MB** dentro del APK (antes 0,86 MB) y 0,88 MB con ASTC.

**Pruebas** (`EngranajesViewTests`, `EngranajesPlasticTests`, 61 en el filtro de Engranajes): el `CantoEffect` repite la malla hacia abajo, del más hondo a la cara, y sigue hacia abajo en la pantalla con el gráfico girado; `AuditVolume` (niveles 1, 7 y 12): las capas de la sombra blanda y del canto van debajo de todos los engranajes, cada engranaje tiene su canto de 4,5 dp en 5 pasos y al girarlo 37° su canto sigue hacia abajo, los carteles y MOTOR tienen su placa, cada luz su aro y domo y las bandas de las correas su canto; el atlas trae las 7 piezas nuevas; la losa de la cuenta de cambios se audita en lugar del fondo invisible. Capturas: [antes](previews/plasticina/antes-engranajes-volumen.png) → [después](previews/plasticina/despues-engranajes-volumen.png).

## 17. Botones y rótulos con el kit (Tarea 116, 17-oct): lo tocable se ve como lo de los otros juegos

Ricardo dijo que los engranajes le encantan (**no se tocan**) pero que los botones y los objetos no tenían «el brillo, la sombra, la profundidad y la textura de plasticina» de los otros juegos. Se midió antes de cambiar nada ([`previews/plasticina/diagnostico-botones.png`](previews/plasticina/diagnostico-botones.png), `tools/juegos/diagnostico_botones.py`): «Arrancar» y VERDAD / DISPARATE / «Continuar» / el tablero de Rescate son la MISMA losa del kit, y el labio (3,3 dp), el brillo (1,025×), la sombra y la saturación de la cara **no cambian con el ancho** de la losa («Reiniciar» de la pausa, 211 × 49 dp, y «Arrancar», 216 × 101 dp, dan lo mismo): **la sospecha del nine-slice estirado se descartó con números**. Lo que los separaba eran añadidos de la Tarea 115 encima de la losa: un canto de 4 dp cian oscuro que se quedaba al presionar, un halo claro bajo el texto (relieve +0,22 contra ±0,02 de los demás), un triángulo plano con sombra dura, la cuenta de llaves teñida (L 0,12 contra 0,18-0,19 de la losa «noche» de Rescate) con llaves y monedas planas, y faltaba el rebote al soltar.

**Qué cambió** (solo arte y capas; ninguna regla, medida ni tiempo; los engranajes y la mecánica no se tocaron)
| Pieza | Ahora |
|---|---|
| **«Arrancar»** | la losa Sky del kit tal cual (la misma de «Reiniciar» de la pausa), **sin** canto propio, **sin** halo bajo el texto (tinta lisa), el triángulo es una cuña de plasticina de tinta con sombra suave en el suelo (`ico_play`, como los íconos de VERDAD y DISPARATE) y **rebota al soltar** (`PlasticTouch.Bounce`, como Rescate); atenuado (45 %) cuando no se puede tocar |
| la **cuenta de llaves** | la losa «noche» del kit SIN teñir (como las casillas de Rescate), el texto liso y cada llave sobre una moneda de plasticina con la llave de churro de tinta (`llave_libre` lila, `llave_usada` dorada; el mismo formato de las marcas ✓ y ✗ de lo común) |
| los **carteles** y el rótulo **MOTOR** | losas CHICAS del kit (`losa_ch_noche`, `losa_ch_acierto`, `losa_ch_error`: los colores de las casillas de Rescate); el rótulo dorado es la `losa_ch_sol` de `comun`; letras e íconos lisos; MOTOR pasa de 62 × 24 a 66 × 30 dp (la losa chica pide ≥ 28 dp de alto) |
| el **portón**, el **marco**, la **plataforma**, la **caja** y la **barra** | la misma receta de color de los engranajes (`fija_rol`: la luz del render recorre la rampa de su rol —portón, marco y barra lila; caja azul; plataforma dorada—, con el bisel y el grano de los engranajes) en lugar del gris lavanda del render |

La marca de verificación es `estilo 17-oct · botones del kit en engranajes`. Las pruebas (`AuditVolume` → `AuditKitButtons`) fallan si algo de eso vuelve a llevar canto, halo o teñido propio encima de la losa del kit. **Pendiente (se anota, no se hizo):** las correas siguen siendo bandas planas con canto (no un tubo sombreado) y el casco conserva el cilindro de la Tarea 115.

## Cambio de diseño del 18-oct-2026 (Tarea 129): el kit en carteles, MOTOR y la cuenta de cambios

Los carteles («Antena · al revés», «Turbina · reloj») y el rótulo MOTOR pasan de la losa chica noche a la **losa chica crema** del kit (`losa_ch_crema`, grupo `comun`): el nombre en #514B78 a 16 dp y la palabra en tinta a 16 dp; cumplido y fallido siguen con sus losas oscuras (acierto/error) y las monedas ✓ / ✗. El cartel pasa de 96 × 38 a **104 × 44 dp** para la letra de 16 dp.
La cuenta de cambios es una losa crema (`md`): el número a 24 dp en tinta y «cambio/s» a 16 dp en #514B78 (las llaves en sus monedas quedan). Se borró el panel de resultado que se armaba oculto. Mismas reglas, escena y cantidades.

## 18. La mesa de taller y la paleta «Madera clara» (Tarea 164, 24-25-oct): la sala sobre una losa crema, el aviso en crema, la escena más arriba y los colores nuevos

La prueba de Ricardo con 5 personas (22-oct): «Engranajes: estéticamente se ve mal. La funcionalidad, bien.» Fable midió las 15 capturas reales y concluyó que los engranajes están bien (Ricardo: «le encantan, no se tocan», 17-oct) y lo que se ve mal es TODO lo que los rodea: una sala noche sobre un cielo noche, medio panel vacío con rayas, un cohete más claro que el puzle y un aviso oscuro ([`auditoria-fable/engranajes-visual.md`](../../auditoria-fable/engranajes-visual.md)). Esta primera parte hace las partes A, D y E de su spec y, en la misma pasada, la parte F: los colores de la **paleta 1, «Madera clara»** que eligió Ricardo ([`auditoria-fable/engranajes-colores.md`](../../auditoria-fable/engranajes-colores.md)); **no toca un diente, una silueta, la forma del canto ni de la sombra de los engranajes ni la mecánica**. Lámina «hoy | propuesta» con capturas reales: [`previews/tarea-164.png`](previews/tarea-164.png) (`tools/previews/tarea_164.py`). **Marca: `madera clara 25-oct`** (antes `mesa de taller 24-oct`).

| Parte | Qué es | Dónde |
|---|---|---|
| **A · la mesa** | una **losa crema mediana del kit** (`losa_md_crema`, `PlasticSlab.TryStyle`) de 385 × 392 dp del boceto (de −8 a 377 de ancho y de 84 a 476 de alto), DEBAJO de la sala, del cohete y de la máquina. La sala de antes (242 × 348 dp, en el mismo lugar: la máquina no se mueve ni un píxel) es ahora su **ventanilla noche hundida** (regla R4: lo calibrado sobre oscuro va en su ventanilla): `EngranajesSprites.Window()`, (36, 34, 88) arriba y un punto más clara abajo, con una sombra interior arriba y a la izquierda y un hilo claro abajo y a la derecha; sin rayas. Los cuatro puntos de las esquinas son **cuatro tornillos de plasticina** (`Dome()` con su canto, 10 dp) en el marco crema, junto a las esquinas de la ventanilla, nunca encima de ella | `Build.cs` (`BuildScene`, `AssignSprites`), `EngranajesLayout` (`TableLeft/Right`, `Window*`) |
| **D · el aviso** | la losa crema mediana del kit con la letra en **tinta** (primera línea a 20 dp negrita, la pista a 16 dp `InkMuted`) y la **moneda de lo común** a la izquierda: ✓ lima al acertar, ✗ coral al fallar (acierto y error nunca solo por color). Las frases no cambian. La losa **crece** si una frase se parte en dos renglones (el error más largo mide unos 104 dp; mínimo 84 dp). Sin plasticina, o sin las monedas, la losa noche de siempre | `BuildSay`, `LayoutSay`, `AnimateSay` |
| **E · la composición y el aire repartido** | la mesa empieza bajo «Cohete número N» (a 108 dp del borde de arriba, `EngranajesLayout.TableTopDp`, más lo que baja si sobra aire); la escena sube unos 35 dp respecto de antes (en un teléfono de 800 dp empezaba hacia 174, ahora hacia 139) y el aviso queda 10 dp bajo los botones (`SayGap`). **El aire que sobra se reparte** (antes, con la escena arriba, quedaban unos 190 dp de cielo vacío bajo los botones): los botones crecen hasta 26 dp (de 88 a 114 dp en un teléfono de 800 dp: más a mano), la mesa baja hasta 36 dp de la cabecera, los botones se separan hasta 64 dp de la mesa y el resto queda bajo el aviso (`MaxButtonGrow`, `MaxTopShift`, `MaxGap`); a 800 dp: mesa 31 dp más abajo, 44 dp entre mesa y botones, 56 dp libres bajo el aviso y 150 dp de cielo con el aviso apagado | `EngranajesLayout.Compute` |
| **F · los colores (paleta «Madera clara»)** | el **camino** en **arena** (madera clara), el **motor** en **cielo** (el celeste del kit) y la **pieza** que mueve la antena o el cohete en **uva** (el morado de Razonamiento): tres familias de color a más de 100° de tono entre sí (antes lila, azul y oro; lila = botón, área y casco a la vez). Solo cambia el COLOR: son tres rampas nuevas (`RAMPAS['arena'/'cielo'/'uva']`, `ROL_PALETA = {0: cielo, 1: arena, 2: uva}` en `tools/juegos/engranajes_volumen.py`) y `python -I tools/juegos/empaqueta.py engranajes` rehace el atlas sin volver a renderizar en Blender (la luz, los rayos, el bisel, el grano y el presionado salen solos). Las piezas fijas del cohete siguen la receta por rol (`fija_rol`): **portón y barra dentada en arena, caja en cielo, marco y plataforma en uva**; sus cantos y los de los engranajes cambian de color con ellas (`GearCantoLight/Deep`, `CantoDeep/LightWood`; la madera mide de 1,2 a 2,2 : 1 contra la ventanilla). Los colores de respaldo del dibujo procedural (`EngranajesSprites.Pal`) son los mismos. La flecha del motor sigue celeste. El casco conserva el tinte lila (`HullOnTable`); la turbina (oro) y el radar no se tocaron | `engranajes_volumen.py`, `empaqueta.py` (`grupo_engranajes`), `EngranajesSprites.Pal`, `Scene.cs` (`GearCanto*`), `Build.cs` |

**Decisiones y desvíos de la spec**
1. **El contraste canto/ventanilla ≥ 1,5 : 1 de la spec no se alcanza, ni se puede** con el tono de su boceto: sobre (36, 34, 88) ni una sombra negra daría más de 1,47 : 1, y el canto lila de los engranajes (de 46, 31, 84 a 86, 62, 131) es MÁS CLARO que el fondo, de modo que aclarar la ventanilla lo confunde (probado a (47, 45, 113): quedó peor). Medido sobre capturas reales (`tools/juegos/mide_engranajes_mesa.py`): diente contra hueco 4,5 → 5,0 : 1 (se pedía no bajar de 1,5); canto lila contra el fondo de 1,1 a 1,7 : 1 → de 1,1 a 1,6 : 1; lo más oscuro (canto hondo y sombra) 1,06 → 1,06 : 1; ventanilla contra el cielo 1,05 → 1,23 : 1; la mesa crema contra el cielo, 15 : 1. El volumen se lee por el color y la forma (recortes al 100 % de la lámina), no por la luminancia sola. Ricardo y Opus aceptaron esto (24-oct) y pidieron **no tocar el canto ni la sombra de la Tarea 115**.
2. **El casco** (que sigue siendo el blanco lavanda hasta la parte C) se perdía sobre el crema: lleva un tinte lila de una línea (`HullOnTable`, (0,74, 0,68, 0,95)) que el cohete lila de la parte C reemplazará.
3. **La mesa mide 385 dp** para que los carteles, que llegan a 369, queden adentro; a la escala máxima (0,88) deja 10 dp a cada lado de la pantalla. El corrimiento de 4,5 dp de la escena es fijo: a escalas menores la mesa queda hasta 4,5 × (1 − escala) dp descentrada (como la escena de antes).
4. **El aire de abajo:** repartido (parte E); con el aviso apagado quedan unos 150 dp de cielo bajo los botones (donde sale el aviso 3 s).
5. **La paleta, medida** (atlas y capturas reales; `tools/juegos/mide_engranajes.py`, `mide_engranajes_mesa.py`): cara contra la ventanilla (36, 34, 88) motor celeste 11,5 : 1, camino de madera 11,0 : 1, pieza uva 9,4 : 1 (antes 8,0, 6,9 y 10,1); diente contra hueco en el juego, madera 5,6 : 1 y motor 5,2 : 1 (antes 6,5 y 4,5; se pide ≥ 3 : 1); canto contra el fondo, madera de 1,2 a 2,2 : 1 (antes lila de 1,1 a 1,6) y motor de 1,2 a 4,3 : 1; **lo más oscuro (canto hondo y sombra) sigue en ~1,2 : 1**: el ≥ 1,5 : 1 de la spec de la paleta se cumple en lo más claro del canto, no en lo hondo (eso es la Tarea 115, que no se toca). Las pruebas que miden la luz (rayos ≥ 0,035: salen 0,27 a 0,32; asimetría; presionado más oscuro) pasaron sin tocar un margen. `tools/juegos/propuesta_medidas.py engr` ya no se puede correr: pedía capturas `-fondo` y `-sincanto` de un gancho temporal de la Tarea 115.
6. **Las piezas fijas** (portón, barra, caja, marco, plataforma) siguen la receta por rol; el marco va en uva porque la spec deja «el morado en la pieza, el casco y el marco» (si se prefiere madera, es un cambio de una palabra en `empaqueta.py`).

**Pruebas** (`EngranajesViewTests`): `EngranajesLayoutTests` (la mesa empieza bajo la cabecera, entre 108 y 144 dp en cada altura; el aviso queda 10 dp bajo los botones; a 800 dp la escena sigue 30 dp o más arriba que antes, bajo los botones no quedan más de 160 dp vacíos, los botones miden 110 dp o más y bajo el aviso quedan 40 dp o más; la mesa queda dentro de la pantalla y centrada, la ventanilla deja 12 dp de marco y los carteles caben); `EngranajesPlasticTests`: `ThePalette_IsMaderaClara_…` (motor celeste 185-205°, camino de madera 25-45°, pieza uva 250-270°, tres familias a más de 60° de tono entre sí y cada cara a más de 3 : 1 contra la ventanilla) y `TheFixedRocketPieces_FollowTheRoleRecipe_…` y `TheWorkshopTable_IsACreamSlabUnderEverything_…` (`AuditTable`: la mesa tiene su losa y su fondo invisible, la ventanilla mide 242 × 348 dp en el lugar del panel, es un paso más clara que el cielo y está hundida, los tornillos van en el marco y no sobre la ventanilla, el aviso es crema con la letra en tinta y la moneda ✗ o ✓); `AuditVisibility` suma la mesa, los tornillos y el orden de las capas (mesa < sala < cohete < máquina). Capturas reales: `bash tools/verificar-todo.sh --capturas engranajes` (el corredor suma las tomas `aviso-bien` y `aviso-mal`: `ScreenshotRunner.EngranajesNoticeShots`, y `primera-aviso`, el aviso de un error con los botones de abajo en su estado normal: el piloto automático se detiene en las dos primeras tomas, porque a los 1,5 s toca «Arrancar» y atenúa los botones). **Falta** (siguientes tareas): B, los agujeros del tablero en las casillas libres; C, el cohete lila (Blender).
