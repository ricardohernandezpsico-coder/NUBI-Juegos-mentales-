# Satélites (`satelites`) — ficha técnica

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**Satélites** (`Games/Satelites/`, id `satelites`, dominio atención): seguimiento de múltiples objetos (Pylyshyn y
Storm, 1988; NeuroTracker: Faubert y Sidebottom, 2012). Satélites iguales (`SymbolSprite` Satellite 0 sobre un disco
tenue); k encienden su señal 2,2 s, se apagan, todos se mueven 5-8 s y al detenerse se tocan los k (tocar de nuevo
desmarca). Al revelar: ✓/✗, los que faltaron vuelven a brillar, se dibuja la estela del recorrido de los de la señal y,
si se confundió uno por otro, un aro coral "¿Aquí se cruzaron?" en el punto donde el que faltó pasó más cerca del
elegido por error (`ClosestApproach` sobre las trayectorias muestreadas cada 0,1 s).
- Reglas y pruebas: `SatelliteContract` + `SatelliteSwarm` (movimiento puro con semilla: velocidad constante, rumbo
  que deriva, rebotes en bordes y entre ellos, nunca se tapan) / `SatelliteContractTests` (9). 12 niveles: a seguir
  2 → 5, en total 6 → 11, velocidad 0,16 → 0,52 anchos de campo/s (factor 1× → 3,3×), 5 → 8 s de movimiento. DDA con
  `stepUp` 0.5 (pocas rondas); acierto = todos los de la señal. Reto 120 s; Precisión 8 rondas. 60 cuadros por segundo.
- Medida propia: **tu seguimiento** (`TrackedEstimate`: cuántos se siguieron de verdad descontando la suerte, modelo
  aciertos = m + (k − m)² / (n − m); promedio de las rondas) y la velocidad más alta superada completa. Viajan en
  `StroopSessionMetrics.tracking_capacity / tracking_targets / tracking_speed` → `GamePlayResult.trackingCapacity /
  trackingTargets / trackingSpeed` → `GameResultScreen`: "Tu seguimiento: 2,6 de 3 a la vez" (el "de N" = cuántos había
  que seguir en promedio: es el techo de esa partida, no un límite personal) con 5 discos que se llenan + velocidad; la
  referencia "a velocidad moderada, los adultos suelen seguir entre 3 y 4; más rápido, menos (Alvarez y Franconeri,
  2007)" solo aparece si había que seguir 3,5 o más.
- Arte y vista previa: `GameWorld.MissionControl` (cielo quieto); `python3 tools/art-preview/satelites.py <raw>` →
  `docs/previews/satelites.png`. Sin probar en el teléfono: revisar tamaño de los satélites, ritmo (~12 s por ronda),
  que se entienda el "¿Aquí se cruzaron?".
