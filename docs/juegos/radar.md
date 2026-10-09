# Rescate relámpago (antes Radar) (`radar`) — ficha técnica

> **RENOVADO el 9-oct (Tarea 62): la ficha vigente es [`docs/diseno-rescate.md`](../diseno-rescate.md)** («Rescate relámpago: qué cápsulas viste»: se responde QUÉ se vio y no DÓNDE, seis cápsulas con forma y color fijos, rocas grises, nave que se llena, lluvia de cápsulas, récord, tutorial con Nubi). Lo de abajo describe la versión ANTERIOR (astronautas en 16 lugares) y se conserva como historia; ya no vale.


> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**Radar** (`Games/Radar/`, id `radar`, dominio velocidad) = **Rescate relámpago** (rediseño aprobado por Ricardo el
29-sep; la versión con nave central tipo UFOV se parecía a la patente US 8,348,671 de Posit). Informe total (Sperling,
1960; TVA: Bundesen, 1990): 1) atento, el haz gira y el destello llega sin aviso (1,5-3,5 s: alerta propia, Penning et
al. 2021); 2) destello con VARIOS astronautas en 16 lugares (8 direcciones x 2 anillos, cerca/lejos del centro); desde
el nivel 5, robots (casco CUADRADO gris, `RadarSprites.Robot`) que no se rescatan; 3) interferencia 350 ms; 4) "¿Dónde
estaban los N?": tocar pone/saca balizas (`NearestSlot`: toda la zona cercana cuenta; máximo N) y "¡RESCATAR!";
5) revelación: rescatado ✓ + resplandor lima (vuela a la fila), el que se escapó con aro sol, baliza de más con ✗.
Cada 5 rondas "¡Lluvia de astronautas!" (6, 300 ms, fuera de la escalera).
- Reglas y pruebas: `RadarContract` / `RadarContractTests` (13). 12 niveles: destello 600 → 80 ms, astronautas 2/3/4/5
  (niveles 1-2/3-5/6-8/9-12), robots 0/1/2 (1-4/5-8/9-12). Ronda lograda = todos hasta 3, todos menos uno con 4+
  (`Needed`). DDA común `stepUp` 0.3, sin tiempo de reacción. Reto 120 s; Precisión 20 destellos. 60 cuadros/s y
  duración REAL del destello; una pausa en pleno destello lo anula.
- Medidas: **tu vistazo** (`GlanceMs`, como antes, + `GlanceLoad`: con cuántos a la vez), **tu captura** (`Capture`:
  promedio de las lluvias, rescatados − balizas de más; ≈ K de TVA, adultos 3-4), **tu filtro** (robots tocados de los
  mostrados, se nombra con 6+) y **tu radar** (por dirección + cerca/lejos, `ringSummary`). Telemetría `glance_ms /
  glance_load / sector_* / ring_* / capture / robots_shown / robots_touched` → `GamePlayResult` → `GameResultScreen`.
  Justificación en `docs/medidas-juegos-estrella.md`.
- Arte: `RadarSprites` (radar, haz, interferencia, robot, baliza, aro de lugar), `GameWorld.RadarStation`. Vista previa:
  `python3 tools/art-preview/radar.py <raw>` → `docs/previews/radar.png`; maqueta del diseño `docs/previews/radar-rescate.png`.
  Sin probar en el teléfono (la versión anterior: "espectacular", le encantó la información del final).
