# Carga exacta — reemplaza a Cálculo Sereno (aprobado por Ricardo el 4-oct)

Reemplaza a Cálculo Sereno. El id interno `calculo` SE MANTIENE, para no perder avance, marcas ni historial. Área:
Razonamiento.
Boceto jugable aprobado: `docs/previews/carga-exacta-boceto.html` (https://claude.ai/artifact/PZ5Uz57sFwAwiczX1tdgcH).
Ricardo (4-oct): «me gusta».

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
- Nota común «Medida de esta partida… No es un diagnóstico». Consejo en `ResultAdvice`, por ejemplo: «Mira primero si
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
