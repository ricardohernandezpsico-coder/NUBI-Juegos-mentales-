# La estrella intrusa — diseño (1-oct, para aprobar con maqueta)

Juego estrella de **Lenguaje** (id `intrusa`), el quinto del área (con él Lenguaje llega a 5). Aprobado por Ricardo el
1-oct con vía libre para sumar mejoras. Complementa a los otros: Meteoros = reconocer palabras, Disparate = comprender
frases rápido, Cosecha = producir palabras; este = **organizar significados y resistir asociaciones engañosas**.

## 1. La idea en una línea

Cinco estrellas-palabra unidas por líneas tenues: **una no pertenece. Tócala**; se suelta como estrella fugaz y las
otras cuatro se unen en una constelación con nombre ("Frutas", "Sirven para cortar") que se guarda en **Tu cielo**.

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

- **Escena**: cielo fondo C con una nebulosa suave (`GameWorld.Observatorio` no: nuevo `GameWorld.CieloProfundo`);
  5 estrellas-palabra en una disposición de constelación (pentágono irregular, distinta en cada ronda), palabra en placa
  crema bajo cada estrella (Atkinson Hyperlegible Bold 24 sp, 28 mayores). Zona de toque = estrella + placa, ≥ 64 dp,
  separación ≥ 12 dp. Líneas tenues entre todas, titilando muy suave (quietas con "quitar animaciones").
- **Tocar la intrusa** (cuenta al PRESIONAR): la estrella se suelta como estrella fugaz con estela; las 4 restantes se
  unen con líneas de luz que se dibujan una a una (~400 ms) y aparece el nombre del grupo. Campana de la pentatónica,
  vibración corta.
- **Error**: la estrella tocada titila coral con ✗; la verdadera intrusa se ilumina con aro sol y **la explicación
  enseña la regla**: "Todas son animales; el hueso no." Si era una TRAMPA DE ASOCIACIÓN, además se dibuja una línea
  punteada entre la trampa y su pareja con el rótulo "va con perro, pero no es un animal" (enseña la diferencia entre
  "es del mismo tipo" y "suele ir junto"). Sin culpa, se lee con calma (2,5 s; 3,5 mayores; toque para seguir).
- **Bonus "¿Qué las une?"** (opcional, desde el nivel 3): tras acertar, 3 opciones cortas ("Animales" / "Viven en la
  casa" / "Tienen cuatro patas") durante 4 s; acertar = puntos × 2 y la constelación se guarda "con nombre propio"
  (brilla más en Tu cielo). Ignorarlo no castiga. Mayores: 6 s.
- **Ritmo**: Reto 120 s (cometa que cruza arriba = tiempo de la partida, sin reloj por ronda); Precisión 20 rondas sin
  tiempo. Racha de 5 = "Cielo despejado" (× 1,5, el fondo se aclara un poco).

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
  ("Constelación por repasar", marcada con un aro lila); si se aciertan, quedan "dominadas" en Tu cielo. Así el juego
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

1. **Tu red de significados** (cifra grande: aciertos de N) + barras por tipo de relación (tipo de cosa / uso /
   material-lugar / trampas), cada una solo con ≥ 3 rondas; la más baja marcada con TEXTO.
2. **Las trampas**: "Las trampas te engañaron 3 de 8" + lectura: "Es normal: el cerebro une lo que suele ir junto.
   Truco: antes de tocar, pregúntate qué TIPO de cosa es cada una." Solo con ≥ 4 trampas vistas.
3. **¿Qué las une?**: cuántos nombraste bien (si se jugó el bonus).
4. **Tu cielo**: constelaciones nuevas en esta partida y total; "por repasar" que quedaron dominadas.
Rapidez solo como dato secundario (mediana del tiempo en aciertos), sin perfil de sesgo. Nota común al pie.

## 7. Arte, sonido y efectos

- Estrellas de luz con núcleo crema y halo del color del área (Lenguaje), placas crema, líneas de luz que se dibujan;
  estrella fugaz con estela; constelación terminada con destello y el nombre en Fredoka. **Tu cielo** (en la pantalla
  final y como colección): mapa del cielo oscuro donde cada constelación ganada es un dibujo de estrellas propio
  (forma generada por la categoría, siempre la misma para esa categoría), agrupadas por tipo de relación.
- Sonido en la pentatónica de `GameFeel`: cada línea que se dibuja suena una nota (la constelación suena como
  acorde), estrella fugaz = soplo brillante, error = cristal suave apagado, bonus = campanas. Nada arcade.
- Guía UX (skill ui-ux-pro-max, 1-oct): toque ≥ 48 dp (acá 64), separación ≥ 8 dp; vibración solo en confirmaciones
  (acierto, constelación), no en cada toque; animación continua solo sutil (titileo), nunca decorativa invasiva; el error
  siempre explica y deja seguir; respetar "quitar animaciones", sonido y vibración apagados.

## 8. Pendiente antes de programar

1. Aprobar la maqueta `docs/previews/intrusa.png` (hecha el 1-oct).
2. Aprobar la muestra de 60 grupos `docs/intrusa-muestra.md` (hecha el 1-oct).
