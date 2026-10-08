# Correo Estelar: «La estación de correo» (propuesta A, elegida por Ricardo el 8-oct)

Renovación a fondo de **Correo Estelar** (id `correo`, área Memoria). Se conservan:
- el id, el nombre y el historial;
- la memoria prospectiva (acordarse de hacer algo en el momento justo), que nadie más mide;
- el reloj tapado, la hoja de ruta (ahora «Encargos de hoy») y el arte de sobres y planetas.

Se va el vuelo, porque se manejaba igual que Piloto Estelar.

- **Boceto:** `docs/previews/correo-estacion-boceto.html`.
  - `?etapa=N` entra directo a una etapa.
  - `?debug` deja el estado en el DOM.
  - Simulación sin navegador en `scratchpad/correo/sim.js`.
- **Ricardo (8-oct):** «vamos con la A. Revisemos bien el tema de las licencias y patentes… mi idea es generar enganche y a
  la vez estimular cognitivamente, que sea divertido». Pidió además usar a fondo las skills de UI/UX y considerar
  componentes de 21st.dev; se le ofreció el complemento «21st» para instalar.
- **Ricardo sobre la versión anterior (28-sep):** «demasiado lento, monótono, sin dificultad; a nivel gráfico, excelente».

## 1. Por qué se rehízo

1. **Un solo vuelo de 150 s**, con encargos cada 20-30 s y nada en medio. El resultado llegaba recién al final.
2. **Era el mismo pilotaje de Piloto Estelar** (`PilotContract`): dos juegos que se juegan igual.
3. **Conducir y a la vez responder a señales** es la estructura de NeuroRacer (UCSD/Akili). Se elimina el pilotaje de este
   juego (ver §8).

## 2. La idea

Trabajas en la estación de correo de la nave.

**La tarea de fondo:**
- Las cartas llegan por una cinta y tocas el buzón del planeta de su sello: 2 a 4 buzones, una carta cada 1,6 a 2,6 s.
- Cada carta bien puesta suena con la nota de su buzón y sube la **racha**; con ×10 la cinta se enciende.
- Si se juntan más de 3 cartas en la cinta, la más antigua cae a «atrasadas».

**Los encargos del día** se dan en la mañana y **no se ven durante el día**:
- **Por evento:** «Si llega una carta con **sello dorado** (o con **lazo**), guárdala en la caja fuerte».
- **Por hora:** «**Al mediodía**, enciende el faro».
  - La hora está en el **reloj tapado** (arriba a la derecha); tocarlo muestra 1,6 s la barra del día, con mañana,
    mediodía, tarde y noche y el lucero que avanza.
  - La ventana dura un 12 % del día (8-10 % en etapas altas).
- **Lo de todos los días** (etapa 3): el primer encargo con hora se fija el día 1 y se repite cada día.
  - Desde el día 2 la hoja dice «Y lo de todos los días (ya no se anota: acuérdate tú)».
  - Si la etapa cambia, el encargo de todos los días se mantiene.
- **Cancelado** (etapa 4): desde el día 2, y con probabilidad 0,6, la radio avisa «hoy NO hace falta encender el faro…».
  - Hacerlo igual es un error de **comisión**: acordarse de NO hacer lo cancelado, algo que cuesta más con la edad.

**El día y la partida:**
- Un **día dura 50 s**; una **partida, 4 días** (unos 4 minutos con las hojas y los resúmenes).
- Al final de cada día, resumen inmediato: cada encargo con su marca (✓ en círculo lleno, o raya en aro vacío), las
  cartas bien puestas, la racha mayor y cuántas veces miró el reloj y cuántas cerca de la hora.

## 3. Respaldo científico (PubMed, revisado el 8-oct)

- **Entrenar la memoria prospectiva en mayores funciona a corto plazo.** Metaanálisis de 29 ensayos: g = 0,54 inmediato,
  0,20 a largo plazo (Tse et al., 2022). https://doi.org/10.1007/s11065-022-09536-5
- **Un juego que simula días con tareas por recordar** («Virtual Week») mejoró la memoria prospectiva **real** y las
  actividades diarias de personas mayores (Rose et al., 2015). Es la base de días cortos con encargos habituales y de
  hoy, por evento y por hora. https://doi.org/10.3389/fnhum.2015.00592
- **Combinar práctica con estrategia** («cuando vea X, haré Y») es lo que más transfiere (Henry et al., 2021). Por eso la
  hoja del día dice: «Dilo en voz baja: “cuando vea…, haré…”». https://doi.org/10.1037/pag0000593
- **Revisar a tiempo** (más cerca de la hora) mejora el desempeño (Peper y Ball, 2023): es la medida del reloj.
  https://doi.org/10.1177/17470218231161015
- Revisión: Hering et al., 2014, https://doi.org/10.1007/s00426-014-0566-4

## 4. Etapas (10 niveles del DDA común)

| Etapa | Buzones | Carta cada | Encargos por evento | Encargos por hora | Todos los días | Cancelado | Ventana | NUEVO |
|---|---|---|---|---|---|---|---|---|
| 1 | 2 | 2,6 s | 2 sellos dorados | – | – | – | – | **CORREO ESTELAR**: «Toca el buzón del sello de cada carta. / Y cumple los encargos del día: / nadie te los va a recordar.» |
| 2 | 3 | 2,4 | 2 dorados | mediodía | – | – | 12 % | «Encargos con hora. / El reloj va tapado: / tócalo para mirar la hora.» |
| 3 | 3 | 2,3 | 2 dorados | mediodía | sí | – | 12 % | «Lo de todos los días…» |
| 4 | 3 | 2,1 | 2 dorados | tarde | sí | sí | 12 % | «A veces la radio cancela un encargo. / Si lo cancela, / acuérdate de NO hacerlo.» |
| 5 | 4 | 2,1 | 2 con lazo | mediodía | sí | – | 12 % | «Ahora la señal es un lazo, / no el sello. / Mira la carta entera.» |
| 6 | 4 | 2,0 | 1 dorado + 2 lazo | tarde | sí | sí | 12 % | |
| 7 | 4 | 1,9 | 2 lazo | mañana + tarde | sí | – | 10 % | |
| 8 | 4 | 1,8 | 1 dorado + 2 lazo | mañana + tarde | sí | sí | 10 % | |
| 9 | 4 | 1,7 | 3 lazo | mediodía + noche | sí | sí | 9 % | |
| 10 | 4 | 1,6 | 1 dorado + 3 lazo | mañana + tarde + noche | sí | sí | 8 % | |

- **Momentos del día** (centro de la ventana): mañana 0,26, mediodía 0,50, tarde 0,70, noche 0,88.
- **Cartas señal:** repartidas en el día, nunca en los primeros ~6 s ni dos seguidas.
- **El sello dorado es una señal «focal»** (se mira el sello para clasificar). **El lazo es «no focal»**: está en la
  carta, no en el sello, y es más fácil de pasar por alto. Es la manipulación clásica de dificultad en memoria
  prospectiva.
- **Avance (DDA común en la app):**
  - un día con todos los encargos cumplidos y ≥ 85 % de cartas bien puestas sube una etapa;
  - con menos de la mitad de los encargos, baja una;
  - en el motor común, cada encargo es un ensayo de memoria prospectiva y la clasificación es la tarea de fondo.
  - Grupo para la pantalla final: etapas 1-2 → 1, 3-4 → 2, 5-6 → 3, 7-8 → 4, 9-10 → 5.
- **Simulación** (cientos de partidas con jugadores de distinta memoria, sin navegador):
  - 0 partidas trabadas;
  - la medida sigue a la memoria del jugador: 100 % si recuerda todo, 77 % si recuerda el 80 %, 45 % con la mitad y
    20 % si recuerda el 20 %;
  - la etapa sube con la memoria.

## 5. Pantalla (360×640 dp)

| Zona | Contenido |
|---|---|
| Arriba | «Día N de 4» y «Cartas: N · atrasadas: M». El **reloj tapado** a la derecha (aro de 56 dp, «?», rótulo «reloj»). |
| Al mirar el reloj | Panel «Hora del día» con la barra, los 4 momentos (14 dp) y el lucero. |
| Radio | Franja de 3,2 s con «RADIO» y el aviso. |
| Cinta (y ≈ 250) | La carta activa en el marco punteado (112×76) y las que vienen detrás, más chicas, avanzando con resorte. Píldora «Racha ×N» desde 3. |
| Buzones (y 326-438) | 2-4 botones de arcilla, 78-150 dp de ancho y 112 de alto, con planeta, figura y nombre: Coralia (círculo), Celesta (triángulo), Lima (cuadrado) y Uva (gota). |
| Acciones (y 468-556) | **Caja fuerte** (dial con marcas y manija) y **Faro** (torre que, encendida, lanza dos haces), de 158×88 dp. Siempre visibles, haya o no encargo, para que no sirvan de recordatorio. |
| Abajo | «Toca el buzón del sello». |

**Respuestas inmediatas:**

| Situación | Lo que se ve y se oye |
|---|---|
| Carta al buzón correcto | La carta vuela en arco al buzón; el buzón brilla y suena su nota. |
| Buzón equivocado | El buzón tiembla, aparece «Otro buzón» y se corta la racha. |
| Carta señal guardada | «¡Encargo cumplido!», fanfarria y la rueda del dial gira. |
| Carta señal mal guardada | Si se va a un buzón o se cae, «Era con sello dorado: iba a la caja fuerte» y la caja brilla. |
| Carta normal a la caja | «Esa carta no va a la caja fuerte» y la carta vuelve. |
| Faro en su ventana | «¡Faro encendido a tiempo!» y los haces. |
| Faro antes de hora | «Aún no es la hora». |
| Ventana cerrada sin faro | «Se pasó la hora: …» y el faro brilla. |
| Faro estando cancelado | «¡Estaba cancelado!». |

**Accesibilidad:**
- Nada solo por color: el sello también se distingue por figura y el lazo es una forma.
- Textos de 14 dp o más.
- Toques de 56 dp o más.
- «Quitar animaciones»: sin vuelos, sin brillos latentes y sin partículas.

**Símbolos neutros:** círculo, triángulo, cuadrado y gota. La caja fuerte usa un dial con marcas; la rueda de 3 rayos se
descartó porque se parecía al símbolo de la paz.

## 6. Sonido propio

- Cada buzón tiene su nota pentatónica, con un brillo que sube con la racha.
- Fanfarria de tres notas al cumplir un encargo.
- Tono grave y corto al equivocarse, que no es un castigo.
- Tic del reloj al mirarlo.
- Dos tonos con ruido para la radio.
- Zumbido de inicio del día y campanas al cerrarlo.

## 7. Medidas al final (juego estrella)

- **Tu memoria para lo pendiente** = encargos cumplidos / encargos. Incluye los cancelados que no se hicieron.
- **Detalle:**
  - por evento (cartas señal) y por hora (faro);
  - cancelados que no hiciste;
  - **miradas al reloj cerca de la hora** (en el 30 % del día antes de la ventana o durante ella) sobre el total;
  - cartas bien puestas;
  - etapa más alta.
- **Consejo:** un truco de intención de implementación. Al pie, «Medida de esta partida. No es un diagnóstico.».
- **Telemetría:** reemplaza `mail_*`. Lo del escudo y los asteroides se va.
- **En la app:** `data/Mail.kt` se rehace (lugar contra hora pasa a evento contra hora + cancelados + reloj).

## 8. Licencias y patentes (revisión del 8-oct; NO reemplaza al abogado)

| Patente | Qué exige | Cómo queda este diseño |
|---|---|---|
| **US 9,940,844** (UCSD, licenciada a Akili; NeuroRacer; vence ~2032) | Todas sus reivindicaciones (1, 12, 23 y 24) exigen un **sensor de movimiento o de posición, o equipo de ejercicio**. La 23 y la 24 describen una tarea **continua de seguimiento visomotor** con una **discriminación de blancos** como interferencia. | Sin seguimiento continuo: la tarea de fondo son toques discretos. Sin sensores: solo pantalla táctil. **Regla:** este juego nunca vuelve a tener una nave que se conduce. Para el abogado: si una pantalla táctil cuenta como «sensor de posición» (afecta más a Piloto que a Correo). |
| **US 10,692,029** (Lumos Labs, «multiple timer management», el juego de pedidos de Lumosity; vence ~2035) | Un dispositivo de **procesamiento de pedidos** con cola, tarjetas, componentes y **contenedores**; pedidos que se **superponen**, cada uno con su **tiempo de entrega**. | **Regla:** sin pedidos con componentes ni tiempos de entrega, sin cola de pedidos y sin varios relojes simultáneos. Cada encargo es una sola acción ante una señal o a una hora. |
| Lumos Labs, «transportation routing network» (Train of Thought) | Dirigir viajeros por una red de rutas con desvíos. | **Regla:** las cartas se tocan directo a su buzón; nunca vías, desvíos ni rutas. |
| Posit Science | No encontré nada sobre clasificar ni sobre memoria prospectiva. | – |
| **Virtual Week** (Rendell y Craik; instrumento de investigación) | Es un juego de tablero con dado, casillas y tarjetas de eventos. | Se toma solo el principio científico, que no es protegible: días con encargos habituales y del día, por evento y por hora. **No** se copia su formato de tablero, dado ni tarjetas, ni el nombre. |
| Juegos de «clasificar» (por ejemplo, de Peak) | No encontré uno de Lumosity, Peak ni Elevate que combine clasificar con recordar encargos. | Va a la lista del abogado como «tarea de fondo genérica». |

Nombre: «Correo Estelar» no cambia. Palabras que no se usan: Virtual Week, NeuroRacer, EndeavorRx.

## 9. Tutorial (NubiCoach, se aprende haciendo)

1. **Touch** en el buzón correcto de la primera carta: «Toca el buzón del sello.»
2. **Notice** de la hoja del día con un encargo de sello dorado.
3. Llega una carta dorada. **Touch** en la caja fuerte: «¡Esta es la del encargo! A la caja fuerte.»
4. Encargo por hora. **Touch** en el reloj: «Toca el reloj para mirar la hora.»
5. Cuando llega la ventana, **Touch** en el faro: «¡Es la hora! Enciende el faro.»

Luego, un día corto de práctica de 30 s que no cuenta.

## 10. Lo que se va

- El vuelo, el pilotaje y la ruta (`PilotContract`).
- Los asteroides y el escudo.
- Los planetas-puerto al costado de la ruta.
- La radio cada 30 s: ahora la radio solo trae avisos.
- Los «parecidos» de color.
- Las DOS dificultades: queda UNA, la de los encargos, y la cinta acompaña con la etapa.
- La lámina `correo-escudo.png` queda como historia.

## 11. Aprobación y enganche (8-oct, versión 2 del boceto)

Ricardo jugó un día completo: «sí engancha, pero a ratos algo monótono. Fácil de entender, y para diferentes edades, ya
que los botones son grandes. Lo apruebo, pero añadiría algún efecto cuando el faro funcione, que esté más iluminado».
Se agregó:

- **El faro guía la nave del correo** (le da sentido al faro). Al encenderlo a tiempo, durante 3,4 s:
  - **Detrás de la estación** (`drawShowBack`):
    - un baño de luz tibia que nace del faro (degradado radial de 620 dp, modo «screen» para aclarar sin ensuciar);
    - un **haz** que barre el cielo de lado a lado (ángulo `-π/2 + sin(t·2,2)·1,05`, medio ancho 0,15, con halo de 0,34).
    - Va detrás para que los buzones y botones **siempre se lean**: probado, encima los tapaba mientras se seguía
      jugando.
  - **Delante** (`drawShowFront`):
    - el destello de la lámpara;
    - la **nave del correo** (arcilla clara, ventanilla, sobre dorado de emblema y propulsor que titila) entra desde la
      izquierda siguiendo la luz (0,5-1,7 s) y se detiene sobre la cinta;
    - deja caer un **saco dorado** que estalla en chispas («¡La nave del correo llegó!») y se va por la derecha.
  - Sonido: fanfarria y una **bocina grave** de tres notas (146, 220 y 293 Hz).
  - Destellos en el cielo.
  - Quitar animaciones: luz fija hacia arriba y la nave quieta sobre la cinta, sin vuelo ni chispas.
  - En el resumen: «a tiempo: la nave del correo llegó» o «se pasó la hora: la nave no llegó».
  - La nave aparece SOLO después de encender el faro. Nunca antes, porque sería una pista de la hora.
  - Tarjeta NUEVO de la hora: «Encargos con hora. / El faro guía la nave del correo: / enciéndelo a la hora justa. / El
    reloj va tapado: tócalo para mirarla.».
- **«¡Llega un saco!»** (contra la monotonía; cambia el ritmo: calma, apuro, calma):
  - 1 vez al día (2 desde la etapa 5); llegan 5 cartas seguidas, cada 0,85 s;
  - aviso arriba con el saco dibujado («5 cartas seguidas: ¡rápido!») y sonido de saco;
  - mientras dura, la cinta aguanta hasta 7 cartas, y después se vacía sin castigo (`grace`);
  - una píldora «+N» muestra las que esperan fuera de la pantalla;
  - en las etapas 1-5 el saco no cae cerca de la hora de un encargo.
- **Racha en escalones:**
  - ×10, la cinta se enciende;
  - ×20, «¡Imparable!» con un brillo que recorre la cinta;
  - ×30 o más, «¡Maestro del correo!» con fuegos de cuatro colores.
- **Simulación otra vez:** 0 partidas trabadas. Con memoria perfecta, 100 % (98-99 % en etapas altas, por el ritmo del
  jugador simulado).

## 12. Cómo quedó hecho (8-oct, tras implementarlo)

Todo lo anterior se implementó. Estas son las diferencias con el boceto y lo que el documento no fijaba, con el motivo:

- **Código.** `MailContract` (etapas, textos, telemetría, `Advance`), `MailDay` (el día: máquina de estados sin Unity; 50 pruebas en total entre `MailContractTests` y `MailViewTests`), `MailLayout`/`MailMotion`/`MailSprites`/`MailSounds`/`WedgeGraphic`, y el controlador
  repartido en `MailGameController.cs` (flujo y entrada), `.Build.cs` (construcción de la pantalla), `.Scene.cs` (animación y pantallas) y `.Guided.cs` (tutorial). Sin `Time.time` ni esperas de Unity: todo con `GameClock` y `Motion`.
- **Reto: no aplica.** Son 4 días de 50 s con su hoja y su resumen; no hay una versión sin reloj ni una contra el reloj. `RetoChoice.WITHOUT_RETO = {correo}` (la ficha de Juegos no muestra la elección) y `GameResultScreen.NO_SPEED_GAMES` lo incluye.
  *Duda abierta:* si Ricardo quiere un Reto de varios días seguidos, hay que decidir su duración.
- **El rating de antes se llevó a la mitad.** Room 13 → 14 (`UPDATE game_progress SET ddaRating = ddaRating * 0.5 WHERE gameId = 'correo'`; la cuenta está en `Mail.translateOldRating`). Motivo: el vuelo guardaba el rating de OTRA tarea y cada etapa nueva trae
  mecánicas (la hora, lo de todos los días, lo que cancela la radio, el lazo); así el mejor jugador del vuelo (0,9) empieza en la etapa 5 y no en una con mecánicas que nunca vio. El factor 0,5 es mío: no hay datos para calibrarlo. Hay prueba de migración.
- **La etapa dentro de la partida la decide la regla del boceto (`MailContract.Advance`), y el motor común recibe un ensayo por encargo** (`stepUp` 0,3, sin tiempo de reacción). Motivo: el boceto aprobado fija cuándo sube y baja la etapa; el motor común
  sigue dando el `end_rating` que guarda la app. Los encargos cuya carta señal nunca llegó a verse en el día (no se pudo ver el aviso) se excluyen del resumen y de los ensayos.
- **La racha propia.** El HUD común se crea sin la píldora de racha (`withStreak: false`) y el juego dibuja su píldora dorada «Racha ×N» sobre la cinta, que es la que escala a ×10 / ×20 / ×30.
- **Textos sin flecha.** Las fuentes de Fredoka no traen «→»; en el resumen y en los rótulos de las cartas señal se escribe «Sello dorado: caja fuerte» / «Carta con lazo: caja fuerte».
- **Tutorial:** ocho pasos en vez de cinco (se sumaron avisos para que se entienda el reloj tapado y el faro): 1) tocar el buzón del sello; 2) Notice de la hoja con un encargo de sello dorado; 3) caja fuerte; 4) reloj; 5) Notice «Aquí está la hora del día» (el reloj destapado); 6) faro; 7) Notice «El faro guía la nave del correo» (la nave aparece solo ahora); 8) «¡Listo! Ahora va en serio». El día de práctica dura 30 s, no cuenta y no trae sacos. Pasa la red de seguridad de la Tarea 42 y no toca `_dda`,
  `_run`, el récord ni la telemetría (lo vigilan las pruebas de `GuidedTutorialTests`).
- **Herramientas de prueba:** `mail_stage` fuerza la etapa de arranque; botones «Correo con tutorial» y «Correo etapa 2 / 4 / 5 / 8» en las herramientas de depuración.
- **Smoke:** corre `Correo` y `TutorialCorreo` (en 3 formas de pantalla). En el smoke el juego arranca en la etapa 4 con días de 24 s y 85 s en total: con días de 12 s no daba tiempo de ver ningún encargo.
- **Datos que llegan a la app:** telemetría nueva `mail_*` (`mail_ev_hits/total`, `mail_time_hits/total`, `mail_cancels`, `mail_commissions`, `mail_early`, `mail_peeks`, `mail_peeks_good`, `mail_right`, `mail_sorted`, `mail_best_combo`,
  `mail_days_perfect`, `mail_group`, `mail_best`, `mail_new`); la medida se guarda con la clave nueva `estacion` en `star_measures` (los puntos viejos `pending` quedan guardados pero no se leen) y el récord en las preferencias `correo_record`
  (que van en `backup_rules.xml` y en las dos secciones de `data_extraction_rules.xml`; lo vigila `BackupRulesTest`). Se quitaron de `GameResultScreen` el texto de asteroides y escudo.
- **Reglas que se mantienen:** ninguna cara reacciona al desempeño; nada depende solo del color (el lazo es una cinta con moño; el sello dorado, desde la Tarea 47, tiene forma y tamaño propios —ver más abajo—; los buzones tienen su forma propia además del color); la nave aparece SOLO después de encender el faro;
  toques de 56 dp o más y textos de 14 dp o más; la caja fuerte y el faro siempre visibles; sin tablero, dado ni pedidos (§8).
- **Lámina:** `docs/previews/correo-estacion.png` (script `tools/art-preview/correo.py`, arte real horneado) y `correo-sonidos.wav`; sello visible «estilo 8-oct · estación de correo».
- **Código retirado:** el vuelo (`PilotContract` ya no se usa aquí), `ShipShield`, los asteroides, los planetas-puerto, la radio cada 30 s, los parecidos de color, las dos dificultades y las láminas del vuelo (`correo-escudo.png` y
  `correo-estelar.png` quedan solo como historia).
- **Dudas abiertas para Ricardo:** (1) el factor 0,5 de la migración; (2) si quiere que Correo tenga Reto; (3) el tutorial: ¿explica bastante el reloj tapado?; (4) la frase del consejo «imagínate haciendo el encargo» (viene de la intención de
  implementación, pero no es un truco que él haya revisado).
- **Tarea 47 (8-oct, tras revisar el primer commit):**
  1. **Sello dorado por forma y tamaño, no solo por color.** Era el mismo cuadrado que el normal con relleno dorado (en grises, ~1,6:1 contra el papel; error heredado del boceto). Ahora es ~1,2 veces más grande (36 dp contra 30) y con el borde dentado de un sello
     postal (cinco mordidas por lado, el borde café todo hacia adentro para que las mordidas dejen ver el papel). Es un cuadrado con dientes, no rayos: nada que parezca sol ni estrella. Se corrió 2 dp hacia adentro para no pegarse al borde del papel; su resplandor
     ahora queda FIJO con «quitar animaciones» en vez de apagarse. Lo vigilan `MailArtTests` (tamaño, mordidas, lectura en grises y que no se salga del papel). El ícono de la hoja y la tarjeta «estación» usan el mismo sprite y se actualizan solos.
  2. **El consejo de las cartas señal nombra la señal que se escapó.** Telemetría nueva `mail_gold_missed` / `mail_lazo_missed` (desglose de las cartas señal perdidas); `Mail.tip` dice «cuando vea un sello dorado, caja fuerte» o «…un lazo…», la que más veces se escapó
     (empate: el sello dorado); sin desglose (una versión vieja de Unity), «una carta con señal». Las etapas 1 a 4 no traen lazo, así que ya no reciben un consejo sobre algo que no vieron.
  3. **Consejo de la hora más concreto** (Ricardo puede vetarlo; son constantes `Mail.TIP_*`): «Truco: imagínate tocando el faro cuando llegue la hora.».
  4. **Avisos sobre la caja fuerte y el faro.** Antes subían 18 dp y su borde de abajo quedaba a 1 dp del botón: pisaban el borde de los buzones. Ahora (`AddFloatAbove`) miden 28 dp, su borde de abajo queda 2 dp sobre el botón y no suben, así que caben en el hueco de 30 dp
     entre los buzones y los botones (`MailLayout.AboveH` / `AboveGap`, con prueba). Un aviso de dos líneas sí se extiende hacia arriba sobre los buzones, nunca sobre un botón.
  5. **Lámina:** `glow()` de `correo.py` calcula el alfa por distancia y cae a 0 en el radio (el degradado de PIL llegaba solo a ~10 % en el borde y dejaba un corte duro y halos cuadrados). Lámina regenerada (`correo-estacion.png`).
- **Tarea 48 (8-oct, Ricardo lo probó en el teléfono: «no se ven las cartas» y «el botón verde sin texto»):** dos errores míos que las pruebas no veían porque la lámina de Correo se arma en Python, no con la escena de Unity.
  1. **Las cartas se dibujaban DEBAJO de la cinta.** `SetSiblingIndex` ordena solo entre hermanos: las cartas eran hijas directas de la capa de la estación y se mandaban a los índices 0 a n−1, antes de la banda opaca de la cinta. Ahora viven en un contenedor propio
     («Letters», sin raycast) creado después de la cinta, los rieles, los rodillos y el marco; el orden se fija dentro de él. Las que vuelan o caen siguen en su capa (`_flyLayer`).
  2. **Cuatro textos hijos posicionados como si fueran de la capa.** `SetRect` usa coordenadas de la capa; el texto de «Empezar el día» (hijo del botón), el de «Siguiente día», «Racha ×N» y «+N» (hijos de sus píldoras) quedaban desplazados el doble, fuera del botón o de la pantalla.
     Ahora van con `SetChild` (desplazamiento desde el centro del padre). Un grep confirma que no queda otro hijo con `SetRect`.
  3. **Guardias para que no vuelva a pasar:** (a) en el smoke, la **guardia de textos** (ver CLAUDE.md, «Pruebas»), en una corrida en forma de teléfono por juego (`Pantalla…`): todo texto visible cae dentro de la pantalla y, si es hijo de un `Image`, su centro cae dentro del padre; el mensaje dice el
     juego, el objeto y la posición. Se informa un texto solo si queda fuera de lugar en dos revisiones seguidas (lo que está animándose no cuenta). (b) En `MailViewTests`: con cartas en la cinta, cada una se dibuja después de la banda, los rieles, los rodillos y el marco, y la de adelante
     encima de las de atrás (`MailGameController.AuditLayering`, con `MailHierarchy.DrawnAfter`); todo texto hijo de un botón o una píldora tiene su centro dentro del padre (`AuditChildPlacement`); y una prueba de control que reproduce el error viejo. Se comprobó que las tres guardias FALLAN con el error
     original (el texto de «Empezar el día» quedaba a 744 unidades del botón).
