# La isla de plasticina (tools/isla)

Las imágenes de la isla de Hoy no se dibujan a mano: las genera un script de Blender (`isla.py`, Blender 4.5, solo herramientas de Blender, sin texturas externas) y de ahí salen las capas que usa la app. Es un paso de ARTE, a mano: ninguna verificación lo corre y **la app no depende de Blender** (lo que entra al repo son los WebP y `isla.json` ya hechos).

## Qué hay

| Archivo | Para qué |
|---|---|
| `isla.py` | El script `bpy`: isla, edificios (Observatorio, Antena, Biblioteca, Hangar), Nubi, farolas y el tablero del fondo, todo por código. Escribe PNG con fondo transparente, `isla.json` (posiciones en píxeles) y `tiempos.txt`. |
| `componer.py` | Recorta las capas a un encuadre común, las guarda en WebP a 2× y normaliza `isla.json` a coordenadas 0-1. |
| `tablero.py` | Convierte los renders del tablero en las DOS capas del fondo (`fondo_lamina.webp` y `fondo_luceros.webp`), mide cuánto se parece su apilado al B completo y el contraste del saludo sobre la lámina. |
| `render.sh` | Corre todo en orden (Cycles, 112 muestras, ~20 min por CPU). Al final imprime las líneas para copiar a la app. |
| `out/` | Salida (git-ignorada). |

```
BLENDER=/ruta/a/blender.exe bash tools/isla/render.sh        # lámina B (azul marino)
```

El paso del tablero (lámina, luceros y `tablero.py`) se corrió de verdad en la Tarea 83. Los pasos de la isla y de Nubi (1 y 2 de `render.sh`) son los de la cadena con la que se hicieron las imágenes vigentes (ronda 26b, 11-oct-2026) pero no se volvieron a correr desde el repo.

## El fondo en dos capas (Tarea 83)

El fondo B (tablero azul marino con luceros de plasticina) son DOS imágenes apiladas: `fondo_lamina` (la lámina con su nebulosa y su textura, sin luceros) y `fondo_luceros` (las bolitas con su sombra de contacto, transparente, encima). Desde la Tarea 87 van FIJAS detrás de Hoy, Juegos y Avance (~80 luceros visibles; antes solo se veían en Hoy, con 17). En la app: `ui/components/BoardBackground.kt` (sin fundido por pestaña).

Opciones de `isla.py` para el tablero (se usan siempre con `--tablero --lamina E`, «Espacio profundo», el fondo vigente desde la Tarea 122; `--lamina B` es el azul marino de antes y `A` el lila descartado; para E, `render.sh` se corre con `E` y trae luces propias: `--tabsol` 1,6 y `--tabcentro` 190):

| Opción | Qué renderiza | Archivo |
|---|---|---|
| (ninguna) | el B entero (lámina y luceros) SIN los luceros de la zona libre de arriba | `fondo_tablero.png` |
| `--sin-luceros` | solo la lámina, opaca | `fondo_lamina.png` |
| `--solo-luceros` | solo los luceros y su sombra, transparente (la lámina es «shadow catcher» de Cycles: no se ve, solo recibe la sombra y el rebote) | `fondo_luceros.png` |
| `--b-aprobado` | no quita los luceros de la zona libre de arriba (la secuencia llega a `N_LUCEROS`; los primeros 58 son los del B aprobado de la ronda 26b) | (se suma a cualquiera de las anteriores) |

Los luceros salen de la MISMA semilla de colocación (`random.Random(5)`) que el B aprobado: los primeros 58 quedan donde estaban y la secuencia sigue hasta `N_LUCEROS` = 95 colocados (~80 visibles: los que caen en la zona libre de arriba o en la banda de la barra se calculan y NO se crean). La única zona libre (`ZONAS_HOY` en `isla.py`, x 0-100 %, y 3-28 %) es el encabezado: el saludo de Hoy y los títulos de Juegos y Avance, que siempre caen en el mismo lugar.
El texto de Hoy medido en las pruebas llega a y 22,5 % (encabezado); «Tu avance» está en y 5-8 % y «¿Qué jugamos hoy?» con su sugerencia en y 10-20 %. Bajo botones, piedritas y tarjetas no se excluye nada (son opacas); los demás textos claros directo sobre el fondo se leen por su contorno navy (`OnBoardText`).
Si se agrega un título grande directo sobre el fondo que caiga siempre en el mismo lugar, hay que ampliar la zona y volver a correr `render.sh`; `HoyIslaScreenshotTest` falla si el saludo, la fecha o la racha de Hoy caen sobre un lucero (e imprime en qué fracción de la imagen cae cada texto: líneas «ZONAS …»), y `BoardBackgroundContrastTest` cuenta los luceros (70-90) y que ninguno se pegue a otro.

**Apilados dan el B.** `tablero.py` mide el apilado (los WebP que entran a la app) contra el B completo: media 0,85/255, p99 3, y solo el 0,23 % de los píxeles difieren en más de 8 (máximo 27); es el mismo nivel de ruido que hay entre dos renders del MISMO B (media 0,85, p99 3). Detalle: el «shadow catcher» solo da bien la máscara (dónde hay luceros y sombras); su sombra sale gris por el canal alfa y la real es más cálida (rebote del lucero) y menos azul, así que dentro de esa máscara el color y la opacidad de la capa se calculan para que `lámina · (1-α) + color · α` dé el píxel del B completo.

## Cómo entra a la app

- `isla_base.webp`, `isla_edificios.webp`, `isla_nubi_hola.webp`, `fondo_lamina.webp` y `fondo_luceros.webp` van en `app/src/main/res/drawable-nodpi/`, a 2×. **No** en carpetas de densidad: Android las reescala al decodificarlas (un teléfono de 420 dpi las achicaba a 0,66 y se veían borrosas). `nubi_hola` se llama `isla_nubi_hola` porque ya existe un `nubi_hola` en la app.
- `isla.json` va en `app/src/main/res/raw/`. Sus coordenadas son 0-1 sobre el recorte común (930 × 1042 px); `data/IslaLayout.kt` lo lee y `ui/components/Island.kt` dibuja sobre él la sombra de Nubi, el halo de la torre y los de las farolas.
- `IslaJsonTest` comprueba que las imágenes y el JSON son del mismo render (mismo tamaño, Nubi en su caja) y que los edificios, las farolas y la sombra no se pisan. `BoardBackgroundContrastTest` comprueba que el saludo se lea sobre la lámina (≥ 4,5:1), que la lámina no tenga luceros y que la capa de luceros no tenga ninguno en las zonas libres de Hoy.
- Si se cambia el encuadre o los edificios, hay que volver a correr `componer.py` y copiar LAS TRES cosas juntas (imágenes y JSON): mezclarlas de renders distintos desalinea los toques y los halos.

## Las piezas de la isla por medallas (Tarea 134, 18-oct)

Diez piezas que la isla va ganando con las medallas de conducta (spec `auditoria-fable/spec-adopcion-A1-A7.md` §A1): **Constancia** (el jardín: C1 dos arbustos, C2 flores, C3 un árbol, C4 sus frutos), **Variedad** (los banderines: V1 cuerda Antena–Biblioteca, V2 cuerda Observatorio–Antena, V3 un banderín en cada techo) y **Volver** (el muelle: Vo1 los tablones, Vo2 una nave redonda lila amarrada, Vo3 el farolito de la punta, con su halo). Nada con puntas de estrella, medias lunas ni cruces: los banderines son triángulos.

- **`isla.py`: `build_deco()`** arma las diez en colecciones propias `deco_c1` … `deco_vo3` (con `clay()` y los colores del kit; azar propio, así la base, los edificios y Nubi salen idénticos y `isla-completa.png` NO las trae). `isla.py --deco` renderiza cada pieza SOLA (`capa-deco-<id>.png`): las de suelo con la isla de «shadow catcher» (su sombra de contacto sobre la losa) y los edificios y las piezas de abajo de «holdout» (la recortan donde quedan delante, **sin proyectar su sombra**: la de los edificios ya está en `isla_base` y se duplicaría); las de aire (V1-V3) flotan sin sombra. Solo se renderiza el rectángulo de la pieza (`render(..., border=)`): ~15-100 s cada una.
- Opciones: `--deco-solo c1,v1` (solo esas), `--deco-completa` (`isla-completa-deco.png`: la isla con las diez, render real), `--deco-lamina --deco-zoom 2` (primeros planos para la lámina). `render.sh` los corre después de las capas.
- **Posiciones**: el recorte común es el de siempre (930 × 1042; la caja de `isla-completa.png`), así las capas calzan con `isla_base`/`isla_edificios` ya en la app. `isla.json` suma `decoraciones` (`ancla` = donde la pieza toca la isla, `capa` = `suelo` | `aire`, y el `halo` de vo3). **El árbol (C3) está en el borde delantero derecho, entre el Hangar y el muelle**: el único hueco donde la copa no pisa edificios, piezas ni la caja de Nubi (la guía de la spec lo ponía en la esquina derecha, que está debajo de la Biblioteca y fuera del recorte).
- **`componer.py`** deja `isla_deco_<id>.webp` (mismo recorte, la bruma del «shadow catcher» recortada, los bordes del rectángulo renderizado desvanecidos para que la sombra blanda no deje costura, el halo de vo3 horneado). Las diez pesan unos 80 KB (tope de la spec: 200).
- **Lámina**: `python -I tools/previews/isla_piezas.py tools/isla/out/<carpeta>` → `docs/previews/isla-piezas.png` (las diez sueltas, la isla de hoy, con 3, con las 10 y las comprobaciones: tamaños en dp, nada sobre edificios ni zonas tocables ni farolas ni Nubi) y `docs/previews/isla-piezas/` (las diez capas en WebP e `isla-deco.json`).
- **Ricardo aprobó la lámina (19-oct) y las piezas entraron a la app (Tarea 136).** `python -I tools/isla/piezas_app.py [carpeta_de_renders]` copia las diez capas de `docs/previews/isla-piezas/` a `app/src/main/res/drawable-nodpi/`, suma `decoraciones` a `res/raw/isla.json` (sin tocar nada más de ese archivo: la base y los edificios de la app no se re-renderizan) y arma las diez fichas chicas `kit_pz_deco_<id>.webp` (264 × 168 px = 88 × 56 dp, recortadas de los primeros planos `lamina-deco-<id>.png`, sin la sombra de contacto) que usa la celebración de la medalla. La app las dibuja en `Island.kt` y las deriva de las medallas en `data/IslaPiezas.kt`; ver `docs/diseno-isla.md` §14. Si se vuelven a renderizar las piezas, correr este script otra vez (y `tools/previews/isla_piezas.py` para la lámina).

## Qué falta (fases siguientes de `docs/diseno-isla.md`)

Las miradas de Nubi hacia cada edificio (`nubi_mira_*`, ya salen de `render.sh` pero la app aún no las usa), el movimiento (flotación y sombra), la barra de pestañas en 3D y la cortina de arranque.

**Farolas de la ruta (Tarea 88).** `isla.json` lleva `lampara_de` (qué farola, por índice en `lamparas`, es de cada lugar: la asignación de menor distancia total entre el pie del edificio y el tope de la farola) y `farolas_encendidas` (disco sol, halo de 2× el de las apagadas, `pie_dy` y resplandor de la losa). `componer.py` los calcula; si no se re-renderiza la isla, se pueden agregar al `app/src/main/res/raw/isla.json` con el mismo código sin tocar las imágenes.

**Kit de plasticina (Tareas 89 y 90: el estilo de toda la app).** `isla.py --kit` renderiza las piezas de la interfaz con la misma luz, el mismo material `clay()` y el sol de la isla: **losas de nine-slice en tres radios** (chica `ch` = 14 dp para chips y casillas: crema, sol y lima; mediana `md` = 22 dp para botones y tarjetas: sol `#FFCB45`, crema `#F5EBDB`, lima `#9BE564`, cielo `#4CC9F0`, coral `#FF6B4A` y lila `#B8A4FF`; grande `gr` = 30 dp para láminas y paneles: crema), cada una en dos estados (normal y presionada: la mitad de alto, un punto más oscura y de sombra más chica); **discos de 48 dp** (crema con la X de dos churros tinta, crema liso y cielo liso, dos estados: el ícono o la inicial va encima, en Compose); **íconos de churro tinta** (volver, visto, flecha izquierda y derecha, compartir, candado, engranaje y el triángulo de «Jugar»; y, de la Tarea 91, los que reemplazan a los de Material de los paneles: globo, altavoz, vibración, reloj, sin reloj, escudo, info, campana, persona y las flechas arriba y abajo; sin estados: al presionar el disco bajan 2 dp); y (Tarea 91) **siete avatares** de plasticina de 48 dp con su sombra (cohete, farol e isla en miniatura sacados de la barra, engranaje, planeta sin anillo, lucero redondo y gota). Los avatares se QUITARON de la app en la Tarea 93: el modo `--kit-solo avatares` sigue en `isla.py` y `kit.py --con-avatares` los procesa, por si algún día vuelven, pero por defecto `kit.py` no los escribe ni los pone en `kit.json` (y no se copian a `res/`). `kit.py` normaliza los PNG (cuerpo en el mismo sitio en todos los estados, recorta la bruma del «shadow catcher», WebP con alfa) y escribe `kit.json` con el lienzo, los márgenes (donde cae la sombra), el cuerpo y los insets del nine-slice de cada radio.

```
BLENDER=/ruta/a/blender
$BLENDER -b -P tools/isla/isla.py -- --engine CYCLES --samples 112 --kit --out tools/isla/out/kit     # ~4,5 min con Cycles por CPU (Ryzen 5 5625U): 59 imágenes de 1 a 17 s
python -I tools/isla/kit.py tools/isla/out/kit tools/isla/out/kit/app                                  # WebP + kit.json
cp tools/isla/out/kit/app/kit_*.webp app/src/main/res/drawable-nodpi/ && cp tools/isla/out/kit/app/kit.json app/src/main/res/raw/kit.json
```

Opciones de `--kit`: `--kit-px` (píxeles por dp; 3 por defecto —la app—; **6** para el atlas de emblemas de los juegos de Unity: `--kit --kit-solo avance --pieza emblema_ --kit-px 6` y luego `python -I tools/juegos/empaqueta.py emblemas`, Tarea 126), `--kit-solo losas|discos|iconos|avatares` (renderiza solo una parte), `--kit-tilt` (grados de inclinación de la cámara respecto de la vista desde arriba; 30 por defecto: se ve un poco el canto de adelante), `--kit-h` (alto de la losa elevada en dp; 9) y `--kit-exp` (exposición; −0,42: con el sol desde arriba la plasticina clara se quemaba y la crema y la lima salían a 255).

**Glifos de la barra de pestañas (Tarea 156, 21-oct).** `--kit --kit-solo barra` (no entra en `todo`) renderiza tres íconos de churro tinta de 24 dp con la receta de los `kit_i_*`: `kit_i_isla` (la isla con su banderita: Hoy), `kit_i_cohete` (Juegos) y `kit_i_farola` (Avance); `python -I tools/isla/kit_barra.py tools/isla/out/kit-barra` los pasa a WebP a 3 px por dp (sin la bruma del suelo) y suma sus tres piezas a `kit.json` en `tools/isla/out/kit-barra/app/`, de donde se copian a `app/src/main/res` (`drawable-nodpi` y `raw`). La barra vieja (`--barra`: bandeja y cubos `barra_*`) ya no está en la app; su receta queda en `isla.py` como historia.

**Piezas de Avance y Juegos (Tarea 124, 18-oct).** `--kit --kit-solo avance` (no entra en `todo`) carga `kit_avance.py` y renderiza: el **canal** (sobre losa crema y sobre el cielo) y el **churro** (sol y lima), nine-slice horizontal; la **bolita** lima del cambio; los **17 emblemas de juego** (ficha de 64 dp en el pastel de su área, objeto al ~70 %, sin puntas ni cruces); la **farola** apagada y encendida (la misma del ícono de Avance); el **pedestal de lugar** (150 × 56 dp); las dos **colas del globo** de Nubi (16 × 12 dp); las **3 medallas** (Constancia, Variedad, Volver) conseguidas y bloqueadas (plasticina cruda) y el **escudo de liga** en los 7 colores de `RankTier` (mate). Deja `kit_pz_<nombre>.png` y `kit_avance.json` en `--out`; `--pieza emblema_,escudo_` renderiza solo las que empiezan así. ~12 min con Cycles por CPU a 112 muestras. Dos cámaras, las del resto del kit: la inclinada 30° del kit (lo que se acuesta: todo cuelga de `raiz_kit()`, estirada 1/cos, y las esferas `pz_rd_*` cancelan ese estirón) y la de los íconos de la barra (lo que se para: farola y medallas). La hoja: `python -I tools/previews/kit_avance_juegos.py` → `docs/previews/kit-avance-juegos.png` (todo a 3 px por dp sobre el fondo A, y los 17 emblemas juntos) y los WebP con `kit_avance.json` (tamaños, márgenes y cuerpo medidos en la imagen) en `docs/previews/kit-avance-juegos/`. Desde la Tarea 125 entran en la app: `python -I tools/isla/kit_avance_app.py` (con los renders de `tools/isla/out/kit-avance/`) deja los WebP `drawable-nodpi/kit_<canal|churro|bolita|emb_<juego>|farola_<off|on>|pedestal|cola_<izq|der>|med_<familia>[_off]|escudo_<liga>|edif_<área>>.webp` (con la bruma del «shadow catcher» recortada, como `kit.py`) y `res/raw/kit_avance.json` (márgenes y cuerpo medidos, insets de las tiras, placa de las medallas y pie de los edificios); `AvanceKitTest` los vigila. Además de lo anterior, `--kit-solo avance` renderiza los **cuatro edificios de la isla** (`edificio_<área>`, la cámara de la isla en ortogonal; observatorio = Memoria, antena = Atención, hangar = Razonamiento, biblioteca = Lenguaje) para pararlos sobre el pedestal. El emblema de Acoplamiento es el módulo en L entrando en su hueco (no los cuatro cuadrados de colores).
**3× (3 px por dp).** El Motorola va a ~2,625 px por dp: una imagen a 3× se reduce 0,875 y queda nítida; a 2× habría que ampliarla 1,31 y se vería borrosa. Con 3× es 1:1 en pantallas de 480 dpi. Cuestan ~231 KB los 59 archivos (WebP calidad 92). Van en `drawable-nodpi` para que Android no los reescale.
**El centro de la losa va casi liso** (desviación de 0,5-0,9 sobre 255 a lo ancho y a lo alto: `kit.py` lo mide): se estira hasta 5× en horizontal, y una huella o una veta estiradas se verían como un barrido. Las esquinas y los bordes, que no se estiran, llevan el volumen: canto blando con bisel, sombra de contacto y el jaspeado leve del material.

**La Nubi 3D de las pantallas y el ícono (Tarea 92).** Un solo render de frente, con fondo transparente, y `icono.py` deja todo lo que entra a `app/src/main/res/` (ver su cabecera): `drawable-nodpi/nubi_3d.webp` (720 px de ancho), el ícono adaptativo (`mipmap-*/ic_launcher_foreground.png` y `ic_launcher_monochrome.png`: la silueta sólida con dos ojos y la sonrisa calados, medidos en el render), los de Android 7 (`ic_launcher.webp`, `ic_launcher_round.webp`) y el de notificación (`drawable-*/ic_stat_neurovida.png`). El fondo del ícono es `res/drawable/ic_launcher_background.xml` (a mano: el azul marino de la lámina B).

**Nubi viva (Tarea 148, P1 y P3 de `auditoria-fable/propuestas-diseno-visual.md`).** Nubi flota y parpadea (`ui/components/NubiIdle.kt`), y el parpadeo es un fundido a la imagen «descansa» (ojos cerrados), así que hacen falta tres renders más con la MISMA cámara y luz de siempre (`isla.py` no se toca) y un recorte común: `nubi_reposo.py` toma el recorte de la Nubi de ojos abiertos y se lo pone a la de ojos cerrados, y deja `drawable-nodpi/isla_nubi_descansa.webp` (la de la isla, la caja de `isla_nubi_hola`), `nubi_3d.webp` y `nubi_3d_descansa.webp` (de frente, **1080 px de ancho**, antes 720). Imprime el `NUBI_ASPECT` que debe llevar `ui/components/NubiHome.kt` (1080 / 713). El ícono de la app no cambia (sigue saliendo de `icono.py`).

```
B=/c/Users/RURAL7/Tools/blender-4.5.14/blender-4.5.14-windows-x64/blender.exe
$B -b -P tools/isla/isla.py -- --engine CYCLES --samples 112 --width 1400 --solo-nubi --mirada frente --nubi descansa --out tools/isla/out/n148/isla-descansa    # la de la isla, ojos cerrados (~2 min)
$B -b -P tools/isla/isla.py -- --engine CYCLES --samples 112 --width 3800 --solo-nubi --mirada frente --nubi hola     --out tools/isla/out/n148/hola                # de frente, abiertos (~8 min)
$B -b -P tools/isla/isla.py -- --engine CYCLES --samples 112 --width 3800 --solo-nubi --mirada frente --nubi descansa --out tools/isla/out/n148/descansa            # de frente, cerrados (~8 min)
python -I tools/isla/nubi_reposo.py tools/isla/out/t134/capa-nubi.png tools/isla/out/n148/isla-descansa/capa-nubi-frente.png tools/isla/out/n148/hola/capa-nubi-frente.png tools/isla/out/n148/descansa/capa-nubi-frente.png app/src/main/res
```

**La gota de la racha (Tarea 148, P4).** `kit_avance.py` trae `llama_encendida` y `llama_apagada` (32 dp; una GOTA de plasticina con la punta arriba, la forma aprobada de `docs/simbolos-neutros.md`; la encendida, sol con una gota clara por dentro; la apagada, crema lisa) para reemplazar al ícono `Whatshot` de Material en Hoy, Avance, el Perfil y el resumen de la ruta: `isla.py --kit --kit-solo avance --pieza llama_`, y `kit_avance_app.py` las lleva a `kit_llama_on` y `kit_llama_off` (ver su `MAPA`).

```
BLENDER=/ruta/a/blender
$BLENDER -b -P tools/isla/isla.py -- --engine CYCLES --samples 112 --width 3000 --solo-nubi --mirada frente --out tools/isla/out/nubi3d     # ~6,5 min con Cycles por CPU
python -I tools/isla/icono.py tools/isla/out/nubi3d/capa-nubi-frente.png app/src/main/res
```

`--width 3000` deja a Nubi de ~860 px de ancho (el ícono la pide a ≥ 432 px dentro de la zona segura). `nubi_recursos.py` (tools/previews) ya NO genera a Nubi: solo los planetas de las áreas.
