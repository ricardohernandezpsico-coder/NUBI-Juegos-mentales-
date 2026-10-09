# Piloto Estelar: «la ruta de las balizas» (renovación, 9-oct-2026)

Boceto aprobado por Ricardo el 9-oct: `docs/previews/piloto-balizas-boceto.html`
Mismo id `piloto`, área Atención.

## 1. Por qué se renueva

- **Patente:** las continuaciones de Akili (US 12,016,700 / 11,723,598, solicitud 2025/0000455) describen una tarea con y sin interferencia. Los 15 s de piloto automático y el «costo de multitarea» son justo eso. Es la regla permanente 1 de CLAUDE.md, aprobada por Ricardo el 9-oct. Detalle del riesgo: `docs/nombre-marca-y-riesgos.md` §9.
- **Accesibilidad:** las señales parecidas se distinguían solo por color (misma forma, otro color).
- **Producto:**
  - visual pobre (dos líneas de puntos casi rectas, nave pequeña, mucho vacío);
  - sin tutorial;
  - el cambio a «¡A LOS MANDOS!» no se captaba;
  - faltaban el sonido de motor y «quitar animaciones»;
  - los avisos tapaban las señales que había que tocar. Ricardo lo pidió explícito el 9-oct: **ningún mensaje puede tapar una señal**.

## 2. La idea

Un dedo, en la franja de abajo, guía la nave por una ruta de balizas. El otro dedo atrapa solo las señales de la misión. El viaje cruza sectores con nombre, y en cada sector cambia la misión. **Las dos tareas van siempre juntas**, desde el primer segundo hasta el final.

## 3. Base científica

- **Doble tarea con dificultad adaptativa en mayores:** Anguera et al. 2013, doi:10.1038/nature12486. Sin replicación independiente: no se promete transferencia.
- **Búsqueda por conjunción de rasgos (forma + detalle):** Treisman y Gelade 1980, doi:10.1016/0010-0285(80)90005-5.
- **Cambio de regla (misión nueva por sector):** costo de cambio de tarea, Monsell 2003, doi:10.1016/S1364-6613(03)00028-7.
- **Señales en la periferia** (desde el nivel 5): campo visual útil, Ball et al. 2002, doi:10.1001/jama.288.18.2271.

## 4. Reglas exactas

- **Reto:** vuelo de 90 s en 3 sectores de 30 s.
- **Precisión:** velocidad × 0,8, exposición × 1,3; termina tras 24 señales, con un sector cada 8 señales.
- **Inicio suave (10 s):** la ruta usa el nivel de pilotaje − 2 (mínimo 1), las señales salen × 1,4 más espaciadas y duran × 1,25. Las dos tareas siguen juntas.
- **Pilotaje:**
  - La nave sigue la x del dedo de la franja con inercia: `lerp`, factor 9/s.
  - Ventanas de 1,5 s; acierto = ≥ 85 % del tiempo dentro de la ruta (margen interior de 10 dp).
  - Motor común propio, objetivo 85 %.
  - Ruta = balizas cada 64 dp. Cada baliza guarda su centro y su ancho al crearse, así un cambio de nivel no deforma lo visible. El centro es la suma de dos senos (como el `CenterAt` actual).
- **Señales:**
  - Motor común propio, objetivo 80 %.
  - 40 % son de la misión.
  - Parecidas desde el nivel 3: misma forma con OTRO detalle (30 % en los niveles 3-4, 55 % desde el 5).
  - Desde el nivel 5, 45 % aparecen en los bordes.
  - Toque a la señal más cercana dentro de 40 dp.
- **Señal no atrapada:** si era de la misión, «Se fue» (cuenta como omisión); si no era, cuenta como acierto del motor, en silencio.
- **Sectores:**
  - Al cruzar el límite, un arco dorado sobre la ruta y el aviso de MISIÓN NUEVA (Tarea 63, ver §13): «¡Nueva misión!», la forma con su detalle dibujada grande, su nombre y «Sector N · nombre».
  - La tarjeta de misión dice «NUEVA MISIÓN:», se ilumina 1,6 s y late dos veces (Tarea 63); la misión nueva es distinta de la anterior.
  - El cielo cambia de tinte (Nebulosa azul, Cinturón de hielo, Mar de polvo coral, Puerto lunar).
  - Nada se detiene.
- **Hiperimpulso:** cada 5 señales bien resueltas seguidas, con ≥ 3 ventanas limpias de pilotaje: puntos × 2 durante 6 s, velocidad × 1,15 y estrellas en líneas.
- **Puntos:** 10 por señal de la misión (× 2 en hiperimpulso). Racha de señales bien resueltas.

## 5. Niveles (9 por tarea)

| Nivel | Avance (dp/s) | Medio ancho de la ruta (dp) | Curva (fracción del ancho) | Entre señales (s) | Exposición (ms) | Parecidas | Bordes |
|---|---|---|---|---|---|---|---|
| 1 | 150 | 104 | 0,10 | 2,2 | 1600 | — | — |
| 3 | 186 | 92 | 0,15 | 1,9 | 1362 | 30 % | — |
| 5 | 222 | 79 | 0,20 | 1,65 | 1125 | 55 % | sí |
| 7 | 258 | 67 | 0,25 | 1,38 | 887 | 55 % | sí |
| 9 | 294 | 54 | 0,30 | 1,1 | 650 | 55 % | sí |

Interpolación lineal entre filas (fórmulas en `src.html` del boceto: `P.speed`, `P.half`, `P.amp`, `P.gap`, `P.expo`, `P.look`, `P.periph`).

## 6. Señales: forma + detalle, no color

- **Formas:** hexágono, gota, círculo, cuadrado y triángulo. Sin estrellas, cruces ni medias lunas.
- **Detalles:** con punto, con anillo y con franja.
- **Misión:** «hexágono con punto», por ejemplo.
- Cada forma tiene SIEMPRE el mismo color, así que las parecidas (misma forma, otro detalle) tienen el mismo color: se distinguen por el detalle, no por el color.
- **Tamaño:** señal ≥ 34 dp de diámetro, con un anillo que se vacía (tiempo restante).

## 7. Mensajes y zonas protegidas (pedido explícito de Ricardo, 9-oct)

- **Ningún aviso, globo o cartel puede tapar una señal visible, ni la zona donde nacen las señales mientras el aviso está a la vista.**
- **Reglas:**
  - Los avisos van en una franja fija entre la tarjeta de misión y el cielo de señales, o sobre la franja del dedo.
  - Mientras un aviso está a la vista, las señales nuevas nacen fuera de su rectángulo.
  - Los textos flotantes («+10», «No era de tu misión», «Se fue») van junto a la señal, sin tapar otra.
- Usar la pieza común de avisos (Toast con zonas prohibidas, Tarea 55). **Quitar la exención que tenía Piloto.**
- Agregar al smoke de Piloto la guardia: ningún aviso intersecta una señal activa.

## 8. Pantalla, sonido y accesibilidad

- **Arriba:** «Piloto Estelar», «Sector N de 3 · nombre», puntos o racha, una barra del viaje con 3 tramos y la tarjeta de misión (forma dibujada + nombre).
- **Centro:** cielo con estrellas en movimiento (se estiran en hiperimpulso), la ruta con su canal tenue y las balizas que se encienden al pasar, y la nave de 44 dp con llama.
  - Si la nave pasa por fuera, la baliza parpadea en coral, la nave tiembla y suena un zumbido: no solo color.
- **Abajo:** la franja del dedo («‹ Desliza aquí ›»), un círculo donde está el dedo y una línea punteada hasta la nave. En el inicio suave dice «Con el otro dedo, toca solo tu misión».
- **Final:** «Llegaste al puerto», puntos, mapa de los 3 sectores y:
  - «En la ruta: N %»;
  - «Señales de tu misión: X de Y»;
  - «Toques equivocados: N»;
  - «Tu nivel de señales: N de 9»;
  - «Racha mayor»;
  - la nota «Todo medido mientras hacías las dos cosas a la vez. Medida de esta partida. No es un diagnóstico.»
- **Sonido:**
  - motor sintetizado (diente de sierra + triángulo con filtro, sube con la velocidad y abre en hiperimpulso);
  - nota al pasar cada baliza dentro;
  - zumbido fuera;
  - campana al atrapar;
  - golpe sordo al equivocarse;
  - un soplido al cruzar el arco de un sector y, al cambiar la misión, dos notas que suben con timbre de triángulo (no de campana: no se confunde con la de atrapar; Tarea 63).
- **«Quitar animaciones»:** sin temblor, sin estrellas en línea, sin parpadeo; balizas con brillo fijo.
- **Toques:** franja del dedo de ≥ 100 dp de alto; toque de señal de 80 dp de diámetro.

## 9. Medida propia

- **«Tus señales a los mandos»** = (aciertos − falsas alarmas) / señales de la misión, más el nivel asentado del motor de señales.
- Todo se mide con las dos tareas juntas. **No existe medida de una tarea sola. Se borra `multitask_cost`** de la telemetría, la app, `StarMeasures` y `docs/medidas-juegos-estrella.md`.
- Mínimos: ≥ 8 señales de la misión para mostrar la proporción.

## 10. Tutorial con Nubi (las dos tareas juntas: regla 1)

Práctica que no cuenta (no alimenta motores ni medidas): ruta ancha, nave lenta, misión «hexágono con punto».
1. Aviso «Esta es tu misión» (hueco: la tarjeta).
2. «Desliza aquí para guiar la nave», con señales ya apareciendo despacio; las dos tareas a la vez desde este paso.
3. Tocar una señal de la misión (hueco: la señal).
4. Aviso «Las parecidas tienen otro detalle: no las toques».
5. Un ARCO de verdad sale arriba y se acerca mientras Nubi dice «Al cruzar un arco dorado, cambia la misión» (la nave y la ruta no se detienen); al cruzarlo la misión cambia.
6. Nubi apunta a la tarjeta (hueco: la tarjeta, que late y brilla): «Cruzaste el arco: ¡tu misión cambió!».
7. Aparece una señal de la misión NUEVA («círculo con anillo») y se toca con toque real: «Ahora toca una señal de la misión nueva».
8. «¡Listo!». (Hasta la Tarea 62 el paso 5 era solo un aviso; Tarea 63.)

Nunca una pantalla de señales sola con conteo, ni una ruta sin señales.

## 11. Qué se va

- Los 15 s de piloto automático y «¡A LOS MANDOS!».
- El costo de multitarea.
- Las parecidas por color.
- La ruta de dos líneas de puntos.
- La exención de Piloto en la pieza de avisos.

## 12. Cómo quedó (Tarea 61, 9-oct) — desvíos del diseño y su motivo

- **Avisos propios, no el `Toast` común.** El `Toast` pide una franja libre de al menos 72 dp y en Piloto no hay: el cielo de señales ocupa el medio y la tarjeta de misión el borde de arriba. El aviso de sector y el de hiperimpulso van en un rectángulo fijo (`PilotPlan.BannerBox`, entre la tarjeta de misión y el cielo) y los textos flotantes junto a su señal. Se cumple igual el pedido de Ricardo (§7): mientras un aviso está a la vista **o en espera**, las señales nuevas nacen fuera de su rectángulo (`PilotSpawn`); un aviso espera a que ninguna señal quede bajo él; un flotante solo sale si no tapa otra señal. La exención de Piloto en el smoke (`ToastCoverExceptions`) y su corrida `AvisoPiloto` se quitaron, y el juego trae su propia guardia: en el Editor, si un aviso se dibuja sobre una señal viva, falla el smoke (`GuardNotices`).
- **El chip de arriba dice «Sector N de 3»** (no «Nivel N») y el nombre del sector sale en el aviso y en el mapa del final: «Sector 2 de 3 · Cinturón de hielo» no cabe junto a los puntos y la racha del marcador común (`GameHud.SetLevelText`).
- **Antes de un cambio de sector no nacen señales durante 2,2 s** (más que la exposición más larga): así la misión nueva no encuentra señales viejas. El arco sale arriba a tiempo para que la nave lo cruce justo en el límite del sector. Nada se detiene: la ruta, la nave y las balizas siguen.
- **Sin récord de puntos**: el diseño no lo pide; los puntos y la racha mayor se muestran al final y viajan en la telemetría.
- **Rating viejo: sin migración.** Los dos motores ya existían (pilotaje y señales) con la misma escala de 1 a 9; el rating guardado se lleva a la partida nueva tal cual (`dda_rating` → los dos motores arrancan ahí) y se reajusta en las primeras señales (paso de subida 0,25, el doble de rápido que antes). No hace falta tocar Room.
- **Medida nueva con clave nueva** en `StarMeasures`: `mandos` («Tus señales a los mandos»). Los puntos viejos de `multitask` (costo de multitarea) quedan guardados sin leerse, como pasó con `taller` y `estacion`.
- **Telemetría** (`StroopSessionMetrics`): sin `multitask_cost`; `pil_lane_pct`, `pil_hits`, `pil_targets`, `pil_false`, `pil_signal_pct` (−1 con menos de 8 señales de la misión), `pil_signal_level`, `pil_drive_level`, `pil_best_streak`, `pil_points`, `pil_hyper`. Config de entrada solo de pruebas: `pil_stage` (nivel de los dos motores) y `pil_sector_s` (segundos por sector; con 5, el sector 2 llega a los 5 s y el vuelo dura 15).
- **Motor**: un solo clip de un segundo en bucle cuyo tono sube con la velocidad; en hiperimpulso el tono sube un poco más (el filtro no se abre: un clip no puede cambiar su filtro).
- **Pruebas**: `Games/Piloto/Tests` (contrato, ruta, nacimiento de señales, vuelo, pantalla en cuatro formas, motores, arte, sonido, y que no exista piloto automático ni `multitask_cost` en ninguna parte); smoke `Piloto` (Precisión), `PilotoReto`, `TutorialPiloto` (3 formas de teléfono, toque real en el hueco), `PantallaPiloto` y `HowToPiloto`.

## 13. El cambio de misión se nota (Tarea 63, 9-oct)

Ricardo probó el tutorial y una partida de 90 s y lo aprobó; lo único: «cuando pasa el arco de luz va cambiando el signo… de un comienzo no lo noté muy bien» (lo entendió jugando). El cambio se anunciaba arriba, en la tarjeta y el nombre del sector, mientras la vista está abajo, en la nave y las señales. Ahora:

- **Aviso de misión nueva** (reemplaza al aviso de «SECTOR N · nombre»): arriba «¡Nueva misión!» (18 dp; en el sector 1, donde nada cambió, «¡Tu misión!»), al medio la forma con su detalle dibujada a 52 dp con su nombre («círculo con punto», 18 dp) y abajo «Sector N · nombre» (14 dp). Dura 2,2 s, aprovechando la ventana sin señales nuevas (no nacen señales 2,2 s antes del arco). Va en el mismo lugar fijo de arriba que el aviso corto, bajo la tarjeta de misión, pero **más alto**: `PilotPlan.NoticeBox` (112 dp, de 30 a 330 dp de ancho) contra los 52 dp de `BannerBox`, que queda para el hiperimpulso. El cielo de señales NO se achica: el rectángulo solo es zona prohibida mientras el aviso está a la vista o en espera (misma guardia: ninguna señal debajo, `GuardNotices` en el Editor; los textos flotantes tampoco lo pisan).
- **Tarjeta de misión:** al cambiar late dos veces (escala 1 → 1,12 → 1, dos latidos de 0,4 s = 0,8 s, `PilotContract.MissionPulseScale`), además del brillo dorado de siempre; la tarjeta es un solo objeto centrado en sí mismo (`_missionCard`) para que lata alrededor de su centro. Con «quitar animaciones»: sin latido, queda el brillo fijo y el tono.
- **Tono propio:** `PilotSounds.MissionChange`, dos notas que suben (mi y si agudos, una por latido) con timbre de triángulo y caída rápida; el soplido del arco (`Whoosh`) ya no lleva las dos campanas que tenía.
- **Tutorial:** pasos 5 a 7 de §10: un arco real que cruza la práctica, la tarjeta señalada por Nubi y una señal de la misión nueva para tocar (toque real). Las dos tareas siguen juntas (regla permanente 1). En la práctica no sale el aviso alto: Nubi y su globo ocupan esa zona y la explicación va en el globo; el aviso se ve en el vuelo de verdad.
- **Nada se detiene:** ni la nave ni la ruta, ni en el vuelo ni en la práctica.
- **Pruebas:** contrato (`PilotRunTests`: alto y lugar del aviso en cuatro formas de teléfono, textos, latido), nacimiento de señales (`ASignalNeverBornUnderTheMissionNotice`), sonido; el smoke de Piloto (la guardia de avisos) y `TutorialPiloto` en las tres formas de pantalla con toque real en cada paso de Tocar. Capturas: `sector-nueva-mision` y `tutorial-nueva-mision` en `docs/previews/capturas/piloto.png`.
