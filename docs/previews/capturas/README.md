# Capturas reales de los juegos

Fecha: 2026-10-18 · commit: a3593dd (con cambios sin comitear)

Cada lámina muestra UN juego corriendo de verdad en Unity con el lienzo en forma de teléfono (1080×2400): la primera pantalla jugable, a los 8, 20 y 40 s de juego, la pausa, la cortina «¡Listo!» y la pantalla final si la partida llega a su fin, una toma con «quitar animaciones» y, en los juegos con tutorial, los pasos 1 y 3. Sirven para ver a golpe de ojo errores visuales: textos fuera de su lugar, cosas tapadas, fondos que no cubren, elementos detrás de otros. No son una prueba de que el juego esté bien: donde no hay piloto automático (solo Engranajes, Bodega, Constelaciones y Correo se juegan solos) la partida se queda en su primera situación. Las tomas sueltas, a tamaño completo, quedan en `unity/test-results/capturas/<juego>/` (fuera de git).

Se regeneran con `bash tools/verificar-todo.sh --capturas todos` (necesita tarjeta de video; no corre en el CI).

Corrida completa: 1197 s · 269 capturas · 19 juegos.

| Juego | Lámina | Tomas | Tiempo | Errores de consola y avisos del guion |
|---|---|---|---|---|
| Constelaciones (`parejas`) | [parejas.png](parejas.png) | 12 | 58 s | error de consola: MissingReferenceException: The object of type 'UnityEngine.CanvasGroup' has been destroyed but you are still trying to acc |
| Rastro de luz (`secuencia`) | [secuencia.png](secuencia.png) | 12 | 58 s | sin errores de consola |
| Bodega de carga (`bodega`) | [bodega.png](bodega.png) | 12 | 60 s | error de consola: MissingReferenceException: The object of type 'UnityEngine.CanvasGroup' has been destroyed but you are still trying to acc |
| Rumbo a Casa (`rumbo`) | [rumbo.png](rumbo.png) | 11 | 53 s | sin errores de consola |
| La estación de correo (`correo`) | [correo.png](correo.png) | 16 | 46 s | sin errores de consola |
| Tinta o Palabra (`stroop`) | [stroop.png](stroop.png) | 13 | 61 s | error de consola: MissingReferenceException: The object of type 'UnityEngine.CanvasGroup' has been destroyed but you are still trying to acc |
| Piloto Estelar (`piloto`) | [piloto.png](piloto.png) | 17 | 57 s | sin errores de consola |
| Freno de Emergencia (`freno`) | [freno.png](freno.png) | 13 | 61 s | sin errores de consola |
| Satélites (`satelites`) | [satelites.png](satelites.png) | 14 | 54 s | sin errores de consola |
| Rescate relámpago (`radar`) | [radar.png](radar.png) | 21 | 55 s | sin errores de consola |
| Acoplamiento (`acoplamiento`) | [acoplamiento.png](acoplamiento.png) | 21 | 152 s | sin errores de consola |
| Carga exacta (`calculo`) | [calculo.png](calculo.png) | 12 | 56 s | error de consola: MissingReferenceException: The object of type 'UnityEngine.CanvasGroup' has been destroyed but you are still trying to acc |
| Aterrizaje Lunar (`aterrizaje`) | [aterrizaje.png](aterrizaje.png) | 18 | 70 s | sin errores de consola |
| Engranajes (`engranajes`) | [engranajes.png](engranajes.png) | 12 | 58 s | error de consola: MissingReferenceException: The object of type 'UnityEngine.CanvasGroup' has been destroyed but you are still trying to acc |
| En la punta de la lengua (`anagramas`) | [anagramas.png](anagramas.png) | 13 | 56 s | sin errores de consola |
| Lluvia de meteoros (`meteoros`) | [meteoros.png](meteoros.png) | 13 | 69 s | sin errores de consola |
| ¿Verdad o disparate? (`disparate`) | [disparate.png](disparate.png) | 13 | 56 s | sin errores de consola |
| Cosecha de palabras (`cosecha`) | [cosecha.png](cosecha.png) | 13 | 58 s | sin errores de consola |
| La estrella intrusa (`intrusa`) | [intrusa.png](intrusa.png) | 13 | 58 s | sin errores de consola |

Lo que se ve mal a primera vista, juego por juego (revisión a mano de estas láminas): [hallazgos.md](hallazgos.md).
