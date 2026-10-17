# Propuesta de volumen: cómo se sacan las capas de Engranajes

`PropuestaHook.cs` es un gancho **temporal** del Editor (nunca se commitea dentro de `Assets/`). Con él, el capturador de pantallas saca, para un cuadro de Engranajes, además de la toma normal (el «hoy»):

- `NN-<toma>-fondo.png`: el mismo cuadro sin el cohete ni la máquina (queda la sala y el cielo);
- `NN-<toma>-gII-K.png` y `-W.png`: el grupo II (cada engranaje, cada correa con sus poleas, el motor, su flecha, cada cartel, cada parte del cohete…) SOLO, sin la sombra ni el soporte horneados, sobre un telón negro (K) y uno blanco (W). Con las dos, el alfa de cada píxel sale exacto (`alfa = 1 − (W − K)`, color premultiplicado = K), así que cada objeto tiene su silueta aunque toque a otro;
- `NN-<toma>-sincanto.png` (Tarea 115): el mismo cuadro con las capas `Soft` y `Canto` de los engranajes apagadas: lo que hace el canto y nada más (`propuesta_medidas.py engr` compara los dos);
- `<toma>-grupos.txt`: «II<TAB>nombre» de cada grupo, de abajo hacia arriba.

Además carga a la fuerza las máquinas de los niveles 7 y 12 (`CargaNivel`: correas, barra, tres cartels, dos llaves; las deja en la fase de juego con `Arrancar` activo), **mide el costo de las capas** del nivel 12 con y sin el canto (`MideCosto`, ms por cuadro de CPU del Editor: sale en el log como «[Propuesta] COSTO …»), toca el motor (`n12p`, el estado presionado) y gasta las llaves tocando una más (`n12b`, el cambio bloqueado con su moneda).

## Pasos

1. Copiar `PropuestaHook.cs` a `unity/NeuroVidaCore/Assets/Scripts/Bootstrap/Editor/`.
2. Aplicar **temporalmente** el parche de [`ScreenshotRunner.parche.cs.txt`](ScreenshotRunner.parche.cs.txt) a `ScreenshotRunner.cs` (renombrar el cuerpo de `Shot` a `ShotRaw`, agregar lo del parche y la llamada a `PropuestaNiveles()` después de la toma `t40` de `PlayShots`).
3. `NUBI_SHOTS_PROPUESTA=1 bash tools/verificar-todo.sh --capturas Engranajes` (~2 min; necesita tarjeta de video). Las tomas quedan en `unity/test-results/capturas/engranajes/`. Un «NullReferenceException» en `RevealRoutine` del log es del gancho (carga una máquina con el piloto automático a mitad de una revelación), no del juego.
4. **Deshacer todo lo de Unity** (borrar el gancho y su `.meta`, `git checkout` de `ScreenshotRunner.cs` y de `docs/previews/capturas/engranajes.png`, que la corrida regenera con las tomas de más) antes de cualquier commit. Para la lámina de capturas de verdad, volver a correr SIN la variable.
5. `python -I tools/juegos/propuesta_engranajes.py <carpeta con las tomas> n12` (la propuesta compuesta; y `t08` para el cartel que falla), `propuesta_acoplamiento.py`, `propuesta_medidas.py aco | aco2 | eng <carpeta> n12` (la propuesta) o `engr <carpeta> n12` (el juego integrado: con canto contra sin canto) y `propuesta_volumen.py` / `propuesta_acoplamiento_v2.py` (las hojas).

Los niveles salen al azar en cada corrida: las tomas `t08` y `t20` cambian de máquina; `n7` y `n12` sirven para ver correas, barra y tres cartels.

## Las tomas de los botones (Tarea 116)

`ScreenshotRunner.botones.parche.cs.txt` (con `NUBI_SHOTS_BOTONES=1`, el mismo `PropuestaHook.cs` y su método `PresionaArrancar`) saca tres tomas del nivel 12 con «Arrancar» activo: `rep` (en reposo), `pres` (presionado, 30 ms después de tocarlo) y `llaves` (con todas las llaves usadas y el toque bloqueado). `python -I tools/juegos/diagnostico_botones.py --despues <carpeta con rep.png y pres.png>` las mide contra los botones aprobados y arma `docs/previews/plasticina/diagnostico-botones.png`. Igual que lo demás de esta carpeta: gancho y parche son TEMPORALES, se deshacen antes de cualquier commit (`git checkout` de `ScreenshotRunner.cs` y de `docs/previews/capturas/engranajes.png`).
