# NeuroVida — proyecto Unity

Los 9 juegos de NeuroVida. Se embeben en la app Android como librería ("Unity as a Library").
Arquitectura y reglas: [`../CLAUDE.md`](../CLAUDE.md).

- `NeuroVidaCore/`: el proyecto (Unity 6000.0.84f1). Se versiona `Assets/`, `Packages/manifest.json` y
  `ProjectSettings/`.
- `AndroidExport/`: proyecto Gradle exportado (fuera de git). Nunca se edita a mano. Se regenera con el menú
  `NeuroVida > Exportar como librería Android` o sin abrir el Editor:
  `Unity.exe -batchmode -nographics -quit -projectPath NeuroVidaCore -executeMethod NeuroVida.Bridge.EditorTools.AndroidLibraryExport.Export`.
- `test-results/`: registros de `tools/verificar-todo.sh` (fuera de git).

## Trabajar en el Editor

1. Abrir `NeuroVidaCore/` desde Unity Hub (la primera vez genera `Library/`, tarda).
2. Pruebas: `Window > General > Test Runner` → EditMode → Run All (todas en verde).
3. Jugar sin teléfono: escena `Assets/Scenes/SecuenciaPilotoTest.unity` → Play. Abre Secuencia; nivel, modo y edad
   se eligen en el Inspector de `EditorPlaytestBootstrap` (los smoke tests abren los otros juegos con
   `EditorPlaytestBootstrap.GameIdOverride`).
4. Después de cualquier cambio: reexportar y recompilar la app antes de probar en el teléfono. La cuenta regresiva de
   las builds de depuración muestra una marca (`CountdownScreen.StyleStamp`) para confirmar que el APK trae la
   versión nueva.

Los juegos Unity son arm64: se prueban en un teléfono real, no en el emulador x86_64.
