# Tutoriales con Nubi: nada tapado, nada encima de nada

Ricardo (5-oct, probando en su teléfono): «en los tutoriales aún no queda muy claro; letras que quedan sobre otras, recuadros de Nubi que tapan otras letras también relevantes».
La causa: `NubiCoach.PlaceNubi()` solo evitaba el hueco; no sabía que existían la pregunta, la consigna, los rótulos ni el marcador, y el globo medía un alto fijo (200) con la letra
achicándose si no cabía. Esto es el arreglo de sistema (no un parche por juego). Código: `Games/Shared/NubiCoach.cs`, `CoachLayout.cs`, `CoachTexts.cs`.

## Cómo funciona

1. **Zonas protegidas.** Cada paso (`Touch`, `Watch`, `Notice`) recibe, además del hueco, una lista `keep:` de zonas que deben quedar a la vista (la pregunta, la consigna, el rótulo del que habla
   el texto, el contador). Se declaran con `coach.Zone(rect)`, `coach.ZoneOf(rects…)` o `coach.ZoneOfTexts(textos…)` (esta última cubre justo las letras, no toda la caja). Hasta 3.
2. **Se iluminan junto al hueco.** El velo gris es toda el área MENOS el hueco y las zonas protegidas (`CoachLayout.Subtract`): la persona lee la pregunta mientras Nubi habla de ella.
3. **Nada de texto del juego bajo Nubi ni el globo.** Además de lo declarado, `NubiCoach` mide cada `Text` activo y visible del juego (las letras realmente dibujadas, no su caja) y lo trata como zona
   que no se tapa. Así no hace falta acordarse de declarar cada rótulo: lo declarado sirve para ILUMINAR; lo demás se respeta igual.
4. **El globo crece con el texto.** Alto según las líneas (máximo 3); la letra nunca baja de 54. Si un texto no cabe en 3 líneas se acorta el texto (la prueba falla si pasa).
5. **Búsqueda del lugar** (`CoachLayout.Place`): prueba los dos costados, 4 tamaños de Nubi (300 / 240 / 180 / 150), 5 anchos de globo y una altura cada 10 unidades. Gana el de cero solape que menos se
   aleja de lo esperado (abajo, del lado contrario al hueco, globo ancho, Nubi grande). Evita el hueco (+24), las zonas (+10), los textos del juego (+6) y el dedo que insiste. Si ninguno queda
   limpio gana el que menos tapa, queda un aviso en el log (`[Coach] sin lugar limpio…`) y las pruebas fallan. Si algo se mueve y el lugar deja de servir, se busca otro (como mucho cada 0,3 s).

## Qué zonas declara cada juego

| Juego | Zonas protegidas (`keep`) |
|---|---|
| Carga exacta (`calculo`) | el número del reactor y su rótulo «carga exacta» (en todos los pasos) |
| Rastro de luz (`secuencia`) | el contador «luces recordadas» |
| Aterrizaje lunar (`aterrizaje`) | el número grande de la misión (a dónde aterrizar) |
| Dos orillas (`stroop`) | la cinta «Responde: TINTA / PALABRA» y la tarjeta con la palabra; el hueco son solo los 4 botones de color (antes era todo el escenario y no dejaba dónde poner a Nubi) |
| En la punta de la lengua (`anagramas`) | la tarjeta de la definición (pasos «¡La tengo!», letras y «Una ayuda»; en el primero ES el hueco) |
| Engranajes (`engranajes`) | la antena (que se ve en los tres pasos) y, en el último, el cartel de la antena |
| Bodega de carga (`bodega`) | la tarjeta de arriba (en los pasos de mirar y de tocar la escotilla pedida); el hueco es el área de la bodega (sin el borde del casco: así queda lugar para Nubi) y, al tocar, la escotilla pedida. Durante el tutorial el carro no lleva textos |
| Freno de emergencia (`freno`) y Lluvia de meteoros (`meteoros`) | ninguna declarada: el hueco es lo único de lo que habla el texto; lo demás lo respeta la detección automática |

## Textos (todos en `CoachTexts.cs`)

Reglas: una idea por globo; primero el verbo de lo que hay que hacer («Toca…», «Mira…»); nada que dependa de algo que todavía no se vio; el texto nombra lo iluminado con la misma palabra que se
lee en pantalla. La prueba `CoachLayoutTests` comprueba que cada texto cabe en 3 líneas con la letra a 54 y que no pasan de 62 caracteres ni de 2 frases.

## Cómo se comprueba

- **EditMode** (`CoachLayoutTests`): colocación en 20:9, 18:9 y 16:9 con huecos de varios tamaños y posiciones + una zona de pregunta (sin solape), con texto del juego, el caso imposible (gana el
  que menos tapa y no es «limpio»), el velo (cubre todo menos lo iluminado) y todos los textos.
- **Smoke** (`HeadlessPlaymodeSmokeTest`): cada tutorial corre en las TRES formas de pantalla (el lienzo de cada juego pasa a 1080×2400, 1080×2160 y 1080×1920) hasta su último aviso; `NubiCoach`
  registra cada paso (`AuditEnabled`) y el smoke falla si Nubi o el globo tapan algo o si el texto se dibuja en más de 3 líneas. Deja un JSON por juego y forma en
  `unity/test-results/coach-audit/`. En el Editor, las jugadas que espera el tutorial se hacen solas a los 2 s (`GuidedTutorial.AutoPlay`) para que el arranque pase por todos los pasos.
  Engranajes entra desde el rediseño «Taller de reparación» (5-oct): su tutorial de 3 focos se revisa igual que los demás.
- **Láminas** (`python tools/coach-preview/tutoriales.py [alto]` → `docs/previews/tutoriales/<juego>-paso<N>.png`): NO son capturas (el smoke corre sin gráficos); son los rectángulos reales de
  cada paso: el hueco y las zonas iluminadas, los textos del juego (gris), Nubi y el globo con su texto en la misma letra.
