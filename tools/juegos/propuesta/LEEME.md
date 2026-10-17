# Propuesta de volumen: cómo se sacan las capas de Engranajes

`PropuestaHook.cs` es un gancho **temporal** del Editor (nunca se commitea dentro de `Assets/`). Con él, el capturador de pantallas saca, para un cuadro de Engranajes, además de la toma normal (el «hoy»):

- `NN-<toma>-fondo.png`: el mismo cuadro sin el cohete ni la máquina (queda la sala y el cielo);
- `NN-<toma>-gII-K.png` y `-W.png`: el grupo II (cada engranaje, cada correa con sus poleas, el motor, su flecha, cada cartel, cada parte del cohete…) SOLO, sin la sombra ni el soporte horneados, sobre un telón negro (K) y uno blanco (W). Con las dos, el alfa de cada píxel sale exacto (`alfa = 1 − (W − K)`, color premultiplicado = K), así que cada objeto tiene su silueta aunque toque a otro;
- `<toma>-grupos.txt`: «II<TAB>nombre» de cada grupo, de abajo hacia arriba.

Además carga a la fuerza las máquinas de los niveles 7 y 12 (`CargaNivel`: correas, barra, tres cartels, dos llaves) y las deja en la fase de juego (`Arrancar` activo) para sacarlas.

## Pasos

1. Copiar `PropuestaHook.cs` a `unity/NeuroVidaCore/Assets/Scripts/Bootstrap/Editor/`.
2. Parchar **temporalmente** `ScreenshotRunner.Shot` (renombrar su cuerpo a `ShotRaw` y agregar el envoltorio de abajo) y llamar a `PropuestaNiveles()` después de la toma `t40` en `PlayShots`:

```csharp
private static bool PropuestaOn() => Environment.GetEnvironmentVariable("NUBI_SHOTS_PROPUESTA") == "1" && _gameId == "engranajes" && NeuroVida.Bridge.EditorTools.PropuestaHook.Hay();

private static IEnumerator PropuestaNiveles()
{
    foreach (var nivel in new[] { 7, 12 })
    {
        if (!NeuroVida.Bridge.EditorTools.PropuestaHook.CargaNivel(nivel)) continue;
        yield return Wait.For(2.2);
        Shot("n" + nivel);
    }
}

private static void Shot(string name)
{
    ShotRaw(name);
    if (!PropuestaOn()) return;
    if (name != "t08" && name != "t20" && name != "n7" && name != "n12") return;
    int c0 = _count;
    var nombres = NeuroVida.Bridge.EditorTools.PropuestaHook.Grupos();
    var lineas = new List<string>();
    NeuroVida.Bridge.EditorTools.PropuestaHook.Solo(-1, null); Canvas.ForceUpdateCanvases(); ShotRaw(name + "-fondo");
    for (int i = 0; i < nombres.Count; i++)
    {
        lineas.Add(i.ToString("00") + "\t" + nombres[i]);
        NeuroVida.Bridge.EditorTools.PropuestaHook.Solo(i, Color.black); Canvas.ForceUpdateCanvases(); ShotRaw(name + "-g" + i.ToString("00") + "-K");
        NeuroVida.Bridge.EditorTools.PropuestaHook.Solo(i, Color.white); Canvas.ForceUpdateCanvases(); ShotRaw(name + "-g" + i.ToString("00") + "-W");
    }
    NeuroVida.Bridge.EditorTools.PropuestaHook.Vuelve(); Canvas.ForceUpdateCanvases();
    File.WriteAllLines(Path.Combine(_dir, name + "-grupos.txt"), lineas.ToArray(), new UTF8Encoding(false));
    _count = c0;
}
// en PlayShots, justo después de «else Shot(step.Item1);»:
//     if (step.Item1 == "t40" && PropuestaOn()) yield return PropuestaNiveles();
```

3. `NUBI_SHOTS_PROPUESTA=1 bash tools/verificar-todo.sh --capturas Engranajes` (~2 min; necesita tarjeta de video). Las tomas quedan en `unity/test-results/capturas/engranajes/`.
4. **Deshacer todo lo de Unity** (borrar el gancho y su `.meta`, `git checkout` de `ScreenshotRunner.cs` y de `docs/previews/capturas/engranajes.png`, que la corrida regenera) antes de cualquier commit.
5. `python -I tools/juegos/propuesta_engranajes.py <carpeta con las tomas> n12` (y `t08` para el cartel que falla), `propuesta_acoplamiento.py`, `propuesta_medidas.py aco | eng <carpeta> n12` y `propuesta_volumen.py` (arma `docs/previews/plasticina/propuesta-volumen.png`).

Los niveles salen al azar en cada corrida: las tomas `t08` y `t20` cambian de máquina; `n7` y `n12` sirven para ver correas, barra y tres cartels.
