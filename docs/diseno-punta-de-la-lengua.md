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

- **Primera pantalla (Tarea 144, 20-oct; Ricardo: «casillas vacías desde el inicio»; Opus: pozos FIJOS)**: «¿Cuál es la palabra?» va pegada a 16 dp bajo la tarjeta y, desde el primer cuadro de la palabra, el banco ya tiene su **bandeja**: una losa «noche» del kit (la del puerto de Acoplamiento) con **15 pozos hundidos** (5 por fila, 3 filas) a mitad de pantalla. **Son los mismos 15 para toda palabra y alcanzan para el banco más grande que el juego puede armar** (la palabra más larga, 10 letras, más el relleno más grande, 4 = 14; una prueba lo compara con los datos reales): **NO dicen cuántas letras tiene** (eso es la ayuda 1 de «Una ayuda», de la que depende lo que mide «sola»; los huecos de LA PALABRA siguen saliendo solo con esa ayuda, más arriba). **Tenue mientras se piensa** (~45 %: un lugar que espera, no algo que llenar ni las letras de la palabra; así no compite con lo que se mide, el tiempo hasta «¡La tengo!»), **firme en 200 ms con «¡La tengo!»** a la vez que caen las fichas (izquierda a derecha, fila por fila; con «quitar animaciones», de golpe); los pozos que quedan sin ficha se desvanecen (200 ms) y la bandeja se achica a las filas que usa; al resolverse la palabra se desvanece. Sin toque ni estado presionado: son hundidos, no botones (`PuntaLayout.TrayPockets`, `TrayAlpha`; `PuntaGameController.AnimateTray`). La ronda guiada del tutorial conserva su banco centrado de siempre, sin bandeja.
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
- Nota común: «Medida de esta partida… No es un diagnóstico». Consejo: Truco para la próxima, en el final con sentido (`FinalModels`; la marca «Truco: » la lee `ResultAdvice.tipOf`), por ejemplo: «Si una palabra no
  sale, piensa en cómo empieza o en otra parecida: suele destrabarla».

## 7. Tutorial y movimiento reducido

- **Tutorial guiado** (sistema común): una palabra fácil donde Nubi muestra «¡La tengo!», arma la palabra, y otra donde
  enseña a pedir una ayuda. «Cómo se juega» en la pausa.
- **Movimiento reducido:**
  - **Se quita lo decorativo:** el texto aparece completo, sin ondas ni flotación, sin temblor ni chispas.
  - **Se mantiene:** el vuelo de la ficha al hueco (es la tarea), hecho en su forma corta, y los tiempos (`Motion.Hold`).

## 8. Cómo quedó (3-oct, tarea 26)

- **Banco** (`tools/punta/`): 533 palabras con definición propia en 5 niveles (108 / 130 / 122 / 105 / 68) y 13 categorías; `datos/*.txt` → `banco.py` →
  `Resources/Lexico/punta_banco.json`. El nivel sale de la frecuencia de uso (zipf de SPALEX) y del largo, no de la banda 1-6 (casi todas estas palabras tienen prevalencia > 93 %);
  la banda se guarda igual. Créditos (Tarea 79): SPALEX (CC BY 4.0) y el Hunspell que sirvió de filtro al generar figuran en Ajustes → Licencias y créditos; la frecuencia «zipf» viene en SPALEX de EsPal, cuyos términos de uso no se pudieron confirmar: desde la Tarea 80 se cita por cortesía en Licencias y créditos y la consulta a los autores o al abogado queda pendiente antes de publicar (ver `docs/nombre-marca-y-riesgos.md`). 16 pruebas en `test_banco.py`; muestra para revisar en `docs/previews/punta-muestra.md`.
- **Juego** (`Games/Anagramas/`): `PuntaGameController` (UI y flujo), `PuntaContract` (reglas puras: colores del lucero, ayudas, puntos, DDA, directo de palabras, cuentas),
  `PuntaLayout` (disposición en dp, estirada a cada altura de teléfono), `PuntaBank`, `PuntaSounds`. Tiempos y colores del boceto; fichas de 46 dp (≥ 48 dp de blanco al tocar).
- **Qué cuenta para el DDA común:** sola o con 1-2 ayudas = acierto; con las letras justas = medio (una sí y otra no); mostrada por Nubi = error. Escalera de 5 niveles (`stepUp` 0,4).
  Letras de más: 2, 2, 3, 3, 4 por nivel (mayores, una menos). El puntaje pesa 1 / 0,85 / 0,6 / 0 (no se ve en el juego: las ayudas solo cambian el color del lucero).
- **Las azules vuelven:** la app guarda las palabras que Nubi mostró (`punta_words`, respaldado, máx. 20) y las manda en `punta_pending` (nuevo campo opcional de `SequenceConfigDetails`);
  Precisión trae hasta 2 (nunca la primera), el Reto una cada 4 palabras. Salen de la lista al encontrarse solas o con 1-2 ayudas.
- **Telemetría** (campos nuevos de `StroopSessionMetrics`): `punta_solo/pista/letras/vista`, `punta_ms` (tiempo medio hasta «¡La tengo!» de las solas), `punta_words`, `punta_blue`, `punta_cleared`.
- **Tutorial guiado («Nubi entrenadora», 4-oct, tarea 30; antes «aprender haciendo»):** cuatro focos de `NubiCoach`, cada uno congela el juego y el toque en el hueco es el de verdad; la definición
  aparece entera. Palabra 1: (1) la tarjeta («Lee la definición y piensa la palabra»), (2) «¡La tengo!», (3) la primera ficha («Arma la palabra tocando las letras en orden»; el resto se arma a tu
  ritmo) y un aviso con el color del lucero. Palabra 2: (4) «Una ayuda» una sola vez y se sigue normal. Cierra con «¡Listo! Ahora va en serio». «Cómo se juega» en la pausa.
  **Disposición de la ronda (9-oct, tarea 60):** las dos palabras son de las más cortas (nivel 1, 4 letras; los mayores, hasta 5) para que el banco sea de UNA fila (6 fichas como mucho,
  `PuntaLayout.GuidedMaxTiles`); con 7 el banco pasaba a dos filas y, en la pantalla más corta (1080x1920), el hueco de una ficha de la fila de abajo pisaba «Saltar tutorial». Además la ronda deja
  `PuntaLayout.GuidedReserve` (64 dp) de aire entre el banco y «Ayudas», y «Saltar tutorial» va en el centro de esa franja (`GuidedSkipFromBottom`) en vez de a un 19 % de la altura.
- **Movimiento reducido:** texto entero, sin ondas, nebulosa, flotación, temblor ni chispas; el lucero aparece directo en el cielo; el vuelo de la ficha a su casilla se queda (más corto).
- **Se borró:** `BubbleField` (las burbujas), los sprites del hueco y los iconos de botones, `AnagramContract` (banco viejo de ~95 palabras) y el mundo `SkyLetters` de `WorldBackdrop`
  (letras flotando); solo los usaba Anagramas. `docs/previews/anagramas-burbujas.png` y su script también.
- **Piso para Desafío/Experto:** el juego ahora cuenta como «de rondas» (`Skill.LONG_ROUNDS`: 6 rondas), porque con 8 palabras nunca llegaba a las 12 que pide el resto.


## 9. Plasticina (Tarea 101, 14-oct): fichas de plasticina y losas del kit

Ricardo: todos los juegos en plasticina. Es SOLO arte: las palabras, las definiciones, las ayudas, el DDA, los tiempos y las medidas no cambian. Técnica y pesos: [`tools/juegos/README.md`](../tools/juegos/README.md); seguimiento de los 17 juegos: [`plasticina-juegos.md`](plasticina-juegos.md). **Fecha del cambio de arte: 14-oct-2026** (marca `estilo 14-oct · plasticina punta e intrusa`; sin `art_version`: si hace falta separar series, se hace por fecha).

| De plasticina (atlas `Resources/Plasticina/punta`, `tools/juegos/punta.py`) | Se queda procedural |
|---|---|
| la **ficha-letra**: un disco de plasticina GRIS CLARO con cúpula que el código tiñe (crema, fija por ayuda, y los cuatro metales de las encendidas) como antes; trae su sombra de contacto, así que sobran la sombra dura, el aro de tinta y el brillo de siempre | los resplandores de las fichas encendidas, la estela del lucero que vuela y las ondas de la antena de la tarjeta (luz) |
| la **casilla** (el hueco de cada letra): un cuenco oscuro con un aro celeste tenue; su aparición gradual sigue siendo el alfa de la imagen | las partículas, la nebulosa, el cielo y los puntos de la escalera de ayudas |
| la **perla del lucero del cielo** (una esfera gris clara que el código tiñe de oro, plata, cobre o azul según lo bien que salió la palabra) | los textos (la letra de cada ficha sigue siendo `Text`) |
| la **tarjeta de la definición**, los **botones** (el principal, cielo; el secundario, «noche») y el **panel del final**: losas del kit | |

El cuerpo de cada pieza mide lo de siempre: la imagen se escala con 0,8 (ficha), 0,833 (casilla) y 0,94 (perla) del cuerpo dentro de su lado. Sin el atlas, todo cae al arte de siempre (`AuditVisibility` revisa los dos casos).

**Lo que se midió** (`tools/juegos/mide_punta.py`): el cuerpo mide 0,79 del lado en la ficha, 0,83 en la casilla y 0,91 en la perla; la letra de tinta rinde 14,3 (normal), 12,1 (fija), 10,6 (oro), 11,8 (plata), 7,0 (cobre) y 6,8:1 (azul) sobre la ficha ya teñida; **los cuatro metales de los luceros se siguen distinguiendo** con la perla teñida (el color ES el dato): el par más cercano (oro y cobre) rinde ΔE 29,3 contra 33,2 de diseño, plata y azul 38,0 contra 42,0; las perlas contra el cielo, de 6,3 a 10,6:1. Pesa 0,06 MB. Pruebas: `PuntaPlasticTests`.

### Estado presionado (Tarea 104, 14-oct)

Regla del 14-oct: todo objeto de plasticina que se toca tiene su estado presionado, con textura (la pieza aplastada, un punto más oscura y con la huella de un dedo; mismo lienzo y escala; sin «saltar»). Detalle, medidas y cómo funciona la ayuda común `PlasticTouch`: [`plasticina-juegos.md`](plasticina-juegos.md) § «Estado presionado».
- **En la punta de la lengua:** la ficha que se toca se ve presionada (`ficha_p`, gris claro como la normal: el código la tiñe igual; la letra de tinta rinde 5,8 a 11,9:1 sobre ella) un instante, y los dos botones (cielo y «noche») pasan a su losa presionada con `PlasticTouch`.

## Cambio de diseño del 18-oct-2026 (Tarea 129): el kit en la tarjeta y los botones

La tarjeta de la definición es ahora una lámina **crema** (`gr`): la definición en tinta 20 dp (la misma fuente), el rótulo «Nubi capta una definición» en #514B78 a 16 dp y el punto y las ondas de la antena en tinta al 50 %. «Una ayuda», «Borrar» y «Ver la palabra» pasan a **lila** con tinta (letra mínima 16 dp); «¡La tengo!» sigue en cielo. «Ayudas · · ·» va en crema a 16 dp sobre el cielo. Se borró el panel de resultado que se armaba oculto.

## «Juice» del 18-oct-2026 (Tarea 130): la tarjeta participa

Con «¡La tengo!» la tarjeta de la definición se hace a un lado (escala 1 → 0,94 y sube 10 dp en 0,30 s, `EaseOutCubic`, sobre su propio centro) y las fichas nacen desde su borde de abajo. Con «Una ayuda» que revela la primera letra, esa ficha viaja DESDE la tarjeta hasta su casilla (0,40 s). Al completar, la palabra entera late una vez (1 → 1,08 → 1 en 0,6 s) dentro del `tail` que ya existía (`n · 0,09 + 0,5 s`): la duración total no cambia.
Como la tarjeta baja a 0,94, su letra mínima es de 17 dp (queda en 16 con la tarjeta a un lado). Sin sonido nuevo (`SoundRulesTests` sigue verde) y ninguna medida ni tiempo de ensayo cambia. Con «quitar animaciones» cada cosa va a su estado final de una vez, con la misma duración.
