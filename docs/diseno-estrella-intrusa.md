# La estrella intrusa — diseño (Atlas celeste, 2-oct)

Juego estrella de **Lenguaje** (id `intrusa`), el quinto del área (con él Lenguaje llega a 5). Aprobado por Ricardo el
1-oct con vía libre para sumar mejoras; presentación **Atlas celeste** elegida el 2-oct. Complementa a los otros: Meteoros = reconocer palabras, Disparate = comprender
frases rápido, Cosecha = producir palabras; este = **organizar significados y resistir asociaciones engañosas**.

## 1. La idea en una línea (Atlas celeste, 2-oct)

Cinco estrellas-palabra sobre un cielo quieto: **una no pertenece. Tócala**; cae como estrella fugaz y una chispa traza la
**figura de la regla** que unía a las otras cuatro («La Manzana», «El Pez», «Las Tijeras»…); la figura queda grabada en tu
**atlas**. La constelación ES la regla: cada regla del banco tiene su propia figura emblemática, reconocible por una persona de
70 años. **Antes de responder no hay ninguna línea** (solo las 5 estrellas con su palabra y unas estrellas menores muy tenues que
son parte de la figura): así el dibujo no regala la respuesta. La maqueta de líneas todas-con-todas del 1-oct se descartó: formaba
una estrella de cinco puntas y las formas de «Tu atlas» parecían símbolos. Elegido por Ricardo: el MODELO 1 «Atlas celeste» de
`docs/previews/intrusa-boceto-modelos.html` (sección `data-model="atlas"`).

## 2. Ciencia (PubMed, 1-oct)

- **Dos sistemas de significado**: por tipo de cosa (taxonómico: perro–oso) y por lo que va junto (temático:
  perro–correa); revisión sistemática de su disociación conductual y neural: Mirman, Landrigan y Britt, 2017,
  *Psychological Bulletin* 143:499-520, [doi:10.1037/bul0000092](https://doi.org/10.1037/bul0000092). Ambos se activan
  solos al leer una palabra y las personas difieren en cuál pesa más: Mirman y Graziano, 2012, *J Exp Psychol Gen*
  141:601-9, [doi:10.1037/a0026451](https://doi.org/10.1037/a0026451).
- **Control semántico**: las relaciones débiles piden más esfuerzo de control (pupilometría, tiempos y errores): Geller,
  Landrigan y Mirman, 2019, *Journal of Cognition* 2:6, [doi:10.5334/joc.56](https://doi.org/10.5334/joc.56). Es la
  palanca de dificultad: intrusas cada vez más cercanas y trampas de asociación que hay que inhibir.
- **"¿Cuál no va?" como ventana a cómo representamos las cosas**: la tarea de elegir el diferente entre varios objetos
  revela dimensiones estables y medibles del significado (Hebart et al., 2020, *Nature Human Behaviour* 4:1173-85,
  [doi:10.1038/s41562-020-00951-3](https://doi.org/10.1038/s41562-020-00951-3)).
- **Fortaleza de los mayores**: el vocabulario y el conocimiento semántico mejoran con la edad (metaanálisis de 324
  comparaciones, ventaja de 0,8 DE para mayores): Verhaeghen, 2003, *Psychology and Aging* 18:332-9,
  [doi:10.1037/0882-7974.18.2.332](https://doi.org/10.1037/0882-7974.18.2.332). Es el juego donde los mayores
  brillan: bueno para la confianza y la adherencia. Se dice sin prometer nada de salud.
- No se copian ítems de pruebas (Semejanzas del WAIS u otras): contenido propio generado y revisado.
- Patentes: "busca el que no pertenece" es un formato de dominio público de décadas; sin hallazgos. Lista del abogado.

## 3. Cómo se juega

- **Escena**: `GameWorld.CieloProfundo` = fondo C (#02030F → #050823 → #0A0F33) con ~230 estrellas fijas (~28 titilan), una banda de
  Vía Láctea inclinada −35° (~1500 puntos gaussianos, velo rgba(200,190,255,0,09), una textura hecha una vez) y nebulosas lila y coral.
  Cinco estrellas-palabra: 4 en las «anclas» de la figura y la intrusa en uno de sus 2-3 «huecos». Palabra en placa crema
  (Atkinson Hyperlegible Bold 17 sp; arriba de la estrella si esta está en el 35% superior de la caja, si no abajo). Toque ≥ 64 dp,
  incluida la placa. Las estrellas menores de la figura se dibujan desde el inicio como estrellas tenues (radio ~1,25 dp, alfa 0,5).
- **Anti-pista**: en cada ronda la figura se refleja al azar y se gira ±12°, la intrusa ocupa uno de los huecos al azar y las 4
  palabras se reparten al azar en las anclas (se busca una combinación sin placas encimadas: ver sección 7).
- **Tocar la intrusa** (cuenta al PRESIONAR; vibración leve): cae como estrella fugaz con estela (900 ms). Desde el nivel 3 sigue
  «¿Qué las une?» (3 opciones, 4 s; mayores 6 s) con las 4 estrellas latiendo suave; ignorarlo no castiga. En los niveles 1-2 no hay
  bonus y la chispa sale ya (mientras cae). Después, la **chispa** traza la figura: ~430 dp/s sobre una pantalla de 360 de ancho
  (trazado completo entre 1,2 y 2,2 s), recorre las aristas en el orden del archivo (si la siguiente no toca donde quedó,
  reaparece en su origen); cabeza de halo dorado de 44 dp y núcleo #FFF8E6 de 2,8 dp de radio, 3 partículas por cuadro; la línea se
  pinta en 3 pasadas (9 / 4 / 1,5 dp, alfa 0,10 / 0,25 / 0,95) y, al pasar, brilla de 0,4 a 1 en 380 ms. En cada estrella: destello
  (anillo de 5 a 27 dp en 650 ms) y una nota de la pentatónica ascendente de `GameFeel` (más fuerte en las estrellas con palabra);
  al cerrar, un acorde de cuatro notas y una vibración firme.
- **El grabado**: contorno dorado #E9C77B (alfa 0,55, 1,2 dp) que se dibuja de 0 a 1500 ms (curva cúbica); de 700 a 1600 ms, sombreado
  de líneas diagonales cada 6 dp (alfa 0,13) recortado al contorno, más los detalles (ojo, aro, línea). Se rasteriza UNA vez por
  ronda (texturas, no cuadros). El **nombre** va en Fraunces SemiBold Italic de 32 sp (#FFE3A3) con fundido desde los 900 ms; la **regla** en
  Atkinson Bold 16 sp (#D9D4F5); si se acertó el bonus: «La nombraste tú · puntos ×2» (#FFC94A). Un toque cierra la ronda.
- **Error**: la estrella tocada con aro coral y una X dibujada (sin emoji), la intrusa con aro sol que late, la explicación («Todos
  viven en el agua; el loro no.») y, si era TRAMPA, una línea punteada entre la intrusa y su pareja con «va con perro, pero no es un
  animal». Con «Seguir» (≥ 64 dp) la intrusa cae y la figura se traza al doble de velocidad, sin bonus ni notas fuertes. La regla
  queda **«por repasar»** y NO suma lámina.
- **Ritmo**: Reto 120 s (una cometa cruza arriba = tiempo de la partida, sin reloj por ronda); Precisión 20 rondas sin tiempo.
  Racha de 5 = «Cielo despejado» (× 1,5; el fondo se aclara un poco).
- **«Quitar animaciones»**: las líneas aparecen con un fundido de 300 ms, sin partículas ni recorrido de caída, y el grabado de una vez.
  Sonido y vibración apagados se respetan (vibración solo al acertar y al completar la figura).

## 4. Dificultad (DDA común, 12 niveles)

| Nivel | Qué cambia | Ejemplo (intrusa en negrita) |
|---|---|---|
| 1-2 | categorías amplias, intrusa lejana | manzana, pera, uva, plátano, **martillo** |
| 3-4 | intrusa de una categoría vecina | perro, gato, vaca, caballo, **águila** |
| 5-6 | la regla es el USO o la FUNCIÓN | cuchillo, tijera, sierra, hacha, **cuchara** |
| 7-8 | material, lugar o parte (dónde vive, de qué está hecho) | delfín, pulpo, tiburón, medusa, **rana** |
| 9-10 | TRAMPA DE ASOCIACIÓN (la intrusa va con uno del grupo) | perro, gato, conejo, caballo, **hueso** |
| 11-12 | regla no obvia + trampa | sartén, olla, tetera, cacerola, **fuego** |

- Mayores: la escalera sube más lento (85% de aciertos, como el DDA común) y empiezan con más tiempo en el bonus.
- **Repaso espaciado (mejora propia)**: las reglas falladas vuelven en partidas de OTRO día con palabras distintas
  ("Constelación por repasar", marcada con un aro lila); si se aciertan, quedan "dominadas" en tu atlas. Así el juego
  enseña, no solo mide. (Se guarda en PlayerPrefs de Unity o en la app: la lista de reglas falladas con su fecha.)

## 5. Contenido: el banco de grupos (español; generado y revisado)

- `tools/intrusa/grupos.py` (determinista) reutiliza la base de conocimiento de `tools/frases/conocimiento.py`
  (categorías, usos, materiales, dónde viven, partes) y una tabla NUEVA de asociaciones temáticas
  `tools/intrusa/asociaciones.py` (~250 pares "va con": perro–hueso, abeja–miel, lluvia–paraguas, mar–arena…),
  revisada a mano.
- Cada grupo: 4 palabras que comparten la regla + 1 intrusa, el tipo de relación (amplia / vecina / uso / material-lugar
  / trampa / regla+trampa), el nombre de la constelación, 3 opciones para "¿Qué las une?" (1 correcta + 2 plausibles
  pero falsas para ESE grupo) y, si es trampa, con qué palabra del grupo "va".
- **Verificador de unicidad (obligatorio)**: para cada grupo, revisar los 5 subconjuntos de 4 con todas las
  propiedades de la base: solo el subconjunto correcto puede compartir una propiedad "nombrable"; si otro subconjunto
  comparte alguna, se descarta el grupo. Además, ninguna palabra puede pertenecer a la regla por un sentido secundario
  (ej. "ratón" animal y de computador): lista de palabras ambiguas excluidas.
- Filtros de siempre (`tools/lexico/excluir.txt`, sin regionalismos, sin temas sensibles). ≥ 150 grupos por tipo.
- Muestra para Ricardo: 60 grupos (10 por tipo) con la intrusa, la regla y las opciones del bonus.

### Estado del banco (1-oct)

`python tools/intrusa/grupos.py [--muestra]` (determinista, ~20 s; 20 pruebas en `tools/intrusa/test_grupos.py`) escribe
`Resources/Lexico/intrusa_es.json`: **960 grupos, 160 por tipo** (1 amplia, 2 vecina, 3 uso, 4 material-lugar-parte, 5 trampa, 6 regla +
trampa), con la base propia `tools/intrusa/lexico.py` (~390 palabras con sus propiedades; no se amplió `tools/frases/conocimiento.py`
para no mover las frases de ¿Verdad o disparate?) y 222 parejas «va con» (`asociaciones.py`).
- **Verificador de unicidad**: por cada grupo y cada palabra x distinta de la intrusa, las otras cuatro (con la intrusa) no pueden
  compartir una propiedad que x no tenga. Las propiedades dudosas (`DUDOSO`) cuentan como «podría tenerla». Se vuelve a comprobar en las
  pruebas con otro código sobre el archivo escrito.
- **Intrusa demostrable**: en los tipos 2-4 la intrusa debe tener una propiedad que pruebe que NO cumple la regla (`COMPLEMENTO`: una
  libélula también es salvaje aunque la base no lo diga). Las opciones falsas del bonus las cumplen a lo sumo 2 de las 4 palabras, de
  cada palabra se sabe si las cumple, y no son «primas» de la correcta (`CLUSTERS`: lavar/limpiar, vuela/va por el aire…).
- **Filtros**: `excluir.txt` (queda fuera huevo, regla, hacha, burro, zorro…; se permiten solo perro, cerdo, cuchillo, camisa, imán,
  canario y caparazón, cotidianas y sin doble sentido aquí), palabras con doble sentido o regionales (ratón, banco, planta, sierra, cola,
  mango, regadera, rastrillo, borrador, banqueta…) y trampas dobles (la intrusa va con una sola palabra del grupo).

## 6. Medidas al final (juego estrella)

Todas salen de UNA sola fuente (aciertos y rondas por tipo de grupo, 6 tipos) y se dicen solo con el mínimo de rondas:
1. **Tu red de significados** (cada dato aparece UNA sola vez; el total de aciertos ya está en la fila «aciertos», así que no hay cifra grande): una barra por categoría (tipo de cosa / para qué sirve /
   dónde está o de qué es / trampas), cada una **solo con ≥ 3 rondas**; la más baja marcada con TEXTO («la más baja»), nunca solo con
   color, y solo si hay dos o más categorías y no todas empatan. Una nota común al pie.
2. **Las trampas** (solo con ≥ 4 trampas): solo «Las trampas te engañaron 3 de 8» (lo resistido ya está en la barra «Trampas»: la misma
   cuenta, sin repetirla) y la lectura: «Es normal: el cerebro une lo que suele ir junto. Truco: antes de tocar, pregúntate qué TIPO de cosa es cada una.»
3. **¿Qué las une?**: «Nombraste 3 de 5» (si se jugó el bonus).
4. **Tu atlas** (SIN recuadro): hasta 3 miniaturas de las láminas ganadas hoy (estrellas, líneas, grabado y nombre) y una línea
   «14 láminas · 2 nuevas hoy · 1 por repasar» (se omiten las partes que valen cero).
Rapidez solo como dato secundario (mediana al tocar en los aciertos), sin perfil de sesgo. Marca para la evolución (`StarMeasures`
`atlas`): % de aciertos de la partida. Nota común al pie: «Medida de esta partida… No es un diagnóstico».
**Atlas** (`data/Atlas.kt`, SharedPreferences `atlas`, con respaldo): láminas ganadas (regla → día), las ganadas con nombre propio y las
reglas por repasar. El **repaso espaciado** sigue: Unity (PlayerPrefs `intrusa_review`) devuelve una regla fallada en una partida de
OTRO día, con un grupo distinto, cada 4 rondas; acertada, queda «dominada».

## 7. Figuras, arte, sonido y efectos

- **Figuras** (`tools/intrusa/figuras.py` + `figuras_a/b/c.py`, determinista): 77 dibujos propios, uno por regla, escritos a mano
  como puntos + trazos (nada copiado de láminas históricas ni constelaciones reales ni del zodíaco). Escribe `intrusa_figuras.json`
  para Unity (listas planas: JsonUtility no lee listas de listas) y `app/src/main/assets/intrusa_figuras.json` (anidado). Hoja de
  revisión con todas: `docs/previews/intrusa-figuras.png`. Cada figura: `{regla, nombre, puntos, aristas (en orden de trazado), anclas
  ×4, huecos ×2-3, contorno, detalles}`. El validador (y 7 pruebas) exige: 7-12 puntos, grafo conexo, anclas separadas ≥ 0,25,
  ningún hueco a < 0,15 de línea o estrella, **sin polígonos casi regulares** (ciclos de 5-6 lados con lados y ángulos ±15%), sin
  estrellas de 4, 5 o 6 puntas, sin cruces/aspas y, desde el anexo del 2-oct, **nada que se lea como símbolo**: ningún nodo con 4 aristas o más (ni
  abanicos de 5+ rayos, ni el semicírculo con rayos), ninguna rueda (contorno circular con líneas que cruzan el centro), ninguna simetría de giro de
  orden ≥ 3 y ningún triángulo con una línea central. Las figuras se leen por su silueta de perfil. Rediseñadas el 2-oct (marcadas con * en la hoja):
  El Búho, La Mariposa, El Rábano, La Rosa (antes Margarita), La Pata con Garras (antes La Garra), La Paloma, Las Tijeras (abiertas ~25°, dos
  pivotes), La Nariz (antes Los Pulmones), El Balde (antes El Grifo), El Calcetín, El Lobo, La Mano y otras 18 con nodos repartidos en cadenas.
  Y las etiquetas: la palabra más larga del
  banco («rompecabezas», 116 dp a 17 sp) siempre cabe en pantalla y para CADA grupo real del banco existen ≥ 6 de las combinaciones
  reflejo × giro × hueco con algún reparto sin choques (el juego busca una al armar la ronda).
- Estrellas de luz con núcleo crema y halo; placas crema; la chispa; el grabado; el nombre en Fraunces (Fraunces SemiBold Italic, SIL OFL, en Licencias; archivo `Fonts/Fraunces-Italic.ttf` + `Fraunces-OFL.txt`, bajado del repositorio oficial de los autores; si falta, cae a Fredoka SemiBold).
- Sonido en la pentatónica de `GameFeel`: nota por estrella, acorde al cerrar, estrella fugaz = soplo brillante, error = cristal suave
  apagado, bonus = campanas, latido de las 4 estrellas. Nada arcade.
- Guía UX (skill ui-ux-pro-max): toque ≥ 64 dp, texto ≥ 14 sp (placas 17 sp), contraste ≥ 4,5:1, nada solo por color (la X, el
  «la más baja»), sin emojis, vibración solo en confirmaciones.

## 8. Estado (2-oct)

Programado: banco (960 grupos, 77 reglas), 77 figuras y su hoja, juego en Unity (`Games/Intrusa/`: Contract, Layout, Bank, Director/Tally,
Sprites, Sounds, Controller; 26 pruebas puras + 5 del banco real), telemetría `intr_*`, lectura en la app (`Atlas.kt`, `FigureBank.kt`,
`GameResultScreen` con «Tu atlas», `AtlasThumb`), prefs `atlas` con respaldo. Captura real del final: `docs/previews/intrusa-final-real.png`.
Pendiente: que Ricardo lo pruebe en el teléfono (ritmo de cada ronda, cuántas láminas reconoce a la primera, las figuras dudosas de la
hoja: El Calcetín, Los Pulmones, La Garra, La Margarita, El Baúl), el archivo de la tipografía Fraunces (ya incluido, tarea 16).

---

## Ficha técnica (movida desde CLAUDE.md, 2-oct)

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**La estrella intrusa** (`Games/Intrusa/`, id `intrusa`, dominio lenguaje; 2-oct, presentación **Atlas celeste** elegida por Ricardo, diseño en
`docs/diseno-estrella-intrusa.md`, boceto `docs/previews/intrusa-boceto-modelos.html`, hoja de figuras `docs/previews/intrusa-figuras.png`): organizar
significados y resistir asociaciones engañosas (Mirman, Landrigan y Britt, 2017; Geller et al., 2019). Cinco estrellas-palabra sobre un cielo quieto
(`GameWorld.CieloProfundo`: fondo C, ~230 estrellas fijas, Vía Láctea a −35°): una no pertenece. NO hay líneas antes de responder. Al tocar la
intrusa cae como estrella fugaz (900 ms); desde el nivel 3, «¿Qué las une?» (3 opciones, 4 s; mayores 6 s); después una chispa (~430 dp/s,
1,2-2,2 s en total) traza la FIGURA de la regla (cada una de las 77 reglas tiene la suya: «La Manzana», «Las Tijeras»…) con una nota de la
pentatónica por estrella y un acorde al cerrar, y se graba (contorno dorado, sombreado, nombre en Fraunces Italic, regla). Error: aro coral + X en la
tocada, aro sol en la intrusa, explicación, línea punteada si era trampa y «Seguir» (la figura se traza al doble, sin bonus; la regla queda «por
repasar» y no suma lámina). Cada ronda la figura se refleja y gira ±12° y la intrusa ocupa uno de sus 2-3 huecos.
- Reglas y pruebas: `IntrusaContract` (niveles, puntaje, medidas, repaso espaciado y atlas en PlayerPrefs `intrusa_recent / intrusa_review /
  intrusa_plates`) / `IntrusaLayout` (geometría lógica de 360 dp, la MISMA que valida `tools/intrusa/figuras.py`; busca una combinación sin placas
  encimadas; recorrido de la chispa `IntrusaSpark`) / `IntrusaDirector` + `IntrusaTally` / `IntrusaBank` (JsonUtility: la copia de Unity de las
  figuras va en listas planas) con `IntrusaContractTests` (26, NUnit puro) e `IntrusaBankTests` (el archivo real). 12 niveles: tipo de grupo por nivel
  (1-2 amplia, 3-4 vecina, 5-6 uso, 7-8 material/lugar/parte, 9-10 trampa, 11-12 regla + trampa). DDA común `stepUp` 0.4 sin tiempo de reacción. Reto 120 s
  (una cometa cruza arriba); Precisión 20 rondas. Racha de 5 = «Cielo despejado» (× 1,5). Puntos 100 + 10 por nivel; «¿Qué las une?» × 2.
- Banco (`Resources/Lexico/intrusa_es.json`, `python tools/intrusa/grupos.py [--muestra]`, determinista): 77 reglas, 960 grupos (160 por tipo),
  verificador de unicidad; muestra `docs/intrusa-muestra.md`; 20 pruebas en `tools/intrusa/test_grupos.py`. Figuras (`python tools/intrusa/figuras.py
  [--hoja]`): `figuras_a/b/c.py` con los dibujos propios (se leen por su SILUETA; el validador prohíbe: nodos con 4 aristas o más y abanicos de 5+ rayos, ruedas
  (contorno circular con líneas por el centro), simetría de giro de orden ≥ 3, triángulo con línea central, estrellas de 4, 5 o 6 puntas, polígonos
  casi regulares, cruces/aspas — las tijeras se abren ~25° con dos pivotes —, letras y signos; `figuras_d.py` trae los rediseños del 2-oct) y 12 pruebas en `test_figuras.py`; escribe `Resources/Lexico/intrusa_figuras.json` (Unity, plano) y
  `app/src/main/assets/intrusa_figuras.json` (app).
- Medidas (telemetría `intr_*` → `GamePlayResult.intr*`; lectura pura en `data/Atlas.kt` con pruebas): **tu red de significados** (una barra por
  categoría con ≥ 3 rondas, la más baja marcada con texto; sin cifra grande, cada dato una sola vez), **las trampas** (con ≥ 4: solo «te engañaron 3 de 8»;
  lo resistido ya está en la barra, la misma cuenta), rapidez y nota al pie después de «tu atlas», **¿qué las une?** y **tu atlas** SIN recuadro (hasta 3 miniaturas de láminas ganadas hoy, `ui/components/AtlasThumb.kt`, y
  «14 láminas · 2 nuevas hoy · 1 por repasar»). Marca (`StarMeasures` `atlas`): % de aciertos. El atlas (SharedPreferences `atlas`, con respaldo:
  láminas con su día, ganadas con nombre propio, por repasar) lo llena `NeuroVidaRepository.recordAtlas`. Captura real: `docs/previews/intrusa-final-real.png`.
- Tipografía: Fraunces SemiBold Italic (SIL OFL) SOLO para el nombre de la figura (`UiFonts.Name`; cae a Fredoka SemiBold si falta
  `Resources/Fonts/Fraunces-Italic.ttf`); ya está en Ajustes → Licencias (archivo y licencia en `Resources/Fonts/`, del repositorio oficial undercasetype/Fraunces).
- Pendiente: que Ricardo lo pruebe en el teléfono (ritmo de cada ronda, que se reconozcan las figuras; dudosas en la hoja: El Calcetín, La Nariz,
  El Baúl).
