# Piloto Estelar (`piloto`) — ficha técnica

> **Renovado el 9-oct (Tarea 61): «la ruta de las balizas».** El diseño, las reglas, los niveles, el tutorial, la medida y los desvíos están en [`docs/diseno-piloto.md`](../diseno-piloto.md); el boceto aprobado, en [`docs/previews/piloto-balizas-boceto.html`](../previews/piloto-balizas-boceto.html). Código: `Games/Piloto/` (`PilotContract`, `PilotRoute`, `PilotSpawn`, `PilotRun`, `PilotLayout`, `PilotMetrics`, `PilotSignalSprites`, `PilotSounds`, `PilotGameController` en partes). Lectura en la app: `data/Piloto.kt`.
> Lo de abajo es la historia del juego anterior (con piloto automático y «costo de multitarea», que ya no existen por la regla permanente 1 de Ricardo); se conserva solo como referencia.

## Historia: el Piloto Estelar anterior (27-sep a 9-oct)

**Piloto Estelar** (`Games/Piloto/`, id `piloto`, dominio atención): multitarea al estilo NeuroRacer (Anguera et al.,
Nature 2013) + señales periféricas breves (UFOV). La nave vuela por una ruta de balizas que serpentea: un pulgar la
guía (tocar o arrastrar en la franja de abajo); el otro dedo atrapa solo las señales de la misión (forma + color,
íconos de `SymbolSprite`) que aparecen un instante con un anillo que se vacía.
- Reglas puras y pruebas: `PilotContract` / `PilotContractTests` (11). Ruta: `CenterAt` (dos senos), cada tramo guarda
  su amplitud/ancho al crearse (un cambio de nivel no deforma lo visible). Señales go/no-go, 40% de misión; desde el
  nivel 3 distractores de la misma forma y otro color; desde el 5, en la periferia.
- Vuelo: 15 s de piloto automático (solo señales) → "¡A LOS MANDOS!" → las dos tareas. Reto 90 s; Precisión = más
  lento, termina tras 24 señales. Dos `AdaptiveDifficulty` (pilotaje por ventanas de 1,5 s con ≥85% en ruta; señales
  por señal). Hiperimpulso: racha ≥5 + 4 ventanas limpias = puntos x2 6 s y estrellas a hiperespacio.
- **Costo de multitarea** = caída relativa de Pr (aciertos − falsas alarmas) entre piloto automático y a los mandos.
  Viaja en `StroopSessionMetrics.multitask_cost` → `GamePlayResult.multitaskCost` (no se guarda en Room) → línea en
  `GameResultScreen`. Pendiente: guardarlo por partida para mostrar su evolución.
- Nuevos compartidos: `Shared/AnswerMarkSprite` (✓/✗ de arcilla, sirve para la marca pendiente en los otros juegos),
  `GameWorld.Hyperspace`. App: `GameRegistry`, ícono en `GameIcon.kt`, botón Debug (Reto 90 s).
- Vista previa: `python3 tools/art-preview/piloto.py <raw>` → `docs/previews/piloto-estelar.png`.
- Probado por Ricardo (27-sep): "me encantó", sonidos muy bien. La primera vez no captó el momento de tomar los
  mandos y el cartel de la misión confunde un poco al principio, pero el piloto automático sirve de práctica: por
  ahora sin ajustes. Falta: respetar "quitar animaciones" (la config de Unity no lo trae), sonido propio del motor.
