# Tráfico Estelar (`trafico`) — ficha técnica

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**Tráfico Estelar** (`Games/Trafico/`, id `trafico`, dominio razonamiento): ruteo con desvíos (mecánica genérica;
nombre, arte y medidas propios), atención dividida y planificación bajo presión de tiempo, en la línea de la tarea de
control de tráfico aéreo de Kanfer y Ackerman (1989). De la compuerta de una estación de carga (cúpula crema sobre
plataforma azul, arriba; reemplazó al "vórtice" que no le gustó a Ricardo) salen cápsulas de colores por rutas CURVAS;
tocando los desvíos (discos con flecha que apunta hacia donde sale la ruta activa; alcance de toque 100 u) cada una
debe llegar al planeta-puerto de su color Y su símbolo (8 pares color + forma: corazón, estrella, rombo, triángulo,
luna, cruz, cuadrado, aro). Rutas activas: riel celeste con resplandor y luces que corren en el sentido del viaje;
las otras, riel de arcilla apagado (visible para planificar). Cápsulas con estela de su color.
- Reglas y pruebas: `TrafficContract` + `TrafficNetwork` + `TrafficSim` (simulación pura: las cápsulas avanzan por el
  RECORRIDO de cada tramo, `PathX/PathY`, a velocidad constante; toman la salida activa AL LLEGAR al desvío; mover un
  desvío después no cambia a la que ya pasó) / `TrafficContractTests` (11). La red es un árbol: puertos en una "U" con
  algo de desorden, el grupo se parte al azar en dos, cada desvío entre el portal y el centro de sus puertos; `Relax`
  separa desvíos pegados; se acepta si no hay cruces, nodos a < 0,14 anchos, rutas rozando nodos (0,08) ni rutas
  pegadas (0,055). Después `CurveRoutes`: cada tramo sale del desvío girado ±38° respecto de la ruta que llega (como un
  cambio de vía) y llega en Bézier; según `Twist(nivel)` la ruta de la estación es una "S" (niveles 1-3) o una cornisa
  que va y vuelve con medias vueltas redondas (`Switchback`: 2 tramos desde el 4, 3 desde el 8), y algunos tramos
  largos ondulan. Cada curva se acepta solo si no cruza ni roza nada y su radio de giro es ≥ 0,035; si no, una más
  suave, y en el peor caso la recta.
- Ritmo "lento y lleno" (Ricardo, tras probarlo: iban "casi una a la vez" y muy rápido; el juego de trenes de
  Lumosity le parecía mejor): lo que sube con el nivel es CUÁNTAS van a la vez (`TargetInFlight` 2,5 → 7,5), no el
  apuro. Velocidad 0,09 → 0,18 alturas/s (viaje ~8-9 s); cada cuánto sale una = viaje medio de ESTA red
  (`MeanRouteLength` / velocidad) / `TargetInFlight`, mínimo 0,9 s (Precisión: 20% más lento y 30% más espaciado, 30
  cápsulas). Flujo continuo: la pantalla no se vacía; 10 entregas seguidas sin error = "¡Serie perfecta!" +200. Si
  cambia la cantidad de puertos (y la red lleva ≥ 20 s), deja de sacar, se entregan las que van y la red se rearma.
  12 niveles: puertos 2 → 8. DDA `stepUp` 0.2 por cápsula entregada, sin tiempo de reacción. Reto 120 s.
- "Próximas" (propio, no está en el juego de trenes): las 3 que vienen esperan a la derecha de la compuerta (rótulo
  suelto "próximas", la primera más grande); se puede preparar la ruta antes de que salgan (`Spawn(..., announcedAt)`:
  mover un desvío mientras espera cuenta para "tu anticipación"). **Cápsula urgente** desde el nivel 5 (8% → 18%,
  nunca dos seguidas): aro sol que late, un poco más grande, ×1,35 de velocidad, vale el doble, salida con dos
  campanas; la primera vez, aviso "¡Cápsula urgente!".
- Sonido (`TrafficSounds`, sintetizado) con la identidad de la app, NO arcade (Ricardo probó una versión arcade y la
  rechazó: "debe ser mejor y asociado a lo que queremos en la app"): marimba y campana en la pentatónica de do de
  `GameFeel`, así todo suena afinado entre sí. Vuelo en bucle (fuente aparte `_engine`, colchón do + sol con soplo
  suave que respira) solo mientras hay cápsulas en viaje: más cápsulas = algo más de volumen (el tono NO cambia), y se
  corre a izquierda/derecha según dónde van. Salida = soplo + marimba grave; desvío = golpecito de madera (mi/sol según
  el lado); al pasar por un desvío, campanita con la NOTA DEL COLOR de la cápsula (color + símbolo + nota: el tráfico
  arma una melodía); entrega = "pling" de racha de `GameFeel` + marimba grave del color; error y subir de nivel =
  los de `GameFeel`; serie perfecta = lluvia de campanas; urgente = salida con dos campanas agudas. Respeta sonido apagado y pausa. Muestra escuchable:
  ArtPreview vuelca `trafico-sonidos.wav` (copia en `docs/previews/`; los sonidos de `GameFeel` van replicados ahí).
- Dibujo: `RailLine` (malla propia de la UI que sigue los puntos; sección de textura con bordes suaves: arcilla con
  borde tinta, cinta pareja o resplandor). Por tramo: resplandor, sombra dura, riel y línea de luz, cada uno en su capa.
- Medida principal: **tu carga** (`CleanPeakLoad`: la mayor cantidad en viaje a la vez en un momento LIMPIO, sin
  ninguna mal entregada en viaje; cada error ensucia desde que esa cápsula se vio hasta que llegó; muestras cada 0,5 s).
  Viaja en `traffic_peak_pods` → `GameResultScreen`: "Tu carga: N cápsulas a la vez" + 8 cápsulas que se encienden.
  Además **tu anticipación** (mediana de cuánto antes de que pase la cápsula se movió el desvío que la mandó
  bien; solo desvíos movidos para ella) y **planificas / a último momento** (% con ≥ 1 s: control proactivo vs reactivo,
  Braver 2012). Viajan en `traffic_lead_ms / traffic_proactive_pct / traffic_peak_pods` →
  `GamePlayResult.trafficLeadMs / trafficProactivePct / trafficPeakPods` → `GameResultScreen`: barra partida lima/sol.
- Arte: `TrafficSprites` (estación, puertos, cápsulas, desvío), `GameWorld.TrafficHub`. Vista previa (redes y curvas
  reales de `BuildNetwork` que vuelca ArtPreview): `python3 tools/art-preview/trafico.py <raw>` →
  `docs/previews/trafico.png`. Ricardo vio la primera maqueta (27-sep): "muy rígida, poco llamativa", pidió curvas y
  cambios de sentido → rehecho así. Probado por Ricardo (27-sep): le gustó la estructura; pidió que la nave suene al viajar (ver Sonido) y más
  trabajo simultáneo (ver Ritmo). Versión "lento y lleno" sin probar en el teléfono.
