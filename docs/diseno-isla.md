# Diseño «La isla»: dirección visual elegida para Nubi

**Fecha:** 10-oct-2026. **Autor:** Fable 5.1 (solo lectura sobre el repo). **Para:** Opus → Sonnet (este documento vive en el repo como `docs/diseno-isla.md`; Tareas 82 y 83 lo cumplen en parte, ver «Valores finales»).
**Decisión de Ricardo (10-oct):** «me gustó la de diorama, engancha más y le da el toque premium. además sus colores suaves no molestan a los adultos mayores, y los colores de tinte pastel pero con esa textura agrada».

**Revisión del 10-oct (noche), método 3D:** la isla, los cuatro edificios y Nubi ya no se dibujan como vectores: se **renderizan una vez en Blender** por script (plasticina con luz real) y entran a la app como capas WebP; encima, en Compose, se dibujan en vectores solo lo que cambia (ruta del día, lámparas encendidas, sombra de Nubi, halos). Ricardo aprobó esta dirección al ver las rondas 9 a 11; Opus coincide (92 %). Las secciones 6, 7, 11, 13, 14, 15 y 16 describen ese método; el resto del documento (tokens, pantallas, movimiento, nombres, semántica) sigue vigente. **Página de comparación con la referencia (rondas 7 a 11):** https://claude.ai/artifact/4RjVBsAKAb1b8aJpfBDynC

**Boceto interactivo (Hoy, Juegos y Avance navegables; tokens, símbolos a 24/32/48, Nubi, barra, movimiento; la isla ahí es la versión vectorial previa):** https://claude.ai/artifact/2eJdfUQpb6rezBmmC8exXK
**Comparativa de las cinco direcciones (contexto):** https://claude.ai/artifact/2LLgwcr6B6Uhf5dKz3DXY8

> Lo que se conserva porque fue lo que enganchó: la isla con los cuatro lugares y la ruta del día entre ellos; Nubi flotando encima; tintes pastel suaves con textura; la sensación premium. Las imágenes generadas con IA del boceto son referencias de acabado, no arte: todo lo que entra a la app sale de código (Compose para la interfaz, un script de Blender para la isla).

---

## Valores finales (ronda 26e, 11-oct-2026) y lo que cambió después

Estos valores mandan sobre el resto del documento donde difieran.

- **La isla de plasticina:** plataforma como cojín sobre base firme; textura de manos sin facetas; colores con brillo de cera y jaspeado; farolas a ~2700 K con halos; ruta de lentejitas amarillo sol; Nubi de silueta calma (ancho/alto 1,51), iris ciruela mate, boca corta en arco, manchas como gotas blandas, un 18 % más grande, con su sombra sobre la losa. Cara medida: ojos 65 %, boca 76 % (la referencia daba 64/76).
- **En la app (Tarea 82, fase 1):** `isla_base.webp`, `isla_edificios.webp` e `isla_nubi_hola.webp` (el recorte común mide 930 × 1042 px) más `res/raw/isla.json` (coordenadas 0-1 sobre ese recorte: edificios, farolas, `sombra_elipse` 1,4 × 0,5 al 50 %, `halo_antena`, `halos_farolas`, `capa_nubi`, `mirada`). **Van en `drawable-nodpi/` a 2×, sin copia 1×** (esto reemplaza lo dicho en §13.2): en una carpeta de densidad Android las reescala al decodificar (un teléfono de 420 dpi las achicaba a 0,66 y se veían borrosas). `nubi_hola` se llama `isla_nubi_hola` porque ya existe uno en la app. Las miradas (`nubi_mira_*`), la barra y el movimiento son fases siguientes.
- **Hoy a 393 × 873 dp:** saludo a 22 sp (el boceto decía 26: «Buenas tardes, Ricardo» no cabe en una línea junto a los botones de perfil y opciones), liga y racha a la izquierda, isla de ~240 dp (la regla que manda: las tres paradas y el botón se ven sin deslizar; mínimo 150 dp), globo, tres paradas y un solo botón.
- **Fondo (lámina B, azul marino `#23255A`, 720 × 1200 px):** contraste del saludo sobre él: crema 8,3:1, sol 6,7:1.
- **Barra de pestañas (fase posterior):** isométrica con cubos blandos (elegido caramelo con resplandor) e íconos reconocibles. Medidas: bandeja + cubos + íconos 86-90 dp; con etiquetas (14 sp, negrita la elegida) 113-117 dp.
- **Los luceros, en las tres pestañas (Tarea 87, decisión de Ricardo del 13-oct; revierte la de la Tarea 83):** Ricardo encontró Hoy «muy vacío». El fondo sigue siendo DOS capas, `fondo_lamina` y `fondo_luceros` (las bolitas con su sombra de contacto, ahora ~80 en vez de 17 visibles), pero las dos van FIJAS detrás de Hoy, Juegos y Avance, siempre con opacidad 1: se quitó el fundido por pestaña (`lucerosAlpha`). Solo hay una zona libre de luceros, arriba (`ZONAS_HOY` en `tools/isla/isla.py`: x 0-100 %, y 3-28 %): bajo el saludo de Hoy y los títulos de Juegos y Avance, que caen siempre en el mismo lugar; bajo botones, piedritas y tarjetas no se excluye nada (son opacas y los tapan), y la banda de la barra de pestañas (y 88-100 %) sigue sin luceros por sus etiquetas. Como en Juegos y Avance el contenido se desliza, un lucero puede quedar detrás de un texto: todo texto claro que va directo sobre el fondo usa `OnBoardText` (`ui/components/OnBoardText.kt`), que le pinta detrás un contorno navy de 3,5 dp con el mismo trazado de las letras. Una sombra desenfocada NO alcanzaba (medido con la fuente real contra una bolita crema: sombra navy de 7 dp casi opaca = 1,0-1,9:1; contorno de 3,5 dp = 4,9-11,5:1, porque la bolita es tan clara como el texto y solo una orilla densa separa la letra). El color del contorno es el de la lámina en su zona más común (mediana de `fondo_lamina`: #333369): sobre una zona oscura o clara de la nebulosa, sin lucero detrás, no se distingue del texto sin contorno. Medida real en el emulador a 393 y 360 dp (`unity/test-results/t87_matrix.py`: contraste de cada letra contra el anillo de 1-3 px a su alrededor que cae sobre un lucero, peor 5 %): ≥ 4,5:1 en Hoy, Juegos (grilla y ventana de un área, arriba y abajo) y Avance (arriba, medio y abajo, y el detalle de un área); la excepción es el nombre de la liga («Bronce», 34 sp, color de la liga) a 3,4-3,7:1, que por su tamaño es texto grande (3:1). Costo de dibujar el contorno (`gfxinfo` en el emulador con GPU por software): desplazar Avance +8 a +15 ms por cuadro (de ~79 a ~87-94 ms), Juegos +4 ms.
- **Piedritas y enlace de Hoy (Tarea 87, pedido de Ricardo):** nunca se parte una palabra a la mitad en las piedritas de «Tu ruta de hoy» («Constelacione / s» a 411 dp). `StoneFit` mide la palabra más larga de los tres nombres y elige la primera medida en la que cabe: nombre de 15 sp con relleno de 9 dp; 15 sp con 6 dp; 14 sp con 6 dp (el mínimo). Si ni así cabe (a 360 dp con «Constelaciones» o «Acoplamiento»; con letra grande siempre), las paradas pasan a filas, una por juego, solo cuando hace falta. `HoyStonesWordsTest` prueba los nombres de todos los juegos, de a tres, a 360, 393 y 411 dp. El enlace del punto de partida pasó a «Encuentra tu punto de partida · 5 min», en una sola línea (con letra grande pasa a dos): con él la isla a 393 × 873 dp mide 331 dp (antes 307; sin el enlace, 371).
- **Sin igualar todavía a la referencia:** un leve degradado en el iris por el rebote de la losa; halos de farolas más discretos que en la referencia; sin marcas de alisado ni costuras entre piezas.

---

## Pedidos de Ricardo para las próximas fases (11-oct)

Ricardo probó la fase 1 en su teléfono: Hoy se ve como se le describió y los edificios responden. Pidió esto para lo que sigue. **Es solo un registro: nada de esto está implementado.** Cada punto necesita su diseño y su fase.

1. **Barra nueva (fase 5):** bandeja crema y cubos con íconos, como la versión aprobada (ver «Barra de pestañas» en los valores finales).
2. **La isla, protagonista de Hoy.** Más grande que los ~240 dp actuales. Debajo, algo informativo o de las partidas de hoy, compacto. Se rediseña cómo conviven la isla grande y la ruta del día (hoy: globo, tres paradas y un botón, con la isla en lo que sobra).
3. **Nubi dinámica (fase 3):** mira hacia el edificio que se toca.
4. **Tocar un edificio ya NO lleva a Juegos ni al detalle de área de hoy.** Abre una **lámina** de plasticina con el avance de esa área y, ahí mismo, la opción de jugar en esa área. **Esto reemplaza** lo dicho en §4.1 (punto 3, «Tocar un edificio abre Juegos con ese lugar elegido») y en §6 («Los edificios se tocan»): ambos textos se conservan, marcados.
5. **Al abrirse la lámina, Nubi se desplaza a otra zona,** como presentando la información. Expresión neutra y amable, nunca según el desempeño.

---

## 1 · Tokens

### 1.1 Color (texto siempre en tinta sobre pastel o crema; crema o sol sobre noche)

| Token | Hex | Uso | Contraste medido |
|---|---|---|---|
| `night0` | `#15123A` | fondo abajo | crema sobre night0: 15,0:1 |
| `night1` | `#221E5C` | fondo medio | crema sobre night1: 12,6:1 · sol sobre night1: 10,2:1 · `#D6D1F0` (subtítulos) sobre night1: 10,1:1 |
| `night2` | `#2E2A72` | fondo arriba (halo) | — |
| `ink` | `#2B2451` | texto, bordes, sombra dura | — |
| `cream` | `#F6EBD3` | tarjetas, repisa | tinta sobre crema: 12,0:1 |
| `creamIsla` | `#F1DFBF` | losa de la isla (color del material en el render; en pantalla sale más claro por la luz) | solo superficie, sin texto |
| `creamDk` | `#E6D6B4` | borde inferior de la repisa, fichas apagadas | tinta: 9,9:1 |
| `sand` | `#DDB782` | base de tierra de la isla (material del render) | solo superficie |
| `sandDk` | `#B89A66` | sombra dura de la repisa | — |
| `sun` | `#FFCF5A` | botón principal, lámparas, parada que sigue, pestaña elegida | tinta sobre sol: 9,7:1 |
| `sunDk` | `#B98312` | sombra dura del botón sol | — |
| `lime` | `#BDE68A` | «hecho» (check, día cumplido) | tinta sobre lima: 10,1:1 |
| `rose` | `#F5B8C8` | mejillas de Nubi, bandera | tinta: 8,5:1 |
| `mem` / `memDk` | `#9CC7F5` / `#2F5FAF` | Observatorio · Memoria | tinta sobre mem: 8,1:1 · blanco sobre memDk: 6,2:1 |
| `ate` / `ateDk` | `#F7C58E` / `#A8541A` | Antena · Atención | 9,0:1 · 5,3:1 |
| `raz` / `razDk` | `#C9B8F0` / `#5C47A8` | Hangar · Razonamiento | 7,9:1 · 7,2:1 |
| `len` / `lenDk` | `#A9E3C0` / `#276E4F` | Biblioteca · Lenguaje | 9,8:1 · 6,1:1 |
| texto secundario sobre crema | `#5A4F8A` | «Observatorio · hecho», etapas | 6,1:1 |
| etiqueta de pestaña no elegida | `#6B5F8E` | barra | 4,9:1 |

Reglas: el color de área pinta superficies (techos, fichas, tramos de barra), **nunca texto fino**. Acierto/hecho = lima **más** un check; la parada que sigue = sol **más** halo **más** el texto «es la que sigue». Nada depende solo del color.

### 1.2 Textura («pastel con textura»)
Grano de ruido fractal, 2 octavas, frecuencia alta (en el boceto `baseFrequency .85`), tinta al **22 %** de opacidad, sobre las superficies de arcilla **dibujadas en Compose** (tarjetas, repisa, fichas de lugar, botones). La isla, los edificios y Nubi **ya traen su textura horneada** en el render (huellas de dedos y relieve de plasticina): no se les aplica grano encima. Más un **brillo superior** de 2 dp blanco al 70 % (inset) que da el volumen. Sin grano en el fondo nocturno ni en el texto.

### 1.3 Tipografía (sin cambios de familia)
Fredoka 600/700 para títulos, botones, números y nombres; Nunito 700 para el resto. Tamaños del boceto: saludo 26, fecha y subtítulos 14, rótulos de sección 14 en versalitas con +0,08 em, nombre de parada o juego 16, detalle 14, botón 18, etiqueta de pestaña 14, nombre de lugar 14. **Nada bajo 14 sp**; la guardia existente lo cubre.

### 1.4 Forma, sombra y espacio
Radios: tarjetas 20 dp, fichas de lugar y de juego 16 dp, repisa 26 dp, botón 22 dp, píldoras 999. Sombras: **dura** (desplazamiento 6 dp hacia abajo, tinta al 55 %; el botón sol usa `sunDk`; la repisa usa `sandDk`) + **suave** (desenfoque 14 dp, 45 %; en Compose se dibuja como degradado radial, no como blur). Alturas mínimas: filas 58-64 dp, pestañas 74 dp, botón 58 dp. Margen lateral 16 dp; separaciones 8-12 dp.

---

## 2 · Los cuatro lugares (símbolos de área)

| Área | Lugar | Silueta (lo que se ve a 24 dp) | Detalle (desde 48 dp) | Dónde se usa |
|---|---|---|---|---|
| Memoria | **Observatorio** | cúpula pastel azul sobre base crema | rendija oscura, puerta | ficha de lugar, lista de juegos (30 dp), paradas de Hoy (34), finales de juego (48), isla (edificio grande) |
| Atención | **Antena** | torre de celosía naranja con lámpara sol arriba, **sin plato** (un disco de frente se leía como luna llena) | travesaños y cruces, pies en la losa | ídem |
| Razonamiento | **Hangar** | cubo lavanda de techo claro con un engranaje grande de dientes redondos en la fachada izquierda | portón de lamas y engranaje chico a la derecha, chimenea | ídem |
| Lenguaje | **Biblioteca** | casa crema con franja verde y **techo a dos aguas** (decisión de Ricardo, 10-oct noche: «biblioteca a dos aguas»; la referencia de IA lleva techo plano con alero grueso y no se imita), toldo a rayas sobre la puerta en arco, ventanas cálidas | letrero amarillo con la **Ñ** grande con marco, tejas en relieve, buzón con ranura | ídem |

Regla de dibujo: nada de estrellas con puntas, medias lunas ni cruces (el engranaje tiene dientes redondos; el check es un trazo; la ✗ futura es aspa diagonal).

Implementación (método 3D, ver §13.4): los **símbolos chicos** de área (24 a 48 dp) se sacan de los **mismos edificios renderizados**, cada uno solo y de frente, recortado y exportado a 96 y 192 px (`lugar_observatorio.webp`, etc.). Condición: una captura a 24 dp debe dejar reconocer los cuatro; si a 24 dp no se leen, los tamaños de 24 y 32 dp usan los glifos vectoriales del boceto (`IslaIcons.kt`, dos tonos sobre crema con contorno tinta de 2 dp) y los renders quedan para 40 dp o más. En la isla, los edificios son parte de la capa `isla_edificios` (§6).

---

## 3 · Nubi en arcilla pastel

- Nube de plasticina renderizada (ronda 19): una **galleta gruesa**, no un globo (Ricardo: «la cara de Nubi se ve como inflada»): el cuerpo se aplasta un 40 % de adelante hacia atrás y la forma de nube viene de la silueta; base achatada con el contorno inferior en bultos sutiles, frente casi plano donde se apoyan ojos, sonrisa y mejillas, cinco lóbulos (grande al centro, medianos a los lados con el izquierdo más grande y bajo, chicos en los costados), contorno con irregularidad de mano; lila `#B6ABD9` **mate** (rugosidad alta, poca subsuperficie, sin brillo de pelusa) con huellas de dedos de tamaño medio, **cinco manchas celeste y turquesa (`#8FCCE4`, `#7EC9CF`) incrustadas en la misma plasticina**: máscara suave de borde ondulado que cambia el color, relieve mínimo (0,5 % del ancho) y una leve oclusión en el borde inferior, como plasticina de dos colores amasada (no son piezas pegadas: Ricardo rechazó esa versión por «parches sobresalientes»). Ojos como lentejuela: disco crema-blanco fino apenas sobre el frente (no hundido, con una luz propia leve para que no se sombree gris) e iris café-burdeo centrado que ocupa el 75 % del disco, de 0,085 del ancho, con un reflejo grande arriba a la izquierda y un punto chico; en las miradas el iris se corre dentro del disco y el aro se ve siempre completo. Sonrisa corta y fina, mejillas coral por fuera de los ojos. **Sin contorno** (el volumen lo da la luz del render). Sombra elíptica dibujada en Compose cuando flota sobre la isla (§6). **Sin bata, sin tablilla, sin lentes.**
- Poses, siempre por momento o pantalla, **nunca por desempeño**: `hola` (Hoy, de día), `descansa` (Hoy, de noche: ojos cerrados, misma sonrisa), `explora` (Juegos: aro de casco y linterna frontal), `piensa` (ficha de juego y tutorial: mano al mentón), `celebra` (fin del camino o semana cumplida: brazos arriba). La celebración premia conducta (terminar, volver), no puntaje.
- Tamaños: 72 dp sobre la isla, 88 dp en Juegos, 110 dp en la bienvenida, 70 dp en Avance.
- Implementación: cinco renders de la misma Nubi (`nubi_hola`, `nubi_descansa`, `nubi_explora`, `nubi_piensa`, `nubi_celebra`), recortados al borde, a 1× y 2× (§13.1). `NubiClay(pose, size)` es un `Image` con `painterResource` y `ContentScale.Fit`; reemplaza a `nubi_{hola,mira,celebra,cientifica}.webp` (354 KB en total hoy, 80-96 KB cada uno; las nuevas pesan 10 KB a 2× y 6 KB a 1×). `nubi_recursos.py` deja de generar Nubi (puede seguir generando el ícono adaptativo hasta que se rehaga con la Nubi renderizada).

---

## 4 · Pantallas

### 4.1 Hoy (la isla es la protagonista)
1. **Saludo** con nombre y momento del día («Buenas tardes, Ricardo») y la fecha.
2. **Semana de constancia**: siete farolitos colgados de una cuerda; los días jugados se encienden (sol + brillo), el día de hoy lleva un aro lima; debajo «3 de 4 días de tu meta».
3. **La isla** (≈ 40 % de la altura): la losa crema sobre su base de tierra, los cuatro edificios en rombo (Observatorio a la izquierda, Antena arriba a la izquierda, Biblioteca a la derecha, Hangar abajo), la **ruta de hoy** como puntos sol que unen los tres lugares del camino, **lámparas encendidas** en esos tres lugares, Nubi flotando arriba a la derecha del centro con su sombra en la losa. Tocar un edificio abre Juegos con ese lugar elegido (también por teclado y lector de pantalla: cada edificio es un botón con el nombre del lugar). **[Reemplazado el 11-oct en lo que hace el toque: ahora abre una lámina con el avance de esa área y la opción de jugarla; ver «Pedidos de Ricardo para las próximas fases», punto 4. Lo demás (isla de ≈ 40 %, botones con nombre) sigue.]**
4. **Globo de Nubi**: «Hoy la ruta pasa por el Observatorio, la Antena y la Biblioteca. Seis minutos.» De noche: «Ya pasaste por la isla hoy. Mañana sigo aquí.»
5. **«Hoy en la isla»**: tres paradas en tarjetas de arcilla (número, glifo del lugar, nombre del juego, «Observatorio · hecho»); la que sigue va en sol con «es la que sigue». Tocar una parada abre su ficha.
6. **Un solo botón grande**: «Seguir la ruta · 1 juego» (o «Empezar la ruta · 3 juegos» a primera hora; «Ruta completa» con Nubi celebrando al terminar).
Se van de Hoy: las cuatro barras de área (pasan a Avance y a Juegos) y el «Ver más detalles».

### 4.2 Juegos (llegar a los juegos de cada lugar, simple)
1. Título «Juegos» y «Elige un lugar de la isla».
2. **Cuatro fichas de lugar** en cuadrícula de 2 × 2 (72 dp de alto, toque completo, caben a 360 dp sin cortarse): glifo a 40 dp a la izquierda, nombre del lugar y el área al lado. La elegida **sube 4 dp, enciende una lámpara arriba y lleva aro sol**: tres señales, no solo color. Es la vía principal y es una lista de cuatro botones con nombre, accesible.
3. Nubi `explora` con su globo (sugerencia por días sin jugar ese lugar).
4. Rótulo con el glifo del lugar a 24 dp, «Hangar · Razonamiento · Hábil», y la **lista de juegos** del lugar: filas de 64 dp con el **planeta propio de cada juego** (`MiniPlanet`, el que ya existe) en una ficha pastel del color del lugar, nombre, etapa o récord, y check lima si se jugó hoy. El símbolo del lugar va solo en el rótulo, no en cada fila. Tocar abre la ficha (`GameSheet`, sin cambios).
5. Al pie, «Ver todos los juegos en una lista»: la vía alternativa, una lista plana de los 18 con su lugar, por si la isla no se entiende.
Se va de Juegos: «¿Qué entrenamos hoy?» y «¿Con cuál entrenamos tu memoria?».

### 4.3 Avance (la isla crece por constancia)
1. Título «Tu avance» y la línea «Lo que haces, no cuánto rindes».
2. **Tu constancia**: días seguidos y días de esta semana, con Nubi `celebra` si la semana está cumplida.
3. **«Tu isla crece con los días»**: la isla en pequeño con sus decoraciones y, debajo, las cuatro decoraciones con su hito: **lámparas en la ruta (7 días jugados)**, **banderines (14)**, **jardín (30)**, **muelle y nave (60)**. Las no conseguidas se ven en silueta con «a los 30 días». Los hitos cuentan **días jugados en total**, no seguidos: la racha que se rompe nunca quita una decoración.
4. **Tus lugares**: cuatro filas con glifo, «Observatorio · Hábil» y la barra de cinco tramos (el tramo sol es lo avanzado esta semana).
5. **Lo que descubriste**: las medidas estrella como hoy (promedio de 5 partidas, nota «no es un diagnóstico»).
Nunca: percentiles, comparación con otras personas, puntaje único.

### 4.4 La barra inferior como objeto (revisión por pedido de Ricardo, 10-oct noche)
Ricardo quiere la barra como la de la referencia: «del mismo color crema de la losa de la isla, le da un toque distintivo», con los íconos «con un toque de plasticina, para seguir con la misma marca».
- **La repisa (ronda 24, Ricardo: «cuando tú las presiones, hay un cambio de luz»)**: una **bandeja** de plasticina crema (88 dp) del **mismo color que la isla** (`creamIsla`, §1.1) con **tres casillas que sobresalen**: botones cuadrados redondeados, mullidos, con su sombra sobre la bandeja, cada uno con su ícono. Estado normal: la casilla elevada, mate. Estado elegido: la casilla **se hunde** un poco (4 dp) y **cambia la luz** (un brillo cálido dentro, emisión suave) con la lámpara del ícono encendida. Bandeja y casillas (tres, en dos estados) se renderizan en Blender con la luz de la isla (`--barra`); en Compose cada pestaña es un `Image` que cambia de recurso con un fundido de 150 ms. La versión anterior de este punto (repisa dibujada en Compose con tira de huellas) queda como respaldo si el peso o el rendimiento lo piden: Se dibuja en Compose (se estira a cualquier ancho, cuesta nada): fondo `creamIsla`, el grano de §1.2 al 18 % y, encima, un **mapa de huellas** horneado en Blender como tira repetible (`repisa_huellas.webp`, 512 × 176 px, transparente, tinta al 10 %, `TileMode.Repeated`) para que la textura sea literalmente la de la losa. Alternativa si Ricardo quiere la repisa entera horneada: un 9-patch PNG renderizado (tapas de 40 px y centro estirable), unos 15 KB; se decide viéndolas juntas en la fase 5.
- **Los tres íconos**, objetos chicos de plasticina **renderizados en Blender con el mismo material, la misma luz y la misma cámara de la isla**, cada uno en dos estados:
  - **Hoy** = la isla en miniatura: losa crema sobre base de tierra con la cúpula azul del Observatorio y la torre con su lámpara (es la marca de la app; sin cerebro);
  - **Juegos** = un cohete redondo de plasticina, lavanda con ventana crema y tres aletas (los juegos viven en el espacio: Piloto Estelar, Satélites, Correo Estelar); alternativa si el cohete no gusta: un dado de plasticina con puntos redondos (nunca estrellas con puntas);
  - **Avance** = **el farol de la ruta** (elegido por Ricardo el 10-oct, noche: «icono farol»): la misma lámpara de plasticina que se enciende en la isla cuando se cumple una parada, con pie, poste, vidrio translúcido, varillas, sombrero y argolla. Descartadas tras verlas renderizadas: la cumbre con banderín («no me llama mucho la atención»), el brote en maceta, el globo aerostático y el frasco de luceros.
  - **Estado normal**: el objeto en sus colores, sentado sobre la repisa. **Estado elegido**: el mismo objeto renderizado con su **lámpara encendida** (halo cálido horneado) y, en Compose, además **sube 8 dp** y la etiqueta pasa a negrita con una línea de tinta debajo. Tres señales a la vez; ninguna es solo color.
  - Hoy y Avance llevan lámpara los dos: a 40 dp se distinguen porque la de Hoy está en lo alto de una torre sobre una losa con cúpula y la de Avance está dentro de un farol con sombrero y pie (comprobado en la hoja de barras a 1×). Si en una captura real se confundieran, la lámpara de la isla en miniatura puede apagarse en el estado normal (ya es así) y encenderse solo al elegirla.
- Etiquetas siempre visibles, 14 sp o más. Toque de 74 dp por pestaña. Semántica de §12 (`Role.Tab`, `selected`). Tamaño del ícono: 40 dp (56 px a 1×, 112 px a 2×). Cómo se renderizan: §13.5. Fase: con los símbolos de área, la 5 (§7).
- **Implementada en la Tarea 86 (13-oct)**: `ui/components/NeuroNavBar.kt`. La bandeja es UNA imagen (`barra_bandeja.webp`, 786 × 420 px en `drawable-nodpi`, recortada a las filas 98-283) y cada cubo es una imagen aparte en dos estados (`barra_<hoy|juegos|avance>[_on].webp`) que se funden en 150 ms (`PlaceMotion.CUBE_FADE_MS`; con «quitar animaciones», de golpe); la sombra de cada cubo es un óvalo pintado. Todo sale del compuesto aprobado (cubos a 0,5 de su render, centro de render en y 190) y se escala con el ancho (393 dp = 1,0; entre 0,75 y 1,2). Mide ~92 dp la bandeja y ~115 dp con las etiquetas a 393 dp (la maqueta decía 113-117). Cada pestaña es toda su columna (143, 106 y 143 dp a 393: se parten a medio camino entre cubo y cubo) con `Role.Tab`; la etiqueta (14 sp, negrita la elegida) va centrada bajo SU cubo. Pruebas: `NeuroNavBarTest` (semántica, 56 dp de toque, sobre el inset de 48 dp, letra 1,3 y 2,0, y capturas a 393, 360, 411 y letra 2,0). Costo en Hoy: la isla a 393 × 873 pasó de ~417 a 371 dp sin el enlace del punto de partida (307 con él; a 360 × 780, 224 dp).

---

## 5 · Movimiento con intención (y su versión sin animaciones)

| Qué | Cómo | Con «quitar animaciones» |
|---|---|---|
| La isla respira | sube y baja 3 dp cada 6 s | quieta |
| Nubi flota | 6 dp cada 4,5 s | quieta, con su sombra |
| Lámparas de las paradas de hoy | titilan (opacidad 0,75 → 1) cada 2,6 s | encendidas fijas |
| La ruta avanza | los puntos caminan hacia la parada que sigue (desplazamiento del guion, 2,4 s) | línea fija y **halo** fijo en la parada que sigue |
| Terminar un juego | la lámpara del lugar se enciende y queda fija | igual (es un estado, no una animación) |
| Pestaña elegida | el cubo sube con un rebote corto (250 ms) | salta sin rebote |

La lectura de `ANIMATOR_DURATION_SCALE == 0` ya existe (`ReduceMotion.kt`); todo lo anterior se apaga con ella. Ninguna información depende del movimiento. Con el método 3D nada de esto cambia: lo que se mueve son capas de imagen (`translationY` en `graphicsLayer`) y vectores dibujados encima; los renders no se vuelven a decodificar por cuadro (§14).

---

## 6 · Cómo se construye en código: la isla se renderiza una vez, el resto se dibuja encima

**Luz (ronda 23, por la revisión de Ricardo: «parece una imagen estática… las estructuras parecen un juego en 3D»).** Un sol con disco de 6° desde arriba a la izquierda y algo al frente, elevación ~45°: cada edificio, la torre y Nubi proyectan una sombra blanda hacia abajo a la derecha sobre la losa, como en la referencia; relleno bajo y oclusión en los materiales. Las capas de la app se renderizan con *holdout*: para la capa base, los edificios están presentes pero invisibles, así la base lleva sus sombras; para la capa de edificios, la isla es holdout. Apiladas no hay costura, y la sombra de Nubi sigue como elipse en Compose porque Nubi se mueve.

**Qué viene del render (Blender, una sola vez, fuera del teléfono).** Tres capas con fondo transparente, misma cámara y misma luz, que apiladas forman la isla: `isla_base` (losa, base de tierra, los puntos de la ruta y las cuatro lámparas apagadas), `isla_edificios` (los cuatro lugares con sus ventanas cálidas) y `nubi_<pose>` (Nubi sola, recortada al borde). Más las capas de decoración por días (§13.2) y los símbolos de área (§13.4).

**Qué se dibuja en Compose, en vectores, sobre coordenadas que exporta el script** (`isla.json`, §13.3):
- la **ruta del día**: puntos sol más grandes y más claros sobre los tramos del rombo que unen las tres paradas de hoy, y la **guía que avanza** hacia la parada que sigue (`PathEffect.dashPathEffect` con fase animada; sin animaciones, halo fijo);
- las **lámparas encendidas** de las paradas hechas: un disco sol con halo de degradado radial centrado en la posición de cada lámpara (las apagadas ya están en la imagen);
- la **sombra de Nubi** (Ricardo, 10-oct noche: «¿otra cosa que falta es la sombra de Nubi?»; en la referencia es una mancha oscura, suave y grande): elipse de tinta centrada en `nubi.sombra_elipse.centro` del JSON, de **1,15 del ancho de Nubi** y 0,46 de alto, **46 % de opacidad**, borde muy difuso (desenfoque del 7 % del ancho de Nubi, dibujado como degradado radial, no blur), corrida un 4 % a la derecha por la luz. Va en la **misma capa que Nubi** para que la siga al flotar: en el punto más alto, 0,85 del tamaño y 30 % de opacidad; en el más bajo, 1,0 y 46 %. Con «quitar animaciones», fija en el punto medio. Los renders de la página la muestran ya compuesta así (`compose.py`);
- el **halo** de la lámpara de la Antena (Ricardo: «la luz que sale de la torre en el ejemplo brilla más»): degradado radial sol centrado en `halo_antena.centro` del JSON, radio 7,5 % del ancho de la isla, 60 % de opacidad al centro (de noche, 75 %), dibujado sobre la capa de edificios y bajo Nubi; las lámparas chicas quedan como están. Los renders de la página lo muestran ya compuesto así. Las ventanas de noche: discos de degradado radial sol al 35 %.
- **Nubi mira hacia el lugar tocado** (Ricardo, 10-oct noche): en reposo mira al frente (`nubi_hola`). Al tocar un lugar, fundido de 180 ms a `nubi_mira_<lugar>` (render con las pupilas, la cara y un leve giro del cuerpo hacia ese lugar), desplazamiento del 4 % del ancho de la isla hacia el lugar y ladeo de 6° hacia él, con la sombra siguiéndolo; a los 2 s vuelve al frente con el mismo fundido. Con «quitar animaciones» solo cambia la imagen, sin desplazamiento ni ladeo. Regla: reacciona al toque de la persona, nunca al desempeño. Son cuatro renders más de la capa de Nubi (§14), 1 min cada uno.

**Por qué así y no todo en vectores.** La referencia que eligió Ricardo es plasticina con luz real; en vectores se llegó al 70 % y con el render al 92 % (página de comparación). Lo que cambia por estado (ruta, lámparas, noche, Nubi) sigue en vectores o en capas intercambiables, así no hay que renderizar combinaciones.

**Encaje en la pantalla.** La isla es un `Box` con `aspectRatio(ancho/alto del recorte común)` (hoy 977/972 = 1,005) y ancho = ancho de pantalla + 28 dp de sangrado lateral (igual que en el boceto), con tope de 480 dp en tabletas; dentro, las tres capas con `ContentScale.FillBounds` y los vectores encima en un `Canvas` que convierte las coordenadas 0-1 del JSON a píxeles del `Box`. Nubi es un `Image` aparte, posicionado por su caja del JSON (`capa_nubi.hola.caja`), para animarlo solo.

**Los edificios se tocan.** Cada lugar es un `Box` transparente de 56 dp como mínimo centrado en `lugares.<nombre>.centro`, con `Role.Button` y nombre «Hangar, Razonamiento» (§12); tocarlo abre Juegos con ese lugar elegido **[reemplazado el 11-oct: abre una lámina con el avance del área y la opción de jugarla; ver «Pedidos de Ricardo para las próximas fases», punto 4. Hoy, en la fase 1, abre el detalle de su área como puente]**. Las áreas tocables no se superponen: entre centros hay más de 120 dp a 360 dp de ancho.

## 7 · Plan por fases para Sonnet (cada fase deja algo que Ricardo prueba en el teléfono)

| Fase | Qué se construye | Qué prueba Ricardo | Días | ¿Antes de la prueba con personas (Etapa 2b)? |
|---|---|---|---|---|
| **1 · Herramienta y la isla fija en Hoy** | `tools/isla/` en el repo (script, compositor, `render.sh`, `isla.json`); capas WebP en `res`; `Island()` con las tres capas apiladas, la sombra de Nubi, el halo de la torre y Nubi `hola`; **el fondo de noche como capa** (la opción que elija Ricardo, §14); Hoy reordenada: saludo, farolitos, isla, globo, 3 paradas, un botón | Hoy con la isla de plasticina sobre el fondo nuevo, sin movimiento; el camino visible desde el primer segundo | 2-3 | Sí |
| **2 · Ruta, lámparas y toque** | ruta del día, lámparas encendidas, sombra de Nubi y halos desde `isla.json`; cada edificio tocable con semántica; prueba de que las anclas del JSON caen dentro del recorte | Tocar un edificio abre su lugar; al terminar un juego la lámpara se enciende | 2 | Sí |
| **3 · Movimiento, mirada y noche** | respiración, flotación, titileo y marcha; **Nubi mira hacia el lugar tocado** (4 renders de mirada, fundido, desplazamiento, ladeo, sombra que sigue); versión sin animaciones; de noche: tinte, halo de la torre al 75 % y Nubi `descansa` | Hoy con vida; tocar un edificio hace que Nubi lo mire y se incline hacia él; con «quitar animaciones» todo quieto y solo cambia la mirada; de noche descansa | 2 | Sí |
| **4 · Nubi en toda la app** | las 5 poses renderizadas reemplazan los 4 WebP viejos en Hoy, Juegos, ficha, bienvenida y celebraciones; `nubi_recursos.py` deja de generar Nubi | Nubi nueva en todas partes; de noche descansa | 2 | Sí |
| **5 · Símbolos de área, Juegos y barra** | símbolos desde los renders (o glifos vectoriales a 24-32 dp si no se leen), fichas de lugar en 2 × 2, lista de juegos del lugar, «Ver todos los juegos»; **la repisa crema como la losa y los tres íconos de plasticina renderizados en dos estados** (§4.4, §13.5); se quitan «entrenamos» y el cerebro | Llegar a cualquier juego en dos toques; la lista plana como alternativa; la barra nueva en las tres pestañas | 3 | Sí |
| **6 · Avance** | constancia, la isla en pequeño con las 4 capas de decoración por días jugados, lugares con barras, medidas | Avance nueva; con 7 días ve la primera decoración | 3 | Después (Etapa 3 de la hoja de ruta) |

Total: 14-16 días; antes de la Etapa 2b, 11-13. Cada fase cierra con `verificar-todo.sh --solo-app --instalar` y capturas Roborazzi nuevas de Hoy, Juegos y Avance (día, noche, 30 días, sin animaciones). **Sonnet no necesita Blender para ninguna fase:** las imágenes y el JSON van al repo ya generados; Blender solo hace falta cuando cambia el diseño de la isla (§13.1), y lo corre quien tenga el PC de Ricardo.

**Pruebas que acompañan:** un test de contraste que calcule la tabla de §1.1 contra los tokens (falla si baja de 4,5:1); la guardia de 14 sp existente; `VisibleTextsGuardTest` con «entrenamos» en la lista; una prueba de que las decoraciones dependen solo de días jugados; capturas de Hoy con `reduceMotion = true`; una prueba unitaria que lea `isla.json` y compruebe que las 4 anclas de lugares, las 4 lámparas y la sombra de Nubi están dentro de 0-1 y separadas al menos 0,12 entre sí (si alguien regenera las imágenes con otra cámara, falla antes de llegar al teléfono).

## 8 · Lo que no cambia y lo que se vigila
- Los juegos de Unity siguen igual por ahora (arcilla nocturna). El salto app → juego se suaviza en una tarea posterior llevando el grano y la repisa a la cuenta regresiva, la pausa y la pantalla final.
- Siguen las reglas: nada de estrellas con puntas, medias lunas ni cruces; sin emojis; sin caras que reaccionen al desempeño; sin «cerebro», «entrenamiento» ni «cognitivo»; sin percentiles ni comparaciones; patentes: el mapa de la isla **no** es una «red social dentro de un mapa» (no hay otras personas) ni un portal que entregue juegos (todo vive en el APK): se respeta la regla permanente 3 del 9-oct.
- `nubi_recursos.py` sigue generando el ícono adaptativo; conviene rehacer el ícono con la Nubi renderizada cuando se cierre la fase 4 (un render de frente a 432 px).
- Blender es GPL y eso no alcanza a las imágenes que produce; el script es propio; la única tipografía en el render es la **Ñ** del letrero, hecha con la fuente interna de Blender (Bfont, derivada de DejaVu Sans, licencia Bitstream Vera: libre, sin atribución obligatoria en imágenes). Las referencias de IA no entraron al proceso: ni como entrada del script ni como textura.

## 9 · Referencias generadas con IA (no son arte)
Hubo tres imágenes de referencia (Hoy, Juegos, Avance) hechas con IA a partir de descripciones de texto, sin archivos de Nubi, autorizadas por Ricardo el 10-oct solo como referencia de acabado. NO son arte y NO están en el repo; la de Avance traía la palabra «POST» en el correo (no va). La IA no entró al proceso: ni como entrada del script ni como textura. Términos de la API del generador (no verificados): sin propiedad del proveedor sobre las salidas, pero sin protección de autor en EE. UU. para imágenes hechas solo por IA; por eso son referencia y no arte final.

---

## 10 · Nombres de los lugares (revisión del 10-oct, tarde)
«Correo» chocaba con el juego **Correo Estelar** (Memoria) y «Taller» con **Engranajes: Taller de reparación** (Razonamiento): la persona buscaría ese juego en el lugar. Nombres definitivos: **Observatorio** (Memoria), **Antena** (Atención), **Hangar** (Razonamiento), **Biblioteca** (Lenguaje). Comprobado contra los 18 nombres de juego de `CLAUDE.md`: ninguno coincide ni se parece. Única asociación cruzada posible: «Lluvia de meteoros» (Lenguaje) podría buscarse en el Observatorio; se resuelve con la lista plana «Ver todos los juegos» y con el nombre del lugar en la ficha de cada juego.

## 11 · Qué se hornea en el render y qué se dibuja encima (reemplaza la tabla de fidelidad vectorial)

| Elemento | Dónde vive | Cambia con |
|---|---|---|
| Losa crema con huellas suaves, base de tierra, puntos de la ruta (apagados), 4 lámparas apagadas | `isla_base` (render) | nunca |
| Los 4 edificios con ventanas cálidas, telescopio, engranajes, toldo, letrero Ñ, buzón | `isla_edificios` (render) | nunca |
| Nubi en 5 poses | `nubi_<pose>` (render, 5 archivos) | hora y pantalla, nunca desempeño |
| Nubi mirando a cada lugar | `nubi_mira_<lugar>` (render, 4 archivos) | el toque de la persona sobre un lugar |
| Halo de la lámpara de la torre | Compose, degradado radial sobre `halo_antena` del JSON | día/noche |
| Fondo de noche elegido (luceros de plasticina con relieve + nebulosa suave) | capa de fondo: degradado y nebulosa en Compose + un lucero renderizado una vez (`fondo_lucero`) colocado por código con semilla fija, con zonas sin luceros bajo el saludo y la barra | nunca (de noche puede bajar su opacidad) |
| Decoraciones por días jugados (lámparas en la ruta, banderines, jardín, muelle con nave) | 4 capas más (render), apiladas sobre `isla_base` | hitos de 7, 14, 30 y 60 días jugados |
| Ruta del día (puntos resaltados y guía que avanza) | Compose, `Canvas` sobre `isla.json` | la ruta de hoy y la parada que sigue |
| Lámparas encendidas y sus halos | Compose, discos con degradado radial | paradas hechas |
| Sombra de Nubi | Compose, elipse en la capa de Nubi | la flotación |
| Noche | Compose: tinte `ink` al 25 % sobre base y edificios, halos en ventanas y lámparas, Nubi `descansa` | la hora (y «ya pasaste por la isla hoy») |
| Textura de las superficies de interfaz (tarjetas, fichas, botones) | Compose, grano de §1.2 | nunca |
| Repisa de la barra (crema de la losa, huellas) | Compose: `creamIsla` + grano + tira de huellas horneada repetible | nunca |
| Los 3 íconos de la barra (isla, cohete, cumbre con banderín) | render, 2 estados cada uno (6 archivos) | pestaña elegida |

Lo que la referencia de IA tiene y el render **ya iguala**: volumen con luz real, huellas de plasticina, sombra de contacto. Lo que **no se imita a propósito**: la sombra de Nubi horneada (sería fija mientras Nubi flota), la ruta y las lámparas horneadas (habría que renderizar combinaciones). Rendimiento y memoria: §14 y §15.

## 12 · Ajustes de la versión 3 (10-oct, noche) y semántica de accesibilidad
- **Nubi flota arriba a la derecha del centro de la isla** (en el render, posición `nubi.centro` del JSON), con su sombra en la losa del centro; los cuatro lugares van en rombo (Observatorio a la izquierda, Antena arriba a la izquierda, Biblioteca a la derecha, Hangar abajo) y ninguno queda bajo Nubi ni bajo la lámpara de la torre. El Hangar se ve completo: engranaje grande al frente izquierdo, portón y engranaje chico a la derecha, chimenea atrás.
- **La Ñ se lee a 24 dp:** en el letrero renderizado la Ñ ocupa el 60 % del letrero y se lee en la isla (comprobado a 1400 px y a tamaño de teléfono en la página de comparación). Para el glifo chico vectorial, si se usa: `Text` con la N y un `Path` para la tilde (no depender del glifo tipográfico a tamaños chicos).
- **Juegos:** las cuatro fichas de lugar en 2 × 2 (72 dp), con glifo y nombre en fila; la lista usa el planeta propio de cada juego (`MiniPlanet`) y el glifo del lugar queda en el rótulo de la sección.
- **Semántica (obligatoria en Compose):** cada pestaña de la repisa es `Role.Tab` con `selected` y `contentDescription` «Hoy», «Juegos», «Avance»; cada edificio de la isla y cada ficha de lugar es `Role.Button` con nombre «Hangar, Razonamiento» y `stateDescription` «elegido» cuando corresponde; las paradas del camino son botones con «Constelaciones, Observatorio, hecho»; los farolitos de la semana llevan una sola descripción de grupo («Tu semana: 3 de 4 días»). El boceto en HTML usa `role="tab"`, `role="button"`, `aria-selected` y `aria-pressed` como referencia; TalkBack debe leer lo mismo.


## 13 · Flujo de arte: el render en el repo

### 13.1 Herramienta y comando
- Van al repo, en `tools/isla/` (ya están, Tareas 82 y 83): `isla.py` (geometría, materiales, luz, cámara, compositor, capas y JSON), `componer.py` (recortes, WebP a 2× y `isla.json` normalizado), `tablero.py` (el fondo) y `render.sh`, que hace todo de corrido (ver su README):
  ```
  BLENDER=/ruta/a/blender bash tools/isla/render.sh
  ```
  `--solo-nubi` (por hacer: hoy `--layers` renderiza las tres capas; falta la opción de renderizar solo la capa de Nubi con otra pose) y las poses `explora`, `piensa` y `celebra` (hoy existen `hola` y `descansa`) son lo que falta en el script. El `.blend` no va al repo: se regenera con el script en 10 s.
- Requiere Blender 4.5 LTS (portátil, 300 MB; no se instala en el teléfono ni en la nube de compilación). Tiempo en ese PC: las tres capas más la isla completa, 7 min 22 s; cada pose más, 1 min. Determinista: mismo script, misma imagen (semilla fija de Cycles).
- `tools/isla/out/` queda fuera de git (`.gitignore`); al repo van solo los WebP finales en `res` y `isla.json`.

### 13.2 Salidas, carpetas y peso
- Ancho común en dp: **350 dp** para la isla (el `Box` la escala al ancho de pantalla más el sangrado). Dos densidades: `drawable-xxxhdpi/` con el recorte a 2× (977 × 972 px, para xxhdpi y xxxhdpi, que es la del Motorola) y `drawable-xhdpi/` con el 1× (488 × 486 px, para mdpi, hdpi y xhdpi). Android elige la cubeta más cercana y escala; en `drawable-nodpi` no van, porque ahí no se escalarían por densidad.
- Nombres: `isla_base.webp`, `isla_edificios.webp`, `nubi_hola.webp`, `nubi_descansa.webp`, `nubi_explora.webp`, `nubi_piensa.webp`, `nubi_celebra.webp`, `isla_deco_lamparas.webp`, `isla_deco_banderines.webp`, `isla_deco_jardin.webp`, `isla_deco_muelle.webp`, `lugar_observatorio.webp`, `lugar_antena.webp`, `lugar_hangar.webp`, `lugar_biblioteca.webp`, `tab_hoy.webp`, `tab_hoy_on.webp`, `tab_juegos.webp`, `tab_juegos_on.webp`, `tab_avance.webp`, `tab_avance_on.webp`, `repisa_huellas.webp`, `nubi_mira_observatorio.webp`, `nubi_mira_antena.webp`, `nubi_mira_hangar.webp`, `nubi_mira_biblioteca.webp`, `fondo_lucero.webp` (planeta y nubecita quedan fuera: fondo descartado). `isla.json` va en `res/raw/isla.json`.
- Peso medido (ronda 11, WebP calidad 88): `isla_base` 28 KB a 2× y 16 a 1×; `isla_edificios` 34 y 19; `nubi_hola` 10 y 6. Estimado para el resto: 4 poses más 64 KB; 4 decoraciones unos 160 KB (capas con poco contenido); 4 símbolos 20 KB; 6 íconos de barra unos 20 KB y la tira de huellas 8 KB. **Total estimado: unos 380 KB en las dos densidades**, contra 354 KB que pesan hoy los cuatro Nubi WebP que se retiran: el APK (112 MB en depuración, por Unity) no cambia de tamaño.

### 13.3 `isla.json`: las coordenadas que Compose necesita
El script calcula con la cámara del render la posición en pantalla de cada ancla y el compositor las normaliza (0-1) al recorte común. Comprobado sobre la ronda 11 dibujando las anclas encima de la imagen (`r11-cycles/app/comprobacion-anclas.png`): caen donde corresponde.
```
{
 "losa":     {"esquinas": [[x,y]×4], "centro": [x,y]},
 "lugares":  {"observatorio": {"pie": [x,y], "centro": [x,y], "techo": [x,y], "area": "memoria"}, "antena": …, "hangar": …, "biblioteca": …},
 "paradas":  {"rombo": [[x,y]×4]},          // vértices de la ruta: observatorio, antena, biblioteca, hangar
 "lamparas": [[x,y]×4],                     // tope de cada lámpara, en el mismo orden que los tramos
 "nubi":     {"centro": [x,y], "sombra": [x,y], "base": [x,y]},
 "imagen":   {"recorte_px_2x": [977, 972], "relacion_alto_ancho": 0.9949},
 "capa_nubi": {"hola": {"caja": [[x0,y0],[x1,y1]], "px_2x": [411, 257]}, "mira_observatorio": {…}, "mira_antena": {…}, "mira_hangar": {…}, "mira_biblioteca": {…}},
 "halo_antena": {"centro": [x,y], "radio": 0.11, "opacidad": 0.6, "color": "#FFC86A"},
 "mirada":   {"nota": "fundido 180 ms, desplazamiento 4 % hacia el lugar, ladeo 6°, 2 s, vuelve a hola"}
}
```
En Compose: `val a = json.lugares.hangar.centro; Offset(a[0]*size.width, a[1]*size.height)`. Si se regenera la isla con otra cámara, cambian la imagen y el JSON juntos: nada se mide a mano.

### 13.4 Símbolos chicos de área desde los renders
Un modo del script (`--simbolos`, por hacer) renderiza cada edificio solo, de frente y con la misma luz, a 192 px de alto, fondo transparente; el compositor recorta y guarda 192 px (2×) y 96 px (1×). Prueba de aceptación: captura de Juegos con las cuatro fichas a 40 dp y de la lista de paradas a 34 dp; si a 24 dp (rótulos) no se distinguen, ahí van los glifos vectoriales (§2).

### 13.5 Íconos de la barra desde el render
Un modo del script (`--iconos`, por hacer) arma tres objetos chicos con el material de plasticina de la isla (mismo `clay()`, mismas huellas suaves) y los renderiza con la **misma luz de estudio y la misma elevación de cámara** que la isla, cada uno solo y con fondo transparente, en dos estados:
- `tab_hoy`: losa crema en miniatura sobre base de tierra, con la cúpula azul y la torre con lámpara; `tab_hoy_on`: la lámpara de la torre encendida con halo.
- `tab_juegos`: cohete redondo lavanda con ventana crema y tres aletas sol; `tab_juegos_on`: ventana encendida y una llama corta de plasticina ámbar (sin puntas).
- `tab_avance`: el farol de la ruta (pie y poste tierra, vidrio translúcido, varillas, sombrero, argolla, lucero apagado dentro); `tab_avance_on`: el lucero encendido con halo. Elegido por Ricardo entre cuatro alternativas renderizadas (`--alternativas`: brote, globo, frasco, farol); las otras tres quedan descartadas y sus renders en `alternativas-prueba/`.
Salida: 224 px de alto, recortados al contenido; el compositor guarda 112 px (2×) y 56 px (1×) para 40 dp. Prueba de aceptación: captura de la barra a 360 dp con los tres íconos y una pestaña elegida; los tres deben distinguirse entre sí a 40 dp y el estado elegido debe notarse aunque la captura se pase a escala de grises. También se renderiza la **tira de huellas** de la repisa (`repisa_huellas.webp`: un plano de plasticina crema iluminado desde arriba, 512 × 176 px, del que se guarda solo la variación de luz como tinta al 10 % sobre transparente, repetible en horizontal).

## 14 · Variantes que se renderizan una sola vez

- **Las 5 poses de Nubi** (siempre por momento o pantalla, nunca por desempeño): `hola` (ojos abiertos, sonrisa), `descansa` (párpados cerrados como dos arcos, misma sonrisa; ya existe en el script), `explora` (aro de casco y linterna frontal, pose un poco inclinada), `piensa` (una mano al mentón, mirada arriba), `celebra` (brazos arriba, boca más abierta). Cada pose es un render de la capa de Nubi sola (1 min) con la misma cámara, así la sombra de Compose sirve para todas. Las tres nuevas hay que modelarlas en el script (brazos como cilindros curvos, casco como aro); 1 día de trabajo de script.
- **Decoraciones por días jugados**, cada una como capa transparente sobre `isla_base` y bajo `isla_edificios`: `lamparas` (7 días: las cuatro lámparas de la ruta encendidas de forma permanente, con halo horneado), `banderines` (14: una cuerda de banderines pastel entre la Antena y la Biblioteca), `jardin` (30: arbustos y flores redondas en las esquinas libres de la losa), `muelle` (60: un muelle corto en el borde delantero y una nave redonda de plasticina amarrada). Se apilan: a los 60 días se ven las cuatro. Los hitos cuentan días jugados en total, no seguidos.
- **Noche**: sin render aparte. Compose pinta un tinte `ink` al 25 % sobre `isla_base` e `isla_edificios` (con `BlendMode.Multiply`), suma halos de degradado radial sol sobre las ventanas (posiciones derivadas de `lugares.*.centro`) y sobre las lámparas, y cambia Nubi a `descansa`. Si Ricardo quiere más noche, un render nocturno de base y edificios cuesta 5 min y 60 KB más; se decide viéndolo.
- **Las 4 miradas de Nubi** (`--mirada observatorio|antena|hangar|biblioteca` con `--solo-nubi`): mismas pose y cámara, pupilas corridas hacia el lugar (hasta 0,065 en x y 0,055 en z), cabeceo de 0-8°, ladeo de ±4-6° y giro de ±3-18° del cuerpo sobre sus ejes propios. Un render de la capa de Nubi por mirada.
- **Fondo de noche como capa**, en su versión final (ronda 24): un **tablero físico**, lámina de plasticina azul noche con textura sutil y la nebulosa pintada en el material, iluminada por la misma luz de la isla (caída de luz y viñeta naturales), con los **luceros como bolitas de plasticina crema apoyadas y medio hundidas** en tres tamaños, cada una con su sombra de contacto; se renderiza una vez (`--tablero`, 720 × 1200 a 1× del teléfono) y va como imagen de fondo; bajo el saludo y la barra no hay luceros y el contraste del texto se mide igual (`tablero.py`). Antes de esto se había elegido (Opus, 10-oct noche; Ricardo: «me gustó la A o la C, decide tú»): luceros redondos de plasticina de varios tamaños con relieve más una nebulosa suave lila y azul, sin nubecitas; el tablero conserva esa elección. Motivos: Nubi es una nube y las nubes de fondo compiten con ella y se leen como humo; los luceros son lo más fiel a la referencia y lo de mejor contraste; la nebulosa da la profundidad que pidió Ricardo. Descartadas: **B** (luceros más planeta lejano con anillo: un guiño espacial que no aporta a la isla) y **C** (nubecitas lejanas y nebulosa: las nubecitas compiten con Nubi). Cómo se construye: el degradado de noche se dibuja en Compose (`night0`→`night2`); la nebulosa son tres elipses de color (lila `#7860BE`, azul `#5082AA`) al 20-28 % con desenfoque amplio, dibujadas como degradados radiales; los luceros son un solo render de plasticina (`fondo_lucero`, con la luz de la isla) colocado por código con semilla fija, 1,2-3 % del ancho, con **zonas sin luceros bajo el saludo (x 4-70 %, y 8-22 %) y bajo la barra**. Contraste medido del texto en la zona del saludo con el fondo elegido: crema 8,6:1, sol 6,9:1 (mínimo 4,5:1). Las opciones se comparan en `fondo.py` (A, B, C y D = elegida); la página de comparación usa ya el fondo elegido bajo la isla.
- **Símbolos de área**: §13.4. **Íconos de la barra**: §13.5 (seis renders chicos más la tira de huellas de la repisa).

## 15 · Movimiento y accesibilidad con capas de imagen

- **Qué se mueve y cómo** (misma tabla de §5): la isla respira moviendo `translationY` de un `graphicsLayer` que contiene `isla_base`, las decoraciones e `isla_edificios` (una sola capa compuesta, cacheada); Nubi flota con su propio `graphicsLayer` (imagen + sombra elipse: la sombra se encoge al 85 % y baja a 18 % de opacidad en el punto más alto); las lámparas de las paradas de hoy titilan en `alpha`; la guía de la ruta avanza con la fase del guion. Nada vuelve a decodificar un bitmap por cuadro: los `painterResource` se cargan una vez.
- **Con «quitar animaciones»** (`ReduceMotion.kt`): isla y Nubi quietas (Nubi en su punto medio, sombra fija), lámparas encendidas fijas, guía fija y **halo fijo** en la parada que sigue, cubo de la pestaña sin rebote. Ninguna información depende del movimiento: la parada que sigue se reconoce por sol, halo y el texto «es la que sigue».
- **Semántica de cada edificio tocable** (obligatoria): `Box` de 56 dp mínimo centrado en `lugares.<nombre>.centro`, `Modifier.semantics { role = Role.Button; contentDescription = "Hangar, Razonamiento" }`, `stateDescription = "hecho hoy"` cuando la parada del día en ese lugar está cumplida; orden de foco: Observatorio, Antena, Biblioteca, Hangar (el mismo del rombo). La imagen de la isla lleva `contentDescription = null` (decorativa) y el `Box` de la isla un `contentDescription` de grupo: «Tu isla: hoy la ruta pasa por Observatorio, Antena y Biblioteca». Nubi: `contentDescription = null` salvo en la bienvenida («Nubi te saluda»). Nada cambia respecto de §12 para pestañas, fichas y paradas.
- **Tamaño de fuente grande** (`fontScale` 1,3-2,0): la isla no crece; crecen el saludo, el globo, las paradas y el botón, que están fuera de la imagen. A 2,0 el globo pasa a dos líneas y la isla sigue entera.

## 16 · Riesgos y cómo se controlan

| Riesgo | Dato | Control |
|---|---|---|
| **Memoria en el teléfono** | A 2× las dos capas de la isla decodificadas pesan 977 × 972 × 4 B = 3,8 MB cada una; Nubi 0,4 MB; con 4 decoraciones, unos 20 MB en total en el peor caso (60 días, Hoy y Avance con la isla). El Motorola edge 60 pro de Ricardo tiene 12 GB de RAM (medido por `adb`); un gama baja de 2-3 GB lo soporta igual (el límite por app suele ser 256-512 MB). | Capas recortadas al contenido (Nubi 411 × 257, no 1400 × 1330); decodificar con `painterResource` (cachea por recurso); en Avance, la isla en pequeño reutiliza los mismos recursos escalados, no otros. Medir con `adb shell dumpsys meminfo com.example` en Hoy: la meta es que la isla no sume más de 25 MB. |
| **Rendimiento (cuadros lentos)** | Hoy dibuja 3-7 bitmaps grandes más vectores, dentro de un `HorizontalPager`. | Una capa compuesta cacheada para la isla y otra para Nubi; solo `translationY`, `alpha` y la fase del guion cambian por cuadro; pausar las animaciones cuando Hoy no está visible. Medir con `adb shell dumpsys gfxinfo com.example` tras 30 s en Hoy: cuadros lentos < 5 %. Si no se cumple, quitar la respiración de la isla y dejar solo Nubi. |
| **Tamaño del APK** | +≈350 KB de recursos en dos densidades; se retiran 354 KB de los Nubi WebP actuales. El APK de depuración pesa 112 MB por Unity. | Nada que hacer; vigilar que nadie suba los PNG de `tools/isla/out` (están en `.gitignore`). |
| **Pantallas de distinto ancho** | La isla se escala al ancho: a 320 dp los edificios miden unos 55 dp y las áreas tocables de 56 dp siguen separadas (más de 100 dp entre centros); a 412 dp (el Motorola a densidad 450 da 434 dp de ancho) se ve a 2× nítida; en tabletas el tope de 480 dp la centra con márgenes de noche. | `aspectRatio` fijo del JSON; áreas tocables por coordenadas normalizadas; capturas Roborazzi a 320, 360, 412 y 600 dp. |
| **Legibilidad de los símbolos chicos** | A 24 dp un edificio renderizado puede verse como una mancha de color. | Prueba de aceptación de §13.4; respaldo: glifos vectoriales para 24-32 dp. |
| **Regeneración del arte** | Cambiar la isla exige Blender 4.5 y 8-10 min en el PC de Ricardo; en la nube de compilación no hay Blender. | Las imágenes y el JSON se versionan ya generados; el test del JSON (§7) detecta imágenes y JSON desparejados. |
| **Noche demasiado oscura para mayores** | El tinte al 25 % sobre la losa crema baja el contraste de los edificios. | Medir en captura nocturna: los edificios deben seguir distinguiéndose entre sí; si no, bajar el tinte al 15 % o renderizar la variante nocturna. |
| **Patentes y reglas** | La isla no es una red social en un mapa ni un portal que entregue juegos (todo vive en el APK); sin estrellas con puntas, lunas ni cruces (el plato de la Antena se quitó por eso); sin caras que reaccionen al desempeño. | Las reglas de §8 se mantienen; cualquier función en línea que toque el acceso a juegos pasa antes por el abogado. |

