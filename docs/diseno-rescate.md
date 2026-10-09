# Rescate relámpago: «qué cápsulas viste» (renovación, 9-oct-2026)

Boceto v3 aprobado por Ricardo el 9-oct: `docs/previews/rescate-boceto.html`
(artifact https://claude.ai/artifact/WBNUtFmtcGjvxcEg4wy63q). Mismo id `radar`, carpeta `Games/Radar/`.
Título visible en la app: **«Rescate relámpago»** (antes «Radar»); donde un texto corto no quepa, «Rescate».

## 1. Por qué se renueva

- **Patente (regla permanente 2 de CLAUDE.md, aprobada por Ricardo el 9-oct).** La versión actual pide «¿Dónde estaban los N?» en 16 lugares fijos (8 direcciones × 2 anillos). Eso es justo lo que reivindican las patentes de Nike US 8,136,943 y 8,342,685: informar POSICIONES activadas, con lugares en círculos concéntricos. La nueva versión pide QUÉ se vio, no DÓNDE. Análisis de patentes del 9-oct (informe externo, fuera del repositorio): riesgo BAJO con este diseño.
- **Accesibilidad.** Los astronautas eran chicos y se distinguían por detalles del casco. Ricardo lo encontró «muy complejo» y con «símbolos pequeños», y pensó en las personas con baja visión. Ahora **la cápsula ES el símbolo**: forma y color grandes, unos 55 dp.
- **Producto.** Ricardo pidió calidad, finalidad, base científica y enganche, o sacar el juego. Por eso se agregan la nave que se llena, el rayo tractor, la racha, la lluvia y el récord, con estética de arcilla de noche.

## 2. La idea

Hubo un accidente y hay cápsulas de escape a la deriva. El radar gira y, sin aviso, un relámpago las ilumina un instante. Luego la estática borra la imagen. En el tablero eliges **qué cápsulas viste** y tocas «¡Rescatar!». Las que acertaste suben por un rayo tractor a tu nave. Las rocas grises no se rescatan: solo distraen la mirada.

## 3. Base científica (DOIs comprobados en PubMed el 9-oct, salvo Sperling y Cowan)

- **Informe total tras una exposición breve con máscara:** Sperling 1960, doi:10.1037/h0093759.
- **Teoría de la atención visual (TVA):** velocidad de procesamiento y capacidad de la memoria visual de corto plazo. Bundesen 1990, doi:10.1037/0033-295x.97.4.523. Revisión clínica: Habekost 2015, doi:10.3389/fpsyg.2015.00290.
- **Capacidad de unos 4 objetos:** Luck y Vogel 1997, doi:10.1038/36846; Cowan 2001, doi:10.1017/S0140525X01003922.
- **Destello sin aviso (alerta propia):** entrenar la alerta aumentó la velocidad de procesamiento visual medida con TVA en mayores sanos (Penning et al. 2021, doi:10.1177/0956797620965520). Es un estudio, sin replicación amplia, así que **no se promete transferencia**.
- **Qué practica (texto permitido):** mirar rápido y retener varias cosas de un vistazo. No se habla de mejorar la vista, la conducción ni la salud.

## 4. Reglas exactas

- **Ronda:**
  1. **Atento** (1,5-3,5 s al azar): el haz del radar gira y suena un «ping» por vuelta. El relámpago llega sin aviso.
  2. **Destello** (`ms` del nivel; se mide la duración REAL, a 60 cuadros/s). Una pausa en pleno destello lo anula y se repite la ronda.
  3. **Estática** (350 ms) dentro del disco.
  4. **Respuesta.** Arriba dice «¿Qué cápsulas viste? (eran N)» y «Elige solo las que viste».
     - Tocar un botón lo marca; tocarlo otra vez lo desmarca. Se marcan como máximo N.
     - «¡Rescatar!» se activa con 1 o más marcadas.
     - Tope de espera: 25 s. Después se evalúa lo que haya, como en Satélites.
  5. **Revelación** (2,3 s): se muestra lo que pasó (sección 7).
- **Cápsulas:**
  - 6 tipos, cada uno con forma, color y nombre fijos: Hexágono #A6E36B, Gota #7FD8FF, Círculo #FF8A6B, Cuadrado #FFC94A, Triángulo #B79BFF, Rombo #FFB3D1.
  - En una ronda no se repite ningún tipo.
- **Rocas:** grises (#76718F), irregulares, sin forma de cápsula ni emblema. No están en el tablero, así que no se pueden elegir.
- **Ubicación:**
  - Posiciones **continuas al azar** dentro de un disco de radio fijo (`RR − 34`, con `RR` = 128 dp).
  - Distancia mínima de 66 dp entre centros.
  - Sin lugares fijos, sin cuadrícula, sin anillos.
  - La ubicación tiene que funcionar SIEMPRE con 6 objetos (4 cápsulas y 2 rocas): nada de «si no cabe, al centro». Una prueba con 10.000 semillas lo garantiza.
- **Éxito para el motor (`Needed` como hoy):** aciertos − elegidas que no estaban ≥ `Needed(n)`. `Needed(n)` es n hasta 3 cápsulas y n − 1 con 4.
- **Dificultad:** motor común (`AdaptiveDifficulty`, como hoy: stepUp 0,3, objetivo ~80 %). Las lluvias quedan fuera del motor.
- **Ronda perfecta** (para la celebración y la racha, no para el motor): todas las cápsulas y ninguna de más.
- **Lluvia de cápsulas:**
  - Cada 4 rondas (la 4, 8, 12…): «¡Lluvia de cápsulas!».
  - 4 cápsulas, sin rocas, destello FIJO de 300 ms, fuera de la escalera.
  - Es fija para que «Tu captura» se pueda comparar entre partidas. El boceto usaba `max(250, ms del nivel)`: **se corrige a 300 fijo**.
- **Partida:** Reto = 120 s (como hoy); Precisión = 12 rondas (9 normales y 3 lluvias).

## 5. Niveles (12)

| Nivel | Cápsulas | Destello (ms) | Rocas |
|---|---|---|---|
| 1 | 2 | 800 | 0 |
| 2 | 2 | 600 | 0 |
| 3 | 3 | 600 | 0 |
| 4 | 3 | 450 | 0 |
| 5 | 3 | 350 | 0 |
| 6 | 3 | 280 | 1 |
| 7 | 4 | 280 | 1 |
| 8 | 4 | 220 | 1 |
| 9 | 4 | 180 | 2 |
| 10 | 4 | 150 | 2 |
| 11 | 4 | 120 | 2 |
| 12 | 4 | 100 | 2 |

- **Solo cambian** la cantidad, la duración y las rocas.
- **Nunca cambian** el tamaño de la zona, el contraste ni el color de las cápsulas: sección 9.
- **Sin migración de nivel.** La escalera nueva tiene 12 niveles parecidos a los de hoy, un poco más amables, y el motor se reacomoda en 2 o 3 partidas.

## 6. Medidas

- **«Tu vistazo»** (`glance_ms` y `glance_load`, se mantienen):
  - Media geométrica de las duraciones REALES de las rondas normales desde la 4.ª, y con cuántas cápsulas a la vez.
  - Se muestra con 5 o más rondas.
  - El texto sigue igual: «unas 4 de cada 5 veces».
- **«Tu captura»** (`capture`): cuántas cápsulas nombras bien de un vistazo, de 4.
  - Se calcula en las lluvias: promedio de (aciertos − 2 × elegidas que no estaban). El total tiene mínimo 0; cada ronda no se corta en 0, para no inflar el azar.
  - **Por qué 2×.** En una lluvia hay 4 cápsulas entre 6 tipos, así que adivinar acierta 2 de cada 3 veces. Con «aciertos − de más», marcar al azar daría ~1,3 sin haber visto nada. Con el factor 2 (= 4 que estaban / 2 que no estaban), adivinar da 0 en promedio, y quien marca solo lo que vio obtiene exactamente lo que vio.
  - Se muestra con 2 o más lluvias.
  - **Ya no se compara con la referencia de adultos (3-4).** El techo es 4, así que no equivale a la K de TVA. Texto: «X de 4».
- **Se borran:**
  - **«Tu radar»** (`sector_*`, `ring_*`, `ringSummary`): medía por lugar, y esta versión no responde dónde.
  - **«Tu filtro»** (`robots_shown`, `robots_touched`): las rocas no se pueden elegir.
  - Quitarlos de la telemetría, de `GamePlayResult`, de `NativeReceiver`, de `GameResultScreen` y de `docs/medidas-juegos-estrella.md`. No están en Room: no hace falta migración.
- **Premio, no medida:** cápsulas rescatadas en la partida y su récord (`rescate_record`), igual que las luces de Satélites.

## 7. Pantalla (360 × 640 dp de referencia; layout por proporciones en pantallas altas)

- **Arriba:**
  - «Rescate relámpago»;
  - píldoras «Ronda N de 12» (en el Reto, «Ronda N» y la barra de tiempo) y «Nivel N», con el marcador común `GameHud`;
  - a la derecha, una cápsula chica y el número de rescatadas;
  - «Racha ×N» desde 2.
- **Franja de mensajes** (y ≈ 80-112): «Atento al relámpago…», la pregunta y los avisos. **Nunca sobre el radar ni el tablero.**
- **Radar** (centro en y 258, radio 128):
  - bisel de arcilla con 48 marcas;
  - pantalla azul noche;
  - dos aros punteados y una cruz, solo de adorno;
  - haz con estela que gira;
  - ecos sueltos al azar (adorno, no anuncian posiciones);
  - eje lima en el centro.
- **Nave de rescate** (bajo el radar):
  - casco claro, cabina celeste y dos propulsores coral con llama;
  - 10 ventanas: cada cápsula rescatada ocupa una con su color (se ven las últimas 10);
  - al entrar una cápsula la nave «salta» (estira y aplasta, 260 ms) y vibra 12 ms, si la app ya usa vibración en otros juegos; si no, sin vibración.
- **Tablero** (y 446): 6 botones de arcilla de 104 × 62 dp en 3 × 2.
  - Cada botón tiene la forma con su color y el nombre debajo (14 dp, negrita): **la identidad nunca es solo el color**.
  - Antes de la respuesta se ve atenuado (45 %) e inactivo.
  - Al tocarlo se hunde 3 px durante 140 ms.
  - Marcado: fondo teñido del color de la forma y un aro de 4 px.
  - El orden es FIJO por tipo y nunca refleja posiciones.
- **«¡Rescatar!»:** 172 × 44 dp, sol si hay marcadas y apagado si no.
- **Revelación:**
  - Las acertadas suben a la nave por un rayo tractor lima: 750 ms cada una, escalonadas 160 ms, con curva suave de entrada y salida.
  - Las que no elegiste derivan hacia afuera y se apagan, con aro coral PUNTEADO.
  - Las rocas quedan a media luz.
  - En el tablero:
    - acertada: fondo verde, ✓ y aro lima;
    - elegida que no estaba: fondo vino, aspa diagonal y aro coral;
    - estaba y no la elegiste: aro coral punteado.
  - Aviso: «¡Todos a salvo!» (con «Racha ×N» desde 3), o si no «Rescataste X de N · Y no estaba(n)».
- **Final** (`GameResultScreen`, como Satélites):
  - «¡RESCATE COMPLETO!» y «N cápsulas a salvo»;
  - las cápsulas rescatadas entran una tras otra en filas de 8;
  - tarjeta con «Tu captura: X de 4», «Destello más corto resuelto: N ms», «Rondas perfectas: N de M» y «Racha mayor»;
  - «¡Récord nuevo!» si corresponde;
  - nota al pie: «Captura: cuántas nombras bien en la lluvia; elegir una que no estaba descuenta el doble. Medida de esta partida. No es un diagnóstico.»

## 8. Estética: arcilla de noche (ui-ux-pro-max, aprobada en el boceto v3)

- **Piezas:**
  - borde de tinta #1A1240 de 3-4 dp;
  - sombra dura hacia abajo (5-7 dp, sin desenfoque);
  - degradado vertical (+32 % de luz arriba, −12 % abajo);
  - brillo elíptico arriba a la izquierda;
  - esquinas de 16-24 dp.
- **Fondo:** planeta lejano a media luz arriba a la derecha y 26 motas de polvo que derivan.
- **Destello:**
  - el disco se enciende y un resplandor nace del centro;
  - una onda circular sale hacia el borde;
  - 120 ms de luz suave en toda la pantalla (16 %);
  - un solo destello, nunca repetido a más de 3 por segundo.
- **Estática:** barras de color al azar (celeste, uva, blanco) y líneas de barrido, solo dentro del disco.
- **Respuesta al toque:** 80-150 ms, sin mover el resto de la pantalla.
- **Al final:** confeti de 60 trocitos con los colores de las cápsulas.
- **Fuentes:** las de la app. El boceto usa Fredoka y Nunito; en Unity, las que ya usa el juego.
- **El arte se hace en código, como `RadarSprites` hoy:** sin imágenes nuevas descargadas.

## 9. Reglas de patentes (no romper; van en el documento del juego y en una prueba)

1. **Se responde QUÉ, no DÓNDE.** Las posiciones son continuas y al azar, sin lugares fijos ni cuadrícula. El tablero está en otra zona, en orden fijo por tipo, y nunca se dispone según las posiciones del radar.
2. **La zona del destello no crece con el nivel.** El radio es fijo; solo cambian n, ms y rocas. Una prueba de contrato lo verifica para los 12 niveles.
3. **Los aros del radar son adorno.** Nunca se usan para ubicar cápsulas ni como lugares de respuesta, y ninguna medida es por lugar.
4. **Sin objetivo central con candidatos en la periferia** (Posit US 8,348,671).
5. **La dificultad no ajusta el contraste figura-fondo** (Posit US 7,773,097). El color y el brillo de cápsulas, rocas y fondo son iguales en todos los niveles.
6. **Nombres propios.** Nada de UFOV, Double Decision, Eagle Eye ni Speed Match.

Fable revisó el boceto v1 (8 tipos, robots, tablero solo en la respuesta). El v3 tiene 6 tipos, rocas y el tablero atenuado desde antes. No agrega ninguna correspondencia entre lugares y respuestas, así que el análisis sigue valiendo. Lo de equivalentes de Nike sigue en la lista del abogado.

## 10. Mensajes y zonas protegidas

- Usar la pieza común de avisos (`Toast` con `ToastPlacement`, Tarea 55).
- **Zonas prohibidas:** el disco del radar (con bisel), la nave, el tablero y «¡Rescatar!».
- Los avisos van en la franja de arriba (sección 7).
- Agregar al smoke de Rescate la guardia de que ningún aviso intersecta el radar ni el tablero.

## 11. Sonido

- Ping agudo por vuelta del haz.
- Golpe de ruido filtrado en el relámpago.
- Al marcar, una nota pentatónica que sube con cada marca. Al desmarcar, un «pop».
- Campanas al entrar cada cápsula en la nave.
- Acorde breve si la ronda es perfecta; golpe sordo si no.
- Al final, acorde.

## 12. «Quitar animaciones»

- Sin estela del haz, sin ecos, sin resplandor ni onda del destello y sin luz en toda la pantalla.
- Polvo quieto, sin llamas que titilan, sin salto de la nave y sin confeti.
- Las cápsulas no rotan al alejarse.
- **La estática se mantiene** (es la máscara de la tarea), pero como imagen quieta, sin parpadeo.
- El rayo tractor queda como un cono fijo, sin chispas.

## 13. Tutorial con Nubi (rondas de práctica que no cuentan: no alimentan el motor ni las medidas)

1. «Mira el radar: un relámpago mostrará unas cápsulas» (hueco: el radar). Práctica con 2 cápsulas, sin rocas y un destello largo de 1200 ms.
2. «Toca en el tablero las que viste» (hueco: el tablero). **Toque real** en un botón correcto.
3. «Ahora toca ¡Rescatar!» (hueco: el botón). Toque real.
4. «¡Las rescatadas suben a tu nave!» (hueco: la nave).
5. «Las rocas grises no se rescatan: no están en el tablero». Segunda práctica con 2 cápsulas, 1 roca y 800 ms.
6. «¡Listo!».

Rescate entra en `TUTORIAL_GAMES` con Piloto ya sumado. Se siguen `docs/tutoriales-con-nubi.md`, el toque real en cada paso y los controles del tutorial abajo, sin pisar el tablero; se aplica lo aprendido el 9-oct en Anagramas.

## 14. Qué se va

- Los 16 lugares, las balizas, `NearestSlot` y «¿Dónde estaban los N?».
- Los robots de casco cuadrado.
- «Tu radar» y «Tu filtro», con su telemetría.
- La lluvia de 6 astronautas cada 5 rondas: ahora son 4 cápsulas cada 4 rondas.
- Los astronautas como símbolo: ahora el símbolo es la cápsula; el astronauta va solo de adorno en la ventanita.
- La línea de CLAUDE.md «hoy NO lo cumple: se rehace en la Etapa 1»: pasa a «cumple desde el 9-oct (Tarea 62)».

## 15. Cómo quedó hecho (Tarea 62, 9-oct): lo que se decidió distinto o se agregó al construirlo

- **Archivos** (`Games/Radar/`): `RadarContract` (reglas, niveles, textos), `RadarRun` (la partida: motor aparte de las lluvias, medidas), `RadarLayout` (la pantalla por proporciones), `RadarMetrics` (motor y telemetría), `RadarSprites` y `RadarSounds` (todo el arte y el sonido, en código), y el controlador en cinco partes (`RadarGameController` con el flujo, `.Build` la interfaz, `.Scene` los movimientos, `.Guided` el tutorial, `.Shots` las capturas).
- **Avisos: franja propia, no el `Toast` común.** El `Toast` busca solo una franja libre, pero en esta pantalla no sobra ninguna: el radar, la nave, el tablero y «¡Rescatar!» llenan el alto. Los mensajes (la espera, la pregunta, el resultado de la ronda y los de la lluvia) van en la franja fija de arriba (y 78-114 dp) y nada más. Lo vigilan una guardia del smoke («ningún aviso toca el radar, la nave ni el tablero»; falla con `Debug.LogError` si pasa) y la prueba `TheScreenFitsInFourPhoneShapes_AndTheStripNeverTouchesTheRadarTheShipOrTheBoard`. Por eso el juego tiene su propia guardia y ya no usa la corrida «AvisoRadar» del smoke común (la de los avisos del `Toast`).
- **Pantalla escalada en teléfonos bajos.** El plano de `RadarLayout` es el de 360 × 640 dp; en pantallas bajas, y en el tutorial (que reserva abajo la franja de Nubi), todo se achica parejo hasta un mínimo de 0,75 para que nada se pise. En una pantalla alta la escala es 1 (el tablero queda de 3 × 2 con botones de 104 × 62 dp). Lo que sobra abajo en 20:9 sigue pendiente (hoja de ruta, «en espera»).
- **Tutorial (sección 13).** El paso 1 oculta el tablero (aparece justo después del destello): así Nubi tiene dónde ponerse y no hay nada que mirar fuera del radar. Los pasos de Tocar son de TOQUE REAL, uno por cada cápsula de la práctica (el hueco es su botón y la práctica hace la jugada, sin bucles) y otro para «¡Rescatar!». La segunda práctica (2 cápsulas y 1 roca, 800 ms) contesta sola después del aviso de las rocas.
- **«Destello más corto resuelto»** es la duración REAL (en ms, a 60 cuadros/s) del destello más breve de una ronda normal perfecta; no el valor de la tabla de niveles.
- **Reto de 120 s** también en `RetoChoice` (la app lo tenía en 90 s para Radar).
- **Sin migración de nivel** (sección 5): se mantiene el rating guardado; el motor se reacomoda solo en 2 o 3 partidas. La escalera de la app (`Skill`) ya tenía 12 niveles.
- **Pruebas:** 33 en Unity (`RadarContractTests`: 10.000 semillas con 6 objetos, posiciones continuas, radio fijo en los 12 niveles, mismos colores y contraste, tablero de orden fijo, `Needed`/éxito, captura, lluvia fija de 300 ms; `RadarRunTests`: partida, medidas, pantalla en cuatro formas de teléfono, arte, sonido y telemetría sin ningún dato de lugar) y las de Kotlin (`RescateTest`, `RescateRecordTest`, el respaldo de `rescate_record` en `BackupRulesTest`, las del tutorial en `TrailTelemetryTest`/`DailyPathTest`/`PuntaPendingTest` y la captura `RescateResultScreenshotTest`).
- **Telemetría nueva** (sin ningún dato por lugar): `glance_ms`, `glance_load`, `capture`, `resc_rescued`, `resc_perfect`, `resc_rounds`, `resc_best_streak`, `resc_shortest_ms`, `resc_best`, `resc_new`; la app manda `resc_best` (récord guardado en `rescate_record`) y, solo en depuración, `resc_stage`.
- **Final en la app** (`GameResultScreen`): las cápsulas rescatadas en filas de 8 (hasta 40, con las seis formas en orden: la cantidad se lee por el número y no por el color), «N cápsulas a salvo», rondas perfectas y racha, el destello más corto resuelto, el récord, «Tu vistazo» y «Tu captura: X de 4» con sus discos. El título «¡RESCATE COMPLETO!» y el confeti los pone el cierre del juego en Unity.
- **Piezas de arte:** `docs/previews/rescate-piezas.png` (script `tools/art-preview/radar.py`, sprites reales); pantallas reales: `docs/previews/capturas/radar.png`.
