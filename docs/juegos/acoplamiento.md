# Acoplamiento (`acoplamiento`) — ficha técnica

> **RENOVADO el 10-oct (Tarea 68): la ficha vigente es [`docs/diseno-acoplamiento.md`](../diseno-acoplamiento.md)** («muelle de acoplamiento»: piezas de hasta 4 × 4 bloques, módulo y hueco grandes y a la misma escala, estación de anillos que crece, luz y sombra fijas en la pantalla, revelación en seis tiempos, sonido «Madera cálida», récord y totales, tutorial con Nubi). Lo de abajo describe la versión ANTERIOR (puerto chico abajo, pieza flotando, franja «Tu estación», «combustible») y se conserva como historia; ya no vale.

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**Acoplamiento** (`Games/Acoplamiento/`, id `acoplamiento`, dominio razonamiento): rotación mental (Shepard y Metzler,
1971; Cooper y Shepard, 1973; meta-análisis de Uttal et al., 2013). Abajo el puerto de la estación con el hueco de una
pieza (poliominó quiral al azar); arriba llega el módulo girado, que es la pieza o su reflejo. Botones grandes
"ENCAJA" (lima) y "ESPEJO" (uva), con texto e ícono; la respuesta cuenta al PRESIONAR. Siempre se muestra la verdad:
el módulo gira hasta quedar derecho; si era el reflejo se da vuelta como un espejo (escala x 1 → −1); si encajaba,
baja al puerto (clunk, puerto lima) y se suma a "tu estación" (fila de arriba, crece en la partida).
- Reglas y pruebas: `DockingContract` / `DockingContractTests` (7): piezas conexas y quirales (`IsChiral` compara con
  los 4 giros del reflejo), 12 niveles: 4 → 7 bloques, giro máximo 90/135/180°, ángulos de a 45° y desde el 5 de a
  15°, "combustible" (tiempo para decidir) 6 → 2,5 s en Reto (Precisión 12 s, 24 módulos). DDA `stepUp` 0.3 SIN
  modular por tiempo (el tiempo es la medida).
- La pieza se hornea ENTERA en cada intento (`DockingSprites.PieceSprite`: SDF de bloques + "puentes" entre vecinos,
  una sola silueta; juntas tenues y remaches simétricos, que no delatan la orientación). La sombra dura va aparte
  (silueta en tinta, en un contenedor que no gira): cae siempre hacia abajo aunque la pieza gire.
- Medida propia: **tu giro mental** (`RotationSpeed`: recta de mínimos cuadrados del tiempo contra el ángulo en los
  aciertos; 1000 / pendiente = grados por segundo; -1 con menos de 8 aciertos, menos de 3 ángulos o menos de 70% de
  aciertos en total: con mucho azar la curva sale plana y parecería un giro rapidísimo) y **tu curva de
  giro** (tiempo medio a 0/45/90/135/180°). Viajan en `rotation_speed_dps / rotation_curve_ms` →
  `GamePlayResult.rotationSpeedDps / rotationCurveMs` → `GameResultScreen`: "Tu giro mental: N° por segundo" y cinco
  columnas uva con el tiempo encima y el ángulo debajo.
- Arte: `DockingSprites`, `GameWorld.DockingBay` (cielo quieto: nada gira en el fondo). Vista previa:
  `python3 tools/art-preview/acoplamiento.py <raw>` → `docs/previews/acoplamiento.png`. Probado por Ricardo (27-sep).
