# En la punta de la lengua — reemplaza a Anagramas (aprobado por Ricardo el 3-oct)

Reemplaza a Anagramas. El id interno `anagramas` SE MANTIENE, para no perder avance, marcas ni historial. Área: Lenguaje.
Boceto jugable aprobado: `docs/previews/punta-de-la-lengua-boceto.html` (https://claude.ai/artifact/Q8KXkxXiXajQCQFxYPrtbL).
Ricardo (3-oct): «lo apruebo, tal como se ve, con la mecánica y animaciones cumple mis criterios». Pidió cuidar sobre todo
la **frustración**, porque es lo que más hace desinstalar este tipo de apps, y movimientos suaves, luces y sonido como en
Rastro de luz y La estrella intrusa.

Por qué se reemplaza Anagramas:
- repetía a Cosecha de palabras (armar palabras con letras);
- tenía un banco de ~95 palabras, que se repite enseguida;
- al área le faltaba **encontrar una palabra que uno sabe**, la queja de memoria más común con la edad.

## 1. La idea en una línea

Nubi capta una señal: la **definición** de una palabra. La buscas en tu memoria. Si la tienes, la armas con letras. Si no
sale, una escalera de ayudas aclara la señal hasta que aparece. Nunca te quedas trabado.

## 2. Ciencia (PubMed, 3-oct)

- El estado de «la tengo en la punta de la lengua»: revisión de Brown, 1991, *Psychol Bull* 109:204-23,
  [doi:10.1037/0033-2909.109.2.204](https://doi.org/10.1037/0033-2909.109.2.204) (PMID 2034750).
- Aumenta con la edad, aunque el vocabulario se mantiene: Shafto y otros, 2007, *J Cogn Neurosci* 19:2060-70,
  [doi:10.1162/jocn.2007.19.12.2060](https://doi.org/10.1162/jocn.2007.19.12.2060) (PMID 17892392).
- Dar la primera letra o el largo son pistas clásicas para destrabar la palabra (las estudia Brown, 1991): por eso son los
  peldaños de la escalera.
- Qué NO decir: que previene el deterioro ni que «recupera la memoria». Sí: «mide cuántas palabras encontraste por tu cuenta
  en esta partida».

## 3. Cómo se juega (como en el boceto)

- **Definición**: tarjeta de «transmisión» con ondas de señal que laten. El texto se escribe solo, letra por letra (~22 ms
  cada una). Debajo, mientras no hay ayudas, una nebulosa suave y la frase «¿Cuál es la palabra?».
- **«¡La tengo!»**: aparecen las fichas con las letras de la palabra y algunas de más, flotando suavemente. Se tocan en
  orden; cada ficha vuela a su hueco y suena con una nota de la escala pentatónica. Una ficha puesta se toca para
  devolverla. «Borrar» devuelve todas, salvo la primera letra dada por una ayuda.
- **«Una ayuda»**, una escalera de 3 peldaños: 1) cuántas letras tiene (aparecen los huecos); 2) la primera letra (queda
  fija y dorada); 3) solo sus letras, sin las de más. Con cada ayuda la señal se ve más nítida. Después del tercero, el
  botón dice «Ver la palabra».
- **Al completar**: si es correcta, cada letra se enciende en orden con su nota, suena un acorde y la palabra se vuelve un
  **lucero redondo** (nunca una estrella con puntas) que vuela al cielo de arriba. Si no lo es, las letras tiemblan
  suave y vuelven («Casi. Las letras vuelven: prueba otro orden»). A la segunda, Nubi la muestra: «Era "búho". Volverá
  otro día».
- **Color del lucero**:

  | Color | Cómo la encontraste |
  |---|---|
  | dorado | sola |
  | plateado | con 1-2 ayudas |
  | cobre | con las letras justas |
  | azul | te la mostró Nubi |

  Las ayudas no restan puntos de forma visible: solo cambian el color.
- **Las azules vuelven**: las palabras que Nubi tuvo que mostrar se guardan y reaparecen en una partida de otro día.
  Esto sirve para aprender, no es un castigo.
- Tildes: las fichas van sin tilde (la Ñ es su propia ficha) y la palabra final se muestra bien escrita («búho»).

## 4. Banco de definiciones

- Unas **400 palabras** para empezar, en 5 niveles. Cada una con:
  - la palabra bien escrita;
  - las letras de las fichas (mayúsculas, sin tildes, con Ñ);
  - una **definición propia**;
  - el nivel;
  - la banda de frecuencia (el mismo léxico de Cosecha y Meteoros);
  - una categoría (objeto, animal, naturaleza, oficio, acción, emoción, lugar…).
- **Definiciones escritas para Nubi, nunca copiadas** del diccionario de la RAE ni de otro: tienen derechos. Tono
  cercano y concreto («Lo miras para saber qué hora es»), ≤ 90 caracteres, sin la palabra ni su familia (ni la raíz de 4
  letras), con una sola respuesta razonable.
- Fuera:
  - nombres propios (filtro de `tools/lexico/propios.py`) y marcas;
  - palabras groseras, de enfermedad, muerte o violencia, y políticas o religiosas;
  - regionalismos fuertes: el público es Latinoamérica y España. Si una cosa tiene otro nombre según el país (por ejemplo, autobús o micro), se elige otra palabra.
- Niveles:

  | Nivel | Frecuencia | Largo | Ejemplo |
  |---|---|---|---|
  | 1 | muy comunes | 4-5 letras | reloj |
  | 3 | comunes | 6-8 letras | colmena |
  | 5 | menos comunes | hasta 10 letras | nostalgia |

  Lo concreto antes que lo abstracto.
- Herramienta y pruebas en Python (como `tools/cosecha/`) que validen lo anterior. Además, una **lámina de revisión para
  Ricardo** (psicólogo) con una muestra de ~40 definiciones de todos los niveles.

## 5. Dificultad (DDA común)

`AdaptiveDifficulty` de 5 niveles:
- **Qué sube:** la frecuencia y el largo de la palabra, y las letras de más (2 en el nivel 1, 4 en el nivel 5).
- **Mayores:** una letra de más menos.
- **Para el DDA**, acierto = encontrada sola o con 1-2 ayudas; con las letras justas cuenta medio; mostrada por Nubi
  cuenta como error.
- **Precisión:** 8 palabras. **Reto:** 120 s, todas las que alcances.

## 6. Medidas al final

- «Encontraste X de N por tu cuenta» (luceros dorados) y la lista de palabras con su lucero.
- Telemetría: cuántas fueron solas, con pista, con letras y mostradas, y el tiempo medio hasta «¡La tengo!» de las que
  salieron solas.
- Nota común: «Medida de esta partida… No es un diagnóstico». Consejo en `ResultAdvice`, por ejemplo: «Si una palabra no
  sale, piensa en cómo empieza o en otra parecida: suele destrabarla».

## 7. Tutorial y movimiento reducido

- **Tutorial guiado** (sistema común): una palabra fácil donde Nubi muestra «¡La tengo!», arma la palabra, y otra donde
  enseña a pedir una ayuda. «Cómo se juega» en la pausa.
- **Movimiento reducido:**
  - **Se quita lo decorativo:** el texto aparece completo, sin ondas ni flotación, sin temblor ni chispas.
  - **Se mantiene:** el vuelo de la ficha al hueco (es la tarea), hecho en su forma corta, y los tiempos (`Motion.Hold`).
