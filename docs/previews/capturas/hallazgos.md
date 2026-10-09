# Hallazgos de las capturas reales y qué se hizo (8-oct-2026)

Revisión a primera vista de las 19 láminas de esta carpeta (Tarea 53) y lo que se arregló después (Tarea 54, commits A y B). Las láminas de hoy ya son las de DESPUÉS de los arreglos.

| Juego | Lo que se vio | Qué se hizo |
|---|---|---|
| Constelaciones (`parejas`) | A los 8 s se ven líneas doradas sin sus luces. | **Es intencional**: al terminar un cielo las luces se encogen a estrellas chicas (y después se van) y las líneas doradas quedan 2,6 s a la vista, para mostrar lo que armaste de memoria (`EndBoard`, `docs/diseno-constelaciones.md`). Sin cambios. |
| Rastro de luz (`secuencia`) | Tutorial, paso 3: cuadrado oscuro detrás de la luz dorada. | Arreglado en la raíz: `NubiCoach` no apagaba las esquinas oscuras del foco anterior cuando seguía un aviso (sin velo). Prueba nueva en `HudRulesTests`. |
| Bodega de carga (`bodega`) | La pausa no ofrecía «Cómo se juega». | **Era una falsa alarma de la toma**: la pausa se sacaba a los ~41 s, cuando el piloto automático ya había terminado sus 6 pedidos (`_phase = Done`) y una partida terminada no ofrece el tutorial. Ahora la pausa se saca a los 21 s, a mitad de partida. |
| Rumbo a Casa (`rumbo`) | «Recuerda cada giro» pegada arriba y tapando el título. | Arreglado: los avisos salen debajo del marcador (`Toast.SetBelowHud`). |
| La estación de correo (`correo`) | En el tutorial paso 3 el globo de Nubi queda sobre los buzones Coralia y Celesta. | **Se deja**: el paso nombra la caja fuerte y la carta del encargo (ambas se ven y están protegidas); los buzones de colores no son parte de ese paso. |
| Tinta o Palabra (`stroop`) | La fila de 16 puntos de avance se salía por la derecha. | Arreglado: la fila cabe con 16 dp de margen a cada lado (`ProgressDots`, con prueba). |
| Piloto Estelar (`piloto`) | Los puntos de la ruta se dibujaban sobre el panel «Desliza aquí…». | Arreglado: la ruta termina arriba de la franja. |
| Freno de Emergencia (`freno`) | «¡FRENO PERFECTO!» pisaba el aro de la señal; «¡Nuevo límite!» tapaba el título. | Arreglados los dos (el letrero va debajo del aro; el aviso, debajo del marcador). |
| Satélites (`satelites`), Radar (`radar`), Carga exacta (`calculo`), ¿Verdad o disparate? (`disparate`), Cosecha (`cosecha`) | Limpios en lo visual. | Solo la letra de 14 dp (ver abajo). |
| Acoplamiento (`acoplamiento`) | «Con calma» cortado arriba y tapando el encabezado. | Arreglado: el aviso sale debajo del marcador. |
| Aterrizaje Lunar (`aterrizaje`) | El foco del tutorial cortaba los rótulos «0» y «10»; un rectángulo gris sobre la regla. | El foco ahora los incluye. El rectángulo es la **franja guía del lugar justo** (`GuideZone`, a propósito, con sprite redondeado): se deja. |
| Engranajes (`engranajes`) | Marco del tutorial cortado y cartel a 5 px del borde derecho. | Arreglado: la escena no pasa de 0,88 y el cartel queda a ≥ 16 dp (prueba nueva). |
| En la punta de la lengua (`anagramas`) | En el paso 1 del tutorial la tarjeta de la definición estaba VACÍA. | Arreglado: era un error real (el foco congelaba el reloj antes de que la tarjeta se mostrara, alfa 0); ahora aparece entera desde el primer cuadro. |
| Lluvia de meteoros (`meteoros`) | Nacían sobre el título; estela sobre el marcador; «se fue: fondo» chico. | Nacen debajo del marcador y la estela crece sin pasar de él. «se fue: …» mide 46 unidades = 15 dp (≥ 14): sin cambio. **Queda**: al empezar, el primer meteoro pasa sobre la línea de ayuda «Toca las palabras que existen…» (ya pasaba antes, más tarde). |
| La estrella intrusa (`intrusa`) | Las palabras parecen de ~29 dp de alto. | **Verificado**: el área TOCABLE de cada palabra es de al menos 64 dp (`IntrusaLayout.TouchArea`, con prueba `TouchArea_NuncaBajaDe64Dp…`), mayor que el rótulo. Sin cambios. |

## Tutoriales nuevos (Tarea 56, 9-oct)

Las láminas de Cosecha, ¿Verdad o disparate? y La estrella intrusa ya traen sus pasos 1 y 3 del tutorial. Lo que se vio al revisarlas:

- **¿Verdad o disparate?, paso 3**: Nubi y su globo caían sobre el botón VERDAD (tapaban su ícono). **Arreglado**: los avisos del tutorial protegen los dos botones (y la placa y, en el Reto, la barra de señal).
- **La estrella intrusa, paso 3**: el hueco cortaba las placas «manzana» y «pera». **Arreglado**: el hueco es la caja de la figura más las placas de las cuatro palabras.
- **Cosecha, paso 1**: el globo de Nubi queda pegado al borde de arriba de Sembrar. **Se deja**: no tapa ningún texto ni el hueco (la C) y en pantalla 16:9 no hay otro lugar sin cubrir letras.

## Letra de 14 dp

Se midió con una regla exacta en el smoke (el tamaño efectivo de cada texto visible entre 3 unidades por dp). Estaban bajo 14 dp: «Nivel N», el avance («1 de 40»…) y «racha» del marcador de TODOS los juegos (13,3 y 9,3 dp); «Límite del freno» (12) y su valor (13,3) en Freno; «Tu estación» (11,3) y «PUERTO» (10,7) en Acoplamiento; el cartel de la misión de Piloto (12) y su franja «Desliza aquí…» (13,3); «Rescatados» (13,3) y la ayuda (12,3) de Radar; «Toca la señal para ir al cristal» (13,3) y «faro» (12) de Rumbo; la ayuda de Aterrizaje (13,3), la cruz de Satélites (12,7), el «Extra» de los resultados de 9 juegos (13,3) y los mínimos de los ajustes automáticos de los avisos de Acoplamiento, Cosecha, Freno, Radar, Rumbo, Satélites y Stroop (hasta 9,3 dp). Constelaciones, Rastro de luz y Carga exacta SÍ estaban en regla (la medición por cuadro de la revisión visual era imprecisa).

## Lo que no se hizo (hoja de ruta)

En pantalla 20:9 sobra casi un tercio de abajo en Satélites, Rescate relámpago, Rumbo, La estrella intrusa y Bodega: se resuelve agrandando el área de juego cuando se toque cada uno (etapas 1 y 2). Tampoco se hicieron los pilotos automáticos que faltan en 15 juegos.
