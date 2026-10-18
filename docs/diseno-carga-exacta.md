# Carga exacta — reemplaza a Cálculo Sereno (aprobado por Ricardo el 4-oct)

Reemplaza a Cálculo Sereno. El id interno `calculo` SE MANTIENE, para no perder avance, marcas ni historial. Área:
Razonamiento.
Boceto jugable aprobado: `docs/previews/carga-exacta-boceto.html` (https://claude.ai/artifact/PZ5Uz57sFwAwiczX1tdgcH).
Ricardo (4-oct): «me gusta».

**Estado: implementado el 4-oct (tarea 32).** Código en `Games/Calculo/` (`CalculoContract.cs`: reglas, generador, solucionador, tablero, medidas; `CargaLayout.cs`; `CargaSounds.cs`; `CalculoGameController.cs`), lectura en la app en `data/Carga.kt`. Diferencias con lo de abajo: (1) si pasan 60 s después de la pista sin lograr la carga, Nubi la deja para otra vez (cuenta como no lograda) para que nadie se quede trabado; (2) el tiempo medio es solo de las cargas logradas SIN pista (el de las que tuvieron pista incluiría la espera hasta la pista); (3) «Camino corto» = los pasos del camino más corto que encuentra el solucionador, sin pista.

Por qué se reemplaza Cálculo Sereno:
- su modo Reto, con cuentas en gotas que caen al agua, es casi igual a «Raindrops» de Lumosity;
- era un examen de cuentas, y el área es Razonamiento. Esto es un acertijo: planificar y probar caminos con números.

## 1. La idea en una línea

El reactor de la nave pide una **carga exacta** (p. ej. 24). Tienes **celdas de energía** con números; las combinas de a dos
(sumar, restar, multiplicar, dividir) hasta que una celda tenga justo esa carga. Vale cualquier camino que llegue.

## 2. Ciencia y originalidad

- Componer un número con otros pide calcular, planificar y probar estrategias: aritmética al servicio del razonamiento.
  Se presenta como juego, sin afirmar que mida una capacidad validada.
- Qué NO decir: que mejora la memoria o previene el deterioro. Sí: «cuántas cargas lograste y en cuánto tiempo».
- La idea es antigua y de dominio público (el juego del «24» y similares). Nombre, arte y reglas son propios. No usar
  nombres ajenos («24 Game», «Countdown», «Cifras y letras»). Va a la lista del abogado con lo demás.

## 3. Cómo se juega (como en el boceto)

- Arriba, el **reactor**: un disco con anillo de luces que giran y la carga grande al centro. Si el nivel pide usar todas
  las celdas, lo dice debajo («usa todas las celdas»).
- Al medio, las **celdas**: fichas de arcilla lila con el número y una ventanita de energía. Entran con un pequeño rebote,
  una tras otra.
- **Jugada:**
  1. Se toca una celda, que se levanta y brilla celeste.
  2. Se toca una operación (+ − × ÷): el botón se pone dorado.
  3. Se toca otra celda: un arco de luz dorado une las dos, la primera vuela a la segunda y quedan fundidas en una con el
     resultado. La celda nueva hace «pop» con chispas y suena una nota que sube con cada paso.
- Una línea de guía dice qué toca hacer: «Toca una celda» → «Ahora toca una operación» → «7 + … toca otra celda».
- **No se permite** lo que no tiene sentido: una división que no es exacta, o un resultado negativo. La jugada no se hace
  y un aviso amable explica por qué («Esa división no da exacta», «Prueba al revés»).
- **«Deshacer»** vuelve un paso atrás y **«Empezar de nuevo»** devuelve las celdas originales. Equivocarse no cuesta nada.
- Si queda una sola celda y no es la carga: «Quedó 15: deshaz y prueba otro camino».
- **Pista de Nubi** a los 25 s sin lograrlo: vuelve a las celdas originales, dice el primer paso de una solución
  («prueba 6 × 4») y esas dos celdas laten en dorado. Solo una pista por carga.
- **Al lograrlo:** el reactor se enciende dorado, la celda ganadora brilla, suena un arpegio y salen chispas; luego viene la
  siguiente carga. El aviso dice «¡Carga exacta!», y además «Camino corto» si usó tantos pasos como la solución
  guardada, o «(con pista)».

## 4. Generación (en el contrato, con pruebas)

Cada carga se arma combinando al azar algunas celdas con las operaciones del nivel. Así siempre hay solución, y se guarda
una para la pista. Reglas:
- **Resultados:** sin negativos, sin divisiones no exactas, sin «× 1» ni «÷ 1»; la carga entre 8 y 99.
- **Celdas:** la carga no puede estar ya entre las celdas, y no hay celdas repetidas (salvo una cuando hay 5).
- **Aceptación:** cualquier camino que llegue a la carga. Si el nivel pide usar todas, solo cuenta cuando queda una
  celda.
- **Pista:** un solucionador (búsqueda sobre los pares) da también el camino más corto.

## 5. Dificultad (DDA común, 5 niveles)

| Nivel | Celdas | Operaciones | Celdas en la solución | Números hasta |
|---|---|---|---|---|
| 1 | 3 | + | 2 | 9 |
| 2 | 3-4 | + − | 2-3 | 12 |
| 3 | 4 | + − × | 2-3 | 9 |
| 4 | 4 | + − × ÷ | 3 | 10 |
| 5 | 4-5 | + − × ÷ | 4 (a veces «usa todas») | 10 |

- **Para el DDA:** acierto = lograda sin pista; con pista cuenta medio.
- **Mayores:** la pista a los 20 s.
- **Precisión:** 8 cargas. **Reto:** 120 s, todas las que alcances. En ningún modo hay nada que caiga ni que apure dentro
  de cada carga.

## 6. Medidas al final

- Cargas logradas, cuántas sin pista y tiempo medio por carga.
- **«Caminos cortos»**: cuántas resolvió en tantos pasos como la solución más corta.
- Nota común «Medida de esta partida… No es un diagnóstico». Consejo: Truco para la próxima, en el final con sentido (`FinalModels`; la marca «Truco: » la lee `ResultAdvice.tipOf`), por ejemplo: «Mira primero si
  multiplicar dos celdas te deja cerca de la carga; después ajusta sumando o restando».

## 7. Tutorial (Nubi entrenadora) y movimiento reducido

- **Tutorial**, con `NubiCoach` y 2-3 focos, ≤ 30 s, en una carga fácil (3 celdas, solo +):
  1. Tocar la primera celda: «Toca una celda».
  2. Tocar «+»: «Elige una operación».
  3. Tocar la segunda celda: «Toca otra celda: se juntan».
  4. Aviso breve al lograrlo.
  «Cómo se juega» va en la pausa.
- **Movimiento reducido:**
  - **Se quita lo decorativo:** el rebote, las chispas, el arco y el giro del anillo; queda un fundido corto.
  - **Se mantiene:** el cambio de número y el color dorado del reactor.


## 8. Plasticina (Tarea 97, 14-oct): arte de Blender y losas del kit

Ricardo: todos los juegos en plasticina. Es SOLO arte: la generación, el DDA, los tiempos, las pistas, el puntaje y las medidas no cambian. Técnica y pesos: [`tools/juegos/README.md`](../tools/juegos/README.md); seguimiento de los 17 juegos: [`plasticina-juegos.md`](plasticina-juegos.md). **Fecha del cambio de arte: 14-oct-2026** (marca `estilo 14-oct · plasticina carga y disparate`; no se agrega `art_version`: si hace falta separar series, se hace por fecha).

| De plasticina | Se queda procedural |
|---|---|
| las **celdas de energía** (losas `md` del kit: crema; elegida, cielo; lograda, sol); con «quitar animaciones» la elegida va además HUNDIDA (la losa presionada: ya no solo cambia de color, se distingue por la forma) | la **ventanita** de la celda (franja de tinta al 20 %), el halo de la pista, el resplandor, las chispas y el arco que junta dos celdas |
| los **botones de operación** (losa «noche»; la elegida, sol) y **Deshacer / Empezar de nuevo** (losa apagada, la más oscura) | el aro de 12 luces que gira, el brillo del núcleo y el resplandor del reactor: son luz |
| el **panel del final** (losa «noche») | los puntitos de progreso y la barra del tiempo |
| el **aro del reactor** (`reactor_aro`, azul noche) y su **núcleo** (`reactor_nucleo`, un domo de plasticina GRIS CLARO: el código lo tiñe de azul noche a dorado con `Image.color`, como antes) | los textos (la carga, «carga exacta», «usa todas las celdas»), siempre `Text` |

Las dos imágenes del reactor comparten el mismo lienzo cuadrado de ±92 dp centrado en el reactor (`PlasticReactorDp`), así que el diseño (`CargaLayout`) no cambia; el aro trae su sombra de contacto (sobran la sombra y el borde de tinta de hoy). Sin el atlas, todo cae al arte de siempre (`AuditVisibility` revisa los dos casos).

**Lo que se midió** (`tools/juegos/mide_calculo.py`): el aro mide 167 dp (hoy 168) y el núcleo 143 dp (hoy 144), centrados; el número blanco rinde 18:1 sobre el núcleo apagado y el de tinta 7,9:1 sobre el dorado (apagado y dorado se distinguen 8,3:1: por luminosidad, no solo por color); el número de tinta sobre las celdas, 15:1 (crema), 8,7:1 (elegida) y 11,6:1 (lograda). El aro (azul noche) rinde 1,5:1 contra el cielo, igual que el de hoy: es el cuerpo del reactor, no un estímulo. Pesa 0,32 MB en el APK. Pruebas: `CalculoPlasticTests`.

### Estado presionado (Tarea 104, 14-oct)

Regla del 14-oct: todo objeto de plasticina que se toca tiene su estado presionado, con textura (la pieza aplastada, un punto más oscura y con la huella de un dedo; mismo lienzo y escala; sin «saltar»). Detalle, medidas y cómo funciona la ayuda común `PlasticTouch`: [`plasticina-juegos.md`](plasticina-juegos.md) § «Estado presionado».
- **Carga exacta:** las celdas, las 4 operaciones y las dos herramientas (Deshacer y Empezar de nuevo) se ven presionadas al tocarlas, con o sin animaciones (`PlasticTouch` en modo cuenta; la «elegida» sigue hundida con «quitar animaciones»). Las herramientas usan la losa «apagada», que ahora trae su `losa_md_apagada_p` en `comun`.

## Cambio de diseño del 18-oct-2026 (Tarea 128): todo hacia la mitad de abajo

`CargaLayout.Compute`: el aire que sobra va arriba del reactor (0,45) y entre los bloques en poco (0,17 / 0,09 / 0,09), así que el reactor, las celdas, las operaciones y los botones quedan compactos en la mitad de abajo, sin huecos grandes. Mismos tamaños, reglas y cantidades.
