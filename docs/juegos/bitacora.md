# Bitácora de Misión (`bitacora`) — ficha técnica

> **RETIRADA de la app el 5-oct-2026** (pedido de Ricardo; razones y qué se conserva en [descartados.md](descartados.md)). Esta ficha es historia: el código de `Games/Bitacora/` sigue en Unity, pero la app ya no lo lanza ni lee `MissionLog` (se borró).

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**Bitácora de Misión** (`Games/Bitacora/`, id `bitacora`, dominio MEMORIA; primer juego estrella de memoria, 28-sep,
pedido de Ricardo: "muy innovadora, con enganche, llamativa, sonidos modernos, destellos"): memoria episódica (qué,
dónde y en qué orden) con recuerdo DIFERIDO, como las pruebas de aprendizaje y recuerdo diferido (tipo RAVLT). Ninguna
app de la competencia mide memoria con demora a lo largo de la sesión.
- Partida: **transmisión** (una sonda recorre planetas del mapa, los planetas-puerto (`Shared/PortSprites`, antes de Tráfico Estelar) con color + símbolo + nombre:
  Coral, Sol, Cielo...; en cada parada aparece un hallazgo con destello, su nota y "Planeta Sol · una llave"; la
  persona lo TOCA para guardarlo en la bitácora, fila de abajo; si no, se guarda solo a los 5 s) → **primer repaso**
  (planeta por planeta, en otro orden, elegir el hallazgo en un cajón con los de la misión + señuelos; respuesta al
  instante: se aprende) → **espera** → **informe** (lo mismo sin ayuda) → **la ruta** (tocar los planetas en el orden de
  la sonda) → **revelación** (la sonda repite la ruta; ✓ lo recordado vuela a la bitácora, ✗ muestra lo que era).
  Consejo del juego: imaginar el hallazgo EN su planeta como una escena (Bower, 1970).
- Fases por configuración (`SequenceConfigDetails.memory_phase / memory_seed / memory_level / memory_elapsed_s`):
  "" = completa (la espera es una **patrulla** de 45 s atrapando cometas, que ocupa la atención sin repasar);
  "encode" = transmisión y repaso; "recall" = informe. La misión se rearma con la semilla (`MissionRng` xorshift propio:
  igual en cualquier versión) y el nivel.
- **Misión del día** (app, `data/MissionLog` + `MissionLogStore` en SharedPreferences `mission_log`). Desde el 29-sep
  va APARTE de la sesión diaria (Ricardo: el botón decía un juego y abría otro, y jugó 5 en vez de 3): ya no se
  intercala sola. Se abre tocando la línea de la Bitácora en Hoy (`MissionLine`: transmisión / espera con minutos /
  informe listo / al día con la colección); el informe se abre al terminar la sesión o pasados 10 min, y uno pendiente
  de otro día mide memoria a un día. Bitácora no entra al camino de 3 (`BOOKEND_GAMES`). La transmisión sola NO se guarda en Room (no es partida completa): muestra su pantalla y sigue.
- Reglas y pruebas: `BitacoraContract` / `BitacoraContractTests` (5). 10 niveles: paradas 3 → 8, planetas extra que la
  sonda no visita (0, 1 desde el 3, 2 desde el 6), señuelos 2 → 5. DDA `stepUp` 0.5 por parada del informe.
- Medidas: **tu memoria a los X minutos** (paradas recordadas en su planeta), **retención** (de lo aprendido en el
  repaso, cuánto seguía en el informe: la app la calcula con la máscara guardada, `MissionLog.retentionPct`), **la
  ruta** (paradas en su lugar), **hallazgos que no estaban** (intrusiones, dicho sin culpa) y **tu bitácora**
  (colección acumulada). Telemetría `mem_*` → `GamePlayResult.mem*` → `GameResultScreen` (`FilledSlots`).
- Arte: `BitacoraSprites` (16 hallazgos de silueta distinta y fácil de nombrar: llave, campana, pluma, concha, reloj de
  arena, brújula, farol, corona, bellota, libro, copa, gema, hongo, ancla, estrella de mar, paraguas; y la sonda),
  `GameWorld.Logbook`. Sonido `BitacoraSounds`: cristal y "destellos" (parciales muy agudos) con eco suave, en la
  pentatónica de la app; cada parada con su nota (la misión suena como melodía). Vista previa:
  `python3 tools/art-preview/bitacora.py <raw>` → `docs/previews/bitacora.png`; sonidos → `docs/previews/bitacora-sonidos.wav`.
  Sin probar en el teléfono.
